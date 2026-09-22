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

h = v46.h
CLAN_SRC = v46.CLAN_SRC
RUNNER_SRC = ROOT / r"workspace\_PatchStaging\AutonomousOperator_v002148_ProviderResultEnvelope_20260921_1436\candidate"
CLAN_SHA = v46.CLAN_SHA
RUNNER_SHA = "1B540B14C9E1AACFDE616BFEED4A08CE3A96B228064090831A0809CF0D16C56A"
OWNER = "sol_provider_result_envelope_v02148"
SOURCE_PREFIX = "ClanAI V02148 PROVIDER RESULT SOURCE "
FIXTURE_PROJECT = ROOT / r"workspace\_PatchStaging\AutonomousOperator_v002148_ProviderResultEnvelope_20260921_1436\fixtures\Fixtures.csproj"
OUTBOX = v46.OUTBOX
PROVIDER_INVOCATIONS = v46.PROVIDER_INVOCATIONS
RESULT_ADMISSIONS = ROOT / r"Automation\TestRunner\patrol_defense_provider_result_admissions.jsonl"

h.CLAN_SRC = CLAN_SRC
h.RUNNER_SRC = RUNNER_SRC
h.NEW_CLAN = CLAN_SHA
h.NEW_RUNNER = RUNNER_SHA



def read_result_admissions():
    return h.read_jsonl(RESULT_ADMISSIONS)


def worker_build_result_envelope(job):
    if not isinstance(job, dict):
        raise RuntimeError("worker job missing")
    if job.get("schema") != "BannerlordAI.PatrolDefenseProviderDispatchJob.v1":
        raise RuntimeError("worker job schema mismatch")
    if job.get("state") != "PENDING":
        raise RuntimeError("worker job not pending")

    fingerprint = job.get("requestFingerprint")
    request = job.get("deliberationRequest")
    if not isinstance(fingerprint, str) or not fingerprint:
        raise RuntimeError("worker fingerprint missing")
    if not isinstance(request, dict):
        raise RuntimeError("worker request missing")
    if request.get("requestFingerprint") != fingerprint:
        raise RuntimeError("worker request fingerprint mismatch")

    advisory = {
        "schema": "BannerlordAI.PatrolDefenseDeliberationAdvisory.v1",
        "requestFingerprint": fingerprint,
        "disposition": "KEEP_BASELINE",
    }
    advisory_json = json.dumps(advisory, separators=(",", ":"))
    advisory_base64 = base64.b64encode(
        advisory_json.encode("utf-8")
    ).decode("ascii")

    envelope = {
        "schema": "BannerlordAI.PatrolDefenseProviderResult.v1",
        "providerId": "deterministic_external_worker",
        "attemptId": "attempt-1",
        "requestFingerprint": fingerprint,
        "status": "SUCCESS",
        "advisoryBase64": advisory_base64,
    }
    envelope_json = json.dumps(envelope, separators=(",", ":"))
    command_base64 = base64.b64encode(
        envelope_json.encode("utf-8")
    ).decode("ascii")
    command = "PATROL_DEFENSE_PROVIDER_RESULT_ADMIT " + command_base64
    return fingerprint, request, advisory, envelope, command


def result_success_checks(receipt, fingerprint):
    admission = receipt.get("advisoryAdmission")
    return {
        "schema": receipt.get("schema")
        == "BannerlordAI.PatrolDefenseProviderResultAdmission.v1",
        "mode": receipt.get("mode") == "observe",
        "provider_id": receipt.get("providerId")
        == "deterministic_external_worker",
        "attempt_id": receipt.get("attemptId") == "attempt-1",
        "fingerprint": receipt.get("requestFingerprint") == fingerprint,
        "status": receipt.get("status") == "SUCCESS",
        "accepted": receipt.get("providerResultAccepted") is True,
        "retryable_false": receipt.get("retryable") is False,
        "no_rejections": receipt.get("rejectionReasons") == [],
        "admission_present": isinstance(admission, dict),
        "admission_admitted": isinstance(admission, dict)
        and admission.get("admitted") is True,
        "admission_disposition": isinstance(admission, dict)
        and admission.get("disposition") == "KEEP_BASELINE",
        "admission_execution_false": isinstance(admission, dict)
        and admission.get("executionAuthorized") is False,
        "network_false": receipt.get("externalNetworkUsed") is False,
        "model_false": receipt.get("modelInvoked") is False,
        "llm_false": receipt.get("llmInvoked") is False,
        "planner_false": receipt.get("plannerInvoked") is False,
        "execution_false": receipt.get("executionAuthorized") is False,
        "behavior_false": receipt.get("behaviorMutation") is False,
        "intent_false": receipt.get("intentMutation") is False,
        "score_false": receipt.get("scoreMutation") is False,
        "movement_zero": receipt.get("nativeMovementCalls") == 0,
    }


def result_replay_checks(receipt, fingerprint):
    reasons = receipt.get("rejectionReasons")
    return {
        "schema": receipt.get("schema")
        == "BannerlordAI.PatrolDefenseProviderResultAdmission.v1",
        "provider_id": receipt.get("providerId")
        == "deterministic_external_worker",
        "attempt_id": receipt.get("attemptId") == "attempt-1",
        "fingerprint": receipt.get("requestFingerprint") == fingerprint,
        "status": receipt.get("status") == "SUCCESS",
        "accepted_false": receipt.get("providerResultAccepted") is False,
        "no_pending_reason": isinstance(reasons, list)
        and "NO_PENDING_ROUTE_REQUEST" in reasons,
        "admission_null": receipt.get("advisoryAdmission") is None,
        "model_false": receipt.get("modelInvoked") is False,
        "execution_false": receipt.get("executionAuthorized") is False,
        "behavior_false": receipt.get("behaviorMutation") is False,
        "score_false": receipt.get("scoreMutation") is False,
        "movement_zero": receipt.get("nativeMovementCalls") == 0,
    }


def main():
    if h.base.process_pids():
        raise RuntimeError("Bannerlord already running")

    runner_source = (RUNNER_SRC / "SubModule.cs").read_text(
        encoding="utf-8", errors="replace"
    )
    result_source = (
        RUNNER_SRC / "PatrolDefenseProviderResultAdmission.cs"
    ).read_text(encoding="utf-8", errors="replace")

    static_checks = {
        "runner_sha": h.sha(RUNNER_SRC / "BannerlordAITestRunner.dll")
        == RUNNER_SHA,
        "result_command": "PATROL_DEFENSE_PROVIDER_RESULT_ADMIT " in runner_source,
        "result_path": "patrol_defense_provider_result_admissions.jsonl"
        in runner_source,
        "result_schema": "BannerlordAI.PatrolDefenseProviderResult.v1"
        in result_source,
        "admission_schema": "BannerlordAI.PatrolDefenseProviderResultAdmission.v1"
        in result_source,
        "success_status": '"SUCCESS"' in result_source,
        "transient_status": '"TRANSIENT_FAILURE"' in result_source,
        "permanent_status": '"PERMANENT_FAILURE"' in result_source,
        "strict_advisory_gate": "gate.Evaluate" in result_source,
        "execution_false": 'executionAuthorized\\":false' in result_source,
        "model_false": 'modelInvoked\\":false' in result_source,
        "no_http": "HttpClient" not in result_source
        and "HttpClient" not in runner_source,
        "no_openai": "OpenAI" not in result_source
        and "OpenAI" not in runner_source,
        "no_apply": "PatrolDefenseApplyGate" not in result_source,
        "no_score": "SetBehaviorScore" not in result_source,
        "no_memory_write": "RecordPatrolDefenseEpisode" not in result_source,
    }
    if not all(static_checks.values()):
        raise RuntimeError("static checks " + json.dumps(static_checks, sort_keys=True))

    fixture = subprocess.run(
        ["dotnet", "run", "--project", str(FIXTURE_PROJECT), "-c", "Release"],
        capture_output=True,
        text=True,
    )
    fixture_result = (fixture.stdout or "").strip()
    if fixture.returncode != 0 or "PASS_FIXTURES checks=123" not in fixture_result:
        raise RuntimeError(
            "binary reuse fixture gate failed "
            + fixture_result
            + " "
            + (fixture.stderr or "")
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
        "PatrolDefense_v02148_ProviderResultEnvelope_" + stamp
    )
    validation.mkdir(parents=True)
    backup = ROOT / "workspace/_PatchBackups" / (
        "Before_ProviderResultEnvelope_" + stamp
    )
    backup.mkdir(parents=True)

    operation = "provider-result-envelope-v02148-" + stamp
    token = secrets.token_hex(32)
    token_hash = hashlib.sha256(token.encode()).hexdigest().upper()

    evidence = {
        "schema": "BannerlordAI.ProviderResultEnvelopeValidation.v1",
        "created_local": datetime.datetime.now().astimezone().isoformat(),
        "result": "IN_PROGRESS",
        "source_slot": source_slot,
        "clanai_test_sha256": CLAN_SHA,
        "runner_test_sha256": RUNNER_SHA,
        "feature_binary_reused_from": "v0.2.10.48-provider-result-envelope-shadow",
        "isolated_fixtures": fixture_result,
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
            "patrol_defense_provider_result_envelope_shadow",
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
        for path in (OUTBOX, PROVIDER_INVOCATIONS, RESULT_ADMISSIONS):
            if path.exists():
                path.unlink()

        run = h.base.bus.start_run(
            issuer="provider_result_envelope_live",
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
                "v02148 provider result environmental inconclusive",
            )
            run = None
        else:
            dispatch_checks = v46.dispatch_checks(row, request, route, dispatch)
            outbox_rows = h.read_jsonl(OUTBOX)
            if len(outbox_rows) != 1:
                raise RuntimeError(
                    "expected exactly one outbox job, got " + str(len(outbox_rows))
                )
            job = outbox_rows[0]
            dispatch_checks.update(
                {
                    "outbox_" + k: v
                    for k, v in v46.outbox_checks(job, request).items()
                }
            )

            (
                fingerprint,
                worker_request,
                worker_advisory,
                worker_envelope,
                command,
            ) = worker_build_result_envelope(job)

            before = len(read_result_admissions())
            h.send_wait(
                OWNER,
                run,
                command,
                "patrol_defense_provider_result_admitted",
                10,
            )
            result_rows = read_result_admissions()
            if len(result_rows) <= before:
                raise RuntimeError("provider result admission receipt missing")
            admitted = result_rows[-1]
            admitted_checks = result_success_checks(
                admitted,
                fingerprint,
            )
            if not all(admitted_checks.values()):
                raise RuntimeError(
                    "provider result success checks "
                    + json.dumps(admitted_checks, sort_keys=True)
                )

            before_replay = len(result_rows)
            h.send_wait(
                OWNER,
                run,
                command,
                "patrol_defense_provider_result_rejected:NO_PENDING_ROUTE_REQUEST",
                10,
            )
            result_rows2 = read_result_admissions()
            if len(result_rows2) <= before_replay:
                raise RuntimeError("provider result replay receipt missing")
            replay = result_rows2[-1]
            replay_result = result_replay_checks(
                replay,
                fingerprint,
            )
            if not all(replay_result.values()):
                raise RuntimeError(
                    "provider result replay checks "
                    + json.dumps(replay_result, sort_keys=True)
                )

            provider_rows = h.read_jsonl(PROVIDER_INVOCATIONS)

            checks = {}
            checks.update({"dispatch_" + k: v for k, v in dispatch_checks.items()})
            checks.update(
                {"result_" + k: v for k, v in admitted_checks.items()}
            )
            checks.update(
                {"replay_" + k: v for k, v in replay_result.items()}
            )
            checks["worker_job_request_exact"] = worker_request == request
            checks["worker_envelope_fingerprint"] = (
                worker_envelope.get("requestFingerprint") == fingerprint
            )
            checks["worker_advisory_fingerprint"] = (
                worker_advisory.get("requestFingerprint") == fingerprint
            )
            checks["provider_invocation_rows_empty"] = len(provider_rows) == 0
            checks["no_apply_side_effects"] = h.no_apply_side_effects()

            if not all(checks.values()):
                raise RuntimeError(
                    "provider result envelope live checks "
                    + json.dumps(checks, sort_keys=True)
                )

            evidence["shadow_receipt"] = row
            evidence["provider_dispatch"] = dispatch
            evidence["outbox_job"] = job
            evidence["worker_advisory_json"] = worker_advisory
            evidence["worker_result_envelope"] = worker_envelope
            evidence["provider_result_admission"] = admitted
            evidence["provider_result_replay"] = replay
            evidence["provider_invocation_rows"] = len(provider_rows)
            evidence["live_checks"] = checks
            evidence["result"] = "PASS_PROVIDER_RESULT_ENVELOPE_SHADOW"
            h.base.atomic_json(validation / "validation.json", evidence)

            h.safe_exit(
                OWNER,
                token,
                run,
                "v02148 provider result envelope verified",
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
                    "providerResultAccepted": None
                    if row is None
                    else admitted.get("providerResultAccepted"),
                    "replayAccepted": None
                    if row is None
                    else replay.get("providerResultAccepted"),
                    "providerRows": None if row is None else len(provider_rows),
                },
                indent=2,
            )
        )
        return (
            0
            if evidence["result"]
            == "PASS_PROVIDER_RESULT_ENVELOPE_SHADOW"
            else 2
        )

    except Exception as ex:
        evidence["result"] = "FAIL"
        evidence["error"] = type(ex).__name__ + ": " + str(ex)
        evidence["failed_local"] = datetime.datetime.now().astimezone().isoformat()
        h.base.atomic_json(validation / "validation.json", evidence)

        if run is not None and h.base.process_pids():
            try:
                h.safe_exit(OWNER, token, run, "v02148 provider result failure")
                run = None
            except Exception as e:
                evidence["exit_cleanup_error"] = type(e).__name__ + ": " + str(e)

        if run is not None and not h.base.process_pids():
            try:
                h.base.bus.close_run("v02148 pre-ready failure")
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

