using System;

namespace ClanAI
{
    internal enum KingdomObjectiveBehaviorKind
    {
        Other = 0,
        BesiegeSettlement = 1,
        AssaultSettlement = 2,
        GoToSettlement = 3,
        PatrolAroundPoint = 4,
        DefendSettlement = 5
    }

    internal struct KingdomObjectiveCandidateDecision
    {
        internal bool Apply;
        internal bool Direct;
        internal bool Competitive;
        internal bool CanReachWinnerMargin;
        internal float BaseFactor;
        internal float CompetitiveRatio;
        internal float RequiredWinnerFactor;
        internal float MaxFactor;
        internal float AppliedFactor;
        internal float ProjectedScore;
        internal string Mode;
    }
    internal static class KingdomObjectivePolicy
    {
        internal const float BesiegeFactor = 1.75f;
        internal const float AssaultFactor = 1.65f;
        internal const float GoToFactor = 1.50f;
        internal const float PatrolFactor = 1.35f;
        internal const float StagingGoToFactor = 1.55f;
        internal const float StagingPatrolFactor = 1.40f;
        internal const float StagingDefendFactor = 1.30f;
        internal const float MaxDirectOrderFactor = 2.50f;
        internal const float MaxStagingOrderFactor = 2.25f;
        internal const float MinimumCompetitiveRatio = 0.45f;
        internal const float WinnerMargin = 1.025f;
        internal const float MinimumReadiness = 0.72f;
        internal const int MinimumFoodDays = 3;

        internal static string RefusalReason(
            float readiness,
            int foodDays,
            bool urgentClanHomeThreat)
        {
            if (readiness < MinimumReadiness ||
                (foodDays >= 0 && foodDays < MinimumFoodDays))
                return "low-readiness-or-supplies";

            if (urgentClanHomeThreat)
                return "urgent-clan-home-threat";

            return null;
        }

        internal static KingdomObjectiveCandidateDecision EvaluateCandidate(
            bool direct,
            bool staging,
            int stagingRank,
            KingdomObjectiveBehaviorKind behavior,
            float baseScore,
            float beforeWinnerScore)
        {
            var result = new KingdomObjectiveCandidateDecision
            {
                Apply = false,
                Direct = direct,
                Competitive = false,
                CanReachWinnerMargin = false,
                BaseFactor = 1f,
                CompetitiveRatio = 0f,
                RequiredWinnerFactor = 1f,
                MaxFactor = direct
                    ? MaxDirectOrderFactor
                    : MaxStagingOrderFactor,
                AppliedFactor = 1f,
                ProjectedScore = baseScore,
                Mode = direct
                    ? "direct"
                    : (staging ? "staging-" + (Math.Max(0, stagingRank) + 1) : "none")
            };

            if (!direct && !staging)
                return result;

            float baseFactor = direct
                ? DirectFactorFor(behavior)
                : StagingFactorFor(behavior, stagingRank);
            result.BaseFactor = baseFactor;
            if (baseFactor <= 1.001f || baseScore <= 0f)
                return result;

            result.Apply = true;
            float competitiveRatio = beforeWinnerScore > 0f
                ? baseScore / beforeWinnerScore
                : 1f;
            float requiredFactor = beforeWinnerScore > 0f
                ? (beforeWinnerScore * WinnerMargin) / baseScore
                : baseFactor;
            float maxFactor = result.MaxFactor;
            float factor = baseFactor;
            bool competitive = competitiveRatio >= MinimumCompetitiveRatio;
            if (competitive && requiredFactor <= maxFactor)
                factor = Math.Max(baseFactor, requiredFactor);

            result.Competitive = competitive;
            result.CompetitiveRatio = competitiveRatio;
            result.RequiredWinnerFactor = requiredFactor;
            result.AppliedFactor = factor;
            result.ProjectedScore = baseScore * factor;
            result.CanReachWinnerMargin =
                beforeWinnerScore <= 0f ||
                (competitive && requiredFactor <= maxFactor);
            return result;
        }

        internal static bool ShouldCreatePendingCommit(
            bool winnerChanged,
            bool objectiveWon,
            bool winningSettlementPresent)
        {
            return winnerChanged &&
                   objectiveWon &&
                   winningSettlementPresent;
        }

        private static float DirectFactorFor(KingdomObjectiveBehaviorKind behavior)
        {
            if (behavior == KingdomObjectiveBehaviorKind.BesiegeSettlement)
                return BesiegeFactor;
            if (behavior == KingdomObjectiveBehaviorKind.AssaultSettlement)
                return AssaultFactor;
            if (behavior == KingdomObjectiveBehaviorKind.GoToSettlement)
                return GoToFactor;
            if (behavior == KingdomObjectiveBehaviorKind.PatrolAroundPoint)
                return PatrolFactor;
            return 1f;
        }

        private static float StagingFactorFor(
            KingdomObjectiveBehaviorKind behavior,
            int rank)
        {
            float factor;
            if (behavior == KingdomObjectiveBehaviorKind.GoToSettlement)
                factor = StagingGoToFactor;
            else if (behavior == KingdomObjectiveBehaviorKind.PatrolAroundPoint)
                factor = StagingPatrolFactor;
            else if (behavior == KingdomObjectiveBehaviorKind.DefendSettlement)
                factor = StagingDefendFactor;
            else
                return 1f;

            float rankPenalty = Math.Max(0, rank) * 0.10f;
            return Math.Max(1.10f, factor - rankPenalty);
        }
    }
}