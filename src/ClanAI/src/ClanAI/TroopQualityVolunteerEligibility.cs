using TaleWorlds.CampaignSystem;

namespace ClanAI
{
    // Read-only mirror of native occupied-volunteer upgrade eligibility.
    // It does not choose an upgrade target or mutate the volunteer slot.
    internal static class TroopQualityVolunteerEligibility
    {
        internal static bool IsNativeUpgradeEligible(
            CharacterObject volunteer,
            int maxVolunteerTier)
        {
            if (volunteer == null ||
                volunteer.UpgradeTargets == null ||
                volunteer.UpgradeTargets.Length == 0)
            {
                return false;
            }

            return TroopQualityProbabilityPolicy
                .IsNativeUpgradeEligible(
                    true,
                    true,
                    volunteer.Tier,
                    maxVolunteerTier);
        }
    }
}
