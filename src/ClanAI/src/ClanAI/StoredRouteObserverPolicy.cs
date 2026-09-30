using System;
using System.Collections.Generic;
using System.Threading;

namespace ClanAI
{
    internal enum StoredRouteNavigationKind
    {
        Unknown = 0,
        Land = 1,
        Sea = 2
    }

    internal struct StoredRouteSampleWindow
    {
        internal readonly int Start;
        internal readonly int Count;
        internal readonly int Remaining;
        internal readonly bool Truncated;

        internal StoredRouteSampleWindow(int start, int count, int remaining, bool truncated)
        { Start = start; Count = count; Remaining = remaining; Truncated = truncated; }
    }

    internal sealed class StoredRouteObserverBudget
    {
        internal const int MaximumPartiesPerHour = 4;
        private readonly HashSet<string> _admitted = new HashSet<string>(StringComparer.Ordinal);
        private long _hour = long.MinValue;
        private int _quotaSkips;
        private int _duplicateSkips;
        private long _totalQuotaSkips;
        private long _totalDuplicateSkips;

        internal int QuotaSkips { get { return _quotaSkips; } }
        internal int DuplicateSkips { get { return _duplicateSkips; } }
        internal int AdmittedCount { get { return _admitted.Count; } }
        internal long TotalQuotaSkips { get { return _totalQuotaSkips; } }
        internal long TotalDuplicateSkips { get { return _totalDuplicateSkips; } }

        internal void Reset()
        {
            _admitted.Clear();
            _hour = long.MinValue;
            _quotaSkips = 0;
            _duplicateSkips = 0;
            _totalQuotaSkips = 0;
            _totalDuplicateSkips = 0;
        }

        internal bool TryAdmit(long campaignHour, string partyId)
        {
            if (_hour != campaignHour)
            {
                _hour = campaignHour;
                _admitted.Clear();
                _quotaSkips = 0;
                _duplicateSkips = 0;
            }
            if (string.IsNullOrEmpty(partyId))
            {
                _quotaSkips++;
                _totalQuotaSkips++;
                return false;
            }
            if (_admitted.Contains(partyId))
            {
                _duplicateSkips++;
                _totalDuplicateSkips++;
                return false;
            }
            if (_admitted.Count >= MaximumPartiesPerHour)
            {
                _quotaSkips++;
                _totalQuotaSkips++;
                return false;
            }
            _admitted.Add(partyId);
            return true;
        }
    }

    // A clock-independent emergency gate for exceptions before normal hourly admission.
    // One diagnostic write per campaign session; suppressed events remain countable and
    // are included in the next regular bounded observer record.
    internal sealed class PreAdmissionObserverErrorReporter
    {
        private int _logClaimed;
        private long _failures;
        private long _suppressed;

        internal long Failures { get { return Interlocked.Read(ref _failures); } }
        internal long Suppressed { get { return Interlocked.Read(ref _suppressed); } }

        internal bool Report(Action<string> writeLog, long quotaSkips, long duplicateSkips)
        {
            long failureNumber = Interlocked.Increment(ref _failures);
            if (Interlocked.CompareExchange(ref _logClaimed, 1, 0) != 0)
            {
                Interlocked.Increment(ref _suppressed);
                return false;
            }

            if (writeLog != null)
            {
                try
                {
                    writeLog("STORED_ROUTE_OBSERVER status=unavailable party=- visitor=- sampled=0 " +
                        "truncated=False sea=0 unknown=1 errors=1 preAdmissionFailuresTotal=" + failureNumber +
                        " preAdmissionErrorLogsSuppressed=" + Suppressed +
                        " quotaSkipsTotal=" + quotaSkips + " duplicateSkipsTotal=" + duplicateSkips +
                        " movementMutation=False candidateMutation=False");
                }
                catch { }
            }
            return true;
        }

        internal void Reset()
        {
            Interlocked.Exchange(ref _logClaimed, 0);
            Interlocked.Exchange(ref _failures, 0);
            Interlocked.Exchange(ref _suppressed, 0);
        }
    }

    internal struct StoredRouteSampleResult
    {
        internal readonly KingdomLandZoneKind Kind;
        internal readonly string KingdomId;
        internal readonly string SettlementId;
        internal readonly bool ExplicitlyClosed;
        internal readonly bool AtWarWithZoneKingdom;

        internal StoredRouteSampleResult(KingdomLandZoneKind kind, string kingdomId,
            string settlementId, bool explicitlyClosed, bool atWarWithZoneKingdom)
        {
            Kind = kind;
            KingdomId = kingdomId;
            SettlementId = settlementId;
            ExplicitlyClosed = explicitlyClosed;
            AtWarWithZoneKingdom = atWarWithZoneKingdom;
        }
    }

    internal sealed class StoredRouteObservationSummary
    {
        internal string Status;
        internal int Samples;
        internal int SeaSamples;
        internal int UnknownSamples;
        internal int WarSkips;
        internal bool Truncated;
        internal readonly List<string> ZoneKingdomIds = new List<string>();
        internal readonly List<string> SettlementIds = new List<string>();
        internal readonly List<string> ClosedDirections = new List<string>();
    }

    internal static class StoredRouteObserverPolicy
    {
        internal static StoredRouteNavigationKind ClassifyNavigationModes(bool landValid, bool seaValid)
        {
            if (landValid == seaValid) return StoredRouteNavigationKind.Unknown;
            return landValid ? StoredRouteNavigationKind.Land : StoredRouteNavigationKind.Sea;
        }

        internal static KingdomLandZoneResult ClassifySafely(
            Func<KingdomLandZoneResult> classify, out bool hadError)
        {
            try
            {
                hadError = false;
                return classify == null
                    ? new KingdomLandZoneResult(KingdomLandZoneKind.Unknown, null, null)
                    : classify();
            }
            catch
            {
                hadError = true;
                return new KingdomLandZoneResult(KingdomLandZoneKind.Unknown, null, null);
            }
        }

        internal static bool TryGetWindow(int pathSize, int pathBegin, out StoredRouteSampleWindow window)
        {
            window = new StoredRouteSampleWindow(0, 0, 0, false);
            if (pathSize < 0 || pathBegin < 0 || pathBegin > pathSize)
                return false;
            int remaining = pathSize - pathBegin;
            int count = Math.Min(KingdomLandControlPolicy.MaximumRouteWaypoints, remaining);
            window = new StoredRouteSampleWindow(pathBegin, count, remaining,
                remaining > KingdomLandControlPolicy.MaximumRouteWaypoints);
            return true;
        }

        internal static StoredRouteObservationSummary Summarize(
            IList<StoredRouteSampleResult> samples, string visitorKingdomId,
            bool truncated, bool hadError)
        {
            var summary = new StoredRouteObservationSummary();
            summary.Truncated = truncated;
            if (samples == null || samples.Count == 0)
            {
                summary.Status = "unavailable";
                return summary;
            }

            summary.Samples = samples.Count;
            bool unknown = hadError;
            bool closedFound = false;
            for (int i = 0; i < samples.Count; i++)
            {
                StoredRouteSampleResult sample = samples[i];
                if (sample.Kind == KingdomLandZoneKind.SeaOrOpenWater)
                    summary.SeaSamples++;
                else if (sample.Kind == KingdomLandZoneKind.Unknown ||
                    sample.Kind == KingdomLandZoneKind.Ambiguous ||
                    sample.Kind == KingdomLandZoneKind.NoNearbyFortification)
                {
                    unknown = true;
                    summary.UnknownSamples++;
                }

                if (!string.IsNullOrEmpty(sample.KingdomId) &&
                    !summary.ZoneKingdomIds.Contains(sample.KingdomId))
                    summary.ZoneKingdomIds.Add(sample.KingdomId);
                if (!string.IsNullOrEmpty(sample.SettlementId) &&
                    !summary.SettlementIds.Contains(sample.SettlementId))
                    summary.SettlementIds.Add(sample.SettlementId);

                bool blocked = KingdomLandControlPolicy.BlocksRouteZone(
                    new KingdomLandZoneResult(sample.Kind, sample.KingdomId, sample.SettlementId),
                    visitorKingdomId, sample.AtWarWithZoneKingdom, sample.ExplicitlyClosed);
                if (blocked)
                {
                    closedFound = true;
                    string direction = sample.KingdomId + ">" + visitorKingdomId;
                    if (!summary.ClosedDirections.Contains(direction))
                        summary.ClosedDirections.Add(direction);
                }
                else if (sample.Kind == KingdomLandZoneKind.KingdomOwned &&
                    sample.ExplicitlyClosed && sample.AtWarWithZoneKingdom)
                    summary.WarSkips++;
            }

            summary.ZoneKingdomIds.Sort(StringComparer.Ordinal);
            summary.SettlementIds.Sort(StringComparer.Ordinal);
            summary.ClosedDirections.Sort(StringComparer.Ordinal);
            summary.Status = closedFound ? "closed_zone_sample_observed" :
                (unknown || truncated ? "partial" : "no_closed_zone_in_samples");
            return summary;
        }
    }
}
