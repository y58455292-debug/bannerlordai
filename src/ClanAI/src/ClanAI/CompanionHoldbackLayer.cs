using System;
using System.Globalization;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace ClanAI
{
    internal static class CompanionHoldbackLayer
    {
        private const float ReadinessGate = 0.78f;
        private const int FoodDaysGate = 4;
        private const float BaseOffenseFactor = 0.45f;
        private const float ExposureReadinessGate = 0.70f;
        private const float ExposureFactor = 0.35f;

        private sealed class PendingCommit
        {
            internal string ActorName;
            internal AiBehavior Behavior;
            internal string SettlementId;
            internal string Label;
            internal string RememberedThreat;
        }

        private sealed class PendingOutcomeCommit
        {
            internal string ActorName;
            internal AiBehavior Behavior;
            internal string SettlementId;
            internal string Label;
            internal string OutcomeKind;
            internal string OutcomeContext;
            internal double CreatedAtHours;
            internal int Attempts;
        }

        private static readonly System.Collections.Generic.Dictionary<string, PendingCommit> PendingByParty =
            new System.Collections.Generic.Dictionary<string, PendingCommit>(StringComparer.Ordinal);
        private static readonly System.Collections.Generic.Dictionary<string, PendingOutcomeCommit> PendingOutcomeByParty =
            new System.Collections.Generic.Dictionary<string, PendingOutcomeCommit>(StringComparer.Ordinal);

        private static long _evaluations;
        private static long _memoryLowReadinessChecks;
        private static long _applications;
        private static long _winnerChanges;
        private static long _commitChecks;
        private static long _commitMatches;
        private static long _outcomeApplications;
        private static long _outcomeWinnerChanges;
        private static long _outcomeCommitChecks;
        private static long _outcomeCommitMatches;

        internal static void Reset()
        {
            PendingByParty.Clear();
            PendingOutcomeByParty.Clear();
            _evaluations = 0;
            _memoryLowReadinessChecks = 0;
            _applications = 0;
            _winnerChanges = 0;
            _commitChecks = 0;
            _commitMatches = 0;
            _outcomeApplications = 0;
            _outcomeWinnerChanges = 0;
            _outcomeCommitChecks = 0;
            _outcomeCommitMatches = 0;
        }

        internal static void Apply(
            MobileParty actor,
            PartyThinkParams thinkParams,
            StrategicDecisionComposer.Frame composer)
        {
            VerifyPendingCommit(actor);

            if (!Eligible(actor) || thinkParams == null || composer == null)
                return;

            string threatSettlement;
            string threatType;
            double threatAgeHours;
            int threatCount;
            bool threatActive;
            if (!CompanionExperienceMemory.TryGetRecentThreatForHoldback(
                    actor,
                    out threatSettlement,
                    out threatType,
                    out threatAgeHours,
                    out threatCount,
                    out threatActive))
                return;

            float readiness = actor.PartySizeRatio;
            int foodDays = actor.GetNumDaysForFoodToLast();
            if (readiness >= ReadinessGate && foodDays >= FoodDaysGate)
                return;

            int beforeIndex = composer.CurrentBestIndex(thinkParams);
            if (beforeIndex < 0 || beforeIndex >= thinkParams.AIBehaviorScores.Count)
                return;

            _memoryLowReadinessChecks++;
            AIBehaviorData beforeData = thinkParams.AIBehaviorScores[beforeIndex].Item1;
            string before = CandidateLabel(thinkParams, beforeIndex);
            bool beforeOffensive = IsOffensive(beforeData.AiBehavior);

            if (_memoryLowReadinessChecks <= 5 ||
                beforeOffensive ||
                (_memoryLowReadinessChecks % 25) == 0)
            {
                ClanAIPostVanilla.WriteExternalLog(
                    "HOLDBACK_ELIGIBILITY actor=" + actor.LeaderHero.Name.ToString() +
                    " readiness=" + readiness.ToString("0.###", CultureInfo.InvariantCulture) +
                    " foodDays=" + foodDays +
                    " rememberedThreat=" + threatSettlement +
                    " threatAgeHours=" + threatAgeHours.ToString("0.###", CultureInfo.InvariantCulture) +
                    " threatActive=" + threatActive +
                    " before=" + before +
                    " beforeOffensive=" + beforeOffensive +
                    " lowReadinessChecks=" + _memoryLowReadinessChecks);
            }

            if (!beforeOffensive)
                return;

            _evaluations++;

            float factor = BaseOffenseFactor;
            if (readiness < 0.50f)
                factor = 0.35f;
            if (foodDays < 2)
                factor = Math.Min(factor, 0.30f);

            int cuts = 0;
            for (int i = 0; i < thinkParams.AIBehaviorScores.Count; i++)
            {
                AIBehaviorData data = thinkParams.AIBehaviorScores[i].Item1;
                if (!IsOffensive(data.AiBehavior))
                    continue;

                float rawScore = thinkParams.AIBehaviorScores[i].Item2;
                float baseScore = composer.CurrentScore(i, rawScore);
                if (baseScore <= 0f)
                    continue;

                composer.ApplyFactor(
                    i,
                    "companion-holdback",
                    baseScore,
                    factor,
                    "remembered-home-threat-low-readiness");
                cuts++;
            }

            if (cuts == 0)
                return;

            _applications++;
            int afterIndex = composer.CurrentBestIndex(thinkParams);
            string after = CandidateLabel(thinkParams, afterIndex);
            bool winnerChanged = afterIndex != beforeIndex;
            bool heldBack = winnerChanged &&
                afterIndex >= 0 &&
                afterIndex < thinkParams.AIBehaviorScores.Count &&
                !IsOffensive(thinkParams.AIBehaviorScores[afterIndex].Item1.AiBehavior);

            ClanAIPostVanilla.WriteExternalLog(
                "HOLDBACK_APPLIED actor=" + actor.LeaderHero.Name.ToString() +
                " readiness=" + readiness.ToString("0.###", CultureInfo.InvariantCulture) +
                " foodDays=" + foodDays +
                " rememberedThreat=" + threatSettlement +
                " threatType=" + (threatType ?? "<none>") +
                " threatAgeHours=" + threatAgeHours.ToString("0.###", CultureInfo.InvariantCulture) +
                " threatCount=" + threatCount +
                " threatActive=" + threatActive +
                " offenseFactor=" + factor.ToString("0.###", CultureInfo.InvariantCulture) +
                " offenseCuts=" + cuts +
                " before=" + before +
                " after=" + after +
                " heldBack=" + heldBack);

            if (!heldBack)
                return;

            _winnerChanges++;
            AIBehaviorData winner = thinkParams.AIBehaviorScores[afterIndex].Item1;
            Settlement target = winner.Party as Settlement;
            string partyId = actor.StringId ?? actor.LeaderHero.StringId;

            PendingByParty[partyId] = new PendingCommit
            {
                ActorName = actor.LeaderHero.Name.ToString(),
                Behavior = winner.AiBehavior,
                SettlementId = target == null ? null : target.StringId,
                Label = after,
                RememberedThreat = threatSettlement
            };

            ClanAIPostVanilla.WriteExternalLog(
                "HOLDBACK_WINNER_CHANGE actor=" + actor.LeaderHero.Name.ToString() +
                " partyId=" + partyId +
                " before=" + before +
                " after=" + after +
                " readiness=" + readiness.ToString("0.###", CultureInfo.InvariantCulture) +
                " foodDays=" + foodDays +
                " rememberedThreat=" + threatSettlement +
                " threatAgeHours=" + threatAgeHours.ToString("0.###", CultureInfo.InvariantCulture) +
                " threatActive=" + threatActive +
                " reason=remembered-home-threat-low-readiness" +
                " winnerChanges=" + _winnerChanges);
        }

        internal static void ApplyOutcomeSafety(
            MobileParty actor,
            PartyThinkParams thinkParams,
            StrategicDecisionComposer.Frame composer)
        {
            VerifyOutcomePendingCommit(actor);

            if (!Eligible(actor) || thinkParams == null || composer == null)
                return;

            string outcomeKind;
            string outcomeContext;
            double outcomeAgeHours;
            int outcomeCount;
            float outcomeSeverity;
            AiBehavior priorBehavior;
            if (!CompanionNegativeOutcomeMemory.TryGetRecentOutcome(
                    actor,
                    out outcomeKind,
                    out outcomeContext,
                    out outcomeAgeHours,
                    out outcomeCount,
                    out outcomeSeverity,
                    out priorBehavior))
                return;

            float readiness = actor.PartySizeRatio;
            int foodDays = actor.GetNumDaysForFoodToLast();
            if (readiness >= 0.88f && foodDays >= FoodDaysGate)
                return;

            int beforeIndex = composer.CurrentBestIndex(thinkParams);
            if (beforeIndex < 0 || beforeIndex >= thinkParams.AIBehaviorScores.Count)
                return;
            AIBehaviorData beforeData = thinkParams.AIBehaviorScores[beforeIndex].Item1;
            if (!IsRecklessExposure(beforeData.AiBehavior))
                return;

            string before = CandidateLabel(thinkParams, beforeIndex);
            float factor = 0.50f;
            if (string.Equals(outcomeKind, "capture", StringComparison.Ordinal))
                factor = 0.35f;
            else if (outcomeSeverity >= 0.20f)
                factor = 0.38f;
            else if (outcomeSeverity >= 0.12f)
                factor = 0.45f;
            if (readiness < 0.60f)
                factor = Math.Min(factor, 0.32f);
            if (foodDays < 2)
                factor = Math.Min(factor, 0.30f);

            int cuts = 0;
            for (int i = 0; i < thinkParams.AIBehaviorScores.Count; i++)
            {
                AIBehaviorData data = thinkParams.AIBehaviorScores[i].Item1;
                if (!IsRecklessExposure(data.AiBehavior))
                    continue;

                float rawScore = thinkParams.AIBehaviorScores[i].Item2;
                float baseScore = composer.CurrentScore(i, rawScore);
                if (baseScore <= 0f)
                    continue;

                composer.ApplyFactor(
                    i,
                    "companion-outcome-holdback",
                    baseScore,
                    factor,
                    "remembered-negative-outcome");
                cuts++;
            }

            if (cuts == 0)
                return;
            _outcomeApplications++;
            int afterIndex = composer.CurrentBestIndex(thinkParams);
            if (afterIndex < 0 || afterIndex >= thinkParams.AIBehaviorScores.Count)
                return;

            string after = CandidateLabel(thinkParams, afterIndex);
            bool heldBack = afterIndex != beforeIndex &&
                !IsRecklessExposure(thinkParams.AIBehaviorScores[afterIndex].Item1.AiBehavior);

            ClanAIPostVanilla.WriteExternalLog(
                "OUTCOME_HOLDBACK_APPLIED actor=" + actor.LeaderHero.Name.ToString() +
                " readiness=" + readiness.ToString("0.###", CultureInfo.InvariantCulture) +
                " foodDays=" + foodDays +
                " outcomeKind=" + (outcomeKind ?? "<none>") +
                " outcomeAgeHours=" + outcomeAgeHours.ToString("0.###", CultureInfo.InvariantCulture) +
                " outcomeCount=" + outcomeCount +
                " outcomeSeverity=" + outcomeSeverity.ToString("0.###", CultureInfo.InvariantCulture) +
                " priorBehavior=" + priorBehavior +
                " factor=" + factor.ToString("0.###", CultureInfo.InvariantCulture) +
                " cuts=" + cuts +
                " before=" + before +
                " after=" + after +
                " heldBack=" + heldBack);

            if (!heldBack)
                return;

            _outcomeWinnerChanges++;
            AIBehaviorData winner = thinkParams.AIBehaviorScores[afterIndex].Item1;
            Settlement target = winner.Party as Settlement;
            string partyId = actor.StringId ?? actor.LeaderHero.StringId;
            PendingOutcomeByParty[partyId] = new PendingOutcomeCommit
            {
                ActorName = actor.LeaderHero.Name.ToString(),
                Behavior = winner.AiBehavior,
                SettlementId = target == null ? null : target.StringId,
                Label = after,
                OutcomeKind = outcomeKind,
                OutcomeContext = outcomeContext,
                CreatedAtHours = CampaignTime.Now.ToHours,
                Attempts = 0
            };
            ClanAIPostVanilla.WriteExternalLog(
                "OUTCOME_HOLDBACK_WINNER_CHANGE actor=" + actor.LeaderHero.Name.ToString() +
                " partyId=" + partyId +
                " before=" + before +
                " after=" + after +
                " readiness=" + readiness.ToString("0.###", CultureInfo.InvariantCulture) +
                " outcomeKind=" + (outcomeKind ?? "<none>") +
                " outcomeAgeHours=" + outcomeAgeHours.ToString("0.###", CultureInfo.InvariantCulture) +
                " outcomeSeverity=" + outcomeSeverity.ToString("0.###", CultureInfo.InvariantCulture) +
                " reason=remembered-negative-outcome" +
                " outcomeWinnerChanges=" + _outcomeWinnerChanges);
        }

        private static void VerifyOutcomePendingCommit(MobileParty actor)
        {
            VerifyOutcomePendingCommit(actor, "ai-think");
        }

        private static void VerifyOutcomePendingCommit(
            MobileParty actor,
            string source)
        {
            if (actor == null || string.IsNullOrEmpty(actor.StringId))
                return;

            PendingOutcomeCommit pending;
            if (!PendingOutcomeByParty.TryGetValue(actor.StringId, out pending))
                return;

            _outcomeCommitChecks++;
            pending.Attempts++;

            Settlement target = actor.TargetSettlement ?? actor.ShortTermTargetSettlement;
            Settlement current = actor.CurrentSettlement;
            bool behaviorMatch =
                actor.DefaultBehavior == pending.Behavior ||
                actor.ShortTermBehavior == pending.Behavior;
            bool targetMatch =
                string.IsNullOrEmpty(pending.SettlementId) ||
                (target != null && string.Equals(
                    target.StringId,
                    pending.SettlementId,
                    StringComparison.Ordinal));
            bool arrivedMatch =
                pending.Behavior == AiBehavior.GoToSettlement &&
                !string.IsNullOrEmpty(pending.SettlementId) &&
                current != null &&
                string.Equals(
                    current.StringId,
                    pending.SettlementId,
                    StringComparison.Ordinal);
            bool matched = (behaviorMatch && targetMatch) || arrivedMatch;

            double ageHours = CampaignTime.Now.ToHours - pending.CreatedAtHours;
            if (ageHours < 0.0)
                ageHours = 0.0;
            bool expired = ageHours > 18.0;

            if (matched)
            {
                _outcomeCommitMatches++;
                PendingOutcomeByParty.Remove(actor.StringId);
            }
            else if (expired)
            {
                PendingOutcomeByParty.Remove(actor.StringId);
            }

            ClanAIPostVanilla.WriteExternalLog(
                "OUTCOME_HOLDBACK_COMMIT_CHECK actor=" + pending.ActorName +
                " partyId=" + actor.StringId +
                " expected=" + (pending.Label ?? "<none>") +
                " outcomeKind=" + (pending.OutcomeKind ?? "<none>") +
                " outcomeContext=" + (pending.OutcomeContext ?? "<none>") +
                " actualDefault=" + actor.DefaultBehavior +
                " actualShort=" + actor.ShortTermBehavior +
                " actualTarget=" + (target == null ? "<none>" : target.Name.ToString()) +
                " actualCurrent=" + (current == null ? "<none>" : current.Name.ToString()) +
                " arrivedMatch=" + arrivedMatch +
                " matched=" + matched +
                " expired=" + expired +
                " ageHours=" + ageHours.ToString("0.###", CultureInfo.InvariantCulture) +
                " attempts=" + pending.Attempts +
                " source=" + (source ?? "<none>") +
                " checks=" + _outcomeCommitChecks +
                " matches=" + _outcomeCommitMatches);
        }

        internal static void VerifyOutcomePendingCommitsFromWorld()
        {
            if (PendingOutcomeByParty.Count == 0)
                return;

            var partyIds = new System.Collections.Generic.List<string>(
                PendingOutcomeByParty.Keys);

            for (int i = 0; i < partyIds.Count; i++)
            {
                string partyId = partyIds[i];
                MobileParty actor = null;

                foreach (MobileParty party in MobileParty.AllLordParties)
                {
                    if (party != null && string.Equals(
                            party.StringId,
                            partyId,
                            StringComparison.Ordinal))
                    {
                        actor = party;
                        break;
                    }
                }

                if (actor != null)
                    VerifyOutcomePendingCommit(actor, "hourly-world");
            }
        }

        internal static void ApplyFinalSafety(
            MobileParty actor,
            PartyThinkParams thinkParams,
            StrategicDecisionComposer.Frame composer)
        {
            if (!Eligible(actor) || thinkParams == null || composer == null)
                return;

            string threatSettlement;
            string threatType;
            double threatAgeHours;
            int threatCount;
            bool threatActive;
            if (!CompanionExperienceMemory.TryGetRecentThreatForHoldback(
                    actor,
                    out threatSettlement,
                    out threatType,
                    out threatAgeHours,
                    out threatCount,
                    out threatActive))
                return;

            float readiness = actor.PartySizeRatio;
            if (readiness >= ExposureReadinessGate)
                return;

            int beforeIndex = composer.CurrentBestIndex(thinkParams);
            if (beforeIndex < 0 || beforeIndex >= thinkParams.AIBehaviorScores.Count)
                return;

            AIBehaviorData beforeData = thinkParams.AIBehaviorScores[beforeIndex].Item1;
            if (!IsRecklessExposure(beforeData.AiBehavior))
                return;

            string before = CandidateLabel(thinkParams, beforeIndex);
            int cuts = 0;
            for (int i = 0; i < thinkParams.AIBehaviorScores.Count; i++)
            {
                AIBehaviorData data = thinkParams.AIBehaviorScores[i].Item1;
                if (!IsRecklessExposure(data.AiBehavior))
                    continue;

                float rawScore = thinkParams.AIBehaviorScores[i].Item2;
                float baseScore = composer.CurrentScore(i, rawScore);
                if (baseScore <= 0f)
                    continue;

                composer.ApplyFactor(
                    i,
                    "companion-holdback-final-safety",
                    baseScore,
                    ExposureFactor,
                    "remembered-threat-low-readiness-exposure");
                cuts++;
            }

            if (cuts == 0)
                return;

            _applications++;
            int afterIndex = composer.CurrentBestIndex(thinkParams);
            if (afterIndex < 0 || afterIndex >= thinkParams.AIBehaviorScores.Count)
                return;

            string after = CandidateLabel(thinkParams, afterIndex);
            bool heldBack = afterIndex != beforeIndex &&
                !IsRecklessExposure(thinkParams.AIBehaviorScores[afterIndex].Item1.AiBehavior);

            ClanAIPostVanilla.WriteExternalLog(
                "HOLDBACK_FINAL_APPLIED actor=" + actor.LeaderHero.Name.ToString() +
                " readiness=" + readiness.ToString("0.###", CultureInfo.InvariantCulture) +
                " rememberedThreat=" + threatSettlement +
                " threatType=" + (threatType ?? "<none>") +
                " threatAgeHours=" + threatAgeHours.ToString("0.###", CultureInfo.InvariantCulture) +
                " threatCount=" + threatCount +
                " threatActive=" + threatActive +
                " exposureFactor=" + ExposureFactor.ToString("0.###", CultureInfo.InvariantCulture) +
                " cuts=" + cuts +
                " before=" + before +
                " after=" + after +
                " heldBack=" + heldBack);

            if (!heldBack)
                return;

            _winnerChanges++;
            AIBehaviorData winner = thinkParams.AIBehaviorScores[afterIndex].Item1;
            Settlement target = winner.Party as Settlement;
            string partyId = actor.StringId ?? actor.LeaderHero.StringId;
            PendingByParty[partyId] = new PendingCommit
            {
                ActorName = actor.LeaderHero.Name.ToString(),
                Behavior = winner.AiBehavior,
                SettlementId = target == null ? null : target.StringId,
                Label = after,
                RememberedThreat = threatSettlement
            };

            ClanAIPostVanilla.WriteExternalLog(
                "HOLDBACK_WINNER_CHANGE actor=" + actor.LeaderHero.Name.ToString() +
                " partyId=" + partyId +
                " before=" + before +
                " after=" + after +
                " readiness=" + readiness.ToString("0.###", CultureInfo.InvariantCulture) +
                " rememberedThreat=" + threatSettlement +
                " threatAgeHours=" + threatAgeHours.ToString("0.###", CultureInfo.InvariantCulture) +
                " threatActive=" + threatActive +
                " reason=remembered-threat-low-readiness-exposure" +
                " winnerChanges=" + _winnerChanges);
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
                _commitMatches++;

            ClanAIPostVanilla.WriteExternalLog(
                "HOLDBACK_COMMIT_CHECK actor=" + pending.ActorName +
                " partyId=" + actor.StringId +
                " expected=" + (pending.Label ?? "<none>") +
                " rememberedThreat=" + (pending.RememberedThreat ?? "<none>") +
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

        private static bool IsOffensive(AiBehavior behavior)
        {
            return behavior == AiBehavior.RaidSettlement ||
                   behavior == AiBehavior.BesiegeSettlement ||
                   behavior == AiBehavior.AssaultSettlement ||
                   behavior == AiBehavior.EngageParty;
        }

        private static bool IsRecklessExposure(AiBehavior behavior)
        {
            return IsOffensive(behavior) ||
                   behavior == AiBehavior.PatrolAroundPoint;
        }

        private static bool SameClan(Clan a, Clan b)
        {
            if (a == null || b == null)
                return false;
            return ReferenceEquals(a, b) ||
                string.Equals(a.StringId, b.StringId, StringComparison.Ordinal);
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

        internal static string SummaryFields()
        {
            return " holdbackEvaluations=" + _evaluations +
                   " holdbackMemoryLowReadinessChecks=" + _memoryLowReadinessChecks +
                   " holdbackApplications=" + _applications +
                   " holdbackWinnerChanges=" + _winnerChanges +
                   " holdbackCommitChecks=" + _commitChecks +
                   " holdbackCommitMatches=" + _commitMatches +
                   " outcomeHoldbackApplications=" + _outcomeApplications +
                   " outcomeHoldbackWinnerChanges=" + _outcomeWinnerChanges +
                   " outcomeHoldbackCommitChecks=" + _outcomeCommitChecks +
                   " outcomeHoldbackCommitMatches=" + _outcomeCommitMatches;
        }
    }
}
