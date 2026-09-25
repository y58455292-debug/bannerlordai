using System;

namespace ClanAI
{
    internal struct StrategicCommitmentCommitCheckResult
    {
        internal bool BehaviorMatch;
        internal bool TargetMatch;
        internal bool ArrivedMatch;
        internal bool Matched;
        internal bool Expired;
        internal bool Remove;
        internal double AgeHours;
    }

    internal static class StrategicCommitmentCommitPolicy
    {
        internal const double PendingLifetimeHours = 18.0;

        internal static bool ShouldCreateExpectation(
            bool applyMode,
            bool factorApplied,
            bool finalWinnerIsPrevious,
            bool retained)
        {
            return
                applyMode &&
                factorApplied &&
                finalWinnerIsPrevious &&
                retained;
        }

        internal static StrategicCommitmentCommitCheckResult Evaluate(
            string expectedBehavior,
            string expectedTargetKey,
            string actualDefaultBehavior,
            string actualShortTermBehavior,
            string actualTargetKey,
            string arrivedTargetKey,
            double createdAtHours,
            double nowHours)
        {
            double ageHours =
                nowHours - createdAtHours;

            if (ageHours < 0.0)
                ageHours = 0.0;

            bool behaviorMatch =
                !string.IsNullOrEmpty(expectedBehavior) &&
                (string.Equals(
                     actualDefaultBehavior,
                     expectedBehavior,
                     StringComparison.Ordinal) ||
                 string.Equals(
                     actualShortTermBehavior,
                     expectedBehavior,
                     StringComparison.Ordinal));

            bool targetMatch =
                !string.IsNullOrEmpty(expectedTargetKey) &&
                !string.IsNullOrEmpty(actualTargetKey) &&
                string.Equals(
                    actualTargetKey,
                    expectedTargetKey,
                    StringComparison.Ordinal);

            bool settlementTarget =
                !string.IsNullOrEmpty(expectedTargetKey) &&
                expectedTargetKey.StartsWith(
                    "S:",
                    StringComparison.Ordinal);

            bool arrivedMatch =
                settlementTarget &&
                !string.IsNullOrEmpty(arrivedTargetKey) &&
                string.Equals(
                    arrivedTargetKey,
                    expectedTargetKey,
                    StringComparison.Ordinal);

            bool matched =
                (behaviorMatch && targetMatch) ||
                arrivedMatch;

            bool expired =
                ageHours > PendingLifetimeHours;

            return new StrategicCommitmentCommitCheckResult
            {
                BehaviorMatch = behaviorMatch,
                TargetMatch = targetMatch,
                ArrivedMatch = arrivedMatch,
                Matched = matched,
                Expired = expired,
                Remove = matched || expired,
                AgeHours = ageHours
            };
        }
    }
}