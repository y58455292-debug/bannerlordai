using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using HarmonyLib;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;

namespace BannerlordInspector
{
    /// <summary>
    /// Read-only census of TaleWorlds' MobilePartyAIModel decision contract.
    /// Discovers every loaded concrete implementation and patches only the
    /// four public initiative/attack/avoidance methods. Hot paths update
    /// counters only; detailed logs are emitted only when the model itself
    /// selects EngageParty or a bandit target.
    /// </summary>
    public static class InitiativeModelProbe
    {
        private static bool _applied;

        private static long _checkCalls;
        private static long _checkTrue;

        private static long _attackCalls;
        private static long _attackBanditTargets;
        private static long _attackBanditTrue;
        private static long _attackLordBanditTrue;

        private static long _avoidCalls;
        private static long _avoidBanditTargets;
        private static long _avoidBanditTrue;

        private static long _bestCalls;
        private static long _bestEngage;
        private static long _bestBanditTarget;
        private static long _bestLordBanditTarget;

        private static long _nextSummaryAt =
            250000;

        private static readonly object _detailSync =
            new object();

        private static readonly Dictionary<string, string>
            _lastLordBanditBestByActor =
                new Dictionary<string, string>(
                    StringComparer.Ordinal);

        public static int Apply(
            Harmony harmony)
        {
            if (_applied ||
                harmony == null)
            {
                return 0;
            }

            var patched =
                new HashSet<MethodBase>();

            Type baseType =
                typeof(MobilePartyAIModel);

            foreach (
                Assembly assembly
                in AppDomain.CurrentDomain
                    .GetAssemblies())
            {
                Type[] types;

                try
                {
                    types = assembly.GetTypes();
                }
                catch (
                    ReflectionTypeLoadException ex)
                {
                    types =
                        ex.Types
                            .Where(x => x != null)
                            .ToArray();
                }
                catch
                {
                    continue;
                }

                foreach (Type type in types)
                {
                    if (type == null ||
                        type.IsAbstract ||
                        !baseType.IsAssignableFrom(
                            type))
                    {
                        continue;
                    }

                    // ClanAI v0.20A is a delegating mirror. Patching both
                    // the mirror and its inner model would count the same
                    // decision twice. The inner TaleWorlds model remains
                    // fully observed during mirror validation.
                    if (string.Equals(
                            type.FullName,
                            "ClanAI.DelegatingMobilePartyAIModel",
                            StringComparison.Ordinal))
                    {
                        continue;
                    }

                    PatchMethod(
                        harmony,
                        type,
                        "ShouldPartyCheckInitiativeBehavior",
                        nameof(
                            AfterShouldCheck),
                        patched);

                    PatchMethod(
                        harmony,
                        type,
                        "ShouldConsiderAttacking",
                        nameof(
                            AfterShouldAttack),
                        patched);

                    PatchMethod(
                        harmony,
                        type,
                        "ShouldConsiderAvoiding",
                        nameof(
                            AfterShouldAvoid),
                        patched);

                    PatchMethod(
                        harmony,
                        type,
                        "GetBestInitiativeBehavior",
                        nameof(
                            AfterGetBest),
                        patched);
                }
            }

            _applied = true;

            InspectorLog.Info(
                "INITIATIVE_MODEL_PROBE_ATTACHED" +
                " methods=" +
                patched.Count);

            return patched.Count;
        }

        public static void LogActiveModel()
        {
            try
            {
                var campaign =
                    TaleWorlds.CampaignSystem
                        .Campaign.Current;

                MobilePartyAIModel model =
                    campaign == null ||
                    campaign.Models == null
                        ? null
                        : campaign.Models
                            .MobilePartyAIModel;

                InspectorLog.Info(
                    "INITIATIVE_MODEL_ACTIVE type=" +
                    (
                        model == null
                            ? "<null>"
                            : model.GetType()
                                .FullName
                    ));
            }
            catch (Exception ex)
            {
                InspectorLog.Warn(
                    "Could not read active " +
                    "MobilePartyAIModel: " +
                    ex.GetType().Name +
                    " - " +
                    ex.Message);
            }
        }

        private static void PatchMethod(
            Harmony harmony,
            Type type,
            string name,
            string postfixName,
            HashSet<MethodBase> patched)
        {
            try
            {
                MethodInfo method =
                    AccessTools.Method(
                        type,
                        name);

                if (method == null ||
                    method.IsAbstract ||
                    !patched.Add(method))
                {
                    return;
                }

                harmony.Patch(
                    method,
                    postfix:
                        new HarmonyMethod(
                            AccessTools.Method(
                                typeof(
                                    InitiativeModelProbe),
                                postfixName)));
            }
            catch (Exception ex)
            {
                InspectorLog.Warn(
                    "Initiative probe skipped " +
                    type.FullName +
                    "." +
                    name +
                    ": " +
                    ex.GetType().Name +
                    " - " +
                    ex.Message);
            }
        }

        public static void AfterShouldCheck(
            MobileParty __0,
            bool __result)
        {
            Interlocked.Increment(
                ref _checkCalls);

            if (__result)
            {
                Interlocked.Increment(
                    ref _checkTrue);
            }

            MaybeSummary();
        }

        public static void AfterShouldAttack(
            MobileParty __0,
            MobileParty __1,
            bool __result)
        {
            Interlocked.Increment(
                ref _attackCalls);

            bool bandit =
                IsBandit(__1);

            if (bandit)
            {
                Interlocked.Increment(
                    ref _attackBanditTargets);

                if (__result)
                {
                    Interlocked.Increment(
                        ref _attackBanditTrue);

                    if (IsLord(__0))
                    {
                        Interlocked.Increment(
                            ref _attackLordBanditTrue);
                    }
                }
            }

            MaybeSummary();
        }

        public static void AfterShouldAvoid(
            MobileParty __0,
            MobileParty __1,
            bool __result)
        {
            Interlocked.Increment(
                ref _avoidCalls);

            bool bandit =
                IsBandit(__1);

            if (bandit)
            {
                Interlocked.Increment(
                    ref _avoidBanditTargets);

                if (__result)
                {
                    Interlocked.Increment(
                        ref _avoidBanditTrue);
                }
            }

            MaybeSummary();
        }

        public static void AfterGetBest(
            MobileParty __0,
            ref AiBehavior __1,
            ref MobileParty __2,
            ref float __3,
            ref Vec2 __4)
        {
            Interlocked.Increment(
                ref _bestCalls);

            bool engage =
                __1 == AiBehavior.EngageParty;

            bool bandit =
                IsBandit(__2);

            if (engage)
            {
                Interlocked.Increment(
                    ref _bestEngage);
            }

            if (bandit)
            {
                Interlocked.Increment(
                    ref _bestBanditTarget);

                if (IsLord(__0))
                {
                    Interlocked.Increment(
                        ref _bestLordBanditTarget);
                }
            }

            bool lordBandit =
                IsLord(__0) &&
                bandit;

            if (lordBandit &&
                ShouldLogLordBanditBest(
                    __0,
                    __1,
                    __2))
            {
                InspectorLog.Info(
                    "INITIATIVE_MODEL_LORD_BANDIT_BEST" +
                    " actor=" +
                    SafeName(__0) +
                    " actorId=" +
                    SafeId(__0) +
                    " behavior=" +
                    __1 +
                    " target=" +
                    SafeName(__2) +
                    " targetId=" +
                    SafeId(__2) +
                    " score=" +
                    __3.ToString("0.000"));
            }

            MaybeSummary();
        }

        private static void MaybeSummary()
        {
            long total =
                Interlocked.Read(
                    ref _attackCalls) +
                Interlocked.Read(
                    ref _avoidCalls) +
                Interlocked.Read(
                    ref _bestCalls);

            long threshold =
                Interlocked.Read(
                    ref _nextSummaryAt);

            if (total < threshold)
                return;

            if (Interlocked.CompareExchange(
                    ref _nextSummaryAt,
                    threshold + 250000,
                    threshold) != threshold)
            {
                return;
            }

            InspectorLog.Info(
                "INITIATIVE_MODEL_CENSUS" +
                " checkCalls=" +
                Interlocked.Read(
                    ref _checkCalls) +
                " checkTrue=" +
                Interlocked.Read(
                    ref _checkTrue) +
                " attackCalls=" +
                Interlocked.Read(
                    ref _attackCalls) +
                " attackBanditTargets=" +
                Interlocked.Read(
                    ref _attackBanditTargets) +
                " attackBanditTrue=" +
                Interlocked.Read(
                    ref _attackBanditTrue) +
                " attackLordBanditTrue=" +
                Interlocked.Read(
                    ref _attackLordBanditTrue) +
                " avoidCalls=" +
                Interlocked.Read(
                    ref _avoidCalls) +
                " avoidBanditTargets=" +
                Interlocked.Read(
                    ref _avoidBanditTargets) +
                " avoidBanditTrue=" +
                Interlocked.Read(
                    ref _avoidBanditTrue) +
                " bestCalls=" +
                Interlocked.Read(
                    ref _bestCalls) +
                " bestEngage=" +
                Interlocked.Read(
                    ref _bestEngage) +
                " bestBanditTarget=" +
                Interlocked.Read(
                    ref _bestBanditTarget) +
                " bestLordBanditTarget=" +
                Interlocked.Read(
                    ref _bestLordBanditTarget));
        }

        private static bool ShouldLogLordBanditBest(
            MobileParty actor,
            AiBehavior behavior,
            MobileParty target)
        {
            string actorId =
                SafeId(actor);

            string signature =
                behavior.ToString() +
                "|" +
                SafeId(target);

            lock (_detailSync)
            {
                string previous;

                if (_lastLordBanditBestByActor
                    .TryGetValue(
                        actorId,
                        out previous) &&
                    string.Equals(
                        previous,
                        signature,
                        StringComparison.Ordinal))
                {
                    return false;
                }

                _lastLordBanditBestByActor[
                    actorId] = signature;

                return true;
            }
        }

        private static bool IsBandit(
            MobileParty party)
        {
            try
            {
                return
                    party != null &&
                    party.IsBandit;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsLord(
            MobileParty party)
        {
            try
            {
                return
                    party != null &&
                    party.IsLordParty;
            }
            catch
            {
                return false;
            }
        }

        private static string SafeName(
            MobileParty party)
        {
            try
            {
                return
                    party == null
                        ? "<null>"
                        : party.Name.ToString();
            }
            catch
            {
                return "<unreadable>";
            }
        }

        private static string SafeId(
            MobileParty party)
        {
            try
            {
                return
                    party == null
                        ? "<null>"
                        : party.StringId;
            }
            catch
            {
                return "<unreadable>";
            }
        }
    }
}
