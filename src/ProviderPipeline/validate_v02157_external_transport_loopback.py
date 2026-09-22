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
import validate_v02156_provider_result_v2_binding as v56
import provider_request_contract_v02154 as request_contract
import provider_transport_loopback_v02157 as transport

h = v46.h
CLAN_SRC = v46.CLAN_SRC
RUNNER_SRC = ROOT / r"workspace\_PatchStaging\AutonomousOperator_v002156_ProviderResultV2Binding_20260921_1549\candidate"
CLAN_SHA = v46.CLAN_SHA
RUNNER_SHA = "7732C135648EE1E81587EF72E742DEB29260E0CEBD3005009BF66774F413A39C"
OWNER = "sol_external_transport_loopback_v02157"
SOURCE_PREFIX = "ClanAI V02157 TRANSPORT LOOPBACK SOURCE "
TRANSPORT_FIXTURE = ROOT / r"workspace\test_provider_transport_loopback_v02157.py"
RUNNER_FIXTURE = ROOT / r"workspace\_PatchStaging\AutonomousOperator_v002156_ProviderResultV2Binding_20260921_1549\fixtures\Fixtures.csproj"
CONTRACT_FIXTURE = ROOT / r"workspace\test_provider_request_contract_v02154.py"
OUTBOX = v46.OUTBOX
CLAIMS = v50.CLAIMS
REGISTRATIONS = v55.REGISTRATIONS
RESULT_V2 = v56.RESULT_V2
COMPLETIONS = v49.COMPLETIONS
PROVIDER_INVOCATIONS = v46.PROVIDER_INVOCATIONS
PROVIDER_ID = "deterministic_external_worker"
MODEL_ID = "deterministic_mock_no_model"

h.CLAN_SRC = CLAN_SRC
h.RUNNER_SRC = RUNNER_SRC
h.NEW_CLAN = CLAN_SHA
h.NEW_RUNNER = RUNNER_SHA


def transport_checks(outcome, request_envelope, registration):
    result = outcome.result
    receipt = outcome.receipt
    expected_result_sha = hashlib.sha256(
        outcome.result_json.encode("utf-8")
    ).hexdigest().upper()
    return {
        "success": outcome.success,
        "no_reasons": outcome.reasons == [],
        "result_present": isinstance(result, dict),
        "result_schema": isinstance(result, dict)
        and result.get("schema") == "BannerlordAI.PatrolDefenseProviderResult.v2",
        "result_provider": isinstance(result, dict)
        and result.get("providerId") == request_envelope.get("providerId"),
        "result_attempt": isinstance(result, dict)
        and result.get("attemptId") == request_envelope.get("attemptId"),
        "result_fingerprint": isinstance(result, dict)
        and result.get("requestFingerprint") == request_envelope.get("requestFingerprint"),
        "result_provider_request_id": isinstance(result, dict)
        and result.get("providerRequestId") == registration.get("providerRequestId"),
        "result_status": isinstance(result, dict)
        and result.get("status") == "SUCCESS",
        "receipt_schema": receipt.get("schema")
        == "BannerlordAI.PatrolDefenseProviderTransportReceipt.v1",
        "transport_id": receipt.get("transportId") == "deterministic_local_loopback",
        "receipt_request_id": receipt.get("providerRequestId")
        == registration.get("providerRequestId"),
        "receipt_provider": receipt.get("providerId") == request_envelope.get("providerId"),
        "receipt_model": receipt.get("modelId") == request_envelope.get("modelId"),
        "receipt_attempt": receipt.get("attemptId") == request_envelope.get("attemptId"),
        "receipt_fingerprint": receipt.get("requestFingerprint")
        == request_envelope.get("requestFingerprint"),
        "receipt_input_sha": receipt.get("requestInputSha256")
        == request_envelope.get("inputSha256"),
        "receipt_result_sha": receipt.get("resultSha256") == expected_result_sha,
        "receipt_status": receipt.get("resultStatus") == "SUCCESS",
        "network_false": receipt.get("externalNetworkUsed") is False,
        "model_false": receipt.get("modelInvoked") is False,
        "receipt_success": receipt.get("success") is True,
        "error_null": receipt.get("errorCode") is None,
    }


def main():
    if h.base.process_pids():
        raise RuntimeError("Bannerlord already running")

    transport_source = (
        ROOT / r"workspace\provider_transport_loopback_v02157.py"
    ).read_text(encoding="utf-8", errors="replace")

    static_checks = {
        "unchanged_runner_sha": h.sha(RUNNER_SRC / "BannerlordAITestRunner.dll")
        == RUNNER_SHA,
        "transport_id": "deterministic_local_loopback" in transport_source,
        "transport_receipt_schema":
        "BannerlordAI.PatrolDefenseProviderTransportReceipt.v1" in transport_source,
        "request_validation": "validate_provider_request" in transport_source,
        "result_v2_schema": "BannerlordAI.PatrolDefenseProviderResult.v2"
        in transport_source,
        "result_sha": "resultSha256" in transport_source
        and "hashlib.sha256" in transport_source,
        "network_false": '"externalNetworkUsed": False' in transport_source,
        "model_false": '"modelInvoked": False' in transport_source,
        "no_requests": "import requests" not in transport_source,
        "no_httpx": "import httpx" not in transport_source,
        "no_urllib_request": "urllib.request" not in transport_source,
        "no_socket": "import socket" not in transport_source,
        "no_openai": "openai" not in transport_source.lower(),
    }
    if not all(static_checks.values()):
        raise RuntimeError("static checks " + json.dumps(static_checks, sort_keys=True))

    transport_fixture = subprocess.run(
        [sys.executable, "-X", "utf8", str(TRANSPORT_FIXTURE)],
        capture_output=True,
        text=True,
    )
    transport_fixture_result = (transport_fixture.stdout or "").strip()
    if (
        transport_fixture.returncode != 0
        or "PASS_FIXTURES checks=61" not in transport_fixture_result
    ):
        raise RuntimeError(
            "transport fixtures failed "
            + transport_fixture_result
            + " "
            + (transport_fixture.stderr or "")
        )

    runner_fixture = subprocess.run(
        ["dotnet", "run", "--project", str(RUNNER_FIXTURE), "-c", "Release"],
        capture_output=True,
        text=True,
    )
    runner_fixture_result = (runner_fixture.stdout or "").strip()
    if (
        runner_fixture.returncode != 0
        or "PASS_FIXTURES checks=206" not in runner_fixture_result
    ):
        raise RuntimeError(
            "runner fixtures failed "
            + runner_fixture_result
            + " "
            + (runner_fixture.stderr or "")
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
            "contract fixtures failed "
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
        "PatrolDefense_v02157_ExternalTransportLoopback_" + stamp
    )
    validation.mkdir(parents=True)
    backup = ROOT / "workspace/_PatchBackups" / (
        "Before_ExternalTransportLoopback_" + stamp
    )
    backup.mkdir(parents=True)

    operation = "external-transport-loopback-v02157-" + stamp
    token = secrets.token_hex(32)
    token_hash = hashlib.sha256(token.encode()).hexdigest().upper()

    evidence = {
        "schema": "BannerlordAI.ExternalTransportLoopbackValidation.v1",
        "created_local": datetime.datetime.now().astimezone().isoformat(),
        "result": "IN_PROGRESS",
        "source_slot": source_slot,
        "feature_binary_reused_from":
        "v0.2.10.56-provider-result-request-binding-shadow",
        "clanai_test_sha256": CLAN_SHA,
        "runner_test_sha256": RUNNER_SHA,
        "transport_fixtures": transport_fixture_result,
        "runner_fixtures": runner_fixture_result,
        "contract_fixtures": contract_fixture_result,
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
            "patrol_defense_external_transport_loopback_shadow",
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
            REGISTRATIONS,
            RESULT_V2,
            COMPLETIONS,
            PROVIDER_INVOCATIONS,
        ):
            if path.exists():
                path.unlink()

        run = h.base.bus.start_run(
            issuer="external_transport_loopback_live",
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
                "v02157 transport loopback environmental inconclusive",
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

            request_envelope, request_envelope_json = request_contract.build_provider_request(
                job_line=job_line,
                claim=claim,
                provider_id=PROVIDER_ID,
                model_id=MODEL_ID,
            )
            register_command = (
                "PATROL_DEFENSE_PROVIDER_REQUEST_REGISTER "
                + base64.b64encode(
                    request_envelope_json.encode("utf-8")
                ).decode("ascii")
            )

            before_registration = len(h.read_jsonl(REGISTRATIONS))
            h.send_wait(
                OWNER,
                run,
                register_command,
                "patrol_defense_provider_request_registered",
                10,
            )
            registration_rows = h.read_jsonl(REGISTRATIONS)
            if len(registration_rows) <= before_registration:
                raise RuntimeError("registration receipt missing")
            registration = registration_rows[-1]
            expected_request_id = hashlib.sha256(
                request_envelope_json.encode("utf-8")
            ).hexdigest().upper()
            registration_checks = v55.registration_checks(
                registration,
                request_envelope,
                expected_request_id,
                False,
            )
            if not all(registration_checks.values()):
                raise RuntimeError(
                    "registration checks "
                    + json.dumps(registration_checks, sort_keys=True)
                )

            outcome = transport.send_loopback(
                request_envelope_json,
                registration,
            )
            outcome_repeat = transport.send_loopback(
                request_envelope_json,
                registration,
            )
            transport_result_checks = transport_checks(
                outcome,
                request_envelope,
                registration,
            )
            transport_result_checks["byte_deterministic_result"] = (
                outcome_repeat.result_json == outcome.result_json
            )
            transport_result_checks["byte_deterministic_receipt"] = (
                outcome_repeat.receipt_json == outcome.receipt_json
            )
            if not all(transport_result_checks.values()):
                raise RuntimeError(
                    "transport checks "
                    + json.dumps(transport_result_checks, sort_keys=True)
                )

            independent_result_sha = hashlib.sha256(
                outcome.result_json.encode("utf-8")
            ).hexdigest().upper()
            if independent_result_sha != outcome.receipt.get("resultSha256"):
                raise RuntimeError("independent result sha mismatch")

            result_command = (
                "PATROL_DEFENSE_PROVIDER_RESULT_V2_ADMIT "
                + base64.b64encode(
                    outcome.result_json.encode("utf-8")
                ).decode("ascii")
            )
            before_result = len(h.read_jsonl(RESULT_V2))
            before_completion = len(h.read_jsonl(COMPLETIONS))
            h.send_wait(
                OWNER,
                run,
                result_command,
                "patrol_defense_provider_result_v2_admitted",
                10,
            )
            result_rows = h.read_jsonl(RESULT_V2)
            completion_rows = h.read_jsonl(COMPLETIONS)
            if len(result_rows) <= before_result:
                raise RuntimeError("v2 result receipt missing")
            if len(completion_rows) <= before_completion:
                raise RuntimeError("completion receipt missing")

            result = result_rows[-1]
            completion = completion_rows[-1]
            result_checks = v56.v2_success_checks(
                result,
                fingerprint,
                registration.get("providerRequestId"),
            )
            completion_checks = v49.completion_success_checks(completion, fingerprint)
            if not all(result_checks.values()):
                raise RuntimeError(
                    "v2 success checks "
                    + json.dumps(result_checks, sort_keys=True)
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
                "patrol_defense_provider_result_v2_rejected:DISPATCH_JOB_NOT_FOUND",
                10,
            )
            replay_rows = h.read_jsonl(RESULT_V2)
            replay_completion_rows = h.read_jsonl(COMPLETIONS)
            if len(replay_rows) <= before_replay_result:
                raise RuntimeError("v2 replay receipt missing")
            if len(replay_completion_rows) <= before_replay_completion:
                raise RuntimeError("v2 replay completion receipt missing")

            replay = replay_rows[-1]
            replay_completion = replay_completion_rows[-1]
            replay_checks = v56.v2_replay_checks(
                replay,
                fingerprint,
                registration.get("providerRequestId"),
            )
            replay_completion_checks = v49.completion_replay_checks(
                replay_completion,
                fingerprint,
            )

            checks = {}
            checks.update({"claim_" + k: v for k, v in claim_checks.items()})
            checks.update(
                {"registration_" + k: v for k, v in registration_checks.items()}
            )
            checks.update(
                {"transport_" + k: v for k, v in transport_result_checks.items()}
            )
            checks.update({"result_" + k: v for k, v in result_checks.items()})
            checks.update(
                {"completion_" + k: v for k, v in completion_checks.items()}
            )
            checks.update({"replay_" + k: v for k, v in replay_checks.items()})
            checks.update(
                {
                    "replay_completion_" + k: v
                    for k, v in replay_completion_checks.items()
                }
            )
            checks["independent_result_sha"] = (
                independent_result_sha == outcome.receipt.get("resultSha256")
            )
            checks["provider_invocation_rows_empty"] = (
                len(h.read_jsonl(PROVIDER_INVOCATIONS)) == 0
            )
            checks["no_apply_side_effects"] = h.no_apply_side_effects()

            if not all(checks.values()):
                raise RuntimeError(
                    "external transport loopback live checks "
                    + json.dumps(checks, sort_keys=True)
                )

            evidence["shadow_receipt"] = row
            evidence["provider_job_claim"] = claim
            evidence["provider_request_registration"] = registration
            evidence["provider_request_envelope"] = request_envelope
            evidence["transport_receipt"] = outcome.receipt
            evidence["transport_receipt_json"] = outcome.receipt_json
            evidence["provider_result_v2_json"] = outcome.result_json
            evidence["independent_result_sha256"] = independent_result_sha
            evidence["provider_result_v2_admission"] = result
            evidence["dispatch_completion"] = completion
            evidence["provider_result_v2_replay"] = replay
            evidence["dispatch_completion_replay"] = replay_completion
            evidence["live_checks"] = checks
            evidence["result"] = "PASS_EXTERNAL_TRANSPORT_LOOPBACK_SHADOW"
            h.base.atomic_json(validation / "validation.json", evidence)

            h.safe_exit(
                OWNER,
                token,
                run,
                "v02157 external transport loopback verified",
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
                    "transportId": None
                    if row is None
                    else outcome.receipt.get("transportId"),
                    "resultSha256": None
                    if row is None
                    else outcome.receipt.get("resultSha256"),
                    "requestMatch": None
                    if row is None
                    else result.get("providerRequestMatchReason"),
                },
                indent=2,
            )
        )
        return 0 if evidence["result"] == "PASS_EXTERNAL_TRANSPORT_LOOPBACK_SHADOW" else 2

    except Exception as ex:
        evidence["result"] = "FAIL"
        evidence["error"] = type(ex).__name__ + ": " + str(ex)
        evidence["failed_local"] = datetime.datetime.now().astimezone().isoformat()
        h.base.atomic_json(validation / "validation.json", evidence)

        if run is not None and h.base.process_pids():
            try:
                h.safe_exit(OWNER, token, run, "v02157 transport loopback failure")
                run = None
            except Exception as e:
                evidence["exit_cleanup_error"] = type(e).__name__ + ": " + str(e)

        if run is not None and not h.base.process_pids():
            try:
                h.base.bus.close_run("v02157 pre-ready failure")
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
