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
            ClanAIDiagnostics.EndSession("module_unload");
            ClanAIEvidenceWriter.Shutdown();
            ClanAIDiagnostics.WriteShutdownHealth();
            base.OnSubModuleUnloaded();
        }


        protected override void OnApplicationTick(float dt)
        {
            base.OnApplicationTick(dt);
            ClanAIDiagnostics.Tick();
        }

        public override void OnGameEnd(Game game)
        {
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
                }
            }
        }
    }
}



