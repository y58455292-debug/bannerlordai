using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace BannerlordInspector
{
    public static class MarketCensus
    {
        // One town per frame.
        private const int SliceSize = 1;

        private sealed class CategoryRecord
        {
            public string Id;
            public string Name;

            public int Count;
            public float Supply;
            public float Demand;
            public float PriceFactor;
            public float PriceIndex;
        }

        private sealed class TownRecord
        {
            public string Id;
            public string Name;

            public List<CategoryRecord> Categories =
                new List<CategoryRecord>();
        }

        private static List<Town> _towns;
        private static List<TownRecord> _building;
        private static List<TownRecord> _published;

        private static int _cursor;

        private static DateTime _passStarted;
        private static DateTime _completedAt;

        private static double _lastPassMs;
        private static long _passes;

        public static void Tick()
        {
            try
            {
                if (Campaign.Current == null)
                {
                    Reset();
                    return;
                }

                if (_building == null)
                    BeginPass();

                int budget = SliceSize;

                while (
                    budget > 0 &&
                    _towns != null &&
                    _cursor < _towns.Count
                )
                {
                    ReadTown(
                        _towns[_cursor]
                    );

                    _cursor++;
                    budget--;
                }

                if (
                    _towns != null &&
                    _cursor >= _towns.Count
                )
                {
                    FinishPass();
                }
            }
            catch (Exception ex)
            {
                InspectorLog.Error(
                    "Market census slice failed; restarting pass.",
                    ex
                );

                Reset();
            }
        }

        private static void Reset()
        {
            _towns = null;
            _building = null;
            _cursor = 0;
        }

        private static void BeginPass()
        {
            _passStarted =
                DateTime.UtcNow;

            _cursor = 0;

            _towns =
                Town.AllTowns
                    .Where(t =>
                        t != null &&
                        t.Settlement != null
                    )
                    .ToList();

            _building =
                new List<TownRecord>(
                    _towns.Count
                );
        }

        private static void ReadTown(
            Town town)
        {
            if (
                town == null ||
                town.Settlement == null ||
                town.MarketData == null
            )
                return;

            var record =
                new TownRecord
                {
                    Id =
                        town.Settlement
                            .StringId,

                    Name =
                        town.Settlement
                            .Name
                            ?.ToString()
                };

            foreach (
                ItemCategory category
                in FoodCategories()
            )
            {
                if (category == null)
                    continue;

                try
                {
                    record.Categories.Add(
                        new CategoryRecord
                        {
                            Id =
                                category.StringId,

                            Name =
                                category
                                    .GetName()
                                    ?.ToString()
                                ?? category.StringId,

                            Count =
                                town.MarketData
                                    .GetItemCountOfCategory(
                                        category
                                    ),

                            Supply =
                                town.MarketData
                                    .GetSupply(
                                        category
                                    ),

                            Demand =
                                town.MarketData
                                    .GetDemand(
                                        category
                                    ),

                            PriceFactor =
                                town.MarketData
                                    .GetPriceFactor(
                                        category
                                    ),

                            PriceIndex =
                                town.GetItemCategoryPriceIndex(
                                    category
                                )
                        }
                    );
                }
                catch
                {
                    // One bad category must not
                    // invalidate the town/pass.
                }
            }

            _building.Add(record);
        }

        private static IEnumerable<ItemCategory>
            FoodCategories()
        {
            yield return
                DefaultItemCategories.Grain;

            yield return
                DefaultItemCategories.Meat;

            yield return
                DefaultItemCategories.Fish;

            yield return
                DefaultItemCategories.Cheese;

            yield return
                DefaultItemCategories.Butter;

            yield return
                DefaultItemCategories.Olives;

            yield return
                DefaultItemCategories.DateFruit;
        }

        private static void FinishPass()
        {
            _published =
                _building;

            _completedAt =
                DateTime.UtcNow;

            _lastPassMs =
                (
                    _completedAt -
                    _passStarted
                ).TotalMilliseconds;

            _passes++;

            Reset();
        }

        public static object Current()
        {
            var data =
                _published;

            if (data == null)
            {
                return new
                {
                    ready = false,

                    note =
                        "Market census has not completed its first pass yet."
                };
            }

            return new
            {
                ready = true,

                asOf = new
                {
                    secondsOld =
                        Math.Round(
                            (
                                DateTime.UtcNow -
                                _completedAt
                            ).TotalSeconds,
                            2
                        ),

                    passTookMs =
                        Math.Round(
                            _lastPassMs,
                            2
                        ),

                    passes =
                        _passes
                },

                townCount =
                    data.Count,

                categoriesPerTown =
                    7,

                towns =
                    data.Select(
                        town => new
                        {
                            id =
                                town.Id,

                            name =
                                town.Name,

                            categories =
                                town.Categories
                                    .Select(
                                        c => new
                                        {
                                            id =
                                                c.Id,

                                            name =
                                                c.Name,

                                            count =
                                                c.Count,

                                            supply =
                                                c.Supply,

                                            demand =
                                                c.Demand,

                                            priceFactor =
                                                c.PriceFactor,

                                            priceIndex =
                                                c.PriceIndex
                                        }
                                    )
                                    .ToArray()
                        }
                    )
                    .ToArray()
            };
        }
    }
}