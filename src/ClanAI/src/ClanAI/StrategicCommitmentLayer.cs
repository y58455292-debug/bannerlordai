using System;
using System.Globalization;
using System.Threading;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;

namespace ClanAI
{
    internal static class StrategicCommitmentLayer
    {
        internal const float RetentionFactor = 1.10f;
        internal const double MaxAgeHours = 12.0;

        private static long _evaluations;
        private static long _priorStateFound;
        private static long _naturalObjectiveChanges;
        private static long _crossStateSkipped;
        private static long _ageExpired;
        private static long _previousMissing;
        private static long _sameStateEligible;
        private static long _wouldRetain;
        private static long _applied;
        private static long _actualRetains;
        private static long _failures;

        internal static void Reset()
        {
            Interlocked.Exchange(ref _evaluations, 0);
            Interlocked.Exchange(ref _priorStateFound, 0);
            Interlocked.Exchange(ref _naturalObjectiveChanges, 0);
            Interlocked.Exchange(ref _crossStateSkipped, 0);
            Interlocked.Exchange(ref _ageExpired, 0);
            Interlocked.Exchange(ref _previousMissing, 0);
            Interlocked.Exchange(ref _sameStateEligible, 0);
            Interlocked.Exchange(ref _wouldRetain, 0);
            Interlocked.Exchange(ref _applied, 0);
            Interlocked.Exchange(ref _actualRetains, 0);
            Interlocked.Exchange(ref _failures, 0);

            StrategicCommitmentConfig.EnsureLoaded();

            ClanAIPostVanilla.WriteExternalLog(
                "STRATEGIC_COMMITMENT_RESET" +
                " mode=" +
                StrategicCommitmentConfig.Mode +
                " status=" +
                StrategicCommitmentConfig.Status +
                " factor=" +
                RetentionFactor.ToString(
                    "R",
                    CultureInfo.InvariantCulture) +
                " maxAgeHours=" +
                MaxAgeHours.ToString(
                    "R",
                    CultureInfo.InvariantCulture));
        }
        internal static string SummaryFields()
        {
            return
                " commitmentEvaluations=" +
                Interlocked.Read(ref _evaluations) +
                " commitmentPriorStateFound=" +
                Interlocked.Read(ref _priorStateFound) +
                " commitmentNaturalChanges=" +
                Interlocked.Read(ref _naturalObjectiveChanges) +
                " commitmentCrossStateSkipped=" +
                Interlocked.Read(ref _crossStateSkipped) +
                " commitmentAgeExpired=" +
                Interlocked.Read(ref _ageExpired) +
                " commitmentPreviousMissing=" +
                Interlocked.Read(ref _previousMissing) +
                " commitmentSameStateEligible=" +
                Interlocked.Read(ref _sameStateEligible) +
                " commitmentWouldRetain=" +
                Interlocked.Read(ref _wouldRetain) +
                " commitmentApplied=" +
                Interlocked.Read(ref _applied) +
                " commitmentActualRetains=" +
                Interlocked.Read(ref _actualRetains) +
                " commitmentFailures=" +
                Interlocked.Read(ref _failures);
        }

        internal static void Evaluate(
            MobileParty actor,
            PartyThinkParams thinkParams,
            StrategicDecisionComposer.Frame composer)
        {
            try
            {
                if (actor == null ||
                    actor.LeaderHero == null ||
                    thinkParams == null ||
                    composer == null ||
                    thinkParams.AIBehaviorScores.Count == 0)
                {
                    return;
                }

                Interlocked.Increment(ref _evaluations);

                ActorStrategicBlackboard.CommitmentState previous;

                if (!ActorStrategicBlackboard.TryGetCommitmentState(
                        actor,
                        out previous))
                {
                    return;
                }

                Interlocked.Increment(
                    ref _priorStateFound);

                int naturalWinner =
                    composer.CurrentBestIndex(
                        thinkParams);

                if (naturalWinner < 0)
                    return;

                AIBehaviorData naturalData =
                    thinkParams
                        .AIBehaviorScores[naturalWinner]
                        .Item1;

                string naturalSignature =
                    ActorStrategicBlackboard.Signature(
                        naturalData);

                if (string.Equals(
                        naturalSignature,
                        previous.ObjectiveSignature,
                        StringComparison.Ordinal))
                {
                    return;
                }

                Interlocked.Increment(
                    ref _naturalObjectiveChanges);

                bool weak =
                    actor.PartySizeRatio < 0.72f ||
                    actor.GetNumDaysForFoodToLast() < 3;

                ActorStrategicBlackboard.CoarseState naturalState =
                    ActorStrategicBlackboard.ClassifyState(
                        naturalData,
                        naturalWinner,
                        weak,
                        composer);

                if (naturalState != previous.State)
                {
                    Interlocked.Increment(
                        ref _crossStateSkipped);

                    return;
                }
                double ageHours =
                    CampaignTime.Now.ToHours -
                    previous.ObjectiveSinceHours;

                if (ageHours < 0.0)
                {
                    Interlocked.Increment(
                        ref _failures);

                    ClanAIPostVanilla.WriteExternalLog(
                        "STRATEGIC_COMMITMENT_FAILURE" +
                        " actor=" +
                        actor.LeaderHero.Name.ToString() +
                        " reason=negative-objective-age" +
                        " ageHours=" +
                        ageHours.ToString(
                            "R",
                            CultureInfo.InvariantCulture));

                    return;
                }

                if (ageHours > MaxAgeHours)
                {
                    Interlocked.Increment(
                        ref _ageExpired);

                    return;
                }

                float previousScore;

                int previousIndex =
                    ActorStrategicBlackboard.FindObjectiveIndex(
                        thinkParams,
                        composer,
                        previous.ObjectiveSignature,
                        out previousScore);

                if (previousIndex < 0 ||
                    previousScore <= 0f ||
                    float.IsNaN(previousScore) ||
                    float.IsInfinity(previousScore))
                {
                    Interlocked.Increment(
                        ref _previousMissing);

                    return;
                }

                Interlocked.Increment(
                    ref _sameStateEligible);

                float naturalScore =
                    composer.CurrentScore(
                        naturalWinner,
                        thinkParams
                            .AIBehaviorScores[naturalWinner]
                            .Item2);

                if (naturalScore <= 0f ||
                    float.IsNaN(naturalScore) ||
                    float.IsInfinity(naturalScore))
                {
                    return;
                }

                float retainedScore =
                    previousScore *
                    RetentionFactor;

                if (retainedScore <= naturalScore)
                    return;

                Interlocked.Increment(
                    ref _wouldRetain);

                float requiredFactor =
                    naturalScore /
                    previousScore;

                StrategicCommitmentMode mode =
                    StrategicCommitmentConfig.Mode;

                if (mode ==
                    StrategicCommitmentMode.Apply)
                {
                    composer.ApplyFactor(
                        previousIndex,
                        "commitment",
                        previousScore,
                        RetentionFactor,
                        "same-state-objective-retention");

                    Interlocked.Increment(
                        ref _applied);

                    int afterWinner =
                        composer.CurrentBestIndex(
                            thinkParams);

                    string afterSignature =
                        afterWinner >= 0
                            ? ActorStrategicBlackboard.Signature(
                                thinkParams
                                    .AIBehaviorScores[afterWinner]
                                    .Item1)
                            : "<none>";

                    bool retained =
                        string.Equals(
                            afterSignature,
                            previous.ObjectiveSignature,
                            StringComparison.Ordinal);

                    if (retained)
                    {
                        Interlocked.Increment(
                            ref _actualRetains);
                    }

                    LogDecision(
                        "STRATEGIC_COMMITMENT_APPLY",
                        actor,
                        previous,
                        naturalData,
                        previousScore,
                        naturalScore,
                        retainedScore,
                        requiredFactor,
                        ageHours,
                        retained);
                }
                else
                {
                    LogDecision(
                        "STRATEGIC_COMMITMENT_WOULD_RETAIN",
                        actor,
                        previous,
                        naturalData,
                        previousScore,
                        naturalScore,
                        retainedScore,
                        requiredFactor,
                        ageHours,
                        true);
                }
            }
            catch (Exception ex)
            {
                Interlocked.Increment(
                    ref _failures);

                ClanAIPostVanilla.WriteExternalLog(
                    "STRATEGIC_COMMITMENT_FAILURE" +
                    " actor=" +
                    (actor == null ||
                     actor.LeaderHero == null
                        ? "<none>"
                        : actor.LeaderHero.Name.ToString()) +
                    " reason=exception" +
                    " type=" +
                    ex.GetType().Name);
            }
        }
        private static void LogDecision(
            string kind,
            MobileParty actor,
            ActorStrategicBlackboard.CommitmentState previous,
            AIBehaviorData naturalData,
            float previousScore,
            float naturalScore,
            float retainedScore,
            float requiredFactor,
            double ageHours,
            bool retained)
        {
            ClanAIPostVanilla.WriteExternalLog(
                kind +
                " actor=" +
                actor.LeaderHero.Name.ToString() +
                " state=" +
                previous.State +
                " previous=" +
                previous.ObjectiveLabel +
                " natural=" +
                ActorStrategicBlackboard.Label(
                    naturalData) +
                " ageHours=" +
                ageHours.ToString(
                    "R",
                    CultureInfo.InvariantCulture) +
                " previousScore=" +
                previousScore.ToString(
                    "R",
                    CultureInfo.InvariantCulture) +
                " naturalScore=" +
                naturalScore.ToString(
                    "R",
                    CultureInfo.InvariantCulture) +
                " retainedScore=" +
                retainedScore.ToString(
                    "R",
                    CultureInfo.InvariantCulture) +
                " requiredFactor=" +
                requiredFactor.ToString(
                    "R",
                    CultureInfo.InvariantCulture) +
                " configuredFactor=" +
                RetentionFactor.ToString(
                    "R",
                    CultureInfo.InvariantCulture) +
                " retained=" +
                retained +
                " mode=" +
                StrategicCommitmentConfig.Mode);
        }
    }
}
