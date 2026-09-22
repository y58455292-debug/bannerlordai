using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace BannerlordInspector
{
    /// <summary>
    /// Experiment 03E - Natural Near-Tie Flip Detector (READ ONLY)
    ///
    /// This detector NEVER changes PartyThinkParams scores.
    ///
    /// It watches Apolytea / Neretzes after all vanilla AI scorers have run
    /// and asks a counterfactual question:
    ///
    ///   If we applied the already-proven Clan Gravity rule
    ///       bonusFraction = min(0.25, pressure)
    ///       hypotheticalScore = vanillaScore * (1 + bonusFraction)
    ///   would a needy clan-owned GoToSettlement candidate overtake vanilla's
    ///   current top candidate?
    ///
    /// We log the real vanilla rank, the hypothetical rank, the required bonus,
    /// the available need bonus, and a strict wouldFlip=True/False.
    ///
    /// No marker is used because there is no mutation path in this experiment.
    /// </summary>
    public static class NearTieDetector
    {
        public const string Version = "0.3";
        public const string TargetPartyId = "lord_1_1_8_party_1"; // Apolytea
        public const string TargetClanId = "clan_empire_north_3"; // Neretzes
        public const float MaxBonusFraction = 0.25f;

        private static readonly MethodInfo IdealGarrisonMethod =
            AccessTools.Method(
                AccessTools.TypeByName("Helpers.FactionHelper"),
                "FindIdealGarrisonStrengthPerWalledCenter");

        private sealed class Candidate
        {
            public object Data;
            public object Target;
            public Settlement Settlement;
            public string Behavior;
            public float Score;
        }

        private sealed class NeedCandidate
        {
            public Candidate Candidate;
            public float Pressure;
            public float BonusFraction;
            public float HypotheticalScore;
            public float RequiredBonusFraction;
            public int VanillaRank;
            public int HypotheticalRank;
            public float IdealGarrison;
            public float ActualGarrison;
            public float GarrisonDeficit;
            public bool UnderRaid;
            public bool UnderSiege;
        }

        public static void Observe(MobileParty party, PartyThinkParams think)
        {
            if (party == null || think == null) return;
            if (!string.Equals(
                party.StringId,
                TargetPartyId,
                StringComparison.Ordinal))
                return;

            Clan clan = party.ActualClan;
            if (clan == null ||
                !string.Equals(
                    clan.StringId,
                    TargetClanId,
                    StringComparison.Ordinal))
            {
                InspectorLog.Warn(
                    "NEAR TIE DETECTOR v" + Version +
                    " guard stopped: Apolytea/Neretzes target no longer matches.");
                return;
            }

            List<Candidate> candidates = ReadCandidates(think);
            if (candidates.Count == 0)
            {
                InspectorLog.Info(
                    "NEAR TIE SUMMARY v" + Version +
                    " party=" + party.Name +
                    " clan=" + clan.Name +
                    " candidateCount=0 mutation=False");
                return;
            }

            candidates.Sort((a, b) => b.Score.CompareTo(a.Score));

            Candidate vanillaTop = candidates[0];
            float topScore = vanillaTop.Score;
            float idealGarrison = ReadIdealGarrisonStrength(clan);

            var needs = new List<NeedCandidate>();
            int clanTerritoryCandidates = 0;

            foreach (Candidate c in candidates)
            {
                if (!string.Equals(
                    c.Behavior,
                    "GoToSettlement",
                    StringComparison.Ordinal))
                    continue;

                Settlement settlement = c.Settlement;
                if (settlement == null || settlement.OwnerClan == null)
                    continue;

                if (!object.ReferenceEquals(settlement.OwnerClan, clan) &&
                    !string.Equals(
                        settlement.OwnerClan.StringId,
                        clan.StringId,
                        StringComparison.Ordinal))
                    continue;

                clanTerritoryCandidates++;

                float actual;
                float deficit;
                float pressure = CalculatePressure(
                    settlement,
                    idealGarrison,
                    out actual,
                    out deficit);

                if (pressure <= 0f)
                    continue;

                float bonus = Math.Min(MaxBonusFraction, pressure);
                float hypothetical = c.Score * (1f + bonus);

                float requiredBonus;
                if (c.Score <= 0f)
                    requiredBonus = float.PositiveInfinity;
                else
                    requiredBonus = Math.Max(0f, (topScore / c.Score) - 1f);

                var n = new NeedCandidate
                {
                    Candidate = c,
                    Pressure = pressure,
                    BonusFraction = bonus,
                    HypotheticalScore = hypothetical,
                    RequiredBonusFraction = requiredBonus,
                    VanillaRank = RankByScore(candidates, c.Score, c),
                    HypotheticalRank = HypotheticalRank(
                        candidates, c, hypothetical),
                    IdealGarrison = idealGarrison,
                    ActualGarrison = actual,
                    GarrisonDeficit = deficit,
                    UnderRaid = settlement.IsUnderRaid,
                    UnderSiege = settlement.IsUnderSiege
                };

                needs.Add(n);

                bool wouldFlip =
                    n.VanillaRank > 1 &&
                    hypothetical > topScore;

                InspectorLog.Info(
                    "NEAR TIE NEED v" + Version +
                    " party=" + party.Name +
                    " clan=" + clan.Name +
                    " target=" + settlement.Name +
                    " targetId=" + settlement.StringId +
                    " vanillaScore=" + F(c.Score) +
                    " vanillaRank=" + n.VanillaRank +
                    " pressure=" + F(pressure) +
                    " availableBonusFraction=" + F(bonus) +
                    " requiredBonusFraction=" +
                    (float.IsPositiveInfinity(requiredBonus)
                        ? "INF"
                        : F(requiredBonus)) +
                    " hypotheticalScore=" + F(hypothetical) +
                    " hypotheticalRank=" + n.HypotheticalRank +
                    " topVanillaBehavior=" + vanillaTop.Behavior +
                    " topVanillaTarget=" + TargetName(vanillaTop.Target) +
                    " topVanillaScore=" + F(topScore) +
                    " alreadyVanillaWinner=" + (n.VanillaRank == 1) +
                    " wouldFlip=" + wouldFlip +
                    " underRaid=" + n.UnderRaid +
                    " underSiege=" + n.UnderSiege +
                    " idealGarrison=" + F(n.IdealGarrison) +
                    " actualGarrisonStrength=" + F(n.ActualGarrison) +
                    " garrisonDeficit=" + F(n.GarrisonDeficit) +
                    " mutation=False");
            }

            NeedCandidate bestFlipOpportunity = null;
            float bestMargin = float.NegativeInfinity;

            foreach (NeedCandidate n in needs)
            {
                float margin = n.HypotheticalScore - topScore;

                // Prefer genuine non-winning vanilla candidates.
                if (n.VanillaRank <= 1)
                    continue;

                if (bestFlipOpportunity == null || margin > bestMargin)
                {
                    bestFlipOpportunity = n;
                    bestMargin = margin;
                }
            }

            bool flipExists =
                bestFlipOpportunity != null &&
                bestFlipOpportunity.HypotheticalScore > topScore;

            string bestTarget =
                bestFlipOpportunity == null
                    ? "<none>"
                    : bestFlipOpportunity.Candidate.Settlement.Name.ToString();

            string bestRequired =
                bestFlipOpportunity == null
                    ? "<n/a>"
                    : (float.IsPositiveInfinity(
                        bestFlipOpportunity.RequiredBonusFraction)
                        ? "INF"
                        : F(bestFlipOpportunity.RequiredBonusFraction));

            string bestAvailable =
                bestFlipOpportunity == null
                    ? "<n/a>"
                    : F(bestFlipOpportunity.BonusFraction);

            string bestHypothetical =
                bestFlipOpportunity == null
                    ? "<n/a>"
                    : F(bestFlipOpportunity.HypotheticalScore);

            int bestVanillaRank =
                bestFlipOpportunity == null
                    ? -1
                    : bestFlipOpportunity.VanillaRank;

            int bestHypotheticalRank =
                bestFlipOpportunity == null
                    ? -1
                    : bestFlipOpportunity.HypotheticalRank;

            InspectorLog.Info(
                "NEAR TIE SUMMARY v" + Version +
                " party=" + party.Name +
                " clan=" + clan.Name +
                " candidateCount=" + candidates.Count +
                " clanTerritoryCandidates=" + clanTerritoryCandidates +
                " needPositive=" + needs.Count +
                " topVanillaBehavior=" + vanillaTop.Behavior +
                " topVanillaTarget=" + TargetName(vanillaTop.Target) +
                " topVanillaScore=" + F(topScore) +
                " bestNonWinningNeedTarget=" + bestTarget +
                " bestVanillaRank=" + bestVanillaRank +
                " bestRequiredBonusFraction=" + bestRequired +
                " bestAvailableBonusFraction=" + bestAvailable +
                " bestHypotheticalScore=" + bestHypothetical +
                " bestHypotheticalRank=" + bestHypotheticalRank +
                " flipOpportunity=" + flipExists +
                " formula=hypotheticalScore(vanilla*(1+min(0.25,pressure)))" +
                " mutation=False");
        }

        private static List<Candidate> ReadCandidates(PartyThinkParams think)
        {
            var result = new List<Candidate>();

            object scoreList = ReadMember(think, "AIBehaviorScores");
            IEnumerable enumerable = scoreList as IEnumerable;
            if (enumerable == null) return result;

            foreach (object item in enumerable)
            {
                object data = ReadMember(item, "Item1");
                if (data == null) continue;

                object target = ReadMember(data, "Party");

                result.Add(new Candidate
                {
                    Data = data,
                    Target = target,
                    Settlement = target as Settlement,
                    Behavior = Text(ReadMember(data, "AiBehavior")),
                    Score = ToFloat(ReadMember(item, "Item2"))
                });
            }

            return result;
        }

        private static int RankByScore(
            List<Candidate> candidates,
            float score,
            Candidate self)
        {
            int higher = 0;

            foreach (Candidate c in candidates)
            {
                if (object.ReferenceEquals(c, self))
                    continue;

                if (c.Score > score)
                    higher++;
            }

            return higher + 1;
        }

        private static int HypotheticalRank(
            List<Candidate> candidates,
            Candidate self,
            float hypotheticalScore)
        {
            int higher = 0;

            foreach (Candidate c in candidates)
            {
                if (object.ReferenceEquals(c, self))
                    continue;

                if (c.Score > hypotheticalScore)
                    higher++;
            }

            return higher + 1;
        }

        private static float CalculatePressure(
            Settlement settlement,
            float idealGarrison,
            out float actualGarrison,
            out float garrisonDeficit)
        {
            actualGarrison = 0f;
            garrisonDeficit = 0f;

            if (settlement == null)
                return 0f;

            bool isVillage = settlement.IsVillage;
            bool isWalled = !isVillage && settlement.Town != null;

            if (isWalled && idealGarrison > 0f)
            {
                MobileParty garrison = FindGarrison(settlement);

                if (garrison != null && garrison.Party != null)
                    actualGarrison = garrison.Party.EstimatedStrength;

                garrisonDeficit = Clamp01(
                    (idealGarrison - actualGarrison) / idealGarrison);
            }

            float siegePressure = settlement.IsUnderSiege ? 1f : 0f;
            float raidPressure = settlement.IsUnderRaid ? 1f : 0f;

            return Math.Max(
                Math.Max(siegePressure, raidPressure),
                garrisonDeficit);
        }

        private static float ReadIdealGarrisonStrength(Clan clan)
        {
            if (clan == null || IdealGarrisonMethod == null)
                return -1f;

            try
            {
                Kingdom kingdom = ReadMember(clan, "Kingdom") as Kingdom;
                if (kingdom == null)
                    return -1f;

                object value = IdealGarrisonMethod.Invoke(
                    null,
                    new object[] { kingdom, clan });

                return ToFloat(value);
            }
            catch
            {
                return -1f;
            }
        }

        private static MobileParty FindGarrison(Settlement settlement)
        {
            if (settlement == null) return null;

            try
            {
                foreach (MobileParty party in MobileParty.AllGarrisonParties)
                {
                    if (party == null || party.GarrisonPartyComponent == null)
                        continue;

                    Settlement home = party.GarrisonPartyComponent.Settlement;

                    if (object.ReferenceEquals(home, settlement))
                        return party;

                    if (home != null &&
                        string.Equals(
                            home.StringId,
                            settlement.StringId,
                            StringComparison.Ordinal))
                        return party;
                }
            }
            catch
            {
            }

            return null;
        }

        private static string TargetName(object target)
        {
            if (target == null) return "<null>";

            Settlement settlement = target as Settlement;
            if (settlement != null)
                return settlement.Name.ToString();

            MobileParty party = target as MobileParty;
            if (party != null)
                return party.Name.ToString();

            return target.ToString();
        }

        private static float Clamp01(float value)
        {
            if (value < 0f) return 0f;
            if (value > 1f) return 1f;
            return value;
        }

        private static object ReadMember(object instance, string name)
        {
            if (instance == null || string.IsNullOrEmpty(name))
                return null;

            Type t = instance.GetType();

            FieldInfo f = t.GetField(
                name,
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic);

            if (f != null)
                return f.GetValue(instance);

            PropertyInfo p = t.GetProperty(
                name,
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic);

            if (p != null && p.GetIndexParameters().Length == 0)
                return p.GetValue(instance, null);

            return null;
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
                return Convert.ToSingle(
                    value,
                    CultureInfo.InvariantCulture);
            }
            catch
            {
                return 0f;
            }
        }

        private static string F(float value)
        {
            return value.ToString(
                "0.######",
                CultureInfo.InvariantCulture);
        }
    }
}
