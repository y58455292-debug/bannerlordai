using System;
using System.Globalization;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Localization;

namespace ClanAI
{
    internal static class WarStrainEconomicPatches
    {
        private const float KRecruitCost = 0.50f;
        private const float KMilitaryUpkeep = 0.35f;
        private const float ExactTolerance = 0.011f;

        private static readonly TextObject RecruitCostExplanation =
            new TextObject("{=ClanAIWarStrainRecruitCost}War strain");
        private static readonly TextObject UpkeepExplanation =
            new TextObject("{=ClanAIWarStrainUpkeep}War strain");

        private static bool _installed;
        private static long _costApplications;
        private static long _wageApplications;

        internal static void Install()
        {
            if (_installed)
                return;

            Type wageType = AccessTools.TypeByName(
                "TaleWorlds.CampaignSystem.GameComponents.DefaultPartyWageModel");
            if (wageType == null)
                throw new Exception("DefaultPartyWageModel not found");

            MethodInfo recruitCost = AccessTools.Method(
                wageType,
                "GetTroopRecruitmentCost",
                new Type[]
                {
                    typeof(CharacterObject),
                    typeof(Hero),
                    typeof(bool)
                });
            MethodInfo totalWage = AccessTools.Method(
                wageType,
                "GetTotalWage",
                new Type[]
                {
                    typeof(MobileParty),
                    typeof(TroopRoster),
                    typeof(bool)
                });
            if (recruitCost == null)
                throw new Exception("GetTroopRecruitmentCost not found");
            if (totalWage == null)
                throw new Exception("GetTotalWage not found");

            MethodInfo recruitPostfix = AccessTools.Method(
                typeof(WarStrainEconomicPatches),
                nameof(RecruitCostPostfix));
            MethodInfo wagePostfix = AccessTools.Method(
                typeof(WarStrainEconomicPatches),
                nameof(TotalWagePostfix));
            if (recruitPostfix == null || wagePostfix == null)
                throw new Exception("War strain economic postfix not found");

            Harmony harmony = new Harmony(
                "com.bannerlordairesearch.clanai.warstrain.economy.v1");
            harmony.Patch(
                recruitCost,
                postfix: new HarmonyMethod(recruitPostfix));
            harmony.Patch(
                totalWage,
                postfix: new HarmonyMethod(wagePostfix));

            _installed = true;
            ClanAIPostVanilla.WriteExternalLog(
                "WAR_STRAIN_ECONOMY_PATCH_INSTALLED" +
                " recruitCostTarget=" + recruitCost.DeclaringType.FullName + "." + recruitCost.Name +
                " wageTarget=" + totalWage.DeclaringType.FullName + "." + totalWage.Name +
                " recruitCostK=" + F(KRecruitCost) +
                " upkeepK=" + F(KMilitaryUpkeep));
        }

        internal static void BeginSession()
        {
            _costApplications = 0;
            _wageApplications = 0;
            ClanAIPostVanilla.WriteExternalLog(
                "WAR_STRAIN_ECONOMY_SESSION_READY recruitCostK=" + F(KRecruitCost) +
                " upkeepK=" + F(KMilitaryUpkeep));
        }

        private static void RecruitCostPostfix(
            CharacterObject troop,
            Hero buyerHero,
            bool withoutItemCost,
            ref ExplainedNumber __result)
        {
            Kingdom kingdom = buyerHero == null || buyerHero.Clan == null
                ? null
                : buyerHero.Clan.Kingdom;
            if (kingdom == null)
                return;

            float strain = WarStateTracker.GetWarStrain(kingdom);
            if (strain <= 0.001f)
                return;

            float factor = strain * KRecruitCost;
            float before;
            float target;
            float after;
            float addedFactor;
            bool exact;
            if (!ApplyExactResultMultiplier(
                    ref __result,
                    1f + factor,
                    RecruitCostExplanation,
                    out before,
                    out target,
                    out after,
                    out addedFactor,
                    out exact))
                return;

            _costApplications++;

            if (_costApplications <= 120 || !exact || (_costApplications % 500) == 0)
            {
                MobileParty party = buyerHero.PartyBelongedTo;
                ClanAIPostVanilla.WriteExternalLog(
                    "WAR_STRAIN_RECRUIT_COST actor=" + buyerHero.Name.ToString() +
                    " partyId=" + (party == null ? "<none>" : party.StringId) +
                    " kingdom=" + kingdom.Name.ToString() +
                    " troop=" + (troop == null ? "<none>" : troop.Name.ToString()) +
                    " strain=" + F(strain) +
                    " requestedMultiplier=" + F(1f + factor) +
                    " vanillaCost=" + F(before) +
                    " targetCost=" + F(target) +
                    " finalCost=" + F(after) +
                    " addedNativeFactor=" + F(addedFactor) +
                    " exact=" + exact +
                    " withoutItemCost=" + withoutItemCost +
                    " applications=" + _costApplications);
            }
        }

        private static void TotalWagePostfix(
            MobileParty mobileParty,
            TroopRoster troopRoster,
            bool includeDescriptions,
            ref ExplainedNumber __result)
        {
            if (mobileParty == null ||
                (!mobileParty.IsLordParty && !mobileParty.IsGarrison))
                return;

            Kingdom kingdom = ResolveKingdom(mobileParty);
            if (kingdom == null)
                return;

            float strain = WarStateTracker.GetWarStrain(kingdom);
            if (strain <= 0.001f)
                return;

            float factor = strain * KMilitaryUpkeep;
            float before;
            float target;
            float after;
            float addedFactor;
            bool exact;
            if (!ApplyExactResultMultiplier(
                    ref __result,
                    1f + factor,
                    UpkeepExplanation,
                    out before,
                    out target,
                    out after,
                    out addedFactor,
                    out exact))
                return;

            _wageApplications++;

            if (_wageApplications <= 120 || !exact || (_wageApplications % 500) == 0)
            {
                ClanAIPostVanilla.WriteExternalLog(
                    "WAR_STRAIN_MILITARY_UPKEEP party=" + mobileParty.Name.ToString() +
                    " partyId=" + mobileParty.StringId +
                    " kingdom=" + kingdom.Name.ToString() +
                    " isLordParty=" + mobileParty.IsLordParty +
                    " isGarrison=" + mobileParty.IsGarrison +
                    " strain=" + F(strain) +
                    " requestedMultiplier=" + F(1f + factor) +
                    " vanillaWage=" + F(before) +
                    " targetWage=" + F(target) +
                    " finalWage=" + F(after) +
                    " addedNativeFactor=" + F(addedFactor) +
                    " exact=" + exact +
                    " applications=" + _wageApplications);
            }
        }

        private static bool ApplyExactResultMultiplier(
            ref ExplainedNumber value,
            float requestedMultiplier,
            TextObject explanation,
            out float before,
            out float target,
            out float after,
            out float addedFactor,
            out bool exact)
        {
            before = value.ResultNumber;
            target = before;
            after = before;
            addedFactor = 0f;
            exact = true;

            if (before <= 0.0001f || requestedMultiplier <= 1.0001f)
                return false;

            float requestedTarget = before * requestedMultiplier;
            target = Math.Max(
                value.LimitMinValue,
                Math.Min(value.LimitMaxValue, requestedTarget));

            if (Math.Abs(target - before) <= ExactTolerance)
                return false;

            float baseNumber = value.BaseNumber;
            if (Math.Abs(baseNumber) <= 0.0001f)
                return false;

            float requiredTotalFactor = (target / baseNumber) - 1f;
            addedFactor = requiredTotalFactor - value.SumOfFactors;
            value.AddFactor(addedFactor, explanation);
            after = value.ResultNumber;
            exact = Math.Abs(after - target) <= ExactTolerance;
            return true;
        }

        private static Kingdom ResolveKingdom(MobileParty party)
        {
            if (party == null)
                return null;
            if (party.ActualClan != null && party.ActualClan.Kingdom != null)
                return party.ActualClan.Kingdom;
            if (party.CurrentSettlement != null &&
                party.CurrentSettlement.OwnerClan != null)
                return party.CurrentSettlement.OwnerClan.Kingdom;
            return null;
        }

        private static string F(float value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }
    }
}
