using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace ClanAI
{
    internal static class ActorStrategicBlackboard
    {
        internal enum CoarseState
        {
            Other,
            Recover,
            Travel,
            Defend,
            Patrol,
            Secure,
            Raid,
            Campaign,
            Pursue,
            Support,
            Retreat
        }

        internal sealed class CommitmentState
        {
            internal string ObjectiveSignature;
            internal string ObjectiveLabel;
            internal CoarseState State;
            internal double ObjectiveSinceHours;
        }

        private sealed class Record
        {
            internal string ActorId;
            internal string ActorName;
            internal string ObjectiveSignature;
            internal string ObjectiveLabel;
            internal CoarseState State;
            internal double ObjectiveSinceHours;
            internal double LastDecisionHours;
            internal long Reconsiderations;
            internal long ObjectiveSwitches;
            internal long StateSwitches;
            internal float LastWinnerScore;
            internal float LastRunnerUpScore;
            internal float LastGapPct;
            internal string LastSwitchReason;
        }

        private static readonly Dictionary<string, Record> Records =
            new Dictionary<string, Record>(StringComparer.Ordinal);

        private static long _decisions;
        private static long _objectiveSwitches;
        private static long _stateSwitches;
        private static long _previousStillEligibleSwitches;
        private static long _previousMissingSwitches;
        private static long _near5;
        private static long _near10;
        private static long _near20;
        private static long _stable;
        private static long _failures;
        internal static void Reset()
        {
            Records.Clear();
            Interlocked.Exchange(ref _decisions, 0);
            Interlocked.Exchange(ref _objectiveSwitches, 0);
            Interlocked.Exchange(ref _stateSwitches, 0);
            Interlocked.Exchange(ref _previousStillEligibleSwitches, 0);
            Interlocked.Exchange(ref _previousMissingSwitches, 0);
            Interlocked.Exchange(ref _near5, 0);
            Interlocked.Exchange(ref _near10, 0);
            Interlocked.Exchange(ref _near20, 0);
            Interlocked.Exchange(ref _stable, 0);
            Interlocked.Exchange(ref _failures, 0);

            ClanAIPostVanilla.WriteExternalLog(
                "ACTOR_INTENT_BLACKBOARD_RESET mode=shadow");
        }

        internal static string SummaryFields()
        {
            return
                " intentBlackboardActors=" + Records.Count +
                " intentBlackboardDecisions=" +
                Interlocked.Read(ref _decisions) +
                " intentBlackboardStable=" +
                Interlocked.Read(ref _stable) +
                " intentBlackboardObjectiveSwitches=" +
                Interlocked.Read(ref _objectiveSwitches) +
                " intentBlackboardStateSwitches=" +
                Interlocked.Read(ref _stateSwitches) +
                " intentBlackboardPrevEligibleSwitches=" +
                Interlocked.Read(ref _previousStillEligibleSwitches) +
                " intentBlackboardPrevMissingSwitches=" +
                Interlocked.Read(ref _previousMissingSwitches) +
                " intentBlackboardNear5=" +
                Interlocked.Read(ref _near5) +
                " intentBlackboardNear10=" +
                Interlocked.Read(ref _near10) +
                " intentBlackboardNear20=" +
                Interlocked.Read(ref _near20) +
                " intentBlackboardFailures=" +
                Interlocked.Read(ref _failures);
        }

        internal static void Observe(
            MobileParty actor,
            PartyThinkParams thinkParams,
            int winnerIndex,
            StrategicDecisionComposer.Frame composer)
        {
            try
            {
                if (actor == null ||
                    actor.LeaderHero == null ||
                    thinkParams == null ||
                    winnerIndex < 0 ||
                    winnerIndex >= thinkParams.AIBehaviorScores.Count)
                {
                    return;
                }

                Interlocked.Increment(ref _decisions);

                string actorId =
                    string.IsNullOrEmpty(actor.StringId)
                        ? actor.LeaderHero.StringId
                        : actor.StringId;

                if (string.IsNullOrEmpty(actorId))
                    actorId = actor.LeaderHero.Name.ToString();

                AIBehaviorData winner =
                    thinkParams.AIBehaviorScores[winnerIndex].Item1;

                float winnerScore =
                    thinkParams.AIBehaviorScores[winnerIndex].Item2;

                string signature =
                    Signature(winner);

                string label =
                    Label(winner);

                int runnerIndex = -1;
                float runnerScore = float.MinValue;

                for (int i = 0;
                     i < thinkParams.AIBehaviorScores.Count;
                     i++)
                {
                    if (i == winnerIndex)
                        continue;

                    float score =
                        thinkParams.AIBehaviorScores[i].Item2;

                    if (runnerIndex < 0 ||
                        score > runnerScore)
                    {
                        runnerIndex = i;
                        runnerScore = score;
                    }
                }
                float gapPct =
                    GapPct(
                        winnerScore,
                        runnerScore);

                bool weak =
                    actor.PartySizeRatio < 0.72f ||
                    actor.GetNumDaysForFoodToLast() < 3;

                CoarseState state =
                    ClassifyState(
                        winner,
                        winnerIndex,
                        weak,
                        composer);

                double nowHours =
                    CampaignTime.Now.ToHours;

                Record record;

                if (!Records.TryGetValue(
                        actorId,
                        out record))
                {
                    record =
                        new Record
                        {
                            ActorId = actorId,
                            ActorName =
                                actor.LeaderHero.Name.ToString(),
                            ObjectiveSignature = signature,
                            ObjectiveLabel = label,
                            State = state,
                            ObjectiveSinceHours = nowHours,
                            LastDecisionHours = nowHours,
                            Reconsiderations = 1,
                            LastWinnerScore = winnerScore,
                            LastRunnerUpScore = runnerScore,
                            LastGapPct = gapPct,
                            LastSwitchReason = "born"
                        };

                    Records.Add(actorId, record);

                    ClanAIPostVanilla.WriteExternalLog(
                        "ACTOR_BLACKBOARD_BORN" +
                        " actor=" +
                        record.ActorName +
                        " state=" +
                        state +
                        " objective=" +
                        label +
                        " winnerScore=" +
                        F(winnerScore) +
                        " runnerScore=" +
                        F(runnerScore) +
                        " gapPct=" +
                        F(gapPct));

                    return;
                }

                record.Reconsiderations++;
                record.LastDecisionHours = nowHours;

                if (string.Equals(
                        record.ObjectiveSignature,
                        signature,
                        StringComparison.Ordinal))
                {
                    Interlocked.Increment(ref _stable);

                    record.LastWinnerScore =
                        winnerScore;

                    record.LastRunnerUpScore =
                        runnerScore;

                    record.LastGapPct =
                        gapPct;

                    return;
                }

                Interlocked.Increment(
                    ref _objectiveSwitches);

                record.ObjectiveSwitches++;

                bool stateChanged =
                    record.State != state;

                if (stateChanged)
                {
                    Interlocked.Increment(
                        ref _stateSwitches);

                    record.StateSwitches++;
                }

                bool previousFound = false;
                float previousScore = float.MinValue;

                for (int i = 0;
                     i < thinkParams.AIBehaviorScores.Count;
                     i++)
                {
                    AIBehaviorData candidate =
                        thinkParams.AIBehaviorScores[i].Item1;

                    if (!string.Equals(
                            Signature(candidate),
                            record.ObjectiveSignature,
                            StringComparison.Ordinal))
                    {
                        continue;
                    }

                    float score =
                        thinkParams.AIBehaviorScores[i].Item2;

                    if (!previousFound ||
                        score > previousScore)
                    {
                        previousFound = true;
                        previousScore = score;
                    }
                }
                string switchReason;
                float switchGapPct = float.NaN;
                float requiredFactor = float.NaN;

                if (previousFound)
                {
                    Interlocked.Increment(
                        ref _previousStillEligibleSwitches);

                    switchReason =
                        "previous-still-eligible";

                    switchGapPct =
                        GapPct(
                            winnerScore,
                            previousScore);

                    if (previousScore > 0.000001f)
                    {
                        requiredFactor =
                            winnerScore /
                            previousScore;
                    }

                    if (switchGapPct <= 0.05f)
                        Interlocked.Increment(ref _near5);

                    if (switchGapPct <= 0.10f)
                        Interlocked.Increment(ref _near10);

                    if (switchGapPct <= 0.20f)
                        Interlocked.Increment(ref _near20);
                }
                else
                {
                    Interlocked.Increment(
                        ref _previousMissingSwitches);

                    switchReason =
                        "previous-no-longer-eligible";
                }

                double objectiveAgeHours =
                    nowHours -
                    record.ObjectiveSinceHours;

                ClanAIPostVanilla.WriteExternalLog(
                    "ACTOR_INTENT_SWITCH" +
                    " actor=" +
                    record.ActorName +
                    " fromState=" +
                    record.State +
                    " toState=" +
                    state +
                    " from=" +
                    record.ObjectiveLabel +
                    " to=" +
                    label +
                    " objectiveAgeHours=" +
                    D(objectiveAgeHours) +
                    " previousEligible=" +
                    previousFound +
                    " previousScore=" +
                    F(previousScore) +
                    " winnerScore=" +
                    F(winnerScore) +
                    " switchGapPct=" +
                    F(switchGapPct) +
                    " requiredFactor=" +
                    F(requiredFactor) +
                    " runnerGapPct=" +
                    F(gapPct) +
                    " readiness=" +
                    F(actor.PartySizeRatio) +
                    " foodDays=" +
                    actor.GetNumDaysForFoodToLast() +
                    " reason=" +
                    switchReason);

                record.ObjectiveSignature =
                    signature;

                record.ObjectiveLabel =
                    label;

                record.State =
                    state;

                record.ObjectiveSinceHours =
                    nowHours;

                record.LastWinnerScore =
                    winnerScore;

                record.LastRunnerUpScore =
                    runnerScore;

                record.LastGapPct =
                    gapPct;

                record.LastSwitchReason =
                    switchReason;
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref _failures);

                ClanAIPostVanilla.WriteExternalLog(
                    "ACTOR_BLACKBOARD_FAILURE" +
                    " type=" +
                    ex.GetType().Name);
            }
        }
        internal static bool TryGetCommitmentState(
            MobileParty actor,
            out CommitmentState state)
        {
            state = null;

            if (actor == null ||
                actor.LeaderHero == null)
            {
                return false;
            }

            string actorId =
                ActorKey(actor);

            Record record;

            if (!Records.TryGetValue(
                    actorId,
                    out record))
            {
                return false;
            }

            state =
                new CommitmentState
                {
                    ObjectiveSignature =
                        record.ObjectiveSignature,
                    ObjectiveLabel =
                        record.ObjectiveLabel,
                    State =
                        record.State,
                    ObjectiveSinceHours =
                        record.ObjectiveSinceHours
                };

            return true;
        }

        internal static int FindObjectiveIndex(
            PartyThinkParams thinkParams,
            StrategicDecisionComposer.Frame composer,
            string objectiveSignature,
            out float bestScore)
        {
            bestScore = float.MinValue;

            if (thinkParams == null ||
                composer == null ||
                string.IsNullOrEmpty(
                    objectiveSignature))
            {
                return -1;
            }

            int bestIndex = -1;

            for (int i = 0;
                 i < thinkParams.AIBehaviorScores.Count;
                 i++)
            {
                AIBehaviorData data =
                    thinkParams.AIBehaviorScores[i].Item1;

                if (!string.Equals(
                        Signature(data),
                        objectiveSignature,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                float score =
                    composer.CurrentScore(
                        i,
                        thinkParams
                            .AIBehaviorScores[i]
                            .Item2);

                if (bestIndex < 0 ||
                    score > bestScore)
                {
                    bestIndex = i;
                    bestScore = score;
                }
            }

            return bestIndex;
        }

        internal static string ActorKey(
            MobileParty actor)
        {
            if (actor == null)
                return "<null>";

            if (!string.IsNullOrEmpty(
                    actor.StringId))
            {
                return actor.StringId;
            }

            if (actor.LeaderHero != null &&
                !string.IsNullOrEmpty(
                    actor.LeaderHero.StringId))
            {
                return actor.LeaderHero.StringId;
            }

            return
                actor.LeaderHero != null
                    ? actor.LeaderHero.Name.ToString()
                    : actor.Name.ToString();
        }

        internal static CoarseState ClassifyState(
            AIBehaviorData winner,
            int winnerIndex,
            bool weak,
            StrategicDecisionComposer.Frame composer)
        {
            AiBehavior behavior =
                winner.AiBehavior;

            if (weak &&
                behavior ==
                    AiBehavior.GoToSettlement)
            {
                return CoarseState.Recover;
            }

            if (behavior ==
                    AiBehavior.FleeToPoint ||
                behavior ==
                    AiBehavior.FleeToGate)
            {
                return CoarseState.Retreat;
            }

            if (HasReason(
                    composer,
                    winnerIndex,
                    "visual-war",
                    "active-defense") ||
                HasReason(
                    composer,
                    winnerIndex,
                    "visual-war",
                    "frontier-defense"))
            {
                return CoarseState.Defend;
            }

            if (HasReason(
                    composer,
                    winnerIndex,
                    "visual-war",
                    "rear-security"))
            {
                return CoarseState.Secure;
            }

            if (behavior ==
                AiBehavior.RaidSettlement)
            {
                return CoarseState.Raid;
            }

            if (behavior ==
                    AiBehavior.BesiegeSettlement ||
                behavior ==
                    AiBehavior.AssaultSettlement)
            {
                return CoarseState.Campaign;
            }

            if (behavior ==
                AiBehavior.EngageParty)
            {
                return CoarseState.Pursue;
            }

            if (behavior ==
                AiBehavior.PatrolAroundPoint)
            {
                return CoarseState.Patrol;
            }

            if (behavior ==
                AiBehavior.EscortParty)
            {
                return CoarseState.Support;
            }

            if (behavior ==
                AiBehavior.GoToSettlement)
            {
                return CoarseState.Travel;
            }

            return CoarseState.Other;
        }

        private static bool HasReason(
            StrategicDecisionComposer.Frame composer,
            int index,
            string source,
            string reason)
        {
            if (composer == null)
                return false;

            StrategicDecisionComposer.Entry entry;

            if (!composer.Entries.TryGetValue(
                    index,
                    out entry))
            {
                return false;
            }

            for (int i = 0;
                 i < entry.Contributions.Count;
                 i++)
            {
                StrategicDecisionComposer.Contribution c =
                    entry.Contributions[i];

                if (c.Applied &&
                    string.Equals(
                        c.Source,
                        source,
                        StringComparison.Ordinal) &&
                    string.Equals(
                        c.Reason,
                        reason,
                        StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
        internal static string Signature(
            AIBehaviorData data)
        {
            return
                data.AiBehavior.ToString() +
                "|" +
                TargetKey(data);
        }

        internal static string Label(
            AIBehaviorData data)
        {
            return
                data.AiBehavior.ToString() +
                ":" +
                TargetName(data);
        }

        private static string TargetKey(
            AIBehaviorData data)
        {
            Settlement settlement =
                data.Party as Settlement;

            if (settlement != null)
            {
                return
                    "S:" +
                    settlement.StringId;
            }

            MobileParty party =
                data.Party as MobileParty;

            if (party != null)
            {
                return
                    "P:" +
                    party.StringId;
            }

            return
                "V:" +
                Math.Round(
                    data.Position.X,
                    1).ToString(
                        "0.0",
                        CultureInfo.InvariantCulture) +
                "," +
                Math.Round(
                    data.Position.Y,
                    1).ToString(
                        "0.0",
                        CultureInfo.InvariantCulture) +
                "," +
                (data.Position.IsOnLand
                    ? "L"
                    : "W");
        }

        private static string TargetName(
            AIBehaviorData data)
        {
            Settlement settlement =
                data.Party as Settlement;

            if (settlement != null)
                return settlement.Name.ToString();

            MobileParty party =
                data.Party as MobileParty;

            if (party != null)
                return party.Name.ToString();

            return
                "point(" +
                Math.Round(
                    data.Position.X,
                    1).ToString(
                        "0.0",
                        CultureInfo.InvariantCulture) +
                "," +
                Math.Round(
                    data.Position.Y,
                    1).ToString(
                        "0.0",
                        CultureInfo.InvariantCulture) +
                ")";
        }

        private static float GapPct(
            float winner,
            float other)
        {
            if (float.IsNaN(winner) ||
                float.IsInfinity(winner) ||
                float.IsNaN(other) ||
                float.IsInfinity(other))
            {
                return float.NaN;
            }

            float denominator =
                Math.Max(
                    Math.Abs(winner),
                    0.000001f);

            return
                (winner - other) /
                denominator;
        }

        private static string F(
            float value)
        {
            if (float.IsNaN(value))
                return "NaN";

            if (float.IsPositiveInfinity(value))
                return "+Inf";

            if (float.IsNegativeInfinity(value))
                return "-Inf";

            return value.ToString(
                "R",
                CultureInfo.InvariantCulture);
        }

        private static string D(
            double value)
        {
            return value.ToString(
                "R",
                CultureInfo.InvariantCulture);
        }
    }
}
