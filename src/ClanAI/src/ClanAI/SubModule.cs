using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace ClanAI
{
    public sealed class ClanAISubModule : MBSubModuleBase
    {
        protected override void OnSubModuleUnloaded()
        {
            if (RuntimeProfile.EvidenceEnabled)
            {
                ClanAIDiagnostics.EndSession("module_unload");
                ClanAIEvidenceWriter.Shutdown();
                ClanAIDiagnostics.WriteShutdownHealth();
            }
            base.OnSubModuleUnloaded();
        }


        protected override void OnApplicationTick(float dt)
        {
            base.OnApplicationTick(dt);
            if (RuntimeProfile.EvidenceEnabled)
            {
                ClanAIDiagnostics.Tick();
                DynastyStructuralRuntimeObserver.FlushPending();
            }
        }

        public override void OnGameEnd(Game game)
        {
            if (RuntimeProfile.EvidenceEnabled)
                ClanAIDiagnostics.EndSession("game_end");
            base.OnGameEnd(game);
        }

        private static void InstallGlobalAiModelMirror(
            CampaignGameStarter starter)
        {
            try
            {
                MobilePartyAIModel current =
                    starter.GetModel<MobilePartyAIModel>();

                if (current == null)
                {
                    ClanAIPostVanilla.WriteExternalLog(
                        "GLOBAL_AI_MODEL_MIRROR_SKIPPED reason=no-current-model");
                    return;
                }

                if (current is DelegatingMobilePartyAIModel)
                {
                    ClanAIPostVanilla.WriteExternalLog(
                        "GLOBAL_AI_MODEL_MIRROR_ALREADY_ACTIVE type=" +
                        current.GetType().FullName);
                    return;
                }

                string innerType =
                    current.GetType().FullName;

                var mirror =
                    new DelegatingMobilePartyAIModel(
                        current);

                starter.AddModel<MobilePartyAIModel>(
                    mirror);

                MobilePartyAIModel selected =
                    starter.GetModel<MobilePartyAIModel>();

                bool selectedMirror =
                    object.ReferenceEquals(
                        selected,
                        mirror);

                ClanAIPostVanilla.WriteExternalLog(
                    "GLOBAL_AI_MODEL_MIRROR_INSTALLED" +
                    " inner=" +
                    innerType +
                    " wrapper=" +
                    mirror.GetType().FullName +
                    " selected=" +
                    (
                        selected == null
                            ? "<null>"
                            : selected.GetType().FullName
                    ) +
                    " selectedMirror=" +
                    selectedMirror);
            }
            catch (System.Exception ex)
            {
                ClanAIPostVanilla.WriteExternalLog(
                    "GLOBAL_AI_MODEL_MIRROR_FAILED type=" +
                    ex.GetType().Name +
                    " message=" +
                    ex.Message);
            }
        }

        private static void InstallCivicProjectBuildingScoreCalculationModel(
            CampaignGameStarter starter)
        {
            BuildingScoreCalculationModel current =
                starter.GetModel<BuildingScoreCalculationModel>();

            if (current == null)
            {
                ClanAIPostVanilla.WriteExternalLog(
                    "CIVIC_PROJECT_MODEL_SKIPPED" +
                    " inner=<none>" +
                    " reason=no-current-model" +
                    " mutation=False");
                return;
            }

            if (current is CivicProjectBuildingScoreCalculationModel)
            {
                return;
            }

            if (!CivicProjectBuildingScoreCalculationModel
                    .SupportsInnerModel(current))
            {
                ClanAIPostVanilla.WriteExternalLog(
                    "CIVIC_PROJECT_MODEL_SKIPPED" +
                    " inner=" +
                    current.GetType().FullName +
                    " reason=unsupported-inner-model" +
                    " mutation=False");
                return;
            }

            var wrapper =
                new CivicProjectBuildingScoreCalculationModel(
                    current);

            starter.AddModel<BuildingScoreCalculationModel>(
                wrapper);

            BuildingScoreCalculationModel selected =
                starter.GetModel<BuildingScoreCalculationModel>();

            if (!object.ReferenceEquals(
                    selected,
                    wrapper))
            {
                throw new System.InvalidOperationException(
                    "Phase 6 CivicProject BuildingScoreCalculationModel was not selected");
            }

            if (RuntimeProfile.EvidenceEnabled)
            {
                CivicProjectRuntimeTelemetry.Install(
                    starter,
                    wrapper,
                    current);
            }
        }

        private static void InstallLocalManpowerVolunteerModel(
            CampaignGameStarter starter)
        {
            VolunteerModel current =
                starter.GetModel<VolunteerModel>();

            if (current == null ||
                current is LocalManpowerVolunteerModel)
            {
                return;
            }

            string innerType =
                current.GetType().FullName;

            var wrapper =
                new LocalManpowerVolunteerModel(
                    current);

            starter.AddModel<VolunteerModel>(
                wrapper);

            VolunteerModel selected =
                starter.GetModel<VolunteerModel>();

            if (!object.ReferenceEquals(
                    selected,
                    wrapper))
            {
                throw new System.InvalidOperationException(
                    "Local Manpower VolunteerModel was not selected");
            }

            ClanAIPostVanilla.WriteExternalLog(
                "LOCAL_MANPOWER_MODEL_SELECTED" +
                " inner=" + innerType +
                " wrapper=" + wrapper.GetType().FullName +
                " selected=" + selected.GetType().FullName +
                " selectedWrapper=True" +
                " policy=phase4b-empty+phase4c-occupied-native-eligible" +
                " mutation=False");

            if (RuntimeProfile.EvidenceEnabled)
                LocalManpowerRuntimeTelemetry.Install();
        }

        protected override void InitializeGameStarter(
            Game game,
            IGameStarter gameStarterObject)
        {
            base.InitializeGameStarter(game, gameStarterObject);

            if (game.GameType is Campaign)
            {
                CampaignGameStarter starter =
                    gameStarterObject as CampaignGameStarter;

                if (starter != null)
                {
                    InstallGlobalAiModelMirror(starter);
                    InstallLocalManpowerVolunteerModel(starter);
                    InstallCivicProjectBuildingScoreCalculationModel(starter);
                    LocalBanditControlPatch.Install();

                    ClanAIPostVanilla.WriteExternalLog(
                        "WRAPPER_PASS_THROUGH global_ai_model_wrapper=enabled mutation=disabled ai_hourly_patch=disabled");

                    starter.AddBehavior(
                        new ClanAIStrategicBehavior()
                    );

                    starter.AddBehavior(
                        new ClanAIObserverSafetyBehavior()
                    );
                    starter.AddBehavior(new SocialCaptivityObserverBehavior());
                    starter.AddBehavior(new PrisonerMercyDecisionBehavior());
                    starter.AddBehavior(new WarStateBehavior());
                    starter.AddBehavior(new RulerClanCourtshipBehavior());
                    starter.AddBehavior(new KingdomContinuityBehavior());
                    if (RuntimeProfile.EvidenceEnabled)
                    {
                        starter.AddBehavior(new GenerationalContinuityPreflightBehavior());
                        starter.AddBehavior(new Phase4ARecoveryObserverBehavior());
                        starter.AddBehavior(new DynastyStructuralRuntimeObserver());
                    }
                }
            }
        }
    }
}

