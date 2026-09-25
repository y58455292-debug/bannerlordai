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

    private static bool Close(float a, float b, float eps = 0.0005f)
    {
        return Math.Abs(a - b) <= eps;
    }

    private static void ContextTests()
    {
        var noPrior = StrategicCommitmentPolicy.EvaluateContext(
            false, false, false, 0.0);
        Check(!noPrior.Continue && noPrior.NoPriorState,
            "no-prior-state-no-retention");

        var sameObjective = StrategicCommitmentPolicy.EvaluateContext(
            true, true, true, 4.0);
        Check(!sameObjective.Continue && sameObjective.SameObjective,
            "same-natural-objective-no-retention");
        var crossState = StrategicCommitmentPolicy.EvaluateContext(
            true, false, false, 4.0);
        Check(!crossState.Continue && crossState.CrossState,
            "cross-state-change-skips-retention");

        var negativeAge = StrategicCommitmentPolicy.EvaluateContext(
            true, false, true, -0.01);
        Check(!negativeAge.Continue && negativeAge.NegativeAge,
            "negative-age-rejected");

        var exactBoundary = StrategicCommitmentPolicy.EvaluateContext(
            true, false, true, 12.0);
        Check(exactBoundary.Continue &&
              !exactBoundary.Expired &&
              Math.Abs(exactBoundary.AgeHours - 12.0) < 0.0001,
            "exact-12-hour-boundary-allowed");

        var expired = StrategicCommitmentPolicy.EvaluateContext(
            true, false, true, 12.001);
        Check(!expired.Continue && expired.Expired,
            "over-12-hours-expired");

        Check(
            Math.Abs(StrategicCommitmentPolicy.MaxAgeHours - 12.0) < 0.0001,
            "max-age-constant-12-hours");
    }

    private static void PreviousCandidateTests()
    {
        var missing = StrategicCommitmentPolicy.EvaluateScores(
            false,
            100f,
            90f,
            StrategicCommitmentMode.Apply);
        Check(!missing.PreviousCandidateValid &&
              !missing.WouldRetain &&
              !missing.Apply,
            "previous-native-candidate-missing");
        foreach (float score in new[]
        {
            0f,
            -1f,
            float.NaN,
            float.PositiveInfinity,
            float.NegativeInfinity
        })
        {
            Check(
                !StrategicCommitmentPolicy.ValidPositiveScore(score),
                "previous-invalid-score-" + score);
        }

        Check(
            StrategicCommitmentPolicy.ValidPositiveScore(0.001f),
            "positive-score-valid");

        var zero = StrategicCommitmentPolicy.EvaluateScores(
            true,
            0f,
            1f,
            StrategicCommitmentMode.Apply);
        Check(!zero.PreviousCandidateValid,
            "previous-zero-score-no-retention");

        var negative = StrategicCommitmentPolicy.EvaluateScores(
            true,
            -5f,
            1f,
            StrategicCommitmentMode.Apply);
        Check(!negative.PreviousCandidateValid,
            "previous-negative-score-no-retention");

        var nan = StrategicCommitmentPolicy.EvaluateScores(
            true,
            float.NaN,
            1f,
            StrategicCommitmentMode.Apply);
        Check(!nan.PreviousCandidateValid,
            "previous-nan-score-no-retention");

        var infinity = StrategicCommitmentPolicy.EvaluateScores(
            true,
            float.PositiveInfinity,
            1f,
            StrategicCommitmentMode.Apply);
        Check(!infinity.PreviousCandidateValid,
            "previous-infinite-score-no-retention");
    }
    private static void NaturalScoreTests()
    {
        foreach (float natural in new[]
        {
            0f,
            -1f,
            float.NaN,
            float.PositiveInfinity,
            float.NegativeInfinity
        })
        {
            var result = StrategicCommitmentPolicy.EvaluateScores(
                true,
                100f,
                natural,
                StrategicCommitmentMode.Apply);

            Check(result.PreviousCandidateValid &&
                  !result.NaturalScoreValid &&
                  !result.WouldRetain &&
                  !result.Apply,
                "invalid-natural-score-" + natural);
        }

        var overflow = StrategicCommitmentPolicy.EvaluateScores(
            true,
            float.MaxValue,
            1f,
            StrategicCommitmentMode.Apply);
        Check(overflow.PreviousCandidateValid &&
              overflow.NaturalScoreValid &&
              !overflow.WouldRetain &&
              !overflow.Apply,
            "retained-score-infinity-not-applied");
    }

    private static void RetentionTests()
    {
        var observe = StrategicCommitmentPolicy.EvaluateScores(
            true,
            100f,
            105f,
            StrategicCommitmentMode.Observe);

        Check(observe.PreviousCandidateValid &&
              observe.NaturalScoreValid,
            "valid-same-state-previous-candidate");
        Check(Close(observe.RetainedScore, 110f),
            "retained-score-is-previous-times-1.10");
        Check(observe.WouldRetain,
            "retention-when-retained-score-exceeds-natural");
        Check(!observe.Apply,
            "observe-mode-never-applies-factor");
        Check(Close(observe.RequiredFactor, 1.05f),
            "required-factor-is-natural-over-previous");

        var apply = StrategicCommitmentPolicy.EvaluateScores(
            true,
            100f,
            105f,
            StrategicCommitmentMode.Apply);
        Check(apply.WouldRetain && apply.Apply,
            "apply-mode-allows-existing-previous-candidate-factor");

        var exact = StrategicCommitmentPolicy.EvaluateScores(
            true,
            100f,
            110f,
            StrategicCommitmentMode.Apply);
        Check(Close(exact.RetainedScore, 110f) &&
              !exact.WouldRetain &&
              !exact.Apply,
            "retained-equals-natural-no-retention");

        var above = StrategicCommitmentPolicy.EvaluateScores(
            true,
            100f,
            110.01f,
            StrategicCommitmentMode.Apply);
        Check(!above.WouldRetain && !above.Apply,
            "retained-below-natural-no-retention");

        var justBelow = StrategicCommitmentPolicy.EvaluateScores(
            true,
            100f,
            109.99f,
            StrategicCommitmentMode.Apply);
        Check(justBelow.WouldRetain && justBelow.Apply,
            "strict-greater-than-boundary-retains");

        Check(Close(StrategicCommitmentPolicy.RetentionFactor, 1.10f),
            "retention-factor-constant-1.10");
    }
    private static int Main()
    {
        ContextTests();
        PreviousCandidateTests();
        NaturalScoreTests();
        RetentionTests();

        if (_failures != 0)
        {
            Console.WriteLine(
                "FAIL Strategic Commitment policy tests failures=" +
                _failures +
                " checks=" + _checks);
            return 1;
        }

        Console.WriteLine(
            "PASS Strategic Commitment policy tests checks=" + _checks);
        Console.WriteLine(
            "same-state age/score gates, strict retention boundary, and Observe/Apply semantics preserved");
        return 0;
    }
}