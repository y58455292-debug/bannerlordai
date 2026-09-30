using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace ClanAI
{
    internal sealed class HomeAssignmentCampaignBehavior : CampaignBehaviorBase
    {
        private bool _loaded;
        public override void RegisterEvents()
        {
            HomeAssignmentVisitPatch.Install();
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSession);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDaily);
        }
        private void OnDaily() { HomeAssignmentStore.InvalidateOwnership(); HomeAssignmentLayer.Prune(); }
        public override void SyncData(IDataStore store)
        {
            List<string> rows = store.IsSaving ? HomeAssignmentStore.Records.Export() : null;
            store.SyncData(HomeAssignmentRecords.SaveKey, ref rows);
            if (store.IsLoading) { HomeAssignmentStore.Records.Import(rows); _loaded = true; HomeAssignmentLayer.Reset(); }
        }
        private void OnSession(CampaignGameStarter starter)
        {
            HomeAssignmentStore.BeginNewSession(_loaded);
            starter.AddGameMenuOption("town", "clanai_home_assignment_town",
                "Manage Home Assignments", CanManage, Open, false, 6);
            starter.AddGameMenuOption("castle", "clanai_home_assignment_castle",
                "Manage Home Assignments", CanManage, Open, false, 6);
        }
        private static bool CanManage(MenuCallbackArgs args)
        { return HomeAssignmentStore.ValidHome(Settlement.CurrentSettlement); }
        private static void Open(MenuCallbackArgs args)
        {
            Settlement holding = Settlement.CurrentSettlement;
            if (!HomeAssignmentStore.ValidHome(holding)) return;
            var choices = new List<InquiryElement>();
            foreach (var component in Clan.PlayerClan.WarPartyComponents)
            {
                MobileParty party = component.MobileParty;
                if (!HomeAssignmentStore.Eligible(party)) continue;
                Settlement home = HomeAssignmentStore.CurrentHome(party.LeaderHero);
                choices.Add(new InquiryElement(party,
                    party.LeaderHero.Name + " — " + (home == null ? "No assigned home" : home.Name.ToString()),
                    null, true, party.Name.ToString()));
            }
            if (choices.Count == 0)
            {
                InformationManager.DisplayMessage(new InformationMessage("No eligible independent clan parties are available."));
                return;
            }
            MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(
                "Home Assignments", "Choose a clan party. Current holding: " + holding.Name,
                choices, true, 1, 1, "Select", "Cancel",
                selected => { if (selected.Count == 1) ChooseAction((MobileParty)selected[0].Identifier, holding); },
                null));
        }
        private static void ChooseAction(MobileParty party, Settlement holding)
        {
            if (!HomeAssignmentStore.Eligible(party) || !HomeAssignmentStore.ValidHome(holding)) return;
            var choices = new List<InquiryElement> {
                new InquiryElement("assign", "Assign " + holding.Name, null, true, ""),
                new InquiryElement("clear", "Clear assigned home", null,
                    HomeAssignmentStore.CurrentHome(party.LeaderHero) != null, "")
            };
            MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(
                party.LeaderHero.Name.ToString(), "Choose a home responsibility action.",
                choices, true, 1, 1, "Confirm", "Cancel", selected => {
                    if (selected.Count != 1 || !HomeAssignmentStore.Eligible(party) ||
                        !HomeAssignmentStore.ValidHome(holding)) return;
                    string name = party.LeaderHero.Name.ToString();
                    if ((string)selected[0].Identifier == "clear") {
                        if (HomeAssignmentStore.Clear(party.LeaderHero.StringId))
                            InformationManager.DisplayMessage(new InformationMessage(name + " no longer has an assigned home."));
                    } else {
                        Settlement old = HomeAssignmentStore.CurrentHome(party.LeaderHero);
                        if (HomeAssignmentStore.Assign(party, holding))
                            InformationManager.DisplayMessage(new InformationMessage(old == null
                                ? name + " is now responsible for " + holding.Name + "."
                                : name + "’s home responsibility has changed from " + old.Name + " to " + holding.Name + "."));
                    }
                }, null));
        }
    }
}
