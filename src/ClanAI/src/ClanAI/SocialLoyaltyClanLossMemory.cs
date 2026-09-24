using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Settlements;

namespace ClanAI
{
    internal static class SocialLoyaltyClanLossMemory
    {
        private const double MemoryLifetimeHours = 720.0;
        private const float LossValueFraction = 0.25f;
        private const float MaxNativePressure = 750000f;

        private sealed class Record
        {
            internal string ClanId;
            internal string ClanName;
            internal string SettlementId;
            internal string SettlementName;
            internal double LostAtHours;
            internal float NativeSettlementValue;
        }

        private static readonly List<Record> Records =
            new List<Record>();

        private static bool _loadedFromSave;
        private static long _recorded;

        internal static void BeginSession()
        {
            if (!_loadedFromSave)
                Records.Clear();

            PruneExpired();
            _recorded = Records.Count;

            ClanAIPostVanilla.WriteExternalLog(
                "SOCIAL_LOYALTY_CLAN_LOSS_SESSION_READY records=" +
                Records.Count +
                " lifetimeHours=" + F(MemoryLifetimeHours) +
                " valueFraction=" + F(LossValueFraction) +
                " maxNativePressure=" + F(MaxNativePressure) +
                " restored=" + _loadedFromSave);

            _loadedFromSave = false;
        }
        internal static void SyncData(IDataStore dataStore)
        {
            List<string> lines =
                new List<string>();

            if (dataStore.IsSaving)
                lines = ExportSaveLines();

            bool found =
                dataStore.SyncData(
                    "ClanAI_SocialLoyaltyClanLoss_v2",
                    ref lines);

            if (dataStore.IsLoading)
                ImportSaveLines(found ? lines : null);
        }

        internal static void RecordHoldingLoss(
            Settlement settlement,
            Hero oldOwner,
            Hero newOwner,
            ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
        {
            if (detail != ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail.BySiege ||
                settlement == null ||
                oldOwner == null ||
                oldOwner.Clan == null ||
                newOwner == null ||
                newOwner.Clan == null ||
                SameClan(oldOwner.Clan, newOwner.Clan))
            {
                return;
            }

            IFaction oldFaction = oldOwner.Clan.MapFaction;
            IFaction newFaction = newOwner.Clan.MapFaction;

            if (oldFaction == null ||
                newFaction == null ||
                SameFaction(oldFaction, newFaction))
            {
                return;
            }

            if (!settlement.IsTown &&
                !settlement.IsCastle)
            {
                return;
            }

            float nativeSettlementValue = 0f;

            try
            {
                nativeSettlementValue =
                    settlement.GetSettlementValueForFaction(
                        oldOwner.Clan);
            }
            catch
            {
                nativeSettlementValue = 0f;
            }

            if (nativeSettlementValue <= 0f)
            {
                try
                {
                    nativeSettlementValue =
                        settlement.GetValue(
                            oldOwner,
                            countAlsoBoundedSettlements: true) *
                        0.33f;
                }
                catch
                {
                    nativeSettlementValue = 0f;
                }
            }
            if (nativeSettlementValue <= 0f)
                return;

            string clanId =
                SafeId(
                    oldOwner.Clan.StringId,
                    oldOwner.Clan.Name.ToString());

            if (string.IsNullOrEmpty(clanId))
                return;

            Record record =
                new Record
                {
                    ClanId = clanId,
                    ClanName = oldOwner.Clan.Name.ToString(),
                    SettlementId = settlement.StringId,
                    SettlementName = settlement.Name.ToString(),
                    LostAtHours = CampaignTime.Now.ToHours,
                    NativeSettlementValue = nativeSettlementValue
                };

            Records.Add(record);
            _recorded++;

            PruneExpired();

            ClanAIPostVanilla.WriteExternalLog(
                "SOCIAL_LOYALTY_CLAN_LOSS_RECORDED clan=" +
                record.ClanName +
                " settlement=" + record.SettlementName +
                " detail=" + detail +
                " nativeSettlementValue=" +
                F(record.NativeSettlementValue) +
                " pressureAtLoss=" +
                F(record.NativeSettlementValue * LossValueFraction) +
                " recordsForClan=" +
                CountForClan(record.ClanId) +
                " recorded=" + _recorded +
                " source=OnSettlementOwnerChangedEvent");
        }

        internal static bool TryGetPressure(
            Clan clan,
            out float nativePressure,
            out double youngestAgeHours,
            out int count,
            out string settlements,
            out float totalNativeLossValue)
        {
            nativePressure = 0f;
            youngestAgeHours = double.MaxValue;
            count = 0;
            settlements = null;
            totalNativeLossValue = 0f;

            if (clan == null)
                return false;

            PruneExpired();

            string clanId =
                SafeId(
                    clan.StringId,
                    clan.Name.ToString());

            if (string.IsNullOrEmpty(clanId))
                return false;

            List<string> names =
                new List<string>();

            double now =
                CampaignTime.Now.ToHours;
            for (int i = 0; i < Records.Count; i++)
            {
                Record record = Records[i];

                if (record == null ||
                    !string.Equals(
                        record.ClanId,
                        clanId,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                double age =
                    now - record.LostAtHours;

                if (age < 0.0)
                    age = 0.0;

                double freshness =
                    1.0 -
                    (age / MemoryLifetimeHours);

                if (freshness <= 0.0)
                    continue;

                float contribution =
                    record.NativeSettlementValue *
                    LossValueFraction *
                    (float)freshness;

                nativePressure += contribution;
                totalNativeLossValue +=
                    record.NativeSettlementValue;
                count++;

                if (age < youngestAgeHours)
                    youngestAgeHours = age;

                if (!string.IsNullOrEmpty(
                    record.SettlementName) &&
                    !names.Contains(record.SettlementName))
                {
                    names.Add(
                        record.SettlementName);
                }
            }

            if (count <= 0 ||
                nativePressure <= 0f)
            {
                return false;
            }

            if (nativePressure >
                MaxNativePressure)
            {
                nativePressure =
                    MaxNativePressure;
            }

            settlements =
                string.Join(",", names);

            return true;
        }

        private static List<string> ExportSaveLines()
        {
            PruneExpired();

            List<string> lines =
                new List<string>();

            for (int i = 0; i < Records.Count; i++)
            {
                Record r = Records[i];

                if (r == null)
                    continue;

                lines.Add(
                    B64(r.ClanId) + "|" +
                    B64(r.ClanName) + "|" +
                    B64(r.SettlementId) + "|" +
                    B64(r.SettlementName) + "|" +
                    r.LostAtHours.ToString(
                        "R",
                        CultureInfo.InvariantCulture) + "|" +
                    r.NativeSettlementValue.ToString(
                        "R",
                        CultureInfo.InvariantCulture));
            }
            ClanAIPostVanilla.WriteExternalLog(
                "SOCIAL_LOYALTY_CLAN_LOSS_SAVE records=" +
                lines.Count);

            return lines;
        }

        private static void ImportSaveLines(
            List<string> lines)
        {
            Records.Clear();

            if (lines != null)
            {
                foreach (string line in lines)
                {
                    if (string.IsNullOrEmpty(line))
                        continue;

                    string[] p =
                        line.Split('|');

                    if (p.Length != 6)
                        continue;

                    double lostAt;
                    float nativeValue;

                    if (!double.TryParse(
                        p[4],
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out lostAt))
                    {
                        continue;
                    }

                    if (!float.TryParse(
                        p[5],
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out nativeValue))
                    {
                        continue;
                    }

                    string clanId =
                        FromB64(p[0]);

                    if (string.IsNullOrEmpty(clanId) ||
                        nativeValue <= 0f)
                    {
                        continue;
                    }

                    Records.Add(
                        new Record
                        {
                            ClanId = clanId,
                            ClanName = FromB64(p[1]),
                            SettlementId = FromB64(p[2]),
                            SettlementName = FromB64(p[3]),
                            LostAtHours = lostAt,
                            NativeSettlementValue = nativeValue
                        });
                }
            }

            _loadedFromSave = true;

            ClanAIPostVanilla.WriteExternalLog(
                "SOCIAL_LOYALTY_CLAN_LOSS_LOAD records=" +
                Records.Count);
        }
        private static void PruneExpired()
        {
            double now =
                CampaignTime.Now.ToHours;

            for (int i = Records.Count - 1;
                i >= 0;
                i--)
            {
                Record r = Records[i];

                if (r == null)
                {
                    Records.RemoveAt(i);
                    continue;
                }

                double age =
                    now - r.LostAtHours;

                if (age <= MemoryLifetimeHours)
                    continue;

                ClanAIPostVanilla.WriteExternalLog(
                    "SOCIAL_LOYALTY_CLAN_LOSS_EXPIRED clan=" +
                    r.ClanName +
                    " settlement=" +
                    r.SettlementName +
                    " ageHours=" +
                    F(age));

                Records.RemoveAt(i);
            }
        }

        private static int CountForClan(
            string clanId)
        {
            int count = 0;

            for (int i = 0;
                i < Records.Count;
                i++)
            {
                Record r = Records[i];

                if (r != null &&
                    string.Equals(
                        r.ClanId,
                        clanId,
                        StringComparison.Ordinal))
                {
                    count++;
                }
            }

            return count;
        }

        private static bool SameClan(
            Clan a,
            Clan b)
        {
            if (a == null || b == null)
                return false;

            return ReferenceEquals(a, b) ||
                string.Equals(
                    a.StringId,
                    b.StringId,
                    StringComparison.Ordinal);
        }
        private static bool SameFaction(
            IFaction a,
            IFaction b)
        {
            if (a == null || b == null)
                return false;

            return ReferenceEquals(a, b) ||
                string.Equals(
                    a.StringId,
                    b.StringId,
                    StringComparison.Ordinal);
        }

        private static string SafeId(
            string id,
            string fallback)
        {
            return string.IsNullOrEmpty(id)
                ? (fallback ?? "")
                : id;
        }

        private static string B64(
            string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            return Convert.ToBase64String(
                Encoding.UTF8.GetBytes(value));
        }

        private static string FromB64(
            string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            try
            {
                return Encoding.UTF8.GetString(
                    Convert.FromBase64String(value));
            }
            catch
            {
                return "";
            }
        }

        private static string F(
            float value)
        {
            return value.ToString(
                "0.###",
                CultureInfo.InvariantCulture);
        }

        private static string F(
            double value)
        {
            return value.ToString(
                "0.###",
                CultureInfo.InvariantCulture);
        }
    }
}
