using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using TaleWorlds.CampaignSystem;

namespace ClanAI
{
    // Read-only evidence adapter. No writer, policy, native action or save-state writes.
    public sealed class DynastyStructuralRuntimeObserver : CampaignBehaviorBase
    {
        private const BindingFlags Fields = BindingFlags.Public | BindingFlags.NonPublic |
            BindingFlags.Instance | BindingFlags.Static;

        private static string _pendingStage;
        internal static void FlushPending()
        {
            string stage = _pendingStage;
            _pendingStage = null;
            if (stage != null) Capture(stage);
        }

        public override void RegisterEvents()
        {
            CampaignEvents.RulingClanChanged.AddNonSerializedListener(this,
                new Action<Kingdom, Clan>(OnRulingClanChanged));
            CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(this,
                new Action(OnLoadFinished));
            CampaignEvents.OnBeforeSaveEvent.AddNonSerializedListener(this,
                new Action(OnBeforeSave));
        }

        public override void SyncData(IDataStore dataStore) { }
        private void OnLoadFinished() { _pendingStage = "post-load"; }
        private void OnBeforeSave() { Capture("pre-save"); }

        private void OnRulingClanChanged(Kingdom kingdom, Clan oldClan)
        {
            try
            {
                Clan newClan = kingdom == null ? null : kingdom.RulingClan;
                bool qualified = kingdom != null && !kingdom.IsEliminated &&
                    oldClan != null && newClan != null &&
                    !string.Equals(oldClan.StringId, newClan.StringId, StringComparison.Ordinal);
                Log("I3_RUNTIME_EVENT campaignHour=" + Number(CampaignTime.Now.ToHours) +
                    " kingdomId=" + Safe(kingdom == null ? null : kingdom.StringId) +
                    " oldRulingClanId=" + Safe(oldClan == null ? null : oldClan.StringId) +
                    " newRulingClanId=" + Safe(newClan == null ? null : newClan.StringId) +
                    " oldRulerHeroId=" + LeaderId(oldClan) +
                    " newRulerHeroId=" + LeaderId(newClan) +
                    " qualified=" + qualified + " source=CampaignEvents.RulingClanChanged");
                _pendingStage = "native-event";
            }
            catch (Exception ex) { Error("native-event", ex); }
        }

        private static void Capture(string stage)
        {
            try
            {
                string branch = DynastyMindSeed.BranchId;
                string actor = Hero.MainHero == null ? "" : Hero.MainHero.StringId;
                double now = CampaignTime.Now.ToHours;
                IEnumerable episodes = (IEnumerable)ReadStatic(typeof(DynastyBranchEpisodeMemory), "Episodes");
                var rows = new List<object>();
                var identities = new Dictionary<string, int>(StringComparer.Ordinal);
                foreach (object row in episodes)
                {
                    if (Text(row, "Kind") != "KingdomRulingClanChanged") continue;
                    rows.Add(row);
                    string identity = Identity(row);
                    if (!identities.ContainsKey(identity)) identities.Add(identity, 0);
                    identities[identity]++;
                }
                Log("I3_RUNTIME_SNAPSHOT stage=" + stage + " campaignHour=" + Number(now) +
                    " branchId=" + Safe(branch) + " observerActorId=" + Safe(actor) +
                    " structuralRows=" + rows.Count + " semanticIdentities=" + identities.Count);
                foreach (object row in rows)
                {
                    var descriptor = new DynastyEpisodeDescriptor {
                        Id = Text(row, "Id"), BranchId = Text(row, "BranchId"),
                        ActorId = Text(row, "ActorId"), ActorName = Text(row, "ActorName"),
                        Kind = Text(row, "Kind"), ObservedUtc = Text(row, "ObservedUtc"),
                        CampaignHours = Convert.ToDouble(Read(row, "CampaignHours"), CultureInfo.InvariantCulture)
                    };
                    var personal = DynastyEpisodeRetrievalPolicy.SelectLatestActorEpisode(
                        new List<DynastyEpisodeDescriptor> { descriptor }, branch, actor, now);
                    Log("I3_RUNTIME_ROW stage=" + stage + " id=" + Safe(descriptor.Id) +
                        " branchId=" + Safe(descriptor.BranchId) + " actorId=" + Safe(descriptor.ActorId) +
                        " kind=" + descriptor.Kind + " campaignHour=" + Number(descriptor.CampaignHours) +
                        " contextId=" + Safe(Text(row, "ContextId")) +
                        " source=" + Safe(Text(row, "Source")) + " detail=" + Safe(Text(row, "Detail")) +
                        " semanticIdentity=" + Identity(row) + " sameIdentityCount=" + identities[Identity(row)] +
                        " personalStructuralVisibleCount=" + personal.VisibleCount +
                        " personalSelectedStructural=" + (personal.Episode != null));
                }
                IDictionary ledger = (IDictionary)ReadStatic(typeof(KingdomContinuityLedger), "Records");
                foreach (DictionaryEntry entry in ledger)
                {
                    object record = entry.Value;
                    Log("I3_RUNTIME_LEDGER stage=" + stage + " kingdomId=" + Safe(Text(record, "KingdomId")) +
                        " rulingClanId=" + Safe(Text(record, "CurrentRulingClanId")) +
                        " successionOrdinal=" + Text(record, "SuccessionCount") +
                        " lastSuccessionHour=" + Text(record, "LastSuccessionHours") +
                        " destroyed=" + Text(record, "Destroyed"));
                }
                string context = "phase7ci3-" + stage;
                Log("I3_RUNTIME_HISTORY_RECEIPT stage=" + stage + " receipt=" +
                    EmptyJson(DynastyMindOperatorBridge.RetrieveLatestDynastyHistory(context)));
                Log("I3_RUNTIME_PERSONAL_RECEIPT stage=" + stage + " receipt=" +
                    EmptyJson(DynastyMindOperatorBridge.RetrieveLatestBranchEpisode(context)));
                Log("I3_RUNTIME_CHOICE_RECEIPT stage=" + stage + " receipt=" +
                    EmptyJson(DynastyMindOperatorBridge.RetrieveLatestBranchChoice(context)));
            }
            catch (Exception ex) { Error(stage, ex); }
        }

        private static string Identity(object row)
        {
            return DynastyStructuralEpisodePolicy.IdentityFromStoredRow(
                Text(row, "BranchId"), Text(row, "Kind"), Text(row, "Detail")) ?? "<invalid>";
        }
        private static object ReadStatic(Type owner, string name)
        {
            FieldInfo field = owner.GetField(name, Fields);
            if (field == null) throw new MissingFieldException(owner.FullName, name);
            return field.GetValue(null);
        }
        private static object Read(object owner, string name)
        {
            FieldInfo field = owner.GetType().GetField(name, Fields);
            if (field == null) throw new MissingFieldException(owner.GetType().FullName, name);
            return field.GetValue(owner);
        }
        private static string Text(object owner, string name)
        {
            return Convert.ToString(Read(owner, name), CultureInfo.InvariantCulture);
        }
        private static string LeaderId(Clan clan)
        {
            return Safe(clan == null || clan.Leader == null ? null : clan.Leader.StringId);
        }
        private static string Number(double value) { return value.ToString("R", CultureInfo.InvariantCulture); }
        private static string Safe(string value)
        {
            return string.IsNullOrEmpty(value) ? "<none>" : value.Replace(' ', '_').Replace('\r', '_').Replace('\n', '_');
        }
        private static string EmptyJson(string value) { return string.IsNullOrEmpty(value) ? "null" : value; }
        private static void Error(string stage, Exception ex)
        {
            Log("I3_RUNTIME_ERROR stage=" + stage + " type=" + ex.GetType().Name);
        }
        private static void Log(string text)
        {
            try { ClanAIPostVanilla.WriteExternalLog(text + " mutationByClanAI=False"); }
            catch { } // Observation output must never disrupt native processing.
        }
    }
}
