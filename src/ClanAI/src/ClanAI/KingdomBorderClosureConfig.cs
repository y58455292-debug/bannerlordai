using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace ClanAI
{
    // Optional developer/demo input. Loaded once per campaign session; no tick-time IO.
    // Each non-comment line is ClosedTerritoryKingdomStringId>ExcludedVisitorKingdomStringId.
    internal static class KingdomBorderClosureConfig
    {
        private static HashSet<string> _closedPairs = new HashSet<string>(StringComparer.Ordinal);
        private static long _candidateChecks, _candidatesFiltered;
        internal static string CandidateSummary()
        { return "candidateChecks=" + Interlocked.Read(ref _candidateChecks) + " candidatesFiltered=" + Interlocked.Read(ref _candidatesFiltered); }

        internal static int Load()
        {
            string path = ModuleRuntimePaths.Data("KingdomBorderClosures.cfg");
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                try
                {
                    return Load(File.ReadAllLines(path));
                }
                catch (IOException) { return Load(null); }
                catch (UnauthorizedAccessException) { return Load(null); }
            }
            return Load(null);
        }

        // Pure parser entry point for deterministic validation. Any malformed directive
        // resets the whole set to open rather than partially applying policy.
        internal static int Load(IEnumerable<string> lines)
        {
            Interlocked.Exchange(ref _candidateChecks, 0);
            Interlocked.Exchange(ref _candidatesFiltered, 0);
            var next = new HashSet<string>(StringComparer.Ordinal);
            if (lines != null)
            {
                foreach (string raw in lines)
                {
                    if (raw == null) continue;
                    string line = raw.Trim();
                    if (line.Length == 0 || line[0] == '#') continue;
                    int separator = line.IndexOf('>');
                    if (separator <= 0 || separator == line.Length - 1 ||
                        line.IndexOf('>', separator + 1) >= 0)
                    { next.Clear(); break; }
                    string source = line.Substring(0, separator).Trim();
                    string destination = line.Substring(separator + 1).Trim();
                    if (source.Length == 0 || destination.Length == 0 ||
                        string.Equals(source, destination, StringComparison.Ordinal))
                    { next.Clear(); break; }
                    next.Add(Key(source, destination));
                }
            }
            _closedPairs = next;
            return _closedPairs.Count;
        }

        internal static bool IsClosed(string sourceKingdomId, string destinationKingdomId)
        {
            return !string.IsNullOrEmpty(sourceKingdomId) &&
                !string.IsNullOrEmpty(destinationKingdomId) &&
                _closedPairs.Contains(Key(sourceKingdomId, destinationKingdomId));
        }

        internal static bool BlocksVisit(MobileParty party, Settlement settlement)
        {
            Interlocked.Increment(ref _candidateChecks);
            if (party == null || settlement == null || !party.IsActive ||
                !party.IsLordParty || party.IsMainParty || party == MobileParty.MainParty ||
                party.Army != null || party.IsDisbanding || party.IsCaravan ||
                party.IsCurrentlyAtSea || party.MapEvent != null || party.SiegeEvent != null ||
                party.AttachedTo != null || party.Ai == null || party.Ai.IsDisabled ||
                party.Ai.DoNotMakeNewDecisions || party.ActualClan == null ||
                Campaign.Current == null || Campaign.Current.Models == null ||
                Campaign.Current.Models.AgeModel == null ||
                party.LeaderHero == null || party.LeaderHero == Hero.MainHero ||
                party.LeaderHero.Clan != party.ActualClan || !party.LeaderHero.IsAlive ||
                party.LeaderHero.IsTemplate || party.LeaderHero.IsPrisoner ||
                !party.LeaderHero.IsActive || party.LeaderHero.PartyBelongedTo != party ||
                party.LeaderHero.Age < Campaign.Current.Models.AgeModel.HeroComesOfAge ||
                !party.LeaderHero.IsLord || string.IsNullOrEmpty(party.LeaderHero.StringId) ||
                HomeAssignmentStore.IsWaitingForDisband(party) ||
                party.ActualClan.Kingdom == null || settlement.OwnerClan == null ||
                settlement.OwnerClan.Kingdom == null)
                return false;

            var source = party.ActualClan.Kingdom;
            var destination = settlement.OwnerClan.Kingdom;
            bool atWarWithDestination = source.IsAtWarWith(destination);
            if (ReferenceEquals(source, destination) || atWarWithDestination)
                return false;

            bool blocked = KingdomBorderPolicy.BlocksLordSettlementVisit(
                true,
                atWarWithDestination,
                true,
                source.StringId,
                destination.StringId,
                IsClosed(destination.StringId, source.StringId));
            if (blocked) Interlocked.Increment(ref _candidatesFiltered);
            return blocked;
        }

        private static string Key(string source, string destination)
        { return source + ">" + destination; }
    }
}
