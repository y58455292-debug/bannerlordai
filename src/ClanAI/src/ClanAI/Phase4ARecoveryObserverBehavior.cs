using System;
using System.Collections.Generic;
using System.Globalization;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace ClanAI
{
    public sealed class Phase4ARecoveryObserverBehavior
        : CampaignBehaviorBase
    {
        private const double MaxTrackHours = 168.0;
        private const double PeriodicSnapshotHours = 6.0;

        private sealed class RosterSnapshot
        {
            internal int Total;
            internal int Healthy;
            internal int Wounded;
            internal int Heroes;
            internal readonly int[] Tiers = new int[8];
        }

        private sealed class SettlementSnapshot
        {
            internal bool VolunteerObserved;
            internal int VolunteerTotal;
            internal readonly int[] VolunteerTiers = new int[8];
            internal bool GarrisonObserved;
            internal RosterSnapshot Garrison;
        }

        private sealed class VisitState
        {
            internal Settlement Settlement;
            internal double EnteredAtHours;
            internal RosterSnapshot PartyBefore;
            internal SettlementSnapshot SettlementBefore;
        }

        private sealed class TrackState
        {
            internal string PartyId;
            internal string ActorName;
            internal string StartReason;
            internal double StartedAtHours;
            internal double LastSnapshotHours;
            internal int LastPartyTotal;
            internal bool HasVisitedSettlement;
            internal VisitState Visit;
        }

        private static readonly Dictionary<string, TrackState> Tracked =
            new Dictionary<string, TrackState>(StringComparer.Ordinal);

        private static long _started;
        private static long _completed;
        private static long _settlementVisits;
        private static long _growthEvents;

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent
                .AddNonSerializedListener(
                    this,
                    new Action<CampaignGameStarter>(
                        OnSessionLaunched));

            CampaignEvents.MobilePartyCreated
                .AddNonSerializedListener(
                    this,
                    new Action<MobileParty>(
                        OnMobilePartyCreated));

            CampaignEvents.MobilePartyDestroyed
                .AddNonSerializedListener(
                    this,
                    new Action<MobileParty, PartyBase>(
                        OnMobilePartyDestroyed));

            CampaignEvents.MapEventEnded
                .AddNonSerializedListener(
                    this,
                    new Action<MapEvent>(
                        OnMapEventEnded));

            CampaignEvents.HourlyTickPartyEvent
                .AddNonSerializedListener(
                    this,
                    new Action<MobileParty>(
                        OnHourlyPartyTick));

            CampaignEvents.BeforeSettlementEnteredEvent
                .AddNonSerializedListener(
                    this,
                    new Action<MobileParty, Settlement, Hero>(
                        OnBeforeSettlementEntered));

            CampaignEvents.AfterSettlementEntered
                .AddNonSerializedListener(
                    this,
                    new Action<MobileParty, Settlement, Hero>(
                        OnAfterSettlementEntered));

            CampaignEvents.OnSettlementLeftEvent
                .AddNonSerializedListener(
                    this,
                    new Action<MobileParty, Settlement>(
                        OnSettlementLeft));
        }

        public override void SyncData(IDataStore dataStore)
        {
        }
        private static void OnSessionLaunched(
            CampaignGameStarter starter)
        {
            Tracked.Clear();
            _started = 0;
            _completed = 0;
            _settlementVisits = 0;
            _growthEvents = 0;

            ClanAIPostVanilla.WriteExternalLog(
                "PHASE4A_RECOVERY_RESET" +
                " mode=observe-only" +
                " severeRatio=" +
                F(Phase4ARecoveryClassificationPolicy
                    .SevereDepletionRatio) +
                " maxTrackHours=" +
                D(MaxTrackHours));

            foreach (MobileParty party in MobileParty.All)
            {
                if (EligibleLordParty(party) &&
                    Phase4ARecoveryClassificationPolicy
                        .IsSeverelyDepleted(
                            party.PartySizeRatio))
                {
                    StartTracking(
                        party,
                        "severe-depletion-at-session-start",
                        false);
                }
            }
        }

        private static void OnMobilePartyCreated(
            MobileParty party)
        {
            if (!EligibleLordParty(party))
                return;

            StartTracking(
                party,
                "mobile-party-created",
                true);
        }

        private static void OnMobilePartyDestroyed(
            MobileParty party,
            PartyBase destroyerParty)
        {
            TrackState state;
            if (!TryGetTrack(party, out state))
                return;

            RosterSnapshot roster =
                CaptureRoster(party);

            LogPartySnapshot(
                "PHASE4A_RECOVERY_END",
                party,
                state,
                roster,
                "destroyed",
                Phase4ARecoverySource.None,
                0);

            Tracked.Remove(state.PartyId);
            _completed++;
        }

        private static void OnMapEventEnded(
            MapEvent mapEvent)
        {
            if (mapEvent == null)
                return;

            ObserveMapEventSide(
                mapEvent,
                BattleSideEnum.Attacker);

            ObserveMapEventSide(
                mapEvent,
                BattleSideEnum.Defender);
        }

        private static void ObserveMapEventSide(
            MapEvent mapEvent,
            BattleSideEnum side)
        {
            var parties =
                mapEvent.PartiesOnSide(side);

            if (parties == null)
                return;

            for (int i = 0; i < parties.Count; i++)
            {
                PartyBase partyBase =
                    parties[i].Party;

                MobileParty party =
                    partyBase == null
                        ? null
                        : partyBase.MobileParty;

                if (!EligibleLordParty(party))
                    continue;

                bool severelyDepleted =
                    Phase4ARecoveryClassificationPolicy
                        .IsSeverelyDepleted(
                            party.PartySizeRatio);

                TrackState state;
                bool alreadyTracked =
                    TryGetTrack(
                        party,
                        out state);

                if (!alreadyTracked &&
                    severelyDepleted)
                {
                    string reason =
                        mapEvent.DefeatedSide == side
                            ? "post-battle-severe-depletion-defeated-side"
                            : "post-battle-severe-depletion";

                    StartTracking(
                        party,
                        reason,
                        false);

                    TryGetTrack(
                        party,
                        out state);
                }

                if (state != null)
                {
                    RosterSnapshot roster =
                        CaptureRoster(party);

                    LogPartySnapshot(
                        "PHASE4A_POST_BATTLE",
                        party,
                        state,
                        roster,
                        "side=" + side +
                        ";defeatedSide=" +
                        mapEvent.DefeatedSide,
                        Phase4ARecoverySource.None,
                        roster.Total -
                            state.LastPartyTotal);

                    state.LastPartyTotal =
                        roster.Total;
                    state.LastSnapshotHours =
                        CampaignTime.Now.ToHours;
                }
            }
        }
        private static void OnHourlyPartyTick(
            MobileParty party)
        {
            if (!EligibleLordParty(party))
                return;

            TrackState state;
            if (!TryGetTrack(
                    party,
                    out state))
            {
                if (Phase4ARecoveryClassificationPolicy
                    .IsSeverelyDepleted(
                        party.PartySizeRatio))
                {
                    StartTracking(
                        party,
                        "severe-depletion-hourly",
                        false);
                }

                return;
            }

            double now =
                CampaignTime.Now.ToHours;

            if (now - state.StartedAtHours >
                MaxTrackHours)
            {
                RosterSnapshot finalRoster =
                    CaptureRoster(party);

                LogPartySnapshot(
                    "PHASE4A_RECOVERY_END",
                    party,
                    state,
                    finalRoster,
                    "track-window-expired",
                    Phase4ARecoverySource.None,
                    finalRoster.Total -
                        state.LastPartyTotal);

                Tracked.Remove(state.PartyId);
                _completed++;
                return;
            }

            RosterSnapshot roster =
                CaptureRoster(party);

            int delta =
                roster.Total -
                state.LastPartyTotal;

            bool periodic =
                now - state.LastSnapshotHours >=
                PeriodicSnapshotHours;

            if (delta != 0 || periodic)
            {
                Phase4ARecoverySource source =
                    Phase4ARecoverySource.None;

                if (delta > 0 &&
                    state.Visit == null)
                {
                    source =
                        Phase4ARecoveryClassificationPolicy
                            .ClassifyIncrease(
                                delta,
                                false,
                                false,
                                0,
                                false,
                                0);

                    _growthEvents++;
                }

                LogPartySnapshot(
                    "PHASE4A_RECOVERY_STATE",
                    party,
                    state,
                    roster,
                    state.Visit == null
                        ? "hourly"
                        : "hourly-inside-settlement",
                    source,
                    delta);

                state.LastPartyTotal =
                    roster.Total;
                state.LastSnapshotHours =
                    now;
            }
        }

        private static void OnBeforeSettlementEntered(
            MobileParty party,
            Settlement settlement,
            Hero hero)
        {
            TrackState state;
            if (!TryGetTrack(
                    party,
                    out state) ||
                settlement == null)
            {
                return;
            }

            double now =
                CampaignTime.Now.ToHours;

            RosterSnapshot partyBefore =
                CaptureRoster(party);

            int preVisitDelta =
                partyBefore.Total -
                state.LastPartyTotal;

            if (preVisitDelta > 0)
            {
                _growthEvents++;

                LogPartySnapshot(
                    "PHASE4A_RECOVERY_STATE",
                    party,
                    state,
                    partyBefore,
                    "growth-before-settlement-entry",
                    Phase4ARecoverySource
                        .OtherOrUnknownNativeSource,
                    preVisitDelta);
            }

            bool firstVisit =
                !state.HasVisitedSettlement;

            SettlementSnapshot settlementBefore =
                CaptureSettlement(
                    settlement);

            state.Visit =
                new VisitState
                {
                    Settlement = settlement,
                    EnteredAtHours = now,
                    PartyBefore = partyBefore,
                    SettlementBefore =
                        settlementBefore
                };

            state.HasVisitedSettlement = true;
            state.LastPartyTotal =
                partyBefore.Total;
            state.LastSnapshotHours = now;

            if (firstVisit)
            {
                ClanAIPostVanilla.WriteExternalLog(
                    "PHASE4A_FIRST_SETTLEMENT_PRE" +
                    IdentityFields(party) +
                    " campaignHour=" + D(now) +
                    " elapsedHours=" +
                        D(now - state.StartedAtHours) +
                    " settlement=" +
                        SettlementLabel(settlement) +
                    " troopsBeforeFirstSettlement=" +
                        partyBefore.Total +
                    " startReason=" +
                        state.StartReason +
                    RosterFields(partyBefore) +
                    PartyContextFields(party));
            }

            LogSettlementEvent(
                "PHASE4A_SETTLEMENT_ENTER",
                party,
                state,
                settlement,
                partyBefore,
                settlementBefore,
                null,
                null,
                Phase4ARecoverySource.None);
        }
        private static void OnAfterSettlementEntered(
            MobileParty party,
            Settlement settlement,
            Hero hero)
        {
            TrackState state;
            if (!TryGetTrack(
                    party,
                    out state) ||
                state.Visit == null ||
                state.Visit.Settlement !=
                    settlement)
            {
                return;
            }

            RosterSnapshot partyAfterEnter =
                CaptureRoster(party);

            SettlementSnapshot settlementAfterEnter =
                CaptureSettlement(
                    settlement);

            LogSettlementEvent(
                "PHASE4A_SETTLEMENT_ENTERED",
                party,
                state,
                settlement,
                state.Visit.PartyBefore,
                state.Visit.SettlementBefore,
                partyAfterEnter,
                settlementAfterEnter,
                Phase4ARecoverySource.None);
        }

        private static void OnSettlementLeft(
            MobileParty party,
            Settlement settlement)
        {
            TrackState state;
            if (!TryGetTrack(
                    party,
                    out state) ||
                state.Visit == null)
            {
                return;
            }

            VisitState visit =
                state.Visit;

            if (settlement == null)
                settlement = visit.Settlement;

            RosterSnapshot partyAfter =
                CaptureRoster(party);

            SettlementSnapshot settlementAfter =
                CaptureSettlement(
                    settlement);

            int partyGain =
                partyAfter.Total -
                visit.PartyBefore.Total;

            int volunteerDecrease =
                visit.SettlementBefore
                    .VolunteerTotal -
                settlementAfter
                    .VolunteerTotal;

            int garrisonDecrease =
                GarrisonTotal(
                    visit.SettlementBefore) -
                GarrisonTotal(
                    settlementAfter);

            Phase4ARecoverySource source =
                Phase4ARecoveryClassificationPolicy
                    .ClassifyIncrease(
                        partyGain,
                        true,
                        visit.SettlementBefore
                            .VolunteerObserved &&
                            settlementAfter
                                .VolunteerObserved,
                        volunteerDecrease,
                        visit.SettlementBefore
                            .GarrisonObserved &&
                            settlementAfter
                                .GarrisonObserved,
                        garrisonDecrease);

            if (partyGain > 0)
                _growthEvents++;

            _settlementVisits++;

            LogSettlementEvent(
                "PHASE4A_SETTLEMENT_EXIT",
                party,
                state,
                settlement,
                visit.PartyBefore,
                visit.SettlementBefore,
                partyAfter,
                settlementAfter,
                source);

            state.Visit = null;
            state.LastPartyTotal =
                partyAfter.Total;
            state.LastSnapshotHours =
                CampaignTime.Now.ToHours;
        }

        private static void StartTracking(
            MobileParty party,
            string reason,
            bool createdParty)
        {
            if (!EligibleLordParty(party))
                return;

            string key =
                PartyKey(party);

            if (string.IsNullOrEmpty(key) ||
                Tracked.ContainsKey(key))
            {
                return;
            }

            double now =
                CampaignTime.Now.ToHours;

            RosterSnapshot roster =
                CaptureRoster(party);

            var state =
                new TrackState
                {
                    PartyId = key,
                    ActorName =
                        party.LeaderHero.Name.ToString(),
                    StartReason = reason,
                    StartedAtHours = now,
                    LastSnapshotHours = now,
                    LastPartyTotal = roster.Total,
                    HasVisitedSettlement =
                        party.CurrentSettlement != null,
                    Visit = null
                };

            Tracked.Add(
                key,
                state);

            _started++;

            Phase4ARecoverySource initialSource =
                Phase4ARecoveryClassificationPolicy
                    .ClassifyInitialPresence(
                        createdParty,
                        party.CurrentSettlement != null,
                        roster.Total);

            LogPartySnapshot(
                "PHASE4A_RECOVERY_START",
                party,
                state,
                roster,
                reason,
                initialSource,
                0);
        }
        private static bool EligibleLordParty(
            MobileParty party)
        {
            return
                party != null &&
                party.IsLordParty &&
                party != MobileParty.MainParty &&
                party.LeaderHero != null &&
                party.ActualClan != null &&
                party.ActualClan !=
                    Clan.PlayerClan;
        }

        private static bool TryGetTrack(
            MobileParty party,
            out TrackState state)
        {
            state = null;

            string key =
                PartyKey(party);

            return
                !string.IsNullOrEmpty(key) &&
                Tracked.TryGetValue(
                    key,
                    out state);
        }

        private static string PartyKey(
            MobileParty party)
        {
            if (party == null)
                return null;

            if (!string.IsNullOrEmpty(
                    party.StringId))
            {
                return party.StringId;
            }

            if (party.LeaderHero != null &&
                !string.IsNullOrEmpty(
                    party.LeaderHero.StringId))
            {
                return
                    party.LeaderHero.StringId;
            }

            return
                party.Name.ToString();
        }

        private static RosterSnapshot CaptureRoster(
            MobileParty party)
        {
            if (party == null)
                return new RosterSnapshot();

            return CaptureRoster(
                party.MemberRoster,
                party.Party);
        }

        private static RosterSnapshot CaptureRoster(
            TroopRoster roster,
            PartyBase partyBase)
        {
            var snapshot =
                new RosterSnapshot();

            if (roster == null)
                return snapshot;

            snapshot.Total =
                roster.TotalManCount;

            snapshot.Wounded =
                roster.TotalWounded;

            snapshot.Healthy =
                Math.Max(
                    0,
                    snapshot.Total -
                    snapshot.Wounded);

            var elements =
                roster.GetTroopRoster();

            for (int i = 0;
                 i < elements.Count;
                 i++)
            {
                TroopRosterElement element =
                    elements[i];

                CharacterObject character =
                    element.Character;

                if (character == null ||
                    element.Number <= 0)
                {
                    continue;
                }

                if (character.IsHero)
                {
                    snapshot.Heroes +=
                        element.Number;

                    continue;
                }

                int tier =
                    character.Tier;

                int bucket =
                    tier < 0
                        ? 0
                        : Math.Min(
                            7,
                            tier);

                snapshot.Tiers[bucket] +=
                    element.Number;
            }

            return snapshot;
        }

        private static SettlementSnapshot
            CaptureSettlement(
                Settlement settlement)
        {
            var snapshot =
                new SettlementSnapshot();

            if (settlement == null)
                return snapshot;

            if (settlement.Notables != null)
            {
                snapshot.VolunteerObserved = true;

                for (int i = 0;
                     i < settlement.Notables.Count;
                     i++)
                {
                    Hero notable =
                        settlement.Notables[i];

                    if (notable == null ||
                        notable.VolunteerTypes == null)
                    {
                        continue;
                    }

                    CharacterObject[] volunteers =
                        notable.VolunteerTypes;

                    for (int j = 0;
                         j < volunteers.Length;
                         j++)
                    {
                        CharacterObject volunteer =
                            volunteers[j];

                        if (volunteer == null)
                            continue;

                        snapshot.VolunteerTotal++;

                        int bucket =
                            Math.Min(
                                7,
                                Math.Max(
                                    0,
                                    volunteer.Tier));

                        snapshot
                            .VolunteerTiers[bucket]++;
                    }
                }
            }

            MobileParty garrison =
                settlement.Town == null
                    ? null
                    : settlement.Town
                        .GarrisonParty;

            if (garrison != null)
            {
                snapshot.GarrisonObserved = true;
                snapshot.Garrison =
                    CaptureRoster(garrison);
            }

            return snapshot;
        }
        private static int GarrisonTotal(
            SettlementSnapshot snapshot)
        {
            return
                snapshot != null &&
                snapshot.GarrisonObserved &&
                snapshot.Garrison != null
                    ? snapshot.Garrison.Total
                    : 0;
        }

        private static void LogPartySnapshot(
            string kind,
            MobileParty party,
            TrackState state,
            RosterSnapshot roster,
            string phase,
            Phase4ARecoverySource source,
            int delta)
        {
            double now =
                CampaignTime.Now.ToHours;

            ClanAIPostVanilla.WriteExternalLog(
                kind +
                IdentityFields(party) +
                " campaignHour=" + D(now) +
                " elapsedHours=" +
                    D(now - state.StartedAtHours) +
                " phase=" + phase +
                " delta=" + delta +
                " source=" + source +
                " startReason=" +
                    state.StartReason +
                " visitedSettlement=" +
                    state.HasVisitedSettlement +
                RosterFields(roster) +
                PartyContextFields(party) +
                " tracked=" + Tracked.Count +
                " starts=" + _started +
                " visits=" + _settlementVisits +
                " growthEvents=" + _growthEvents +
                " completed=" + _completed);
        }

        private static void LogSettlementEvent(
            string kind,
            MobileParty party,
            TrackState state,
            Settlement settlement,
            RosterSnapshot beforeParty,
            SettlementSnapshot beforeSettlement,
            RosterSnapshot afterParty,
            SettlementSnapshot afterSettlement,
            Phase4ARecoverySource source)
        {
            double now =
                CampaignTime.Now.ToHours;

            int partyDelta =
                afterParty == null
                    ? 0
                    : afterParty.Total -
                        beforeParty.Total;

            int volunteerDelta =
                beforeSettlement == null ||
                afterSettlement == null ||
                !beforeSettlement
                    .VolunteerObserved ||
                !afterSettlement
                    .VolunteerObserved
                    ? 0
                    : afterSettlement
                        .VolunteerTotal -
                      beforeSettlement
                        .VolunteerTotal;

            int garrisonDelta =
                beforeSettlement == null ||
                afterSettlement == null ||
                !beforeSettlement
                    .GarrisonObserved ||
                !afterSettlement
                    .GarrisonObserved
                    ? 0
                    : GarrisonTotal(
                          afterSettlement) -
                      GarrisonTotal(
                          beforeSettlement);

            ClanAIPostVanilla.WriteExternalLog(
                kind +
                IdentityFields(party) +
                " campaignHour=" + D(now) +
                " elapsedHours=" +
                    D(now - state.StartedAtHours) +
                " settlement=" +
                    SettlementLabel(settlement) +
                " visitAgeHours=" +
                    D(state.Visit == null
                        ? 0.0
                        : now -
                          state.Visit
                              .EnteredAtHours) +
                " source=" + source +
                " partyDelta=" +
                    partyDelta +
                " volunteerDelta=" +
                    volunteerDelta +
                " garrisonDelta=" +
                    garrisonDelta +
                " before" +
                    RosterFields(beforeParty) +
                " after" +
                    (afterParty == null
                        ? "=<not-captured>"
                        : RosterFields(afterParty)) +
                " volunteerBefore=" +
                    VolunteerFields(
                        beforeSettlement) +
                " volunteerAfter=" +
                    VolunteerFields(
                        afterSettlement) +
                " garrisonBefore=" +
                    GarrisonFields(
                        beforeSettlement) +
                " garrisonAfter=" +
                    GarrisonFields(
                        afterSettlement) +
                PartyContextFields(party));
        }

        private static string IdentityFields(
            MobileParty party)
        {
            Clan clan =
                party == null
                    ? null
                    : party.ActualClan;

            Kingdom kingdom =
                clan == null
                    ? null
                    : clan.Kingdom;

            return
                " partyId=" +
                    (party == null
                        ? "<none>"
                        : PartyKey(party)) +
                " party=" +
                    (party == null
                        ? "<none>"
                        : Safe(
                            party.Name.ToString())) +
                " actor=" +
                    (party == null ||
                     party.LeaderHero == null
                        ? "<none>"
                        : Safe(
                            party.LeaderHero
                                .Name.ToString())) +
                " clan=" +
                    (clan == null
                        ? "<none>"
                        : Safe(
                            clan.Name.ToString())) +
                " kingdom=" +
                    (kingdom == null
                        ? "<none>"
                        : Safe(
                            kingdom.Name.ToString()));
        }

        private static string PartyContextFields(
            MobileParty party)
        {
            if (party == null)
                return string.Empty;

            int limit =
                party.Party == null
                    ? 0
                    : party.Party
                        .PartySizeLimit;

            return
                " partyLimit=" + limit +
                " partyRatio=" +
                    F(party.PartySizeRatio) +
                " foodDays=" +
                    party.GetNumDaysForFoodToLast() +
                " food=" +
                    F(party.Food) +
                " currentSettlement=" +
                    SettlementLabel(
                        party.CurrentSettlement) +
                " targetSettlement=" +
                    SettlementLabel(
                        party.TargetSettlement ??
                        party.ShortTermTargetSettlement ??
                        party.BesiegedSettlement);
        }
        private static string RosterFields(
            RosterSnapshot roster)
        {
            if (roster == null)
                return "=<none>";

            return
                "Total=" + roster.Total +
                ",Healthy=" + roster.Healthy +
                ",Wounded=" + roster.Wounded +
                ",Heroes=" + roster.Heroes +
                ",T0=" + roster.Tiers[0] +
                ",T1=" + roster.Tiers[1] +
                ",T2=" + roster.Tiers[2] +
                ",T3=" + roster.Tiers[3] +
                ",T4=" + roster.Tiers[4] +
                ",T5=" + roster.Tiers[5] +
                ",T6=" + roster.Tiers[6] +
                ",T7Plus=" + roster.Tiers[7];
        }

        private static string VolunteerFields(
            SettlementSnapshot snapshot)
        {
            if (snapshot == null ||
                !snapshot.VolunteerObserved)
            {
                return "<unavailable>";
            }

            return
                "Total=" +
                    snapshot.VolunteerTotal +
                ",T0=" +
                    snapshot.VolunteerTiers[0] +
                ",T1=" +
                    snapshot.VolunteerTiers[1] +
                ",T2=" +
                    snapshot.VolunteerTiers[2] +
                ",T3=" +
                    snapshot.VolunteerTiers[3] +
                ",T4=" +
                    snapshot.VolunteerTiers[4] +
                ",T5=" +
                    snapshot.VolunteerTiers[5] +
                ",T6=" +
                    snapshot.VolunteerTiers[6] +
                ",T7Plus=" +
                    snapshot.VolunteerTiers[7];
        }

        private static string GarrisonFields(
            SettlementSnapshot snapshot)
        {
            if (snapshot == null ||
                !snapshot.GarrisonObserved ||
                snapshot.Garrison == null)
            {
                return "<unavailable>";
            }

            return RosterFields(
                snapshot.Garrison);
        }

        private static string SettlementLabel(
            Settlement settlement)
        {
            if (settlement == null)
                return "<none>";

            return
                Safe(settlement.Name.ToString()) +
                "[" +
                (string.IsNullOrEmpty(
                    settlement.StringId)
                    ? "<no-id>"
                    : settlement.StringId) +
                "]";
        }

        private static string Safe(
            string value)
        {
            return
                string.IsNullOrEmpty(value)
                    ? "<none>"
                    : value.Replace(
                        " ",
                        "_");
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
    }
}