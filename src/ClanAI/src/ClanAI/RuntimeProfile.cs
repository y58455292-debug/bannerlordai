using System;
using System.IO;

namespace ClanAI
{
    internal enum ClanAIRuntimeProfile
    {
        Release,
        Evidence
    }

    internal static class RuntimeProfile
    {
        internal const string ConfigFileName =
            "RuntimeProfile.cfg";

        internal static readonly string ConfigPath =
            ModuleRuntimePaths.Data(ConfigFileName);

        private static readonly object Sync = new object();
        private static bool _loaded;
        private static ClanAIRuntimeProfile _profile =
            ClanAIRuntimeProfile.Release;
        private static string _status = "not_loaded";

        internal static bool EvidenceEnabled
        {
            get
            {
                EnsureLoaded();
                return _profile == ClanAIRuntimeProfile.Evidence;
            }
        }

        internal static string Status
        {
            get { EnsureLoaded(); return _status; }
        }

        private static void EnsureLoaded()
        {
            if (_loaded)
                return;

            lock (Sync)
            {
                if (_loaded)
                    return;

                try
                {
                    if (string.IsNullOrEmpty(ConfigPath) ||
                        !File.Exists(ConfigPath))
                    {
                        _profile = ClanAIRuntimeProfile.Release;
                        _status = "missing_default_release";
                    }
                    else
                    {
                        _profile = Parse(
                            File.ReadAllLines(ConfigPath),
                            out _status);
                    }
                }
                catch (Exception ex)
                {
                    _profile = ClanAIRuntimeProfile.Release;
                    _status = "read_failed_default_release_" +
                        ex.GetType().Name;
                }

                _loaded = true;
            }
        }

        internal static ClanAIRuntimeProfile Parse(
            string[] lines,
            out string status)
        {
            if (lines != null)
            {
                foreach (string raw in lines)
                {
                    string line = (raw ?? string.Empty).Trim();
                    if (line.Length == 0 || line.StartsWith("#"))
                        continue;

                    int equals = line.IndexOf('=');
                    if (equals <= 0)
                        continue;

                    string key = line.Substring(0, equals).Trim();
                    if (!key.Equals(
                            "Profile",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    string value = line.Substring(equals + 1).Trim();
                    if (value.Equals(
                            "Evidence",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        status = "configured_evidence";
                        return ClanAIRuntimeProfile.Evidence;
                    }

                    if (value.Equals(
                            "Release",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        status = "configured_release";
                        return ClanAIRuntimeProfile.Release;
                    }

                    status = "invalid_profile_default_release";
                    return ClanAIRuntimeProfile.Release;
                }
            }

            status = "profile_missing_default_release";
            return ClanAIRuntimeProfile.Release;
        }
    }
}

