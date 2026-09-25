using System;
using ClanAI;

internal static class Program
{
    private static int _failures;

    private static void ExpectNoChange(string name, HomeResponsibilityPolicyResult actual)
    {
        if (actual.Apply || Math.Abs(actual.Factor - 1f) > 0.0001f || actual.Reason != null)
        {
            Console.WriteLine("FAIL " + name + " expected=no-change");
            _failures++;
        }
    }

    private static void Expect(
        string name,
        HomeResponsibilityPolicyResult actual,
        float factor,
        string reason)
    {
        if (!actual.Apply ||
            Math.Abs(actual.Factor - factor) > 0.0001f ||
            !string.Equals(actual.Reason, reason, StringComparison.Ordinal))
        {
            Console.WriteLine(
                "FAIL " + name +
                " factor=" + actual.Factor +
                " reason=" + (actual.Reason ?? "<null>"));
            _failures++;
        }
    }

    private static HomeResponsibilityPolicyResult Eval(
        bool eligible,
        bool owned,
        float score,
        bool siege,
        bool raid,
        bool weak,
        HomeResponsibilityBehaviorKind behavior)
    {
        return HomeResponsibilityPolicy.Evaluate(
            eligible,
            owned,
            score,
            siege,
            raid,
            weak,
            behavior);
    }

    private static int Main()
    {
        ExpectNoChange(
            "actor-not-eligible-at-war",
            Eval(false, true, 10f, false, false, false,
                HomeResponsibilityBehaviorKind.GoToSettlement));
        ExpectNoChange(
            "foreign-settlement",
            Eval(true, false, 10f, false, false, false,
                HomeResponsibilityBehaviorKind.DefendSettlement));
        ExpectNoChange(
            "non-positive-native-score",
            Eval(true, true, 0f, true, false, false,
                HomeResponsibilityBehaviorKind.GoToSettlement));

        Expect(
            "siege-precedes-weak-recovery",
            Eval(true, true, 10f, true, false, true,
                HomeResponsibilityBehaviorKind.GoToSettlement),
            1.60f,
            "home-under-siege");
        Expect(
            "raid-threat",
            Eval(true, true, 10f, false, true, false,
                HomeResponsibilityBehaviorKind.Other),
            1.60f,
            "home-under-raid");
        Expect(
            "weak-recovery",
            Eval(true, true, 10f, false, false, true,
                HomeResponsibilityBehaviorKind.GoToSettlement),
            1.35f,
            "recover-at-home");
        Expect(
            "defend-home",
            Eval(true, true, 10f, false, false, false,
                HomeResponsibilityBehaviorKind.DefendSettlement),
            1.30f,
            "defend-home-at-war");
        Expect(
            "patrol-home",
            Eval(true, true, 10f, false, false, false,
                HomeResponsibilityBehaviorKind.PatrolAroundPoint),
            1.25f,
            "patrol-home-at-war");
        Expect(
            "ordinary-home",
            Eval(true, true, 10f, false, false, false,
                HomeResponsibilityBehaviorKind.GoToSettlement),
            1.15f,
            "stay-near-home-at-war");
        ExpectNoChange(
            "unhandled-native-behavior",
            Eval(true, true, 10f, false, false, false,
                HomeResponsibilityBehaviorKind.Other));

        if (_failures != 0)
        {
            Console.WriteLine("FAIL HomeResponsibility policy tests failures=" + _failures);
            return 1;
        }

        Console.WriteLine("PASS HomeResponsibility policy tests cases=10");
        Console.WriteLine("native-candidate eligibility preserved: actor-war, ownership, positive score");
        Console.WriteLine("factors: threat=1.60 recovery=1.35 defend=1.30 patrol=1.25 home=1.15");
        return 0;
    }
}