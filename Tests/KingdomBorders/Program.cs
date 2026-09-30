using System;
using ClanAI;

internal static class Program
{
    private static int _failures;

    private static void Expect(string name, KingdomBorderRelation expected,
        bool sea, string actorKingdom, bool hasOwner, string ownerKingdom, bool closed)
    {
        KingdomBorderRelation actual = KingdomBorderPolicy.ClassifySettlementDestination(
            sea, actorKingdom, hasOwner, ownerKingdom, closed);
        if (actual != expected)
        {
            Console.WriteLine("FAIL " + name + " expected=" + expected + " actual=" + actual);
            _failures++;
        }
    }

    private static void ExpectBlock(string name, bool expected, bool ordinaryLord,
        bool atWar, bool hasOwner, string actorKingdom, string ownerKingdom, bool closed)
    {
        bool actual = KingdomBorderPolicy.BlocksLordSettlementVisit(
            ordinaryLord, atWar, hasOwner, actorKingdom, ownerKingdom, closed);
        if (actual != expected)
        {
            Console.WriteLine("FAIL " + name + " expected=" + expected + " actual=" + actual);
            _failures++;
        }
    }

    private static KingdomLandZoneResult Zone(bool onLand, bool complete,
        params KingdomLandZoneSettlement[] candidates)
    {
        return KingdomLandControlPolicy.Classify(onLand, complete, candidates);
    }

    private static void ExpectZone(string name, KingdomLandZoneKind expected,
        bool onLand, bool complete, params KingdomLandZoneSettlement[] candidates)
    {
        KingdomLandZoneKind actual = Zone(onLand, complete, candidates).Kind;
        if (actual != expected)
        {
            Console.WriteLine("FAIL " + name + " expected=" + expected + " actual=" + actual);
            _failures++;
        }
    }

    private static void ExpectRouteBlock(string name, bool expected,
        KingdomLandZoneResult zone, string visitor, bool atWar, bool explicitlyClosed)
    {
        bool actual = KingdomLandControlPolicy.BlocksRouteZone(zone, visitor, atWar, explicitlyClosed);
        if (actual != expected)
        {
            Console.WriteLine("FAIL " + name + " expected=" + expected + " actual=" + actual);
            _failures++;
        }
    }

    private static void ExpectZoneSettlementId(string name, string expected,
        params KingdomLandZoneSettlement[] candidates)
    {
        string actual = Zone(true, true, candidates).SettlementId;
        if (!string.Equals(actual, expected, StringComparison.Ordinal))
        {
            Console.WriteLine("FAIL " + name + " expected=" + expected + " actual=" + actual);
            _failures++;
        }
    }

    private static void ExpectZoneIdentity(string name, KingdomLandZoneKind expectedKind,
        string expectedKingdomId, string expectedSettlementId,
        params KingdomLandZoneSettlement[] candidates)
    {
        KingdomLandZoneResult result = Zone(true, true, candidates);
        if (result.Kind != expectedKind ||
            !string.Equals(result.KingdomId, expectedKingdomId, StringComparison.Ordinal) ||
            !string.Equals(result.SettlementId, expectedSettlementId, StringComparison.Ordinal))
        {
            Console.WriteLine("FAIL " + name + " expected=" + expectedKind + "/" + expectedKingdomId +
                "/" + expectedSettlementId + " actual=" + result.Kind + "/" + result.KingdomId + "/" + result.SettlementId);
            _failures++;
        }
    }

    private static int Main()
    {
        Expect("same-kingdom", KingdomBorderRelation.Open, false, "kingdom-a", true, "kingdom-a", true);
        Expect("foreign-open-by-default", KingdomBorderRelation.Open, false, "kingdom-a", true, "kingdom-b", false);
        Expect("explicit-directional-closure", KingdomBorderRelation.Closed, false, "kingdom-a", true, "kingdom-b", true);
        Expect("closure-does-not-rewrite-enclave-owner", KingdomBorderRelation.Closed, false, "kingdom-a", true, "kingdom-b", true);
        Expect("captured-fief-uses-current-owner", KingdomBorderRelation.Open, false, "kingdom-a", true, "kingdom-a", false);
        Expect("independent-owner", KingdomBorderRelation.IndependentDestination, false, "kingdom-a", true, null, false);
        Expect("unknown-owner", KingdomBorderRelation.Unknown, false, "kingdom-a", false, null, false);
        Expect("independent-actor-is-not-guessed", KingdomBorderRelation.Unknown, false, null, true, "kingdom-b", false);
        Expect("distant-open-water-is-unknown", KingdomBorderRelation.SeaOrOpenWater, true, "kingdom-a", true, "kingdom-b", true);
        Expect("missing-kingdom-is-not-equal-to-missing-kingdom", KingdomBorderRelation.Unknown, false, null, true, null, false);
        ExpectBlock("closed-border-blocks-lord-visit", true, true, false, true, "kingdom-a", "kingdom-b", true);
        ExpectBlock("open-border-keeps-native-visit", false, true, false, true, "kingdom-a", "kingdom-b", false);
        ExpectBlock("war-operation-passes-through", false, true, true, true, "kingdom-a", "kingdom-b", true);
        ExpectBlock("non-lord-party-passes-through", false, false, false, true, "kingdom-a", "kingdom-b", true);

        var nearestB = new KingdomLandZoneSettlement("fort-enclave", true, true, "kingdom-b", 1f);
        var fartherA = new KingdomLandZoneSettlement("fort-surrounding", true, true, "kingdom-a", 4f);
        ExpectZone("nearest-current-owner", KingdomLandZoneKind.KingdomOwned, true, true, fartherA, nearestB);
        ExpectZone("enclave-keeps-actual-owner", KingdomLandZoneKind.KingdomOwned, true, true, nearestB, fartherA);
        ExpectZoneIdentity("enclave-owner-and-settlement-exact", KingdomLandZoneKind.KingdomOwned,
            "kingdom-b", "fort-enclave", fartherA, nearestB);
        ExpectZoneIdentity("enclave-permutation-preserves-identity", KingdomLandZoneKind.KingdomOwned,
            "kingdom-b", "fort-enclave", nearestB, fartherA);
        ExpectZone("capture-uses-new-live-owner", KingdomLandZoneKind.KingdomOwned, true, true,
            new KingdomLandZoneSettlement("fort-enclave", true, true, "kingdom-c", 1f));
        ExpectZoneIdentity("capture-uses-new-owner-and-same-fief", KingdomLandZoneKind.KingdomOwned,
            "kingdom-c", "fort-enclave",
            new KingdomLandZoneSettlement("fort-enclave", true, true, "kingdom-c", 1f));
        ExpectZoneIdentity("later-transfer-is-read-live", KingdomLandZoneKind.KingdomOwned,
            "kingdom-d", "fort-enclave",
            new KingdomLandZoneSettlement("fort-enclave", true, true, "kingdom-d", 1f));
        ExpectZoneIdentity("recaptured-fief-is-read-live-again", KingdomLandZoneKind.KingdomOwned,
            "kingdom-a", "fort-enclave",
            new KingdomLandZoneSettlement("fort-enclave", true, true, "kingdom-a", 1f));
        ExpectZone("sea-skips-nearest-land", KingdomLandZoneKind.SeaOrOpenWater, false, true, fartherA);
        ExpectZone("incomplete-native-search-fails-open", KingdomLandZoneKind.Unknown, true, false, nearestB);
        ExpectZone("ambiguous-equidistant-owners", KingdomLandZoneKind.Ambiguous, true, true,
            new KingdomLandZoneSettlement("fort-a", true, true, "kingdom-a", 1f),
            new KingdomLandZoneSettlement("fort-b", true, true, "kingdom-b", 1f));
        ExpectZone("ambiguous-tie-independent-of-order", KingdomLandZoneKind.Ambiguous, true, true,
            new KingdomLandZoneSettlement("fort-b", true, true, "kingdom-b", 1f),
            new KingdomLandZoneSettlement("fort-a", true, true, "kingdom-a", 1f));
        ExpectZone("same-owner-tie-is-resolved", KingdomLandZoneKind.KingdomOwned, true, true,
            new KingdomLandZoneSettlement("fort-b", true, true, "kingdom-a", 1f),
            new KingdomLandZoneSettlement("fort-a", true, true, "kingdom-a", 1f));
        ExpectZoneSettlementId("same-owner-tie-id-is-ordinal", "fort-a",
            new KingdomLandZoneSettlement("fort-b", true, true, "kingdom-a", 1f),
            new KingdomLandZoneSettlement("fort-a", true, true, "kingdom-a", 1f));
        ExpectZoneSettlementId("same-owner-tie-id-independent-of-order", "fort-a",
            new KingdomLandZoneSettlement("fort-a", true, true, "kingdom-a", 1f),
            new KingdomLandZoneSettlement("fort-b", true, true, "kingdom-a", 1f));
        ExpectZone("independent-fort-remains-independent", KingdomLandZoneKind.IndependentOwned, true, true,
            new KingdomLandZoneSettlement("independent-fort", true, true, null, 1f));
        ExpectZone("unowned-fort-is-unknown", KingdomLandZoneKind.Unknown, true, true,
            new KingdomLandZoneSettlement("unowned-fort", true, false, null, 1f));
        ExpectZone("villages-do-not-define-control-zone", KingdomLandZoneKind.NoNearbyFortification, true, true,
            new KingdomLandZoneSettlement("village", false, true, "kingdom-a", 1f));
        ExpectZone("native-radius-edge-excluded", KingdomLandZoneKind.NoNearbyFortification, true, true,
            new KingdomLandZoneSettlement("fort-edge", true, true, "kingdom-a", 25f));
        ExpectZone("outside-radius-excluded", KingdomLandZoneKind.NoNearbyFortification, true, true,
            new KingdomLandZoneSettlement("fort-outside", true, true, "kingdom-a", 25.01f));
        ExpectZone("invalid-native-distance-is-unknown", KingdomLandZoneKind.Unknown, true, true,
            new KingdomLandZoneSettlement("fort-invalid", true, true, "kingdom-a", float.NaN));
        var tooMany = new KingdomLandZoneSettlement[KingdomLandControlPolicy.MaximumNearbyLocatables + 1];
        for (int i = 0; i < tooMany.Length; i++)
            tooMany[i] = new KingdomLandZoneSettlement("fort-" + i, true, true, "kingdom-a", i);
        ExpectZone("candidate-budget-exhaustion-is-unknown", KingdomLandZoneKind.Unknown, true, true, tooMany);
        ExpectZone("empty-area-is-unassigned", KingdomLandZoneKind.NoNearbyFortification, true, true);

        ExpectRouteBlock("route-zone-explicit-directional-close", true,
            Zone(true, true, nearestB), "kingdom-a", false, true);
        ExpectRouteBlock("route-zone-default-open", false,
            Zone(true, true, nearestB), "kingdom-a", false, false);
        ExpectRouteBlock("route-zone-reverse-direction-stays-open", false,
            Zone(true, true, new KingdomLandZoneSettlement("fort-a", true, true, "kingdom-a", 1f)),
            "kingdom-b", false, false);
        ExpectRouteBlock("war-with-zone-kingdom-passes", false,
            Zone(true, true, nearestB), "kingdom-a", true, true);
        ExpectRouteBlock("same-kingdom-zone-passes", false,
            Zone(true, true, nearestB), "kingdom-b", false, true);
        ExpectRouteBlock("sea-cannot-inherit-land-closure", false,
            Zone(false, true, nearestB), "kingdom-a", false, true);
        ExpectRouteBlock("ambiguous-zone-fails-open", false,
            Zone(true, true,
                new KingdomLandZoneSettlement("fort-a", true, true, "kingdom-a", 1f),
                new KingdomLandZoneSettlement("fort-b", true, true, "kingdom-b", 1f)), "kingdom-a", false, true);
        ExpectRouteBlock("unowned-zone-fails-open", false,
            Zone(true, true, new KingdomLandZoneSettlement("unowned", true, false, null, 1f)),
            "kingdom-a", false, true);

        int observerFailures = StoredRouteObserverTests.Run();
        if (_failures != 0 || observerFailures != 0)
        {
            Console.WriteLine("FAIL KingdomBorderPolicy=14 land-zone/route policy cases=31 stored-route observer cases=37 policyFailures=" +
                _failures + " observerFailures=" + observerFailures);
            return 1;
        }
        Console.WriteLine("PASS KingdomBorderPolicy cases=14; land-zone/route policy cases=31; stored-route observer policy cases=37");
        Console.WriteLine("default=open; only explicit directional closure classifies a foreign kingdom as closed");
        Console.WriteLine("observer reads at most eight stored waypoints; no candidate or movement mutation; runtime native path behavior is not tested here");
        return 0;
    }
}
