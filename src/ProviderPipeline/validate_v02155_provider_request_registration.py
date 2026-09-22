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
import provider_request_contract_v02154 as contract

h = v46.h
CLAN_SRC = v46.CLAN_SRC
RUNNER_SRC = ROOT / r"workspace\_PatchStaging\AutonomousOperator_v002155_ProviderRequestRegistration_20260921_1541\candidate"
CLAN_SHA = v46.CLAN_SHA
RUNNER_SHA = "F9B8DCFF3F87D643E67C4D7DC70DCA9FE0CD2BC24ECF73CF0CE07FC003D0E7CB"
OWNER = "sol_provider_request_registration_v02155"
SOURCE_PREFIX = "ClanAI V02155 REQUEST REG SOURCE "
FIXTURE_PROJECT = ROOT / r"workspace\_PatchStaging\AutonomousOperator_v002155_ProviderRequestRegistration_20260921_1541\fixtures\Fixtures.csproj"
CONTRACT_FIXTURE = ROOT / r"workspace\test_provider_request_contract_v02154.py"
OUTBOX = v46.OUTBOX
CLAIMS = v50.CLAIMS
REGISTRATIONS = ROOT / r"Automation\TestRunner\patrol_defense_provider_request_registrations.jsonl"
PROVIDER_INVOCATIONS = v46.PROVIDER_INVOCATIONS
RESULT_ADMISSIONS = ROOT / r"Automation\TestRunner\patrol_defense_provider_result_admissions.jsonl"
PROVIDER_ID = "deterministic_external_worker"
MODEL_ID = "deterministic_mock_no_model"

h.CLAN_SRC = CLAN_SRC
h.RUNNER_SRC = RUNNER_SRC
h.NEW_CLAN = CLAN_SHA
h.NEW_RUNNER = RUNNER_SHA


def registration_checks(row, envelope, expected_id, idempotent):
    expected_reason = (
        "SAME_PROVIDER_REQUEST_ALREADY_REGISTERED"
        if idempotent
        else "REGISTERED"
    )
    return {
        "schema": row.get("schema")
        == "BannerlordAI.PatrolDefenseProviderRequestRegistration.v1",
        "mode": row.get("mode") == "observe",
        "provider_request_id": row.get("providerRequestId") == expected_id,
        "provider": row.get("providerId") == envelope.get("providerId"),
        "model": row.get("modelId") == envelope.get("modelId"),
        "attempt": row.get("attemptId") == envelope.get("attemptId"),
        "fingerprint": row.get("requestFingerprint")
        == envelope.get("requestFingerprint"),
        "prompt": row.get("promptContractVersion")
        == envelope.get("promptContractVersion"),
        "response_schema": row.get("responseSchema")
        == envelope.get("responseSchema"),
        "input_sha": row.get("inputSha256") == envelope.get("inputSha256"),
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
    registration_source = (
        RUNNER_SRC / "PatrolDefenseProviderRequestRegistration.cs"
    ).read_text(encoding="utf-8", errors="replace")

    static_checks = {
        "registration_class": "PatrolDefenseProviderRequestRegistration" in registration_source,
        "registration_schema": "BannerlordAI.PatrolDefenseProviderRequestRegistration.v1"
        in registration_source,
        "request_schema": "BannerlordAI.PatrolDefenseProviderRequest.v1"
        in registration_source,
        "register_method": "RegisterProviderRequest(" in dispatch_source,
        "command": "PATROL_DEFENSE_PROVIDER_REQUEST_REGISTER " in runner_source,
        "path": "patrol_defense_provider_request_registrations.jsonl"
        in runner_source,
        "provider_request_id_job": "ProviderRequestId" in dispatch_source,
        "release_clears_registration": "job.ProviderRequestId = null;" in dispatch_source
        and "job.ProviderRequestInputSha256 = null;" in dispatch_source,
        "sha256": "SHA256.Create()" in registration_source,
        "strict_prompt": "BannerlordAI.PatrolDefenseAdvisoryPrompt.v1"
        in registration_source,
        "strict_response": "BannerlordAI.PatrolDefenseDeliberationAdvisory.v1"
        in registration_source,
        "no_http": "HttpClient" not in registration_source,
        "no_openai": "OpenAI" not in registration_source,
        "no_model_api": "ChatCompletion" not in registration_source,
        "no_apply": "PatrolDefenseApplyGate" not in registration_source,
        "no_score": "SetBehaviorScore" not in registration_source,
        "no_memory_write": "RecordPatrolDefenseEpisode" not in registration_source,
    }
    if not all(static_checks.values()):
        raise RuntimeError("static checks " + json.dumps(static_checks, sort_keys=True))

    fixture = subprocess.run(
        ["dotnet", "run", "--project", str(FIXTURE_PROJECT), "-c", "Release"],
        capture_output=True,
        text=True,
    )
    fixture_result = (fixture.stdout or "").strip()
    if fixture.returncode != 0 or "PASS_FIXTURES checks=190" not in fixture_result:
        raise RuntimeError(
            "runner fixture gate failed " + fixture_result + " " + (fixture.stderr or "")
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
        "PatrolDefense_v02155_ProviderRequestRegistration_" + stamp
    )
    validation.mkdir(parents=True)
    backup = ROOT / "workspace/_PatchBackups" / (
        "Before_ProviderRequestRegistration_" + stamp
    )
    backup.mkdir(parents=True)

    operation = "provider-request-registration-v02155-" + stamp
    token = secrets.token_hex(32)
    token_hash = hashlib.sha256(token.encode()).hexdigest().upper()

    evidence = {
        "schema": "BannerlordAI.ProviderRequestRegistrationValidation.v1",
        "created_local": datetime.datetime.now().astimezone().isoformat(),
        "result": "IN_PROGRESS",
        "source_slot": source_slot,
        "clanai_test_sha256": CLAN_SHA,
        "runner_test_sha256": RUNNER_SHA,
        "isolated_fixtures": fixture_result,
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
            "patrol_defense_provider_request_registration_shadow",
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
        for path in (OUTBOX, CLAIMS, REGISTRATIONS, PROVIDER_INVOCATIONS, RESULT_ADMISSIONS):
            if path.exists():
                path.unlink()

        run = h.base.bus.start_run(
            issuer="provider_request_registration_live",
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
                "v02155 provider request registration environmental inconclusive",
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

            envelope, envelope_json = contract.build_provider_request(
                job_line=job_line,
                claim=claim,
                provider_id=PROVIDER_ID,
                model_id=MODEL_ID,
            )
            expected_request_id = hashlib.sha256(
                envelope_json.encode("utf-8")
            ).hexdigest().upper()
            command_b64 = base64.b64encode(
                envelope_json.encode("utf-8")
            ).decode("ascii")
            command = "PATROL_DEFENSE_PROVIDER_REQUEST_REGISTER " + command_b64

            before_registration = len(h.read_jsonl(REGISTRATIONS))
            h.send_wait(
                OWNER,
                run,
                command,
                "patrol_defense_provider_request_registered",
                10,
            )
            registration_rows = h.read_jsonl(REGISTRATIONS)
            if len(registration_rows) <= before_registration:
                raise RuntimeError("registration receipt missing")
            registration = registration_rows[-1]
            first_checks = registration_checks(
                registration,
                envelope,
                expected_request_id,
                False,
            )

            before_replay = len(registration_rows)
            h.send_wait(
                OWNER,
                run,
                command,
                "patrol_defense_provider_request_registration_idempotent",
                10,
            )
            replay_rows = h.read_jsonl(REGISTRATIONS)
            if len(replay_rows) <= before_replay:
                raise RuntimeError("registration replay receipt missing")
            replay = replay_rows[-1]
            replay_checks = registration_checks(
                replay,
                envelope,
                expected_request_id,
                True,
            )

            checks = {}
            checks.update({"claim_" + k: v for k, v in claim_checks.items()})
            checks.update({"first_" + k: v for k, v in first_checks.items()})
            checks.update({"replay_" + k: v for k, v in replay_checks.items()})
            checks["provider_request_id_independent"] = (
                registration.get("providerRequestId")
                == hashlib.sha256(envelope_json.encode("utf-8")).hexdigest().upper()
            )
            checks["provider_invocation_rows_empty"] = (
                len(h.read_jsonl(PROVIDER_INVOCATIONS)) == 0
            )
            checks["provider_result_rows_empty"] = (
                len(h.read_jsonl(RESULT_ADMISSIONS)) == 0
            )
            checks["no_apply_side_effects"] = h.no_apply_side_effects()

            if not all(checks.values()):
                raise RuntimeError(
                    "provider request registration live checks "
                    + json.dumps(checks, sort_keys=True)
                )

            evidence["shadow_receipt"] = row
            evidence["provider_job_claim"] = claim
            evidence["provider_request_envelope"] = envelope
            evidence["provider_request_envelope_json"] = envelope_json
            evidence["provider_request_registration"] = registration
            evidence["provider_request_registration_replay"] = replay
            evidence["independent_provider_request_id"] = expected_request_id
            evidence["live_checks"] = checks
            evidence["result"] = "PASS_PROVIDER_REQUEST_REGISTRATION_SHADOW"
            h.base.atomic_json(validation / "validation.json", evidence)

            h.safe_exit(
                OWNER,
                token,
                run,
                "v02155 provider request registration verified",
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
                    "providerRequestId": None
                    if row is None
                    else registration.get("providerRequestId"),
                    "replayReason": None
                    if row is None
                    else replay.get("reason"),
                },
                indent=2,
            )
        )
        return 0 if evidence["result"] == "PASS_PROVIDER_REQUEST_REGISTRATION_SHADOW" else 2

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
                    "v02155 provider request registration failure",
                )
                run = None
            except Exception as e:
                evidence["exit_cleanup_error"] = type(e).__name__ + ": " + str(e)

        if run is not None and not h.base.process_pids():
            try:
                h.base.bus.close_run("v02155 pre-ready failure")
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
