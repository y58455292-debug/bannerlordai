using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace BannerlordInspector
{
    /// <summary>
    /// Clan Gravity - Experiment 03G: Natural Causal Flip Proof.
    ///
    /// PURPOSE
    /// Preserve vanilla candidate generation and selection, but give clan-owned
    /// GoToSettlement candidates a small, explainable bonus ONLY when that
    /// specific holding has real territorial need.
    ///
    /// TerritoryNeedPressure:
    ///   max(activeSiege, activeRaid, normalizedGarrisonDeficit)
    ///
    /// Score rule:
    ///   bonusFraction = min(0.25, TerritoryNeedPressure)
    ///   multiplier    = 1 + bonusFraction
    ///   newScore      = vanillaScore * multiplier
    ///
    /// Examples:
    ///   healthy pressure 0.000 -> x1.000 (no change)
    ///   3.35% garrison deficit -> x1.0335
    ///   10% deficit            -> x1.10
    ///   active raid/siege      -> x1.25 maximum
    ///
    /// SCOPE / SAFETY
    /// - one lab party only: Arytha (lord_1_62_1_party_1)
    /// - one clan only: Sorados (clan_empire_west_6)
    /// - one settlement only: Gersegos Castle (castle_EW8)
    /// - one behavior only: GoToSettlement
    /// - only settlements currently owned by the party's ActualClan
    /// - requires explicit marker:
    ///   D:\BannerlordAIResearch\ENABLE_CLAN_GRAVITY_LAB.txt
    /// - vanilla still selects and commits the final action.
    /// </summary>
    public static class ClanGravityExperiment
    {
        public const string Version = "0.7";
        public const string TargetPartyId = "lord_1_62_1_party_1";
        public const string TargetClanId = "clan_empire_west_6";
        public const string TargetSettlementId = "castle_EW8"; // Gersegos Castle
        public const float MaxBonusFraction = 0.25f;

        public const string EnableMarker =
            @"D:\BannerlordAIResearch\ENABLE_CLAN_GRAVITY_LAB.txt";

        private static readonly MethodInfo SetBehaviorScoreMethod =
            AccessTools.Method(typeof(PartyThinkParams), "SetBehaviorScore");

        private static readonly MethodInfo IdealGarrisonMethod =
            AccessTools.Method(
                AccessTools.TypeByName("Helpers.FactionHelper"),
                "FindIdealGarrisonStrengthPerWalledCenter");

        private static bool _loggedDisabled;
        private static bool _loggedGuardFailure;

        private sealed class Match
        {
            public object Data;
            public Settlement Settlement;
            public float OldScore;
            public float Pressure;
            public float GarrisonDeficit;
            public float IdealGarrison;
            public float ActualGarrison;
            public bool UnderRaid;
            public bool UnderSiege;
        }

        public static void ApplyNeedSensitiveTerritoryProof(
            MobileParty party,
            PartyThinkParams think)
        {
            if (party == null || think == null) return;
            if (!string.Equals(
                party.StringId,
                TargetPartyId,
                StringComparison.Ordinal))
                return;

            if (!File.Exists(EnableMarker))
            {
                if (!_loggedDisabled)
                {
                    _loggedDisabled = true;
                    InspectorLog.Info(
                        "CLAN GRAVITY NEED v" + Version +
                        " disabled: lab marker not present at " + EnableMarker);
                }
                return;
            }

            Clan clan = party.ActualClan;

            // Exact internal-ID guard for this controlled experiment.
            if (clan == null ||
                !string.Equals(
                    clan.StringId,
                    TargetClanId,
                    StringComparison.Ordinal))
            {
                if (!_loggedGuardFailure)
                {
                    _loggedGuardFailure = true;
                    InspectorLog.Warn(
                        "CLAN GRAVITY NEED v" + Version +
                        " guard stopped experiment: Arytha/Sorados baseline no longer matches.");
                }
                return;
            }

            if (SetBehaviorScoreMethod == null)
            {
                InspectorLog.Warn(
                    "CLAN GRAVITY NEED v" + Version +
                    " cannot run: PartyThinkParams.SetBehaviorScore not found.");
                return;
            }

            float idealGarrison = ReadIdealGarrisonStrength(clan);

            object scoreList = ReadMember(think, "AIBehaviorScores");
            IEnumerable enumerable = scoreList as IEnumerable;
            if (enumerable == null) return;

            // Collect first, mutate second so the list is never changed
            // while its enumerator is active.
            var matches = new List<Match>();
            int clanTerritoryCandidates = 0;

            foreach (object item in enumerable)
            {
                object data = ReadMember(item, "Item1");
                if (data == null) continue;

                string behavior = Text(ReadMember(data, "AiBehavior"));
                if (!string.Equals(
                    behavior,
                    "GoToSettlement",
                    StringComparison.Ordinal))
                    continue;

                Settlement settlement = ReadMember(data, "Party") as Settlement;
                if (settlement == null || settlement.OwnerClan == null)
                    continue;

                if (!object.ReferenceEquals(settlement.OwnerClan, clan) &&
                    !string.Equals(
                        settlement.OwnerClan.StringId,
                        clan.StringId,
                        StringComparison.Ordinal))
                    continue;

                // 03G is intentionally one-settlement / one-proof.
                if (!string.Equals(
                    settlement.StringId,
                    TargetSettlementId,
                    StringComparison.Ordinal))
                    continue;

                clanTerritoryCandidates++;

                float actualGarrison;
                float garrisonDeficit;
                float pressure = CalculatePressure(
                    settlement,
                    idealGarrison,
                    out actualGarrison,
                    out garrisonDeficit);

                // Healthy territory receives exactly zero modification.
                if (pressure <= 0f)
                    continue;

                matches.Add(new Match
                {
                    Data = data,
                    Settlement = settlement,
                    OldScore = ToFloat(ReadMember(item, "Item2")),
                    Pressure = pressure,
                    GarrisonDeficit = garrisonDeficit,
                    IdealGarrison = idealGarrison,
                    ActualGarrison = actualGarrison,
                    UnderRaid = settlement.IsUnderRaid,
                    UnderSiege = settlement.IsUnderSiege
                });
            }

            int changed = 0;

            foreach (Match match in matches)
            {
                float bonusFraction = Math.Min(
                    MaxBonusFraction,
                    match.Pressure);

                float multiplier = 1f + bonusFraction;
                float newScore = match.OldScore * multiplier;

                try
                {
                    SetBehaviorScoreMethod.Invoke(
                        think,
                        new object[] { match.Data, newScore });

                    changed++;

                    InspectorLog.Info(
                        "CLAN GRAVITY NEED v" + Version +
                        " party=" + party.Name +
                        " clan=" + clan.Name +
                        " target=" + match.Settlement.Name +
                        " targetId=" + match.Settlement.StringId +
                        " behavior=GoToSettlement" +
                        " oldScore=" +
                        match.OldScore.ToString(
                            "0.######",
                            CultureInfo.InvariantCulture) +
                        " pressure=" +
                        match.Pressure.ToString(
                            "0.######",
                            CultureInfo.InvariantCulture) +
                        " bonusFraction=" +
                        bonusFraction.ToString(
                            "0.######",
                            CultureInfo.InvariantCulture) +
                        " multiplier=" +
                        multiplier.ToString(
                            "0.######",
                            CultureInfo.InvariantCulture) +
                        " newScore=" +
                        newScore.ToString(
                            "0.######",
                            CultureInfo.InvariantCulture) +
                        " underRaid=" + match.UnderRaid +
                        " underSiege=" + match.UnderSiege +
                        " idealGarrison=" +
                        match.IdealGarrison.ToString(
                            "0.######",
                            CultureInfo.InvariantCulture) +
                        " actualGarrisonStrength=" +
                        match.ActualGarrison.ToString(
                            "0.######",
                            CultureInfo.InvariantCulture) +
                        " garrisonDeficit=" +
                        match.GarrisonDeficit.ToString(
                            "0.######",
                            CultureInfo.InvariantCulture));
                }
                catch (Exception ex)
                {
                    InspectorLog.Error(
                        "CLAN GRAVITY NEED v" + Version +
                        " failed while applying need-sensitive territory score.",
                        ex);
                }
            }

            InspectorLog.Info(
                "CLAN GRAVITY NEED SUMMARY v" + Version +
                " party=" + party.Name +
                " clan=" + clan.Name +
                " clanTerritoryCandidates=" + clanTerritoryCandidates +
                " needPositive=" + matches.Count +
                " changed=" + changed +
                " formula=multiplier(1+min(0.25,pressure))" +
                " markerArmed=True");
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
                    null,
                    new object[] { kingdom, clan });

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
            if (value == null) return 0f;

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
    }
}
