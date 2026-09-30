using System;
using System.Collections.Generic;
using System.IO;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace ClanAI
{
    // Optional developer/demo input. Loaded once per campaign session; no tick-time IO.
    // Each non-comment line is SourceKingdomStringId>DestinationKingdomStringId.
    internal static class KingdomBorderClosureConfig
    {
        private static HashSet<string> _closedPairs = new HashSet<string>(StringComparer.Ordinal);

        internal static int Load()
        {
            var next = new HashSet<string>(StringComparer.Ordinal);
            string path = ModuleRuntimePaths.Data("KingdomBorderClosures.cfg");
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                try
                {
                    foreach (string raw in File.ReadAllLines(path))
                    {
                        if (raw == null) continue;
                        string line = raw.Trim();
                        if (line.Length == 0 || line[0] == '#') continue;
                        int separator = line.IndexOf('>');
                        if (separator <= 0 || separator == line.Length - 1 ||
                            line.IndexOf('>', separator + 1) >= 0) continue;
                        string source = line.Substring(0, separator).Trim();
                        string destination = line.Substring(separator + 1).Trim();
                        if (source.Length == 0 || destination.Length == 0 ||
                            string.Equals(source, destination, StringComparison.Ordinal)) continue;
                        next.Add(Key(source, destination));
                    }
                }
                catch (IOException) { next.Clear(); }
                catch (UnauthorizedAccessException) { next.Clear(); }
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
            if (party == null || settlement == null || !party.IsActive ||
                !party.IsLordParty || party.IsMainParty || party == MobileParty.MainParty ||
                party.Army != null || party.IsDisbanding || party.IsCaravan ||
                party.ActualClan == null || party.ActualClan == Clan.PlayerClan ||
                party.ActualClan.Kingdom == null || settlement.OwnerClan == null ||
                settlement.OwnerClan.Kingdom == null)
                return false;

            var source = party.ActualClan.Kingdom;
            var destination = settlement.OwnerClan.Kingdom;
            if (ReferenceEquals(source, destination) || source.IsAtWarWith(destination))
                return false;

            return KingdomBorderPolicy.BlocksLordSettlementVisit(
                true,
                false,
                true,
                source.StringId,
                destination.StringId,
                IsClosed(source.StringId, destination.StringId));
        }

        private static string Key(string source, string destination)
        { return source + ">" + destination; }
    }
}
