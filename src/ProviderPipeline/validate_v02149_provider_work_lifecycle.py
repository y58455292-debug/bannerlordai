from pathlib import Path
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
import validate_v02148_provider_result_envelope as v48

h = v46.h
CLAN_SRC = v46.CLAN_SRC
RUNNER_SRC = ROOT / r"workspace\_PatchStaging\AutonomousOperator_v002149_ProviderWorkLifecycle_20260921_1451\candidate"
CLAN_SHA = v46.CLAN_SHA
RUNNER_SHA = "722BF96AA18F83FD4AB11BBC36504BE4CD7CEA3A4F614D39B89F1961995FE083"
OWNER = "sol_provider_work_lifecycle_v02149"
SOURCE_PREFIX = "ClanAI V02149 WORK LIFECYCLE SOURCE "
FIXTURE_PROJECT = ROOT / r"workspace\_PatchStaging\AutonomousOperator_v002149_ProviderWorkLifecycle_20260921_1451\fixtures\Fixtures.csproj"
OUTBOX = v46.OUTBOX
PROVIDER_INVOCATIONS = v46.PROVIDER_INVOCATIONS
RESULT_ADMISSIONS = ROOT / r"Automation\TestRunner\patrol_defense_provider_result_admissions.jsonl"
COMPLETIONS = ROOT / r"Automation\TestRunner\patrol_defense_provider_dispatch_completions.jsonl"

h.CLAN_SRC = CLAN_SRC
h.RUNNER_SRC = RUNNER_SRC
h.NEW_CLAN = CLAN_SHA
h.NEW_RUNNER = RUNNER_SHA


def read_rows(path):
    return h.read_jsonl(path)


def completion_success_checks(receipt, fingerprint):
    return {
        "schema": receipt.get("schema")
        == "BannerlordAI.PatrolDefenseProviderDispatchCompletion.v1",
        "mode": receipt.get("mode") == "observe",
        "fingerprint": receipt.get("requestFingerprint") == fingerprint,
        "status": receipt.get("resultStatus") == "SUCCESS",
        "applied": receipt.get("completionApplied") is True,
        "reason": receipt.get("reason") == "SUCCESS_ACKED",
        "before": receipt.get("queueCountBefore") == 1,
        "after": receipt.get("queueCountAfter") == 0,
        "pending_false": receipt.get("pendingRetained") is False,
        "network_false": receipt.get("externalNetworkUsed") is False,
        "model_false": receipt.get("modelInvoked") is False,
        "llm_false": receipt.get("llmInvoked") is False,
        "planner_false": receipt.get("plannerInvoked") is False,
        "execution_false": receipt.get("executionAuthorized") is False,
        "interpretation_false": receipt.get("interpretationApplied") is False,
        "behavior_false": receipt.get("behaviorMutation") is False,
        "intent_false": receipt.get("intentMutation") is False,
        "score_false": receipt.get("scoreMutation") is False,
        "movement_zero": receipt.get("nativeMovementCalls") == 0,
    }


def completion_replay_checks(receipt, fingerprint):
    return {
        "schema": receipt.get("schema")
        == "BannerlordAI.PatrolDefenseProviderDispatchCompletion.v1",
        "fingerprint": receipt.get("requestFingerprint") == fingerprint,
        "status": receipt.get("resultStatus") == "SUCCESS",
        "applied_false": receipt.get("completionApplied") is False,
        "reason": receipt.get("reason") == "DISPATCH_JOB_NOT_FOUND",
        "before_zero": receipt.get("queueCountBefore") == 0,
        "after_zero": receipt.get("queueCountAfter") == 0,
        "pending_false": receipt.get("pendingRetained") is False,
        "network_false": receipt.get("externalNetworkUsed") is False,
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
    dispatch_source = (RUNNER_SRC / "PatrolDefenseProviderDispatch.cs").read_text(
        encoding="utf-8", errors="replace"
    )
    result_source = (RUNNER_SRC / "PatrolDefenseProviderResultAdmission.cs").read_text(
        encoding="utf-8", errors="replace"
    )

    eval_pos = runner_source.find("PatrolDefenseProviderResultAdmission.Evaluate(")
    completion_pos = runner_source.find("_patrolDefenseProviderDispatchQueue.ApplyProviderResult(")

    static_checks = {
        "runner_sha": h.sha(RUNNER_SRC / "BannerlordAITestRunner.dll") == RUNNER_SHA,
        "completion_class": "PatrolDefenseProviderDispatchCompletionResult"
        in dispatch_source,
        "completion_method": "ApplyProviderResult(" in dispatch_source,
        "completion_schema": "BannerlordAI.PatrolDefenseProviderDispatchCompletion.v1"
        in dispatch_source,
        "completion_path": "patrol_defense_provider_dispatch_completions.jsonl"
        in runner_source,
        "result_before_completion": eval_pos >= 0
        and completion_pos >= 0
        and eval_pos < completion_pos,
        "success_acked": "SUCCESS_ACKED" in dispatch_source,
        "permanent_acked": "PERMANENT_FAILURE_ACKED" in dispatch_source,
        "transient_retained": "TRANSIENT_FAILURE_RETAINED" in dispatch_source,
        "job_not_found": "DISPATCH_JOB_NOT_FOUND" in dispatch_source,
        "strict_result_gate": "PatrolDefenseProviderResultAdmission.Evaluate"
        in runner_source,
        "no_http": "HttpClient" not in dispatch_source
        and "HttpClient" not in result_source,
        "no_openai": "OpenAI" not in dispatch_source
        and "OpenAI" not in result_source,
        "no_apply": "PatrolDefenseApplyGate" not in dispatch_source,
        "no_score": "SetBehaviorScore" not in dispatch_source,
        "no_memory_write": "RecordPatrolDefenseEpisode" not in dispatch_source,
    }
    if not all(static_checks.values()):
        raise RuntimeError("static checks " + json.dumps(static_checks, sort_keys=True))

    fixture = subprocess.run(
        ["dotnet", "run", "--project", str(FIXTURE_PROJECT), "-c", "Release"],
        capture_output=True,
        text=True,
    )
    fixture_result = (fixture.stdout or "").strip()
    if fixture.returncode != 0 or "PASS_FIXTURES checks=135" not in fixture_result:
        raise RuntimeError(
            "fixture gate failed " + fixture_result + " " + (fixture.stderr or "")
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
        "PatrolDefense_v02149_ProviderWorkLifecycle_" + stamp
    )
    validation.mkdir(parents=True)
    backup = ROOT / "workspace/_PatchBackups" / (
        "Before_ProviderWorkLifecycle_" + stamp
    )
    backup.mkdir(parents=True)

    operation = "provider-work-lifecycle-v02149-" + stamp
    token = secrets.token_hex(32)
    token_hash = hashlib.sha256(token.encode()).hexdigest().upper()

    evidence = {
        "schema": "BannerlordAI.ProviderWorkLifecycleValidation.v1",
        "created_local": datetime.datetime.now().astimezone().isoformat(),
        "result": "IN_PROGRESS",
        "source_slot": source_slot,
        "clanai_test_sha256": CLAN_SHA,
        "runner_test_sha256": RUNNER_SHA,
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
            "patrol_defense_provider_work_lifecycle_shadow",
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
        for path in (OUTBOX, PROVIDER_INVOCATIONS, RESULT_ADMISSIONS, COMPLETIONS):
            if path.exists():
                path.unlink()

        run = h.base.bus.start_run(
            issuer="provider_work_lifecycle_live",
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
                "v02149 provider work lifecycle environmental inconclusive",
            )
            run = None
        else:
            dispatch_checks = v46.dispatch_checks(row, request, route, dispatch)
            outbox_rows = h.read_jsonl(OUTBOX)
            if len(outbox_rows) != 1:
                raise RuntimeError(
                    "expected exactly one outbox row, got " + str(len(outbox_rows))
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
            ) = v48.worker_build_result_envelope(job)

            before_results = len(read_rows(RESULT_ADMISSIONS))
            before_completions = len(read_rows(COMPLETIONS))
            h.send_wait(
                OWNER,
                run,
                command,
                "patrol_defense_provider_result_admitted",
                10,
            )

            result_rows = read_rows(RESULT_ADMISSIONS)
            completion_rows = read_rows(COMPLETIONS)
            if len(result_rows) <= before_results:
                raise RuntimeError("provider result receipt missing")
            if len(completion_rows) <= before_completions:
                raise RuntimeError("dispatch completion receipt missing")

            admitted = result_rows[-1]
            completion = completion_rows[-1]
            admitted_checks = v48.result_success_checks(admitted, fingerprint)
            completion_checks = completion_success_checks(completion, fingerprint)

            if not all(admitted_checks.values()):
                raise RuntimeError(
                    "result success checks "
                    + json.dumps(admitted_checks, sort_keys=True)
                )
            if not all(completion_checks.values()):
                raise RuntimeError(
                    "completion success checks "
                    + json.dumps(completion_checks, sort_keys=True)
                )

            before_results2 = len(result_rows)
            before_completions2 = len(completion_rows)
            h.send_wait(
                OWNER,
                run,
                command,
                "patrol_defense_provider_result_rejected:NO_PENDING_ROUTE_REQUEST",
                10,
            )

            result_rows2 = read_rows(RESULT_ADMISSIONS)
            completion_rows2 = read_rows(COMPLETIONS)
            if len(result_rows2) <= before_results2:
                raise RuntimeError("provider result replay receipt missing")
            if len(completion_rows2) <= before_completions2:
                raise RuntimeError("dispatch completion replay receipt missing")

            replay = result_rows2[-1]
            replay_completion = completion_rows2[-1]
            replay_checks = v48.result_replay_checks(replay, fingerprint)
            replay_completion_checks = completion_replay_checks(
                replay_completion, fingerprint
            )

            if not all(replay_checks.values()):
                raise RuntimeError(
                    "result replay checks "
                    + json.dumps(replay_checks, sort_keys=True)
                )
            if not all(replay_completion_checks.values()):
                raise RuntimeError(
                    "completion replay checks "
                    + json.dumps(replay_completion_checks, sort_keys=True)
                )

            provider_rows = h.read_jsonl(PROVIDER_INVOCATIONS)

            checks = {}
            checks.update({"dispatch_" + k: v for k, v in dispatch_checks.items()})
            checks.update({"result_" + k: v for k, v in admitted_checks.items()})
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
            checks["worker_request_exact"] = worker_request == request
            checks["worker_advisory_fingerprint"] = (
                worker_advisory.get("requestFingerprint") == fingerprint
            )
            checks["worker_envelope_fingerprint"] = (
                worker_envelope.get("requestFingerprint") == fingerprint
            )
            checks["provider_invocation_rows_empty"] = len(provider_rows) == 0
            checks["no_apply_side_effects"] = h.no_apply_side_effects()

            if not all(checks.values()):
                raise RuntimeError(
                    "provider work lifecycle live checks "
                    + json.dumps(checks, sort_keys=True)
                )

            evidence["shadow_receipt"] = row
            evidence["provider_dispatch"] = dispatch
            evidence["outbox_job"] = job
            evidence["provider_result_admission"] = admitted
            evidence["dispatch_completion"] = completion
            evidence["provider_result_replay"] = replay
            evidence["dispatch_completion_replay"] = replay_completion
            evidence["provider_invocation_rows"] = len(provider_rows)
            evidence["live_checks"] = checks
            evidence["result"] = "PASS_PROVIDER_WORK_COMPLETION_LIFECYCLE_SHADOW"
            h.base.atomic_json(validation / "validation.json", evidence)

            h.safe_exit(
                OWNER,
                token,
                run,
                "v02149 provider work lifecycle verified",
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
                    "completionReason": None
                    if row is None
                    else completion.get("reason"),
                    "queueAfter": None
                    if row is None
                    else completion.get("queueCountAfter"),
                    "replayCompletionReason": None
                    if row is None
                    else replay_completion.get("reason"),
                },
                indent=2,
            )
        )
        return (
            0
            if evidence["result"]
            == "PASS_PROVIDER_WORK_COMPLETION_LIFECYCLE_SHADOW"
            else 2
        )

    except Exception as ex:
        evidence["result"] = "FAIL"
        evidence["error"] = type(ex).__name__ + ": " + str(ex)
        evidence["failed_local"] = datetime.datetime.now().astimezone().isoformat()
        h.base.atomic_json(validation / "validation.json", evidence)

        if run is not None and h.base.process_pids():
            try:
                h.safe_exit(OWNER, token, run, "v02149 provider work failure")
                run = None
            except Exception as e:
                evidence["exit_cleanup_error"] = type(e).__name__ + ": " + str(e)

        if run is not None and not h.base.process_pids():
            try:
                h.base.bus.close_run("v02149 pre-ready failure")
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
