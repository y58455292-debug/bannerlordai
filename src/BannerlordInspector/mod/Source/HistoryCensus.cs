using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.LogEntries;
using TaleWorlds.Localization;

namespace BannerlordInspector
{
    public static class HistoryCensus
    {
        private static object _published;
        private static long _passes;

        private static string SafeName(object value)
        {
            if (value == null)
                return null;

            try
            {
                var type = value.GetType();

                var nameProperty =
                    type.GetProperty(
                        "Name",
                        BindingFlags.Public |
                        BindingFlags.Instance
                    );

                if (nameProperty != null)
                {
                    object name =
                        nameProperty.GetValue(value, null);

                    if (name != null)
                        return name.ToString();
                }

                return value.ToString();
            }
            catch
            {
                return null;
            }
        }

        private static object SafeValue(object value)
        {
            if (value == null)
                return null;

            try
            {
                Type t = value.GetType();

                if (
                    t.IsPrimitive ||
                    t.IsEnum ||
                    value is string ||
                    value is decimal
                )
                {
                    return value.ToString();
                }

                return SafeName(value);
            }
            catch
            {
                return null;
            }
        }

        private static object ExtractFields(LogEntry entry)
        {
            var result =
                new Dictionary<string, object>();

            try
            {
                FieldInfo[] fields =
                    entry.GetType().GetFields(
                        BindingFlags.Public |
                        BindingFlags.NonPublic |
                        BindingFlags.Instance
                    );

                foreach (FieldInfo field in fields)
                {
                    try
                    {
                        object value =
                            field.GetValue(entry);

                        result[field.Name] =
                            SafeValue(value);
                    }
                    catch
                    {
                        result[field.Name] = null;
                    }
                }
            }
            catch
            {
            }

            return result;
        }

        public static void Tick()
{
    if (_published != null)
        return;

    Campaign campaign =
        Campaign.Current;

            if (
                campaign == null ||
                campaign.LogEntryHistory == null
            )
            {
                _published = null;
                return;
            }

            DateTime started =
                DateTime.UtcNow;

            try
            {
                var entries =
                    new List<object>();

                var typeCounts =
                    new Dictionary<string, int>();

                foreach (
                    LogEntry entry
                    in campaign
                        .LogEntryHistory
                        .GameActionLogs
                )
                {
                    if (entry == null)
                        continue;

                    string type =
                        entry.GetType().Name;

                    if (!typeCounts.ContainsKey(type))
                        typeCounts[type] = 0;

                    typeCounts[type]++;

                    string text = null;

                    try
                    {
                        text = entry.ToString();
                    }
                    catch
                    {
                    }

                    bool valid = false;

                    try
                    {
                        valid = entry.IsValid();
                    }
                    catch
                    {
                    }

                    entries.Add(
                        new
                        {
                            id = entry.Id,

                            type,

                            gameTime =
                                entry.GameTime.ToString(),

                            gameTimeRaw =
                                entry.GameTime.ToHours,

                            keepInHistoryTime =
                                entry.KeepInHistoryTime
                                    .ToString(),

                            keepInHistoryHours =
                                entry.KeepInHistoryTime
                                    .ToHours,

                            notificationType =
                                entry.NotificationType
                                    .ToString(),

                            valid,

                            text,

                            fields =
                                ExtractFields(entry)
                        }
                    );
                }

                _passes++;

                DateTime completed =
                    DateTime.UtcNow;

                _published =
                    new
                    {
                        ready = true,
                        version = "0.1",

                        asOf = new
                        {
                            secondsOld = 0.0,

                            passTookMs =
                                Math.Round(
                                    (
                                        completed -
                                        started
                                    ).TotalMilliseconds,
                                    2
                                ),

                            passes = _passes
                        },

                        count =
                            entries.Count,

                               

                       typeCounts =
                            typeCounts
                                .OrderByDescending(x => x.Value)
                                .ThenBy(x => x.Key)
                                .Select(x => new
                                {
                                    type = x.Key,
                                    count = x.Value
                                })
                                .ToArray(),

                        entries =
                            entries.ToArray()
                    };
            }
            catch (Exception ex)
            {
                InspectorLog.Error(
                    "History census failed.",
                    ex
                );
            }
        }

        public static object Current()
        {
            if (_published == null)
            {
                return new
                {
                    ready = false,
                    version = "0.1",
                    note =
                        "History census has not completed."
                };
            }

            return _published;
        }
    }
}