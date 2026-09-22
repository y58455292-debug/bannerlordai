from pathlib import Path
import base64
import datetime
import hashlib
import json
import secrets
import shutil
import subprocess
import sys
import time

ROOT = Path(r"D:\BannerlordAIResearch")
sys.path.insert(0, str(ROOT / "workspace"))

import validate_v02146_provider_dispatch_outbox as v46
import validate_v02149_provider_work_lifecycle as v49
import validate_v02150_provider_job_claim as v50
import validate_v02155_provider_request_registration as v55
import validate_v02158_provider_transport_registration as v58
import validate_v02161_execution_policy as v61
import provider_request_contract_v02154 as contract
import provider_transport_loopback_v02166 as loop

h = v46.h
CLAN_SRC = v46.CLAN_SRC
RUNNER_SRC = ROOT / r"workspace\_PatchStaging\AutonomousOperator_v002166_AuthBoundResult_20260921_2331\candidate"
CLAN_SHA = v46.CLAN_SHA
RUNNER_SHA = "D91136C0378834206ABB30C2E1A5FF728F51673836BA632F49F7019A82241401"
OWNER = "sol_auth_bound_result_v02166"
SOURCE_PREFIX = "ClanAI V02166 AUTH BOUND RESULT SOURCE "

MAIN_FIXTURES = ROOT / r"workspace\_PatchStaging\AutonomousOperator_v002166_AuthBoundResult_20260921_2331\fixtures\Fixtures.csproj"
V3_FIXTURES = ROOT / r"workspace\_PatchStaging\AutonomousOperator_v002166_AuthBoundResult_20260921_2331\fixtures_v3\FixturesV3.csproj"
V4_FIXTURES = ROOT / r"workspace\_PatchStaging\AutonomousOperator_v002166_AuthBoundResult_20260921_2331\fixtures_v4\FixturesV4.csproj"
V5_FIXTURES = ROOT / r"workspace\_PatchStaging\AutonomousOperator_v002166_AuthBoundResult_20260921_2331\fixtures_v5\FixturesV5.csproj"
LOOPBACK_FIXTURE = ROOT / r"workspace\test_provider_transport_loopback_v02166.py"
CONTRACT_FIXTURE = ROOT / r"workspace\test_provider_request_contract_v02154.py"

OUTBOX = v46.OUTBOX
CLAIMS = v50.CLAIMS
REQUEST_REGISTRATIONS = v55.REGISTRATIONS
TRANSPORT_REGISTRATIONS = v58.TRANSPORT_REGISTRATIONS
POLICY_REGISTRATIONS = v61.POLICY_REGISTRATIONS
AUTHORIZATIONS = ROOT / r"Automation\TestRunner\patrol_defense_provider_transport_execution_authorizations.jsonl"
PROVIDER_INVOCATIONS = v46.PROVIDER_INVOCATIONS
RESULT_V5 = ROOT / r"Automation\TestRunner\patrol_defense_provider_result_v5_admissions.jsonl"
BINDINGS_V5 = ROOT / r"Automation\TestRunner\patrol_defense_provider_transport_result_v5_bindings.jsonl"
COMPLETIONS = v49.COMPLETIONS

PROVIDER_ID = "deterministic_external_worker"
MODEL_ID = "deterministic_mock_no_model"

h.CLAN_SRC = CLAN_SRC
h.RUNNER_SRC = RUNNER_SRC
h.NEW_CLAN = CLAN_SHA
h.NEW_RUNNER = RUNNER_SHA


def run_text(cmd, expected):
    p = subprocess.run(cmd, capture_output=True, text=True)
    text = (p.stdout or "").strip()
    if p.returncode != 0 or expected not in text:
        raise RuntimeError(
            "fixture failed: " + " ".join(str(x) for x in cmd)
            + "\nstdout=" + text
            + "\nstderr=" + (p.stderr or "")
        )
    return text


def binding_success_checks(row, receipt, result, result_json, policy_registration, authorization):
    nested=row.get("providerResultAdmission")
    expected_sha=hashlib.sha256(result_json.encode("utf-8")).hexdigest().upper()
    return {
        "schema": row.get("schema")=="BannerlordAI.PatrolDefenseProviderTransportReceiptResultV5Binding.v1",
        "mode": row.get("mode")=="observe",
        "transport_id": row.get("transportId")==receipt.get("transportId"),
        "transport_request": row.get("transportRequestId")==receipt.get("transportRequestId")==result.get("transportRequestId"),
        "provider_request": row.get("providerRequestId")==receipt.get("providerRequestId")==result.get("providerRequestId"),
        "provider": row.get("providerId")==receipt.get("providerId"),
        "model": row.get("modelId")==receipt.get("modelId"),
        "attempt": row.get("attemptId")==receipt.get("attemptId"),
        "fingerprint": row.get("requestFingerprint")==receipt.get("requestFingerprint"),
        "input_sha": row.get("requestInputSha256")==receipt.get("requestInputSha256"),
        "status": row.get("resultStatus")==receipt.get("resultStatus")==result.get("status"),
        "execution_policy": row.get("executionPolicyId")==receipt.get("executionPolicyId")==result.get("executionPolicyId")==policy_registration.get("executionPolicyId"),
        "auth_id": row.get("transportExecutionAuthorizationId")==receipt.get("transportExecutionAuthorizationId")==result.get("transportExecutionAuthorizationId")==authorization.get("transportExecutionAuthorizationId"),
        "auth_ordinal": row.get("transportExecutionAttemptOrdinal")==int(receipt.get("transportExecutionAttemptOrdinal"))==int(result.get("transportExecutionAttemptOrdinal"))==authorization.get("attemptOrdinal"),
        "receipt_sha": row.get("resultSha256")==expected_sha,
        "computed_sha": row.get("computedResultSha256")==expected_sha,
        "receipt_match": row.get("transportReceiptMatchReason")=="TRANSPORT_RECEIPT_MATCHED",
        "policy_match": row.get("executionPolicyMatchReason")=="EXECUTION_POLICY_MATCHED",
        "auth_match": row.get("transportExecutionAuthorizationMatchReason")=="EXECUTION_AUTHORIZATION_MATCHED",
        "size_reason": row.get("resultSizePolicyReason")=="RESULT_SIZE_WITHIN_LIMIT",
        "size_count": row.get("resultByteCount")==len(result_json.encode("utf-8")),
        "size_max": row.get("maxResultBytes")==int(v61.TEST_MAX_RESULT_BYTES),
        "binding": row.get("bindingAccepted") is True,
        "no_rejections": row.get("rejectionReasons")==[],
        "network_false": row.get("externalNetworkUsed") is False,
        "model_false": row.get("modelInvoked") is False,
        "receipt_success": row.get("receiptSuccess") is True,
        "error_null": row.get("errorCode") is None,
        "nested_present": isinstance(nested,dict),
        "execution_false": row.get("executionAuthorized") is False,
        "behavior_false": row.get("behaviorMutation") is False,
        "intent_false": row.get("intentMutation") is False,
        "score_false": row.get("scoreMutation") is False,
        "movement_zero": row.get("nativeMovementCalls")==0,
    }

def result_v5_checks(row, result, policy_registration, authorization):
    advisory=row.get("advisoryAdmission")
    return {
        "schema": row.get("schema")=="BannerlordAI.PatrolDefenseProviderResultV5Admission.v1",
        "mode": row.get("mode")=="observe",
        "provider": row.get("providerId")==result.get("providerId"),
        "attempt": row.get("attemptId")==result.get("attemptId"),
        "fingerprint": row.get("requestFingerprint")==result.get("requestFingerprint"),
        "provider_request": row.get("providerRequestId")==result.get("providerRequestId"),
        "transport_request": row.get("transportRequestId")==result.get("transportRequestId"),
        "execution_policy": row.get("executionPolicyId")==result.get("executionPolicyId")==policy_registration.get("executionPolicyId"),
        "auth_id": row.get("transportExecutionAuthorizationId")==result.get("transportExecutionAuthorizationId")==authorization.get("transportExecutionAuthorizationId"),
        "auth_ordinal": row.get("transportExecutionAttemptOrdinal")==int(result.get("transportExecutionAttemptOrdinal"))==authorization.get("attemptOrdinal"),
        "status": row.get("status")=="SUCCESS",
        "claim_match": row.get("claimMatchReason")=="CLAIM_MATCHED",
        "provider_request_match": row.get("providerRequestMatchReason")=="PROVIDER_REQUEST_MATCHED",
        "transport_request_match": row.get("transportRequestMatchReason")=="TRANSPORT_REQUEST_MATCHED",
        "execution_policy_match": row.get("executionPolicyMatchReason")=="EXECUTION_POLICY_MATCHED",
        "authorization_match": row.get("transportExecutionAuthorizationMatchReason")=="EXECUTION_AUTHORIZATION_MATCHED",
        "accepted": row.get("providerResultAccepted") is True,
        "retryable_false": row.get("retryable") is False,
        "no_rejections": row.get("rejectionReasons")==[],
        "advisory_admitted": isinstance(advisory,dict) and advisory.get("admitted") is True,
        "disposition": isinstance(advisory,dict) and advisory.get("disposition")=="KEEP_BASELINE",
        "execution_false": row.get("executionAuthorized") is False,
        "model_false": row.get("modelInvoked") is False,
        "behavior_false": row.get("behaviorMutation") is False,
        "score_false": row.get("scoreMutation") is False,
        "movement_zero": row.get("nativeMovementCalls")==0,
    }

def binding_replay_checks(row,fingerprint):
    reasons=row.get("rejectionReasons")
    return {
        "schema": row.get("schema")=="BannerlordAI.PatrolDefenseProviderTransportReceiptResultV5Binding.v1",
        "fingerprint": row.get("requestFingerprint")==fingerprint,
        "binding_false": row.get("bindingAccepted") is False,
        "job_missing_match": row.get("transportReceiptMatchReason")=="DISPATCH_JOB_NOT_FOUND",
        "job_missing_reason": isinstance(reasons,list) and "DISPATCH_JOB_NOT_FOUND" in reasons,
        "nested_null": row.get("providerResultAdmission") is None,
        "execution_false": row.get("executionAuthorized") is False,
        "movement_zero": row.get("nativeMovementCalls")==0,
    }

def main():
    if h.base.process_pids():
        raise RuntimeError("Bannerlord already running")

    runner_source = (RUNNER_SRC / "SubModule.cs").read_text(
        encoding="utf-8", errors="replace"
    )
    dispatch_source = (RUNNER_SRC / "PatrolDefenseProviderDispatch.cs").read_text(
        encoding="utf-8", errors="replace"
    )
    result_source = (RUNNER_SRC / "PatrolDefenseProviderResultV5Admission.cs").read_text(
        encoding="utf-8", errors="replace"
    )
    binding_source = (
        RUNNER_SRC / "PatrolDefenseProviderTransportReceiptResultV5Binding.cs"
    ).read_text(encoding="utf-8", errors="replace")

    static_checks = {
        "authorization_command": "PATROL_DEFENSE_PROVIDER_TRANSPORT_EXECUTION_AUTHORIZE "
        in runner_source,
        "authorization_path":
        "patrol_defense_provider_transport_execution_authorizations.jsonl"
        in runner_source,
        "authorization_method": "AuthorizeTransportExecution(" in dispatch_source,
        "authorization_receipt":
        "BannerlordAI.PatrolDefenseProviderTransportExecutionAuthorization.v1"
        in dispatch_source,
        "authorization_limit_reason":
        "EXECUTION_ATTEMPT_LIMIT_EXCEEDED" in dispatch_source,
        "authorization_invalid_reason":
        "EXECUTION_ATTEMPT_POLICY_INVALID" in dispatch_source,
        "authorization_timeout_invalid_reason":
        "EXECUTION_TIMEOUT_POLICY_INVALID" in dispatch_source,
        "authorization_timeout_receipt":
        "result.TimeoutMs.ToString(" in dispatch_source,
        "authorization_matcher":
        "MatchRegisteredTransportExecutionAuthorization(" in dispatch_source,
        "authorization_match_reasons":
        "EXECUTION_AUTHORIZATION_ID_MISMATCH" in dispatch_source
        and "EXECUTION_AUTHORIZATION_ORDINAL_MISMATCH" in dispatch_source
        and "EXECUTION_AUTHORIZATION_MATCHED" in dispatch_source,
        "authorization_v5_wiring":
        "transportExecutionAuthorizationId" in result_source
        and "transportExecutionAttemptOrdinal" in result_source
        and "MatchRegisteredTransportExecutionAuthorization(" in result_source
        and "transportExecutionAuthorizationId" in binding_source
        and "MatchRegisteredTransportExecutionAuthorization(" in binding_source,
        "no_sleep": "Thread.Sleep" not in runner_source
        and "Thread.Sleep" not in dispatch_source,
        "v5_command": "PATROL_DEFENSE_PROVIDER_TRANSPORT_RESULT_V5_ADMIT "
        in runner_source,
        "v5_result_path": "patrol_defense_provider_result_v5_admissions.jsonl"
        in runner_source,
        "v5_binding_path":
        "patrol_defense_provider_transport_result_v5_bindings.jsonl"
        in runner_source,
        "result_schema": "BannerlordAI.PatrolDefenseProviderResult.v5"
        in result_source,
        "receipt_schema": "BannerlordAI.PatrolDefenseProviderTransportReceipt.v4"
        in binding_source,
        "binding_schema":
        "BannerlordAI.PatrolDefenseProviderTransportReceiptResultV5Binding.v1"
        in binding_source,
        "execution_policy_match": "MatchRegisteredExecutionPolicy("
        in dispatch_source,
        "execution_policy_wiring":
        "MatchRegisteredExecutionPolicy(" in result_source
        and "executionPolicyMatchReason" in result_source
        and "MatchRegisteredExecutionPolicy(" in binding_source
        and "EXECUTION_POLICY_MATCHED" in binding_source
        and "EXECUTION_POLICY_MATCHED" in dispatch_source,
        "result_hash": "Sha256Upper(resultBytes)" in binding_source,
        "result_size_policy_method": "EvaluateRegisteredResultSizePolicy("
        in dispatch_source,
        "result_size_policy_reasons":
        "RESULT_SIZE_WITHIN_LIMIT" in dispatch_source
        and "RESULT_SIZE_LIMIT_EXCEEDED" in dispatch_source
        and "RESULT_SIZE_POLICY_INVALID" in dispatch_source,
        "binding_size_audit":
        "resultSizePolicyReason" in binding_source
        and "resultByteCount" in binding_source
        and "maxResultBytes" in binding_source,
        "no_env_read": "GetEnvironmentVariable" not in runner_source
        and "GetEnvironmentVariable" not in result_source
        and "GetEnvironmentVariable" not in binding_source,
        "no_http": "HttpClient" not in runner_source
        and "HttpClient" not in result_source
        and "HttpClient" not in binding_source,
        "no_socket": "Socket" not in result_source and "Socket" not in binding_source,
        "no_openai": "OpenAI" not in result_source
        and "OpenAI" not in binding_source,
        "no_apply": "PatrolDefenseApplyGate" not in result_source
        and "PatrolDefenseApplyGate" not in binding_source,
        "no_score": "SetBehaviorScore" not in result_source
        and "SetBehaviorScore" not in binding_source,
        "no_memory_write": "RecordPatrolDefenseEpisode" not in result_source
        and "RecordPatrolDefenseEpisode" not in binding_source,
    }
    if not all(static_checks.values()):
        raise RuntimeError("static checks " + json.dumps(static_checks, sort_keys=True))

    main_fixture = run_text(
        ["dotnet", "run", "--project", str(MAIN_FIXTURES), "-c", "Release"],
        "PASS_FIXTURES checks=257",
    )
    v3_fixture = run_text(
        ["dotnet", "run", "--project", str(V3_FIXTURES), "-c", "Release"],
        "PASS_V3_FIXTURES checks=63",
    )
    v4_fixture = run_text(
        ["dotnet", "run", "--project", str(V4_FIXTURES), "-c", "Release"],
        "PASS_V4_FIXTURES checks=135",
    )
    v5_fixture = run_text(
        ["dotnet", "run", "--project", str(V5_FIXTURES), "-c", "Release"],
        "PASS_V5_FIXTURES checks=40",
    )
    loop_fixture = run_text(
        [sys.executable, "-X", "utf8", str(LOOPBACK_FIXTURE)],
        "PASS_FIXTURES checks=63",
    )
    contract_fixture = run_text(
        [sys.executable, "-X", "utf8", str(CONTRACT_FIXTURE)],
        "PASS_FIXTURES checks=49",
    )

    if h.sha(h.DST / "ClanAI.dll") != h.OLD_CLAN:
        raise RuntimeError("installed ClanAI drift")
    if h.sha(h.DST / "BannerlordAITestRunner.dll") != h.OLD_RUNNER:
        raise RuntimeError("installed runner drift")
    if h.sha(CLAN_SRC / "ClanAI.dll") != CLAN_SHA:
        raise RuntimeError("candidate ClanAI drift")
    if h.sha(RUNNER_SRC / "BannerlordAITestRunner.dll") != RUNNER_SHA:
        raise RuntimeError("candidate runner drift")

    root = h.SAVES / "ClanAI MANAN BRANCH ROOT.sav"
    blood = h.SAVES / "BLOOD FUED.sav"
    if h.sha(root) != h.SAVE_HASH or h.sha(blood) != h.SAVE_HASH:
        raise RuntimeError("protected save drift")

    stamp = datetime.datetime.now().strftime("%Y%m%d_%H%M%S_%f")
    source_slot = SOURCE_PREFIX + stamp
    source = h.SAVES / (source_slot + ".sav")
    if source.exists():
        source.unlink()
    shutil.copy2(root, source)

    validation = ROOT / "Longitudinal/LiveValidation" / (
        "PatrolDefense_v02166_AuthorizationBoundResult_" + stamp
    )
    validation.mkdir(parents=True)
    backup = ROOT / "workspace/_PatchBackups" / (
        "Before_AuthorizationBoundResult_" + stamp
    )
    backup.mkdir(parents=True)

    operation = "authorization-bound-result-v02166-" + stamp
    token = secrets.token_hex(32)
    token_hash = hashlib.sha256(token.encode()).hexdigest().upper()

    evidence = {
        "schema": "BannerlordAI.AuthorizationBoundResultValidation.v1",
        "created_local": datetime.datetime.now().astimezone().isoformat(),
        "result": "IN_PROGRESS",
        "source_slot": source_slot,
        "clanai_test_sha256": CLAN_SHA,
        "runner_test_sha256": RUNNER_SHA,
        "main_fixtures": main_fixture,
        "v3_fixtures": v3_fixture,
        "v4_fixtures": v4_fixture,
        "v5_fixtures": v5_fixture,
        "loopback_v5_fixtures": loop_fixture,
        "request_contract_fixtures": contract_fixture,
        "static_checks": static_checks,
    }
    h.base.atomic_json(validation / "validation.json", evidence)

    binaries = {
        "ClanAI.dll": CLAN_SRC / "ClanAI.dll",
        "ClanAI.pdb": CLAN_SRC / "ClanAI.pdb",
        "BannerlordAITestRunner.dll": RUNNER_SRC / "BannerlordAITestRunner.dll",
        "BannerlordAITestRunner.pdb": RUNNER_SRC / "BannerlordAITestRunner.pdb",
    }
    for name in binaries:
        if (h.DST / name).exists():
            shutil.copy2(h.DST / name, backup / name)

    run = None
    lease = False
    deployed = False
    try:
        h.base.guard(
            "acquire",
            OWNER,
            "patrol_defense_authorization_bound_result_shadow",
            h.OLD_CLAN,
            operation,
            "1800",
        )
        lease = True
        h.base.guard("phase", OWNER, "PREPARED", operation)

        for name, src in binaries.items():
            shutil.copy2(src, h.DST / name)
        deployed = True
        if (
            h.sha(h.DST / "ClanAI.dll") != CLAN_SHA
            or h.sha(h.DST / "BannerlordAITestRunner.dll") != RUNNER_SHA
        ):
            raise RuntimeError("deploy mismatch")

        clear_paths = (
            OUTBOX,
            CLAIMS,
            REQUEST_REGISTRATIONS,
            TRANSPORT_REGISTRATIONS,
            POLICY_REGISTRATIONS,
            AUTHORIZATIONS,
            PROVIDER_INVOCATIONS,
            RESULT_V5,
            BINDINGS_V5,
            COMPLETIONS,
        )
        h.clear_runtime_outputs()
        for path in clear_paths:
            if path.exists():
                path.unlink()

        run = h.base.bus.start_run(
            issuer="authorization_bound_result_live",
            expected_campaign_id=None,
            expected_runner_version="v0.2.10.20-temporal-history-context-shadow",
            deployment_owner=OWNER,
            deployment_operation_id=operation,
            exit_capability_sha256=token_hash,
        )
        h.base.guard("phase", OWNER, "ACTIVE", operation)

        launch_dir = validation / "launch"
        launch_dir.mkdir()
        proc, ready, state, record = h.launch.attempt_launch(source, 180, launch_dir)
        evidence["launch"] = record
        if ready is None or state != "READY":
            h.launch.stop_pre_ready_process(proc, record)
            raise RuntimeError("bootstrap " + state)

        exact, epoch, seed = h.load_exact(OWNER, run, source_slot, h.EXPECTED_HOURS)
        evidence["exact_load"] = exact
        evidence["seed_line"] = seed
        start_hours = h.status_hours()

        h.send_wait(
            OWNER,
            run,
            "PATROL_SETTLEMENT town_ES7",
            "operator_patrol_settlement:town_ES7",
            10,
        )
        h.send_ack(OWNER, run, "AUTO_FAST", 10)

        row, request, route, dispatch, last_hours = v46.wait_dispatch(
            start_hours, 180, 120.0
        )
        evidence["campaign_start_hours"] = start_hours
        evidence["campaign_end_hours"] = last_hours

        if row is None:
            evidence["result"] = "ENVIRONMENTAL_INCONCLUSIVE"
            evidence["reason"] = "No natural provider dispatch job within bounded window."
            h.base.atomic_json(validation / "validation.json", evidence)
            h.safe_exit(
                OWNER,
                token,
                run,
                "v02166 authorization-bound result environmental inconclusive",
            )
            run = None
        else:
            fingerprint = request.get("requestFingerprint")
            outbox_text = OUTBOX.read_text(encoding="utf-8", errors="strict")
            raw_lines = [line for line in outbox_text.splitlines() if line.strip()]
            if len(raw_lines) != 1:
                raise RuntimeError("expected exactly one raw outbox row")
            job_line = raw_lines[0]

            before_claim = len(h.read_jsonl(CLAIMS))
            claim_command = (
                "PATROL_DEFENSE_PROVIDER_JOB_CLAIM "
                + fingerprint
                + " deterministic_external_worker attempt-1"
            )
            h.send_wait(
                OWNER,
                run,
                claim_command,
                "patrol_defense_provider_job_claimed",
                10,
            )
            claims = h.read_jsonl(CLAIMS)
            if len(claims) <= before_claim:
                raise RuntimeError("claim receipt missing")
            claim = claims[-1]
            claim_checks = v50.claim_checks(
                claim,
                fingerprint,
                "CLAIMED",
                True,
                False,
                "PENDING",
                "CLAIMED",
            )

            request_envelope, request_json = contract.build_provider_request(
                job_line=job_line,
                claim=claim,
                provider_id=PROVIDER_ID,
                model_id=MODEL_ID,
            )
            expected_provider_request_id = hashlib.sha256(
                request_json.encode("utf-8")
            ).hexdigest().upper()
            request_cmd = (
                "PATROL_DEFENSE_PROVIDER_REQUEST_REGISTER "
                + base64.b64encode(request_json.encode("utf-8")).decode("ascii")
            )
            before_request = len(h.read_jsonl(REQUEST_REGISTRATIONS))
            h.send_wait(
                OWNER,
                run,
                request_cmd,
                "patrol_defense_provider_request_registered",
                10,
            )
            request_rows = h.read_jsonl(REQUEST_REGISTRATIONS)
            if len(request_rows) <= before_request:
                raise RuntimeError("provider request registration missing")
            request_registration = request_rows[-1]
            request_checks = v55.registration_checks(
                request_registration,
                request_envelope,
                expected_provider_request_id,
                False,
            )

            transport, transport_json = v58.transport_envelope(
                request_registration,
                request_envelope,
            )
            expected_transport_request_id = hashlib.sha256(
                transport_json.encode("utf-8")
            ).hexdigest().upper()
            transport_cmd = (
                "PATROL_DEFENSE_PROVIDER_TRANSPORT_REGISTER "
                + base64.b64encode(transport_json.encode("utf-8")).decode("ascii")
            )
            before_transport = len(h.read_jsonl(TRANSPORT_REGISTRATIONS))
            h.send_wait(
                OWNER,
                run,
                transport_cmd,
                "patrol_defense_provider_transport_registered",
                10,
            )
            transport_rows = h.read_jsonl(TRANSPORT_REGISTRATIONS)
            if len(transport_rows) <= before_transport:
                raise RuntimeError("transport registration missing")
            transport_registration = transport_rows[-1]
            transport_checks = v58.transport_checks(
                transport_registration,
                transport,
                expected_transport_request_id,
                False,
            )

            policy, _ = v61.policy_envelope(
                request_registration,
                transport_registration,
                request_envelope,
            )
            policy["maxAttempts"]="2"
            policy_json=json.dumps(policy,separators=(",",":"),ensure_ascii=False)
            expected_policy_id=hashlib.sha256(policy_json.encode("utf-8")).hexdigest().upper()
            policy_cmd=(
                "PATROL_DEFENSE_PROVIDER_EXECUTION_POLICY_REGISTER "
                + base64.b64encode(policy_json.encode("utf-8")).decode("ascii")
            )
            before_policy=len(h.read_jsonl(POLICY_REGISTRATIONS))
            h.send_wait(OWNER,run,policy_cmd,"patrol_defense_provider_execution_policy_registered",10)
            policy_rows=h.read_jsonl(POLICY_REGISTRATIONS)
            if len(policy_rows)<=before_policy:
                raise RuntimeError("execution policy registration missing")
            policy_registration=policy_rows[-1]
            policy_checks={
                "schema":policy_registration.get("schema")=="BannerlordAI.PatrolDefenseProviderExecutionPolicyRegistration.v1",
                "execution_policy_id":policy_registration.get("executionPolicyId")==expected_policy_id,
                "transport_request":policy_registration.get("transportRequestId")==expected_transport_request_id,
                "provider_request":policy_registration.get("providerRequestId")==expected_provider_request_id,
                "fingerprint":policy_registration.get("requestFingerprint")==fingerprint,
                "timeout":policy_registration.get("timeoutMs")==v61.TEST_TIMEOUT_MS,
                "max_attempts":policy_registration.get("maxAttempts")=="2",
                "max_result_bytes":policy_registration.get("maxResultBytes")==v61.TEST_MAX_RESULT_BYTES,
                "registered":policy_registration.get("registered") is True,
                "reason":policy_registration.get("reason")=="REGISTERED",
                "network_false":policy_registration.get("externalNetworkUsed") is False,
                "model_false":policy_registration.get("modelInvoked") is False,
            }

            before_authorization=len(h.read_jsonl(AUTHORIZATIONS))
            authorization_cmd=(
                "PATROL_DEFENSE_PROVIDER_TRANSPORT_EXECUTION_AUTHORIZE "
                + fingerprint+" "+expected_policy_id
            )
            h.send_wait(OWNER,run,authorization_cmd,"patrol_defense_provider_transport_execution_authorized",10)
            h.send_wait(OWNER,run,authorization_cmd,"patrol_defense_provider_transport_execution_authorized",10)
            authorization_rows=h.read_jsonl(AUTHORIZATIONS)
            for _ in range(30):
                if len(authorization_rows)>=before_authorization+2:
                    break
                time.sleep(0.1)
                authorization_rows=h.read_jsonl(AUTHORIZATIONS)
            if len(authorization_rows)<before_authorization+2:
                raise RuntimeError("two transport execution authorizations missing after bounded wait")
            authorization1=authorization_rows[before_authorization]
            authorization2=authorization_rows[before_authorization+1]
            authorization_checks={
                "rows_two":len(authorization_rows)==before_authorization+2,
                "auth1":authorization1.get("authorized") is True and authorization1.get("reason")=="EXECUTION_ATTEMPT_AUTHORIZED",
                "auth2":authorization2.get("authorized") is True and authorization2.get("reason")=="EXECUTION_ATTEMPT_AUTHORIZED",
                "ordinals":authorization1.get("attemptOrdinal")==1 and authorization2.get("attemptOrdinal")==2,
                "counts":authorization1.get("attemptCount")==1 and authorization2.get("attemptCount")==2,
                "max_two":authorization1.get("maxAttempts")==2 and authorization2.get("maxAttempts")==2,
                "timeout":authorization1.get("timeoutMs")==1000 and authorization2.get("timeoutMs")==1000,
                "ids_distinct":authorization1.get("transportExecutionAuthorizationId")!=authorization2.get("transportExecutionAuthorizationId"),
                "network_false":authorization1.get("externalNetworkUsed") is False and authorization2.get("externalNetworkUsed") is False,
                "model_false":authorization1.get("modelInvoked") is False and authorization2.get("modelInvoked") is False,
            }

            stale_outcome=loop.send_loopback_v5(
                request_json,request_registration,transport_json,transport_registration,
                policy_registration,authorization1,
            )
            if not stale_outcome.success:
                raise RuntimeError("stale loopback construction failed "+json.dumps(stale_outcome.reasons))
            stale_command=(
                "PATROL_DEFENSE_PROVIDER_TRANSPORT_RESULT_V5_ADMIT "
                + base64.b64encode(stale_outcome.receipt_json.encode("utf-8")).decode("ascii")
                + " "
                + base64.b64encode(stale_outcome.result_json.encode("utf-8")).decode("ascii")
            )
            before_stale_binding=len(h.read_jsonl(BINDINGS_V5))
            before_stale_result=len(h.read_jsonl(RESULT_V5))
            before_stale_completion=len(h.read_jsonl(COMPLETIONS))
            h.send_wait(
                OWNER,run,stale_command,
                "patrol_defense_provider_transport_result_v5_rejected:EXECUTION_AUTHORIZATION_ID_MISMATCH",10,
            )
            stale_bindings=h.read_jsonl(BINDINGS_V5)
            stale_results=h.read_jsonl(RESULT_V5)
            stale_completions=h.read_jsonl(COMPLETIONS)
            if len(stale_bindings)<=before_stale_binding:
                raise RuntimeError("stale v5 binding receipt missing")
            if len(stale_results)!=before_stale_result:
                raise RuntimeError("stale v5 unexpectedly admitted nested result")
            if len(stale_completions)<=before_stale_completion:
                raise RuntimeError("stale completion receipt missing")
            stale_binding=stale_bindings[-1]
            stale_completion=stale_completions[-1]
            stale_checks={
                "binding_false":stale_binding.get("bindingAccepted") is False,
                "auth_reason":stale_binding.get("transportExecutionAuthorizationMatchReason")=="EXECUTION_AUTHORIZATION_ID_MISMATCH",
                "reason_list":"EXECUTION_AUTHORIZATION_ID_MISMATCH" in (stale_binding.get("rejectionReasons") or []),
                "nested_null":stale_binding.get("providerResultAdmission") is None,
                "auth1_carried":stale_binding.get("transportExecutionAuthorizationId")==authorization1.get("transportExecutionAuthorizationId"),
                "ordinal1":stale_binding.get("transportExecutionAttemptOrdinal")==1,
                "completion_retained":stale_completion.get("reason")=="SUCCESS_NOT_ADMITTED_RETAINED",
                "queue_retained":stale_completion.get("queueCountBefore")==1 and stale_completion.get("queueCountAfter")==1,
                "pending_retained":stale_completion.get("pendingRetained") is True,
            }

            outcome=loop.send_loopback_v5(
                request_json,request_registration,transport_json,transport_registration,
                policy_registration,authorization2,
            )
            if not outcome.success:
                raise RuntimeError("exact loopback v5 failed "+json.dumps(outcome.reasons))
            result=outcome.result
            receipt=outcome.receipt
            result_json=outcome.result_json
            receipt_json=outcome.receipt_json
            expected_result_sha=hashlib.sha256(result_json.encode("utf-8")).hexdigest().upper()
            loop_checks={
                "receipt_schema":receipt.get("schema")=="BannerlordAI.PatrolDefenseProviderTransportReceipt.v4",
                "result_schema":result.get("schema")=="BannerlordAI.PatrolDefenseProviderResult.v5",
                "policy":receipt.get("executionPolicyId")==result.get("executionPolicyId")==expected_policy_id,
                "auth_id":receipt.get("transportExecutionAuthorizationId")==result.get("transportExecutionAuthorizationId")==authorization2.get("transportExecutionAuthorizationId"),
                "auth_ordinal":int(receipt.get("transportExecutionAttemptOrdinal"))==int(result.get("transportExecutionAttemptOrdinal"))==2,
                "result_hash":receipt.get("resultSha256")==expected_result_sha,
                "network_false":receipt.get("externalNetworkUsed") is False,
                "model_false":receipt.get("modelInvoked") is False,
                "success":receipt.get("success") is True,
            }
            command=(
                "PATROL_DEFENSE_PROVIDER_TRANSPORT_RESULT_V5_ADMIT "
                + base64.b64encode(receipt_json.encode("utf-8")).decode("ascii")
                + " "
                + base64.b64encode(result_json.encode("utf-8")).decode("ascii")
            )
            before_binding=len(h.read_jsonl(BINDINGS_V5))
            before_result=len(h.read_jsonl(RESULT_V5))
            before_completion=len(h.read_jsonl(COMPLETIONS))
            h.send_wait(OWNER,run,command,"patrol_defense_provider_transport_result_v5_admitted",10)
            binding_rows=h.read_jsonl(BINDINGS_V5)
            result_rows=h.read_jsonl(RESULT_V5)
            completion_rows=h.read_jsonl(COMPLETIONS)
            if len(binding_rows)<=before_binding or len(result_rows)<=before_result or len(completion_rows)<=before_completion:
                raise RuntimeError("exact v5 receipts missing")
            binding=binding_rows[-1]
            admitted=result_rows[-1]
            completion=completion_rows[-1]
            binding_checks=binding_success_checks(binding,receipt,result,result_json,policy_registration,authorization2)
            result_checks=result_v5_checks(admitted,result,policy_registration,authorization2)
            completion_checks=v49.completion_success_checks(completion,fingerprint)

            before_replay_binding=len(binding_rows)
            before_replay_result=len(result_rows)
            before_replay_completion=len(completion_rows)
            h.send_wait(
                OWNER,run,command,
                "patrol_defense_provider_transport_result_v5_rejected:DISPATCH_JOB_NOT_FOUND",10,
            )
            replay_binding_rows=h.read_jsonl(BINDINGS_V5)
            replay_result_rows=h.read_jsonl(RESULT_V5)
            replay_completion_rows=h.read_jsonl(COMPLETIONS)
            if len(replay_binding_rows)<=before_replay_binding:
                raise RuntimeError("v5 replay binding missing")
            if len(replay_result_rows)!=before_replay_result:
                raise RuntimeError("v5 replay unexpectedly admitted nested result")
            if len(replay_completion_rows)<=before_replay_completion:
                raise RuntimeError("v5 replay completion missing")
            replay_binding=replay_binding_rows[-1]
            replay_completion=replay_completion_rows[-1]
            replay_binding_checks=binding_replay_checks(replay_binding,fingerprint)
            replay_completion_checks=v49.completion_replay_checks(replay_completion,fingerprint)

            provider_rows=h.read_jsonl(PROVIDER_INVOCATIONS)
            checks={}
            checks.update({"claim_"+k:v for k,v in claim_checks.items()})
            checks.update({"request_"+k:v for k,v in request_checks.items()})
            checks.update({"transport_"+k:v for k,v in transport_checks.items()})
            checks.update({"policy_"+k:v for k,v in policy_checks.items()})
            checks.update({"authorization_"+k:v for k,v in authorization_checks.items()})
            checks.update({"stale_"+k:v for k,v in stale_checks.items()})
            checks.update({"loop_"+k:v for k,v in loop_checks.items()})
            checks.update({"binding_"+k:v for k,v in binding_checks.items()})
            checks.update({"result_"+k:v for k,v in result_checks.items()})
            checks.update({"completion_"+k:v for k,v in completion_checks.items()})
            checks.update({"replay_binding_"+k:v for k,v in replay_binding_checks.items()})
            checks.update({"replay_completion_"+k:v for k,v in replay_completion_checks.items()})
            checks["provider_invocations_empty"]=len(provider_rows)==0
            checks["no_apply_side_effects"]=h.no_apply_side_effects()
            if not all(checks.values()):
                raise RuntimeError("v02166 live checks "+json.dumps(checks,sort_keys=True))

            evidence["shadow_receipt"]=row
            evidence["provider_request_registration"]=request_registration
            evidence["transport_registration"]=transport_registration
            evidence["execution_policy_registration"]=policy_registration
            evidence["transport_execution_authorization_1"]=authorization1
            evidence["transport_execution_authorization_2"]=authorization2
            evidence["stale_transport_receipt"]=stale_outcome.receipt
            evidence["stale_provider_result_v5"]=stale_outcome.result
            evidence["stale_transport_result_v5_binding"]=stale_binding
            evidence["stale_dispatch_completion"]=stale_completion
            evidence["transport_receipt"]=receipt
            evidence["provider_result_v5"]=result
            evidence["transport_result_v5_binding"]=binding
            evidence["provider_result_v5_admission"]=admitted
            evidence["dispatch_completion"]=completion
            evidence["transport_result_v5_binding_replay"]=replay_binding
            evidence["dispatch_completion_replay"]=replay_completion
            evidence["provider_invocation_rows"]=len(provider_rows)
            evidence["live_checks"]=checks
            evidence["result"]="PASS_AUTHORIZATION_BOUND_RESULT_SHADOW"
            h.base.atomic_json(validation/"validation.json",evidence)
            h.safe_exit(OWNER,token,run,"v02166 authorization-bound result verified")
            run=None

        for name in binaries:
            old = backup / name
            dest = h.DST / name
            if old.exists():
                shutil.copy2(old, dest)
            elif dest.exists():
                dest.unlink()
        deployed = False

        if (
            h.sha(h.DST / "ClanAI.dll") != h.OLD_CLAN
            or h.sha(h.DST / "BannerlordAITestRunner.dll") != h.OLD_RUNNER
        ):
            raise RuntimeError("restore failed")

        if source.exists():
            source.unlink()

        evidence.update({
            "protected_saves_match": h.sha(root) == h.SAVE_HASH
            and h.sha(blood) == h.SAVE_HASH,
            "disposable_saves_cleaned": not source.exists(),
            "clanai_restored_sha256": h.sha(h.DST / "ClanAI.dll"),
            "runner_restored_sha256": h.sha(
                h.DST / "BannerlordAITestRunner.dll"
            ),
        })

        h.base.guard("phase", OWNER, "EVIDENCE_FROZEN", operation)
        evidence["lease_release"] = h.base.guard("release", OWNER, operation)
        lease = False
        evidence["completed_local"] = datetime.datetime.now().astimezone().isoformat()
        h.base.atomic_json(validation / "validation.json", evidence)

        print(json.dumps({
            "validation": str(validation / "validation.json"),
            "result": evidence["result"],
            "executionPolicyId": None if row is None else expected_policy_id,
            "timeoutMs": None if row is None else authorization2.get("timeoutMs"),
            "binding": None if row is None else binding.get("bindingAccepted"),
            "sizeReason": None if row is None else binding.get("resultSizePolicyReason"),
            "resultByteCount": None if row is None else binding.get("resultByteCount"),
            "maxResultBytes": None if row is None else binding.get("maxResultBytes"),
            "completion": None if row is None else completion.get("reason"),
            "replay": None if row is None else replay_binding.get(
                "transportReceiptMatchReason"
            ),
        }, indent=2))
        return 0 if evidence["result"] == "PASS_AUTHORIZATION_BOUND_RESULT_SHADOW" else 2

    except Exception as ex:
        evidence["result"] = "FAIL"
        evidence["error"] = type(ex).__name__ + ": " + str(ex)
        evidence["failed_local"] = datetime.datetime.now().astimezone().isoformat()
        h.base.atomic_json(validation / "validation.json", evidence)

        if run is not None and h.base.process_pids():
            try:
                h.safe_exit(
                    OWNER,
                    token,
                    run,
                    "v02166 authorization-bound result failure",
                )
                run = None
            except Exception as e:
                evidence["exit_cleanup_error"] = type(e).__name__ + ": " + str(e)

        if run is not None and not h.base.process_pids():
            try:
                h.base.bus.close_run("v02166 pre-ready failure")
                run = None
            except Exception as e:
                evidence["control_close_error"] = type(e).__name__ + ": " + str(e)

        if not h.base.process_pids() and deployed:
            try:
                for name in binaries:
                    old = backup / name
                    dest = h.DST / name
                    if old.exists():
                        shutil.copy2(old, dest)
                    elif dest.exists():
                        dest.unlink()
                deployed = False
            except Exception as e:
                evidence["rollback_error"] = type(e).__name__ + ": " + str(e)

        if lease and not h.base.process_pids():
            try:
                evidence["lease_release_after_failure"] = h.base.guard(
                    "release", OWNER, operation
                )
                lease = False
            except Exception as e:
                evidence["lease_release_error"] = type(e).__name__ + ": " + str(e)

        if not h.base.process_pids():
            try:
                if source.exists():
                    source.unlink()
            except Exception:
                pass

        h.base.atomic_json(validation / "validation.json", evidence)
        print(json.dumps({
            "validation": str(validation / "validation.json"),
            "result": "FAIL",
            "error": evidence["error"],
        }, indent=2))
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
