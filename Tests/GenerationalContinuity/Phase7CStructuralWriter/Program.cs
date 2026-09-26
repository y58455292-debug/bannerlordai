using System;
using System.Collections.Generic;
using ClanAI;

internal static class Program
{
    private static int _checks;

    private static void Check(bool condition, string name)
    {
        _checks++;
        if (!condition)
            throw new Exception("FAIL " + name);
    }

    private static bool Create(
        ISet<string> existing,
        int ordinal,
        out DynastyStructuralEpisodeDraft draft,
        string branch = "branch-a",
        string actor = "observer-a",
        string kingdom = "kingdom-a")
    {
        return DynastyStructuralEpisodePolicy.TryCreateRulingClanChanged(
            branch,
            actor,
            "Observer A",
            kingdom,
            "Kingdom A",
            "old-clan",
            "new-clan",
            "old-ruler",
            "new-ruler",
            ordinal,
            100.0,
            "2026-09-25T00:00:00.0000000Z",
            existing,
            out draft);
    }

    private static void Main()
    {
        HashSet<string> existing = new HashSet<string>(StringComparer.Ordinal);
        DynastyStructuralEpisodeDraft first;
        Check(Create(existing, 1, out first), "accepted transition creates draft");
        Check(first.Kind == "KingdomRulingClanChanged", "exact production kind");
        Check(first.BranchId == "branch-a", "branch preserved");
        Check(first.ActorId == "observer-a" && first.ActorName == "Observer A", "observer provenance preserved");
        Check(first.ContextId == "kingdom:kingdom-a" && first.ContextName == "Kingdom A", "context preserved");
        Check(first.Source == "CampaignEvents.RulingClanChanged", "native source exact");
        Check(first.OptionIndex == -1 && first.OptionText == "", "non-choice fields exact");
        foreach (string token in new[] {
            "kingdomId=kingdom-a", "oldRulingClanId=old-clan",
            "newRulingClanId=new-clan", "oldRulerHeroId=old-ruler",
            "newRulerHeroId=new-ruler", "successionOrdinal=1" })
        {
            Check(first.Detail.Contains(token), "detail contains " + token);
        }

        existing.Add(first.SemanticIdentity);
        DynastyStructuralEpisodeDraft duplicate;
        Check(!Create(existing, 1, out duplicate), "same semantic transition deduplicated");
        DynastyStructuralEpisodeDraft later;
        Check(Create(existing, 2, out later), "higher ordinal remains recordable");
        Check(later.SemanticIdentity != first.SemanticIdentity, "ordinal participates in identity");

        Check(!DynastyStructuralEpisodePolicy.IsAcceptedNativeTransition("new-clan", "new-clan"), "no-change callback rejected");
        Check(DynastyStructuralEpisodePolicy.IsAcceptedNativeTransition("old-clan", "new-clan"), "real transition accepted");

        DynastyStructuralEpisodeDraft missing;
        Check(!Create(existing, 3, out missing, kingdom: ""), "missing kingdom rejected");
        Check(!Create(existing, 3, out missing, actor: ""), "missing actor rejected");

        string restoredIdentity = DynastyStructuralEpisodePolicy.IdentityFromStoredRow(
            first.BranchId, first.Kind, first.Detail);
        Check(restoredIdentity == first.SemanticIdentity, "restored D2 detail reconstructs identity");

        List<DynastyEpisodeDescriptor> rows = new List<DynastyEpisodeDescriptor>
        {
            new DynastyEpisodeDescriptor {
                SourceIndex = 0, Id = "structural", BranchId = first.BranchId,
                ActorId = first.ActorId, ActorName = first.ActorName,
                Kind = first.Kind, CampaignHours = first.CampaignHours,
                ObservedUtc = first.ObservedUtc },
            new DynastyEpisodeDescriptor {
                SourceIndex = 1, Id = "choice", BranchId = first.BranchId,
                ActorId = "heir", ActorName = "Heir",
                Kind = "IncidentChoice", CampaignHours = 99,
                ObservedUtc = first.ObservedUtc },
            new DynastyEpisodeDescriptor {
                SourceIndex = 2, Id = "opened", BranchId = first.BranchId,
                ActorId = "heir", ActorName = "Heir",
                Kind = "IncidentOpened", CampaignHours = 98,
                ObservedUtc = first.ObservedUtc }
        };

        DynastyEpisodeSelection history = DynastyEpisodeRetrievalPolicy
            .SelectLatestBranchHistory(rows, first.BranchId, 100);
        Check(history.Episode.Id == "structural", "structural history visible across actor ids");
        Check(history.Episode.ActorId == "observer-a", "history preserves original actor provenance");
        Check(!history.IsPersonalMemory, "history explicitly non-personal");

        DynastyEpisodeSelection personal = DynastyEpisodeRetrievalPolicy
            .SelectLatestActorEpisode(rows, first.BranchId, "observer-a", 100);
        Check(personal.Episode == null, "structural row excluded from personal retrieval");
        Check(!DynastyEpisodeRetrievalPolicy.IsBranchHistoryKind("IncidentChoice"), "choice remains excluded from history");
        Check(!DynastyEpisodeRetrievalPolicy.IsBranchHistoryKind("IncidentOpened"), "opened remains excluded from history");

        Check(DynastyStructuralEpisodePolicy.IsAcceptedProductionKind("IncidentOpened"), "D2 opened remains accepted");
        Check(DynastyStructuralEpisodePolicy.IsAcceptedProductionKind("IncidentChoice"), "D2 choice remains accepted");
        Check(DynastyStructuralEpisodePolicy.IsAcceptedProductionKind("KingdomRulingClanChanged"), "D2 structural kind accepted");
        Check(!DynastyStructuralEpisodePolicy.IsAcceptedProductionKind("Unknown"), "unknown kind rejected");

        Console.WriteLine("PASS Phase 7C-I3 structural-writer checks=" + _checks);
        Console.WriteLine("PASS native authority, deduplication, provenance, retrieval isolation and D2 kind compatibility");
    }
}

