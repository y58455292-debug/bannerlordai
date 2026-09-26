using System;

namespace ClanAI
{
    internal enum CivicProjectPreferenceReason
    {
        InvalidContext = 0,
        NonNpcTown = 1,
        ConstructionActive = 2,
        FestivalUnavailable = 3,
        InvalidLoyalty = 4,
        InvalidThreshold = 5,
        LoyaltyNotBelowThreshold = 6,
        LowLoyaltyFestivalPreferred = 7
    }

    internal readonly struct CivicProjectSelectionResult
    {
        internal CivicProjectSelectionResult(
            bool preferFestivalAndGames,
            CivicProjectPreferenceReason reason)
        {
            PreferFestivalAndGames = preferFestivalAndGames;
            Reason = reason;
        }

        internal bool PreferFestivalAndGames { get; }
        internal CivicProjectPreferenceReason Reason { get; }
    }

    // Phase 6-v1 pure decision policy. Game-assembly-free by design.
    internal static class CivicProjectSelectionPolicy
    {
        internal static CivicProjectSelectionResult Evaluate(
            bool contextValid,
            bool isNpcTown,
            bool isConstructionIdle,
            bool hasFestivalAndGamesProject,
            float loyalty,
            float nativeRebelliousThreshold)
        {
            if (!contextValid)
            {
                return No(
                    CivicProjectPreferenceReason.InvalidContext);
            }

            if (!isNpcTown)
            {
                return No(
                    CivicProjectPreferenceReason.NonNpcTown);
            }

            if (!isConstructionIdle)
            {
                return No(
                    CivicProjectPreferenceReason.ConstructionActive);
            }

            if (!hasFestivalAndGamesProject)
            {
                return No(
                    CivicProjectPreferenceReason.FestivalUnavailable);
            }

            if (!IsFinite(loyalty))
            {
                return No(
                    CivicProjectPreferenceReason.InvalidLoyalty);
            }

            if (!IsFinite(nativeRebelliousThreshold))
            {
                return No(
                    CivicProjectPreferenceReason.InvalidThreshold);
            }

            if (loyalty >= nativeRebelliousThreshold)
            {
                return No(
                    CivicProjectPreferenceReason.LoyaltyNotBelowThreshold);
            }

            return new CivicProjectSelectionResult(
                true,
                CivicProjectPreferenceReason.LowLoyaltyFestivalPreferred);
        }

        internal static bool IsFinite(float value)
        {
            return !float.IsNaN(value) &&
                !float.IsInfinity(value);
        }

        private static CivicProjectSelectionResult No(
            CivicProjectPreferenceReason reason)
        {
            return new CivicProjectSelectionResult(
                false,
                reason);
        }
    }
}

