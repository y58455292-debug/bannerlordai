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
    /// Experiment 03F - Global Natural Near-Tie Scanner (READ ONLY)
    ///
    /// Searches all eligible independent hero-led clan parties for a naturally
    /// occurring case where the already-proven need-sensitive Clan Gravity rule
    /// could change vanilla's winner.
    ///
    /// Rule tested counterfactually:
    ///   pressure = max(activeSiege, activeRaid, normalizedGarrisonDeficit)
    ///   availableBonus = min(0.25, pressure)
    ///   hypotheticalScore = vanillaScore * (1 + availableBonus)
    ///
    /// A hit is logged only when the needy clan-owned settlement is not already
    /// vanilla rank 1 and its hypothetical score strictly exceeds vanilla's top
    /// score. This class never changes PartyThinkParams.
    /// </summary>
    public static class GlobalNearTieScanner
    {
        public const string Version = "0.1";
        public const float MaxBonusFraction = 0.25f;

        public static long ObservedPartyThinks;
        public static long EligibleIndependentLordThinks;
        public static long NeedPositiveThinks;
        public static long NeedPositiveCandidates;
        public static long FlipOpportunities;

        public static float ClosestGapFraction = -1f;
        public static string ClosestPartyId = null;
        public static string ClosestPartyName = null;
        public static string ClosestClanId = null;
        public static string ClosestClanName = null;
        public static string ClosestTargetId = null;
        public static string ClosestTargetName = null;
        public static int ClosestVanillaRank = -1;
        public static float ClosestRequiredBonusFraction = -1f;
        public static float ClosestAvailableBonusFraction = -1f;
        public static float ClosestVanillaScore = -1f;
        public static float ClosestTopVanillaScore = -1f;
        public static string ClosestTopVanillaBehavior = null;
        public static string ClosestTopVanillaTarget = null;

        public static string LastNeedPartyId = null;
        public static string LastNeedPartyName = null;
        public static string LastNeedTargetId = null;
        public static string LastNeedTargetName = null;
        public static float LastNeedPressure = -1f;
        public static float LastNeedAvailableBonusFraction = -1f;
        public static float LastNeedRequiredBonusFraction = -1f;
        public static int LastNeedVanillaRank = -1;

        public static string LastHitPartyId = null;
        public static string LastHitPartyName = null;
        public static string LastHitClanId = null;
        public static string LastHitClanName = null;
        public static string LastHitTargetId = null;
        public static string LastHitTargetName = null;
        public static float LastHitVanillaScore = -1f;
        public static int LastHitVanillaRank = -1;
        public static float LastHitPressure = -1f;
        public static float LastHitAvailableBonusFraction = -1f;
        public static float LastHitRequiredBonusFraction = -1f;
        public static float LastHitHypotheticalScore = -1f;
        public static string LastHitTopVanillaBehavior = null;
        public static string LastHitTopVanillaTarget = null;
        public static float LastHitTopVanillaScore = -1f;

        private static readonly MethodInfo IdealGarrisonMethod =
            AccessTools.Method(
                AccessTools.TypeByName("Helpers.FactionHelper"),
                "FindIdealGarrisonStrengthPerWalledCenter");

        private static readonly Dictionary<string, MobileParty> GarrisonBySettlement =
            new Dictionary<string, MobileParty>(StringComparer.Ordinal);

        private static long _lastGarrisonRefreshObservedCount = -1000;

        private sealed class Candidate
        {
            public object Target;
            public Settlement Settlement;
            public string Behavior;
            public float Score;
        }

        public static void Observe(MobileParty party, PartyThinkParams think)
        {
            ObservedPartyThinks++;

            if (party == null || think == null)
                return;

            Clan clan = party.ActualClan;
            if (clan == null || party.LeaderHero == null)
                return;

            Kingdom kingdom = ReadMember(clan, "Kingdom") as Kingdom;
            if (kingdom == null)
                return;

            if (Bool(ReadMember(party, "IsMainParty")))
                return;

            if (ReadMember(party, "Army") != null)
                return;

            if (ReadMember(party, "AttachedTo") != null)
                return;

            if (Bool(ReadMember(party, "InMapEvent")))
                return;

            EligibleIndependentLordThinks++;

            List<Candidate> candidates = ReadCandidates(think);
            if (candidates.Count == 0)
                return;

            Candidate vanillaTop = null;
            foreach (Candidate c in candidates)
            {
                if (vanillaTop == null || c.Score > vanillaTop.Score)
                    vanillaTop = c;
            }

            if (vanillaTop == null)
                return;

            float idealGarrison = ReadIdealGarrisonStrength(kingdom, clan);
            if (idealGarrison <= 0f)
                return;

            RefreshGarrisonCacheIfNeeded();

            bool partyHasNeed = false;

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

                float actualGarrison;
                float garrisonDeficit;
                float pressure = CalculatePressure(
                    settlement,
                    idealGarrison,
                    out actualGarrison,
                    out garrisonDeficit);

                if (pressure <= 0f)
                    continue;

                partyHasNeed = true;
                NeedPositiveCandidates++;

                float availableBonus = Math.Min(MaxBonusFraction, pressure);
                float hypotheticalScore = c.Score * (1f + availableBonus);
                int vanillaRank = RankByScore(candidates, c);
                int hypotheticalRank = HypotheticalRank(
                    candidates,
                    c,
                    hypotheticalScore);

                float requiredBonus =
                    c.Score <= 0f
                        ? float.PositiveInfinity
                        : Math.Max(0f, (vanillaTop.Score / c.Score) - 1f);

                LastNeedPartyId = party.StringId;
                LastNeedPartyName = party.Name.ToString();
                LastNeedTargetId = settlement.StringId;
                LastNeedTargetName = settlement.Name.ToString();
                LastNeedPressure = pressure;
                LastNeedAvailableBonusFraction = availableBonus;
                LastNeedRequiredBonusFraction =
                    float.IsPositiveInfinity(requiredBonus)
                        ? -1f
                        : requiredBonus;
                LastNeedVanillaRank = vanillaRank;

                if (vanillaRank > 1 &&
                    !float.IsPositiveInfinity(requiredBonus))
                {
                    float gap = requiredBonus - availableBonus;
                    if (gap < 0f)
                        gap = 0f;

                    if (ClosestGapFraction < 0f || gap < ClosestGapFraction)
                    {
                        ClosestGapFraction = gap;
                        ClosestPartyId = party.StringId;
                        ClosestPartyName = party.Name.ToString();
                        ClosestClanId = clan.StringId;
                        ClosestClanName = clan.Name.ToString();
                        ClosestTargetId = settlement.StringId;
                        ClosestTargetName = settlement.Name.ToString();
                        ClosestVanillaRank = vanillaRank;
                        ClosestRequiredBonusFraction = requiredBonus;
                        ClosestAvailableBonusFraction = availableBonus;
                        ClosestVanillaScore = c.Score;
                        ClosestTopVanillaScore = vanillaTop.Score;
                        ClosestTopVanillaBehavior = vanillaTop.Behavior;
                        ClosestTopVanillaTarget = TargetName(vanillaTop.Target);
                    }
                }

                bool wouldFlip =
                    vanillaRank > 1 &&
                    hypotheticalScore > vanillaTop.Score;

                if (!wouldFlip)
                    continue;

                FlipOpportunities++;

                LastHitPartyId = party.StringId;
                LastHitPartyName = party.Name.ToString();
                LastHitClanId = clan.StringId;
                LastHitClanName = clan.Name.ToString();
                LastHitTargetId = settlement.StringId;
                LastHitTargetName = settlement.Name.ToString();
                LastHitVanillaScore = c.Score;
                LastHitVanillaRank = vanillaRank;
                LastHitPressure = pressure;
                LastHitAvailableBonusFraction = availableBonus;
                LastHitRequiredBonusFraction = requiredBonus;
                LastHitHypotheticalScore = hypotheticalScore;
                LastHitTopVanillaBehavior = vanillaTop.Behavior;
                LastHitTopVanillaTarget = TargetName(vanillaTop.Target);
                LastHitTopVanillaScore = vanillaTop.Score;

                InspectorLog.Info(
                    "GLOBAL NEAR TIE HIT v" + Version +
                    " party=" + party.Name +
                    " partyId=" + party.StringId +
                    " clan=" + clan.Name +
                    " clanId=" + clan.StringId +
                    " target=" + settlement.Name +
                    " targetId=" + settlement.StringId +
                    " vanillaScore=" + F(c.Score) +
                    " vanillaRank=" + vanillaRank +
                    " pressure=" + F(pressure) +
                    " availableBonusFraction=" + F(availableBonus) +
                    " requiredBonusFraction=" + F(requiredBonus) +
                    " hypotheticalScore=" + F(hypotheticalScore) +
                    " hypotheticalRank=" + hypotheticalRank +
                    " topVanillaBehavior=" + vanillaTop.Behavior +
                    " topVanillaTarget=" + TargetName(vanillaTop.Target) +
                    " topVanillaScore=" + F(vanillaTop.Score) +
                    " underRaid=" + settlement.IsUnderRaid +
                    " underSiege=" + settlement.IsUnderSiege +
                    " idealGarrison=" + F(idealGarrison) +
                    " actualGarrisonStrength=" + F(actualGarrison) +
                    " garrisonDeficit=" + F(garrisonDeficit) +
                    " mutation=False");
            }

            if (partyHasNeed)
                NeedPositiveThinks++;
        }

        private static List<Candidate> ReadCandidates(PartyThinkParams think)
        {
            var result = new List<Candidate>();

            object scoreList = ReadMember(think, "AIBehaviorScores");
            IEnumerable enumerable = scoreList as IEnumerable;
            if (enumerable == null)
                return result;

            foreach (object item in enumerable)
            {
                object data = ReadMember(item, "Item1");
                if (data == null)
                    continue;

                object target = ReadMember(data, "Party");

                result.Add(new Candidate
                {
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
            Candidate self)
        {
            int higher = 0;

            foreach (Candidate c in candidates)
            {
                if (object.ReferenceEquals(c, self))
                    continue;

                if (c.Score > self.Score)
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

        private static float ReadIdealGarrisonStrength(
            Kingdom kingdom,
            Clan clan)
        {
            if (kingdom == null ||
                clan == null ||
                IdealGarrisonMethod == null)
                return -1f;

            try
            {
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

        private static void RefreshGarrisonCacheIfNeeded()
        {
            if (ObservedPartyThinks - _lastGarrisonRefreshObservedCount < 50 &&
                GarrisonBySettlement.Count > 0)
                return;

            GarrisonBySettlement.Clear();

            try
            {
                foreach (MobileParty garrison in MobileParty.AllGarrisonParties)
                {
                    if (garrison == null ||
                        garrison.GarrisonPartyComponent == null)
                        continue;

                    Settlement settlement =
                        garrison.GarrisonPartyComponent.Settlement;

                    if (settlement == null ||
                        string.IsNullOrEmpty(settlement.StringId))
                        continue;

                    GarrisonBySettlement[settlement.StringId] = garrison;
                }
            }
            catch
            {
                GarrisonBySettlement.Clear();
            }

            _lastGarrisonRefreshObservedCount = ObservedPartyThinks;
        }

        private static MobileParty FindGarrison(Settlement settlement)
        {
            if (settlement == null ||
                string.IsNullOrEmpty(settlement.StringId))
                return null;

            MobileParty garrison;
            if (GarrisonBySettlement.TryGetValue(
                settlement.StringId,
                out garrison))
                return garrison;

            return null;
        }

        private static string TargetName(object target)
        {
            if (target == null)
                return "<null>";

            Settlement settlement = target as Settlement;
            if (settlement != null)
                return settlement.Name.ToString();

            MobileParty party = target as MobileParty;
            if (party != null)
                return party.Name.ToString();

            return target.ToString();
        }

        private static bool Bool(object value)
        {
            if (value == null)
                return false;

            try
            {
                return Convert.ToBoolean(
                    value,
                    CultureInfo.InvariantCulture);
            }
            catch
            {
                return false;
            }
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
            if (value == null)
                return 0f;

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
