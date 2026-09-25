using System;

namespace ClanAI
{
    internal enum LocalBanditCandidateKind
    {
        Unsupported = 0,
        Town = 1,
        Village = 2
    }

    internal readonly struct LocalBanditControlResult
    {
        internal LocalBanditControlResult(
            float nativeWeight,
            float boundedSecurity,
            float controlMultiplier,
            float finalWeight,
            bool contextApplied)
        {
            NativeWeight = nativeWeight;
            BoundedSecurity = boundedSecurity;
            ControlMultiplier = controlMultiplier;
            FinalWeight = finalWeight;
            ContextApplied = contextApplied;
        }

        internal float NativeWeight { get; }
        internal float BoundedSecurity { get; }
        internal float ControlMultiplier { get; }
        internal float FinalWeight { get; }
        internal bool ContextApplied { get; }
    }

    // Game-assembly-free Phase 5-v1 policy. It changes only the relative
    // spawn-site weight already produced by Bannerlord.
    internal static class LocalBanditControlPolicy
    {
        internal const float MinimumControlMultiplier = 0.75f;
        internal const float MaximumControlMultiplier = 1.25f;
        internal const float MinimumSecurity = 0.0f;
        internal const float MaximumSecurity = 100.0f;
        internal const float NeutralSecurity = 50.0f;

        internal static LocalBanditControlResult Evaluate(
            float nativeWeight,
            bool hasSecurity,
            float security,
            LocalBanditCandidateKind candidateKind)
        {
            float safeNative = SanitizeNativeWeight(nativeWeight);
            bool supported =
                candidateKind == LocalBanditCandidateKind.Town ||
                candidateKind == LocalBanditCandidateKind.Village;

            if (!supported ||
                !hasSecurity ||
                !IsFinite(security))
            {
                return new LocalBanditControlResult(
                    safeNative,
                    0.0f,
                    1.0f,
                    safeNative,
                    false);
            }

            float boundedSecurity = Clamp(
                security,
                MinimumSecurity,
                MaximumSecurity);

            float controlMultiplier = Clamp(
                1.25f - (0.005f * boundedSecurity),
                MinimumControlMultiplier,
                MaximumControlMultiplier);

            float finalWeight = safeNative;
            if (safeNative > 0.0f &&
                controlMultiplier != 1.0f)
            {
                float scaled =
                    safeNative * controlMultiplier;

                // A finite native weight must never become NaN/Infinity.
                // On numeric overflow, preserve the finite native weight.
                if (IsFinite(scaled) &&
                    scaled >= 0.0f)
                {
                    finalWeight = scaled;
                }
            }

            return new LocalBanditControlResult(
                safeNative,
                boundedSecurity,
                controlMultiplier,
                finalWeight,
                true);
        }

        internal static bool IsFinite(float value)
        {
            return !float.IsNaN(value) &&
                !float.IsInfinity(value);
        }

        private static float SanitizeNativeWeight(
            float nativeWeight)
        {
            if (!IsFinite(nativeWeight) ||
                nativeWeight < 0.0f)
            {
                return 0.0f;
            }

            return nativeWeight == 0.0f
                ? 0.0f
                : nativeWeight;
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
