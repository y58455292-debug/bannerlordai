using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace ClanAI
{
    internal static class SocialMemoryCausalLayer
    {
        // Review calibration for the causal milestone.
        // History may bias an EXISTING aggressive candidate only.
        internal const float PressureScale = 0.0075f;
        internal const float MaxAbsDelta = 0.12f;
        internal const float MinCandidateScore = 0.10f;

        private static long _decisionsWithMemory;
        private static long _wouldFlip;
        private static long _actualFlip;
        private static long _scoreWrites;
        private static long _failures;

        internal static void Reset()
        {
            Interlocked.Exchange(ref _decisionsWithMemory, 0);
            Interlocked.Exchange(ref _wouldFlip, 0);
            Interlocked.Exchange(ref _actualFlip, 0);
            Interlocked.Exchange(ref _scoreWrites, 0);
            Interlocked.Exchange(ref _failures, 0);

            SocialMemoryCausalConfig.EnsureLoaded();

            ClanAIPostVanilla.WriteExternalLog(
                "MEMORY_CAUSAL_MODE mode=" +
                SocialMemoryCausalConfig.Mode +
                " status=" +
                SocialMemoryCausalConfig.Status +
                " pressureScale=" +
                PressureScale.ToString(
                    "R",
                    CultureInfo.InvariantCulture) +
                " maxAbsDelta=" +
                MaxAbsDelta.ToString(
                    "R",
                    CultureInfo.InvariantCulture));
        }

        internal static string SummaryFields()
        {
            return
                " memoryCausalDecisions=" +
                Interlocked.Read(ref _decisionsWithMemory) +
                " memoryCausalWouldFlip=" +
                Interlocked.Read(ref _wouldFlip) +
                " memoryCausalActualFlip=" +
                Interlocked.Read(ref _actualFlip) +
                " memoryCausalScoreWrites=" +
                Interlocked.Read(ref _scoreWrites) +
                " memoryCausalFailures=" +
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
                    thinkParams.AIBehaviorScores.Count == 0)
                {
                    return;
                }

                int beforeIndex = -1;
                float beforeScore = float.MinValue;
                int proposedIndex = -1;
                float proposedScore = float.MinValue;

                int relevantCount = 0;

                var changes =
                    new List<
                        ValueTuple<
                            int,
                            AIBehaviorData,
                            float,
                            float,
                            float>>();

                CandidateMemory strongest =
                    new CandidateMemory();

                CandidateMemory proposedMemory =
                    new CandidateMemory();

                for (int i = 0;
                     i < thinkParams.AIBehaviorScores.Count;
                     i++)
                {
                    var entry =
                        thinkParams.AIBehaviorScores[i];

                    AIBehaviorData data =
                        entry.Item1;

                    float rawScore =
                        entry.Item2;

                    float baseScore =
                        composer != null
                            ? composer.CurrentScore(
                                i,
                                rawScore)
                            : rawScore;

                    if (baseScore > beforeScore)
                    {
                        beforeScore = baseScore;
                        beforeIndex = i;
                    }

                    float factor = 1f;

                    CandidateMemory memory;
                    if (TryMemoryFactor(
                            actor,
                            data,
                            baseScore,
                            out factor,
                            out memory))
                    {
                        relevantCount++;

                        float adjusted =
                            baseScore * factor;

                        changes.Add(
                            new ValueTuple<
                                int,
                                AIBehaviorData,
                                float,
                                float,
                                float>(
                                    i,
                                    data,
                                    baseScore,
                                    factor,
                                    adjusted));

                        memory.Index = i;
                        memory.BaseScore = baseScore;
                        memory.AdjustedScore = adjusted;
                        float absDelta =
                            Math.Abs(factor - 1f);

                        if (!strongest.Valid ||
                            absDelta >
                                Math.Abs(
                                    strongest.Factor - 1f))
                        {
                            strongest = memory;
                        }
                    }

                    float candidateProposed =
                        baseScore * factor;

                    if (candidateProposed >
                        proposedScore)
                    {
                        proposedScore =
                            candidateProposed;
                        proposedIndex = i;

                        proposedMemory =
                            memory != null &&
                            memory.Valid
                                ? memory
                                : new CandidateMemory();
                    }
                }

                if (relevantCount == 0)
                    return;
                Interlocked.Increment(
                    ref _decisionsWithMemory);

                bool wouldFlip =
                    beforeIndex != proposedIndex;

                if (wouldFlip)
                {
                    Interlocked.Increment(
                        ref _wouldFlip);
                }

                SocialMemoryCausalMode mode =
                    SocialMemoryCausalConfig.Mode;

                int afterIndex = beforeIndex;

                if (mode ==
                        SocialMemoryCausalMode.Apply)
                {
                    for (int i = 0;
                         i < changes.Count;
                         i++)
                    {
                        int index =
                            changes[i].Item1;

                        AIBehaviorData data =
                            changes[i].Item2;

                        float baseScore =
                            changes[i].Item3;

                        float factor =
                            changes[i].Item4;

                        float score =
                            changes[i].Item5;

                        if (composer == null)
                        {
                            throw new InvalidOperationException(
                                "Strategic composer missing in social-memory Apply scope.");
                        }

                        composer.ApplyFactor(
                            index,
                            "social-memory",
                            baseScore,
                            factor,
                            "persistent-social-memory");
                    }

                    GenerationalContinuityRuntimeTelemetry
                        .ObserveHeroMemoryResolution(
                            "SocialLedgerStrategicApply",
                            actor.LeaderHero.StringId,
                            actor.LeaderHero.StringId,
                            true);

                    afterIndex =
                        composer.CurrentBestIndex(
                            thinkParams);

                    if (afterIndex != beforeIndex)
                    {
                        Interlocked.Increment(
                            ref _actualFlip);
                    }
                }
                else if (composer != null)
                {
                    for (int i = 0;
                         i < changes.Count;
                         i++)
                    {
                        composer.RecordProposal(
                            changes[i].Item1,
                            "social-memory",
                            changes[i].Item3,
                            changes[i].Item4,
                            "persistent-social-memory");
                    }
                }

                if (wouldFlip ||
                    (mode ==
                        SocialMemoryCausalMode.Apply &&
                     afterIndex != beforeIndex))
                {
                    CandidateMemory causalMemory =
                        proposedMemory.Valid
                            ? proposedMemory
                            : strongest;

                    LogCausalEvent(
                        actor,
                        thinkParams,
                        beforeIndex,
                        beforeScore,
                        proposedIndex,
                        proposedScore,
                        afterIndex,
                        relevantCount,
                        causalMemory,
                        mode);
                }
                else if (strongest.Valid)
                {
                    LogNearTieIfUseful(
                        actor,
                        thinkParams,
                        beforeIndex,
                        beforeScore,
                        strongest,
                        mode);
                }
            }
            catch
            {
                Interlocked.Increment(ref _failures);
            }
        }

        private static bool TryMemoryFactor(
            MobileParty actor,
            AIBehaviorData data,
            float baseScore,
            out float factor,
            out CandidateMemory memory)
        {
            factor = 1f;
            memory = new CandidateMemory();

            if (baseScore <= MinCandidateScore ||
                !IsAggressive(data.AiBehavior))
            {
                return false;
            }

            Clan targetClan =
                ResolveTargetClan(data);

            if (targetClan == null)
                return false;

            int trust;
            int grievance;
            int bloodDebt;
            int obligation;
            int tension;

            if (!SocialLedger.TryGetState(
                    actor.LeaderHero,
                    targetClan,
                    out trust,
                    out grievance,
                    out bloodDebt,
                    out obligation,
                    out tension))
            {
                return false;
            }

            if (trust == 0 &&
                grievance == 0 &&
                bloodDebt == 0 &&
                obligation == 0 &&
                tension == 0)
            {
                return false;
            }

            int pressure =
                grievance +
                tension +
                (bloodDebt * 2) -
                trust -
                obligation;

            float delta =
                pressure * PressureScale;

            if (delta > MaxAbsDelta)
                delta = MaxAbsDelta;

            if (delta < -MaxAbsDelta)
                delta = -MaxAbsDelta;

            if (Math.Abs(delta) < 0.0001f)
                return false;

            factor = 1f + delta;

            memory.Valid = true;
            memory.Behavior =
                data.AiBehavior.ToString();
            memory.Target =
                TargetName(data);
            memory.TargetClan =
                targetClan.Name.ToString();
            memory.Pressure = pressure;
            memory.Factor = factor;
            memory.Trust = trust;
            memory.Grievance = grievance;
            memory.BloodDebt = bloodDebt;
            memory.Obligation = obligation;
            memory.Tension = tension;

            return true;
        }

        private static void LogCausalEvent(
            MobileParty actor,
            PartyThinkParams thinkParams,
            int beforeIndex,
            float beforeScore,
            int proposedIndex,
            float proposedScore,
            int afterIndex,
            int relevantCount,
            CandidateMemory strongest,
            SocialMemoryCausalMode mode)
        {
            ClanAIPostVanilla.WriteExternalLog(
                (mode ==
                    SocialMemoryCausalMode.Apply
                    ? "MEMORY_CAUSAL_APPLY "
                    : "MEMORY_CAUSAL_WOULD_FLIP ") +
                "actor=" +
                actor.LeaderHero.Name.ToString() +
                " actorId=" +
                actor.LeaderHero.StringId +
                " faction=" +
                actor.MapFaction.Name.ToString() +
                " relevantCandidates=" +
                relevantCount +
                " before=" +
                CandidateLabel(
                    thinkParams,
                    beforeIndex) +
                " beforeScore=" +
                beforeScore.ToString(
                    "R",
                    CultureInfo.InvariantCulture) +
                " proposed=" +
                CandidateLabel(
                    thinkParams,
                    proposedIndex) +
                " proposedScore=" +
                proposedScore.ToString(
                    "R",
                    CultureInfo.InvariantCulture) +
                " after=" +
                CandidateLabel(
                    thinkParams,
                    afterIndex) +
                " targetClan=" +
                strongest.TargetClan +
                " memoryBehavior=" +
                strongest.Behavior +
                " memoryTarget=" +
                strongest.Target +
                " pressure=" +
                strongest.Pressure +
                " factor=" +
                strongest.Factor.ToString(
                    "R",
                    CultureInfo.InvariantCulture) +
                " trust=" +
                strongest.Trust +
                " grievance=" +
                strongest.Grievance +
                " bloodDebt=" +
                strongest.BloodDebt +
                " obligation=" +
                strongest.Obligation +
                " tension=" +
                strongest.Tension +
                " mode=" +
                mode);
        }

        private static void LogNearTieIfUseful(
            MobileParty actor,
            PartyThinkParams thinkParams,
            int beforeIndex,
            float beforeScore,
            CandidateMemory strongest,
            SocialMemoryCausalMode mode)
        {
            if (strongest.Index < 0 ||
                strongest.Index == beforeIndex ||
                beforeScore <= 0.001f)
            {
                return;
            }

            float gap =
                beforeScore -
                strongest.AdjustedScore;

            float gapPct =
                gap / beforeScore;
            if (gapPct > 0.20f)
                return;

            ClanAIPostVanilla.WriteExternalLog(
                "MEMORY_CAUSAL_NEAR " +
                "actor=" +
                actor.LeaderHero.Name.ToString() +
                " actorId=" +
                actor.LeaderHero.StringId +
                " before=" +
                CandidateLabel(
                    thinkParams,
                    beforeIndex) +
                " beforeScore=" +
                beforeScore.ToString(
                    "R",
                    CultureInfo.InvariantCulture) +
                " memoryCandidate=" +
                CandidateLabel(
                    thinkParams,
                    strongest.Index) +
                " memoryBase=" +
                strongest.BaseScore.ToString(
                    "R",
                    CultureInfo.InvariantCulture) +
                " memoryAdjusted=" +
                strongest.AdjustedScore.ToString(
                    "R",
                    CultureInfo.InvariantCulture) +
                " gapPct=" +
                (gapPct * 100f).ToString(
                    "0.0",
                    CultureInfo.InvariantCulture) +
                " requiredFactor=" +
                (beforeScore /
                    strongest.BaseScore).ToString(
                        "R",
                        CultureInfo.InvariantCulture) +
                " factor=" +
                strongest.Factor.ToString(
                    "R",
                    CultureInfo.InvariantCulture) +
                " targetClan=" +
                strongest.TargetClan +
                " pressure=" +
                strongest.Pressure +
                " mode=" +
                mode);
        }

        private static int FindBestIndex(
            PartyThinkParams thinkParams)
        {
            int bestIndex = -1;
            float bestScore = float.MinValue;

            for (int i = 0;
                 i < thinkParams.AIBehaviorScores.Count;
                 i++)
            {
                float score =
                    thinkParams.AIBehaviorScores[i].Item2;

                if (bestIndex < 0 ||
                    score > bestScore)
                {
                    bestIndex = i;
                    bestScore = score;
                }
            }

            return bestIndex;
        }

        private static bool IsAggressive(
            AiBehavior behavior)
        {
            return
                behavior == AiBehavior.RaidSettlement ||
                behavior == AiBehavior.BesiegeSettlement ||
                behavior == AiBehavior.AssaultSettlement ||
                behavior == AiBehavior.EngageParty;
        }

        private static Clan ResolveTargetClan(
            AIBehaviorData data)
        {
            Settlement settlement =
                data.Party as Settlement;

            if (settlement != null)
                return settlement.OwnerClan;

            MobileParty party =
                data.Party as MobileParty;

            if (party == null)
                return null;

            Hero owner = party.Owner;

            if (owner == null)
                owner = party.LeaderHero;

            return owner != null
                ? owner.Clan
                : null;
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

            return party != null
                ? party.Name.ToString()
                : "<none>";
        }

        private static string CandidateLabel(
            PartyThinkParams thinkParams,
            int index)
        {
            if (index < 0 ||
                index >=
                    thinkParams.AIBehaviorScores.Count)
            {
                return "NONE";
            }
            AIBehaviorData data =
                thinkParams.AIBehaviorScores[index].Item1;

            return
                data.AiBehavior.ToString() +
                ":" +
                TargetName(data);
        }

        private sealed class CandidateMemory
        {
            public bool Valid;
            public int Index = -1;
            public string Behavior = "<none>";
            public string Target = "<none>";
            public string TargetClan = "<none>";
            public int Pressure;
            public float Factor = 1f;
            public float BaseScore;
            public float AdjustedScore;
            public int Trust;
            public int Grievance;
            public int BloodDebt;
            public int Obligation;
            public int Tension;
        }
    }
}