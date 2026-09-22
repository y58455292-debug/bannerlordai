using System;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;

namespace ClanAI
{
    internal static class SocialMercyPatch
    {
        private static bool _installed;

        public static void Install()
        {
            if (_installed)
                return;

            MethodInfo target =
                AccessTools.Method(
                    typeof(EndCaptivityAction),
                    "ApplyInternal",
                    new Type[]
                    {
                        typeof(Hero),
                        typeof(EndCaptivityDetail),
                        typeof(Hero),
                        typeof(bool)
                    });

            if (target == null)
            {
                ClanAIPostVanilla.WriteExternalLog(
                    "CAPTIVITY_FUNNEL_PATCH_FAILED target=<not-found>");

                return;
            }

            MethodInfo postfix =
                AccessTools.Method(
                    typeof(SocialMercyPatch),
                    nameof(Postfix));

            Harmony harmony =
                new Harmony(
                    "ClanAI.SocialCaptivityFunnel.v017C");

            harmony.Patch(
                target,
                postfix:
                    new HarmonyMethod(postfix));

            _installed = true;

            ClanAIPostVanilla.WriteExternalLog(
                "CAPTIVITY_FUNNEL_PATCH_INSTALLED" +
                " target=" +
                target.DeclaringType.FullName +
                "." +
                target.Name +
                "(Hero,EndCaptivityDetail,Hero,bool)");
        }

        private static void Postfix(
            Hero __0,
            EndCaptivityDetail __1,
            Hero __2,
            bool __3)
        {
            Hero prisoner =
                __0;

            EndCaptivityDetail detail =
                __1;

            Hero facilitator =
                __2;

            bool showNotification =
                __3;

            ClanAIPostVanilla.WriteExternalLog(
                "CAPTIVITY_FUNNEL" +
                " prisoner=" +
                HeroName(prisoner) +
                " prisonerClan=" +
                ClanName(
                    prisoner != null
                        ? prisoner.Clan
                        : null) +
                " detail=" +
                detail.ToString() +
                " facilitator=" +
                HeroName(facilitator) +
                " facilitatorClan=" +
                ClanName(
                    facilitator != null
                        ? facilitator.Clan
                        : null) +
                " showNotification=" +
                showNotification);

            // Only intentional mercy changes social memory.
            if (detail !=
                EndCaptivityDetail.ReleasedByChoice)
            {
                return;
            }

            ClanAIPostVanilla.WriteExternalLog(
                "MERCY_DIRECT" +
                " prisoner=" +
                HeroName(prisoner) +
                " prisonerClan=" +
                ClanName(
                    prisoner != null
                        ? prisoner.Clan
                        : null) +
                " facilitator=" +
                HeroName(facilitator) +
                " facilitatorClan=" +
                ClanName(
                    facilitator != null
                        ? facilitator.Clan
                        : null));

            SocialLedger.RecordMercy(
                prisoner,
                facilitator);
        }

        private static string HeroName(
            Hero hero)
        {
            return hero != null
                ? hero.Name.ToString()
                : "<none>";
        }

        private static string ClanName(
            Clan clan)
        {
            return clan != null
                ? clan.Name.ToString()
                : "<none>";
        }
    }
}
