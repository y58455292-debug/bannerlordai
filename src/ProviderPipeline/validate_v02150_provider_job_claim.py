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

import validate_v02146_provider_dispatch_outbox as v46

h = v46.h
CLAN_SRC = v46.CLAN_SRC
RUNNER_SRC = ROOT / r"workspace\_PatchStaging\AutonomousOperator_v002150_ProviderJobClaim_20260921_1457\candidate"
CLAN_SHA = v46.CLAN_SHA
RUNNER_SHA = "8FE355CDA2A4A70091E985432F86DF146235924BA905AE7344928C6511C3B7AF"
OWNER = "sol_provider_job_claim_v02150"
SOURCE_PREFIX = "ClanAI V02150 JOB CLAIM SOURCE "
FIXTURE_PROJECT = ROOT / r"workspace\_PatchStaging\AutonomousOperator_v002150_ProviderJobClaim_20260921_1457\fixtures\Fixtures.csproj"
OUTBOX = v46.OUTBOX
CLAIMS = ROOT / r"Automation\TestRunner\patrol_defense_provider_job_claims.jsonl"
PROVIDER_INVOCATIONS = v46.PROVIDER_INVOCATIONS

h.CLAN_SRC = CLAN_SRC
h.RUNNER_SRC = RUNNER_SRC
h.NEW_CLAN = CLAN_SHA
h.NEW_RUNNER = RUNNER_SHA


def claim_checks(row, fingerprint, reason, applied, idempotent, state_before, state_after):
    return {
        "schema": row.get("schema")
        == "BannerlordAI.PatrolDefenseProviderJobClaim.v1",
        "mode": row.get("mode") == "observe",
        "fingerprint": row.get("requestFingerprint") == fingerprint,
        "provider": row.get("providerId") == "deterministic_external_worker",
        "attempt": row.get("attemptId")
        == ("attempt-1" if reason != "ALREADY_CLAIMED" else "attempt-2"),
        "claim_applied": row.get("claimApplied") is applied,
        "idempotent": row.get("idempotent") is idempotent,
        "reason": row.get("reason") == reason,
        "state_before": row.get("stateBefore") == state_before,
        "state_after": row.get("stateAfter") == state_after,
        "queue_count": row.get("queueCount") == 1,
        "claimed_ticks": isinstance(row.get("claimedUtcTicks"), int)
        and row.get("claimedUtcTicks") > 0,
        "network_false": row.get("externalNetworkUsed") is False,
        "model_false": row.get("modelInvoked") is False,
        "llm_false": row.get("llmInvoked") is False,
        "planner_false": row.get("plannerInvoked") is False,
        "execution_false": row.get("executionAuthorized") is False,
        "interpretation_false": row.get("interpretationApplied") is False,
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

    lower_dispatch = dispatch_source.lower()
    static_checks = {
        "claim_result": "PatrolDefenseProviderJobClaimResult" in dispatch_source,
        "claim_method": "internal PatrolDefenseProviderJobClaimResult Claim("
        in dispatch_source,
        "claim_schema": "BannerlordAI.PatrolDefenseProviderJobClaim.v1"
        in dispatch_source,
        "claim_command": "PATROL_DEFENSE_PROVIDER_JOB_CLAIM " in runner_source,
        "claim_path": "patrol_defense_provider_job_claims.jsonl" in runner_source,
        "claimed_state": '"CLAIMED"' in dispatch_source,
        "idempotent_reason": "SAME_ATTEMPT_ALREADY_CLAIMED" in dispatch_source,
        "competing_reason": "ALREADY_CLAIMED" in dispatch_source,
        "no_timeout_word": "timeout" not in lower_dispatch,
        "no_ttl_word": "ttl" not in lower_dispatch,
        "no_expires_word": "expires" not in lower_dispatch,
        "no_timespan": "timespan" not in lower_dispatch,
        "no_http": "HttpClient" not in dispatch_source,
        "no_openai": "OpenAI" not in dispatch_source,
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
    if fixture.returncode != 0 or "PASS_FIXTURES checks=147" not in fixture_result:
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
        "PatrolDefense_v02150_ProviderJobClaim_" + stamp
    )
    validation.mkdir(parents=True)
    backup = ROOT / "workspace/_PatchBackups" / (
        "Before_ProviderJobClaim_" + stamp
    )
    backup.mkdir(parents=True)

    operation = "provider-job-claim-v02150-" + stamp
    token = secrets.token_hex(32)
    token_hash = hashlib.sha256(token.encode()).hexdigest().upper()

    evidence = {
        "schema": "BannerlordAI.ProviderJobClaimValidation.v1",
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
            "patrol_defense_provider_job_claim_shadow",
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
        for path in (OUTBOX, CLAIMS, PROVIDER_INVOCATIONS):
            if path.exists():
                path.unlink()

        run = h.base.bus.start_run(
            issuer="provider_job_claim_live",
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
                "v02150 provider job claim environmental inconclusive",
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
            fingerprint = request.get("requestFingerprint")

            before = len(h.read_jsonl(CLAIMS))
            first_command = (
                "PATROL_DEFENSE_PROVIDER_JOB_CLAIM "
                + fingerprint
                + " deterministic_external_worker attempt-1"
            )
            h.send_wait(
                OWNER,
                run,
                first_command,
                "patrol_defense_provider_job_claimed",
                10,
            )
            claim_rows = h.read_jsonl(CLAIMS)
            if len(claim_rows) <= before:
                raise RuntimeError("first claim receipt missing")
            first_claim = claim_rows[-1]
            first_checks = claim_checks(
                first_claim,
                fingerprint,
                "CLAIMED",
                True,
                False,
                "PENDING",
                "CLAIMED",
            )

            before_idempotent = len(claim_rows)
            h.send_wait(
                OWNER,
                run,
                first_command,
                "patrol_defense_provider_job_claim_idempotent",
                10,
            )
            claim_rows2 = h.read_jsonl(CLAIMS)
            if len(claim_rows2) <= before_idempotent:
                raise RuntimeError("idempotent claim receipt missing")
            same_claim = claim_rows2[-1]
            same_checks = claim_checks(
                same_claim,
                fingerprint,
                "SAME_ATTEMPT_ALREADY_CLAIMED",
                False,
                True,
                "CLAIMED",
                "CLAIMED",
            )
            same_checks["same_claimed_ticks"] = (
                same_claim.get("claimedUtcTicks")
                == first_claim.get("claimedUtcTicks")
            )

            before_competing = len(claim_rows2)
            competing_command = (
                "PATROL_DEFENSE_PROVIDER_JOB_CLAIM "
                + fingerprint
                + " deterministic_external_worker attempt-2"
            )
            h.send_wait(
                OWNER,
                run,
                competing_command,
                "patrol_defense_provider_job_claim_rejected:ALREADY_CLAIMED",
                10,
            )
            claim_rows3 = h.read_jsonl(CLAIMS)
            if len(claim_rows3) <= before_competing:
                raise RuntimeError("competing claim receipt missing")
            competing_claim = claim_rows3[-1]
            competing_checks = claim_checks(
                competing_claim,
                fingerprint,
                "ALREADY_CLAIMED",
                False,
                False,
                "CLAIMED",
                "CLAIMED",
            )
            competing_checks["same_original_claimed_ticks"] = (
                competing_claim.get("claimedUtcTicks")
                == first_claim.get("claimedUtcTicks")
            )

            provider_rows = h.read_jsonl(PROVIDER_INVOCATIONS)

            checks = {}
            checks.update({"dispatch_" + k: v for k, v in dispatch_checks.items()})
            checks.update({"first_" + k: v for k, v in first_checks.items()})
            checks.update({"same_" + k: v for k, v in same_checks.items()})
            checks.update(
                {"competing_" + k: v for k, v in competing_checks.items()}
            )
            checks["outbox_request_exact"] = (
                job.get("requestFingerprint") == fingerprint
                and job.get("deliberationRequest") == request
            )
            checks["provider_invocation_rows_empty"] = len(provider_rows) == 0
            checks["no_apply_side_effects"] = h.no_apply_side_effects()

            if not all(checks.values()):
                raise RuntimeError(
                    "provider job claim live checks "
                    + json.dumps(checks, sort_keys=True)
                )

            evidence["shadow_receipt"] = row
            evidence["provider_dispatch"] = dispatch
            evidence["outbox_job"] = job
            evidence["first_claim"] = first_claim
            evidence["idempotent_claim"] = same_claim
            evidence["competing_claim"] = competing_claim
            evidence["provider_invocation_rows"] = len(provider_rows)
            evidence["live_checks"] = checks
            evidence["result"] = "PASS_PROVIDER_JOB_CLAIM_ATTEMPT_SHADOW"
            h.base.atomic_json(validation / "validation.json", evidence)

            h.safe_exit(
                OWNER,
                token,
                run,
                "v02150 provider job claim verified",
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
                    "firstReason": None if row is None else first_claim.get("reason"),
                    "sameReason": None if row is None else same_claim.get("reason"),
                    "competingReason": None
                    if row is None
                    else competing_claim.get("reason"),
                },
                indent=2,
            )
        )
        return 0 if evidence["result"] == "PASS_PROVIDER_JOB_CLAIM_ATTEMPT_SHADOW" else 2

    except Exception as ex:
        evidence["result"] = "FAIL"
        evidence["error"] = type(ex).__name__ + ": " + str(ex)
        evidence["failed_local"] = datetime.datetime.now().astimezone().isoformat()
        h.base.atomic_json(validation / "validation.json", evidence)

        if run is not None and h.base.process_pids():
            try:
                h.safe_exit(OWNER, token, run, "v02150 provider job claim failure")
                run = None
            except Exception as e:
                evidence["exit_cleanup_error"] = type(e).__name__ + ": " + str(e)

        if run is not None and not h.base.process_pids():
            try:
                h.base.bus.close_run("v02150 pre-ready failure")
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
