using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using TaleWorlds.CampaignSystem;

namespace ClanAI
{
    public static class DynastyBranchEpisodeMemory
    {
        private const int Capacity = 2000;

        private sealed class Episode
        {
            public string Id;
            public string BranchId;
            public double CampaignHours;
            public string ObservedUtc;
            public string ActorId;
            public string ActorName;
            public string Kind;
            public string ContextId;
            public string ContextName;
            public string Source;
            public string Detail;
            public int OptionIndex;
            public string OptionText;
        }

        private static readonly List<Episode> Episodes =
            new List<Episode>();

        private static readonly HashSet<string> SessionFingerprints =
            new HashSet<string>(StringComparer.Ordinal);

        private static bool _prepared;
        private static bool _ready;
        private static bool _restored;
        private static int _rejected;
        private static int _duplicates;
        private static int _recorded;
        private static int _retrievals;
        private static int _choiceRecorded;
        private static int _choiceRetrievals;
        private static int _branchHistoryRetrievals;
        private static int _structuralRecorded;

        public static void SyncData(IDataStore dataStore)
        {
            List<string> rows =
                dataStore.IsSaving
                    ? ExportRows()
                    : new List<string>();

            bool found =
                dataStore.SyncData(
                    "ClanAI_DynastyBranchEpisodes_v1",
                    ref rows);

            if (dataStore.IsLoading)
            {
                Episodes.Clear();
                SessionFingerprints.Clear();
                _rejected = 0;
                _duplicates = 0;
                ImportRows(found ? rows : null);
                _restored = found;
                _prepared = true;
                _ready = false;
            }

            if (dataStore.IsSaving)
            {
                ClanAIPostVanilla.WriteExternalLog(
                    "DYNASTY_BRANCH_EPISODE_SAVE" +
                    " records=" + Episodes.Count +
                    " rejected=" + _rejected +
                    " duplicates=" + _duplicates);
            }
        }

        public static void BeginSession()
        {
            if (!_prepared)
            {
                Episodes.Clear();
                _restored = false;
                _rejected = 0;
                _duplicates = 0;
            }

            _prepared = false;
            _ready = true;
            SessionFingerprints.Clear();
            _recorded = 0;
            _retrievals = 0;
            _choiceRecorded = 0;
            _choiceRetrievals = 0;
            _branchHistoryRetrievals = 0;
            _structuralRecorded = 0;

            ClanAIPostVanilla.WriteExternalLog(
                "DYNASTY_BRANCH_EPISODE_SESSION_READY" +
                " records=" + Episodes.Count +
                " restored=" + _restored +
                " branchId=" + Clean(DynastyMindSeed.BranchId) +
                " scoreMutation=False");
        }

        public static string RecordIncidentOpened(
            string title,
            int optionCount,
            string source)
        {
            try
            {
                if (!_ready)
                    return "not_ready";

                Hero actor = Hero.MainHero;
                if (actor == null)
                    return "no_actor";

                double hours;
                try
                {
                    hours = CampaignTime.Now.ToHours;
                }
                catch
                {
                    return "time_unavailable";
                }

                string contextName =
                    string.IsNullOrWhiteSpace(title)
                        ? "<untitled-incident>"
                        : title.Trim();

                string contextId =
                    "incident:" + Slug(contextName);

                string fingerprint =
                    Safe(actor.StringId) + "|" +
                    contextId + "|" +
                    hours.ToString("0.0000", CultureInfo.InvariantCulture);

                if (SessionFingerprints.Contains(fingerprint))
                {
                    _duplicates++;
                    return "duplicate";
                }

                if (Episodes.Count >= Capacity)
                    return "capacity";

                SessionFingerprints.Add(fingerprint);

                Episode episode = new Episode();
                episode.Id = Guid.NewGuid().ToString("N");
                episode.BranchId = DynastyMindSeed.BranchId;
                episode.CampaignHours = hours;
                episode.ObservedUtc = DateTime.UtcNow.ToString("O");
                episode.ActorId = actor.StringId ?? "";
                episode.ActorName = actor.Name == null
                    ? "<unnamed>"
                    : actor.Name.ToString();
                episode.Kind = "IncidentOpened";
                episode.ContextId = contextId;
                episode.ContextName = contextName;
                episode.Source = source ?? "<unknown>";
                episode.Detail =
                    "optionCount=" +
                    optionCount.ToString(CultureInfo.InvariantCulture);
                episode.OptionIndex = -1;
                episode.OptionText = "";

                Episodes.Add(episode);
                _recorded++;

                ClanAIPostVanilla.WriteExternalLog(
                    "DYNASTY_BRANCH_EPISODE_RECORDED" +
                    " id=" + episode.Id +
                    " branchId=" + Clean(episode.BranchId) +
                    " actor=" + Clean(episode.ActorName) +
                    " actorId=" + Clean(episode.ActorId) +
                    " kind=" + episode.Kind +
                    " contextId=" + Clean(episode.ContextId) +
                    " context=" + Clean(episode.ContextName) +
                    " campaignHours=" +
                    episode.CampaignHours.ToString(
                        "R",
                        CultureInfo.InvariantCulture) +
                    " source=" + Clean(episode.Source) +
                    " detail=" + Clean(episode.Detail) +
                    " scoreMutation=False");

                return episode.Id;
            }
            catch (Exception ex)
            {
                ClanAIPostVanilla.WriteExternalLog(
                    "DYNASTY_BRANCH_EPISODE_RECORD_FAILED" +
                    " type=" + ex.GetType().Name +
                    " message=" + Clean(ex.Message));

                return "failed_" + ex.GetType().Name;
            }
        }

        public static string RecordIncidentChoice(
            string title,
            int optionIndex,
            string optionText,
            string source)
        {
            try
            {
                if (!_ready)
                    return "not_ready";

                Hero actor = Hero.MainHero;
                if (actor == null)
                    return "no_actor";

                double hours;
                try
                {
                    hours = CampaignTime.Now.ToHours;
                }
                catch
                {
                    return "time_unavailable";
                }

                string contextName =
                    string.IsNullOrWhiteSpace(title)
                        ? "<untitled-incident>"
                        : title.Trim();

                string contextId =
                    "incident:" + Slug(contextName);

                string fingerprint =
                    "IncidentChoice|" +
                    Safe(actor.StringId) + "|" +
                    contextId + "|" +
                    optionIndex.ToString(
                        CultureInfo.InvariantCulture) + "|" +
                    hours.ToString(
                        "0.0000",
                        CultureInfo.InvariantCulture);

                if (SessionFingerprints.Contains(fingerprint))
                {
                    _duplicates++;
                    return "duplicate";
                }

                if (Episodes.Count >= Capacity)
                    return "capacity";

                SessionFingerprints.Add(fingerprint);

                Episode episode = new Episode();
                episode.Id = Guid.NewGuid().ToString("N");
                episode.BranchId = DynastyMindSeed.BranchId;
                episode.CampaignHours = hours;
                episode.ObservedUtc = DateTime.UtcNow.ToString("O");
                episode.ActorId = actor.StringId ?? "";
                episode.ActorName = actor.Name == null
                    ? "<unnamed>"
                    : actor.Name.ToString();
                episode.Kind = "IncidentChoice";
                episode.ContextId = contextId;
                episode.ContextName = contextName;
                episode.Source = source ?? "<unknown>";
                episode.Detail = "executed=True";
                episode.OptionIndex = optionIndex;
                episode.OptionText = optionText ?? "";

                Episodes.Add(episode);
                _recorded++;
                _choiceRecorded++;

                ClanAIPostVanilla.WriteExternalLog(
                    "DYNASTY_BRANCH_CHOICE_RECORDED" +
                    " id=" + episode.Id +
                    " branchId=" + Clean(episode.BranchId) +
                    " actor=" + Clean(episode.ActorName) +
                    " actorId=" + Clean(episode.ActorId) +
                    " contextId=" + Clean(episode.ContextId) +
                    " context=" + Clean(episode.ContextName) +
                    " optionIndex=" + episode.OptionIndex +
                    " optionText=" + Clean(episode.OptionText) +
                    " campaignHours=" +
                    episode.CampaignHours.ToString(
                        "R",
                        CultureInfo.InvariantCulture) +
                    " source=" + Clean(episode.Source) +
                    " scoreMutation=False");

                return episode.Id;
            }
            catch (Exception ex)
            {
                ClanAIPostVanilla.WriteExternalLog(
                    "DYNASTY_BRANCH_CHOICE_RECORD_FAILED" +
                    " type=" + ex.GetType().Name +
                    " message=" + Clean(ex.Message));

                return "failed_" + ex.GetType().Name;
            }
        }

        internal static string RecordKingdomRulingClanChanged(
            string kingdomId,
            string kingdomName,
            string oldRulingClanId,
            string newRulingClanId,
            string oldRulerHeroId,
            string newRulerHeroId,
            int successionOrdinal,
            double campaignHours)
        {
            try
            {
                if (!_ready)
                    return "not_ready";

                Hero actor = Hero.MainHero;
                if (actor == null ||
                    string.IsNullOrWhiteSpace(actor.StringId))
                {
                    return "no_actor";
                }

                if (Episodes.Count >= Capacity)
                    return "capacity";

                HashSet<string> existing =
                    new HashSet<string>(StringComparer.Ordinal);

                for (int i = 0; i < Episodes.Count; i++)
                {
                    Episode stored = Episodes[i];
                    string identity =
                        DynastyStructuralEpisodePolicy.IdentityFromStoredRow(
                            stored.BranchId,
                            stored.Kind,
                            stored.Detail);

                    if (!string.IsNullOrEmpty(identity))
                        existing.Add(identity);
                }

                DynastyStructuralEpisodeDraft draft;
                bool created =
                    DynastyStructuralEpisodePolicy.TryCreateRulingClanChanged(
                        DynastyMindSeed.BranchId,
                        actor.StringId,
                        actor.Name == null
                            ? "<unnamed>"
                            : actor.Name.ToString(),
                        kingdomId,
                        kingdomName,
                        oldRulingClanId,
                        newRulingClanId,
                        oldRulerHeroId,
                        newRulerHeroId,
                        successionOrdinal,
                        campaignHours,
                        DateTime.UtcNow.ToString("O"),
                        existing,
                        out draft);

                if (!created)
                {
                    _duplicates++;
                    return "duplicate_or_invalid";
                }

                Episode episode = new Episode
                {
                    Id = Guid.NewGuid().ToString("N"),
                    BranchId = draft.BranchId,
                    CampaignHours = draft.CampaignHours,
                    ObservedUtc = draft.ObservedUtc,
                    ActorId = draft.ActorId,
                    ActorName = draft.ActorName,
                    Kind = draft.Kind,
                    ContextId = draft.ContextId,
                    ContextName = draft.ContextName,
                    Source = draft.Source,
                    Detail = draft.Detail,
                    OptionIndex = draft.OptionIndex,
                    OptionText = draft.OptionText
                };

                Episodes.Add(episode);
                SessionFingerprints.Add(draft.SemanticIdentity);
                _recorded++;
                _structuralRecorded++;

                ClanAIPostVanilla.WriteExternalLog(
                    "DYNASTY_BRANCH_STRUCTURAL_RECORDED" +
                    " id=" + episode.Id +
                    " branchId=" + Clean(episode.BranchId) +
                    " observer=" + Clean(episode.ActorName) +
                    " observerId=" + Clean(episode.ActorId) +
                    " kind=" + episode.Kind +
                    " contextId=" + Clean(episode.ContextId) +
                    " source=" + Clean(episode.Source) +
                    " detail=" + Clean(episode.Detail) +
                    " campaignHours=" + episode.CampaignHours.ToString(
                        "R", CultureInfo.InvariantCulture) +
                    " personalMemory=False mutation=False" +
                    " scoreMutation=False");

                return episode.Id;
            }
            catch (Exception ex)
            {
                ClanAIPostVanilla.WriteExternalLog(
                    "DYNASTY_BRANCH_STRUCTURAL_RECORD_FAILED" +
                    " type=" + ex.GetType().Name +
                    " message=" + Clean(ex.Message) +
                    " mutation=False");

                return "failed_" + ex.GetType().Name;
            }
        }

        public static string BuildLatestRetrievalReceipt(
            string retrievalContext)
        {
            try
            {
                if (!_ready)
                    return "";

                Hero actor = Hero.MainHero;
                if (actor == null)
                    return "";

                double now;
                try
                {
                    now = CampaignTime.Now.ToHours;
                }
                catch
                {
                    return "";
                }

                string actorId = actor.StringId ?? "";
                string branchId = DynastyMindSeed.BranchId;

                DynastyEpisodeSelection selection =
                    DynastyEpisodeRetrievalPolicy.SelectLatestActorEpisode(
                        BuildDescriptors(), branchId, actorId, now);

                Episode latest = FindEpisode(selection.Episode);
                int visibleCount = selection.VisibleCount;

                if (latest == null)
                    return "";

                _retrievals++;

                StringBuilder sb = new StringBuilder();
                sb.Append("{");
                sb.Append("\"schema\":\"BannerlordAI.DynastyBranchEpisodeRetrieval.v1\",");
                sb.Append("\"mode\":\"observe\",");
                sb.Append("\"actor\":");
                sb.Append(Json(actor.Name.ToString()));
                sb.Append(",\"actorId\":");
                sb.Append(Json(actorId));
                sb.Append(",\"branchId\":");
                sb.Append(Json(branchId));
                sb.Append(",\"retrievalContext\":");
                sb.Append(Json(retrievalContext));
                sb.Append(",\"currentCampaignHours\":");
                sb.Append(now.ToString("R", CultureInfo.InvariantCulture));
                sb.Append(",\"visibleCount\":");
                sb.Append(visibleCount.ToString(CultureInfo.InvariantCulture));
                sb.Append(",\"latest\":{");
                sb.Append("\"id\":");
                sb.Append(Json(latest.Id));
                sb.Append(",\"kind\":");
                sb.Append(Json(latest.Kind));
                sb.Append(",\"knownByHours\":");
                sb.Append(
                    latest.CampaignHours.ToString(
                        "R",
                        CultureInfo.InvariantCulture));
                sb.Append(",\"observedUtc\":");
                sb.Append(Json(latest.ObservedUtc));
                sb.Append(",\"contextId\":");
                sb.Append(Json(latest.ContextId));
                sb.Append(",\"contextName\":");
                sb.Append(Json(latest.ContextName));
                sb.Append(",\"source\":");
                sb.Append(Json(latest.Source));
                sb.Append(",\"detail\":");
                sb.Append(Json(latest.Detail));
                sb.Append(",\"actorValid\":true");
                sb.Append(",\"branchValid\":true");
                sb.Append(",\"futureLeak\":false");
                sb.Append("},\"scoreMutation\":false}");

                string receipt = sb.ToString();

                ClanAIPostVanilla.WriteExternalLog(
                    "DYNASTY_BRANCH_EPISODE_RETRIEVED" +
                    " actor=" + Clean(actor.Name.ToString()) +
                    " actorId=" + Clean(actorId) +
                    " branchId=" + Clean(branchId) +
                    " visibleCount=" + visibleCount +
                    " latestKind=" + Clean(latest.Kind) +
                    " contextId=" + Clean(latest.ContextId) +
                    " knownByHours=" +
                    latest.CampaignHours.ToString(
                        "R",
                        CultureInfo.InvariantCulture) +
                    " retrievalContext=" +
                    Clean(retrievalContext) +
                    " futureLeak=False scoreMutation=False");

                return receipt;
            }
            catch (Exception ex)
            {
                ClanAIPostVanilla.WriteExternalLog(
                    "DYNASTY_BRANCH_EPISODE_RETRIEVE_FAILED" +
                    " type=" + ex.GetType().Name +
                    " message=" + Clean(ex.Message));

                return "";
            }
        }

        public static string BuildLatestChoiceRetrievalReceipt(
            string retrievalContext)
        {
            try
            {
                if (!_ready)
                    return "";

                Hero actor = Hero.MainHero;
                if (actor == null)
                    return "";

                double now;
                try
                {
                    now = CampaignTime.Now.ToHours;
                }
                catch
                {
                    return "";
                }

                string actorId = actor.StringId ?? "";
                string branchId = DynastyMindSeed.BranchId;
                DynastyEpisodeSelection selection =
                    DynastyEpisodeRetrievalPolicy.SelectLatestActorChoice(
                        BuildDescriptors(), branchId, actorId, now);

                Episode latest = FindEpisode(selection.Episode);
                int visibleCount = selection.VisibleCount;

                if (latest == null)
                    return "";

                _choiceRetrievals++;

                StringBuilder sb = new StringBuilder();
                sb.Append("{");
                sb.Append("\"schema\":\"BannerlordAI.DynastyBranchChoiceRetrieval.v1\",");
                sb.Append("\"mode\":\"observe\",");
                sb.Append("\"actor\":");
                sb.Append(Json(actor.Name.ToString()));
                sb.Append(",\"actorId\":");
                sb.Append(Json(actorId));
                sb.Append(",\"branchId\":");
                sb.Append(Json(branchId));
                sb.Append(",\"retrievalContext\":");
                sb.Append(Json(retrievalContext));
                sb.Append(",\"currentCampaignHours\":");
                sb.Append(now.ToString("R", CultureInfo.InvariantCulture));
                sb.Append(",\"visibleChoiceCount\":");
                sb.Append(visibleCount.ToString(CultureInfo.InvariantCulture));
                sb.Append(",\"latestChoice\":{");
                sb.Append("\"id\":");
                sb.Append(Json(latest.Id));
                sb.Append(",\"knownByHours\":");
                sb.Append(latest.CampaignHours.ToString(
                    "R",
                    CultureInfo.InvariantCulture));
                sb.Append(",\"observedUtc\":");
                sb.Append(Json(latest.ObservedUtc));
                sb.Append(",\"contextId\":");
                sb.Append(Json(latest.ContextId));
                sb.Append(",\"contextName\":");
                sb.Append(Json(latest.ContextName));
                sb.Append(",\"optionIndex\":");
                sb.Append(latest.OptionIndex.ToString(
                    CultureInfo.InvariantCulture));
                sb.Append(",\"optionText\":");
                sb.Append(Json(latest.OptionText));
                sb.Append(",\"source\":");
                sb.Append(Json(latest.Source));
                sb.Append(",\"actorValid\":true");
                sb.Append(",\"branchValid\":true");
                sb.Append(",\"futureLeak\":false");
                sb.Append("},\"scoreMutation\":false}");

                string receipt = sb.ToString();

                ClanAIPostVanilla.WriteExternalLog(
                    "DYNASTY_BRANCH_CHOICE_RETRIEVED" +
                    " actor=" + Clean(actor.Name.ToString()) +
                    " actorId=" + Clean(actorId) +
                    " branchId=" + Clean(branchId) +
                    " visibleChoiceCount=" + visibleCount +
                    " contextId=" + Clean(latest.ContextId) +
                    " optionIndex=" + latest.OptionIndex +
                    " optionText=" + Clean(latest.OptionText) +
                    " knownByHours=" +
                    latest.CampaignHours.ToString(
                        "R",
                        CultureInfo.InvariantCulture) +
                    " retrievalContext=" +
                    Clean(retrievalContext) +
                    " futureLeak=False scoreMutation=False");

                return receipt;
            }
            catch (Exception ex)
            {
                ClanAIPostVanilla.WriteExternalLog(
                    "DYNASTY_BRANCH_CHOICE_RETRIEVE_FAILED" +
                    " type=" + ex.GetType().Name +
                    " message=" + Clean(ex.Message));

                return "";
            }
        }

        public static string BuildLatestBranchHistoryReceipt(
            string retrievalContext)
        {
            try
            {
                if (!_ready)
                    return "";

                double now;
                try
                {
                    now = CampaignTime.Now.ToHours;
                }
                catch
                {
                    return "";
                }

                string branchId = DynastyMindSeed.BranchId;
                DynastyEpisodeSelection selection =
                    DynastyEpisodeRetrievalPolicy.SelectLatestBranchHistory(
                        BuildDescriptors(), branchId, now);

                Episode latest = FindEpisode(selection.Episode);
                if (latest == null)
                    return "";

                _branchHistoryRetrievals++;

                StringBuilder sb = new StringBuilder();
                sb.Append("{");
                sb.Append("\"schema\":\"BannerlordAI.DynastyBranchHistoryRetrieval.v1\",");
                sb.Append("\"mode\":\"observe\",");
                sb.Append("\"contextType\":\"branch-history\",");
                sb.Append("\"personalMemory\":false,");
                sb.Append("\"branchId\":");
                sb.Append(Json(branchId));
                sb.Append(",\"retrievalContext\":");
                sb.Append(Json(retrievalContext));
                sb.Append(",\"currentCampaignHours\":");
                sb.Append(now.ToString("R", CultureInfo.InvariantCulture));
                sb.Append(",\"visibleHistoryCount\":");
                sb.Append(selection.VisibleCount.ToString(CultureInfo.InvariantCulture));
                sb.Append(",\"latestHistory\":{");
                sb.Append("\"id\":");
                sb.Append(Json(latest.Id));
                sb.Append(",\"kind\":");
                sb.Append(Json(latest.Kind));
                sb.Append(",\"actor\":");
                sb.Append(Json(latest.ActorName));
                sb.Append(",\"actorId\":");
                sb.Append(Json(latest.ActorId));
                sb.Append(",\"knownByHours\":");
                sb.Append(latest.CampaignHours.ToString("R", CultureInfo.InvariantCulture));
                sb.Append(",\"observedUtc\":");
                sb.Append(Json(latest.ObservedUtc));
                sb.Append(",\"contextId\":");
                sb.Append(Json(latest.ContextId));
                sb.Append(",\"contextName\":");
                sb.Append(Json(latest.ContextName));
                sb.Append(",\"source\":");
                sb.Append(Json(latest.Source));
                sb.Append(",\"detail\":");
                sb.Append(Json(latest.Detail));
                sb.Append(",\"provenancePreserved\":true");
                sb.Append(",\"branchValid\":true");
                sb.Append(",\"futureLeak\":false");
                sb.Append("},\"scoreMutation\":false}");

                string receipt = sb.ToString();

                ClanAIPostVanilla.WriteExternalLog(
                    "DYNASTY_BRANCH_HISTORY_RETRIEVED" +
                    " branchId=" + Clean(branchId) +
                    " originalActor=" + Clean(latest.ActorName) +
                    " originalActorId=" + Clean(latest.ActorId) +
                    " visibleHistoryCount=" + selection.VisibleCount +
                    " latestKind=" + Clean(latest.Kind) +
                    " retrievalContext=" + Clean(retrievalContext) +
                    " contextType=branch-history" +
                    " personalMemory=False provenancePreserved=True" +
                    " futureLeak=False scoreMutation=False");

                return receipt;
            }
            catch (Exception ex)
            {
                ClanAIPostVanilla.WriteExternalLog(
                    "DYNASTY_BRANCH_HISTORY_RETRIEVE_FAILED" +
                    " type=" + ex.GetType().Name +
                    " message=" + Clean(ex.Message));

                return "";
            }
        }

        public static string SummaryFields()
        {
            return
                " dynastyBranchEpisodes=" + Episodes.Count +
                " dynastyBranchEpisodeRecorded=" + _recorded +
                " dynastyBranchEpisodeRetrievals=" + _retrievals +
                " dynastyBranchChoiceRecorded=" + _choiceRecorded +
                " dynastyBranchChoiceRetrievals=" + _choiceRetrievals +
                " dynastyBranchHistoryRetrievals=" + _branchHistoryRetrievals +
                " dynastyBranchStructuralRecorded=" + _structuralRecorded +
                " dynastyBranchEpisodeDuplicates=" + _duplicates +
                " dynastyBranchEpisodeRejected=" + _rejected;
        }

        private static List<DynastyEpisodeDescriptor> BuildDescriptors()
        {
            List<DynastyEpisodeDescriptor> result =
                new List<DynastyEpisodeDescriptor>(Episodes.Count);

            for (int i = 0; i < Episodes.Count; i++)
            {
                Episode e = Episodes[i];
                result.Add(new DynastyEpisodeDescriptor
                {
                    SourceIndex = i,
                    Id = e.Id,
                    BranchId = e.BranchId,
                    CampaignHours = e.CampaignHours,
                    ObservedUtc = e.ObservedUtc,
                    ActorId = e.ActorId,
                    ActorName = e.ActorName,
                    Kind = e.Kind
                });
            }

            return result;
        }

        private static Episode FindEpisode(
            DynastyEpisodeDescriptor descriptor)
        {
            if (descriptor == null ||
                descriptor.SourceIndex < 0 ||
                descriptor.SourceIndex >= Episodes.Count)
            {
                return null;
            }

            return Episodes[descriptor.SourceIndex];
        }

        private static List<string> ExportRows()
        {
            List<string> rows = new List<string>();

            for (int i = 0; i < Episodes.Count; i++)
            {
                Episode e = Episodes[i];

                rows.Add(
                    "D2|" +
                    B64(e.Id) + "|" +
                    B64(e.BranchId) + "|" +
                    B64(e.CampaignHours.ToString(
                        "R",
                        CultureInfo.InvariantCulture)) + "|" +
                    B64(e.ObservedUtc) + "|" +
                    B64(e.ActorId) + "|" +
                    B64(e.ActorName) + "|" +
                    B64(e.Kind) + "|" +
                    B64(e.ContextId) + "|" +
                    B64(e.ContextName) + "|" +
                    B64(e.Source) + "|" +
                    B64(e.Detail) + "|" +
                    B64(e.OptionIndex.ToString(
                        CultureInfo.InvariantCulture)) + "|" +
                    B64(e.OptionText));
            }

            return rows;
        }

        private static void ImportRows(
            List<string> rows)
        {
            if (rows == null)
                return;

            HashSet<string> ids =
                new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < rows.Count; i++)
            {
                if (Episodes.Count >= Capacity)
                    break;

                Episode e;

                if (!TryDecode(
                        rows[i],
                        out e))
                {
                    _rejected++;
                    continue;
                }

                if (ids.Contains(e.Id))
                {
                    _duplicates++;
                    continue;
                }

                ids.Add(e.Id);
                Episodes.Add(e);
            }
        }

        private static bool TryDecode(
            string row,
            out Episode episode)
        {
            episode = null;

            if (string.IsNullOrWhiteSpace(row))
                return false;

            string[] p = row.Split('|');
            bool d1 = p.Length == 12 && p[0] == "D1";
            bool d2 = p.Length == 14 && p[0] == "D2";

            if (!d1 && !d2)
                return false;

            int fieldCount = d1 ? 11 : 13;
            string[] f = new string[fieldCount];

            try
            {
                for (int i = 1; i < p.Length; i++)
                {
                    f[i - 1] = FromB64(p[i]);
                }
            }
            catch
            {
                return false;
            }

            Guid id;
            DateTime utc;
            double hours;

            if (!Guid.TryParse(f[0], out id) ||
                id == Guid.Empty)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(f[1]) ||
                !double.TryParse(
                    f[2],
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out hours) ||
                double.IsNaN(hours) ||
                double.IsInfinity(hours) ||
                !DateTime.TryParse(
                    f[3],
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out utc) ||
                string.IsNullOrWhiteSpace(f[4]) ||
                string.IsNullOrWhiteSpace(f[6]) ||
                string.IsNullOrWhiteSpace(f[7]) ||
                string.IsNullOrWhiteSpace(f[9]))
            {
                return false;
            }

            string kind = f[6];

            if (!DynastyStructuralEpisodePolicy.IsAcceptedProductionKind(kind))
            {
                return false;
            }

            int optionIndex = -1;
            string optionText = "";

            if (d2)
            {
                if (!int.TryParse(
                        f[11],
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out optionIndex))
                {
                    return false;
                }

                optionText = f[12] ?? "";
            }

            if (kind == "IncidentChoice" &&
                (!d2 ||
                 optionIndex < 0 ||
                 string.IsNullOrWhiteSpace(optionText)))
            {
                return false;
            }

            if (kind ==
                    DynastyStructuralEpisodePolicy.RulingClanChangedKind &&
                (!d2 ||
                 optionIndex != -1 ||
                 !string.IsNullOrEmpty(optionText) ||
                 string.IsNullOrEmpty(
                    DynastyStructuralEpisodePolicy.IdentityFromStoredRow(
                        f[1],
                        kind,
                        f[10]))))
            {
                return false;
            }

            episode = new Episode();
            episode.Id = f[0];
            episode.BranchId = f[1];
            episode.CampaignHours = hours;
            episode.ObservedUtc = f[3];
            episode.ActorId = f[4];
            episode.ActorName = f[5];
            episode.Kind = kind;
            episode.ContextId = f[7];
            episode.ContextName = f[8];
            episode.Source = f[9];
            episode.Detail = f[10];
            episode.OptionIndex = optionIndex;
            episode.OptionText = optionText;

            return true;
        }

        private static string B64(
            string value)
        {
            return Convert.ToBase64String(
                Encoding.UTF8.GetBytes(
                    value ?? ""));
        }

        private static string FromB64(
            string value)
        {
            return Encoding.UTF8.GetString(
                Convert.FromBase64String(
                    value ?? ""));
        }

        private static string Safe(
            string value)
        {
            return value ?? "";
        }

        private static string Clean(
            string value)
        {
            return
                (value ?? "<none>")
                    .Replace("\r", " ")
                    .Replace("\n", " ")
                    .Replace("\t", " ")
                    .Replace("|", "/");
        }

        private static string Slug(
            string value)
        {
            string text =
                (value ?? "")
                    .Trim()
                    .ToLowerInvariant();

            StringBuilder sb =
                new StringBuilder();

            bool lastDash = false;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];

                if (char.IsLetterOrDigit(c))
                {
                    sb.Append(c);
                    lastDash = false;
                }
                else if (!lastDash)
                {
                    sb.Append('-');
                    lastDash = true;
                }
            }

            return
                sb.ToString()
                    .Trim('-');
        }

        private static string Json(
            string value)
        {
            return
                "\"" +
                (value ?? "")
                    .Replace("\\", "\\\\")
                    .Replace("\"", "\\\"")
                    .Replace("\r", "\\r")
                    .Replace("\n", "\\n") +
                "\"";
        }
    }
}

