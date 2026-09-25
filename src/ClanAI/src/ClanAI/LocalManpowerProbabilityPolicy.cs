using System;

namespace ClanAI
{
    internal enum LocalManpowerPopulationBand
    {
        Unknown = 0,
        Low = 1,
        Mid = 2,
        High = 3
    }

    internal sealed class LocalManpowerProbabilityResult
    {
        internal float NativeProbability;
        internal float PopulationFactor;
        internal float SecurityFactor;
        internal float AcuteFactor;
        internal float LocalMultiplier;
        internal float FinalProbability;
        internal bool Applied;
        internal string Reason;
    }

    // Pure Phase 4B-v1 probability policy. This class has no Bannerlord types
    // and never mutates volunteer slots, rosters, settlements, or world state.
    internal static class LocalManpowerProbabilityPolicy
    {
        internal const float HighPopulationFactor = 1.00f;
        internal const float MidPopulationFactor = 0.95f;
        internal const float LowPopulationFactor = 0.80f;
        internal const float MinimumSecurityFactor = 0.80f;
        internal const float FullSecurityThreshold = 50.0f;
        internal const float AcuteDisruptionFactor = 0.50f;
        internal const float MinimumLocalMultiplier = 0.35f;
        internal const float MaximumLocalMultiplier = 1.00f;

        internal static LocalManpowerProbabilityResult Evaluate(
            float nativeProbability,
            bool slotIsEmpty,
            bool contextValid,
            LocalManpowerPopulationBand populationBand,
            bool hasSecurity,
            float security,
            bool acuteDisruption)
        {
            float safeNative = SanitizeProbability(nativeProbability);

            var result = new LocalManpowerProbabilityResult
            {
                NativeProbability = safeNative,
                PopulationFactor = 1.0f,
                SecurityFactor = 1.0f,
                AcuteFactor = 1.0f,
                LocalMultiplier = 1.0f,
                FinalProbability = safeNative,
                Applied = false,
                Reason = "native-passthrough"
            };

            if (!slotIsEmpty)
            {
                result.Reason = "occupied-slot-native-passthrough";
                return result;
            }

            if (!contextValid ||
                populationBand == LocalManpowerPopulationBand.Unknown)
            {
                result.Reason = "invalid-or-unsupported-context";
                return result;
            }

            if (hasSecurity && !IsFinite(security))
            {
                result.Reason = "invalid-security-context";
                return result;
            }

            result.PopulationFactor = PopulationFactor(populationBand);
            result.SecurityFactor = SecurityFactor(hasSecurity, security);
            result.AcuteFactor = acuteDisruption
                ? AcuteDisruptionFactor
                : 1.0f;

            result.LocalMultiplier = Clamp(
                result.PopulationFactor *
                result.SecurityFactor *
                result.AcuteFactor,
                MinimumLocalMultiplier,
                MaximumLocalMultiplier);

            result.FinalProbability = Clamp(
                safeNative * result.LocalMultiplier,
                0.0f,
                1.0f);

            result.Applied =
                result.LocalMultiplier < MaximumLocalMultiplier;
            result.Reason = result.Applied
                ? "local-manpower-slowdown"
                : "healthy-native-rate";
            return result;
        }

        internal static float PopulationFactor(
            LocalManpowerPopulationBand band)
        {
            switch (band)
            {
                case LocalManpowerPopulationBand.High:
                    return HighPopulationFactor;
                case LocalManpowerPopulationBand.Mid:
                    return MidPopulationFactor;
                case LocalManpowerPopulationBand.Low:
                    return LowPopulationFactor;
                default:
                    return 1.0f;
            }
        }

        internal static float SecurityFactor(
            bool hasSecurity,
            float security)
        {
            if (!hasSecurity || !IsFinite(security))
                return 1.0f;

            float boundedSecurity = Clamp(
                security,
                0.0f,
                FullSecurityThreshold);

            return Clamp(
                MinimumSecurityFactor +
                (1.0f - MinimumSecurityFactor) *
                boundedSecurity /
                FullSecurityThreshold,
                MinimumSecurityFactor,
                1.0f);
        }

        internal static float SanitizeProbability(
            float value)
        {
            if (float.IsNaN(value) ||
                float.IsNegativeInfinity(value))
            {
                return 0.0f;
            }

            if (float.IsPositiveInfinity(value))
                return 1.0f;

            return Clamp(value, 0.0f, 1.0f);
        }

        internal static bool IsFinite(float value)
        {
            return !float.IsNaN(value) &&
                !float.IsInfinity(value);
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
