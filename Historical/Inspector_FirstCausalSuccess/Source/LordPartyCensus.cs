using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;

namespace BannerlordInspector
{
    public static class LordPartyCensus
    {
        private static object _published;
        private static long _passes;
        private static DateTime _nextRefreshUtc = DateTime.MinValue;
        private const int RefreshIntervalMs = 250;

        private static string Name(object value)
        {
            return value == null
                ? null
                : value.ToString();
        }

        public static void Tick()
        {
            if (Campaign.Current == null)
            {
                _published = null;
                _nextRefreshUtc = DateTime.MinValue;
                return;
            }

            DateTime now = DateTime.UtcNow;

            // This census used to rebuild every rendered frame. Lord-party
            // strategic state does not need 100+ full-world scans per second,
            // so cap the global snapshot to 4 Hz. Targeted AI tracing can run
            // independently at a higher rate later when we need it.
            if (now < _nextRefreshUtc)
            {
                return;
            }

            _nextRefreshUtc = now.AddMilliseconds(RefreshIntervalMs);
            DateTime started = now;

            try
            {
                var parties =
                    MobileParty.AllLordParties
                        .Where(p => p != null && p.IsActive)
                        .Select(p =>
                        {
                            Hero h = p.LeaderHero;

                            return new
                            {
                                id = p.StringId,
                                name = p.Name.ToString(),

                                hero = h == null
                                    ? null
                                    : new
                                    {
                                        id = h.StringId,
                                        name = h.Name.ToString(),

                                        clan =
                                            h.Clan?.Name?.ToString(),

                                        kingdom =
                                            h.Clan?
                                                .Kingdom?
                                                .Name?
                                                .ToString()
                                    },

                                clan =
                                    p.ActualClan?
                                        .Name?
                                        .ToString(),

                                faction =
                                    p.MapFaction?
                                        .Name?
                                        .ToString(),

                                army =
                                    p.Army?
                                        .Name?
                                        .ToString(),

                                attachedTo =
                                    p.AttachedTo?
                                        .Name?
                                        .ToString(),

                                men =
                                    p.MemberRoster
                                        .TotalManCount,

                                healthy =
                                    p.MemberRoster
                                        .TotalHealthyCount,

                                wounded =
                                    p.MemberRoster
                                        .TotalWounded,

                                prisoners =
                                    p.PrisonRoster
                                        .TotalManCount,

                                food = p.Food,

                                inventoryFood =
                                    p.TotalFoodAtInventory,

                                foodChange =
                                    p.FoodChange,

                                baseFoodChange =
                                    p.BaseFoodChange,

                                foodDays =
                                    p.GetNumDaysForFoodToLast(),

                                morale =
                                    p.Morale,

                                recentEventsMorale =
                                    p.RecentEventsMorale,

                                speed =
                                    p.Speed,

                                currentSettlement =
                                    p.CurrentSettlement?
                                        .Name?
                                        .ToString(),

                                lastVisitedSettlement =
                                    p.LastVisitedSettlement?
                                        .Name?
                                        .ToString(),

                                homeSettlement =
                                    p.HomeSettlement?
                                        .Name?
                                        .ToString(),

                                targetSettlement =
                                    p.TargetSettlement?
                                        .Name?
                                        .ToString(),

                                targetParty =
                                    p.TargetParty?
                                        .Name?
                                        .ToString(),

                                shortTermTargetSettlement =
                                    p.ShortTermTargetSettlement?
                                        .Name?
                                        .ToString(),

                                shortTermTargetParty =
                                    p.ShortTermTargetParty?
                                        .Name?
                                        .ToString(),

                                behavior =
                                    p.ShortTermBehavior
                                        .ToString(),

                                moving =
                                    p.IsMoving,

                                disorganized =
                                    p.IsDisorganized,

                                disbanding =
                                    p.IsDisbanding,

                                engaging =
                                    p.IsEngaging,

                                besiegedSettlement =
                                    p.BesiegedSettlement?
                                        .Name?
                                        .ToString(),

                                inMapEvent =
                                    p.MapEvent != null,

                                isMainParty =
                                    p.IsMainParty
                            };
                        })
                        .OrderBy(p => p.faction)
                        .ThenBy(p => p.name)
                        .ToArray();

                _passes++;

                _published = new
                {
                    ready = true,
                    version = "0.2",

                    asOf = new
                    {
                        passes = _passes,
                        refreshIntervalMs = RefreshIntervalMs,

                        passTookMs =
                            Math.Round(
                                (
                                    DateTime.UtcNow -
                                    started
                                ).TotalMilliseconds,
                                2
                            )
                    },

                    count = parties.Length,

                    independent =
                        parties.Count(
                            p => p.army == null
                        ),

                    inArmies =
                        parties.Count(
                            p => p.army != null
                        ),

                    parties
                };
            }
            catch (Exception ex)
            {
                InspectorLog.Error(
                    "Lord party census failed.",
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
                    version = "0.2",
                    note =
                        "Lord party census has not completed."
                };
            }

            return _published;
        }
    }
}
