using System;
using System.Reflection;
using ClanAI;

internal static class Program
{
    private static int _checks;

    private static void Check(bool condition, string name)
    {
        _checks++;
        if (!condition)
            throw new Exception("FAIL " + name);
    }

    private static bool SameBits(float a, float b)
    {
        return BitConverter.SingleToInt32Bits(a) ==
            BitConverter.SingleToInt32Bits(b);
    }

    private static LocalManpowerProbabilityResult Eval(
        float nativeProbability,
        bool empty,
        bool valid,
        LocalManpowerPopulationBand band,
        bool hasSecurity,
        float security,
        bool acute)
    {
        return LocalManpowerProbabilityPolicy.Evaluate(
            nativeProbability,
            empty,
            valid,
            band,
            hasSecurity,
            security,
            acute);
    }

    private static void Main()
    {
        var occupied = Eval(
            0.4375f, false, true,
            LocalManpowerPopulationBand.Low,
            true, 0f, true);
        Check(SameBits(occupied.FinalProbability, 0.4375f),
            "occupied slot exact native result");
        Check(!occupied.Applied, "occupied slot not applied");

        var invalid = Eval(
            0.3125f, true, false,
            LocalManpowerPopulationBand.High,
            true, 50f, false);
        Check(SameBits(invalid.FinalProbability, 0.3125f),
            "invalid context exact native result");

        var healthy = Eval(
            0.6f, true, true,
            LocalManpowerPopulationBand.High,
            true, 50f, false);
        Check(SameBits(healthy.LocalMultiplier, 1f),
            "healthy multiplier one");
        Check(SameBits(healthy.FinalProbability, 0.6f),
            "healthy context exact native probability");
        Check(!healthy.Applied, "healthy context no slowdown");

        Check(SameBits(
            LocalManpowerProbabilityPolicy.PopulationFactor(
                LocalManpowerPopulationBand.Mid),
            0.95f), "mid population factor");
        Check(SameBits(
            LocalManpowerProbabilityPolicy.PopulationFactor(
                LocalManpowerPopulationBand.Low),
            0.80f), "low population factor");
        Check(SameBits(
            LocalManpowerProbabilityPolicy.PopulationFactor(
                LocalManpowerPopulationBand.High),
            1.00f), "high population factor");

        Check(Math.Abs(
            LocalManpowerProbabilityPolicy.SecurityFactor(true, 0f) -
            0.80f) < 0.000001f, "security zero");
        Check(Math.Abs(
            LocalManpowerProbabilityPolicy.SecurityFactor(true, 25f) -
            0.90f) < 0.000001f, "security twenty five");
        Check(Math.Abs(
            LocalManpowerProbabilityPolicy.SecurityFactor(true, 50f) -
            1.00f) < 0.000001f, "security fifty");
        Check(Math.Abs(
            LocalManpowerProbabilityPolicy.SecurityFactor(true, 75f) -
            1.00f) < 0.000001f, "security above fifty");
        Check(Math.Abs(
            LocalManpowerProbabilityPolicy.SecurityFactor(false, 0f) -
            1.00f) < 0.000001f, "missing security passthrough");

        var raid = Eval(
            0.8f, true, true,
            LocalManpowerPopulationBand.High,
            true, 50f, true);
        Check(Math.Abs(raid.AcuteFactor - 0.50f) < 0.000001f,
            "active raid acute factor");
        Check(Math.Abs(raid.FinalProbability - 0.4f) < 0.000001f,
            "active raid final probability");

        var siege = Eval(
            0.8f, true, true,
            LocalManpowerPopulationBand.High,
            true, 50f, true);
        Check(Math.Abs(siege.AcuteFactor - 0.50f) < 0.000001f,
            "active siege acute factor");

        var floor = Eval(
            0.8f, true, true,
            LocalManpowerPopulationBand.Low,
            true, 0f, true);
        Check(Math.Abs(floor.LocalMultiplier - 0.35f) < 0.000001f,
            "combined multiplier floor");
        Check(floor.FinalProbability > 0f,
            "phase4b alone does not zero positive native refill");

        float[] nativeValues = { 0f, 0.01f, 0.25f, 0.5f, 1f };
        foreach (float native in nativeValues)
        {
            var r = Eval(
                native, true, true,
                LocalManpowerPopulationBand.Low,
                true, 10f, true);
            Check(r.FinalProbability <= native + 0.000001f,
                "final never exceeds native " + native);
            Check(r.FinalProbability >= 0f &&
                  r.FinalProbability <= 1f,
                "final in probability bounds " + native);
        }

        var zero = Eval(
            0f, true, true,
            LocalManpowerPopulationBand.Low,
            true, 0f, true);
        Check(SameBits(zero.FinalProbability, 0f),
            "native zero stays zero");

        var missingVillageSecurity = Eval(
            0.5f, true, true,
            LocalManpowerPopulationBand.Mid,
            false, 0f, false);
        Check(Math.Abs(missingVillageSecurity.SecurityFactor - 1f) <
              0.000001f,
            "missing village bound security factor one");
        Check(Math.Abs(missingVillageSecurity.LocalMultiplier - 0.95f) <
              0.000001f,
            "missing village security keeps population factor");

        var unsupportedBand = Eval(
            0.375f, true, true,
            LocalManpowerPopulationBand.Unknown,
            true, 0f, true);
        Check(SameBits(unsupportedBand.FinalProbability, 0.375f),
            "unknown population passthrough");

        foreach (float invalidNumber in new[]
        {
            float.NaN,
            float.PositiveInfinity,
            float.NegativeInfinity
        })
        {
            var nativeInvalid = Eval(
                invalidNumber, true, true,
                LocalManpowerPopulationBand.Low,
                false, 0f, false);
            Check(!float.IsNaN(nativeInvalid.FinalProbability) &&
                  !float.IsInfinity(nativeInvalid.FinalProbability),
                "invalid native numeric sanitized");

            var securityInvalid = Eval(
                0.4f, true, true,
                LocalManpowerPopulationBand.Low,
                true, invalidNumber, false);
            Check(!float.IsNaN(securityInvalid.FinalProbability) &&
                  !float.IsInfinity(securityInvalid.FinalProbability),
                "invalid security numeric finite output");
            Check(SameBits(securityInvalid.FinalProbability, 0.4f),
                "invalid security context native passthrough");
        }

        MethodInfo evaluate = typeof(LocalManpowerProbabilityPolicy)
            .GetMethod(
                "Evaluate",
                BindingFlags.Static | BindingFlags.NonPublic);
        Check(evaluate != null, "evaluate reflection available");
        foreach (ParameterInfo parameter in evaluate.GetParameters())
        {
            string n = parameter.Name.ToLowerInvariant();
            Check(!n.Contains("culture") &&
                  !n.Contains("troop") &&
                  !n.Contains("tier"),
                "policy excludes culture troop tier input " + n);
        }

        Console.WriteLine(
            "PASS Phase 4B local manpower probability policy checks=" +
            _checks);
    }
}
