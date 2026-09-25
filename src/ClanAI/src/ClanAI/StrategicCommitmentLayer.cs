using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace ClanAI
{
    internal static class StrategicCommitmentLayer
    {
        internal const float RetentionFactor =
            StrategicCommitmentPolicy.RetentionFactor;
        internal const double MaxAgeHours =
            StrategicCommitmentPolicy.MaxAgeHours;

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
        private static long _commitChecks;
        private static long _commitMatches;
        private static long _commitExpiries;
        private static long _failures;

        private sealed class PendingCommit
        {
            internal string PartyId;
            internal string ActorName;
            internal AiBehavior ExpectedBehavior;
            internal string ExpectedTargetKey;
            internal string ExpectedTargetName;
            internal string PreviousObjectiveLabel;
            internal string PreviousObjectiveSignature;
            internal double CreatedAtHours;
        }

        private static readonly Dictionary<string, PendingCommit>
            PendingCommitByParty =
                new Dictionary<string, PendingCommit>(
                    StringComparer.Ordinal);

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
            Interlocked.Exchange(ref _commitChecks, 0);
            Interlocked.Exchange(ref _commitMatches, 0);
            Interlocked.Exchange(ref _commitExpiries, 0);
            Interlocked.Exchange(ref _failures, 0);

            PendingCommitByParty.Clear();

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
                " commitmentCommitChecks=" +
                Interlocked.Read(ref _commitChecks) +
                " commitmentCommitMatches=" +
                Interlocked.Read(ref _commitMatches) +
                " commitmentCommitExpiries=" +
                Interlocked.Read(ref _commitExpiries) +
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
                VerifyPendingCommit(actor);

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

                double ageHours =
                    CampaignTime.Now.ToHours -
                    previous.ObjectiveSinceHours;

                StrategicCommitmentContextResult context =
                    StrategicCommitmentPolicy.EvaluateContext(
                        true,
                        false,
                        naturalState == previous.State,
                        ageHours);

                if (context.CrossState)
                {
                    Interlocked.Increment(
                        ref _crossStateSkipped);

                    return;
                }

                if (context.NegativeAge)
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

                if (context.Expired)
                {
                    Interlocked.Increment(
                        ref _ageExpired);

                    return;
                }

                if (!context.Continue)
                    return;

                float previousScore;

                int previousIndex =
                    ActorStrategicBlackboard.FindObjectiveIndex(
                        thinkParams,
                        composer,
                        previous.ObjectiveSignature,
                        out previousScore);

                if (previousIndex < 0 ||
                    !StrategicCommitmentPolicy.ValidPositiveScore(
                        previousScore))
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

                StrategicCommitmentMode mode =
                    StrategicCommitmentConfig.Mode;

                StrategicCommitmentScoreResult scoreDecision =
                    StrategicCommitmentPolicy.EvaluateScores(
                        true,
                        previousScore,
                        naturalScore,
                        mode);

                if (!scoreDecision.NaturalScoreValid)
                    return;

                if (!scoreDecision.WouldRetain)
                    return;

                Interlocked.Increment(
                    ref _wouldRetain);

                float retainedScore =
                    scoreDecision.RetainedScore;

                float requiredFactor =
                    scoreDecision.RequiredFactor;

                if (scoreDecision.Apply)
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

                        if (StrategicCommitmentCommitPolicy
                            .ShouldCreateExpectation(
                                mode ==
                                    StrategicCommitmentMode.Apply,
                                scoreDecision.Apply,
                                retained,
                                retained))
                        {
                            RecordPendingCommit(
                                actor,
                                thinkParams,
                                afterWinner,
                                previous);
                        }
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
        private static void RecordPendingCommit(
            MobileParty actor,
            PartyThinkParams thinkParams,
            int winnerIndex,
            ActorStrategicBlackboard.CommitmentState previous)
        {
            if (actor == null ||
                thinkParams == null ||
                previous == null ||
                winnerIndex < 0 ||
                winnerIndex >=
                    thinkParams.AIBehaviorScores.Count)
            {
                return;
            }

            AIBehaviorData winner =
                thinkParams
                    .AIBehaviorScores[winnerIndex]
                    .Item1;

            string targetName;
            string targetKey =
                CandidateTargetKey(
                    winner,
                    previous.ObjectiveSignature,
                    out targetName);

            string partyId =
                PartyKey(actor);

            if (string.IsNullOrEmpty(partyId) ||
                string.IsNullOrEmpty(targetKey))
            {
                return;
            }

            PendingCommitByParty[partyId] =
                new PendingCommit
                {
                    PartyId = partyId,
                    ActorName =
                        actor.LeaderHero == null
                            ? actor.Name.ToString()
                            : actor.LeaderHero.Name.ToString(),
                    ExpectedBehavior =
                        winner.AiBehavior,
                    ExpectedTargetKey =
                        targetKey,
                    ExpectedTargetName =
                        targetName,
                    PreviousObjectiveLabel =
                        previous.ObjectiveLabel,
                    PreviousObjectiveSignature =
                        previous.ObjectiveSignature,
                    CreatedAtHours =
                        CampaignTime.Now.ToHours
                };
        }

        private static void VerifyPendingCommit(
            MobileParty actor)
        {
            if (actor == null)
                return;

            string partyId =
                PartyKey(actor);

            PendingCommit pending;
            if (!PendingCommitByParty.TryGetValue(
                    partyId,
                    out pending))
            {
                return;
            }

            _commitChecks++;

            string actualTargetKey = null;
            string actualTargetName = "<none>";
            string arrivedTargetKey = null;
            string arrivedTargetName = "<none>";

            if (pending.ExpectedTargetKey.StartsWith(
                    "S:",
                    StringComparison.Ordinal))
            {
                Settlement actualSettlement =
                    actor.TargetSettlement ??
                    actor.ShortTermTargetSettlement ??
                    actor.BesiegedSettlement ??
                    actor.CurrentSettlement;

                actualTargetKey =
                    SettlementTargetKey(
                        actualSettlement);

                actualTargetName =
                    actualSettlement == null
                        ? "<none>"
                        : actualSettlement.Name.ToString();

                arrivedTargetKey =
                    SettlementTargetKey(
                        actor.CurrentSettlement);

                arrivedTargetName =
                    actor.CurrentSettlement == null
                        ? "<none>"
                        : actor.CurrentSettlement.Name.ToString();
            }
            else if (pending.ExpectedTargetKey.StartsWith(
                         "P:",
                         StringComparison.Ordinal))
            {
                MobileParty actualParty =
                    actor.TargetParty ??
                    actor.ShortTermTargetParty;

                actualTargetKey =
                    MobilePartyTargetKey(
                        actualParty);

                actualTargetName =
                    actualParty == null
                        ? "<none>"
                        : actualParty.Name.ToString();
            }

            StrategicCommitmentCommitCheckResult result =
                StrategicCommitmentCommitPolicy.Evaluate(
                    pending.ExpectedBehavior.ToString(),
                    pending.ExpectedTargetKey,
                    actor.DefaultBehavior.ToString(),
                    actor.ShortTermBehavior.ToString(),
                    actualTargetKey,
                    arrivedTargetKey,
                    pending.CreatedAtHours,
                    CampaignTime.Now.ToHours);

            if (result.Remove)
            {
                PendingCommitByParty.Remove(
                    partyId);

                if (result.Matched)
                    _commitMatches++;
                else if (result.Expired)
                    _commitExpiries++;
            }

            ClanAIPostVanilla.WriteExternalLog(
                "STRATEGIC_COMMITMENT_COMMIT_CHECK" +
                " actor=" + pending.ActorName +
                " partyId=" + pending.PartyId +
                " previous=" +
                    pending.PreviousObjectiveLabel +
                " previousSignature=" +
                    pending.PreviousObjectiveSignature +
                " expectedBehavior=" +
                    pending.ExpectedBehavior +
                " expectedTarget=" +
                    pending.ExpectedTargetName +
                " expectedTargetKey=" +
                    pending.ExpectedTargetKey +
                " actualDefault=" +
                    actor.DefaultBehavior +
                " actualShort=" +
                    actor.ShortTermBehavior +
                " actualTarget=" +
                    actualTargetName +
                " actualTargetKey=" +
                    (actualTargetKey ?? "<none>") +
                " arrivedTarget=" +
                    arrivedTargetName +
                " arrivedTargetKey=" +
                    (arrivedTargetKey ?? "<none>") +
                " behaviorMatch=" +
                    result.BehaviorMatch +
                " targetMatch=" +
                    result.TargetMatch +
                " arrivedMatch=" +
                    result.ArrivedMatch +
                " matched=" +
                    result.Matched +
                " expired=" +
                    result.Expired +
                " ageHours=" +
                    result.AgeHours.ToString(
                        "0.###",
                        CultureInfo.InvariantCulture) +
                " checks=" +
                    _commitChecks +
                " matches=" +
                    _commitMatches +
                " expiries=" +
                    _commitExpiries);
        }

        private static string PartyKey(
            MobileParty actor)
        {
            if (actor == null)
                return null;

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

            return actor.Name.ToString();
        }

        private static string CandidateTargetKey(
            AIBehaviorData data,
            string objectiveSignature,
            out string targetName)
        {
            Settlement settlement =
                data.Party as Settlement;

            if (settlement != null)
            {
                targetName =
                    settlement.Name.ToString();

                return
                    SettlementTargetKey(
                        settlement);
            }

            MobileParty party =
                data.Party as MobileParty;

            if (party != null)
            {
                targetName =
                    party.Name.ToString();

                return
                    MobilePartyTargetKey(
                        party);
            }

            targetName = "<point>";

            if (string.IsNullOrEmpty(
                    objectiveSignature))
            {
                return null;
            }

            int separator =
                objectiveSignature.IndexOf('|');

            if (separator < 0 ||
                separator + 1 >=
                    objectiveSignature.Length)
            {
                return null;
            }

            return objectiveSignature.Substring(
                separator + 1);
        }

        private static string SettlementTargetKey(
            Settlement settlement)
        {
            return settlement == null ||
                   string.IsNullOrEmpty(
                       settlement.StringId)
                ? null
                : "S:" + settlement.StringId;
        }

        private static string MobilePartyTargetKey(
            MobileParty party)
        {
            return party == null ||
                   string.IsNullOrEmpty(
                       party.StringId)
                ? null
                : "P:" + party.StringId;
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