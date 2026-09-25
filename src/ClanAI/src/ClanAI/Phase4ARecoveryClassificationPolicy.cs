namespace ClanAI
{
    internal enum Phase4ARecoverySource
    {
        None = 0,
        InitialPresenceBeforeSettlement = 1,
        SettlementRecruitmentSupported = 2,
        GarrisonWithdrawalSupported = 3,
        MixedSettlementSources = 4,
        OtherOrUnknownNativeSource = 5,
        UnknownSettlementSource = 6
    }

    internal static class Phase4ARecoveryClassificationPolicy
    {
        internal const float SevereDepletionRatio = 0.35f;

        internal static bool IsSeverelyDepleted(float partySizeRatio)
        {
            return
                partySizeRatio >= 0f &&
                partySizeRatio <= SevereDepletionRatio &&
                !float.IsNaN(partySizeRatio) &&
                !float.IsInfinity(partySizeRatio);
        }

        internal static Phase4ARecoverySource ClassifyInitialPresence(
            bool createdParty,
            bool currentlyInSettlement,
            int partyTotal)
        {
            if (createdParty &&
                !currentlyInSettlement &&
                partyTotal > 0)
            {
                return
                    Phase4ARecoverySource
                        .InitialPresenceBeforeSettlement;
            }

            return Phase4ARecoverySource.None;
        }

        internal static Phase4ARecoverySource ClassifyIncrease(
            int partyGain,
            bool duringSettlementVisit,
            bool volunteerPoolObserved,
            int volunteerDecrease,
            bool garrisonObserved,
            int garrisonDecrease)
        {
            if (partyGain <= 0)
                return Phase4ARecoverySource.None;

            if (!duringSettlementVisit)
            {
                return
                    Phase4ARecoverySource
                        .OtherOrUnknownNativeSource;
            }

            bool recruitmentEvidence =
                volunteerPoolObserved &&
                volunteerDecrease > 0;

            bool garrisonEvidence =
                garrisonObserved &&
                garrisonDecrease > 0;

            if (recruitmentEvidence &&
                garrisonEvidence)
            {
                return
                    Phase4ARecoverySource
                        .MixedSettlementSources;
            }

            if (recruitmentEvidence)
            {
                return
                    Phase4ARecoverySource
                        .SettlementRecruitmentSupported;
            }

            if (garrisonEvidence)
            {
                return
                    Phase4ARecoverySource
                        .GarrisonWithdrawalSupported;
            }

            return
                Phase4ARecoverySource
                    .UnknownSettlementSource;
        }
    }
}