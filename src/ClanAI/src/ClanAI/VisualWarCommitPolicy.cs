using System;

namespace ClanAI
{
    internal sealed class VisualWarCommitExpectation
    {
        internal string PartyId;
        internal string ActorName;
        internal VisualWarBehaviorKind ExpectedBehavior;
        internal string ExpectedTargetKey;
        internal string ExpectedTargetName;
        internal string Reason;
        internal double CreatedAtHours;
    }

    internal struct VisualWarCommitCheckResult
    {
        internal bool BehaviorMatch;
        internal bool TargetMatch;
        internal bool ArrivedMatch;
        internal bool Matched;
        internal bool Expired;
        internal bool Remove;
        internal double AgeHours;
    }

    internal static class VisualWarCommitPolicy
    {
        internal const double PendingLifetimeHours = 18.0;
        internal static VisualWarCommitExpectation CreateExpectation(
            string partyId,
            string actorName,
            VisualWarBehaviorKind expectedBehavior,
            string expectedTargetKey,
            string expectedTargetName,
            string reason,
            double createdAtHours)
        {
            if (string.IsNullOrEmpty(partyId) ||
                expectedBehavior == VisualWarBehaviorKind.Other ||
                string.IsNullOrEmpty(expectedTargetKey) ||
                string.IsNullOrEmpty(reason))
            {
                return null;
            }

            return new VisualWarCommitExpectation
            {
                PartyId = partyId,
                ActorName = string.IsNullOrEmpty(actorName)
                    ? partyId
                    : actorName,
                ExpectedBehavior = expectedBehavior,
                ExpectedTargetKey = expectedTargetKey,
                ExpectedTargetName = string.IsNullOrEmpty(expectedTargetName)
                    ? expectedTargetKey
                    : expectedTargetName,
                Reason = reason,
                CreatedAtHours = createdAtHours
            };
        }
        internal static VisualWarCommitCheckResult Evaluate(
            VisualWarCommitExpectation expectation,
            VisualWarBehaviorKind actualDefaultBehavior,
            VisualWarBehaviorKind actualShortTermBehavior,
            string actualTargetKey,
            string arrivedTargetKey,
            double nowHours)
        {
            if (expectation == null)
                return new VisualWarCommitCheckResult();

            double ageHours =
                nowHours - expectation.CreatedAtHours;
            if (ageHours < 0.0)
                ageHours = 0.0;

            bool behaviorMatch =
                actualDefaultBehavior == expectation.ExpectedBehavior ||
                actualShortTermBehavior == expectation.ExpectedBehavior;
            bool targetMatch =
                !string.IsNullOrEmpty(actualTargetKey) &&
                string.Equals(
                    actualTargetKey,
                    expectation.ExpectedTargetKey,
                    StringComparison.Ordinal);
            bool arrivedMatch =
                !string.IsNullOrEmpty(arrivedTargetKey) &&
                string.Equals(
                    arrivedTargetKey,
                    expectation.ExpectedTargetKey,
                    StringComparison.Ordinal);
            bool matched =
                (behaviorMatch && targetMatch) ||
                arrivedMatch;
            bool expired =
                ageHours > PendingLifetimeHours;

            return new VisualWarCommitCheckResult
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