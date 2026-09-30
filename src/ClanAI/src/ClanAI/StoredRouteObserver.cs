using System;
using System.Collections.Generic;
using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace ClanAI
{
    // Evidence-only observer. It reads a capped window from the existing path and never
    // computes, copies, changes, or publishes a route to Bannerlord.
    internal static class StoredRouteObserver
    {
        private static readonly StoredRouteObserverBudget Budget = new StoredRouteObserverBudget();

        internal static void Reset()
        {
            Budget.Reset();
        }

        internal static bool IsEligibleNpcLordParty(MobileParty party)
        {
            if (party == null || !party.IsActive || !party.IsLordParty || party.IsMainParty ||
                party == MobileParty.MainParty || party.IsCaravan || party.IsDisbanding ||
                party.Army != null || party.MapEvent != null || party.SiegeEvent != null ||
                party.AttachedTo != null || party.Ai == null || party.Ai.IsDisabled ||
                party.Ai.DoNotMakeNewDecisions || party.ActualClan == null ||
                party.ActualClan.Kingdom == null || Campaign.Current == null ||
                Campaign.Current.Models == null || Campaign.Current.Models.AgeModel == null)
                return false;

            Hero leader = party.LeaderHero;
            return leader != null && leader != Hero.MainHero && leader.Clan == party.ActualClan &&
                leader.IsAlive && !leader.IsTemplate && !leader.IsPrisoner && leader.IsActive &&
                leader.PartyBelongedTo == party &&
                leader.Age >= Campaign.Current.Models.AgeModel.HeroComesOfAge &&
                leader.IsLord && !string.IsNullOrEmpty(leader.StringId) &&
                !HomeAssignmentStore.IsWaitingForDisband(party);
        }

        internal static void Observe(MobileParty party)
        {
            if (!RuntimeProfile.EvidenceEnabled) return;
            try
            {
                ObserveEligibleParty(party);
            }
            catch
            {
                // Includes eligibility and campaign-hour reads: diagnostics never escape
                // into the campaign AI callback, even if native state is transiently invalid.
                ClanAIPostVanilla.WriteExternalLog(
                    "STORED_ROUTE_OBSERVER status=unavailable party=- visitor=- sampled=0 " +
                    "truncated=False sea=0 unknown=1 errors=1 quotaSkipsTotal=" +
                    Budget.TotalQuotaSkips + " duplicateSkipsTotal=" + Budget.TotalDuplicateSkips +
                    " movementMutation=False candidateMutation=False");
            }
        }

        private static void ObserveEligibleParty(MobileParty party)
        {
            if (!IsEligibleNpcLordParty(party)) return;
            long campaignHour = (long)Math.Floor(CampaignTime.Now.ToHours);
            string partyId = party.StringId;
            if (!Budget.TryAdmit(campaignHour, partyId)) return;

            var samples = new List<StoredRouteSampleResult>(KingdomLandControlPolicy.MaximumRouteWaypoints);
            bool truncated = false;
            bool hadError = false;
            int routeSize = 0;
            int pathBegin = 0;
            try
            {
                var path = party.Path;
                if (path == null)
                {
                    Log(partyId, null, samples, "unavailable", 0, 0, false, 0);
                    return;
                }
                routeSize = path.Size;
                pathBegin = party.PathBegin;
                StoredRouteSampleWindow window;
                if (!StoredRouteObserverPolicy.TryGetWindow(routeSize, pathBegin, out window))
                {
                    Log(partyId, null, samples, "unavailable", routeSize, pathBegin, false, 1);
                    return;
                }
                truncated = window.Truncated;
                if (window.Count == 0)
                {
                    Log(partyId, null, samples, "unavailable", routeSize, pathBegin, truncated, 0);
                    return;
                }

                Kingdom visitorKingdom = party.ActualClan.Kingdom;
                string visitorId = visitorKingdom == null ? null : visitorKingdom.StringId;
                int errors = 0;
                if (string.IsNullOrEmpty(visitorId))
                {
                    hadError = true;
                    errors++;
                }
                for (int i = 0; i < window.Count; i++)
                {
                    try
                    {
                        TaleWorlds.Library.Vec2 point = path[window.Start + i];
                        bool pointError;
                        KingdomLandZoneResult zone = StoredRouteObserverPolicy.ClassifySafely(
                            () => KingdomLandControlZoneNativeAdapter.ClassifyRoutePoint(point), out pointError);
                        if (pointError)
                        {
                            hadError = true;
                            errors++;
                        }
                        Kingdom zoneKingdom = null;
                        if (zone.Kind == KingdomLandZoneKind.KingdomOwned)
                        {
                            // Resolve the locally selected settlement by native ID, then read
                            // its live owner. This is a direct identity lookup, not a world scan.
                            Settlement settlement = Settlement.Find(zone.SettlementId);
                            zoneKingdom = settlement == null || settlement.OwnerClan == null
                                ? null : settlement.OwnerClan.Kingdom;
                            if (zoneKingdom == null ||
                                !string.Equals(zoneKingdom.StringId, zone.KingdomId, StringComparison.Ordinal))
                            {
                                zone = new KingdomLandZoneResult(KingdomLandZoneKind.Unknown, null,
                                    zone.SettlementId);
                                hadError = true;
                                errors++;
                            }
                        }

                        bool atWarWithZone = zoneKingdom != null && visitorKingdom != null &&
                            visitorKingdom.IsAtWarWith(zoneKingdom);
                        bool closed = zone.Kind == KingdomLandZoneKind.KingdomOwned &&
                            KingdomBorderClosureConfig.IsClosed(zone.KingdomId, visitorId);
                        samples.Add(new StoredRouteSampleResult(zone.Kind, zone.KingdomId,
                            zone.SettlementId, closed, atWarWithZone));
                    }
                    catch
                    {
                        hadError = true;
                        errors++;
                        samples.Add(new StoredRouteSampleResult(KingdomLandZoneKind.Unknown,
                            null, null, false, false));
                    }
                }

                StoredRouteObservationSummary summary = StoredRouteObserverPolicy.Summarize(
                    samples, visitorId, truncated, hadError);
                Log(partyId, visitorId, summary, routeSize, pathBegin, errors);
            }
            catch
            {
                // A diagnostic observer exception is isolated from the native AI callback.
                Log(partyId, null, samples, "unavailable", routeSize, pathBegin, truncated, 1);
            }
        }

        private static void Log(string partyId, string visitorId,
            IList<StoredRouteSampleResult> samples, string status,
            int routeSize, int pathBegin, bool truncated, int errors)
        {
            var summary = StoredRouteObserverPolicy.Summarize(samples, visitorId, truncated, errors != 0);
            Log(partyId, visitorId, summary, routeSize, pathBegin, errors);
        }

        private static void Log(string partyId, string visitorId,
            StoredRouteObservationSummary summary, int routeSize, int pathBegin, int errors)
        {
            ClanAIPostVanilla.WriteExternalLog(
                "STORED_ROUTE_OBSERVER" +
                " status=" + summary.Status +
                " party=" + Safe(partyId) +
                " visitor=" + Safe(visitorId) +
                " routeSize=" + routeSize +
                " pathBegin=" + pathBegin +
                " sampled=" + summary.Samples +
                " truncated=" + summary.Truncated +
                " sea=" + summary.SeaSamples +
                " unknown=" + summary.UnknownSamples +
                " zoneWarSkips=" + summary.WarSkips +
                " zoneKingdoms=" + Join(summary.ZoneKingdomIds) +
                " settlements=" + Join(summary.SettlementIds) +
                " closedDirections=" + Join(summary.ClosedDirections) +
                " errors=" + errors +
                " quotaSkipsTotal=" + Budget.TotalQuotaSkips +
                " duplicateSkipsTotal=" + Budget.TotalDuplicateSkips +
                " movementMutation=False candidateMutation=False");
        }

        private static string Join(List<string> values)
        { return values.Count == 0 ? "-" : string.Join(",", values.ToArray()); }

        private static string Safe(string value)
        { return string.IsNullOrEmpty(value) ? "-" : value.Replace(" ", "_"); }
    }
}
