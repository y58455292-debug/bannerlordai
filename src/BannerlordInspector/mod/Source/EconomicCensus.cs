using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;

namespace BannerlordInspector
{
    /// <summary>
    /// Lightweight economic census.
    ///
    /// IMPORTANT:
    /// - Never scans the whole economy in one frame.
    /// - Processes ONE town per frame.
    /// - Readers only receive a completed snapshot.
    /// - Designed to be expanded only after performance testing.
    /// </summary>
    public static class EconomicCensus
    {
        private sealed class TownRecord
        {
            public string Id;
            public string Name;

            public float Prosperity;
            public float ProsperityChange;

            public float Food;
            public float FoodChange;
            public float FoodChangeWithoutMarketStocks;

            public float Security;
            public float SecurityChange;

            public float Loyalty;
            public float LoyaltyChange;

            public int Garrison;
            public bool UnderSiege;
        }

        private sealed class Snapshot
        {
            public DateTime CompletedAt;
            public double PassMs;
            public long Passes;

            public int Year;
            public int DayOfYear;
            public int Hour;

            public List<TownRecord> Towns = new List<TownRecord>();
        }

        private static volatile Snapshot _published;

        private static Snapshot _building;
        private static List<Settlement> _townSnapshot;
        private static int _cursor;
        private static DateTime _passStarted;
        private static long _passes;

        /// <summary>
        /// Called once per frame.
        /// Does at most ONE town's economic work.
        /// </summary>
        public static void Tick()
        {
            try
            {
                if (Campaign.Current == null)
                {
                    _building = null;
                    _townSnapshot = null;
                    _cursor = 0;
                    return;
                }

                if (_building == null)
                    BeginPass();

                if (_townSnapshot == null || _townSnapshot.Count == 0)
                {
                    FinishPass();
                    return;
                }

                // PERFORMANCE RULE:
                // Exactly one town per frame for v0.1.
                if (_cursor < _townSnapshot.Count)
                {
                    ReadTown(_townSnapshot[_cursor]);
                    _cursor++;
                }

                if (_cursor >= _townSnapshot.Count)
                    FinishPass();
            }
            catch (Exception ex)
            {
                InspectorLog.Error(
                    "Economic census slice failed; restarting pass.",
                    ex);

                _building = null;
                _townSnapshot = null;
                _cursor = 0;
            }
        }

        private static void BeginPass()
        {
            _passStarted = DateTime.UtcNow;
            _cursor = 0;

            // Only towns. No castles, villages, hideouts or heroes.
            _townSnapshot =
                Settlement.All?
                    .Where(s => s != null && s.IsTown)
                    .ToList()
                ?? new List<Settlement>();

            CampaignTime now = CampaignTime.Now;

            _building = new Snapshot
            {
                Year = now.GetYear,
                DayOfYear = now.GetDayOfYear,
                Hour = now.GetHourOfDay,
                Towns = new List<TownRecord>(_townSnapshot.Count)
            };
        }

        private static void ReadTown(Settlement settlement)
        {
            if (settlement == null || settlement.Town == null)
                return;

            Town town = settlement.Town;

            var record = new TownRecord
            {
                Id = settlement.StringId ?? "",
                Name = settlement.Name?.ToString() ?? settlement.StringId ?? "",

                Prosperity = town.Prosperity,
                ProsperityChange = town.ProsperityChange,

                Food = town.FoodStocks,
                FoodChange = town.FoodChange,
                FoodChangeWithoutMarketStocks = town.FoodChangeWithoutMarketStocks,
                Security = town.Security,
                SecurityChange = town.SecurityChange,

                Loyalty = town.Loyalty,
                LoyaltyChange = town.LoyaltyChange,

                Garrison = settlement.Town.GarrisonParty?.MemberRoster?.TotalManCount ?? 0,

                UnderSiege = settlement.IsUnderSiege
            };

            _building.Towns.Add(record);
        }

        private static void FinishPass()
        {
            if (_building == null)
                return;

            _passes++;

            _building.CompletedAt = DateTime.UtcNow;
            _building.PassMs =
                (DateTime.UtcNow - _passStarted).TotalMilliseconds;
            _building.Passes = _passes;

            _published = _building;

            _building = null;
            _townSnapshot = null;
            _cursor = 0;
        }

        /// <summary>
        /// Cheap HTTP-facing read.
        /// Never touches every town when called.
        /// It only serializes the last completed snapshot.
        /// </summary>
        public static object Current()
        {
            Snapshot snapshot = _published;

            if (snapshot == null)
            {
                return new
                {
                    ready = false,
                    note =
                        "Economic census has not completed its first pass yet."
                };
            }

            double secondsOld =
                (DateTime.UtcNow - snapshot.CompletedAt).TotalSeconds;

            return new
            {
                ready = true,

                asOf = new
                {
                    secondsOld = Math.Round(secondsOld, 2),
                    passTookMs = Math.Round(snapshot.PassMs, 1),
                    passes = snapshot.Passes
                },

                campaign = new
                {
                    year = snapshot.Year,
                    dayOfYear = snapshot.DayOfYear,
                    hour = snapshot.Hour
                },

                townCount = snapshot.Towns.Count,

                towns = snapshot.Towns.Select(t => new
                {
                    id = t.Id,
                    name = t.Name,

                    prosperity = t.Prosperity,
                    prosperityChange = t.ProsperityChange,

                    food = t.Food,
                    foodChange = t.FoodChange,
                    foodChangeWithoutMarketStocks = t.FoodChangeWithoutMarketStocks,
                    marketFoodContribution =
                    t.FoodChange - t.FoodChangeWithoutMarketStocks,

                    security = t.Security,
                    securityChange = t.SecurityChange,

                    loyalty = t.Loyalty,
                    loyaltyChange = t.LoyaltyChange,

                    garrison = t.Garrison,
                    underSiege = t.UnderSiege
                }).ToArray()
            };
        }
    }
}