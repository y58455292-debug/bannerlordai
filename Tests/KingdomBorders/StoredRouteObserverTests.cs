using System;
using System.Collections.Generic;
using ClanAI;

internal static class StoredRouteObserverTests
{
    internal static int Run()
    {
        int failures = 0;
        int cases = 0;
        Action<string, bool> check = (name, condition) =>
        {
            cases++;
            if (!condition)
            {
                Console.WriteLine("FAIL stored-route " + name);
                failures++;
            }
        };

        check("land-valid-sea-invalid-is-land", StoredRouteObserverPolicy.ClassifyNavigationModes(true, false) == StoredRouteNavigationKind.Land);
        check("sea-valid-land-invalid-is-sea", StoredRouteObserverPolicy.ClassifyNavigationModes(false, true) == StoredRouteNavigationKind.Sea);
        check("both-navigation-modes-valid-is-unknown", StoredRouteObserverPolicy.ClassifyNavigationModes(true, true) == StoredRouteNavigationKind.Unknown);
        check("both-navigation-modes-invalid-is-unknown", StoredRouteObserverPolicy.ClassifyNavigationModes(false, false) == StoredRouteNavigationKind.Unknown);
        bool classifyError;
        KingdomLandZoneResult exceptionResult = StoredRouteObserverPolicy.ClassifySafely(
            () => { throw new InvalidOperationException("fixture"); }, out classifyError);
        check("native-classifier-exception-fails-open", classifyError && exceptionResult.Kind == KingdomLandZoneKind.Unknown);

        var reporter = new PreAdmissionObserverErrorReporter();
        var errorRecords = new List<string>();
        Action<string> injectedLogger = message => errorRecords.Add(message);
        for (int i = 0; i < 10000; i++) reporter.Report(injectedLogger, 17, 3);
        check("repeated-pre-admission-errors-write-once-per-session", errorRecords.Count == 1);
        check("pre-admission-failures-and-suppressions-counted", reporter.Failures == 10000 && reporter.Suppressed == 9999);
        check("bounded-error-record-includes-skip-totals", errorRecords[0].Contains("quotaSkipsTotal=17") &&
            errorRecords[0].Contains("duplicateSkipsTotal=3"));
        reporter.Reset();
        for (int i = 0; i < 3; i++) reporter.Report(injectedLogger, 2, 1);
        check("session-reset-reopens-single-log-and-clears-counts", errorRecords.Count == 2 &&
            reporter.Failures == 3 && reporter.Suppressed == 2);
        var throwingReporter = new PreAdmissionObserverErrorReporter();
        bool writerEscaped = false;
        try { throwingReporter.Report(_ => { throw new InvalidOperationException("logger fixture"); }, 0, 0); }
        catch { writerEscaped = true; }
        check("diagnostic-writer-failure-is-isolated", !writerEscaped && throwingReporter.Failures == 1);

        StoredRouteSampleWindow window;
        check("null-empty-path-window", StoredRouteObserverPolicy.TryGetWindow(0, 0, out window) && window.Count == 0);
        check("negative-size-rejected", !StoredRouteObserverPolicy.TryGetWindow(-1, 0, out window));
        check("negative-begin-rejected", !StoredRouteObserverPolicy.TryGetWindow(8, -1, out window));
        check("begin-after-end-rejected", !StoredRouteObserverPolicy.TryGetWindow(8, 9, out window));
        check("eight-points-not-truncated", StoredRouteObserverPolicy.TryGetWindow(8, 0, out window) &&
            window.Start == 0 && window.Count == 8 && window.Remaining == 8 && !window.Truncated);
        check("ninth-point-truncated", StoredRouteObserverPolicy.TryGetWindow(12, 3, out window) &&
            window.Start == 3 && window.Count == 8 && window.Remaining == 9 && window.Truncated);
        check("exhausted-path-valid-empty", StoredRouteObserverPolicy.TryGetWindow(4, 4, out window) && window.Count == 0);

        var budget = new StoredRouteObserverBudget();
        budget.Reset();
        for (int i = 0; i < StoredRouteObserverBudget.MaximumPartiesPerHour; i++)
            check("quota-admits-" + i, budget.TryAdmit(10, "party-" + i));
        check("duplicate-suppressed", !budget.TryAdmit(10, "party-0") && budget.TotalDuplicateSkips == 1);
        check("fifth-party-quota-skipped", !budget.TryAdmit(10, "party-4") && budget.TotalQuotaSkips == 1);
        check("hour-rollover-resets-admission", budget.TryAdmit(11, "party-4") && budget.AdmittedCount == 1);
        check("session-counters-persist-hour", budget.TotalDuplicateSkips == 1 && budget.TotalQuotaSkips == 1);
        budget.Reset();
        check("session-reset-clears-counters", budget.TotalDuplicateSkips == 0 && budget.TotalQuotaSkips == 0);

        check("empty-is-unavailable", StoredRouteObserverPolicy.Summarize(null, "visitor", false, false).Status == "unavailable");
        var samples = new List<StoredRouteSampleResult>
        {
            new StoredRouteSampleResult(KingdomLandZoneKind.SeaOrOpenWater, null, null, false, false),
            new StoredRouteSampleResult(KingdomLandZoneKind.KingdomOwned, "zone-b", "fort-enclave", true, false)
        };
        var closed = StoredRouteObserverPolicy.Summarize(samples, "visitor-a", false, false);
        check("sea-is-separate-from-land", closed.SeaSamples == 1);
        check("closed-zone-observed-directionally", closed.Status == "closed_zone_sample_observed" &&
            closed.ClosedDirections.Count == 1 && closed.ClosedDirections[0] == "zone-b>visitor-a");
        check("zone-and-settlement-identities-reported", closed.ZoneKingdomIds.Count == 1 &&
            closed.ZoneKingdomIds[0] == "zone-b" && closed.SettlementIds[0] == "fort-enclave");

        var reverseOpen = StoredRouteObserverPolicy.Summarize(new List<StoredRouteSampleResult>
        {
            new StoredRouteSampleResult(KingdomLandZoneKind.KingdomOwned, "visitor-a", "fort-a", false, false)
        }, "zone-b", false, false);
        check("reverse-direction-is-open", reverseOpen.Status == "no_closed_zone_in_samples");

        var zoneWar = StoredRouteObserverPolicy.Summarize(new List<StoredRouteSampleResult>
        {
            new StoredRouteSampleResult(KingdomLandZoneKind.KingdomOwned, "zone-b", "fort-b", true, true)
        }, "visitor-a", false, false);
        check("war-with-this-zone-is-exempt", zoneWar.Status == "no_closed_zone_in_samples" && zoneWar.WarSkips == 1);

        var unrelatedWarDoesNotExempt = StoredRouteObserverPolicy.Summarize(new List<StoredRouteSampleResult>
        {
            // Runtime supplies true only when the visitor is at war with this sampled zone owner;
            // a war with another kingdom therefore follows the explicit-closure path.
            new StoredRouteSampleResult(KingdomLandZoneKind.KingdomOwned, "zone-b", "fort-b", true, false)
        }, "visitor-a", false, false);
        check("unrelated-war-does-not-exempt-zone", unrelatedWarDoesNotExempt.Status == "closed_zone_sample_observed");

        var unknown = StoredRouteObserverPolicy.Summarize(new List<StoredRouteSampleResult>
        {
            new StoredRouteSampleResult(KingdomLandZoneKind.Unknown, null, null, false, false)
        }, "visitor-a", false, false);
        check("unknown-is-partial", unknown.Status == "partial" && unknown.UnknownSamples == 1);
        check("route-cap-is-partial-not-open", StoredRouteObserverPolicy.Summarize(samples, "visitor-a", true, false).Status == "closed_zone_sample_observed" &&
            StoredRouteObserverPolicy.Summarize(new List<StoredRouteSampleResult> { samples[0] }, "visitor-a", true, false).Status == "partial");
        check("isolated-sample-exception-is-partial", StoredRouteObserverPolicy.Summarize(
            new List<StoredRouteSampleResult> { samples[0] }, "visitor-a", false, true).Status == "partial");
        check("empty-kingdom-id-fails-open", !KingdomLandControlPolicy.BlocksRouteZone(
            new KingdomLandZoneResult(KingdomLandZoneKind.KingdomOwned, null, "fort"), "visitor-a", false, true));

        Console.WriteLine("PASS stored-route observer policy cases=" + cases + " failures=" + failures);
        return failures;
    }
}
