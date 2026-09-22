from pathlib import Path
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

import validate_v02142_route_eligibility as v42

h = v42.h
CLAN_SRC = v42.CLAN_SRC
RUNNER_SRC = ROOT / r"workspace\_PatchStaging\AutonomousOperator_v002146_ProviderDispatchOutbox_20260921_1425\candidate"
CLAN_SHA = v42.CLAN_SHA
RUNNER_SHA = "29CF8B4413FC233EF184E9AB540441317697DBD96E757CE52AA726CCD3117CEB"
OWNER = "sol_provider_dispatch_outbox_v02146"
SOURCE_PREFIX = "ClanAI V02146 PROVIDER DISPATCH SOURCE "
FIXTURE_PROJECT = ROOT / r"workspace\_PatchStaging\AutonomousOperator_v002146_ProviderDispatchOutbox_20260921_1425\fixtures\Fixtures.csproj"
SHADOW = ROOT / r"Automation\TestRunner\patrol_defense_shadow.jsonl"
OUTBOX = ROOT / r"Automation\TestRunner\patrol_defense_provider_outbox.jsonl"
PROVIDER_INVOCATIONS = ROOT / r"Automation\TestRunner\patrol_defense_deliberation_provider_invocations.jsonl"

h.CLAN_SRC = CLAN_SRC
h.RUNNER_SRC = RUNNER_SRC
h.NEW_CLAN = CLAN_SHA
h.NEW_RUNNER = RUNNER_SHA


def wait_dispatch(start_hours, timeout=180, max_campaign_hours=120.0):
    end = time.time() + timeout
    last_hours = start_hours
    while time.time() < end:
        rows = h.read_jsonl(SHADOW)
        for row in reversed(rows):
            dispatch = row.get("providerDispatch")
            request = row.get("deliberationRequest")
            route = row.get("deliberationRouteEligibility")
            if (
                isinstance(dispatch, dict)
                and isinstance(request, dict)
                and isinstance(route, dict)
            ):
                return row, request, route, dispatch, last_hours

        status = h.base.read_status()
        try:
            current = float(status.get("campaignHours"))
            if current == current:
                last_hours = current
        except Exception:
            pass

        if (
            isinstance(last_hours, (int, float))
            and isinstance(start_hours, (int, float))
            and last_hours - start_hours >= max_campaign_hours
        ):
            break
        time.sleep(0.25)

    return None, None, None, None, last_hours


def dispatch_checks(row, request, route, dispatch):
    checks = dict(v42.validate_route(row, request, route))
    checks.update(
        {
            "dispatch_schema": dispatch.get("schema")
            == "BannerlordAI.PatrolDefenseProviderDispatch.v1",
            "dispatch_mode": dispatch.get("mode") == "observe",
            "dispatch_same_fingerprint": dispatch.get("requestFingerprint")
            == request.get("requestFingerprint")
            == route.get("requestFingerprint"),
            "dispatch_enqueued": dispatch.get("enqueued") is True,
            "dispatch_reason": dispatch.get("reason") == "ENQUEUED",
            "dispatch_queue_count": dispatch.get("queueCount") == 1,
            "dispatch_capacity": dispatch.get("capacity") == 32,
            "dispatch_sequence": dispatch.get("sequence") == 1,
            "dispatch_provider_false": dispatch.get("providerInvoked") is False,
            "dispatch_network_false": dispatch.get("externalNetworkUsed") is False,
            "dispatch_model_false": dispatch.get("modelInvoked") is False,
            "dispatch_llm_false": dispatch.get("llmInvoked") is False,
            "dispatch_planner_false": dispatch.get("plannerInvoked") is False,
            "dispatch_execution_false": dispatch.get("executionAuthorized") is False,
            "dispatch_interpretation_false": dispatch.get("interpretationApplied") is False,
            "dispatch_behavior_false": dispatch.get("behaviorMutation") is False,
            "dispatch_intent_false": dispatch.get("intentMutation") is False,
            "dispatch_score_false": dispatch.get("scoreMutation") is False,
            "dispatch_movement_zero": dispatch.get("nativeMovementCalls") == 0,
        }
    )
    return checks


def outbox_checks(job, request):
    return {
        "job_schema": job.get("schema")
        == "BannerlordAI.PatrolDefenseProviderDispatchJob.v1",
        "job_sequence": job.get("sequence") == 1,
        "job_fingerprint": job.get("requestFingerprint")
        == request.get("requestFingerprint"),
        "job_state": job.get("state") == "PENDING",
        "job_ticks": isinstance(job.get("enqueuedUtcTicks"), int)
        and job.get("enqueuedUtcTicks") > 0,
        "job_request_exact": job.get("deliberationRequest") == request,
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

    route_pos = runner_source.find("BuildPatrolDefenseDeliberationRouteEligibility(")
    dispatch_pos = runner_source.find("BuildPatrolDefenseProviderDispatch(")

    static_checks = {
        "queue_class": "PatrolDefenseProviderDispatchQueue" in dispatch_source,
        "capacity_32": "new PatrolDefenseProviderDispatchQueue(32)" in runner_source,
        "outbox_path": "patrol_defense_provider_outbox.jsonl" in runner_source,
        "route_before_dispatch": route_pos >= 0
        and dispatch_pos >= 0
        and route_pos < dispatch_pos,
        "dispatch_attachment": "providerDispatch" in dispatch_source,
        "provider_false": 'providerInvoked\\":false' in dispatch_source,
        "network_false": 'externalNetworkUsed\\":false' in dispatch_source,
        "model_false": 'modelInvoked\\":false' in dispatch_source,
        "no_http": "HttpClient" not in dispatch_source,
        "no_openai": "OpenAI" not in dispatch_source,
        "no_provider_runtime": "PatrolDefenseDeliberationProviderRuntime" not in dispatch_source,
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
    if fixture.returncode != 0 or "PASS_FIXTURES checks=108" not in fixture_result:
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
        "PatrolDefense_v02146_ProviderDispatchOutbox_" + stamp
    )
    validation.mkdir(parents=True)
    backup = ROOT / "workspace/_PatchBackups" / (
        "Before_ProviderDispatchOutbox_" + stamp
    )
    backup.mkdir(parents=True)

    operation = "provider-dispatch-outbox-v02146-" + stamp
    token = secrets.token_hex(32)
    token_hash = hashlib.sha256(token.encode()).hexdigest().upper()

    evidence = {
        "schema": "BannerlordAI.ProviderDispatchOutboxValidation.v1",
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
            "patrol_defense_provider_dispatch_outbox_shadow",
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
        if OUTBOX.exists():
            OUTBOX.unlink()
        if PROVIDER_INVOCATIONS.exists():
            PROVIDER_INVOCATIONS.unlink()

        run = h.base.bus.start_run(
            issuer="provider_dispatch_outbox_live",
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

        row, request, route, dispatch, last_hours = wait_dispatch(
            start_hours, 180, 120.0
        )
        evidence["campaign_start_hours"] = start_hours
        evidence["campaign_end_hours"] = last_hours

        if row is None:
            evidence["result"] = "ENVIRONMENTAL_INCONCLUSIVE"
            evidence["reason"] = (
                "No natural PatrolDefense shadow with providerDispatch within bounded window."
            )
            h.base.atomic_json(validation / "validation.json", evidence)
            h.safe_exit(
                OWNER,
                token,
                run,
                "v02146 provider dispatch environmental inconclusive",
            )
            run = None
        else:
            checks = dispatch_checks(row, request, route, dispatch)

            outbox_rows = h.read_jsonl(OUTBOX)
            if len(outbox_rows) != 1:
                raise RuntimeError(
                    "expected exactly one outbox row, got "
                    + str(len(outbox_rows))
                )
            job = outbox_rows[0]
            checks.update(
                {"outbox_" + k: v for k, v in outbox_checks(job, request).items()}
            )

            provider_rows = h.read_jsonl(PROVIDER_INVOCATIONS)
            checks["no_provider_invocation_rows"] = len(provider_rows) == 0
            checks["no_apply_side_effects"] = h.no_apply_side_effects()

            if not all(checks.values()):
                raise RuntimeError(
                    "provider dispatch live checks "
                    + json.dumps(checks, sort_keys=True)
                )

            evidence["shadow_receipt"] = row
            evidence["deliberation_request"] = request
            evidence["route_eligibility"] = route
            evidence["provider_dispatch"] = dispatch
            evidence["outbox_job"] = job
            evidence["provider_invocation_rows"] = len(provider_rows)
            evidence["live_checks"] = checks
            evidence["result"] = "PASS_PROVIDER_DISPATCH_OUTBOX_SHADOW"
            h.base.atomic_json(validation / "validation.json", evidence)

            h.safe_exit(
                OWNER,
                token,
                run,
                "v02146 provider dispatch outbox verified",
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
                    "enqueued": None if row is None else dispatch.get("enqueued"),
                    "outboxRows": None if row is None else 1,
                },
                indent=2,
            )
        )
        return 0 if evidence["result"] == "PASS_PROVIDER_DISPATCH_OUTBOX_SHADOW" else 2

    except Exception as ex:
        evidence["result"] = "FAIL"
        evidence["error"] = type(ex).__name__ + ": " + str(ex)
        evidence["failed_local"] = datetime.datetime.now().astimezone().isoformat()
        h.base.atomic_json(validation / "validation.json", evidence)

        if run is not None and h.base.process_pids():
            try:
                h.safe_exit(
                    OWNER, token, run, "v02146 provider dispatch failure"
                )
                run = None
            except Exception as e:
                evidence["exit_cleanup_error"] = type(e).__name__ + ": " + str(e)

        if run is not None and not h.base.process_pids():
            try:
                h.base.bus.close_run("v02146 pre-ready failure")
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
