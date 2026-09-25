using System;
using System.Globalization;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace ClanAI
{
    // Phase 5 runtime characterization only. Observation and logging only:
    // no party creation/destruction, movement, target, settlement, or save mutation.
    internal static class LocalBanditControlRuntimeTelemetry
    {
        private static readonly object EventOwner =
            new object();

        private static bool _installed;
        private static bool _loggedTown;
        private static bool _loggedVillage;
        private static bool _loggedSecure;
        private static bool _loggedWeak;
        private static bool _loggedNeutral;
        private static bool _loggedHideout;
        private static bool _loggedOther;
        private static bool _loggedMissingSecurity;
        private static bool _loggedNativeLooter;

        internal static void Install()
        {
            if (_installed)
                return;

            CampaignEvents.OnSessionLaunchedEvent
                .AddNonSerializedListener(
                    EventOwner,
                    new Action<CampaignGameStarter>(
                        OnSessionLaunched));

            CampaignEvents.MobilePartyCreated
                .AddNonSerializedListener(
                    EventOwner,
                    new Action<MobileParty>(
                        OnMobilePartyCreated));

            _installed = true;

            ClanAIPostVanilla.WriteExternalLog(
                "LOCAL_BANDIT_CONTROL_TELEMETRY_INSTALLED" +
                " source=phase5-runtime-observation" +
                " mutation=False");
        }

        private static void OnSessionLaunched(
            CampaignGameStarter starter)
        {
            _loggedTown = false;
            _loggedVillage = false;
            _loggedSecure = false;
            _loggedWeak = false;
            _loggedNeutral = false;
            _loggedHideout = false;
            _loggedOther = false;
            _loggedMissingSecurity = false;
            _loggedNativeLooter = false;

            ClanAIPostVanilla.WriteExternalLog(
                "LOCAL_BANDIT_CONTROL_TELEMETRY_READY" +
                " campaignHour=" +
                D(CampaignTime.Now.ToHours) +
                " mutation=False");
        }

        internal static void ObserveEvaluation(
            Settlement settlement,
            Settlement boundSettlement,
            LocalBanditCandidateKind policyKind,
            bool hasSecurity,
            float security,
            float nativeWeight,
            float controlMultiplier,
            float finalWeight,
            bool applied,
            string reason)
        {
            try
            {
                ObserveEvaluationUnsafe(
                    settlement,
                    boundSettlement,
                    policyKind,
                    hasSecurity,
                    security,
                    nativeWeight,
                    controlMultiplier,
                    finalWeight,
                    applied,
                    reason);
            }
            catch (Exception ex)
            {
                ClanAIPostVanilla.WriteExternalLog(
                    "LOCAL_BANDIT_CONTROL_TELEMETRY_ERROR" +
                    " stage=evaluation" +
                    " type=" + ex.GetType().Name +
                    " mutation=False");
            }
        }

        private static void ObserveEvaluationUnsafe(
            Settlement settlement,
            Settlement boundSettlement,
            LocalBanditCandidateKind policyKind,
            bool hasSecurity,
            float security,
            float nativeWeight,
            float controlMultiplier,
            float finalWeight,
            bool applied,
            string reason)
        {
            string runtimeKind =
                RuntimeKind(
                    settlement,
                    policyKind);

            bool log = false;
            string sample = "";

            if (policyKind ==
                LocalBanditCandidateKind.Town &&
                !_loggedTown)
            {
                _loggedTown = true;
                AddSample(ref sample, "town");
                log = true;
            }

            if (policyKind ==
                LocalBanditCandidateKind.Village &&
                !_loggedVillage)
            {
                _loggedVillage = true;
                AddSample(ref sample, "village");
                log = true;
            }

            if (hasSecurity &&
                LocalBanditControlPolicy.IsFinite(security))
            {
                if (security > 50.0f &&
                    !_loggedSecure)
                {
                    _loggedSecure = true;
                    AddSample(ref sample, "secure");
                    log = true;
                }

                if (security < 50.0f &&
                    !_loggedWeak)
                {
                    _loggedWeak = true;
                    AddSample(ref sample, "weak");
                    log = true;
                }

                if (Math.Abs(security - 50.0f) <= 2.5f &&
                    !_loggedNeutral)
                {
                    _loggedNeutral = true;
                    AddSample(ref sample, "near-neutral");
                    log = true;
                }
            }
            else if (!_loggedMissingSecurity)
            {
                _loggedMissingSecurity = true;
                AddSample(ref sample, "missing-security");
                log = true;
            }

            if (runtimeKind == "Hideout" &&
                !_loggedHideout)
            {
                _loggedHideout = true;
                AddSample(ref sample, "hideout");
                log = true;
            }
            else if (runtimeKind == "Other" &&
                !_loggedOther)
            {
                _loggedOther = true;
                AddSample(ref sample, "other");
                log = true;
            }

            if (!log)
                return;

            ClanAIPostVanilla.WriteExternalLog(
                "LOCAL_BANDIT_CONTROL_EVAL" +
                " sample=" + sample +
                " campaignHour=" +
                    D(CampaignTime.Now.ToHours) +
                SettlementFields(settlement) +
                " candidateKind=" + runtimeKind +
                " policyKind=" + policyKind +
                BoundFields(boundSettlement) +
                " securityAvailable=" + hasSecurity +
                " security=" +
                    (hasSecurity
                        ? F(security)
                        : "<unavailable>") +
                " nativeWeight=" + F(nativeWeight) +
                " controlMultiplier=" +
                    F(controlMultiplier) +
                " finalWeight=" + F(finalWeight) +
                " applied=" + applied +
                " reason=" + Safe(reason) +
                " hook=postfix" +
                " originalExecuted=True" +
                " mutationByClanAI=False");
        }

        private static void OnMobilePartyCreated(
            MobileParty party)
        {
            try
            {
                if (_loggedNativeLooter ||
                    party == null ||
                    !party.IsBandit)
                {
                    return;
                }

                string clanId =
                    party.ActualClan == null
                        ? ""
                        : party.ActualClan.StringId;

                string partyName =
                    party.Name == null
                        ? ""
                        : party.Name.ToString();

                bool looksLikeLooter =
                    ContainsLooter(clanId) ||
                    ContainsLooter(partyName);

                if (!looksLikeLooter)
                    return;

                _loggedNativeLooter = true;

                Settlement home =
                    party.HomeSettlement;

                string component =
                    party.PartyComponent == null
                        ? "<none>"
                        : party.PartyComponent
                            .GetType()
                            .FullName;

                string culture =
                    party.ActualClan == null ||
                    party.ActualClan.Culture == null
                        ? "<none>"
                        : Safe(
                            party.ActualClan
                                .Culture
                                .StringId);

                string template =
                    party.ActualClan == null ||
                    party.ActualClan.DefaultPartyTemplate == null
                        ? "<none>"
                        : Safe(
                            party.ActualClan
                                .DefaultPartyTemplate
                                .StringId);

                ClanAIPostVanilla.WriteExternalLog(
                    "LOCAL_BANDIT_CONTROL_NATIVE_LOOTER_CREATED" +
                    " campaignHour=" +
                        D(CampaignTime.Now.ToHours) +
                    " partyId=" +
                        Safe(party.StringId) +
                    " party=" +
                        Safe(partyName) +
                    " clanId=" +
                        Safe(clanId) +
                    " culture=" + culture +
                    " template=" + template +
                    " partyComponent=" +
                        Safe(component) +
                    " homeSettlementId=" +
                        SettlementId(home) +
                    " homeSettlement=" +
                        SettlementName(home) +
                    " source=native-mobile-party-created-event" +
                    " mutationByClanAI=False");
            }
            catch (Exception ex)
            {
                ClanAIPostVanilla.WriteExternalLog(
                    "LOCAL_BANDIT_CONTROL_TELEMETRY_ERROR" +
                    " stage=party-created" +
                    " type=" + ex.GetType().Name +
                    " mutation=False");
            }
        }

        private static bool ContainsLooter(
            string value)
        {
            return !string.IsNullOrEmpty(value) &&
                value.IndexOf(
                    "looter",
                    StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string RuntimeKind(
            Settlement settlement,
            LocalBanditCandidateKind policyKind)
        {
            if (policyKind ==
                LocalBanditCandidateKind.Town)
                return "Town";

            if (policyKind ==
                LocalBanditCandidateKind.Village)
                return "Village";

            if (settlement != null &&
                settlement.IsHideout)
                return "Hideout";

            return "Other";
        }

        private static void AddSample(
            ref string sample,
            string value)
        {
            sample =
                string.IsNullOrEmpty(sample)
                    ? value
                    : sample + "+" + value;
        }

        private static string SettlementFields(
            Settlement settlement)
        {
            return
                " settlementId=" +
                    SettlementId(settlement) +
                " settlement=" +
                    SettlementName(settlement);
        }

        private static string BoundFields(
            Settlement bound)
        {
            return
                " boundSettlementId=" +
                    SettlementId(bound) +
                " boundSettlement=" +
                    SettlementName(bound);
        }

        private static string SettlementId(
            Settlement settlement)
        {
            return settlement == null ||
                string.IsNullOrEmpty(settlement.StringId)
                    ? "<none>"
                    : Safe(settlement.StringId);
        }

        private static string SettlementName(
            Settlement settlement)
        {
            return settlement == null ||
                settlement.Name == null
                    ? "<none>"
                    : Safe(settlement.Name.ToString());
        }

        private static string Safe(
            string value)
        {
            if (string.IsNullOrEmpty(value))
                return "<none>";

            return value
                .Replace(' ', '_')
                .Replace('\r', '_')
                .Replace('\n', '_');
        }

        private static string F(
            float value)
        {
            return value.ToString(
                "R",
                CultureInfo.InvariantCulture);
        }

        private static string D(
            double value)
        {
            return value.ToString(
                "R",
                CultureInfo.InvariantCulture);
        }
    }
}
