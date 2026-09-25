using System;
using ClanAI;

internal static class Program
{
    private static int checks;
    private static void Check(bool ok, string name)
    {
        checks++;
        if (!ok) throw new Exception("FAIL " + name);
    }
    private static void Main()
    {
        Check(Phase4ARecreationLinkPolicy.SameHero("hero_1", "hero_1"), "same stable hero");
        Check(!Phase4ARecreationLinkPolicy.SameHero("hero_1", "hero_2"), "same clan is insufficient");
        Check(!Phase4ARecreationLinkPolicy.SameHero("Hero_1", "hero_1"), "ordinal identity");
        Check(!Phase4ARecreationLinkPolicy.SameHero(null, null), "missing identities");
        Check(!Phase4ARecreationLinkPolicy.SameHero(" ", " "), "blank identities");
        Check(Phase4ARecreationLinkPolicy.CanLink("h", "h", 10, 20, true, true), "defeat to same hero new party");
        Check(!Phase4ARecreationLinkPolicy.CanLink("h", "other", 10, 20, true, true), "unrelated hero");
        Check(!Phase4ARecreationLinkPolicy.CanLink("h", "h", 10, 20, false, true), "surviving same party is not recreation");
        Check(!Phase4ARecreationLinkPolicy.CanLink("h", "h", 10, 20, true, false), "old party still exists");
        Check(!Phase4ARecreationLinkPolicy.CanLink("h", "h", 20, 10, true, true), "creation before defeat");
        Check(Phase4ARecreationLinkPolicy.CanLink("h", "h", 10, 178, true, true), "inclusive 168 hour bound");
        Check(!Phase4ARecreationLinkPolicy.CanLink("h", "h", 10, 178.001, true, true), "expired defeat");
        foreach (double invalid in new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity })
        {
            Check(!Phase4ARecreationLinkPolicy.InWindow(invalid, 10), "invalid start");
            Check(!Phase4ARecreationLinkPolicy.InWindow(10, invalid), "invalid observation hour");
        }
        Check(Phase4ARecreationLinkPolicy.Classify(true, true, true, true, 29, 1) ==
            Phase4ARecreationSource.PostDefeatNativeRecreationInitialTroopsSupported, "linked regular troops");
        Check(Phase4ARecreationLinkPolicy.Classify(true, true, true, true, 0, 0) ==
            Phase4ARecreationSource.PostDefeatNativeRecreationZeroTroops, "zero troops");
        Check(Phase4ARecreationLinkPolicy.Classify(true, true, true, true, 1, 1) ==
            Phase4ARecreationSource.PostDefeatNativeRecreationZeroTroops, "leader alone is not supplied regular troops");
        Check(Phase4ARecreationLinkPolicy.Classify(true, true, false, true, 29, 1) ==
            Phase4ARecreationSource.GenericNativePartyCreation, "generic nonzero creation never post-defeat");
        Check(Phase4ARecreationLinkPolicy.Classify(true, false, false, true, 29, 1) ==
            Phase4ARecreationSource.UnknownOrUnlinkedCreation, "unidentified creation");
        Check(Phase4ARecreationLinkPolicy.Classify(false, true, true, true, 29, 1) ==
            Phase4ARecreationSource.UnknownOrUnlinkedCreation, "duplicate or non-creation notification");
        Check(Phase4ARecreationLinkPolicy.Classify(true, true, true, false, 29, 1) ==
            Phase4ARecreationSource.PostDefeatRecreationObservedButPreSettlementStateIncomplete, "inside settlement at creation");
        Check(Phase4ARecreationLinkPolicy.Classify(true, true, true, true, -1, 0) ==
            Phase4ARecreationSource.PostDefeatRecreationObservedButPreSettlementStateIncomplete, "unavailable initial roster");
        Check(Phase4ARecreationLinkPolicy.Classify(true, true, true, true, 1, 2) ==
            Phase4ARecreationSource.PostDefeatRecreationObservedButPreSettlementStateIncomplete, "invalid hero count");
        Check(Phase4ARecreationLinkPolicy.CompleteFirstVisit(true, true, false, false, "h", "h", 20, 25), "complete first visit");
        Check(!Phase4ARecreationLinkPolicy.CompleteFirstVisit(false, true, false, false, "h", "h", 20, 25), "first visit cannot link generic creation");
        Check(!Phase4ARecreationLinkPolicy.CompleteFirstVisit(true, false, false, false, "h", "h", 20, 25), "initial settlement contamination");
        Check(!Phase4ARecreationLinkPolicy.CompleteFirstVisit(true, true, true, false, "h", "h", 20, 25), "missed pre-entry");
        Check(!Phase4ARecreationLinkPolicy.CompleteFirstVisit(true, true, false, true, "h", "h", 20, 25), "observer fault prevents complete claim");
        Check(!Phase4ARecreationLinkPolicy.CompleteFirstVisit(true, true, false, false, "h", "other", 20, 25), "leader identity changed");
        Check(!Phase4ARecreationLinkPolicy.CompleteFirstVisit(true, true, false, false, "h", "h", 25, 20), "time reversal");
        Check(!Phase4ARecreationLinkPolicy.CompleteFirstVisit(true, true, false, false, "h", "h", 0, 169), "follow-up beyond bound");
        Console.WriteLine("PASS Phase 4A same-hero recreation policy checks=" + checks);
    }
}
