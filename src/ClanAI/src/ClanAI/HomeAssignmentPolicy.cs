namespace ClanAI
{
    internal enum HomeAssignmentCandidateKind { Other, Visit, Patrol }
    internal struct HomeAssignmentDecision
    {
        internal readonly bool Apply;
        internal readonly float Factor;
        internal readonly string Reason;
        internal HomeAssignmentDecision(bool apply, float factor, string reason)
        { Apply = apply; Factor = factor; Reason = reason; }
    }
    internal static class HomeAssignmentPolicy
    {
        internal const float HomeFactor = 1.25f;
        internal static bool PositiveFinite(float value)
        { return value > 0f && !float.IsNaN(value) && !float.IsInfinity(value); }
        internal static HomeAssignmentDecision Evaluate(bool eligible, bool validHome,
            bool peace, bool urgent, bool exactHome, HomeAssignmentCandidateKind behavior,
            float nativeScore, float currentScore)
        {
            if (!eligible || !validHome || !peace || urgent || !exactHome ||
                !PositiveFinite(nativeScore) || !PositiveFinite(currentScore) || currentScore > float.MaxValue / HomeFactor ||
                (behavior != HomeAssignmentCandidateKind.Visit && behavior != HomeAssignmentCandidateKind.Patrol))
                return new HomeAssignmentDecision(false, 1f, null);
            return new HomeAssignmentDecision(true, HomeFactor,
                behavior == HomeAssignmentCandidateKind.Visit ? "assigned-home-visit" : "assigned-home-patrol");
        }
        internal static bool CanExpose(bool eligible, bool validHome, bool peace,
            bool urgent, bool alreadyPresent, bool suitable, bool landNavigation, float distance)
        {
            return eligible && validHome && peace && !urgent && !alreadyPresent &&
                suitable && landNavigation && distance >= 0f &&
                !float.IsNaN(distance) && !float.IsInfinity(distance);
        }
    }
}
