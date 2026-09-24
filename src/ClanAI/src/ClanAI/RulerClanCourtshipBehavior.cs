using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.BarterSystem.Barterables;
using TaleWorlds.CampaignSystem.Party.PartyComponents;

namespace ClanAI
{
    /// <summary>
    /// Gives NPC rulers a ruler-initiated entry point into Bannerlord's native
    /// independent-clan join barter. Candidate desirability, clan acceptance,
    /// barter resolution, and the final kingdom action remain native.
    /// </summary>
    public sealed class RulerClanCourtshipBehavior : CampaignBehaviorBase
    {
        private long _considerations;
        private long _positiveSurplus;
        private long _attempts;
        private long _commits;

        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickClanEvent.AddNonSerializedListener(
                this,
                new Action<Clan>(OnDailyTickClan));

            ClanAIPostVanilla.WriteExternalLog(
                "RULER_COURTSHIP_REGISTERED" +
                " selector=native_GetScoreOfKingdomToGetClan" +
                " executor=native_ExecuteAiBarter" +
                " customScore=False" +
                " directFactionTransfer=False");
        }

        public override void SyncData(IDataStore dataStore)
        {
        }

        private void OnDailyTickClan(Clan tickingClan)
        {
            try
            {
                Kingdom kingdom =
                    tickingClan != null
                        ? tickingClan.Kingdom
                        : null;

                if (!IsEligibleRecruiter(tickingClan, kingdom))
                    return;

                Clan best = null;
                float bestKingdomScore = float.MinValue;
                int eligibleCount = 0;

                foreach (Clan candidate in Clan.All)
                {
                    if (!IsEligibleCandidate(candidate, kingdom))
                        continue;

                    eligibleCount++;

                    float kingdomScore =
                        Campaign.Current.Models.DiplomacyModel
                            .GetScoreOfKingdomToGetClan(
                                kingdom,
                                candidate);

                    if (best == null ||
                        kingdomScore > bestKingdomScore)
                    {
                        best = candidate;
                        bestKingdomScore = kingdomScore;
                    }
                }

                if (best == null)
                {
                    _considerations++;
                    WriteConsideration(
                        kingdom,
                        null,
                        eligibleCount,
                        0f,
                        0,
                        0,
                        0,
                        false,
                        false,
                        false,
                        null);
                    return;
                }

                var barterable =
                    new JoinKingdomAsClanBarterable(
                        best.Leader,
                        kingdom);

                int clanValue =
                    barterable.GetValueForFaction(best);
                int kingdomValue =
                    barterable.GetValueForFaction(kingdom);
                int combinedValue =
                    clanValue + kingdomValue;
                bool shouldAttempt =
                    combinedValue > 0;

                _considerations++;
                if (shouldAttempt)
                    _positiveSurplus++;

                string preKingdom = KingdomName(best.Kingdom);
                bool attempted = false;
                bool accepted = false;

                if (shouldAttempt)
                {
                    attempted = true;
                    _attempts++;

                    Campaign.Current.BarterManager.ExecuteAiBarter(
                        best,
                        kingdom,
                        best.Leader,
                        kingdom.Leader,
                        barterable);

                    accepted =
                        Campaign.Current.BarterManager
                            .LastBarterIsAccepted;
                }

                bool committed =
                    best.Kingdom == kingdom;

                if (committed)
                    _commits++;

                WriteConsideration(
                    kingdom,
                    best,
                    eligibleCount,
                    bestKingdomScore,
                    clanValue,
                    kingdomValue,
                    combinedValue,
                    shouldAttempt,
                    attempted,
                    accepted,
                    preKingdom);
            }
            catch (Exception ex)
            {
                ClanAIPostVanilla.WriteExternalLog(
                    "RULER_COURTSHIP_FAILED" +
                    " type=" + ex.GetType().Name +
                    " message=" + Sanitize(ex.Message));
            }
        }

        private static bool IsEligibleRecruiter(
            Clan tickingClan,
            Kingdom kingdom)
        {
            return tickingClan != null &&
                kingdom != null &&
                kingdom.RulingClan == tickingClan &&
                tickingClan != Clan.PlayerClan &&
                kingdom.RulingClan != Clan.PlayerClan &&
                kingdom.Leader != null &&
                kingdom.Leader != Hero.MainHero &&
                !kingdom.IsEliminated;
        }

        private static bool IsEligibleCandidate(
            Clan clan,
            Kingdom kingdom)
        {
            if (clan == null ||
                kingdom == null ||
                clan == Clan.PlayerClan ||
                clan.Leader == null ||
                clan.Leader == Hero.MainHero ||
                clan.CurrentTotalStrength <= 0f ||
                clan.IsEliminated ||
                clan.IsBanditFaction ||
                clan.IsRebelClan ||
                clan.IsMinorFaction ||
                clan.IsClanTypeMercenary ||
                clan.IsUnderMercenaryService ||
                clan.Kingdom != null ||
                clan.MapFaction == kingdom ||
                clan.MapFaction.IsAtWarWith(kingdom) ||
                Campaign.Current.Models.DiplomacyModel
                    .IsAtConstantWar(clan, kingdom) ||
                !clan.ShouldStayInKingdomUntil.IsPast ||
                !clan.WarPartyComponents.All(
                    (WarPartyComponent x) =>
                        x.MobileParty.MapEvent == null))
            {
                return false;
            }

            // Mirror vanilla's safety gate: do not recruit a normal clan while
            // doing so would strand it in a war the target kingdom will not join.
            foreach (Kingdom other in Kingdom.All)
            {
                if (other != kingdom &&
                    clan.IsAtWarWith(other) &&
                    !other.IsAtWarWith(kingdom) &&
                    !(kingdom.CurrentTotalStrength >
                        10f * other.CurrentTotalStrength))
                {
                    return false;
                }
            }

            return true;
        }

        private void WriteConsideration(
            Kingdom recruiter,
            Clan candidate,
            int eligibleCount,
            float nativeSelectionScore,
            int clanValue,
            int kingdomValue,
            int combinedValue,
            bool positive,
            bool attempted,
            bool accepted,
            string preKingdom)
        {
            bool committed =
                candidate != null &&
                candidate.Kingdom == recruiter;

            ClanAIPostVanilla.WriteExternalLog(
                "RULER_COURTSHIP_CONSIDER" +
                " recruiter=" + KingdomName(recruiter) +
                " ruler=" + HeroName(
                    recruiter != null
                        ? recruiter.Leader
                        : null) +
                " eligibleCount=" + eligibleCount +
                " candidate=" + ClanName(candidate) +
                " leader=" + HeroName(
                    candidate != null
                        ? candidate.Leader
                        : null) +
                " preKingdom=" +
                    (preKingdom ?? "<none>") +
                " strength=" +
                    (candidate != null
                        ? candidate.CurrentTotalStrength.ToString("0.###")
                        : "0") +
                " warPartyLimit=" +
                    (candidate != null
                        ? candidate.WarPartyLimit.ToString()
                        : "0") +
                " holdings=" +
                    (candidate != null
                        ? candidate.Settlements.Count.ToString()
                        : "0") +
                " holdingsValue=<not_exposed_by_candidate_surface>" +
                " cultureMatch=" +
                    (candidate != null &&
                     recruiter != null &&
                     candidate.Culture == recruiter.Culture) +
                " nativeSelectionScore=" +
                    nativeSelectionScore.ToString("0.###") +
                " nativeClanValue=" + clanValue +
                " nativeKingdomValue=" + kingdomValue +
                " nativeCombinedValue=" + combinedValue +
                " selectedBest=" + (candidate != null) +
                " positiveSurplus=" + positive +
                " attempted=" + attempted +
                " barterAccepted=" + accepted +
                " committed=" + committed +
                " kingdomAfter=" + KingdomName(
                    candidate != null
                        ? candidate.Kingdom
                        : null) +
                " customScore=False" +
                " directFactionTransfer=False" +
                " considerations=" + _considerations +
                " positive=" + _positiveSurplus +
                " attempts=" + _attempts +
                " commits=" + _commits);
        }

        private static string KingdomName(Kingdom kingdom)
        {
            return kingdom != null
                ? kingdom.Name.ToString()
                : "<none>";
        }

        private static string ClanName(Clan clan)
        {
            return clan != null
                ? clan.Name.ToString()
                : "<none>";
        }

        private static string HeroName(Hero hero)
        {
            return hero != null
                ? hero.Name.ToString()
                : "<none>";
        }

        private static string Sanitize(string value)
        {
            return (value ?? string.Empty)
                .Replace('\r', ' ')
                .Replace('\n', ' ');
        }
    }
}
