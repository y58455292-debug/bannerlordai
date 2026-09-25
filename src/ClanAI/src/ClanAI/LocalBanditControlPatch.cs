using System;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem.Settlements;

namespace ClanAI
{
    // Phase 5-v1: modify only the native result/weight used by the
    // ambient looter town/village spawn-site selector.
    internal static class LocalBanditControlPatch
    {
        internal const string TargetTypeName =
            "TaleWorlds.CampaignSystem.CampaignBehaviors.BanditSpawnCampaignBehavior";
        internal const string TargetMethodName =
            "GetSpawnChanceInSettlement";

        private static bool _installed;

        internal static MethodInfo ResolveTargetMethod()
        {
            Type behaviorType =
                AccessTools.TypeByName(TargetTypeName);

            MethodInfo target =
                behaviorType == null
                    ? null
                    : AccessTools.Method(
                        behaviorType,
                        TargetMethodName,
                        new Type[] { typeof(Settlement) });

            if (target == null)
            {
                throw new MissingMethodException(
                    TargetTypeName,
                    TargetMethodName);
            }

            ParameterInfo[] parameters =
                target.GetParameters();

            if (!target.IsPrivate ||
                target.IsStatic ||
                target.ReturnType != typeof(float) ||
                parameters.Length != 1 ||
                parameters[0].ParameterType != typeof(Settlement))
            {
                throw new InvalidOperationException(
                    "Phase 5 target signature does not match the supported private instance float(Settlement) seam.");
            }

            return target;
        }

        internal static void Install()
        {
            if (_installed)
                return;

            MethodInfo target =
                ResolveTargetMethod();

            MethodInfo postfix =
                AccessTools.Method(
                    typeof(LocalBanditControlPatch),
                    nameof(SpawnWeightPostfix));

            if (postfix == null)
            {
                throw new MissingMethodException(
                    typeof(LocalBanditControlPatch).FullName,
                    nameof(SpawnWeightPostfix));
            }

            Harmony harmony = new Harmony(
                "com.bannerlordairesearch.clanai.localbanditcontrol.v1");

            harmony.Patch(
                target,
                postfix: new HarmonyMethod(postfix));

            _installed = true;
        }

        private static void SpawnWeightPostfix(
            Settlement __0,
            ref float __result)
        {
            Settlement settlement = __0;
            LocalBanditCandidateKind candidateKind =
                LocalBanditCandidateKind.Unsupported;
            bool hasSecurity = false;
            float security = 0.0f;

            if (settlement != null)
            {
                if (settlement.IsTown)
                {
                    candidateKind =
                        LocalBanditCandidateKind.Town;

                    if (settlement.Town != null)
                    {
                        security =
                            settlement.Town.Security;
                        hasSecurity = true;
                    }
                }
                else if (settlement.IsVillage)
                {
                    candidateKind =
                        LocalBanditCandidateKind.Village;

                    if (settlement.Village != null)
                    {
                        Settlement bound =
                            settlement.Village.Bound;

                        if (bound != null &&
                            bound.Town != null)
                        {
                            security =
                                bound.Town.Security;
                            hasSecurity = true;
                        }
                    }
                }
            }

            if (candidateKind ==
                    LocalBanditCandidateKind.Unsupported ||
                !hasSecurity ||
                !LocalBanditControlPolicy.IsFinite(security))
            {
                return;
            }

            LocalBanditControlResult result =
                LocalBanditControlPolicy.Evaluate(
                    __result,
                    hasSecurity,
                    security,
                    candidateKind);

            __result = result.FinalWeight;
        }
    }
}
