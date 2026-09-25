using System;

namespace ClanAI
{
    internal enum Phase4ARecreationSource
    {
        PostDefeatNativeRecreationInitialTroopsSupported,
        PostDefeatNativeRecreationZeroTroops,
        PostDefeatRecreationObservedButPreSettlementStateIncomplete,
        GenericNativePartyCreation,
        UnknownOrUnlinkedCreation
    }

    // Pure evidence rules. These never authorize a game action.
    internal static class Phase4ARecreationLinkPolicy
    {
        internal const double MaxObservationHours = 168.0;

        internal static bool SameHero(string defeatedHeroId, string createdHeroId)
        {
            return !string.IsNullOrWhiteSpace(defeatedHeroId) &&
                !string.IsNullOrWhiteSpace(createdHeroId) &&
                string.Equals(defeatedHeroId, createdHeroId, StringComparison.Ordinal);
        }

        internal static bool CanAcceptDefeatIdentity(
            bool sameBattle, bool sameParty, bool sameSide,
            string capturedHeroId, string liveHeroId)
        {
            return sameBattle && sameParty && sameSide &&
                !string.IsNullOrWhiteSpace(capturedHeroId) &&
                (string.IsNullOrWhiteSpace(liveHeroId) ||
                 SameHero(capturedHeroId, liveHeroId));
        }

        internal static bool InWindow(double start, double now)
        {
            return !double.IsNaN(start) && !double.IsInfinity(start) &&
                !double.IsNaN(now) && !double.IsInfinity(now) &&
                now >= start && now - start <= MaxObservationHours;
        }

        internal static bool CanLink(
            string defeatedHeroId, string createdHeroId,
            double defeatHour, double creationHour,
            bool distinctNativeParty, bool priorPartyGone)
        {
            return SameHero(defeatedHeroId, createdHeroId) &&
                InWindow(defeatHour, creationHour) &&
                distinctNativeParty && priorPartyGone;
        }

        internal static Phase4ARecreationSource Classify(
            bool nativeCreation, bool stableIdentity, bool linkedDefeat,
            bool preSettlementComplete, int initialTotal, int initialHeroes)
        {
            if (!nativeCreation || !stableIdentity)
                return Phase4ARecreationSource.UnknownOrUnlinkedCreation;
            if (!linkedDefeat)
                return Phase4ARecreationSource.GenericNativePartyCreation;
            if (!preSettlementComplete || initialTotal < 0 || initialHeroes < 0 ||
                initialHeroes > initialTotal)
                return Phase4ARecreationSource.PostDefeatRecreationObservedButPreSettlementStateIncomplete;
            // A leader alone is not evidence of supplied replacement troops.
            return initialTotal > initialHeroes
                ? Phase4ARecreationSource.PostDefeatNativeRecreationInitialTroopsSupported
                : Phase4ARecreationSource.PostDefeatNativeRecreationZeroTroops;
        }

        internal static bool CompleteFirstVisit(
            bool linkedDefeat, bool creationBeforeSettlement, bool missingBoundary,
            bool observerFault, string createdHeroId, string visitingHeroId,
            double createdHour, double visitHour)
        {
            return linkedDefeat && creationBeforeSettlement && !missingBoundary &&
                !observerFault && SameHero(createdHeroId, visitingHeroId) &&
                InWindow(createdHour, visitHour);
        }
    }
}
