using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace ClanAI
{
    public static class SocialWorldObserver
    {
        // party/leader -> currently observed raid.
        // A raid is recorded only when a NEW active raid appears.
        private static readonly Dictionary<string, string>
            ActiveRaidByLord =
                new Dictionary<string, string>();

        public static void BeginSession()
        {
            ActiveRaidByLord.Clear();

            int baseline = 0;

            // Baseline any raid already active when a save is loaded.
            // That prevents reloading mid-raid from inventing a
            // second grievance for the same ongoing raid.
            foreach (MobileParty party
                     in MobileParty.AllLordParties)
            {
                Settlement target;

                if (!TryGetActiveRaid(
                    party,
                    out target))
                {
                    continue;
                }

                string lordKey =
                    GetLordKey(party);

                if (lordKey == null)
                    continue;

                ActiveRaidByLord[lordKey] =
                    GetRaidKey(target);

                baseline++;
            }

            ClanAIPostVanilla.WriteExternalLog(
                "WORLD_MEMORY_SESSION_READY" +
                " activeRaidBaseline=" +
                baseline);
        }

        public static void Observe(
            MobileParty party)
        {
            if (party == null ||
                party.LeaderHero == null)
            {
                return;
            }

            string lordKey =
                GetLordKey(party);

            if (lordKey == null)
                return;

            Settlement target;

            if (!TryGetActiveRaid(
                party,
                out target))
            {
                ActiveRaidByLord.Remove(
                    lordKey);

                return;
            }

            string raidKey =
                GetRaidKey(target);

            string previous;

            if (ActiveRaidByLord.TryGetValue(
                lordKey,
                out previous) &&
                previous == raidKey)
            {
                return;
            }

            ActiveRaidByLord[lordKey] =
                raidKey;

            SocialLedger.RecordRaidStarted(
                party,
                target);
        }

        private static bool TryGetActiveRaid(
            MobileParty party,
            out Settlement target)
        {
            target = null;

            if (party == null ||
                party.LeaderHero == null ||
                party.MapEvent == null ||
                !party.MapEvent.IsRaid)
            {
                return false;
            }

            // Only the actual raid leader records the event.
            if (party.Party !=
                party.MapEvent.AttackerSide.LeaderParty)
            {
                return false;
            }

            target =
                party.MapEvent.MapEventSettlement;

            if (target == null)
                return false;

            return true;
        }

        private static string GetLordKey(
            MobileParty party)
        {
            if (party == null ||
                party.LeaderHero == null)
            {
                return null;
            }

            string id =
                party.LeaderHero.StringId;

            if (!string.IsNullOrEmpty(id))
                return id;

            return party.LeaderHero.Name.ToString();
        }

        private static string GetRaidKey(
            Settlement settlement)
        {
            if (settlement == null)
                return "<none>";

            if (!string.IsNullOrEmpty(
                settlement.StringId))
            {
                return settlement.StringId;
            }

            return settlement.Name.ToString();
        }
    }
}
