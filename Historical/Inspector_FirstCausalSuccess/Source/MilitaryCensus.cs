using TaleWorlds.CampaignSystem.Roster;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;

namespace BannerlordInspector
{
    public static class MilitaryCensus
    {
        private static object _published;
        private static long _passes;
        private static DateTime _lastRefreshUtc = DateTime.MinValue;

        // Army state is strategic data. Refreshing it every rendered frame
        // wastes main-thread time, especially at high FPS. Two snapshots per
        // second is still far faster than campaign-level decisions need.
        private const int RefreshIntervalMs = 500;

        private static int RosterCount(TroopRoster roster)
        {
            if (roster == null)
                return 0;

            return roster.TotalManCount;
        }

        private static MapEventParty FindMapEventParty(
            MapEvent mapEvent,
            PartyBase party,
            out MapEventSide side)
        {
            side = null;

            if (mapEvent == null || party == null)
                return null;

            foreach (MapEventSide candidate in mapEvent.Sides)
            {
                if (candidate == null)
                    continue;

                foreach (MapEventParty eventParty in candidate.Parties)
                {
                    if (
                        eventParty != null &&
                        eventParty.Party == party
                    )
                    {
                        side = candidate;
                        return eventParty;
                    }
                }
            }

            return null;
        }

        public static void Tick()
        {
            if (Campaign.Current == null)
            {
                _published = null;
                _lastRefreshUtc = DateTime.MinValue;
                return;
            }

            DateTime now = DateTime.UtcNow;

            if (
                _published != null &&
                (now - _lastRefreshUtc).TotalMilliseconds < RefreshIntervalMs
            )
            {
                return;
            }

            // Mark the refresh window before doing the expensive scan so a
            // transient failure cannot make us retry every rendered frame.
            _lastRefreshUtc = now;

            DateTime started = now;

            try
            {
                var armies =
                    new List<object>();

                var seen =
                    new HashSet<Army>();

                foreach (
                    MobileParty mobileParty
                    in MobileParty.All
                )
                {
                    Army army =
                        mobileParty?.Army;

                    if (
                        army == null ||
                        !seen.Add(army)
                    )
                    {
                        continue;
                    }

                    var parties =
                        new List<object>();

                    foreach (
                        MobileParty party
                        in army.Parties
                    )
                    {
                        if (
                            party == null ||
                            party.Party == null
                        )
                        {
                            continue;
                        }

                        PartyBase pb =
                            party.Party;

                        int foodDays;

                        try
                        {
                            foodDays =
                                party
                                    .GetNumDaysForFoodToLast();
                        }
                        catch
                        {
                            foodDays = -1;
                        }

                        MapEvent mapEvent =
                            pb.MapEvent;

                        MapEventSide eventSide;
                        MapEventParty eventParty =
                            FindMapEventParty(
                                mapEvent,
                                pb,
                                out eventSide
                            );

                        object battle = null;

                        if (mapEvent != null)
                        {
                            battle = new
                            {
                                active = true,

                                eventType =
                                    mapEvent.EventType
                                        .ToString(),

                                state =
                                    mapEvent.State
                                        .ToString(),

                                battleState =
                                    mapEvent.BattleState
                                        .ToString(),

                                settlement =
                                    mapEvent.MapEventSettlement
                                        ?.Name
                                        ?.ToString(),

                                isFieldBattle =
                                    mapEvent.IsFieldBattle,

                                isRaid =
                                    mapEvent.IsRaid,

                                isSiegeAssault =
                                    mapEvent.IsSiegeAssault,

                                isSallyOut =
                                    mapEvent.IsSallyOut,

                                isSiegeOutside =
                                    mapEvent.IsSiegeOutside,

                                isSiegeAmbush =
                                    mapEvent.IsSiegeAmbush,

                                isBlockade =
                                    mapEvent.IsBlockade,

                                finalized =
                                    mapEvent.IsFinalized,

                                hasWinner =
                                    mapEvent.HasWinner,

                                winningSide =
                                    mapEvent.WinningSide
                                        .ToString(),

                                defeatedSide =
                                    mapEvent.DefeatedSide
                                        .ToString(),

                                endedByRetreat =
                                    mapEvent.EndedByRetreat,

                                retreatingSide =
                                    mapEvent.RetreatingSide
                                        .ToString(),

                                side =
                                    eventSide
                                        ?.MissionSide
                                        .ToString(),

                                sideMorale =
                                    eventSide != null
                                        ? eventSide.GetSideMorale()
                                        : (float?)null,

                                sideTroops =
    eventSide
        ?.TroopCount,

sideCasualties =
    eventSide
        ?.TroopCasualties,

healthyAtBattleStart =
    eventParty
        ?.HealthyManCountAtStart,

participatingTroops =
    eventParty
        ?.ParticipatingTroopCount,

                                diedInBattle =
                                    eventParty != null
                                        ? RosterCount(
                                            eventParty.DiedInBattle
                                        )
                                        : (int?)null,

                                woundedInBattle =
                                    eventParty != null
                                        ? RosterCount(
                                            eventParty.WoundedInBattle
                                        )
                                        : (int?)null,

                                routedInBattle =
                                    eventParty != null
                                        ? RosterCount(
                                            eventParty.RoutedInBattle
                                        )
                                        : (int?)null,

                                contribution =
                                    eventParty
                                        ?.ContributionToBattle,

                                gainedMorale =
                                    eventParty
                                        ?.GainedMorale,

                                plunderedGold =
                                    eventParty
                                        ?.PlunderedGold,

                                goldLost =
                                    eventParty
                                        ?.GoldLost
                            };
                        }

                        parties.Add(
                            new
                            {
                                id =
                                    pb.Id,

                                name =
                                    party.Name
                                        ?.ToString(),

                                men =
                                    pb.NumberOfAllMembers,

                                healthy =
                                    pb.NumberOfHealthyMembers,

                                wounded =
                                    pb.NumberOfWoundedTotalMembers,

                                strength =
                                    pb.EstimatedStrength,

                                food =
                                    party.Food,

                                inventoryFood =
                                    party.TotalFoodAtInventory,

                                foodChange =
                                    party.FoodChange,

                                baseFoodChange =
                                    party.BaseFoodChange,

                                foodDays,

                                starving =
                                    pb.IsStarving,

                                daysStarving =
                                    pb.DaysStarving,

                                remainingFoodPercentage =
                                    pb.RemainingFoodPercentage,

                                morale =
                                    party.Morale,

                                speed =
                                    party.Speed,

                                currentSettlement =
                                    party.CurrentSettlement
                                        ?.Name
                                        ?.ToString(),

                                targetSettlement =
                                    party.TargetSettlement
                                        ?.Name
                                        ?.ToString(),

                                besiegedSettlement =
                                    party.BesiegedSettlement
                                        ?.Name
                                        ?.ToString(),

                                moving =
                                    party.IsMoving,

                                disorganized =
                                    party.IsDisorganized,

                                battle
                            }
                        );
                    }

                    armies.Add(
                        new
                        {
                            name =
                                army.Name
                                    ?.ToString(),

                            owner =
                                army.ArmyOwner
                                    ?.Name
                                    ?.ToString(),

                            kingdom =
                                army.Kingdom
                                    ?.Name
                                    ?.ToString(),

                            cohesion =
                                army.Cohesion,

                            dailyCohesionChange =
                                army.DailyCohesionChange,

                            morale =
                                army.Morale,

                            strength =
                                army.EstimatedStrength,

                            men =
                                army.TotalManCount,

                            healthy =
                                army.TotalHealthyMembers,

                            regulars =
                                army.TotalRegularCount,

                            dispersing =
                                army.IsDispersing,

                            ready =
                                army.IsReady,

                            leaderParty =
                                army.LeaderParty
                                    ?.Name
                                    ?.ToString(),

                            objective =
                                army.AiBehaviorObject
                                    ?.ToString(),

                            parties =
                                parties.ToArray()
                        }
                    );
                }

                _passes++;

                DateTime completed =
                    DateTime.UtcNow;

                _published =
                    new
                    {
                        ready = true,

                        version = "0.3",

                        asOf = new
                        {
                            secondsOld = 0.0,

                            passTookMs =
                                Math.Round(
                                    (
                                        completed -
                                        started
                                    ).TotalMilliseconds,
                                    2
                                ),

                            passes =
                                _passes,

                            refreshIntervalMs =
                                RefreshIntervalMs
                        },

                        armyCount =
                            armies.Count,

                        armies =
                            armies.ToArray()
                    };
            }
            catch (Exception ex)
            {
                InspectorLog.Error(
                    "Military census failed.",
                    ex
                );
            }
        }

        public static object Current()
        {
            if (_published == null)
            {
                return new
                {
                    ready = false,
                    version = "0.3",

                    note =
                        "Military census has not completed."
                };
            }

            return _published;
        }
    }
}
