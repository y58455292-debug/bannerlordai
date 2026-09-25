using System;

namespace ClanAI
{
    internal struct StrategicCommitmentContextResult
    {
        internal bool Continue;
        internal bool NoPriorState;
        internal bool SameObjective;
        internal bool CrossState;
        internal bool NegativeAge;
        internal bool Expired;
        internal double AgeHours;
    }

    internal struct StrategicCommitmentScoreResult
    {
        internal bool PreviousCandidateValid;
        internal bool NaturalScoreValid;
        internal bool WouldRetain;
        internal bool Apply;
        internal float RetainedScore;
        internal float RequiredFactor;
    }

    internal static class StrategicCommitmentPolicy
    {
        internal const float RetentionFactor = 1.10f;
        internal const double MaxAgeHours = 12.0;

        internal static StrategicCommitmentContextResult EvaluateContext(
            bool hasPriorState,
            bool sameObjective,
            bool sameCoarseState,
            double ageHours)
        {
            var result = new StrategicCommitmentContextResult
            {
                Continue = false,
                NoPriorState = !hasPriorState,
                SameObjective = false,
                CrossState = false,
                NegativeAge = false,
                Expired = false,
                AgeHours = ageHours
            };

            if (!hasPriorState)
                return result;

            if (sameObjective)
            {
                result.SameObjective = true;
                return result;
            }
            if (!sameCoarseState)
            {
                result.CrossState = true;
                return result;
            }

            if (ageHours < 0.0)
            {
                result.NegativeAge = true;
                return result;
            }

            if (ageHours > MaxAgeHours)
            {
                result.Expired = true;
                return result;
            }

            result.Continue = true;
            return result;
        }

        internal static StrategicCommitmentScoreResult EvaluateScores(
            bool previousCandidateFound,
            float previousScore,
            float naturalScore,
            StrategicCommitmentMode mode)
        {
            var result = new StrategicCommitmentScoreResult
            {
                PreviousCandidateValid = false,
                NaturalScoreValid = false,
                WouldRetain = false,
                Apply = false,
                RetainedScore = float.NaN,
                RequiredFactor = float.NaN
            };

            if (!previousCandidateFound ||
                !ValidPositiveScore(previousScore))
            {
                return result;
            }

            result.PreviousCandidateValid = true;

            if (!ValidPositiveScore(naturalScore))
                return result;

            result.NaturalScoreValid = true;
            result.RetainedScore =
                previousScore * RetentionFactor;
            result.RequiredFactor =
                naturalScore / previousScore;

            if (!ValidPositiveScore(result.RetainedScore) ||
                result.RetainedScore <= naturalScore)
            {
                return result;
            }
            result.WouldRetain = true;
            result.Apply =
                mode == StrategicCommitmentMode.Apply;
            return result;
        }

        internal static bool ValidPositiveScore(
            float score)
        {
            return
                score > 0f &&
                !float.IsNaN(score) &&
                !float.IsInfinity(score);
        }
    }
}