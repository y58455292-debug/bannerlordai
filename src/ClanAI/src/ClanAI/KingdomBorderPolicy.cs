using System;

namespace ClanAI
{
    // Settlement ownership is a native, live fact. This class deliberately does not
    // infer sovereignty for arbitrary map positions or routes between settlements.
    internal enum KingdomBorderRelation
    {
        Unknown = 0,
        Open = 1,
        Closed = 2,
        IndependentDestination = 3,
        SeaOrOpenWater = 4
    }

    internal static class KingdomBorderPolicy
    {
        internal static KingdomBorderRelation ClassifySettlementDestination(
            bool isSeaOrOpenWater,
            string actorKingdomId,
            bool destinationHasOwnerClan,
            string destinationOwnerKingdomId,
            bool explicitlyClosed)
        {
            if (isSeaOrOpenWater)
                return KingdomBorderRelation.SeaOrOpenWater;

            if (!destinationHasOwnerClan || string.IsNullOrEmpty(actorKingdomId))
                return KingdomBorderRelation.Unknown;

            if (string.IsNullOrEmpty(destinationOwnerKingdomId))
                return KingdomBorderRelation.IndependentDestination;

            if (string.Equals(actorKingdomId, destinationOwnerKingdomId, StringComparison.Ordinal))
                return KingdomBorderRelation.Open;

            return explicitlyClosed
                ? KingdomBorderRelation.Closed
                : KingdomBorderRelation.Open;
        }
    }
}

