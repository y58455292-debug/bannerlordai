using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Media;
using System.Security;
using System.Speech.Synthesis;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace BannerlordAI.FieldCommander
{
    internal sealed class Report
    {
        public int Priority;
        public string Key;
        public string Text;
    }

    internal static class Program
    {
        private const string VoiceProfile = "Commander Aldric v1";
        private static readonly string Root = @"D:\BannerlordAIResearch\Tools\FieldCommander";
        private static readonly string CommandPath = Path.Combine(Root, "command.txt");
        private static readonly string LatestPath = Path.Combine(Root, "latest.txt");
        private static readonly string SpokenLogPath = Path.Combine(Root, "spoken.log");
        private static readonly string PidPath = Path.Combine(Root, "FieldCommander.pid");
        private static readonly string TelemetryPath =
            @"D:\BannerlordAIResearch\Telemetry\BannerlordInspector\logs\inspector.log";

        private static readonly Dictionary<string, DateTime> Recent =
            new Dictionary<string, DateTime>(StringComparer.Ordinal);

        private static DateTime _lastAutoSpeech = DateTime.MinValue;
        private const int PollMs = 500;
        private const int AutoCooldownSeconds = 7;
        private const int DuplicateSeconds = 120;

        [STAThread]
        private static void Main()
        {
            bool created;
            using (var mutex = new Mutex(true, @"Global\BannerlordAI_FieldCommander", out created))
            {
                if (!created) return;

                Directory.CreateDirectory(Root);
                EnsureFile(CommandPath);
                File.WriteAllText(PidPath, Process.GetCurrentProcess().Id.ToString());

                long telemetryPosition = GetLength(TelemetryPath);
                string lastCommand = ReadShared(CommandPath).Trim();
                Report pending = null;

                using (var synth = new SpeechSynthesizer())
                {
                    SelectBaseVoice(synth);
                    synth.Volume = 96;
                    synth.Rate = 0;

                    Log("START v1.0 voiceProfile=" + VoiceProfile +
                        " base=" + synth.Voice.Name +
                        " telemetryOffset=" + telemetryPosition);

                    bool running = true;
                    while (running)
                    {
                        string command = ReadShared(CommandPath).Trim();
                        if (command != lastCommand)
                        {
                            lastCommand = command;

                            if (string.Equals(command, "!stop", StringComparison.OrdinalIgnoreCase))
                            {
                                Log("STOP");
                                running = false;
                                continue;
                            }

                            if (!string.IsNullOrWhiteSpace(command))
                                SpeakCustom(synth, command, "MANUAL");
                        }

                        foreach (string line in ReadNewTelemetry(ref telemetryPosition))
                        {
                            Report report = BuildReport(line);
                            if (report == null) continue;

                            if (IsDuplicate(report.Key))
                            {
                                Log("SKIP_DUPLICATE " + report.Key);
                                continue;
                            }

                            if (pending == null || report.Priority > pending.Priority)
                                pending = report;
                        }

                        if (pending != null && AutoReady())
                        {
                            SpeakCustom(synth, pending.Text, "AUTO " + pending.Key);
                            MarkSpoken(pending.Key);
                            pending = null;
                        }

                        Thread.Sleep(PollMs);
                    }
                }

                try { File.Delete(PidPath); } catch { }
            }
        }

        private static Report BuildReport(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return null;

            if (line.Contains("VISUAL_WAR_WINNER_CHANGE"))
                return BuildVisualWarReport(line);

            if (line.Contains("WORLD_MEMORY event=RaidStarted"))
            {
                Match m = Regex.Match(line, @"raider=(.*?) target=(.*?) relation=");
                if (!m.Success) return null;

                string actor = Clean(m.Groups[1].Value);
                string target = Clean(m.Groups[2].Value);

                return NewReport(
                    3,
                    "raid|" + actor + "|" + target,
                    "Riders bring word, my lord. " + actor +
                    " has begun a raid at " + target +
                    ". Look there now. This grievance will be remembered.");
            }

            if (line.Contains("WORLD_MEMORY event=HeroCaptured"))
            {
                Match m = Regex.Match(
                    line,
                    @"rememberer=(.*?) remembererClan=.*? capturer=(.*?) capturerParty=");

                if (!m.Success) return null;

                string prisoner = Clean(m.Groups[1].Value);
                string capturer = Clean(m.Groups[2].Value);

                return NewReport(
                    1,
                    "capture|" + prisoner + "|" + capturer,
                    "News from the war, my lord. " + prisoner +
                    " has been taken by " + capturer +
                    ". Their next meeting may not be the same.");
            }

            if (line.Contains("WORLD_MEMORY event=MercyRelease"))
            {
                Match m = Regex.Match(
                    line,
                    @"rememberer=(.*?) remembererClan=.*? facilitator=(.*?) relation=");

                if (!m.Success) return null;

                string prisoner = Clean(m.Groups[1].Value);
                string facilitator = Clean(m.Groups[2].Value);

                return NewReport(
                    1,
                    "mercy|" + prisoner + "|" + facilitator,
                    "Word of mercy, my lord. " + facilitator +
                    " has released " + prisoner +
                    " by choice. Remember those names. The act may shape what follows.");
            }

            if (line.Contains("MEMORY_CAUSAL_RESULT") && line.Contains("PASS_FLIPPED"))
            {
                Match m = Regex.Match(
                    line,
                    @"party=(.*?) .*? finalBehavior=(\S+) finalTarget=(.*?) finalTargetId=");

                if (!m.Success) return null;

                string party = Clean(m.Groups[1].Value);
                string target = Clean(m.Groups[3].Value);

                return NewReport(
                    5,
                    "memory|" + party + "|" + target,
                    "My lord, watch " + target +
                    ". Memory has just changed " + party +
                    "'s course. Follow them now.");
            }

            return null;
        }

        private static Report BuildVisualWarReport(string line)
        {
            Match m = Regex.Match(
                line,
                @"actor=(.*?) party=.*? after=(.*?) reason=(\S+) applications=");

            if (!m.Success) return null;

            string actor = Clean(m.Groups[1].Value);
            string candidate = Clean(m.Groups[2].Value);
            string reason = Clean(m.Groups[3].Value);
            string target = CandidateTarget(candidate);

            if (reason == "active-defense")
                return NewReport(
                    5,
                    "active-defense|" + actor + "|" + target,
                    "Urgent word, my lord. " + actor +
                    " has turned to defend " + target +
                    ". Put your eyes on that front.");

            if (reason == "frontier-defense")
                return NewReport(
                    3,
                    "frontier-defense|" + actor + "|" + target,
                    "My lord, look to " + target + ". " + actor +
                    " is moving toward the threatened frontier.");

            if (reason == "frontier-offense")
                return NewReport(
                    4,
                    "frontier-offense|" + actor + "|" + target,
                    "Scouts report a push, my lord. " + actor +
                    " is pressing toward " + target +
                    ". Watch the approach.");

            if (reason == "rear-security")
                return NewReport(
                    2,
                    "rear-security|" + actor + "|" + target,
                    "News from the rear, my lord. " + actor +
                    " has turned on " + target +
                    ". Watch the roads behind the front.");

            return NewReport(
                2,
                "strategic|" + actor + "|" + target,
                "My lord, a course has changed. Watch " + target +
                ". " + actor + " is moving there now.");
        }

        private static Report NewReport(int priority, string key, string text)
        {
            return new Report { Priority = priority, Key = key, Text = text };
        }

        private static bool AutoReady()
        {
            return (DateTime.UtcNow - _lastAutoSpeech).TotalSeconds >= AutoCooldownSeconds;
        }

        private static bool IsDuplicate(string key)
        {
            DateTime prior;
            return Recent.TryGetValue(key, out prior) &&
                (DateTime.UtcNow - prior).TotalSeconds < DuplicateSeconds;
        }

        private static void MarkSpoken(string key)
        {
            DateTime now = DateTime.UtcNow;
            _lastAutoSpeech = now;
            Recent[key] = now;

            var expired = new List<string>();
            foreach (var pair in Recent)
            {
                if ((now - pair.Value).TotalMinutes > 10)
                    expired.Add(pair.Key);
            }

            foreach (string keyToRemove in expired)
                Recent.Remove(keyToRemove);
        }

        private static IEnumerable<string> ReadNewTelemetry(ref long position)
        {
            var lines = new List<string>();

            try
            {
                if (!File.Exists(TelemetryPath))
                {
                    position = 0;
                    return lines;
                }

                long length = new FileInfo(TelemetryPath).Length;

                if (length < position)
                    position = 0;

                if (length == position)
                    return lines;

                using (var fs = new FileStream(
                    TelemetryPath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete))
                {
                    fs.Seek(position, SeekOrigin.Begin);

                    using (var sr = new StreamReader(
                        fs,
                        Encoding.UTF8,
                        true,
                        4096,
                        true))
                    {
                        string appended = sr.ReadToEnd();
                        position = fs.Position;

                        lines.AddRange(appended.Split(
                            new[] { "\r\n", "\n" },
                            StringSplitOptions.RemoveEmptyEntries));
                    }
                }
            }
            catch (Exception ex)
            {
                Log("TELEMETRY_ERROR " + ex.GetType().Name);
            }

            return lines;
        }

        private static void SpeakCustom(
            SpeechSynthesizer synth,
            string text,
            string source)
        {
            try
            {
                File.WriteAllText(LatestPath, text, new UTF8Encoding(false));

                string rate = text.StartsWith("Urgent", StringComparison.OrdinalIgnoreCase)
                    ? "-5%"
                    : "-12%";

                string pitch = text.StartsWith("Urgent", StringComparison.OrdinalIgnoreCase)
                    ? "-9%"
                    : "-13%";

                string ssml =
                    "<speak version=\"1.0\" xml:lang=\"en-US\">" +
                    "<voice name=\"" + Xml(synth.Voice.Name) + "\">" +
                    "<prosody rate=\"" + rate + "\" pitch=\"" + pitch + "\">" +
                    Xml(text.Trim()) +
                    "</prosody></voice></speak>";

                byte[] dry = RenderSpeech(synth, ssml);
                byte[] voiced = ApplyCommanderProfile(dry);

                using (var ms = new MemoryStream(voiced, false))
                using (var player = new SoundPlayer(ms))
                {
                    player.PlaySync();
                }

                Log("SPOKE " + source + " profile=" + VoiceProfile + " " + text.Trim());
            }
            catch (Exception ex)
            {
                Log("SPEAK_ERROR " + ex.GetType().Name + " " + ex.Message);

                try
                {
                    synth.SetOutputToDefaultAudioDevice();
                    synth.Speak(text.Trim());
                }
                catch { }
            }
        }

        private static byte[] RenderSpeech(SpeechSynthesizer synth, string ssml)
        {
            using (var ms = new MemoryStream())
            {
                synth.SetOutputToWaveStream(ms);

                try
                {
                    synth.SpeakSsml(ssml);
                }
                finally
                {
                    synth.SetOutputToNull();
                }

                return ms.ToArray();
            }
        }

        private static byte[] ApplyCommanderProfile(byte[] wav)
        {
            if (wav == null || wav.Length < 64)
                return wav;

            int channels;
            int sampleRate;
            int bits;
            int dataOffset;
            int dataLength;

            if (!ReadWaveInfo(
                    wav,
                    out channels,
                    out sampleRate,
                    out bits,
                    out dataOffset,
                    out dataLength))
            {
                return wav;
            }

            if (bits != 16 || channels < 1 || sampleRate < 8000)
                return wav;

            int frameBytes = channels * 2;
            int frames = dataLength / frameBytes;
            if (frames <= 0) return wav;

            var mono = new float[frames];
            var shaped = new float[frames];

            for (int i = 0; i < frames; i++)
            {
                int p = dataOffset + i * frameBytes;
                float sum = 0f;

                for (int ch = 0; ch < channels; ch++)
                {
                    short s = BitConverter.ToInt16(wav, p + ch * 2);
                    sum += s / 32768f;
                }

                mono[i] = sum / channels;
            }

            float body = 0f;
            float smooth = 0f;
            float bodyAlpha =
                1f - (float)Math.Exp(-2.0 * Math.PI * 420.0 / sampleRate);
            float smoothAlpha =
                1f - (float)Math.Exp(-2.0 * Math.PI * 2600.0 / sampleRate);

            for (int i = 0; i < frames; i++)
            {
                float x = mono[i];

                body += bodyAlpha * (x - body);
                smooth += smoothAlpha * (x - smooth);

                float harsh = x - smooth;
                float y =
                    (x * 0.82f) +
                    (body * 0.30f) -
                    (harsh * 0.10f);

                y = SoftSaturate(y * 1.16f);
                shaped[i] = y;
            }

            int early = Math.Max(1, (int)(sampleRate * 0.032));
            int hall = Math.Max(1, (int)(sampleRate * 0.071));
            var wet = new float[frames];
            float peak = 0.001f;

            for (int i = 0; i < frames; i++)
            {
                float y = shaped[i];

                if (i >= early)
                    y += shaped[i - early] * 0.075f;

                if (i >= hall)
                    y += shaped[i - hall] * 0.035f;

                wet[i] = y;

                float abs = Math.Abs(y);
                if (abs > peak) peak = abs;
            }

            float gain = peak > 0.90f ? 0.90f / peak : 1f;
            byte[] output = (byte[])wav.Clone();

            for (int i = 0; i < frames; i++)
            {
                float y = wet[i] * gain;

                if (y > 1f) y = 1f;
                if (y < -1f) y = -1f;

                short s = (short)Math.Round(y * 32767f);
                byte lo = (byte)(s & 0xff);
                byte hi = (byte)((s >> 8) & 0xff);

                int p = dataOffset + i * frameBytes;

                for (int ch = 0; ch < channels; ch++)
                {
                    output[p + ch * 2] = lo;
                    output[p + ch * 2 + 1] = hi;
                }
            }

            return output;
        }

        private static float SoftSaturate(float x)
        {
            float ax = Math.Abs(x);
            return x / (1f + 0.28f * ax);
        }

        private static bool ReadWaveInfo(
            byte[] wav,
            out int channels,
            out int sampleRate,
            out int bits,
            out int dataOffset,
            out int dataLength)
        {
            channels = 0;
            sampleRate = 0;
            bits = 0;
            dataOffset = 0;
            dataLength = 0;

            try
            {
                using (var ms = new MemoryStream(wav, false))
                using (var br = new BinaryReader(ms))
                {
                    string riff = new string(br.ReadChars(4));
                    br.ReadInt32();
                    string wave = new string(br.ReadChars(4));

                    if (riff != "RIFF" || wave != "WAVE")
                        return false;

                    bool fmtFound = false;

                    while (ms.Position + 8 <= ms.Length)
                    {
                        string id = new string(br.ReadChars(4));
                        int size = br.ReadInt32();
                        long chunkStart = ms.Position;

                        if (size < 0 || chunkStart + size > ms.Length)
                            return false;
                        if (id == "fmt ")
                        {
                            short format = br.ReadInt16();
                            channels = br.ReadInt16();
                            sampleRate = br.ReadInt32();
                            br.ReadInt32();
                            br.ReadInt16();
                            bits = br.ReadInt16();

                            if (format != 1)
                                return false;

                            fmtFound = true;
                        }
                        else if (id == "data")
                        {
                            dataOffset = (int)ms.Position;
                            dataLength = size;

                            return fmtFound &&
                                dataOffset + dataLength <= wav.Length;
                        }

                        ms.Position = chunkStart + size;

                        if ((size & 1) != 0 && ms.Position < ms.Length)
                            ms.Position++;
                    }
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        private static void SelectBaseVoice(SpeechSynthesizer synth)
        {
            try
            {
                synth.SelectVoice("Microsoft David Desktop");
            }
            catch
            {
                foreach (InstalledVoice v in synth.GetInstalledVoices())
                {
                    if (!v.Enabled) continue;
                    synth.SelectVoice(v.VoiceInfo.Name);
                    break;
                }
            }
        }

        private static string CandidateTarget(string candidate)
        {
            if (string.IsNullOrWhiteSpace(candidate))
                return "the reported position";

            int colon = candidate.IndexOf(':');

            return colon >= 0 && colon + 1 < candidate.Length
                ? candidate.Substring(colon + 1).Trim()
                : candidate.Trim();
        }

        private static string Clean(string value)
        {
            return (value ?? "")
                .Replace("\r", " ")
                .Replace("\n", " ")
                .Trim();
        }

        private static long GetLength(string path)
        {
            try
            {
                return File.Exists(path)
                    ? new FileInfo(path).Length
                    : 0;
            }
            catch
            {
                return 0;
            }
        }

        private static string ReadShared(string path)
        {
            for (int i = 0; i < 4; i++)
            {
                try
                {
                    using (var fs = new FileStream(
                        path,
                        FileMode.OpenOrCreate,
                        FileAccess.Read,
                        FileShare.ReadWrite | FileShare.Delete))
                    using (var sr = new StreamReader(fs, Encoding.UTF8, true))
                    {
                        return sr.ReadToEnd();
                    }
                }
                catch
                {
                    Thread.Sleep(30);
                }
            }

            return "";
        }

        private static void EnsureFile(string path)
        {
            if (!File.Exists(path))
                File.WriteAllText(path, "", new UTF8Encoding(false));
        }

        private static string Xml(string value)
        {
            return SecurityElement.Escape(value ?? "") ?? "";
        }

        private static void Log(string line)
        {
            try
            {
                File.AppendAllText(
                    SpokenLogPath,
                    DateTime.UtcNow.ToString("O") +
                    " " + line + Environment.NewLine,
                    new UTF8Encoding(false));
            }
            catch { }
        }
    }
}

