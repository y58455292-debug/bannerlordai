using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors.AiBehaviors;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace BannerlordInspector
{
    /// <summary>
    /// v0.4 read-only AI tracer.
    ///
    /// Instead of trying to hook internal helper methods, this version snapshots
    /// PartyThinkParams.AIBehaviorScores immediately before and after every
    /// AiBehaviors.*.AiHourlyTick(MobileParty, PartyThinkParams) subscriber.
    ///
    /// This lets us attribute score additions/changes to the actual vanilla behavior
    /// boundary even when TaleWorlds writes directly to the underlying list or a tiny
    /// helper is inlined/bypassed.
    ///
    /// Scope: one lab party only (Kanujan). No scores or campaign state are modified.
    /// </summary>
    public static class AiDecisionTrace
    {
        public const string Version = "0.32";
        public const string TargetPartyId = "__GLOBAL_SCANNER_NO_DETAIL__"; // detailed trace intentionally disabled

        private static bool _applied;
        private static long _traceNumber;
        private static long _transitionNumber;
        private static long _strategicCommitNumber;

        private static readonly object _globalCommitSync =
            new object();

        private static readonly Dictionary<string, long>
            _globalCommitCounts =
                new Dictionary<string, long>(
                    StringComparer.Ordinal);

        private static long _globalCommitCalls;
        private static long _globalLordCommitCalls;
        private static long _globalEngageCalls;
        private static long _globalLordEngageCalls;
        private static long _globalEngageBanditTargets;
        private static long _globalEngageDuringOwnThink;

        private static readonly object _shortTermSync =
            new object();

        private static readonly Dictionary<string, long>
            _shortTermBehaviorCounts =
                new Dictionary<string, long>(
                    StringComparer.Ordinal);

        private static long _shortTermCalls;
        private static long _shortTermLordCalls;
        private static long _shortTermMobileTargets;
        private static long _shortTermSettlementTargets;
        private static long _shortTermEngageCalls;
        private static long _shortTermLordEngageCalls;
        private static long _shortTermBanditTargets;
        private static long _shortTermEngageBanditTargets;
        private static long _shortTermDuringOwnThink;

        private static readonly object _defaultBehaviorSync =
            new object();

        private static readonly Dictionary<string, long>
            _defaultBehaviorCounts =
                new Dictionary<string, long>(
                    StringComparer.Ordinal);

        private static long _defaultBehaviorCalls;
        private static long _defaultBehaviorLordCalls;
        private static long _defaultBehaviorMobileTargets;
        private static long _defaultBehaviorSettlementTargets;
        private static long _defaultBehaviorEngageCalls;
        private static long _defaultBehaviorLordEngageCalls;
        private static long _defaultBehaviorBanditTargets;
        private static long _defaultBehaviorEngageBanditTargets;
        private static long _defaultBehaviorDuringOwnThink;

        [ThreadStatic] private static bool _insideAnyPartyThink;
        [ThreadStatic] private static MobileParty _currentThinkingParty;
        [ThreadStatic] private static bool _insideTargetThink;
        [ThreadStatic] private static string _beforeDefaultBehavior;
        [ThreadStatic] private static string _beforeTargetSettlement;
        [ThreadStatic] private static string _beforeTargetParty;
        [ThreadStatic] private static string _beforeShortTermBehavior;
        [ThreadStatic] private static string _beforeShortTermTargetSettlement;
        [ThreadStatic] private static string _beforeShortTermTargetParty;
        [ThreadStatic] private static PendingTrace _pending;
        [ThreadStatic] private static List<BoundaryDelta> _boundaryDeltas;
        [ThreadStatic] private static HashSet<string> _needProbeSeen;

        public sealed class Candidate
        {
            public int Index;
            public float Score;
            public string Key;
            public string Behavior;
            public string Target;
            public string Position;
            public string WillGatherArmy;
            public string NavigationType;
            public string IsFromPort;
            public string IsTargetingPort;
        }

        private sealed class PartyState
        {
            public string DefaultBehavior;
            public string TargetSettlement;
            public string TargetParty;
            public string ShortTermBehavior;
            public string ShortTermTargetSettlement;
            public string ShortTermTargetParty;
            public string MoveTargetParty;
            public string PartyMoveMode;
        }

        private sealed class TransitionState
        {
            public MobileParty Party;
            public PartyState Before;
            public string Caller;
            public string RequestedBehavior;
            public string RequestedTarget;
            public string RequestedPoint;
            public string RequestedAttackInitiative;
            public string RequestedAvoidInitiative;
            public string RequestedResetHours;
        }

        private sealed class ActionCommitState
        {
            public MobileParty Party;
            public PartyState Before;
            public string Caller;
            public string Detail;
            public string Settlement;
            public string MobilePartyTarget;
            public string Position;
            public string NavigationType;
            public string IsFromPort;
            public string IsTargetingPort;
            public string CandidateBehavior;
            public string CandidateTarget;
            public string MatchedScore;
            public string MatchedRank;
        }

        private sealed class PendingTrace
        {
            public string PartyName;
            public string PartyId;
            public string Leader;
            public string CurrentObjectiveValue;
            public string WillGatherAnArmy;
            public string DoNotChangeBehavior;
            public string StrengthWithoutArmy;
            public string StrengthWithArmy;
            public string StrengthSameClanWithoutArmy;
            public int CandidateCount;
            public List<Candidate> Candidates;
        }

        public sealed class BoundaryState
        {
            public string Source;
            public int BeforeCount;
            public Dictionary<string, Candidate> Before;
        }

        private sealed class DeltaCandidate
        {
            public string Kind; // ADDED / CHANGED / REMOVED
            public string Behavior;
            public string Target;
            public float BeforeScore;
            public float AfterScore;
        }

        private sealed class BoundaryDelta
        {
            public string Source;
            public int BeforeCount;
            public int AfterCount;
            public List<DeltaCandidate> Changes = new List<DeltaCandidate>();
        }

        public static void Apply(Harmony harmony)
        {
            if (_applied || harmony == null) return;

            try
            {
                MethodInfo think = AccessTools.Method(
                    typeof(AiPartyThinkBehavior),
                    "PartyHourlyAiTick",
                    new[] { typeof(MobileParty) });

                MethodInfo dispatch = AccessTools.Method(
                    typeof(CampaignEventDispatcher),
                    "AiHourlyTick",
                    new[] { typeof(MobileParty), typeof(PartyThinkParams) });

                if (think == null || dispatch == null)
                {
                    InspectorLog.Warn("AI TRACE v0.8: required core method not found; tracer not attached.");
                    return;
                }

                harmony.Patch(
                    think,
                    prefix: new HarmonyMethod(AccessTools.Method(typeof(AiDecisionTrace), nameof(BeforePartyThink))),
                    postfix: new HarmonyMethod(AccessTools.Method(typeof(AiDecisionTrace), nameof(AfterPartyThink))));

                harmony.Patch(
                    dispatch,
                    postfix: new HarmonyMethod(AccessTools.Method(typeof(AiDecisionTrace), nameof(AfterAiHourlyTick))));

                int boundaryCount = PatchAiHourlyTickBoundaries(harmony);
                int transitionCount = PatchStrategicVsShortTermTransitions(harmony);
                int strategicCommitCount = PatchStrategicCommitMethods(harmony);
                int actionCommitBoundaryCount = PatchSetPartyAiActionBoundary(harmony);
                int needProbeCount = PatchTerritoryNeedProbe(harmony);

                // v0.27: generalized, read-only behavior-delta / local-radius recorder.
                // It watches every hero-led kingdom party and only performs a deep
                // local capture when vanilla thinking or final strategic state changes.
                BehaviorDeltaRadiusRecorder.Initialize();

                _applied = true;
                InspectorLog.Info(
                    "AI TRACE v" + Version +
                    " attached. Target party: " + TargetPartyId +
                    " (global scanner mode; detailed single-party trace disabled); subscriber-boundary methods patched=" + boundaryCount +
                    "; strategic/short-term transition methods patched=" + transitionCount +
                    "; strategic commit methods patched=" + strategicCommitCount +
                    "; SetPartyAiAction.ApplyInternal patched=" + actionCommitBoundaryCount +
                    "; territory need probe patched=" + needProbeCount +
                    "; BehaviorDeltaRadiusRecorder=v0.2 READ-ONLY dispatcher-primary.");
            }
            catch (Exception ex)
            {
                InspectorLog.Error("AI TRACE v0.8 could not attach.", ex);
            }
        }

        private static int PatchAiHourlyTickBoundaries(Harmony harmony)
        {
            int patched = 0;
            Type[] types;

            try
            {
                types = typeof(AiPartyThinkBehavior).Assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types.Where(x => x != null).ToArray();
            }

            foreach (Type type in types)
            {
                if (type == null || string.IsNullOrEmpty(type.FullName)) continue;
                if (!type.FullName.StartsWith(
                    "TaleWorlds.CampaignSystem.CampaignBehaviors.AiBehaviors.",
                    StringComparison.Ordinal)) continue;

                foreach (MethodInfo method in type.GetMethods(
                    BindingFlags.Instance | BindingFlags.Static |
                    BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (!string.Equals(method.Name, "AiHourlyTick", StringComparison.Ordinal))
                        continue;

                    ParameterInfo[] p = method.GetParameters();
                    if (p.Length != 2) continue;
                    if (p[0].ParameterType != typeof(MobileParty)) continue;
                    if (p[1].ParameterType != typeof(PartyThinkParams)) continue;

                    try
                    {
                        harmony.Patch(
                            method,
                            prefix: new HarmonyMethod(AccessTools.Method(
                                typeof(AiDecisionTrace), nameof(BeforeBehaviorBoundary))),
                            postfix: new HarmonyMethod(AccessTools.Method(
                                typeof(AiDecisionTrace), nameof(AfterBehaviorBoundary))));

                        patched++;
                    }
                    catch (Exception ex)
                    {
                        InspectorLog.Warn(
                            "AI TRACE v0.8 boundary patch skipped for " +
                            type.FullName + "." + method.Name + ": " +
                            ex.GetType().Name + " - " + ex.Message);
                    }
                }
            }

            return patched;
        }

        private static int PatchTerritoryNeedProbe(Harmony harmony)
        {
            try
            {
                MethodInfo method = AccessTools.Method(
                    typeof(AiVisitSettlementBehavior),
                    "CalculateBeingSettlementOwnerScores");

                if (method == null)
                {
                    InspectorLog.Warn(
                        "AI TRACE v0.25 territory need probe method not found.");
                    return 0;
                }

                harmony.Patch(
                    method,
                    prefix: new HarmonyMethod(AccessTools.Method(
                        typeof(AiDecisionTrace), nameof(BeforeTerritoryNeedScore))));

                return 1;
            }
            catch (Exception ex)
            {
                InspectorLog.Warn(
                    "AI TRACE v0.25 territory need probe patch skipped: " +
                    ex.GetType().Name + " - " + ex.Message);
                return 0;
            }
        }

        private static int PatchSetPartyAiActionBoundary(Harmony harmony)
        {
            try
            {
                Type actionType = AccessTools.TypeByName(
                    "TaleWorlds.CampaignSystem.Actions.SetPartyAiAction");

                MethodInfo applyInternal = actionType == null
                    ? null
                    : AccessTools.Method(actionType, "ApplyInternal");

                if (applyInternal == null)
                {
                    InspectorLog.Warn("AI TRACE v0.8 SetPartyAiAction.ApplyInternal not found.");
                    return 0;
                }

                harmony.Patch(
                    applyInternal,
                    prefix: new HarmonyMethod(AccessTools.Method(
                        typeof(AiDecisionTrace), nameof(BeforeSetPartyAiActionApplyInternal))),
                    postfix: new HarmonyMethod(AccessTools.Method(
                        typeof(AiDecisionTrace), nameof(AfterSetPartyAiActionApplyInternal))));

                return 1;
            }
            catch (Exception ex)
            {
                InspectorLog.Warn(
                    "AI TRACE v0.8 SetPartyAiAction.ApplyInternal patch skipped: " +
                    ex.GetType().Name + " - " + ex.Message);
                return 0;
            }
        }

        private static int PatchStrategicCommitMethods(Harmony harmony)
        {
            int patched = 0;

            string[] names =
            {
                "SetMoveModeHold",
                "SetMoveEngageParty",
                "SetMoveGoAroundParty",
                "SetMoveGoToSettlement",
                "SetMoveGoToPoint",
                "SetMoveToNearestLand",
                "SetMoveGoToInteractablePoint",
                "SetMoveEscortParty",
                "SetMovePatrolAroundPoint",
                "SetMovePatrolAroundSettlement",
                "SetMoveRaidSettlement",
                "SetMoveBesiegeSettlement",
                "SetMoveDefendSettlement"
            };

            foreach (string name in names)
            {
                try
                {
                    MethodInfo original = AccessTools.Method(typeof(MobileParty), name);
                    if (original == null)
                    {
                        InspectorLog.Warn("AI TRACE v0.8 strategic commit method not found: MobileParty." + name);
                        continue;
                    }

                    harmony.Patch(
                        original,
                        prefix: new HarmonyMethod(AccessTools.Method(
                            typeof(AiDecisionTrace), nameof(BeforeStrategicCommit))),
                        postfix: new HarmonyMethod(AccessTools.Method(
                            typeof(AiDecisionTrace), nameof(AfterStrategicCommit))));

                    patched++;
                }
                catch (Exception ex)
                {
                    InspectorLog.Warn(
                        "AI TRACE v0.8 strategic commit patch skipped for MobileParty." + name +
                        ": " + ex.GetType().Name + " - " + ex.Message);
                }
            }

            return patched;
        }

        private static int PatchStrategicVsShortTermTransitions(Harmony harmony)
        {
            int patched = 0;

            patched += PatchTransitionMethod(
                harmony,
                typeof(MobilePartyAi),
                "SetAiBehavior",
                nameof(BeforeSetAiBehavior),
                nameof(AfterSetAiBehavior));

            patched += PatchTransitionMethod(
                harmony,
                typeof(MobileParty),
                "SetShortTermBehavior",
                nameof(BeforeSetShortTermBehavior),
                nameof(AfterSetShortTermBehavior));

            patched += PatchTransitionMethod(
                harmony,
                typeof(MobilePartyAi),
                "SetInitiative",
                nameof(BeforeSetInitiative),
                nameof(AfterSetInitiative));

            return patched;
        }

        private static int PatchTransitionMethod(
            Harmony harmony,
            Type type,
            string methodName,
            string prefixName,
            string postfixName)
        {
            try
            {
                MethodInfo original = AccessTools.Method(type, methodName);
                if (original == null)
                {
                    InspectorLog.Warn(
                        "AI TRACE v0.8 transition method not found: " +
                        type.FullName + "." + methodName);
                    return 0;
                }

                harmony.Patch(
                    original,
                    prefix: new HarmonyMethod(AccessTools.Method(typeof(AiDecisionTrace), prefixName)),
                    postfix: new HarmonyMethod(AccessTools.Method(typeof(AiDecisionTrace), postfixName)));

                return 1;
            }
            catch (Exception ex)
            {
                InspectorLog.Warn(
                    "AI TRACE v0.8 transition patch skipped for " +
                    type.FullName + "." + methodName + ": " +
                    ex.GetType().Name + " - " + ex.Message);
                return 0;
            }
        }

        public static void BeforePartyThink(MobileParty __0)
        {
            _insideAnyPartyThink = true;
            _currentThinkingParty = __0;

            try
            {
                BehaviorDeltaRadiusRecorder.BeforePartyThink(__0);
            }
            catch (Exception ex)
            {
                InspectorLog.Error("BEHAVIOR DELTA RADIUS RECORDER v0.2 before-think failed.", ex);
            }

            try
            {
                _insideTargetThink = IsTarget(__0);
                _pending = null;
                _boundaryDeltas = _insideTargetThink ? new List<BoundaryDelta>() : null;
                _needProbeSeen = _insideTargetThink
                    ? new HashSet<string>(StringComparer.Ordinal)
                    : null;

                if (!_insideTargetThink)
                {
                    _beforeDefaultBehavior = null;
                    _beforeTargetSettlement = null;
                    _beforeTargetParty = null;
                    _beforeShortTermBehavior = null;
                    _beforeShortTermTargetSettlement = null;
                    _beforeShortTermTargetParty = null;
                    return;
                }

                _beforeDefaultBehavior = Text(ReadMember(__0, "DefaultBehavior"));
                _beforeTargetSettlement = SafeName(ReadMember(__0, "TargetSettlement"));
                _beforeTargetParty = SafeName(ReadMember(__0, "TargetParty"));
                _beforeShortTermBehavior = Text(ReadMember(__0, "ShortTermBehavior"));
                _beforeShortTermTargetSettlement = SafeName(ReadMember(__0, "ShortTermTargetSettlement"));
                _beforeShortTermTargetParty = SafeName(ReadMember(__0, "ShortTermTargetParty"));
            }
            catch
            {
                _insideTargetThink = false;
                _pending = null;
                _boundaryDeltas = null;
            }
        }

        public static void BeforeBehaviorBoundary(
            MethodBase __originalMethod,
            MobileParty __0,
            PartyThinkParams __1,
            out BoundaryState __state)
        {
            __state = null;

            if (!_insideTargetThink || !IsTarget(__0) || __1 == null)
                return;

            try
            {
                List<Candidate> before = SnapshotCandidates(__1);

                __state = new BoundaryState
                {
                    Source = DescribeMethod(__originalMethod),
                    BeforeCount = before.Count,
                    Before = ToCandidateMap(before)
                };
            }
            catch
            {
                __state = null;
            }
        }

        public static void AfterBehaviorBoundary(
            MethodBase __originalMethod,
            MobileParty __0,
            PartyThinkParams __1,
            BoundaryState __state)
        {
            if (!_insideTargetThink || !IsTarget(__0) || __1 == null || __state == null)
                return;

            try
            {
                List<Candidate> afterList = SnapshotCandidates(__1);
                Dictionary<string, Candidate> after = ToCandidateMap(afterList);

                var delta = new BoundaryDelta
                {
                    Source = __state.Source ?? DescribeMethod(__originalMethod),
                    BeforeCount = __state.BeforeCount,
                    AfterCount = afterList.Count
                };

                foreach (KeyValuePair<string, Candidate> kv in after)
                {
                    Candidate oldCandidate;
                    if (!__state.Before.TryGetValue(kv.Key, out oldCandidate))
                    {
                        delta.Changes.Add(new DeltaCandidate
                        {
                            Kind = "ADDED",
                            Behavior = kv.Value.Behavior,
                            Target = kv.Value.Target,
                            BeforeScore = 0f,
                            AfterScore = kv.Value.Score
                        });
                    }
                    else if (Math.Abs(oldCandidate.Score - kv.Value.Score) > 0.000001f)
                    {
                        delta.Changes.Add(new DeltaCandidate
                        {
                            Kind = "CHANGED",
                            Behavior = kv.Value.Behavior,
                            Target = kv.Value.Target,
                            BeforeScore = oldCandidate.Score,
                            AfterScore = kv.Value.Score
                        });
                    }
                }

                foreach (KeyValuePair<string, Candidate> kv in __state.Before)
                {
                    if (!after.ContainsKey(kv.Key))
                    {
                        delta.Changes.Add(new DeltaCandidate
                        {
                            Kind = "REMOVED",
                            Behavior = kv.Value.Behavior,
                            Target = kv.Value.Target,
                            BeforeScore = kv.Value.Score,
                            AfterScore = 0f
                        });
                    }
                }

                if (_boundaryDeltas == null)
                    _boundaryDeltas = new List<BoundaryDelta>();

                _boundaryDeltas.Add(delta);
            }
            catch (Exception ex)
            {
                InspectorLog.Error("AI TRACE v0.8 boundary diff failed.", ex);
            }
        }

        public static void AfterAiHourlyTick(MobileParty __0, PartyThinkParams __1)
        {
            // v0.27: after all vanilla scorers have finished, capture the current
            // untouched candidate ranking for the generalized read-only recorder.
            // No PartyThinkParams score is modified in this build.
            try
            {
                BehaviorDeltaRadiusRecorder.CaptureDecision(__0, __1);
            }
            catch (Exception ex)
            {
                InspectorLog.Error("BEHAVIOR DELTA RADIUS RECORDER v0.2 decision capture failed.", ex);
            }

            // Detailed legacy trace is intentionally disabled in this build.
            if (!_insideTargetThink || !IsTarget(__0) || __1 == null) return;

            try
            {
                // v0.8 lab experiment boundary:
                // vanilla has finished generating/scoring candidates, but PartyHourlyAiTick
                // has not yet selected/committed the winner.
                //
                // v0.20 global-scanner mode:
                // no single-party experiment is called here.

                List<Candidate> candidates = SnapshotCandidates(__1);

                _pending = new PendingTrace
                {
                    PartyName = SafeName(__0),
                    PartyId = __0.StringId,
                    Leader = __0.LeaderHero == null ? null : __0.LeaderHero.Name.ToString(),
                    CurrentObjectiveValue = Text(ReadMember(__1, "CurrentObjectiveValue")),
                    WillGatherAnArmy = Text(ReadMember(__1, "WillGatherAnArmy")),
                    DoNotChangeBehavior = Text(ReadMember(__1, "DoNotChangeBehavior")),
                    StrengthWithoutArmy = Text(ReadMember(__1, "StrengthOfLordsWithoutArmy")),
                    StrengthWithArmy = Text(ReadMember(__1, "StrengthOfLordsWithArmy")),
                    StrengthSameClanWithoutArmy = Text(ReadMember(__1, "StrengthOfLordsAtSameClanWithoutArmy")),
                    CandidateCount = candidates.Count,
                    Candidates = candidates
                };
            }
            catch (Exception ex)
            {
                InspectorLog.Error("AI TRACE v0.8 could not capture final AiHourlyTick scores.", ex);
            }
        }

        public static void AfterPartyThink(MobileParty __0)
        {
            try
            {
                BehaviorDeltaRadiusRecorder.AfterPartyThink(__0);
            }
            catch (Exception ex)
            {
                InspectorLog.Error("BEHAVIOR DELTA RADIUS RECORDER v0.2 end-of-think failed.", ex);
            }

            if (!_insideTargetThink || !IsTarget(__0))
            {
                ClearThinkContext();
                return;
            }

            try
            {
                if (_pending == null) return;

                long n = ++_traceNumber;
                PendingTrace t = _pending;

                var sb = new StringBuilder(8192);
                sb.AppendLine("=== AI TRACE BEGIN #" + n + " v" + Version + " ===");
                sb.AppendLine(
                    "party=" + (t.PartyName ?? "<null>") +
                    " id=" + (t.PartyId ?? "<null>") +
                    " leader=" + (t.Leader ?? "<null>"));

                sb.AppendLine(
                    "before defaultBehavior=" + (_beforeDefaultBehavior ?? "<null>") +
                    " targetSettlement=" + (_beforeTargetSettlement ?? "<null>") +
                    " targetParty=" + (_beforeTargetParty ?? "<null>"));

                sb.AppendLine(
                    "before shortTermBehavior=" + (_beforeShortTermBehavior ?? "<null>") +
                    " shortTermTargetSettlement=" + (_beforeShortTermTargetSettlement ?? "<null>") +
                    " shortTermTargetParty=" + (_beforeShortTermTargetParty ?? "<null>"));

                sb.AppendLine(
                    "think currentObjectiveValue=" + (t.CurrentObjectiveValue ?? "<null>") +
                    " doNotChangeBehavior=" + (t.DoNotChangeBehavior ?? "<null>") +
                    " willGatherAnArmy=" + (t.WillGatherAnArmy ?? "<null>"));

                sb.AppendLine(
                    "strength noArmy=" + (t.StrengthWithoutArmy ?? "<null>") +
                    " withArmy=" + (t.StrengthWithArmy ?? "<null>") +
                    " sameClanNoArmy=" + (t.StrengthSameClanWithoutArmy ?? "<null>"));

                sb.AppendLine(
                    "boundaryCount=" + (_boundaryDeltas == null ? 0 : _boundaryDeltas.Count));

                if (_boundaryDeltas != null)
                {
                    foreach (BoundaryDelta d in _boundaryDeltas)
                    {
                        int goToSettlementChanges = d.Changes.Count(x =>
                            string.Equals(x.Behavior, "GoToSettlement", StringComparison.Ordinal));

                        sb.AppendLine(
                            "boundary source=" + (d.Source ?? "<unknown>") +
                            " beforeCount=" + d.BeforeCount +
                            " afterCount=" + d.AfterCount +
                            " changes=" + d.Changes.Count +
                            " goToSettlementChanges=" + goToSettlementChanges);

                        foreach (DeltaCandidate c in d.Changes
                            .Where(x => string.Equals(x.Behavior, "GoToSettlement", StringComparison.Ordinal))
                            .OrderByDescending(x => x.AfterScore)
                            .Take(20))
                        {
                            sb.AppendLine(
                                "  boundaryDelta kind=" + c.Kind +
                                " behavior=" + (c.Behavior ?? "<null>") +
                                " target=" + (c.Target ?? "<null>") +
                                " beforeScore=" + c.BeforeScore.ToString("0.######", CultureInfo.InvariantCulture) +
                                " afterScore=" + c.AfterScore.ToString("0.######", CultureInfo.InvariantCulture));
                        }
                    }
                }

                sb.AppendLine("candidateCount=" + t.CandidateCount);

                foreach (Candidate c in t.Candidates
                    .OrderByDescending(x => x.Score)
                    .ThenBy(x => x.Index)
                    .Take(40))
                {
                    sb.Append("candidate score=");
                    sb.Append(c.Score.ToString("0.######", CultureInfo.InvariantCulture));
                    sb.Append(" behavior=");
                    sb.Append(c.Behavior ?? "<null>");
                    sb.Append(" target=");
                    sb.Append(c.Target ?? "<null>");

                    if (!string.IsNullOrEmpty(c.Position))
                    {
                        sb.Append(" position=");
                        sb.Append(c.Position);
                    }

                    sb.Append(" gatherArmy=");
                    sb.Append(c.WillGatherArmy ?? "<null>");
                    sb.Append(" nav=");
                    sb.Append(c.NavigationType ?? "<null>");
                    sb.Append(" fromPort=");
                    sb.Append(c.IsFromPort ?? "<null>");
                    sb.Append(" targetPort=");
                    sb.Append(c.IsTargetingPort ?? "<null>");
                    sb.AppendLine();
                }

                if (t.CandidateCount > 40)
                    sb.AppendLine("candidateNote=top 40 by score shown; total=" + t.CandidateCount);

                PartyState finalState = SnapshotPartyState(__0);

                sb.AppendLine(
                    "final defaultBehavior=" + (finalState.DefaultBehavior ?? "<null>") +
                    " targetSettlement=" + (finalState.TargetSettlement ?? "<null>") +
                    " targetParty=" + (finalState.TargetParty ?? "<null>"));

                sb.AppendLine(
                    "final shortTermBehavior=" + (finalState.ShortTermBehavior ?? "<null>") +
                    " shortTermTargetSettlement=" + (finalState.ShortTermTargetSettlement ?? "<null>") +
                    " shortTermTargetParty=" + (finalState.ShortTermTargetParty ?? "<null>") +
                    " moveTargetParty=" + (finalState.MoveTargetParty ?? "<null>") +
                    " moveMode=" + (finalState.PartyMoveMode ?? "<null>") +
                    " army=" + (SafeName(__0.Army) ?? "<null>") +
                    " attachedTo=" + (SafeName(__0.AttachedTo) ?? "<null>"));

                sb.Append("=== AI TRACE END #");
                sb.Append(n);
                sb.Append(" ===");

                InspectorLog.Info(sb.ToString());
            }
            catch (Exception ex)
            {
                InspectorLog.Error("AI TRACE v0.8 could not publish result.", ex);
            }
            finally
            {
                ClearThinkContext();
            }
        }

        // ---------------------------------------------------------------------
        // v0.11 read-only territory need probe
        // ---------------------------------------------------------------------

        private static void BeforeTerritoryNeedScore(object[] __args)
        {
            if (!_insideTargetThink || __args == null || __args.Length < 6)
                return;

            try
            {
                MobileParty party = __args[0] as MobileParty;
                Settlement settlement = __args[1] as Settlement;

                if (!IsTarget(party) || settlement == null)
                    return;

                Clan clan = party.ActualClan;
                if (clan == null || settlement.OwnerClan == null)
                    return;

                if (!object.ReferenceEquals(settlement.OwnerClan, clan) &&
                    !string.Equals(
                        settlement.OwnerClan.StringId,
                        clan.StringId,
                        StringComparison.Ordinal))
                    return;

                string key = settlement.StringId ?? settlement.Name.ToString();

                if (_needProbeSeen == null)
                    _needProbeSeen = new HashSet<string>(StringComparer.Ordinal);

                if (!_needProbeSeen.Add(key))
                    return;

                Settlement currentSettlement = __args[2] as Settlement;
                float idealGarrisonStrength = ToFloat(__args[3]);
                float distanceScorePure = ToFloat(__args[4]);
                float averagePartySizeRatioToMaximumSize = ToFloat(__args[5]);

                InspectorLog.Info(
                    "TERRITORY NEED PROBE v0.11" +
                    " party=" + party.Name +
                    " clan=" + clan.Name +
                    " target=" + settlement.Name +
                    " targetId=" + settlement.StringId +
                    " idealGarrisonStrength=" +
                    idealGarrisonStrength.ToString("0.######", CultureInfo.InvariantCulture) +
                    " distanceScorePure=" +
                    distanceScorePure.ToString("0.######", CultureInfo.InvariantCulture) +
                    " avgPartySizeRatioToMax=" +
                    averagePartySizeRatioToMaximumSize.ToString("0.######", CultureInfo.InvariantCulture) +
                    " currentSettlement=" +
                    (currentSettlement == null ? "<null>" : currentSettlement.Name.ToString()) +
                    " underRaid=" + settlement.IsUnderRaid +
                    " raided=" + settlement.IsRaided +
                    " underSiege=" + settlement.IsUnderSiege +
                    " militia=" +
                    settlement.Militia.ToString("0.######", CultureInfo.InvariantCulture));
            }
            catch (Exception ex)
            {
                InspectorLog.Error(
                    "AI TRACE v0.25 territory need probe failed.", ex);
            }
        }

        // ---------------------------------------------------------------------
        // v0.7 final selected-action boundary
        // ---------------------------------------------------------------------

        private static void BeforeSetPartyAiActionApplyInternal(
            MethodBase __originalMethod,
            object[] __args,
            out ActionCommitState __state)
        {
            __state = null;

            if (!_insideTargetThink || __args == null || __args.Length < 8)
                return;

            try
            {
                MobileParty owner = __args[0] as MobileParty;
                if (!IsTarget(owner))
                    return;

                object settlement = __args[1];
                object mobilePartyTarget = __args[2];
                object position = __args[3];
                object detail = __args[4];

                string detailText = Text(detail);
                string candidateBehavior = MapActionDetailToBehavior(detailText);
                string candidateTarget = DescribeActionTarget(
                    candidateBehavior, settlement, mobilePartyTarget, position);

                string matchedScore = "<not-found>";
                string matchedRank = "<not-found>";

                if (_pending != null && _pending.Candidates != null)
                {
                    List<Candidate> ordered = _pending.Candidates
                        .OrderByDescending(x => x.Score)
                        .ThenBy(x => x.Index)
                        .ToList();

                    Candidate match = ordered.FirstOrDefault(x =>
                        string.Equals(x.Behavior, candidateBehavior, StringComparison.Ordinal) &&
                        string.Equals(x.Target, candidateTarget, StringComparison.Ordinal));

                    if (match != null)
                    {
                        matchedScore = match.Score.ToString(
                            "0.######", CultureInfo.InvariantCulture);
                        matchedRank = (ordered.IndexOf(match) + 1).ToString(
                            CultureInfo.InvariantCulture);
                    }
                }

                __state = new ActionCommitState
                {
                    Party = owner,
                    Before = SnapshotPartyState(owner),
                    Caller = FindTransitionCaller(
                        __originalMethod == null ? null : __originalMethod.Name),
                    Detail = detailText,
                    Settlement = DescribeInteractable(settlement),
                    MobilePartyTarget = DescribeInteractable(mobilePartyTarget),
                    Position = Text(position),
                    NavigationType = ArgText(__args, 5),
                    IsFromPort = ArgText(__args, 6),
                    IsTargetingPort = ArgText(__args, 7),
                    CandidateBehavior = candidateBehavior,
                    CandidateTarget = candidateTarget,
                    MatchedScore = matchedScore,
                    MatchedRank = matchedRank
                };
            }
            catch
            {
                __state = null;
            }
        }

        private static void AfterSetPartyAiActionApplyInternal(
            MethodBase __originalMethod,
            object[] __args,
            ActionCommitState __state)
        {
            // v0.27 generalized recorder is observational only; no causal
            // intervention/commit observer is active in this build.

            if (__state == null || !_insideTargetThink || !IsTarget(__state.Party))
                return;

            try
            {
                PartyState after = SnapshotPartyState(__state.Party);
                var sb = new StringBuilder(3072);

                sb.AppendLine("=== SELECTED ACTION COMMIT BEGIN v" + Version + " ===");
                sb.AppendLine(
                    "caller=" + (__state.Caller ?? "<unknown>") +
                    " detail=" + (__state.Detail ?? "<null>") +
                    " duringPartyThink=" + _insideTargetThink);

                sb.AppendLine(
                    "selectedCandidate behavior=" + (__state.CandidateBehavior ?? "<null>") +
                    " target=" + (__state.CandidateTarget ?? "<null>") +
                    " score=" + (__state.MatchedScore ?? "<not-found>") +
                    " rank=" + (__state.MatchedRank ?? "<not-found>"));

                sb.AppendLine(
                    "args settlement=" + (__state.Settlement ?? "<null>") +
                    " mobileParty=" + (__state.MobilePartyTarget ?? "<null>") +
                    " position=" + (__state.Position ?? "<null>") +
                    " nav=" + (__state.NavigationType ?? "<null>") +
                    " fromPort=" + (__state.IsFromPort ?? "<null>") +
                    " targetingPort=" + (__state.IsTargetingPort ?? "<null>"));

                AppendPartyState(sb, "before", __state.Before);
                AppendPartyState(sb, "after", after);

                sb.Append("=== SELECTED ACTION COMMIT END v");
                sb.Append(Version);
                sb.Append(" ===");

                InspectorLog.Info(sb.ToString());
            }
            catch (Exception ex)
            {
                InspectorLog.Error("AI TRACE v0.8 could not publish selected action commit.", ex);
            }
        }

        private static string MapActionDetailToBehavior(string detail)
        {
            switch (detail)
            {
                case "GoToSettlement": return "GoToSettlement";
                case "PatrolAroundSettlement": return "PatrolAroundPoint";
                case "PatrolAroundPoint": return "PatrolAroundPoint";
                case "RaidSettlement": return "RaidSettlement";
                case "BesiegeSettlement": return "BesiegeSettlement";
                case "EngageParty": return "EngageParty";
                case "GoAroundParty": return "GoAroundParty";
                case "EscortParty": return "EscortParty";
                case "MoveToNearestLand": return "MoveToNearestLandOrPort";
                case "DefendParty": return "DefendSettlement";
                default: return detail;
            }
        }

        private static string DescribeActionTarget(
            string behavior,
            object settlement,
            object mobilePartyTarget,
            object position)
        {
            if (settlement != null)
                return DescribeMapPoint(settlement);

            if (mobilePartyTarget != null)
                return DescribeMapPoint(mobilePartyTarget);

            // Position-only actions use no IMapPoint target in AIBehaviorData.
            return null;
        }

        // ---------------------------------------------------------------------
        // v0.6 strategic commit trace
        // ---------------------------------------------------------------------

        private static void BeforeStrategicCommit(
            MethodBase __originalMethod,
            MobileParty __instance,
            object[] __args,
            out TransitionState __state)
        {
            __state = null;

            if (!InspectorConfig.AiResearchLightMode)
            {
                RecordGlobalStrategicCommit(
                    __originalMethod,
                    __instance,
                    __args);
            }

            // Detailed legacy trace remains single-party only.
            if (!_insideTargetThink || !IsTarget(__instance))
                return;

            try
            {
                __state = new TransitionState
                {
                    Party = __instance,
                    Before = SnapshotPartyState(__instance),
                    Caller = FindTransitionCaller(__originalMethod == null ? null : __originalMethod.Name),
                    RequestedBehavior = __originalMethod == null ? "<unknown>" : __originalMethod.Name,
                    RequestedTarget = DescribeStrategicArgs(__args),
                    RequestedPoint = null
                };
            }
            catch
            {
                __state = null;
            }
        }

        private static void AfterStrategicCommit(
            MethodBase __originalMethod,
            MobileParty __instance,
            object[] __args,
            TransitionState __state)
        {
            if (__state == null || !_insideTargetThink || !IsTarget(__instance))
                return;

            try
            {
                long n = ++_strategicCommitNumber;
                PartyState after = SnapshotPartyState(__instance);

                var sb = new StringBuilder(2048);
                sb.AppendLine("=== STRATEGIC COMMIT BEGIN #" + n + " v" + Version + " ===");
                sb.AppendLine(
                    "method=" + (__originalMethod == null ? "<unknown>" : __originalMethod.Name) +
                    " caller=" + (__state.Caller ?? "<unknown>") +
                    " duringPartyThink=" + _insideTargetThink);
                sb.AppendLine("args=" + (__state.RequestedTarget ?? "<none>"));

                AppendPartyState(sb, "before", __state.Before);
                AppendPartyState(sb, "after", after);

                sb.Append("=== STRATEGIC COMMIT END #");
                sb.Append(n);
                sb.Append(" ===");

                InspectorLog.Info(sb.ToString());
            }
            catch (Exception ex)
            {
                InspectorLog.Error("AI TRACE v0.8 could not publish strategic commit.", ex);
            }
        }

        private static void RecordGlobalStrategicCommit(
            MethodBase originalMethod,
            MobileParty actor,
            object[] args)
        {
            try
            {
                string methodName =
                    originalMethod == null
                        ? "<unknown>"
                        : originalMethod.Name;

                bool actorIsLord = false;

                try
                {
                    actorIsLord =
                        actor != null &&
                        actor.IsLordParty;
                }
                catch
                {
                    actorIsLord = false;
                }

                MobileParty target = null;

                if (args != null)
                {
                    for (int i = 0; i < args.Length; i++)
                    {
                        MobileParty candidate =
                            args[i] as MobileParty;

                        if (candidate != null &&
                            !object.ReferenceEquals(
                                candidate,
                                actor))
                        {
                            target = candidate;
                            break;
                        }
                    }
                }

                bool targetIsBandit = false;

                try
                {
                    targetIsBandit =
                        target != null &&
                        target.IsBandit;
                }
                catch
                {
                    targetIsBandit = false;
                }

                bool duringOwnThink =
                    _insideAnyPartyThink &&
                    actor != null &&
                    _currentThinkingParty != null &&
                    (
                        object.ReferenceEquals(
                            actor,
                            _currentThinkingParty) ||
                        (
                            !string.IsNullOrEmpty(
                                actor.StringId) &&
                            string.Equals(
                                actor.StringId,
                                _currentThinkingParty.StringId,
                                StringComparison.Ordinal)
                        )
                    );

                string censusLine = null;

                lock (_globalCommitSync)
                {
                    _globalCommitCalls++;

                    long methodCount;
                    _globalCommitCounts.TryGetValue(
                        methodName,
                        out methodCount);

                    _globalCommitCounts[methodName] =
                        methodCount + 1;

                    if (actorIsLord)
                        _globalLordCommitCalls++;

                    if (string.Equals(
                            methodName,
                            "SetMoveEngageParty",
                            StringComparison.Ordinal))
                    {
                        _globalEngageCalls++;

                        if (actorIsLord)
                            _globalLordEngageCalls++;

                        if (targetIsBandit)
                            _globalEngageBanditTargets++;

                        if (duringOwnThink)
                            _globalEngageDuringOwnThink++;
                    }

                    if ((_globalCommitCalls % 1000) == 0)
                    {
                        string methods =
                            string.Join(
                                ",",
                                _globalCommitCounts
                                    .OrderByDescending(
                                        x => x.Value)
                                    .ThenBy(
                                        x => x.Key)
                                    .Select(
                                        x =>
                                            x.Key +
                                            ":" +
                                            x.Value)
                                    .ToArray());

                        censusLine =
                            "GLOBAL_STRATEGIC_COMMIT_CENSUS" +
                            " total=" +
                            _globalCommitCalls +
                            " lord=" +
                            _globalLordCommitCalls +
                            " engage=" +
                            _globalEngageCalls +
                            " lordEngage=" +
                            _globalLordEngageCalls +
                            " engageBanditTargets=" +
                            _globalEngageBanditTargets +
                            " engageDuringOwnThink=" +
                            _globalEngageDuringOwnThink +
                            " methods=" +
                            methods;
                    }
                }

                if (string.Equals(
                        methodName,
                        "SetMoveEngageParty",
                        StringComparison.Ordinal))
                {
                    string actorName =
                        actor == null
                            ? "<null>"
                            : actor.Name.ToString();

                    string actorId =
                        actor == null
                            ? "<null>"
                            : actor.StringId;

                    string targetName =
                        target == null
                            ? "<null>"
                            : target.Name.ToString();

                    string targetId =
                        target == null
                            ? "<null>"
                            : target.StringId;

                    InspectorLog.Info(
                        "GLOBAL_ENGAGE_COMMIT" +
                        " actor=" +
                        actorName +
                        " actorId=" +
                        actorId +
                        " actorLord=" +
                        actorIsLord +
                        " target=" +
                        targetName +
                        " targetId=" +
                        targetId +
                        " targetBandit=" +
                        targetIsBandit +
                        " duringOwnThink=" +
                        duringOwnThink +
                        " caller=" +
                        FindTransitionCaller(
                            methodName));
                }

                if (!string.IsNullOrEmpty(
                    censusLine))
                {
                    InspectorLog.Info(censusLine);
                }
            }
            catch (Exception ex)
            {
                InspectorLog.Warn(
                    "AI TRACE global strategic commit census failed: " +
                    ex.GetType().Name +
                    " - " +
                    ex.Message);
            }
        }

        private static string DescribeStrategicArgs(object[] args)
        {
            if (args == null || args.Length == 0)
                return "<none>";

            var parts = new List<string>();

            for (int i = 0; i < args.Length; i++)
            {
                object value = args[i];
                string rendered;

                if (value == null)
                {
                    rendered = "<null>";
                }
                else
                {
                    string interactable = DescribeInteractable(value);
                    rendered = string.IsNullOrEmpty(interactable) ? value.ToString() : interactable;
                }

                parts.Add("arg" + i + "=" + rendered);
            }

            return string.Join("; ", parts.ToArray());
        }

        // ---------------------------------------------------------------------
        // v0.5 strategic/default vs short-term transition trace (retained in v0.6)
        // ---------------------------------------------------------------------

        private static void BeforeSetAiBehavior(
            MobilePartyAi __instance,
            object[] __args,
            out TransitionState __state)
        {
            __state = null;

            MobileParty party =
                GetMobileParty(__instance);

            if (!InspectorConfig.AiResearchLightMode)
            {
                RecordGlobalDefaultBehavior(
                    party,
                    __args);
            }

            try
            {
                if (!IsTarget(party)) return;

                __state = new TransitionState
                {
                    Party = party,
                    Before = SnapshotPartyState(party),
                    Caller = FindTransitionCaller("SetAiBehavior"),
                    RequestedBehavior = ArgText(__args, 0),
                    RequestedTarget = DescribeInteractable(Arg(__args, 1)),
                    RequestedPoint = ArgText(__args, 2)
                };
            }
            catch
            {
                __state = null;
            }
        }

        private static void RecordGlobalDefaultBehavior(
            MobileParty actor,
            object[] args)
        {
            try
            {
                string behavior =
                    ArgText(args, 0) ??
                    "<null>";

                PartyBase targetBase =
                    Arg(args, 1) as PartyBase;

                MobileParty targetMobile =
                    targetBase == null
                        ? null
                        : targetBase.MobileParty;

                Settlement targetSettlement =
                    targetBase == null
                        ? null
                        : targetBase.Settlement;

                bool actorIsLord = false;
                bool targetIsBandit = false;

                try
                {
                    actorIsLord =
                        actor != null &&
                        actor.IsLordParty;
                }
                catch
                {
                    actorIsLord = false;
                }

                try
                {
                    targetIsBandit =
                        targetMobile != null &&
                        targetMobile.IsBandit;
                }
                catch
                {
                    targetIsBandit = false;
                }

                bool duringOwnThink =
                    _insideAnyPartyThink &&
                    actor != null &&
                    _currentThinkingParty != null &&
                    (
                        object.ReferenceEquals(
                            actor,
                            _currentThinkingParty) ||
                        (
                            !string.IsNullOrEmpty(
                                actor.StringId) &&
                            string.Equals(
                                actor.StringId,
                                _currentThinkingParty.StringId,
                                StringComparison.Ordinal)
                        )
                    );

                string censusLine = null;

                lock (_defaultBehaviorSync)
                {
                    _defaultBehaviorCalls++;

                    long behaviorCount;
                    _defaultBehaviorCounts.TryGetValue(
                        behavior,
                        out behaviorCount);

                    _defaultBehaviorCounts[behavior] =
                        behaviorCount + 1;

                    if (actorIsLord)
                        _defaultBehaviorLordCalls++;

                    if (targetMobile != null)
                        _defaultBehaviorMobileTargets++;

                    if (targetSettlement != null)
                        _defaultBehaviorSettlementTargets++;

                    if (targetIsBandit)
                        _defaultBehaviorBanditTargets++;

                    if (duringOwnThink)
                        _defaultBehaviorDuringOwnThink++;

                    if (string.Equals(
                            behavior,
                            "EngageParty",
                            StringComparison.Ordinal))
                    {
                        _defaultBehaviorEngageCalls++;

                        if (actorIsLord)
                            _defaultBehaviorLordEngageCalls++;

                        if (targetIsBandit)
                            _defaultBehaviorEngageBanditTargets++;
                    }

                    if ((_defaultBehaviorCalls % 1000) == 0)
                    {
                        string behaviors =
                            string.Join(
                                ",",
                                _defaultBehaviorCounts
                                    .OrderByDescending(
                                        x => x.Value)
                                    .ThenBy(
                                        x => x.Key)
                                    .Select(
                                        x =>
                                            x.Key +
                                            ":" +
                                            x.Value)
                                    .ToArray());

                        censusLine =
                            "GLOBAL_DEFAULT_BEHAVIOR_CENSUS" +
                            " total=" +
                            _defaultBehaviorCalls +
                            " lord=" +
                            _defaultBehaviorLordCalls +
                            " mobileTargets=" +
                            _defaultBehaviorMobileTargets +
                            " settlementTargets=" +
                            _defaultBehaviorSettlementTargets +
                            " banditTargets=" +
                            _defaultBehaviorBanditTargets +
                            " engage=" +
                            _defaultBehaviorEngageCalls +
                            " lordEngage=" +
                            _defaultBehaviorLordEngageCalls +
                            " engageBanditTargets=" +
                            _defaultBehaviorEngageBanditTargets +
                            " duringOwnThink=" +
                            _defaultBehaviorDuringOwnThink +
                            " behaviors=" +
                            behaviors;
                    }
                }

                bool rare =
                    string.Equals(
                        behavior,
                        "EngageParty",
                        StringComparison.Ordinal) ||
                    targetIsBandit;

                if (rare)
                {
                    string actorName =
                        actor == null
                            ? "<null>"
                            : actor.Name.ToString();

                    string actorId =
                        actor == null
                            ? "<null>"
                            : actor.StringId;

                    string targetName =
                        targetMobile != null
                            ? targetMobile.Name.ToString()
                            : targetSettlement != null
                                ? targetSettlement.Name.ToString()
                                : targetBase != null
                                    ? targetBase.Name.ToString()
                                    : "<null>";

                    string targetKind =
                        targetMobile != null
                            ? "MobileParty"
                            : targetSettlement != null
                                ? "Settlement"
                                : targetBase != null
                                    ? "PartyBase"
                                    : "<null>";

                    InspectorLog.Info(
                        "GLOBAL_DEFAULT_BEHAVIOR_RARE" +
                        " actor=" +
                        actorName +
                        " actorId=" +
                        actorId +
                        " actorLord=" +
                        actorIsLord +
                        " behavior=" +
                        behavior +
                        " target=" +
                        targetName +
                        " targetKind=" +
                        targetKind +
                        " targetBandit=" +
                        targetIsBandit +
                        " duringOwnThink=" +
                        duringOwnThink +
                        " caller=" +
                        FindTransitionCaller(
                            "SetAiBehavior"));
                }

                if (!string.IsNullOrEmpty(
                    censusLine))
                {
                    InspectorLog.Info(censusLine);
                }
            }
            catch (Exception ex)
            {
                InspectorLog.Warn(
                    "AI TRACE global default-behavior census failed: " +
                    ex.GetType().Name +
                    " - " +
                    ex.Message);
            }
        }

        private static void AfterSetAiBehavior(
            MobilePartyAi __instance,
            object[] __args,
            TransitionState __state)
        {
            if (__state == null || !IsTarget(__state.Party)) return;

            PublishTransition(
                "MobilePartyAi.SetAiBehavior",
                __state,
                SnapshotPartyState(__state.Party),
                __instance);
        }

        private static void BeforeSetShortTermBehavior(
            MobileParty __instance,
            object[] __args,
            out TransitionState __state)
        {
            __state = null;

            if (!InspectorConfig.AiResearchLightMode)
            {
                RecordGlobalShortTermBehavior(
                    __instance,
                    __args);
            }

            try
            {
                if (!IsTarget(__instance)) return;

                __state = new TransitionState
                {
                    Party = __instance,
                    Before = SnapshotPartyState(__instance),
                    Caller = FindTransitionCaller("SetShortTermBehavior"),
                    RequestedBehavior = ArgText(__args, 0),
                    RequestedTarget = DescribeInteractable(Arg(__args, 1))
                };
            }
            catch
            {
                __state = null;
            }
        }

        private static void RecordGlobalShortTermBehavior(
            MobileParty actor,
            object[] args)
        {
            try
            {
                string behavior =
                    ArgText(args, 0) ??
                    "<null>";

                PartyBase targetBase =
                    Arg(args, 1) as PartyBase;

                MobileParty targetMobile =
                    targetBase == null
                        ? null
                        : targetBase.MobileParty;

                Settlement targetSettlement =
                    targetBase == null
                        ? null
                        : targetBase.Settlement;

                bool actorIsLord = false;
                bool targetIsBandit = false;

                try
                {
                    actorIsLord =
                        actor != null &&
                        actor.IsLordParty;
                }
                catch
                {
                    actorIsLord = false;
                }

                try
                {
                    targetIsBandit =
                        targetMobile != null &&
                        targetMobile.IsBandit;
                }
                catch
                {
                    targetIsBandit = false;
                }

                bool duringOwnThink =
                    _insideAnyPartyThink &&
                    actor != null &&
                    _currentThinkingParty != null &&
                    (
                        object.ReferenceEquals(
                            actor,
                            _currentThinkingParty) ||
                        (
                            !string.IsNullOrEmpty(
                                actor.StringId) &&
                            string.Equals(
                                actor.StringId,
                                _currentThinkingParty.StringId,
                                StringComparison.Ordinal)
                        )
                    );

                string censusLine = null;

                lock (_shortTermSync)
                {
                    _shortTermCalls++;

                    long behaviorCount;
                    _shortTermBehaviorCounts.TryGetValue(
                        behavior,
                        out behaviorCount);

                    _shortTermBehaviorCounts[behavior] =
                        behaviorCount + 1;

                    if (actorIsLord)
                        _shortTermLordCalls++;

                    if (targetMobile != null)
                        _shortTermMobileTargets++;

                    if (targetSettlement != null)
                        _shortTermSettlementTargets++;

                    if (targetIsBandit)
                        _shortTermBanditTargets++;

                    if (duringOwnThink)
                        _shortTermDuringOwnThink++;

                    if (string.Equals(
                            behavior,
                            "EngageParty",
                            StringComparison.Ordinal))
                    {
                        _shortTermEngageCalls++;

                        if (actorIsLord)
                            _shortTermLordEngageCalls++;

                        if (targetIsBandit)
                            _shortTermEngageBanditTargets++;
                    }

                    if ((_shortTermCalls % 1000) == 0)
                    {
                        string behaviors =
                            string.Join(
                                ",",
                                _shortTermBehaviorCounts
                                    .OrderByDescending(
                                        x => x.Value)
                                    .ThenBy(
                                        x => x.Key)
                                    .Select(
                                        x =>
                                            x.Key +
                                            ":" +
                                            x.Value)
                                    .ToArray());

                        censusLine =
                            "GLOBAL_SHORTTERM_CENSUS" +
                            " total=" +
                            _shortTermCalls +
                            " lord=" +
                            _shortTermLordCalls +
                            " mobileTargets=" +
                            _shortTermMobileTargets +
                            " settlementTargets=" +
                            _shortTermSettlementTargets +
                            " banditTargets=" +
                            _shortTermBanditTargets +
                            " engage=" +
                            _shortTermEngageCalls +
                            " lordEngage=" +
                            _shortTermLordEngageCalls +
                            " engageBanditTargets=" +
                            _shortTermEngageBanditTargets +
                            " duringOwnThink=" +
                            _shortTermDuringOwnThink +
                            " behaviors=" +
                            behaviors;
                    }
                }

                bool rare =
                    string.Equals(
                        behavior,
                        "EngageParty",
                        StringComparison.Ordinal) ||
                    targetIsBandit;

                if (rare)
                {
                    string actorName =
                        actor == null
                            ? "<null>"
                            : actor.Name.ToString();

                    string actorId =
                        actor == null
                            ? "<null>"
                            : actor.StringId;

                    string targetName =
                        targetMobile != null
                            ? targetMobile.Name.ToString()
                            : targetSettlement != null
                                ? targetSettlement.Name.ToString()
                                : targetBase != null
                                    ? targetBase.Name.ToString()
                                    : "<null>";

                    string targetKind =
                        targetMobile != null
                            ? "MobileParty"
                            : targetSettlement != null
                                ? "Settlement"
                                : targetBase != null
                                    ? "PartyBase"
                                    : "<null>";

                    InspectorLog.Info(
                        "GLOBAL_SHORTTERM_RARE" +
                        " actor=" +
                        actorName +
                        " actorId=" +
                        actorId +
                        " actorLord=" +
                        actorIsLord +
                        " behavior=" +
                        behavior +
                        " target=" +
                        targetName +
                        " targetKind=" +
                        targetKind +
                        " targetBandit=" +
                        targetIsBandit +
                        " duringOwnThink=" +
                        duringOwnThink +
                        " caller=" +
                        FindTransitionCaller(
                            "SetShortTermBehavior"));
                }

                if (!string.IsNullOrEmpty(
                    censusLine))
                {
                    InspectorLog.Info(censusLine);
                }
            }
            catch (Exception ex)
            {
                InspectorLog.Warn(
                    "AI TRACE global short-term census failed: " +
                    ex.GetType().Name +
                    " - " +
                    ex.Message);
            }
        }

        private static void AfterSetShortTermBehavior(
            MobileParty __instance,
            object[] __args,
            TransitionState __state)
        {
            if (__state == null || !IsTarget(__instance)) return;

            PublishTransition(
                "MobileParty.SetShortTermBehavior",
                __state,
                SnapshotPartyState(__instance),
                ReadMember(__instance, "Ai"));
        }

        private static void BeforeSetInitiative(
            MobilePartyAi __instance,
            object[] __args,
            out TransitionState __state)
        {
            __state = null;

            try
            {
                MobileParty party = GetMobileParty(__instance);
                if (!IsTarget(party)) return;

                __state = new TransitionState
                {
                    Party = party,
                    Before = SnapshotPartyState(party),
                    Caller = FindTransitionCaller("SetInitiative"),
                    RequestedAttackInitiative = ArgText(__args, 0),
                    RequestedAvoidInitiative = ArgText(__args, 1),
                    RequestedResetHours = ArgText(__args, 2)
                };
            }
            catch
            {
                __state = null;
            }
        }

        private static void AfterSetInitiative(
            MobilePartyAi __instance,
            object[] __args,
            TransitionState __state)
        {
            if (__state == null || !IsTarget(__state.Party)) return;

            PublishTransition(
                "MobilePartyAi.SetInitiative",
                __state,
                SnapshotPartyState(__state.Party),
                __instance);
        }

        private static void PublishTransition(
            string source,
            TransitionState state,
            PartyState after,
            object aiObject)
        {
            if (state == null || state.Party == null || after == null) return;

            long n = ++_transitionNumber;
            var sb = new StringBuilder(2048);

            sb.AppendLine("=== AI STATE TRANSITION BEGIN #" + n + " v" + Version + " ===");
            sb.AppendLine(
                "source=" + source +
                " caller=" + (state.Caller ?? "<unknown>") +
                " duringPartyThink=" + _insideTargetThink);

            if (!string.IsNullOrEmpty(state.RequestedBehavior) ||
                !string.IsNullOrEmpty(state.RequestedTarget) ||
                !string.IsNullOrEmpty(state.RequestedPoint))
            {
                sb.AppendLine(
                    "request behavior=" + (state.RequestedBehavior ?? "<null>") +
                    " target=" + (state.RequestedTarget ?? "<null>") +
                    " point=" + (state.RequestedPoint ?? "<null>"));
            }

            if (!string.IsNullOrEmpty(state.RequestedAttackInitiative) ||
                !string.IsNullOrEmpty(state.RequestedAvoidInitiative) ||
                !string.IsNullOrEmpty(state.RequestedResetHours))
            {
                sb.AppendLine(
                    "request initiative attack=" + (state.RequestedAttackInitiative ?? "<null>") +
                    " avoid=" + (state.RequestedAvoidInitiative ?? "<null>") +
                    " resetHours=" + (state.RequestedResetHours ?? "<null>"));
            }

            AppendPartyState(sb, "before", state.Before);
            AppendPartyState(sb, "after", after);

            if (aiObject != null)
            {
                sb.AppendLine(
                    "initiative attack=" + (Text(ReadMember(aiObject, "AttackInitiative")) ?? "<null>") +
                    " avoid=" + (Text(ReadMember(aiObject, "AvoidInitiative")) ?? "<null>") +
                    " restoreTime=" + (Text(ReadMember(aiObject, "_initiativeRestoreTime")) ?? "<null>") +
                    " defaultBehaviorNeedsUpdate=" + (Text(ReadMember(aiObject, "DefaultBehaviorNeedsUpdate")) ?? "<null>") +
                    " aiInteractable=" + (DescribeInteractable(ReadMember(aiObject, "AiBehaviorInteractable")) ?? "<null>"));
            }

            sb.Append("=== AI STATE TRANSITION END #");
            sb.Append(n);
            sb.Append(" ===");

            InspectorLog.Info(sb.ToString());
        }

        private static void AppendPartyState(StringBuilder sb, string label, PartyState state)
        {
            if (state == null)
            {
                sb.AppendLine(label + "=<null>");
                return;
            }

            sb.AppendLine(
                label +
                " defaultBehavior=" + (state.DefaultBehavior ?? "<null>") +
                " targetSettlement=" + (state.TargetSettlement ?? "<null>") +
                " targetParty=" + (state.TargetParty ?? "<null>"));

            sb.AppendLine(
                label +
                " shortTermBehavior=" + (state.ShortTermBehavior ?? "<null>") +
                " shortTermTargetSettlement=" + (state.ShortTermTargetSettlement ?? "<null>") +
                " shortTermTargetParty=" + (state.ShortTermTargetParty ?? "<null>") +
                " moveTargetParty=" + (state.MoveTargetParty ?? "<null>") +
                " moveMode=" + (state.PartyMoveMode ?? "<null>"));
        }

        private static PartyState SnapshotPartyState(MobileParty party)
        {
            if (party == null) return null;

            return new PartyState
            {
                DefaultBehavior = Text(ReadMember(party, "DefaultBehavior")),
                TargetSettlement = SafeName(ReadMember(party, "TargetSettlement")),
                TargetParty = SafeName(ReadMember(party, "TargetParty")),
                ShortTermBehavior = Text(ReadMember(party, "ShortTermBehavior")),
                ShortTermTargetSettlement = SafeName(ReadMember(party, "ShortTermTargetSettlement")),
                ShortTermTargetParty = SafeName(ReadMember(party, "ShortTermTargetParty")),
                MoveTargetParty = SafeName(ReadMember(party, "MoveTargetParty")),
                PartyMoveMode = Text(ReadMember(party, "PartyMoveMode"))
            };
        }

        private static MobileParty GetMobileParty(MobilePartyAi ai)
        {
            if (ai == null) return null;

            object party = ReadMember(ai, "_mobileParty");
            if (party == null)
                party = ReadMember(ai, "MobileParty");

            return party as MobileParty;
        }

        private static object Arg(object[] args, int index)
        {
            return args != null && index >= 0 && index < args.Length ? args[index] : null;
        }

        private static string ArgText(object[] args, int index)
        {
            return Text(Arg(args, index));
        }

        private static string DescribeInteractable(object value)
        {
            if (value == null) return null;

            string name = SafeName(value);
            string id = Text(ReadMember(value, "StringId"));

            if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(id))
                return name + " [" + id + "]";

            if (!string.IsNullOrEmpty(name))
                return name;

            if (!string.IsNullOrEmpty(id))
                return id;

            return value.ToString();
        }

        private static string FindTransitionCaller(string setterName)
        {
            try
            {
                StackFrame[] frames = new StackTrace(1, false).GetFrames();
                if (frames == null) return "<unknown>";

                foreach (StackFrame frame in frames)
                {
                    MethodBase method = frame.GetMethod();
                    if (method == null) continue;

                    Type type = method.DeclaringType;
                    string full = type == null ? null : type.FullName;

                    if (string.IsNullOrEmpty(full)) continue;
                    if (type == typeof(AiDecisionTrace)) continue;
                    if (string.Equals(method.Name, setterName, StringComparison.Ordinal)) continue;
                    if (full.StartsWith("HarmonyLib.", StringComparison.Ordinal)) continue;
                    if (full.StartsWith("System.", StringComparison.Ordinal)) continue;
                    if (full.StartsWith("Microsoft.", StringComparison.Ordinal)) continue;

                    if (full.StartsWith("TaleWorlds.CampaignSystem.", StringComparison.Ordinal))
                        return full + "." + method.Name;
                }
            }
            catch
            {
            }

            return "<unknown>";
        }

        private static List<Candidate> SnapshotCandidates(PartyThinkParams p)
        {
            var candidates = new List<Candidate>();
            if (p == null) return candidates;

            object scoreList = ReadMember(p, "AIBehaviorScores");
            IEnumerable enumerable = scoreList as IEnumerable;
            if (enumerable == null) return candidates;

            int i = 0;
            foreach (object item in enumerable)
            {
                object data = ReadMember(item, "Item1");
                object scoreObject = ReadMember(item, "Item2");

                if (data == null)
                {
                    i++;
                    continue;
                }

                var c = new Candidate
                {
                    Index = i++,
                    Score = ToFloat(scoreObject),
                    Behavior = Text(ReadMember(data, "AiBehavior")),
                    Target = DescribeMapPoint(ReadMember(data, "Party")),
                    Position = Text(ReadMember(data, "Position")),
                    WillGatherArmy = Text(ReadMember(data, "WillGatherArmy")),
                    NavigationType = Text(ReadMember(data, "NavigationType")),
                    IsFromPort = Text(ReadMember(data, "IsFromPort")),
                    IsTargetingPort = Text(ReadMember(data, "IsTargetingPort"))
                };

                c.Key = BuildCandidateKey(c);
                candidates.Add(c);
            }

            return candidates;
        }

        private static Dictionary<string, Candidate> ToCandidateMap(List<Candidate> candidates)
        {
            var map = new Dictionary<string, Candidate>(StringComparer.Ordinal);

            foreach (Candidate c in candidates)
            {
                string key = c.Key ?? BuildCandidateKey(c);

                // Preserve duplicates, if any, instead of silently overwriting them.
                if (map.ContainsKey(key))
                {
                    int suffix = 2;
                    string baseKey = key;
                    while (map.ContainsKey(baseKey + "#" + suffix))
                        suffix++;
                    key = baseKey + "#" + suffix;
                }

                map[key] = c;
            }

            return map;
        }

        private static string BuildCandidateKey(Candidate c)
        {
            if (c == null) return "<null>";

            return
                (c.Behavior ?? "<null>") + "|" +
                (c.Target ?? "<null>") + "|" +
                (c.Position ?? "<null>") + "|" +
                (c.WillGatherArmy ?? "<null>") + "|" +
                (c.NavigationType ?? "<null>") + "|" +
                (c.IsFromPort ?? "<null>") + "|" +
                (c.IsTargetingPort ?? "<null>");
        }

        private static string DescribeMethod(MethodBase method)
        {
            if (method == null) return "<unknown>";
            Type t = method.DeclaringType;
            return (t == null ? "<unknown>" : t.FullName) + "." + method.Name;
        }

        private static bool IsTarget(MobileParty party)
        {
            return party != null &&
                   string.Equals(party.StringId, TargetPartyId, StringComparison.Ordinal);
        }

        private static void ClearThinkContext()
        {
            _insideAnyPartyThink = false;
            _currentThinkingParty = null;
            _insideTargetThink = false;
            _pending = null;
            _boundaryDeltas = null;
            _needProbeSeen = null;
            _beforeDefaultBehavior = null;
            _beforeTargetSettlement = null;
            _beforeTargetParty = null;
            _beforeShortTermBehavior = null;
            _beforeShortTermTargetSettlement = null;
            _beforeShortTermTargetParty = null;
        }

        private static object ReadMember(object instance, string name)
        {
            if (instance == null || string.IsNullOrEmpty(name)) return null;

            Type t = instance.GetType();

            FieldInfo f = t.GetField(
                name,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f != null) return f.GetValue(instance);

            PropertyInfo p = t.GetProperty(
                name,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (p != null && p.GetIndexParameters().Length == 0)
                return p.GetValue(instance, null);

            return null;
        }

        private static string DescribeMapPoint(object target)
        {
            if (target == null) return null;

            try
            {
                string name = Text(ReadMember(target, "Name"));
                string id = Text(ReadMember(target, "StringId"));

                if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(id))
                    return name + " [" + id + "]";

                if (!string.IsNullOrEmpty(name))
                    return name;

                if (!string.IsNullOrEmpty(id))
                    return id;

                return target.ToString();
            }
            catch
            {
                return target.GetType().FullName;
            }
        }

        private static string SafeName(object value)
        {
            if (value == null) return null;

            try
            {
                object name = ReadMember(value, "Name");
                if (name != null) return name.ToString();
                return value.ToString();
            }
            catch
            {
                return null;
            }
        }

        private static string Text(object value)
        {
            return value == null ? null : value.ToString();
        }

        private static float ToFloat(object value)
        {
            if (value == null) return 0f;

            try
            {
                return Convert.ToSingle(value, CultureInfo.InvariantCulture);
            }
            catch
            {
                return 0f;
            }
        }
    }
}
