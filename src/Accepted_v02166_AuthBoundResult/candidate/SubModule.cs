using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using HarmonyLib;
using TaleWorlds.CampaignSystem.Incidents;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ScreenSystem;
using SandBox;

namespace BannerlordAITestRunner
{
    public sealed class TestRunnerSubModule : MBSubModuleBase
    {
        private const string Root =
            @"D:\BannerlordAIResearch\Automation\TestRunner";

        private const string CommandPath =
            Root + @"\command.txt";

        private const string StatusPath =
            Root + @"\status.txt";

        private const string LogPath =
            Root + @"\runner.log";

        private const string KingdomDecisionPath =
            Root + @"\kingdom_decisions.json";

        private const string KingdomVoteReceiptPath =
            Root + @"\kingdom_vote_receipts.jsonl";

        private const string DynastyDeliberationReceiptPath =
            Root + @"\dynasty_memory_deliberations.jsonl";

        private const string DynastyCausalReceiptPath =
            Root + @"\dynasty_memory_causal_decisions.jsonl";

        private const string DynastyBranchRetrievalPath =
            Root + @"\dynasty_branch_episode_retrievals.jsonl";

        private const string DynastyChoiceRetrievalPath =
            Root + @"\dynasty_branch_choice_retrievals.jsonl";

        private const string PatrolDefenseShadowPath =
            Root + @"\patrol_defense_shadow.jsonl";

        private const string PatrolDefenseRearmShadowPath =
            Root + @"\patrol_defense_rearm_shadow.jsonl";

        private const string PatrolDefenseMemoryRetrievalPath =
            Root + @"\patrol_defense_memory_retrievals.jsonl";

        private const string PatrolDefenseRecognitionPath =
            Root + @"\patrol_defense_recognition.jsonl";

        private const string PatrolDefenseMemoryAcquisitionPath =
            Root + @"\patrol_defense_memory_acquisition.jsonl";

        private const string PatrolDefenseDeliberationAdmissionPath =
            Root + @"\patrol_defense_deliberation_admissions.jsonl";
        private const string PatrolDefenseDeliberationProviderPath =
            Root + @"\patrol_defense_deliberation_provider_invocations.jsonl";
        private const string PatrolDefenseProviderOutboxPath =
            Root + @"\patrol_defense_provider_outbox.jsonl";
        private const string PatrolDefenseProviderResultAdmissionPath =
            Root + @"\patrol_defense_provider_result_admissions.jsonl";
        private const string PatrolDefenseProviderResultV2AdmissionPath =
            Root + @"\patrol_defense_provider_result_v2_admissions.jsonl";
        private const string PatrolDefenseProviderResultV3AdmissionPath =
            Root + @"\patrol_defense_provider_result_v3_admissions.jsonl";
        private const string PatrolDefenseProviderTransportResultBindingPath =
            Root + @"\patrol_defense_provider_transport_result_bindings.jsonl";
        private const string PatrolDefenseProviderResultV4AdmissionPath =
            Root + @"\patrol_defense_provider_result_v4_admissions.jsonl";
        private const string PatrolDefenseProviderTransportResultV4BindingPath =
            Root + @"\patrol_defense_provider_transport_result_v4_bindings.jsonl";
        private const string PatrolDefenseProviderResultV5AdmissionPath =
            Root + @"\patrol_defense_provider_result_v5_admissions.jsonl";
        private const string PatrolDefenseProviderTransportResultV5BindingPath =
            Root + @"\patrol_defense_provider_transport_result_v5_bindings.jsonl";
        private const string PatrolDefenseProviderExecutionPolicyRegistrationPath =
            Root + @"\patrol_defense_provider_execution_policy_registrations.jsonl";
        private const string PatrolDefenseProviderTransportExecutionAuthorizationPath =
            Root + @"\patrol_defense_provider_transport_execution_authorizations.jsonl";
        private const string PatrolDefenseProviderDispatchCompletionPath =
            Root + @"\patrol_defense_provider_dispatch_completions.jsonl";
        private const string PatrolDefenseProviderJobClaimPath =
            Root + @"\patrol_defense_provider_job_claims.jsonl";
        private const string PatrolDefenseProviderJobReleasePath =
            Root + @"\patrol_defense_provider_job_releases.jsonl";
        private const string PatrolDefenseProviderRequestRegistrationPath =
            Root + @"\patrol_defense_provider_request_registrations.jsonl";
        private const string PatrolDefenseProviderTransportRegistrationPath =
            Root + @"\patrol_defense_provider_transport_registrations.jsonl";

        private const double RearmAbsenceBoundaryCampaignHours = 2.0;

        private static readonly object IoLock =
            new object();

        private readonly Stopwatch _poll =
            Stopwatch.StartNew();
        private string _lastCommand = "<none>";
        private string _lastResult = "not-started";
        private string _lastLoadedSaveName = "<none>";
        private bool _campaignSeen;
        private Campaign _lastCampaignInstance;
        private int _campaignGeneration;
        private CampaignTimeControlMode? _autoDesiredMode;
        private int _autoResumeCount;
        private string _lastBlockers = "<none>";
        private bool _movementIntentActive;
        private string _patrolSettlementId;
        private DateTime _nextPatrolDefenseShadowUtc;
        private string _lastPatrolDefenseShadowFingerprint;
        private bool _postConsumedRepeatLogged;
        private double _rearmAbsenceStartCampaignHours = double.NaN;
        private bool _rearmBoundaryObserved;
        private bool _rearmEligibleLogged;
        private long _patrolDefenseShadowScans;
        private long _patrolDefenseShadowReceipts;
        private long _patrolDefenseShadowWouldInterrupt;
        private string _pendingSaveName;
        private string _pendingSaveVerifyName;
        private DateTime _pendingSaveVerifyDeadlineUtc;
        private static Incident _currentIncident;
        private static object _currentIncidentView;
        private static bool _incidentPatchApplied;
        private static bool _postDecisionHold;
        private bool _incidentPauseLogged;
        private readonly PatrolDefenseReconsiderationTracker
            _patrolDefenseReconsiderationTracker =
                new PatrolDefenseReconsiderationTracker();
        private readonly PatrolDefenseDeliberationRouteTracker
            _patrolDefenseDeliberationRouteTracker =
                new PatrolDefenseDeliberationRouteTracker(128);
        private readonly PatrolDefenseProviderDispatchQueue
            _patrolDefenseProviderDispatchQueue =
                new PatrolDefenseProviderDispatchQueue(32);
        private readonly PatrolDefenseAdvisoryRuntimeGate
            _patrolDefenseAdvisoryRuntimeGate =
                new PatrolDefenseAdvisoryRuntimeGate();
        private string _pendingPatrolDefenseDeliberationRequestJson;

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();

            try
            {
                Directory.CreateDirectory(Root);
                ApplyIncidentPatch();
                WriteLog("RUNNER_LOAD version=v0.2.10.20-temporal-history-context-shadow");
                WriteStatus("module_loaded");
            }
            catch
            {
            }
        }

        protected override void OnApplicationTick(float dt)
        {
            base.OnApplicationTick(dt);

            if (_poll.ElapsedMilliseconds < 200)
                return;

            _poll.Restart();

            Campaign campaign = Campaign.Current;

            if (campaign != null &&
                !object.ReferenceEquals(
                    campaign,
                    _lastCampaignInstance))
            {
                _lastCampaignInstance = campaign;
                _campaignGeneration++;
                PatrolDefenseApplyGate.ResetForCampaign();
                ResetPatrolDefenseRearmShadow();
                _patrolDefenseDeliberationRouteTracker.Reset();
            _patrolDefenseProviderDispatchQueue.Reset();
            _patrolDefenseAdvisoryRuntimeGate.Reset();
            _pendingPatrolDefenseDeliberationRequestJson = null;

                WriteLog(
                    "CAMPAIGN_INSTANCE generation=" +
                    _campaignGeneration.ToString(
                        CultureInfo.InvariantCulture));

                WriteStatus("campaign_instance");
            }

            if (campaign != null && !_campaignSeen)
            {
                _campaignSeen = true;
                WriteLog(
                    "CAMPAIGN_READY timeControl=" +
                    campaign.TimeControlMode);

                WriteStatus("campaign_ready");
            }

            TryConsumeCommand(campaign);

            if (campaign != null)
            {
                TryMaintainAutomation(campaign);
                TryPatrolDefenseShadow(campaign);
                TryProcessPendingSave(campaign);
                WriteStatus("tick");
            }
        }

        private void TryConsumeCommand(Campaign campaign)
        {
            string command = null;

            try
            {
                lock (IoLock)
                {
                    if (!File.Exists(CommandPath))
                        return;

                    command =
                        File.ReadAllText(CommandPath).Trim();

                    File.Delete(CommandPath);
                }
            }
            catch (Exception ex)
            {
                _lastResult =
                    "command_read_failed_" +
                    ex.GetType().Name;

                WriteLog(
                    "COMMAND_READ_FAILED type=" +
                    ex.GetType().Name);

                return;
            }

            if (string.IsNullOrWhiteSpace(command))
                return;

            _lastCommand = command;

            bool isLoadSave =
                command.Trim().StartsWith(
                    "LOAD_SAVE ",
                    StringComparison.OrdinalIgnoreCase);

            if (campaign == null && !isLoadSave)
            {
                _lastResult = "campaign_not_ready";
                WriteLog(
                    "COMMAND_DEFERRED command=" +
                    Clean(command) +
                    " reason=campaign_not_ready");
                return;
            }

            Execute(campaign, command);
        }
        private void Execute(
            Campaign campaign,
            string command)
        {
            string normalized =
                command.Trim().ToUpperInvariant();

            try
            {
                if (normalized.StartsWith("LOAD_SAVE "))
                {
                    string saveName = command.Substring("LOAD_SAVE ".Length).Trim();

                    if (string.IsNullOrWhiteSpace(saveName))
                    {
                        _lastResult = "operator_load_save_empty";
                    }
                    else if (!MBSaveLoad.IsSaveGameFileExists(saveName))
                    {
                        _lastResult = "operator_load_save_not_found:" + saveName;
                    }
                    else
                    {
                        var loadResult = MBSaveLoad.LoadSaveGameData(saveName);

                        if (loadResult == null)
                        {
                            _lastResult = "operator_load_save_failed:" + saveName;
                        }
                        else
                        {
                            _autoDesiredMode = null;

                            if (campaign != null)
                            {
                                campaign.SetTimeControlModeLock(false);
                                campaign.TimeControlMode =
                                    CampaignTimeControlMode.Stop;
                            }

                            _lastLoadedSaveName = saveName;
                            _lastResult =
                                "operator_load_save_started:" + saveName;

                            WriteLog(
                                "COMMAND command=" +
                                Clean(command) +
                                " result=" +
                                _lastResult +
                                " timeControl=" +
                                (campaign == null
                                    ? "<none>"
                                    : campaign.TimeControlMode.ToString()));

                            WriteStatus("load_save_started");

                            MBGameManager.StartNewGame(
                                new SandBoxGameManager(loadResult));

                            return;
                        }
                    }
                }
                else if (normalized.StartsWith("SAVE_AS_TEST "))
                {
                    string saveName =
                        command.Substring(
                            "SAVE_AS_TEST ".Length)
                            .Trim();

                    if (string.IsNullOrWhiteSpace(saveName))
                    {
                        _lastResult =
                            "operator_save_test_empty";
                    }
                    else if (!saveName.StartsWith(
                        "ClanAI V020V PERSIST ",
                        StringComparison.Ordinal))
                    {
                        _lastResult =
                            "operator_save_test_prefix_denied";
                    }
                    else if (MBSaveLoad.IsSaveGameFileExists(
                        saveName))
                    {
                        _lastResult =
                            "operator_save_test_exists:" +
                            saveName;
                    }
                    else if (
                        _pendingSaveName != null ||
                        _pendingSaveVerifyName != null)
                    {
                        _lastResult =
                            "operator_save_test_already_pending";
                    }
                    else
                    {
                        _autoDesiredMode = null;

                        campaign.SetTimeControlModeLock(false);
                        campaign.TimeControlMode =
                            CampaignTimeControlMode.Stop;

                        _pendingSaveName = saveName;
                        _lastResult =
                            "operator_save_test_scheduled:" +
                            saveName;

                        WriteLog(
                            "SAVE_TEST_SCHEDULED name=" +
                            Clean(saveName) +
                            " overwriteAllowed=False" +
                            " prefixGuard=True");
                    }
                }
                else if (normalized == "HOLD")
                {
                    MobileParty party = MobileParty.MainParty;
                    if (party == null)
                    {
                        _lastResult = "operator_no_main_party";
                    }
                    else
                    {
                        TryDynastyShadow(
                            "strategic_command",
                            "HOLD",
                            "Hold position",
                            "HOLD");

                        party.SetMoveModeHold();
                        _movementIntentActive = false;
                        ClearPatrolDefenseShadowIntent();
                        _lastResult = "operator_hold";
                    }
                }
                else if (normalized.StartsWith("MOVE_SETTLEMENT "))
                {
                    string id = command.Substring("MOVE_SETTLEMENT ".Length).Trim();
                    Settlement target = Settlement.All.FirstOrDefault(
                        s => string.Equals(s.StringId, id, StringComparison.OrdinalIgnoreCase));
                    MobileParty party = MobileParty.MainParty;
                    if (party == null)
                        _lastResult = "operator_no_main_party";
                    else if (target == null)
                        _lastResult = "operator_settlement_not_found";
                    else
                    {
                        TryDynastyShadow(
                            "strategic_command",
                            "MOVE_SETTLEMENT:" + target.StringId,
                            "Move to settlement",
                            target.StringId);

                        party.SetMoveGoToSettlement(target, MobileParty.NavigationType.Default, false);
                        _movementIntentActive = true;
                        ClearPatrolDefenseShadowIntent();
                        _lastResult = "operator_move_settlement:" + target.StringId;
                    }
                }
                else if (normalized.StartsWith("PATROL_SETTLEMENT "))
                {
                    string id = command.Substring("PATROL_SETTLEMENT ".Length).Trim();
                    Settlement target = Settlement.All.FirstOrDefault(
                        s => string.Equals(s.StringId, id, StringComparison.OrdinalIgnoreCase));
                    MobileParty party = MobileParty.MainParty;
                    if (party == null)
                        _lastResult = "operator_no_main_party";
                    else if (target == null)
                        _lastResult = "operator_settlement_not_found";
                    else
                    {
                        TryDynastyShadow(
                            "strategic_command",
                            "PATROL_SETTLEMENT:" + target.StringId,
                            "Patrol settlement",
                            target.StringId);

                        party.SetMovePatrolAroundSettlement(target, MobileParty.NavigationType.Default, false);
                        _movementIntentActive = true;
                        ArmPatrolDefenseShadow(target);
                        _lastResult = "operator_patrol_settlement:" + target.StringId;
                    }
                }
                else if (normalized.StartsWith("DYNASTY_DEFENSE_DECIDE "))
                {
                    string id =
                        command.Substring(
                            "DYNASTY_DEFENSE_DECIDE ".Length)
                            .Trim();

                    _lastResult =
                        ExecuteDynastyDefenseDecision(id);
                }
                else if (normalized.StartsWith("DYNASTY_BRANCH_RETRIEVE "))
                {
                    string context =
                        command.Substring(
                            "DYNASTY_BRANCH_RETRIEVE ".Length)
                            .Trim();

                    _lastResult =
                        TryRetrieveDynastyBranchEpisode(
                            context);
                }
                else if (normalized.StartsWith("DYNASTY_CHOICE_RETRIEVE "))
                {
                    string context =
                        command.Substring(
                            "DYNASTY_CHOICE_RETRIEVE ".Length)
                            .Trim();

                    _lastResult =
                        TryRetrieveDynastyBranchChoice(
                            context);
                }
                else if (normalized ==
                    "PATROL_DEFENSE_MEMORY_SEED_REAL_V02107")
                {
                    _lastResult =
                        TrySeedRealPatrolDefenseMemories();
                }
                else if (normalized.StartsWith(
                    "PATROL_DEFENSE_RECOGNIZE "))
                {
                    string raw =
                        command.Substring(
                            "PATROL_DEFENSE_RECOGNIZE ".Length)
                            .Trim();

                    string[] parts =
                        raw.Split(
                            new[] { ' ' },
                            3,
                            StringSplitOptions.RemoveEmptyEntries);

                    if (parts.Length < 2)
                    {
                        _lastResult =
                            "patrol_defense_recognize_invalid";
                    }
                    else
                    {
                        _lastResult =
                            TryRecognizePatrolDefenseEnemy(
                                parts[0],
                                parts[1],
                                parts.Length >= 3
                                    ? parts[2]
                                    : "manual");
                    }
                }
                else if (normalized.StartsWith(
                    "PATROL_DEFENSE_HISTORY_FIXTURE "))
                {
                    string raw = command.Substring(
                        "PATROL_DEFENSE_HISTORY_FIXTURE ".Length).Trim();
                    string[] parts = raw.Split(
                        new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    _lastResult = parts.Length != 8
                        ? "patrol_defense_history_fixture_invalid"
                        : TryRecordPatrolDefenseHistoryFixture(parts);
                }
                else if (normalized.StartsWith(
                    "PATROL_DEFENSE_PROVIDER_JOB_RELEASE "))
                {
                    string raw = command.Substring(
                        "PATROL_DEFENSE_PROVIDER_JOB_RELEASE ".Length).Trim();
                    string[] parts = raw.Split(
                        new[] { ' ' },
                        3,
                        StringSplitOptions.RemoveEmptyEntries);
                    _lastResult = parts.Length != 3
                        ? "patrol_defense_provider_job_release_invalid"
                        : TryReleasePatrolDefenseProviderJob(
                            parts[0],
                            parts[1],
                            parts[2]);
                }
                else if (normalized.StartsWith(
                    "PATROL_DEFENSE_PROVIDER_JOB_CLAIM "))
                {
                    string raw = command.Substring(
                        "PATROL_DEFENSE_PROVIDER_JOB_CLAIM ".Length).Trim();
                    string[] parts = raw.Split(
                        new[] { ' ' },
                        3,
                        StringSplitOptions.RemoveEmptyEntries);
                    _lastResult = parts.Length != 3
                        ? "patrol_defense_provider_job_claim_invalid"
                        : TryClaimPatrolDefenseProviderJob(
                            parts[0],
                            parts[1],
                            parts[2]);
                }
                else if (normalized.StartsWith(
                    "PATROL_DEFENSE_PROVIDER_REQUEST_REGISTER "))
                {
                    string raw = command.Substring(
                        "PATROL_DEFENSE_PROVIDER_REQUEST_REGISTER ".Length).Trim();
                    _lastResult = string.IsNullOrWhiteSpace(raw)
                        ? "patrol_defense_provider_request_registration_invalid"
                        : TryRegisterPatrolDefenseProviderRequest(raw);
                }
                else if (normalized.StartsWith(
                    "PATROL_DEFENSE_PROVIDER_TRANSPORT_REGISTER "))
                {
                    string raw = command.Substring(
                        "PATROL_DEFENSE_PROVIDER_TRANSPORT_REGISTER ".Length).Trim();
                    _lastResult = string.IsNullOrWhiteSpace(raw)
                        ? "patrol_defense_provider_transport_registration_invalid"
                        : TryRegisterPatrolDefenseProviderTransport(raw);
                }
                else if (normalized.StartsWith(
                    "PATROL_DEFENSE_PROVIDER_EXECUTION_POLICY_REGISTER "))
                {
                    string raw = command.Substring(
                        "PATROL_DEFENSE_PROVIDER_EXECUTION_POLICY_REGISTER ".Length).Trim();
                    _lastResult = string.IsNullOrWhiteSpace(raw)
                        ? "patrol_defense_provider_execution_policy_registration_invalid"
                        : TryRegisterPatrolDefenseProviderExecutionPolicy(raw);
                }
                else if (normalized.StartsWith(
                    "PATROL_DEFENSE_PROVIDER_TRANSPORT_EXECUTION_AUTHORIZE "))
                {
                    string raw = command.Substring(
                        "PATROL_DEFENSE_PROVIDER_TRANSPORT_EXECUTION_AUTHORIZE ".Length).Trim();
                    string[] parts = raw.Split(
                        new[] { ' ' },
                        2,
                        StringSplitOptions.RemoveEmptyEntries);
                    _lastResult = parts.Length != 2
                        ? "patrol_defense_provider_transport_execution_authorization_invalid"
                        : TryAuthorizePatrolDefenseProviderTransportExecution(
                            parts[0],
                            parts[1]);
                }
                else if (normalized.StartsWith(
                    "PATROL_DEFENSE_PROVIDER_TRANSPORT_RESULT_V5_ADMIT "))
                {
                    string raw = command.Substring(
                        "PATROL_DEFENSE_PROVIDER_TRANSPORT_RESULT_V5_ADMIT ".Length).Trim();
                    string[] parts = raw.Split(
                        new[] { ' ' },
                        2,
                        StringSplitOptions.RemoveEmptyEntries);
                    _lastResult = parts.Length != 2
                        ? "patrol_defense_provider_transport_result_v5_invalid"
                        : TryAdmitPatrolDefenseProviderTransportResultV5(
                            parts[0],
                            parts[1]);
                }
                else if (normalized.StartsWith(
                    "PATROL_DEFENSE_PROVIDER_TRANSPORT_RESULT_V4_ADMIT "))
                {
                    string raw = command.Substring(
                        "PATROL_DEFENSE_PROVIDER_TRANSPORT_RESULT_V4_ADMIT ".Length).Trim();
                    string[] parts = raw.Split(
                        new[] { ' ' },
                        2,
                        StringSplitOptions.RemoveEmptyEntries);
                    _lastResult = parts.Length != 2
                        ? "patrol_defense_provider_transport_result_v4_invalid"
                        : TryAdmitPatrolDefenseProviderTransportResultV4(
                            parts[0],
                            parts[1]);
                }
                else if (normalized.StartsWith(
                    "PATROL_DEFENSE_PROVIDER_TRANSPORT_RESULT_ADMIT "))
                {
                    string raw = command.Substring(
                        "PATROL_DEFENSE_PROVIDER_TRANSPORT_RESULT_ADMIT ".Length).Trim();
                    string[] parts = raw.Split(
                        new[] { ' ' },
                        2,
                        StringSplitOptions.RemoveEmptyEntries);
                    _lastResult = parts.Length != 2
                        ? "patrol_defense_provider_transport_result_invalid"
                        : TryAdmitPatrolDefenseProviderTransportResult(
                            parts[0],
                            parts[1]);
                }
                else if (normalized.StartsWith(
                    "PATROL_DEFENSE_PROVIDER_RESULT_V3_ADMIT "))
                {
                    string raw = command.Substring(
                        "PATROL_DEFENSE_PROVIDER_RESULT_V3_ADMIT ".Length).Trim();
                    _lastResult = string.IsNullOrWhiteSpace(raw)
                        ? "patrol_defense_provider_result_v3_invalid"
                        : TryAdmitPatrolDefenseProviderResultV3(raw);
                }
                else if (normalized.StartsWith(
                    "PATROL_DEFENSE_PROVIDER_RESULT_V2_ADMIT "))
                {
                    string raw = command.Substring(
                        "PATROL_DEFENSE_PROVIDER_RESULT_V2_ADMIT ".Length).Trim();
                    _lastResult = string.IsNullOrWhiteSpace(raw)
                        ? "patrol_defense_provider_result_v2_invalid"
                        : TryAdmitPatrolDefenseProviderResultV2(raw);
                }
                else if (normalized.StartsWith(
                    "PATROL_DEFENSE_PROVIDER_RESULT_ADMIT "))
                {
                    string raw = command.Substring(
                        "PATROL_DEFENSE_PROVIDER_RESULT_ADMIT ".Length).Trim();
                    _lastResult = string.IsNullOrWhiteSpace(raw)
                        ? "patrol_defense_provider_result_invalid"
                        : TryAdmitPatrolDefenseProviderResult(raw);
                }
                else if (normalized.StartsWith(
                    "PATROL_DEFENSE_PROVIDER_MOCK_RUN "))
                {
                    string raw = command.Substring(
                        "PATROL_DEFENSE_PROVIDER_MOCK_RUN ".Length).Trim();
                    string[] parts = raw.Split(
                        new[] { ' ' },
                        2,
                        StringSplitOptions.RemoveEmptyEntries);
                    _lastResult = parts.Length != 2
                        ? "patrol_defense_provider_mock_invalid"
                        : TryRunPatrolDefenseMockProvider(
                            parts[0],
                            parts[1]);
                }
                else if (normalized.StartsWith(
                    "PATROL_DEFENSE_ADVISORY_ADMIT "))
                {
                    string raw = command.Substring(
                        "PATROL_DEFENSE_ADVISORY_ADMIT ".Length).Trim();
                    string[] parts = raw.Split(
                        new[] { ' ' },
                        2,
                        StringSplitOptions.RemoveEmptyEntries);
                    _lastResult = parts.Length != 2
                        ? "patrol_defense_advisory_admit_invalid"
                        : TryAdmitPatrolDefenseAdvisory(
                            parts[0],
                            parts[1]);
                }
                else if (normalized.StartsWith(
                    "PATROL_DEFENSE_RECENT_OUTCOME_CONTEXT "))
                {
                    string raw=command.Substring(
                        "PATROL_DEFENSE_RECENT_OUTCOME_CONTEXT ".Length).Trim();
                    string[] parts=raw.Split(
                        new[] { ' ' }, 3,
                        StringSplitOptions.RemoveEmptyEntries);
                    _lastResult=parts.Length < 2
                        ? "patrol_defense_recent_outcome_context_invalid"
                        : TryRetrievePatrolDefenseRecentOutcomeContext(
                            parts[0],parts[1],
                            parts.Length >= 3 ? parts[2] : "manual");
                }
                else if (normalized.StartsWith(
                    "PATROL_DEFENSE_HISTORY_CONTEXT "))
                {
                    string raw = command.Substring(
                        "PATROL_DEFENSE_HISTORY_CONTEXT ".Length).Trim();
                    string[] parts = raw.Split(
                        new[] { ' ' }, 3,
                        StringSplitOptions.RemoveEmptyEntries);
                    _lastResult = parts.Length < 2
                        ? "patrol_defense_history_context_invalid"
                        : TryRetrievePatrolDefenseHistoryContext(
                            parts[0], parts[1],
                            parts.Length >= 3 ? parts[2] : "manual");
                }
                else if (normalized.StartsWith(
                    "PATROL_DEFENSE_MEMORY_RETRIEVE "))
                {
                    string raw =
                        command.Substring(
                            "PATROL_DEFENSE_MEMORY_RETRIEVE ".Length)
                            .Trim();

                    string[] parts =
                        raw.Split(
                            new[] { ' ' },
                            3,
                            StringSplitOptions.RemoveEmptyEntries);

                    if (parts.Length < 2)
                    {
                        _lastResult =
                            "patrol_defense_memory_retrieve_invalid";
                    }
                    else
                    {
                        _lastResult =
                            TryRetrievePatrolDefenseMemory(
                                parts[0],
                                parts[1],
                                parts.Length >= 3
                                    ? parts[2]
                                    : "manual");
                    }
                }
                else if (normalized == "INCIDENT_COOLDOWN_STATUS")
                {
                    _lastResult =
                        BuildIncidentCooldownStatus(
                            campaign);
                }
                else if (normalized.StartsWith("MENU_SELECT_ID "))
                {
                    string optionId =
                        command.Substring(
                            "MENU_SELECT_ID ".Length).Trim();

                    _lastResult =
                        SelectCurrentMenuOptionById(
                            campaign,
                            optionId);
                }
                else if (normalized.StartsWith("MENU_SELECT_INDEX "))
                {
                    string raw =
                        command.Substring(
                            "MENU_SELECT_INDEX ".Length).Trim();

                    if (!int.TryParse(raw, out int index))
                    {
                        _lastResult =
                            "menu_select_invalid_index";
                    }
                    else
                    {
                        _lastResult =
                            SelectCurrentMenuOptionByIndex(
                                campaign,
                                index);
                    }
                }
                else if (normalized.StartsWith("INCIDENT_SELECT "))
                {
                    string raw =
                        command.Substring(
                            "INCIDENT_SELECT ".Length).Trim();

                    if (!int.TryParse(raw, out int index))
                    {
                        _lastResult =
                            "incident_select_invalid_index";
                    }
                    else
                    {
                        _lastResult =
                            SelectIncidentOption(index);
                    }
                }
                else if (normalized == "KINGDOM_DECISIONS_SNAPSHOT")
                {
                    _lastResult =
                        SnapshotKingdomDecisions(campaign);
                }
                else if (normalized.StartsWith("KINGDOM_VOTE "))
                {
                    string raw =
                        command.Substring(
                            "KINGDOM_VOTE ".Length).Trim();

                    _lastResult =
                        ExecuteKingdomVote(
                            campaign,
                            raw);
                }
                else if (normalized == "CLOSE_ESCAPE_MENU")
                {
                    _lastResult =
                        CloseEscapeMenuIfOpen();
                }
                else if (normalized == "PAUSE" ||
                    normalized == "STOP")
                {
                    _autoDesiredMode = null;

                    campaign.SetTimeControlModeLock(false);
                    campaign.TimeControlMode =
                        CampaignTimeControlMode.Stop;

                    _lastResult = "paused";
                }
                else if (normalized == "PLAY")
                {
                    _postDecisionHold = false;
                    _autoDesiredMode = null;

                    campaign.SetTimeControlModeLock(false);
                    campaign.TimeControlMode =
                        CampaignTimeControlMode.StoppablePlay;
                    campaign.SetTimeSpeed(1);

                    _lastResult = "playing";
                }
                else if (normalized == "FAST")
                {
                    _autoDesiredMode = null;

                    campaign.SetTimeControlModeLock(false);
                    campaign.TimeControlMode =
                        CampaignTimeControlMode.StoppableFastForward;
                    campaign.SetTimeSpeed(2);

                    _lastResult = "fast_forward";
                }
                else if (normalized == "AUTO_PLAY")
                {
                    _postDecisionHold = false;
                    _autoDesiredMode =
                        CampaignTimeControlMode.StoppablePlay;

                    _lastResult = "auto_play_armed";

                    TryMaintainAutomation(campaign);
                }
                else if (normalized == "AUTO_FAST")
                {
                    _postDecisionHold = false;
                    _autoDesiredMode =
                        CampaignTimeControlMode.UnstoppableFastForwardForPartyWaitTime;

                    _lastResult = "auto_fast_armed";

                    TryMaintainAutomation(campaign);
                }
                else if (normalized == "STATUS")
                {
                    _lastResult = "status_requested";
                }
                else if (normalized == "EXIT_NOSAVE")
                {
                    _autoDesiredMode = null;

                    campaign.SetTimeControlModeLock(false);
                    campaign.TimeControlMode =
                        CampaignTimeControlMode.Stop;

                    _lastResult = "exit_scheduled";

                    WriteLog(
                        "COMMAND command=" +
                        Clean(command) +
                        " result=" +
                        _lastResult +
                        " timeControl=" +
                        campaign.TimeControlMode);

                    WriteStatus("exit_scheduled");

                    TaleWorlds.MountAndBlade.Module.CurrentModule.ShutDownWithDelay(
                        "BannerlordAI TestRunner no-save exit",
                        2);

                    return;
                }
                else
                {
                    _lastResult = "unknown_command";
                }

                WriteLog(
                    "COMMAND command=" +
                    Clean(command) +
                    " result=" +
                    _lastResult +
                    " timeControl=" +
                    campaign.TimeControlMode);

                WriteStatus("command");
            }
            catch (Exception ex)
            {
                _lastResult =
                    "command_failed_" +
                    ex.GetType().Name;
                WriteLog(
                    "COMMAND_FAILED command=" +
                    Clean(command) +
                    " type=" +
                    ex.GetType().Name +
                    " message=" +
                    Clean(ex.Message));

                WriteStatus("command_failed");
            }
        }

        private static string
            BuildIncidentCooldownStatus(
                Campaign campaign)
        {
            try
            {
                if (campaign == null)
                    return "incident_cooldown:no_campaign";

                var manager =
                    campaign.IncidentManager;

                if (manager == null)
                    return "incident_cooldown:no_manager";

                Type managerType =
                    manager.GetType();

                FieldInfo globalField =
                    managerType.GetField(
                        "_lastGlobalIncidentCooldown",
                        BindingFlags.Instance |
                        BindingFlags.NonPublic);

                FieldInfo perIncidentField =
                    managerType.GetField(
                        "_incidentsOnCooldown",
                        BindingFlags.Instance |
                        BindingFlags.NonPublic);

                CampaignTime lastGlobal =
                    globalField == null
                        ? default(CampaignTime)
                        : (CampaignTime)
                            globalField.GetValue(
                                manager);

                int perIncidentCount = -1;
                if (perIncidentField != null)
                {
                    object raw =
                        perIncidentField.GetValue(
                            manager);

                    if (raw != null)
                    {
                        PropertyInfo countProperty =
                            raw.GetType()
                                .GetProperty(
                                    "Count");

                        if (countProperty != null)
                        {
                            perIncidentCount =
                                (int)
                                    countProperty
                                        .GetValue(
                                            raw,
                                            null);
                        }
                    }
                }

                var model =
                    campaign.Models == null
                        ? null
                        : campaign.Models.IncidentModel;

                CampaignTime minCooldown =
                    model == null
                        ? default(CampaignTime)
                        : model.GetMinGlobalCooldownTime();

                CampaignTime maxCooldown =
                    model == null
                        ? default(CampaignTime)
                        : model.GetMaxGlobalCooldownTime();

                float probability =
                    model == null
                        ? float.NaN
                        : model.GetIncidentTriggerGlobalProbability();

                double nowHours =
                    CampaignTime.Now.ToHours;

                string result =
                    "incident_cooldown_status" +
                    ":nowHours=" +
                    nowHours.ToString(
                        "R",
                        CultureInfo.InvariantCulture) +
                    ":globalDeadlineHours=" +
                    lastGlobal.ToHours.ToString(
                        "R",
                        CultureInfo.InvariantCulture) +
                    ":globalRemainingHours=" +
                    lastGlobal.RemainingHoursFromNow.ToString(
                        "R",
                        CultureInfo.InvariantCulture) +
                    ":globalIsPast=" +
                    lastGlobal.IsPast +
                    ":globalIsFuture=" +
                    lastGlobal.IsFuture;

                result +=
                    ":minCooldownToHours=" +
                    minCooldown.ToHours.ToString(
                        "R",
                        CultureInfo.InvariantCulture) +
                    ":maxCooldownToHours=" +
                    maxCooldown.ToHours.ToString(
                        "R",
                        CultureInfo.InvariantCulture) +
                    ":triggerProbability=" +
                    probability.ToString(
                        "R",
                        CultureInfo.InvariantCulture) +
                    ":perIncidentCount=" +
                    perIncidentCount.ToString(
                        CultureInfo.InvariantCulture);

                WriteLog(
                    "INCIDENT_COOLDOWN_STATUS " +
                    result);

                return result;
            }
            catch (Exception ex)
            {
                WriteLog(
                    "INCIDENT_COOLDOWN_STATUS_FAILED" +
                    " type=" +
                    ex.GetType().Name +
                    " message=" +
                    Clean(ex.Message));

                return
                    "incident_cooldown_failed_" +
                    ex.GetType().Name;
            }
        }

        private void ArmPatrolDefenseShadow(
            Settlement settlement)
        {
            _patrolSettlementId =
                settlement == null
                    ? null
                    : settlement.StringId;

            _nextPatrolDefenseShadowUtc =
                DateTime.MinValue;

            _lastPatrolDefenseShadowFingerprint =
                null;
            _postConsumedRepeatLogged = false;
            ResetPatrolDefenseRearmShadow();
            _patrolDefenseReconsiderationTracker.Reset();
            _patrolDefenseDeliberationRouteTracker.Reset();
            _patrolDefenseProviderDispatchQueue.Reset();
            _patrolDefenseAdvisoryRuntimeGate.Reset();
            _pendingPatrolDefenseDeliberationRequestJson = null;

            PatrolDefenseApplyGate.Cancel(
                "recognition_shadow_no_apply");
        }

        private void ClearPatrolDefenseShadowIntent()
        {
            PatrolDefenseApplyGate.Cancel("intent_cleared");
            _patrolSettlementId = null;
            _nextPatrolDefenseShadowUtc =
                DateTime.MinValue;
            _lastPatrolDefenseShadowFingerprint =
                null;
            _postConsumedRepeatLogged = false;
            ResetPatrolDefenseRearmShadow();
            _patrolDefenseReconsiderationTracker.Reset();
            _patrolDefenseDeliberationRouteTracker.Reset();
            _patrolDefenseProviderDispatchQueue.Reset();
            _patrolDefenseAdvisoryRuntimeGate.Reset();
            _pendingPatrolDefenseDeliberationRequestJson = null;
        }

        private void TryPatrolDefenseShadow(
            Campaign campaign)
        {
            try
            {
                if (campaign == null ||
                    !_movementIntentActive ||
                    string.IsNullOrWhiteSpace(
                        _patrolSettlementId) ||
                    _currentIncident != null ||
                    _postDecisionHold)
                {
                    return;
                }

                DateTime now =
                    DateTime.UtcNow;

                if (now <
                    _nextPatrolDefenseShadowUtc)
                {
                    return;
                }

                _nextPatrolDefenseShadowUtc =
                    now.AddSeconds(1);

                Settlement target =
                    Settlement.All.FirstOrDefault(
                        s => s != null &&
                        string.Equals(
                            s.StringId,
                            _patrolSettlementId,
                            StringComparison.Ordinal));

                if (target == null)
                    return;

                PatrolDefenseShadowResult result =
                    PatrolDefenseShadow.Evaluate(
                        target);

                _patrolDefenseShadowScans++;

                if (result == null ||
                    string.IsNullOrWhiteSpace(
                        result.Json))
                {
                    return;
                }

                bool sameFingerprint = string.Equals(
                    result.Fingerprint,
                    _lastPatrolDefenseShadowFingerprint,
                    StringComparison.Ordinal);
                bool postConsumedRepeat =
                    sameFingerprint &&
                    result.WouldInterrupt &&
                    PatrolDefenseApplyGate.IsConsumed &&
                    !PatrolDefenseApplyGate.IsActive &&
                    !_postConsumedRepeatLogged;

                if (sameFingerprint && !postConsumedRepeat)
                    return;

                if (postConsumedRepeat)
                    _postConsumedRepeatLogged = true;
                else
                    _lastPatrolDefenseShadowFingerprint =
                        result.Fingerprint;

                string memoryContextJson =
                    ObservePatrolDefenseRecognition(result);
                string memoryStateComparisonJson =
                    BuildPatrolDefenseMemoryStateComparison(
                        memoryContextJson,
                        result);
                string memoryUpdateCandidateJson =
                    BuildPatrolDefenseMemoryUpdateCandidate(
                        memoryContextJson,
                        memoryStateComparisonJson,
                        result);
                string memoryUpdateWriteJson =
                    TryAppendEligibleKnownPatrolDefenseMemoryUpdate(
                        memoryContextJson,
                        memoryUpdateCandidateJson,
                        result);
                string recentOutcomeContextJson =
                    ObservePatrolDefenseRecentOutcomeContext(
                        result);
                string actorKnownContextJson =
                    BuildPatrolDefenseActorKnownContext(
                        result,
                        memoryContextJson,
                        memoryStateComparisonJson,
                        recentOutcomeContextJson);
                PatrolDefenseReconsiderationEvidenceData
                    reconsiderationData;
                string reconsiderationEvidenceJson =
                    BuildPatrolDefenseReconsiderationEvidence(
                        result,
                        memoryContextJson,
                        recentOutcomeContextJson,
                        out reconsiderationData);
                string deliberationRequestJson =
                    BuildPatrolDefenseDeliberationRequest(
                        result,
                        actorKnownContextJson,
                        reconsiderationEvidenceJson,
                        reconsiderationData);
                string deliberationRouteEligibilityJson =
                    BuildPatrolDefenseDeliberationRouteEligibility(
                        deliberationRequestJson);
                string providerDispatchOutboxJobJson;
                string providerDispatchJson =
                    BuildPatrolDefenseProviderDispatch(
                        deliberationRequestJson,
                        deliberationRouteEligibilityJson,
                        out providerDispatchOutboxJobJson);

                lock (IoLock)
                {
                    string receiptJson =
                        postConsumedRepeat
                            ? result.Json.Substring(0, result.Json.Length - 1) +
                                ",\"postConsumedRepeat\":true}" 
                            : result.Json;

                    if (!string.IsNullOrWhiteSpace(
                            memoryContextJson))
                    {
                        receiptJson =
                            receiptJson.Substring(
                                0,
                                receiptJson.Length - 1) +
                            ",\"memoryContext\":" +
                            memoryContextJson +
                            "}";
                    }

                    if (!string.IsNullOrWhiteSpace(
                            memoryStateComparisonJson))
                    {
                        receiptJson =
                            receiptJson.Substring(
                                0,
                                receiptJson.Length - 1) +
                            ",\"memoryStateComparison\":" +
                            memoryStateComparisonJson +
                            "}";
                    }

                    if (!string.IsNullOrWhiteSpace(
                            memoryUpdateCandidateJson))
                    {
                        receiptJson =
                            receiptJson.Substring(
                                0,
                                receiptJson.Length - 1) +
                            ",\"memoryUpdateCandidate\":" +
                            memoryUpdateCandidateJson +
                            "}";
                    }

                    if (!string.IsNullOrWhiteSpace(
                            memoryUpdateWriteJson))
                    {
                        receiptJson =
                            receiptJson.Substring(
                                0,
                                receiptJson.Length - 1) +
                            ",\"memoryUpdateWrite\":" +
                            memoryUpdateWriteJson +
                            "}";
                    }

                    receiptJson =
                        PatrolDefenseShadowContextAttachment.Attach(
                            receiptJson,
                            recentOutcomeContextJson);
                    receiptJson =
                        PatrolDefenseActorKnownContextBuilder.AttachToShadow(
                            receiptJson,
                            actorKnownContextJson);
                    receiptJson =
                        PatrolDefenseReconsiderationEvidenceBuilder.AttachToShadow(
                            receiptJson,
                            reconsiderationEvidenceJson);
                    receiptJson =
                        PatrolDefenseDeliberationRequestBuilder.AttachToShadow(
                            receiptJson,
                            deliberationRequestJson);
                    receiptJson =
                        PatrolDefenseDeliberationRouteEligibilityBuilder.AttachToShadow(
                            receiptJson,
                            deliberationRouteEligibilityJson);
                    receiptJson =
                        PatrolDefenseProviderDispatchBuilder.AttachToShadow(
                            receiptJson,
                            providerDispatchJson);

                    File.AppendAllText(
                        PatrolDefenseShadowPath,
                        receiptJson +
                        Environment.NewLine);

                    if (!string.IsNullOrWhiteSpace(
                            providerDispatchOutboxJobJson))
                    {
                        File.AppendAllText(
                            PatrolDefenseProviderOutboxPath,
                            providerDispatchOutboxJobJson +
                            Environment.NewLine);
                    }
                }

                _patrolDefenseShadowReceipts++;

                if (result.WouldInterrupt)
                    _patrolDefenseShadowWouldInterrupt++;

                WriteLog(
                    "PATROL_DEFENSE_SHADOW" +
                    " patrolSettlement=" +
                    Clean(_patrolSettlementId) +
                    " threatFound=" +
                    result.ThreatFound +
                    " wouldInterrupt=" +
                    result.WouldInterrupt +
                    " banditId=" +
                    Clean(result.BanditId) +
                    " villagerId=" +
                    Clean(result.VillagerId) +
                    " behaviorMutation=False" +
                    " intentMutation=False" +
                    " scoreMutation=False" +
                    (postConsumedRepeat
                        ? " postConsumedRepeat=True"
                        : ""));
            }
            catch (Exception ex)
            {
                WriteLog(
                    "PATROL_DEFENSE_SHADOW_FAILED" +
                    " type=" +
                    ex.GetType().Name +
                    " message=" +
                    Clean(ex.Message));
            }
        }

        private static string ReadDynastyBranchId()
        {
            try
            {
                Type seed=Type.GetType(
                    "ClanAI.DynastyMindSeed, ClanAI",
                    false);
                if (seed == null)
                    return null;

                PropertyInfo property=seed.GetProperty(
                    "BranchId",
                    BindingFlags.Static |
                    BindingFlags.NonPublic |
                    BindingFlags.Public);
                if (property == null)
                    return null;

                return property.GetValue(
                    null,
                    null) as string;
            }
            catch
            {
                return null;
            }
        }

        private static string BuildPatrolDefenseCurrentThreatJson(
            PatrolDefenseShadowResult result)
        {
            if (result == null)
                return null;

            StringBuilder sb=new StringBuilder();
            AppendPatrolDefenseThreatSnapshot(
                sb,
                result.ActorStrength,
                result.ActorHealthy,
                result.BanditStrength,
                result.BanditHealthy,
                result.ActorToBanditStrengthRatio);
            return sb.ToString();
        }

        private string BuildPatrolDefenseProviderDispatch(
            string deliberationRequestJson,
            string deliberationRouteEligibilityJson,
            out string outboxJobJson)
        {
            outboxJobJson = null;

            if (string.IsNullOrWhiteSpace(
                    deliberationRequestJson) ||
                string.IsNullOrWhiteSpace(
                    deliberationRouteEligibilityJson))
            {
                return null;
            }

            bool routeEligible;
            if (!TryExtractJsonBooleanProperty(
                    deliberationRouteEligibilityJson,
                    "routeEligible",
                    out routeEligible) ||
                !routeEligible)
            {
                return null;
            }

            string requestFingerprint = null;
            TryExtractJsonStringProperty(
                deliberationRouteEligibilityJson,
                "requestFingerprint",
                out requestFingerprint);

            PatrolDefenseProviderDispatchResult dispatch =
                _patrolDefenseProviderDispatchQueue.Enqueue(
                    requestFingerprint,
                    deliberationRequestJson,
                    DateTime.UtcNow.Ticks);

            if (dispatch != null &&
                dispatch.Enqueued &&
                dispatch.Job != null)
            {
                outboxJobJson =
                    PatrolDefenseProviderDispatchBuilder.ToOutboxJobJson(
                        dispatch.Job);
            }

            return
                PatrolDefenseProviderDispatchBuilder.ToReceiptJson(
                    dispatch,
                    requestFingerprint);
        }


        private string BuildPatrolDefenseDeliberationRouteEligibility(
            string deliberationRequestJson)
        {
            if (string.IsNullOrWhiteSpace(
                    deliberationRequestJson))
                return null;

            string requestFingerprint = null;
            TryExtractJsonStringProperty(
                deliberationRequestJson,
                "requestFingerprint",
                out requestFingerprint);

            PatrolDefenseDeliberationRouteEligibilityData route =
                _patrolDefenseDeliberationRouteTracker.Evaluate(
                    requestFingerprint);

            if (route != null &&
                route.RouteEligible)
            {
                _patrolDefenseAdvisoryRuntimeGate.Arm(
                    route.RequestFingerprint);
                _pendingPatrolDefenseDeliberationRequestJson =
                    deliberationRequestJson;
            }

            return
                PatrolDefenseDeliberationRouteEligibilityBuilder.ToJson(
                    route);
        }


        private string BuildPatrolDefenseDeliberationRequest(
            PatrolDefenseShadowResult result,
            string actorKnownContextJson,
            string reconsiderationEvidenceJson,
            PatrolDefenseReconsiderationEvidenceData reconsiderationData)
        {
            if (result == null)
                return null;

            string reason = null;
            string candidateAction = null;
            string resumeAction = null;
            bool safeToIntervene;

            if (!TryExtractJsonStringProperty(
                    result.Json,
                    "reason",
                    out reason) ||
                !TryExtractJsonStringProperty(
                    result.Json,
                    "candidateAction",
                    out candidateAction) ||
                !TryExtractJsonStringProperty(
                    result.Json,
                    "resumeAction",
                    out resumeAction) ||
                !TryExtractJsonBooleanProperty(
                    result.Json,
                    "safeToIntervene",
                    out safeToIntervene))
            {
                return null;
            }

            PatrolDefenseDeliberationRequestData request =
                PatrolDefenseDeliberationRequestBuilder.Build(
                    actorKnownContextJson,
                    reconsiderationEvidenceJson,
                    reconsiderationData,
                    result.WouldInterrupt,
                    safeToIntervene,
                    reason,
                    candidateAction,
                    resumeAction);

            return
                PatrolDefenseDeliberationRequestBuilder.ToJson(
                    request);
        }

        private static bool TryExtractJsonBooleanProperty(
            string json,
            string property,
            out bool value)
        {
            value = false;
            if (string.IsNullOrWhiteSpace(json) ||
                string.IsNullOrWhiteSpace(property))
                return false;

            string marker = "\"" + property + "\":";
            int index = json.IndexOf(
                marker,
                StringComparison.Ordinal);
            if (index < 0)
                return false;

            int start = index + marker.Length;
            while (
                start < json.Length &&
                char.IsWhiteSpace(json[start]))
                start++;

            if (
                start + 4 <= json.Length &&
                string.CompareOrdinal(
                    json,
                    start,
                    "true",
                    0,
                    4) == 0)
            {
                value = true;
                return true;
            }

            if (
                start + 5 <= json.Length &&
                string.CompareOrdinal(
                    json,
                    start,
                    "false",
                    0,
                    5) == 0)
            {
                value = false;
                return true;
            }

            return false;
        }


        private string BuildPatrolDefenseReconsiderationEvidence(
            PatrolDefenseShadowResult result,
            string memoryContextJson,
            string recentOutcomeContextJson,
            out PatrolDefenseReconsiderationEvidenceData data)
        {
            data = null;
            if (result == null)
                return null;

            string actorId = null;
            try
            {
                Hero actor = Hero.MainHero;
                actorId = actor == null
                    ? null
                    : actor.StringId;
            }
            catch
            {
                actorId = null;
            }

            string identityMaterial =
                PatrolDefenseReconsiderationEvidenceBuilder
                    .BuildIdentityMaterial(
                        actorId,
                        ReadDynastyBranchId(),
                        result.PatrolSettlementId,
                        result.BanditId);

            string threatMaterial =
                PatrolDefenseReconsiderationEvidenceBuilder
                    .BuildThreatMaterial(
                        result.ActorStrength,
                        result.ActorHealthy,
                        result.BanditStrength,
                        result.BanditHealthy,
                        result.ActorToBanditStrengthRatio);

            string persistentMemoryMaterial =
                BuildPatrolDefensePersistentMemoryReconsiderationMaterial(
                    memoryContextJson);

            string recentOutcomeMaterial =
                BuildPatrolDefenseRecentOutcomeReconsiderationMaterial(
                    recentOutcomeContextJson);

            data =
                _patrolDefenseReconsiderationTracker.Evaluate(
                    identityMaterial,
                    threatMaterial,
                    persistentMemoryMaterial,
                    recentOutcomeMaterial);

            return
                PatrolDefenseReconsiderationEvidenceBuilder.ToJson(
                    data);
        }

        private static string
            BuildPatrolDefensePersistentMemoryReconsiderationMaterial(
                string memoryContextJson)
        {
            if (string.IsNullOrWhiteSpace(memoryContextJson))
                return null;

            string recognition = null;
            string requestedSettlementId = null;
            string requestedBanditId = null;
            string priorEpisodeJson = null;

            TryExtractJsonStringProperty(
                memoryContextJson,
                "recognition",
                out recognition);
            TryExtractJsonStringProperty(
                memoryContextJson,
                "requestedSettlementId",
                out requestedSettlementId);
            TryExtractJsonStringProperty(
                memoryContextJson,
                "requestedBanditId",
                out requestedBanditId);
            TryExtractJsonObjectProperty(
                memoryContextJson,
                "priorEpisode",
                out priorEpisodeJson);

            return
                PatrolDefenseReconsiderationEvidenceBuilder
                    .BuildPersistentMemoryMaterial(
                        recognition,
                        requestedSettlementId,
                        requestedBanditId,
                        priorEpisodeJson);
        }

        private static string
            BuildPatrolDefenseRecentOutcomeReconsiderationMaterial(
                string recentOutcomeContextJson)
        {
            if (string.IsNullOrWhiteSpace(recentOutcomeContextJson))
                return null;

            string result = null;
            string eventKey = null;

            TryExtractJsonStringProperty(
                recentOutcomeContextJson,
                "result",
                out result);
            TryExtractJsonStringProperty(
                recentOutcomeContextJson,
                "eventKey",
                out eventKey);

            return
                PatrolDefenseReconsiderationEvidenceBuilder
                    .BuildRecentOutcomeMaterial(
                        result,
                        eventKey);
        }


        private static string BuildPatrolDefenseActorKnownContext(
            PatrolDefenseShadowResult result,
            string memoryContextJson,
            string memoryStateComparisonJson,
            string recentOutcomeContextJson)
        {
            if (result == null)
                return null;

            string actorId=null;
            try
            {
                Hero actor=Hero.MainHero;
                actorId=actor == null
                    ? null
                    : actor.StringId;
            }
            catch
            {
                actorId=null;
            }

            return PatrolDefenseActorKnownContextBuilder.Build(
                actorId,
                ReadDynastyBranchId(),
                result.PatrolSettlementId,
                result.BanditId,
                BuildPatrolDefenseCurrentThreatJson(result),
                memoryContextJson,
                memoryStateComparisonJson,
                recentOutcomeContextJson);
        }


        private string ObservePatrolDefenseRecentOutcomeContext(
            PatrolDefenseShadowResult result)
        {
            if (result == null ||
                string.IsNullOrWhiteSpace(result.BanditId))
                return null;

            try
            {
                Hero actor=Hero.MainHero;
                if (actor == null ||
                    string.IsNullOrWhiteSpace(actor.StringId))
                    return null;

                Type bridge=Type.GetType(
                    "ClanAI.DynastyMindOperatorBridge, ClanAI",
                    false);
                if (bridge == null)
                    return null;

                MethodInfo retrieve=bridge.GetMethod(
                    "RetrievePatrolDefenseRecentOutcomeContext",
                    BindingFlags.Public | BindingFlags.Static);
                if (retrieve == null)
                    return null;

                return retrieve.Invoke(
                    null,
                    new object[]
                    {
                        actor.StringId,
                        result.BanditId,
                        "patrol_defense_shadow"
                    }) as string;
            }
            catch (Exception ex)
            {
                WriteLog(
                    "PATROL_DEFENSE_RECENT_OUTCOME_SHADOW_FAILED type=" +
                    ex.GetType().Name +
                    " message=" +
                    Clean(ex.Message));
                return null;
            }
        }


        private string ObservePatrolDefenseRecognition(
            PatrolDefenseShadowResult result)
        {
            if (result == null ||
                !result.WouldInterrupt ||
                string.IsNullOrWhiteSpace(
                    result.PatrolSettlementId) ||
                string.IsNullOrWhiteSpace(
                    result.BanditId))
            {
                return null;
            }

            string recognitionReceipt;
            string recognitionResult =
                TryRecognizePatrolDefenseEnemy(
                    result.PatrolSettlementId,
                    result.BanditId,
                    "patrol_defense_observation:" +
                    (result.ProposalId ?? ""),
                    out recognitionReceipt);

            if (!string.Equals(
                    recognitionResult,
                    "patrol_defense_recognition_new",
                    StringComparison.Ordinal))
            {
                return recognitionReceipt;
            }

            string episodeId =
                TryRecordPatrolDefenseMemoryEvidenceWithState(
                    result,
                    "observer_new_enemy",
                    "PatrolDefenseObserver.v02115.natural_new_enemy_state");

            bool recorded =
                !string.IsNullOrWhiteSpace(episodeId) &&
                episodeId.Length == 32;
            bool duplicate =
                string.Equals(
                    episodeId,
                    "duplicate",
                    StringComparison.Ordinal);
            string acquisition =
                recorded
                    ? "RECORDED"
                    : duplicate
                        ? "DUPLICATE"
                        : "FAILED";

            StringBuilder sb = new StringBuilder();
            sb.Append("{");
            sb.Append("\"schema\":\"BannerlordAI.PatrolDefenseMemoryAcquisition.v1\"");
            sb.Append(",\"mode\":\"observe\"");
            sb.Append(",\"acquisition\":");
            sb.Append(RearmJson(acquisition));
            sb.Append(",\"recognition\":\"NEW_ENEMY\"");
            sb.Append(",\"episodeId\":");
            sb.Append(RearmJson(episodeId));
            sb.Append(",\"settlementId\":");
            sb.Append(RearmJson(result.PatrolSettlementId));
            sb.Append(",\"banditId\":");
            sb.Append(RearmJson(result.BanditId));
            sb.Append(",\"victimId\":");
            sb.Append(RearmJson(result.VillagerId));
            sb.Append(",\"proposalId\":");
            sb.Append(RearmJson(result.ProposalId));
            sb.Append(",\"proposalFingerprint\":");
            sb.Append(RearmJson(result.Fingerprint));
            sb.Append(",\"proposalCampaignHours\":");
            sb.Append(
                result.ProposalCampaignHours.ToString(
                    "R",
                    CultureInfo.InvariantCulture));
            sb.Append(",\"source\":\"PatrolDefenseObserver.v02115.natural_new_enemy_state\"");
            sb.Append(",\"threatSnapshot\":{");
            sb.Append("\"actorStrength\":");
            sb.Append(
                result.ActorStrength.ToString(
                    "R",
                    CultureInfo.InvariantCulture));
            sb.Append(",\"actorHealthy\":");
            sb.Append(
                result.ActorHealthy.ToString(
                    CultureInfo.InvariantCulture));
            sb.Append(",\"banditStrength\":");
            sb.Append(
                result.BanditStrength.ToString(
                    "R",
                    CultureInfo.InvariantCulture));
            sb.Append(",\"banditHealthy\":");
            sb.Append(
                result.BanditHealthy.ToString(
                    CultureInfo.InvariantCulture));
            sb.Append(",\"actorToBanditStrengthRatio\":");
            sb.Append(
                result.ActorToBanditStrengthRatio.ToString(
                    "R",
                    CultureInfo.InvariantCulture));
            sb.Append("}");
            sb.Append(",\"behaviorMutation\":false");
            sb.Append(",\"intentMutation\":false");
            sb.Append(",\"scoreMutation\":false");
            sb.Append(",\"nativeMovementCalls\":0");
            sb.Append("}");

            lock (IoLock)
            {
                File.AppendAllText(
                    PatrolDefenseMemoryAcquisitionPath,
                    sb.ToString() +
                    Environment.NewLine);
            }

            WriteLog(
                "PATROL_DEFENSE_MEMORY_ACQUISITION" +
                " acquisition=" + acquisition +
                " episodeId=" + Clean(episodeId) +
                " settlementId=" +
                Clean(result.PatrolSettlementId) +
                " banditId=" + Clean(result.BanditId) +
                " victimId=" + Clean(result.VillagerId) +
                " proposalId=" + Clean(result.ProposalId) +
                " actorStrength=" +
                result.ActorStrength.ToString(
                    "R",
                    CultureInfo.InvariantCulture) +
                " actorHealthy=" +
                result.ActorHealthy.ToString(
                    CultureInfo.InvariantCulture) +
                " banditStrength=" +
                result.BanditStrength.ToString(
                    "R",
                    CultureInfo.InvariantCulture) +
                " banditHealthy=" +
                result.BanditHealthy.ToString(
                    CultureInfo.InvariantCulture) +
                " actorToBanditStrengthRatio=" +
                result.ActorToBanditStrengthRatio.ToString(
                    "R",
                    CultureInfo.InvariantCulture) +
                " behaviorMutation=False" +
                " intentMutation=False" +
                " scoreMutation=False" +
                " nativeMovementCalls=0");

            return recognitionReceipt;
        }

        private static string BuildPatrolDefenseMemoryStateComparison(
            string recognitionReceipt,
            PatrolDefenseShadowResult result)
        {
            if (result == null ||
                string.IsNullOrWhiteSpace(recognitionReceipt) ||
                recognitionReceipt.IndexOf(
                    "\"recognition\":\"KNOWN_ENEMY\"",
                    StringComparison.Ordinal) < 0)
            {
                return null;
            }

            string detail;
            if (!TryExtractJsonStringProperty(
                    recognitionReceipt,
                    "detail",
                    out detail))
            {
                return null;
            }

            float priorActorStrength;
            int priorActorHealthy;
            float priorBanditStrength;
            int priorBanditHealthy;
            float priorStrengthRatio;

            if (!TryParsePatrolDefenseThreatSnapshot(
                    detail,
                    out priorActorStrength,
                    out priorActorHealthy,
                    out priorBanditStrength,
                    out priorBanditHealthy,
                    out priorStrengthRatio))
            {
                return null;
            }

            StringBuilder sb = new StringBuilder();
            sb.Append("{");
            sb.Append("\"schema\":\"BannerlordAI.PatrolDefenseThreatStateComparison.v1\"");
            sb.Append(",\"mode\":\"observe\"");

            sb.Append(",\"prior\":");
            AppendPatrolDefenseThreatSnapshot(
                sb,
                priorActorStrength,
                priorActorHealthy,
                priorBanditStrength,
                priorBanditHealthy,
                priorStrengthRatio);

            sb.Append(",\"current\":");
            AppendPatrolDefenseThreatSnapshot(
                sb,
                result.ActorStrength,
                result.ActorHealthy,
                result.BanditStrength,
                result.BanditHealthy,
                result.ActorToBanditStrengthRatio);

            sb.Append(",\"deltas\":{");
            sb.Append("\"actorStrength\":");
            AppendFiniteJsonNumber(
                sb,
                (double)result.ActorStrength -
                (double)priorActorStrength);
            sb.Append(",\"actorHealthy\":");
            sb.Append(
                (result.ActorHealthy - priorActorHealthy)
                    .ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"banditStrength\":");
            AppendFiniteJsonNumber(
                sb,
                (double)result.BanditStrength -
                (double)priorBanditStrength);
            sb.Append(",\"banditHealthy\":");
            sb.Append(
                (result.BanditHealthy - priorBanditHealthy)
                    .ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"actorToBanditStrengthRatio\":");
            AppendFiniteJsonNumber(
                sb,
                (double)result.ActorToBanditStrengthRatio -
                (double)priorStrengthRatio);
            sb.Append("}");

            sb.Append(",\"currentToPriorRatios\":{");
            sb.Append("\"actorStrength\":");
            AppendRatioJsonNumber(
                sb,
                result.ActorStrength,
                priorActorStrength);
            sb.Append(",\"actorHealthy\":");
            AppendRatioJsonNumber(
                sb,
                result.ActorHealthy,
                priorActorHealthy);
            sb.Append(",\"banditStrength\":");
            AppendRatioJsonNumber(
                sb,
                result.BanditStrength,
                priorBanditStrength);
            sb.Append(",\"banditHealthy\":");
            AppendRatioJsonNumber(
                sb,
                result.BanditHealthy,
                priorBanditHealthy);
            sb.Append(",\"actorToBanditStrengthRatio\":");
            AppendRatioJsonNumber(
                sb,
                result.ActorToBanditStrengthRatio,
                priorStrengthRatio);
            sb.Append("}");

            sb.Append(",\"interpretationApplied\":false");
            sb.Append("}");
            return sb.ToString();
        }

        private static string BuildPatrolDefenseMemoryUpdateCandidate(
            string recognitionReceipt,
            string memoryStateComparisonJson,
            PatrolDefenseShadowResult result)
        {
            if (result == null ||
                string.IsNullOrWhiteSpace(recognitionReceipt) ||
                string.IsNullOrWhiteSpace(memoryStateComparisonJson) ||
                recognitionReceipt.IndexOf(
                    "\"recognition\":\"KNOWN_ENEMY\"",
                    StringComparison.Ordinal) < 0 ||
                memoryStateComparisonJson.IndexOf(
                    "\"schema\":\"BannerlordAI.PatrolDefenseThreatStateComparison.v1\"",
                    StringComparison.Ordinal) < 0 ||
                memoryStateComparisonJson.IndexOf(
                    "\"mode\":\"observe\"",
                    StringComparison.Ordinal) < 0 ||
                memoryStateComparisonJson.IndexOf(
                    "\"interpretationApplied\":false",
                    StringComparison.Ordinal) < 0)
            {
                return null;
            }

            string requestedSettlementId;
            string requestedBanditId;
            string priorEpisodeJson;
            if (!TryExtractJsonStringProperty(
                    recognitionReceipt,
                    "requestedSettlementId",
                    out requestedSettlementId) ||
                !TryExtractJsonStringProperty(
                    recognitionReceipt,
                    "requestedBanditId",
                    out requestedBanditId) ||
                !TryExtractJsonObjectProperty(
                    recognitionReceipt,
                    "priorEpisode",
                    out priorEpisodeJson) ||
                !string.Equals(
                    requestedSettlementId,
                    result.PatrolSettlementId,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    requestedBanditId,
                    result.BanditId,
                    StringComparison.Ordinal))
            {
                return null;
            }

            string priorEpisodeId;
            string priorContextId;
            string priorSource;
            string priorDetail;
            if (!TryExtractJsonStringProperty(
                    priorEpisodeJson,
                    "id",
                    out priorEpisodeId) ||
                !TryExtractJsonStringProperty(
                    priorEpisodeJson,
                    "contextId",
                    out priorContextId) ||
                !TryExtractJsonStringProperty(
                    priorEpisodeJson,
                    "source",
                    out priorSource) ||
                !TryExtractJsonStringProperty(
                    priorEpisodeJson,
                    "detail",
                    out priorDetail))
            {
                return null;
            }

            string proposedContextId =
                "patrol-defense:" +
                result.PatrolSettlementId +
                ":" +
                result.BanditId;
            if (priorEpisodeId.Length != 32 ||
                !string.Equals(
                    priorContextId,
                    proposedContextId,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    priorSource,
                    "PatrolDefenseObserver.v02115.natural_new_enemy_state",
                    StringComparison.Ordinal))
            {
                return null;
            }

            float priorActorStrength;
            int priorActorHealthy;
            float priorBanditStrength;
            int priorBanditHealthy;
            float priorStrengthRatio;
            if (!TryParsePatrolDefenseThreatSnapshot(
                    priorDetail,
                    out priorActorStrength,
                    out priorActorHealthy,
                    out priorBanditStrength,
                    out priorBanditHealthy,
                    out priorStrengthRatio))
            {
                return null;
            }

            bool actorStrengthChanged =
                PatrolDefenseNumericChanged(
                    priorActorStrength,
                    result.ActorStrength);
            bool actorHealthyChanged =
                PatrolDefenseNumericChanged(
                    priorActorHealthy,
                    result.ActorHealthy);
            bool banditStrengthChanged =
                PatrolDefenseNumericChanged(
                    priorBanditStrength,
                    result.BanditStrength);
            bool banditHealthyChanged =
                PatrolDefenseNumericChanged(
                    priorBanditHealthy,
                    result.BanditHealthy);
            bool strengthRatioChanged =
                PatrolDefenseNumericChanged(
                    priorStrengthRatio,
                    result.ActorToBanditStrengthRatio);
            bool eligible =
                actorStrengthChanged ||
                actorHealthyChanged ||
                banditStrengthChanged ||
                banditHealthyChanged ||
                strengthRatioChanged;

            StringBuilder sb = new StringBuilder();
            sb.Append("{");
            sb.Append("\"schema\":\"BannerlordAI.PatrolDefenseMemoryUpdateCandidate.v1\"");
            sb.Append(",\"mode\":\"observe\"");
            sb.Append(",\"eligible\":");
            sb.Append(eligible ? "true" : "false");
            sb.Append(",\"priorEpisodeId\":");
            sb.Append(RearmJson(priorEpisodeId));
            sb.Append(",\"settlementId\":");
            sb.Append(RearmJson(result.PatrolSettlementId));
            sb.Append(",\"banditId\":");
            sb.Append(RearmJson(result.BanditId));
            sb.Append(",\"proposedContextId\":");
            sb.Append(RearmJson(proposedContextId));
            sb.Append(",\"proposedSource\":\"PatrolDefenseObserver.v02118.known_reencounter_memory_update_candidate\"");
            sb.Append(",\"proposedCampaignHours\":");
            AppendFiniteJsonNumber(
                sb,
                result.ProposalCampaignHours);
            sb.Append(",\"changedFields\":[");
            bool first = true;
            if (actorStrengthChanged)
            {
                sb.Append(RearmJson("actorStrength"));
                first = false;
            }
            if (actorHealthyChanged)
            {
                if (!first) sb.Append(",");
                sb.Append(RearmJson("actorHealthy"));
                first = false;
            }
            if (banditStrengthChanged)
            {
                if (!first) sb.Append(",");
                sb.Append(RearmJson("banditStrength"));
                first = false;
            }
            if (banditHealthyChanged)
            {
                if (!first) sb.Append(",");
                sb.Append(RearmJson("banditHealthy"));
                first = false;
            }
            if (strengthRatioChanged)
            {
                if (!first) sb.Append(",");
                sb.Append(RearmJson("actorToBanditStrengthRatio"));
            }
            sb.Append("]");
            sb.Append(",\"appendOnly\":true");
            sb.Append(",\"overwritePrior\":false");
            sb.Append(",\"interpretationApplied\":false");
            sb.Append(",\"behaviorMutation\":false");
            sb.Append(",\"intentMutation\":false");
            sb.Append(",\"scoreMutation\":false");
            sb.Append(",\"nativeMovementCalls\":0");
            sb.Append("}");
            return sb.ToString();
        }

        private static string TryAppendEligibleKnownPatrolDefenseMemoryUpdate(
            string recognitionReceipt,
            string memoryUpdateCandidateJson,
            PatrolDefenseShadowResult result)
        {
            if (result == null ||
                string.IsNullOrWhiteSpace(recognitionReceipt) ||
                string.IsNullOrWhiteSpace(memoryUpdateCandidateJson) ||
                recognitionReceipt.IndexOf(
                    "\"recognition\":\"KNOWN_ENEMY\"",
                    StringComparison.Ordinal) < 0 ||
                memoryUpdateCandidateJson.IndexOf(
                    "\"eligible\":true",
                    StringComparison.Ordinal) < 0)
            {
                return null;
            }

            string priorEpisodeJson;
            string priorEpisodeId;
            if (!TryExtractJsonObjectProperty(
                    recognitionReceipt,
                    "priorEpisode",
                    out priorEpisodeJson) ||
                !TryExtractJsonStringProperty(
                    priorEpisodeJson,
                    "id",
                    out priorEpisodeId) ||
                priorEpisodeId.Length != 32)
            {
                return null;
            }

            string newEpisodeId =
                TryRecordPatrolDefenseMemoryEvidenceWithState(
                    result,
                    "observer_known_enemy_changed_update",
                    "PatrolDefenseObserver.v02119.known_enemy_changed_update");

            bool recorded =
                !string.IsNullOrWhiteSpace(newEpisodeId) &&
                newEpisodeId.Length == 32;
            bool duplicate =
                string.Equals(
                    newEpisodeId,
                    "duplicate",
                    StringComparison.Ordinal);

            StringBuilder sb = new StringBuilder();
            sb.Append("{");
            sb.Append("\"schema\":\"BannerlordAI.PatrolDefenseMemoryUpdateWrite.v1\"");
            sb.Append(",\"mode\":\"observe\"");
            sb.Append(",\"write\":");
            sb.Append(RearmJson(
                recorded
                    ? "RECORDED"
                    : duplicate
                        ? "DUPLICATE"
                        : "FAILED"));
            sb.Append(",\"priorEpisodeId\":");
            sb.Append(RearmJson(priorEpisodeId));
            sb.Append(",\"newEpisodeId\":");
            sb.Append(RearmJson(newEpisodeId));
            sb.Append(",\"settlementId\":");
            sb.Append(RearmJson(result.PatrolSettlementId));
            sb.Append(",\"banditId\":");
            sb.Append(RearmJson(result.BanditId));
            sb.Append(",\"proposalId\":");
            sb.Append(RearmJson(result.ProposalId));
            sb.Append(",\"proposalCampaignHours\":");
            AppendFiniteJsonNumber(
                sb,
                result.ProposalCampaignHours);
            sb.Append(",\"source\":\"PatrolDefenseObserver.v02119.known_enemy_changed_update\"");
            sb.Append(",\"appendOnly\":true");
            sb.Append(",\"overwritePrior\":false");
            sb.Append(",\"behaviorMutation\":false");
            sb.Append(",\"intentMutation\":false");
            sb.Append(",\"scoreMutation\":false");
            sb.Append(",\"nativeMovementCalls\":0");
            sb.Append("}");
            return sb.ToString();
        }

        private static bool PatrolDefenseNumericChanged(
            double prior,
            double current)
        {
            double tolerance =
                0.00001d *
                Math.Max(
                    1d,
                    Math.Max(
                        Math.Abs(prior),
                        Math.Abs(current)));
            return Math.Abs(current - prior) > tolerance;
        }

        private static bool TryExtractJsonObjectProperty(
            string json,
            string propertyName,
            out string value)
        {
            value = null;
            if (string.IsNullOrWhiteSpace(json) ||
                string.IsNullOrWhiteSpace(propertyName))
            {
                return false;
            }

            string token =
                "\"" + propertyName + "\":";
            int tokenIndex =
                json.IndexOf(
                    token,
                    StringComparison.Ordinal);
            if (tokenIndex < 0)
                return false;

            int index = tokenIndex + token.Length;
            while (
                index < json.Length &&
                char.IsWhiteSpace(json[index]))
            {
                index++;
            }
            if (index >= json.Length ||
                json[index] != '{')
            {
                return false;
            }

            int start = index;
            int depth = 0;
            bool inString = false;
            bool escaped = false;
            for (; index < json.Length; index++)
            {
                char c = json[index];
                if (inString)
                {
                    if (escaped)
                    {
                        escaped = false;
                    }
                    else if (c == '\\')
                    {
                        escaped = true;
                    }
                    else if (c == '"')
                    {
                        inString = false;
                    }
                    continue;
                }

                if (c == '"')
                {
                    inString = true;
                }
                else if (c == '{')
                {
                    depth++;
                }
                else if (c == '}')
                {
                    depth--;
                    if (depth == 0)
                    {
                        value =
                            json.Substring(
                                start,
                                index - start + 1);
                        return true;
                    }
                }
            }
            return false;
        }

        private static void AppendPatrolDefenseThreatSnapshot(
            StringBuilder sb,
            float actorStrength,
            int actorHealthy,
            float banditStrength,
            int banditHealthy,
            float actorToBanditStrengthRatio)
        {
            sb.Append("{");
            sb.Append("\"actorStrength\":");
            AppendFiniteJsonNumber(sb, actorStrength);
            sb.Append(",\"actorHealthy\":");
            sb.Append(
                actorHealthy.ToString(
                    CultureInfo.InvariantCulture));
            sb.Append(",\"banditStrength\":");
            AppendFiniteJsonNumber(sb, banditStrength);
            sb.Append(",\"banditHealthy\":");
            sb.Append(
                banditHealthy.ToString(
                    CultureInfo.InvariantCulture));
            sb.Append(",\"actorToBanditStrengthRatio\":");
            AppendFiniteJsonNumber(
                sb,
                actorToBanditStrengthRatio);
            sb.Append("}");
        }

        private static void AppendRatioJsonNumber(
            StringBuilder sb,
            double current,
            double prior)
        {
            if (Math.Abs(prior) < 0.000000001d)
            {
                sb.Append("null");
                return;
            }

            AppendFiniteJsonNumber(
                sb,
                current / prior);
        }

        private static void AppendFiniteJsonNumber(
            StringBuilder sb,
            double value)
        {
            if (double.IsNaN(value) ||
                double.IsInfinity(value))
            {
                sb.Append("null");
                return;
            }

            sb.Append(
                value.ToString(
                    "R",
                    CultureInfo.InvariantCulture));
        }

        private static bool TryParsePatrolDefenseThreatSnapshot(
            string detail,
            out float actorStrength,
            out int actorHealthy,
            out float banditStrength,
            out int banditHealthy,
            out float actorToBanditStrengthRatio)
        {
            actorStrength = 0f;
            actorHealthy = 0;
            banditStrength = 0f;
            banditHealthy = 0;
            actorToBanditStrengthRatio = 0f;

            if (string.IsNullOrWhiteSpace(detail))
                return false;

            bool actorStrengthFound = false;
            bool actorHealthyFound = false;
            bool banditStrengthFound = false;
            bool banditHealthyFound = false;
            bool ratioFound = false;

            string[] parts = detail.Split(';');
            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i] ?? "";
                int equals = part.IndexOf('=');
                if (equals <= 0 ||
                    equals >= part.Length - 1)
                {
                    continue;
                }

                string key =
                    part.Substring(0, equals);
                string value =
                    part.Substring(equals + 1);

                if (key == "actorStrength")
                {
                    actorStrengthFound =
                        float.TryParse(
                            value,
                            NumberStyles.Float,
                            CultureInfo.InvariantCulture,
                            out actorStrength);
                }
                else if (key == "actorHealthy")
                {
                    actorHealthyFound =
                        int.TryParse(
                            value,
                            NumberStyles.Integer,
                            CultureInfo.InvariantCulture,
                            out actorHealthy);
                }
                else if (key == "banditStrength")
                {
                    banditStrengthFound =
                        float.TryParse(
                            value,
                            NumberStyles.Float,
                            CultureInfo.InvariantCulture,
                            out banditStrength);
                }
                else if (key == "banditHealthy")
                {
                    banditHealthyFound =
                        int.TryParse(
                            value,
                            NumberStyles.Integer,
                            CultureInfo.InvariantCulture,
                            out banditHealthy);
                }
                else if (
                    key ==
                    "actorToBanditStrengthRatio")
                {
                    ratioFound =
                        float.TryParse(
                            value,
                            NumberStyles.Float,
                            CultureInfo.InvariantCulture,
                            out actorToBanditStrengthRatio);
                }
            }

            return
                actorStrengthFound &&
                actorHealthyFound &&
                banditStrengthFound &&
                banditHealthyFound &&
                ratioFound;
        }

        private static bool TryExtractJsonStringProperty(
            string json,
            string propertyName,
            out string value)
        {
            value = null;
            if (string.IsNullOrWhiteSpace(json) ||
                string.IsNullOrWhiteSpace(propertyName))
            {
                return false;
            }

            string token =
                "\"" + propertyName + "\":";
            int tokenIndex =
                json.IndexOf(
                    token,
                    StringComparison.Ordinal);
            if (tokenIndex < 0)
                return false;

            int index =
                tokenIndex + token.Length;
            while (
                index < json.Length &&
                char.IsWhiteSpace(json[index]))
            {
                index++;
            }

            if (index >= json.Length ||
                json[index] != '"')
            {
                return false;
            }

            index++;
            StringBuilder sb = new StringBuilder();

            while (index < json.Length)
            {
                char c = json[index++];
                if (c == '"')
                {
                    value = sb.ToString();
                    return true;
                }

                if (c != '\\')
                {
                    sb.Append(c);
                    continue;
                }

                if (index >= json.Length)
                    return false;

                char escaped = json[index++];
                if (escaped == '"' ||
                    escaped == '\\' ||
                    escaped == '/')
                {
                    sb.Append(escaped);
                }
                else if (escaped == 'b')
                {
                    sb.Append('\b');
                }
                else if (escaped == 'f')
                {
                    sb.Append('\f');
                }
                else if (escaped == 'n')
                {
                    sb.Append('\n');
                }
                else if (escaped == 'r')
                {
                    sb.Append('\r');
                }
                else if (escaped == 't')
                {
                    sb.Append('\t');
                }
                else if (escaped == 'u')
                {
                    if (index + 4 > json.Length)
                        return false;

                    int code;
                    if (!int.TryParse(
                            json.Substring(index, 4),
                            NumberStyles.HexNumber,
                            CultureInfo.InvariantCulture,
                            out code))
                    {
                        return false;
                    }

                    sb.Append((char)code);
                    index += 4;
                }
                else
                {
                    return false;
                }
            }

            return false;
        }

        private void ResetPatrolDefenseRearmShadow()
        {
            _rearmAbsenceStartCampaignHours = double.NaN;
            _rearmBoundaryObserved = false;
            _rearmEligibleLogged = false;
        }

        private PatrolDefenseShadowResult ObservePatrolDefenseRearm(
            PatrolDefenseShadowResult result)
        {
            if (result == null ||
                !PatrolDefenseApplyGate.IsConsumed ||
                PatrolDefenseApplyGate.IsActive ||
                string.IsNullOrWhiteSpace(
                    PatrolDefenseApplyGate.ConsumedBanditId) ||
                string.IsNullOrWhiteSpace(
                    PatrolDefenseApplyGate.ConsumedSettlementId) ||
                !string.Equals(
                    result.PatrolSettlementId,
                    PatrolDefenseApplyGate.ConsumedSettlementId,
                    StringComparison.Ordinal))
            {
                return null;
            }

            double nowHours = result.ProposalCampaignHours;
            bool sameConsumedThreat =
                result.WouldInterrupt &&
                string.Equals(
                    result.BanditId,
                    PatrolDefenseApplyGate.ConsumedBanditId,
                    StringComparison.Ordinal);
            if (sameConsumedThreat)
            {
                _rearmAbsenceStartCampaignHours = double.NaN;
                _rearmBoundaryObserved = false;
                return null;
            }

            if (double.IsNaN(
                    _rearmAbsenceStartCampaignHours))
            {
                _rearmAbsenceStartCampaignHours = nowHours;
                return null;
            }

            double absenceHours = Math.Max(
                0.0,
                nowHours - _rearmAbsenceStartCampaignHours);

            if (!_rearmBoundaryObserved &&
                absenceHours >= RearmAbsenceBoundaryCampaignHours)
            {
                _rearmBoundaryObserved = true;
                AppendPatrolDefenseRearmShadow(
                    "EPISODE_BOUNDARY",
                    result,
                    absenceHours);
            }
            bool distinctThreat =
                result.WouldInterrupt &&
                !string.IsNullOrWhiteSpace(result.BanditId) &&
                !string.Equals(
                    result.BanditId,
                    PatrolDefenseApplyGate.ConsumedBanditId,
                    StringComparison.Ordinal);

            if (_rearmBoundaryObserved &&
                distinctThreat &&
                !_rearmEligibleLogged)
            {
                _rearmEligibleLogged = true;
                AppendPatrolDefenseRearmShadow(
                    "REARM_ELIGIBLE",
                    result,
                    absenceHours);
                return result;
            }

            return null;
        }

        private void AppendPatrolDefenseRearmShadow(
            string phase,
            PatrolDefenseShadowResult result,
            double absenceHours)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("{");
            sb.Append("\"schema\":\"BannerlordAI.PatrolDefenseRearmShadow.v1\",");
            sb.Append("\"phase\":");
            sb.Append(RearmJson(phase));
            sb.Append(",\"consumedBanditId\":");
            sb.Append(RearmJson(
                PatrolDefenseApplyGate.ConsumedBanditId));
            sb.Append(",\"patrolSettlementId\":");
            sb.Append(RearmJson(
                PatrolDefenseApplyGate.ConsumedSettlementId));            sb.Append(",\"absenceStartCampaignHours\":");
            sb.Append(
                _rearmAbsenceStartCampaignHours
                    .ToString(
                        "R",
                        CultureInfo.InvariantCulture));
            sb.Append(",\"currentCampaignHours\":");
            sb.Append(
                result.ProposalCampaignHours
                    .ToString(
                        "R",
                        CultureInfo.InvariantCulture));
            sb.Append(",\"absenceHours\":");
            sb.Append(
                absenceHours.ToString(
                    "R",
                    CultureInfo.InvariantCulture));
            sb.Append(",\"minimumAbsenceHours\":2");
            sb.Append(",\"currentBanditId\":");
            sb.Append(RearmJson(result.BanditId));
            sb.Append(",\"currentVictimId\":");
            sb.Append(RearmJson(result.VillagerId));
            sb.Append(",\"currentProposalId\":");
            sb.Append(RearmJson(result.ProposalId));
            sb.Append(",\"wouldInterrupt\":");
            sb.Append(result.WouldInterrupt ? "true" : "false");
            sb.Append(",\"threatFound\":");
            sb.Append(result.ThreatFound ? "true" : "false");
            sb.Append(",\"latchConsumed\":true");
            sb.Append(",\"latchMutation\":false");
            sb.Append(",\"behaviorMutation\":false");
            sb.Append(",\"intentMutation\":false");
            sb.Append(",\"scoreMutation\":false");
            sb.Append(",\"nativeMovementCalls\":0");
            sb.Append("}");

            lock (IoLock)
            {
                File.AppendAllText(
                    PatrolDefenseRearmShadowPath,
                    sb.ToString() +
                    Environment.NewLine);
            }

            WriteLog(
                "PATROL_DEFENSE_REARM_SHADOW phase=" +
                phase +
                " consumedBanditId=" +
                Clean(PatrolDefenseApplyGate.ConsumedBanditId) +
                " currentBanditId=" +
                Clean(result.BanditId) +
                " absenceHours=" +
                absenceHours.ToString(
                    "R",
                    CultureInfo.InvariantCulture) +
                " behaviorMutation=False intentMutation=False scoreMutation=False nativeMovementCalls=0");
        }
        private static string RearmJson(string value)
        {
            return
                "\"" +
                (value ?? "")
                    .Replace("\\", "\\\\")
                    .Replace("\"", "\\\"")
                    .Replace("\r", "\\r")
                    .Replace("\n", "\\n") +
                "\"";
        }


        private void TryProcessPendingSave(
            Campaign campaign)
        {
            if (!string.IsNullOrWhiteSpace(
                    _pendingSaveVerifyName))
            {
                string verifyName =
                    _pendingSaveVerifyName;

                bool exists =
                    MBSaveLoad.IsSaveGameFileExists(
                        verifyName);

                if (exists)
                {
                    _pendingSaveVerifyName = null;

                    _lastResult =
                        "operator_save_test_completed:" +
                        verifyName;

                    WriteLog(
                        "SAVE_TEST_END name=" +
                        Clean(verifyName) +
                        " exists=True");

                    WriteStatus(
                        "save_test_completed");
                }
                else if (
                    DateTime.UtcNow >=
                    _pendingSaveVerifyDeadlineUtc)
                {
                    _pendingSaveVerifyName = null;

                    _lastResult =
                        "operator_save_test_missing_after_timeout:" +
                        verifyName;

                    WriteLog(
                        "SAVE_TEST_FAILED name=" +
                        Clean(verifyName) +
                        " reason=verify_timeout");

                    WriteStatus(
                        "save_test_missing");
                }

                return;
            }

            string saveName =
                _pendingSaveName;

            if (string.IsNullOrWhiteSpace(
                    saveName))
            {
                return;
            }

            _pendingSaveName = null;

            try
            {
                if (campaign == null ||
                    campaign.SaveHandler == null)
                {
                    _lastResult =
                        "operator_save_test_no_handler";

                    WriteLog(
                        "SAVE_TEST_FAILED name=" +
                        Clean(saveName) +
                        " reason=no_save_handler");

                    return;
                }

                if (!saveName.StartsWith(
                        "ClanAI V020V PERSIST ",
                        StringComparison.Ordinal))
                {
                    _lastResult =
                        "operator_save_test_prefix_denied";

                    WriteLog(
                        "SAVE_TEST_FAILED name=" +
                        Clean(saveName) +
                        " reason=prefix_guard");

                    return;
                }

                if (MBSaveLoad.IsSaveGameFileExists(
                        saveName))
                {
                    _lastResult =
                        "operator_save_test_exists:" +
                        saveName;

                    WriteLog(
                        "SAVE_TEST_FAILED name=" +
                        Clean(saveName) +
                        " reason=already_exists");

                    return;
                }

                WriteLog(
                    "SAVE_TEST_BEGIN name=" +
                    Clean(saveName));

                campaign.SaveHandler.SaveAs(
                    saveName);

                _pendingSaveVerifyName =
                    saveName;

                _pendingSaveVerifyDeadlineUtc =
                    DateTime.UtcNow.AddSeconds(30);

                _lastResult =
                    "operator_save_test_verifying:" +
                    saveName;

                WriteLog(
                    "SAVE_TEST_RETURNED name=" +
                    Clean(saveName) +
                    " verifyDeadlineUtc=" +
                    _pendingSaveVerifyDeadlineUtc
                        .ToString("O"));

                WriteStatus(
                    "save_test_verifying");
            }
            catch (Exception ex)
            {
                _lastResult =
                    "operator_save_test_failed_" +
                    ex.GetType().Name;

                WriteLog(
                    "SAVE_TEST_FAILED name=" +
                    Clean(saveName) +
                    " type=" +
                    ex.GetType().Name +
                    " message=" +
                    Clean(ex.Message));

                WriteStatus(
                    "save_test_failed");
            }
        }

        private void TryMaintainAutomation(
            Campaign campaign)
        {
            if (_currentIncident != null)
            {
                _lastBlockers = "incident_active";

                if (campaign.TimeControlMode !=
                    CampaignTimeControlMode.Stop)
                {
                    campaign.SetTimeControlModeLock(false);
                    campaign.TimeControlMode =
                        CampaignTimeControlMode.Stop;

                    if (!_incidentPauseLogged)
                    {
                        WriteLog(
                            "INCIDENT_AUTO_PAUSE title=" +
                            Clean(
                                _currentIncident.Title
                                    ?.ToString()));

                        _incidentPauseLogged = true;
                    }
                }

                return;
            }

            _incidentPauseLogged = false;

            if (_movementIntentActive &&
                IsEscapeMenuOpen())
            {
                string closeResult =
                    CloseEscapeMenuIfOpen();

                _lastBlockers =
                    "escape_menu_open";

                WriteLog(
                    "AUTO_ESCAPE_RESOLVE result=" +
                    Clean(closeResult));

                return;
            }

            KingdomDecision
                pendingKingdomDecision =
                    GetFirstPendingKingdomDecision();

            if (pendingKingdomDecision != null)
            {
                _lastBlockers =
                    "kingdom_decision_pending";

                if (campaign.TimeControlMode !=
                    CampaignTimeControlMode.Stop)
                {
                    campaign.SetTimeControlModeLock(false);
                    campaign.TimeControlMode =
                        CampaignTimeControlMode.Stop;
                }

                return;
            }

            if (_postDecisionHold)
            {
                _lastBlockers = "post_decision_hold";

                if (campaign.TimeControlMode !=
                    CampaignTimeControlMode.Stop)
                {
                    campaign.SetTimeControlModeLock(false);
                    campaign.TimeControlMode =
                        CampaignTimeControlMode.Stop;
                }

                return;
            }

            if (_autoDesiredMode.HasValue &&
                TryResolveWhitelistedMenu(campaign))
            {
                return;
            }

            List<string> blockers =
                GetBlockers(campaign);

            _lastBlockers =
                blockers.Count == 0
                    ? "<none>"
                    : string.Join(";", blockers);

            if (!_autoDesiredMode.HasValue)
                return;

            if (blockers.Count > 0)
                return;

            if (campaign.TimeControlMode ==
                _autoDesiredMode.Value)
            {
                return;
            }

            CampaignTimeControlMode
                previousTimeControlMode =
                    campaign.TimeControlMode;

            campaign.SetTimeControlModeLock(false);
            campaign.TimeControlMode =
                _autoDesiredMode.Value;

            if (_autoDesiredMode.Value ==
                CampaignTimeControlMode
                    .UnstoppableFastForwardForPartyWaitTime)
            {
                campaign.SetTimeSpeed(4);
            }
            else if (_autoDesiredMode.Value ==
                CampaignTimeControlMode.StoppablePlay)
            {
                campaign.SetTimeSpeed(1);
            }
            else if (_autoDesiredMode.Value ==
                CampaignTimeControlMode.StoppableFastForward)
            {
                campaign.SetTimeSpeed(2);
            }

            _autoResumeCount++;

            WriteLog(
                "AUTO_RESUME count=" +
                _autoResumeCount +
                " from=" +
                previousTimeControlMode +
                " mode=" +
                campaign.TimeControlMode +
                " blockers=" +
                _lastBlockers);
        }

        private bool TryResolveWhitelistedMenu(
            Campaign campaign)
        {
            try
            {
                if (!_movementIntentActive)
                    return false;

                var context =
                    campaign?.CurrentMenuContext;

                var gameMenu =
                    context?.GameMenu;

                if (gameMenu == null)
                    return false;

                string menuId =
                    gameMenu.StringId;

                var manager =
                    campaign.GameMenuManager;

                int count =
                    manager.GetVirtualMenuOptionAmount(
                        context);

                // A loaded save may resume while the player is waiting.
                // Resolve that only when the actual active menu says so.
                if (string.Equals(
                        menuId,
                        "town_wait_menus",
                        StringComparison.Ordinal))
                {
                    for (int i = 0; i < count; i++)
                    {
                        if (!manager
                                .GetVirtualMenuOptionIsEnabled(
                                    context, i))
                        {
                            continue;
                        }

                        string text =
                            manager
                                .GetVirtualMenuOptionText(
                                    context, i)
                                ?.ToString() ??
                            string.Empty;

                        if (text.IndexOf(
                                "Stop waiting",
                                StringComparison
                                    .OrdinalIgnoreCase) < 0)
                        {
                            continue;
                        }

                        RunVirtual(
                            manager,
                            context,
                            i);

                        _lastResult =
                            "auto_stop_waiting:" +
                            menuId;

                        WriteLog(
                            "AUTO_MENU_RESOLVE menu=" +
                            Clean(menuId) +
                            " action=stop_waiting text=" +
                            Clean(text));

                        return true;
                    }

                    return false;
                }

                // If a movement intent exists while the party is still inside
                // a settlement, choose Bannerlord's own enabled Leave option.
                if (MobileParty.MainParty?
                        .CurrentSettlement != null)
                {
                    for (int i = 0; i < count; i++)
                    {
                        if (!manager
                                .GetVirtualMenuOptionIsEnabled(
                                    context, i))
                        {
                            continue;
                        }

                        if (!manager
                                .GetVirtualMenuOptionIsLeave(
                                    context, i))
                        {
                            continue;
                        }

                        string text =
                            manager
                                .GetVirtualMenuOptionText(
                                    context, i)
                                ?.ToString() ??
                            "<unknown>";

                        string optionId =
                            manager
                                .GetVirtualGameMenuOption(
                                    context, i)
                                ?.IdString ??
                            "<unknown>";

                        RunVirtual(
                            manager,
                            context,
                            i);

                        _lastResult =
                            "auto_leave_settlement:" +
                            optionId;

                        WriteLog(
                            "AUTO_MENU_RESOLVE menu=" +
                            Clean(menuId) +
                            " action=leave_settlement option=" +
                            Clean(optionId) +
                            " text=" +
                            Clean(text));

                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                WriteLog(
                    "AUTO_MENU_RESOLVE_FAILED type=" +
                    ex.GetType().Name +
                    " message=" +
                    Clean(ex.Message));

                return false;
            }
        }

        private static void RunVirtual(
            object manager,
            object context,
            int index)
        {
            if (manager == null || context == null)
                return;

            var flags =
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance;

            var runVirtualMethod =
                manager.GetType()
                    .GetMethod(
                        "RunConsequenceOfVirtualMenuOption",
                        flags);

            if (runVirtualMethod != null)
            {
                runVirtualMethod.Invoke(
                    manager,
                    new object[]
                    {
                        context,
                        index
                    });

                return;
            }

            var fallback =
                manager.GetType()
                    .GetMethods(flags)
                    .FirstOrDefault(
                        m => m.Name ==
                            "RunConsequencesOfMenuOption" &&
                            m.GetParameters().Length == 2);

            fallback?.Invoke(
                manager,
                new object[]
                {
                    context,
                    index
                });
        }

        private static string SelectCurrentMenuOptionById(
            Campaign campaign,
            string optionId)
        {
            if (campaign == null)
                return "menu_select_no_campaign";

            var context =
                campaign.CurrentMenuContext;

            if (context?.GameMenu == null)
                return "menu_select_no_active_menu";

            var manager =
                campaign.GameMenuManager;

            int count =
                manager.GetVirtualMenuOptionAmount(
                    context);

            for (int i = 0; i < count; i++)
            {
                var option =
                    manager.GetVirtualGameMenuOption(
                        context, i);

                if (!string.Equals(
                        option?.IdString,
                        optionId,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!manager
                        .GetVirtualMenuOptionIsEnabled(
                            context, i))
                {
                    return
                        "menu_select_disabled:" +
                        optionId;
                }

                string text =
                    manager
                        .GetVirtualMenuOptionText(
                            context, i)
                        ?.ToString() ??
                    "<unknown>";

                RunVirtual(
                    manager,
                    context,
                    i);

                WriteLog(
                    "MENU_SELECT option=" +
                    Clean(optionId) +
                    " text=" +
                    Clean(text));

                return
                    "menu_selected:" +
                    optionId;
            }

            return
                "menu_select_not_found:" +
                optionId;
        }

        private static string SelectCurrentMenuOptionByIndex(
            Campaign campaign,
            int index)
        {
            if (campaign == null)
                return "menu_select_no_campaign";

            var context =
                campaign.CurrentMenuContext;

            if (context?.GameMenu == null)
                return "menu_select_no_active_menu";

            var manager =
                campaign.GameMenuManager;

            int count =
                manager.GetVirtualMenuOptionAmount(
                    context);

            if (index < 0 || index >= count)
                return "menu_select_index_out_of_range";

            if (!manager
                    .GetVirtualMenuOptionIsEnabled(
                        context, index))
            {
                return
                    "menu_select_disabled_index:" +
                    index;
            }

            string optionId =
                manager
                    .GetVirtualGameMenuOption(
                        context, index)
                    ?.IdString ??
                index.ToString();

            string text =
                manager
                    .GetVirtualMenuOptionText(
                        context, index)
                    ?.ToString() ??
                "<unknown>";

            RunVirtual(
                manager,
                context,
                index);

            WriteLog(
                "MENU_SELECT index=" +
                index +
                " option=" +
                Clean(optionId) +
                " text=" +
                Clean(text));

            return
                "menu_selected:" +
                optionId;
        }

        private static bool IsEscapeMenuOpen()
        {
            try
            {
                object top =
                    ScreenManager.TopScreen;

                if (top == null)
                    return false;

                PropertyInfo property =
                    top.GetType()
                        .GetProperty(
                            "IsEscapeMenuOpened",
                            BindingFlags.Instance |
                            BindingFlags.Public |
                            BindingFlags.NonPublic);

                if (property == null ||
                    property.PropertyType != typeof(bool))
                {
                    return false;
                }

                return
                    (bool)property.GetValue(top);
            }
            catch
            {
                return false;
            }
        }

        private static string CloseEscapeMenuIfOpen()
        {
            try
            {
                object top =
                    ScreenManager.TopScreen;

                if (top == null)
                    return "escape_menu_no_top_screen";

                PropertyInfo property =
                    top.GetType()
                        .GetProperty(
                            "IsEscapeMenuOpened",
                            BindingFlags.Instance |
                            BindingFlags.Public |
                            BindingFlags.NonPublic);

                if (property == null ||
                    property.PropertyType != typeof(bool))
                {
                    return "escape_menu_state_unavailable";
                }

                bool open =
                    (bool)property.GetValue(top);

                if (!open)
                    return "escape_menu_already_closed";

                MethodInfo close = null;
                Type type =
                    top.GetType();

                while (close == null &&
                    type != null)
                {
                    close =
                        type.GetMethod(
                            "CloseEscapeMenu",
                            BindingFlags.Instance |
                            BindingFlags.Public |
                            BindingFlags.NonPublic);

                    type =
                        type.BaseType;
                }

                if (close == null)
                    return "escape_menu_close_method_missing";

                close.Invoke(
                    top,
                    null);

                return
                    IsEscapeMenuOpen()
                        ? "escape_menu_close_invoked_still_open"
                        : "escape_menu_closed";
            }
            catch (Exception ex)
            {
                WriteLog(
                    "ESCAPE_MENU_CLOSE_FAILED type=" +
                    ex.GetType().Name +
                    " message=" +
                    Clean(ex.Message));

                return
                    "escape_menu_close_failed:" +
                    ex.GetType().Name;
            }
        }

        private static string SelectIncidentOption(
            int index)
        {
            Incident incident =
                _currentIncident;

            object view =
                _currentIncidentView;

            if (incident == null)
                return "incident_select_no_active_incident";

            if (index < 0 ||
                index >= incident.NumOfOptions)
            {
                return
                    "incident_select_index_out_of_range";
            }

            string title =
                incident.Title?.ToString() ??
                "<unknown>";

            string text =
                incident.GetOptionText(index)
                    ?.ToString() ??
                "<unknown>";

            TryDynastyShadow(
                "incident_choice",
                "INCIDENT:" + title,
                title,
                text);

            var result =
                incident.InvokeOption(index);

            if (result != null)
            {
                foreach (var message in result)
                {
                    if (message != null)
                    {
                        MBInformationManager
                            .AddQuickInformation(
                                message);
                    }
                }
            }

            string choiceRecord =
                TryRecordDynastyChoice(
                    title,
                    index,
                    text);

            try
            {
                if (view != null)
                {
                    var closeMethod =
                        view.GetType()
                            .GetMethod(
                                "OnCloseView",
                                BindingFlags.Instance |
                                BindingFlags.NonPublic |
                                BindingFlags.Public);

                    if (closeMethod == null)
                    {
                        WriteLog(
                            "INCIDENT_CLOSE_METHOD_MISSING type=" +
                            Clean(view.GetType().FullName));
                    }
                    else
                    {
                        closeMethod.Invoke(
                            view,
                            null);

                        WriteLog(
                            "INCIDENT_CLOSE_INVOKED type=" +
                            Clean(view.GetType().FullName));
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLog(
                    "INCIDENT_CLOSE_FAILED type=" +
                    ex.GetType().Name +
                    " message=" +
                    Clean(ex.Message));
            }

            _postDecisionHold = true;

            WriteLog(
                "INCIDENT_SELECT title=" +
                Clean(title) +
                " index=" +
                index +
                " text=" +
                Clean(text) +
                " choiceRecord=" +
                Clean(choiceRecord));

            return
                "incident_selected:" +
                index;
        }

        private static void ReleaseStaleMapDisableAfterIncident()
        {
            try
            {
                Game game = Game.Current;
                var manager =
                    game?.GameStateManager;
                var top =
                    ScreenManager.TopScreen;

                if (manager == null ||
                    top == null ||
                    !manager.ActiveStateDisabledByUser)
                {
                    return;
                }

                bool incidentActive = false;

                try
                {
                    var property =
                        top.GetType()
                            .GetProperty(
                                "IsMapIncidentActive",
                                BindingFlags.Instance |
                                BindingFlags.Public |
                                BindingFlags.NonPublic);

                    if (property != null &&
                        property.PropertyType == typeof(bool))
                    {
                        incidentActive =
                            (bool)property.GetValue(
                                top);
                    }
                }
                catch
                {
                }

                if (incidentActive)
                    return;

                var field =
                    manager.GetType()
                        .GetField(
                            "_activeStateDisableRequests",
                            BindingFlags.Instance |
                            BindingFlags.NonPublic);

                var requests =
                    field?.GetValue(manager)
                        as System.Collections.IEnumerable;

                if (requests == null)
                    return;

                var liveTargets =
                    new List<object>();

                foreach (object item in requests)
                {
                    var weak =
                        item as System.WeakReference;

                    object target =
                        weak?.Target;

                    if (target != null)
                        liveTargets.Add(target);
                }

                if (liveTargets.Count != 1 ||
                    !object.ReferenceEquals(
                        liveTargets[0],
                        top))
                {
                    WriteLog(
                        "INCIDENT_RELEASE_GUARD_SKIP targets=" +
                        liveTargets.Count +
                        " top=" +
                        Clean(top.GetType().FullName));

                    return;
                }

                manager
                    .UnregisterActiveStateDisableRequest(
                        top);

                WriteLog(
                    "INCIDENT_RELEASE_GUARD_CLEARED owner=" +
                    Clean(top.GetType().FullName));
            }
            catch (Exception ex)
            {
                WriteLog(
                    "INCIDENT_RELEASE_GUARD_FAILED type=" +
                    ex.GetType().Name +
                    " message=" +
                    Clean(ex.Message));
            }
        }

        private static void ApplyIncidentPatch()
        {
            if (_incidentPatchApplied)
                return;

            try
            {
                var harmony =
                    new Harmony(
                        "BannerlordAI.TestRunner.IncidentBridge");

                var viewType =
                    AppDomain.CurrentDomain
                        .GetAssemblies()
                        .SelectMany(
                            a =>
                            {
                                try
                                {
                                    return a.GetTypes();
                                }
                                catch
                                {
                                    return Type.EmptyTypes;
                                }
                            })
                        .FirstOrDefault(
                            type =>
                                type.Name ==
                                "GauntletMapIncidentView");

                if (viewType == null)
                {
                    WriteLog(
                        "INCIDENT_PATCH view_not_found");
                    return;
                }

                var createLayout =
                    viewType.GetMethod(
                        "CreateLayout",
                        BindingFlags.Instance |
                        BindingFlags.NonPublic |
                        BindingFlags.Public);

                var onFinalize =
                    viewType.GetMethod(
                        "OnFinalize",
                        BindingFlags.Instance |
                        BindingFlags.NonPublic |
                        BindingFlags.Public);

                if (createLayout != null)
                {
                    harmony.Patch(
                        createLayout,
                        postfix:
                            new HarmonyMethod(
                                typeof(
                                    TestRunnerSubModule),
                                nameof(
                                    IncidentCreateLayoutPostfix)));
                }

                if (onFinalize != null)
                {
                    harmony.Patch(
                        onFinalize,
                        postfix:
                            new HarmonyMethod(
                                typeof(
                                    TestRunnerSubModule),
                                nameof(
                                    IncidentOnFinalizePostfix)));
                }

                var registerDisable =
                    AccessTools.Method(
                        typeof(GameStateManager),
                        "RegisterActiveStateDisableRequest",
                        new[] { typeof(object) });

                var unregisterDisable =
                    AccessTools.Method(
                        typeof(GameStateManager),
                        "UnregisterActiveStateDisableRequest",
                        new[] { typeof(object) });

                if (registerDisable != null)
                {
                    harmony.Patch(
                        registerDisable,
                        postfix:
                            new HarmonyMethod(
                                typeof(TestRunnerSubModule),
                                nameof(StateDisableRegisterPostfix)));
                }

                if (unregisterDisable != null)
                {
                    harmony.Patch(
                        unregisterDisable,
                        postfix:
                            new HarmonyMethod(
                                typeof(TestRunnerSubModule),
                                nameof(StateDisableUnregisterPostfix)));
                }

                _incidentPatchApplied = true;

                WriteLog(
                    "INCIDENT_PATCH applied");
            }
            catch (Exception ex)
            {
                WriteLog(
                    "INCIDENT_PATCH_FAILED type=" +
                    ex.GetType().Name +
                    " message=" +
                    Clean(ex.Message));
            }
        }

        public static void StateDisableRegisterPostfix(
            object requestingInstance)
        {
            LogMapStateDisableTransition(
                "REGISTER",
                requestingInstance);
        }

        public static void StateDisableUnregisterPostfix(
            object requestingInstance)
        {
            LogMapStateDisableTransition(
                "UNREGISTER",
                requestingInstance);
        }

        private static void LogMapStateDisableTransition(
            string action,
            object requestingInstance)
        {
            try
            {
                string requester =
                    requestingInstance
                        ?.GetType()
                        .FullName ??
                    "<null>";

                // Keep the diagnostic bounded: the known failure is a map-screen
                // disable request. Do not log unrelated state users.
                if (requester.IndexOf(
                        "MapScreen",
                        StringComparison.OrdinalIgnoreCase) < 0)
                {
                    return;
                }

                var manager =
                    Game.Current
                        ?.GameStateManager;

                var top =
                    ScreenManager.TopScreen;

                bool incidentActive = false;
                try
                {
                    var property =
                        top?.GetType()
                            .GetProperty(
                                "IsMapIncidentActive",
                                BindingFlags.Instance |
                                BindingFlags.Public |
                                BindingFlags.NonPublic);

                    if (property != null &&
                        property.PropertyType == typeof(bool))
                    {
                        incidentActive =
                            (bool)property.GetValue(top);
                    }
                }
                catch
                {
                }

                string stack =
                    Environment.StackTrace ??
                    string.Empty;

                if (stack.Length > 1800)
                    stack = stack.Substring(0, 1800);

                WriteLog(
                    "STATE_DISABLE_" + action +
                    " requester=" + Clean(requester) +
                    " activeDisabled=" +
                    (manager?.ActiveStateDisabledByUser
                        .ToString() ?? "<none>") +
                    " incidentActive=" +
                    incidentActive +
                    " incident=" +
                    Clean(
                        _currentIncident
                            ?.Title
                            ?.ToString() ??
                        "<none>") +
                    " top=" +
                    Clean(
                        top?.GetType()
                            .FullName ??
                        "<none>") +
                    " campaignHours=" +
                    (Campaign.Current == null
                        ? "<none>"
                        : CampaignTime.Now
                            .ToHours
                            .ToString(
                                "R",
                                CultureInfo.InvariantCulture)) +
                    " stack=" + Clean(stack));
            }
            catch (Exception ex)
            {
                WriteLog(
                    "STATE_DISABLE_TRACE_FAILED type=" +
                    ex.GetType().Name +
                    " message=" +
                    Clean(ex.Message));
            }
        }

        public static void IncidentCreateLayoutPostfix(
            object __instance)
        {
            try
            {
                var field =
                    __instance
                        .GetType()
                        .GetField(
                            "Incident",
                            BindingFlags.Instance |
                            BindingFlags.Public |
                            BindingFlags.NonPublic);

                var incident =
                    field?.GetValue(
                        __instance)
                    as Incident;

                if (incident == null)
                    return;

                _currentIncident =
                    incident;

                _currentIncidentView =
                    __instance;

                TryRecordDynastyIncident(
                    incident);

                WriteDecisionState();

                WriteLog(
                    "INCIDENT_OPEN title=" +
                    Clean(
                        incident.Title
                            ?.ToString()) +
                    " options=" +
                    incident.NumOfOptions);
            }
            catch (Exception ex)
            {
                WriteLog(
                    "INCIDENT_CAPTURE_FAILED type=" +
                    ex.GetType().Name +
                    " message=" +
                    Clean(ex.Message));
            }
        }

        public static void IncidentOnFinalizePostfix(
            object __instance)
        {
            try
            {
                if (!ReferenceEquals(
                        _currentIncidentView,
                        __instance))
                {
                    return;
                }

                WriteLog(
                    "INCIDENT_CLOSE title=" +
                    Clean(
                        _currentIncident
                            ?.Title
                            ?.ToString()));

                _currentIncident =
                    null;

                _currentIncidentView =
                    null;

                WriteDecisionState();
            }
            catch
            {
            }
        }

        private static void WriteDecisionState()
        {
            try
            {
                var incident =
                    _currentIncident;

                string path =
                    Root +
                    @"\decision_state.txt";

                if (incident == null)
                {
                    File.WriteAllText(
                        path,
                        "type=<none>" +
                        Environment.NewLine);

                    return;
                }

                var lines =
                    new List<string>
                    {
                        "type=incident",
                        "title=" +
                            Clean(
                                incident.Title
                                    ?.ToString()),
                        "description=" +
                            Clean(
                                incident.Description
                                    ?.ToString()),
                        "optionCount=" +
                            incident.NumOfOptions
                    };

                for (int i = 0;
                     i < incident.NumOfOptions;
                     i++)
                {
                    lines.Add(
                        "option" +
                        i +
                        "Text=" +
                        Clean(
                            incident
                                .GetOptionText(i)
                                ?.ToString()));

                    object hint =
                        null;

                    try
                    {
                        hint =
                            incident
                                .GetOptionHint(i);
                    }
                    catch
                    {
                    }

                    lines.Add(
                        "option" +
                        i +
                        "Hint=" +
                        Clean(
                            hint?.ToString()));
                }

                File.WriteAllText(
                    path,
                    string.Join(
                        Environment.NewLine,
                        lines) +
                    Environment.NewLine);
            }
            catch
            {
            }
        }

        private static List<string> GetBlockers(
            Campaign campaign)
        {
            List<string> blockers =
                new List<string>();

            if (campaign == null)
            {
                blockers.Add("no_campaign");
                return blockers;
            }

            try
            {
                if (campaign.ConversationManager != null &&
                    campaign.ConversationManager
                        .IsConversationInProgress)
                {
                    blockers.Add(
                        "conversation_active");
                }
            }
            catch
            {
            }

            try
            {
                var screen =
                    ScreenManager.TopScreen;

                if (screen != null)
                {
                    foreach (var layer in screen.Layers)
                    {
                        if (layer.Name ==
                                "MapConversation" &&
                            layer.IsActive)
                        {
                            blockers.Add(
                                "map_conversation_overlay");
                            break;
                        }
                    }

                    if (IsEscapeMenuOpen())
                    {
                        blockers.Add(
                            "escape_menu_open");
                    }

                    string screenType =
                        screen.GetType().Name;

                    if (screenType != "MapScreen" &&
                        screenType != "NavalMapScreen")
                    {
                        blockers.Add(
                            "screen_active:" +
                            screenType);
                    }
                }
            }
            catch
            {
            }

            try
            {
                if (_currentIncident != null)
                {
                    blockers.Add(
                        "incident_active");
                }
            }
            catch
            {
            }

            try
            {
                if (InformationManager
                    .IsAnyInquiryActive())
                {
                    blockers.Add(
                        "inquiry_active");
                }
            }
            catch
            {
            }

            try
            {
                if (Mission.Current != null)
                {
                    blockers.Add(
                        "mission_active:" +
                        Mission.Current.SceneName);
                }
            }
            catch
            {
            }

            try
            {
                if (campaign.CurrentMenuContext !=
                        null &&
                    campaign.CurrentMenuContext
                        .GameMenu != null)
                {
                    blockers.Add(
                        "menu_active:" +
                        campaign.CurrentMenuContext
                            .GameMenu.StringId);
                }
            }
            catch
            {
            }

            return blockers;
        }

        private static KingdomDecision
            GetFirstPendingKingdomDecision()
        {
            try
            {
                Clan playerClan = Clan.PlayerClan;
                Kingdom kingdom =
                    playerClan == null
                        ? null
                        : playerClan.Kingdom;

                if (kingdom == null ||
                    kingdom.UnresolvedDecisions == null)
                    return null;

                for (int i = 0;
                    i < kingdom.UnresolvedDecisions.Count;
                    i++)
                {
                    KingdomDecision decision =
                        kingdom.UnresolvedDecisions[i];

                    if (decision != null &&
                        decision.NeedsPlayerResolution)
                        return decision;
                }
            }
            catch
            {
            }

            return null;
        }

        private static string
            StableDecisionKey(
                KingdomDecision decision,
                KingdomElection election)
        {
            unchecked
            {
                uint h = 2166136261;

                Action<string> add =
                    value =>
                    {
                        string text =
                            value ?? "<null>";

                        for (int i = 0;
                            i < text.Length;
                            i++)
                        {
                            h ^= text[i];
                            h *= 16777619;
                        }

                        h ^= 31;
                        h *= 16777619;
                    };

                add(
                    decision == null
                        ? "<none>"
                        : decision
                            .GetType()
                            .FullName);

                add(
                    decision == null
                        ? "<none>"
                        : decision
                            .GetSupportTitle()
                            ?.ToString());

                if (election != null &&
                    election.PossibleOutcomes != null)
                {
                    add(
                        election
                            .PossibleOutcomes
                            .Count
                            .ToString(
                                CultureInfo
                                    .InvariantCulture));

                    for (int i = 0;
                        i <
                        election
                            .PossibleOutcomes
                            .Count;
                        i++)
                    {
                        DecisionOutcome outcome =
                            election
                                .PossibleOutcomes[i];

                        add(
                            outcome == null
                                ? "<none>"
                                : outcome
                                    .GetDecisionTitle()
                                    ?.ToString());

                        add(
                            outcome == null ||
                            outcome.SponsorClan == null
                                ? "<none>"
                                : outcome
                                    .SponsorClan
                                    .StringId);
                    }
                }

                return h.ToString(
                    "X8",
                    CultureInfo
                        .InvariantCulture);
            }
        }

        private static string J(string value)
        {
            string text = value ?? "";

            return "\"" +
                text
                    .Replace("\\", "\\\\")
                    .Replace("\"", "\\\"")
                    .Replace("\r", "\\r")
                    .Replace("\n", "\\n")
                    .Replace("\t", "\\t") +
                "\"";
        }

        private static string F(float value)
        {
            return value.ToString(
                "R",
                CultureInfo.InvariantCulture);
        }

        private static string OutcomeJson(
            KingdomDecision decision,
            DecisionOutcome outcome,
            int index)
        {
            float playerSupport =
                decision.DetermineSupport(
                    Clan.PlayerClan,
                    outcome);

            return "{" +
                J("index") + ":" +
                index.ToString(
                    CultureInfo
                        .InvariantCulture) +
                "," +
                J("title") + ":" +
                J(
                    outcome
                        .GetDecisionTitle()
                        ?.ToString()) +
                "," +
                J("description") + ":" +
                J(
                    outcome
                        .GetDecisionDescription()
                        ?.ToString()) +
                "," +
                J("sponsorClan") + ":" +
                J(
                    outcome.SponsorClan == null
                        ? "<none>"
                        : outcome
                            .SponsorClan
                            .StringId) +
                "," +
                J("initialSupport") + ":" +
                F(outcome.InitialSupport) +
                "," +
                J("likelihood") + ":" +
                F(outcome.Likelihood) +
                "," +
                J("merit") + ":" +
                F(outcome.Merit) +
                "," +
                J("playerNativeSupport") + ":" +
                F(playerSupport) +
                "}";
        }

        private static string
            DecisionJson(
                KingdomDecision decision,
                int index)
        {
            KingdomElection election =
                new KingdomElection(
                    decision);

            string key =
                StableDecisionKey(
                    decision,
                    election);

            List<string> outcomes =
                new List<string>();

            for (int i = 0;
                i <
                election
                    .PossibleOutcomes
                    .Count;
                i++)
            {
                outcomes.Add(
                    OutcomeJson(
                        decision,
                        election
                            .PossibleOutcomes[i],
                        i));
            }

            string proposer =
                decision.ProposerClan == null
                    ? "<none>"
                    : decision
                        .ProposerClan
                        .StringId;

            return "{" +
                J("index") + ":" +
                index.ToString(
                    CultureInfo
                        .InvariantCulture) +
                "," +
                J("key") + ":" +
                J(key) +
                "," +
                J("type") + ":" +
                J(
                    decision
                        .GetType()
                        .FullName) +
                "," +
                J("title") + ":" +
                J(
                    decision
                        .GetSupportTitle()
                        ?.ToString()) +
                "," +
                J("description") + ":" +
                J(
                    decision
                        .GetSupportDescription()
                        ?.ToString()) +
                "," +
                J("secondaryEffects") + ":" +
                J(
                    decision
                        .GetSecondaryEffects()
                        ?.ToString()) +
                "," +
                J("proposerClan") + ":" +
                J(proposer) +
                "," +
                J("needsPlayerResolution") + ":" +
                (decision.NeedsPlayerResolution
                    ? "true"
                    : "false") +
                "," +
                J("isAllowed") + ":" +
                (decision.IsAllowed()
                    ? "true"
                    : "false") +
                "," +
                J("outcomes") + ":[" +
                string.Join(
                    ",",
                    outcomes) +
                "]" +
                "}";
        }

        private string
            SnapshotKingdomDecisions(
                Campaign campaign)
        {
            try
            {
                Clan playerClan =
                    Clan.PlayerClan;
                Kingdom kingdom =
                    playerClan == null
                        ? null
                        : playerClan.Kingdom;

                if (kingdom == null)
                    return
                        "kingdom_snapshot_no_kingdom";

                List<string> decisions =
                    new List<string>();

                for (int i = 0;
                    i <
                    kingdom
                        .UnresolvedDecisions
                        .Count;
                    i++)
                {
                    KingdomDecision decision =
                        kingdom
                            .UnresolvedDecisions[i];

                    if (decision == null)
                        continue;

                    decisions.Add(
                        DecisionJson(
                            decision,
                            i));
                }

                string json =
                    "{" +
                    J("schema") + ":" +
                    J(
                        "BannerlordAI." +
                        "KingdomDecisionSnapshot." +
                        "v1") +
                    "," +
                    J("utc") + ":" +
                    J(
                        DateTime
                            .UtcNow
                            .ToString("O")) +
                    "," +
                    J("campaignHours") + ":" +
                    J(
                        CampaignTime
                            .Now
                            .ToHours
                            .ToString(
                                "R",
                                CultureInfo
                                    .InvariantCulture)) +
                    "," +
                    J("hero") + ":" +
                    J(
                        Hero.MainHero == null
                            ? "<none>"
                            : Hero
                                .MainHero
                                .StringId) +
                    "," +
                    J("clan") + ":" +
                    J(playerClan.StringId) +
                    "," +
                    J("kingdom") + ":" +
                    J(kingdom.StringId) +
                    "," +
                    J("decisionCount") + ":" +
                    decisions
                        .Count
                        .ToString(
                            CultureInfo
                                .InvariantCulture) +
                    "," +
                    J("decisions") + ":[" +
                    string.Join(
                        ",",
                        decisions) +
                    "]" +
                    "}";

                lock (IoLock)
                {
                    File.WriteAllText(
                        KingdomDecisionPath,
                        json);
                }

                return
                    "kingdom_snapshot:" +
                    decisions
                        .Count
                        .ToString(
                            CultureInfo
                                .InvariantCulture);
            }
            catch (Exception ex)
            {
                WriteLog(
                    "KINGDOM_SNAPSHOT_FAILED type=" +
                    ex.GetType().Name +
                    " message=" +
                    Clean(ex.Message));

                return
                    "kingdom_snapshot_failed_" +
                    ex.GetType().Name;
            }
        }

        private static bool
            TryParseSupportWeight(
                string raw,
                out Supporter.SupportWeights
                    weight)
        {
            weight =
                Supporter.SupportWeights
                    .StayNeutral;

            if (string.IsNullOrWhiteSpace(raw))
                return false;

            string value =
                raw.Trim()
                    .ToUpperInvariant();

            if (value == "CHOOSE")
                weight =
                    Supporter.SupportWeights
                        .Choose;
            else if (
                value == "NEUTRAL" ||
                value == "STAYNEUTRAL")
                weight =
                    Supporter.SupportWeights
                        .StayNeutral;
            else if (
                value == "SLIGHT" ||
                value == "SLIGHTLYFAVOR")
                weight =
                    Supporter.SupportWeights
                        .SlightlyFavor;
            else if (
                value == "STRONG" ||
                value == "STRONGLYFAVOR")
                weight =
                    Supporter.SupportWeights
                        .StronglyFavor;
            else if (
                value == "FULL" ||
                value == "FULLYPUSH")
                weight =
                    Supporter.SupportWeights
                        .FullyPush;
            else
                return false;

            return true;
        }

        private static string
            TryDynastyShadow(
                string decisionType,
                string decisionKey,
                string decisionTitle,
                string selectedOrTarget)
        {
            try
            {
                Type bridge =
                    Type.GetType(
                        "ClanAI.DynastyMindOperatorBridge, ClanAI",
                        false);

                if (bridge == null)
                    return "bridge_missing";

                MethodInfo method =
                    bridge.GetMethod(
                        "ObserveDeliberation",
                        BindingFlags.Public |
                        BindingFlags.Static);

                if (method == null)
                    return "method_missing";

                string receipt =
                    method.Invoke(
                        null,
                        new object[]
                        {
                            decisionType,
                            decisionKey,
                            decisionTitle,
                            selectedOrTarget
                        }) as string;

                if (string.IsNullOrWhiteSpace(
                        receipt))
                {
                    return "no_memory";
                }

                lock (IoLock)
                {
                    File.AppendAllText(
                        DynastyDeliberationReceiptPath,
                        receipt +
                        Environment.NewLine);
                }

                WriteLog(
                    "DYNASTY_SHADOW_RECEIPT" +
                    " decisionType=" +
                    Clean(decisionType) +
                    " decisionKey=" +
                    Clean(decisionKey));

                return "receipt_written";
            }
            catch (Exception ex)
            {
                WriteLog(
                    "DYNASTY_SHADOW_FAILED type=" +
                    ex.GetType().Name +
                    " message=" +
                    Clean(ex.Message));

                return
                    "failed_" +
                    ex.GetType().Name;
            }
        }

        private static string
            TryRecordDynastyIncident(
                Incident incident)
        {
            try
            {
                if (incident == null)
                    return "incident_missing";

                Type bridge =
                    Type.GetType(
                        "ClanAI.DynastyMindOperatorBridge, ClanAI",
                        false);

                if (bridge == null)
                    return "bridge_missing";

                MethodInfo method =
                    bridge.GetMethod(
                        "RecordIncidentOpened",
                        BindingFlags.Public |
                        BindingFlags.Static);

                if (method == null)
                    return "method_missing";

                string title =
                    incident.Title?.ToString() ??
                    "<unknown>";

                string result =
                    method.Invoke(
                        null,
                        new object[]
                        {
                            title,
                            incident.NumOfOptions,
                            "TestRunner.GauntletMapIncidentView.CreateLayout"
                        }) as string;

                WriteLog(
                    "DYNASTY_BRANCH_EPISODE_NOTIFY" +
                    " title=" +
                    Clean(title) +
                    " options=" +
                    incident.NumOfOptions +
                    " result=" +
                    Clean(result));

                return result ?? "no_result";
            }
            catch (Exception ex)
            {
                WriteLog(
                    "DYNASTY_BRANCH_EPISODE_NOTIFY_FAILED" +
                    " type=" +
                    ex.GetType().Name +
                    " message=" +
                    Clean(ex.Message));

                return
                    "failed_" +
                    ex.GetType().Name;
            }
        }

        private static string
            TryRetrieveDynastyBranchEpisode(
                string context)
        {
            try
            {
                Type bridge =
                    Type.GetType(
                        "ClanAI.DynastyMindOperatorBridge, ClanAI",
                        false);

                if (bridge == null)
                    return "dynasty_branch_retrieve_bridge_missing";

                MethodInfo method =
                    bridge.GetMethod(
                        "RetrieveLatestBranchEpisode",
                        BindingFlags.Public |
                        BindingFlags.Static);

                if (method == null)
                    return "dynasty_branch_retrieve_method_missing";

                string receipt =
                    method.Invoke(
                        null,
                        new object[]
                        {
                            context ?? ""
                        }) as string;

                if (string.IsNullOrWhiteSpace(receipt))
                    return "dynasty_branch_retrieve_empty";

                lock (IoLock)
                {
                    File.AppendAllText(
                        DynastyBranchRetrievalPath,
                        receipt +
                        Environment.NewLine);
                }

                WriteLog(
                    "DYNASTY_BRANCH_EPISODE_RECEIPT" +
                    " context=" +
                    Clean(context));

                return "dynasty_branch_retrieved";
            }
            catch (Exception ex)
            {
                WriteLog(
                    "DYNASTY_BRANCH_EPISODE_RETRIEVE_FAILED" +
                    " type=" +
                    ex.GetType().Name +
                    " message=" +
                    Clean(ex.Message));

                return
                    "dynasty_branch_retrieve_failed_" +
                    ex.GetType().Name;
            }
        }



        private static string TryRecordPatrolDefenseMemoryEvidenceWithState(
            PatrolDefenseShadowResult result,
            string restoreReason,
            string source)
        {
            if (result == null)
                return "state_result_missing";

            try
            {
                Type bridge =
                    Type.GetType(
                        "ClanAI.DynastyMindOperatorBridge, ClanAI",
                        false);

                if (bridge == null)
                    return "state_bridge_missing";

                MethodInfo method =
                    bridge.GetMethod(
                        "RecordPatrolDefenseEpisodeWithState",
                        BindingFlags.Public |
                        BindingFlags.Static);

                if (method == null)
                    return "state_method_missing";

                string recorded =
                    method.Invoke(
                        null,
                        new object[]
                        {
                            result.PatrolSettlementId,
                            result.BanditId,
                            result.VillagerId,
                            result.ProposalId,
                            result.Fingerprint,
                            result.ProposalCampaignHours,
                            restoreReason,
                            source,
                            result.ActorStrength,
                            result.ActorHealthy,
                            result.BanditStrength,
                            result.BanditHealthy,
                            result.ActorToBanditStrengthRatio
                        }) as string;

                return
                    string.IsNullOrWhiteSpace(recorded)
                        ? "state_empty"
                        : recorded;
            }
            catch (Exception ex)
            {
                return
                    "state_failed_" +
                    ex.GetType().Name;
            }
        }


        private static string TryRecordPatrolDefenseMemoryEvidence(
            string settlementId,
            string banditId,
            string victimId,
            string proposalId,
            string proposalFingerprint,
            double proposalCampaignHours,
            string restoreReason,
            string source)
        {
            try
            {
                Type bridge =
                    Type.GetType(
                        "ClanAI.DynastyMindOperatorBridge, ClanAI",
                        false);

                if (bridge == null)
                    return "seed_bridge_missing";

                MethodInfo method =
                    bridge.GetMethod(
                        "RecordPatrolDefenseEpisode",
                        BindingFlags.Public |
                        BindingFlags.Static);

                if (method == null)
                    return "seed_method_missing";

                string result =
                    method.Invoke(
                        null,
                        new object[]
                        {
                            settlementId,
                            banditId,
                            victimId,
                            proposalId,
                            proposalFingerprint,
                            proposalCampaignHours,
                            restoreReason,
                            source
                        }) as string;

                return
                    string.IsNullOrWhiteSpace(result)
                        ? "seed_empty"
                        : result;
            }
            catch (Exception ex)
            {
                return
                    "seed_failed_" +
                    ex.GetType().Name;
            }
        }

        private static string TrySeedRealPatrolDefenseMemories()
        {
            string first =
                TryRecordPatrolDefenseMemoryEvidence(
                    "town_ES7",
                    "southern_pirates_47910",
                    "player_faction_47784",
                    "patrol-defense|town_ES7|southern_pirates_47910|player_faction_47784|648656.76291122218",
                    "southern_pirates_47910|player_faction_47784|True",
                    648656.76291122218d,
                    "bounded_timeout",
                    "ValidationSeed.v02107.real_stage1");

            string second =
                TryRecordPatrolDefenseMemoryEvidence(
                    "town_ES7",
                    "mountain_bandits_48928",
                    "villager_empire_template_37437",
                    "patrol-defense|town_ES7|mountain_bandits_48928|villager_empire_template_37437|648740.48954619444",
                    "mountain_bandits_48928|villager_empire_template_37437|True",
                    648740.48954619444d,
                    "bounded_timeout",
                    "ValidationSeed.v02107.real_stage1");

            bool firstOk =
                !string.IsNullOrWhiteSpace(first) &&
                first.Length == 32 &&
                first.IndexOf(
                    "seed_",
                    StringComparison.Ordinal) != 0;
            bool secondOk =
                !string.IsNullOrWhiteSpace(second) &&
                second.Length == 32 &&
                second.IndexOf(
                    "seed_",
                    StringComparison.Ordinal) != 0;

            WriteLog(
                "PATROL_DEFENSE_MEMORY_SEED_REAL_V02107" +
                " first=" + Clean(first) +
                " second=" + Clean(second) +
                " firstOk=" + firstOk +
                " secondOk=" + secondOk +
                " behaviorMutation=False" +
                " intentMutation=False" +
                " scoreMutation=False" +
                " nativeMovementCalls=0");

            return
                firstOk && secondOk
                    ? "patrol_defense_memory_seeded:2"
                    : "patrol_defense_memory_seed_failed:" +
                        first + ":" + second;
        }

        private static string TryRecognizePatrolDefenseEnemy(
            string settlementId,
            string banditId,
            string context)
        {
            string ignoredReceipt;
            return TryRecognizePatrolDefenseEnemy(
                settlementId,
                banditId,
                context,
                out ignoredReceipt);
        }

        private static string TryRecognizePatrolDefenseEnemy(
            string settlementId,
            string banditId,
            string context,
            out string receiptJson)
        {
            receiptJson = null;

            try
            {
                Type bridge =
                    Type.GetType(
                        "ClanAI.DynastyMindOperatorBridge, ClanAI",
                        false);

                if (bridge == null)
                    return "patrol_recognition_bridge_missing";

                MethodInfo method =
                    bridge.GetMethod(
                        "RecognizePatrolDefenseEnemy",
                        BindingFlags.Public |
                        BindingFlags.Static);

                if (method == null)
                    return "patrol_recognition_method_missing";

                string receipt =
                    method.Invoke(
                        null,
                        new object[]
                        {
                            settlementId ?? "",
                            banditId ?? "",
                            context ?? ""
                        }) as string;

                if (string.IsNullOrWhiteSpace(receipt))
                    return "patrol_recognition_empty";

                receiptJson = receipt;

                string recognition =
                    receipt.Contains(
                        "\"recognition\":\"KNOWN_ENEMY\"")
                        ? "KNOWN_ENEMY"
                        : receipt.Contains(
                            "\"recognition\":\"NEW_ENEMY\"")
                            ? "NEW_ENEMY"
                            : "UNCLASSIFIED";

                lock (IoLock)
                {
                    File.AppendAllText(
                        PatrolDefenseRecognitionPath,
                        receipt +
                        Environment.NewLine);
                }

                WriteLog(
                    "PATROL_DEFENSE_RECOGNITION_RECEIPT" +
                    " recognition=" +
                    recognition +
                    " settlementId=" +
                    Clean(settlementId) +
                    " banditId=" +
                    Clean(banditId) +
                    " context=" +
                    Clean(context) +
                    " behaviorMutation=False" +
                    " intentMutation=False" +
                    " scoreMutation=False");

                if (recognition == "KNOWN_ENEMY")
                    return "patrol_defense_recognition_known";
                if (recognition == "NEW_ENEMY")
                    return "patrol_defense_recognition_new";

                return "patrol_defense_recognition_unclassified";
            }
            catch (Exception ex)
            {
                WriteLog(
                    "PATROL_DEFENSE_RECOGNITION_FAILED" +
                    " type=" +
                    ex.GetType().Name +
                    " message=" +
                    Clean(ex.Message));

                return
                    "patrol_recognition_failed_" +
                    ex.GetType().Name;
            }
        }

        private static string TryRecordPatrolDefenseHistoryFixture(string[] parts)
        {
            try
            {
                Type bridge = Type.GetType(
                    "ClanAI.DynastyMindOperatorBridge, ClanAI", false);
                MethodInfo method = bridge == null ? null : bridge.GetMethod(
                    "RecordPatrolDefenseHistoryFixture",
                    BindingFlags.Public | BindingFlags.Static);
                if (method == null)
                    return "patrol_history_fixture_method_missing";
                float actorStrength, banditStrength, ratio;
                int actorHealthy, banditHealthy;
                if (!float.TryParse(parts[3], System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out actorStrength) ||
                    !int.TryParse(parts[4], out actorHealthy) ||
                    !float.TryParse(parts[5], System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out banditStrength) ||
                    !int.TryParse(parts[6], out banditHealthy) ||
                    !float.TryParse(parts[7], System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out ratio))
                    return "patrol_history_fixture_parse_failed";
                string id = method.Invoke(null,new object[]{
                    parts[0],parts[1],parts[2],actorStrength,actorHealthy,
                    banditStrength,banditHealthy,ratio}) as string;
                return !string.IsNullOrWhiteSpace(id) && id.Length==32
                    ? "patrol_defense_history_fixture_recorded:"+id
                    : "patrol_history_fixture_failed:"+Clean(id);
            }
            catch(Exception ex)
            {
                return "patrol_history_fixture_exception_"+ex.GetType().Name;
            }
        }

        private static string TryRetrievePatrolDefenseRecentOutcomeContext(
            string actorId,
            string banditId,
            string context)
        {
            try
            {
                Type bridge=Type.GetType(
                    "ClanAI.DynastyMindOperatorBridge, ClanAI",false);
                if (bridge == null)
                    return "patrol_recent_outcome_bridge_missing";
                MethodInfo method=bridge.GetMethod(
                    "RetrievePatrolDefenseRecentOutcomeContext",
                    BindingFlags.Public | BindingFlags.Static);
                if (method == null)
                    return "patrol_recent_outcome_method_missing";
                string receipt=method.Invoke(
                    null,new object[]{actorId,banditId,context}) as string;
                if (string.IsNullOrWhiteSpace(receipt))
                    return "patrol_recent_outcome_receipt_missing";
                lock (IoLock)
                {
                    File.AppendAllText(
                        PatrolDefenseMemoryRetrievalPath,
                        receipt + Environment.NewLine);
                }
                WriteLog(
                    "PATROL_DEFENSE_RECENT_OUTCOME_CONTEXT_RECEIPT actorId=" +
                    Clean(actorId) + " banditId=" + Clean(banditId) +
                    " context=" + Clean(context));
                return "patrol_defense_recent_outcome_context_retrieved";
            }
            catch (Exception ex)
            {
                WriteLog(
                    "PATROL_DEFENSE_RECENT_OUTCOME_CONTEXT_FAILED type=" +
                    ex.GetType().Name + " message=" + Clean(ex.Message));
                return "patrol_recent_outcome_failed_" + ex.GetType().Name;
            }
        }

        private string TryReleasePatrolDefenseProviderJob(
            string requestFingerprint,
            string providerId,
            string attemptId)
        {
            PatrolDefenseProviderJobReleaseResult release =
                _patrolDefenseProviderDispatchQueue.ReleaseClaim(
                    requestFingerprint,
                    providerId,
                    attemptId);

            string receipt =
                PatrolDefenseProviderDispatchBuilder.ToReleaseJson(
                    release);

            lock (IoLock)
            {
                File.AppendAllText(
                    PatrolDefenseProviderJobReleasePath,
                    receipt + Environment.NewLine);
            }

            string reason =
                release == null
                    ? "REJECTED"
                    : release.Reason;

            WriteLog(
                "PATROL_DEFENSE_PROVIDER_JOB_RELEASE" +
                " requestFingerprint=" +
                Clean(requestFingerprint) +
                " providerId=" +
                Clean(providerId) +
                " attemptId=" +
                Clean(attemptId) +
                " releaseApplied=" +
                (release != null &&
                 release.ReleaseApplied
                    ? "True"
                    : "False") +
                " reason=" +
                Clean(reason) +
                " externalNetworkUsed=False" +
                " modelInvoked=False" +
                " executionAuthorized=False" +
                " behaviorMutation=False" +
                " intentMutation=False" +
                " scoreMutation=False" +
                " nativeMovementCalls=0");

            return release != null &&
                release.ReleaseApplied
                    ? "patrol_defense_provider_job_released"
                    : "patrol_defense_provider_job_release_rejected:" +
                        reason;
        }


        private string TryClaimPatrolDefenseProviderJob(
            string requestFingerprint,
            string providerId,
            string attemptId)
        {
            PatrolDefenseProviderJobClaimResult claim =
                _patrolDefenseProviderDispatchQueue.Claim(
                    requestFingerprint,
                    providerId,
                    attemptId,
                    DateTime.UtcNow.Ticks);

            string receipt =
                PatrolDefenseProviderDispatchBuilder.ToClaimJson(
                    claim);

            lock (IoLock)
            {
                File.AppendAllText(
                    PatrolDefenseProviderJobClaimPath,
                    receipt + Environment.NewLine);
            }

            string reason =
                claim == null
                    ? "REJECTED"
                    : claim.Reason;

            WriteLog(
                "PATROL_DEFENSE_PROVIDER_JOB_CLAIM" +
                " requestFingerprint=" +
                Clean(requestFingerprint) +
                " providerId=" +
                Clean(providerId) +
                " attemptId=" +
                Clean(attemptId) +
                " claimApplied=" +
                (claim != null &&
                 claim.ClaimApplied
                    ? "True"
                    : "False") +
                " idempotent=" +
                (claim != null &&
                 claim.Idempotent
                    ? "True"
                    : "False") +
                " reason=" +
                Clean(reason) +
                " externalNetworkUsed=False" +
                " modelInvoked=False" +
                " executionAuthorized=False" +
                " behaviorMutation=False" +
                " intentMutation=False" +
                " scoreMutation=False" +
                " nativeMovementCalls=0");

            if (claim != null &&
                claim.ClaimApplied)
            {
                return
                    "patrol_defense_provider_job_claimed";
            }

            if (claim != null &&
                claim.Idempotent)
            {
                return
                    "patrol_defense_provider_job_claim_idempotent";
            }

            return
                "patrol_defense_provider_job_claim_rejected:" +
                reason;
        }


        private string TryRegisterPatrolDefenseProviderRequest(
            string base64EnvelopeJson)
        {
            PatrolDefenseProviderRequestRegistrationData registration;

            try
            {
                byte[] bytes =
                    Convert.FromBase64String(
                        base64EnvelopeJson);
                string envelopeJson =
                    new UTF8Encoding(false, true)
                        .GetString(bytes);

                registration =
                    PatrolDefenseProviderRequestRegistration.Evaluate(
                        _patrolDefenseProviderDispatchQueue,
                        envelopeJson);
            }
            catch (FormatException)
            {
                registration =
                    new PatrolDefenseProviderRequestRegistrationData();
                registration.Reason =
                    "INVALID_ENVELOPE_BASE64";
            }
            catch (DecoderFallbackException)
            {
                registration =
                    new PatrolDefenseProviderRequestRegistrationData();
                registration.Reason =
                    "INVALID_ENVELOPE_UTF8";
            }

            string receipt =
                PatrolDefenseProviderRequestRegistration.ToJson(
                    registration);

            lock (IoLock)
            {
                File.AppendAllText(
                    PatrolDefenseProviderRequestRegistrationPath,
                    receipt + Environment.NewLine);
            }

            bool registered =
                registration != null &&
                registration.Registered;
            bool idempotent =
                registration != null &&
                registration.Idempotent;

            string reason =
                registration == null
                    ? "REJECTED"
                    : registration.Reason;

            WriteLog(
                "PATROL_DEFENSE_PROVIDER_REQUEST_REGISTRATION" +
                " providerRequestId=" +
                Clean(
                    registration == null
                        ? null
                        : registration.ProviderRequestId) +
                " providerId=" +
                Clean(
                    registration == null
                        ? null
                        : registration.ProviderId) +
                " modelId=" +
                Clean(
                    registration == null
                        ? null
                        : registration.ModelId) +
                " attemptId=" +
                Clean(
                    registration == null
                        ? null
                        : registration.AttemptId) +
                " requestFingerprint=" +
                Clean(
                    registration == null
                        ? null
                        : registration.RequestFingerprint) +
                " registered=" +
                (registered ? "True" : "False") +
                " idempotent=" +
                (idempotent ? "True" : "False") +
                " reason=" +
                Clean(reason) +
                " externalNetworkUsed=False" +
                " modelInvoked=False" +
                " executionAuthorized=False" +
                " behaviorMutation=False" +
                " intentMutation=False" +
                " scoreMutation=False" +
                " nativeMovementCalls=0");

            if (registered)
            {
                return
                    "patrol_defense_provider_request_registered";
            }

            if (idempotent)
            {
                return
                    "patrol_defense_provider_request_registration_idempotent";
            }

            return
                "patrol_defense_provider_request_registration_rejected:" +
                reason;
        }


        private string TryRegisterPatrolDefenseProviderTransport(
            string base64EnvelopeJson)
        {
            PatrolDefenseProviderTransportRegistrationData registration;

            try
            {
                byte[] bytes =
                    Convert.FromBase64String(
                        base64EnvelopeJson);
                string envelopeJson =
                    new UTF8Encoding(false, true)
                        .GetString(bytes);

                registration =
                    PatrolDefenseProviderTransportRegistration.Evaluate(
                        _patrolDefenseProviderDispatchQueue,
                        envelopeJson);
            }
            catch (FormatException)
            {
                registration =
                    new PatrolDefenseProviderTransportRegistrationData();
                registration.Reason =
                    "INVALID_ENVELOPE_BASE64";
            }
            catch (DecoderFallbackException)
            {
                registration =
                    new PatrolDefenseProviderTransportRegistrationData();
                registration.Reason =
                    "INVALID_ENVELOPE_UTF8";
            }

            string receipt =
                PatrolDefenseProviderTransportRegistration.ToJson(
                    registration);

            lock (IoLock)
            {
                File.AppendAllText(
                    PatrolDefenseProviderTransportRegistrationPath,
                    receipt + Environment.NewLine);
            }

            bool registered =
                registration != null &&
                registration.Registered;
            bool idempotent =
                registration != null &&
                registration.Idempotent;

            string reason =
                registration == null
                    ? "REJECTED"
                    : registration.Reason;

            WriteLog(
                "PATROL_DEFENSE_PROVIDER_TRANSPORT_REGISTRATION" +
                " transportRequestId=" +
                Clean(
                    registration == null
                        ? null
                        : registration.TransportRequestId) +
                " transportId=" +
                Clean(
                    registration == null
                        ? null
                        : registration.TransportId) +
                " providerRequestId=" +
                Clean(
                    registration == null
                        ? null
                        : registration.ProviderRequestId) +
                " requestFingerprint=" +
                Clean(
                    registration == null
                        ? null
                        : registration.RequestFingerprint) +
                " registered=" +
                (registered ? "True" : "False") +
                " idempotent=" +
                (idempotent ? "True" : "False") +
                " reason=" +
                Clean(reason) +
                " externalNetworkUsed=False" +
                " modelInvoked=False" +
                " executionAuthorized=False" +
                " behaviorMutation=False" +
                " intentMutation=False" +
                " scoreMutation=False" +
                " nativeMovementCalls=0");

            if (registered)
            {
                return
                    "patrol_defense_provider_transport_registered";
            }

            if (idempotent)
            {
                return
                    "patrol_defense_provider_transport_registration_idempotent";
            }

            return
                "patrol_defense_provider_transport_registration_rejected:" +
                reason;
        }


        private string TryRegisterPatrolDefenseProviderExecutionPolicy(
            string base64PolicyJson)
        {
            PatrolDefenseProviderExecutionPolicyRegistrationData registration;

            try
            {
                byte[] bytes =
                    Convert.FromBase64String(
                        base64PolicyJson);
                string policyJson =
                    new UTF8Encoding(false, true)
                        .GetString(bytes);

                registration =
                    PatrolDefenseProviderExecutionPolicyRegistration.Evaluate(
                        _patrolDefenseProviderDispatchQueue,
                        policyJson);
            }
            catch (FormatException)
            {
                registration =
                    new PatrolDefenseProviderExecutionPolicyRegistrationData();
                registration.Reason =
                    "INVALID_ENVELOPE_BASE64";
            }
            catch (DecoderFallbackException)
            {
                registration =
                    new PatrolDefenseProviderExecutionPolicyRegistrationData();
                registration.Reason =
                    "INVALID_ENVELOPE_UTF8";
            }

            string receipt =
                PatrolDefenseProviderExecutionPolicyRegistration.ToJson(
                    registration);

            lock (IoLock)
            {
                File.AppendAllText(
                    PatrolDefenseProviderExecutionPolicyRegistrationPath,
                    receipt + Environment.NewLine);
            }

            bool registered =
                registration != null &&
                registration.Registered;
            bool idempotent =
                registration != null &&
                registration.Idempotent;

            string reason =
                registration == null
                    ? "REJECTED"
                    : registration.Reason;

            WriteLog(
                "PATROL_DEFENSE_PROVIDER_EXECUTION_POLICY_REGISTRATION" +
                " executionPolicyId=" +
                Clean(
                    registration == null
                        ? null
                        : registration.ExecutionPolicyId) +
                " transportRequestId=" +
                Clean(
                    registration == null
                        ? null
                        : registration.TransportRequestId) +
                " providerRequestId=" +
                Clean(
                    registration == null
                        ? null
                        : registration.ProviderRequestId) +
                " providerId=" +
                Clean(
                    registration == null
                        ? null
                        : registration.ProviderId) +
                " modelId=" +
                Clean(
                    registration == null
                        ? null
                        : registration.ModelId) +
                " attemptId=" +
                Clean(
                    registration == null
                        ? null
                        : registration.AttemptId) +
                " requestFingerprint=" +
                Clean(
                    registration == null
                        ? null
                        : registration.RequestFingerprint) +
                " timeoutMs=" +
                Clean(
                    registration == null
                        ? null
                        : registration.TimeoutMs) +
                " maxAttempts=" +
                Clean(
                    registration == null
                        ? null
                        : registration.MaxAttempts) +
                " maxResultBytes=" +
                Clean(
                    registration == null
                        ? null
                        : registration.MaxResultBytes) +
                " credentialRef=" +
                Clean(
                    registration == null
                        ? null
                        : registration.CredentialRef) +
                " registered=" +
                (registered ? "True" : "False") +
                " idempotent=" +
                (idempotent ? "True" : "False") +
                " reason=" +
                Clean(reason) +
                " externalNetworkUsed=False" +
                " modelInvoked=False" +
                " executionAuthorized=False" +
                " behaviorMutation=False" +
                " intentMutation=False" +
                " scoreMutation=False" +
                " nativeMovementCalls=0");

            if (registered)
            {
                return
                    "patrol_defense_provider_execution_policy_registered";
            }

            if (idempotent)
            {
                return
                    "patrol_defense_provider_execution_policy_registration_idempotent";
            }

            return
                "patrol_defense_provider_execution_policy_registration_rejected:" +
                    reason;
        }


        private string TryAuthorizePatrolDefenseProviderTransportExecution(
            string requestFingerprint,
            string executionPolicyId)
        {
            PatrolDefenseProviderTransportExecutionAuthorizationResult authorization =
                _patrolDefenseProviderDispatchQueue.AuthorizeTransportExecution(
                    requestFingerprint,
                    executionPolicyId);

            string receipt =
                PatrolDefenseProviderDispatchBuilder.ToTransportExecutionAuthorizationJson(
                    authorization);

            lock (IoLock)
            {
                File.AppendAllText(
                    PatrolDefenseProviderTransportExecutionAuthorizationPath,
                    receipt + Environment.NewLine);
            }

            bool authorized =
                authorization != null &&
                authorization.Authorized;

            string reason =
                authorization == null
                    ? "REJECTED"
                    : authorization.Reason;

            WriteLog(
                "PATROL_DEFENSE_PROVIDER_TRANSPORT_EXECUTION_AUTHORIZATION" +
                " transportExecutionAuthorizationId=" +
                Clean(
                    authorization == null
                        ? null
                        : authorization.TransportExecutionAuthorizationId) +
                " requestFingerprint=" +
                Clean(requestFingerprint) +
                " executionPolicyId=" +
                Clean(executionPolicyId) +
                " attemptOrdinal=" +
                (authorization == null
                    ? "<null>"
                    : authorization.AttemptOrdinal.ToString(
                        CultureInfo.InvariantCulture)) +
                " attemptCount=" +
                (authorization == null
                    ? "<null>"
                    : authorization.AttemptCount.ToString(
                        CultureInfo.InvariantCulture)) +
                " maxAttempts=" +
                (authorization == null
                    ? "<null>"
                    : authorization.MaxAttempts.ToString(
                        CultureInfo.InvariantCulture)) +
                " timeoutMs=" +
                (authorization == null
                    ? "<null>"
                    : authorization.TimeoutMs.ToString(
                        CultureInfo.InvariantCulture)) +
                " authorized=" +
                (authorized ? "True" : "False") +
                " reason=" +
                Clean(reason) +
                " externalNetworkUsed=False" +
                " modelInvoked=False" +
                " executionAuthorized=False" +
                " behaviorMutation=False" +
                " intentMutation=False" +
                " scoreMutation=False" +
                " nativeMovementCalls=0");

            return authorized
                ? "patrol_defense_provider_transport_execution_authorized"
                : "patrol_defense_provider_transport_execution_rejected:" +
                    reason;
        }


        private string TryAdmitPatrolDefenseProviderTransportResultV5(
            string base64ReceiptJson,
            string base64ResultJson)
        {
            PatrolDefenseProviderTransportReceiptResultV5BindingData binding =
                null;

            try
            {
                byte[] receiptBytes =
                    Convert.FromBase64String(
                        base64ReceiptJson);
                byte[] resultBytes =
                    Convert.FromBase64String(
                        base64ResultJson);

                string receiptJson =
                    new UTF8Encoding(false, true)
                        .GetString(receiptBytes);
                string resultJson =
                    new UTF8Encoding(false, true)
                        .GetString(resultBytes);

                binding =
                    PatrolDefenseProviderTransportReceiptResultV5Binding.Evaluate(
                        _patrolDefenseAdvisoryRuntimeGate,
                        _patrolDefenseProviderDispatchQueue,
                        receiptJson,
                        resultJson,
                        resultBytes);
            }
            catch (FormatException)
            {
                binding =
                    new PatrolDefenseProviderTransportReceiptResultV5BindingData();
                binding.RejectionReasons.Add(
                    "INVALID_ENVELOPE_BASE64");
            }
            catch (DecoderFallbackException)
            {
                binding =
                    new PatrolDefenseProviderTransportReceiptResultV5BindingData();
                binding.RejectionReasons.Add(
                    "INVALID_ENVELOPE_UTF8");
            }

            PatrolDefenseProviderResultV5AdmissionData result =
                binding == null
                    ? null
                    : binding.ProviderResultAdmission;

            bool accepted =
                result != null &&
                result.ProviderResultAccepted;

            string bindingReceipt =
                PatrolDefenseProviderTransportReceiptResultV5Binding.ToJson(
                    binding);
            string resultReceipt =
                PatrolDefenseProviderResultV5Admission.ToJson(
                    result);

            string requestFingerprint =
                binding != null &&
                !string.IsNullOrWhiteSpace(
                    binding.RequestFingerprint)
                    ? binding.RequestFingerprint
                    : result == null
                        ? null
                        : result.RequestFingerprint;

            string resultStatus =
                binding != null &&
                !string.IsNullOrWhiteSpace(
                    binding.ResultStatus)
                    ? binding.ResultStatus
                    : result == null
                        ? null
                        : result.Status;

            PatrolDefenseProviderDispatchCompletionResult completion =
                _patrolDefenseProviderDispatchQueue.ApplyProviderResult(
                    requestFingerprint,
                    resultStatus,
                    accepted);

            string completionReceipt =
                PatrolDefenseProviderDispatchBuilder.ToCompletionJson(
                    completion);

            lock (IoLock)
            {
                File.AppendAllText(
                    PatrolDefenseProviderTransportResultV5BindingPath,
                    bindingReceipt + Environment.NewLine);

                if (!string.IsNullOrWhiteSpace(
                        resultReceipt))
                {
                    File.AppendAllText(
                        PatrolDefenseProviderResultV5AdmissionPath,
                        resultReceipt + Environment.NewLine);
                }

                File.AppendAllText(
                    PatrolDefenseProviderDispatchCompletionPath,
                    completionReceipt + Environment.NewLine);
            }

            if (accepted)
            {
                _pendingPatrolDefenseDeliberationRequestJson =
                    null;
            }

            string firstReason =
                binding != null &&
                binding.RejectionReasons.Count > 0
                    ? binding.RejectionReasons[0]
                    : result != null &&
                        result.RejectionReasons.Count > 0
                        ? result.RejectionReasons[0]
                        : accepted
                            ? "ACCEPTED"
                            : "REJECTED";

            WriteLog(
                "PATROL_DEFENSE_PROVIDER_TRANSPORT_RESULT_V5_ADMISSION" +
                " transportId=" +
                Clean(
                    binding == null
                        ? null
                        : binding.TransportId) +
                " transportRequestId=" +
                Clean(
                    binding == null
                        ? null
                        : binding.TransportRequestId) +
                " providerRequestId=" +
                Clean(
                    binding == null
                        ? null
                        : binding.ProviderRequestId) +
                " requestFingerprint=" +
                Clean(requestFingerprint) +
                " resultStatus=" +
                Clean(resultStatus) +
                " resultSha256=" +
                Clean(
                    binding == null
                        ? null
                        : binding.ResultSha256) +
                " computedResultSha256=" +
                Clean(
                    binding == null
                        ? null
                        : binding.ComputedResultSha256) +
                " transportReceiptMatchReason=" +
                Clean(
                    binding == null
                        ? null
                        : binding.TransportReceiptMatchReason) +
                " executionPolicyId=" +
                Clean(
                    binding == null
                        ? null
                        : binding.ExecutionPolicyId) +
                " executionPolicyMatchReason=" +
                Clean(
                    binding == null
                        ? null
                        : binding.ExecutionPolicyMatchReason) +
                " accepted=" +
                (accepted ? "True" : "False") +
                " reason=" +
                Clean(firstReason) +
                " dispatchCompletionReason=" +
                Clean(
                    completion == null
                        ? null
                        : completion.Reason) +
                " modelInvoked=False" +
                " executionAuthorized=False" +
                " behaviorMutation=False" +
                " intentMutation=False" +
                " scoreMutation=False" +
                " nativeMovementCalls=0");

            return accepted
                ? "patrol_defense_provider_transport_result_v5_admitted"
                : "patrol_defense_provider_transport_result_v5_rejected:" +
                    firstReason;
        }



        private string TryAdmitPatrolDefenseProviderTransportResultV4(
            string base64ReceiptJson,
            string base64ResultJson)
        {
            PatrolDefenseProviderTransportReceiptResultV4BindingData binding =
                null;

            try
            {
                byte[] receiptBytes =
                    Convert.FromBase64String(
                        base64ReceiptJson);
                byte[] resultBytes =
                    Convert.FromBase64String(
                        base64ResultJson);

                string receiptJson =
                    new UTF8Encoding(false, true)
                        .GetString(receiptBytes);
                string resultJson =
                    new UTF8Encoding(false, true)
                        .GetString(resultBytes);

                binding =
                    PatrolDefenseProviderTransportReceiptResultV4Binding.Evaluate(
                        _patrolDefenseAdvisoryRuntimeGate,
                        _patrolDefenseProviderDispatchQueue,
                        receiptJson,
                        resultJson,
                        resultBytes);
            }
            catch (FormatException)
            {
                binding =
                    new PatrolDefenseProviderTransportReceiptResultV4BindingData();
                binding.RejectionReasons.Add(
                    "INVALID_ENVELOPE_BASE64");
            }
            catch (DecoderFallbackException)
            {
                binding =
                    new PatrolDefenseProviderTransportReceiptResultV4BindingData();
                binding.RejectionReasons.Add(
                    "INVALID_ENVELOPE_UTF8");
            }

            PatrolDefenseProviderResultV4AdmissionData result =
                binding == null
                    ? null
                    : binding.ProviderResultAdmission;

            bool accepted =
                result != null &&
                result.ProviderResultAccepted;

            string bindingReceipt =
                PatrolDefenseProviderTransportReceiptResultV4Binding.ToJson(
                    binding);
            string resultReceipt =
                PatrolDefenseProviderResultV4Admission.ToJson(
                    result);

            string requestFingerprint =
                binding != null &&
                !string.IsNullOrWhiteSpace(
                    binding.RequestFingerprint)
                    ? binding.RequestFingerprint
                    : result == null
                        ? null
                        : result.RequestFingerprint;

            string resultStatus =
                binding != null &&
                !string.IsNullOrWhiteSpace(
                    binding.ResultStatus)
                    ? binding.ResultStatus
                    : result == null
                        ? null
                        : result.Status;

            PatrolDefenseProviderDispatchCompletionResult completion =
                _patrolDefenseProviderDispatchQueue.ApplyProviderResult(
                    requestFingerprint,
                    resultStatus,
                    accepted);

            string completionReceipt =
                PatrolDefenseProviderDispatchBuilder.ToCompletionJson(
                    completion);

            lock (IoLock)
            {
                File.AppendAllText(
                    PatrolDefenseProviderTransportResultV4BindingPath,
                    bindingReceipt + Environment.NewLine);

                if (!string.IsNullOrWhiteSpace(
                        resultReceipt))
                {
                    File.AppendAllText(
                        PatrolDefenseProviderResultV4AdmissionPath,
                        resultReceipt + Environment.NewLine);
                }

                File.AppendAllText(
                    PatrolDefenseProviderDispatchCompletionPath,
                    completionReceipt + Environment.NewLine);
            }

            if (accepted)
            {
                _pendingPatrolDefenseDeliberationRequestJson =
                    null;
            }

            string firstReason =
                binding != null &&
                binding.RejectionReasons.Count > 0
                    ? binding.RejectionReasons[0]
                    : result != null &&
                        result.RejectionReasons.Count > 0
                        ? result.RejectionReasons[0]
                        : accepted
                            ? "ACCEPTED"
                            : "REJECTED";

            WriteLog(
                "PATROL_DEFENSE_PROVIDER_TRANSPORT_RESULT_V4_ADMISSION" +
                " transportId=" +
                Clean(
                    binding == null
                        ? null
                        : binding.TransportId) +
                " transportRequestId=" +
                Clean(
                    binding == null
                        ? null
                        : binding.TransportRequestId) +
                " providerRequestId=" +
                Clean(
                    binding == null
                        ? null
                        : binding.ProviderRequestId) +
                " requestFingerprint=" +
                Clean(requestFingerprint) +
                " resultStatus=" +
                Clean(resultStatus) +
                " resultSha256=" +
                Clean(
                    binding == null
                        ? null
                        : binding.ResultSha256) +
                " computedResultSha256=" +
                Clean(
                    binding == null
                        ? null
                        : binding.ComputedResultSha256) +
                " transportReceiptMatchReason=" +
                Clean(
                    binding == null
                        ? null
                        : binding.TransportReceiptMatchReason) +
                " executionPolicyId=" +
                Clean(
                    binding == null
                        ? null
                        : binding.ExecutionPolicyId) +
                " executionPolicyMatchReason=" +
                Clean(
                    binding == null
                        ? null
                        : binding.ExecutionPolicyMatchReason) +
                " accepted=" +
                (accepted ? "True" : "False") +
                " reason=" +
                Clean(firstReason) +
                " dispatchCompletionReason=" +
                Clean(
                    completion == null
                        ? null
                        : completion.Reason) +
                " modelInvoked=False" +
                " executionAuthorized=False" +
                " behaviorMutation=False" +
                " intentMutation=False" +
                " scoreMutation=False" +
                " nativeMovementCalls=0");

            return accepted
                ? "patrol_defense_provider_transport_result_v4_admitted"
                : "patrol_defense_provider_transport_result_v4_rejected:" +
                    firstReason;
        }


        private string TryAdmitPatrolDefenseProviderTransportResult(
            string base64ReceiptJson,
            string base64ResultJson)
        {
            PatrolDefenseProviderTransportReceiptResultBindingData binding =
                null;

            try
            {
                byte[] receiptBytes =
                    Convert.FromBase64String(
                        base64ReceiptJson);
                byte[] resultBytes =
                    Convert.FromBase64String(
                        base64ResultJson);

                string receiptJson =
                    new UTF8Encoding(false, true)
                        .GetString(receiptBytes);
                string resultJson =
                    new UTF8Encoding(false, true)
                        .GetString(resultBytes);

                binding =
                    PatrolDefenseProviderTransportReceiptResultBinding.Evaluate(
                        _patrolDefenseAdvisoryRuntimeGate,
                        _patrolDefenseProviderDispatchQueue,
                        receiptJson,
                        resultJson,
                        resultBytes);
            }
            catch (FormatException)
            {
                binding =
                    new PatrolDefenseProviderTransportReceiptResultBindingData();
                binding.RejectionReasons.Add(
                    "INVALID_ENVELOPE_BASE64");
            }
            catch (DecoderFallbackException)
            {
                binding =
                    new PatrolDefenseProviderTransportReceiptResultBindingData();
                binding.RejectionReasons.Add(
                    "INVALID_ENVELOPE_UTF8");
            }

            PatrolDefenseProviderResultV3AdmissionData result =
                binding == null
                    ? null
                    : binding.ProviderResultAdmission;

            bool accepted =
                result != null &&
                result.ProviderResultAccepted;

            string bindingReceipt =
                PatrolDefenseProviderTransportReceiptResultBinding.ToJson(
                    binding);
            string resultReceipt =
                PatrolDefenseProviderResultV3Admission.ToJson(
                    result);

            string requestFingerprint =
                binding != null &&
                !string.IsNullOrWhiteSpace(
                    binding.RequestFingerprint)
                    ? binding.RequestFingerprint
                    : result == null
                        ? null
                        : result.RequestFingerprint;

            string resultStatus =
                binding != null &&
                !string.IsNullOrWhiteSpace(
                    binding.ResultStatus)
                    ? binding.ResultStatus
                    : result == null
                        ? null
                        : result.Status;

            PatrolDefenseProviderDispatchCompletionResult completion =
                _patrolDefenseProviderDispatchQueue.ApplyProviderResult(
                    requestFingerprint,
                    resultStatus,
                    accepted);

            string completionReceipt =
                PatrolDefenseProviderDispatchBuilder.ToCompletionJson(
                    completion);

            lock (IoLock)
            {
                File.AppendAllText(
                    PatrolDefenseProviderTransportResultBindingPath,
                    bindingReceipt + Environment.NewLine);

                if (!string.IsNullOrWhiteSpace(
                        resultReceipt))
                {
                    File.AppendAllText(
                        PatrolDefenseProviderResultV3AdmissionPath,
                        resultReceipt + Environment.NewLine);
                }

                File.AppendAllText(
                    PatrolDefenseProviderDispatchCompletionPath,
                    completionReceipt + Environment.NewLine);
            }

            if (accepted)
            {
                _pendingPatrolDefenseDeliberationRequestJson =
                    null;
            }

            string firstReason =
                binding != null &&
                binding.RejectionReasons.Count > 0
                    ? binding.RejectionReasons[0]
                    : result != null &&
                        result.RejectionReasons.Count > 0
                        ? result.RejectionReasons[0]
                        : accepted
                            ? "ACCEPTED"
                            : "REJECTED";

            WriteLog(
                "PATROL_DEFENSE_PROVIDER_TRANSPORT_RESULT_ADMISSION" +
                " transportId=" +
                Clean(
                    binding == null
                        ? null
                        : binding.TransportId) +
                " transportRequestId=" +
                Clean(
                    binding == null
                        ? null
                        : binding.TransportRequestId) +
                " providerRequestId=" +
                Clean(
                    binding == null
                        ? null
                        : binding.ProviderRequestId) +
                " requestFingerprint=" +
                Clean(requestFingerprint) +
                " resultStatus=" +
                Clean(resultStatus) +
                " resultSha256=" +
                Clean(
                    binding == null
                        ? null
                        : binding.ResultSha256) +
                " computedResultSha256=" +
                Clean(
                    binding == null
                        ? null
                        : binding.ComputedResultSha256) +
                " transportReceiptMatchReason=" +
                Clean(
                    binding == null
                        ? null
                        : binding.TransportReceiptMatchReason) +
                " accepted=" +
                (accepted ? "True" : "False") +
                " reason=" +
                Clean(firstReason) +
                " dispatchCompletionReason=" +
                Clean(
                    completion == null
                        ? null
                        : completion.Reason) +
                " modelInvoked=False" +
                " executionAuthorized=False" +
                " behaviorMutation=False" +
                " intentMutation=False" +
                " scoreMutation=False" +
                " nativeMovementCalls=0");

            return accepted
                ? "patrol_defense_provider_transport_result_admitted"
                : "patrol_defense_provider_transport_result_rejected:" +
                    firstReason;
        }


        private string TryAdmitPatrolDefenseProviderResultV3(
            string base64EnvelopeJson)
        {
            PatrolDefenseProviderResultV3AdmissionData result;

            try
            {
                byte[] bytes =
                    Convert.FromBase64String(
                        base64EnvelopeJson);
                string envelopeJson =
                    Encoding.UTF8.GetString(bytes);

                result =
                    PatrolDefenseProviderResultV3Admission.EvaluateRequestBound(
                        _patrolDefenseAdvisoryRuntimeGate,
                        _patrolDefenseProviderDispatchQueue,
                        envelopeJson);
            }
            catch (FormatException)
            {
                result =
                    new PatrolDefenseProviderResultV3AdmissionData();
                result.ProviderResultAccepted = false;
                result.Retryable = true;
                result.RejectionReasons.Add(
                    "INVALID_ENVELOPE_BASE64");
            }

            string receipt =
                PatrolDefenseProviderResultV3Admission.ToJson(
                    result);

            PatrolDefenseProviderDispatchCompletionResult completion =
                _patrolDefenseProviderDispatchQueue.ApplyProviderResult(
                    result == null
                        ? null
                        : result.RequestFingerprint,
                    result == null
                        ? null
                        : result.Status,
                    result != null &&
                        result.ProviderResultAccepted);

            string completionReceipt =
                PatrolDefenseProviderDispatchBuilder.ToCompletionJson(
                    completion);

            lock (IoLock)
            {
                File.AppendAllText(
                    PatrolDefenseProviderResultV3AdmissionPath,
                    receipt + Environment.NewLine);

                File.AppendAllText(
                    PatrolDefenseProviderDispatchCompletionPath,
                    completionReceipt + Environment.NewLine);
            }

            bool accepted =
                result != null &&
                result.ProviderResultAccepted;

            if (accepted)
            {
                _pendingPatrolDefenseDeliberationRequestJson =
                    null;
            }

            string firstReason =
                result != null &&
                result.RejectionReasons.Count > 0
                    ? result.RejectionReasons[0]
                    : accepted
                        ? "ACCEPTED"
                        : "REJECTED";

            WriteLog(
                "PATROL_DEFENSE_PROVIDER_RESULT_V3_ADMISSION" +
                " providerId=" +
                Clean(result == null ? null : result.ProviderId) +
                " attemptId=" +
                Clean(result == null ? null : result.AttemptId) +
                " requestFingerprint=" +
                Clean(result == null ? null : result.RequestFingerprint) +
                " providerRequestId=" +
                Clean(result == null ? null : result.ProviderRequestId) +
                " transportRequestId=" +
                Clean(result == null ? null : result.TransportRequestId) +
                " status=" +
                Clean(result == null ? null : result.Status) +
                " claimMatchReason=" +
                Clean(
                    result == null
                        ? null
                        : result.ClaimMatchReason) +
                " providerRequestMatchReason=" +
                Clean(
                    result == null
                        ? null
                        : result.ProviderRequestMatchReason) +
                " transportRequestMatchReason=" +
                Clean(
                    result == null
                        ? null
                        : result.TransportRequestMatchReason) +
                " accepted=" +
                (accepted ? "True" : "False") +
                " reason=" +
                Clean(firstReason) +
                " dispatchCompletionReason=" +
                Clean(
                    completion == null
                        ? null
                        : completion.Reason) +
                " dispatchQueueBefore=" +
                (completion == null
                    ? "<null>"
                    : completion.QueueCountBefore.ToString(
                        CultureInfo.InvariantCulture)) +
                " dispatchQueueAfter=" +
                (completion == null
                    ? "<null>"
                    : completion.QueueCountAfter.ToString(
                        CultureInfo.InvariantCulture)) +
                " modelInvoked=False" +
                " executionAuthorized=False" +
                " behaviorMutation=False" +
                " intentMutation=False" +
                " scoreMutation=False" +
                " nativeMovementCalls=0");

            return accepted
                ? "patrol_defense_provider_result_v3_admitted"
                : "patrol_defense_provider_result_v3_rejected:" +
                    firstReason;
        }


        private string TryAdmitPatrolDefenseProviderResultV2(
            string base64EnvelopeJson)
        {
            PatrolDefenseProviderResultV2AdmissionData result;

            try
            {
                byte[] bytes =
                    Convert.FromBase64String(
                        base64EnvelopeJson);
                string envelopeJson =
                    Encoding.UTF8.GetString(bytes);

                result =
                    PatrolDefenseProviderResultV2Admission.EvaluateRequestBound(
                        _patrolDefenseAdvisoryRuntimeGate,
                        _patrolDefenseProviderDispatchQueue,
                        envelopeJson);
            }
            catch (FormatException)
            {
                result =
                    new PatrolDefenseProviderResultV2AdmissionData();
                result.ProviderResultAccepted = false;
                result.Retryable = true;
                result.RejectionReasons.Add(
                    "INVALID_ENVELOPE_BASE64");
            }

            string receipt =
                PatrolDefenseProviderResultV2Admission.ToJson(
                    result);

            PatrolDefenseProviderDispatchCompletionResult completion =
                _patrolDefenseProviderDispatchQueue.ApplyProviderResult(
                    result == null
                        ? null
                        : result.RequestFingerprint,
                    result == null
                        ? null
                        : result.Status,
                    result != null &&
                        result.ProviderResultAccepted);

            string completionReceipt =
                PatrolDefenseProviderDispatchBuilder.ToCompletionJson(
                    completion);

            lock (IoLock)
            {
                File.AppendAllText(
                    PatrolDefenseProviderResultV2AdmissionPath,
                    receipt + Environment.NewLine);

                File.AppendAllText(
                    PatrolDefenseProviderDispatchCompletionPath,
                    completionReceipt + Environment.NewLine);
            }

            bool accepted =
                result != null &&
                result.ProviderResultAccepted;

            if (accepted)
            {
                _pendingPatrolDefenseDeliberationRequestJson =
                    null;
            }

            string firstReason =
                result != null &&
                result.RejectionReasons.Count > 0
                    ? result.RejectionReasons[0]
                    : accepted
                        ? "ACCEPTED"
                        : "REJECTED";

            WriteLog(
                "PATROL_DEFENSE_PROVIDER_RESULT_V2_ADMISSION" +
                " providerId=" +
                Clean(result == null ? null : result.ProviderId) +
                " attemptId=" +
                Clean(result == null ? null : result.AttemptId) +
                " requestFingerprint=" +
                Clean(result == null ? null : result.RequestFingerprint) +
                " providerRequestId=" +
                Clean(result == null ? null : result.ProviderRequestId) +
                " status=" +
                Clean(result == null ? null : result.Status) +
                " claimMatchReason=" +
                Clean(
                    result == null
                        ? null
                        : result.ClaimMatchReason) +
                " providerRequestMatchReason=" +
                Clean(
                    result == null
                        ? null
                        : result.ProviderRequestMatchReason) +
                " accepted=" +
                (accepted ? "True" : "False") +
                " reason=" +
                Clean(firstReason) +
                " dispatchCompletionReason=" +
                Clean(
                    completion == null
                        ? null
                        : completion.Reason) +
                " dispatchQueueBefore=" +
                (completion == null
                    ? "<null>"
                    : completion.QueueCountBefore.ToString(
                        CultureInfo.InvariantCulture)) +
                " dispatchQueueAfter=" +
                (completion == null
                    ? "<null>"
                    : completion.QueueCountAfter.ToString(
                        CultureInfo.InvariantCulture)) +
                " modelInvoked=False" +
                " executionAuthorized=False" +
                " behaviorMutation=False" +
                " intentMutation=False" +
                " scoreMutation=False" +
                " nativeMovementCalls=0");

            return accepted
                ? "patrol_defense_provider_result_v2_admitted"
                : "patrol_defense_provider_result_v2_rejected:" +
                    firstReason;
        }


        private string TryAdmitPatrolDefenseProviderResult(
            string base64EnvelopeJson)
        {
            PatrolDefenseProviderResultAdmissionData result;

            try
            {
                byte[] bytes =
                    Convert.FromBase64String(
                        base64EnvelopeJson);
                string envelopeJson =
                    Encoding.UTF8.GetString(bytes);

                result =
                    PatrolDefenseProviderResultAdmission.EvaluateClaimBound(
                        _patrolDefenseAdvisoryRuntimeGate,
                        _patrolDefenseProviderDispatchQueue,
                        envelopeJson);
            }
            catch (FormatException)
            {
                result =
                    new PatrolDefenseProviderResultAdmissionData();
                result.ProviderResultAccepted = false;
                result.Retryable = true;
                result.RejectionReasons.Add(
                    "INVALID_ENVELOPE_BASE64");
            }

            string receipt =
                PatrolDefenseProviderResultAdmission.ToJson(
                    result);

            PatrolDefenseProviderDispatchCompletionResult completion =
                _patrolDefenseProviderDispatchQueue.ApplyProviderResult(
                    result == null
                        ? null
                        : result.RequestFingerprint,
                    result == null
                        ? null
                        : result.Status,
                    result != null &&
                        result.ProviderResultAccepted);

            string completionReceipt =
                PatrolDefenseProviderDispatchBuilder.ToCompletionJson(
                    completion);

            lock (IoLock)
            {
                File.AppendAllText(
                    PatrolDefenseProviderResultAdmissionPath,
                    receipt + Environment.NewLine);

                File.AppendAllText(
                    PatrolDefenseProviderDispatchCompletionPath,
                    completionReceipt + Environment.NewLine);
            }

            bool accepted =
                result != null &&
                result.ProviderResultAccepted;

            if (accepted)
            {
                _pendingPatrolDefenseDeliberationRequestJson =
                    null;
            }

            string firstReason =
                result != null &&
                result.RejectionReasons.Count > 0
                    ? result.RejectionReasons[0]
                    : accepted
                        ? "ACCEPTED"
                        : "REJECTED";

            WriteLog(
                "PATROL_DEFENSE_PROVIDER_RESULT_ADMISSION" +
                " providerId=" +
                Clean(result == null ? null : result.ProviderId) +
                " attemptId=" +
                Clean(result == null ? null : result.AttemptId) +
                " requestFingerprint=" +
                Clean(result == null ? null : result.RequestFingerprint) +
                " status=" +
                Clean(result == null ? null : result.Status) +
                " claimMatchReason=" +
                Clean(
                    result == null
                        ? null
                        : result.ClaimMatchReason) +
                " accepted=" +
                (accepted ? "True" : "False") +
                " reason=" +
                Clean(firstReason) +
                " dispatchCompletionReason=" +
                Clean(
                    completion == null
                        ? null
                        : completion.Reason) +
                " dispatchQueueBefore=" +
                (completion == null
                    ? "<null>"
                    : completion.QueueCountBefore.ToString(
                        CultureInfo.InvariantCulture)) +
                " dispatchQueueAfter=" +
                (completion == null
                    ? "<null>"
                    : completion.QueueCountAfter.ToString(
                        CultureInfo.InvariantCulture)) +
                " modelInvoked=False" +
                " executionAuthorized=False" +
                " behaviorMutation=False" +
                " intentMutation=False" +
                " scoreMutation=False" +
                " nativeMovementCalls=0");

            return accepted
                ? "patrol_defense_provider_result_admitted"
                : "patrol_defense_provider_result_rejected:" +
                    firstReason;
        }


        private string TryRunPatrolDefenseMockProvider(
            string submittedRequestFingerprint,
            string disposition)
        {
            IPatrolDefenseDeliberationProvider provider =
                new PatrolDefenseMockDeliberationProvider(
                    disposition);

            PatrolDefenseProviderInvocationData invocation =
                PatrolDefenseDeliberationProviderRuntime.Invoke(
                    provider,
                    _patrolDefenseAdvisoryRuntimeGate,
                    submittedRequestFingerprint,
                    _pendingPatrolDefenseDeliberationRequestJson);

            string receipt =
                PatrolDefenseDeliberationProviderRuntime.ToJson(
                    invocation);

            lock (IoLock)
            {
                File.AppendAllText(
                    PatrolDefenseDeliberationProviderPath,
                    receipt + Environment.NewLine);
            }

            bool admitted =
                invocation != null &&
                invocation.Admission != null &&
                invocation.Admission.Admitted;

            if (admitted)
            {
                _pendingPatrolDefenseDeliberationRequestJson =
                    null;
            }

            string firstReason =
                invocation != null &&
                invocation.Admission != null &&
                invocation.Admission.RejectionReasons.Count > 0
                    ? invocation.Admission.RejectionReasons[0]
                    : admitted
                        ? "ADMITTED"
                        : "REJECTED";

            WriteLog(
                "PATROL_DEFENSE_DELIBERATION_PROVIDER_INVOCATION" +
                " providerId=" +
                Clean(
                    invocation == null
                        ? null
                        : invocation.ProviderId) +
                " providerInvoked=" +
                (invocation != null &&
                 invocation.ProviderInvoked
                    ? "True"
                    : "False") +
                " requestFingerprint=" +
                Clean(submittedRequestFingerprint) +
                " admitted=" +
                (admitted ? "True" : "False") +
                " reason=" +
                Clean(firstReason) +
                " externalNetworkUsed=False" +
                " modelInvoked=False" +
                " executionAuthorized=False" +
                " behaviorMutation=False" +
                " intentMutation=False" +
                " scoreMutation=False" +
                " nativeMovementCalls=0");

            return admitted
                ? "patrol_defense_provider_admitted"
                : "patrol_defense_provider_rejected:" +
                    firstReason;
        }


        private string TryAdmitPatrolDefenseAdvisory(
            string submittedRequestFingerprint,
            string base64Json)
        {
            PatrolDefenseDeliberationAdvisoryAdmissionData admission;

            try
            {
                byte[] bytes =
                    Convert.FromBase64String(base64Json);
                string responseJson =
                    Encoding.UTF8.GetString(bytes);

                admission =
                    _patrolDefenseAdvisoryRuntimeGate.Evaluate(
                        submittedRequestFingerprint,
                        responseJson);
            }
            catch (FormatException)
            {
                admission =
                    new PatrolDefenseDeliberationAdvisoryAdmissionData();
                admission.RequestFingerprint =
                    submittedRequestFingerprint;
                admission.Admitted = false;
                admission.RejectionReasons.Add(
                    "INVALID_BASE64");
            }

            string receipt =
                PatrolDefenseDeliberationAdvisoryAdmission.ToJson(
                    admission);

            lock (IoLock)
            {
                File.AppendAllText(
                    PatrolDefenseDeliberationAdmissionPath,
                    receipt + Environment.NewLine);
            }

            string firstReason =
                admission != null &&
                admission.RejectionReasons.Count > 0
                    ? admission.RejectionReasons[0]
                    : "REJECTED";

            WriteLog(
                "PATROL_DEFENSE_ADVISORY_ADMISSION" +
                " requestFingerprint=" +
                Clean(submittedRequestFingerprint) +
                " admitted=" +
                (admission != null &&
                 admission.Admitted
                    ? "True"
                    : "False") +
                " reason=" +
                Clean(firstReason) +
                " executionAuthorized=False" +
                " behaviorMutation=False" +
                " intentMutation=False" +
                " scoreMutation=False" +
                " nativeMovementCalls=0");

            return
                admission != null &&
                admission.Admitted
                    ? "patrol_defense_advisory_admitted"
                    : "patrol_defense_advisory_rejected:" +
                        firstReason;
        }


        private static string TryRetrievePatrolDefenseHistoryContext(
            string settlementId,
            string banditId,
            string context)
        {
            try
            {
                Type bridge = Type.GetType(
                    "ClanAI.DynastyMindOperatorBridge, ClanAI", false);
                if (bridge == null)
                    return "patrol_history_bridge_missing";
                MethodInfo method = bridge.GetMethod(
                    "RetrievePatrolDefenseHistoryContext",
                    BindingFlags.Public | BindingFlags.Static);
                if (method == null)
                    return "patrol_history_method_missing";
                string receipt = method.Invoke(
                    null, new object[] {
                        settlementId ?? "", banditId ?? "", context ?? ""
                    }) as string;
                if (string.IsNullOrWhiteSpace(receipt))
                    return "patrol_history_empty";
                lock (IoLock)
                {
                    File.AppendAllText(
                        PatrolDefenseMemoryRetrievalPath,
                        receipt + Environment.NewLine);
                }
                WriteLog(
                    "PATROL_DEFENSE_HISTORY_CONTEXT_RECEIPT settlementId=" +
                    Clean(settlementId) + " banditId=" + Clean(banditId) +
                    " context=" + Clean(context));
                return "patrol_defense_history_context_retrieved";
            }
            catch (Exception ex)
            {
                WriteLog(
                    "PATROL_DEFENSE_HISTORY_CONTEXT_FAILED type=" +
                    ex.GetType().Name + " message=" + Clean(ex.Message));
                return "patrol_history_failed_" + ex.GetType().Name;
            }
        }

        private static string TryRetrievePatrolDefenseMemory(
            string settlementId,
            string banditId,
            string context)
        {
            try
            {
                Type bridge =
                    Type.GetType(
                        "ClanAI.DynastyMindOperatorBridge, ClanAI",
                        false);

                if (bridge == null)
                    return "patrol_memory_bridge_missing";

                MethodInfo method =
                    bridge.GetMethod(
                        "RetrievePatrolDefenseEpisode",
                        BindingFlags.Public |
                        BindingFlags.Static);

                if (method == null)
                    return "patrol_memory_method_missing";

                string receipt =
                    method.Invoke(
                        null,
                        new object[]
                        {
                            settlementId ?? "",
                            banditId ?? "",
                            context ?? ""
                        }) as string;

                if (string.IsNullOrWhiteSpace(receipt))
                    return "patrol_memory_empty";

                lock (IoLock)
                {
                    File.AppendAllText(
                        PatrolDefenseMemoryRetrievalPath,
                        receipt +
                        Environment.NewLine);
                }

                WriteLog(
                    "PATROL_DEFENSE_MEMORY_RECEIPT" +
                    " settlementId=" +
                    Clean(settlementId) +
                    " banditId=" +
                    Clean(banditId) +
                    " context=" +
                    Clean(context));

                return "patrol_defense_memory_retrieved";
            }
            catch (Exception ex)
            {
                WriteLog(
                    "PATROL_DEFENSE_MEMORY_RETRIEVE_FAILED" +
                    " type=" +
                    ex.GetType().Name +
                    " message=" +
                    Clean(ex.Message));

                return
                    "patrol_memory_failed_" +
                    ex.GetType().Name;
            }
        }

        private static string
            TryRecordDynastyChoice(
                string title,
                int optionIndex,
                string optionText)
        {
            try
            {
                Type bridge =
                    Type.GetType(
                        "ClanAI.DynastyMindOperatorBridge, ClanAI",
                        false);

                if (bridge == null)
                    return "choice_bridge_missing";

                MethodInfo method =
                    bridge.GetMethod(
                        "RecordIncidentChoice",
                        BindingFlags.Public |
                        BindingFlags.Static);

                if (method == null)
                    return "choice_method_missing";

                string result =
                    method.Invoke(
                        null,
                        new object[]
                        {
                            title ?? "<unknown>",
                            optionIndex,
                            optionText ?? "<unknown>",
                            "TestRunner.Incident.InvokeOption"
                        }) as string;

                WriteLog(
                    "DYNASTY_BRANCH_CHOICE_NOTIFY" +
                    " title=" + Clean(title) +
                    " optionIndex=" + optionIndex +
                    " optionText=" + Clean(optionText) +
                    " result=" + Clean(result));

                return result ?? "choice_no_result";
            }
            catch (Exception ex)
            {
                WriteLog(
                    "DYNASTY_BRANCH_CHOICE_NOTIFY_FAILED" +
                    " type=" + ex.GetType().Name +
                    " message=" + Clean(ex.Message));

                return
                    "choice_failed_" +
                    ex.GetType().Name;
            }
        }

        private static string
            TryRetrieveDynastyBranchChoice(
                string context)
        {
            try
            {
                Type bridge =
                    Type.GetType(
                        "ClanAI.DynastyMindOperatorBridge, ClanAI",
                        false);

                if (bridge == null)
                    return "dynasty_choice_retrieve_bridge_missing";

                MethodInfo method =
                    bridge.GetMethod(
                        "RetrieveLatestBranchChoice",
                        BindingFlags.Public |
                        BindingFlags.Static);

                if (method == null)
                    return "dynasty_choice_retrieve_method_missing";

                string receipt =
                    method.Invoke(
                        null,
                        new object[]
                        {
                            context ?? ""
                        }) as string;

                if (string.IsNullOrWhiteSpace(receipt))
                    return "dynasty_choice_retrieve_empty";

                lock (IoLock)
                {
                    File.AppendAllText(
                        DynastyChoiceRetrievalPath,
                        receipt +
                        Environment.NewLine);
                }

                WriteLog(
                    "DYNASTY_BRANCH_CHOICE_RECEIPT" +
                    " context=" +
                    Clean(context));

                return "dynasty_choice_retrieved";
            }
            catch (Exception ex)
            {
                WriteLog(
                    "DYNASTY_BRANCH_CHOICE_RETRIEVE_FAILED" +
                    " type=" +
                    ex.GetType().Name +
                    " message=" +
                    Clean(ex.Message));

                return
                    "dynasty_choice_retrieve_failed_" +
                    ex.GetType().Name;
            }
        }

        private string
            ExecuteDynastyDefenseDecision(
                string settlementId)
        {
            try
            {
                MobileParty party =
                    MobileParty.MainParty;

                if (party == null)
                    return "dynasty_defense_no_main_party";

                Settlement target =
                    Settlement.All.FirstOrDefault(
                        s => string.Equals(
                            s.StringId,
                            settlementId,
                            StringComparison.OrdinalIgnoreCase));

                if (target == null)
                    return "dynasty_defense_settlement_not_found";

                bool atTarget =
                    party.CurrentSettlement != null &&
                    object.ReferenceEquals(
                        party.CurrentSettlement,
                        target);

                Type bridge =
                    Type.GetType(
                        "ClanAI.DynastyMindOperatorBridge, ClanAI",
                        false);

                if (bridge == null)
                    return "dynasty_defense_bridge_missing";

                MethodInfo method =
                    bridge.GetMethod(
                        "EvaluateSettlementDefense",
                        BindingFlags.Public |
                        BindingFlags.Static);

                if (method == null)
                    return "dynasty_defense_method_missing";

                string receipt =
                    method.Invoke(
                        null,
                        new object[]
                        {
                            target.Name.ToString(),
                            atTarget
                        }) as string;

                if (string.IsNullOrWhiteSpace(receipt))
                    return "dynasty_defense_empty_receipt";

                lock (IoLock)
                {
                    File.AppendAllText(
                        DynastyCausalReceiptPath,
                        receipt +
                        Environment.NewLine);
                }

                bool applied =
                    receipt.IndexOf(
                        "\"applied\":true",
                        StringComparison.Ordinal) >= 0;

                bool finalPatrol =
                    receipt.IndexOf(
                        "\"finalChoice\":\"PATROL\"",
                        StringComparison.Ordinal) >= 0;

                bool finalHold =
                    receipt.IndexOf(
                        "\"finalChoice\":\"HOLD\"",
                        StringComparison.Ordinal) >= 0;

                bool wouldFlip =
                    receipt.IndexOf(
                        "\"wouldFlip\":true",
                        StringComparison.Ordinal) >= 0;

                WriteLog(
                    "DYNASTY_DEFENSE_DECISION" +
                    " settlement=" +
                    Clean(target.StringId) +
                    " atTarget=" +
                    atTarget +
                    " wouldFlip=" +
                    wouldFlip +
                    " applied=" +
                    applied +
                    " finalPatrol=" +
                    finalPatrol);

                if (!applied)
                {
                    return
                        "dynasty_defense_observed:" +
                        (wouldFlip
                            ? "would_flip"
                            : "no_flip");
                }

                if (finalPatrol)
                {
                    party.SetMovePatrolAroundSettlement(
                        target,
                        MobileParty.NavigationType.Default,
                        false);

                    _movementIntentActive = true;

                    return
                        "dynasty_defense_applied:PATROL:" +
                        target.StringId;
                }

                if (finalHold)
                {
                    party.SetMoveModeHold();
                    _movementIntentActive = false;

                    return
                        "dynasty_defense_applied:HOLD:" +
                        target.StringId;
                }

                return "dynasty_defense_unknown_final_choice";
            }
            catch (Exception ex)
            {
                WriteLog(
                    "DYNASTY_DEFENSE_FAILED type=" +
                    ex.GetType().Name +
                    " message=" +
                    Clean(ex.Message));

                return
                    "dynasty_defense_failed_" +
                    ex.GetType().Name;
            }
        }

        private static void
            AppendVoteReceipt(
                string json)
        {
            lock (IoLock)
            {
                File.AppendAllText(
                    KingdomVoteReceiptPath,
                    json +
                    Environment.NewLine);
            }
        }

        private string
            ExecuteKingdomVote(
                Campaign campaign,
                string raw)
        {
            try
            {
                string[] parts =
                    (raw ?? "")
                        .Split(
                            new[] { ' ' },
                            StringSplitOptions
                                .RemoveEmptyEntries);

                if (parts.Length != 4)
                {
                    return
                        "kingdom_vote_usage:" +
                        "<decisionIndex> " +
                        "<decisionKey> " +
                        "<outcomeIndex|ABSTAIN> " +
                        "<weight>";
                }

                if (!int.TryParse(
                    parts[0],
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out int decisionIndex))
                {
                    return
                        "kingdom_vote_bad_decision_index";
                }

                Clan playerClan =
                    Clan.PlayerClan;
                Kingdom kingdom =
                    playerClan == null
                        ? null
                        : playerClan.Kingdom;

                if (kingdom == null)
                    return
                        "kingdom_vote_no_kingdom";

                if (decisionIndex < 0 ||
                    decisionIndex >=
                    kingdom
                        .UnresolvedDecisions
                        .Count)
                {
                    return
                        "kingdom_vote_decision_index_oob";
                }

                KingdomDecision decision =
                    kingdom
                        .UnresolvedDecisions[
                            decisionIndex];

                if (decision == null ||
                    !decision
                        .NeedsPlayerResolution)
                {
                    return
                        "kingdom_vote_not_player_pending";
                }

                KingdomElection election =
                    new KingdomElection(
                        decision);

                string currentKey =
                    StableDecisionKey(
                        decision,
                        election);

                if (!string.Equals(
                    currentKey,
                    parts[1],
                    StringComparison
                        .OrdinalIgnoreCase))
                {
                    SnapshotKingdomDecisions(
                        campaign);

                    return
                        "kingdom_vote_stale_key:" +
                        currentKey;
                }

                bool abstain =
                    string.Equals(
                        parts[2],
                        "ABSTAIN",
                        StringComparison
                            .OrdinalIgnoreCase);

                int outcomeIndex = -1;

                if (!abstain &&
                    !int.TryParse(
                        parts[2],
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out outcomeIndex))
                {
                    return
                        "kingdom_vote_bad_outcome";
                }

                if (!abstain &&
                    (outcomeIndex < 0 ||
                    outcomeIndex >=
                    election
                        .PossibleOutcomes
                        .Count))
                {
                    return
                        "kingdom_vote_outcome_oob";
                }

                if (!TryParseSupportWeight(
                    parts[3],
                    out Supporter
                        .SupportWeights
                        weight))
                {
                    return
                        "kingdom_vote_bad_weight";
                }

                string title =
                    decision
                        .GetSupportTitle()
                        ?.ToString();

                DecisionOutcome selected =
                    abstain
                        ? null
                        : election
                            .PossibleOutcomes[
                                outcomeIndex];

                string selectedTitle =
                    selected == null
                        ? "ABSTAIN"
                        : selected
                            .GetDecisionTitle()
                            ?.ToString();

                TryDynastyShadow(
                    "kingdom_vote",
                    currentKey,
                    title,
                    selectedTitle);

                float nativeSupport =
                    selected == null
                        ? 0f
                        : decision
                            .DetermineSupport(
                                playerClan,
                                selected);

                float influenceBefore =
                    playerClan.Influence;

                int decisionsBefore =
                    kingdom
                        .UnresolvedDecisions
                        .Count;

                string before =
                    "{" +
                    J("schema") + ":" +
                    J(
                        "BannerlordAI." +
                        "KingdomVoteReceipt." +
                        "v1") +
                    "," +
                    J("phase") + ":" +
                    J("before") +
                    "," +
                    J("utc") + ":" +
                    J(
                        DateTime
                            .UtcNow
                            .ToString("O")) +
                    "," +
                    J("campaignHours") + ":" +
                    J(
                        CampaignTime
                            .Now
                            .ToHours
                            .ToString(
                                "R",
                                CultureInfo
                                    .InvariantCulture)) +
                    "," +
                    J("hero") + ":" +
                    J(
                        Hero.MainHero == null
                            ? "<none>"
                            : Hero
                                .MainHero
                                .StringId) +
                    "," +
                    J("decisionIndex") + ":" +
                    decisionIndex
                        .ToString(
                            CultureInfo
                                .InvariantCulture) +
                    "," +
                    J("decisionKey") + ":" +
                    J(currentKey) +
                    "," +
                    J("decisionTitle") + ":" +
                    J(title) +
                    "," +
                    J("selectedOutcome") + ":" +
                    J(selectedTitle) +
                    "," +
                    J("supportWeight") + ":" +
                    J(weight.ToString()) +
                    "," +
                    J("nativeSupport") + ":" +
                    F(nativeSupport) +
                    "," +
                    J("influenceBefore") + ":" +
                    F(influenceBefore) +
                    "," +
                    J("unresolvedBefore") + ":" +
                    decisionsBefore
                        .ToString(
                            CultureInfo
                                .InvariantCulture) +
                    "}";

                AppendVoteReceipt(before);

                election.StartElection();

                if (election.IsCancelled)
                {
                    return
                        "kingdom_vote_cancelled";
                }

                if (abstain)
                {
                    election.OnPlayerSupport(
                        null,
                        Supporter
                            .SupportWeights
                            .StayNeutral);
                }
                else
                {
                    selected =
                        election
                            .PossibleOutcomes[
                                outcomeIndex];

                    election.OnPlayerSupport(
                        selected,
                        weight);
                }

                election.ApplySelection();

                float influenceAfter =
                    playerClan.Influence;

                int decisionsAfter =
                    kingdom
                        .UnresolvedDecisions
                        .Count;

                string resultText =
                    election
                        .GetChosenOutcomeText()
                        ?.ToString();

                string after =
                    "{" +
                    J("schema") + ":" +
                    J(
                        "BannerlordAI." +
                        "KingdomVoteReceipt." +
                        "v1") +
                    "," +
                    J("phase") + ":" +
                    J("after") +
                    "," +
                    J("utc") + ":" +
                    J(
                        DateTime
                            .UtcNow
                            .ToString("O")) +
                    "," +
                    J("campaignHours") + ":" +
                    J(
                        CampaignTime
                            .Now
                            .ToHours
                            .ToString(
                                "R",
                                CultureInfo
                                    .InvariantCulture)) +
                    "," +
                    J("hero") + ":" +
                    J(
                        Hero.MainHero == null
                            ? "<none>"
                            : Hero
                                .MainHero
                                .StringId) +
                    "," +
                    J("decisionKey") + ":" +
                    J(currentKey) +
                    "," +
                    J("decisionTitle") + ":" +
                    J(title) +
                    "," +
                    J("selectedOutcome") + ":" +
                    J(selectedTitle) +
                    "," +
                    J("supportWeight") + ":" +
                    J(weight.ToString()) +
                    "," +
                    J("chosenOutcomeText") + ":" +
                    J(resultText) +
                    "," +
                    J("influenceAfter") + ":" +
                    F(influenceAfter) +
                    "," +
                    J("influenceDelta") + ":" +
                    F(
                        influenceAfter -
                        influenceBefore) +
                    "," +
                    J("unresolvedAfter") + ":" +
                    decisionsAfter
                        .ToString(
                            CultureInfo
                                .InvariantCulture) +
                    "}";

                AppendVoteReceipt(after);

                SnapshotKingdomDecisions(
                    campaign);

                return
                    "kingdom_vote_applied:" +
                    Clean(selectedTitle) +
                    ":" +
                    weight;
            }
            catch (Exception ex)
            {
                WriteLog(
                    "KINGDOM_VOTE_FAILED type=" +
                    ex.GetType().Name +
                    " message=" +
                    Clean(ex.Message));

                return
                    "kingdom_vote_failed_" +
                    ex.GetType().Name;
            }
        }

        private void WriteStatus(string reason)
        {
            try
            {
                Campaign campaign = Campaign.Current;

                string campaignReady =
                    (campaign != null).ToString();

                string mode =
                    campaign == null
                        ? "<none>"
                        : campaign.TimeControlMode.ToString();

                string hours = "<none>";
                if (campaign != null)
                {
                    try
                    {
                        hours =
                            CampaignTime.Now.ToHours
                                .ToString(
                                    "R",
                                    CultureInfo
                                        .InvariantCulture);
                    }
                    catch
                    {
                        hours = "<loading>";
                    }
                }
                MobileParty mainParty =
                    campaign == null
                        ? null
                        : MobileParty.MainParty;

                Hero mainHero =
                    campaign == null
                        ? null
                        : Hero.MainHero;
                string playerHero =
                    mainHero == null ? "<none>" : Clean(mainHero.Name == null ? mainHero.StringId : mainHero.Name.ToString());
                string partySettlement =
                    mainParty == null || mainParty.CurrentSettlement == null
                        ? "<none>"
                        : Clean(mainParty.CurrentSettlement.StringId);
                string partyPosition =
                    mainParty == null
                        ? "<none>"
                        : mainParty.GetPosition2D.x.ToString("R", CultureInfo.InvariantCulture) +
                          "," +
                          mainParty.GetPosition2D.y.ToString("R", CultureInfo.InvariantCulture);

                string text =
                    "schema=BannerlordAI.TestRunner.v2" +
                    Environment.NewLine +
                    "utc=" +
                    DateTime.UtcNow.ToString("O") +
                    Environment.NewLine +
                    "reason=" + reason +
                    Environment.NewLine +
                    "campaignReady=" + campaignReady +
                    Environment.NewLine +
                    "timeControl=" + mode +
                    Environment.NewLine +
                    "campaignHours=" + hours +
                    Environment.NewLine +
                    "loadedSave=" +
                    Clean(_lastLoadedSaveName) +
                    Environment.NewLine +
                    "campaignGeneration=" +
                    _campaignGeneration.ToString(
                        CultureInfo.InvariantCulture) +
                    Environment.NewLine +
                    "autoDesiredMode=" +
                    (_autoDesiredMode.HasValue
                        ? _autoDesiredMode.Value.ToString()
                        : "<none>") +
                    Environment.NewLine +
                    "autoResumeCount=" +
                    _autoResumeCount +
                    Environment.NewLine +
                    "blockers=" +
                    Clean(_lastBlockers) +
                    Environment.NewLine +
                    "lastCommand=" +
                    Clean(_lastCommand) +
                    Environment.NewLine +
                    "lastResult=" +
                    Clean(_lastResult) +
                    Environment.NewLine +
                    "decisionType=" +
                    (_currentIncident != null
                        ? "incident"
                        : (GetFirstPendingKingdomDecision() != null
                            ? "kingdom"
                            : "<none>")) +
                    Environment.NewLine +
                    "decisionTitle=" +
                    Clean(
                        _currentIncident != null
                            ? _currentIncident.Title
                                ?.ToString()
                            : (GetFirstPendingKingdomDecision() == null
                                ? "<none>"
                                : GetFirstPendingKingdomDecision()
                                    .GetSupportTitle()
                                    ?.ToString())) +
                    Environment.NewLine +
                    "decisionOptionCount=" +
                    (_currentIncident != null
                        ? _currentIncident
                            .NumOfOptions.ToString(
                                CultureInfo
                                    .InvariantCulture)
                        : (GetFirstPendingKingdomDecision() == null
                            ? "0"
                            : new KingdomElection(
                                GetFirstPendingKingdomDecision())
                                .PossibleOutcomes
                                .Count
                                .ToString(
                                    CultureInfo
                                        .InvariantCulture))) +
                    Environment.NewLine +
                    "playerHero=" +
                    playerHero +
                    Environment.NewLine +
                    "partySettlement=" +
                    partySettlement +
                    Environment.NewLine +
                    "partyPosition=" +
                    partyPosition +
                    Environment.NewLine;

                lock (IoLock)
                {
                    File.WriteAllText(StatusPath, text);
                }
            }
            catch
            {
            }
        }
        private static void WriteLog(string message)
        {
            try
            {
                lock (IoLock)
                {
                    Directory.CreateDirectory(Root);

                    File.AppendAllText(
                        LogPath,
                        DateTime.UtcNow.ToString("O") +
                        " [TestRunner v0.1] " +
                        message +
                        Environment.NewLine);
                }
            }
            catch
            {
            }
        }

        private static string Clean(string text)
        {
            return
                (text ?? "<null>")
                .Replace("\r", " ")
                .Replace("\n", " ")
                .Replace("\t", " ");
        }
    }
}

