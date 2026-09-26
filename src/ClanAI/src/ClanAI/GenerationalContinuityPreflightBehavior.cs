using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Settlements;

namespace ClanAI
{
    // Phase 7B runtime-preflight only. Read-only/session-only observation.
    // No lifecycle, succession, family, ownership, memory, or save mutation.
    public sealed class GenerationalContinuityPreflightBehavior
        : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(
                this, new Action<CampaignGameStarter>(OnSessionLaunched));
            CampaignEvents.BeforeHeroKilledEvent.AddNonSerializedListener(
                this, new Action<Hero, Hero, KillCharacterAction.KillCharacterActionDetail, bool>(OnBeforeHeroKilled));
            CampaignEvents.HeroKilledEvent.AddNonSerializedListener(
                this, new Action<Hero, Hero, KillCharacterAction.KillCharacterActionDetail, bool>(OnHeroKilled));
            CampaignEvents.OnClanLeaderChangedEvent.AddNonSerializedListener(
                this, new Action<Hero, Hero>(OnClanLeaderChanged));
            CampaignEvents.RulingClanChanged.AddNonSerializedListener(
                this, new Action<Kingdom, Clan>(OnRulingClanChanged));
            CampaignEvents.OnBeforeSaveEvent.AddNonSerializedListener(
                this, new Action(OnBeforeSave));
            CampaignEvents.OnSaveOverEvent.AddNonSerializedListener(
                this, new Action<bool, string>(OnSaveOver));
            CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(
                this, new Action(OnGameLoadFinished));
        }

        public override void SyncData(IDataStore dataStore)
        {
            // Intentionally empty: Phase 7B telemetry adds no save schema.
        }

        private static void OnSessionLaunched(CampaignGameStarter starter)
        {
            GenerationalContinuityRuntimeTelemetry.BeginSession();
            GenerationalContinuityRuntimeTelemetry.SnapshotAllRulers("session-start");
        }

        private static void OnBeforeHeroKilled(
            Hero victim,
            Hero killer,
            KillCharacterAction.KillCharacterActionDetail detail,
            bool showNotification)
        {
            GenerationalContinuityRuntimeTelemetry.ObserveHeroDeathEvent(
                "before-hero-killed",
                victim,
                killer,
                detail.ToString());
        }

        private static void OnHeroKilled(
            Hero victim,
            Hero killer,
            KillCharacterAction.KillCharacterActionDetail detail,
            bool showNotification)
        {
            GenerationalContinuityRuntimeTelemetry.ObserveHeroDeathEvent(
                "hero-killed",
                victim,
                killer,
                detail.ToString());
        }

        private static void OnClanLeaderChanged(
            Hero oldLeader,
            Hero newLeader)
        {
            GenerationalContinuityRuntimeTelemetry.ObserveClanLeaderChanged(
                oldLeader,
                newLeader);
        }

        private static void OnRulingClanChanged(
            Kingdom kingdom,
            Clan oldRulingClan)
        {
            GenerationalContinuityRuntimeTelemetry.ObserveRulingClanChanged(
                kingdom,
                oldRulingClan);
        }

        private static void OnBeforeSave()
        {
            GenerationalContinuityRuntimeTelemetry.SnapshotAllRulers(
                "pre-save-post-succession");
        }

        private static void OnSaveOver(
            bool success,
            string saveName)
        {
            GenerationalContinuityRuntimeTelemetry.ObserveSaveOver(
                success,
                saveName);
        }

        private static void OnGameLoadFinished()
        {
            GenerationalContinuityRuntimeTelemetry.SnapshotAllRulers(
                "post-reload");
        }
    }

    internal static class GenerationalContinuityRuntimeTelemetry
    {
        private static readonly HashSet<string> DeadHeroIds =
            new HashSet<string>(StringComparer.Ordinal);
        private static readonly HashSet<string> LoggedMemoryResolutions =
            new HashSet<string>(StringComparer.Ordinal);

        private static bool _crossHeroMemoryAppliedDetected;

        internal static void BeginSession()
        {
            DeadHeroIds.Clear();
            LoggedMemoryResolutions.Clear();
            _crossHeroMemoryAppliedDetected = false;

            ClanAIPostVanilla.WriteExternalLog(
                "GENCONT_PREFLIGHT_READY" +
                " campaignHour=" + D(SafeHours()) +
                " saveSchemaAdded=False" +
                " gameplayMutation=False" +
                " mutationByClanAI=False");
        }

        internal static void ObserveHeroMemoryResolution(
            string store,
            string requestedHeroId,
            string resolvedHeroId,
            bool influenceApplied)
        {
            if (!RuntimeProfile.EvidenceEnabled)
                return;

            string requested = CleanId(requestedHeroId);
            string resolved = CleanId(resolvedHeroId);
            bool crossHero =
                !string.Equals(
                    requested,
                    resolved,
                    StringComparison.Ordinal);

            if (crossHero && influenceApplied)
                _crossHeroMemoryAppliedDetected = true;

            string key =
                (store ?? "<none>") + "|" +
                requested + "|" +
                resolved + "|" +
                influenceApplied;

            if (!crossHero &&
                !LoggedMemoryResolutions.Add(key))
            {
                return;
            }

            bool resolvedKnownDead =
                DeadHeroIds.Contains(resolved);

            ClanAIPostVanilla.WriteExternalLog(
                "GENCONT_HERO_MEMORY_RESOLUTION" +
                " campaignHour=" + D(SafeHours()) +
                " store=" + Safe(store) +
                " requestedHeroId=" + Safe(requested) +
                " resolvedHeroId=" + Safe(resolved) +
                " influenceApplied=" + influenceApplied +
                " crossHero=" + crossHero +
                " resolvedHeroKnownDead=" + resolvedKnownDead +
                " deceasedHeroKeyAppliedToDifferentHero=" +
                    (crossHero && resolvedKnownDead && influenceApplied) +
                " mutationByClanAI=False");
        }

        internal static void ObserveHeroDeathEvent(
            string stage,
            Hero victim,
            Hero killer,
            string detail)
        {
            if (victim != null &&
                !string.IsNullOrEmpty(victim.StringId) &&
                string.Equals(stage, "hero-killed", StringComparison.Ordinal))
            {
                DeadHeroIds.Add(victim.StringId);
            }

            Clan clan = victim == null ? null : victim.Clan;
            Kingdom kingdom = clan == null ? null : clan.Kingdom;

            ClanAIPostVanilla.WriteExternalLog(
                "GENCONT_NATIVE_HERO_DEATH" +
                " stage=" + Safe(stage) +
                " campaignHour=" + D(SafeHours()) +
                " heroId=" + HeroId(victim) +
                " hero=" + HeroName(victim) +
                " clanId=" + ClanId(clan) +
                " kingdomId=" + KingdomId(kingdom) +
                " wasClanLeader=" +
                    (clan != null &&
                     object.ReferenceEquals(clan.Leader, victim)) +
                " wasRulingClanLeader=" +
                    (kingdom != null &&
                     object.ReferenceEquals(kingdom.RulingClan, clan) &&
                     clan != null &&
                     object.ReferenceEquals(clan.Leader, victim)) +
                " killerId=" + HeroId(killer) +
                " detail=" + Safe(detail) +
                " mutationByClanAI=False");
        }

        internal static void ObserveClanLeaderChanged(
            Hero oldLeader,
            Hero newLeader)
        {
            Clan clan =
                newLeader != null && newLeader.Clan != null
                    ? newLeader.Clan
                    : (oldLeader == null ? null : oldLeader.Clan);
            Kingdom kingdom = clan == null ? null : clan.Kingdom;

            ClanAIPostVanilla.WriteExternalLog(
                "GENCONT_NATIVE_CLAN_LEADER_CHANGED" +
                " campaignHour=" + D(SafeHours()) +
                " clanId=" + ClanId(clan) +
                " kingdomId=" + KingdomId(kingdom) +
                " oldLeaderId=" + HeroId(oldLeader) +
                " newLeaderId=" + HeroId(newLeader) +
                " oldLeaderDead=" + IsDead(oldLeader) +
                " newLeaderDead=" + IsDead(newLeader) +
                " clanOwnsSettlements=" + SettlementIds(clan) +
                " mutationByClanAI=False");

            if (kingdom != null)
                SnapshotKingdom("clan-leader-change", kingdom);
        }

        internal static void ObserveRulingClanChanged(
            Kingdom kingdom,
            Clan oldRulingClan)
        {
            ClanAIPostVanilla.WriteExternalLog(
                "GENCONT_NATIVE_RULING_CLAN_CHANGED" +
                " campaignHour=" + D(SafeHours()) +
                " kingdomId=" + KingdomId(kingdom) +
                " oldRulingClanId=" + ClanId(oldRulingClan) +
                " newRulingClanId=" +
                    ClanId(kingdom == null ? null : kingdom.RulingClan) +
                " oldLeaderId=" +
                    HeroId(oldRulingClan == null ? null : oldRulingClan.Leader) +
                " newLeaderId=" +
                    HeroId(kingdom == null ||
                           kingdom.RulingClan == null
                        ? null
                        : kingdom.RulingClan.Leader) +
                " mutationByClanAI=False");

            SnapshotKingdom("ruling-clan-change", kingdom);
        }

        internal static void ObserveSaveOver(
            bool success,
            string saveName)
        {
            ClanAIPostVanilla.WriteExternalLog(
                "GENCONT_SAVE_OVER" +
                " campaignHour=" + D(SafeHours()) +
                " success=" + success +
                " saveName=" + Safe(saveName) +
                " mutationByClanAI=False");
        }

        internal static void SnapshotAllRulers(
            string stage)
        {
            try
            {
                for (int i = 0; i < Kingdom.All.Count; i++)
                {
                    Kingdom kingdom = Kingdom.All[i];
                    if (kingdom == null || kingdom.IsEliminated)
                        continue;

                    SnapshotKingdom(stage, kingdom);
                }
            }
            catch (Exception ex)
            {
                LogError("snapshot-all", ex);
            }
        }

        private static void SnapshotKingdom(
            string stage,
            Kingdom kingdom)
        {
            try
            {
                if (kingdom == null)
                    return;

                Clan rulingClan = kingdom.RulingClan;
                Hero ruler =
                    rulingClan == null
                        ? null
                        : rulingClan.Leader;

                string heroId =
                    ruler == null
                        ? null
                        : ruler.StringId;
                string clanId =
                    rulingClan == null
                        ? null
                        : rulingClan.StringId;

                ClanAIPostVanilla.WriteExternalLog(
                    "GENCONT_RULER_SNAPSHOT" +
                    " stage=" + Safe(stage) +
                    " campaignHour=" + D(SafeHours()) +
                    " targetRulerHeroId=" + HeroId(ruler) +
                    " targetRuler=" + HeroName(ruler) +
                    " rulerDead=" + IsDead(ruler) +
                    " rulingClanId=" + ClanId(rulingClan) +
                    " kingdomId=" + KingdomId(kingdom) +
                    " kingdomCulture=" +
                        (kingdom.Culture == null
                            ? "<none>"
                            : Safe(kingdom.Culture.StringId)) +
                    " currentClanLeaderId=" +
                        HeroId(rulingClan == null ? null : rulingClan.Leader) +
                    " clanOwnedSettlements=" +
                        SettlementIds(rulingClan) +
                    " spouseId=" + RelatedHeroId(ruler, "Spouse") +
                    " childIds=" + RelatedHeroIds(ruler, "Children") +
                    " familyIds=" + FamilyIds(ruler) +
                    " kingdomContinuitySuccessionCount=" +
                        KingdomSuccessionCount(kingdom.StringId) +
                    " clanLossMemoryCount=" +
                        CountRecordsByField(
                            typeof(SocialLoyaltyClanLossMemory),
                            "Records",
                            "ClanId",
                            clanId) +
                    " nobleMemoryCount=" +
                        CountDictionaryKey(
                            typeof(NobleMemory),
                            "Records",
                            heroId) +
                    " socialLedgerActorCount=" +
                        CountRecordsByField(
                            typeof(SocialLedger),
                            "Records",
                            "ActorHeroId",
                            heroId) +
                    " companionDutyMemoryCount=" +
                        CountDictionaryKey(
                            typeof(CompanionDutyMemory),
                            "Records",
                            heroId) +
                    " companionExperienceMemoryCount=" +
                        CountNestedDictionaryEntries(
                            typeof(CompanionExperienceMemory),
                            "ByHero",
                            heroId) +
                    " companionNegativeOutcomeCount=" +
                        CountDictionaryKey(
                            typeof(CompanionNegativeOutcomeMemory),
                            "Records",
                            heroId) +
                    " dynastyBranchActorEpisodeCount=" +
                        CountRecordsByField(
                            typeof(DynastyBranchEpisodeMemory),
                            "Episodes",
                            "ActorId",
                            heroId) +
                    " dynastyBranchTotalEpisodes=" +
                        CollectionCount(
                            typeof(DynastyBranchEpisodeMemory),
                            "Episodes") +
                    " socialEpisodeTotal=" +
                        SocialEpisodeTotalCount() +
                    WarStateFields() +
                    " crossHeroMemoryAppliedDetected=" +
                        _crossHeroMemoryAppliedDetected +
                    " mutationByClanAI=False");
            }
            catch (Exception ex)
            {
                LogError("snapshot-kingdom", ex);
            }
        }

        private static int KingdomSuccessionCount(
            string kingdomId)
        {
            object records =
                StaticField(
                    typeof(KingdomContinuityLedger),
                    "Records");
            IDictionary dictionary = records as IDictionary;
            if (dictionary == null ||
                string.IsNullOrEmpty(kingdomId) ||
                !dictionary.Contains(kingdomId))
            {
                return -1;
            }

            object record = dictionary[kingdomId];
            return IntField(record, "SuccessionCount", -1);
        }

        private static int CountDictionaryKey(
            Type owner,
            string fieldName,
            string key)
        {
            if (string.IsNullOrEmpty(key))
                return 0;

            IDictionary dictionary =
                StaticField(owner, fieldName)
                    as IDictionary;

            return dictionary != null &&
                dictionary.Contains(key)
                    ? 1
                    : 0;
        }

        private static int CountNestedDictionaryEntries(
            Type owner,
            string fieldName,
            string key)
        {
            if (string.IsNullOrEmpty(key))
                return 0;

            IDictionary dictionary =
                StaticField(owner, fieldName)
                    as IDictionary;

            if (dictionary == null ||
                !dictionary.Contains(key))
            {
                return 0;
            }

            object value = dictionary[key];

            IDictionary nested =
                value as IDictionary;
            if (nested != null)
                return nested.Count;

            ICollection values =
                value as ICollection;
            if (values != null)
                return values.Count;

            PropertyInfo count =
                value == null
                    ? null
                    : value.GetType().GetProperty(
                        "Count",
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic);

            return count == null
                ? 0
                : Convert.ToInt32(
                    count.GetValue(value, null),
                    CultureInfo.InvariantCulture);
        }

        private static int CountRecordsByField(
            Type owner,
            string fieldName,
            string memberName,
            string expected)
        {
            if (string.IsNullOrEmpty(expected))
                return 0;

            object collection =
                StaticField(owner, fieldName);

            int count = 0;

            IDictionary dictionary =
                collection as IDictionary;
            if (dictionary != null)
            {
                foreach (DictionaryEntry item in dictionary)
                {
                    if (MemberString(
                            item.Value,
                            memberName) == expected)
                    {
                        count++;
                    }
                }
                return count;
            }

            IEnumerable enumerable =
                collection as IEnumerable;
            if (enumerable != null)
            {
                foreach (object item in enumerable)
                {
                    if (MemberString(
                            item,
                            memberName) == expected)
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        private static int CollectionCount(
            Type owner,
            string fieldName)
        {
            object value =
                StaticField(owner, fieldName);

            IDictionary dictionary =
                value as IDictionary;
            if (dictionary != null)
                return dictionary.Count;

            ICollection collection =
                value as ICollection;
            if (collection != null)
                return collection.Count;

            PropertyInfo count =
                value == null
                    ? null
                    : value.GetType().GetProperty(
                        "Count",
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic);

            return count == null
                ? -1
                : Convert.ToInt32(
                    count.GetValue(value, null),
                    CultureInfo.InvariantCulture);
        }

        private static int SocialEpisodeTotalCount()
        {
            object store =
                StaticField(
                    typeof(SocialEpisodeMemory),
                    "_store");
            if (store == null)
                return -1;

            PropertyInfo count =
                store.GetType().GetProperty(
                    "Count",
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic);

            return count == null
                ? -1
                : Convert.ToInt32(
                    count.GetValue(store, null),
                    CultureInfo.InvariantCulture);
        }

        private static string WarStateFields()
        {
            return
                " warScarCount=" +
                    CollectionCount(typeof(WarStateBehavior), "Scars") +
                " warScarSettlementIds=" +
                    JoinFieldValues(
                        typeof(WarStateBehavior),
                        "Scars",
                        "SettlementId") +
                " activeSiegeCount=" +
                    CollectionCount(
                        typeof(WarStateBehavior),
                        "ActiveSieges") +
                " activeSiegeIds=" +
                    JoinDictionaryKeys(
                        typeof(WarStateBehavior),
                        "ActiveSieges") +
                " raidRecordCount=" +
                    CollectionCount(
                        typeof(WarStateBehavior),
                        "Raids") +
                " raidVillageIds=" +
                    JoinFieldValues(
                        typeof(WarStateBehavior),
                        "Raids",
                        "VillageId") +
                " lossRecordCount=" +
                    CollectionCount(
                        typeof(WarStateBehavior),
                        "Losses") +
                " lossSettlementIds=" +
                    JoinFieldValues(
                        typeof(WarStateBehavior),
                        "Losses",
                        "SettlementId") +
                " objectiveCount=" +
                    CollectionCount(
                        typeof(WarStateBehavior),
                        "Objectives") +
                " objectiveIds=" +
                    JoinDictionaryKeys(
                        typeof(WarStateBehavior),
                        "Objectives") +
                " strainKingdomIds=" +
                    JoinDictionaryKeys(
                        typeof(WarStateBehavior),
                        "StrainByKingdom");
        }

        private static string SettlementIds(
            Clan clan)
        {
            if (clan == null)
                return "<none>";

            List<string> ids =
                new List<string>();

            foreach (Settlement settlement in Settlement.All)
            {
                if (settlement == null ||
                    settlement.OwnerClan == null)
                {
                    continue;
                }

                if (object.ReferenceEquals(
                        settlement.OwnerClan,
                        clan) ||
                    string.Equals(
                        settlement.OwnerClan.StringId,
                        clan.StringId,
                        StringComparison.Ordinal))
                {
                    ids.Add(
                        string.IsNullOrEmpty(
                            settlement.StringId)
                            ? "<no-id>"
                            : settlement.StringId);
                }
            }

            return Join(ids);
        }

        private static string FamilyIds(
            Hero hero)
        {
            List<string> ids =
                new List<string>();

            AddUnique(ids, RawRelatedHeroId(hero, "Spouse"));
            AddUnique(ids, RawRelatedHeroId(hero, "Mother"));
            AddUnique(ids, RawRelatedHeroId(hero, "Father"));
            AddRelatedHeroIds(ids, hero, "Children");
            AddRelatedHeroIds(ids, hero, "Siblings");

            return Join(ids);
        }

        private static string RelatedHeroId(
            Hero hero,
            string propertyName)
        {
            return Safe(
                RawRelatedHeroId(
                    hero,
                    propertyName));
        }

        private static string RawRelatedHeroId(
            Hero hero,
            string propertyName)
        {
            if (hero == null)
                return null;

            PropertyInfo property =
                hero.GetType().GetProperty(
                    propertyName,
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic);

            Hero related =
                property == null
                    ? null
                    : property.GetValue(hero, null) as Hero;

            return related == null
                ? null
                : related.StringId;
        }

        private static string RelatedHeroIds(
            Hero hero,
            string propertyName)
        {
            List<string> ids =
                new List<string>();
            AddRelatedHeroIds(
                ids,
                hero,
                propertyName);
            return Join(ids);
        }

        private static void AddRelatedHeroIds(
            List<string> ids,
            Hero hero,
            string propertyName)
        {
            if (hero == null)
                return;

            PropertyInfo property =
                hero.GetType().GetProperty(
                    propertyName,
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic);

            IEnumerable values =
                property == null
                    ? null
                    : property.GetValue(hero, null)
                        as IEnumerable;

            if (values == null)
                return;

            foreach (object value in values)
            {
                Hero related = value as Hero;
                AddUnique(
                    ids,
                    related == null
                        ? null
                        : related.StringId);
            }
        }

        private static void AddUnique(
            List<string> values,
            string value)
        {
            if (!string.IsNullOrEmpty(value) &&
                !values.Contains(value))
            {
                values.Add(value);
            }
        }

        private static string JoinDictionaryKeys(
            Type owner,
            string fieldName)
        {
            IDictionary dictionary =
                StaticField(owner, fieldName)
                    as IDictionary;

            if (dictionary == null ||
                dictionary.Count == 0)
            {
                return "<none>";
            }

            List<string> values =
                new List<string>();

            foreach (object key in dictionary.Keys)
                AddUnique(values, key == null ? null : key.ToString());

            return Join(values);
        }

        private static string JoinFieldValues(
            Type owner,
            string fieldName,
            string memberName)
        {
            IEnumerable enumerable =
                StaticField(owner, fieldName)
                    as IEnumerable;

            if (enumerable == null)
                return "<none>";

            List<string> values =
                new List<string>();

            foreach (object item in enumerable)
                AddUnique(values, MemberString(item, memberName));

            return Join(values);
        }

        private static object StaticField(
            Type owner,
            string fieldName)
        {
            FieldInfo field =
                owner.GetField(
                    fieldName,
                    BindingFlags.Static |
                    BindingFlags.Public |
                    BindingFlags.NonPublic);

            return field == null
                ? null
                : field.GetValue(null);
        }

        private static string MemberString(
            object value,
            string memberName)
        {
            if (value == null)
                return null;

            Type type = value.GetType();

            FieldInfo field =
                type.GetField(
                    memberName,
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic);

            if (field != null)
            {
                object fieldValue =
                    field.GetValue(value);
                return fieldValue == null
                    ? null
                    : fieldValue.ToString();
            }

            PropertyInfo property =
                type.GetProperty(
                    memberName,
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic);

            if (property == null)
                return null;

            object propertyValue =
                property.GetValue(value, null);

            return propertyValue == null
                ? null
                : propertyValue.ToString();
        }

        private static int IntField(
            object value,
            string memberName,
            int fallback)
        {
            string text =
                MemberString(
                    value,
                    memberName);
            int parsed;
            return int.TryParse(
                    text,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out parsed)
                ? parsed
                : fallback;
        }

        private static bool IsDead(
            Hero hero)
        {
            return hero != null && hero.IsDead;
        }

        private static string HeroId(
            Hero hero)
        {
            return hero == null ||
                string.IsNullOrEmpty(hero.StringId)
                    ? "<none>"
                    : Safe(hero.StringId);
        }

        private static string HeroName(
            Hero hero)
        {
            return hero == null ||
                hero.Name == null
                    ? "<none>"
                    : Safe(hero.Name.ToString());
        }

        private static string ClanId(
            Clan clan)
        {
            return clan == null ||
                string.IsNullOrEmpty(clan.StringId)
                    ? "<none>"
                    : Safe(clan.StringId);
        }

        private static string KingdomId(
            Kingdom kingdom)
        {
            return kingdom == null ||
                string.IsNullOrEmpty(kingdom.StringId)
                    ? "<none>"
                    : Safe(kingdom.StringId);
        }

        private static string CleanId(
            string value)
        {
            return string.IsNullOrEmpty(value)
                ? "<none>"
                : value;
        }

        private static string Join(
            List<string> values)
        {
            return values == null ||
                values.Count == 0
                    ? "<none>"
                    : Safe(string.Join(",", values.ToArray()));
        }

        private static string Safe(
            string value)
        {
            return string.IsNullOrEmpty(value)
                ? "<none>"
                : value
                    .Replace(' ', '_')
                    .Replace('\r', '_')
                    .Replace('\n', '_')
                    .Replace('\t', '_');
        }

        private static double SafeHours()
        {
            try
            {
                return CampaignTime.Now.ToHours;
            }
            catch
            {
                return 0.0;
            }
        }

        private static string D(
            double value)
        {
            return value.ToString(
                "R",
                CultureInfo.InvariantCulture);
        }

        private static void LogError(
            string stage,
            Exception ex)
        {
            ClanAIPostVanilla.WriteExternalLog(
                "GENCONT_PREFLIGHT_ERROR" +
                " stage=" + Safe(stage) +
                " type=" + ex.GetType().Name +
                " mutationByClanAI=False");
        }
    }
}


