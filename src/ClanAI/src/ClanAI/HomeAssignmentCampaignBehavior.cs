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
            foreach (Hero hero in HomeAssignmentRoster.Build(Clan.PlayerClan))
            {
                Settlement home = HomeAssignmentStore.CurrentHome(hero);
                HomeAssignmentStatus status = HomeAssignmentRoster.Status(hero);
                string homeName = home == null ? "No assigned home" : home.Name.ToString();
                string title = HomeAssignmentRoster.DisplayName(hero) + " — Home: " + homeName;
                string hint = HomeAssignmentRoster.Location(hero) + " — " + HomeAssignmentRoster.StatusText(status);
                choices.Add(new InquiryElement(hero, title, null, true, hint));
            }
            if (choices.Count == 0)
            {
                InformationManager.DisplayMessage(new InformationMessage("No household members are available."));
                return;
            }
            MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(
                "Household Home Responsibilities", "Current holding: " + holding.Name,
                choices, true, 1, 1, "Select", "Cancel",
                selected => { if (selected.Count == 1) ChooseAction((Hero)selected[0].Identifier, holding); },
                null));
        }
        private static void ChooseAction(Hero hero, Settlement holding)
        {
            if (!HomeAssignmentStore.ValidAssignedHero(hero)) return;
            Settlement home = HomeAssignmentStore.CurrentHome(hero);
            HomeAssignmentStatus status = HomeAssignmentRoster.Status(hero);
            bool active = status == HomeAssignmentStatus.Active;
            var choices = new List<InquiryElement>();
            if (active)
                choices.Add(new InquiryElement("assign", (home == null ? "Assign " : "Change home to ") + holding.Name, null, true, ""));
            if (home != null)
                choices.Add(new InquiryElement("clear", "Clear assigned home", null, true, ""));
            if (choices.Count == 0)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    HomeAssignmentRoster.DisplayName(hero) + ": " + HomeAssignmentRoster.StatusText(status) + "."));
                return;
            }
            string actionHint = active ? "Choose a home responsibility action."
                : HomeAssignmentRoster.StatusText(status) + ". You may clear the saved home.";
            MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(
                HomeAssignmentRoster.DisplayName(hero), actionHint,
                choices, true, 1, 1, "Confirm", "Cancel", selected => {
                    if (selected.Count != 1 || !HomeAssignmentStore.ValidAssignedHero(hero)) return;
                    string action = (string)selected[0].Identifier;
                    string name = HomeAssignmentRoster.DisplayName(hero);
                    if (action == "clear")
                    {
                        if (HomeAssignmentStore.Clear(hero.StringId))
                            InformationManager.DisplayMessage(new InformationMessage(name + " no longer has an assigned home."));
                        return;
                    }
                    MobileParty party = hero.PartyBelongedTo;
                    if (action != "assign" || HomeAssignmentRoster.Status(hero) != HomeAssignmentStatus.Active ||
                        party == null || !ReferenceEquals(party.LeaderHero, hero) ||
                        !HomeAssignmentStore.Eligible(party) || !HomeAssignmentStore.ValidHome(holding)) return;
                    Settlement previous = HomeAssignmentStore.CurrentHome(hero);
                    if (HomeAssignmentStore.Assign(party, holding))
                        InformationManager.DisplayMessage(new InformationMessage(previous == null
                            ? name + " is now responsible for " + holding.Name + "."
                            : name + "'s home responsibility has changed from " + previous.Name + " to " + holding.Name + "."));
                }, null));
        }
    }
}
