using System;
using System.IO;
using ClanAI;

internal static class VisualWarProgram
{
    private static int _checks;
    private static int _failures;

    private static bool Close(float a, float b, float tolerance = 0.0005f)
    {
        return Math.Abs(a - b) <= tolerance;
    }

    private static void Check(bool condition, string name)
    {
        _checks++;
        if (condition)
            return;

        Console.WriteLine("FAIL " + name);
        _failures++;
    }

    private static VisualWarPolicyResult Settlement(
        float score,
        VisualWarBehaviorKind behavior,
        bool weak,
        bool friendly,
        bool atWar,
        float frontier,
        bool underAttack,
        bool nearestEnemyIsActor)
    {
        return VisualWarPolicy.EvaluateSettlementCandidate(
            score,
            behavior,
            weak,
            friendly,
            atWar,
            frontier,
            underAttack,
            nearestEnemyIsActor);
    }

    private static VisualWarPolicyResult Engage(
        float score,
        VisualWarBehaviorKind behavior,
        bool weak,
        int men,
        bool mobile,
        bool bandit)
    {
        return VisualWarPolicy.EvaluateEngagePartyCandidate(
            score,
            behavior,
            weak,
            men,
            mobile,
            bandit);
    }

    private static void CheckWeakRules()
    {
        Check(
            VisualWarPolicy.IsWeakForRecovery(0.719f, 10),
            "readiness-below-0.72-is-weak");
        Check(
            VisualWarPolicy.IsWeakForRecovery(0.90f, 2),
            "food-below-3-is-weak");
        Check(
            !VisualWarPolicy.IsWeakForRecovery(0.72f, 3),
            "exact-threshold-is-healthy");

        var active = Settlement(
            10f, VisualWarBehaviorKind.DefendSettlement,
            true, true, false, 1f, true, false);
        Check(
            !active.Apply && active.WeakActiveDefenseSkip &&
            !active.WeakFrontierDefenseSkip,
            "weak-actor-skips-active-defense");

        var frontier = Settlement(
            10f, VisualWarBehaviorKind.PatrolAroundPoint,
            true, true, false, 0.8f, false, false);
        Check(
            !frontier.Apply && frontier.WeakFrontierDefenseSkip &&
            !frontier.WeakActiveDefenseSkip,
            "weak-actor-skips-frontier-defense");
    }

    private static void CheckSettlementRules()
    {
        var active = Settlement(
            10f, VisualWarBehaviorKind.GoToSettlement,
            false, true, false, 0.2f, true, false);
        Check(
            active.Apply && active.Reason == "active-defense" &&
            Close(active.Factor, 1.32f),
            "healthy-active-defense-factor");

        var frontier = Settlement(
            10f, VisualWarBehaviorKind.DefendSettlement,
            false, true, false, 0.8f, false, false);
        Check(
            frontier.Apply && frontier.Reason == "frontier-defense" &&
            Close(frontier.Factor, 1.176f),
            "healthy-frontier-defense-factor");

        var capped = Settlement(
            10f, VisualWarBehaviorKind.PatrolAroundPoint,
            false, true, false, 2f, false, false);
        Check(
            capped.Apply &&
            Close(capped.Factor, VisualWarPolicy.MaxDefenseFactor),
            "defense-factor-cap-1.35");

        var offense = Settlement(
            10f, VisualWarBehaviorKind.BesiegeSettlement,
            false, false, true, 0.75f, false, true);
        Check(
            offense.Apply && offense.Reason == "frontier-offense" &&
            Close(offense.Factor, 1.12f),
            "enemy-frontier-offense-factor");

        var wrongEnemy = Settlement(
            10f, VisualWarBehaviorKind.RaidSettlement,
            false, false, true, 1f, false, false);
        Check(
            !wrongEnemy.Apply && Close(wrongEnemy.Factor, 1f),
            "frontier-offense-requires-nearest-enemy-match");

        var weakOffense = Settlement(
            10f, VisualWarBehaviorKind.AssaultSettlement,
            true, false, true, 1f, false, true);
        Check(
            !weakOffense.Apply,
            "weak-actor-skips-frontier-offense");

        var unrelated = Settlement(
            10f, VisualWarBehaviorKind.Other,
            false, true, false, 1f, true, false);
        Check(
            !unrelated.Apply && unrelated.Reason == null,
            "unrelated-settlement-behavior-untouched");

        var zero = Settlement(
            0f, VisualWarBehaviorKind.GoToSettlement,
            false, true, false, 1f, true, false);
        Check(
            !zero.Apply && Close(zero.Factor, 1f),
            "zero-native-score-untouched");
    }

    private static void CheckRearSecurityRules()
    {
        var small = Engage(
            10f, VisualWarBehaviorKind.EngageParty,
            false, 80, true, true);
        Check(
            small.Apply && small.RearSecurityEligible &&
            small.Reason == "rear-security" &&
            Close(small.Factor, 1.25f),
            "rear-security-small-bandit-party");

        var at90 = Engage(
            10f, VisualWarBehaviorKind.EngageParty,
            false, 90, true, true);
        Check(
            at90.Apply && Close(at90.Factor, 1.25f),
            "rear-security-90-inclusive");

        var at91 = Engage(
            10f, VisualWarBehaviorKind.EngageParty,
            false, 91, true, true);
        Check(
            at91.Apply && Close(at91.Factor, 1.15f),
            "rear-security-91-lower-factor");

        var at160 = Engage(
            10f, VisualWarBehaviorKind.EngageParty,
            false, 160, true, true);
        Check(
            at160.Apply && Close(at160.Factor, 1.15f),
            "rear-security-160-inclusive");

        var over160 = Engage(
            10f, VisualWarBehaviorKind.EngageParty,
            false, 161, true, true);
        Check(
            !over160.Apply && !over160.RearSecurityEligible,
            "rear-security-over-160-ineligible");

        var zeroMen = Engage(
            10f, VisualWarBehaviorKind.EngageParty,
            false, 0, true, true);
        Check(
            !zeroMen.Apply,
            "rear-security-zero-men-ineligible");

        var weak = Engage(
            10f, VisualWarBehaviorKind.EngageParty,
            true, 80, true, true);
        Check(
            !weak.Apply && weak.RearSecurityWeakSkip &&
            !weak.RearSecurityEligible,
            "rear-security-weak-skip");

        var nonBandit = Engage(
            10f, VisualWarBehaviorKind.EngageParty,
            false, 80, true, false);
        Check(
            !nonBandit.Apply && Close(nonBandit.Factor, 1f),
            "non-bandit-engage-untouched");

        var nonEngage = Engage(
            10f, VisualWarBehaviorKind.Other,
            false, 80, true, true);
        Check(
            !nonEngage.Apply,
            "non-engage-mobile-party-untouched");

        var negative = Engage(
            -1f, VisualWarBehaviorKind.EngageParty,
            false, 80, true, true);
        Check(
            !negative.Apply && Close(negative.Factor, 1f),
            "negative-native-score-untouched");
    }

    private static void CheckStandaloneActivation()
    {
        string assembly =
            @"C:\Games\Bannerlord\Modules\ClanAI\bin\Win64_Shipping_Client\ClanAI.dll";
        string expected =
            @"C:\Games\Bannerlord\Modules\ClanAI\Data\ENABLE_VISUAL_WAR_LAB.txt";
        string resolved =
            VisualWarActivation.ResolveEnablePath(assembly);

        Check(
            string.Equals(
                resolved,
                expected,
                StringComparison.OrdinalIgnoreCase),
            "module-local-activation-path");

        Check(
            string.IsNullOrEmpty(
                VisualWarActivation.ResolveEnablePath(null)),
            "null-assembly-path-safe-off");

        Check(
            string.IsNullOrEmpty(
                VisualWarActivation.ResolveEnablePath(
                    @"C:\Games\ClanAI.dll")),
            "unexpected-layout-safe-off");

        Check(
            resolved != null &&
            resolved.IndexOf(
                @"D:\BannerlordAIResearch",
                StringComparison.OrdinalIgnoreCase) < 0,
            "activation-path-has-no-development-root");

        Check(
            resolved != null &&
            resolved.EndsWith(
                Path.Combine(
                    "Data",
                    VisualWarActivation.MarkerFileName),
                StringComparison.OrdinalIgnoreCase),
            "activation-marker-under-module-data");
    }

    private static int Main()
    {
        CheckWeakRules();
        CheckSettlementRules();
        CheckRearSecurityRules();
        CheckStandaloneActivation();

        Check(
            Close(VisualWarPolicy.MinimumReadiness, 0.72f) &&
            VisualWarPolicy.MinimumFoodDays == 3,
            "weak-threshold-constants");
        Check(
            Close(VisualWarPolicy.DefenseFrontierScale, 0.22f) &&
            Close(VisualWarPolicy.ActiveDefenseBonus, 0.10f) &&
            Close(VisualWarPolicy.MaxDefenseFactor, 1.35f),
            "defense-factor-constants");
        Check(
            Close(VisualWarPolicy.FrontierOffenseScale, 0.16f),
            "offense-factor-constant");
        Check(
            VisualWarPolicy.RearSecuritySmallPartyMax == 90 &&
            VisualWarPolicy.RearSecurityPartyMax == 160 &&
            Close(VisualWarPolicy.RearSecuritySmallFactor, 1.25f) &&
            Close(VisualWarPolicy.RearSecurityFactor, 1.15f),
            "rear-security-constants");

        if (_failures != 0)
        {
            Console.WriteLine(
                "FAIL VisualWar policy tests failures=" + _failures +
                " checks=" + _checks);
            return 1;
        }

        Console.WriteLine(
            "PASS VisualWar policy tests checks=" + _checks);
        Console.WriteLine(
            "defense/offense/rear-security factors and weak exclusions preserved");
        Console.WriteLine(
            "activation marker resolves module-locally and remains absent-by-default");
        return 0;
    }
}