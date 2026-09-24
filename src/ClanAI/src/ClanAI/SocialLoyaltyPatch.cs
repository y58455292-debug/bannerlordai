using System;
using System.Globalization;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.BarterSystem.Barterables;

namespace ClanAI
{
    internal static class SocialLoyaltyPatch
    {
        private const float RelativeCap = 0.25f;
        private const int AbsoluteCap = 25000;
        private const int MinimumMeaningfulCap = 5000;
        private const float DirectLossRelativeCap = 1.25f;
        private const int DirectLossAbsoluteCap = 750000;
        private const int DirectLossMinimumCap = 75000;

        [ThreadStatic]
        private static LoyaltyContext _context;

        private static bool _installed;
        private static long _considerations;
        private static long _modified;
        private static long _positiveFlips;
        private static long _retainedByMemory;
        private static long _commits;

        private sealed class LoyaltyContext
        {
            internal Clan SourceClan;
            internal Kingdom OldKingdom;
            internal int NativeValue;
            internal int AdjustedValue;
            internal int SocialModifier;
            internal bool SawValue;
        }

        internal static void Install()
        {
            if (_installed)
                return;

            Type behaviorType = AccessTools.TypeByName(
                "TaleWorlds.CampaignSystem.CampaignBehaviors.BarterBehaviors.DiplomaticBartersBehavior");

            MethodInfo consider = behaviorType == null
                ? null
                : AccessTools.Method(
                    behaviorType,
                    "ConsiderClanLeaveKingdom",
                    new Type[] { typeof(Clan) });

            MethodInfo valueMethod = AccessTools.Method(
                typeof(LeaveKingdomAsClanBarterable),
                "GetUnitValueForFaction",
                new Type[] { typeof(IFaction) });

            if (consider == null)
                throw new Exception("DiplomaticBartersBehavior.ConsiderClanLeaveKingdom not found");
            if (valueMethod == null)
                throw new Exception("LeaveKingdomAsClanBarterable.GetUnitValueForFaction not found");

            Harmony harmony = new Harmony(
                "com.bannerlordairesearch.clanai.socialloyalty.v1");

            harmony.Patch(
                consider,
                prefix: new HarmonyMethod(
                    AccessTools.Method(typeof(SocialLoyaltyPatch), nameof(ConsiderPrefix))),
                postfix: new HarmonyMethod(
                    AccessTools.Method(typeof(SocialLoyaltyPatch), nameof(ConsiderPostfix))));

            harmony.Patch(
                valueMethod,
                postfix: new HarmonyMethod(
                    AccessTools.Method(typeof(SocialLoyaltyPatch), nameof(ValuePostfix))));

            _installed = true;

            ClanAIPostVanilla.WriteExternalLog(
                "SOCIAL_LOYALTY_PATCH_INSTALLED relativeCap=" + F(RelativeCap) +
                " absoluteCap=" + AbsoluteCap +
                " minCap=" + MinimumMeaningfulCap +
                " directLossRelativeCap=" + F(DirectLossRelativeCap) +
                " directLossAbsoluteCap=" + DirectLossAbsoluteCap +
                " directLossMinimumCap=" + DirectLossMinimumCap);
        }

        internal static void BeginSession()
        {
            _context = null;
            _considerations = 0;
            _modified = 0;
            _positiveFlips = 0;
            _retainedByMemory = 0;
            _commits = 0;

            ClanAIPostVanilla.WriteExternalLog(
                "SOCIAL_LOYALTY_SESSION_READY mode=ai-leave-kingdom-memory-bias");
        }

        private static void ConsiderPrefix(Clan clan)
        {
            _context = null;

            if (clan == null ||
                clan.Leader == null ||
                clan.Kingdom == null ||
                clan.Kingdom.RulingClan == clan ||
                clan.IsMinorFaction ||
                clan == Clan.PlayerClan)
            {
                return;
            }

            _context = new LoyaltyContext
            {
                SourceClan = clan,
                OldKingdom = clan.Kingdom
            };

            _considerations++;
        }

        private static void ValuePostfix(
            LeaveKingdomAsClanBarterable __instance,
            IFaction faction,
            ref int __result)
        {
            LoyaltyContext ctx = _context;

            if (ctx == null ||
                __instance == null ||
                __instance.OriginalOwner == null ||
                __instance.OriginalOwner.Clan != ctx.SourceClan ||
                faction != ctx.SourceClan)
            {
                return;
            }

            int native = __result;
            int modifier = ComputeLoyaltyModifier(
                ctx.SourceClan,
                ctx.OldKingdom,
                native);

            ctx.NativeValue = native;
            ctx.SocialModifier = modifier;
            ctx.AdjustedValue = native + modifier;
            ctx.SawValue = true;

            if (modifier != 0)
            {
                __result = ctx.AdjustedValue;
                _modified++;
            }
        }

        private static void ConsiderPostfix(Clan clan)
        {
            LoyaltyContext ctx = _context;
            _context = null;

            if (ctx == null ||
                clan == null ||
                !ctx.SawValue)
            {
                return;
            }

            bool nativeWouldLeave =
                ctx.NativeValue > 0;

            bool adjustedWouldLeave =
                ctx.AdjustedValue > 0;

            bool committed =
                clan.Kingdom != ctx.OldKingdom;

            if (!nativeWouldLeave && adjustedWouldLeave)
                _positiveFlips++;
            else if (nativeWouldLeave && !adjustedWouldLeave)
                _retainedByMemory++;

            if (committed)
                _commits++;

            ClanAIPostVanilla.WriteExternalLog(
                "SOCIAL_LOYALTY_CONSIDER clan=" + clan.Name +
                " leader=" + (clan.Leader == null ? "<none>" : clan.Leader.Name.ToString()) +
                " kingdom=" + (ctx.OldKingdom == null ? "<none>" : ctx.OldKingdom.Name.ToString()) +
                " nativeValue=" + ctx.NativeValue +
                " socialModifier=" + ctx.SocialModifier +
                " adjustedValue=" + ctx.AdjustedValue +
                " nativeWouldLeave=" + nativeWouldLeave +
                " adjustedWouldLeave=" + adjustedWouldLeave +
                " committed=" + committed +
                " kingdomAfter=" + (clan.Kingdom == null ? "<independent>" : clan.Kingdom.Name.ToString()) +
                " considerations=" + _considerations +
                " modified=" + _modified +
                " positiveFlips=" + _positiveFlips +
                " retainedByMemory=" + _retainedByMemory +
                " commits=" + _commits);
        }

        private static int ComputeLoyaltyModifier(
            Clan sourceClan,
            Kingdom currentKingdom,
            int nativeValue)
        {
            if (sourceClan == null ||
                sourceClan.Leader == null ||
                currentKingdom == null ||
                currentKingdom.RulingClan == null)
            {
                return 0;
            }

            int leaderTrust, leaderGrievance, leaderBloodDebt, leaderObligation, leaderTension;
            int clanTrust, clanGrievance, clanBloodDebt, clanObligation, clanTension;
            int clanRecords;

            float directLossPressure;
            double directLossAgeHours;
            int directLossCount;
            string directLossSettlements;
            float directLossValue;

            bool hasLeader = SocialLedger.TryGetState(
                sourceClan.Leader,
                currentKingdom.RulingClan,
                out leaderTrust,
                out leaderGrievance,
                out leaderBloodDebt,
                out leaderObligation,
                out leaderTension);

            bool hasClan = SocialLedger.TryGetClanAggregateStateForKingdom(
                sourceClan,
                currentKingdom,
                out clanTrust,
                out clanGrievance,
                out clanBloodDebt,
                out clanObligation,
                out clanTension,
                out clanRecords);

            bool hasDirectLoss =
                SocialLoyaltyClanLossMemory.TryGetPressure(
                    sourceClan,
                    out directLossPressure,
                    out directLossAgeHours,
                    out directLossCount,
                    out directLossSettlements,
                    out directLossValue);

            if (!hasLeader && !hasClan && !hasDirectLoss)
                return 0;

            float leaderIndex = hasLeader
                ? DissatisfactionIndex(
                    leaderTrust,
                    leaderGrievance,
                    leaderBloodDebt,
                    leaderObligation,
                    leaderTension)
                : 0f;

            float clanIndex = hasClan
                ? DissatisfactionIndex(
                    clanTrust,
                    clanGrievance,
                    clanBloodDebt,
                    clanObligation,
                    clanTension)
                : 0f;

            float socialIndex =
                BlendMemory(
                    hasLeader,
                    leaderIndex,
                    hasClan,
                    clanIndex);

            if (socialIndex > 100f)
                socialIndex = 100f;
            else if (socialIndex < -100f)
                socialIndex = -100f;

            float socialCap = Math.Min(
                AbsoluteCap,
                Math.Max(
                    MinimumMeaningfulCap,
                    Math.Abs((float)nativeValue) * RelativeCap));

            int socialModifier = (int)Math.Round(
                socialCap * (socialIndex / 100f));

            if (socialModifier > AbsoluteCap)
                socialModifier = AbsoluteCap;
            else if (socialModifier < -AbsoluteCap)
                socialModifier = -AbsoluteCap;

            float directLossCap = Math.Min(
                DirectLossAbsoluteCap,
                Math.Max(
                    DirectLossMinimumCap,
                    Math.Abs((float)nativeValue) *
                    DirectLossRelativeCap));

            int directLossModifier =
                hasDirectLoss
                    ? (int)Math.Round(
                        Math.Min(
                            directLossPressure,
                            directLossCap))
                    : 0;

            int modifier =
                socialModifier +
                directLossModifier;

            if (modifier != 0)
            {
                ClanAIPostVanilla.WriteExternalLog(
                    "SOCIAL_LOYALTY_MEMORY clan=" + sourceClan.Name +
                    " leader=" + sourceClan.Leader.Name +
                    " kingdom=" + currentKingdom.Name +
                    " nativeValue=" + nativeValue +
                    " socialIndex=" + F(socialIndex) +
                    " socialCap=" + F(socialCap) +
                    " socialModifier=" + socialModifier +
                    " directLossPressure=" +
                    F(hasDirectLoss ? directLossPressure : 0f) +
                    " directLossValue=" +
                    F(hasDirectLoss ? directLossValue : 0f) +
                    " directLoss=" +
                    (hasDirectLoss
                        ? "settlements:" + directLossSettlements +
                          ",youngestAgeHours:" +
                          directLossAgeHours.ToString(
                              "0.###",
                              CultureInfo.InvariantCulture) +
                          ",count:" + directLossCount
                        : "<none>") +
                    " directLossCap=" + F(directLossCap) +
                    " directLossModifier=" + directLossModifier +
                    " modifier=" + modifier +
                    " leaderState=" + StateLabel(
                        hasLeader,
                        leaderTrust,
                        leaderGrievance,
                        leaderBloodDebt,
                        leaderObligation,
                        leaderTension) +
                    " clanState=" + StateLabel(
                        hasClan,
                        clanTrust,
                        clanGrievance,
                        clanBloodDebt,
                        clanObligation,
                        clanTension) +
                    " clanRecords=" + clanRecords);
            }

            return modifier;
        }

        private static float DissatisfactionIndex(
            int trust,
            int grievance,
            int bloodDebt,
            int obligation,
            int tension)
        {
            return grievance * 0.50f +
                   bloodDebt * 0.70f +
                   tension * 0.25f -
                   trust * 0.35f -
                   obligation * 0.45f;
        }

        private static float BlendMemory(
            bool hasLeader,
            float leaderIndex,
            bool hasClan,
            float clanIndex)
        {
            if (hasLeader && hasClan)
                return leaderIndex * 0.65f + clanIndex * 0.35f;
            if (hasLeader)
                return leaderIndex;
            if (hasClan)
                return clanIndex;
            return 0f;
        }

        private static string StateLabel(
            bool present,
            int trust,
            int grievance,
            int bloodDebt,
            int obligation,
            int tension)
        {
            if (!present)
                return "<none>";

            return "trust:" + trust +
                ",grievance:" + grievance +
                ",bloodDebt:" + bloodDebt +
                ",obligation:" + obligation +
                ",tension:" + tension;
        }

        private static string F(float value)
        {
            return value.ToString(
                "0.###",
                CultureInfo.InvariantCulture);
        }
    }
}
