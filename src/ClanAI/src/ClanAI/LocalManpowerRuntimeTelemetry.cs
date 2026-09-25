using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace ClanAI
{
    // Phase 4B runtime evidence only. This class observes native calls and
    // slot state; it never changes probability, volunteers, rosters, or world state.
    internal static class LocalManpowerRuntimeTelemetry
    {
        private sealed class EvaluationRecord
        {
            internal Hero Notable;
            internal int SlotIndex;
            internal bool SlotKnown;
            internal bool SlotIsEmpty;
            internal float NativeProbability;
            internal LocalManpowerProbabilityResult Result;
        }

        private sealed class SlotSnapshot
        {
            internal Hero Notable;
            internal int SlotIndex;
            internal CharacterObject Troop;
        }

        private sealed class DailySnapshot
        {
            internal Settlement Settlement;
            internal Dictionary<string, SlotSnapshot> Slots;
        }

        private sealed class RecruitSnapshot
        {
            internal MobileParty Party;
            internal Hero Notable;
            internal int SlotIndex;
            internal CharacterObject TroopBefore;
        }

        private static readonly Dictionary<string, EvaluationRecord>
            CurrentDailyEvaluations =
                new Dictionary<string, EvaluationRecord>(
                    StringComparer.Ordinal);

        private static Settlement _currentDailySettlement;
        private static bool _installed;
        private static bool _loggedHealthyEmpty;
        private static bool _loggedDegradedEmpty;
        private static bool _loggedOccupied;
        private static bool _loggedUnsupported;

        internal static void Install()
        {
            if (_installed)
                return;

            Type type = AccessTools.TypeByName(
                "TaleWorlds.CampaignSystem.CampaignBehaviors.RecruitmentCampaignBehavior");
            if (type == null)
                throw new InvalidOperationException(
                    "RecruitmentCampaignBehavior not found for Local Manpower telemetry");

            MethodInfo daily = AccessTools.Method(
                type,
                "UpdateVolunteersOfNotablesInSettlement",
                new Type[] { typeof(Settlement) });
            MethodInfo recruit = AccessTools.Method(
                type,
                "GetRecruitVolunteerFromIndividual",
                new Type[]
                {
                    typeof(MobileParty),
                    typeof(CharacterObject),
                    typeof(Hero),
                    typeof(int)
                });

            if (daily == null || recruit == null)
                throw new InvalidOperationException(
                    "Native recruitment telemetry seam not found");

            var harmony = new Harmony(
                "com.bannerlordairesearch.clanai.localmanpower.telemetry.v1");

            harmony.Patch(
                daily,
                prefix: new HarmonyMethod(
                    typeof(LocalManpowerRuntimeTelemetry),
                    nameof(DailyPrefix)),
                postfix: new HarmonyMethod(
                    typeof(LocalManpowerRuntimeTelemetry),
                    nameof(DailyPostfix)));

            harmony.Patch(
                recruit,
                prefix: new HarmonyMethod(
                    typeof(LocalManpowerRuntimeTelemetry),
                    nameof(RecruitPrefix)),
                postfix: new HarmonyMethod(
                    typeof(LocalManpowerRuntimeTelemetry),
                    nameof(RecruitPostfix)));

            _installed = true;
            ClanAIPostVanilla.WriteExternalLog(
                "LOCAL_MANPOWER_TELEMETRY_INSTALLED" +
                " dailyTarget=" + daily.DeclaringType.FullName + "." + daily.Name +
                " recruitTarget=" + recruit.DeclaringType.FullName + "." + recruit.Name +
                " mutation=False");
        }

        internal static void ObserveEvaluation(
            Hero notable,
            int slotIndex,
            Settlement settlement,
            bool slotKnown,
            bool slotIsEmpty,
            float nativeProbability,
            LocalManpowerProbabilityResult result)
        {
            try
            {
                ObserveEvaluationUnsafe(
                    notable,
                    slotIndex,
                    settlement,
                    slotKnown,
                    slotIsEmpty,
                    nativeProbability,
                    result);
            }
            catch (Exception ex)
            {
                LogTelemetryError(
                    "observe-evaluation",
                    ex);
            }
        }

        private static void ObserveEvaluationUnsafe(
            Hero notable,
            int slotIndex,
            Settlement settlement,
            bool slotKnown,
            bool slotIsEmpty,
            float nativeProbability,
            LocalManpowerProbabilityResult result)
        {
            if (result == null)
            {
                result = LocalManpowerProbabilityPolicy.Evaluate(
                    nativeProbability,
                    slotIsEmpty,
                    false,
                    LocalManpowerPopulationBand.Unknown,
                    false,
                    0.0f,
                    false);
            }

            if (ReferenceEquals(
                    settlement,
                    _currentDailySettlement))
            {
                CurrentDailyEvaluations[
                    SlotKey(notable, slotIndex)] =
                    new EvaluationRecord
                    {
                        Notable = notable,
                        SlotIndex = slotIndex,
                        SlotKnown = slotKnown,
                        SlotIsEmpty = slotIsEmpty,
                        NativeProbability = nativeProbability,
                        Result = result
                    };
            }

            bool log = false;
            string sample = "other";

            if (!slotKnown)
            {
                if (!_loggedUnsupported)
                {
                    _loggedUnsupported = true;
                    log = true;
                    sample = "unsupported";
                }
            }
            else if (!slotIsEmpty)
            {
                if (!_loggedOccupied)
                {
                    _loggedOccupied = true;
                    log = true;
                    sample = "occupied";
                }
            }
            else if (result.LocalMultiplier >= 0.999999f)
            {
                if (!_loggedHealthyEmpty)
                {
                    _loggedHealthyEmpty = true;
                    log = true;
                    sample = "healthy-empty";
                }
            }
            else
            {
                if (!_loggedDegradedEmpty)
                {
                    _loggedDegradedEmpty = true;
                    log = true;
                    sample = "degraded-empty";
                }
            }

            if (log)
                LogEvaluation(
                    "LOCAL_MANPOWER_EVAL_SAMPLE",
                    sample,
                    notable,
                    slotIndex,
                    settlement,
                    slotKnown,
                    slotIsEmpty,
                    nativeProbability,
                    result);
        }

        private static void DailyPrefix(
            Settlement settlement,
            out DailySnapshot __state)
        {
            __state = null;

            try
            {
                __state = new DailySnapshot
                {
                    Settlement = settlement,
                    Slots = CaptureSlots(settlement)
                };

                _currentDailySettlement = settlement;
                CurrentDailyEvaluations.Clear();
            }
            catch (Exception ex)
            {
                CurrentDailyEvaluations.Clear();
                _currentDailySettlement = null;
                LogTelemetryError(
                    "daily-prefix",
                    ex);
            }
        }

        private static void DailyPostfix(
            Settlement settlement,
            DailySnapshot __state)
        {
            try
            {
                if (__state == null ||
                    __state.Slots == null)
                {
                    return;
                }

                Dictionary<string, SlotSnapshot> after =
                    CaptureSlots(settlement);

                foreach (var pair in __state.Slots)
                {
                    SlotSnapshot before = pair.Value;
                    SlotSnapshot current;
                    if (!after.TryGetValue(
                            pair.Key,
                            out current))
                    {
                        continue;
                    }

                    if (SameTroop(
                            before.Troop,
                            current.Troop))
                    {
                        continue;
                    }

                    EvaluationRecord evaluation;
                    CurrentDailyEvaluations.TryGetValue(
                        pair.Key,
                        out evaluation);

                    ClanAIPostVanilla.WriteExternalLog(
                        "LOCAL_MANPOWER_NATIVE_SLOT_TRANSITION" +
                        " campaignHour=" + D(CampaignTime.Now.ToHours) +
                        SettlementFields(settlement) +
                        NotableFields(before.Notable) +
                        " slot=" + before.SlotIndex +
                        " before=" + TroopFields(before.Troop) +
                        " after=" + TroopFields(current.Troop) +
                        " transition=" + Transition(
                            before.Troop,
                            current.Troop) +
                        EvaluationFields(evaluation) +
                        " source=native-daily-volunteer-update" +
                        " mutationByClanAI=False");
                }
            }
            catch (Exception ex)
            {
                LogTelemetryError(
                    "daily-postfix",
                    ex);
            }
            finally
            {
                CurrentDailyEvaluations.Clear();
                _currentDailySettlement = null;
            }
        }

        private static void RecruitPrefix(
            MobileParty side1Party,
            CharacterObject subject,
            Hero individual,
            int bitCode,
            out RecruitSnapshot __state)
        {
            __state = null;

            try
            {
                __state = new RecruitSnapshot
                {
                    Party = side1Party,
                    Notable = individual,
                    SlotIndex = bitCode,
                    TroopBefore = ReadSlot(
                        individual,
                        bitCode)
                };
            }
            catch (Exception ex)
            {
                LogTelemetryError(
                    "recruit-prefix",
                    ex);
            }
        }

        private static void RecruitPostfix(
            MobileParty side1Party,
            CharacterObject subject,
            Hero individual,
            int bitCode,
            RecruitSnapshot __state)
        {
            try
            {
                if (__state == null ||
                    __state.TroopBefore == null)
                {
                    return;
                }

                CharacterObject after =
                    ReadSlot(
                        __state.Notable,
                        __state.SlotIndex);

                if (after != null)
                    return;

                ClanAIPostVanilla.WriteExternalLog(
                    "LOCAL_MANPOWER_SHARED_POOL_CONSUMPTION" +
                    " campaignHour=" + D(CampaignTime.Now.ToHours) +
                    " consumerPartyId=" +
                        Safe(__state.Party == null
                            ? null
                            : __state.Party.StringId) +
                    " consumer=" +
                        Safe(__state.Party == null
                            ? null
                            : __state.Party.Name.ToString()) +
                    NotableFields(__state.Notable) +
                    " slot=" + __state.SlotIndex +
                    " troop=" + TroopFields(__state.TroopBefore) +
                    " after=<empty>" +
                    " source=native-ai-notable-recruitment" +
                    " mutationByClanAI=False");
            }
            catch (Exception ex)
            {
                LogTelemetryError(
                    "recruit-postfix",
                    ex);
            }
        }

        private static void LogEvaluation(
            string eventName,
            string sample,
            Hero notable,
            int slotIndex,
            Settlement settlement,
            bool slotKnown,
            bool slotIsEmpty,
            float nativeProbability,
            LocalManpowerProbabilityResult result)
        {
            LocalManpowerPopulationBand band =
                PopulationBandSafe(settlement);

            bool hasSecurity;
            float security;
            SecuritySafe(
                settlement,
                out hasSecurity,
                out security);

            ClanAIPostVanilla.WriteExternalLog(
                eventName +
                " sample=" + sample +
                " campaignHour=" + D(CampaignTime.Now.ToHours) +
                SettlementFields(settlement) +
                NotableFields(notable) +
                " slot=" + slotIndex +
                " slotKnown=" + slotKnown +
                " slotState=" +
                    (slotKnown
                        ? (slotIsEmpty ? "empty" : "occupied")
                        : "unknown") +
                " populationBand=" + band +
                RawPopulationFields(settlement) +
                " securityAvailable=" + hasSecurity +
                " security=" +
                    (hasSecurity ? F(security) : "<unavailable>") +
                " underRaid=" +
                    (settlement != null && settlement.IsUnderRaid) +
                " underSiege=" +
                    (settlement != null && settlement.IsUnderSiege) +
                " nativeProbability=" + F(nativeProbability) +
                " populationFactor=" + F(result.PopulationFactor) +
                " securityFactor=" + F(result.SecurityFactor) +
                " acuteFactor=" + F(result.AcuteFactor) +
                " localMultiplier=" + F(result.LocalMultiplier) +
                " finalProbability=" + F(result.FinalProbability) +
                " applied=" + result.Applied +
                " reason=" + Safe(result.Reason) +
                " mutationByClanAI=False");
        }

        private static Dictionary<string, SlotSnapshot>
            CaptureSlots(Settlement settlement)
        {
            var result =
                new Dictionary<string, SlotSnapshot>(
                    StringComparer.Ordinal);

            if (settlement == null ||
                settlement.Notables == null)
            {
                return result;
            }

            foreach (Hero notable in settlement.Notables)
            {
                if (notable == null ||
                    notable.VolunteerTypes == null)
                {
                    continue;
                }

                for (int i = 0;
                     i < notable.VolunteerTypes.Length;
                     i++)
                {
                    result[SlotKey(notable, i)] =
                        new SlotSnapshot
                        {
                            Notable = notable,
                            SlotIndex = i,
                            Troop = notable.VolunteerTypes[i]
                        };
                }
            }

            return result;
        }

        private static CharacterObject ReadSlot(
            Hero notable,
            int index)
        {
            if (notable == null ||
                notable.VolunteerTypes == null ||
                index < 0 ||
                index >= notable.VolunteerTypes.Length)
            {
                return null;
            }

            return notable.VolunteerTypes[index];
        }

        private static bool SameTroop(
            CharacterObject a,
            CharacterObject b)
        {
            if (ReferenceEquals(a, b))
                return true;
            if (a == null || b == null)
                return false;
            return string.Equals(
                a.StringId,
                b.StringId,
                StringComparison.Ordinal);
        }

        private static string Transition(
            CharacterObject before,
            CharacterObject after)
        {
            if (before == null && after != null)
                return "fill-or-reorder-into-empty-slot";
            if (before != null && after == null)
                return "reorder-out-of-slot";
            return "upgrade-or-reorder";
        }

        private static string EvaluationFields(
            EvaluationRecord evaluation)
        {
            if (evaluation == null ||
                evaluation.Result == null)
            {
                return " evaluation=<unavailable>";
            }

            return
                " evaluationSlotState=" +
                    (evaluation.SlotKnown
                        ? (evaluation.SlotIsEmpty ? "empty" : "occupied")
                        : "unknown") +
                " nativeProbability=" +
                    F(evaluation.NativeProbability) +
                " populationFactor=" +
                    F(evaluation.Result.PopulationFactor) +
                " securityFactor=" +
                    F(evaluation.Result.SecurityFactor) +
                " acuteFactor=" +
                    F(evaluation.Result.AcuteFactor) +
                " localMultiplier=" +
                    F(evaluation.Result.LocalMultiplier) +
                " finalProbability=" +
                    F(evaluation.Result.FinalProbability) +
                " evaluationReason=" +
                    Safe(evaluation.Result.Reason);
        }

        private static LocalManpowerPopulationBand
            PopulationBandSafe(Settlement settlement)
        {
            try
            {
                if (settlement == null)
                    return LocalManpowerPopulationBand.Unknown;
                if (settlement.IsTown &&
                    settlement.Town != null)
                {
                    return MapLevel(
                        settlement.Town.GetProsperityLevel());
                }
                if (settlement.IsVillage &&
                    settlement.Village != null)
                {
                    return MapLevel(
                        settlement.Village.GetProsperityLevel());
                }
            }
            catch
            {
            }

            return LocalManpowerPopulationBand.Unknown;
        }

        private static LocalManpowerPopulationBand MapLevel(
            SettlementComponent.ProsperityLevel level)
        {
            if (level ==
                SettlementComponent.ProsperityLevel.High)
                return LocalManpowerPopulationBand.High;
            if (level ==
                SettlementComponent.ProsperityLevel.Mid)
                return LocalManpowerPopulationBand.Mid;
            if (level ==
                SettlementComponent.ProsperityLevel.Low)
                return LocalManpowerPopulationBand.Low;
            return LocalManpowerPopulationBand.Unknown;
        }

        private static void SecuritySafe(
            Settlement settlement,
            out bool hasSecurity,
            out float security)
        {
            hasSecurity = false;
            security = 0.0f;

            try
            {
                if (settlement == null)
                    return;

                if (settlement.IsTown &&
                    settlement.Town != null)
                {
                    security = settlement.Town.Security;
                    hasSecurity =
                        LocalManpowerProbabilityPolicy
                            .IsFinite(security);
                    return;
                }

                if (settlement.IsVillage &&
                    settlement.Village != null &&
                    settlement.Village.Bound != null &&
                    settlement.Village.Bound.Town != null)
                {
                    security =
                        settlement.Village.Bound.Town.Security;
                    hasSecurity =
                        LocalManpowerProbabilityPolicy
                            .IsFinite(security);
                }
            }
            catch
            {
                hasSecurity = false;
                security = 0.0f;
            }
        }

        private static string RawPopulationFields(
            Settlement settlement)
        {
            try
            {
                if (settlement != null &&
                    settlement.IsTown &&
                    settlement.Town != null)
                {
                    return
                        " prosperity=" +
                        F(settlement.Town.Prosperity) +
                        " hearth=<n/a>";
                }

                if (settlement != null &&
                    settlement.IsVillage &&
                    settlement.Village != null)
                {
                    return
                        " prosperity=<n/a>" +
                        " hearth=" +
                        F(settlement.Village.Hearth);
                }
            }
            catch
            {
            }

            return
                " prosperity=<unavailable>" +
                " hearth=<unavailable>";
        }

        private static string SettlementFields(
            Settlement settlement)
        {
            if (settlement == null)
            {
                return
                    " settlementId=<none>" +
                    " settlement=<none>" +
                    " kind=<none>";
            }

            return
                " settlementId=" +
                    Safe(settlement.StringId) +
                " settlement=" +
                    Safe(settlement.Name == null
                        ? null
                        : settlement.Name.ToString()) +
                " kind=" +
                    (settlement.IsTown
                        ? "Town"
                        : (settlement.IsVillage
                            ? "Village"
                            : "Other"));
        }

        private static string NotableFields(
            Hero notable)
        {
            return
                " notableId=" +
                    Safe(notable == null
                        ? null
                        : notable.StringId) +
                " notable=" +
                    Safe(notable == null ||
                         notable.Name == null
                        ? null
                        : notable.Name.ToString()) +
                " notableCulture=" +
                    Safe(notable == null ||
                         notable.Culture == null
                        ? null
                        : notable.Culture.StringId);
        }

        private static string TroopFields(
            CharacterObject troop)
        {
            if (troop == null)
                return "<empty>";

            return
                Safe(troop.StringId) +
                "[tier=" + troop.Tier +
                ",culture=" +
                Safe(troop.Culture == null
                    ? null
                    : troop.Culture.StringId) +
                "]";
        }

        private static string SlotKey(
            Hero notable,
            int slotIndex)
        {
            return
                (notable == null
                    ? "<null>"
                    : (notable.StringId ??
                       notable.Name.ToString())) +
                "|" +
                slotIndex.ToString(
                    CultureInfo.InvariantCulture);
        }

        private static void LogTelemetryError(
            string stage,
            Exception ex)
        {
            try
            {
                ClanAIPostVanilla.WriteExternalLog(
                    "LOCAL_MANPOWER_TELEMETRY_ERROR" +
                    " stage=" + Safe(stage) +
                    " type=" +
                        (ex == null
                            ? "<none>"
                            : ex.GetType().Name) +
                    " message=" +
                        Safe(ex == null
                            ? null
                            : ex.Message) +
                    " mutationByClanAI=False");
            }
            catch
            {
            }
        }

        private static string Safe(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "<none>";

            return value
                .Replace(" ", "_")
                .Replace("\t", "_")
                .Replace("\r", "_")
                .Replace("\n", "_");
        }

        private static string F(float value)
        {
            return value.ToString(
                "R",
                CultureInfo.InvariantCulture);
        }

        private static string D(double value)
        {
            return value.ToString(
                "R",
                CultureInfo.InvariantCulture);
        }
    }
}
