from pathlib import Path
import base64
import datetime
import hashlib
import json
import secrets
import shutil
import subprocess
import sys

ROOT = Path(r"D:\BannerlordAIResearch")
sys.path.insert(0, str(ROOT / "workspace"))

import validate_v02146_provider_dispatch_outbox as v46
import validate_v02150_provider_job_claim as v50
import validate_v02155_provider_request_registration as v55
import validate_v02158_provider_transport_registration as v58
import provider_request_contract_v02154 as contract

h = v46.h
CLAN_SRC = v46.CLAN_SRC
RUNNER_SRC = ROOT / r"workspace\_PatchStaging\AutonomousOperator_v002161_ExecutionPolicy_20260921_2041\candidate"
CLAN_SHA = v46.CLAN_SHA
RUNNER_SHA = "70F2733153CC21D910C4D424DD5BABA429EE54C93FEE02376D0C6FD41D81621B"
OWNER = "sol_provider_execution_policy_v02161"
SOURCE_PREFIX = "ClanAI V02161 EXECUTION POLICY SOURCE "
FIXTURE_PROJECT = ROOT / r"workspace\_PatchStaging\AutonomousOperator_v002161_ExecutionPolicy_20260921_2041\fixtures\Fixtures.csproj"
V3_FIXTURE_PROJECT = ROOT / r"workspace\_PatchStaging\AutonomousOperator_v002161_ExecutionPolicy_20260921_2041\fixtures_v3\FixturesV3.csproj"
CONTRACT_FIXTURE = ROOT / r"workspace\test_provider_request_contract_v02154.py"

OUTBOX = v46.OUTBOX
CLAIMS = v50.CLAIMS
REQUEST_REGISTRATIONS = v55.REGISTRATIONS
TRANSPORT_REGISTRATIONS = v58.TRANSPORT_REGISTRATIONS
POLICY_REGISTRATIONS = ROOT / r"Automation\TestRunner\patrol_defense_provider_execution_policy_registrations.jsonl"
PROVIDER_INVOCATIONS = v46.PROVIDER_INVOCATIONS
RESULT_V1 = ROOT / r"Automation\TestRunner\patrol_defense_provider_result_admissions.jsonl"
RESULT_V2 = ROOT / r"Automation\TestRunner\patrol_defense_provider_result_v2_admissions.jsonl"
RESULT_V3 = ROOT / r"Automation\TestRunner\patrol_defense_provider_result_v3_admissions.jsonl"
BINDINGS = ROOT / r"Automation\TestRunner\patrol_defense_provider_transport_result_bindings.jsonl"

PROVIDER_ID = "deterministic_external_worker"
MODEL_ID = "deterministic_mock_no_model"
TRANSPORT_ID = "deterministic_local_loopback"

TEST_TIMEOUT_MS = "1000"
TEST_MAX_ATTEMPTS = "1"
TEST_MAX_RESULT_BYTES = "65536"
TEST_CREDENTIAL_REF = "env:BANNERLORDAI_TEST_PROVIDER_KEY"

h.CLAN_SRC = CLAN_SRC
h.RUNNER_SRC = RUNNER_SRC
h.NEW_CLAN = CLAN_SHA
h.NEW_RUNNER = RUNNER_SHA


def policy_envelope(provider_registration, transport_registration, request_envelope):
    envelope = {
        "schema": "BannerlordAI.PatrolDefenseProviderExecutionPolicy.v1",
        "transportRequestId": transport_registration["transportRequestId"],
        "providerRequestId": provider_registration["providerRequestId"],
        "providerId": request_envelope["providerId"],
        "modelId": request_envelope["modelId"],
        "attemptId": request_envelope["attemptId"],
        "requestFingerprint": request_envelope["requestFingerprint"],
        "timeoutMs": TEST_TIMEOUT_MS,
        "maxAttempts": TEST_MAX_ATTEMPTS,
        "maxResultBytes": TEST_MAX_RESULT_BYTES,
        "credentialRef": TEST_CREDENTIAL_REF,
    }
    raw = json.dumps(envelope, separators=(",", ":"), ensure_ascii=False)
    return envelope, raw


def policy_checks(row, envelope, expected_id, idempotent):
    expected_reason = (
        "SAME_EXECUTION_POLICY_ALREADY_REGISTERED"
        if idempotent
        else "REGISTERED"
    )
    return {
        "schema": row.get("schema")
        == "BannerlordAI.PatrolDefenseProviderExecutionPolicyRegistration.v1",
        "mode": row.get("mode") == "observe",
        "execution_policy_id": row.get("executionPolicyId") == expected_id,
        "transport_request_id": row.get("transportRequestId")
        == envelope.get("transportRequestId"),
        "provider_request_id": row.get("providerRequestId")
        == envelope.get("providerRequestId"),
        "provider": row.get("providerId") == envelope.get("providerId"),
        "model": row.get("modelId") == envelope.get("modelId"),
        "attempt": row.get("attemptId") == envelope.get("attemptId"),
        "fingerprint": row.get("requestFingerprint")
        == envelope.get("requestFingerprint"),
        "timeout": row.get("timeoutMs") == TEST_TIMEOUT_MS,
        "max_attempts": row.get("maxAttempts") == TEST_MAX_ATTEMPTS,
        "max_result_bytes": row.get("maxResultBytes") == TEST_MAX_RESULT_BYTES,
        "credential_ref": row.get("credentialRef") == TEST_CREDENTIAL_REF,
        "registered": row.get("registered") is (not idempotent),
        "idempotent": row.get("idempotent") is idempotent,
        "reason": row.get("reason") == expected_reason,
        "queue_count": row.get("queueCount") == 1,
        "network_false": row.get("externalNetworkUsed") is False,
        "model_false": row.get("modelInvoked") is False,
        "llm_false": row.get("llmInvoked") is False,
        "planner_false": row.get("plannerInvoked") is False,
        "execution_false": row.get("executionAuthorized") is False,
        "behavior_false": row.get("behaviorMutation") is False,
        "intent_false": row.get("intentMutation") is False,
        "score_false": row.get("scoreMutation") is False,
        "movement_zero": row.get("nativeMovementCalls") == 0,
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
    policy_source = (
        RUNNER_SRC / "PatrolDefenseProviderExecutionPolicyRegistration.cs"
    ).read_text(encoding="utf-8", errors="replace")

    static_checks = {
        "policy_class": "PatrolDefenseProviderExecutionPolicyRegistration"
        in policy_source,
        "policy_schema": "BannerlordAI.PatrolDefenseProviderExecutionPolicy.v1"
        in policy_source,
        "policy_receipt": "BannerlordAI.PatrolDefenseProviderExecutionPolicyRegistration.v1"
        in policy_source,
        "queue_method": "RegisterExecutionPolicy(" in dispatch_source,
        "job_policy_id": "ExecutionPolicyId" in dispatch_source,
        "command": "PATROL_DEFENSE_PROVIDER_EXECUTION_POLICY_REGISTER "
        in runner_source,
        "path": "patrol_defense_provider_execution_policy_registrations.jsonl"
        in runner_source,
        "release_clears": "job.ExecutionPolicyId = null;" in dispatch_source
        and "job.ExecutionPolicyCredentialRef = null;" in dispatch_source,
        "positive_int_parse": "PositiveInt(" in policy_source,
        "credential_ref_rule": '"env:"' in policy_source,
        "no_env_read": "GetEnvironmentVariable" not in policy_source
        and "GetEnvironmentVariable" not in runner_source,
        "no_http": "HttpClient" not in policy_source,
        "no_socket": "Socket" not in policy_source,
        "no_openai": "OpenAI" not in policy_source,
        "no_model_api": "ChatCompletion" not in policy_source,
        "no_apply": "PatrolDefenseApplyGate" not in policy_source,
        "no_score": "SetBehaviorScore" not in policy_source,
        "no_memory_write": "RecordPatrolDefenseEpisode" not in policy_source,
    }
    if not all(static_checks.values()):
        raise RuntimeError("static checks " + json.dumps(static_checks, sort_keys=True))

    fixture = subprocess.run(
        ["dotnet", "run", "--project", str(FIXTURE_PROJECT), "-c", "Release"],
        capture_output=True,
        text=True,
    )
    fixture_result = (fixture.stdout or "").strip()
    if fixture.returncode != 0 or "PASS_FIXTURES checks=257" not in fixture_result:
        raise RuntimeError(
            "runner fixture gate failed "
            + fixture_result
            + " "
            + (fixture.stderr or "")
        )

    v3_fixture = subprocess.run(
        ["dotnet", "run", "--project", str(V3_FIXTURE_PROJECT), "-c", "Release"],
        capture_output=True,
        text=True,
    )
    v3_fixture_result = (v3_fixture.stdout or "").strip()
    if (
        v3_fixture.returncode != 0
        or "PASS_V3_FIXTURES checks=63" not in v3_fixture_result
    ):
        raise RuntimeError(
            "v3/hash-binding fixture gate failed "
            + v3_fixture_result
            + " "
            + (v3_fixture.stderr or "")
        )

    contract_fixture = subprocess.run(
        [sys.executable, "-X", "utf8", str(CONTRACT_FIXTURE)],
        capture_output=True,
        text=True,
    )
    contract_fixture_result = (contract_fixture.stdout or "").strip()
    if (
        contract_fixture.returncode != 0
        or "PASS_FIXTURES checks=49" not in contract_fixture_result
    ):
        raise RuntimeError(
            "contract fixture gate failed "
            + contract_fixture_result
            + " "
            + (contract_fixture.stderr or "")
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
        "PatrolDefense_v02161_ExecutionPolicy_" + stamp
    )
    validation.mkdir(parents=True)
    backup = ROOT / "workspace/_PatchBackups" / (
        "Before_ExecutionPolicy_" + stamp
    )
    backup.mkdir(parents=True)

    operation = "provider-execution-policy-v02161-" + stamp
    token = secrets.token_hex(32)
    token_hash = hashlib.sha256(token.encode()).hexdigest().upper()

    evidence = {
        "schema": "BannerlordAI.ProviderExecutionPolicyValidation.v1",
        "created_local": datetime.datetime.now().astimezone().isoformat(),
        "result": "IN_PROGRESS",
        "source_slot": source_slot,
        "clanai_test_sha256": CLAN_SHA,
        "runner_test_sha256": RUNNER_SHA,
        "runner_fixtures": fixture_result,
        "v3_hash_binding_fixtures": v3_fixture_result,
        "request_contract_fixtures": contract_fixture_result,
        "static_checks": static_checks,
        "test_policy": {
            "timeoutMs": TEST_TIMEOUT_MS,
            "maxAttempts": TEST_MAX_ATTEMPTS,
            "maxResultBytes": TEST_MAX_RESULT_BYTES,
            "credentialRef": TEST_CREDENTIAL_REF,
            "scope": "TEST_ONLY_NOT_PRODUCT_DEFAULTS",
        },
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
            "patrol_defense_provider_execution_policy_shadow",
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

        h.clear_runtime_outputs()
        for path in (
            OUTBOX,
            CLAIMS,
            REQUEST_REGISTRATIONS,
            TRANSPORT_REGISTRATIONS,
            POLICY_REGISTRATIONS,
            PROVIDER_INVOCATIONS,
            RESULT_V1,
            RESULT_V2,
            RESULT_V3,
            BINDINGS,
        ):
            if path.exists():
                path.unlink()

        run = h.base.bus.start_run(
            issuer="provider_execution_policy_live",
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
                "v02161 execution policy environmental inconclusive",
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
            claim_rows = h.read_jsonl(CLAIMS)
            if len(claim_rows) <= before_claim:
                raise RuntimeError("claim receipt missing")
            claim = claim_rows[-1]
            claim_checks = v50.claim_checks(
                claim,
                fingerprint,
                "CLAIMED",
                True,
                False,
                "PENDING",
                "CLAIMED",
            )

            request_envelope, request_envelope_json = contract.build_provider_request(
                job_line=job_line,
                claim=claim,
                provider_id=PROVIDER_ID,
                model_id=MODEL_ID,
            )
            expected_provider_request_id = hashlib.sha256(
                request_envelope_json.encode("utf-8")
            ).hexdigest().upper()
            request_register_command = (
                "PATROL_DEFENSE_PROVIDER_REQUEST_REGISTER "
                + base64.b64encode(
                    request_envelope_json.encode("utf-8")
                ).decode("ascii")
            )

            before_request_reg = len(h.read_jsonl(REQUEST_REGISTRATIONS))
            h.send_wait(
                OWNER,
                run,
                request_register_command,
                "patrol_defense_provider_request_registered",
                10,
            )
            request_reg_rows = h.read_jsonl(REQUEST_REGISTRATIONS)
            if len(request_reg_rows) <= before_request_reg:
                raise RuntimeError("provider request registration receipt missing")
            request_registration = request_reg_rows[-1]
            request_reg_checks = v55.registration_checks(
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
            transport_command = (
                "PATROL_DEFENSE_PROVIDER_TRANSPORT_REGISTER "
                + base64.b64encode(
                    transport_json.encode("utf-8")
                ).decode("ascii")
            )

            before_transport = len(h.read_jsonl(TRANSPORT_REGISTRATIONS))
            h.send_wait(
                OWNER,
                run,
                transport_command,
                "patrol_defense_provider_transport_registered",
                10,
            )
            transport_rows = h.read_jsonl(TRANSPORT_REGISTRATIONS)
            if len(transport_rows) <= before_transport:
                raise RuntimeError("transport registration receipt missing")
            transport_registration = transport_rows[-1]
            transport_checks = v58.transport_checks(
                transport_registration,
                transport,
                expected_transport_request_id,
                False,
            )

            policy, policy_json = policy_envelope(
                request_registration,
                transport_registration,
                request_envelope,
            )
            expected_policy_id = hashlib.sha256(
                policy_json.encode("utf-8")
            ).hexdigest().upper()
            policy_command = (
                "PATROL_DEFENSE_PROVIDER_EXECUTION_POLICY_REGISTER "
                + base64.b64encode(
                    policy_json.encode("utf-8")
                ).decode("ascii")
            )

            before_policy = len(h.read_jsonl(POLICY_REGISTRATIONS))
            h.send_wait(
                OWNER,
                run,
                policy_command,
                "patrol_defense_provider_execution_policy_registered",
                10,
            )
            policy_rows = h.read_jsonl(POLICY_REGISTRATIONS)
            if len(policy_rows) <= before_policy:
                raise RuntimeError("execution policy registration receipt missing")
            first_policy = policy_rows[-1]
            first_policy_checks = policy_checks(
                first_policy,
                policy,
                expected_policy_id,
                False,
            )

            before_replay = len(policy_rows)
            h.send_wait(
                OWNER,
                run,
                policy_command,
                "patrol_defense_provider_execution_policy_registration_idempotent",
                10,
            )
            replay_rows = h.read_jsonl(POLICY_REGISTRATIONS)
            if len(replay_rows) <= before_replay:
                raise RuntimeError("execution policy replay receipt missing")
            replay = replay_rows[-1]
            replay_checks = policy_checks(
                replay,
                policy,
                expected_policy_id,
                True,
            )

            checks = {}
            checks.update({"claim_" + k: v for k, v in claim_checks.items()})
            checks.update(
                {"provider_request_" + k: v for k, v in request_reg_checks.items()}
            )
            checks.update(
                {"transport_" + k: v for k, v in transport_checks.items()}
            )
            checks.update(
                {"policy_first_" + k: v for k, v in first_policy_checks.items()}
            )
            checks.update(
                {"policy_replay_" + k: v for k, v in replay_checks.items()}
            )
            checks["policy_id_independent"] = (
                first_policy.get("executionPolicyId")
                == hashlib.sha256(policy_json.encode("utf-8")).hexdigest().upper()
            )
            checks["provider_invocation_rows_empty"] = (
                len(h.read_jsonl(PROVIDER_INVOCATIONS)) == 0
            )
            checks["result_v1_rows_empty"] = len(h.read_jsonl(RESULT_V1)) == 0
            checks["result_v2_rows_empty"] = len(h.read_jsonl(RESULT_V2)) == 0
            checks["result_v3_rows_empty"] = len(h.read_jsonl(RESULT_V3)) == 0
            checks["binding_rows_empty"] = len(h.read_jsonl(BINDINGS)) == 0
            checks["no_apply_side_effects"] = h.no_apply_side_effects()

            if not all(checks.values()):
                raise RuntimeError(
                    "execution policy live checks "
                    + json.dumps(checks, sort_keys=True)
                )

            evidence["shadow_receipt"] = row
            evidence["provider_job_claim"] = claim
            evidence["provider_request_envelope"] = request_envelope
            evidence["provider_request_registration"] = request_registration
            evidence["transport_request_envelope"] = transport
            evidence["transport_registration"] = transport_registration
            evidence["execution_policy_envelope"] = policy
            evidence["execution_policy_registration"] = first_policy
            evidence["execution_policy_registration_replay"] = replay
            evidence["independent_execution_policy_id"] = expected_policy_id
            evidence["live_checks"] = checks
            evidence["result"] = "PASS_PROVIDER_EXECUTION_POLICY_REGISTRATION_SHADOW"
            h.base.atomic_json(validation / "validation.json", evidence)

            h.safe_exit(
                OWNER,
                token,
                run,
                "v02161 provider execution policy verified",
            )
            run = None

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

        evidence.update(
            {
                "protected_saves_match": h.sha(root) == h.SAVE_HASH
                and h.sha(blood) == h.SAVE_HASH,
                "disposable_saves_cleaned": not source.exists(),
                "clanai_restored_sha256": h.sha(h.DST / "ClanAI.dll"),
                "runner_restored_sha256": h.sha(
                    h.DST / "BannerlordAITestRunner.dll"
                ),
            }
        )

        h.base.guard("phase", OWNER, "EVIDENCE_FROZEN", operation)
        evidence["lease_release"] = h.base.guard("release", OWNER, operation)
        lease = False
        evidence["completed_local"] = datetime.datetime.now().astimezone().isoformat()
        h.base.atomic_json(validation / "validation.json", evidence)

        print(
            json.dumps(
                {
                    "validation": str(validation / "validation.json"),
                    "result": evidence["result"],
                    "executionPolicyId": None
                    if row is None
                    else first_policy.get("executionPolicyId"),
                    "reason": None
                    if row is None
                    else first_policy.get("reason"),
                    "replayReason": None
                    if row is None
                    else replay.get("reason"),
                },
                indent=2,
            )
        )
        return (
            0
            if evidence["result"]
            == "PASS_PROVIDER_EXECUTION_POLICY_REGISTRATION_SHADOW"
            else 2
        )

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
                    "v02161 execution policy failure",
                )
                run = None
            except Exception as e:
                evidence["exit_cleanup_error"] = type(e).__name__ + ": " + str(e)

        if run is not None and not h.base.process_pids():
            try:
                h.base.bus.close_run("v02161 pre-ready failure")
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
        print(
            json.dumps(
                {
                    "validation": str(validation / "validation.json"),
                    "result": "FAIL",
                    "error": evidence["error"],
                },
                indent=2,
            )
        )
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
