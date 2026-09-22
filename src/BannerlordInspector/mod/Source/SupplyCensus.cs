using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;

namespace BannerlordInspector
{
    public static class SupplyCensus
    {
        private const int SliceSize = 4;

        private sealed class VillageRecord
        {
            public string Id;
            public string Name;

            public string State;
            public bool Deserted;

            public float Hearth;
            public float HearthChange;

            public float Militia;
            public float MilitiaChange;

            public string BoundId;
            public string BoundName;

            public string TradeBoundId;
            public string TradeBoundName;

            public string PrimaryProductionId;
            public string PrimaryProductionName;
        }

        private static List<Settlement> _villages;
        private static List<VillageRecord> _building;
        private static List<VillageRecord> _published;

        private static int _cursor;
        private static DateTime _completedAt;
        private static long _passes;

        public static void Tick()
        {
            try
            {
                if (Campaign.Current == null)
                {
                    _villages = null;
                    _building = null;
                    return;
                }

                if (_building == null)
                    BeginPass();

                int budget = SliceSize;

                while (
                    budget > 0 &&
                    _villages != null &&
                    _cursor < _villages.Count
                )
                {
                    ReadVillage(_villages[_cursor]);
                    _cursor++;
                    budget--;
                }

                if (
                    _villages != null &&
                    _cursor >= _villages.Count
                )
                {
                    FinishPass();
                }
            }
            catch (Exception ex)
            {
                InspectorLog.Error(
                    "Supply census slice failed; restarting pass.",
                    ex
                );

                _building = null;
                _villages = null;
            }
        }

        private static void BeginPass()
        {
            _villages = Settlement.All
                .Where(s =>
                    s != null &&
                    s.IsVillage &&
                    s.Village != null
                )
                .ToList();

            _building =
                new List<VillageRecord>(_villages.Count);

            _cursor = 0;
        }

        private static void ReadVillage(Settlement settlement)
        {
            if (settlement == null || settlement.Village == null)
                return;

            Village village = settlement.Village;

            string productionId = null;
            string productionName = null;

            try
            {
                var item = village.VillageType?.PrimaryProduction;

                if (item != null)
                {
                    productionId = item.StringId;
                    productionName = item.Name?.ToString();
                }
            }
            catch
            {
                // Production identity is useful but must never
                // be allowed to kill the census.
            }

            Settlement bound = null;
            Settlement tradeBound = null;

            try
            {
                bound = village.Bound;
            }
            catch { }

            try
            {
                tradeBound = village.TradeBound;
            }
            catch { }

            _building.Add(
                new VillageRecord
                {
                    Id = settlement.StringId,
                    Name = settlement.Name?.ToString(),

                    State = village.VillageState.ToString(),
                    Deserted = village.IsDeserted,

                    Hearth = village.Hearth,
                    HearthChange = village.HearthChange,

                    Militia = village.Militia,
                    MilitiaChange = village.MilitiaChange,

                    BoundId = bound?.StringId,
                    BoundName = bound?.Name?.ToString(),

                    TradeBoundId = tradeBound?.StringId,
                    TradeBoundName = tradeBound?.Name?.ToString(),

                    PrimaryProductionId = productionId,
                    PrimaryProductionName = productionName
                }
            );
        }

        private static void FinishPass()
        {
            _published = _building;
            _completedAt = DateTime.UtcNow;
            _passes++;

            _building = null;
            _villages = null;
        }

        public static object Current()
        {
            var data = _published;

            if (data == null)
            {
                return new
                {
                    ready = false,
                    note = "Supply census has not completed its first pass yet."
                };
            }

            var stateCounts = data
                .GroupBy(v => v.State ?? "(unknown)")
                .ToDictionary(
                    g => g.Key,
                    g => g.Count()
                );

            var productionCounts = data
                .GroupBy(v =>
                    v.PrimaryProductionName ?? "(unknown)")
                .ToDictionary(
                    g => g.Key,
                    g => g.Count()
                );

            return new
            {
                ready = true,

                asOf = new
                {
                    secondsOld =
                        Math.Round(
                            (DateTime.UtcNow - _completedAt)
                            .TotalSeconds,
                            1
                        ),

                    passes = _passes
                },

                villageCount = data.Count,

                stateCounts,
                productionCounts,

                villages = data.Select(v => new
                {
                    id = v.Id,
                    name = v.Name,

                    state = v.State,
                    deserted = v.Deserted,

                    hearth = v.Hearth,
                    hearthChange = v.HearthChange,

                    militia = v.Militia,
                    militiaChange = v.MilitiaChange,

                    bound = new
                    {
                        id = v.BoundId,
                        name = v.BoundName
                    },

                    tradeBound = new
                    {
                        id = v.TradeBoundId,
                        name = v.TradeBoundName
                    },

                    primaryProduction = new
                    {
                        id = v.PrimaryProductionId,
                        name = v.PrimaryProductionName
                    }
                }).ToList()
            };
        }
    }
}