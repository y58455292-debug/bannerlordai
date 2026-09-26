using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace ClanAI
{
    internal static class CompanionDutyMemory
    {
        private const double MemoryLifetimeHours = 72.0;
        private const double ReinforceCooldownHours = 6.0;
        private const float BaseRecallBonus = 0.12f;
        private const float MaxExperienceBonus = 0.08f;

        private sealed class Record
        {
            internal string HeroKey;
            internal string ActorName;
            internal string SettlementId;
            internal string SettlementName;
            internal AiBehavior Behavior;
            internal double LearnedAtHours;
            internal double LastReinforcedHours;
            internal int SuccessfulCommits;
            internal int RecallUses;
        }

        private sealed class PendingCommit
        {
            internal string ActorName;
            internal AiBehavior Behavior;
            internal string SettlementId;
            internal string Label;
        }

        private static readonly Dictionary<string, Record> Records =
            new Dictionary<string, Record>(StringComparer.Ordinal);
        private static readonly Dictionary<string, PendingCommit> PendingByParty =
            new Dictionary<string, PendingCommit>(StringComparer.Ordinal);

        private static bool _loadedFromSave;
        private static long _recallEvaluations;
        private static long _recallApplications;
        private static long _recallWinnerChanges;
        private static long _commitChecks;
        private static long _commitMatches;

        internal static void BeginSession()
        {
            if (!_loadedFromSave)
                Records.Clear();

            PendingByParty.Clear();
            _recallEvaluations = 0;
            _recallApplications = 0;
            _recallWinnerChanges = 0;
            _commitChecks = 0;
            _commitMatches = 0;

            ClanAIPostVanilla.WriteExternalLog(
                "COMPANION_MEMORY_SESSION_READY records=" +
                Records.Count +
                " restored=" + _loadedFromSave +
                " lifetimeHours=" + MemoryLifetimeHours);

            _loadedFromSave = false;
        }

        internal static void SyncData(IDataStore dataStore)
        {
            List<string> lines = new List<string>();

            if (dataStore.IsSaving)
                lines = ExportSaveLines();

            bool found = dataStore.SyncData(
                "ClanAI_CompanionDutyMemory_v1",
                ref lines);

            if (dataStore.IsLoading)
                ImportSaveLines(found ? lines : null);
        }

        private static List<string> ExportSaveLines()
        {
            var lines = new List<string>();
            foreach (var pair in Records)
            {
                Record r = pair.Value;
                lines.Add(
                    B64(r.HeroKey) + "|" +
                    B64(r.ActorName) + "|" +
                    B64(r.SettlementId) + "|" +
                    B64(r.SettlementName) + "|" +
                    ((int)r.Behavior).ToString(CultureInfo.InvariantCulture) + "|" +
                    r.LearnedAtHours.ToString("R", CultureInfo.InvariantCulture) + "|" +
                    r.LastReinforcedHours.ToString("R", CultureInfo.InvariantCulture) + "|" +
                    r.SuccessfulCommits.ToString(CultureInfo.InvariantCulture) + "|" +
                    r.RecallUses.ToString(CultureInfo.InvariantCulture));
            }

            ClanAIPostVanilla.WriteExternalLog(
                "COMPANION_MEMORY_SAVE records=" + lines.Count);

            return lines;
        }

        private static void ImportSaveLines(List<string> lines)
        {
            Records.Clear();

            if (lines != null)
            {
                foreach (string line in lines)
                {
                    if (string.IsNullOrEmpty(line))
                        continue;
                    string[] p = line.Split('|');
                    if (p.Length != 9)
                        continue;

                    int behaviorValue;
                    double learned;
                    double reinforced;
                    int commits;
                    int recalls;

                    if (!int.TryParse(p[4], NumberStyles.Integer,
                            CultureInfo.InvariantCulture, out behaviorValue) ||
                        !double.TryParse(p[5], NumberStyles.Float,
                            CultureInfo.InvariantCulture, out learned) ||
                        !double.TryParse(p[6], NumberStyles.Float,
                            CultureInfo.InvariantCulture, out reinforced) ||
                        !int.TryParse(p[7], NumberStyles.Integer,
                            CultureInfo.InvariantCulture, out commits) ||
                        !int.TryParse(p[8], NumberStyles.Integer,
                            CultureInfo.InvariantCulture, out recalls))
                        continue;

                    string heroKey = FromB64(p[0]);
                    string settlementId = FromB64(p[2]);
                    if (string.IsNullOrEmpty(heroKey) ||
                        string.IsNullOrEmpty(settlementId))
                        continue;
                    Records[heroKey] = new Record
                    {
                        HeroKey = heroKey,
                        ActorName = FromB64(p[1]),
                        SettlementId = settlementId,
                        SettlementName = FromB64(p[3]),
                        Behavior = (AiBehavior)behaviorValue,
                        LearnedAtHours = learned,
                        LastReinforcedHours = reinforced,
                        SuccessfulCommits = commits,
                        RecallUses = recalls
                    };
                }
            }

            _loadedFromSave = true;
            ClanAIPostVanilla.WriteExternalLog(
                "COMPANION_MEMORY_LOAD records=" + Records.Count);
        }

        internal static void Apply(
            MobileParty actor,
            PartyThinkParams thinkParams,
            StrategicDecisionComposer.Frame composer)
        {
            VerifyPendingCommit(actor);

            if (!Eligible(actor) || thinkParams == null || composer == null)
                return;
            string heroKey = HeroKey(actor);
            Record record;
            if (string.IsNullOrEmpty(heroKey) ||
                !Records.TryGetValue(heroKey, out record))
                return;

            double nowHours = CampaignTime.Now.ToHours;
            double ageHours = nowHours - record.LastReinforcedHours;
            if (ageHours < 0.0)
                ageHours = 0.0;

            if (ageHours > MemoryLifetimeHours)
            {
                ClanAIPostVanilla.WriteExternalLog(
                    "COMPANION_MEMORY_EXPIRED actor=" + record.ActorName +
                    " settlement=" + record.SettlementName +
                    " ageHours=" + F(ageHours));
                Records.Remove(heroKey);
                return;
            }

            Settlement remembered = FindSettlement(record.SettlementId);
            Clan actorClan = actor.ActualClan;
            if (remembered == null || !SameClan(remembered.OwnerClan, actorClan))
            {
                ClanAIPostVanilla.WriteExternalLog(
                    "COMPANION_MEMORY_FORGOT actor=" + record.ActorName +
                    " settlementId=" + record.SettlementId +
                    " reason=holding-no-longer-actor-clan");
                Records.Remove(heroKey);
                return;
            }
            if (WorldScopeContext.HasOtherUrgentClanThreat(actor, record.SettlementId))
            {
                if ((_recallEvaluations % 50) == 0)
                {
                    ClanAIPostVanilla.WriteExternalLog(
                        "COMPANION_MEMORY_YIELD actor=" + record.ActorName +
                        " remembered=" + record.SettlementName +
                        " reason=other-actor-clan-holding-under-attack");
                }
                return;
            }

            _recallEvaluations++;
            int beforeIndex = composer.CurrentBestIndex(thinkParams);
            string before = CandidateLabel(thinkParams, beforeIndex);

            double freshness = 1.0 - (ageHours / MemoryLifetimeHours);
            if (freshness < 0.0) freshness = 0.0;
            if (freshness > 1.0) freshness = 1.0;

            float experienceBonus =
                Math.Min(MaxExperienceBonus,
                    Math.Max(0, record.SuccessfulCommits - 1) * 0.02f);
            float recallBonus =
                (BaseRecallBonus + experienceBonus) * (float)freshness;
            float factor = 1f + recallBonus;

            int applied = 0;
            for (int i = 0; i < thinkParams.AIBehaviorScores.Count; i++)
            {
                AIBehaviorData data = thinkParams.AIBehaviorScores[i].Item1;
                Settlement settlement = data.Party as Settlement;
                if (settlement == null ||
                    !string.Equals(settlement.StringId, record.SettlementId,
                        StringComparison.Ordinal) ||
                    !IsHomeDutyBehavior(data.AiBehavior))
                    continue;
                float rawScore = thinkParams.AIBehaviorScores[i].Item2;
                float baseScore = composer.CurrentScore(i, rawScore);
                if (baseScore <= 0f)
                    continue;

                composer.ApplyFactor(
                    i,
                    "companion-duty-memory",
                    baseScore,
                    factor,
                    "remembered-home-duty");
                applied++;
            }

            if (applied == 0)
                return;

            GenerationalContinuityRuntimeTelemetry
                .ObserveHeroMemoryResolution(
                    "CompanionDutyMemory",
                    heroKey,
                    record.HeroKey,
                    true);

            _recallApplications++;
            record.RecallUses++;

            int afterIndex = composer.CurrentBestIndex(thinkParams);
            string after = CandidateLabel(thinkParams, afterIndex);

            ClanAIPostVanilla.WriteExternalLog(
                "COMPANION_MEMORY_RECALL actor=" + record.ActorName +
                " remembered=" + record.SettlementName +
                " rememberedBehavior=" + record.Behavior +
                " ageHours=" + F(ageHours) +
                " successfulCommits=" + record.SuccessfulCommits +
                " recallUses=" + record.RecallUses +
                " factor=" + factor.ToString("0.###", CultureInfo.InvariantCulture) +
                " candidates=" + applied +
                " before=" + before +
                " after=" + after);
            if (afterIndex == beforeIndex)
                return;

            _recallWinnerChanges++;
            AIBehaviorData winner = thinkParams.AIBehaviorScores[afterIndex].Item1;
            Settlement winnerSettlement = winner.Party as Settlement;
            string partyId = actor.StringId ?? record.HeroKey;

            PendingByParty[partyId] = new PendingCommit
            {
                ActorName = record.ActorName,
                Behavior = winner.AiBehavior,
                SettlementId = winnerSettlement == null ? null : winnerSettlement.StringId,
                Label = after
            };

            ClanAIPostVanilla.WriteExternalLog(
                "COMPANION_MEMORY_RECALL_WINNER_CHANGE actor=" + record.ActorName +
                " partyId=" + partyId +
                " remembered=" + record.SettlementName +
                " before=" + before +
                " after=" + after +
                " factor=" + factor.ToString("0.###", CultureInfo.InvariantCulture) +
                " ageHours=" + F(ageHours) +
                " recallWinnerChanges=" + _recallWinnerChanges);
        }

        internal static void RecordSuccessfulHomeDuty(
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

            CompanionExperienceMemory.RecordSuccessfulDuty(
                actor,
                behavior,
                settlement,
                source);

            double nowHours = CampaignTime.Now.ToHours;
            Record record;
            bool exists = Records.TryGetValue(heroKey, out record);

            if (!exists || !string.Equals(
                    record.SettlementId,
                    settlement.StringId,
                    StringComparison.Ordinal))
            {
                record = new Record
                {
                    HeroKey = heroKey,
                    ActorName = actor.LeaderHero.Name.ToString(),
                    SettlementId = settlement.StringId,
                    SettlementName = settlement.Name.ToString(),
                    Behavior = behavior,
                    LearnedAtHours = nowHours,
                    LastReinforcedHours = nowHours,
                    SuccessfulCommits = 1,
                    RecallUses = 0
                };
                Records[heroKey] = record;

                ClanAIPostVanilla.WriteExternalLog(
                    "COMPANION_MEMORY_LEARNED actor=" + record.ActorName +
                    " settlement=" + record.SettlementName +
                    " behavior=" + record.Behavior +
                    " source=" + (source ?? "<none>") +
                    " learnedAtHours=" + F(nowHours));
                return;
            }

            record.ActorName = actor.LeaderHero.Name.ToString();
            record.Behavior = behavior;
            record.SettlementName = settlement.Name.ToString();

            if ((nowHours - record.LastReinforcedHours) >= ReinforceCooldownHours)
            {
                record.LastReinforcedHours = nowHours;
                record.SuccessfulCommits++;

                ClanAIPostVanilla.WriteExternalLog(
                    "COMPANION_MEMORY_REINFORCED actor=" + record.ActorName +
                    " settlement=" + record.SettlementName +
                    " behavior=" + record.Behavior +
                    " source=" + (source ?? "<none>") +
                    " successfulCommits=" + record.SuccessfulCommits +
                    " reinforcedAtHours=" + F(nowHours));
            }
        }

        internal static string GetRememberedSettlementId(MobileParty actor)
        {
            string heroKey = HeroKey(actor);
            if (string.IsNullOrEmpty(heroKey))
                return null;

            Record record;
            return Records.TryGetValue(heroKey, out record)
                ? record.SettlementId
                : null;
        }

        private static void VerifyPendingCommit(MobileParty actor)
        {
            if (actor == null)
                return;

            string partyId = actor.StringId;
            if (string.IsNullOrEmpty(partyId))
                return;

            PendingCommit pending;
            if (!PendingByParty.TryGetValue(partyId, out pending))
                return;

            PendingByParty.Remove(partyId);
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
                RecordSuccessfulHomeDuty(
                    actor,
                    pending.Behavior,
                    target,
                    "memory-recall-commit");
            }

            ClanAIPostVanilla.WriteExternalLog(
                "COMPANION_MEMORY_COMMIT_CHECK actor=" + pending.ActorName +
                " partyId=" + partyId +
                " expected=" + (pending.Label ?? "<none>") +
                " actualDefault=" + actor.DefaultBehavior +
                " actualShort=" + actor.ShortTermBehavior +
                " actualTarget=" + (target == null ? "<none>" : target.Name.ToString()) +
                " matched=" + matched +
                " checks=" + _commitChecks +
                " matches=" + _commitMatches);
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

        private static bool HasOtherUrgentHomeThreat(
            Clan playerClan,
            string rememberedSettlementId)
        {
            if (playerClan == null)
                return false;

            foreach (Settlement settlement in Settlement.All)
            {
                if (settlement == null || !SameClan(settlement.OwnerClan, playerClan))
                    continue;
                if (!settlement.IsUnderSiege && !settlement.IsUnderRaid)
                    continue;

                if (string.Equals(
                        settlement.StringId,
                        rememberedSettlementId,
                        StringComparison.Ordinal))
                    continue;

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

        private static string F(double value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static string B64(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            return Convert.ToBase64String(
                Encoding.UTF8.GetBytes(value));
        }

        private static string FromB64(string value)
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
    }
}