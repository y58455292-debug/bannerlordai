using System;
using System.Collections.Generic;

namespace ClanAI
{
    // A control zone is an explicitly approximate native-settlement proximity result,
    // not a claim about Bannerlord political polygons or settlement ownership itself.
    internal enum KingdomLandZoneKind
    {
        Unknown = 0,
        NoNearbyFortification = 1,
        KingdomOwned = 2,
        IndependentOwned = 3,
        SeaOrOpenWater = 4,
        Ambiguous = 5
    }

    internal struct KingdomLandZoneSettlement
    {
        internal readonly string StringId;
        internal readonly bool IsFortification;
        internal readonly bool HasOwnerClan;
        internal readonly string OwnerKingdomId;
        internal readonly float DistanceSquared;

        internal KingdomLandZoneSettlement(string stringId, bool isFortification,
            bool hasOwnerClan, string ownerKingdomId, float distanceSquared)
        {
            StringId = stringId;
            IsFortification = isFortification;
            HasOwnerClan = hasOwnerClan;
            OwnerKingdomId = ownerKingdomId;
            DistanceSquared = distanceSquared;
        }
    }

    internal struct KingdomLandZoneResult
    {
        internal readonly KingdomLandZoneKind Kind;
        internal readonly string KingdomId;
        internal readonly string SettlementId;

        internal KingdomLandZoneResult(KingdomLandZoneKind kind, string kingdomId, string settlementId)
        {
            Kind = kind;
            KingdomId = kingdomId;
            SettlementId = settlementId;
        }
    }

    internal static class KingdomLandControlPolicy
    {
        // Initial conservative tuning, in Bannerlord map Vec2 units. Runtime usefulness
        // still requires campaign observation; the small radius avoids claiming a region.
        internal const float ControlRadius = 5f;
        internal const int MaximumNearbyLocatables = 8;
        internal const int MaximumRouteWaypoints = 8;

        internal static KingdomLandZoneResult Classify(bool isOnLand, bool nativeSearchComplete,
            IList<KingdomLandZoneSettlement> nearbySettlements)
        {
            if (!isOnLand)
                return new KingdomLandZoneResult(KingdomLandZoneKind.SeaOrOpenWater, null, null);
            if (!nativeSearchComplete || nearbySettlements == null ||
                nearbySettlements.Count > MaximumNearbyLocatables)
                return new KingdomLandZoneResult(KingdomLandZoneKind.Unknown, null, null);

            float nearestDistance = float.MaxValue;
            string nearestKingdom = null;
            string nearestSettlement = null;
            bool nearestHasOwner = false;
            bool found = false;
            bool ambiguous = false;
            float radiusSquared = ControlRadius * ControlRadius;

            for (int i = 0; i < nearbySettlements.Count; i++)
            {
                KingdomLandZoneSettlement settlement = nearbySettlements[i];
                if (!settlement.IsFortification) continue;
                if (string.IsNullOrEmpty(settlement.StringId) ||
                    float.IsNaN(settlement.DistanceSquared) || float.IsInfinity(settlement.DistanceSquared) ||
                    settlement.DistanceSquared < 0f)
                    return new KingdomLandZoneResult(KingdomLandZoneKind.Unknown, null, null);
                // Native LocatorGrid excludes results at exactly the radius boundary.
                if (settlement.DistanceSquared >= radiusSquared) continue;

                if (!found || settlement.DistanceSquared < nearestDistance)
                {
                    found = true;
                    ambiguous = false;
                    nearestDistance = settlement.DistanceSquared;
                    nearestKingdom = settlement.OwnerKingdomId;
                    nearestSettlement = settlement.StringId;
                    nearestHasOwner = settlement.HasOwnerClan;
                }
                else if (settlement.DistanceSquared == nearestDistance &&
                    (nearestHasOwner != settlement.HasOwnerClan ||
                     !string.Equals(nearestKingdom, settlement.OwnerKingdomId, StringComparison.Ordinal)))
                {
                    ambiguous = true;
                }
                else if (settlement.DistanceSquared == nearestDistance &&
                    string.Compare(settlement.StringId, nearestSettlement, StringComparison.Ordinal) < 0)
                {
                    nearestSettlement = settlement.StringId;
                }
            }

            if (!found)
                return new KingdomLandZoneResult(KingdomLandZoneKind.NoNearbyFortification, null, null);
            if (ambiguous)
                return new KingdomLandZoneResult(KingdomLandZoneKind.Ambiguous, null, null);
            if (!nearestHasOwner)
                return new KingdomLandZoneResult(KingdomLandZoneKind.Unknown, null, nearestSettlement);
            if (string.IsNullOrEmpty(nearestKingdom))
                return new KingdomLandZoneResult(KingdomLandZoneKind.IndependentOwned, null, nearestSettlement);
            return new KingdomLandZoneResult(KingdomLandZoneKind.KingdomOwned, nearestKingdom, nearestSettlement);
        }

        internal static bool BlocksRouteZone(KingdomLandZoneResult zone, string visitorKingdomId,
            bool visitorAtWarWithZoneKingdom, bool explicitlyClosed)
        {
            return zone.Kind == KingdomLandZoneKind.KingdomOwned &&
                !string.IsNullOrEmpty(zone.KingdomId) &&
                !string.IsNullOrEmpty(visitorKingdomId) &&
                !string.Equals(visitorKingdomId, zone.KingdomId, StringComparison.Ordinal) &&
                !visitorAtWarWithZoneKingdom && explicitlyClosed;
        }
    }
}
