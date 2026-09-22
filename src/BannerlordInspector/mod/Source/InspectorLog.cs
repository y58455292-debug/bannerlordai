using System;
using System.IO;
using TaleWorlds.Library;

namespace BannerlordInspector
{
    /// <summary>Append-only log under the module folder. Never allowed to throw.</summary>
    public static class InspectorLog
    {
        private static string _path;

        private static string Path_
        {
            get
            {
                if (_path == null)
                {
                    _path = @"D:\BannerlordAIResearch\Telemetry\BannerlordInspector\logs\inspector.log";
                }
                return _path;
            }
        }

        public static void Info(string message) => Write("INFO ", message);
        public static void Warn(string message) => Write("WARN ", message);

        public static void Error(string message, Exception ex = null) =>
            Write("ERROR", ex == null ? message : message + " :: " + ex);

        private static void Write(
            string level,
            string message)
        {
            try
            {
                EvidenceWriter.AppendLine(
                    Path_,
                    "[" +
                    DateTime.Now
                        .ToString("HH:mm:ss.fff") +
                    "] " +
                    level +
                    " " +
                    message);
            }
            catch
            {
                // Intentionally silent.
            }
        }
    }
}

