namespace ClanAI
{
    internal sealed class TroopQualityProbabilityResult
    {
        internal float NativeProbability;
        internal float PopulationFactor;
        internal float SecurityFactor;
        internal float AcuteFactor;
        internal float QualityMultiplier;
        internal float FinalProbability;
        internal bool Applied;
        internal string Reason;
    }

    // Pure Phase 4C-v1 first-gate policy. Bannerlord remains authoritative
    // over notable power, current tier, UpgradeTargets, RNG and slot mutation.
    internal static class TroopQualityProbabilityPolicy
    {
        internal const float MinimumQualityMultiplier = 0.50f;
        internal const float MaximumQualityMultiplier = 1.00f;

        internal static bool IsNativeUpgradeEligible(
            bool slotOccupied,
            bool hasUpgradeTargets,
            int currentTier,
            int maxVolunteerTier)
        {
            return slotOccupied &&
                hasUpgradeTargets &&
                currentTier < maxVolunteerTier;
        }

        internal static bool ShouldApplyToOccupiedSlot(
            bool slotKnown,
            bool slotIsEmpty,
            bool nativeUpgradeEligible)
        {
            return slotKnown &&
                !slotIsEmpty &&
                nativeUpgradeEligible;
        }

        internal static TroopQualityProbabilityResult Evaluate(
            float nativeProbability,
            bool contextValid,
            LocalManpowerPopulationBand populationBand,
            bool hasSecurity,
            float security,
            bool acuteDisruption)
        {
            float safeNative =
                LocalManpowerProbabilityPolicy
                    .SanitizeProbability(nativeProbability);

            var result = new TroopQualityProbabilityResult
            {
                NativeProbability = safeNative,
                PopulationFactor = 1.0f,
                SecurityFactor = 1.0f,
                AcuteFactor = 1.0f,
                QualityMultiplier = 1.0f,
                FinalProbability = safeNative,
                Applied = false,
                Reason = "native-passthrough"
            };

            if (!contextValid ||
                populationBand ==
                    LocalManpowerPopulationBand.Unknown)
            {
                result.Reason =
                    "invalid-or-unsupported-context";
                return result;
            }

            if (hasSecurity &&
                !LocalManpowerProbabilityPolicy
                    .IsFinite(security))
            {
                result.Reason =
                    "invalid-security-context";
                return result;
            }

            result.PopulationFactor =
                LocalManpowerProbabilityPolicy
                    .PopulationFactor(populationBand);

            result.SecurityFactor =
                LocalManpowerProbabilityPolicy
                    .SecurityFactor(
                        hasSecurity,
                        security);

            result.AcuteFactor =
                acuteDisruption
                    ? LocalManpowerProbabilityPolicy
                        .AcuteDisruptionFactor
                    : 1.0f;

            result.QualityMultiplier = Clamp(
                result.PopulationFactor *
                result.SecurityFactor *
                result.AcuteFactor,
                MinimumQualityMultiplier,
                MaximumQualityMultiplier);

            result.FinalProbability =
                LocalManpowerProbabilityPolicy
                    .SanitizeProbability(
                        safeNative *
                        result.QualityMultiplier);

            result.Applied =
                result.QualityMultiplier <
                    MaximumQualityMultiplier;

            result.Reason = result.Applied
                ? "troop-quality-slowdown"
                : "healthy-native-quality-rate";

            return result;
        }

        private static float Clamp(
            float value,
            float minimum,
            float maximum)
        {
            if (value < minimum)
                return minimum;
            if (value > maximum)
                return maximum;
            return value;
        }
    }
}
