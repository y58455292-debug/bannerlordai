using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace BannerlordInspector
{
    /// <summary>
    /// Experiment 03H - Garrison-Only Same-Think One-Shot Causal Capture.
    ///
    /// Isolates the graded garrison-deficit component of Clan Gravity.
    /// Eligible need candidates must NOT be under raid or siege and must have
    /// normalizedGarrisonDeficit > 0. Causal proof still happens inside one
    /// live PartyThinkParams: vanilla scores -> capture untouched winner ->
    /// detect natural flip opportunity -> optional one-shot score change ->
    /// vanilla normal selection/commit -> automatic final report.
    ///
    /// Dedicated marker:
    /// D:\BannerlordAIResearch\ENABLE_CAUSAL_FLIP_ONESHOT.txt
    ///
    /// The old broad marker must remain absent:
    /// D:\BannerlordAIResearch\ENABLE_CLAN_GRAVITY_LAB.txt
    ///
    /// 03H eligibility / formula:
    /// activeSiege = false
    /// activeRaid = false
    /// pressure = normalizedGarrisonDeficit (> 0)
    /// bonusFraction = min(0.25, pressure)
    /// newScore = vanillaScore * (1 + bonusFraction)
    /// </summary>
    public static class GlobalSameThinkCausalCapture
    {
        public const string Version = "0.5";
        public const string ExperimentMode = "GARRISON_ONLY_NO_RAID_NO_SIEGE";
        public const float MaxBonusFraction = 0.25f;

        public const string OneShotMarker =
            @"D:\BannerlordAIResearch\ENABLE_CAUSAL_FLIP_ONESHOT.txt";

        public const string LegacyMarker =
            @"D:\BannerlordAIResearch\ENABLE_CLAN_GRAVITY_LAB.txt";

        public const string ReportDirectory =
            @"D:\BannerlordAIResearch\Telemetry\BannerlordInspector\ai\causal";

        public static long ObservedPartyThinks;
        public static long EligibleIndependentLordThinks;
        public static long NeedPositiveThinks;
        public static long NeedPositiveCandidates;
        public static long FlipOpportunities;
        public static long InterventionsApplied;
        public static long CompletedReports;
        public static long MutationFailures;

        public static bool Consumed;
        public static bool PendingCommit;

        public static float ClosestGapFraction = -1f;
        public static string ClosestPartyId = null;
        public static string ClosestPartyName = null;
        public static string ClosestClanId = null;
        public static string ClosestClanName = null;
        public static string ClosestTargetId = null;
        public static string ClosestTargetName = null;
        public static int ClosestVanillaRank = -1;
        public static float ClosestRequiredBonusFraction = -1f;
        public static float ClosestAvailableBonusFraction = -1f;
        public static string ClosestTopVanillaBehavior = null;
        public static string ClosestTopVanillaTarget = null;

        public static string LastOpportunityPartyId = null;
        public static string LastOpportunityPartyName = null;
        public static string LastOpportunityClanId = null;
        public static string LastOpportunityClanName = null;
        public static string LastOpportunityTargetId = null;
        public static string LastOpportunityTargetName = null;
        public static int LastOpportunityVanillaRank = -1;
        public static float LastOpportunityVanillaScore = -1f;
        public static float LastOpportunityTopVanillaScore = -1f;
        public static string LastOpportunityTopVanillaBehavior = null;
        public static string LastOpportunityTopVanillaTarget = null;
        public static float LastOpportunityPressure = -1f;
        public static float LastOpportunityRequiredBonusFraction = -1f;
        public static float LastOpportunityAvailableBonusFraction = -1f;
        public static float LastOpportunityHypotheticalScore = -1f;

        public static string PendingPartyId = null;
        public static string PendingPartyName = null;
        public static string PendingTargetId = null;
        public static string PendingTargetName = null;

        public static string LastReportPath = null;
        public static string LastResult = null;

        private static readonly MethodInfo SetBehaviorScoreMethod =
            AccessTools.Method(typeof(PartyThinkParams), "SetBehaviorScore");

        private static readonly MethodInfo IdealGarrisonMethod =
            AccessTools.Method(
                AccessTools.TypeByName("Helpers.FactionHelper"),
                "FindIdealGarrisonStrengthPerWalledCenter");

        private static readonly Dictionary<string, MobileParty> GarrisonBySettlement =
            new Dictionary<string, MobileParty>(StringComparer.Ordinal);

        private static long _lastGarrisonRefreshObservedCount = -1000;
        private static PendingProof _pending;

        private sealed class Candidate
        {
            public object Data;
            public object Target;
            public Settlement Settlement;
            public string Behavior;
            public string TargetId;
            public string TargetName;
            public float Score;
            public int OriginalIndex;
        }

        private sealed class FlipCandidate
        {
            public Candidate Candidate;
            public float Pressure;
            public float AvailableBonusFraction;
            public float RequiredBonusFraction;
            public float HypotheticalScore;
            public int VanillaRank;
            public int HypotheticalRank;
            public float IdealGarrison;
            public float ActualGarrison;
            public float GarrisonDeficit;
            public bool UnderRaid;
            public bool UnderSiege;
        }

        private sealed class PendingProof
        {
            public string StartedLocalTime;
            public string PartyId;
            public string PartyName;
            public string HeroId;
            public string HeroName;
            public string ClanId;
            public string ClanName;

            public string VanillaTopBehavior;
            public string VanillaTopTargetId;
            public string VanillaTopTargetName;
            public float VanillaTopScore;

            public string TargetId;
            public string TargetName;
            public float TargetVanillaScore;
            public int TargetVanillaRank;
            public float Pressure;
            public float AvailableBonusFraction;
            public float RequiredBonusFraction;
            public float HypotheticalScore;
            public int HypotheticalRank;

            public bool UnderRaid;
            public bool UnderSiege;
            public float IdealGarrison;
            public float ActualGarrison;
            public float GarrisonDeficit;

            public float RequestedAppliedScore;
            public float AppliedScore;
            public float MutationReadbackScore;
            public bool MutationReadbackVerified;
            public string MutationReadbackMatchMode;
            public int MutationReadbackMatchCount;
            public string VanillaTopTen;

            public string ReportPath;
            public bool CommitCaptured;

            public string SelectedBehavior;
            public string SelectedTargetId;
            public string SelectedTargetName;
            public string AfterDefaultBehavior;
            public string AfterTargetSettlementId;
            public string AfterTargetSettlementName;
            public string AfterTargetPartyId;
            public string AfterTargetPartyName;
            public string Result;
            public string CaptureMethod;
            public bool FallbackFinalStateCaptured;
        }

        public static void Observe(MobileParty party, PartyThinkParams think)
        {
            ObservedPartyThinks++;

            if (party == null || think == null)
                return;

            Clan clan = party.ActualClan;
            if (clan == null || party.LeaderHero == null)
                return;

            Kingdom kingdom = ReadMember(clan, "Kingdom") as Kingdom;
            if (kingdom == null)
                return;

            if (Bool(ReadMember(party, "IsMainParty")))
                return;

            if (ReadMember(party, "Army") != null)
                return;

            if (ReadMember(party, "AttachedTo") != null)
                return;

            if (Bool(ReadMember(party, "InMapEvent")))
                return;

            if (Bool(ReadMember(party, "IsDisbanding")))
                return;

            EligibleIndependentLordThinks++;

            List<Candidate> candidates = ReadCandidates(think);
            if (candidates.Count == 0)
                return;

            Candidate vanillaTop = candidates
                .OrderByDescending(x => x.Score)
                .ThenBy(x => x.OriginalIndex)
                .FirstOrDefault();

            if (vanillaTop == null)
                return;

            float idealGarrison = ReadIdealGarrisonStrength(kingdom, clan);
            if (idealGarrison <= 0f)
                return;

            RefreshGarrisonCacheIfNeeded();

            bool partyHasNeed = false;
            FlipCandidate bestFlip = null;

            foreach (Candidate c in candidates)
            {
                if (!string.Equals(
                    c.Behavior,
                    "GoToSettlement",
                    StringComparison.Ordinal))
                    continue;

                Settlement settlement = c.Settlement;
                if (settlement == null || settlement.OwnerClan == null)
                    continue;

                if (!object.ReferenceEquals(settlement.OwnerClan, clan) &&
                    !string.Equals(
                        settlement.OwnerClan.StringId,
                        clan.StringId,
                        StringComparison.Ordinal))
                    continue;

                // Experiment 03H isolates garrison deficit only. Active raids
                // and sieges are intentionally excluded from eligibility.
                if (settlement.IsUnderRaid || settlement.IsUnderSiege)
                    continue;

                float actualGarrison;
                float garrisonDeficit;
                float pressure = CalculatePressure(
                    settlement,
                    idealGarrison,
                    out actualGarrison,
                    out garrisonDeficit);

                // With raid/siege excluded above, pressure must come only
                // from the normalized garrison deficit. Fail closed otherwise.
                if (garrisonDeficit <= 0f)
                    continue;

                pressure = garrisonDeficit;

                if (pressure <= 0f)
                    continue;

                partyHasNeed = true;
                NeedPositiveCandidates++;

                float availableBonus =
                    Math.Min(MaxBonusFraction, pressure);

                float hypothetical =
                    c.Score * (1f + availableBonus);

                int vanillaRank =
                    RankByScore(candidates, c.Score, c);

                int hypotheticalRank =
                    HypotheticalRank(
                        candidates,
                        c,
                        hypothetical);

                float requiredBonus =
                    c.Score <= 0f
                        ? float.PositiveInfinity
                        : Math.Max(
                            0f,
                            (vanillaTop.Score / c.Score) - 1f);

                if (vanillaRank > 1 &&
                    !float.IsPositiveInfinity(requiredBonus))
                {
                    float gap = requiredBonus - availableBonus;
                    if (gap < 0f)
                        gap = 0f;

                    if (ClosestGapFraction < 0f ||
                        gap < ClosestGapFraction)
                    {
                        ClosestGapFraction = gap;
                        ClosestPartyId = party.StringId;
                        ClosestPartyName = party.Name.ToString();
                        ClosestClanId = clan.StringId;
                        ClosestClanName = clan.Name.ToString();
                        ClosestTargetId = settlement.StringId;
                        ClosestTargetName = settlement.Name.ToString();
                        ClosestVanillaRank = vanillaRank;
                        ClosestRequiredBonusFraction = requiredBonus;
                        ClosestAvailableBonusFraction = availableBonus;
                        ClosestTopVanillaBehavior = vanillaTop.Behavior;
                        ClosestTopVanillaTarget = vanillaTop.TargetName;
                    }
                }

                bool wouldFlip =
                    vanillaRank > 1 &&
                    hypothetical > vanillaTop.Score;

                if (!wouldFlip)
                    continue;

                FlipOpportunities++;

                var flip = new FlipCandidate
                {
                    Candidate = c,
                    Pressure = pressure,
                    AvailableBonusFraction = availableBonus,
                    RequiredBonusFraction = requiredBonus,
                    HypotheticalScore = hypothetical,
                    VanillaRank = vanillaRank,
                    HypotheticalRank = hypotheticalRank,
                    IdealGarrison = idealGarrison,
                    ActualGarrison = actualGarrison,
                    GarrisonDeficit = garrisonDeficit,
                    UnderRaid = settlement.IsUnderRaid,
                    UnderSiege = settlement.IsUnderSiege
                };

                if (bestFlip == null ||
                    flip.HypotheticalScore > bestFlip.HypotheticalScore)
                    bestFlip = flip;
            }

            if (partyHasNeed)
                NeedPositiveThinks++;

            if (bestFlip == null)
                return;

            RecordOpportunity(party, clan, vanillaTop, bestFlip);

            if (Consumed || _pending != null)
                return;

            if (!File.Exists(OneShotMarker))
                return;

            if (File.Exists(LegacyMarker))
            {
                InspectorLog.Warn(
                    "GLOBAL SAME-THINK CAUSAL v" + Version +
                    " refused intervention because legacy marker exists: " +
                    LegacyMarker);
                return;
            }

            if (SetBehaviorScoreMethod == null)
            {
                InspectorLog.Warn(
                    "GLOBAL SAME-THINK CAUSAL v" + Version +
                    " cannot intervene: PartyThinkParams.SetBehaviorScore not found.");
                return;
            }

            try
            {
                File.Delete(OneShotMarker);
            }
            catch (Exception ex)
            {
                InspectorLog.Error(
                    "GLOBAL SAME-THINK CAUSAL v" + Version +
                    " refused intervention because one-shot marker could not be consumed.",
                    ex);
                return;
            }

            if (File.Exists(OneShotMarker))
            {
                InspectorLog.Warn(
                    "GLOBAL SAME-THINK CAUSAL v" + Version +
                    " refused intervention: one-shot marker still exists after delete attempt.");
                return;
            }

            Consumed = true;

            PendingProof proof = BuildPendingProof(
                party, clan, vanillaTop, bestFlip, candidates);

            try
            {
                proof.RequestedAppliedScore =
                    bestFlip.HypotheticalScore;

                SetBehaviorScoreMethod.Invoke(
                    think,
                    new object[]
                    {
                        bestFlip.Candidate.Data,
                        bestFlip.HypotheticalScore
                    });

                float readbackScore;
                string readbackMatchMode;
                int readbackMatchCount;

                bool readbackFound =
                    TryReadScoreForCandidateIdentity(
                        think,
                        bestFlip.Candidate.Data,
                        out readbackScore,
                        out readbackMatchMode,
                        out readbackMatchCount);

                proof.MutationReadbackScore =
                    readbackScore;

                proof.MutationReadbackMatchMode =
                    readbackMatchMode;

                proof.MutationReadbackMatchCount =
                    readbackMatchCount;

                proof.MutationReadbackVerified =
                    readbackFound &&
                    readbackMatchCount == 1 &&
                    Math.Abs(
                        readbackScore -
                        bestFlip.HypotheticalScore) <=
                    0.00001f;

                if (!proof.MutationReadbackVerified)
                {
                    MutationFailures++;
                    proof.Result =
                        "MUTATION_READBACK_FAILED";
                    proof.AppliedScore =
                        readbackScore;
                    proof.ReportPath =
                        MakeReportPath(
                            proof.PartyId,
                            "READBACK_FAILED");

                    WriteFinalReport(proof);
                    LastResult = proof.Result;
                    LastReportPath = proof.ReportPath;

                    InspectorLog.Warn(
                        "GLOBAL SAME-THINK CAUSAL v" + Version +
                        " mutation call returned but score readback did not verify. " +
                        "party=" + proof.PartyName +
                        " target=" + proof.TargetName +
                        " requestedScore=" +
                        F(proof.RequestedAppliedScore) +
                        " readbackFound=" +
                        readbackFound +
                        " readbackMatchMode=" +
                        (readbackMatchMode ?? "<null>") +
                        " readbackMatchCount=" +
                        readbackMatchCount +
                        " readbackScore=" +
                        F(readbackScore) +
                        " report=" +
                        proof.ReportPath);

                    return;
                }

                proof.AppliedScore =
                    readbackScore;

                _pending = proof;
                PendingCommit = true;
                PendingPartyId = proof.PartyId;
                PendingPartyName = proof.PartyName;
                PendingTargetId = proof.TargetId;
                PendingTargetName = proof.TargetName;
                InterventionsApplied++;

                WritePendingReport(proof);

                InspectorLog.Info(
                    "GLOBAL CAUSAL FLIP INTERVENTION v" + Version +
                    " mode=" + ExperimentMode +
                    " party=" + proof.PartyName +
                    " partyId=" + proof.PartyId +
                    " clan=" + proof.ClanName +
                    " clanId=" + proof.ClanId +
                    " target=" + proof.TargetName +
                    " targetId=" + proof.TargetId +
                    " vanillaScore=" + F(proof.TargetVanillaScore) +
                    " vanillaRank=" + proof.TargetVanillaRank +
                    " topVanillaBehavior=" + proof.VanillaTopBehavior +
                    " topVanillaTarget=" + proof.VanillaTopTargetName +
                    " topVanillaScore=" + F(proof.VanillaTopScore) +
                    " pressure=" + F(proof.Pressure) +
                    " availableBonusFraction=" + F(proof.AvailableBonusFraction) +
                    " requiredBonusFraction=" + F(proof.RequiredBonusFraction) +
                    " requestedScore=" + F(proof.RequestedAppliedScore) +
                    " readbackScore=" + F(proof.MutationReadbackScore) +
                    " readbackVerified=" + proof.MutationReadbackVerified +
                    " readbackMatchMode=" +
                    (proof.MutationReadbackMatchMode ?? "<null>") +
                    " readbackMatchCount=" +
                    proof.MutationReadbackMatchCount +
                    " newScore=" + F(proof.AppliedScore) +
                    " hypotheticalRank=" + proof.HypotheticalRank +
                    " markerConsumed=True");
            }
            catch (Exception ex)
            {
                MutationFailures++;
                proof.Result = "MUTATION_FAILED";
                proof.ReportPath = MakeReportPath(
                    proof.PartyId, "MUTATION_FAILED");
                WriteFinalReport(proof);
                LastResult = proof.Result;
                LastReportPath = proof.ReportPath;

                _pending = null;
                PendingCommit = false;
                PendingPartyId = null;
                PendingPartyName = null;
                PendingTargetId = null;
                PendingTargetName = null;

                InspectorLog.Error(
                    "GLOBAL SAME-THINK CAUSAL v" + Version +
                    " SetBehaviorScore invocation failed after marker consumption.",
                    ex);
            }
        }

        public static void ObserveCommit(object[] args)
        {
            PendingProof proof = _pending;
            if (proof == null || args == null || args.Length < 5)
                return;

            MobileParty owner = args[0] as MobileParty;
            if (owner == null ||
                !string.Equals(owner.StringId, proof.PartyId, StringComparison.Ordinal))
                return;

            object settlementArg = args[1];
            object partyArg = args[2];
            string detail = Text(args[4]);

            proof.SelectedBehavior = MapActionDetailToBehavior(detail);
            object selectedTarget = settlementArg != null ? settlementArg : partyArg;
            proof.SelectedTargetId = TargetId(selectedTarget);
            proof.SelectedTargetName = TargetName(selectedTarget);

            proof.AfterDefaultBehavior = Text(ReadMember(owner, "DefaultBehavior"));

            Settlement afterSettlement =
                ReadMember(owner, "TargetSettlement") as Settlement;

            MobileParty afterParty =
                ReadMember(owner, "TargetParty") as MobileParty;

            proof.AfterTargetSettlementId =
                afterSettlement == null ? null : afterSettlement.StringId;
            proof.AfterTargetSettlementName =
                afterSettlement == null ? null : afterSettlement.Name.ToString();

            proof.AfterTargetPartyId =
                afterParty == null ? null : afterParty.StringId;
            proof.AfterTargetPartyName =
                afterParty == null ? null : afterParty.Name.ToString();

            if (string.Equals(proof.SelectedBehavior, "GoToSettlement", StringComparison.Ordinal) &&
                string.Equals(proof.SelectedTargetId, proof.TargetId, StringComparison.Ordinal))
            {
                proof.Result = "FLIPPED_TO_NEEDY_CLAN_HOLDING";
            }
            else if (string.Equals(
                         proof.SelectedBehavior,
                         proof.VanillaTopBehavior,
                         StringComparison.Ordinal) &&
                     SameTarget(
                         proof.SelectedTargetId,
                         proof.SelectedTargetName,
                         proof.VanillaTopTargetId,
                         proof.VanillaTopTargetName))
            {
                proof.Result = "VANILLA_WINNER_REMAINED";
            }
            else
            {
                proof.Result = "SELECTED_OTHER_AFTER_INTERVENTION";
            }

            proof.CommitCaptured = true;
            proof.FallbackFinalStateCaptured = false;
            proof.CaptureMethod = "SetPartyAiAction.ApplyInternal postfix";
            WriteFinalReport(proof);

            CompletedReports++;
            LastResult = proof.Result;
            LastReportPath = proof.ReportPath;

            InspectorLog.Info(
                "GLOBAL CAUSAL FLIP COMMIT v" + Version +
                " result=" + proof.Result +
                " party=" + proof.PartyName +
                " partyId=" + proof.PartyId +
                " target=" + proof.TargetName +
                " targetId=" + proof.TargetId +
                " selectedBehavior=" + (proof.SelectedBehavior ?? "<null>") +
                " selectedTarget=" + (proof.SelectedTargetName ?? "<null>") +
                " selectedTargetId=" + (proof.SelectedTargetId ?? "<null>") +
                " report=" + (proof.ReportPath ?? "<null>"));

            ClearPending();
        }

        public static void ObservePartyThinkEnd(MobileParty party)
        {
            PendingProof proof = _pending;
            if (proof == null || party == null)
                return;

            if (!string.Equals(
                    party.StringId,
                    proof.PartyId,
                    StringComparison.Ordinal))
                return;

            // Some vanilla decisions do not pass through the exact
            // SetPartyAiAction.ApplyInternal boundary we instrumented (for example,
            // when vanilla can retain/reuse an already-established strategic
            // destination). The authoritative fallback is therefore the party's
            // strategic state at the END of this same PartyHourlyAiTick.
            //
            // This still gives us a same-think causal proof:
            // untouched pre-mutation winner + one-shot intervention + final
            // DefaultBehavior/TargetSettlement/TargetParty from the same think.

            proof.CommitCaptured = false;
            proof.FallbackFinalStateCaptured = true;
            proof.CaptureMethod = "PartyHourlyAiTick postfix final strategic state";

            proof.AfterDefaultBehavior =
                Text(ReadMember(party, "DefaultBehavior"));

            Settlement afterSettlement =
                ReadMember(party, "TargetSettlement") as Settlement;

            MobileParty afterParty =
                ReadMember(party, "TargetParty") as MobileParty;

            proof.AfterTargetSettlementId =
                afterSettlement == null
                    ? null
                    : afterSettlement.StringId;

            proof.AfterTargetSettlementName =
                afterSettlement == null
                    ? null
                    : afterSettlement.Name.ToString();

            proof.AfterTargetPartyId =
                afterParty == null
                    ? null
                    : afterParty.StringId;

            proof.AfterTargetPartyName =
                afterParty == null
                    ? null
                    : afterParty.Name.ToString();

            proof.SelectedBehavior =
                proof.AfterDefaultBehavior;

            if (afterSettlement != null)
            {
                proof.SelectedTargetId =
                    afterSettlement.StringId;

                proof.SelectedTargetName =
                    afterSettlement.Name.ToString();
            }
            else if (afterParty != null)
            {
                proof.SelectedTargetId =
                    afterParty.StringId;

                proof.SelectedTargetName =
                    afterParty.Name.ToString();
            }
            else
            {
                proof.SelectedTargetId = null;
                proof.SelectedTargetName = null;
            }

            if (string.Equals(
                    proof.AfterDefaultBehavior,
                    "GoToSettlement",
                    StringComparison.Ordinal) &&
                string.Equals(
                    proof.AfterTargetSettlementId,
                    proof.TargetId,
                    StringComparison.Ordinal))
            {
                proof.Result =
                    "FLIPPED_TO_NEEDY_CLAN_HOLDING";
            }
            else if (string.Equals(
                         proof.AfterDefaultBehavior,
                         proof.VanillaTopBehavior,
                         StringComparison.Ordinal) &&
                     SameTarget(
                         proof.SelectedTargetId,
                         proof.SelectedTargetName,
                         proof.VanillaTopTargetId,
                         proof.VanillaTopTargetName))
            {
                proof.Result =
                    "VANILLA_WINNER_REMAINED";
            }
            else
            {
                proof.Result =
                    "FINAL_STATE_OTHER_AFTER_INTERVENTION";
            }

            WriteFinalReport(proof);

            CompletedReports++;
            LastResult = proof.Result;
            LastReportPath = proof.ReportPath;

            InspectorLog.Info(
                "GLOBAL CAUSAL FLIP FINAL-STATE v" + Version +
                " result=" + proof.Result +
                " party=" + proof.PartyName +
                " partyId=" + proof.PartyId +
                " needyTarget=" + proof.TargetName +
                " needyTargetId=" + proof.TargetId +
                " finalDefaultBehavior=" +
                (proof.AfterDefaultBehavior ?? "<null>") +
                " finalTargetSettlement=" +
                (proof.AfterTargetSettlementName ?? "<null>") +
                " finalTargetSettlementId=" +
                (proof.AfterTargetSettlementId ?? "<null>") +
                " finalTargetParty=" +
                (proof.AfterTargetPartyName ?? "<null>") +
                " finalTargetPartyId=" +
                (proof.AfterTargetPartyId ?? "<null>") +
                " captureMethod=" + proof.CaptureMethod +
                " report=" + proof.ReportPath);

            ClearPending();
        }

        private static void RecordOpportunity(
            MobileParty party,
            Clan clan,
            Candidate vanillaTop,
            FlipCandidate flip)
        {
            LastOpportunityPartyId = party.StringId;
            LastOpportunityPartyName = party.Name.ToString();
            LastOpportunityClanId = clan.StringId;
            LastOpportunityClanName = clan.Name.ToString();
            LastOpportunityTargetId = flip.Candidate.Settlement.StringId;
            LastOpportunityTargetName = flip.Candidate.Settlement.Name.ToString();
            LastOpportunityVanillaRank = flip.VanillaRank;
            LastOpportunityVanillaScore = flip.Candidate.Score;
            LastOpportunityTopVanillaScore = vanillaTop.Score;
            LastOpportunityTopVanillaBehavior = vanillaTop.Behavior;
            LastOpportunityTopVanillaTarget = vanillaTop.TargetName;
            LastOpportunityPressure = flip.Pressure;
            LastOpportunityRequiredBonusFraction = flip.RequiredBonusFraction;
            LastOpportunityAvailableBonusFraction = flip.AvailableBonusFraction;
            LastOpportunityHypotheticalScore = flip.HypotheticalScore;

            InspectorLog.Info(
                "GLOBAL CAUSAL FLIP OPPORTUNITY v" + Version +
                " mode=" + ExperimentMode +
                " party=" + party.Name +
                " partyId=" + party.StringId +
                " clan=" + clan.Name +
                " clanId=" + clan.StringId +
                " target=" + flip.Candidate.Settlement.Name +
                " targetId=" + flip.Candidate.Settlement.StringId +
                " vanillaScore=" + F(flip.Candidate.Score) +
                " vanillaRank=" + flip.VanillaRank +
                " pressure=" + F(flip.Pressure) +
                " availableBonusFraction=" + F(flip.AvailableBonusFraction) +
                " requiredBonusFraction=" + F(flip.RequiredBonusFraction) +
                " hypotheticalScore=" + F(flip.HypotheticalScore) +
                " hypotheticalRank=" + flip.HypotheticalRank +
                " topVanillaBehavior=" + vanillaTop.Behavior +
                " topVanillaTarget=" + vanillaTop.TargetName +
                " topVanillaScore=" + F(vanillaTop.Score) +
                " armed=" + File.Exists(OneShotMarker) +
                " consumed=" + Consumed +
                " mutation=FalseUntilOneShot");
        }

        private static PendingProof BuildPendingProof(
            MobileParty party,
            Clan clan,
            Candidate vanillaTop,
            FlipCandidate flip,
            List<Candidate> candidates)
        {
            return new PendingProof
            {
                StartedLocalTime =
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture),

                PartyId = party.StringId,
                PartyName = party.Name.ToString(),

                HeroId =
                    party.LeaderHero == null ? null : party.LeaderHero.StringId,

                HeroName =
                    party.LeaderHero == null ? null : party.LeaderHero.Name.ToString(),

                ClanId = clan.StringId,
                ClanName = clan.Name.ToString(),

                VanillaTopBehavior = vanillaTop.Behavior,
                VanillaTopTargetId = vanillaTop.TargetId,
                VanillaTopTargetName = vanillaTop.TargetName,
                VanillaTopScore = vanillaTop.Score,

                TargetId = flip.Candidate.Settlement.StringId,
                TargetName = flip.Candidate.Settlement.Name.ToString(),
                TargetVanillaScore = flip.Candidate.Score,
                TargetVanillaRank = flip.VanillaRank,
                Pressure = flip.Pressure,
                AvailableBonusFraction = flip.AvailableBonusFraction,
                RequiredBonusFraction = flip.RequiredBonusFraction,
                HypotheticalScore = flip.HypotheticalScore,
                HypotheticalRank = flip.HypotheticalRank,

                UnderRaid = flip.UnderRaid,
                UnderSiege = flip.UnderSiege,
                IdealGarrison = flip.IdealGarrison,
                ActualGarrison = flip.ActualGarrison,
                GarrisonDeficit = flip.GarrisonDeficit,

                VanillaTopTen = DescribeTopCandidates(candidates),

                ReportPath = MakeReportPath(party.StringId, "PENDING")
            };
        }

        private static void WritePendingReport(PendingProof proof)
        {
            if (proof == null)
                return;

            proof.Result = "INTERVENTION_APPLIED_WAITING_FOR_COMMIT";
            WriteReport(proof, false);
            LastReportPath = proof.ReportPath;
            LastResult = proof.Result;
        }

        private static void WriteFinalReport(PendingProof proof)
        {
            if (proof == null)
                return;

            WriteReport(proof, true);
        }

        private static void WriteReport(PendingProof proof, bool final)
        {
            try
            {
                Directory.CreateDirectory(ReportDirectory);

                var sb = new StringBuilder(8192);

                sb.AppendLine("GLOBAL SAME-THINK CAUSAL CAPTURE v" + Version);
                sb.AppendLine("ExperimentMode: " + ExperimentMode);
                sb.AppendLine("STATUS: " + (proof.Result ?? "<null>"));
                sb.AppendLine("FINAL: " + final);
                sb.AppendLine("StartedLocalTime: " + (proof.StartedLocalTime ?? "<null>"));
                sb.AppendLine();

                sb.AppendLine("PARTY / CLAN");
                sb.AppendLine("Party: " + (proof.PartyName ?? "<null>"));
                sb.AppendLine("PartyId: " + (proof.PartyId ?? "<null>"));
                sb.AppendLine("Hero: " + (proof.HeroName ?? "<null>"));
                sb.AppendLine("HeroId: " + (proof.HeroId ?? "<null>"));
                sb.AppendLine("Clan: " + (proof.ClanName ?? "<null>"));
                sb.AppendLine("ClanId: " + (proof.ClanId ?? "<null>"));
                sb.AppendLine();

                sb.AppendLine("PRE-MUTATION VANILLA");
                sb.AppendLine("VanillaWinnerBehavior: " + (proof.VanillaTopBehavior ?? "<null>"));
                sb.AppendLine("VanillaWinnerTarget: " + (proof.VanillaTopTargetName ?? "<null>"));
                sb.AppendLine("VanillaWinnerTargetId: " + (proof.VanillaTopTargetId ?? "<null>"));
                sb.AppendLine("VanillaWinnerScore: " + F(proof.VanillaTopScore));
                sb.AppendLine("NeedTarget: " + (proof.TargetName ?? "<null>"));
                sb.AppendLine("NeedTargetId: " + (proof.TargetId ?? "<null>"));
                sb.AppendLine("NeedTargetVanillaScore: " + F(proof.TargetVanillaScore));
                sb.AppendLine("NeedTargetVanillaRank: " + proof.TargetVanillaRank);
                sb.AppendLine();

                sb.AppendLine("LIVE TERRITORY NEED");
                sb.AppendLine("UnderRaid: " + proof.UnderRaid);
                sb.AppendLine("UnderSiege: " + proof.UnderSiege);
                sb.AppendLine("IdealGarrisonStrength: " + F(proof.IdealGarrison));
                sb.AppendLine("ActualGarrisonStrength: " + F(proof.ActualGarrison));
                sb.AppendLine("GarrisonDeficit: " + F(proof.GarrisonDeficit));
                sb.AppendLine("Pressure: " + F(proof.Pressure));
                sb.AppendLine();

                sb.AppendLine("INTERVENTION");
                sb.AppendLine("RequiredBonusFraction: " + F(proof.RequiredBonusFraction));
                sb.AppendLine("AvailableBonusFraction: " + F(proof.AvailableBonusFraction));
                sb.AppendLine("Multiplier: " + F(1f + proof.AvailableBonusFraction));
                sb.AppendLine("RequestedAppliedScore: " + F(proof.RequestedAppliedScore));
                sb.AppendLine("MutationReadbackScore: " + F(proof.MutationReadbackScore));
                sb.AppendLine("MutationReadbackVerified: " + proof.MutationReadbackVerified);
                sb.AppendLine("MutationReadbackMatchMode: " + (proof.MutationReadbackMatchMode ?? "<null>"));
                sb.AppendLine("MutationReadbackMatchCount: " + proof.MutationReadbackMatchCount);
                sb.AppendLine("AppliedScore: " + F(proof.AppliedScore));
                sb.AppendLine("PredictedHypotheticalScore: " + F(proof.HypotheticalScore));
                sb.AppendLine("PredictedHypotheticalRank: " + proof.HypotheticalRank);
                sb.AppendLine("OneShotMarkerConsumedBeforeMutation: True");
                sb.AppendLine();

                sb.AppendLine("POST-MUTATION VANILLA RESULT");
                sb.AppendLine("CaptureMethod: " + (proof.CaptureMethod ?? "<null>"));
                sb.AppendLine("CommitCaptured: " + proof.CommitCaptured);
                sb.AppendLine("FallbackFinalStateCaptured: " + proof.FallbackFinalStateCaptured);
                sb.AppendLine("SelectedBehavior: " + (proof.SelectedBehavior ?? "<null>"));
                sb.AppendLine("SelectedTarget: " + (proof.SelectedTargetName ?? "<null>"));
                sb.AppendLine("SelectedTargetId: " + (proof.SelectedTargetId ?? "<null>"));
                sb.AppendLine("AfterDefaultBehavior: " + (proof.AfterDefaultBehavior ?? "<null>"));
                sb.AppendLine("AfterTargetSettlement: " + (proof.AfterTargetSettlementName ?? "<null>"));
                sb.AppendLine("AfterTargetSettlementId: " + (proof.AfterTargetSettlementId ?? "<null>"));
                sb.AppendLine("AfterTargetParty: " + (proof.AfterTargetPartyName ?? "<null>"));
                sb.AppendLine("AfterTargetPartyId: " + (proof.AfterTargetPartyId ?? "<null>"));
                sb.AppendLine();

                sb.AppendLine("RESULT");
                sb.AppendLine(proof.Result ?? "<null>");
                sb.AppendLine();

                sb.AppendLine("TOP VANILLA CANDIDATES BEFORE MUTATION");
                sb.Append(proof.VanillaTopTen ?? "<none>");

                File.WriteAllText(proof.ReportPath, sb.ToString(), Encoding.UTF8);
            }
            catch (Exception ex)
            {
                InspectorLog.Error(
                    "GLOBAL SAME-THINK CAUSAL v" + Version +
                    " could not write automatic report.",
                    ex);
            }
        }

        private static string MakeReportPath(string partyId, string status)
        {
            string stamp =
                DateTime.Now.ToString("yyyyMMdd_HHmmss_fff", CultureInfo.InvariantCulture);

            return Path.Combine(
                ReportDirectory,
                "CausalFlip_" +
                stamp + "_" +
                SafeFilePart(partyId ?? "unknown_party") + "_" +
                SafeFilePart(status ?? "UNKNOWN") +
                ".txt");
        }

        private static string SafeFilePart(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "unknown";

            var sb = new StringBuilder(value.Length);

            foreach (char c in value)
            {
                if (char.IsLetterOrDigit(c) || c == '-' || c == '_')
                    sb.Append(c);
                else
                    sb.Append('_');
            }

            return sb.ToString();
        }

        private static string DescribeTopCandidates(List<Candidate> candidates)
        {
            if (candidates == null || candidates.Count == 0)
                return "<none>";

            List<Candidate> ordered =
                candidates
                    .OrderByDescending(x => x.Score)
                    .ThenBy(x => x.OriginalIndex)
                    .Take(10)
                    .ToList();

            var sb = new StringBuilder(2048);

            for (int i = 0; i < ordered.Count; i++)
            {
                Candidate c = ordered[i];

                sb.Append("#");
                sb.Append(i + 1);
                sb.Append(" score=");
                sb.Append(F(c.Score));
                sb.Append(" behavior=");
                sb.Append(c.Behavior ?? "<null>");
                sb.Append(" target=");
                sb.Append(c.TargetName ?? "<null>");
                sb.Append(" targetId=");
                sb.Append(c.TargetId ?? "<null>");
                sb.AppendLine();
            }

            return sb.ToString();
        }

        private static List<Candidate> ReadCandidates(PartyThinkParams think)
        {
            var result = new List<Candidate>();

            object scoreList = ReadMember(think, "AIBehaviorScores");
            IEnumerable enumerable = scoreList as IEnumerable;
            if (enumerable == null)
                return result;

            int index = 0;

            foreach (object item in enumerable)
            {
                object data = ReadMember(item, "Item1");

                if (data == null)
                {
                    index++;
                    continue;
                }

                object target = ReadMember(data, "Party");

                result.Add(new Candidate
                {
                    Data = data,
                    Target = target,
                    Settlement = target as Settlement,
                    Behavior = Text(ReadMember(data, "AiBehavior")),
                    TargetId = TargetId(target),
                    TargetName = TargetName(target),
                    Score = ToFloat(ReadMember(item, "Item2")),
                    OriginalIndex = index
                });

                index++;
            }

            return result;
        }

        private static int RankByScore(
            List<Candidate> candidates,
            float score,
            Candidate self)
        {
            int higher = 0;

            foreach (Candidate c in candidates)
            {
                if (object.ReferenceEquals(c, self))
                    continue;

                if (c.Score > score)
                    higher++;
            }

            return higher + 1;
        }

        private static int HypotheticalRank(
            List<Candidate> candidates,
            Candidate self,
            float hypotheticalScore)
        {
            int higher = 0;

            foreach (Candidate c in candidates)
            {
                if (object.ReferenceEquals(c, self))
                    continue;

                if (c.Score > hypotheticalScore)
                    higher++;
            }

            return higher + 1;
        }

        private static bool TryReadScoreForCandidateIdentity(
            PartyThinkParams think,
            object targetData,
            out float score,
            out string matchMode,
            out int matchCount)
        {
            score = 0f;
            matchMode = null;
            matchCount = 0;

            if (think == null || targetData == null)
                return false;

            object scoreList =
                ReadMember(think, "AIBehaviorScores");

            IEnumerable enumerable =
                scoreList as IEnumerable;

            if (enumerable == null)
                return false;

            // Stage 1: exact reference match. This is cheapest and strongest
            // when Bannerlord preserves the original AIBehaviorData instance.
            foreach (object item in enumerable)
            {
                object data =
                    ReadMember(item, "Item1");

                if (!object.ReferenceEquals(
                        data,
                        targetData))
                    continue;

                score =
                    ToFloat(
                        ReadMember(
                            item,
                            "Item2"));

                matchMode = "Reference";
                matchCount = 1;
                return true;
            }

            // SetBehaviorScore may replace/rebuild the score-list entry, so the
            // original AIBehaviorData reference can disappear even when the score
            // change succeeded. For our settlement-targeted experiment, verify
            // semantic identity instead:
            //   behavior + target StringId + navigation flags.
            string expectedBehavior =
                Text(ReadMember(
                    targetData,
                    "AiBehavior"));

            object expectedTarget =
                ReadMember(
                    targetData,
                    "Party");

            string expectedTargetId =
                TargetId(expectedTarget);

            string expectedNavigation =
                Text(ReadMember(
                    targetData,
                    "NavigationType"));

            bool expectedFromPort =
                Bool(ReadMember(
                    targetData,
                    "IsFromPort"));

            bool expectedTargetingPort =
                Bool(ReadMember(
                    targetData,
                    "IsTargetingPort"));

            bool expectedGatherArmy =
                Bool(ReadMember(
                    targetData,
                    "WillGatherArmy"));

            if (string.IsNullOrEmpty(expectedBehavior) ||
                string.IsNullOrEmpty(expectedTargetId))
                return false;

            float matchedScore = 0f;

            foreach (object item in enumerable)
            {
                object data =
                    ReadMember(item, "Item1");

                if (data == null)
                    continue;

                string behavior =
                    Text(ReadMember(
                        data,
                        "AiBehavior"));

                if (!string.Equals(
                        behavior,
                        expectedBehavior,
                        StringComparison.Ordinal))
                    continue;

                object target =
                    ReadMember(
                        data,
                        "Party");

                string targetId =
                    TargetId(target);

                if (!string.Equals(
                        targetId,
                        expectedTargetId,
                        StringComparison.Ordinal))
                    continue;

                string navigation =
                    Text(ReadMember(
                        data,
                        "NavigationType"));

                bool fromPort =
                    Bool(ReadMember(
                        data,
                        "IsFromPort"));

                bool targetingPort =
                    Bool(ReadMember(
                        data,
                        "IsTargetingPort"));

                bool gatherArmy =
                    Bool(ReadMember(
                        data,
                        "WillGatherArmy"));

                if (!string.Equals(
                        navigation,
                        expectedNavigation,
                        StringComparison.Ordinal))
                    continue;

                if (fromPort != expectedFromPort ||
                    targetingPort != expectedTargetingPort ||
                    gatherArmy != expectedGatherArmy)
                    continue;

                matchCount++;

                if (matchCount == 1)
                {
                    matchedScore =
                        ToFloat(
                            ReadMember(
                                item,
                                "Item2"));
                }
            }

            if (matchCount == 1)
            {
                score = matchedScore;
                matchMode = "SemanticBehaviorTargetNavigation";
                return true;
            }

            if (matchCount > 1)
            {
                matchMode = "SemanticAmbiguous";
                return false;
            }

            matchMode = "NoMatch";
            return false;
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
                {
                    actualGarrison = garrison.Party.EstimatedStrength;
                    garrisonDeficit =
                        Clamp01(
                            (idealGarrison - actualGarrison) /
                            idealGarrison);
                }
            }

            float siegePressure =
                settlement.IsUnderSiege ? 1f : 0f;

            float raidPressure =
                settlement.IsUnderRaid ? 1f : 0f;

            return Math.Max(
                Math.Max(siegePressure, raidPressure),
                garrisonDeficit);
        }

        private static float ReadIdealGarrisonStrength(
            Kingdom kingdom,
            Clan clan)
        {
            if (kingdom == null || clan == null || IdealGarrisonMethod == null)
                return -1f;

            try
            {
                object value =
                    IdealGarrisonMethod.Invoke(
                        null,
                        new object[] { kingdom, clan });

                return ToFloat(value);
            }
            catch
            {
                return -1f;
            }
        }

        private static void RefreshGarrisonCacheIfNeeded()
        {
            if (ObservedPartyThinks - _lastGarrisonRefreshObservedCount < 50 &&
                GarrisonBySettlement.Count > 0)
                return;

            GarrisonBySettlement.Clear();

            try
            {
                foreach (MobileParty garrison in MobileParty.AllGarrisonParties)
                {
                    if (garrison == null ||
                        garrison.GarrisonPartyComponent == null)
                        continue;

                    Settlement settlement =
                        garrison.GarrisonPartyComponent.Settlement;

                    if (settlement == null ||
                        string.IsNullOrEmpty(settlement.StringId))
                        continue;

                    GarrisonBySettlement[settlement.StringId] = garrison;
                }
            }
            catch
            {
                GarrisonBySettlement.Clear();
            }

            _lastGarrisonRefreshObservedCount = ObservedPartyThinks;
        }

        private static MobileParty FindGarrison(Settlement settlement)
        {
            if (settlement == null ||
                string.IsNullOrEmpty(settlement.StringId))
                return null;

            MobileParty garrison;

            if (GarrisonBySettlement.TryGetValue(
                    settlement.StringId,
                    out garrison))
                return garrison;

            try
            {
                foreach (MobileParty party in MobileParty.AllGarrisonParties)
                {
                    if (party == null ||
                        party.GarrisonPartyComponent == null)
                        continue;

                    Settlement home =
                        party.GarrisonPartyComponent.Settlement;

                    if (home != null &&
                        string.Equals(
                            home.StringId,
                            settlement.StringId,
                            StringComparison.Ordinal))
                    {
                        GarrisonBySettlement[settlement.StringId] = party;
                        return party;
                    }
                }
            }
            catch
            {
            }

            return null;
        }

        private static string TargetId(object target)
        {
            Settlement settlement = target as Settlement;
            if (settlement != null)
                return settlement.StringId;

            MobileParty party = target as MobileParty;
            if (party != null)
                return party.StringId;

            return null;
        }

        private static string TargetName(object target)
        {
            if (target == null)
                return "<null>";

            Settlement settlement = target as Settlement;
            if (settlement != null)
                return settlement.Name.ToString();

            MobileParty party = target as MobileParty;
            if (party != null)
                return party.Name.ToString();

            return target.ToString();
        }

        private static string MapActionDetailToBehavior(string detail)
        {
            switch (detail)
            {
                case "GoToSettlement": return "GoToSettlement";
                case "PatrolAroundSettlement":
                case "PatrolAroundPoint": return "PatrolAroundPoint";
                case "RaidSettlement": return "RaidSettlement";
                case "BesiegeSettlement": return "BesiegeSettlement";
                case "EngageParty": return "EngageParty";
                case "GoAroundParty": return "GoAroundParty";
                case "EscortParty": return "EscortParty";
                case "MoveToNearestLand": return "MoveToNearestLandOrPort";
                case "DefendParty": return "DefendSettlement";
                default: return detail;
            }
        }

        private static bool SameTarget(
            string selectedId,
            string selectedName,
            string expectedId,
            string expectedName)
        {
            if (!string.IsNullOrEmpty(selectedId) &&
                !string.IsNullOrEmpty(expectedId))
            {
                return string.Equals(
                    selectedId,
                    expectedId,
                    StringComparison.Ordinal);
            }

            return string.Equals(
                selectedName,
                expectedName,
                StringComparison.Ordinal);
        }

        private static bool Bool(object value)
        {
            if (value == null)
                return false;

            try
            {
                return Convert.ToBoolean(value, CultureInfo.InvariantCulture);
            }
            catch
            {
                return false;
            }
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

            FieldInfo f =
                t.GetField(
                    name,
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic);

            if (f != null)
                return f.GetValue(instance);

            PropertyInfo p =
                t.GetProperty(
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
            if (value == null)
                return 0f;

            try
            {
                return Convert.ToSingle(value, CultureInfo.InvariantCulture);
            }
            catch
            {
                return 0f;
            }
        }

        private static string F(float value)
        {
            return value.ToString("0.######", CultureInfo.InvariantCulture);
        }

        private static void ClearPending()
        {
            _pending = null;
            PendingCommit = false;
            PendingPartyId = null;
            PendingPartyName = null;
            PendingTargetId = null;
            PendingTargetName = null;
        }
    }
}

