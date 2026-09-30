using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace ClanAI
{
    internal enum HomeAssignmentStatus
    {
        Active, NoActiveParty, AtSea, InArmy, Prisoner, Disbanding,
        Attached, AiStopped, InBattleOrSiege, OtherTemporaryUnavailable
    }

    internal static class HomeAssignmentRoster
    {
        private const int MaximumRows = HomeAssignmentRecords.MaximumRows;
        internal static List<Hero> Build(Clan clan)
        {
            var result = new List<Hero>();
            if (clan == null) return result;
            HomeAssignmentStore.InvalidateOwnership();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (Hero hero in HomeAssignmentStore.AssignedHeroes()) AddAssignedHero(result, seen, hero);
            foreach (Hero hero in clan.Heroes) { if (result.Count >= MaximumRows) break; AddRosterHero(result, seen, hero); }
            foreach (Hero hero in clan.Companions) { if (result.Count >= MaximumRows) break; AddRosterHero(result, seen, hero); }
            result.Sort((left, right) => {
                int byName = StringComparer.Ordinal.Compare(DisplayName(left), DisplayName(right));
                return byName != 0 ? byName : StringComparer.Ordinal.Compare(left.StringId, right.StringId);
            });
            return result;
        }

        private static void AddRosterHero(List<Hero> result, HashSet<string> seen, Hero hero)
        {
            if (!HomeAssignmentStore.ValidHero(hero) || hero.Clan != Clan.PlayerClan) return;
            Add(result, seen, hero);
        }

        private static void AddAssignedHero(List<Hero> result, HashSet<string> seen, Hero hero)
        {
            if (!HomeAssignmentStore.ValidAssignedHero(hero) || hero.Clan != Clan.PlayerClan) return;
            Add(result, seen, hero);
        }

        private static void Add(List<Hero> result, HashSet<string> seen, Hero hero)
        {
            if (result.Count >= MaximumRows || hero == null || string.IsNullOrEmpty(hero.StringId) || !seen.Add(hero.StringId)) return;
            result.Add(hero);
        }

        internal static string DisplayName(Hero hero)
        { return hero == null || hero.Name == null ? string.Empty : hero.Name.ToString(); }

        internal static HomeAssignmentStatus Status(Hero hero)
        {
            if (hero == null) return HomeAssignmentStatus.OtherTemporaryUnavailable;
            if (hero.IsPrisoner) return HomeAssignmentStatus.Prisoner;
            MobileParty party = hero.PartyBelongedTo;
            if (party == null || !party.IsActive) return HomeAssignmentStatus.NoActiveParty;
            if (party.IsCurrentlyAtSea) return HomeAssignmentStatus.AtSea;
            if (party.Army != null) return HomeAssignmentStatus.InArmy;
            if (party.IsDisbanding || HomeAssignmentStore.IsWaitingForDisband(party)) return HomeAssignmentStatus.Disbanding;
            if (party.AttachedTo != null) return HomeAssignmentStatus.Attached;
            if (party.Ai == null || party.Ai.IsDisabled || party.Ai.DoNotMakeNewDecisions) return HomeAssignmentStatus.AiStopped;
            if (party.MapEvent != null || party.SiegeEvent != null ||
                (party.CurrentSettlement != null && party.CurrentSettlement.SiegeEvent != null))
                return HomeAssignmentStatus.InBattleOrSiege;
            if (ReferenceEquals(party.LeaderHero, hero) && HomeAssignmentStore.Eligible(party)) return HomeAssignmentStatus.Active;
            return HomeAssignmentStatus.OtherTemporaryUnavailable;
        }

        internal static string StatusText(HomeAssignmentStatus status)
        {
            switch (status)
            {
                case HomeAssignmentStatus.Active: return "Active";
                case HomeAssignmentStatus.NoActiveParty: return "Responsibility suspended — no active party";
                case HomeAssignmentStatus.AtSea: return "Responsibility suspended — at sea";
                case HomeAssignmentStatus.InArmy: return "Responsibility suspended — serving in an army";
                case HomeAssignmentStatus.Prisoner: return "Responsibility suspended — prisoner";
                case HomeAssignmentStatus.Disbanding: return "Responsibility suspended — party disbanding";
                case HomeAssignmentStatus.Attached: return "Responsibility suspended — attached to another party";
                case HomeAssignmentStatus.AiStopped: return "Responsibility suspended — party decisions stopped";
                case HomeAssignmentStatus.InBattleOrSiege: return "Responsibility suspended — in battle or siege";
                default: return "Responsibility suspended — temporarily unavailable";
            }
        }

        internal static string Location(Hero hero)
        {
            if (hero == null) return "No active party";
            if (hero.IsPrisoner) return "Prisoner";
            MobileParty party = hero.PartyBelongedTo;
            if (party != null)
            {
                if (party.CurrentSettlement != null) return party.CurrentSettlement.Name.ToString();
                if (party.Army != null) return "With an army";
                if (party.IsCurrentlyAtSea) return "At sea";
                Settlement target = party.TargetSettlement ?? party.ShortTermTargetSettlement;
                if (target != null) return "Traveling toward " + target.Name;
            }
            if ((party == null || !party.IsActive) && hero.CurrentSettlement != null)
                return hero.CurrentSettlement.Name.ToString();
            return party != null && party.IsActive ? "On campaign" : "No active party";
        }
    }
}
