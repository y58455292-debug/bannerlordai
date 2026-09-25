using System;
using System.IO;

namespace ClanAI
{
    internal enum StrategicCommitmentMode
    {
        Observe,
        Apply
    }

    internal static class StrategicCommitmentConfig
    {
        internal const string ConfigFileName =
            "StrategicCommitment.cfg";

        internal static readonly string ConfigPath =
            ResolveConfigPath(
                typeof(StrategicCommitmentConfig)
                    .Assembly
                    .Location);

        private static readonly object Sync =
            new object();

        private static bool _loaded;
        private static StrategicCommitmentMode _mode =
            StrategicCommitmentMode.Observe;
        private static string _status =
            "not_loaded";

        internal static StrategicCommitmentMode Mode
        {
            get
            {
                EnsureLoaded();
                return _mode;
            }
        }

        internal static string Status
        {
            get
            {
                EnsureLoaded();
                return _status;
            }
        }
        internal static string ResolveConfigPath(
            string assemblyLocation)
        {
            if (string.IsNullOrWhiteSpace(
                    assemblyLocation))
            {
                return null;
            }

            try
            {
                string binaryDirectory =
                    Path.GetDirectoryName(
                        Path.GetFullPath(
                            assemblyLocation));

                if (string.IsNullOrEmpty(
                        binaryDirectory))
                {
                    return null;
                }

                DirectoryInfo platformDirectory =
                    new DirectoryInfo(
                        binaryDirectory);

                DirectoryInfo binDirectory =
                    platformDirectory.Parent;

                DirectoryInfo moduleDirectory =
                    binDirectory == null
                        ? null
                        : binDirectory.Parent;

                if (binDirectory == null ||
                    moduleDirectory == null ||
                    !string.Equals(
                        binDirectory.Name,
                        "bin",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                return Path.Combine(
                    moduleDirectory.FullName,
                    "Data",
                    ConfigFileName);
            }
            catch
            {
                return null;
            }
        }

        internal static void EnsureLoaded()
        {
            if (_loaded)
                return;

            lock (Sync)
            {
                if (_loaded)
                    return;

                try
                {
                    if (string.IsNullOrEmpty(
                            ConfigPath) ||
                        !File.Exists(ConfigPath))
                    {
                        _mode =
                            StrategicCommitmentMode.Observe;
                        _status =
                            "missing_default_observe";
                    }
                    else
                    {
                        _mode = ParseMode(
                            File.ReadAllLines(ConfigPath),
                            out _status);
                    }
                }
                catch (Exception ex)
                {
                    _mode =
                        StrategicCommitmentMode.Observe;
                    _status =
                        "read_failed_" +
                        ex.GetType().Name;
                }

                _loaded = true;
            }
        }

        internal static StrategicCommitmentMode ParseMode(
            string[] lines,
            out string status)
        {
            if (lines != null)
            {
                foreach (string raw in lines)
                {
                    string line =
                        (raw ?? string.Empty).Trim();

                    if (line.Length == 0 ||
                        line.StartsWith("#"))
                    {
                        continue;
                    }

                    int eq =
                        line.IndexOf('=');

                    if (eq <= 0)
                        continue;

                    string key =
                        line.Substring(0, eq).Trim();

                    if (!key.Equals(
                            "Mode",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    string value =
                        line.Substring(eq + 1).Trim();
                    if (value.Equals(
                            "Apply",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        status =
                            "configured_apply";
                        return
                            StrategicCommitmentMode.Apply;
                    }

                    if (value.Equals(
                            "Observe",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        status =
                            "configured_observe";
                        return
                            StrategicCommitmentMode.Observe;
                    }

                    status =
                        "invalid_mode_default_observe";

                    return
                        StrategicCommitmentMode.Observe;
                }
            }

            status =
                "mode_missing_default_observe";

            return
                StrategicCommitmentMode.Observe;
        }
    }
}