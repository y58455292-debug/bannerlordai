using System;
using ClanAI;

internal static class Program
{
    private static int _checks;

    private static void Check(
        bool condition,
        string name)
    {
        _checks++;
        if (!condition)
            throw new Exception("FAIL " + name);
    }

    private static bool SameBits(
        float a,
        float b)
    {
        return BitConverter.SingleToInt32Bits(a) ==
            BitConverter.SingleToInt32Bits(b);
    }

    private static LocalBanditControlResult R(
        float nativeWeight,
        bool hasSecurity,
        float security,
        LocalBanditCandidateKind kind)
    {
        return LocalBanditControlPolicy.Evaluate(
            nativeWeight,
            hasSecurity,
            security,
            kind);
    }

    private static void Near(
        float actual,
        float expected,
        string name)
    {
        Check(
            Math.Abs(actual - expected) < 0.00001f,
            name + " actual=" + actual +
            " expected=" + expected);
    }

    private static void Main()
    {
        const float native = 0.8f;

        var unsupported = R(
            native, true, 0f,
            LocalBanditCandidateKind.Unsupported);
        Check(SameBits(unsupported.FinalWeight, native),
            "unsupported exact native passthrough");

        var missing = R(
            native, false, 0f,
            LocalBanditCandidateKind.Town);
        Check(SameBits(missing.FinalWeight, native),
            "missing security exact native passthrough");

        var s0 = R(
            native, true, 0f,
            LocalBanditCandidateKind.Town);
        Near(s0.ControlMultiplier, 1.25f,
            "town security 0 multiplier");

        var s25 = R(
            native, true, 25f,
            LocalBanditCandidateKind.Town);
        Near(s25.ControlMultiplier, 1.125f,
            "security 25 multiplier");

        var s50 = R(
            native, true, 50f,
            LocalBanditCandidateKind.Town);
        Near(s50.ControlMultiplier, 1.0f,
            "security 50 multiplier");

        var s75 = R(
            native, true, 75f,
            LocalBanditCandidateKind.Town);
        Near(s75.ControlMultiplier, 0.875f,
            "security 75 multiplier");

        var s100 = R(
            native, true, 100f,
            LocalBanditCandidateKind.Town);
        Near(s100.ControlMultiplier, 0.75f,
            "security 100 multiplier");

        var below = R(
            native, true, -50f,
            LocalBanditCandidateKind.Town);
        Near(below.BoundedSecurity, 0f,
            "security below zero clamps");
        Near(below.ControlMultiplier, 1.25f,
            "below-zero security factor");

        var above = R(
            native, true, 150f,
            LocalBanditCandidateKind.Town);
        Near(above.BoundedSecurity, 100f,
            "security above 100 clamps");
        Near(above.ControlMultiplier, 0.75f,
            "above-100 security factor");

        var zero = R(
            0f, true, 0f,
            LocalBanditCandidateKind.Town);
        Check(SameBits(zero.FinalWeight, 0f),
            "native zero remains zero");

        var scaled = R(
            2f, true, 25f,
            LocalBanditCandidateKind.Town);
        Near(scaled.FinalWeight, 2.25f,
            "positive native weight scales");

        var negative = R(
            -4f, true, 0f,
            LocalBanditCandidateKind.Town);
        Check(negative.FinalWeight >= 0f &&
              SameBits(negative.FinalWeight, 0f),
            "final result non-negative");

        var nan = R(
            float.NaN, true, 0f,
            LocalBanditCandidateKind.Town);
        Check(!float.IsNaN(nan.FinalWeight) &&
              !float.IsInfinity(nan.FinalWeight),
            "no NaN output");

        var infinity = R(
            float.PositiveInfinity, true, 0f,
            LocalBanditCandidateKind.Town);
        Check(!float.IsNaN(infinity.FinalWeight) &&
              !float.IsInfinity(infinity.FinalWeight),
            "no Infinity output");

        var secure = R(
            0.2f, true, 100f,
            LocalBanditCandidateKind.Town);
        Check(secure.FinalWeight > 0f,
            "secure positive native remains positive");

        var low = R(
            3f, true, 0f,
            LocalBanditCandidateKind.Town);
        Check(low.FinalWeight <=
              (3f * 1.25f) + 0.00001f,
            "low security never exceeds native times 1.25");

        var high = R(
            3f, true, 100f,
            LocalBanditCandidateKind.Town);
        Check(high.FinalWeight >=
              (3f * 0.75f) - 0.00001f,
            "high security never below native times 0.75");

        Check(SameBits(s50.FinalWeight, native),
            "neutral security exact native");

        var village = R(
            native, true, 25f,
            LocalBanditCandidateKind.Village);
        Check(SameBits(
                village.ControlMultiplier,
                s25.ControlMultiplier) &&
              SameBits(
                village.FinalWeight,
                s25.FinalWeight),
            "village math equals town math");

        var noInput = R(
            0.4375f, false, 100f,
            LocalBanditCandidateKind.Village);
        Check(SameBits(noInput.FinalWeight, 0.4375f),
            "no security input no effect");

        var invalidSecurity = R(
            0.3125f, true, float.NaN,
            LocalBanditCandidateKind.Town);
        Check(SameBits(
                invalidSecurity.FinalWeight,
                0.3125f),
            "non-finite security passthrough");

        var overflow = R(
            float.MaxValue, true, 0f,
            LocalBanditCandidateKind.Town);
        Check(!float.IsInfinity(overflow.FinalWeight) &&
              !float.IsNaN(overflow.FinalWeight) &&
              overflow.FinalWeight >= 0f,
            "finite native overflow stays finite");

        Console.WriteLine(
            "PASS Phase 5 local bandit control policy checks=" +
            _checks);
    }
}
