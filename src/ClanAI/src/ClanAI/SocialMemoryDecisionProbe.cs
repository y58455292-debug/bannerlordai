using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace ClanAI
{
    public static class SocialMemoryDecisionProbe
    {
        private static long _decisionsWithMemory;
        private static long _wouldFlip;
        private static float _bestGapPct;

        public static void Reset()
        {
            _decisionsWithMemory = 0;
            _wouldFlip = 0;
            _bestGapPct = float.MaxValue;

            ClanAIPostVanilla.WriteExternalLog(
                "MEMORY_SCORE_PROBE_RESET");
        }

        public static void Analyze(
            MobileParty actor,
            PartyThinkParams thinkParams)
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

            int bestMemoryIndex = -1;
            float bestMemoryBaseScore = 0f;
            float bestMemoryProposedScore = float.MinValue;
            float bestMemoryFactor = 1f;
            int bestMemoryPressure = 0;

            int bestMemoryTrust = 0;
            int bestMemoryGrievance = 0;
            int bestMemoryBloodDebt = 0;
            int bestMemoryObligation = 0;
            int bestMemoryTension = 0;

            string strongestBehavior = "";
            string strongestTarget = "";
            string strongestClan = "";

            float strongestFactor = 1f;
            float strongestAbsDelta = 0f;
            int strongestPressure = 0;

            int strongestTrust = 0;
            int strongestGrievance = 0;
            int strongestBloodDebt = 0;
            int strongestObligation = 0;
            int strongestTension = 0;

            for (int i = 0;
                 i < thinkParams.AIBehaviorScores.Count;
                 i++)
            {
                AIBehaviorData data =
                    thinkParams.AIBehaviorScores[i].Item1;

                float baseScore =
                    thinkParams.AIBehaviorScores[i].Item2;

                if (baseScore > beforeScore)
                {
                    beforeScore = baseScore;
                    beforeIndex = i;
                }

                float factor = 1f;

                if (baseScore > 0.10f &&
                    IsAggressive(data.AiBehavior))
                {
                    Clan targetClan =
                        ResolveTargetClan(data);

                    int trust;
                    int grievance;
                    int bloodDebt;
                    int obligation;
                    int tension;

                    if (targetClan != null &&
                        SocialLedger.TryGetState(
                            actor.LeaderHero,
                            targetClan,
                            out trust,
                            out grievance,
                            out bloodDebt,
                            out obligation,
                            out tension))
                    {
                        bool nonzero =
                            trust != 0 ||
                            grievance != 0 ||
                            bloodDebt != 0 ||
                            obligation != 0 ||
                            tension != 0;

                        if (nonzero)
                        {
                            relevantCount++;
                            int pressure =
                                grievance +
                                tension +
                                (bloodDebt * 2) -
                                trust -
                                obligation;

                            float delta =
                                pressure * 0.0025f;

                            if (delta > 0.10f)
                                delta = 0.10f;

                            if (delta < -0.10f)
                                delta = -0.10f;

                            factor = 1f + delta;

                            float memoryProposed =
                                baseScore * factor;

                            if (memoryProposed >
                                bestMemoryProposedScore)
                            {
                                bestMemoryIndex = i;
                                bestMemoryBaseScore = baseScore;
                                bestMemoryProposedScore =
                                    memoryProposed;
                                bestMemoryFactor = factor;
                                bestMemoryPressure = pressure;

                                bestMemoryTrust = trust;
                                bestMemoryGrievance =
                                    grievance;
                                bestMemoryBloodDebt =
                                    bloodDebt;
                                bestMemoryObligation =
                                    obligation;
                                bestMemoryTension =
                                    tension;
                            }

                            float absDelta =
                                Math.Abs(delta);

                            if (absDelta >
                                strongestAbsDelta)
                            {
                                strongestAbsDelta =
                                    absDelta;

                                strongestBehavior =
                                    data.AiBehavior.ToString();

                                strongestTarget =
                                    TargetName(data);

                                strongestClan =
                                    targetClan.Name.ToString();

                                strongestFactor =
                                    factor;

                                strongestPressure =
                                    pressure;
                                strongestTrust =
                                    trust;

                                strongestGrievance =
                                    grievance;

                                strongestBloodDebt =
                                    bloodDebt;

                                strongestObligation =
                                    obligation;

                                strongestTension =
                                    tension;
                            }
                        }
                    }
                }

                float candidateProposed =
                    baseScore * factor;

                if (candidateProposed >
                    proposedScore)
                {
                    proposedScore =
                        candidateProposed;

                    proposedIndex =
                        i;
                }
            }

            if (relevantCount == 0)
                return;

            _decisionsWithMemory++;

            bool wouldFlip =
                beforeIndex != proposedIndex;
            if (wouldFlip)
                _wouldFlip++;

            if (bestMemoryIndex >= 0 &&
                bestMemoryIndex != beforeIndex)
            {
                float gap =
                    beforeScore -
                    bestMemoryProposedScore;

                float gapPct =
                    beforeScore > 0.001f
                        ? gap / beforeScore
                        : 999f;

                float requiredFactor =
                    bestMemoryBaseScore > 0.001f
                        ? beforeScore /
                            bestMemoryBaseScore
                        : 999f;

                if (gapPct < _bestGapPct)
                {
                    _bestGapPct = gapPct;

                    ClanAIPostVanilla.WriteExternalLog(
                        "MEMORY_BEST_OPPORTUNITY" +
                        " actor=" +
                        actor.LeaderHero.Name.ToString() +
                        " faction=" +
                        actor.MapFaction.Name.ToString() +
                        " winner=" +
                        CandidateLabel(
                            thinkParams,
                            beforeIndex) +
                        " winnerScore=" +
                        beforeScore.ToString("0.000") +
                        " memoryCandidate=" +
                        CandidateLabel(
                            thinkParams,
                            bestMemoryIndex) +
                        " memoryBase=" +
                        bestMemoryBaseScore.ToString("0.000") +
                        " memoryProposed=" +
                        bestMemoryProposedScore.ToString("0.000") +
                        " gapPct=" +
                        (gapPct * 100f).ToString("0.0") +
                        " requiredFactor=" +
                        requiredFactor.ToString("0.000") +
                        " currentFactor=" +
                        bestMemoryFactor.ToString("0.000") +
                        " pressure=" +
                        bestMemoryPressure +
                        " trust=" +
                        bestMemoryTrust +
                        " grievance=" +
                        bestMemoryGrievance +
                        " bloodDebt=" +
                        bestMemoryBloodDebt +
                        " obligation=" +
                        bestMemoryObligation +
                        " tension=" +
                        bestMemoryTension);
                }

                if (gapPct <= 0.20f)
                {
                    ClanAIPostVanilla.WriteExternalLog(
                        "MEMORY_NEAR_TIE" +
                        " actor=" +
                        actor.LeaderHero.Name.ToString() +
                        " faction=" +
                        actor.MapFaction.Name.ToString() +
                        " winner=" +
                        CandidateLabel(
                            thinkParams,
                            beforeIndex) +
                        " winnerScore=" +
                        beforeScore.ToString("0.000") +
                        " memoryCandidate=" +
                        CandidateLabel(
                            thinkParams,
                            bestMemoryIndex) +
                        " memoryBase=" +
                        bestMemoryBaseScore.ToString("0.000") +
                        " memoryProposed=" +
                        bestMemoryProposedScore.ToString("0.000") +
                        " gapPct=" +
                        (gapPct * 100f).ToString("0.0") +
                        " requiredFactor=" +
                        requiredFactor.ToString("0.000") +
                        " currentFactor=" +
                        bestMemoryFactor.ToString("0.000") +
                        " pressure=" +
                        bestMemoryPressure +
                        " trust=" +
                        bestMemoryTrust +
                        " grievance=" +
                        bestMemoryGrievance +
                        " bloodDebt=" +
                        bestMemoryBloodDebt +
                        " obligation=" +
                        bestMemoryObligation +
                        " tension=" +
                        bestMemoryTension);
                }
            }

            ClanAIPostVanilla.WriteExternalLog(
                "MEMORY_SCORE_PROBE" +
                " actor=" +
                actor.LeaderHero.Name.ToString() +
                " faction=" +
                actor.MapFaction.Name.ToString() +
                " relevantCandidates=" +
                relevantCount +
                " before=" +
                CandidateLabel(
                    thinkParams,
                    beforeIndex) +
                " beforeScore=" +
                beforeScore.ToString("0.000") +
                " proposed=" +
                CandidateLabel(
                    thinkParams,
                    proposedIndex) +
                " proposedScore=" +
                proposedScore.ToString("0.000") +
                " wouldFlip=" +
                wouldFlip +
                " strongestBehavior=" +
                strongestBehavior +
                " strongestTarget=" +
                strongestTarget +
                " strongestClan=" +
                strongestClan +
                " pressure=" +
                strongestPressure +
                " factor=" +
                strongestFactor.ToString("0.000") +
                " trust=" +
                strongestTrust +
                " grievance=" +
                strongestGrievance +
                " bloodDebt=" +
                strongestBloodDebt +
                " obligation=" +
                strongestObligation +
                " tension=" +
                strongestTension +
                " probeDecisions=" +
                _decisionsWithMemory +
                " probeFlips=" +
                _wouldFlip);
        }

        private static bool IsAggressive(
            AiBehavior behavior)
        {
            return
                behavior ==
                    AiBehavior.RaidSettlement ||
                behavior ==
                    AiBehavior.BesiegeSettlement ||
                behavior ==
                    AiBehavior.AssaultSettlement ||
                behavior ==
                    AiBehavior.EngageParty;
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

            Hero owner =
                party.Owner;

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
                index >= thinkParams.AIBehaviorScores.Count)
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
    }
}
