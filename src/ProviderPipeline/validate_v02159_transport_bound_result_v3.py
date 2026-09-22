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
import validate_v02149_provider_work_lifecycle as v49
import validate_v02150_provider_job_claim as v50
import validate_v02155_provider_request_registration as v55
import validate_v02158_provider_transport_registration as v58
import provider_request_contract_v02154 as request_contract
import provider_transport_loopback_v02159 as transport_v3

h = v46.h
CLAN_SRC = v46.CLAN_SRC
RUNNER_SRC = ROOT / r"workspace\_PatchStaging\AutonomousOperator_v002159_TransportBoundResultV3_20260921_1658\candidate"
CLAN_SHA = v46.CLAN_SHA
RUNNER_SHA = "3445F0843F2CB739641CF5448E011D97CC385DD85C2B0FAC80B912473ED5EAED"
OWNER = "sol_transport_bound_result_v3_v02159"
SOURCE_PREFIX = "ClanAI V02159 RESULT V3 SOURCE "
BACKCOMPAT_FIXTURES = ROOT / r"workspace\_PatchStaging\AutonomousOperator_v002159_TransportBoundResultV3_20260921_1658\fixtures\Fixtures.csproj"
V3_FIXTURES = ROOT / r"workspace\_PatchStaging\AutonomousOperator_v002159_TransportBoundResultV3_20260921_1658\fixtures_v3\FixturesV3.csproj"
CONTRACT_FIXTURE = ROOT / r"workspace\test_provider_request_contract_v02154.py"
TRANSPORT_FIXTURE = ROOT / r"workspace\test_provider_transport_loopback_v02159.py"

OUTBOX = v46.OUTBOX
CLAIMS = v50.CLAIMS
REQUEST_REGISTRATIONS = v55.REGISTRATIONS
TRANSPORT_REGISTRATIONS = v58.TRANSPORT_REGISTRATIONS
RESULT_V3 = ROOT / r"Automation\TestRunner\patrol_defense_provider_result_v3_admissions.jsonl"
COMPLETIONS = v49.COMPLETIONS
PROVIDER_INVOCATIONS = v46.PROVIDER_INVOCATIONS

PROVIDER_ID = "deterministic_external_worker"
MODEL_ID = "deterministic_mock_no_model"
TRANSPORT_ID = "deterministic_local_loopback"

h.CLAN_SRC = CLAN_SRC
h.RUNNER_SRC = RUNNER_SRC
h.NEW_CLAN = CLAN_SHA
h.NEW_RUNNER = RUNNER_SHA


def result_v3_checks(row, result, transport_receipt):
    admission = row.get("advisoryAdmission")
    return {
        "schema": row.get("schema")
        == "BannerlordAI.PatrolDefenseProviderResultV3Admission.v1",
        "mode": row.get("mode") == "observe",
        "provider": row.get("providerId") == result.get("providerId"),
        "attempt": row.get("attemptId") == result.get("attemptId"),
        "fingerprint": row.get("requestFingerprint")
        == result.get("requestFingerprint"),
        "provider_request_id": row.get("providerRequestId")
        == result.get("providerRequestId")
        == transport_receipt.get("providerRequestId"),
        "transport_request_id": row.get("transportRequestId")
        == result.get("transportRequestId")
        == transport_receipt.get("transportRequestId"),
        "status": row.get("status") == "SUCCESS",
        "claim_match": row.get("claimMatchReason") == "CLAIM_MATCHED",
        "provider_request_match": row.get("providerRequestMatchReason")
        == "PROVIDER_REQUEST_MATCHED",
        "transport_request_match": row.get("transportRequestMatchReason")
        == "TRANSPORT_REQUEST_MATCHED",
        "accepted": row.get("providerResultAccepted") is True,
        "retryable_false": row.get("retryable") is False,
        "no_rejections": row.get("rejectionReasons") == [],
        "advisory_present": isinstance(admission, dict),
        "advisory_admitted": isinstance(admission, dict)
        and admission.get("admitted") is True,
        "advisory_disposition": isinstance(admission, dict)
        and admission.get("disposition") == "KEEP_BASELINE",
        "execution_false": row.get("executionAuthorized") is False,
        "network_false": row.get("externalNetworkUsed") is False,
        "model_false": row.get("modelInvoked") is False,
        "llm_false": row.get("llmInvoked") is False,
        "planner_false": row.get("plannerInvoked") is False,
        "behavior_false": row.get("behaviorMutation") is False,
        "intent_false": row.get("intentMutation") is False,
        "score_false": row.get("scoreMutation") is False,
        "movement_zero": row.get("nativeMovementCalls") == 0,
    }


def replay_v3_checks(row, fingerprint):
    reasons = row.get("rejectionReasons")
    return {
        "schema": row.get("schema")
        == "BannerlordAI.PatrolDefenseProviderResultV3Admission.v1",
        "fingerprint": row.get("requestFingerprint") == fingerprint,
        "accepted_false": row.get("providerResultAccepted") is False,
        "claim_job_missing": row.get("claimMatchReason")
        == "DISPATCH_JOB_NOT_FOUND",
        "provider_request_match_null": row.get("providerRequestMatchReason") is None,
        "transport_match_null": row.get("transportRequestMatchReason") is None,
        "job_missing_reason": isinstance(reasons, list)
        and "DISPATCH_JOB_NOT_FOUND" in reasons,
        "advisory_null": row.get("advisoryAdmission") is None,
        "execution_false": row.get("executionAuthorized") is False,
        "model_false": row.get("modelInvoked") is False,
        "movement_zero": row.get("nativeMovementCalls") == 0,
    }


def transport_receipt_checks(receipt, provider_registration, transport_registration, result_json):
    expected_result_sha = hashlib.sha256(
        result_json.encode("utf-8")
    ).hexdigest().upper()
    return {
        "schema": receipt.get("schema")
        == "BannerlordAI.PatrolDefenseProviderTransportReceipt.v2",
        "transport": receipt.get("transportId") == TRANSPORT_ID,
        "transport_request_id": receipt.get("transportRequestId")
        == transport_registration.get("transportRequestId"),
        "provider_request_id": receipt.get("providerRequestId")
        == provider_registration.get("providerRequestId"),
        "provider": receipt.get("providerId") == PROVIDER_ID,
        "model": receipt.get("modelId") == MODEL_ID,
        "attempt": receipt.get("attemptId") == "attempt-1",
        "status": receipt.get("resultStatus") == "SUCCESS",
        "result_sha": receipt.get("resultSha256") == expected_result_sha,
        "network_false": receipt.get("externalNetworkUsed") is False,
        "model_false": receipt.get("modelInvoked") is False,
        "success": receipt.get("success") is True,
        "error_null": receipt.get("errorCode") is None,
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
    v3_source = (RUNNER_SRC / "PatrolDefenseProviderResultV3Admission.cs").read_text(
        encoding="utf-8", errors="replace"
    )
    transport_helper_source = (
        ROOT / r"workspace\provider_transport_loopback_v02159.py"
    ).read_text(encoding="utf-8", errors="replace")

    claim_pos = v3_source.find("dispatchQueue.MatchClaim(")
    request_pos = v3_source.find("dispatchQueue.MatchRegisteredProviderRequest(")
    transport_pos = v3_source.find("dispatchQueue.MatchRegisteredTransportRequest(")
    gate_pos = v3_source.find("gate.PendingRequestFingerprint")

    static_checks = {
        "v1_preserved": "PATROL_DEFENSE_PROVIDER_RESULT_ADMIT " in runner_source,
        "v2_preserved": "PATROL_DEFENSE_PROVIDER_RESULT_V2_ADMIT " in runner_source,
        "v3_command": "PATROL_DEFENSE_PROVIDER_RESULT_V3_ADMIT " in runner_source,
        "v3_path": "patrol_defense_provider_result_v3_admissions.jsonl"
        in runner_source,
        "v3_schema": "BannerlordAI.PatrolDefenseProviderResult.v3" in v3_source,
        "v3_receipt": "BannerlordAI.PatrolDefenseProviderResultV3Admission.v1"
        in v3_source,
        "transport_match_method": "MatchRegisteredTransportRequest("
        in dispatch_source,
        "transport_not_registered": "TRANSPORT_REQUEST_NOT_REGISTERED"
        in dispatch_source,
        "transport_id_mismatch": "TRANSPORT_REQUEST_ID_MISMATCH" in dispatch_source,
        "transport_matched": "TRANSPORT_REQUEST_MATCHED" in dispatch_source,
        "ordering": claim_pos >= 0
        and request_pos > claim_pos
        and transport_pos > request_pos
        and gate_pos > transport_pos,
        "loopback_v3_schema": "BannerlordAI.PatrolDefenseProviderResult.v3"
        in transport_helper_source,
        "loopback_transport_receipt_v2":
        "BannerlordAI.PatrolDefenseProviderTransportReceipt.v2"
        in transport_helper_source,
        "no_http": "HttpClient" not in v3_source
        and "import requests" not in transport_helper_source
        and "import httpx" not in transport_helper_source
        and "urllib.request" not in transport_helper_source,
        "no_socket": "Socket" not in v3_source
        and "import socket" not in transport_helper_source,
        "no_openai": "OpenAI" not in v3_source
        and "openai" not in transport_helper_source.lower(),
        "no_apply": "PatrolDefenseApplyGate" not in v3_source,
        "no_score": "SetBehaviorScore" not in v3_source,
        "no_memory_write": "RecordPatrolDefenseEpisode" not in v3_source,
    }
    if not all(static_checks.values()):
        raise RuntimeError("static checks " + json.dumps(static_checks, sort_keys=True))

    backcompat = subprocess.run(
        ["dotnet", "run", "--project", str(BACKCOMPAT_FIXTURES), "-c", "Release"],
        capture_output=True,
        text=True,
    )
    backcompat_result = (backcompat.stdout or "").strip()
    if backcompat.returncode != 0 or "PASS_FIXTURES checks=225" not in backcompat_result:
        raise RuntimeError(
            "backcompat fixtures failed "
            + backcompat_result
            + " "
            + (backcompat.stderr or "")
        )

    v3_fixture = subprocess.run(
        ["dotnet", "run", "--project", str(V3_FIXTURES), "-c", "Release"],
        capture_output=True,
        text=True,
    )
    v3_fixture_result = (v3_fixture.stdout or "").strip()
    if v3_fixture.returncode != 0 or "PASS_V3_FIXTURES checks=40" not in v3_fixture_result:
        raise RuntimeError(
            "v3 fixtures failed "
            + v3_fixture_result
            + " "
            + (v3_fixture.stderr or "")
        )

    contract_fixture = subprocess.run(
        [sys.executable, "-X", "utf8", str(CONTRACT_FIXTURE)],
        capture_output=True,
        text=True,
    )
    contract_result = (contract_fixture.stdout or "").strip()
    if contract_fixture.returncode != 0 or "PASS_FIXTURES checks=49" not in contract_result:
        raise RuntimeError(
            "request contract fixtures failed "
            + contract_result
            + " "
            + (contract_fixture.stderr or "")
        )

    transport_fixture = subprocess.run(
        [
            sys.executable,
            "-X",
            "utf8",
            str(ROOT / r"workspace\test_provider_transport_loopback_v02159.py"),
        ],
        capture_output=True,
        text=True,
    )
    transport_fixture_result = (transport_fixture.stdout or "").strip()
    if (
        transport_fixture.returncode != 0
        or "PASS_FIXTURES checks=73" not in transport_fixture_result
    ):
        raise RuntimeError(
            "v3 transport fixtures failed "
            + transport_fixture_result
            + " "
            + (transport_fixture.stderr or "")
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
        "PatrolDefense_v02159_TransportBoundProviderResultV3_" + stamp
    )
    validation.mkdir(parents=True)
    backup = ROOT / "workspace/_PatchBackups" / (
        "Before_TransportBoundProviderResultV3_" + stamp
    )
    backup.mkdir(parents=True)

    operation = "transport-bound-provider-result-v3-v02159-" + stamp
    token = secrets.token_hex(32)
    token_hash = hashlib.sha256(token.encode()).hexdigest().upper()

    evidence = {
        "schema": "BannerlordAI.TransportBoundProviderResultV3Validation.v1",
        "created_local": datetime.datetime.now().astimezone().isoformat(),
        "result": "IN_PROGRESS",
        "source_slot": source_slot,
        "clanai_test_sha256": CLAN_SHA,
        "runner_test_sha256": RUNNER_SHA,
        "backcompat_fixtures": backcompat_result,
        "v3_fixtures": v3_fixture_result,
        "request_contract_fixtures": contract_result,
        "transport_v3_fixtures": transport_fixture_result,
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
            "patrol_defense_transport_bound_result_v3_shadow",
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
            RESULT_V3,
            COMPLETIONS,
            PROVIDER_INVOCATIONS,
        ):
            if path.exists():
                path.unlink()

        run = h.base.bus.start_run(
            issuer="transport_bound_result_v3_live",
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
                "v02159 transport-bound v3 environmental inconclusive",
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

            provider_request, provider_request_json = request_contract.build_provider_request(
                job_line=job_line,
                claim=claim,
                provider_id=PROVIDER_ID,
                model_id=MODEL_ID,
            )
            expected_provider_request_id = hashlib.sha256(
                provider_request_json.encode("utf-8")
            ).hexdigest().upper()
            provider_command = (
                "PATROL_DEFENSE_PROVIDER_REQUEST_REGISTER "
                + base64.b64encode(provider_request_json.encode("utf-8")).decode("ascii")
            )
            before_provider_reg = len(h.read_jsonl(REQUEST_REGISTRATIONS))
            h.send_wait(
                OWNER,
                run,
                provider_command,
                "patrol_defense_provider_request_registered",
                10,
            )
            provider_rows = h.read_jsonl(REQUEST_REGISTRATIONS)
            if len(provider_rows) <= before_provider_reg:
                raise RuntimeError("provider request registration receipt missing")
            provider_registration = provider_rows[-1]
            provider_checks = v55.registration_checks(
                provider_registration,
                provider_request,
                expected_provider_request_id,
                False,
            )
            if not all(provider_checks.values()):
                raise RuntimeError(
                    "provider registration checks "
                    + json.dumps(provider_checks, sort_keys=True)
                )

            transport_request, transport_request_json = v58.transport_envelope(
                provider_registration,
                provider_request,
            )
            expected_transport_request_id = hashlib.sha256(
                transport_request_json.encode("utf-8")
            ).hexdigest().upper()
            transport_command = (
                "PATROL_DEFENSE_PROVIDER_TRANSPORT_REGISTER "
                + base64.b64encode(transport_request_json.encode("utf-8")).decode("ascii")
            )
            before_transport_reg = len(h.read_jsonl(TRANSPORT_REGISTRATIONS))
            h.send_wait(
                OWNER,
                run,
                transport_command,
                "patrol_defense_provider_transport_registered",
                10,
            )
            transport_rows = h.read_jsonl(TRANSPORT_REGISTRATIONS)
            if len(transport_rows) <= before_transport_reg:
                raise RuntimeError("transport registration receipt missing")
            transport_registration = transport_rows[-1]
            transport_checks = v58.transport_checks(
                transport_registration,
                transport_request,
                expected_transport_request_id,
                False,
            )
            if not all(transport_checks.values()):
                raise RuntimeError(
                    "transport registration checks "
                    + json.dumps(transport_checks, sort_keys=True)
                )

            outcome = transport_v3.send_loopback_v3(
                provider_request_json,
                provider_registration,
                transport_request_json,
                transport_registration,
            )
            if not outcome.success:
                raise RuntimeError(
                    "v3 transport failed " + json.dumps(outcome.reasons)
                )
            result_json = outcome.result_json
            result = outcome.result
            transport_receipt = outcome.receipt
            transport_receipt_live_checks = transport_receipt_checks(
                transport_receipt,
                provider_registration,
                transport_registration,
                result_json,
            )
            if not all(transport_receipt_live_checks.values()):
                raise RuntimeError(
                    "transport receipt checks "
                    + json.dumps(transport_receipt_live_checks, sort_keys=True)
                )

            independent_result_sha = hashlib.sha256(
                result_json.encode("utf-8")
            ).hexdigest().upper()
            if independent_result_sha != transport_receipt.get("resultSha256"):
                raise RuntimeError("independent result SHA mismatch")

            result_command = (
                "PATROL_DEFENSE_PROVIDER_RESULT_V3_ADMIT "
                + base64.b64encode(result_json.encode("utf-8")).decode("ascii")
            )

            before_result = len(h.read_jsonl(RESULT_V3))
            before_completion = len(h.read_jsonl(COMPLETIONS))
            h.send_wait(
                OWNER,
                run,
                result_command,
                "patrol_defense_provider_result_v3_admitted",
                10,
            )
            result_rows = h.read_jsonl(RESULT_V3)
            completion_rows = h.read_jsonl(COMPLETIONS)
            if len(result_rows) <= before_result:
                raise RuntimeError("v3 admission receipt missing")
            if len(completion_rows) <= before_completion:
                raise RuntimeError("completion receipt missing")

            admitted = result_rows[-1]
            completion = completion_rows[-1]
            admitted_checks = result_v3_checks(
                admitted,
                result,
                transport_receipt,
            )
            completion_checks = v49.completion_success_checks(
                completion,
                fingerprint,
            )
            if not all(admitted_checks.values()):
                raise RuntimeError(
                    "v3 admission checks "
                    + json.dumps(admitted_checks, sort_keys=True)
                )
            if not all(completion_checks.values()):
                raise RuntimeError(
                    "completion checks "
                    + json.dumps(completion_checks, sort_keys=True)
                )

            before_replay_result = len(result_rows)
            before_replay_completion = len(completion_rows)
            h.send_wait(
                OWNER,
                run,
                result_command,
                "patrol_defense_provider_result_v3_rejected:DISPATCH_JOB_NOT_FOUND",
                10,
            )
            replay_rows = h.read_jsonl(RESULT_V3)
            replay_completion_rows = h.read_jsonl(COMPLETIONS)
            if len(replay_rows) <= before_replay_result:
                raise RuntimeError("v3 replay receipt missing")
            if len(replay_completion_rows) <= before_replay_completion:
                raise RuntimeError("v3 replay completion missing")

            replay = replay_rows[-1]
            replay_completion = replay_completion_rows[-1]
            replay_checks = replay_v3_checks(replay, fingerprint)
            replay_completion_checks = v49.completion_replay_checks(
                replay_completion,
                fingerprint,
            )
            if not all(replay_checks.values()):
                raise RuntimeError(
                    "v3 replay checks "
                    + json.dumps(replay_checks, sort_keys=True)
                )
            if not all(replay_completion_checks.values()):
                raise RuntimeError(
                    "v3 replay completion checks "
                    + json.dumps(replay_completion_checks, sort_keys=True)
                )

            checks = {}
            checks.update({"claim_" + k: v for k, v in claim_checks.items()})
            checks.update({"provider_request_" + k: v for k, v in provider_checks.items()})
            checks.update({"transport_registration_" + k: v for k, v in transport_checks.items()})
            checks.update({"transport_receipt_" + k: v for k, v in transport_receipt_live_checks.items()})
            checks.update({"result_" + k: v for k, v in admitted_checks.items()})
            checks.update({"completion_" + k: v for k, v in completion_checks.items()})
            checks.update({"replay_" + k: v for k, v in replay_checks.items()})
            checks.update({
                "replay_completion_" + k: v
                for k, v in replay_completion_checks.items()
            })
            checks["provider_invocation_rows_empty"] = (
                len(h.read_jsonl(PROVIDER_INVOCATIONS)) == 0
            )
            checks["no_apply_side_effects"] = h.no_apply_side_effects()

            if not all(checks.values()):
                raise RuntimeError(
                    "v3 live checks " + json.dumps(checks, sort_keys=True)
                )

            evidence["shadow_receipt"] = row
            evidence["provider_job_claim"] = claim
            evidence["provider_request_envelope"] = provider_request
            evidence["provider_request_registration"] = provider_registration
            evidence["transport_request_envelope"] = transport_request
            evidence["transport_registration"] = transport_registration
            evidence["transport_receipt"] = transport_receipt
            evidence["provider_result_v3_json"] = result_json
            evidence["provider_result_v3"] = result
            evidence["independent_result_sha256"] = independent_result_sha
            evidence["provider_result_v3_admission"] = admitted
            evidence["dispatch_completion"] = completion
            evidence["provider_result_v3_replay"] = replay
            evidence["dispatch_completion_replay"] = replay_completion
            evidence["live_checks"] = checks
            evidence["result"] = "PASS_TRANSPORT_BOUND_PROVIDER_RESULT_V3_SHADOW"
            h.base.atomic_json(validation / "validation.json", evidence)

            h.safe_exit(
                OWNER,
                token,
                run,
                "v02159 transport-bound provider result v3 verified",
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
                    "claimMatch": None if row is None else admitted.get("claimMatchReason"),
                    "providerRequestMatch": None
                    if row is None
                    else admitted.get("providerRequestMatchReason"),
                    "transportRequestMatch": None
                    if row is None
                    else admitted.get("transportRequestMatchReason"),
                    "completion": None if row is None else completion.get("reason"),
                },
                indent=2,
            )
        )
        return 0 if evidence["result"] == "PASS_TRANSPORT_BOUND_PROVIDER_RESULT_V3_SHADOW" else 2

    except Exception as ex:
        evidence["result"] = "FAIL"
        evidence["error"] = type(ex).__name__ + ": " + str(ex)
        evidence["failed_local"] = datetime.datetime.now().astimezone().isoformat()
        h.base.atomic_json(validation / "validation.json", evidence)

        if run is not None and h.base.process_pids():
            try:
                h.safe_exit(OWNER, token, run, "v02159 transport-bound v3 failure")
                run = None
            except Exception as e:
                evidence["exit_cleanup_error"] = type(e).__name__ + ": " + str(e)

        if run is not None and not h.base.process_pids():
            try:
                h.base.bus.close_run("v02159 pre-ready failure")
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
