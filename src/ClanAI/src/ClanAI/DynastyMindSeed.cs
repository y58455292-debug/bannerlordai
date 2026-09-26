using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using TaleWorlds.CampaignSystem;

namespace ClanAI
{
    internal static class DynastyMindSeed
    {
        internal static readonly string ConfigPath =
            ModuleRuntimePaths.Data("DynastyMindSeed.cfg");

        private sealed class Entry
        {
            public string ActorId;
            public string ActorName;
            public double KnownByHours;
            public string Kind;
            public string Key;
            public float Strength;
            public float Confidence;
            public string Source;
            public string Text;
        }

        private static readonly List<Entry> Available =
            new List<Entry>();

        private static readonly Dictionary<string, bool> RetrievalLogged =
            new Dictionary<string, bool>();

        private static string _branchId = "";
        private static double _canonCutoffHours = -1d;
        private static bool _cutoffRestored;
        private static int _parsed;
        private static int _futureHidden;
        private static int _malformed;
        private static long _retrievals;
        private static long _operatorShadowDeliberations;
        private static long _operatorShadowEntries;
        private static long _causalEvaluations;
        private static long _causalWouldFlip;
        private static long _causalApplied;
        private static long _causalActualFlip;
        private static long _causalFailures;

        internal static string BranchId
        {
            get { return _branchId ?? ""; }
        }

        internal static void SyncData(IDataStore dataStore)
        {
            string cutoff = "";
            string branchId = "";

            if (dataStore.IsSaving)
            {
                cutoff =
                    _canonCutoffHours >= 0d
                        ? _canonCutoffHours.ToString(
                            "R",
                            CultureInfo.InvariantCulture)
                        : "";

                branchId = _branchId ?? "";
            }

            bool cutoffFound =
                dataStore.SyncData(
                    "ClanAI_DynastyCanonCutoffHours_v1",
                    ref cutoff);

            bool branchFound =
                dataStore.SyncData(
                    "ClanAI_DynastyBranchId_v1",
                    ref branchId);

            if (dataStore.IsLoading)
            {
                double parsedCutoff;

                if (cutoffFound &&
                    !string.IsNullOrEmpty(cutoff) &&
                    double.TryParse(
                        cutoff,
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out parsedCutoff))
                {
                    _canonCutoffHours = parsedCutoff;
                    _cutoffRestored = true;
                }
                else
                {
                    _canonCutoffHours = -1d;
                    _cutoffRestored = false;
                }

                _branchId =
                    branchFound
                        ? (branchId ?? "")
                        : "";

            }
        }

        internal static void BeginSession()
        {
            Available.Clear();
            RetrievalLogged.Clear();
            _parsed = 0;
            _futureHidden = 0;
            _malformed = 0;
            _retrievals = 0;
            _operatorShadowDeliberations = 0;
            _operatorShadowEntries = 0;
            _causalEvaluations = 0;
            _causalWouldFlip = 0;
            _causalApplied = 0;
            _causalActualFlip = 0;
            _causalFailures = 0;

            DynastyMindCausalConfig.EnsureLoaded();

            if (!_cutoffRestored ||
                _canonCutoffHours < 0d)
            {
                try
                {
                    _canonCutoffHours =
                        CampaignTime.Now.ToHours;
                }
                catch
                {
                    _canonCutoffHours = 0d;
                }
            }

            if (string.IsNullOrEmpty(_branchId))
            {
                _branchId =
                    Guid.NewGuid()
                        .ToString("N");
            }

            LoadConfig();

            Hero main = Hero.MainHero;
            int mainCount =
                main == null
                    ? 0
                    : CountFor(main);

            ClanAIPostVanilla.WriteExternalLog(
                "DYNASTY_MIND_SEED_READY" +
                " branchId=" +
                Clean(_branchId) +
                " cutoffHours=" +
                _canonCutoffHours.ToString(
                    "R",
                    CultureInfo.InvariantCulture) +
                " restoredCutoff=" +
                _cutoffRestored +
                " parsed=" +
                _parsed +
                " available=" +
                Available.Count +
                " futureHidden=" +
                _futureHidden +
                " malformed=" +
                _malformed +
                " mainHero=" +
                (main == null
                    ? "<none>"
                    : Clean(main.Name.ToString())) +
                " mainHeroId=" +
                (main == null
                    ? "<none>"
                    : Clean(main.StringId)) +
                " mainAvailable=" +
                mainCount +
                " scoreMutation=False" +
                " futureCanonLeak=False");

            if (main != null)
            {
                LogActorEntries(
                    main,
                    "session-start");
            }

            _cutoffRestored = false;
        }

        internal static void ObserveDecisionContext(
            Hero actor,
            string context)
        {
            if (actor == null)
                return;

            int count = CountFor(actor);

            if (count <= 0)
                return;

            _retrievals++;

            string key =
                string.IsNullOrEmpty(actor.StringId)
                    ? actor.Name.ToString()
                    : actor.StringId;

            bool logged;

            if (!RetrievalLogged.TryGetValue(
                    key,
                    out logged) ||
                !logged)
            {
                RetrievalLogged[key] = true;

                ClanAIPostVanilla.WriteExternalLog(
                    "DYNASTY_MIND_RETRIEVED" +
                    " actor=" +
                    Clean(actor.Name.ToString()) +
                    " actorId=" +
                    Clean(actor.StringId) +
                    " available=" +
                    count +
                    " context=" +
                    Clean(context) +
                    " cutoffHours=" +
                    _canonCutoffHours.ToString(
                        "R",
                        CultureInfo.InvariantCulture) +
                    " scoreMutation=False");

                LogActorEntries(
                    actor,
                    "first-retrieval");
            }
        }

        internal static string BuildOperatorShadowReceipt(
            Hero actor,
            string decisionType,
            string decisionKey,
            string decisionTitle,
            string selectedOrTarget)
        {
            if (actor == null)
                return "";

            List<Entry> found =
                new List<Entry>();

            string id = actor.StringId;

            for (int i = 0;
                 i < Available.Count;
                 i++)
            {
                Entry e = Available[i];

                if (IdEquals(
                        e.ActorId,
                        id))
                {
                    found.Add(e);
                }
            }

            if (found.Count <= 0)
                return "";

            _retrievals++;
            _operatorShadowDeliberations++;
            _operatorShadowEntries +=
                found.Count;

            double campaignHours = 0d;

            try
            {
                campaignHours =
                    CampaignTime.Now.ToHours;
            }
            catch
            {
            }

            StringBuilder sb =
                new StringBuilder();

            sb.Append("{");
            sb.Append("\"schema\":\"BannerlordAI.DynastyMindDeliberation.v1\",");
            sb.Append("\"mode\":\"shadow\",");
            sb.Append("\"campaignHours\":");
            sb.Append(
                campaignHours.ToString(
                    "R",
                    CultureInfo.InvariantCulture));
            sb.Append(",\"actor\":");
            sb.Append(Json(actor.Name.ToString()));
            sb.Append(",\"actorId\":");
            sb.Append(Json(actor.StringId));
            sb.Append(",\"decisionType\":");
            sb.Append(Json(decisionType));
            sb.Append(",\"decisionKey\":");
            sb.Append(Json(decisionKey));
            sb.Append(",\"decisionTitle\":");
            sb.Append(Json(decisionTitle));
            sb.Append(",\"selectedOrTarget\":");
            sb.Append(Json(selectedOrTarget));
            sb.Append(",\"retrievedCount\":");
            sb.Append(
                found.Count.ToString(
                    CultureInfo.InvariantCulture));
            sb.Append(",\"entries\":[");

            for (int i = 0;
                 i < found.Count;
                 i++)
            {
                if (i > 0)
                    sb.Append(",");

                Entry e = found[i];
                float delta =
                    e.Strength *
                    e.Confidence;

                if (delta > 1f)
                    delta = 1f;
                else if (delta < -1f)
                    delta = -1f;

                sb.Append("{");
                sb.Append("\"kind\":");
                sb.Append(Json(e.Kind));
                sb.Append(",\"key\":");
                sb.Append(Json(e.Key));
                sb.Append(",\"source\":");
                sb.Append(Json(e.Source));
                sb.Append(",\"knownByHours\":");
                sb.Append(
                    e.KnownByHours.ToString(
                        "R",
                        CultureInfo.InvariantCulture));
                sb.Append(",\"strength\":");
                sb.Append(
                    e.Strength.ToString(
                        "R",
                        CultureInfo.InvariantCulture));
                sb.Append(",\"confidence\":");
                sb.Append(
                    e.Confidence.ToString(
                        "R",
                        CultureInfo.InvariantCulture));
                sb.Append(",\"shadowFeature\":");
                sb.Append(
                    Json(
                        "dynasty." +
                        e.Kind +
                        "." +
                        e.Key));
                sb.Append(",\"proposedDelta\":");
                sb.Append(
                    delta.ToString(
                        "R",
                        CultureInfo.InvariantCulture));
                sb.Append(",\"applied\":false");
                sb.Append("}");
            }

            sb.Append("],\"scoreMutation\":false}");

            string receipt =
                sb.ToString();

            ClanAIPostVanilla.WriteExternalLog(
                "DYNASTY_MIND_OPERATOR_SHADOW" +
                " actor=" +
                Clean(actor.Name.ToString()) +
                " actorId=" +
                Clean(actor.StringId) +
                " decisionType=" +
                Clean(decisionType) +
                " decisionKey=" +
                Clean(decisionKey) +
                " retrieved=" +
                found.Count +
                " scoreMutation=False");

            return receipt;
        }

        internal static string BuildSettlementDefenseDecision(
            Hero actor,
            string settlementName,
            bool atTarget)
        {
            try
            {
                _causalEvaluations++;

                const float holdScore = 1.0f;
                const float patrolBaseScore = 0.985f;
                const float supportScale = 0.015f;
                const float maxDelta = 0.04f;

                List<Entry> relevant = new List<Entry>();
                List<float> weights = new List<float>();
                bool anchorMatch = false;
                float evidence = 0f;

                if (actor != null && atTarget)
                {
                    string actorId = actor.StringId;

                    for (int i = 0; i < Available.Count; i++)
                    {
                        Entry e = Available[i];

                        if (!IdEquals(e.ActorId, actorId))
                            continue;

                        float weight = 0f;

                        if (string.Equals(
                                e.Kind,
                                "territorial_anchor",
                                StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(
                                e.Key,
                                settlementName,
                                StringComparison.OrdinalIgnoreCase))
                        {
                            weight = 1f;
                            anchorMatch = true;
                        }
                        else if (string.Equals(
                                     e.Kind,
                                     "family",
                                     StringComparison.OrdinalIgnoreCase) &&
                                 string.Equals(
                                     e.Key,
                                     "protect_line",
                                     StringComparison.OrdinalIgnoreCase))
                        {
                            weight = 0.5f;
                        }
                        else if (string.Equals(
                                     e.Kind,
                                     "logistics",
                                     StringComparison.OrdinalIgnoreCase) &&
                                 string.Equals(
                                     e.Key,
                                     "survival_capacity",
                                     StringComparison.OrdinalIgnoreCase))
                        {
                            weight = 0.5f;
                        }
                        else if (string.Equals(
                                     e.Kind,
                                     "doctrine",
                                     StringComparison.OrdinalIgnoreCase) &&
                                 string.Equals(
                                     e.Key,
                                     "courage_vs_waste",
                                     StringComparison.OrdinalIgnoreCase))
                        {
                            weight = 0.5f;
                        }

                        if (weight <= 0f)
                            continue;

                        relevant.Add(e);
                        weights.Add(weight);

                        evidence +=
                            e.Strength *
                            e.Confidence *
                            weight;
                    }
                }

                if (!anchorMatch)
                {
                    relevant.Clear();
                    weights.Clear();
                    evidence = 0f;
                }

                float delta =
                    Math.Min(
                        maxDelta,
                        evidence * supportScale);

                float factor = 1f + delta;
                float patrolAdjustedScore =
                    patrolBaseScore * factor;

                string baselineChoice = "HOLD";
                string proposedChoice =
                    patrolAdjustedScore > holdScore
                        ? "PATROL"
                        : "HOLD";

                bool wouldFlip =
                    proposedChoice != baselineChoice;

                if (wouldFlip)
                    _causalWouldFlip++;

                DynastyMindCausalMode mode =
                    DynastyMindCausalConfig.Mode;

                bool applied =
                    mode == DynastyMindCausalMode.Apply &&
                    wouldFlip &&
                    anchorMatch &&
                    atTarget;

                string finalChoice =
                    applied
                        ? proposedChoice
                        : baselineChoice;

                if (applied)
                {
                    _causalApplied++;

                    if (finalChoice != baselineChoice)
                        _causalActualFlip++;
                }

                StringBuilder sb = new StringBuilder();
                sb.Append("{");
                sb.Append("\"schema\":\"BannerlordAI.DynastyMindCausalDecision.v1\",");
                sb.Append("\"mode\":");
                sb.Append(Json(mode.ToString()));
                sb.Append(",\"configStatus\":");
                sb.Append(Json(DynastyMindCausalConfig.Status));
                sb.Append(",\"actor\":");
                sb.Append(Json(actor == null ? "<none>" : actor.Name.ToString()));
                sb.Append(",\"actorId\":");
                sb.Append(Json(actor == null ? "<none>" : actor.StringId));
                sb.Append(",\"decisionFamily\":\"settlement_defense_posture\",");
                sb.Append("\"settlement\":");
                sb.Append(Json(settlementName));
                sb.Append(",\"atTarget\":");
                sb.Append(atTarget ? "true" : "false");
                sb.Append(",\"anchorMatch\":");
                sb.Append(anchorMatch ? "true" : "false");
                sb.Append(",\"baselineChoice\":");
                sb.Append(Json(baselineChoice));
                sb.Append(",\"proposedChoice\":");
                sb.Append(Json(proposedChoice));
                sb.Append(",\"finalChoice\":");
                sb.Append(Json(finalChoice));
                sb.Append(",\"holdScore\":");
                sb.Append(holdScore.ToString("R", CultureInfo.InvariantCulture));
                sb.Append(",\"patrolBaseScore\":");
                sb.Append(patrolBaseScore.ToString("R", CultureInfo.InvariantCulture));
                sb.Append(",\"memoryEvidence\":");
                sb.Append(evidence.ToString("R", CultureInfo.InvariantCulture));
                sb.Append(",\"memoryDelta\":");
                sb.Append(delta.ToString("R", CultureInfo.InvariantCulture));
                sb.Append(",\"memoryFactor\":");
                sb.Append(factor.ToString("R", CultureInfo.InvariantCulture));
                sb.Append(",\"patrolAdjustedScore\":");
                sb.Append(patrolAdjustedScore.ToString("R", CultureInfo.InvariantCulture));
                sb.Append(",\"maxDelta\":");
                sb.Append(maxDelta.ToString("R", CultureInfo.InvariantCulture));
                sb.Append(",\"wouldFlip\":");
                sb.Append(wouldFlip ? "true" : "false");
                sb.Append(",\"applied\":");
                sb.Append(applied ? "true" : "false");
                sb.Append(",\"scoreMutation\":false,");
                sb.Append("\"intentMutation\":");
                sb.Append(applied ? "true" : "false");
                sb.Append(",\"entries\":[");

                for (int i = 0; i < relevant.Count; i++)
                {
                    if (i > 0)
                        sb.Append(",");

                    Entry e = relevant[i];
                    float contribution =
                        e.Strength *
                        e.Confidence *
                        weights[i];

                    sb.Append("{");
                    sb.Append("\"kind\":");
                    sb.Append(Json(e.Kind));
                    sb.Append(",\"key\":");
                    sb.Append(Json(e.Key));
                    sb.Append(",\"source\":");
                    sb.Append(Json(e.Source));
                    sb.Append(",\"weight\":");
                    sb.Append(weights[i].ToString("R", CultureInfo.InvariantCulture));
                    sb.Append(",\"contribution\":");
                    sb.Append(contribution.ToString("R", CultureInfo.InvariantCulture));
                    sb.Append("}");
                }

                sb.Append("]}");

                ClanAIPostVanilla.WriteExternalLog(
                    (applied
                        ? "DYNASTY_MIND_CAUSAL_APPLY"
                        : "DYNASTY_MIND_CAUSAL_OBSERVE") +
                    " actor=" +
                    Clean(actor == null ? "<none>" : actor.Name.ToString()) +
                    " settlement=" +
                    Clean(settlementName) +
                    " baseline=" +
                    baselineChoice +
                    " proposed=" +
                    proposedChoice +
                    " final=" +
                    finalChoice +
                    " anchorMatch=" +
                    anchorMatch +
                    " factor=" +
                    factor.ToString("R", CultureInfo.InvariantCulture) +
                    " wouldFlip=" +
                    wouldFlip +
                    " applied=" +
                    applied +
                    " causalEvaluations=" +
                    _causalEvaluations +
                    " causalWouldFlip=" +
                    _causalWouldFlip +
                    " causalApplied=" +
                    _causalApplied +
                    " causalActualFlip=" +
                    _causalActualFlip +
                    " causalFailures=" +
                    _causalFailures +
                    " scoreMutation=False");

                return sb.ToString();
            }
            catch (Exception ex)
            {
                _causalFailures++;

                ClanAIPostVanilla.WriteExternalLog(
                    "DYNASTY_MIND_CAUSAL_FAILURE type=" +
                    ex.GetType().Name +
                    " message=" +
                    Clean(ex.Message));

                return
                    "{\"schema\":\"BannerlordAI.DynastyMindCausalDecision.v1\"," +
                    "\"mode\":\"Observe\",\"baselineChoice\":\"HOLD\"," +
                    "\"proposedChoice\":\"HOLD\",\"finalChoice\":\"HOLD\"," +
                    "\"wouldFlip\":false,\"applied\":false," +
                    "\"scoreMutation\":false,\"intentMutation\":false," +
                    "\"error\":" + Json(ex.GetType().Name) + "}";
            }
        }

        internal static int CountFor(
            Hero actor)
        {
            if (actor == null)
                return 0;

            string id = actor.StringId;
            int count = 0;

            for (int i = 0;
                 i < Available.Count;
                 i++)
            {
                Entry e = Available[i];

                if (IdEquals(
                    e.ActorId,
                    id))
                {
                    count++;
                }
            }

            return count;
        }

        internal static string SummaryFields()
        {
            return
                " dynastyMindAvailable=" +
                Available.Count +
                " dynastyMindFutureHidden=" +
                _futureHidden +
                " dynastyMindMalformed=" +
                _malformed +
                " dynastyMindRetrievals=" +
                _retrievals +
                " dynastyMindOperatorShadowDeliberations=" +
                _operatorShadowDeliberations +
                " dynastyMindOperatorShadowEntries=" +
                _operatorShadowEntries +
                " dynastyMindCausalEvaluations=" +
                _causalEvaluations +
                " dynastyMindCausalWouldFlip=" +
                _causalWouldFlip +
                " dynastyMindCausalApplied=" +
                _causalApplied +
                " dynastyMindCausalActualFlip=" +
                _causalActualFlip +
                " dynastyMindCausalFailures=" +
                _causalFailures +
                " dynastyMindCausalMode=" +
                DynastyMindCausalConfig.Mode +
                " dynastyMindCutoffHours=" +
                _canonCutoffHours.ToString(
                    "R",
                    CultureInfo.InvariantCulture);
        }

        private static void LoadConfig()
        {
            try
            {
                if (string.IsNullOrEmpty(ConfigPath) ||
                    !File.Exists(ConfigPath))
                {
                    ClanAIPostVanilla.WriteExternalLog(
                        "DYNASTY_MIND_SEED_MISSING path=" +
                        ConfigPath);

                    return;
                }

                string[] lines =
                    File.ReadAllLines(
                        ConfigPath);

                for (int i = 0;
                     i < lines.Length;
                     i++)
                {
                    string raw =
                        lines[i] ?? "";

                    string line =
                        raw.Trim();

                    if (line.Length == 0 ||
                        line.StartsWith("#"))
                    {
                        continue;
                    }

                    Entry entry;

                    if (!TryParseEntry(
                            line,
                            out entry))
                    {
                        _malformed++;
                        continue;
                    }

                    _parsed++;

                    if (entry.KnownByHours >
                        _canonCutoffHours +
                        0.000001d)
                    {
                        _futureHidden++;
                        continue;
                    }

                    Available.Add(entry);
                }
            }
            catch (Exception ex)
            {
                ClanAIPostVanilla.WriteExternalLog(
                    "DYNASTY_MIND_SEED_LOAD_FAILED" +
                    " type=" +
                    ex.GetType().Name +
                    " message=" +
                    Clean(ex.Message));
            }
        }

        private static bool TryParseEntry(
            string line,
            out Entry entry)
        {
            entry = null;

            string[] p =
                line.Split(
                    new char[] { '|' },
                    9);

            if (p.Length != 9)
                return false;

            double knownBy;
            float strength;
            float confidence;

            if (!double.TryParse(
                    p[2],
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out knownBy))
            {
                return false;
            }

            if (!float.TryParse(
                    p[5],
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out strength))
            {
                return false;
            }

            if (!float.TryParse(
                    p[6],
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out confidence))
            {
                return false;
            }

            entry = new Entry();

            entry.ActorId = p[0].Trim();
            entry.ActorName = p[1].Trim();
            entry.KnownByHours = knownBy;
            entry.Kind = p[3].Trim();
            entry.Key = p[4].Trim();
            entry.Strength = strength;
            entry.Confidence = confidence;
            entry.Source = p[7].Trim();
            entry.Text = p[8].Trim();

            return
                entry.ActorId.Length > 0 &&
                entry.Kind.Length > 0 &&
                entry.Key.Length > 0;
        }

        private static void LogActorEntries(
            Hero actor,
            string reason)
        {
            if (actor == null)
                return;

            string id = actor.StringId;

            for (int i = 0;
                 i < Available.Count;
                 i++)
            {
                Entry e = Available[i];

                if (!IdEquals(
                        e.ActorId,
                        id))
                {
                    continue;
                }

                ClanAIPostVanilla.WriteExternalLog(
                    "DYNASTY_MIND_ENTRY" +
                    " actor=" +
                    Clean(actor.Name.ToString()) +
                    " actorId=" +
                    Clean(id) +
                    " kind=" +
                    Clean(e.Kind) +
                    " key=" +
                    Clean(e.Key) +
                    " strength=" +
                    e.Strength.ToString(
                        "R",
                        CultureInfo.InvariantCulture) +
                    " confidence=" +
                    e.Confidence.ToString(
                        "R",
                        CultureInfo.InvariantCulture) +
                    " knownByHours=" +
                    e.KnownByHours.ToString(
                        "R",
                        CultureInfo.InvariantCulture) +
                    " source=" +
                    Clean(e.Source) +
                    " reason=" +
                    Clean(reason) +
                    " text=" +
                    Clean(e.Text));
            }
        }

        private static bool IdEquals(
            string a,
            string b)
        {
            return string.Equals(
                a ?? "",
                b ?? "",
                StringComparison.Ordinal);
        }

        private static string Json(
            string text)
        {
            return
                "\"" +
                (text ?? "")
                    .Replace("\\", "\\\\")
                    .Replace("\"", "\\\"")
                    .Replace("\r", "\\r")
                    .Replace("\n", "\\n") +
                "\"";
        }

        private static string Clean(
            string text)
        {
            return
                (text ?? "<none>")
                .Replace("\r", " ")
                .Replace("\n", " ")
                .Replace("\t", " ")
                .Replace("|", "/");
        }
    }
}

