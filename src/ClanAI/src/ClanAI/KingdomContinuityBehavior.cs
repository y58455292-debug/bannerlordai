using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace ClanAI
{
    /// <summary>
    /// Records native kingdom lifecycle facts for continuity and player visibility.
    /// This behavior never changes native political state.
    /// </summary>
    public sealed class KingdomContinuityBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.RulingClanChanged.AddNonSerializedListener(
                this, new Action<Kingdom, Clan>(KingdomContinuityLedger.OnRulingClanChanged));
            CampaignEvents.KingdomCreatedEvent.AddNonSerializedListener(
                this, new Action<Kingdom>(KingdomContinuityLedger.OnKingdomCreated));
            CampaignEvents.KingdomDestroyedEvent.AddNonSerializedListener(
                this, new Action<Kingdom>(KingdomContinuityLedger.OnKingdomDestroyed));
            CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(
                this, new Action<CampaignGameStarter>(KingdomContinuityLedger.OnNewGameCreated));
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(
                this, new Action<CampaignGameStarter>(KingdomContinuityLedger.OnSessionLaunched));
            CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(
                this, new Action(KingdomContinuityLedger.OnGameLoadFinished));
        }

        public override void SyncData(IDataStore dataStore)
        {
            KingdomContinuityLedger.SyncData(dataStore);
        }
    }

    internal static class KingdomContinuityLedger
    {
        private const int SchemaVersion = 1;
        private const string SaveKey = "ClanAI_KingdomContinuity_v1";
        private static readonly Dictionary<string, KingdomContinuityRecord> Records =
            new Dictionary<string, KingdomContinuityRecord>(StringComparer.Ordinal);

        private sealed class KingdomContinuityRecord
        {
            internal string KingdomId;
            internal string OriginalName;
            internal string CurrentName;
            internal string CultureId;
            internal string CurrentRulingClanId;
            internal string PreviousRulingClanId;
            internal int SuccessionCount;
            internal double LastSuccessionHours;
            internal bool Destroyed;
            internal double DestroyedAtHours;
        }

        internal static void SyncData(IDataStore dataStore)
        {
            List<string> rows = dataStore.IsSaving
                ? ExportRows()
                : new List<string>();

            bool found = dataStore.SyncData(SaveKey, ref rows);
            if (dataStore.IsLoading)
            {
                ImportRows(found ? rows : null);
                ClanAIPostVanilla.WriteExternalLog(
                    "KINGDOM_CONTINUITY_RESTORE records=" + Records.Count +
                    " found=" + found +
                    " schema=" + SchemaVersion +
                    " mutation=False");
            }
        }

        internal static void OnNewGameCreated(CampaignGameStarter starter)
        {
            Records.Clear();
            ReconcileActiveKingdoms();
        }

        internal static void OnSessionLaunched(CampaignGameStarter starter)
        {
            ReconcileActiveKingdoms();
        }

        internal static void OnGameLoadFinished()
        {
            ReconcileActiveKingdoms();
        }

        internal static void OnKingdomCreated(Kingdom kingdom)
        {
            if (kingdom == null || string.IsNullOrEmpty(kingdom.StringId))
                return;

            KingdomContinuityRecord record = EnsureRecord(kingdom);
            if (record.Destroyed)
                return;

            record.CurrentName = NameOf(kingdom);
            record.CurrentRulingClanId = ClanId(kingdom.RulingClan);
            ClanAIPostVanilla.WriteExternalLog(
                "KINGDOM_CONTINUITY_CREATED kingdom=" + record.KingdomId +
                " name=" + Safe(record.CurrentName) +
                " culture=" + Safe(record.CultureId) +
                " ruler=" + Safe(record.CurrentRulingClanId) +
                " source=native_event mutation=False");
        }

        internal static void OnRulingClanChanged(Kingdom kingdom, Clan oldRulingClan)
        {
            if (kingdom == null || string.IsNullOrEmpty(kingdom.StringId))
                return;

            KingdomContinuityRecord record = EnsureRecord(kingdom);
            if (record.Destroyed)
                return;

            string newRulingClanId = ClanId(kingdom.RulingClan);
            string oldRulingClanId = ClanId(oldRulingClan);

            // The event can be observed more than once around campaign initialization.
            // Native current state is the idempotency guard; a duplicate cannot increment
            // or notify after the recorded ruler already matches it.
            if (string.Equals(record.CurrentRulingClanId, newRulingClanId, StringComparison.Ordinal))
                return;

            record.PreviousRulingClanId = oldRulingClanId;
            record.CurrentRulingClanId = newRulingClanId;
            record.CurrentName = NameOf(kingdom);
            record.SuccessionCount++;
            record.LastSuccessionHours = CampaignTime.Now.ToHours;

            DynastyBranchEpisodeMemory.RecordKingdomRulingClanChanged(
                record.KingdomId,
                record.CurrentName,
                oldRulingClanId,
                newRulingClanId,
                LeaderId(oldRulingClan),
                LeaderId(kingdom.RulingClan),
                record.SuccessionCount,
                record.LastSuccessionHours);

            string rulerName = kingdom.RulingClan == null || kingdom.RulingClan.Leader == null
                ? "leadership is vacant"
                : kingdom.RulingClan.Leader.Name.ToString();
            ShowNotice(
                "Ruler changed in " + record.CurrentName + ". " + rulerName + " now leads the kingdom.");

            ClanAIPostVanilla.WriteExternalLog(
                "KINGDOM_CONTINUITY_SUCCESSION kingdom=" + record.KingdomId +
                " name=" + Safe(record.CurrentName) +
                " culture=" + Safe(record.CultureId) +
                " oldRuler=" + Safe(oldRulingClanId) +
                " newRuler=" + Safe(newRulingClanId) +
                " count=" + record.SuccessionCount +
                " campaignHours=" + F(record.LastSuccessionHours) +
                " mutation=False");
        }

        internal static void OnKingdomDestroyed(Kingdom kingdom)
        {
            if (kingdom == null || string.IsNullOrEmpty(kingdom.StringId))
                return;

            KingdomContinuityRecord record = EnsureRecord(kingdom);
            if (record.Destroyed)
                return;

            record.CurrentName = NameOf(kingdom);
            record.Destroyed = true;
            record.DestroyedAtHours = CampaignTime.Now.ToHours;
            ShowNotice("The kingdom of " + record.CurrentName + " has ended.");

            ClanAIPostVanilla.WriteExternalLog(
                "KINGDOM_CONTINUITY_DESTROYED kingdom=" + record.KingdomId +
                " name=" + Safe(record.CurrentName) +
                " culture=" + Safe(record.CultureId) +
                " campaignHours=" + F(record.DestroyedAtHours) +
                " mutation=False");
        }

        private static void ReconcileActiveKingdoms()
        {
            int reconciled = 0;
            for (int i = 0; i < Kingdom.All.Count; i++)
            {
                Kingdom kingdom = Kingdom.All[i];
                if (kingdom == null || kingdom.IsEliminated || string.IsNullOrEmpty(kingdom.StringId))
                    continue;

                KingdomContinuityRecord record = EnsureRecord(kingdom);
                if (record.Destroyed)
                    continue;

                // Native state fills gaps after load. OriginalName and CultureId are provenance
                // fields and are intentionally never rewritten during reconciliation.
                record.CurrentName = NameOf(kingdom);
                record.CurrentRulingClanId = ClanId(kingdom.RulingClan);
                reconciled++;
            }

            ClanAIPostVanilla.WriteExternalLog(
                "KINGDOM_CONTINUITY_RECONCILED active=" + reconciled +
                " records=" + Records.Count +
                " mutation=False");
        }

        private static KingdomContinuityRecord EnsureRecord(Kingdom kingdom)
        {
            KingdomContinuityRecord record;
            if (Records.TryGetValue(kingdom.StringId, out record))
                return record;

            record = new KingdomContinuityRecord
            {
                KingdomId = kingdom.StringId,
                OriginalName = NameOf(kingdom),
                CurrentName = NameOf(kingdom),
                CultureId = kingdom.Culture == null ? null : kingdom.Culture.StringId,
                CurrentRulingClanId = ClanId(kingdom.RulingClan),
                PreviousRulingClanId = null,
                SuccessionCount = 0,
                LastSuccessionHours = 0.0,
                Destroyed = false,
                DestroyedAtHours = 0.0
            };
            Records.Add(record.KingdomId, record);
            return record;
        }

        private static List<string> ExportRows()
        {
            List<string> rows = new List<string>(Records.Count);
            foreach (KeyValuePair<string, KingdomContinuityRecord> item in Records)
            {
                KingdomContinuityRecord record = item.Value;
                rows.Add(string.Join("|", new[]
                {
                    SchemaVersion.ToString(CultureInfo.InvariantCulture),
                    Encode(record.KingdomId),
                    Encode(record.OriginalName),
                    Encode(record.CurrentName),
                    Encode(record.CultureId),
                    Encode(record.CurrentRulingClanId),
                    Encode(record.PreviousRulingClanId),
                    record.SuccessionCount.ToString(CultureInfo.InvariantCulture),
                    F(record.LastSuccessionHours),
                    record.Destroyed ? "1" : "0",
                    F(record.DestroyedAtHours)
                }));
            }
            return rows;
        }

        private static void ImportRows(List<string> rows)
        {
            Records.Clear();
            if (rows == null)
                return;

            for (int i = 0; i < rows.Count; i++)
            {
                string[] fields = rows[i] == null ? new string[0] : rows[i].Split('|');
                int version;
                int count;
                double successionHours;
                double destroyedAtHours;
                if (fields.Length != 11 ||
                    !int.TryParse(fields[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out version) ||
                    version != SchemaVersion ||
                    !int.TryParse(fields[7], NumberStyles.Integer, CultureInfo.InvariantCulture, out count) ||
                    !double.TryParse(fields[8], NumberStyles.Float, CultureInfo.InvariantCulture, out successionHours) ||
                    !double.TryParse(fields[10], NumberStyles.Float, CultureInfo.InvariantCulture, out destroyedAtHours))
                    continue;

                string kingdomId = Decode(fields[1]);
                if (string.IsNullOrEmpty(kingdomId) || Records.ContainsKey(kingdomId))
                    continue;

                Records.Add(kingdomId, new KingdomContinuityRecord
                {
                    KingdomId = kingdomId,
                    OriginalName = Decode(fields[2]),
                    CurrentName = Decode(fields[3]),
                    CultureId = Decode(fields[4]),
                    CurrentRulingClanId = Decode(fields[5]),
                    PreviousRulingClanId = Decode(fields[6]),
                    SuccessionCount = Math.Max(0, count),
                    LastSuccessionHours = Math.Max(0.0, successionHours),
                    Destroyed = fields[9] == "1",
                    DestroyedAtHours = Math.Max(0.0, destroyedAtHours)
                });
            }
        }

        private static void ShowNotice(string message)
        {
            InformationManager.DisplayMessage(
                new InformationMessage("ClanAI: " + message));
        }

        private static string NameOf(Kingdom kingdom)
        {
            return kingdom == null || kingdom.Name == null ? "<unknown kingdom>" : kingdom.Name.ToString();
        }

        private static string ClanId(Clan clan)
        {
            return clan == null ? null : clan.StringId;
        }

        private static string LeaderId(Clan clan)
        {
            return clan == null || clan.Leader == null
                ? null
                : clan.Leader.StringId;
        }

        private static string Encode(string value)
        {
            return string.IsNullOrEmpty(value)
                ? string.Empty
                : Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
        }

        private static string Decode(string value)
        {
            if (string.IsNullOrEmpty(value))
                return null;
            try
            {
                return Encoding.UTF8.GetString(Convert.FromBase64String(value));
            }
            catch
            {
                return null;
            }
        }

        private static string Safe(string value)
        {
            return string.IsNullOrEmpty(value) ? "<none>" : value.Replace(' ', '_');
        }

        private static string F(double value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }
    }
}

