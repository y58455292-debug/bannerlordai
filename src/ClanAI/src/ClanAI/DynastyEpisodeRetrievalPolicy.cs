using System;
using System.Collections.Generic;

namespace ClanAI
{
    internal sealed class DynastyEpisodeDescriptor
    {
        public int SourceIndex;
        public string Id;
        public string BranchId;
        public double CampaignHours;
        public string ObservedUtc;
        public string ActorId;
        public string ActorName;
        public string Kind;
    }

    internal sealed class DynastyEpisodeSelection
    {
        public DynastyEpisodeDescriptor Episode;
        public int VisibleCount;
        public bool IsPersonalMemory;
        public string ContextLabel;
    }

    internal static class DynastyEpisodeRetrievalPolicy
    {
        private const double TimeEpsilon = 0.000001d;

        internal static DynastyEpisodeSelection SelectLatestActorEpisode(
            IList<DynastyEpisodeDescriptor> episodes,
            string branchId,
            string actorId,
            double now)
        {
            return SelectActor(
                episodes,
                branchId,
                actorId,
                now,
                false);
        }

        internal static DynastyEpisodeSelection SelectLatestActorChoice(
            IList<DynastyEpisodeDescriptor> episodes,
            string branchId,
            string actorId,
            double now)
        {
            return SelectActor(
                episodes,
                branchId,
                actorId,
                now,
                true);
        }

        internal static DynastyEpisodeSelection SelectLatestBranchHistory(
            IList<DynastyEpisodeDescriptor> episodes,
            string branchId,
            double now)
        {
            return SelectBranchHistory(
                episodes,
                branchId,
                now,
                IsBranchHistoryKind);
        }

        internal static bool IsBranchHistoryKind(string kind)
        {
            switch (kind)
            {
                case "IncidentOpened":
                case "IncidentChoice":
                default:
                    return false;
            }
        }

        // Inactive deterministic seam. Production retrieval always uses the
        // closed IsBranchHistoryKind allowlist above.
        internal static DynastyEpisodeSelection SelectLatestBranchHistoryForKinds(
            IList<DynastyEpisodeDescriptor> episodes,
            string branchId,
            double now,
            ISet<string> allowedKinds)
        {
            return SelectBranchHistory(
                episodes,
                branchId,
                now,
                delegate(string kind)
                {
                    return allowedKinds != null &&
                        allowedKinds.Contains(kind);
                });
        }

        private static DynastyEpisodeSelection SelectActor(
            IList<DynastyEpisodeDescriptor> episodes,
            string branchId,
            string actorId,
            double now,
            bool choicesOnly)
        {
            DynastyEpisodeDescriptor latest = null;
            int visibleCount = 0;

            for (int i = 0; i < episodes.Count; i++)
            {
                DynastyEpisodeDescriptor e = episodes[i];

                if (choicesOnly && e.Kind != "IncidentChoice")
                    continue;

                if (!string.Equals(e.ActorId, actorId, StringComparison.Ordinal) ||
                    !string.Equals(e.BranchId, branchId, StringComparison.Ordinal) ||
                    e.CampaignHours > now + TimeEpsilon)
                {
                    continue;
                }

                visibleCount++;

                if (latest == null ||
                    e.CampaignHours > latest.CampaignHours ||
                    (choicesOnly &&
                     Math.Abs(e.CampaignHours - latest.CampaignHours) <= TimeEpsilon &&
                     string.CompareOrdinal(e.ObservedUtc, latest.ObservedUtc) > 0))
                {
                    latest = e;
                }
            }

            return new DynastyEpisodeSelection
            {
                Episode = latest,
                VisibleCount = visibleCount,
                IsPersonalMemory = true,
                ContextLabel = "actor-memory"
            };
        }

        private static DynastyEpisodeSelection SelectBranchHistory(
            IList<DynastyEpisodeDescriptor> episodes,
            string branchId,
            double now,
            Func<string, bool> kindAllowed)
        {
            DynastyEpisodeDescriptor latest = null;
            int visibleCount = 0;

            for (int i = 0; i < episodes.Count; i++)
            {
                DynastyEpisodeDescriptor e = episodes[i];

                if (!string.Equals(e.BranchId, branchId, StringComparison.Ordinal) ||
                    e.CampaignHours > now + TimeEpsilon ||
                    !kindAllowed(e.Kind))
                {
                    continue;
                }

                visibleCount++;

                if (latest == null ||
                    e.CampaignHours > latest.CampaignHours ||
                    (Math.Abs(e.CampaignHours - latest.CampaignHours) <= TimeEpsilon &&
                     string.CompareOrdinal(e.ObservedUtc, latest.ObservedUtc) > 0))
                {
                    latest = e;
                }
            }

            return new DynastyEpisodeSelection
            {
                Episode = latest,
                VisibleCount = visibleCount,
                IsPersonalMemory = false,
                ContextLabel = "branch-history"
            };
        }
    }
}

