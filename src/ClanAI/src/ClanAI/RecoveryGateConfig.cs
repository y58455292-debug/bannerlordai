using System;
using System.IO;

namespace ClanAI
{
    internal enum RecoveryGateMode
    {
        Suppress,
        Observe
    }

    internal static class RecoveryGateConfig
    {
        internal static readonly string ConfigPath =
            ModuleRuntimePaths.Data("WeakBanditRecovery.cfg");

        internal static RecoveryGateMode LoadMode(
            out string status)
        {
            try
            {
                if (string.IsNullOrEmpty(ConfigPath) ||
                    !File.Exists(ConfigPath))
                {
                    status = "missing_default_suppress";
                    return RecoveryGateMode.Suppress;
                }

                string[] lines =
                    File.ReadAllLines(ConfigPath);

                return ParseMode(
                    lines,
                    out status);
            }
            catch (Exception ex)
            {
                status =
                    "read_failed_" +
                    ex.GetType().Name;

                return RecoveryGateMode.Suppress;
            }
        }

        internal static RecoveryGateMode ParseMode(
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

                    int eq = line.IndexOf('=');
                    if (eq <= 0)
                        continue;

                    string key =
                        line.Substring(0, eq).Trim();

                    string value =
                        line.Substring(eq + 1).Trim();

                    if (!key.Equals(
                            "Mode",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (value.Equals(
                            "Observe",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        status = "configured_observe";
                        return RecoveryGateMode.Observe;
                    }

                    if (value.Equals(
                            "Suppress",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        status = "configured_suppress";
                        return RecoveryGateMode.Suppress;
                    }

                    status = "invalid_mode_default_suppress";
                    return RecoveryGateMode.Suppress;
                }
            }

            status = "mode_missing_default_suppress";
            return RecoveryGateMode.Suppress;
        }
    }
}

