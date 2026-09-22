using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;

namespace BannerlordInspector
{
    /// <summary>
    /// Read-only generalized behavior-delta / local-radius recorder.
    ///
    /// Design goal:
    /// - watch every hero-led kingdom party cheaply at the reliable CampaignEventDispatcher.AiHourlyTick boundary;
    /// - do NOT preselect a lord, settlement, or behavior;
    /// - trigger a deeper local capture only when the party's vanilla candidate winner,
    ///   strategic behavior/target, army state, attachment state, or settlement state changes;
    /// - preserve the exact uninterrupted game-process session as its own timeline;
    /// - never mutate PartyThinkParams or campaign state.
    ///
    /// External live configuration (no rebuild required):
    /// D:\BannerlordAIResearch\Data\BehaviorDeltaRecorder.cfg
    ///
    /// Session output:
    /// D:\BannerlordAIResearch\Data\BehaviorDeltaSessions\<session-id>\
    /// </summary>
    public static class BehaviorDeltaRadiusRecorder
    {
        public const string Version = "0.2";
        public const string ConfigPath = @"D:\BannerlordAIResearch\Data\BehaviorDeltaRecorder.cfg";
        public const string SessionRoot = @"D:\BannerlordAIResearch\Telemetry\BannerlordInspector\ai\behavior-delta";

        public static bool Enabled = true;
        public static float Radius = 15f;
        public static int TopCandidates = 12;
        public static int MaxNearbyParties = 24;
        public static int MaxNearbySettlements = 18;
        public static bool CaptureCandidateWinnerChange = true;
        public static bool CaptureFinalStateChange = true;

        public static long ObservedPartyThinks;
        public static long EligiblePartyThinks;
        public static long CandidateWinnerChanges;
        public static long FinalBehaviorChanges;
        public static long FinalTargetChanges;
        public static long ArmyStateChanges;
        public static long AttachmentStateChanges;
        public static long SettlementStateChanges;
        public static long RadiusCaptures;
        public static long CandidateRowsWritten;
        public static long PartyRadiusRowsWritten;
        public static long SettlementRadiusRowsWritten;
        public static long RadiusDeltaRowsWritten;
        public static long WriteFailures;

        public static string SessionId;
        public static string SessionDirectory;
        public static string LastEventId;
        public static string LastEventPartyId;
        public static string LastEventPartyName;
        public static string LastEventReason;
        public static string LastEventCampaignTime;
        public static string LastEventRealTime;
        public static string LastObservationCampaignTime;
        public static string LastObservationRealTime;

        private static readonly object Sync = new object();
        private static readonly Encoding Utf8 = new UTF8Encoding(false);
        private static readonly Dictionary<string, PartyState> LastFinalStateByParty =
            new Dictionary<string, PartyState>(StringComparer.Ordinal);
        private static readonly Dictionary<string, DecisionState> LastDecisionByParty =
            new Dictionary<string, DecisionState>(StringComparer.Ordinal);
        private static readonly Dictionary<string, Dictionary<string, ContextEntityState>> LastRadiusByAnchorParty =
            new Dictionary<string, Dictionary<string, ContextEntityState>>(StringComparer.Ordinal);

        private static DateTime _lastConfigCheckUtc = DateTime.MinValue;
        private static DateTime _lastConfigWriteUtc = DateTime.MinValue;
        private static DateTime _lastStatusWriteUtc = DateTime.MinValue;
        private static long _eventSequence;
        private static bool _initialized;
        private static bool _disabledByError;

        [ThreadStatic] private static PartyState _beforeThink;
        [ThreadStatic] private static string _beforePartyId;
        [ThreadStatic] private static DecisionState _pendingDecision;
        [ThreadStatic] private static bool _pendingDecisionWinnerChanged;
        [ThreadStatic] private static string _pendingPreviousWinner;

        private sealed class RecorderConfig
        {
            public bool Enabled = true;
            public float Radius = 15f;
            public int TopCandidates = 12;
            public int MaxNearbyParties = 24;
            public int MaxNearbySettlements = 18;
            public bool CaptureCandidateWinnerChange = true;
            public bool CaptureFinalStateChange = true;
        }

        private sealed class PartyState
        {
            public string PartyId;
            public string PartyName;
            public string LeaderId;
            public string LeaderName;
            public string ClanId;
            public string ClanName;
            public string KingdomId;
            public string KingdomName;
            public string Behavior;
            public string TargetSettlementId;
            public string TargetSettlementName;
            public string TargetPartyId;
            public string TargetPartyName;
            public string CurrentSettlementId;
            public string CurrentSettlementName;
            public string ArmyId;
            public string ArmyName;
            public string AttachedToId;
            public string AttachedToName;
            public bool InMapEvent;
            public bool IsDisbanding;
            public float X;
            public float Y;
        }

        private sealed class DecisionCandidate
        {
            public int OriginalIndex;
            public float Score;
            public string Behavior;
            public string TargetId;
            public string TargetName;
            public string NavigationType;
            public string IsFromPort;
            public string IsTargetingPort;
            public string WillGatherArmy;
        }

        private sealed class DecisionState
        {
            public string TopBehavior;
            public string TopTargetId;
            public string TopTargetName;
            public float TopScore;
            public int CandidateCount;
            public string CurrentObjectiveValue;
            public string WillGatherAnArmy;
            public string DoNotChangeBehavior;
            public List<DecisionCandidate> Candidates = new List<DecisionCandidate>();
        }

        private sealed class ContextEntityState
        {
            public string Key;
            public string EntityType;
            public string EntityId;
            public string EntityName;
            public float Distance;
            public string Signature;
            public string CsvPayload;
        }

        public static void Initialize()
        {
            if (_initialized || _disabledByError)
                return;

            lock (Sync)
            {
                if (_initialized || _disabledByError)
                    return;

                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath));
                    Directory.CreateDirectory(SessionRoot);
                    EnsureDefaultConfig();
                    ReloadConfig(force: true);

                    SessionId =
                        DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture) +
                        "_pid" + Process.GetCurrentProcess().Id.ToString(CultureInfo.InvariantCulture);
                    SessionDirectory = Path.Combine(SessionRoot, SessionId);
                    Directory.CreateDirectory(SessionDirectory);

                    WriteHeaders();
                    WriteSessionMeta();
                    WriteStatus();

                    _initialized = true;
                    InspectorLog.Info(
                        "BEHAVIOR DELTA RADIUS RECORDER v" + Version +
                        " initialized READ-ONLY. session=" + SessionId +
                        " radius=" + F(Radius) +
                        " topCandidates=" + TopCandidates +
                        " output=" + SessionDirectory +
                        " config=" + ConfigPath);
                }
                catch (Exception ex)
                {
                    _disabledByError = true;
                    WriteFailures++;
                    InspectorLog.Error(
                        "BEHAVIOR DELTA RADIUS RECORDER v" + Version +
                        " initialization failed; recorder disabled.", ex);
                }
            }
        }

        public static void BeforePartyThink(MobileParty party)
        {
            // Supplemental outer-boundary snapshot only. The proven high-volume
            // observation/counter boundary is CampaignEventDispatcher.AiHourlyTick,
            // handled by CaptureDecision below. Do not count here.
            Initialize();
            ReloadConfig(force: false);

            _beforeThink = null;
            _beforePartyId = null;
            _pendingDecision = null;
            _pendingDecisionWinnerChanged = false;
            _pendingPreviousWinner = null;

            if (!Enabled || _disabledByError || !IsEligibleParty(party))
                return;

            _beforeThink = SnapshotPartyState(party);
            _beforePartyId = _beforeThink == null ? null : _beforeThink.PartyId;
        }

        public static void CaptureDecision(MobileParty party, PartyThinkParams think)
        {
            // PRIMARY RECORDER BOUNDARY.
            // 03F/03H proved CampaignEventDispatcher.AiHourlyTick is the reliable
            // high-volume boundary (hundreds of thousands of observations per season).
            // v0.27 incorrectly made the outer PartyHourlyAiTick prefix a prerequisite;
            // v0.2 is deliberately self-contained here so restarts/replays are not
            // required to preselect a party or behavior.
            ObservedPartyThinks++;
            Initialize();
            ReloadConfig(force: false);

            LastObservationRealTime = DateTime.Now.ToString(
                "yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
            LastObservationCampaignTime = SafeCampaignTime();

            if (!Enabled || _disabledByError || party == null || think == null)
            {
                MaybeWriteStatus();
                return;
            }

            if (!IsEligibleParty(party))
            {
                MaybeWriteStatus();
                return;
            }

            EligiblePartyThinks++;

            try
            {
                PartyState currentParty = SnapshotPartyState(party);
                DecisionState current = SnapshotDecision(think);
                if (currentParty == null || current == null || string.IsNullOrEmpty(currentParty.PartyId))
                    return;

                PartyState previousParty;
                LastFinalStateByParty.TryGetValue(currentParty.PartyId, out previousParty);

                DecisionState previousDecision;
                LastDecisionByParty.TryGetValue(currentParty.PartyId, out previousDecision);

                var reasons = new List<string>();
                bool winnerChanged = false;
                string previousWinnerText = null;

                if (previousDecision != null)
                {
                    winnerChanged =
                        !string.Equals(previousDecision.TopBehavior, current.TopBehavior, StringComparison.Ordinal) ||
                        !string.Equals(previousDecision.TopTargetId, current.TopTargetId, StringComparison.Ordinal);

                    if (winnerChanged)
                    {
                        CandidateWinnerChanges++;
                        reasons.Add("VANILLA_WINNER");
                        previousWinnerText =
                            (previousDecision.TopBehavior ?? "<null>") + " -> " +
                            (previousDecision.TopTargetName ?? previousDecision.TopTargetId ?? "<null>") +
                            " [" + F(previousDecision.TopScore) + "]";
                    }
                }

                bool behaviorChanged =
                    previousParty != null && Different(previousParty.Behavior, currentParty.Behavior);
                if (behaviorChanged)
                {
                    reasons.Add("BEHAVIOR");
                    FinalBehaviorChanges++;
                }

                bool targetChanged =
                    previousParty != null &&
                    (Different(previousParty.TargetSettlementId, currentParty.TargetSettlementId) ||
                     Different(previousParty.TargetPartyId, currentParty.TargetPartyId));
                if (targetChanged)
                {
                    reasons.Add("TARGET");
                    FinalTargetChanges++;
                }

                bool armyChanged =
                    previousParty != null && Different(previousParty.ArmyId, currentParty.ArmyId);
                if (armyChanged)
                {
                    reasons.Add("ARMY");
                    ArmyStateChanges++;
                }

                bool attachmentChanged =
                    previousParty != null && Different(previousParty.AttachedToId, currentParty.AttachedToId);
                if (attachmentChanged)
                {
                    reasons.Add("ATTACHMENT");
                    AttachmentStateChanges++;
                }

                bool settlementChanged =
                    previousParty != null && Different(previousParty.CurrentSettlementId, currentParty.CurrentSettlementId);
                if (settlementChanged)
                {
                    reasons.Add("CURRENT_SETTLEMENT");
                    SettlementStateChanges++;
                }

                // Seed the optional outer-postfix context when that boundary fires,
                // but never require it for collection.
                _pendingDecision = current;
                _pendingDecisionWinnerChanged = winnerChanged;
                _pendingPreviousWinner = previousWinnerText;

                LastDecisionByParty[currentParty.PartyId] = CloneDecisionHead(current);
                LastFinalStateByParty[currentParty.PartyId] = currentParty;

                bool finalStateChanged =
                    behaviorChanged || targetChanged || armyChanged || attachmentChanged || settlementChanged;

                bool shouldCapture =
                    (CaptureCandidateWinnerChange && winnerChanged) ||
                    (CaptureFinalStateChange && finalStateChanged);

                if (shouldCapture)
                    CaptureEvent(party, previousParty, currentParty, current, reasons);
            }
            catch (Exception ex)
            {
                WriteFailures++;
                InspectorLog.Error(
                    "BEHAVIOR DELTA RADIUS RECORDER v" + Version +
                    " dispatcher decision capture failed.", ex);
            }
            finally
            {
                MaybeWriteStatus();
            }
        }

        public static void AfterPartyThink(MobileParty party)
        {
            if (!Enabled || _disabledByError || party == null)
            {
                ClearThreadState();
                return;
            }

            if (string.IsNullOrEmpty(_beforePartyId) ||
                !string.Equals(_beforePartyId, party.StringId, StringComparison.Ordinal))
            {
                ClearThreadState();
                return;
            }

            try
            {
                PartyState after = SnapshotPartyState(party);
                if (after == null)
                    return;

                PartyState lastFinal;
                LastFinalStateByParty.TryGetValue(after.PartyId, out lastFinal);

                var reasons = new List<string>();

                bool behaviorChanged =
                    Different(_beforeThink == null ? null : _beforeThink.Behavior, after.Behavior) ||
                    (lastFinal != null && Different(lastFinal.Behavior, after.Behavior));
                if (behaviorChanged)
                {
                    reasons.Add("BEHAVIOR");
                    FinalBehaviorChanges++;
                }

                bool targetChanged =
                    Different(_beforeThink == null ? null : _beforeThink.TargetSettlementId, after.TargetSettlementId) ||
                    Different(_beforeThink == null ? null : _beforeThink.TargetPartyId, after.TargetPartyId) ||
                    (lastFinal != null &&
                     (Different(lastFinal.TargetSettlementId, after.TargetSettlementId) ||
                      Different(lastFinal.TargetPartyId, after.TargetPartyId)));
                if (targetChanged)
                {
                    reasons.Add("TARGET");
                    FinalTargetChanges++;
                }

                bool armyChanged =
                    Different(_beforeThink == null ? null : _beforeThink.ArmyId, after.ArmyId) ||
                    (lastFinal != null && Different(lastFinal.ArmyId, after.ArmyId));
                if (armyChanged)
                {
                    reasons.Add("ARMY");
                    ArmyStateChanges++;
                }

                bool attachmentChanged =
                    Different(_beforeThink == null ? null : _beforeThink.AttachedToId, after.AttachedToId) ||
                    (lastFinal != null && Different(lastFinal.AttachedToId, after.AttachedToId));
                if (attachmentChanged)
                {
                    reasons.Add("ATTACHMENT");
                    AttachmentStateChanges++;
                }

                bool settlementChanged =
                    Different(_beforeThink == null ? null : _beforeThink.CurrentSettlementId, after.CurrentSettlementId) ||
                    (lastFinal != null && Different(lastFinal.CurrentSettlementId, after.CurrentSettlementId));
                if (settlementChanged)
                {
                    reasons.Add("CURRENT_SETTLEMENT");
                    SettlementStateChanges++;
                }

                bool finalStateChanged =
                    behaviorChanged || targetChanged || armyChanged || attachmentChanged || settlementChanged;

                // Candidate-winner changes are already captured at the reliable
                // dispatcher boundary. If the optional outer postfix also fires,
                // only emit a second event when vanilla actually changed final state.
                if (_pendingDecisionWinnerChanged && finalStateChanged)
                    reasons.Add("VANILLA_WINNER");

                LastFinalStateByParty[after.PartyId] = after;

                bool shouldCapture = CaptureFinalStateChange && finalStateChanged;

                if (!shouldCapture)
                    return;

                CaptureEvent(party, _beforeThink, after, _pendingDecision, reasons);
            }
            catch (Exception ex)
            {
                WriteFailures++;
                InspectorLog.Error(
                    "BEHAVIOR DELTA RADIUS RECORDER v" + Version +
                    " end-of-think capture failed.", ex);
            }
            finally
            {
                ClearThreadState();
            }
        }

        private static void CaptureEvent(
            MobileParty anchor,
            PartyState before,
            PartyState after,
            DecisionState decision,
            List<string> reasons)
        {
            long seq = ++_eventSequence;
            string eventId = SessionId + "_E" + seq.ToString("D7", CultureInfo.InvariantCulture);
            string realTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
            string campaignTime = SafeCampaignTime();
            string reason = reasons == null || reasons.Count == 0
                ? "UNKNOWN"
                : string.Join("+", reasons.Distinct().ToArray());

            LastEventId = eventId;
            LastEventPartyId = after == null ? null : after.PartyId;
            LastEventPartyName = after == null ? null : after.PartyName;
            LastEventReason = reason;
            LastEventCampaignTime = campaignTime;
            LastEventRealTime = realTime;

            var eventRow = new[]
            {
                eventId,
                realTime,
                campaignTime,
                reason,
                after == null ? null : after.PartyId,
                after == null ? null : after.PartyName,
                after == null ? null : after.LeaderId,
                after == null ? null : after.LeaderName,
                after == null ? null : after.ClanId,
                after == null ? null : after.ClanName,
                after == null ? null : after.KingdomId,
                after == null ? null : after.KingdomName,
                before == null ? null : before.Behavior,
                after == null ? null : after.Behavior,
                before == null ? null : before.TargetSettlementId,
                before == null ? null : before.TargetSettlementName,
                after == null ? null : after.TargetSettlementId,
                after == null ? null : after.TargetSettlementName,
                before == null ? null : before.TargetPartyId,
                before == null ? null : before.TargetPartyName,
                after == null ? null : after.TargetPartyId,
                after == null ? null : after.TargetPartyName,
                before == null ? null : before.ArmyId,
                after == null ? null : after.ArmyId,
                before == null ? null : before.AttachedToId,
                after == null ? null : after.AttachedToId,
                before == null ? null : before.CurrentSettlementId,
                after == null ? null : after.CurrentSettlementId,
                _pendingPreviousWinner,
                decision == null ? null : decision.TopBehavior,
                decision == null ? null : decision.TopTargetId,
                decision == null ? null : decision.TopTargetName,
                decision == null ? null : F(decision.TopScore),
                decision == null ? null : decision.CandidateCount.ToString(CultureInfo.InvariantCulture),
                decision == null ? null : decision.CurrentObjectiveValue,
                decision == null ? null : decision.WillGatherAnArmy,
                decision == null ? null : decision.DoNotChangeBehavior,
                after == null ? null : F(after.X),
                after == null ? null : F(after.Y)
            };

            AppendCsv(Path.Combine(SessionDirectory, "behavior_changes.csv"), eventRow);

            if (decision != null && decision.Candidates != null)
            {
                int rank = 0;
                foreach (DecisionCandidate c in decision.Candidates
                    .OrderByDescending(x => x.Score)
                    .ThenBy(x => x.OriginalIndex)
                    .Take(Math.Max(1, TopCandidates)))
                {
                    rank++;
                    AppendCsv(
                        Path.Combine(SessionDirectory, "decision_candidates.csv"),
                        new[]
                        {
                            eventId,
                            rank.ToString(CultureInfo.InvariantCulture),
                            c.OriginalIndex.ToString(CultureInfo.InvariantCulture),
                            F(c.Score),
                            c.Behavior,
                            c.TargetId,
                            c.TargetName,
                            c.NavigationType,
                            c.IsFromPort,
                            c.IsTargetingPort,
                            c.WillGatherArmy
                        });
                    CandidateRowsWritten++;
                }
            }

            CaptureRadius(eventId, anchor, after);
            RadiusCaptures++;

            // Status is human-facing metadata, not evidence. Rewriting the
            // whole file on every AI event used to put avoidable disk I/O on
            // Bannerlord's campaign thread. Five-second freshness is ample.
            MaybeWriteStatus();

            InspectorLog.Info(
                "BEHAVIOR DELTA EVENT v" + Version +
                " event=" + eventId +
                " party=" + (after == null ? "<null>" : after.PartyName) +
                " partyId=" + (after == null ? "<null>" : after.PartyId) +
                " reason=" + reason +
                " winner=" + (decision == null ? "<none>" : decision.TopBehavior) +
                " winnerTarget=" + (decision == null ? "<none>" : decision.TopTargetName) +
                " output=" + SessionDirectory);
        }

        private static void CaptureRadius(string eventId, MobileParty anchor, PartyState anchorState)
        {
            if (anchor == null || anchorState == null)
                return;

            float ax = anchorState.X;
            float ay = anchorState.Y;
            float radius = Math.Max(0.1f, Radius);
            float radiusSq = radius * radius;

            var currentContext = new Dictionary<string, ContextEntityState>(StringComparer.Ordinal);

            var nearbyParties =
                new List<ContextEntityState>();

            var partySearch =
                MobileParty
                    .StartFindingLocatablesAroundPosition(
                        new Vec2(ax, ay),
                        radius);

            while (true)
            {
                MobileParty p =
                    MobileParty.FindNextLocatable(
                        ref partySearch);

                if (p == null)
                    break;

                float x;
                float y;

                if (!TryPosition(p, out x, out y))
                    continue;

                float dx = x - ax;
                float dy = y - ay;
                float d2 = dx * dx + dy * dy;

                if (d2 > radiusSq)
                    continue;

                float distance =
                    (float)Math.Sqrt(d2);

                ContextEntityState state =
                    BuildPartyContextState(
                        p,
                        distance);

                if (state != null)
                    nearbyParties.Add(state);
            }

            foreach (ContextEntityState state in nearbyParties
                .OrderBy(x => x.Distance)
                .ThenBy(x => x.EntityId)
                .Take(Math.Max(1, MaxNearbyParties)))
            {
                currentContext[state.Key] = state;
                AppendCsv(
                    Path.Combine(SessionDirectory, "radius_parties.csv"),
                    new[] { eventId }.Concat(SplitPayload(state.CsvPayload)).ToArray());
                PartyRadiusRowsWritten++;
            }

            var nearbySettlements =
                new List<ContextEntityState>();

            var settlementSearch =
                Settlement
                    .StartFindingLocatablesAroundPosition(
                        new Vec2(ax, ay),
                        radius);

            while (true)
            {
                Settlement s =
                    Settlement.FindNextLocatable(
                        ref settlementSearch);

                if (s == null)
                    break;

                float x;
                float y;

                if (!TryPosition(s, out x, out y))
                    continue;

                float dx = x - ax;
                float dy = y - ay;
                float d2 = dx * dx + dy * dy;

                if (d2 > radiusSq)
                    continue;

                float distance =
                    (float)Math.Sqrt(d2);

                ContextEntityState state =
                    BuildSettlementContextState(
                        s,
                        distance);

                if (state != null)
                    nearbySettlements.Add(state);
            }

            foreach (ContextEntityState state in nearbySettlements
                .OrderBy(x => x.Distance)
                .ThenBy(x => x.EntityId)
                .Take(Math.Max(1, MaxNearbySettlements)))
            {
                currentContext[state.Key] = state;
                AppendCsv(
                    Path.Combine(SessionDirectory, "radius_settlements.csv"),
                    new[] { eventId }.Concat(SplitPayload(state.CsvPayload)).ToArray());
                SettlementRadiusRowsWritten++;
            }

            Dictionary<string, ContextEntityState> previous;
            LastRadiusByAnchorParty.TryGetValue(anchorState.PartyId, out previous);
            WriteContextDeltas(eventId, anchorState.PartyId, previous, currentContext);
            LastRadiusByAnchorParty[anchorState.PartyId] = currentContext;
        }

        private static void WriteContextDeltas(
            string eventId,
            string anchorPartyId,
            Dictionary<string, ContextEntityState> previous,
            Dictionary<string, ContextEntityState> current)
        {
            if (current == null)
                return;

            if (previous == null)
            {
                AppendCsv(
                    Path.Combine(SessionDirectory, "radius_deltas.csv"),
                    new[]
                    {
                        eventId, anchorPartyId, "BASELINE", "<radius>",
                        "<first local capture for this anchor party>", "", ""
                    });
                RadiusDeltaRowsWritten++;
                return;
            }

            foreach (KeyValuePair<string, ContextEntityState> kv in current)
            {
                ContextEntityState oldState;
                if (!previous.TryGetValue(kv.Key, out oldState))
                {
                    AppendContextDelta(eventId, anchorPartyId, "ENTERED_RADIUS", null, kv.Value);
                }
                else if (!string.Equals(oldState.Signature, kv.Value.Signature, StringComparison.Ordinal))
                {
                    AppendContextDelta(eventId, anchorPartyId, "STATE_CHANGED", oldState, kv.Value);
                }
            }

            foreach (KeyValuePair<string, ContextEntityState> kv in previous)
            {
                if (!current.ContainsKey(kv.Key))
                    AppendContextDelta(eventId, anchorPartyId, "LEFT_RADIUS", kv.Value, null);
            }
        }

        private static void AppendContextDelta(
            string eventId,
            string anchorPartyId,
            string change,
            ContextEntityState before,
            ContextEntityState after)
        {
            ContextEntityState state = after ?? before;
            AppendCsv(
                Path.Combine(SessionDirectory, "radius_deltas.csv"),
                new[]
                {
                    eventId,
                    anchorPartyId,
                    change,
                    state == null ? null : state.Key,
                    state == null ? null : state.EntityName,
                    before == null ? null : before.Signature,
                    after == null ? null : after.Signature
                });
            RadiusDeltaRowsWritten++;
        }

        private static ContextEntityState BuildPartyContextState(MobileParty p, float distance)
        {
            if (p == null)
                return null;

            PartyState s = SnapshotPartyState(p);
            if (s == null)
                return null;

            object partyBase = ReadMember(p, "Party");
            float strength = ToFloat(ReadMember(partyBase, "TotalStrength"));
            object roster = ReadMember(p, "MemberRoster");
            int men = ToInt(ReadMember(roster, "TotalManCount"));
            if (men <= 0)
                men = ToInt(ReadMember(partyBase, "NumberOfAllMembers"));

            string signature = string.Join("|", new[]
            {
                s.Behavior ?? "",
                s.TargetSettlementId ?? "",
                s.TargetPartyId ?? "",
                s.CurrentSettlementId ?? "",
                s.ArmyId ?? "",
                s.AttachedToId ?? "",
                men.ToString(CultureInfo.InvariantCulture),
                F(strength)
            });

            string payload = JoinPayload(new[]
            {
                "PARTY",
                p.StringId,
                SafeName(p),
                F(distance),
                s.LeaderId,
                s.LeaderName,
                s.ClanId,
                s.ClanName,
                s.KingdomId,
                s.KingdomName,
                s.Behavior,
                s.TargetSettlementId,
                s.TargetSettlementName,
                s.TargetPartyId,
                s.TargetPartyName,
                s.CurrentSettlementId,
                s.CurrentSettlementName,
                s.ArmyId,
                s.ArmyName,
                s.AttachedToId,
                s.AttachedToName,
                men.ToString(CultureInfo.InvariantCulture),
                F(strength),
                BoolText(s.InMapEvent),
                BoolText(s.IsDisbanding),
                F(s.X),
                F(s.Y)
            });

            return new ContextEntityState
            {
                Key = "P:" + (p.StringId ?? SafeName(p) ?? Guid.NewGuid().ToString("N")),
                EntityType = "PARTY",
                EntityId = p.StringId,
                EntityName = SafeName(p),
                Distance = distance,
                Signature = signature,
                CsvPayload = payload
            };
        }

        private static ContextEntityState BuildSettlementContextState(Settlement s, float distance)
        {
            if (s == null)
                return null;

            Clan owner = ReadMember(s, "OwnerClan") as Clan;
            object town = ReadMember(s, "Town");
            object village = ReadMember(s, "Village");

            string type = Bool(ReadMember(s, "IsTown")) ? "Town" :
                          Bool(ReadMember(s, "IsCastle")) ? "Castle" :
                          Bool(ReadMember(s, "IsVillage")) ? "Village" : "Settlement";

            bool underRaid = Bool(ReadMember(s, "IsUnderRaid"));
            bool underSiege = Bool(ReadMember(s, "IsUnderSiege"));
            float prosperity = ToFloat(ReadMember(town, "Prosperity"));
            float food = ToFloat(ReadMember(town, "FoodStocks"));
            if (Math.Abs(food) < 0.000001f)
                food = ToFloat(ReadMember(town, "Food"));
            float security = ToFloat(ReadMember(town, "Security"));
            float loyalty = ToFloat(ReadMember(town, "Loyalty"));
            float militia = ToFloat(ReadMember(town, "Militia"));
            float hearth = ToFloat(ReadMember(village, "Hearth"));
            if (Math.Abs(hearth) < 0.000001f)
                hearth = ToFloat(ReadMember(village, "Hearths"));

            object garrison = ReadMember(town, "GarrisonParty");
            object garrisonPartyBase = ReadMember(garrison, "Party");
            float garrisonStrength = ToFloat(ReadMember(garrisonPartyBase, "TotalStrength"));
            int garrisonMen = ToInt(ReadMember(ReadMember(garrison, "MemberRoster"), "TotalManCount"));

            float x;
            float y;
            TryPosition(s, out x, out y);

            string ownerId = owner == null ? null : owner.StringId;
            string ownerName = owner == null ? null : SafeName(owner);
            Kingdom kingdom = owner == null ? null : ReadMember(owner, "Kingdom") as Kingdom;
            string kingdomId = kingdom == null ? null : kingdom.StringId;
            string kingdomName = kingdom == null ? null : SafeName(kingdom);

            string signature = string.Join("|", new[]
            {
                ownerId ?? "",
                BoolText(underRaid),
                BoolText(underSiege),
                garrisonMen.ToString(CultureInfo.InvariantCulture),
                F(garrisonStrength),
                F(prosperity),
                F(food),
                F(security),
                F(loyalty),
                F(militia),
                F(hearth)
            });

            string payload = JoinPayload(new[]
            {
                "SETTLEMENT",
                s.StringId,
                SafeName(s),
                F(distance),
                type,
                ownerId,
                ownerName,
                kingdomId,
                kingdomName,
                BoolText(underRaid),
                BoolText(underSiege),
                garrisonMen.ToString(CultureInfo.InvariantCulture),
                F(garrisonStrength),
                F(prosperity),
                F(food),
                F(security),
                F(loyalty),
                F(militia),
                F(hearth),
                F(x),
                F(y)
            });

            return new ContextEntityState
            {
                Key = "S:" + (s.StringId ?? SafeName(s) ?? Guid.NewGuid().ToString("N")),
                EntityType = "SETTLEMENT",
                EntityId = s.StringId,
                EntityName = SafeName(s),
                Distance = distance,
                Signature = signature,
                CsvPayload = payload
            };
        }

        private static PartyState SnapshotPartyState(MobileParty party)
        {
            if (party == null)
                return null;

            Clan clan = party.ActualClan;
            Hero leader = party.LeaderHero;
            Kingdom kingdom = clan == null ? null : ReadMember(clan, "Kingdom") as Kingdom;

            object targetSettlement = ReadMember(party, "TargetSettlement");
            object targetParty = ReadMember(party, "TargetParty");
            object currentSettlement = ReadMember(party, "CurrentSettlement");
            object army = ReadMember(party, "Army");
            object attached = ReadMember(party, "AttachedTo");

            float x;
            float y;
            TryPosition(party, out x, out y);

            return new PartyState
            {
                PartyId = party.StringId,
                PartyName = SafeName(party),
                LeaderId = leader == null ? null : leader.StringId,
                LeaderName = leader == null ? null : SafeName(leader),
                ClanId = clan == null ? null : clan.StringId,
                ClanName = clan == null ? null : SafeName(clan),
                KingdomId = kingdom == null ? null : kingdom.StringId,
                KingdomName = kingdom == null ? null : SafeName(kingdom),
                Behavior = Text(ReadMember(party, "DefaultBehavior")),
                TargetSettlementId = EntityId(targetSettlement),
                TargetSettlementName = SafeName(targetSettlement),
                TargetPartyId = EntityId(targetParty),
                TargetPartyName = SafeName(targetParty),
                CurrentSettlementId = EntityId(currentSettlement),
                CurrentSettlementName = SafeName(currentSettlement),
                ArmyId = EntityId(army),
                ArmyName = SafeName(army),
                AttachedToId = EntityId(attached),
                AttachedToName = SafeName(attached),
                InMapEvent = Bool(ReadMember(party, "InMapEvent")),
                IsDisbanding = Bool(ReadMember(party, "IsDisbanding")),
                X = x,
                Y = y
            };
        }

        private static DecisionState SnapshotDecision(PartyThinkParams think)
        {
            object scores = ReadMember(think, "AIBehaviorScores");
            IEnumerable enumerable = scores as IEnumerable;
            if (enumerable == null)
                return null;

            var candidates = new List<DecisionCandidate>();
            int index = 0;

            foreach (object item in enumerable)
            {
                object data = ReadMember(item, "Item1");
                if (data == null)
                {
                    index++;
                    continue;
                }

                object target = ReadMember(data, "Party");
                candidates.Add(new DecisionCandidate
                {
                    OriginalIndex = index,
                    Score = ToFloat(ReadMember(item, "Item2")),
                    Behavior = Text(ReadMember(data, "AiBehavior")),
                    TargetId = EntityId(target),
                    TargetName = SafeName(target),
                    NavigationType = Text(ReadMember(data, "NavigationType")),
                    IsFromPort = Text(ReadMember(data, "IsFromPort")),
                    IsTargetingPort = Text(ReadMember(data, "IsTargetingPort")),
                    WillGatherArmy = Text(ReadMember(data, "WillGatherArmy"))
                });
                index++;
            }

            DecisionCandidate top = candidates
                .OrderByDescending(x => x.Score)
                .ThenBy(x => x.OriginalIndex)
                .FirstOrDefault();

            return new DecisionState
            {
                TopBehavior = top == null ? null : top.Behavior,
                TopTargetId = top == null ? null : top.TargetId,
                TopTargetName = top == null ? null : top.TargetName,
                TopScore = top == null ? 0f : top.Score,
                CandidateCount = candidates.Count,
                CurrentObjectiveValue = Text(ReadMember(think, "CurrentObjectiveValue")),
                WillGatherAnArmy = Text(ReadMember(think, "WillGatherAnArmy")),
                DoNotChangeBehavior = Text(ReadMember(think, "DoNotChangeBehavior")),
                Candidates = candidates
            };
        }

        private static DecisionState CloneDecisionHead(DecisionState source)
        {
            if (source == null)
                return null;

            return new DecisionState
            {
                TopBehavior = source.TopBehavior,
                TopTargetId = source.TopTargetId,
                TopTargetName = source.TopTargetName,
                TopScore = source.TopScore,
                CandidateCount = source.CandidateCount,
                CurrentObjectiveValue = source.CurrentObjectiveValue,
                WillGatherAnArmy = source.WillGatherAnArmy,
                DoNotChangeBehavior = source.DoNotChangeBehavior
            };
        }

        private static bool IsEligibleParty(MobileParty party)
        {
            if (party == null || party.LeaderHero == null || party.ActualClan == null)
                return false;

            if (Bool(ReadMember(party, "IsMainParty")))
                return false;

            Kingdom kingdom = ReadMember(party.ActualClan, "Kingdom") as Kingdom;
            return kingdom != null;
        }

        private static void EnsureDefaultConfig()
        {
            if (File.Exists(ConfigPath))
                return;

            var sb = new StringBuilder();
            sb.AppendLine("# Behavior Delta / Radius Recorder live config");
            sb.AppendLine("# Edit while Bannerlord is running; recorder reloads automatically.");
            sb.AppendLine("Enabled=true");
            sb.AppendLine("Radius=15");
            sb.AppendLine("TopCandidates=12");
            sb.AppendLine("MaxNearbyParties=24");
            sb.AppendLine("MaxNearbySettlements=18");
            sb.AppendLine("CaptureCandidateWinnerChange=true");
            sb.AppendLine("CaptureFinalStateChange=true");
            File.WriteAllText(ConfigPath, sb.ToString(), Utf8);
        }

        private static void ReloadConfig(bool force)
        {
            if (_disabledByError)
                return;

            DateTime now = DateTime.UtcNow;
            if (!force && (now - _lastConfigCheckUtc).TotalSeconds < 2.0)
                return;

            _lastConfigCheckUtc = now;

            try
            {
                if (!File.Exists(ConfigPath))
                {
                    EnsureDefaultConfig();
                    return;
                }

                DateTime write = File.GetLastWriteTimeUtc(ConfigPath);
                if (!force && write == _lastConfigWriteUtc)
                    return;

                _lastConfigWriteUtc = write;
                RecorderConfig cfg = ParseConfig(File.ReadAllLines(ConfigPath));
                Enabled = cfg.Enabled;
                Radius = Math.Max(0.1f, cfg.Radius);
                TopCandidates = Clamp(cfg.TopCandidates, 1, 50);
                MaxNearbyParties = Clamp(cfg.MaxNearbyParties, 1, 200);
                MaxNearbySettlements = Clamp(cfg.MaxNearbySettlements, 1, 100);
                CaptureCandidateWinnerChange = cfg.CaptureCandidateWinnerChange;
                CaptureFinalStateChange = cfg.CaptureFinalStateChange;

                if (_initialized)
                {
                    InspectorLog.Info(
                        "BEHAVIOR DELTA RADIUS RECORDER v" + Version +
                        " config reloaded Enabled=" + Enabled +
                        " Radius=" + F(Radius) +
                        " TopCandidates=" + TopCandidates +
                        " MaxNearbyParties=" + MaxNearbyParties +
                        " MaxNearbySettlements=" + MaxNearbySettlements +
                        " CaptureCandidateWinnerChange=" + CaptureCandidateWinnerChange +
                        " CaptureFinalStateChange=" + CaptureFinalStateChange);
                }
            }
            catch (Exception ex)
            {
                WriteFailures++;
                InspectorLog.Error(
                    "BEHAVIOR DELTA RADIUS RECORDER v" + Version +
                    " config reload failed; keeping previous settings.", ex);
            }
        }

        private static RecorderConfig ParseConfig(string[] lines)
        {
            var cfg = new RecorderConfig();
            if (lines == null)
                return cfg;

            foreach (string raw in lines)
            {
                string line = raw == null ? "" : raw.Trim();
                if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
                    continue;

                int equals = line.IndexOf('=');
                if (equals <= 0)
                    continue;

                string key = line.Substring(0, equals).Trim();
                string value = line.Substring(equals + 1).Trim();
                bool b;
                int i;
                float f;

                if (key.Equals("Enabled", StringComparison.OrdinalIgnoreCase) && bool.TryParse(value, out b)) cfg.Enabled = b;
                else if (key.Equals("Radius", StringComparison.OrdinalIgnoreCase) && float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out f)) cfg.Radius = f;
                else if (key.Equals("TopCandidates", StringComparison.OrdinalIgnoreCase) && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out i)) cfg.TopCandidates = i;
                else if (key.Equals("MaxNearbyParties", StringComparison.OrdinalIgnoreCase) && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out i)) cfg.MaxNearbyParties = i;
                else if (key.Equals("MaxNearbySettlements", StringComparison.OrdinalIgnoreCase) && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out i)) cfg.MaxNearbySettlements = i;
                else if (key.Equals("CaptureCandidateWinnerChange", StringComparison.OrdinalIgnoreCase) && bool.TryParse(value, out b)) cfg.CaptureCandidateWinnerChange = b;
                else if (key.Equals("CaptureFinalStateChange", StringComparison.OrdinalIgnoreCase) && bool.TryParse(value, out b)) cfg.CaptureFinalStateChange = b;
            }

            return cfg;
        }

        private static void WriteHeaders()
        {
            WriteHeader(
                "behavior_changes.csv",
                new[]
                {
                    "eventId","realTime","campaignTime","reason","partyId","partyName","leaderId","leaderName",
                    "clanId","clanName","kingdomId","kingdomName","beforeBehavior","afterBehavior",
                    "beforeTargetSettlementId","beforeTargetSettlementName","afterTargetSettlementId","afterTargetSettlementName",
                    "beforeTargetPartyId","beforeTargetPartyName","afterTargetPartyId","afterTargetPartyName",
                    "beforeArmyId","afterArmyId","beforeAttachedToId","afterAttachedToId","beforeCurrentSettlementId","afterCurrentSettlementId",
                    "previousVanillaWinner","currentVanillaWinnerBehavior","currentVanillaWinnerTargetId","currentVanillaWinnerTargetName",
                    "currentVanillaWinnerScore","candidateCount","currentObjectiveValue","willGatherAnArmy","doNotChangeBehavior","x","y"
                });

            WriteHeader(
                "decision_candidates.csv",
                new[] { "eventId","rank","originalIndex","score","behavior","targetId","targetName","navigationType","isFromPort","isTargetingPort","willGatherArmy" });

            WriteHeader(
                "radius_parties.csv",
                new[]
                {
                    "eventId","entityType","partyId","partyName","distance","leaderId","leaderName","clanId","clanName",
                    "kingdomId","kingdomName","behavior","targetSettlementId","targetSettlementName","targetPartyId","targetPartyName",
                    "currentSettlementId","currentSettlementName","armyId","armyName","attachedToId","attachedToName","men","strength",
                    "inMapEvent","isDisbanding","x","y"
                });

            WriteHeader(
                "radius_settlements.csv",
                new[]
                {
                    "eventId","entityType","settlementId","settlementName","distance","settlementType","ownerClanId","ownerClanName",
                    "kingdomId","kingdomName","underRaid","underSiege","garrisonMen","garrisonStrength","prosperity","food",
                    "security","loyalty","militia","hearth","x","y"
                });

            WriteHeader(
                "radius_deltas.csv",
                new[] { "eventId","anchorPartyId","changeType","entityKey","entityName","previousSignature","currentSignature" });
        }

        private static void WriteHeader(string fileName, string[] columns)
        {
            string path = Path.Combine(SessionDirectory, fileName);
            if (!File.Exists(path))
                File.WriteAllText(path, CsvLine(columns) + Environment.NewLine, Utf8);
        }

        private static void WriteSessionMeta()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Behavior Delta / Radius Recorder");
            sb.AppendLine("Version: " + Version);
            sb.AppendLine("SessionId: " + SessionId);
            sb.AppendLine("ProcessId: " + Process.GetCurrentProcess().Id.ToString(CultureInfo.InvariantCulture));
            sb.AppendLine("StartedRealTime: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture));
            sb.AppendLine("StartedCampaignTime: " + SafeCampaignTime());
            sb.AppendLine("ReadOnly: True");
            sb.AppendLine("ConfigPath: " + ConfigPath);
            sb.AppendLine("SessionDirectory: " + SessionDirectory);
            sb.AppendLine("Method: CampaignEventDispatcher.AiHourlyTick primary sentinel; local radius deep capture only on vanilla decision/final-state deltas; outer PartyHourlyAiTick is supplemental only.");
            File.WriteAllText(Path.Combine(SessionDirectory, "session_meta.txt"), sb.ToString(), Utf8);
        }

        private static void MaybeWriteStatus()
        {
            DateTime now = DateTime.UtcNow;
            if ((now - _lastStatusWriteUtc).TotalSeconds < 5.0)
                return;

            _lastStatusWriteUtc = now;
            WriteStatus();
        }

        private static void WriteStatus()
        {
            if (string.IsNullOrEmpty(SessionDirectory))
                return;

            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("BehaviorDeltaRadiusRecorder v" + Version);
                sb.AppendLine("SessionId: " + (SessionId ?? "<null>"));
                sb.AppendLine("Enabled: " + Enabled);
                sb.AppendLine("Radius: " + F(Radius));
                sb.AppendLine("ObservedPartyThinks: " + ObservedPartyThinks.ToString(CultureInfo.InvariantCulture));
                sb.AppendLine("EligiblePartyThinks: " + EligiblePartyThinks.ToString(CultureInfo.InvariantCulture));
                sb.AppendLine("CandidateWinnerChanges: " + CandidateWinnerChanges.ToString(CultureInfo.InvariantCulture));
                sb.AppendLine("FinalBehaviorChanges: " + FinalBehaviorChanges.ToString(CultureInfo.InvariantCulture));
                sb.AppendLine("FinalTargetChanges: " + FinalTargetChanges.ToString(CultureInfo.InvariantCulture));
                sb.AppendLine("ArmyStateChanges: " + ArmyStateChanges.ToString(CultureInfo.InvariantCulture));
                sb.AppendLine("AttachmentStateChanges: " + AttachmentStateChanges.ToString(CultureInfo.InvariantCulture));
                sb.AppendLine("SettlementStateChanges: " + SettlementStateChanges.ToString(CultureInfo.InvariantCulture));
                sb.AppendLine("RadiusCaptures: " + RadiusCaptures.ToString(CultureInfo.InvariantCulture));
                sb.AppendLine("CandidateRowsWritten: " + CandidateRowsWritten.ToString(CultureInfo.InvariantCulture));
                sb.AppendLine("PartyRadiusRowsWritten: " + PartyRadiusRowsWritten.ToString(CultureInfo.InvariantCulture));
                sb.AppendLine("SettlementRadiusRowsWritten: " + SettlementRadiusRowsWritten.ToString(CultureInfo.InvariantCulture));
                sb.AppendLine("RadiusDeltaRowsWritten: " + RadiusDeltaRowsWritten.ToString(CultureInfo.InvariantCulture));
                sb.AppendLine("WriteFailures: " + WriteFailures.ToString(CultureInfo.InvariantCulture));
                sb.AppendLine("LastObservationCampaignTime: " + (LastObservationCampaignTime ?? "<null>"));
                sb.AppendLine("LastObservationRealTime: " + (LastObservationRealTime ?? "<null>"));
                sb.AppendLine("LastEventId: " + (LastEventId ?? "<null>"));
                sb.AppendLine("LastEventParty: " + (LastEventPartyName ?? "<null>") + " [" + (LastEventPartyId ?? "<null>") + "]");
                sb.AppendLine("LastEventReason: " + (LastEventReason ?? "<null>"));
                sb.AppendLine("LastEventCampaignTime: " + (LastEventCampaignTime ?? "<null>"));
                sb.AppendLine("LastEventRealTime: " + (LastEventRealTime ?? "<null>"));
                File.WriteAllText(Path.Combine(SessionDirectory, "status.txt"), sb.ToString(), Utf8);
            }
            catch
            {
                WriteFailures++;
            }
        }

        private static void AppendCsv(string path, string[] fields)
        {
            try
            {
                EvidenceWriter.AppendLine(
                    path,
                    CsvLine(fields));
            }
            catch (Exception ex)
            {
                WriteFailures++;
                InspectorLog.Error(
                    "BEHAVIOR DELTA RADIUS RECORDER v" + Version +
                    " CSV write failed path=" + path, ex);
            }
        }

        private static string CsvLine(IEnumerable<string> fields)
        {
            return string.Join(",", (fields ?? Enumerable.Empty<string>()).Select(Csv));
        }

        private static string Csv(string value)
        {
            if (value == null)
                return "";

            bool quote = value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0;
            string escaped = value.Replace("\"", "\"\"");
            return quote ? "\"" + escaped + "\"" : escaped;
        }

        // Payload is only an internal separator-safe transport between builders and CSV writer.
        private const char PayloadSeparator = '\u001F';

        private static string JoinPayload(string[] values)
        {
            return string.Join(PayloadSeparator.ToString(), values ?? new string[0]);
        }

        private static string[] SplitPayload(string payload)
        {
            return (payload ?? "").Split(new[] { PayloadSeparator }, StringSplitOptions.None);
        }

        private static bool TryPosition(object entity, out float x, out float y)
        {
            x = 0f;
            y = 0f;
            if (entity == null)
                return false;

            object pos = ReadMember(entity, "Position2D");
            if (pos == null)
                pos = ReadMember(entity, "Position");
            if (pos == null)
                return false;

            object xo = ReadMember(pos, "X");
            object yo = ReadMember(pos, "Y");
            if (xo == null || yo == null)
                return false;

            x = ToFloat(xo);
            y = ToFloat(yo);
            return true;
        }

        private static IEnumerable GetStaticEnumerable(Type type, string name)
        {
            if (type == null || string.IsNullOrEmpty(name))
                return null;

            try
            {
                BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
                PropertyInfo p = type.GetProperty(name, flags);
                if (p != null)
                    return p.GetValue(null, null) as IEnumerable;

                FieldInfo f = type.GetField(name, flags);
                return f == null ? null : f.GetValue(null) as IEnumerable;
            }
            catch
            {
                return null;
            }
        }

        private static object ReadMember(object instance, string name)
        {
            if (instance == null || string.IsNullOrEmpty(name))
                return null;

            try
            {
                Type type = instance.GetType();
                BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

                PropertyInfo p = type.GetProperty(name, flags);
                if (p != null && p.GetIndexParameters().Length == 0)
                    return p.GetValue(instance, null);

                FieldInfo f = type.GetField(name, flags);
                if (f != null)
                    return f.GetValue(instance);
            }
            catch
            {
            }

            return null;
        }

        private static string EntityId(object value)
        {
            if (value == null)
                return null;

            string id = Text(ReadMember(value, "StringId"));
            if (!string.IsNullOrEmpty(id))
                return id;

            object leaderParty = ReadMember(value, "LeaderParty");
            id = Text(ReadMember(leaderParty, "StringId"));
            if (!string.IsNullOrEmpty(id))
                return id;

            return null;
        }

        private static string SafeName(object value)
        {
            if (value == null)
                return null;

            try
            {
                object name = ReadMember(value, "Name");
                if (name != null)
                    return name.ToString();
            }
            catch
            {
            }

            try
            {
                return value.ToString();
            }
            catch
            {
                return null;
            }
        }

        private static string SafeCampaignTime()
        {
            try
            {
                return CampaignTime.Now.ToString();
            }
            catch
            {
                return "<unavailable>";
            }
        }

        private static string Text(object value)
        {
            return value == null ? null : value.ToString();
        }

        private static bool Bool(object value)
        {
            if (value == null)
                return false;
            if (value is bool)
                return (bool)value;

            bool parsed;
            return bool.TryParse(value.ToString(), out parsed) && parsed;
        }

        private static float ToFloat(object value)
        {
            if (value == null)
                return 0f;
            if (value is float)
                return (float)value;
            if (value is double)
                return (float)(double)value;

            float result;
            return float.TryParse(value.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out result)
                ? result
                : 0f;
        }

        private static int ToInt(object value)
        {
            if (value == null)
                return 0;
            if (value is int)
                return (int)value;

            int result;
            return int.TryParse(value.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out result)
                ? result
                : 0;
        }

        private static bool Different(string a, string b)
        {
            return !string.Equals(a ?? "", b ?? "", StringComparison.Ordinal);
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        private static string F(float value)
        {
            return value.ToString("0.######", CultureInfo.InvariantCulture);
        }

        private static string BoolText(bool value)
        {
            return value ? "True" : "False";
        }

        private static void ClearThreadState()
        {
            _beforeThink = null;
            _beforePartyId = null;
            _pendingDecision = null;
            _pendingDecisionWinnerChanged = false;
            _pendingPreviousWinner = null;
        }
    }
}

