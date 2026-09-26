using System;
using System.IO;

namespace ClanAI
{
    public static class ClanAISwitch
    {
        public static readonly string OffMarker =
            ModuleRuntimePaths.Data("CLANAI_OFF.txt");

        private static bool _enabled = true;
        private static DateTime _nextCheck = DateTime.MinValue;

        public static bool Enabled
        {
            get
            {
                DateTime now = DateTime.UtcNow;

                if (now >= _nextCheck)
                {
                    _enabled = string.IsNullOrEmpty(OffMarker) ||
                        !File.Exists(OffMarker);
                    _nextCheck = now.AddSeconds(2);
                }

                return _enabled;
            }
        }
    }
}

