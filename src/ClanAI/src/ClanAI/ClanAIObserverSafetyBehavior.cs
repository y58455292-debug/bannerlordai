using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;

namespace ClanAI
{
    public sealed class ClanAIObserverSafetyBehavior : CampaignBehaviorBase
    {
        private const int SafetyHours = 10000;

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(
                this,
                new Action<CampaignGameStarter>(OnSessionLaunched)
            );

            CampaignEvents.MobilePartyCreated.AddNonSerializedListener(
                this,
                new Action<MobileParty>(OnMobilePartyCreated)
            );
        }

        public override void SyncData(IDataStore dataStore)
        {
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            foreach (MobileParty party in MobileParty.All)
            {
                Protect(party);
            }
        }

        private void OnMobilePartyCreated(MobileParty party)
        {
            Protect(party);
        }

        private static void Protect(MobileParty party)
        {
            if (party != null &&
                party != MobileParty.MainParty)
            {
                party.Ai.SetDoNotAttackMainParty(SafetyHours);
            }
        }
    }
}
