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

    private static DynastyEpisodeDescriptor Row(
        int index, string id, string branch, string actor,
        string actorName, string kind, double hours, string utc)
    {
        return new DynastyEpisodeDescriptor
        {
            SourceIndex = index,
            Id = id,
            BranchId = branch,
            ActorId = actor,
            ActorName = actorName,
            Kind = kind,
            CampaignHours = hours,
            ObservedUtc = utc
        };
    }

    private static void Main()
    {
        List<DynastyEpisodeDescriptor> rows = new List<DynastyEpisodeDescriptor>
        {
            Row(0, "opened", "branch-a", "heir", "Heir", "IncidentOpened", 10, "2026-01-01T00:00:00Z"),
            Row(1, "choice-first", "branch-a", "heir", "Heir", "IncidentChoice", 20, "2026-01-01T00:00:00Z"),
            Row(2, "choice-later-utc", "branch-a", "heir", "Heir", "IncidentChoice", 20, "2026-01-02T00:00:00Z"),
            Row(3, "former", "branch-a", "former", "Former", "IncidentChoice", 30, "2026-01-03T00:00:00Z"),
            Row(4, "other-branch", "branch-b", "heir", "Heir", "IncidentChoice", 40, "2026-01-04T00:00:00Z"),
            Row(5, "future", "branch-a", "heir", "Heir", "IncidentChoice", 101, "2026-01-05T00:00:00Z")
        };

        DynastyEpisodeSelection latest = DynastyEpisodeRetrievalPolicy
            .SelectLatestActorEpisode(rows, "branch-a", "heir", 100);
        Check(latest.Episode.Id == "choice-first", "actor latest tie remains first inserted");
        Check(latest.VisibleCount == 3, "actor scope count unchanged");
        Check(latest.IsPersonalMemory, "actor retrieval remains personal");

        DynastyEpisodeSelection choice = DynastyEpisodeRetrievalPolicy
            .SelectLatestActorChoice(rows, "branch-a", "heir", 100);
        Check(choice.Episode.Id == "choice-later-utc", "choice tie remains latest UTC");
        Check(choice.VisibleCount == 2, "choice excludes former actor other branch and future");

        DynastyEpisodeSelection formerOnly = DynastyEpisodeRetrievalPolicy
            .SelectLatestActorEpisode(
                new List<DynastyEpisodeDescriptor> { rows[3] },
                "branch-a", "heir", 100);
        Check(formerOnly.Episode == null, "former actor invisible to heir personal retrieval");

        DynastyEpisodeSelection history = DynastyEpisodeRetrievalPolicy
            .SelectLatestBranchHistory(rows, "branch-a", 100);
        Check(history.Episode == null, "current production kinds excluded from history");
        Check(history.VisibleCount == 0, "empty history does not fall back to personal rows");
        Check(!history.IsPersonalMemory, "history is non-personal");
        Check(history.ContextLabel == "branch-history", "history label explicit");
        Check(!DynastyEpisodeRetrievalPolicy.IsBranchHistoryKind("IncidentOpened"), "opened excluded");
        Check(!DynastyEpisodeRetrievalPolicy.IsBranchHistoryKind("IncidentChoice"), "choice excluded");
        Check(!DynastyEpisodeRetrievalPolicy.IsBranchHistoryKind("Unknown"), "closed allowlist rejects unknown");

        const string structural = "TestOnlyStructuralHistory";
        List<DynastyEpisodeDescriptor> structuralRows = new List<DynastyEpisodeDescriptor>
        {
            Row(0, "eligible", "branch-a", "former", "Former", structural, 50, "2026-02-01T00:00:00Z"),
            Row(1, "wrong-branch", "branch-b", "other", "Other", structural, 60, "2026-02-02T00:00:00Z"),
            Row(2, "future", "branch-a", "future", "Future", structural, 101, "2026-02-03T00:00:00Z")
        };
        DynastyEpisodeSelection structuralHistory = DynastyEpisodeRetrievalPolicy
            .SelectLatestBranchHistoryForKinds(
                structuralRows, "branch-a", 100,
                new HashSet<string>(StringComparer.Ordinal) { structural });
        Check(structuralHistory.Episode.Id == "eligible", "test structural kind crosses actors");
        Check(structuralHistory.Episode.ActorId == "former", "actor id provenance preserved");
        Check(structuralHistory.Episode.ActorName == "Former", "actor name provenance preserved");
        Check(structuralHistory.VisibleCount == 1, "structural branch/time filters enforced");
        Check(!structuralHistory.IsPersonalMemory, "structural result non-personal");
        Check(structuralHistory.ContextLabel == "branch-history", "structural label historical");

        Console.WriteLine("PASS Phase 7C retrieval-scope checks=" + _checks);
        Console.WriteLine("PASS actor preservation, history isolation, provenance and no mutation");
    }
}

