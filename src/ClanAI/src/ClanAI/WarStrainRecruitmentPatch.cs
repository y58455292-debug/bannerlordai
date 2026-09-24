using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;

namespace ClanAI
{
    internal static class WarStrainRecruitmentPatch
    {
        private const float KReinforcementSpeed = 0.40f;
        private const float MinimumReinforcementRate = 0.60f;

        private static readonly Dictionary<string, float> AllowanceByParty =
            new Dictionary<string, float>(StringComparer.Ordinal);

        private sealed class AttemptState
        {
            internal long AttemptId;
            internal string ActorName;
            internal string PartyId;
            internal string KingdomName;
            internal string TroopName;
            internal float Strain;
            internal float Rate;
            internal float AllowanceBefore;
            internal float AllowanceAfter;
            internal bool Allow;
            internal int MenBefore;
        }

        private static bool _installed;
        private static long _attempts;
        private static long _allowed;
        private static long _skipped;

        internal static void Install()
        {
            if (_installed)
                return;

            Type type = AccessTools.TypeByName(
                "TaleWorlds.CampaignSystem.CampaignBehaviors.RecruitmentCampaignBehavior");
            if (type == null)
                throw new Exception("RecruitmentCampaignBehavior not found");

            MethodInfo target = AccessTools.Method(
                type,
                "GetRecruitVolunteerFromIndividual",
                new Type[]
                {
                    typeof(MobileParty),
                    typeof(CharacterObject),
                    typeof(Hero),
                    typeof(int)
                });
            if (target == null)
                throw new Exception("GetRecruitVolunteerFromIndividual not found");

            MethodInfo prefix = AccessTools.Method(
                typeof(WarStrainRecruitmentPatch),
                nameof(Prefix));
            MethodInfo postfix = AccessTools.Method(
                typeof(WarStrainRecruitmentPatch),
                nameof(Postfix));
            if (prefix == null)
                throw new Exception("WarStrain recruitment prefix not found");
            if (postfix == null)
                throw new Exception("WarStrain recruitment postfix not found");

            Harmony harmony = new Harmony(
                "com.bannerlordairesearch.clanai.warstrain.recruitment.v1");
            harmony.Patch(
                target,
                prefix: new HarmonyMethod(prefix),
                postfix: new HarmonyMethod(postfix));

            _installed = true;
            ClanAIPostVanilla.WriteExternalLog(
                "WAR_STRAIN_RECRUIT_PATCH_INSTALLED target=" +
                target.DeclaringType.FullName + "." + target.Name +
                " speedK=" + F(KReinforcementSpeed) +
                " minRate=" + F(MinimumReinforcementRate));
        }

        internal static void BeginSession()
        {
            AllowanceByParty.Clear();
            _attempts = 0;
            _allowed = 0;
            _skipped = 0;
            ClanAIPostVanilla.WriteExternalLog(
                "WAR_STRAIN_RECRUIT_SESSION_READY mode=deterministic-fractional-throttle" +
                " speedK=" + F(KReinforcementSpeed) +
                " minRate=" + F(MinimumReinforcementRate));
        }

        private static bool Prefix(
            MobileParty side1Party,
            CharacterObject subject,
            Hero individual,
            int bitCode,
            out AttemptState __state)
        {
            __state = null;
            if (side1Party == null ||
                !side1Party.IsActive ||
                !side1Party.IsLordParty ||
                side1Party == MobileParty.MainParty ||
                side1Party.LeaderHero == null ||
                side1Party.ActualClan == null ||
                side1Party.ActualClan.Kingdom == null)
                return true;

            Kingdom kingdom = side1Party.ActualClan.Kingdom;
            float strain = WarStateTracker.GetWarStrain(kingdom);
            if (strain <= 0.001f)
                return true;

            float rate = Math.Max(
                MinimumReinforcementRate,
                1f - strain * KReinforcementSpeed);

            string partyId = string.IsNullOrEmpty(side1Party.StringId)
                ? side1Party.LeaderHero.StringId
                : side1Party.StringId;
            if (string.IsNullOrEmpty(partyId))
                return true;

            float before;
            if (!AllowanceByParty.TryGetValue(partyId, out before))
                before = 1f;

            float after = before + rate;
            bool allow = after >= 1f;
            if (allow)
                after -= 1f;
            AllowanceByParty[partyId] = after;

            _attempts++;
            if (allow)
                _allowed++;
            else
                _skipped++;

            __state = new AttemptState
            {
                AttemptId = _attempts,
                ActorName = side1Party.LeaderHero.Name.ToString(),
                PartyId = partyId,
                KingdomName = kingdom.Name.ToString(),
                TroopName = subject == null ? "<none>" : subject.Name.ToString(),
                Strain = strain,
                Rate = rate,
                AllowanceBefore = before,
                AllowanceAfter = after,
                Allow = allow,
                MenBefore = side1Party.MemberRoster == null
                    ? 0
                    : side1Party.MemberRoster.TotalManCount
            };

            if (_attempts <= 200 || !allow || (_attempts % 500) == 0)
            {
                ClanAIPostVanilla.WriteExternalLog(
                    "WAR_STRAIN_RECRUIT_ATTEMPT actor=" + __state.ActorName +
                    " partyId=" + partyId +
                    " kingdom=" + __state.KingdomName +
                    " troop=" + __state.TroopName +
                    " sourceNotable=" + (individual == null ? "<none>" : individual.Name.ToString()) +
                    " strain=" + F(strain) +
                    " rate=" + F(rate) +
                    " allowanceBefore=" + F(before) +
                    " allowanceAfter=" + F(after) +
                    " allowed=" + allow +
                    " menBefore=" + __state.MenBefore +
                    " attempts=" + _attempts +
                    " allowedCount=" + _allowed +
                    " skippedCount=" + _skipped);
            }

            return allow;
        }

        private static void Postfix(
            MobileParty side1Party,
            CharacterObject subject,
            AttemptState __state)
        {
            if (__state == null || side1Party == null)
                return;

            int menAfter = side1Party.MemberRoster == null
                ? 0
                : side1Party.MemberRoster.TotalManCount;
            int delta = menAfter - __state.MenBefore;

            if (__state.AttemptId <= 200 ||
                !__state.Allow ||
                delta != (__state.Allow ? 1 : 0) ||
                (__state.AttemptId % 500) == 0)
            {
                ClanAIPostVanilla.WriteExternalLog(
                    "WAR_STRAIN_RECRUIT_RESULT actor=" + __state.ActorName +
                    " partyId=" + __state.PartyId +
                    " kingdom=" + __state.KingdomName +
                    " troop=" + __state.TroopName +
                    " strain=" + F(__state.Strain) +
                    " rate=" + F(__state.Rate) +
                    " allowed=" + __state.Allow +
                    " menBefore=" + __state.MenBefore +
                    " menAfter=" + menAfter +
                    " delta=" + delta +
                    " allowanceBefore=" + F(__state.AllowanceBefore) +
                    " allowanceAfter=" + F(__state.AllowanceAfter) +
                    " attemptId=" + __state.AttemptId);
            }
        }

        private static string F(float value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }
    }
}
