using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Buildings;

namespace ClanAI
{
    // Phase 6-v1 selected-model wrapper. It delegates native selection first
    // and may substitute only an existing Festival and Games project reference.
    public sealed class CivicProjectBuildingScoreCalculationModel
        : BuildingScoreCalculationModel
    {
        private readonly BuildingScoreCalculationModel _inner;

        public CivicProjectBuildingScoreCalculationModel(
            BuildingScoreCalculationModel inner)
        {
            if (inner == null)
                throw new ArgumentNullException("inner");

            _inner = inner;
        }

        public BuildingScoreCalculationModel InnerModel
        {
            get { return _inner; }
        }

        public static bool SupportsInnerModel(
            BuildingScoreCalculationModel inner)
        {
            return inner != null &&
                inner.GetType() ==
                    typeof(DefaultBuildingScoreCalculationModel);
        }

        public override Building GetNextBuilding(
            Town town)
        {
            return _inner.GetNextBuilding(town);
        }

        public override Building GetNextDailyBuilding(
            Town town)
        {
            Building nativeResult =
                _inner.GetNextDailyBuilding(town);

            if (!SupportsInnerModel(_inner))
                return nativeResult;

            bool isNpcTown;
            bool isConstructionIdle;
            bool hasFestivalAndGamesProject;
            float loyalty;
            float nativeRebelliousThreshold;
            Building festivalAndGames;

            bool contextValid =
                TryResolveContext(
                    town,
                    nativeResult,
                    out isNpcTown,
                    out isConstructionIdle,
                    out hasFestivalAndGamesProject,
                    out loyalty,
                    out nativeRebelliousThreshold,
                    out festivalAndGames);

            CivicProjectSelectionResult decision =
                CivicProjectSelectionPolicy.Evaluate(
                    contextValid,
                    isNpcTown,
                    isConstructionIdle,
                    hasFestivalAndGamesProject,
                    loyalty,
                    nativeRebelliousThreshold);

            return SelectResult(
                nativeResult,
                festivalAndGames,
                decision.PreferFestivalAndGames);
        }

        internal static Building SelectResult(
            Building nativeResult,
            Building festivalAndGames,
            bool preferFestivalAndGames)
        {
            if (preferFestivalAndGames &&
                festivalAndGames != null)
            {
                return festivalAndGames;
            }

            return nativeResult;
        }

        private static bool TryResolveContext(
            Town town,
            Building nativeResult,
            out bool isNpcTown,
            out bool isConstructionIdle,
            out bool hasFestivalAndGamesProject,
            out float loyalty,
            out float nativeRebelliousThreshold,
            out Building festivalAndGames)
        {
            isNpcTown = false;
            isConstructionIdle = false;
            hasFestivalAndGamesProject = false;
            loyalty = 0.0f;
            nativeRebelliousThreshold = 0.0f;
            festivalAndGames = null;

            if (town == null ||
                Campaign.Current == null ||
                Campaign.Current.Models == null ||
                Campaign.Current.Models.SettlementLoyaltyModel == null ||
                !town.IsTown ||
                town.OwnerClan == null ||
                town.Buildings == null ||
                town.BuildingsInProgress == null ||
                nativeResult == null ||
                !IsExistingDailyProject(
                    town,
                    nativeResult))
            {
                return false;
            }

            isNpcTown =
                town.OwnerClan != Clan.PlayerClan;

            Building currentDefault =
                town.CurrentDefaultBuilding;

            isConstructionIdle =
                town.BuildingsInProgress.Count == 0 &&
                IsExistingDailyProject(
                    town,
                    currentDefault);

            hasFestivalAndGamesProject =
                TryFindFestivalAndGames(
                    town,
                    out festivalAndGames);

            loyalty = town.Loyalty;
            nativeRebelliousThreshold =
                Campaign.Current.Models
                    .SettlementLoyaltyModel
                    .RebelliousStateStartLoyaltyThreshold;

            return CivicProjectSelectionPolicy.IsFinite(
                    loyalty) &&
                CivicProjectSelectionPolicy.IsFinite(
                    nativeRebelliousThreshold);
        }

        private static bool TryFindFestivalAndGames(
            Town town,
            out Building festivalAndGames)
        {
            festivalAndGames = null;

            BuildingType festivalType =
                DefaultBuildingTypes
                    .SettlementDailyFestivalAndGames;

            if (festivalType == null ||
                !festivalType.IsDailyProject)
            {
                return false;
            }

            foreach (Building building in town.Buildings)
            {
                if (building != null &&
                    object.ReferenceEquals(
                        building.BuildingType,
                        festivalType))
                {
                    festivalAndGames = building;
                    return true;
                }
            }

            return false;
        }

        private static bool IsExistingDailyProject(
            Town town,
            Building building)
        {
            if (building == null ||
                building.BuildingType == null ||
                !building.BuildingType.IsDailyProject)
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
    }
}

[executed on device: DESKTOP-JO4B7VH (fd6618f4-5715-46b1-8665-68172ef15169)]