namespace ClanAI
{
    internal enum VisualWarBehaviorKind
    {
        Other = 0,
        GoToSettlement = 1,
        DefendSettlement = 2,
        PatrolAroundPoint = 3,
        RaidSettlement = 4,
        BesiegeSettlement = 5,
        AssaultSettlement = 6,
        EngageParty = 7
    }

    internal struct VisualWarPolicyResult
    {
        internal bool Apply;
        internal float Factor;
        internal string Reason;
        internal bool WeakActiveDefenseSkip;
        internal bool WeakFrontierDefenseSkip;
        internal bool RearSecurityEligible;
        internal bool RearSecurityWeakSkip;
    }
    internal static class VisualWarPolicy
    {
        internal const float MinimumReadiness = 0.72f;
        internal const int MinimumFoodDays = 3;
        internal const float DefenseFrontierScale = 0.22f;
        internal const float ActiveDefenseBonus = 0.10f;
        internal const float MaxDefenseFactor = 1.35f;
        internal const float FrontierOffenseScale = 0.16f;
        internal const int RearSecuritySmallPartyMax = 90;
        internal const int RearSecurityPartyMax = 160;
        internal const float RearSecuritySmallFactor = 1.25f;
        internal const float RearSecurityFactor = 1.15f;

        internal static bool IsWeakForRecovery(
            float readiness,
            int foodDays)
        {
            return readiness < MinimumReadiness ||
                   foodDays < MinimumFoodDays;
        }

        internal static VisualWarPolicyResult EvaluateSettlementCandidate(
            float baseScore,
            VisualWarBehaviorKind behavior,
            bool weakForRecovery,
            bool sameFactionSettlement,
            bool atWarWithSettlement,
            float frontierScore,
            bool underAttack,
            bool nearestEnemyIsActorFaction)
        {
            VisualWarPolicyResult result = NoChange();
            if (baseScore <= 0f)
                return result;

            if (sameFactionSettlement &&
                IsDefensiveTravel(behavior))
            {
                float pressure = underAttack
                    ? 1f
                    : frontierScore;

                if (pressure <= 0f)
                    return result;

                if (weakForRecovery)
                {
                    result.WeakActiveDefenseSkip = underAttack;
                    result.WeakFrontierDefenseSkip = !underAttack;
                    return result;
                }

                float factor = 1f +
                    (DefenseFrontierScale * pressure);
                if (underAttack)
                {
                    factor += ActiveDefenseBonus;
                    result.Reason = "active-defense";
                }
                else
                {
                    result.Reason = "frontier-defense";
                }

                if (factor > MaxDefenseFactor)
                    factor = MaxDefenseFactor;

                result.Factor = factor;
                result.Apply = factor > 1.001f;
                return result;
            }

            if (atWarWithSettlement &&
                IsAggressive(behavior) &&
                frontierScore > 0f &&
                nearestEnemyIsActorFaction &&
                !weakForRecovery)
            {
                result.Factor = 1f +
                    (FrontierOffenseScale * frontierScore);
                result.Reason = "frontier-offense";
                result.Apply = result.Factor > 1.001f;
            }

            return result;
        }
        internal static VisualWarPolicyResult EvaluateEngagePartyCandidate(
            float baseScore,
            VisualWarBehaviorKind behavior,
            bool weakForRecovery,
            int men,
            bool hasMobilePartyTarget,
            bool isBanditTarget)
        {
            VisualWarPolicyResult result = NoChange();
            if (baseScore <= 0f ||
                behavior != VisualWarBehaviorKind.EngageParty ||
                !hasMobilePartyTarget ||
                !isBanditTarget)
            {
                return result;
            }

            bool sizeEligible =
                men > 0 &&
                men <= RearSecurityPartyMax;

            if (!sizeEligible)
                return result;

            if (weakForRecovery)
            {
                result.RearSecurityWeakSkip = true;
                return result;
            }
            result.RearSecurityEligible = true;
            result.Factor = men <= RearSecuritySmallPartyMax
                ? RearSecuritySmallFactor
                : RearSecurityFactor;
            result.Reason = "rear-security";
            result.Apply = result.Factor > 1.001f;
            return result;
        }

        private static bool IsDefensiveTravel(
            VisualWarBehaviorKind behavior)
        {
            return behavior == VisualWarBehaviorKind.GoToSettlement ||
                   behavior == VisualWarBehaviorKind.DefendSettlement ||
                   behavior == VisualWarBehaviorKind.PatrolAroundPoint;
        }

        private static bool IsAggressive(
            VisualWarBehaviorKind behavior)
        {
            return behavior == VisualWarBehaviorKind.RaidSettlement ||
                   behavior == VisualWarBehaviorKind.BesiegeSettlement ||
                   behavior == VisualWarBehaviorKind.AssaultSettlement;
        }

        private static VisualWarPolicyResult NoChange()
        {
            return new VisualWarPolicyResult
            {
                Apply = false,
                Factor = 1f,
                Reason = null,
                WeakActiveDefenseSkip = false,
                WeakFrontierDefenseSkip = false,
                RearSecurityEligible = false,
                RearSecurityWeakSkip = false
            };
        }
    }
}