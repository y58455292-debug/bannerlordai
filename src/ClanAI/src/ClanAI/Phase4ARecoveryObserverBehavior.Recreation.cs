using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace ClanAI
{
    // Additional observations on the existing recovery behavior, not a new AI layer.
    public sealed partial class Phase4ARecoveryObserverBehavior
    {
        private sealed class RecreationIdentity
        {
            internal string HeroId, HeroName, PartyId;
        }
        private sealed class DefeatLink
        {
            internal RecreationIdentity Identity;
            internal PartyBase OldParty;
            internal double Hour;
        }
        private sealed class RecreationTrack
        {
            internal RecreationIdentity Identity;
            internal DefeatLink Defeat;
            internal RosterSnapshot Initial;
            internal double Hour;
            internal bool InitialBeforeSettlement, MissingBoundary;
            internal int LastTotal, ObservedPositiveGrowth;
        }
        private static readonly Dictionary<PartyBase, RecreationIdentity> RecreationIdentities =
            new Dictionary<PartyBase, RecreationIdentity>();
        private static readonly Dictionary<string, DefeatLink> DefeatsByHero =
            new Dictionary<string, DefeatLink>(StringComparer.Ordinal);
        private static readonly Dictionary<PartyBase, double> DestroyedParties =
            new Dictionary<PartyBase, double>();
        private static readonly Dictionary<PartyBase, RecreationTrack> PendingRecreations =
            new Dictionary<PartyBase, RecreationTrack>();
        private static readonly HashSet<PartyBase> NativeCreationsSeen = new HashSet<PartyBase>();
        private static readonly HashSet<PartyBase> SettlementEntriesSeen = new HashSet<PartyBase>();
        private static bool RecreationSessionReady, RecreationObservationFault;
        private static double RecreationSessionHour;

        private void RegisterRecreationLinkEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this,
                new Action<CampaignGameStarter>(s => ObserveRecreationSafely(ResetRecreationLinks)));
            CampaignEvents.MapEventEnded.AddNonSerializedListener(this,
                new Action<MapEvent>(e => ObserveRecreationSafely(() => ObserveDefeatedHeroes(e))));
            CampaignEvents.MobilePartyDestroyed.AddNonSerializedListener(this,
                new Action<MobileParty, PartyBase>((p, d) => ObserveRecreationSafely(() => ObserveLinkedDestruction(p))));
            CampaignEvents.MobilePartyCreated.AddNonSerializedListener(this,
                new Action<MobileParty>(p => ObserveRecreationSafely(() => ObserveNativeRecreation(p))));
            CampaignEvents.HourlyTickPartyEvent.AddNonSerializedListener(this,
                new Action<MobileParty>(p => ObserveRecreationSafely(() => ObserveRecreationProgress(p))));
            CampaignEvents.BeforeSettlementEnteredEvent.AddNonSerializedListener(this,
                new Action<MobileParty, Settlement, Hero>((p, s, h) => ObserveRecreationSafely(() => ObserveFirstRecreationVisit(p, s, true))));
            CampaignEvents.AfterSettlementEntered.AddNonSerializedListener(this,
                new Action<MobileParty, Settlement, Hero>((p, s, h) => ObserveRecreationSafely(() => ObserveFirstRecreationVisit(p, s, false))));
        }

        private static void ObserveRecreationSafely(Action observation)
        {
            try { observation(); }
            catch (Exception ex)
            {
                RecreationObservationFault = true;
                ClanAIPostVanilla.WriteExternalLog("PHASE4A_LINK_OBSERVER_ERROR type=" + ex.GetType().Name);
            }
        }

        private static void ResetRecreationLinks()
        {
            RecreationIdentities.Clear(); DefeatsByHero.Clear(); DestroyedParties.Clear();
            PendingRecreations.Clear(); NativeCreationsSeen.Clear(); SettlementEntriesSeen.Clear();
            RecreationObservationFault = false;
            RecreationSessionHour = CampaignTime.Now.ToHours;
            RecreationSessionReady = true;
            foreach (MobileParty party in MobileParty.All) RememberRecreationIdentity(party);
            ClanAIPostVanilla.WriteExternalLog("PHASE4A_LINK_RESET campaignHour=" + D(RecreationSessionHour) +
                " maxHours=168 mode=observe-only heroIdentityRequired=True");
        }

        private static bool RecreationWindowOpen()
        {
            return RecreationSessionReady && Phase4ARecreationLinkPolicy.InWindow(
                RecreationSessionHour, CampaignTime.Now.ToHours);
        }

        private static RecreationIdentity RememberRecreationIdentity(MobileParty party)
        {
            if (!EligibleLordParty(party) || party.Party == null ||
                string.IsNullOrWhiteSpace(party.LeaderHero.StringId)) return null;
            var identity = new RecreationIdentity {
                HeroId = party.LeaderHero.StringId,
                HeroName = Safe(party.LeaderHero.Name.ToString()), PartyId = party.StringId
            };
            RecreationIdentities[party.Party] = identity;
            return identity;
        }

        private static void ObserveDefeatedHeroes(MapEvent battle)
        {
            if (!RecreationWindowOpen() || battle == null || !battle.HasWinner ||
                (battle.DefeatedSide != BattleSideEnum.Attacker && battle.DefeatedSide != BattleSideEnum.Defender)) return;
            var parties = battle.PartiesOnSide(battle.DefeatedSide);
            if (parties == null) return;
            foreach (var participant in parties)
            {
                PartyBase oldParty = participant.Party;
                MobileParty mobile = oldParty == null ? null : oldParty.MobileParty;
                // A cached name/clan alone is not accepted as the defeated hero.
                RecreationIdentity identity = RememberRecreationIdentity(mobile);
                if (identity == null)
                {
                    RecreationIdentity cached;
                    if (oldParty != null && RecreationIdentities.TryGetValue(oldParty, out cached))
                        ClanAIPostVanilla.WriteExternalLog("PHASE4A_LINK_DEFEAT_UNRESOLVED heroId=" + cached.HeroId +
                            " partyId=" + cached.PartyId + " campaignHour=" + D(CampaignTime.Now.ToHours) +
                            " reason=no-native-leader-at-defeat linked=False");
                    continue;
                }
                double now = CampaignTime.Now.ToHours;
                var defeat = new DefeatLink { Identity = identity, OldParty = oldParty, Hour = now };
                DefeatsByHero[identity.HeroId] = defeat;
                ClanAIPostVanilla.WriteExternalLog("PHASE4A_LINK_DEFEAT" + LinkIdentityFields(identity) +
                    IdentityFields(mobile) + " campaignHour=" + D(now) + " defeatedSide=" + battle.DefeatedSide +
                    " heroIdentitySource=native-leader-at-map-event-end" + DestructionFields(defeat) +
                    " roster=" + RosterFields(CaptureRoster(mobile)));
            }
        }

        private static void ObserveLinkedDestruction(MobileParty party)
        {
            if (!RecreationWindowOpen() || party == null || party.Party == null) return;
            RecreationIdentity identity = RememberRecreationIdentity(party);
            if (identity == null) RecreationIdentities.TryGetValue(party.Party, out identity);
            if (identity == null) return;
            double now = CampaignTime.Now.ToHours;
            DestroyedParties[party.Party] = now;
            ClanAIPostVanilla.WriteExternalLog("PHASE4A_LINK_PARTY_DESTROYED" + LinkIdentityFields(identity) +
                " campaignHour=" + D(now) + " source=MobilePartyDestroyed");
            RecreationTrack pending;
            if (PendingRecreations.TryGetValue(party.Party, out pending))
            {
                LogRecreationEnd("PHASE4A_LINK_INCOMPLETE", party, pending, null, false, "destroyed-before-first-visit");
                PendingRecreations.Remove(party.Party);
            }
        }

        private static bool NativePartyStillPresent(PartyBase oldParty)
        {
            foreach (MobileParty party in MobileParty.All)
                if (ReferenceEquals(party.Party, oldParty)) return true;
            return false;
        }

        private static void ObserveNativeRecreation(MobileParty party)
        {
            if (!RecreationWindowOpen() || party == null || !party.IsLordParty || party.IsMainParty ||
                party.ActualClan == Clan.PlayerClan || party.Party == null) return;
            RecreationIdentity identity = RememberRecreationIdentity(party);
            bool firstCreationEvent = NativeCreationsSeen.Add(party.Party);
            DefeatLink defeat = null;
            if (identity != null) DefeatsByHero.TryGetValue(identity.HeroId, out defeat);
            bool priorGone = defeat != null && (DestroyedParties.ContainsKey(defeat.OldParty) ||
                !NativePartyStillPresent(defeat.OldParty));
            double now = CampaignTime.Now.ToHours;
            bool linked = firstCreationEvent && defeat != null && Phase4ARecreationLinkPolicy.CanLink(
                defeat.Identity.HeroId, identity.HeroId, defeat.Hour, now,
                !ReferenceEquals(defeat.OldParty, party.Party), priorGone);
            bool available = party.MemberRoster != null;
            RosterSnapshot roster = CaptureRoster(party);
            bool preComplete = available && party.CurrentSettlement == null &&
                !SettlementEntriesSeen.Contains(party.Party) && !RecreationObservationFault;
            var classification = Phase4ARecreationLinkPolicy.Classify(firstCreationEvent, identity != null,
                linked, preComplete, available ? roster.Total : -1, available ? roster.Heroes : -1);
            ClanAIPostVanilla.WriteExternalLog("PHASE4A_LINK_CREATION" + LinkIdentityFields(identity) + IdentityFields(party) +
                " campaignHour=" + D(now) + " linked=" + linked + " classification=" + classification +
                " priorPartyGone=" + priorGone + " initialBeforeSettlement=" + preComplete +
                " rosterAvailable=" + available + " regularTroops=" + (roster.Total - roster.Heroes) +
                (defeat == null ? " defeatHour=<none> defeatedPartyId=<none>" :
                    " defeatHour=" + D(defeat.Hour) + " defeatedPartyId=" + defeat.Identity.PartyId +
                    " defeatToRecreationHours=" + D(now - defeat.Hour) + DestructionFields(defeat)) +
                " roster=" + RosterFields(roster) + PartyContextFields(party));
            if (!linked) return;
            DefeatsByHero.Remove(identity.HeroId); // One observed defeat cannot label multiple creations.
            PendingRecreations[party.Party] = new RecreationTrack {
                Identity = identity, Defeat = defeat, Initial = roster, Hour = now,
                InitialBeforeSettlement = preComplete, LastTotal = roster.Total
            };
        }

        private static void ObserveRecreationProgress(MobileParty party)
        {
            if (!RecreationSessionReady || party == null || party.Party == null) return;
            if (RecreationWindowOpen()) RememberRecreationIdentity(party);
            RecreationTrack track;
            if (!PendingRecreations.TryGetValue(party.Party, out track)) return;
            if (!RecreationWindowOpen() || party.CurrentSettlement != null)
            {
                track.MissingBoundary = true;
                LogRecreationEnd("PHASE4A_LINK_INCOMPLETE", party, track, party.CurrentSettlement, false,
                    RecreationWindowOpen() ? "settlement-seen-without-pre-entry" : "observation-window-ended");
                PendingRecreations.Remove(party.Party);
                return;
            }
            RosterSnapshot roster = CaptureRoster(party);
            if (roster.Total != track.LastTotal)
            {
                int delta = roster.Total - track.LastTotal;
                track.ObservedPositiveGrowth += Math.Max(0, delta);
                track.LastTotal = roster.Total;
                ClanAIPostVanilla.WriteExternalLog("PHASE4A_LINK_PRE_SETTLEMENT_STATE" + LinkIdentityFields(track.Identity) +
                    " campaignHour=" + D(CampaignTime.Now.ToHours) + " delta=" + delta +
                    " observedPositiveGrowth=" + track.ObservedPositiveGrowth + " growthSource=unknown" +
                    " roster=" + RosterFields(roster) + PartyContextFields(party));
            }
        }

        private static void ObserveFirstRecreationVisit(MobileParty party, Settlement settlement, bool before)
        {
            if (!RecreationSessionReady || party == null || party.Party == null || settlement == null) return;
            if (!RecreationWindowOpen() && !PendingRecreations.ContainsKey(party.Party)) return;
            SettlementEntriesSeen.Add(party.Party);
            RecreationTrack track;
            if (!PendingRecreations.TryGetValue(party.Party, out track)) return;
            if (!before) track.MissingBoundary = true;
            LogRecreationEnd(before ? "PHASE4A_LINK_FIRST_SETTLEMENT_PRE" : "PHASE4A_LINK_INCOMPLETE",
                party, track, settlement, before, before ? "BeforeSettlementEnteredEvent" : "pre-entry-event-missing");
            PendingRecreations.Remove(party.Party);
        }

        private static void LogRecreationEnd(string kind, MobileParty party, RecreationTrack track,
            Settlement settlement, bool before, string reason)
        {
            double now = CampaignTime.Now.ToHours;
            RosterSnapshot roster = CaptureRoster(party);
            string visitingHero = party.LeaderHero == null ? null : party.LeaderHero.StringId;
            bool complete = before && RecreationWindowOpen() && party.MemberRoster != null &&
                Phase4ARecreationLinkPolicy.CompleteFirstVisit(true, track.InitialBeforeSettlement,
                    track.MissingBoundary, RecreationObservationFault, track.Identity.HeroId, visitingHero, track.Hour, now);
            var classification = Phase4ARecreationLinkPolicy.Classify(true, true, true, complete,
                track.Initial.Total, track.Initial.Heroes);
            int delta = roster.Total - track.LastTotal;
            int observedGrowth = track.ObservedPositiveGrowth + (before ? Math.Max(0, delta) : 0);
            ClanAIPostVanilla.WriteExternalLog(kind + LinkIdentityFields(track.Identity) + IdentityFields(party) +
                " campaignHour=" + D(now) + " createdHour=" + D(track.Hour) +
                " defeatHour=" + D(track.Defeat.Hour) + " defeatedPartyId=" + track.Defeat.Identity.PartyId +
                " defeatToRecreationHours=" + D(track.Hour - track.Defeat.Hour) +
                " recreationToVisitHours=" + D(now - track.Hour) + DestructionFields(track.Defeat) +
                " classification=" + classification + " completeChain=" + complete + " reason=" + reason +
                " firstSettlement=" + SettlementLabel(settlement) + " creationRoster=" + RosterFields(track.Initial) +
                " observedRoster=" + RosterFields(roster) + " preSettlementNetChange=" + (roster.Total - track.Initial.Total) +
                " observedPositiveGrowth=" + observedGrowth + " initialRegularTroops=" + (track.Initial.Total - track.Initial.Heroes) +
                " observationFault=" + RecreationObservationFault + PartyContextFields(party));
        }

        private static string DestructionFields(DefeatLink defeat)
        {
            double hour;
            bool observed = DestroyedParties.TryGetValue(defeat.OldParty, out hour);
            return " destructionObserved=" + observed + " destructionHour=" + (observed ? D(hour) : "<unavailable>");
        }

        private static string LinkIdentityFields(RecreationIdentity identity)
        {
            return identity == null ? " heroId=<unavailable> hero=<unavailable> nativePartyId=<unavailable>" :
                " heroId=" + identity.HeroId + " hero=" + identity.HeroName + " nativePartyId=" + identity.PartyId;
        }
    }
}
