using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace BannerlordInspector
{
    /// <summary>
    /// Experiment 03E - Reference Territory Need Monitor (READ ONLY; not called by tracer)
    ///
    /// This monitor does not change any AI score or behavior.
    ///
    /// For Apolytea/Neretzes clan territory it records:
    /// - vanilla GoToSettlement candidate score, when present
    /// - active siege / raid state
    /// - Bannerlord's own ideal garrison strength for the clan/kingdom
    /// - actual permanent garrison EstimatedStrength
    /// - normalized garrison deficit
    /// - militia
    /// - town economic/security/loyalty trends
    /// - village hearth/militia trends
    ///
    /// Conservative TerritoryNeedPressure:
    ///   max(activeSiegePressure, activeRaidPressure, normalizedGarrisonDeficit)
    ///
    /// Active siege and active raid are binary pressure=1.
    /// "IsRaided" and economic trends are logged but NOT yet given weights.
    /// This avoids inventing economic thresholds before we study them.
    /// </summary>
    public static class TerritoryNeedMonitor
    {
        public const string Version = "0.8";
        public const string TargetPartyId = "lord_1_1_8_party_1"; // Apolytea

        private static readonly MethodInfo IdealGarrisonMethod =
            AccessTools.Method(
                AccessTools.TypeByName("Helpers.FactionHelper"),
                "FindIdealGarrisonStrengthPerWalledCenter");

        private sealed class CandidateScore
        {
            public string SettlementId;
            public float Score;
        }

        public static void Observe(MobileParty party, PartyThinkParams think)
        {
            if (party == null || think == null) return;
            if (!string.Equals(party.StringId, TargetPartyId, StringComparison.Ordinal)) return;

            Clan clan = party.ActualClan;
            if (clan == null) return;

            // Hard lab identity guard. This monitor is read-only, but keeping the guard
            // prevents us from accidentally interpreting a different world-state as Neretzes.
            if (!string.Equals(clan.Name.ToString(), "Neretzes", StringComparison.Ordinal))
            {
                InspectorLog.Warn(
                    "TERRITORY NEED MONITOR v" + Version +
                    " guard stopped: target party is no longer in clan Neretzes.");
                return;
            }

            Dictionary<string, float> vanillaScores = ReadGoToSettlementScores(think);

            float idealGarrison = ReadIdealGarrisonStrength(clan);
            float highestPressure = 0f;
            string highestPressureTarget = null;
            int holdings = 0;

            foreach (Settlement settlement in Settlement.All)
            {
                if (settlement == null || settlement.OwnerClan == null)
                    continue;

                if (!object.ReferenceEquals(settlement.OwnerClan, clan) &&
                    !string.Equals(
                        settlement.OwnerClan.StringId,
                        clan.StringId,
                        StringComparison.Ordinal))
                    continue;

                holdings++;

                bool isVillage = settlement.IsVillage;
                bool isWalled = !isVillage && settlement.Town != null;

                float actualGarrison = 0f;
                string garrisonPartyId = null;
                float garrisonRatio = -1f;
                float garrisonDeficit = 0f;

                if (isWalled)
                {
                    MobileParty garrison = FindGarrison(settlement);
                    if (garrison != null)
                    {
                        garrisonPartyId = garrison.StringId;
                        actualGarrison = garrison.Party == null
                            ? 0f
                            : garrison.Party.EstimatedStrength;
                    }

                    if (idealGarrison > 0f)
                    {
                        garrisonRatio = actualGarrison / idealGarrison;
                        garrisonDeficit = Clamp01(
                            (idealGarrison - actualGarrison) / idealGarrison);
                    }
                }

                float siegePressure = settlement.IsUnderSiege ? 1f : 0f;
                float raidPressure = settlement.IsUnderRaid ? 1f : 0f;

                // Conservative v0.1 pressure: only active military threat and vanilla
                // garrison deficit are allowed to create pressure.
                float pressure = Math.Max(
                    Math.Max(siegePressure, raidPressure),
                    garrisonDeficit);

                if (pressure > highestPressure)
                {
                    highestPressure = pressure;
                    highestPressureTarget = settlement.Name.ToString();
                }

                float vanillaScore;
                bool hasVanillaScore = vanillaScores.TryGetValue(
                    settlement.StringId, out vanillaScore);

                Town town = settlement.Town;
                Village village = settlement.Village;

                string type =
                    isVillage ? "village" :
                    settlement.IsTown ? "town" :
                    isWalled ? "castle" :
                    "other";

                InspectorLog.Info(
                    "TERRITORY NEED MONITOR v" + Version +
                    " party=" + party.Name +
                    " clan=" + clan.Name +
                    " target=" + settlement.Name +
                    " targetId=" + settlement.StringId +
                    " type=" + type +
                    " vanillaGoToScore=" +
                    (hasVanillaScore
                        ? vanillaScore.ToString("0.######", CultureInfo.InvariantCulture)
                        : "<none>") +
                    " pressure=" +
                    pressure.ToString("0.######", CultureInfo.InvariantCulture) +
                    " underSiege=" + settlement.IsUnderSiege +
                    " underRaid=" + settlement.IsUnderRaid +
                    " raided=" + settlement.IsRaided +
                    " militia=" +
                    settlement.Militia.ToString("0.######", CultureInfo.InvariantCulture) +
                    " idealGarrison=" +
                    (isWalled && idealGarrison > 0f
                        ? idealGarrison.ToString("0.######", CultureInfo.InvariantCulture)
                        : "<n/a>") +
                    " actualGarrisonStrength=" +
                    (isWalled
                        ? actualGarrison.ToString("0.######", CultureInfo.InvariantCulture)
                        : "<n/a>") +
                    " garrisonRatio=" +
                    (isWalled && garrisonRatio >= 0f
                        ? garrisonRatio.ToString("0.######", CultureInfo.InvariantCulture)
                        : "<n/a>") +
                    " garrisonDeficit=" +
                    (isWalled
                        ? garrisonDeficit.ToString("0.######", CultureInfo.InvariantCulture)
                        : "<n/a>") +
                    " garrisonParty=" + (garrisonPartyId ?? "<none>") +
                    " foodChange=" + FloatOrNa(town, "FoodChange") +
                    " prosperity=" + FloatOrNa(town, "Prosperity") +
                    " prosperityChange=" + FloatOrNa(town, "ProsperityChange") +
                    " security=" + FloatOrNa(town, "Security") +
                    " securityChange=" + FloatOrNa(town, "SecurityChange") +
                    " loyalty=" + FloatOrNa(town, "Loyalty") +
                    " loyaltyChange=" + FloatOrNa(town, "LoyaltyChange") +
                    " townMilitiaChange=" + FloatOrNa(town, "MilitiaChange") +
                    " hearth=" + FloatOrNa(village, "Hearth") +
                    " hearthChange=" + FloatOrNa(village, "HearthChange") +
                    " villageMilitiaChange=" + FloatOrNa(village, "MilitiaChange"));
            }

            InspectorLog.Info(
                "TERRITORY NEED SUMMARY v" + Version +
                " party=" + party.Name +
                " clan=" + clan.Name +
                " holdings=" + holdings +
                " highestPressure=" +
                highestPressure.ToString("0.######", CultureInfo.InvariantCulture) +
                " highestPressureTarget=" +
                (highestPressureTarget ?? "<none>") +
                " formula=max(activeSiege,activeRaid,garrisonDeficit)" +
                " observerMutation=False phase=preMutation");
        }

        private static Dictionary<string, float> ReadGoToSettlementScores(
            PartyThinkParams think)
        {
            var result = new Dictionary<string, float>(StringComparer.Ordinal);

            object scoreList = ReadMember(think, "AIBehaviorScores");
            IEnumerable enumerable = scoreList as IEnumerable;
            if (enumerable == null) return result;

            foreach (object item in enumerable)
            {
                object data = ReadMember(item, "Item1");
                if (data == null) continue;

                string behavior = Text(ReadMember(data, "AiBehavior"));
                if (!string.Equals(
                    behavior, "GoToSettlement", StringComparison.Ordinal))
                    continue;

                Settlement settlement = ReadMember(data, "Party") as Settlement;
                if (settlement == null || string.IsNullOrEmpty(settlement.StringId))
                    continue;

                float score = ToFloat(ReadMember(item, "Item2"));
                result[settlement.StringId] = score;
            }

            return result;
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
                    null, new object[] { kingdom, clan });

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

        private static float Clamp01(float value)
        {
            if (value < 0f) return 0f;
            if (value > 1f) return 1f;
            return value;
        }

        private static string FloatOrNa(object instance, string member)
        {
            if (instance == null) return "<n/a>";

            object value = ReadMember(instance, member);
            if (value == null) return "<n/a>";

            return ToFloat(value).ToString(
                "0.######", CultureInfo.InvariantCulture);
        }

        private static object ReadMember(object instance, string name)
        {
            if (instance == null || string.IsNullOrEmpty(name)) return null;

            Type t = instance.GetType();

            FieldInfo f = t.GetField(
                name,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f != null) return f.GetValue(instance);

            PropertyInfo p = t.GetProperty(
                name,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
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
                return Convert.ToSingle(value, CultureInfo.InvariantCulture);
            }
            catch
            {
                return 0f;
            }
        }
    }
}
