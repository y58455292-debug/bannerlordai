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

    private static void DepletionTests()
    {
        Check(
            Phase4ARecoveryClassificationPolicy.IsSeverelyDepleted(0f),
            "zero-ratio-severe");
        Check(
            Phase4ARecoveryClassificationPolicy.IsSeverelyDepleted(0.35f),
            "exact-severe-boundary");
        Check(
            !Phase4ARecoveryClassificationPolicy.IsSeverelyDepleted(0.3501f),
            "above-severe-boundary-not-severe");
        Check(
            !Phase4ARecoveryClassificationPolicy.IsSeverelyDepleted(-0.1f),
            "negative-ratio-invalid");
        Check(
            !Phase4ARecoveryClassificationPolicy.IsSeverelyDepleted(float.NaN),
            "nan-ratio-invalid");
        Check(
            !Phase4ARecoveryClassificationPolicy.IsSeverelyDepleted(float.PositiveInfinity),
            "infinite-ratio-invalid");
        Check(
            Math.Abs(
                Phase4ARecoveryClassificationPolicy.SevereDepletionRatio -
                0.35f) < 0.0001f,
            "severe-threshold-constant");
    }

    private static void InitialTests()
    {
        Check(
            Phase4ARecoveryClassificationPolicy.ClassifyInitialPresence(
                true, false, 12) ==
            Phase4ARecoverySource.InitialPresenceBeforeSettlement,
            "created-party-troops-before-settlement");

        Check(
            Phase4ARecoveryClassificationPolicy.ClassifyInitialPresence(
                true, true, 12) ==
            Phase4ARecoverySource.None,
            "created-inside-settlement-not-pre-settlement-proof");

        Check(
            Phase4ARecoveryClassificationPolicy.ClassifyInitialPresence(
                false, false, 12) ==
            Phase4ARecoverySource.None,
            "severe-observation-does-not-pretend-respawn-source");

        Check(
            Phase4ARecoveryClassificationPolicy.ClassifyInitialPresence(
                true, false, 0) ==
            Phase4ARecoverySource.None,
            "zero-created-roster-has-no-initial-source");
    }

    private static void IncreaseTests()
    {
        Check(
            Phase4ARecoveryClassificationPolicy.ClassifyIncrease(
                0, false, false, 0, false, 0) ==
            Phase4ARecoverySource.None,
            "no-growth-no-source");

        Check(
            Phase4ARecoveryClassificationPolicy.ClassifyIncrease(
                -3, true, true, 2, true, 2) ==
            Phase4ARecoverySource.None,
            "loss-no-recovery-source");

        Check(
            Phase4ARecoveryClassificationPolicy.ClassifyIncrease(
                5, false, false, 0, false, 0) ==
            Phase4ARecoverySource.OtherOrUnknownNativeSource,
            "growth-outside-settlement-remains-unknown");

        Check(
            Phase4ARecoveryClassificationPolicy.ClassifyIncrease(
                5, true, true, 5, true, 0) ==
            Phase4ARecoverySource.SettlementRecruitmentSupported,
            "volunteer-decrease-supports-recruitment");

        Check(
            Phase4ARecoveryClassificationPolicy.ClassifyIncrease(
                5, true, true, 0, true, 5) ==
            Phase4ARecoverySource.GarrisonWithdrawalSupported,
            "garrison-decrease-supports-withdrawal");

        Check(
            Phase4ARecoveryClassificationPolicy.ClassifyIncrease(
                5, true, true, 2, true, 3) ==
            Phase4ARecoverySource.MixedSettlementSources,
            "both-pools-decrease-mixed");

        Check(
            Phase4ARecoveryClassificationPolicy.ClassifyIncrease(
                5, true, true, 0, true, 0) ==
            Phase4ARecoverySource.UnknownSettlementSource,
            "settlement-growth-without-supporting-delta-unknown");

        Check(
            Phase4ARecoveryClassificationPolicy.ClassifyIncrease(
                5, true, false, 8, false, 8) ==
            Phase4ARecoverySource.UnknownSettlementSource,
            "unobserved-pools-cannot-support-source");

        Check(
            Phase4ARecoveryClassificationPolicy.ClassifyIncrease(
                5, true, true, -2, true, -3) ==
            Phase4ARecoverySource.UnknownSettlementSource,
            "pool-growth-does-not-prove-party-source");
    }

    private static int Main()
    {
        DepletionTests();
        InitialTests();
        IncreaseTests();

        if (_failures != 0)
        {
            Console.WriteLine(
                "FAIL Phase 4A recovery policy tests failures=" +
                _failures +
                " checks=" + _checks);
            return 1;
        }

        Console.WriteLine(
            "PASS Phase 4A recovery policy tests checks=" + _checks);
        Console.WriteLine(
            "source labels require supporting volunteer/garrison evidence; unsupported growth remains unknown");
        return 0;
    }
}