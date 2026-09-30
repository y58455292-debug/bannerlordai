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

        if (_failures != 0)
        {
            Console.WriteLine("FAIL KingdomBorderPolicy cases=14 failures=" + _failures);
            return 1;
        }
        Console.WriteLine("PASS KingdomBorderPolicy cases=14");
        Console.WriteLine("default=open; only explicit directional closure classifies a foreign kingdom as closed");
        Console.WriteLine("settlement owner is read live; arbitrary land routes and open water remain unclassified");
        return 0;
    }
}
