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
            if (!position.IsOnLand)
                return new KingdomLandZoneResult(KingdomLandZoneKind.SeaOrOpenWater, null, null);
            if (Campaign.Current == null || Campaign.Current.Models == null ||
                Campaign.Current.Models.PartyNavigationModel == null)
                return new KingdomLandZoneResult(KingdomLandZoneKind.Unknown, null, null);

            // The route point's native land flag alone is not enough at coastlines. Require
            // land-valid and sea-invalid navigation at this point; overlap/invalid points fail open.
            bool landValid = position.IsValid();
            bool seaValid = new CampaignVec2(position.ToVec2(), false).IsValid();
            if (landValid == seaValid)
                return new KingdomLandZoneResult(KingdomLandZoneKind.Unknown, null, null);
            if (seaValid)
                return new KingdomLandZoneResult(KingdomLandZoneKind.SeaOrOpenWater, null, null);

            var nearby = new List<KingdomLandZoneSettlement>(KingdomLandControlPolicy.MaximumNearbyLocatables);
            LocatableSearchData<Settlement> search = Settlement.StartFindingLocatablesAroundPosition(
                position.ToVec2(), KingdomLandControlPolicy.ControlRadius);
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
                float distanceSquared = position.ToVec2().DistanceSquared(settlement.Position.ToVec2());
                nearby.Add(new KingdomLandZoneSettlement(settlement.StringId, settlement.IsFortification,
                    ownerClan != null, ownerKingdomId, distanceSquared));
            }

            return KingdomLandControlPolicy.Classify(true, complete, nearby);
        }
    }
}
