using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace ClanAI
{
    internal static class CompanionExperienceMemory
    {
        private const int MaxHoldingsPerHero = 4;
        private const double DutyLifetimeHours = 168.0;
        private const double ThreatLifetimeHours = 72.0;
        private const double HoldbackThreatLifetimeHours = 168.0;
        private const double ReinforceCooldownHours = 6.0;
        private const float BaseDutyBonus = 0.04f;
        private const float MaxDutyBonus = 0.10f;
        private const float BaseThreatBonus = 0.16f;
        private const float MaxThreatBonus = 0.24f;
        private const float MaxCombinedFactor = 1.28f;
        private const float BaseVigilanceBonus = 0.05f;
        private const float MaxVigilanceBonus = 0.12f;

        private sealed class Entry
        {
            internal string HeroKey;
            internal string ActorName;
            internal string SettlementId;
            internal string SettlementName;
            internal int DutySuccesses;
            internal double LastDutyHours;
            internal int ThreatCount;
            internal double LastThreatHours;
            internal string LastThreatType;
            internal int RecallUses;
        }

        private sealed class PendingCommit
        {
            internal string ActorName;
            internal string SettlementId;
            internal AiBehavior Behavior;
            internal string Label;
            internal string Reason;
        }

        private static readonly Dictionary<string, Dictionary<string, Entry>> ByHero =
            new Dictionary<string, Dictionary<string, Entry>>(StringComparer.Ordinal);
        private static readonly Dictionary<string, PendingCommit> PendingByParty =
            new Dictionary<string, PendingCommit>(StringComparer.Ordinal);

        private static bool _loadedFromSave;
        private static long _recalls;
        private static long _winnerChanges;
        private static long _commitChecks;
        private static long _commitMatches;

        internal static void BeginSession()
        {
            if (!_loadedFromSave)
                ByHero.Clear();

            PendingByParty.Clear();
            _recalls = 0;
            _winnerChanges = 0;
            _commitChecks = 0;
            _commitMatches = 0;

            ClanAIPostVanilla.WriteExternalLog(
                "EXPERIENCE_MEMORY_SESSION_READY heroes=" + ByHero.Count +
                " entries=" + CountEntries() +
                " restored=" + _loadedFromSave +
                " maxHoldings=" + MaxHoldingsPerHero);

            _loadedFromSave = false;
        }

        internal static void SyncData(IDataStore dataStore)
        {
            List<string> lines = new List<string>();
            if (dataStore.IsSaving)
                lines = ExportSaveLines();

            bool found = dataStore.SyncData(
                "ClanAI_CompanionExperienceMemory_v1",
                ref lines);

            if (dataStore.IsLoading)
                ImportSaveLines(found ? lines : null);
        }

        private static List<string> ExportSaveLines()
        {
            var lines = new List<string>();
            foreach (var heroPair in ByHero)
            {
                foreach (var settlementPair in heroPair.Value)
                {
                    Entry e = settlementPair.Value;
                    lines.Add(
                        B64(e.HeroKey) + "|" +
                        B64(e.ActorName) + "|" +
                        B64(e.SettlementId) + "|" +
                        B64(e.SettlementName) + "|" +
                        e.DutySuccesses.ToString(CultureInfo.InvariantCulture) + "|" +
                        e.LastDutyHours.ToString("R", CultureInfo.InvariantCulture) + "|" +
                        e.ThreatCount.ToString(CultureInfo.InvariantCulture) + "|" +
                        e.LastThreatHours.ToString("R", CultureInfo.InvariantCulture) + "|" +
                        B64(e.LastThreatType) + "|" +
                        e.RecallUses.ToString(CultureInfo.InvariantCulture));
                }
            }

            ClanAIPostVanilla.WriteExternalLog(
                "EXPERIENCE_MEMORY_SAVE heroes=" + ByHero.Count +
                " entries=" + lines.Count);
            return lines;
        }

        private static void ImportSaveLines(List<string> lines)
        {
            ByHero.Clear();
            if (lines != null)
            {
                foreach (string line in lines)
                {
                    if (string.IsNullOrEmpty(line))
                        continue;
                    string[] p = line.Split('|');
                    if (p.Length != 10)
                        continue;

                    int dutySuccesses;
                    double lastDuty;
                    int threatCount;
                    double lastThreat;
                    int recallUses;
                    if (!int.TryParse(p[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out dutySuccesses) ||
                        !double.TryParse(p[5], NumberStyles.Float, CultureInfo.InvariantCulture, out lastDuty) ||
                        !int.TryParse(p[6], NumberStyles.Integer, CultureInfo.InvariantCulture, out threatCount) ||
                        !double.TryParse(p[7], NumberStyles.Float, CultureInfo.InvariantCulture, out lastThreat) ||
                        !int.TryParse(p[9], NumberStyles.Integer, CultureInfo.InvariantCulture, out recallUses))
                        continue;

                    string heroKey = FromB64(p[0]);
                    string settlementId = FromB64(p[2]);
                    if (string.IsNullOrEmpty(heroKey) || string.IsNullOrEmpty(settlementId))
                        continue;

                    Dictionary<string, Entry> map = GetOrCreateHeroMap(heroKey);
                    map[settlementId] = new Entry
                    {
                        HeroKey = heroKey,
                        ActorName = FromB64(p[1]),
                        SettlementId = settlementId,
                        SettlementName = FromB64(p[3]),
                        DutySuccesses = dutySuccesses,
                        LastDutyHours = lastDuty,
                        ThreatCount = threatCount,
                        LastThreatHours = lastThreat,
                        LastThreatType = FromB64(p[8]),
                        RecallUses = recallUses
                    };
                }
            }

            _loadedFromSave = true;
            ClanAIPostVanilla.WriteExternalLog(
                "EXPERIENCE_MEMORY_LOAD heroes=" + ByHero.Count +
                " entries=" + CountEntries());
        }

        internal static void RecordSuccessfulDuty(
            MobileParty actor,
            AiBehavior behavior,
            Settlement settlement,
            string source)
        {
            if (!Eligible(actor) || settlement == null ||
                !IsHomeDutyBehavior(behavior) ||
                !SameClan(settlement.OwnerClan, actor.ActualClan))
                return;

            string heroKey = HeroKey(actor);
            if (string.IsNullOrEmpty(heroKey))
                return;

            double nowHours = CampaignTime.Now.ToHours;
            Dictionary<string, Entry> map = GetOrCreateHeroMap(heroKey);
            Entry entry;
            bool exists = map.TryGetValue(settlement.StringId, out entry);

            if (!exists)
            {
                entry = new Entry
                {
                    HeroKey = heroKey,
                    ActorName = actor.LeaderHero.Name.ToString(),
                    SettlementId = settlement.StringId,
                    SettlementName = settlement.Name.ToString(),
                    DutySuccesses = 1,
                    LastDutyHours = nowHours,
                    ThreatCount = 0,
                    LastThreatHours = 0.0,
                    LastThreatType = null,
                    RecallUses = 0
                };
                map[settlement.StringId] = entry;
                TrimHeroMap(map);

                ClanAIPostVanilla.WriteExternalLog(
                    "EXPERIENCE_MEMORY_DUTY_LEARNED actor=" + entry.ActorName +
                    " settlement=" + entry.SettlementName +
                    " source=" + (source ?? "<none>") +
                    " rememberedPlaces=" + map.Count);
                return;
            }

            entry.ActorName = actor.LeaderHero.Name.ToString();
            entry.SettlementName = settlement.Name.ToString();
            if ((nowHours - entry.LastDutyHours) < ReinforceCooldownHours)
                return;

            entry.LastDutyHours = nowHours;
            entry.DutySuccesses++;
            ClanAIPostVanilla.WriteExternalLog(
                "EXPERIENCE_MEMORY_DUTY_REINFORCED actor=" + entry.ActorName +
                " settlement=" + entry.SettlementName +
                " dutySuccesses=" + entry.DutySuccesses +
                " source=" + (source ?? "<none>") +
                " rememberedPlaces=" + map.Count);
        }

        internal static void ObserveAndApply(
            MobileParty actor,
            PartyThinkParams thinkParams,
            StrategicDecisionComposer.Frame composer)
        {
            VerifyPendingCommit(actor);

            if (!Eligible(actor) || thinkParams == null || composer == null)
                return;

            ObserveThreats(actor);
            ObserveCurrentCommittedHomeDuty(actor);

            string heroKey = HeroKey(actor);
            Dictionary<string, Entry> map;
            if (string.IsNullOrEmpty(heroKey) || !ByHero.TryGetValue(heroKey, out map))
                return;

            PruneInvalidOrExpired(map, actor);
            if (map.Count == 0)
                return;

            int beforeIndex = composer.CurrentBestIndex(thinkParams);
            string before = CandidateLabel(thinkParams, beforeIndex);
            string primarySettlementId = CompanionDutyMemory.GetRememberedSettlementId(actor);
            double nowHours = CampaignTime.Now.ToHours;
            float vigilanceBonus = RecentResolvedThreatVigilance(map, nowHours);

            var appliedFactorByIndex = new Dictionary<int, float>();
            var appliedReasonByIndex = new Dictionary<int, string>();
            var appliedSettlementByIndex = new Dictionary<int, string>();
            int applications = 0;
            for (int i = 0; i < thinkParams.AIBehaviorScores.Count; i++)
            {
                AIBehaviorData data = thinkParams.AIBehaviorScores[i].Item1;
                Settlement settlement = data.Party as Settlement;
                if (settlement == null || !IsHomeDutyBehavior(data.AiBehavior))
                    continue;

                Entry entry;
                if (!map.TryGetValue(settlement.StringId, out entry))
                    continue;

                float bonus = 0f;
                string reason = null;

                double dutyAge = nowHours - entry.LastDutyHours;
                if (dutyAge < 0.0) dutyAge = 0.0;
                if (entry.DutySuccesses > 0 &&
                    dutyAge <= DutyLifetimeHours &&
                    !string.Equals(settlement.StringId, primarySettlementId, StringComparison.Ordinal))
                {
                    double freshness = 1.0 - (dutyAge / DutyLifetimeHours);
                    if (freshness < 0.0) freshness = 0.0;
                    float learned = Math.Min(
                        MaxDutyBonus,
                        BaseDutyBonus + Math.Max(0, entry.DutySuccesses - 1) * 0.015f);
                    bonus += learned * (float)freshness;
                    reason = "additional-duty-history";
                }

                double threatAge = nowHours - entry.LastThreatHours;
                if (threatAge < 0.0) threatAge = 0.0;
                if (entry.ThreatCount > 0 &&
                    threatAge <= ThreatLifetimeHours &&
                    !settlement.IsUnderSiege &&
                    !settlement.IsUnderRaid)
                {
                    double freshness = 1.0 - (threatAge / ThreatLifetimeHours);
                    if (freshness < 0.0) freshness = 0.0;
                    float learned = Math.Min(
                        MaxThreatBonus,
                        BaseThreatBonus + Math.Max(0, entry.ThreatCount - 1) * 0.02f);
                    bonus += learned * (float)freshness;
                    reason = reason == null ? "recent-threat-history" : "duty-and-threat-history";
                }

                if (vigilanceBonus > 0.001f &&
                    SameClan(settlement.OwnerClan, actor.ActualClan) &&
                    IsProtectiveHomeBehavior(data.AiBehavior))
                {
                    bonus += vigilanceBonus;
                    reason = reason == null
                        ? "recent-threat-vigilance"
                        : reason + "+recent-threat-vigilance";
                }

                if (bonus <= 0.001f)
                    continue;

                float factor = Math.Min(MaxCombinedFactor, 1f + bonus);
                float rawScore = thinkParams.AIBehaviorScores[i].Item2;
                float baseScore = composer.CurrentScore(i, rawScore);
                if (baseScore <= 0f)
                    continue;

                composer.ApplyFactor(
                    i,
                    "companion-experience-memory",
                    baseScore,
                    factor,
                    reason);

                entry.RecallUses++;
                applications++;
                appliedFactorByIndex[i] = factor;
                appliedReasonByIndex[i] = reason;
                appliedSettlementByIndex[i] = entry.SettlementName;
            }

            if (applications == 0)
                return;

            _recalls++;
            int afterIndex = composer.CurrentBestIndex(thinkParams);
            string after = CandidateLabel(thinkParams, afterIndex);

            if (afterIndex == beforeIndex)
                return;

            float winningFactor;
            string winningReason;
            string winningSettlement;
            if (!appliedFactorByIndex.TryGetValue(afterIndex, out winningFactor) ||
                !appliedReasonByIndex.TryGetValue(afterIndex, out winningReason) ||
                !appliedSettlementByIndex.TryGetValue(afterIndex, out winningSettlement))
                return;

            _winnerChanges++;
            AIBehaviorData winner = thinkParams.AIBehaviorScores[afterIndex].Item1;
            Settlement winnerTarget = winner.Party as Settlement;
            string partyId = actor.StringId ?? heroKey;

            PendingByParty[partyId] = new PendingCommit
            {
                ActorName = actor.LeaderHero.Name.ToString(),
                SettlementId = winnerTarget == null ? null : winnerTarget.StringId,
                Behavior = winner.AiBehavior,
                Label = after,
                Reason = winningReason
            };

            ClanAIPostVanilla.WriteExternalLog(
                "EXPERIENCE_MEMORY_WINNER_CHANGE actor=" + actor.LeaderHero.Name.ToString() +
                " partyId=" + partyId +
                " before=" + before +
                " after=" + after +
                " settlement=" + winningSettlement +
                " factor=" + winningFactor.ToString("0.###", CultureInfo.InvariantCulture) +
                " reason=" + winningReason +
                " rememberedPlaces=" + map.Count +
                " winnerChanges=" + _winnerChanges);
        }

        private static void ObserveCurrentCommittedHomeDuty(MobileParty actor)
        {
            if (actor == null)
                return;

            Settlement target = actor.TargetSettlement ?? actor.ShortTermTargetSettlement;
            if (target == null || !SameClan(target.OwnerClan, actor.ActualClan))
                return;

            AiBehavior behavior = actor.ShortTermBehavior;
            if (!IsHomeDutyBehavior(behavior))
                behavior = actor.DefaultBehavior;
            if (!IsHomeDutyBehavior(behavior))
                return;

            RecordSuccessfulDuty(
                actor,
                behavior,
                target,
                "observed-committed-home-duty");
        }

        private static void ObserveThreats(MobileParty actor)
        {
            List<Settlement> threats =
                WorldScopeContext.ThreatenedFactionSettlements(actor);

            for (int i = 0; i < threats.Count; i++)
            {
                Settlement settlement = threats[i];
                if (settlement == null)
                    continue;

                RecordThreat(
                    actor,
                    settlement,
                    settlement.IsUnderSiege ? "siege" : "raid");
            }
        }

        private static void RecordThreat(
            MobileParty actor,
            Settlement settlement,
            string threatType)
        {
            string heroKey = HeroKey(actor);
            if (string.IsNullOrEmpty(heroKey) || settlement == null)
                return;

            double nowHours = CampaignTime.Now.ToHours;
            Dictionary<string, Entry> map = GetOrCreateHeroMap(heroKey);
            Entry entry;
            bool exists = map.TryGetValue(settlement.StringId, out entry);

            if (!exists)
            {
                entry = new Entry
                {
                    HeroKey = heroKey,
                    ActorName = actor.LeaderHero.Name.ToString(),
                    SettlementId = settlement.StringId,
                    SettlementName = settlement.Name.ToString(),
                    DutySuccesses = 0,
                    LastDutyHours = 0.0,
                    ThreatCount = 1,
                    LastThreatHours = nowHours,
                    LastThreatType = threatType,
                    RecallUses = 0
                };
                map[settlement.StringId] = entry;
                TrimHeroMap(map);

                ClanAIPostVanilla.WriteExternalLog(
                    "EXPERIENCE_MEMORY_THREAT_LEARNED actor=" + entry.ActorName +
                    " settlement=" + entry.SettlementName +
                    " threat=" + threatType +
                    " rememberedPlaces=" + map.Count);
                return;
            }

            entry.ActorName = actor.LeaderHero.Name.ToString();
            entry.SettlementName = settlement.Name.ToString();
            if ((nowHours - entry.LastThreatHours) < ReinforceCooldownHours &&
                string.Equals(entry.LastThreatType, threatType, StringComparison.Ordinal))
                return;

            entry.LastThreatHours = nowHours;
            entry.LastThreatType = threatType;
            entry.ThreatCount++;

            ClanAIPostVanilla.WriteExternalLog(
                "EXPERIENCE_MEMORY_THREAT_REINFORCED actor=" + entry.ActorName +
                " settlement=" + entry.SettlementName +
                " threat=" + threatType +
                " threatCount=" + entry.ThreatCount +
                " rememberedPlaces=" + map.Count);
        }

        private static void VerifyPendingCommit(MobileParty actor)
        {
            if (actor == null || string.IsNullOrEmpty(actor.StringId))
                return;

            PendingCommit pending;
            if (!PendingByParty.TryGetValue(actor.StringId, out pending))
                return;

            PendingByParty.Remove(actor.StringId);
            _commitChecks++;

            Settlement target = actor.TargetSettlement ?? actor.ShortTermTargetSettlement;
            bool behaviorMatch =
                actor.DefaultBehavior == pending.Behavior ||
                actor.ShortTermBehavior == pending.Behavior;
            bool targetMatch =
                string.IsNullOrEmpty(pending.SettlementId) ||
                (target != null && string.Equals(
                    target.StringId,
                    pending.SettlementId,
                    StringComparison.Ordinal));
            bool matched = behaviorMatch && targetMatch;
            if (matched)
            {
                _commitMatches++;
                RecordSuccessfulDuty(
                    actor,
                    pending.Behavior,
                    target,
                    "experience-memory-commit");
            }

            ClanAIPostVanilla.WriteExternalLog(
                "EXPERIENCE_MEMORY_COMMIT_CHECK actor=" + pending.ActorName +
                " partyId=" + actor.StringId +
                " expected=" + (pending.Label ?? "<none>") +
                " reason=" + (pending.Reason ?? "<none>") +
                " actualDefault=" + actor.DefaultBehavior +
                " actualShort=" + actor.ShortTermBehavior +
                " actualTarget=" + (target == null ? "<none>" : target.Name.ToString()) +
                " matched=" + matched +
                " checks=" + _commitChecks +
                " matches=" + _commitMatches);
        }

        private static Dictionary<string, Entry> GetOrCreateHeroMap(string heroKey)
        {
            Dictionary<string, Entry> map;
            if (!ByHero.TryGetValue(heroKey, out map))
            {
                map = new Dictionary<string, Entry>(StringComparer.Ordinal);
                ByHero[heroKey] = map;
            }
            return map;
        }

        private static void TrimHeroMap(Dictionary<string, Entry> map)
        {
            while (map.Count > MaxHoldingsPerHero)
            {
                string oldestKey = null;
                double oldestHours = double.MaxValue;
                foreach (var pair in map)
                {
                    Entry e = pair.Value;
                    double latest = Math.Max(e.LastDutyHours, e.LastThreatHours);
                    if (latest < oldestHours)
                    {
                        oldestHours = latest;
                        oldestKey = pair.Key;
                    }
                }

                if (oldestKey == null)
                    break;
                map.Remove(oldestKey);
            }
        }

        private static void PruneInvalidOrExpired(
            Dictionary<string, Entry> map,
            MobileParty actor)
        {
            double nowHours = CampaignTime.Now.ToHours;
            var remove = new List<string>();

            foreach (var pair in map)
            {
                Entry e = pair.Value;
                Settlement settlement = FindSettlement(e.SettlementId);
                bool relevant = settlement != null && actor != null &&
                    (SameClan(settlement.OwnerClan, actor.ActualClan) ||
                     SameFaction(settlement.MapFaction, actor.MapFaction));
                if (!relevant)
                {
                    remove.Add(pair.Key);
                    continue;
                }

                double dutyAge = e.DutySuccesses > 0 ? nowHours - e.LastDutyHours : double.MaxValue;
                double threatAge = e.ThreatCount > 0 ? nowHours - e.LastThreatHours : double.MaxValue;
                if (dutyAge > DutyLifetimeHours && threatAge > HoldbackThreatLifetimeHours)
                    remove.Add(pair.Key);
            }

            for (int i = 0; i < remove.Count; i++)
                map.Remove(remove[i]);
        }

        internal static bool TryGetRecentThreatForSettlement(
            MobileParty actor,
            string settlementId,
            out string settlementName,
            out string threatType,
            out double ageHours,
            out int threatCount)
        {
            settlementName = null;
            threatType = null;
            ageHours = double.MaxValue;
            threatCount = 0;
            string heroKey = HeroKey(actor);
            Dictionary<string, Entry> map;
            if (string.IsNullOrEmpty(heroKey) ||
                string.IsNullOrEmpty(settlementId) ||
                !ByHero.TryGetValue(heroKey, out map))
                return false;

            Entry entry;
            if (!map.TryGetValue(settlementId, out entry) || entry.ThreatCount <= 0)
                return false;

            double age = CampaignTime.Now.ToHours - entry.LastThreatHours;
            if (age < 0.0) age = 0.0;
            if (age > HoldbackThreatLifetimeHours)
                return false;

            settlementName = entry.SettlementName;
            threatType = entry.LastThreatType;
            ageHours = age;
            threatCount = entry.ThreatCount;
            return true;
        }

        internal static bool TryGetRecentThreatForHoldback(
            MobileParty actor, out string settlementName, out string threatType,
            out double ageHours, out int threatCount, out bool currentlyActive)
        {
            settlementName = null; threatType = null; ageHours = double.MaxValue;
            threatCount = 0; currentlyActive = false;
            string heroKey = HeroKey(actor); Dictionary<string, Entry> map;
            if (string.IsNullOrEmpty(heroKey) || !ByHero.TryGetValue(heroKey, out map)) return false;
            double nowHours = CampaignTime.Now.ToHours;
            foreach (var pair in map)
            {
                Entry entry = pair.Value; if (entry.ThreatCount <= 0) continue;
                double age = nowHours - entry.LastThreatHours; if (age < 0.0) age = 0.0;
                if (age > HoldbackThreatLifetimeHours || age >= ageHours) continue;
                Settlement settlement = FindSettlement(entry.SettlementId); if (settlement == null) continue;
                settlementName = entry.SettlementName; threatType = entry.LastThreatType;
                ageHours = age; threatCount = entry.ThreatCount;
                currentlyActive = settlement.IsUnderSiege || settlement.IsUnderRaid;
            }
            return settlementName != null;
        }

        private static float RecentResolvedThreatVigilance(
            Dictionary<string, Entry> map,
            double nowHours)
        {
            float best = 0f;
            foreach (var pair in map)
            {
                Entry entry = pair.Value;
                if (entry.ThreatCount <= 0)
                    continue;
                double age = nowHours - entry.LastThreatHours;
                if (age < 0.0) age = 0.0;
                if (age > ThreatLifetimeHours)
                    continue;
                Settlement settlement = FindSettlement(entry.SettlementId);
                if (settlement == null || settlement.IsUnderSiege || settlement.IsUnderRaid)
                    continue;
                double freshness = 1.0 - (age / ThreatLifetimeHours);
                float learned = Math.Min(MaxVigilanceBonus,
                    BaseVigilanceBonus + Math.Max(0, entry.ThreatCount - 1) * 0.01f);
                float bonus = learned * (float)freshness;
                if (bonus > best)
                    best = bonus;
            }
            return best;
        }

        private static bool IsProtectiveHomeBehavior(AiBehavior behavior)
        {
            return behavior == AiBehavior.GoToSettlement ||
                   behavior == AiBehavior.PatrolAroundPoint ||
                   behavior == AiBehavior.DefendSettlement;
        }

        private static bool HasAnyActiveRelevantThreat(MobileParty actor)
        {
            return actor != null &&
                WorldScopeContext.ThreatenedFactionSettlements(actor).Count > 0;
        }

        private static bool Eligible(MobileParty actor)
        {
            if (actor == null || actor.LeaderHero == null || actor.IsMainParty ||
                actor.Army != null || actor.MapFaction == null)
                return false;

            return WorldScopeContext.EligibleIndependentLordAtWar(actor);
        }

        private static bool FactionAtWar(IFaction faction)
        {
            if (faction == null)
                return false;

            foreach (Settlement settlement in Settlement.All)
            {
                if (settlement == null || settlement.MapFaction == null)
                    continue;
                if (ReferenceEquals(settlement.MapFaction, faction))
                    continue;
                if (faction.IsAtWarWith(settlement.MapFaction))
                    return true;
            }
            return false;
        }

        private static Settlement FindSettlement(string settlementId)
        {
            if (string.IsNullOrEmpty(settlementId))
                return null;

            foreach (Settlement settlement in Settlement.All)
            {
                if (settlement != null && string.Equals(
                        settlement.StringId,
                        settlementId,
                        StringComparison.Ordinal))
                    return settlement;
            }
            return null;
        }

        private static bool IsHomeDutyBehavior(AiBehavior behavior)
        {
            return behavior == AiBehavior.GoToSettlement ||
                   behavior == AiBehavior.PatrolAroundPoint ||
                   behavior == AiBehavior.DefendSettlement;
        }

        private static bool SameClan(Clan a, Clan b)
        {
            if (a == null || b == null)
                return false;
            if (ReferenceEquals(a, b))
                return true;
            return string.Equals(a.StringId, b.StringId, StringComparison.Ordinal);
        }

        private static bool SameFaction(IFaction a, IFaction b)
        {
            if (a == null || b == null)
                return false;
            if (ReferenceEquals(a, b))
                return true;
            return string.Equals(a.StringId, b.StringId, StringComparison.Ordinal);
        }

        private static string HeroKey(MobileParty actor)
        {
            if (actor == null || actor.LeaderHero == null)
                return null;

            return string.IsNullOrEmpty(actor.LeaderHero.StringId)
                ? actor.LeaderHero.Name.ToString()
                : actor.LeaderHero.StringId;
        }

        private static string CandidateLabel(PartyThinkParams thinkParams, int index)
        {
            if (thinkParams == null || index < 0 ||
                index >= thinkParams.AIBehaviorScores.Count)
                return "<none>";

            AIBehaviorData data = thinkParams.AIBehaviorScores[index].Item1;
            Settlement settlement = data.Party as Settlement;
            return data.AiBehavior + ":" +
                (settlement == null
                    ? (data.Party == null ? "<none>" : data.Party.ToString())
                    : settlement.Name.ToString());
        }

        private static int CountEntries()
        {
            int count = 0;
            foreach (var pair in ByHero)
                count += pair.Value.Count;
            return count;
        }

        private static string B64(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
        }

        private static string FromB64(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";
            try
            {
                return Encoding.UTF8.GetString(Convert.FromBase64String(value));
            }
            catch
            {
                return "";
            }
        }
    }
}
