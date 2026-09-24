using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;

namespace ClanAI
{
    internal static class StrategicDecisionComposer
    {
        private const float AbsoluteTolerance = 0.00001f;
        private const float RelativeTolerance = 0.00001f;

        private static long _frames;
        private static long _framesWithAppliedContributions;
        private static long _appliedContributions;
        private static long _proposalContributions;
        private static long _scoreWrites;
        private static long _scoreMismatches;
        private static long _sequenceMismatches;
        private static long _winnerMismatches;
        private static long _equivalentFrames;
        private static long _failures;

        internal sealed class Contribution
        {
            internal string Source;
            internal string Reason;
            internal float BeforeScore;
            internal float Factor;
            internal float AfterScore;
            internal bool Applied;
        }
        internal sealed class Entry
        {
            internal int Index;
            internal float OriginalScore;
            internal float WorkingScore;
            internal readonly List<Contribution> Contributions =
                new List<Contribution>();
        }

        internal sealed class Frame
        {
            internal MobileParty Actor;
            internal PartyThinkParams ThinkParams;
            internal int CandidateCount;
            internal int BeforeWinner;
            internal readonly Dictionary<int, Entry> Entries =
                new Dictionary<int, Entry>();
            internal bool Completed;

            internal float CurrentScore(
                int index,
                float fallback)
            {
                Entry entry;

                return Entries.TryGetValue(
                        index,
                        out entry)
                    ? entry.WorkingScore
                    : fallback;
            }

            internal int CurrentBestIndex(
                PartyThinkParams thinkParams)
            {
                return FindWorkingBestIndex(
                    this,
                    thinkParams);
            }
            internal void ApplyFactor(
                int index,
                string source,
                float beforeScore,
                float factor,
                string reason)
            {
                if (Completed ||
                    index < 0 ||
                    ThinkParams == null ||
                    index >= CandidateCount ||
                    factor <= 0f)
                {
                    return;
                }

                Entry entry;
                if (!Entries.TryGetValue(index, out entry))
                {
                    entry = new Entry
                    {
                        Index = index,
                        OriginalScore = beforeScore,
                        WorkingScore = beforeScore
                    };

                    Entries.Add(index, entry);
                }
                else if (!NearlyEqual(
                    entry.WorkingScore,
                    beforeScore))
                {
                    Interlocked.Increment(
                        ref _sequenceMismatches);

                    ClanAIPostVanilla.WriteExternalLog(
                        "STRATEGIC_COMPOSER_SEQUENCE_MISMATCH" +
                        " actor=" +
                        ActorName(Actor) +
                        " index=" +
                        index +
                        " expected=" +
                        entry.WorkingScore.ToString(
                            "R",
                            CultureInfo.InvariantCulture) +
                        " supplied=" +
                        beforeScore.ToString(
                            "R",
                            CultureInfo.InvariantCulture) +
                        " source=" +
                        (source ?? "<unknown>"));
                }

                float afterScore =
                    entry.WorkingScore * factor;

                entry.Contributions.Add(
                    new Contribution
                    {
                        Source = source ?? "<unknown>",
                        Reason = reason ?? "<none>",
                        BeforeScore = entry.WorkingScore,
                        Factor = factor,
                        AfterScore = afterScore,
                        Applied = true
                    });

                entry.WorkingScore =
                    afterScore;

                Interlocked.Increment(
                    ref _appliedContributions);
            }
            internal void RecordProposal(
                int index,
                string source,
                float beforeScore,
                float factor,
                string reason)
            {
                if (Completed ||
                    index < 0 ||
                    ThinkParams == null ||
                    index >= CandidateCount ||
                    factor <= 0f)
                {
                    return;
                }

                Entry entry;
                if (!Entries.TryGetValue(index, out entry))
                {
                    entry = new Entry
                    {
                        Index = index,
                        OriginalScore = beforeScore,
                        WorkingScore = beforeScore
                    };

                    Entries.Add(index, entry);
                }

                entry.Contributions.Add(
                    new Contribution
                    {
                        Source = source ?? "<unknown>",
                        Reason = reason ?? "<none>",
                        BeforeScore = beforeScore,
                        Factor = factor,
                        AfterScore = beforeScore * factor,
                        Applied = false
                    });

                Interlocked.Increment(
                    ref _proposalContributions);
            }
        }

        internal static void Reset()
        {
            Interlocked.Exchange(ref _frames, 0);
            Interlocked.Exchange(
                ref _framesWithAppliedContributions,
                0);
            Interlocked.Exchange(
                ref _appliedContributions,
                0);
            Interlocked.Exchange(
                ref _proposalContributions,
                0);
            Interlocked.Exchange(ref _scoreWrites, 0);
            Interlocked.Exchange(
                ref _scoreMismatches,
                0);
            Interlocked.Exchange(
                ref _sequenceMismatches,
                0);
            Interlocked.Exchange(
                ref _winnerMismatches,
                0);
            Interlocked.Exchange(
                ref _equivalentFrames,
                0);
            Interlocked.Exchange(ref _failures, 0);

            ClanAIPostVanilla.WriteExternalLog(
                "STRATEGIC_COMPOSER_RESET mode=single-write-owner");
        }
        internal static Frame Begin(
            MobileParty actor,
            PartyThinkParams thinkParams)
        {
            try
            {
                if (actor == null ||
                    thinkParams == null ||
                    thinkParams.AIBehaviorScores.Count == 0)
                {
                    return null;
                }

                Interlocked.Increment(ref _frames);

                return new Frame
                {
                    Actor = actor,
                    ThinkParams = thinkParams,
                    CandidateCount =
                        thinkParams.AIBehaviorScores.Count,
                    BeforeWinner =
                        FindActualBestIndex(thinkParams)
                };
            }
            catch
            {
                Interlocked.Increment(ref _failures);
                return null;
            }
        }

        internal static void Complete(
            Frame frame,
            PartyThinkParams thinkParams)
        {
            if (frame == null ||
                frame.Completed)
            {
                return;
            }

            frame.Completed = true;

            try
            {
                if (thinkParams == null ||
                    thinkParams.AIBehaviorScores.Count !=
                        frame.CandidateCount)
                {
                    Interlocked.Increment(
                        ref _failures);

                    ClanAIPostVanilla.WriteExternalLog(
                        "STRATEGIC_COMPOSER_FAILURE" +
                        " reason=candidate-count-changed" +
                        " actor=" +
                        ActorName(frame.Actor));
                    return;
                }

                int predictedWinner =
                    FindWorkingBestIndex(
                        frame,
                        thinkParams);

                bool hasApplied = false;
                foreach (KeyValuePair<int, Entry> pair
                    in frame.Entries)
                {
                    Entry entry = pair.Value;
                    bool entryHasApplied = false;

                    for (int i = 0;
                         i < entry.Contributions.Count;
                         i++)
                    {
                        if (entry.Contributions[i].Applied)
                        {
                            entryHasApplied = true;
                            break;
                        }
                    }

                    if (!entryHasApplied)
                        continue;

                    hasApplied = true;

                    AIBehaviorData data =
                        thinkParams
                            .AIBehaviorScores[entry.Index]
                            .Item1;

                    thinkParams.SetBehaviorScore(
                        in data,
                        entry.WorkingScore);

                    Interlocked.Increment(
                        ref _scoreWrites);
                }

                if (hasApplied)
                {
                    Interlocked.Increment(
                        ref _framesWithAppliedContributions);
                }

                bool mismatch = false;

                foreach (KeyValuePair<int, Entry> pair
                    in frame.Entries)
                {
                    Entry entry = pair.Value;
                    bool entryHasApplied = false;

                    for (int i = 0;
                         i < entry.Contributions.Count;
                         i++)
                    {
                        if (entry.Contributions[i].Applied)
                        {
                            entryHasApplied = true;
                            break;
                        }
                    }

                    if (!entryHasApplied)
                        continue;

                    float actual =
                        thinkParams
                            .AIBehaviorScores[entry.Index]
                            .Item2;

                    if (!NearlyEqual(
                        entry.WorkingScore,
                        actual))
                    {
                        mismatch = true;

                        Interlocked.Increment(
                            ref _scoreMismatches);

                        ClanAIPostVanilla.WriteExternalLog(
                            "STRATEGIC_COMPOSER_SCORE_MISMATCH" +
                            " actor=" +
                            ActorName(frame.Actor) +
                            " index=" +
                            entry.Index +
                            " predicted=" +
                            entry.WorkingScore.ToString(
                                "R",
                                CultureInfo.InvariantCulture) +
                            " actual=" +
                            actual.ToString(
                                "R",
                                CultureInfo.InvariantCulture) +
                            " contributions=" +
                            ContributionText(entry));
                    }
                }
                int actualWinner =
                    FindActualBestIndex(thinkParams);

                if (actualWinner !=
                    predictedWinner)
                {
                    mismatch = true;

                    Interlocked.Increment(
                        ref _winnerMismatches);

                    ClanAIPostVanilla.WriteExternalLog(
                        "STRATEGIC_COMPOSER_WINNER_MISMATCH" +
                        " actor=" +
                        ActorName(frame.Actor) +
                        " beforeWinner=" +
                        frame.BeforeWinner +
                        " predictedWinner=" +
                        predictedWinner +
                        " actualWinner=" +
                        actualWinner);
                }

                if (!mismatch)
                {
                    ActorStrategicBlackboard.Observe(
                        frame.Actor,
                        thinkParams,
                        actualWinner,
                        frame);
                }

                if (hasApplied &&
                    !mismatch)
                {
                    long equivalent =
                        Interlocked.Increment(
                            ref _equivalentFrames);

                    if (equivalent <= 5 ||
                        (equivalent % 250) == 0)
                    {
                        ClanAIPostVanilla.WriteExternalLog(
                            "STRATEGIC_COMPOSER_OWNER_EQUIVALENT" +
                            " actor=" +
                            ActorName(frame.Actor) +
                            " contributions=" +
                            CountApplied(frame) +
                            " proposals=" +
                            CountProposals(frame) +
                            " writes=" +
                            CountAppliedEntries(frame) +
                            " beforeWinner=" +
                            frame.BeforeWinner +
                            " finalWinner=" +
                            actualWinner +
                            " equivalentFrames=" +
                            equivalent);
                    }
                }
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref _failures);

                ClanAIPostVanilla.WriteExternalLog(
                    "STRATEGIC_COMPOSER_FAILURE" +
                    " reason=exception" +
                    " actor=" +
                    ActorName(frame.Actor) +
                    " type=" +
                    ex.GetType().Name);
            }
        }
        internal static string SummaryFields()
        {
            return
                " composerFrames=" +
                Interlocked.Read(ref _frames) +
                " composerAppliedFrames=" +
                Interlocked.Read(
                    ref _framesWithAppliedContributions) +
                " composerAppliedContributions=" +
                Interlocked.Read(
                    ref _appliedContributions) +
                " composerProposalContributions=" +
                Interlocked.Read(
                    ref _proposalContributions) +
                " composerScoreWrites=" +
                Interlocked.Read(ref _scoreWrites) +
                " composerScoreMismatches=" +
                Interlocked.Read(
                    ref _scoreMismatches) +
                " composerSequenceMismatches=" +
                Interlocked.Read(
                    ref _sequenceMismatches) +
                " composerWinnerMismatches=" +
                Interlocked.Read(
                    ref _winnerMismatches) +
                " composerEquivalentFrames=" +
                Interlocked.Read(
                    ref _equivalentFrames) +
                " composerFailures=" +
                Interlocked.Read(ref _failures);
        }

        private static int FindWorkingBestIndex(
            Frame frame,
            PartyThinkParams thinkParams)
        {
            int bestIndex = -1;
            float bestScore = float.MinValue;

            for (int i = 0;
                 i < thinkParams.AIBehaviorScores.Count;
                 i++)
            {
                float score =
                    frame.CurrentScore(
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

        private static int FindActualBestIndex(
            PartyThinkParams thinkParams)
        {
            int bestIndex = -1;
            float bestScore = float.MinValue;

            for (int i = 0;
                 i < thinkParams.AIBehaviorScores.Count;
                 i++)
            {
                float score =
                    thinkParams
                        .AIBehaviorScores[i]
                        .Item2;

                if (bestIndex < 0 ||
                    score > bestScore)
                {
                    bestIndex = i;
                    bestScore = score;
                }
            }

            return bestIndex;
        }
        private static bool NearlyEqual(
            float a,
            float b)
        {
            float diff =
                Math.Abs(a - b);

            if (diff <= AbsoluteTolerance)
                return true;

            float scale =
                Math.Max(
                    1f,
                    Math.Max(
                        Math.Abs(a),
                        Math.Abs(b)));

            return
                diff <=
                (RelativeTolerance * scale);
        }

        private static string ContributionText(
            Entry entry)
        {
            if (entry == null ||
                entry.Contributions.Count == 0)
            {
                return "<none>";
            }

            List<string> parts =
                new List<string>();

            for (int i = 0;
                 i < entry.Contributions.Count;
                 i++)
            {
                Contribution c =
                    entry.Contributions[i];

                parts.Add(
                    c.Source +
                    ":" +
                    c.Reason +
                    ":x" +
                    c.Factor.ToString(
                        "R",
                        CultureInfo.InvariantCulture) +
                    ":" +
                    (c.Applied
                        ? "applied"
                        : "proposal"));
            }

            return string.Join("|", parts);
        }

        private static int CountApplied(
            Frame frame)
        {
            int count = 0;

            foreach (Entry entry in
                frame.Entries.Values)
            {
                for (int i = 0;
                     i < entry.Contributions.Count;
                     i++)
                {
                    if (entry.Contributions[i].Applied)
                        count++;
                }
            }

            return count;
        }
        private static int CountProposals(
            Frame frame)
        {
            int count = 0;

            foreach (Entry entry in
                frame.Entries.Values)
            {
                for (int i = 0;
                     i < entry.Contributions.Count;
                     i++)
                {
                    if (!entry.Contributions[i].Applied)
                        count++;
                }
            }

            return count;
        }

        private static int CountAppliedEntries(
            Frame frame)
        {
            int count = 0;

            foreach (Entry entry in
                frame.Entries.Values)
            {
                for (int i = 0;
                     i < entry.Contributions.Count;
                     i++)
                {
                    if (entry.Contributions[i].Applied)
                    {
                        count++;
                        break;
                    }
                }
            }

            return count;
        }

        private static string ActorName(
            MobileParty actor)
        {
            if (actor == null)
                return "<null>";

            if (actor.LeaderHero != null)
                return actor.LeaderHero.Name.ToString();

            return actor.Name.ToString();
        }
    }
}
