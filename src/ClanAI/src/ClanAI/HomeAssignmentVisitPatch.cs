using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace ClanAI
{
    // Exposes one real settlement to the native visit scorer; never creates AIBehaviorData.
    internal static class HomeAssignmentVisitPatch
    {
        private static bool _installed;
        private static MethodInfo Suitable, Navigation;
        private static ConstructorInfo NavigationRow;
        private static FieldInfo RowSettlement;
        internal static void Install()
        {
            if (_installed) return;
            Type visit = typeof(MobileParty).Assembly.GetType(
                "TaleWorlds.CampaignSystem.CampaignBehaviors.AiBehaviors.AiVisitSettlementBehavior", true);
            Type row = visit.GetNestedType("SettlementNavigationData", BindingFlags.NonPublic);
            Type list = typeof(List<>).MakeGenericType(row);
            MethodInfo fill = AccessTools.Method(visit, "FillSettlementsToVisitWithDistancesAsDays",
                new[] { typeof(MobileParty), list });
            Suitable = AccessTools.Method(visit, "IsSettlementSuitableForVisitingCondition");
            Navigation = AccessTools.Method(visit, "GetBestNavigationDataForVisitingSettlement");
            NavigationRow = row.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null, new[] { typeof(float), typeof(int), typeof(Settlement), typeof(MobileParty.NavigationType),
                    typeof(bool), typeof(bool) }, null);
            RowSettlement = row.GetField("Settlement", BindingFlags.Instance | BindingFlags.Public);
            if (fill == null || Suitable == null || Navigation == null || NavigationRow == null || RowSettlement == null)
                throw new MissingMethodException("Supported LW1-B native visit signature unavailable");
            new Harmony("clanai.homeassignment.visit.v1").Patch(fill,
                postfix: new HarmonyMethod(typeof(HomeAssignmentVisitPatch), nameof(Postfix)));
            _installed = true;
        }
        private static void Postfix(MobileParty __0, object __1)
        {
            HomeAssignmentCounters.RetentionEvaluations++;
            IList list = __1 as IList;
            if (list != null)
            {
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    object candidateRow = list[i];
                    Settlement candidate = candidateRow == null ? null : RowSettlement.GetValue(candidateRow) as Settlement;
                    if (KingdomBorderClosureConfig.BlocksVisit(__0, candidate)) list.RemoveAt(i);
                }
            }
            Settlement home;
            if (!HomeAssignmentStore.TryHome(__0, out home) ||
                KingdomBorderClosureConfig.BlocksVisit(__0, home) ||
                !HomeAssignmentStore.Eligible(__0) || !HomeAssignmentStore.Peace(__0) || HomeAssignmentStore.Urgent(__0, home)) return;
            if (list == null) return;
            foreach (object row in list)
                if (ReferenceEquals(RowSettlement.GetValue(row), home)) return;
            try
            {
                if (!(bool)Suitable.Invoke(null, new object[] { __0, home })) return;
                object[] args = { __0, home, MobileParty.NavigationType.None, 0f, false, false };
                Navigation.Invoke(null, args);
                var nav = (MobileParty.NavigationType)args[2];
                float distance = (float)args[3];
                if (!HomeAssignmentPolicy.CanExpose(true, HomeAssignmentStore.ValidHome(home), true,
                    false, false, true, nav == MobileParty.NavigationType.Default, distance)) return;
                list.Add(NavigationRow.Invoke(new object[] { distance, home.GetHashCode(), home, nav, args[4], args[5] }));
                HomeAssignmentCounters.RetentionApplications++;
            }
            catch (TargetInvocationException) { /* Native navigation failure: no destination added. */ }
        }
    }
}
