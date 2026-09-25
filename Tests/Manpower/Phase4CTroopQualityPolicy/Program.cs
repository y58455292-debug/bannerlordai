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

    private static LocalManpowerProbabilityResult LM(
        float nativeProbability,
        bool valid,
        LocalManpowerPopulationBand band,
        bool hasSecurity,
        float security,
        bool acute)
    {
        return LocalManpowerProbabilityPolicy.Evaluate(
            nativeProbability,
            true,
            valid,
            band,
            hasSecurity,
            security,
            acute);
    }

    private static TroopQualityProbabilityResult Q(
        float nativeProbability,
        bool valid,
        LocalManpowerPopulationBand band,
        bool hasSecurity,
        float security,
        bool acute)
    {
        return TroopQualityProbabilityPolicy.Evaluate(
            nativeProbability,
            valid,
            band,
            hasSecurity,
            security,
            acute);
    }

    private static void Main()
    {
        // Phase 4B preservation.
        var lmHealthy = LM(
            0.8f, true,
            LocalManpowerPopulationBand.High,
            true, 75f, false);
        Check(SameBits(lmHealthy.FinalProbability, 0.8f),
            "phase4b healthy high exact native");

        var lmMid = LM(
            0.8f, true,
            LocalManpowerPopulationBand.Mid,
            true, 75f, false);
        Check(Math.Abs(lmMid.LocalMultiplier - 0.95f) < 0.000001f,
            "phase4b mid multiplier unchanged");
        Check(Math.Abs(lmMid.FinalProbability - 0.76f) < 0.000001f,
            "phase4b mid result unchanged");

        var lmLow = LM(
            0.8f, true,
            LocalManpowerPopulationBand.Low,
            true, 75f, false);
        Check(Math.Abs(lmLow.LocalMultiplier - 0.80f) < 0.000001f,
            "phase4b low multiplier unchanged");
        Check(Math.Abs(lmLow.FinalProbability - 0.64f) < 0.000001f,
            "phase4b low result unchanged");

        var lmAcuteFloor = LM(
            0.8f, true,
            LocalManpowerPopulationBand.Low,
            true, 0f, true);
        Check(Math.Abs(lmAcuteFloor.LocalMultiplier - 0.35f) < 0.000001f,
            "phase4b disruption floor unchanged");
        Check(Math.Abs(lmAcuteFloor.FinalProbability - 0.28f) < 0.000001f,
            "phase4b disruption result unchanged");
        Check(SameBits(
            LocalManpowerProbabilityPolicy.MinimumLocalMultiplier,
            0.35f), "phase4b minimum remains 0.35");

        var lmInvalid = LM(
            0.3125f, false,
            LocalManpowerPopulationBand.High,
            true, 50f, false);
        Check(SameBits(lmInvalid.FinalProbability, 0.3125f),
            "phase4b invalid context passthrough unchanged");

        // Native upgrade eligibility mirrors only native occupied-slot conditions.
        Check(!TroopQualityProbabilityPolicy.IsNativeUpgradeEligible(
            false, true, 1, 4),
            "empty/nonoccupied not quality eligible");
        Check(!TroopQualityProbabilityPolicy.IsNativeUpgradeEligible(
            true, false, 1, 4),
            "no upgrade targets not quality eligible");
        Check(!TroopQualityProbabilityPolicy.IsNativeUpgradeEligible(
            true, true, 4, 4),
            "at max volunteer tier not quality eligible");
        Check(!TroopQualityProbabilityPolicy.IsNativeUpgradeEligible(
            true, true, 5, 4),
            "above max volunteer tier not quality eligible");
        Check(TroopQualityProbabilityPolicy.IsNativeUpgradeEligible(
            true, true, 3, 4),
            "occupied direct-target troop below max eligible");

        Check(!TroopQualityProbabilityPolicy.ShouldApplyToOccupiedSlot(
            false, false, true),
            "unknown slot does not use quality");
        Check(!TroopQualityProbabilityPolicy.ShouldApplyToOccupiedSlot(
            true, true, true),
            "empty slot does not use quality");
        Check(!TroopQualityProbabilityPolicy.ShouldApplyToOccupiedSlot(
            true, false, false),
            "occupied noneligible bypasses quality");
        Check(TroopQualityProbabilityPolicy.ShouldApplyToOccupiedSlot(
            true, false, true),
            "occupied eligible uses quality");

        // Phase 4C quality policy.
        var healthy = Q(
            0.6f, true,
            LocalManpowerPopulationBand.High,
            true, 50f, false);
        Check(SameBits(healthy.QualityMultiplier, 1f),
            "quality healthy multiplier one");
        Check(SameBits(healthy.FinalProbability, 0.6f),
            "quality healthy exact native");
        Check(!healthy.Applied,
            "quality healthy not applied");

        var mid = Q(
            0.8f, true,
            LocalManpowerPopulationBand.Mid,
            true, 50f, false);
        Check(Math.Abs(mid.PopulationFactor - 0.95f) < 0.000001f,
            "quality mid population factor");
        Check(Math.Abs(mid.QualityMultiplier - 0.95f) < 0.000001f,
            "quality mid multiplier");
        Check(Math.Abs(mid.FinalProbability - 0.76f) < 0.000001f,
            "quality mid result");

        var low = Q(
            0.8f, true,
            LocalManpowerPopulationBand.Low,
            true, 50f, false);
        Check(Math.Abs(low.PopulationFactor - 0.80f) < 0.000001f,
            "quality low population factor");
        Check(Math.Abs(low.QualityMultiplier - 0.80f) < 0.000001f,
            "quality low multiplier");
        Check(Math.Abs(low.FinalProbability - 0.64f) < 0.000001f,
            "quality low result");

        var security0 = Q(
            0.8f, true,
            LocalManpowerPopulationBand.High,
            true, 0f, false);
        Check(Math.Abs(security0.SecurityFactor - 0.80f) < 0.000001f,
            "quality security zero");

        var security25 = Q(
            0.8f, true,
            LocalManpowerPopulationBand.High,
            true, 25f, false);
        Check(Math.Abs(security25.SecurityFactor - 0.90f) < 0.000001f,
            "quality security twenty five");

        var security50 = Q(
            0.8f, true,
            LocalManpowerPopulationBand.High,
            true, 50f, false);
        Check(Math.Abs(security50.SecurityFactor - 1.00f) < 0.000001f,
            "quality security fifty");

        var security100 = Q(
            0.8f, true,
            LocalManpowerPopulationBand.High,
            true, 100f, false);
        Check(Math.Abs(security100.SecurityFactor - 1.00f) < 0.000001f,
            "quality security above fifty");

        var raid = Q(
            0.8f, true,
            LocalManpowerPopulationBand.High,
            true, 50f, true);
        Check(Math.Abs(raid.AcuteFactor - 0.50f) < 0.000001f,
            "quality raid acute factor");
        Check(Math.Abs(raid.FinalProbability - 0.4f) < 0.000001f,
            "quality raid result");

        var siege = Q(
            0.8f, true,
            LocalManpowerPopulationBand.High,
            true, 50f, true);
        Check(Math.Abs(siege.AcuteFactor - 0.50f) < 0.000001f,
            "quality siege acute factor");

        var floor = Q(
            0.8f, true,
            LocalManpowerPopulationBand.Low,
            true, 0f, true);
        Check(Math.Abs(floor.QualityMultiplier - 0.50f) < 0.000001f,
            "quality combined floor");
        Check(Math.Abs(floor.FinalProbability - 0.40f) < 0.000001f,
            "quality floor result");
        Check(SameBits(
            TroopQualityProbabilityPolicy.MinimumQualityMultiplier,
            0.50f), "quality minimum is 0.50");
        Check(floor.FinalProbability > 0f,
            "positive native quality remains positive");

        var missingSecurity = Q(
            0.8f, true,
            LocalManpowerPopulationBand.Mid,
            false, 0f, false);
        Check(Math.Abs(missingSecurity.SecurityFactor - 1f) < 0.000001f,
            "quality missing security factor one");
        Check(Math.Abs(missingSecurity.QualityMultiplier - 0.95f) < 0.000001f,
            "quality missing security preserves population factor");

        var invalidContext = Q(
            0.375f, false,
            LocalManpowerPopulationBand.High,
            true, 50f, true);
        Check(SameBits(invalidContext.FinalProbability, 0.375f),
            "quality invalid context native passthrough");

        var unknownBand = Q(
            0.4375f, true,
            LocalManpowerPopulationBand.Unknown,
            true, 0f, true);
        Check(SameBits(unknownBand.FinalProbability, 0.4375f),
            "quality unknown population native passthrough");

        float[] nativeValues = { 0f, 0.01f, 0.25f, 0.5f, 1f };
        foreach (float native in nativeValues)
        {
            var r = Q(
                native, true,
                LocalManpowerPopulationBand.Low,
                true, 10f, true);
            Check(r.FinalProbability <= native + 0.000001f,
                "quality result never exceeds native " + native);
            Check(r.FinalProbability >= 0f &&
                  r.FinalProbability <= 1f,
                "quality result in bounds " + native);
        }

        var zero = Q(
            0f, true,
            LocalManpowerPopulationBand.Low,
            true, 0f, true);
        Check(SameBits(zero.FinalProbability, 0f),
            "quality native zero remains zero");

        foreach (float invalidNumber in new[]
        {
            float.NaN,
            float.PositiveInfinity,
            float.NegativeInfinity
        })
        {
            var nativeInvalid = Q(
                invalidNumber, true,
                LocalManpowerPopulationBand.Low,
                false, 0f, false);
            Check(!float.IsNaN(nativeInvalid.FinalProbability) &&
                  !float.IsInfinity(nativeInvalid.FinalProbability),
                "quality invalid native numeric finite output");

            var securityInvalid = Q(
                0.4f, true,
                LocalManpowerPopulationBand.Low,
                true, invalidNumber, false);
            Check(!float.IsNaN(securityInvalid.FinalProbability) &&
                  !float.IsInfinity(securityInvalid.FinalProbability),
                "quality invalid security finite output");
            Check(SameBits(securityInvalid.FinalProbability, 0.4f),
                "quality invalid security passthrough");
        }

        MethodInfo evaluate = typeof(TroopQualityProbabilityPolicy)
            .GetMethod(
                "Evaluate",
                BindingFlags.Static | BindingFlags.NonPublic);
        Check(evaluate != null,
            "quality evaluate reflection available");
        foreach (ParameterInfo parameter in evaluate.GetParameters())
        {
            string n = parameter.Name.ToLowerInvariant();
            Check(!n.Contains("power") &&
                  !n.Contains("tier") &&
                  !n.Contains("culture") &&
                  !n.Contains("troop") &&
                  !n.Contains("tree") &&
                  !n.Contains("target"),
                "quality multiplier excludes native quality input " + n);
        }

        Console.WriteLine(
            "PASS Phase 4C troop quality probability policy checks=" +
            _checks);
    }
}
