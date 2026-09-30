using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.ObjectSystem;

namespace ClanAI
{
    internal static class HomeAssignmentCounters
    {
        internal static long Lookups, NoAssignment, Evaluations, HomeCandidates,
            RetentionEvaluations, RetentionApplications, FactorApplications,
            WinnerChanges, CommitChecks, CommitMatches, CommitMismatches, CommitExpired;
        internal static void Reset()
        {
            Lookups = NoAssignment = Evaluations = HomeCandidates = RetentionEvaluations =
                RetentionApplications = FactorApplications = WinnerChanges = CommitChecks =
                CommitMatches = CommitMismatches = CommitExpired = 0;
        }
        internal static string Summary()
        {
            return "lookups=" + Lookups + " noAssignment=" + NoAssignment +
                " evaluations=" + Evaluations + " homeCandidates=" + HomeCandidates +
                " retentionEvaluations=" + RetentionEvaluations + " retentionApplications=" +
                RetentionApplications + " factors=" + FactorApplications + " winnerChanges=" +
                WinnerChanges + " commitChecks=" + CommitChecks + " commitMatches=" +
                CommitMatches + " commitMismatches=" + CommitMismatches + " commitExpired=" + CommitExpired;
        }
    }
    internal static class HomeAssignmentStore
    {
        internal static readonly HomeAssignmentRecords Records = new HomeAssignmentRecords();
        private sealed class Resolved
        {
            internal Hero Hero;
            internal Settlement Home;
        }
        private static readonly Dictionary<string, Resolved> Cache =
            new Dictionary<string, Resolved>(StringComparer.Ordinal);
        internal static void ResolveLoaded()
        {
            Cache.Clear();
            var remove = new List<string>();
            foreach (var pair in Records.Entries)
            {
                Hero hero = MBObjectManager.Instance.GetObject<Hero>(pair.Key);
                Settlement home = MBObjectManager.Instance.GetObject<Settlement>(pair.Value);
                if (!ValidAssignedHero(hero) || !ValidHome(home)) remove.Add(pair.Key);
                else Cache[pair.Key] = new Resolved { Hero = hero, Home = home };
            }
            foreach (string id in remove) Records.Clear(id);
            HomeAssignmentLayer.Reset();
            HomeAssignmentCounters.Reset();
            if (RuntimeProfile.EvidenceEnabled)
                ClanAIPostVanilla.WriteExternalLog("HOME_ASSIGNMENT_RESTORE rows=" + Cache.Count +
                    " rejected=" + Records.RejectedRows + " invalidReferences=" + remove.Count);
        }
        internal static void BeginNewSession(bool restored)
        {
            if (!restored) Records.Import(null);
            ResolveLoaded();
        }
        internal static bool ValidHero(Hero hero)
        {
            return Campaign.Current != null && hero != null && hero != Hero.MainHero &&
                !hero.IsHumanPlayerCharacter && hero.IsAlive && !hero.IsTemplate &&
                hero.Clan == Clan.PlayerClan && (hero.IsLord || hero.IsPlayerCompanion) &&
                hero.Age >= Campaign.Current.Models.AgeModel.HeroComesOfAge &&
                !string.IsNullOrEmpty(hero.StringId);
        }
        internal static bool ValidAssignedHero(Hero hero)
        {
            return Campaign.Current != null && hero != null && hero != Hero.MainHero &&
                !hero.IsHumanPlayerCharacter && hero.IsAlive && !hero.IsTemplate &&
                hero.Clan == Clan.PlayerClan && Clan.PlayerClan != null &&
                hero.Age >= Campaign.Current.Models.AgeModel.HeroComesOfAge &&
                !string.IsNullOrEmpty(hero.StringId);
        }
        internal static bool ValidHome(Settlement home)
        { return Clan.PlayerClan != null && home != null && (home.IsTown || home.IsCastle) && home.OwnerClan == Clan.PlayerClan; }
        internal static bool Eligible(MobileParty party)
        {
            if (party == null || !party.IsActive || !party.IsLordParty || party.IsMainParty ||
                party == MobileParty.MainParty || party.IsCaravan || party.Army != null ||
                party.IsDisbanding || party.ActualClan != Clan.PlayerClan || party.MapFaction == null ||
                party.Ai == null || party.Ai.IsDisabled || party.Ai.DoNotMakeNewDecisions ||
                party.MapEvent != null || party.SiegeEvent != null || party.AttachedTo != null ||
                party.IsCurrentlyAtSea ||
                !ValidHero(party.LeaderHero) || party.LeaderHero.IsPrisoner ||
                !party.LeaderHero.IsActive || party.LeaderHero.PartyBelongedTo != party)
                return false;
            return !IsWaitingForDisband(party);
        }
        internal static bool Peace(MobileParty party)
        {
            if (party == null || party.MapFaction == null) return false;
            foreach (IFaction enemy in party.MapFaction.FactionsAtWarWith)
                if (enemy != null && !enemy.IsBanditFaction && !enemy.IsOutlaw) return false;
            return true;
        }
        internal static bool Urgent(MobileParty party, Settlement home)
        {
            return home.IsUnderSiege || home.IsUnderRaid ||
                (party.DefaultBehavior == AiBehavior.DefendSettlement && party.TargetSettlement != null &&
                    party.TargetSettlement.OwnerClan == Clan.PlayerClan) ||
                (party.ShortTermBehavior == AiBehavior.DefendSettlement && party.ShortTermTargetSettlement != null &&
                    party.ShortTermTargetSettlement.OwnerClan == Clan.PlayerClan);
        }
        internal static bool TryHome(MobileParty party, out Settlement home)
        {
            HomeAssignmentCounters.Lookups++;
            home = null;
            string id = party == null || party.LeaderHero == null ? null : party.LeaderHero.StringId;
            string saved;
            if (!Records.TryGet(id, out saved))
            { HomeAssignmentCounters.NoAssignment++; return false; }
            Resolved entry;
            if (!Cache.TryGetValue(id, out entry)) return false; // Resolution is never done on AI ticks.
            if (!ReferenceEquals(entry.Hero, party.LeaderHero) || !ValidAssignedHero(entry.Hero) ||
                !ValidHome(entry.Home) || entry.Home.StringId != saved)
            { Clear(id); return false; }
            home = entry.Home; return true;
        }
        internal static Settlement CurrentHome(Hero hero)
        {
            if (hero == null) return null;
            Resolved entry;
            if (!Cache.TryGetValue(hero.StringId, out entry)) return null;
            if (!ReferenceEquals(entry.Hero, hero) || !ValidAssignedHero(entry.Hero) || !ValidHome(entry.Home)) { Clear(hero.StringId); return null; }
            return entry.Home;
        }
        internal static List<Hero> AssignedHeroes()
        {
            var heroes = new List<Hero>();
            foreach (var pair in Records.Entries)
            {
                Resolved entry;
                if (Cache.TryGetValue(pair.Key, out entry)) heroes.Add(entry.Hero);
            }
            return heroes;
        }
        internal static bool IsWaitingForDisband(MobileParty party)
        {
            if (party == null || Campaign.Current == null) return false;
            var disband = Campaign.Current.GetCampaignBehavior<IDisbandPartyCampaignBehavior>();
            return disband != null && disband.IsPartyWaitingForDisband(party);
        }
        internal static bool Assign(MobileParty party, Settlement home)
        {
            if (!Eligible(party) || !ValidHome(home)) return false;
            string id = party.LeaderHero.StringId;
            if (!Records.Set(id, home.StringId)) return false;
            Cache[id] = new Resolved { Hero = party.LeaderHero, Home = home };
            HomeAssignmentLayer.Reset(); return true;
        }
        internal static bool Clear(string id)
        {
            bool changed = Records.Clear(id); if (id != null) Cache.Remove(id);
            if (changed) HomeAssignmentLayer.Reset();
            return changed;
        }
        internal static void InvalidateOwnership()
        {
            var remove = new List<string>();
            foreach (var pair in Cache)
                if (!ValidAssignedHero(pair.Value.Hero) || !ValidHome(pair.Value.Home)) remove.Add(pair.Key);
            foreach (string id in remove) Clear(id);
        }
    }
}
