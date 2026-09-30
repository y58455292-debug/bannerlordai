using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.Settlements;

namespace ClanAI
{
    // Performs one bounded local native settlement lookup on demand. It does not build
    // or cache a world grid and never assigns sovereignty from route/face/group identity.
    internal static class KingdomLandControlZoneNativeAdapter
    {
        internal static KingdomLandZoneResult Classify(CampaignVec2 position)
        {
            return ClassifyRoutePoint(position.ToVec2());
        }

        // Do not infer a stored waypoint's navigation type from the party's current sea state.
        // Test the same coordinate under both native navigation modes; overlap/invalid points
        // fail open as unknown.
        internal static KingdomLandZoneResult ClassifyRoutePoint(TaleWorlds.Library.Vec2 point)
        {
            if (Campaign.Current == null || Campaign.Current.Models == null ||
                Campaign.Current.Models.PartyNavigationModel == null)
                return new KingdomLandZoneResult(KingdomLandZoneKind.Unknown, null, null);

            bool landValid = new CampaignVec2(point, true).IsValid();
            bool seaValid = new CampaignVec2(point, false).IsValid();
            StoredRouteNavigationKind navigationKind = StoredRouteObserverPolicy.ClassifyNavigationModes(
                landValid, seaValid);
            if (navigationKind == StoredRouteNavigationKind.Unknown)
                return new KingdomLandZoneResult(KingdomLandZoneKind.Unknown, null, null);
            if (navigationKind == StoredRouteNavigationKind.Sea)
                return new KingdomLandZoneResult(KingdomLandZoneKind.SeaOrOpenWater, null, null);

            var nearby = new List<KingdomLandZoneSettlement>(KingdomLandControlPolicy.MaximumNearbyLocatables);
            LocatableSearchData<Settlement> search = Settlement.StartFindingLocatablesAroundPosition(
                point, KingdomLandControlPolicy.ControlRadius);
            bool complete = false;
            for (int index = 0; index <= KingdomLandControlPolicy.MaximumNearbyLocatables; index++)
            {
                Settlement settlement = Settlement.FindNextLocatable(ref search);
                if (settlement == null)
                {
                    complete = true;
                    break;
                }
                if (index == KingdomLandControlPolicy.MaximumNearbyLocatables)
                    break;

                var ownerClan = settlement.OwnerClan;
                string ownerKingdomId = ownerClan == null || ownerClan.Kingdom == null
                    ? null
                    : ownerClan.Kingdom.StringId;
                float distanceSquared = point.DistanceSquared(settlement.Position.ToVec2());
                nearby.Add(new KingdomLandZoneSettlement(settlement.StringId, settlement.IsFortification,
                    ownerClan != null, ownerKingdomId, distanceSquared));
            }

            return KingdomLandControlPolicy.Classify(true, complete, nearby);
        }
    }
}
