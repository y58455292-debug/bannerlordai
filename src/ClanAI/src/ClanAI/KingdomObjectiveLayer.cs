using System;
using System.Collections.Generic;
using System.Globalization;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;

namespace ClanAI
{
    internal static class KingdomObjectiveLayer
    {
        private const float BesiegeFactor = 1.75f;
        private const float AssaultFactor = 1.65f;
        private const float GoToFactor = 1.50f;
        private const float PatrolFactor = 1.35f;
        private const float StagingGoToFactor = 1.55f;
        private const float StagingPatrolFactor = 1.40f;
        private const float StagingDefendFactor = 1.30f;
        private const float MaxDirectOrderFactor = 2.50f;
        private const float MaxStagingOrderFactor = 2.25f;
        private const float MinimumCompetitiveRatio = 0.45f;
        private const float WinnerMargin = 1.025f;
        private const float MinimumReadiness = 0.72f;
        private const int MinimumFoodDays = 3;
        private const double PendingLifetimeHours = 18.0;
        private const double InfoLogCooldownHours = 6.0;

        private sealed class PendingCommit
        {
            internal string ActorName;
            internal string PartyId;
            internal string RulerName;
            internal string KingdomName;
            internal string Reason;
            internal string Objective;
            internal string ObjectiveTargetName;
            internal string TargetSettlementId;
            internal string TargetName;
            internal string Mode;
            internal AiBehavior Behavior;
            internal string Label;
            internal double CreatedAtHours;
            internal int Attempts;
        }

        private static readonly Dictionary<string, PendingCommit> PendingByParty =
            new Dictionary<string, PendingCommit>(StringComparer.Ordinal);
        private static readonly Dictionary<string, double> LastInfoLogByParty =
            new Dictionary<string, double>(StringComparer.Ordinal);

        private static long _evaluations;
        private static long _acceptances;
        private static long _refusals;
        private static long _winnerChanges;
        private static long _commitChecks;
        private static long _commitMatches;

        internal static void Reset()
        {
            PendingByParty.Clear();
            LastInfoLogByParty.Clear();
            _evaluations = 0;
            _acceptances = 0;
            _refusals = 0;
            _winnerChanges = 0;
            _commitChecks = 0;
            _commitMatches = 0;
            ClanAIPostVanilla.WriteExternalLog(
                "KINGDOM_OBJECTIVE_LAYER_RESET scope=all-independent-kingdom-lords");
        }

        internal static void Apply(
            MobileParty actor,
            PartyThinkParams thinkParams,
            StrategicDecisionComposer.Frame composer)
        {
            VerifyPendingCommit(actor, "ai-think");

            if (!WorldScopeContext.EligibleIndependentLordAtWar(actor) ||
                thinkParams == null || composer == null ||
                actor.ActualClan == null || actor.ActualClan.Kingdom == null)
                return;

            WarStateTracker.ObjectiveView objective;
            if (!WarStateTracker.TryGetActiveObjectiveFor(
                    actor.ActualClan.Kingdom,
                    out objective) ||
                objective == null)
                return;

            _evaluations++;
            string partyId = actor.StringId ?? actor.LeaderHero.StringId;
            string actorName = actor.LeaderHero.Name.ToString();
            float readiness = actor.PartySizeRatio;
            int foodDays;
            try
            {
                foodDays = actor.GetNumDaysForFoodToLast();
            }
            catch
            {
                foodDays = -1;
            }

            if (readiness < MinimumReadiness ||
                (foodDays >= 0 && foodDays < MinimumFoodDays))
            {
                _refusals++;
                LogInfo(
                    partyId,
                    "KINGDOM_OBJECTIVE_REFUSED actor=" + actorName +
                    " partyId=" + partyId +
                    " kingdom=" + objective.KingdomName +
                    " ruler=" + objective.RulerName +
                    " reason=" + objective.Reason +
                    " objective=" + objective.Objective +
                    " target=" + TargetName(objective.Target) +
                    " readiness=" + F(readiness) +
                    " foodDays=" + foodDays +
                    " refusal=low-readiness-or-supplies");
                return;
            }

            if (WorldScopeContext.HasOtherUrgentClanThreat(
                    actor,
                    objective.Target == null ? null : objective.Target.StringId))
            {
                _refusals++;
                LogInfo(
                    partyId,
                    "KINGDOM_OBJECTIVE_REFUSED actor=" + actorName +
                    " partyId=" + partyId +
                    " kingdom=" + objective.KingdomName +
                    " ruler=" + objective.RulerName +
                    " reason=" + objective.Reason +
                    " objective=" + objective.Objective +
                    " target=" + TargetName(objective.Target) +
                    " readiness=" + F(readiness) +
                    " foodDays=" + foodDays +
                    " refusal=urgent-clan-home-threat");
                return;
            }

            if (!string.Equals(
                    objective.Objective,
                    "CaptureSpecificSettlement",
                    StringComparison.Ordinal) ||
                objective.Target == null)
            {
                LogInfo(
                    partyId,
                    "KINGDOM_OBJECTIVE_DEFERRED actor=" + actorName +
                    " partyId=" + partyId +
                    " kingdom=" + objective.KingdomName +
                    " ruler=" + objective.RulerName +
                    " reason=" + objective.Reason +
                    " objective=" + objective.Objective +
                    " target=" + TargetName(objective.Target) +
                    " defer=objective-type-not-yet-actionable-v1");
                return;
            }

            List<Settlement> staging = FindStagingSettlements(
                actor.ActualClan.Kingdom,
                objective.Target,
                3);
            var stageRank = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < staging.Count; i++)
                stageRank[staging[i].StringId] = i;

            int beforeIndex = composer.CurrentBestIndex(thinkParams);
            string before = CandidateLabel(thinkParams, beforeIndex);
            float beforeWinnerScore = beforeIndex >= 0
                ? composer.CurrentScore(
                    beforeIndex,
                    thinkParams.AIBehaviorScores[beforeIndex].Item2)
                : 0f;
            var applied = new Dictionary<int, float>();
            var appliedMode = new Dictionary<int, string>();
            var appliedSettlement = new Dictionary<int, Settlement>();
            int candidateCount = 0;
            int directCandidates = 0;
            int stagingCandidates = 0;
            int competitiveCandidates = 0;
            float bestCompetitiveRatio = 0f;
            float minimumRequiredFactor = float.MaxValue;
            string bestCompetitiveCandidate = "<none>";

            for (int i = 0; i < thinkParams.AIBehaviorScores.Count; i++)
            {
                AIBehaviorData data = thinkParams.AIBehaviorScores[i].Item1;
                Settlement settlement = data.Party as Settlement;
                if (settlement == null)
                    continue;

                bool direct = string.Equals(
                    settlement.StringId,
                    objective.Target.StringId,
                    StringComparison.Ordinal);
                int rank = 0;
                bool isStaging = !direct &&
                    stageRank.TryGetValue(settlement.StringId, out rank);
                if (!direct && !isStaging)
                    continue;

                float baseFactor = direct
                    ? FactorFor(data.AiBehavior)
                    : StagingFactorFor(data.AiBehavior, rank);
                if (baseFactor <= 1.001f)
                    continue;

                float raw = thinkParams.AIBehaviorScores[i].Item2;
                float baseScore = composer.CurrentScore(i, raw);
                if (baseScore <= 0f)
                    continue;

                float competitiveRatio = beforeWinnerScore > 0f
                    ? baseScore / beforeWinnerScore
                    : 1f;
                float requiredFactor = beforeWinnerScore > 0f
                    ? (beforeWinnerScore * WinnerMargin) / baseScore
                    : baseFactor;
                float maxFactor = direct
                    ? MaxDirectOrderFactor
                    : MaxStagingOrderFactor;
                float factor = baseFactor;

                if (competitiveRatio >= MinimumCompetitiveRatio)
                {
                    competitiveCandidates++;
                    if (competitiveRatio > bestCompetitiveRatio)
                    {
                        bestCompetitiveRatio = competitiveRatio;
                        bestCompetitiveCandidate =
                            data.AiBehavior + ":" + settlement.Name.ToString();
                    }
                    if (requiredFactor < minimumRequiredFactor)
                        minimumRequiredFactor = requiredFactor;

                    if (requiredFactor <= maxFactor)
                        factor = Math.Max(baseFactor, requiredFactor);
                }

                string mode = direct ? "direct" : "staging-" + (rank + 1);
                composer.ApplyFactor(
                    i,
                    "kingdom-objective",
                    baseScore,
                    factor,
                    "ruler-objective:" + objective.Reason + ":" + mode);
                applied[i] = factor;
                appliedMode[i] = mode;
                appliedSettlement[i] = settlement;
                candidateCount++;
                if (direct) directCandidates++; else stagingCandidates++;
            }

            if (candidateCount == 0)
            {
                LogInfo(
                    partyId,
                    "KINGDOM_OBJECTIVE_CONSIDER actor=" + actorName +
                    " partyId=" + partyId +
                    " kingdom=" + objective.KingdomName +
                    " ruler=" + objective.RulerName +
                    " reason=" + objective.Reason +
                    " objective=" + objective.Objective +
                    " target=" + TargetName(objective.Target) +
                    " staging=" + StagingNames(staging) +
                    " readiness=" + F(readiness) +
                    " foodDays=" + foodDays +
                    " result=no-native-direct-or-staging-candidate");
                return;
            }

            _acceptances++;
            int afterIndex = composer.CurrentBestIndex(thinkParams);
            string after = CandidateLabel(thinkParams, afterIndex);
            bool winnerChanged = afterIndex != beforeIndex;
            bool objectiveWon = applied.ContainsKey(afterIndex);
            string winningMode = objectiveWon ? appliedMode[afterIndex] : "none";
            Settlement winningSettlement = objectiveWon ? appliedSettlement[afterIndex] : null;

            ClanAIPostVanilla.WriteExternalLog(
                "KINGDOM_OBJECTIVE_ACCEPTED actor=" + actorName +
                " partyId=" + partyId +
                " kingdom=" + objective.KingdomName +
                " ruler=" + objective.RulerName +
                " enemy=" + objective.EnemyName +
                " reason=" + objective.Reason +
                " objective=" + objective.Objective +
                " objectiveTarget=" + TargetName(objective.Target) +
                " staging=" + StagingNames(staging) +
                " readiness=" + F(readiness) +
                " foodDays=" + foodDays +
                " objectiveCandidates=" + candidateCount +
                " directCandidates=" + directCandidates +
                " stagingCandidates=" + stagingCandidates +
                " competitiveCandidates=" + competitiveCandidates +
                " bestCompetitiveCandidate=" + bestCompetitiveCandidate +
                " bestCompetitiveRatio=" + bestCompetitiveRatio.ToString("0.###", CultureInfo.InvariantCulture) +
                " minimumRequiredFactor=" +
                    (minimumRequiredFactor == float.MaxValue
                        ? "<none>"
                        : minimumRequiredFactor.ToString("0.###", CultureInfo.InvariantCulture)) +
                " beforeWinnerScore=" + beforeWinnerScore.ToString("0.###", CultureInfo.InvariantCulture) +
                " before=" + before +
                " after=" + after +
                " winnerChanged=" + winnerChanged +
                " objectiveWon=" + objectiveWon +
                " winningMode=" + winningMode +
                " winningActionTarget=" + TargetName(winningSettlement) +
                " acceptances=" + _acceptances);

            if (!winnerChanged || !objectiveWon || winningSettlement == null)
                return;

            _winnerChanges++;
            AIBehaviorData winner = thinkParams.AIBehaviorScores[afterIndex].Item1;
            PendingByParty[partyId] = new PendingCommit
            {
                ActorName = actorName,
                PartyId = partyId,
                RulerName = objective.RulerName,
                KingdomName = objective.KingdomName,
                Reason = objective.Reason,
                Objective = objective.Objective,
                ObjectiveTargetName = objective.Target.Name.ToString(),
                TargetSettlementId = winningSettlement.StringId,
                TargetName = winningSettlement.Name.ToString(),
                Mode = winningMode,
                Behavior = winner.AiBehavior,
                Label = after,
                CreatedAtHours = CampaignTime.Now.ToHours,
                Attempts = 0
            };

            ClanAIPostVanilla.WriteExternalLog(
                "KINGDOM_OBJECTIVE_WINNER_CHANGE actor=" + actorName +
                " partyId=" + partyId +
                " kingdom=" + objective.KingdomName +
                " ruler=" + objective.RulerName +
                " reason=" + objective.Reason +
                " objective=" + objective.Objective +
                " objectiveTarget=" + objective.Target.Name +
                " actionTarget=" + winningSettlement.Name +
                " mode=" + winningMode +
                " before=" + before +
                " after=" + after +
                " factor=" + applied[afterIndex].ToString("0.###", CultureInfo.InvariantCulture) +
                " winnerChanges=" + _winnerChanges);
        }

        internal static void VerifyPendingCommitsFromWorld()
        {
            if (PendingByParty.Count == 0)
                return;

            var ids = new List<string>(PendingByParty.Keys);
            for (int i = 0; i < ids.Count; i++)
            {
                MobileParty actor = null;
                foreach (MobileParty party in MobileParty.AllLordParties)
                {
                    if (party != null &&
                        string.Equals(party.StringId, ids[i], StringComparison.Ordinal))
                    {
                        actor = party;
                        break;
                    }
                }
                if (actor != null)
                    VerifyPendingCommit(actor, "hourly-world");
            }
        }

        private static void VerifyPendingCommit(
            MobileParty actor,
            string source)
        {
            if (actor == null || string.IsNullOrEmpty(actor.StringId))
                return;

            PendingCommit pending;
            if (!PendingByParty.TryGetValue(actor.StringId, out pending))
                return;

            _commitChecks++;
            pending.Attempts++;

            Settlement target = actor.TargetSettlement ??
                actor.ShortTermTargetSettlement ??
                actor.BesiegedSettlement ??
                actor.CurrentSettlement;

            bool behaviorMatch =
                actor.DefaultBehavior == pending.Behavior ||
                actor.ShortTermBehavior == pending.Behavior;
            bool targetMatch = target != null &&
                string.Equals(
                    target.StringId,
                    pending.TargetSettlementId,
                    StringComparison.Ordinal);
            bool arrivedMatch = actor.CurrentSettlement != null &&
                string.Equals(
                    actor.CurrentSettlement.StringId,
                    pending.TargetSettlementId,
                    StringComparison.Ordinal);
            bool matched = (behaviorMatch && targetMatch) || arrivedMatch;

            double ageHours = CampaignTime.Now.ToHours - pending.CreatedAtHours;
            if (ageHours < 0.0)
                ageHours = 0.0;
            bool expired = ageHours > PendingLifetimeHours;

            if (matched)
            {
                _commitMatches++;
                PendingByParty.Remove(actor.StringId);
            }
            else if (expired)
            {
                PendingByParty.Remove(actor.StringId);
            }

            ClanAIPostVanilla.WriteExternalLog(
                "KINGDOM_OBJECTIVE_COMMIT_CHECK actor=" + pending.ActorName +
                " partyId=" + pending.PartyId +
                " kingdom=" + pending.KingdomName +
                " ruler=" + pending.RulerName +
                " reason=" + pending.Reason +
                " objective=" + pending.Objective +
                " objectiveTarget=" + pending.ObjectiveTargetName +
                " actionTarget=" + pending.TargetName +
                " mode=" + pending.Mode +
                " expected=" + pending.Label +
                " actualDefault=" + actor.DefaultBehavior +
                " actualShort=" + actor.ShortTermBehavior +
                " actualTarget=" + (target == null ? "<none>" : target.Name.ToString()) +
                " arrivedMatch=" + arrivedMatch +
                " matched=" + matched +
                " expired=" + expired +
                " ageHours=" + ageHours.ToString("0.###", CultureInfo.InvariantCulture) +
                " attempts=" + pending.Attempts +
                " source=" + source +
                " checks=" + _commitChecks +
                " matches=" + _commitMatches);
        }

        private static float FactorFor(AiBehavior behavior)
        {
            if (behavior == AiBehavior.BesiegeSettlement)
                return BesiegeFactor;
            if (behavior == AiBehavior.AssaultSettlement)
                return AssaultFactor;
            if (behavior == AiBehavior.GoToSettlement)
                return GoToFactor;
            if (behavior == AiBehavior.PatrolAroundPoint)
                return PatrolFactor;
            return 1f;
        }

        private static float StagingFactorFor(
            AiBehavior behavior,
            int rank)
        {
            float factor;
            if (behavior == AiBehavior.GoToSettlement)
                factor = StagingGoToFactor;
            else if (behavior == AiBehavior.PatrolAroundPoint)
                factor = StagingPatrolFactor;
            else if (behavior == AiBehavior.DefendSettlement)
                factor = StagingDefendFactor;
            else
                return 1f;

            float rankPenalty = Math.Max(0, rank) * 0.10f;
            return Math.Max(1.10f, factor - rankPenalty);
        }

        private static List<Settlement> FindStagingSettlements(
            Kingdom kingdom,
            Settlement target,
            int maxCount)
        {
            var result = new List<Settlement>();
            if (kingdom == null || target == null || maxCount <= 0)
                return result;

            Vec2 targetPos = target.GetPosition2D;
            var candidates = new List<Tuple<Settlement, float>>();

            for (int i = 0; i < kingdom.Settlements.Count; i++)
            {
                Settlement settlement = kingdom.Settlements[i];
                if (settlement == null || settlement.IsVillage ||
                    string.IsNullOrEmpty(settlement.StringId) ||
                    string.Equals(
                        settlement.StringId,
                        target.StringId,
                        StringComparison.Ordinal))
                    continue;

                Vec2 p = settlement.GetPosition2D;
                float dx = p.x - targetPos.x;
                float dy = p.y - targetPos.y;
                float distanceSquared = dx * dx + dy * dy;
                candidates.Add(Tuple.Create(settlement, distanceSquared));
            }

            candidates.Sort((a, b) =>
            {
                int distance = a.Item2.CompareTo(b.Item2);
                if (distance != 0)
                    return distance;
                return string.CompareOrdinal(
                    a.Item1.StringId,
                    b.Item1.StringId);
            });

            for (int i = 0; i < candidates.Count && result.Count < maxCount; i++)
                result.Add(candidates[i].Item1);

            return result;
        }

        private static string StagingNames(List<Settlement> staging)
        {
            if (staging == null || staging.Count == 0)
                return "<none>";

            var names = new List<string>();
            for (int i = 0; i < staging.Count; i++)
            {
                if (staging[i] != null)
                    names.Add(staging[i].Name.ToString());
            }
            return names.Count == 0 ? "<none>" : string.Join(",", names.ToArray());
        }

        private static void LogInfo(string partyId, string text)
        {
            double now = CampaignTime.Now.ToHours;
            double last;
            if (LastInfoLogByParty.TryGetValue(partyId, out last) &&
                (now - last) < InfoLogCooldownHours)
                return;
            LastInfoLogByParty[partyId] = now;
            ClanAIPostVanilla.WriteExternalLog(text);
        }

        private static string TargetName(Settlement settlement)
        {
            return settlement == null ? "<none>" : settlement.Name.ToString();
        }

        private static string CandidateLabel(
            PartyThinkParams thinkParams,
            int index)
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

        private static string F(float value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }
    }
}
