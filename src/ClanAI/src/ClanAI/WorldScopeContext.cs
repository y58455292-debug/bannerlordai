using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace ClanAI
{
    internal static class WorldScopeContext
    {
        private sealed class WarCacheEntry
        {
            internal double CampaignHour;
            internal bool AtWar;
        }

        private static readonly Dictionary<string, WarCacheEntry> WarByFaction =
            new Dictionary<string, WarCacheEntry>(StringComparer.Ordinal);

        private static readonly Dictionary<string, List<Settlement>> ThreatsByFaction =
            new Dictionary<string, List<Settlement>>(StringComparer.Ordinal);

        private static double _threatSnapshotHour = double.MinValue;

        internal static void Reset()
        {
            WarByFaction.Clear();
            ThreatsByFaction.Clear();
            _threatSnapshotHour = double.MinValue;
            ClanAIPostVanilla.WriteExternalLog(
                "WORLD_SCOPE_CONTEXT_RESET scope=all-independent-ai-lords");
        }

        internal static bool EligibleIndependentLordAtWar(MobileParty actor)
        {
            if (actor == null || !actor.IsActive || !actor.IsLordParty ||
                actor.LeaderHero == null || actor.IsMainParty ||
                actor.Army != null || actor.MapFaction == null ||
                actor.ActualClan == null)
                return false;

            return FactionAtWar(actor.MapFaction);
        }

        internal static bool FactionAtWar(IFaction faction)
        {
            if (faction == null)
                return false;

            string key = FactionKey(faction);
            double hour = CampaignTime.Now.ToHours;
            WarCacheEntry cached;
            if (WarByFaction.TryGetValue(key, out cached) &&
                Math.Abs(hour - cached.CampaignHour) < 1.0)
                return cached.AtWar;

            bool atWar = false;
            foreach (Settlement settlement in Settlement.All)
            {
                if (settlement == null || settlement.MapFaction == null)
                    continue;
                if (SameFaction(settlement.MapFaction, faction))
                    continue;
                if (faction.IsAtWarWith(settlement.MapFaction))
                {
                    atWar = true;
                    break;
                }
            }

            WarByFaction[key] = new WarCacheEntry
            {
                CampaignHour = hour,
                AtWar = atWar
            };
            return atWar;
        }

        internal static List<Settlement> ThreatenedFactionSettlements(MobileParty actor)
        {
            if (actor == null || actor.MapFaction == null)
                return EmptyThreats;

            EnsureThreatSnapshot();
            List<Settlement> list;
            return ThreatsByFaction.TryGetValue(FactionKey(actor.MapFaction), out list)
                ? list
                : EmptyThreats;
        }

        internal static bool HasOtherUrgentClanThreat(
            MobileParty actor,
            string rememberedSettlementId)
        {
            if (actor == null || actor.ActualClan == null)
                return false;

            List<Settlement> threats = ThreatenedFactionSettlements(actor);
            for (int i = 0; i < threats.Count; i++)
            {
                Settlement settlement = threats[i];
                if (settlement == null ||
                    !SameClan(settlement.OwnerClan, actor.ActualClan))
                    continue;
                if (string.Equals(
                        settlement.StringId,
                        rememberedSettlementId,
                        StringComparison.Ordinal))
                    continue;
                return true;
            }
            return false;
        }

        internal static bool ActorOwnsSettlement(
            MobileParty actor,
            Settlement settlement)
        {
            return actor != null && settlement != null &&
                   SameClan(actor.ActualClan, settlement.OwnerClan);
        }

        internal static bool SameClan(Clan a, Clan b)
        {
            if (a == null || b == null)
                return false;
            if (ReferenceEquals(a, b))
                return true;
            return string.Equals(a.StringId, b.StringId, StringComparison.Ordinal);
        }

        internal static bool SameFaction(IFaction a, IFaction b)
        {
            if (a == null || b == null)
                return false;
            if (ReferenceEquals(a, b))
                return true;
            return string.Equals(FactionKey(a), FactionKey(b), StringComparison.Ordinal);
        }

        private static readonly List<Settlement> EmptyThreats =
            new List<Settlement>(0);

        private static void EnsureThreatSnapshot()
        {
            double hour = CampaignTime.Now.ToHours;
            if (_threatSnapshotHour != double.MinValue &&
                Math.Abs(hour - _threatSnapshotHour) < 1.0)
                return;

            ThreatsByFaction.Clear();
            foreach (Settlement settlement in Settlement.All)
            {
                if (settlement == null || settlement.MapFaction == null ||
                    (!settlement.IsUnderSiege && !settlement.IsUnderRaid))
                    continue;

                string key = FactionKey(settlement.MapFaction);
                List<Settlement> list;
                if (!ThreatsByFaction.TryGetValue(key, out list))
                {
                    list = new List<Settlement>();
                    ThreatsByFaction[key] = list;
                }
                list.Add(settlement);
            }

            _threatSnapshotHour = hour;
        }

        private static string FactionKey(IFaction faction)
        {
            if (faction == null)
                return "<null>";
            if (!string.IsNullOrEmpty(faction.StringId))
                return faction.StringId;
            return faction.Name == null ? "<unnamed>" : faction.Name.ToString();
        }
    }
}
