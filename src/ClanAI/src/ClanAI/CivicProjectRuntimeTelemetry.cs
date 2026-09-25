using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using HarmonyLib;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Buildings;

namespace ClanAI
{
    // Phase 6 runtime characterization only. Observation and logging only.
    // The native caller remains the sole owner of project-state mutation.
    internal static class CivicProjectRuntimeTelemetry
    {
        private sealed class PendingChoice
        {
            internal Building NativeResult;
            internal Building FinalResult;
            internal bool PreferredFestival;
            internal bool Substitution;
            internal CivicProjectPreferenceReason Reason;
            internal double CampaignHour;
        }

        private static readonly object EventOwner =
            new object();

        private static readonly Dictionary<Town, PendingChoice>
            PendingByTown =
                new Dictionary<Town, PendingChoice>();

        private static bool _installed;

        internal static void Install(
            CampaignGameStarter starter,
            CivicProjectBuildingScoreCalculationModel wrapper,
            BuildingScoreCalculationModel inner)
        {
            if (_installed)
                return;

            MethodInfo target =
                AccessTools.Method(
                    typeof(BuildingHelper),
                    nameof(BuildingHelper.ChangeDefaultBuilding),
                    new Type[]
                    {
                        typeof(Building),
                        typeof(Town)
                    });

            MethodInfo postfix =
                AccessTools.Method(
                    typeof(CivicProjectRuntimeTelemetry),
                    nameof(ChangeDefaultBuildingPostfix));

            if (target == null || postfix == null)
            {
                throw new MissingMethodException(
                    "Phase 6 native civic-project commit observation seam could not be resolved.");
            }

            ParameterInfo[] parameters =
                target.GetParameters();

            if (!target.IsPublic ||
                !target.IsStatic ||
                target.ReturnType != typeof(void) ||
                parameters.Length != 2 ||
                parameters[0].ParameterType != typeof(Building) ||
                parameters[1].ParameterType != typeof(Town))
            {
                throw new InvalidOperationException(
                    "Phase 6 native ChangeDefaultBuilding signature does not match the audited supported seam.");
            }

            Harmony harmony =
                new Harmony(
                    "com.bannerlordairesearch.clanai.civicproject.runtime.v1");

            harmony.Patch(
                target,
                postfix: new HarmonyMethod(postfix));

            CampaignEvents.OnSessionLaunchedEvent
                .AddNonSerializedListener(
                    EventOwner,
                    new Action<CampaignGameStarter>(
                        OnSessionLaunched));

            _installed = true;

            ClanAIPostVanilla.WriteExternalLog(
                "CIVIC_PROJECT_TELEMETRY_INSTALLED" +
                " wrapper=" + TypeName(wrapper) +
                " inner=" + TypeName(inner) +
                " commitObserver=postfix" +
                " mutation=False");
        }

        private static void OnSessionLaunched(
            CampaignGameStarter starter)
        {
            try
            {
                PendingByTown.Clear();

                BuildingScoreCalculationModel selected =
                    starter == null
                        ? null
                        : starter.GetModel<
                            BuildingScoreCalculationModel>();

                var wrapper =
                    selected as
                        CivicProjectBuildingScoreCalculationModel;

                BuildingScoreCalculationModel inner =
                    wrapper == null
                        ? null
                        : wrapper.InnerModel;

                bool selectedWrapper =
                    wrapper != null;

                bool exactAuditedInner =
                    CivicProjectBuildingScoreCalculationModel
                        .SupportsInnerModel(inner);

                ClanAIPostVanilla.WriteExternalLog(
                    "CIVIC_PROJECT_MODEL_ACTIVE" +
                    " campaignHour=" +
                        D(CampaignTime.Now.ToHours) +
                    " selected=" + TypeName(selected) +
                    " selectedWrapper=" + selectedWrapper +
                    " inner=" + TypeName(inner) +
                    " exactAuditedInner=" +
                        exactAuditedInner +
                    " mutation=False");
            }
            catch (Exception ex)
            {
                LogError(
                    "session-model",
                    ex);
            }
        }

        internal static void ObserveEvaluation(
            Town town,
            Building nativeResult,
            Building finalResult,
            bool contextValid,
            bool isNpcTown,
            bool isConstructionIdle,
            bool hasFestivalAndGamesProject,
            float loyalty,
            float nativeRebelliousThreshold,
            CivicProjectSelectionResult decision)
        {
            try
            {
                bool nativeIsFestival =
                    IsFestival(nativeResult);
                bool finalIsFestival =
                    IsFestival(finalResult);
                bool substitution =
                    decision.PreferFestivalAndGames &&
                    !object.ReferenceEquals(
                        nativeResult,
                        finalResult);

                bool finalExistingReference =
                    ContainsReference(
                        town,
                        finalResult);

                if (town != null &&
                    finalResult != null)
                {
                    PendingByTown[town] =
                        new PendingChoice
                        {
                            NativeResult = nativeResult,
                            FinalResult = finalResult,
                            PreferredFestival =
                                decision.PreferFestivalAndGames,
                            Substitution = substitution,
                            Reason = decision.Reason,
                            CampaignHour =
                                CampaignTime.Now.ToHours
                        };
                }

                ClanAIPostVanilla.WriteExternalLog(
                    "CIVIC_PROJECT_EVAL" +
                    " campaignHour=" +
                        D(CampaignTime.Now.ToHours) +
                    TownFields(town) +
                    " npcTown=" + isNpcTown +
                    " constructionIdle=" +
                        isConstructionIdle +
                    " queueCount=" +
                        QueueCount(town) +
                    " contextValid=" +
                        contextValid +
                    " festivalAvailable=" +
                        hasFestivalAndGamesProject +
                    " loyalty=" + F(loyalty) +
                    " nativeThreshold=" +
                        F(nativeRebelliousThreshold) +
                    " nativeResultId=" +
                        BuildingId(nativeResult) +
                    " nativeResult=" +
                        BuildingName(nativeResult) +
                    " nativeIsFestival=" +
                        nativeIsFestival +
                    " finalResultId=" +
                        BuildingId(finalResult) +
                    " finalResult=" +
                        BuildingName(finalResult) +
                    " finalIsFestival=" +
                        finalIsFestival +
                    " finalExistingReference=" +
                        finalExistingReference +
                    " preferFestival=" +
                        decision.PreferFestivalAndGames +
                    " substitution=" +
                        substitution +
                    " reason=" +
                        decision.Reason +
                    " mutationByClanAI=False");
            }
            catch (Exception ex)
            {
                LogError(
                    "evaluation",
                    ex);
            }
        }

        private static void ChangeDefaultBuildingPostfix(
            Building __0,
            Town __1)
        {
            try
            {
                Town town = __1;
                PendingChoice pending;

                if (town == null ||
                    !PendingByTown.TryGetValue(
                        town,
                        out pending))
                {
                    return;
                }

                if (!object.ReferenceEquals(
                        pending.FinalResult,
                        __0))
                {
                    return;
                }

                bool committedReference =
                    object.ReferenceEquals(
                        town.CurrentDefaultBuilding,
                        __0);

                double ageHours =
                    CampaignTime.Now.ToHours -
                    pending.CampaignHour;

                ClanAIPostVanilla.WriteExternalLog(
                    "CIVIC_PROJECT_NATIVE_COMMIT" +
                    " campaignHour=" +
                        D(CampaignTime.Now.ToHours) +
                    TownFields(town) +
                    " nativeResultId=" +
                        BuildingId(pending.NativeResult) +
                    " nativeResult=" +
                        BuildingName(pending.NativeResult) +
                    " committedResultId=" +
                        BuildingId(__0) +
                    " committedResult=" +
                        BuildingName(__0) +
                    " committedIsFestival=" +
                        IsFestival(__0) +
                    " preferredFestival=" +
                        pending.PreferredFestival +
                    " substitution=" +
                        pending.Substitution +
                    " reason=" +
                        pending.Reason +
                    " currentDefaultMatches=" +
                        committedReference +
                    " ageHours=" +
                        D(ageHours) +
                    " source=native-BuildingHelper.ChangeDefaultBuilding" +
                    " observer=postfix" +
                    " mutationByClanAI=False");

                PendingByTown.Remove(town);
            }
            catch (Exception ex)
            {
                LogError(
                    "native-commit",
                    ex);
            }
        }

        private static bool ContainsReference(
            Town town,
            Building building)
        {
            if (town == null ||
                town.Buildings == null ||
                building == null)
            {
                return false;
            }

            foreach (Building existing in town.Buildings)
            {
                if (object.ReferenceEquals(
                        existing,
                        building))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsFestival(
            Building building)
        {
            if (building == null ||
                building.BuildingType == null)
            {
                return false;
            }

            BuildingType festival =
                DefaultBuildingTypes
                    .SettlementDailyFestivalAndGames;

            return festival != null &&
                object.ReferenceEquals(
                    building.BuildingType,
                    festival);
        }

        private static string TownFields(
            Town town)
        {
            Settlement settlement =
                town == null
                    ? null
                    : town.Settlement;

            return
                " settlementId=" +
                    SettlementId(settlement) +
                " settlement=" +
                    SettlementName(settlement);
        }

        private static int QueueCount(
            Town town)
        {
            return town == null ||
                town.BuildingsInProgress == null
                    ? -1
                    : town.BuildingsInProgress.Count;
        }

        private static string BuildingId(
            Building building)
        {
            return building == null ||
                building.BuildingType == null ||
                string.IsNullOrEmpty(
                    building.BuildingType.StringId)
                    ? "<none>"
                    : Safe(
                        building.BuildingType.StringId);
        }

        private static string BuildingName(
            Building building)
        {
            return building == null ||
                building.Name == null
                    ? "<none>"
                    : Safe(
                        building.Name.ToString());
        }

        private static string SettlementId(
            Settlement settlement)
        {
            return settlement == null ||
                string.IsNullOrEmpty(
                    settlement.StringId)
                    ? "<none>"
                    : Safe(
                        settlement.StringId);
        }

        private static string SettlementName(
            Settlement settlement)
        {
            return settlement == null ||
                settlement.Name == null
                    ? "<none>"
                    : Safe(
                        settlement.Name.ToString());
        }

        private static string TypeName(
            object value)
        {
            return value == null
                ? "<none>"
                : value.GetType().FullName;
        }

        private static string Safe(
            string value)
        {
            if (string.IsNullOrEmpty(value))
                return "<none>";

            return value
                .Replace(' ', '_')
                .Replace('\r', '_')
                .Replace('\n', '_');
        }

        private static string F(
            float value)
        {
            return value.ToString(
                "R",
                CultureInfo.InvariantCulture);
        }

        private static string D(
            double value)
        {
            return value.ToString(
                "R",
                CultureInfo.InvariantCulture);
        }

        private static void LogError(
            string stage,
            Exception ex)
        {
            ClanAIPostVanilla.WriteExternalLog(
                "CIVIC_PROJECT_TELEMETRY_ERROR" +
                " stage=" + stage +
                " type=" +
                    ex.GetType().Name +
                " mutation=False");
        }
    }
}
