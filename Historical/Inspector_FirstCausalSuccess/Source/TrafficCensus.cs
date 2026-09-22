using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;

namespace BannerlordInspector
{
    public static class TrafficCensus
    {
        // Intentionally conservative.
        // Only two economic parties per rendered frame.
        private const int SliceSize = 2;

        private sealed class TrafficRecord
        {
            public string Id;
            public string Name;
            public string Type;

            public bool IsActive;
            public bool IsMoving;
            public bool IsTrading;
            public bool IsAtSea;

            public float Speed;

            public int TradeGold;
            public int TradeTaxGold;

            public int InventoryCapacity;
            public float TotalWeightCarried;

            public string CurrentSettlementId;
            public string CurrentSettlementName;

            public string LastSettlementId;
            public string LastSettlementName;

            public string HomeSettlementId;
            public string HomeSettlementName;

            public string TargetSettlementId;
            public string TargetSettlementName;

            public string ShortTargetSettlementId;
            public string ShortTargetSettlementName;

            // Villager origin
            public string VillageId;
            public string VillageName;

            // Caravan identity
            public string CaravanOwnerId;
            public string CaravanOwnerName;

            public string CaravanLeaderId;
            public string CaravanLeaderName;

            public bool CaravanElite;
        }

        private static List<MobileParty> _parties;
        private static List<TrafficRecord> _building;
        private static List<TrafficRecord> _published;

        private static int _cursor;
        private static DateTime _completedAt;
        private static DateTime _passStarted;
        private static long _passes;
        private static double _lastPassMs;

        public static void Tick()
        {
            try
            {
                if (Campaign.Current == null)
                {
                    ResetBuilding();
                    return;
                }

                if (_building == null)
                    BeginPass();

                int budget = SliceSize;

                while (
                    budget > 0 &&
                    _parties != null &&
                    _cursor < _parties.Count
                )
                {
                    ReadParty(_parties[_cursor]);
                    _cursor++;
                    budget--;
                }

                if (
                    _parties != null &&
                    _cursor >= _parties.Count
                )
                {
                    FinishPass();
                }
            }
            catch (Exception ex)
            {
                InspectorLog.Error(
                    "Traffic census slice failed; restarting pass.",
                    ex
                );

                ResetBuilding();
            }
        }

        private static void ResetBuilding()
        {
            _parties = null;
            _building = null;
            _cursor = 0;
        }

        private static void BeginPass()
        {
            _passStarted = DateTime.UtcNow;
            _cursor = 0;

            _parties = new List<MobileParty>();

            // Use Bannerlord's dedicated economic collections.
            // Do NOT walk every MobileParty in the world.
            if (MobileParty.AllCaravanParties != null)
            {
                foreach (MobileParty party in MobileParty.AllCaravanParties)
                {
                    if (party != null)
                        _parties.Add(party);
                }
            }

            if (MobileParty.AllVillagerParties != null)
            {
                foreach (MobileParty party in MobileParty.AllVillagerParties)
                {
                    if (party != null)
                        _parties.Add(party);
                }
            }

            _building =
                new List<TrafficRecord>(_parties.Count);
        }

        private static void ReadParty(MobileParty party)
        {
            if (party == null)
                return;

            bool isCaravan = false;
            bool isVillager = false;

            try { isCaravan = party.IsCaravan; }
            catch { }

            try { isVillager = party.IsVillager; }
            catch { }

            if (!isCaravan && !isVillager)
                return;

            var record = new TrafficRecord
            {
                Id = party.StringId ?? "",
                Name = party.Name?.ToString() ?? party.StringId ?? "",

                Type = isCaravan
                    ? "Caravan"
                    : "Villager",

                IsActive = party.IsActive,
                IsMoving = party.IsMoving,
                IsTrading = party.IsPartyTradeActive,
                IsAtSea = party.IsCurrentlyAtSea,

                Speed = party.Speed,

                TradeGold = party.PartyTradeGold,
                TradeTaxGold = party.PartyTradeTaxGold,

                InventoryCapacity = party.InventoryCapacity,
                TotalWeightCarried = party.TotalWeightCarried
            };

            ReadSettlement(
                party.CurrentSettlement,
                out record.CurrentSettlementId,
                out record.CurrentSettlementName
            );

            ReadSettlement(
                party.LastVisitedSettlement,
                out record.LastSettlementId,
                out record.LastSettlementName
            );

            ReadSettlement(
                party.HomeSettlement,
                out record.HomeSettlementId,
                out record.HomeSettlementName
            );

            ReadSettlement(
                party.TargetSettlement,
                out record.TargetSettlementId,
                out record.TargetSettlementName
            );

            ReadSettlement(
                party.ShortTermTargetSettlement,
                out record.ShortTargetSettlementId,
                out record.ShortTargetSettlementName
            );

            if (isVillager)
                ReadVillagerIdentity(party, record);

            if (isCaravan)
                ReadCaravanIdentity(party, record);

            _building.Add(record);
        }

        private static void ReadVillagerIdentity(
            MobileParty party,
            TrafficRecord record)
        {
            try
            {
                VillagerPartyComponent component =
                    party.VillagerPartyComponent;

                if (component == null)
                    return;

                Village village = component.Village;

                if (village?.Settlement != null)
                {
                    record.VillageId =
                        village.Settlement.StringId;

                    record.VillageName =
                        village.Settlement.Name?.ToString();
                }

                // Component home is useful because it may differ
                // from whatever generic MobileParty.HomeSettlement
                // reports in edge cases.
                if (component.HomeSettlement != null)
                {
                    record.HomeSettlementId =
                        component.HomeSettlement.StringId;

                    record.HomeSettlementName =
                        component.HomeSettlement.Name?.ToString();
                }
            }
            catch
            {
                // Identity enrichment must never kill census.
            }
        }

        private static void ReadCaravanIdentity(
            MobileParty party,
            TrafficRecord record)
        {
            try
            {
                CaravanPartyComponent component =
                    party.CaravanPartyComponent;

                if (component == null)
                    return;

                if (component.HomeSettlement != null)
                {
                    record.HomeSettlementId =
                        component.HomeSettlement.StringId;

                    record.HomeSettlementName =
                        component.HomeSettlement.Name?.ToString();
                }

                if (component.Owner != null)
                {
                    record.CaravanOwnerId =
                        component.Owner.StringId;

                    record.CaravanOwnerName =
                        component.Owner.Name?.ToString();
                }

                if (component.Leader != null)
                {
                    record.CaravanLeaderId =
                        component.Leader.StringId;

                    record.CaravanLeaderName =
                        component.Leader.Name?.ToString();
                }

                record.CaravanElite =
                    component.IsElite;
            }
            catch
            {
                // Identity enrichment must never kill census.
            }
        }

        private static void ReadSettlement(
            Settlement settlement,
            out string id,
            out string name)
        {
            id = null;
            name = null;

            if (settlement == null)
                return;

            id = settlement.StringId;
            name = settlement.Name?.ToString();
        }

        private static void FinishPass()
        {
            if (_building == null)
                return;

            _published = _building;

            _completedAt = DateTime.UtcNow;

            _lastPassMs =
                (DateTime.UtcNow - _passStarted)
                .TotalMilliseconds;

            _passes++;

            ResetBuilding();
        }

        public static object Current()
        {
            List<TrafficRecord> data =
                _published;

            if (data == null)
            {
                return new
                {
                    ready = false,
                    note =
                        "Traffic census has not completed its first pass yet."
                };
            }

            int caravans =
                data.Count(x => x.Type == "Caravan");

            int villagers =
                data.Count(x => x.Type == "Villager");

            int moving =
                data.Count(x => x.IsMoving);

            int trading =
                data.Count(x => x.IsTrading);

            int atSettlement =
                data.Count(x =>
                    !string.IsNullOrEmpty(
                        x.CurrentSettlementId
                    )
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
                            2
                        ),

                    passTookMs =
                        Math.Round(
                            _lastPassMs,
                            2
                        ),

                    passes = _passes
                },

                counts = new
                {
                    total = data.Count,
                    caravans,
                    villagers,
                    moving,
                    trading,
                    atSettlement
                },

                parties = data.Select(p => new
                {
                    id = p.Id,
                    name = p.Name,
                    type = p.Type,

                    active = p.IsActive,
                    moving = p.IsMoving,
                    trading = p.IsTrading,
                    atSea = p.IsAtSea,

                    speed = p.Speed,

                    tradeGold = p.TradeGold,
                    tradeTaxGold = p.TradeTaxGold,

                    inventoryCapacity =
                        p.InventoryCapacity,

                    totalWeightCarried =
                        p.TotalWeightCarried,

                    currentSettlement = new
                    {
                        id = p.CurrentSettlementId,
                        name = p.CurrentSettlementName
                    },

                    lastSettlement = new
                    {
                        id = p.LastSettlementId,
                        name = p.LastSettlementName
                    },

                    homeSettlement = new
                    {
                        id = p.HomeSettlementId,
                        name = p.HomeSettlementName
                    },

                    targetSettlement = new
                    {
                        id = p.TargetSettlementId,
                        name = p.TargetSettlementName
                    },

                    shortTargetSettlement = new
                    {
                        id = p.ShortTargetSettlementId,
                        name = p.ShortTargetSettlementName
                    },

                    originVillage = new
                    {
                        id = p.VillageId,
                        name = p.VillageName
                    },

                    caravan = p.Type == "Caravan"
                        ? new
                        {
                            ownerId =
                                p.CaravanOwnerId,

                            ownerName =
                                p.CaravanOwnerName,

                            leaderId =
                                p.CaravanLeaderId,

                            leaderName =
                                p.CaravanLeaderName,

                            elite =
                                p.CaravanElite
                        }
                        : null
                }).ToArray()
            };
        }
    }
}