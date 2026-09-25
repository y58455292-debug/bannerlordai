using System;
using ClanAI;

internal static class KingdomObjectiveProgram
{
    private static int _failures;

    private static bool Close(float a, float b, float tolerance = 0.0005f)
    {
        return Math.Abs(a - b) <= tolerance;
    }

    private static void Check(bool condition, string name)
    {
        if (condition)
            return;

        Console.WriteLine("FAIL " + name);
        _failures++;
    }

    private static KingdomObjectiveCandidateDecision Eval(
        bool direct,
        bool staging,
        int rank,
        KingdomObjectiveBehaviorKind behavior,
        float score,
        float winner)
    {
        return KingdomObjectivePolicy.EvaluateCandidate(
            direct,
            staging,
            rank,
            behavior,
            score,
            winner);
    }

    private static void CheckRefusals()
    {
        Check(
            KingdomObjectivePolicy.RefusalReason(0.719f, 10, false) ==
                "low-readiness-or-supplies",
            "readiness-below-threshold-refuses");
        Check(
            KingdomObjectivePolicy.RefusalReason(0.72f, 2, false) ==
                "low-readiness-or-supplies",
            "food-below-threshold-refuses");
        Check(
            KingdomObjectivePolicy.RefusalReason(0.72f, 3, false) == null,
            "exact-readiness-food-threshold-allows");
        Check(
            KingdomObjectivePolicy.RefusalReason(0.90f, -1, false) == null,
            "unknown-food-does-not-refuse");
        Check(
            KingdomObjectivePolicy.RefusalReason(0.50f, 1, true) ==
                "low-readiness-or-supplies",
            "low-readiness-precedes-home-threat");
        Check(
            KingdomObjectivePolicy.RefusalReason(0.90f, 10, true) ==
                "urgent-clan-home-threat",
            "urgent-clan-home-threat-refuses");
    }

    private static void CheckDirectFactors()
    {
        var besiege = Eval(true, false, 0,
            KingdomObjectiveBehaviorKind.BesiegeSettlement, 80f, 100f);
        Check(besiege.Apply && besiege.Direct &&
              besiege.Mode == "direct" &&
              Close(besiege.BaseFactor, 1.75f),
            "direct-besiege-factor");

        var assault = Eval(true, false, 0,
            KingdomObjectiveBehaviorKind.AssaultSettlement, 80f, 100f);
        Check(assault.Apply && Close(assault.BaseFactor, 1.65f),
            "direct-assault-factor");

        var goTo = Eval(true, false, 0,
            KingdomObjectiveBehaviorKind.GoToSettlement, 80f, 100f);
        Check(goTo.Apply && Close(goTo.BaseFactor, 1.50f),
            "direct-goto-factor");

        var patrol = Eval(true, false, 0,
            KingdomObjectiveBehaviorKind.PatrolAroundPoint, 80f, 100f);
        Check(patrol.Apply && Close(patrol.BaseFactor, 1.35f),
            "direct-patrol-factor");

        var defend = Eval(true, false, 0,
            KingdomObjectiveBehaviorKind.DefendSettlement, 80f, 100f);
        Check(!defend.Apply, "direct-defend-not-actionable");
    }

    private static void CheckStagingFactors()
    {
        var first = Eval(false, true, 0,
            KingdomObjectiveBehaviorKind.GoToSettlement, 80f, 100f);
        Check(first.Apply && first.Mode == "staging-1" &&
              Close(first.BaseFactor, 1.55f),
            "staging-goto-rank0");

        var second = Eval(false, true, 1,
            KingdomObjectiveBehaviorKind.GoToSettlement, 80f, 100f);
        Check(second.Apply && second.Mode == "staging-2" &&
              Close(second.BaseFactor, 1.45f),
            "staging-goto-rank1");
        var floor = Eval(false, true, 9,
            KingdomObjectiveBehaviorKind.GoToSettlement, 80f, 100f);
        Check(floor.Apply && Close(floor.BaseFactor, 1.10f),
            "staging-rank-floor");

        var patrol = Eval(false, true, 1,
            KingdomObjectiveBehaviorKind.PatrolAroundPoint, 80f, 100f);
        Check(patrol.Apply && Close(patrol.BaseFactor, 1.30f),
            "staging-patrol-rank1");

        var defend = Eval(false, true, 2,
            KingdomObjectiveBehaviorKind.DefendSettlement, 80f, 100f);
        Check(defend.Apply && Close(defend.BaseFactor, 1.10f),
            "staging-defend-rank2");

        var besiege = Eval(false, true, 0,
            KingdomObjectiveBehaviorKind.BesiegeSettlement, 80f, 100f);
        Check(!besiege.Apply, "staging-besiege-not-actionable");

        var unrelated = Eval(false, false, 0,
            KingdomObjectiveBehaviorKind.GoToSettlement, 80f, 100f);
        Check(!unrelated.Apply && unrelated.Mode == "none",
            "non-objective-candidate-not-actionable");
    }
    private static void CheckCompetitiveRules()
    {
        var nonPositive = Eval(true, false, 0,
            KingdomObjectiveBehaviorKind.BesiegeSettlement, 0f, 100f);
        Check(!nonPositive.Apply, "non-positive-score-not-actionable");

        var below = Eval(true, false, 0,
            KingdomObjectiveBehaviorKind.GoToSettlement, 44.9f, 100f);
        Check(below.Apply && !below.Competitive &&
              !below.CanReachWinnerMargin &&
              Close(below.AppliedFactor, 1.50f) &&
              below.ProjectedScore < 100f,
            "competitive-ratio-below-threshold");

        var atThreshold = Eval(true, false, 0,
            KingdomObjectiveBehaviorKind.GoToSettlement, 45f, 100f);
        Check(atThreshold.Competitive &&
              atThreshold.CanReachWinnerMargin &&
              Close(atThreshold.CompetitiveRatio, 0.45f) &&
              Close(atThreshold.RequiredWinnerFactor, 102.5f / 45f) &&
              Close(atThreshold.AppliedFactor, 102.5f / 45f) &&
              atThreshold.AppliedFactor <=
                KingdomObjectivePolicy.MaxDirectOrderFactor &&
              atThreshold.ProjectedScore > 100f,
            "direct-competitive-threshold-and-margin");

        var stagingAtThreshold = Eval(false, true, 0,
            KingdomObjectiveBehaviorKind.GoToSettlement, 45f, 100f);
        Check(stagingAtThreshold.Competitive &&
              !stagingAtThreshold.CanReachWinnerMargin &&
              stagingAtThreshold.RequiredWinnerFactor >
                KingdomObjectivePolicy.MaxStagingOrderFactor &&
              Close(stagingAtThreshold.AppliedFactor, 1.55f) &&
              stagingAtThreshold.ProjectedScore < 100f,
            "staging-cap-blocks-margin-at-ratio-threshold");

        float stagingMaxScore =
            100f * KingdomObjectivePolicy.WinnerMargin /
            KingdomObjectivePolicy.MaxStagingOrderFactor;
        var stagingAtMax = Eval(false, true, 0,
            KingdomObjectiveBehaviorKind.GoToSettlement,
            stagingMaxScore,
            100f);
        Check(stagingAtMax.Competitive &&
              stagingAtMax.CanReachWinnerMargin &&
              Close(stagingAtMax.RequiredWinnerFactor,
                    KingdomObjectivePolicy.MaxStagingOrderFactor) &&
              stagingAtMax.AppliedFactor <=
                KingdomObjectivePolicy.MaxStagingOrderFactor + 0.0005f &&
              stagingAtMax.ProjectedScore >= 102.49f,
            "staging-max-factor-bound");

        var escalated = Eval(true, false, 0,
            KingdomObjectiveBehaviorKind.GoToSettlement, 50f, 100f);
        Check(escalated.Competitive &&
              escalated.CanReachWinnerMargin &&
              Close(escalated.RequiredWinnerFactor, 2.05f) &&
              Close(escalated.AppliedFactor, 2.05f) &&
              escalated.AppliedFactor <=
                KingdomObjectivePolicy.MaxDirectOrderFactor &&
              Close(escalated.ProjectedScore, 102.5f),
            "required-winner-factor-escalation");

        var baseAlreadyWins = Eval(true, false, 0,
            KingdomObjectiveBehaviorKind.BesiegeSettlement, 70f, 100f);
        Check(baseAlreadyWins.CanReachWinnerMargin &&
              baseAlreadyWins.RequiredWinnerFactor <
                baseAlreadyWins.BaseFactor &&
              Close(baseAlreadyWins.AppliedFactor, 1.75f) &&
              Close(baseAlreadyWins.ProjectedScore, 122.5f),
            "base-factor-kept-when-already-above-margin");

        var noPriorPositiveWinner = Eval(true, false, 0,
            KingdomObjectiveBehaviorKind.GoToSettlement, 10f, 0f);
        Check(noPriorPositiveWinner.Competitive &&
              noPriorPositiveWinner.CanReachWinnerMargin &&
              Close(noPriorPositiveWinner.RequiredWinnerFactor, 1.50f) &&
              Close(noPriorPositiveWinner.AppliedFactor, 1.50f),
            "non-positive-prior-winner-uses-base-factor");
    }
    private static void CheckWinnerCommitConditions()
    {
        Check(
            KingdomObjectivePolicy.ShouldCreatePendingCommit(true, true, true),
            "pending-commit-all-conditions");
        Check(
            !KingdomObjectivePolicy.ShouldCreatePendingCommit(false, true, true),
            "pending-requires-winner-change");
        Check(
            !KingdomObjectivePolicy.ShouldCreatePendingCommit(true, false, true),
            "pending-requires-objective-winner");
        Check(
            !KingdomObjectivePolicy.ShouldCreatePendingCommit(true, true, false),
            "pending-requires-winning-settlement");
    }

    private static int Main()
    {
        CheckRefusals();
        CheckDirectFactors();
        CheckStagingFactors();
        CheckCompetitiveRules();
        CheckWinnerCommitConditions();

        Check(Close(KingdomObjectivePolicy.MaxDirectOrderFactor, 2.50f),
            "max-direct-factor-constant");
        Check(Close(KingdomObjectivePolicy.MaxStagingOrderFactor, 2.25f),
            "max-staging-factor-constant");
        Check(Close(KingdomObjectivePolicy.MinimumCompetitiveRatio, 0.45f),
            "competitive-ratio-constant");
        Check(Close(KingdomObjectivePolicy.WinnerMargin, 1.025f),
            "winner-margin-constant");

        if (_failures != 0)
        {
            Console.WriteLine(
                "FAIL KingdomObjective policy tests failures=" + _failures);
            return 1;
        }

        Console.WriteLine("PASS KingdomObjective policy tests cases=34");
        Console.WriteLine(
            "refusals: readiness/food then urgent clan-home threat");
        Console.WriteLine(
            "native candidate factors: direct/staging with bounded winner escalation");
        Console.WriteLine(
            "pending commit requires winnerChanged + objectiveWon + settlement");
        return 0;
    }
}