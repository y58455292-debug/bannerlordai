using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;

namespace ClanAI
{
    // Observation only. No movement, target, scoring, combat, or save-data writes.
    // Inspector calls read its existing plain reports; no new profiler is installed.
    internal static class ClanAIDiagnostics
    {
        private static volatile bool _active;
        private static string _path;
        private static long _sequence, _failures, _frame, _nextAt;
        private static int _pendingSave;
        private static bool _finalHealthWritten;
        private static MethodInfo _report, _serialize;
        private static FieldInfo[] _recorderFields;

        public static void BeginSession()
        {
            try
            {
                string log = ClanAIPostVanilla.SessionLogPath;
                if (string.IsNullOrEmpty(log)) return;
                _path = Path.ChangeExtension(log, ".diagnostics.txt");
                _sequence = 0;
                _frame = 0;
                Interlocked.Exchange(ref _pendingSave, 0);
                _finalHealthWritten = false;
                DelegatingMobilePartyAIModel.ResetDiagnostics();
                BindInspector();
                _active = true;
                Snapshot("session_begin", false);
            }
            catch { Interlocked.Increment(ref _failures); }
        }

        private static void BindInspector()
        {
            _report = null;
            _serialize = null;
            _recorderFields = null;
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.GetName().Name != "BannerlordInspector") continue;
                Type perf = assembly.GetType("BannerlordInspector.PerformanceMonitor", false);
                Type json = assembly.GetType("BannerlordInspector.Json", false);
                Type recorder = assembly.GetType("BannerlordInspector.BehaviorDeltaRadiusRecorder", false);
                if (perf != null) _report = perf.GetMethod("Report", BindingFlags.Public | BindingFlags.Static);
                if (json != null) _serialize = json.GetMethod("Serialize", BindingFlags.Public | BindingFlags.Static);
                if (recorder != null) _recorderFields = recorder.GetFields(BindingFlags.Public | BindingFlags.Static);
                break;
            }
        }

        // SyncData may run during serialization: only atomic counters and logging
        // are used here. The richer report is deferred to OnApplicationTick.
        public static void RecordSaveBoundary()
        {
            if (!_active) return;
            try
            {
                ClanAIPostVanilla.WriteExternalLog(
                    DelegatingMobilePartyAIModel.DiagnosticSummary("save_serialization"));
                Interlocked.Exchange(ref _pendingSave, 1);
            }
            catch { Interlocked.Increment(ref _failures); }
        }

        public static void Tick()
        {
            if (!_active) return;
            try
            {
                if (Interlocked.Exchange(ref _pendingSave, 0) != 0)
                    Snapshot("save_followup", false);
                else if (((++_frame & 63) == 0) && Stopwatch.GetTimestamp() >= _nextAt)
                    Snapshot("periodic", false);
            }
            catch { Interlocked.Increment(ref _failures); }
        }

        public static void EndSession(string reason)
        {
            if (!_active) return;
            try
            {
                Snapshot(reason, true);
                ClanAIPostVanilla.WriteExternalLog(
                    DelegatingMobilePartyAIModel.DiagnosticSummary(reason));
            }
            catch { Interlocked.Increment(ref _failures); }
            finally { _active = false; }
        }

        private static string PerformanceJson()
        {
            if (_report == null || _serialize == null)
                return "{\"available\":false,\"reason\":\"Inspector report API unavailable\"}";
            try
            {
                object report = _report.Invoke(null, null);
                return (string)_serialize.Invoke(null, new object[] { report });
            }
            catch
            {
                Interlocked.Increment(ref _failures);
                return "{\"available\":false,\"reason\":\"Inspector report failed\"}";
            }
        }

        private static string RecorderJson()
        {
            if (_recorderFields == null || _serialize == null)
                return "{\"available\":false,\"reason\":\"Inspector recorder API unavailable\"}";
            try
            {
                var fields = new Dictionary<string, object>();
                foreach (FieldInfo field in _recorderFields)
                {
                    // Never serialize engine objects or traverse world state.
                    Type type = field.FieldType;
                    if (type.IsPrimitive || type == typeof(string))
                        fields[field.Name] = field.GetValue(null);
                }
                return (string)_serialize.Invoke(null, new object[] { fields });
            }
            catch
            {
                Interlocked.Increment(ref _failures);
                return "{\"available\":false,\"reason\":\"Inspector recorder snapshot failed\"}";
            }
        }

        private static void Snapshot(string reason, bool terminal)
        {
            _nextAt = Stopwatch.GetTimestamp() + 30L * Stopwatch.Frequency;
            long sequence = ++_sequence;
            var text = new StringBuilder(8192);
            text.AppendLine("BEGIN_DIAGNOSTIC_SNAPSHOT schema=v1 build=" + ReleaseIdentity.Version
                + " sequence=" + sequence + " utc=" + DateTime.UtcNow.ToString("O")
                + " reason=" + reason + " terminal=" + terminal);
            text.AppendLine("sessionLog=" + ClanAIPostVanilla.SessionLogPath);
            text.AppendLine(DelegatingMobilePartyAIModel.DiagnosticSummary(reason));
            text.AppendLine(ClanAIEvidenceWriter.HealthSummary());
            text.AppendLine("TIMING_SCOPE frames=rolling_1024 phases=Inspector_since_last_reset"
                + " resetsByClanAI=0 pausedAndMenuFramesMayBeIncluded=True");
            text.AppendLine("PERFORMANCE_JSON " + PerformanceJson());
            text.AppendLine("RECORDER_JSON " + RecorderJson());
            text.AppendLine("diagnosticFailuresProcessLifetime=" + Interlocked.Read(ref _failures));
            text.Append("END_DIAGNOSTIC_SNAPSHOT sequence=" + sequence);
            ClanAIEvidenceWriter.AppendLine(_path, text.ToString());
        }

        // Separate, direct post-drain record: it does not rely on a healthy queue
        // to tell us whether the queue drained. CreateNew preserves any prior file.
        public static void WriteShutdownHealth()
        {
            if (_finalHealthWritten || string.IsNullOrEmpty(_path)) return;
            try
            {
                string path = Path.ChangeExtension(_path, ".finalhealth.txt");
                using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
                using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
                {
                    writer.WriteLine("CLANAI_POST_SHUTDOWN_HEALTH utc=" + DateTime.UtcNow.ToString("O"));
                    writer.WriteLine("sessionLog=" + ClanAIPostVanilla.SessionLogPath);
                    writer.WriteLine(ClanAIEvidenceWriter.HealthSummary());
                    writer.WriteLine("diagnosticFailuresProcessLifetime=" + Interlocked.Read(ref _failures));
                    writer.WriteLine("Scope=process_lifetime; writtenCalls is not proof of exactly-once durable delivery.");
                    writer.WriteLine("Missing file or drainCompleted=False means terminal acceptance remains open.");
                }
                _finalHealthWritten = true;
            }
            catch
            {
                Interlocked.Increment(ref _failures);
                Debug.WriteLine("ClanAI: final diagnostic health file could not be written.");
            }
        }
    }
}

