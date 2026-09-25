using System;
using ClanAI;

internal static class VisualWarCommitProgram
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

    private static VisualWarCommitExpectation NewExpectation(
        VisualWarBehaviorKind behavior,
        string targetKey,
        string targetName,
        string reason,
        double createdAt)
    {
        return VisualWarCommitPolicy.CreateExpectation(
            "party_test",
            "Test Actor",
            behavior,
            targetKey,
            targetName,
            reason,
            createdAt);
    }
    private static void CheckCreation()
    {
        var pending = NewExpectation(
            VisualWarBehaviorKind.GoToSettlement,
            "S:castle_1",
            "Castle One",
            "frontier-defense",
            100.0);

        Check(pending != null, "valid-expectation-created");
        Check(pending.PartyId == "party_test", "party-id-recorded");
        Check(pending.ActorName == "Test Actor", "actor-name-recorded");
        Check(
            pending.ExpectedBehavior ==
                VisualWarBehaviorKind.GoToSettlement,
            "expected-behavior-recorded");
        Check(
            pending.ExpectedTargetKey == "S:castle_1" &&
            pending.ExpectedTargetName == "Castle One",
            "expected-target-identity-recorded");
        Check(
            pending.Reason == "frontier-defense" &&
            Math.Abs(pending.CreatedAtHours - 100.0) < 0.0001,
            "reason-and-hour-recorded");

        Check(
            VisualWarCommitPolicy.CreateExpectation(
                null,
                "Actor",
                VisualWarBehaviorKind.GoToSettlement,
                "S:x",
                "X",
                "active-defense",
                0.0) == null,
            "missing-party-rejected");
        Check(
            VisualWarCommitPolicy.CreateExpectation(
                "p",
                "Actor",
                VisualWarBehaviorKind.Other,
                "S:x",
                "X",
                "active-defense",
                0.0) == null,
            "other-behavior-rejected");
        Check(
            VisualWarCommitPolicy.CreateExpectation(
                "p",
                "Actor",
                VisualWarBehaviorKind.GoToSettlement,
                null,
                null,
                "active-defense",
                0.0) == null,
            "missing-target-rejected");
        Check(
            VisualWarCommitPolicy.CreateExpectation(
                "p",
                "Actor",
                VisualWarBehaviorKind.GoToSettlement,
                "S:x",
                "X",
                null,
                0.0) == null,
            "missing-reason-rejected");
    }

    private static void CheckBehaviorTargetMatches()
    {
        var pending = NewExpectation(
            VisualWarBehaviorKind.DefendSettlement,
            "S:castle_1",
            "Castle One",
            "active-defense",
            50.0);
        var viaDefault = VisualWarCommitPolicy.Evaluate(
            pending,
            VisualWarBehaviorKind.DefendSettlement,
            VisualWarBehaviorKind.Other,
            "S:castle_1",
            null,
            51.0);
        Check(
            viaDefault.BehaviorMatch &&
            viaDefault.TargetMatch &&
            viaDefault.Matched &&
            viaDefault.Remove &&
            !viaDefault.Expired,
            "default-behavior-target-match");

        var viaShort = VisualWarCommitPolicy.Evaluate(
            pending,
            VisualWarBehaviorKind.Other,
            VisualWarBehaviorKind.DefendSettlement,
            "S:castle_1",
            null,
            51.0);
        Check(
            viaShort.BehaviorMatch &&
            viaShort.TargetMatch &&
            viaShort.Matched,
            "short-term-behavior-target-match");

        var wrongTarget = VisualWarCommitPolicy.Evaluate(
            pending,
            VisualWarBehaviorKind.DefendSettlement,
            VisualWarBehaviorKind.Other,
            "S:castle_2",
            null,
            51.0);
        Check(
            wrongTarget.BehaviorMatch &&
            !wrongTarget.TargetMatch &&
            !wrongTarget.Matched &&
            !wrongTarget.Remove,
            "wrong-target-retained-before-expiry");
        var wrongBehavior = VisualWarCommitPolicy.Evaluate(
            pending,
            VisualWarBehaviorKind.PatrolAroundPoint,
            VisualWarBehaviorKind.Other,
            "S:castle_1",
            null,
            51.0);
        Check(
            !wrongBehavior.BehaviorMatch &&
            wrongBehavior.TargetMatch &&
            !wrongBehavior.Matched &&
            !wrongBehavior.Remove,
            "wrong-behavior-retained-before-expiry");

        var arrived = VisualWarCommitPolicy.Evaluate(
            pending,
            VisualWarBehaviorKind.Other,
            VisualWarBehaviorKind.Other,
            null,
            "S:castle_1",
            52.0);
        Check(
            !arrived.BehaviorMatch &&
            arrived.ArrivedMatch &&
            arrived.Matched &&
            arrived.Remove,
            "arrival-confirms-settlement-commit");
    }

    private static void CheckMobilePartyMatch()
    {
        var pending = NewExpectation(
            VisualWarBehaviorKind.EngageParty,
            "P:bandit_1",
            "Bandit Party",
            "rear-security",
            200.0);

        var matched = VisualWarCommitPolicy.Evaluate(
            pending,
            VisualWarBehaviorKind.EngageParty,
            VisualWarBehaviorKind.Other,
            "P:bandit_1",
            null,
            201.0);
        Check(
            matched.BehaviorMatch &&
            matched.TargetMatch &&
            matched.Matched &&
            matched.Remove,
            "mobile-party-target-match");

        var nonBanditTarget = VisualWarCommitPolicy.Evaluate(
            pending,
            VisualWarBehaviorKind.EngageParty,
            VisualWarBehaviorKind.Other,
            "P:other_party",
            null,
            201.0);
        Check(
            !nonBanditTarget.TargetMatch &&
            !nonBanditTarget.Matched,
            "mobile-party-target-identity-required");
    }

    private static void CheckExpiry()
    {
        var pending = NewExpectation(
            VisualWarBehaviorKind.PatrolAroundPoint,
            "S:town_1",
            "Town One",
            "frontier-defense",
            300.0);

        var exactBoundary = VisualWarCommitPolicy.Evaluate(
            pending,
            VisualWarBehaviorKind.Other,
            VisualWarBehaviorKind.Other,
            null,
            null,
            318.0);
        Check(
            !exactBoundary.Expired &&
            !exactBoundary.Remove &&
            Math.Abs(exactBoundary.AgeHours - 18.0) < 0.0001,
            "exact-18-hours-not-expired");

        var expired = VisualWarCommitPolicy.Evaluate(
            pending,
            VisualWarBehaviorKind.Other,
            VisualWarBehaviorKind.Other,
            null,
            null,
            318.001);
        Check(
            expired.Expired &&
            !expired.Matched &&
            expired.Remove,
            "over-18-hours-expires");

        var negativeAge = VisualWarCommitPolicy.Evaluate(
            pending,
            VisualWarBehaviorKind.Other,
            VisualWarBehaviorKind.Other,
            null,
            null,
            299.0);
        Check(
            Math.Abs(negativeAge.AgeHours) < 0.0001 &&
            !negativeAge.Expired,
            "negative-age-clamped-to-zero");

        var matchedAfterBoundary = VisualWarCommitPolicy.Evaluate(
            pending,
            VisualWarBehaviorKind.PatrolAroundPoint,
            VisualWarBehaviorKind.Other,
            "S:town_1",
            null,
            319.0);
        Check(
            matchedAfterBoundary.Matched &&
            matchedAfterBoundary.Expired &&
            matchedAfterBoundary.Remove,
            "late-native-match-still-recorded-and-removed");
    }

    private static int Main()
    {
        CheckCreation();
        CheckBehaviorTargetMatches();
        CheckMobilePartyMatch();
        CheckExpiry();

        Check(
            Math.Abs(
                VisualWarCommitPolicy.PendingLifetimeHours - 18.0) <
                0.0001,
            "expiry-constant-18-hours");
        if (_failures != 0)
        {
            Console.WriteLine(
                "FAIL VisualWar commit policy tests failures=" +
                _failures +
                " checks=" + _checks);
            return 1;
        }

        Console.WriteLine(
            "PASS VisualWar commit policy tests checks=" + _checks);
        Console.WriteLine(
            "creation, native behavior/target matching, arrival matching, and expiry preserved");
        return 0;
    }
}