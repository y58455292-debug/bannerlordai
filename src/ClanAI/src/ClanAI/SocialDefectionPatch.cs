using System;
using System.Globalization;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.BarterSystem.Barterables;

namespace ClanAI
{
    internal static class SocialDefectionPatch
    {
        private const float RelativeCap = 0.25f;
        private const int AbsoluteCap = 25000;
        private const int MinimumMeaningfulCap = 5000;

        [ThreadStatic]
        private static DefectionContext _context;

        private static bool _installed;
        private static long _considerations;
        private static long _modified;
        private static long _positiveFlips;
        private static long _blockedNative;
        private static long _commits;

        private sealed class DefectionContext
        {
            internal Clan SourceClan;
            internal Kingdom OldKingdom;
            internal Kingdom TargetKingdom;
            internal int NativeClanValue;
            internal int AdjustedClanValue;
            internal int TargetKingdomValue;
            internal int SocialModifier;
            internal bool SawClanValue;
            internal bool SawTargetValue;
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
                    "ConsiderDefection",
                    new Type[] { typeof(Clan), typeof(Kingdom) });

            MethodInfo valueMethod = AccessTools.Method(
                typeof(JoinKingdomAsClanBarterable),
                "GetUnitValueForFaction",
                new Type[] { typeof(IFaction) });

            if (consider == null)
                throw new Exception("DiplomaticBartersBehavior.ConsiderDefection not found");
            if (valueMethod == null)
                throw new Exception("JoinKingdomAsClanBarterable.GetUnitValueForFaction not found");

            Harmony harmony = new Harmony(
                "com.bannerlordairesearch.clanai.socialdefection.v1");

            harmony.Patch(
                consider,
                prefix: new HarmonyMethod(
                    AccessTools.Method(typeof(SocialDefectionPatch), nameof(ConsiderPrefix))),
                postfix: new HarmonyMethod(
                    AccessTools.Method(typeof(SocialDefectionPatch), nameof(ConsiderPostfix))));

            harmony.Patch(
                valueMethod,
                postfix: new HarmonyMethod(
                    AccessTools.Method(typeof(SocialDefectionPatch), nameof(ValuePostfix))));

            _installed = true;
            ClanAIPostVanilla.WriteExternalLog(
                "SOCIAL_DEFECTION_PATCH_INSTALLED relativeCap=" + F(RelativeCap) +
                " absoluteCap=" + AbsoluteCap +
                " minCap=" + MinimumMeaningfulCap);
        }

        internal static void BeginSession()
        {
            _context = null;
            _considerations = 0;
            _modified = 0;
            _positiveFlips = 0;
            _blockedNative = 0;
            _commits = 0;

            ClanAIPostVanilla.WriteExternalLog(
                "SOCIAL_DEFECTION_SESSION_READY mode=ai-defection-barter-memory-bias");
        }

        private static void ConsiderPrefix(Clan clan1, Kingdom kingdom)
        {
            _context = null;
            if (clan1 == null || kingdom == null || clan1.Leader == null ||
                clan1.Kingdom == null || clan1.Kingdom == kingdom ||
                clan1.Kingdom.RulingClan == clan1)
                return;

            _context = new DefectionContext
            {
                SourceClan = clan1,
                OldKingdom = clan1.Kingdom,
                TargetKingdom = kingdom
            };
            _considerations++;
        }

        private static void ValuePostfix(
            JoinKingdomAsClanBarterable __instance,
            IFaction factionForEvaluation,
            ref int __result)
        {
            DefectionContext ctx = _context;
            if (ctx == null || __instance == null || !__instance.IsDefecting ||
                __instance.OriginalOwner == null ||
                __instance.OriginalOwner.Clan != ctx.SourceClan ||
                __instance.TargetKingdom != ctx.TargetKingdom)
                return;

            if (factionForEvaluation == ctx.SourceClan)
            {
                int native = __result;
                int modifier = ComputeSocialModifier(
                    ctx.SourceClan,
                    ctx.OldKingdom,
                    ctx.TargetKingdom,
                    native);

                ctx.NativeClanValue = native;
                ctx.SocialModifier = modifier;
                ctx.AdjustedClanValue = native + modifier;
                ctx.SawClanValue = true;

                if (modifier != 0)
                {
                    __result = ctx.AdjustedClanValue;
                    _modified++;
                }
            }
            else if (factionForEvaluation.MapFaction == ctx.TargetKingdom)
            {
                ctx.TargetKingdomValue = __result;
                ctx.SawTargetValue = true;
            }
        }

        private static void ConsiderPostfix(Clan clan1, Kingdom kingdom)
        {
            DefectionContext ctx = _context;
            _context = null;
            if (ctx == null || clan1 == null || kingdom == null ||
                !ctx.SawClanValue || !ctx.SawTargetValue)
                return;

            int nativeSum = ctx.NativeClanValue + ctx.TargetKingdomValue;
            int adjustedSum = ctx.AdjustedClanValue + ctx.TargetKingdomValue;
            int nativeDemand = ctx.NativeClanValue < 0 ? -ctx.NativeClanValue : 0;
            int adjustedDemand = ctx.AdjustedClanValue < 0 ? -ctx.AdjustedClanValue : 0;
            float affordable = kingdom.Leader == null ? 0f : kingdom.Leader.Gold * 0.5f;

            bool nativeWould = nativeSum > 0 && nativeDemand <= affordable;
            bool adjustedWould = adjustedSum > 0 && adjustedDemand <= affordable;
            bool committed = clan1.Kingdom == kingdom;

            if (!nativeWould && adjustedWould)
                _positiveFlips++;
            else if (nativeWould && !adjustedWould)
                _blockedNative++;

            if (committed)
                _commits++;

            ClanAIPostVanilla.WriteExternalLog(
                "SOCIAL_DEFECTION_CONSIDER clan=" + clan1.Name +
                " leader=" + (clan1.Leader == null ? "<none>" : clan1.Leader.Name.ToString()) +
                " from=" + (ctx.OldKingdom == null ? "<none>" : ctx.OldKingdom.Name.ToString()) +
                " target=" + kingdom.Name +
                " nativeClanValue=" + ctx.NativeClanValue +
                " socialModifier=" + ctx.SocialModifier +
                " adjustedClanValue=" + ctx.AdjustedClanValue +
                " targetValue=" + ctx.TargetKingdomValue +
                " nativeSum=" + nativeSum +
                " adjustedSum=" + adjustedSum +
                " nativeDemand=" + nativeDemand +
                " adjustedDemand=" + adjustedDemand +
                " affordable=" + F(affordable) +
                " nativeWouldDefect=" + nativeWould +
                " adjustedWouldDefect=" + adjustedWould +
                " committed=" + committed +
                " considerations=" + _considerations +
                " modified=" + _modified +
                " positiveFlips=" + _positiveFlips +
                " blockedNative=" + _blockedNative +
                " commits=" + _commits);

            PlayerVisibilityLayer.NotifyDefectionConsider(
                clan1,
                ctx.OldKingdom,
                kingdom,
                ctx.SocialModifier,
                committed);
        }

        private static int ComputeSocialModifier(
            Clan sourceClan,
            Kingdom oldKingdom,
            Kingdom targetKingdom,
            int nativeValue)
        {
            if (sourceClan == null || sourceClan.Leader == null ||
                oldKingdom == null || oldKingdom.RulingClan == null ||
                targetKingdom == null || targetKingdom.RulingClan == null)
                return 0;

            int oldTrust, oldGrievance, oldBloodDebt, oldObligation, oldTension;
            int newTrust, newGrievance, newBloodDebt, newObligation, newTension;
            int oldClanTrust, oldClanGrievance, oldClanBloodDebt, oldClanObligation, oldClanTension;
            int newClanTrust, newClanGrievance, newClanBloodDebt, newClanObligation, newClanTension;
            int oldClanRecords;
            int newClanRecords;

            bool hasOldLeader = SocialLedger.TryGetState(
                sourceClan.Leader,
                oldKingdom.RulingClan,
                out oldTrust,
                out oldGrievance,
                out oldBloodDebt,
                out oldObligation,
                out oldTension);

            bool hasNewLeader = SocialLedger.TryGetState(
                sourceClan.Leader,
                targetKingdom.RulingClan,
                out newTrust,
                out newGrievance,
                out newBloodDebt,
                out newObligation,
                out newTension);

            bool hasOldClan = SocialLedger.TryGetClanAggregateStateForKingdom(
                sourceClan,
                oldKingdom,
                out oldClanTrust,
                out oldClanGrievance,
                out oldClanBloodDebt,
                out oldClanObligation,
                out oldClanTension,
                out oldClanRecords);

            bool hasNewClan = SocialLedger.TryGetClanAggregateStateForKingdom(
                sourceClan,
                targetKingdom,
                out newClanTrust,
                out newClanGrievance,
                out newClanBloodDebt,
                out newClanObligation,
                out newClanTension,
                out newClanRecords);

            if (!hasOldLeader && !hasNewLeader && !hasOldClan && !hasNewClan)
                return 0;

            float oldLeaderIndex = hasOldLeader
                ? CurrentLiegeMemoryIndex(
                    oldTrust,
                    oldGrievance,
                    oldBloodDebt,
                    oldObligation,
                    oldTension)
                : 0f;
            float oldClanIndex = hasOldClan
                ? CurrentLiegeMemoryIndex(
                    oldClanTrust,
                    oldClanGrievance,
                    oldClanBloodDebt,
                    oldClanObligation,
                    oldClanTension)
                : 0f;
            float newLeaderIndex = hasNewLeader
                ? TargetLiegeMemoryIndex(
                    newTrust,
                    newGrievance,
                    newBloodDebt,
                    newObligation,
                    newTension)
                : 0f;
            float newClanIndex = hasNewClan
                ? TargetLiegeMemoryIndex(
                    newClanTrust,
                    newClanGrievance,
                    newClanBloodDebt,
                    newClanObligation,
                    newClanTension)
                : 0f;

            float memoryIndex =
                BlendMemory(hasOldLeader, oldLeaderIndex, hasOldClan, oldClanIndex) +
                BlendMemory(hasNewLeader, newLeaderIndex, hasNewClan, newClanIndex);

            if (memoryIndex > 100f)
                memoryIndex = 100f;
            else if (memoryIndex < -100f)
                memoryIndex = -100f;

            float cap = Math.Min(
                AbsoluteCap,
                Math.Max(
                    MinimumMeaningfulCap,
                    Math.Abs(nativeValue) * RelativeCap));

            int modifier = (int)Math.Round(
                cap * (memoryIndex / 100f));

            if (modifier > AbsoluteCap)
                modifier = AbsoluteCap;
            else if (modifier < -AbsoluteCap)
                modifier = -AbsoluteCap;

            if (modifier != 0)
            {
                ClanAIPostVanilla.WriteExternalLog(
                    "SOCIAL_DEFECTION_MEMORY clan=" + sourceClan.Name +
                    " leader=" + sourceClan.Leader.Name +
                    " from=" + oldKingdom.Name +
                    " target=" + targetKingdom.Name +
                    " nativeValue=" + nativeValue +
                    " memoryIndex=" + F(memoryIndex) +
                    " cap=" + F(cap) +
                    " modifier=" + modifier +
                    " oldLeaderState=" + StateLabel(
                        hasOldLeader,
                        oldTrust,
                        oldGrievance,
                        oldBloodDebt,
                        oldObligation,
                        oldTension) +
                    " oldClanState=" + StateLabel(
                        hasOldClan,
                        oldClanTrust,
                        oldClanGrievance,
                        oldClanBloodDebt,
                        oldClanObligation,
                        oldClanTension) +
                    " oldClanRecords=" + oldClanRecords +
                    " targetLeaderState=" + StateLabel(
                        hasNewLeader,
                        newTrust,
                        newGrievance,
                        newBloodDebt,
                        newObligation,
                        newTension) +
                    " targetClanState=" + StateLabel(
                        hasNewClan,
                        newClanTrust,
                        newClanGrievance,
                        newClanBloodDebt,
                        newClanObligation,
                        newClanTension) +
                    " targetClanRecords=" + newClanRecords);
            }

            return modifier;
        }

        private static float CurrentLiegeMemoryIndex(
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

        private static float TargetLiegeMemoryIndex(
            int trust,
            int grievance,
            int bloodDebt,
            int obligation,
            int tension)
        {
            return trust * 0.35f +
                   obligation * 0.45f -
                   grievance * 0.50f -
                   bloodDebt * 0.70f -
                   tension * 0.25f;
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
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }
    }
}
