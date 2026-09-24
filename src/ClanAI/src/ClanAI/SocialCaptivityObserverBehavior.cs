using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace ClanAI
{
    public sealed class SocialCaptivityObserverBehavior
        : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            ClanAIPostVanilla.WriteExternalLog(
                "OBSERVE_ONLY_SHELL SocialMercyPatch=disabled");
            CampaignEvents.HeroPrisonerTaken
                .AddNonSerializedListener(
                    this,
                    new Action<PartyBase, Hero>(
                        OnHeroPrisonerTaken));

            CampaignEvents.HeroPrisonerReleased
                .AddNonSerializedListener(
                    this,
                    new Action<
                        Hero,
                        PartyBase,
                        IFaction,
                        EndCaptivityDetail,
                        bool>(
                            OnHeroPrisonerReleased));

            CampaignEvents.OnSettlementOwnerChangedEvent
                .AddNonSerializedListener(
                    this,
                    new Action<Settlement, bool, Hero, Hero, Hero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail>(
                        OnSettlementOwnerChanged));
        }

        public override void SyncData(
            IDataStore dataStore)
        {
        }

        private void OnHeroPrisonerTaken(
            PartyBase capturer,
            Hero prisoner)
        {
            Hero owner =
                ResolvePartyHero(capturer);

            ClanAIPostVanilla.WriteExternalLog(
                "CAPTIVITY_EVENT" +
                " type=Taken" +
                " prisoner=" +
                HeroName(prisoner) +
                " prisonerClan=" +
                ClanName(
                    prisoner != null
                        ? prisoner.Clan
                        : null) +
                " capturerParty=" +
                PartyName(capturer) +
                " capturerHero=" +
                HeroName(owner) +
                " capturerClan=" +
                ClanName(
                    owner != null
                        ? owner.Clan
                        : null));

            SocialLedger.RecordCapture(
                prisoner,
                capturer);

            CompanionNegativeOutcomeMemory.RecordCapture(
                prisoner,
                capturer);
        }

        private void OnSettlementOwnerChanged(
            Settlement settlement,
            bool openToClaim,
            Hero newOwner,
            Hero oldOwner,
            Hero capturerHero,
            ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
        {
            ClanAIPostVanilla.WriteExternalLog(
                "SETTLEMENT_OWNER_CHANGED settlement=" +
                (settlement == null ? "<none>" : settlement.Name.ToString()) +
                " oldOwner=" + HeroName(oldOwner) +
                " newOwner=" + HeroName(newOwner) +
                " detail=" + detail);

            CompanionNegativeOutcomeMemory.RecordHomeLoss(
                settlement, oldOwner, newOwner, detail);
        }

        private void OnHeroPrisonerReleased(
            Hero prisoner,
            PartyBase party,
            IFaction capturerFaction,
            EndCaptivityDetail detail,
            bool showNotification)
        {
            Hero partyOwner =
                ResolvePartyHero(party);

            ClanAIPostVanilla.WriteExternalLog(
                "CAPTIVITY_RELEASE" +
                " prisoner=" +
                HeroName(prisoner) +
                " prisonerClan=" +
                ClanName(
                    prisoner != null
                        ? prisoner.Clan
                        : null) +
                " detail=" +
                detail.ToString() +
                " party=" +
                PartyName(party) +
                " partyOwner=" +
                HeroName(partyOwner) +
                " partyOwnerClan=" +
                ClanName(
                    partyOwner != null
                        ? partyOwner.Clan
                        : null) +
                " capturerFaction=" +
                FactionName(capturerFaction) +
                " showNotification=" +
                showNotification);

            // IMPORTANT:
            //
            // We deliberately do NOT mutate trust/obligation yet.
            //
            // ReleasedByChoice is semantically a mercy event,
            // but the CampaignEvent does not directly expose
            // EndCaptivityAction's facilitator Hero.
            //
            // First observe real runtime payloads and verify
            // whether partyOwner reliably identifies the
            // releasing noble.

            if (detail ==
                EndCaptivityDetail.ReleasedByChoice)
            {
                ClanAIPostVanilla.WriteExternalLog(
                    "MERCY_CANDIDATE" +
                    " prisoner=" +
                    HeroName(prisoner) +
                    " prisonerClan=" +
                    ClanName(
                        prisoner != null
                            ? prisoner.Clan
                            : null) +
                    " partyOwner=" +
                    HeroName(partyOwner) +
                    " partyOwnerClan=" +
                    ClanName(
                        partyOwner != null
                            ? partyOwner.Clan
                            : null) +
                    " capturerFaction=" +
                    FactionName(capturerFaction));
            }
        }

        private static Hero ResolvePartyHero(
            PartyBase party)
        {
            if (party == null)
                return null;

            if (party.Owner != null)
                return party.Owner;

            if (party.MobileParty != null)
                return party.MobileParty.LeaderHero;

            return null;
        }

        private static string HeroName(
            Hero hero)
        {
            return hero != null
                ? hero.Name.ToString()
                : "<none>";
        }

        private static string ClanName(
            Clan clan)
        {
            return clan != null
                ? clan.Name.ToString()
                : "<none>";
        }

        private static string PartyName(
            PartyBase party)
        {
            return party != null
                ? party.Name.ToString()
                : "<none>";
        }

        private static string FactionName(
            IFaction faction)
        {
            return faction != null
                ? faction.Name.ToString()
                : "<none>";
        }
    }
}


