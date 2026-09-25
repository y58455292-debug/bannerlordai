namespace ClanAI
{
    internal enum HomeResponsibilityBehaviorKind
    {
        Other = 0,
        GoToSettlement = 1,
        DefendSettlement = 2,
        PatrolAroundPoint = 3
    }

    internal struct HomeResponsibilityPolicyResult
    {
        internal HomeResponsibilityPolicyResult(bool apply, float factor, string reason)
        {
            Apply = apply;
            Factor = factor;
            Reason = reason;
        }

        internal bool Apply { get; private set; }
        internal float Factor { get; private set; }
        internal string Reason { get; private set; }
    }
    internal static class HomeResponsibilityPolicy
    {
        internal const float WarGoHomeFactor = 1.15f;
        internal const float WarPatrolFactor = 1.25f;
        internal const float WarDefendFactor = 1.30f;
        internal const float ThreatFactor = 1.60f;
        internal const float RecoveryFactor = 1.35f;

        private static readonly HomeResponsibilityPolicyResult NoChange =
            new HomeResponsibilityPolicyResult(false, 1f, null);

        internal static HomeResponsibilityPolicyResult Evaluate(
            bool actorEligibleAtWar,
            bool ownedByActorClan,
            float baseScore,
            bool underSiege,
            bool underRaid,
            bool weak,
            HomeResponsibilityBehaviorKind behavior)
        {
            if (!actorEligibleAtWar || !ownedByActorClan || baseScore <= 0f)
                return NoChange;

            if (underSiege)
                return Apply(ThreatFactor, "home-under-siege");
            if (underRaid)
                return Apply(ThreatFactor, "home-under-raid");

            if (weak && behavior == HomeResponsibilityBehaviorKind.GoToSettlement)
                return Apply(RecoveryFactor, "recover-at-home");

            if (behavior == HomeResponsibilityBehaviorKind.DefendSettlement)
                return Apply(WarDefendFactor, "defend-home-at-war");

            if (behavior == HomeResponsibilityBehaviorKind.PatrolAroundPoint)
                return Apply(WarPatrolFactor, "patrol-home-at-war");

            if (behavior == HomeResponsibilityBehaviorKind.GoToSettlement)
                return Apply(WarGoHomeFactor, "stay-near-home-at-war");

            return NoChange;
        }

        private static HomeResponsibilityPolicyResult Apply(float factor, string reason)
        {
            return new HomeResponsibilityPolicyResult(true, factor, reason);
        }
    }
}