using System;
using ClanAI;

internal static class Program
{
    private static int _checks;
    private static int _failures;

    private static void Check(bool condition, string name)
    {
        _checks++;
        if (condition)
            return;

        Console.WriteLine("FAIL " + name);
        _failures++;
    }

    private static void CreationTests()
    {
        Check(
            StrategicCommitmentCommitPolicy.ShouldCreateExpectation(
                true, true, true, true),
            "create-only-when-all-retained-apply-gates-pass");

        Check(
            !StrategicCommitmentCommitPolicy.ShouldCreateExpectation(
                false, true, true, true),
            "observe-mode-does-not-create-pending");

        Check(
            !StrategicCommitmentCommitPolicy.ShouldCreateExpectation(
                true, false, true, true),
            "factor-must-actually-be-applied");

        Check(
            !StrategicCommitmentCommitPolicy.ShouldCreateExpectation(
                true, true, false, true),
            "final-winner-must-be-previous-objective");

        Check(
            !StrategicCommitmentCommitPolicy.ShouldCreateExpectation(
                true, true, true, false),
            "retained-must-be-true");
    }

    private static void SettlementMatchTests()
    {
        var defaultMatch =
            StrategicCommitmentCommitPolicy.Evaluate(
                "PatrolAroundPoint",
                "S:castle_1",
                "PatrolAroundPoint",
                "GoToPoint",
                "S:castle_1",
                null,
                100.0,
                101.0);

        Check(defaultMatch.BehaviorMatch,
            "default-behavior-match");
        Check(defaultMatch.TargetMatch,
            "settlement-target-match");
        Check(defaultMatch.Matched &&
              defaultMatch.Remove &&
              !defaultMatch.Expired,
            "behavior-plus-target-confirms-commit");

        var shortMatch =
            StrategicCommitmentCommitPolicy.Evaluate(
                "DefendSettlement",
                "S:castle_1",
                "PatrolAroundPoint",
                "DefendSettlement",
                "S:castle_1",
                null,
                100.0,
                101.0);

        Check(shortMatch.BehaviorMatch &&
              shortMatch.TargetMatch &&
              shortMatch.Matched,
            "short-term-behavior-can-confirm");

        var wrongTarget =
            StrategicCommitmentCommitPolicy.Evaluate(
                "PatrolAroundPoint",
                "S:castle_1",
                "PatrolAroundPoint",
                "Other",
                "S:castle_2",
                null,
                100.0,
                101.0);

        Check(wrongTarget.BehaviorMatch &&
              !wrongTarget.TargetMatch &&
              !wrongTarget.Matched &&
              !wrongTarget.Remove,
            "wrong-target-remains-pending");

        var wrongBehavior =
            StrategicCommitmentCommitPolicy.Evaluate(
                "PatrolAroundPoint",
                "S:castle_1",
                "GoToSettlement",
                "Other",
                "S:castle_1",
                null,
                100.0,
                101.0);

        Check(!wrongBehavior.BehaviorMatch &&
              wrongBehavior.TargetMatch &&
              !wrongBehavior.Matched &&
              !wrongBehavior.Remove,
            "wrong-behavior-remains-pending");

        var arrived =
            StrategicCommitmentCommitPolicy.Evaluate(
                "PatrolAroundPoint",
                "S:castle_1",
                "Other",
                "Other",
                null,
                "S:castle_1",
                100.0,
                102.0);

        Check(arrived.ArrivedMatch &&
              arrived.Matched &&
              arrived.Remove,
            "settlement-arrival-confirms-commit");
    }

    private static void MobilePartyTests()
    {
        var matched =
            StrategicCommitmentCommitPolicy.Evaluate(
                "EngageParty",
                "P:enemy_1",
                "EngageParty",
                "Other",
                "P:enemy_1",
                null,
                200.0,
                201.0);

        Check(matched.BehaviorMatch &&
              matched.TargetMatch &&
              matched.Matched,
            "mobile-party-target-match");

        var wrongParty =
            StrategicCommitmentCommitPolicy.Evaluate(
                "EngageParty",
                "P:enemy_1",
                "EngageParty",
                "Other",
                "P:enemy_2",
                null,
                200.0,
                201.0);

        Check(!wrongParty.TargetMatch &&
              !wrongParty.Matched,
            "mobile-party-target-identity-required");
    }

    private static void UnsupportedPointTests()
    {
        var point =
            StrategicCommitmentCommitPolicy.Evaluate(
                "PatrolAroundPoint",
                "V:10.0,20.0,L",
                "PatrolAroundPoint",
                "Other",
                null,
                null,
                250.0,
                251.0);

        Check(point.BehaviorMatch &&
              !point.TargetMatch &&
              !point.ArrivedMatch &&
              !point.Matched &&
              !point.Remove,
            "point-target-not-falsely-confirmed");
    }

    private static void ExpiryTests()
    {
        var exact =
            StrategicCommitmentCommitPolicy.Evaluate(
                "PatrolAroundPoint",
                "S:castle_1",
                "Other",
                "Other",
                null,
                null,
                300.0,
                318.0);

        Check(!exact.Expired &&
              !exact.Remove &&
              Math.Abs(exact.AgeHours - 18.0) < 0.0001,
            "exact-18-hours-not-expired");

        var expired =
            StrategicCommitmentCommitPolicy.Evaluate(
                "PatrolAroundPoint",
                "S:castle_1",
                "Other",
                "Other",
                null,
                null,
                300.0,
                318.001);

        Check(expired.Expired &&
              !expired.Matched &&
              expired.Remove,
            "over-18-hours-expires");

        var negativeAge =
            StrategicCommitmentCommitPolicy.Evaluate(
                "PatrolAroundPoint",
                "S:castle_1",
                "Other",
                "Other",
                null,
                null,
                300.0,
                299.0);

        Check(Math.Abs(negativeAge.AgeHours) < 0.0001 &&
              !negativeAge.Expired,
            "negative-pending-age-clamped-to-zero");

        var lateMatch =
            StrategicCommitmentCommitPolicy.Evaluate(
                "PatrolAroundPoint",
                "S:castle_1",
                "PatrolAroundPoint",
                "Other",
                "S:castle_1",
                null,
                300.0,
                319.0);

        Check(lateMatch.Matched &&
              lateMatch.Expired &&
              lateMatch.Remove,
            "late-native-match-is-recorded-and-removed");

        Check(
            Math.Abs(
                StrategicCommitmentCommitPolicy.PendingLifetimeHours -
                18.0) < 0.0001,
            "pending-lifetime-is-18-hours-separate-from-eligibility-age");
    }

    private static int Main()
    {
        CreationTests();
        SettlementMatchTests();
        MobilePartyTests();
        UnsupportedPointTests();
        ExpiryTests();

        if (_failures != 0)
        {
            Console.WriteLine(
                "FAIL Strategic Commitment commit policy tests failures=" +
                _failures +
                " checks=" + _checks);
            return 1;
        }

        Console.WriteLine(
            "PASS Strategic Commitment commit policy tests checks=" +
            _checks);
        Console.WriteLine(
            "retained-Apply creation gates, native behavior/target matching, and 18-hour expiry preserved");
        return 0;
    }
}