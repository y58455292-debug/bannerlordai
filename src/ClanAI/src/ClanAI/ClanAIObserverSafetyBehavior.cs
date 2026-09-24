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
            // Survival-showcase candidate: do not install the historical
            // 10,000-hour do-not-attack-main-party observer protection.
            // Any timer serialized in the loaded test save is cleared by
            // the TestRunner's CLEAR_MAIN_PARTY_PROTECTION command.
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
