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

h = v46.h
CLAN_SRC = v46.CLAN_SRC
RUNNER_SRC = ROOT / r"workspace\_PatchStaging\AutonomousOperator_v002152_ProviderClaimRelease_20260921_1510\candidate"
CLAN_SHA = v46.CLAN_SHA
RUNNER_SHA = "CECAFBCD366E82A50D16A803621909E3C930A8BB62A5BC432FF3B4377B0424B8"
OWNER = "sol_provider_claim_release_v02152"
SOURCE_PREFIX = "ClanAI V02152 CLAIM RELEASE SOURCE "
FIXTURE_PROJECT = ROOT / r"workspace\_PatchStaging\AutonomousOperator_v002152_ProviderClaimRelease_20260921_1510\fixtures\Fixtures.csproj"
OUTBOX = v46.OUTBOX
CLAIMS = v50.CLAIMS
RELEASES = ROOT / r"Automation\TestRunner\patrol_defense_provider_job_releases.jsonl"
RESULT_ADMISSIONS = ROOT / r"Automation\TestRunner\patrol_defense_provider_result_admissions.jsonl"
COMPLETIONS = v49.COMPLETIONS
PROVIDER_INVOCATIONS = v46.PROVIDER_INVOCATIONS

h.CLAN_SRC = CLAN_SRC
h.RUNNER_SRC = RUNNER_SRC
h.NEW_CLAN = CLAN_SHA
h.NEW_RUNNER = RUNNER_SHA


def transient_result_command(fingerprint):
    envelope = {
        "schema": "BannerlordAI.PatrolDefenseProviderResult.v1",
        "providerId": "deterministic_external_worker",
        "attemptId": "attempt-1",
        "requestFingerprint": fingerprint,
        "status": "TRANSIENT_FAILURE",
        "advisoryBase64": "",
    }
    raw = json.dumps(envelope, separators=(",", ":"))
    return "PATROL_DEFENSE_PROVIDER_RESULT_ADMIT " + base64.b64encode(
        raw.encode("utf-8")
    ).decode("ascii")


def transient_result_checks(row, fingerprint):
    reasons = row.get("rejectionReasons")
    return {
        "schema": row.get("schema")
        == "BannerlordAI.PatrolDefenseProviderResultAdmission.v1",
        "provider": row.get("providerId") == "deterministic_external_worker",
        "attempt": row.get("attemptId") == "attempt-1",
        "fingerprint": row.get("requestFingerprint") == fingerprint,
        "status": row.get("status") == "TRANSIENT_FAILURE",
        "claim_match": row.get("claimMatchReason") == "CLAIM_MATCHED",
        "accepted_false": row.get("providerResultAccepted") is False,
        "retryable_true": row.get("retryable") is True,
        "reason": isinstance(reasons, list)
        and "PROVIDER_TRANSIENT_FAILURE" in reasons,
        "advisory_null": row.get("advisoryAdmission") is None,
        "execution_false": row.get("executionAuthorized") is False,
        "model_false": row.get("modelInvoked") is False,
        "behavior_false": row.get("behaviorMutation") is False,
        "score_false": row.get("scoreMutation") is False,
        "movement_zero": row.get("nativeMovementCalls") == 0,
    }


def release_checks(row, fingerprint):
    return {
        "schema": row.get("schema")
        == "BannerlordAI.PatrolDefenseProviderJobRelease.v1",
        "mode": row.get("mode") == "observe",
        "fingerprint": row.get("requestFingerprint") == fingerprint,
        "provider": row.get("providerId") == "deterministic_external_worker",
        "attempt": row.get("attemptId") == "attempt-1",
        "applied": row.get("releaseApplied") is True,
        "reason": row.get("reason") == "CLAIM_RELEASED",
        "before": row.get("stateBefore") == "CLAIMED",
        "after": row.get("stateAfter") == "PENDING",
        "queue_count": row.get("queueCount") == 1,
        "network_false": row.get("externalNetworkUsed") is False,
        "model_false": row.get("modelInvoked") is False,
        "execution_false": row.get("executionAuthorized") is False,
        "behavior_false": row.get("behaviorMutation") is False,
        "score_false": row.get("scoreMutation") is False,
        "movement_zero": row.get("nativeMovementCalls") == 0,
    }


def attempt2_claim_checks(row, fingerprint):
    return {
        "schema": row.get("schema")
        == "BannerlordAI.PatrolDefenseProviderJobClaim.v1",
        "fingerprint": row.get("requestFingerprint") == fingerprint,
        "provider": row.get("providerId") == "deterministic_external_worker",
        "attempt": row.get("attemptId") == "attempt-2",
        "applied": row.get("claimApplied") is True,
        "idempotent_false": row.get("idempotent") is False,
        "reason": row.get("reason") == "CLAIMED",
        "before": row.get("stateBefore") == "PENDING",
        "after": row.get("stateAfter") == "CLAIMED",
        "queue_count": row.get("queueCount") == 1,
        "claimed_ticks": isinstance(row.get("claimedUtcTicks"), int)
        and row.get("claimedUtcTicks") > 0,
        "network_false": row.get("externalNetworkUsed") is False,
        "model_false": row.get("modelInvoked") is False,
        "execution_false": row.get("executionAuthorized") is False,
        "behavior_false": row.get("behaviorMutation") is False,
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
        "release_type": "PatrolDefenseProviderJobReleaseResult" in dispatch_source,
        "release_method": "ReleaseClaim(" in dispatch_source,
        "release_schema": "BannerlordAI.PatrolDefenseProviderJobRelease.v1"
        in dispatch_source,
        "release_command": "PATROL_DEFENSE_PROVIDER_JOB_RELEASE " in runner_source,
        "release_path": "patrol_defense_provider_job_releases.jsonl"
        in runner_source,
        "released_reason": "CLAIM_RELEASED" in dispatch_source,
        "mismatch_reason": "PROVIDER_CLAIM_MISMATCH" in dispatch_source,
        "not_claimed_reason": "PROVIDER_JOB_NOT_CLAIMED" in dispatch_source,
        "no_timeout": "timeout" not in lower_dispatch,
        "no_ttl": "ttl" not in lower_dispatch,
        "no_expires": "expires" not in lower_dispatch,
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
    if fixture.returncode != 0 or "PASS_FIXTURES checks=173" not in fixture_result:
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
        "PatrolDefense_v02152_ProviderClaimRelease_" + stamp
    )
    validation.mkdir(parents=True)
    backup = ROOT / "workspace/_PatchBackups" / (
        "Before_ProviderClaimRelease_" + stamp
    )
    backup.mkdir(parents=True)

    operation = "provider-claim-release-v02152-" + stamp
    token = secrets.token_hex(32)
    token_hash = hashlib.sha256(token.encode()).hexdigest().upper()

    evidence = {
        "schema": "BannerlordAI.ProviderClaimReleaseValidation.v1",
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
            "patrol_defense_provider_claim_release_shadow",
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
            RELEASES,
            RESULT_ADMISSIONS,
            COMPLETIONS,
            PROVIDER_INVOCATIONS,
        ):
            if path.exists():
                path.unlink()

        run = h.base.bus.start_run(
            issuer="provider_claim_release_live",
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
                "v02152 provider claim release environmental inconclusive",
            )
            run = None
        else:
            dispatch_checks = v46.dispatch_checks(row, request, route, dispatch)
            fingerprint = request.get("requestFingerprint")

            claim1_command = (
                "PATROL_DEFENSE_PROVIDER_JOB_CLAIM "
                + fingerprint
                + " deterministic_external_worker attempt-1"
            )
            before_claims = len(h.read_jsonl(CLAIMS))
            h.send_wait(
                OWNER,
                run,
                claim1_command,
                "patrol_defense_provider_job_claimed",
                10,
            )
            claims1 = h.read_jsonl(CLAIMS)
            if len(claims1) <= before_claims:
                raise RuntimeError("attempt-1 claim receipt missing")
            claim1 = claims1[-1]
            claim1_checks = v50.claim_checks(
                claim1,
                fingerprint,
                "CLAIMED",
                True,
                False,
                "PENDING",
                "CLAIMED",
            )

            transient_command = transient_result_command(fingerprint)
            before_results = len(h.read_jsonl(RESULT_ADMISSIONS))
            before_completions = len(h.read_jsonl(COMPLETIONS))
            h.send_wait(
                OWNER,
                run,
                transient_command,
                "patrol_defense_provider_result_rejected:PROVIDER_TRANSIENT_FAILURE",
                10,
            )
            result_rows = h.read_jsonl(RESULT_ADMISSIONS)
            completion_rows = h.read_jsonl(COMPLETIONS)
            if len(result_rows) <= before_results:
                raise RuntimeError("transient result receipt missing")
            if len(completion_rows) <= before_completions:
                raise RuntimeError("transient completion receipt missing")
            transient_result = result_rows[-1]
            transient_completion = completion_rows[-1]
            transient_checks = transient_result_checks(
                transient_result, fingerprint
            )
            transient_completion_checks = {
                "reason": transient_completion.get("reason")
                == "TRANSIENT_FAILURE_RETAINED",
                "before": transient_completion.get("queueCountBefore") == 1,
                "after": transient_completion.get("queueCountAfter") == 1,
                "pending": transient_completion.get("pendingRetained") is True,
                "applied_false": transient_completion.get("completionApplied")
                is False,
            }

            release_command = (
                "PATROL_DEFENSE_PROVIDER_JOB_RELEASE "
                + fingerprint
                + " deterministic_external_worker attempt-1"
            )
            before_releases = len(h.read_jsonl(RELEASES))
            h.send_wait(
                OWNER,
                run,
                release_command,
                "patrol_defense_provider_job_released",
                10,
            )
            release_rows = h.read_jsonl(RELEASES)
            if len(release_rows) <= before_releases:
                raise RuntimeError("release receipt missing")
            release = release_rows[-1]
            release_result_checks = release_checks(release, fingerprint)

            claim2_command = (
                "PATROL_DEFENSE_PROVIDER_JOB_CLAIM "
                + fingerprint
                + " deterministic_external_worker attempt-2"
            )
            before_claim2 = len(h.read_jsonl(CLAIMS))
            h.send_wait(
                OWNER,
                run,
                claim2_command,
                "patrol_defense_provider_job_claimed",
                10,
            )
            claims2 = h.read_jsonl(CLAIMS)
            if len(claims2) <= before_claim2:
                raise RuntimeError("attempt-2 claim receipt missing")
            claim2 = claims2[-1]
            claim2_checks = attempt2_claim_checks(claim2, fingerprint)

            provider_rows = h.read_jsonl(PROVIDER_INVOCATIONS)

            checks = {}
            checks.update({"dispatch_" + k: v for k, v in dispatch_checks.items()})
            checks.update({"claim1_" + k: v for k, v in claim1_checks.items()})
            checks.update({"transient_" + k: v for k, v in transient_checks.items()})
            checks.update(
                {
                    "transient_completion_" + k: v
                    for k, v in transient_completion_checks.items()
                }
            )
            checks.update({"release_" + k: v for k, v in release_result_checks.items()})
            checks.update({"claim2_" + k: v for k, v in claim2_checks.items()})
            checks["provider_invocation_rows_empty"] = len(provider_rows) == 0
            checks["claim2_ticks_new"] = (
                claim2.get("claimedUtcTicks") != claim1.get("claimedUtcTicks")
            )
            checks["no_apply_side_effects"] = h.no_apply_side_effects()

            if not all(checks.values()):
                raise RuntimeError(
                    "provider claim release live checks "
                    + json.dumps(checks, sort_keys=True)
                )

            evidence["shadow_receipt"] = row
            evidence["provider_dispatch"] = dispatch
            evidence["attempt1_claim"] = claim1
            evidence["transient_result"] = transient_result
            evidence["transient_completion"] = transient_completion
            evidence["claim_release"] = release
            evidence["attempt2_claim"] = claim2
            evidence["provider_invocation_rows"] = len(provider_rows)
            evidence["live_checks"] = checks
            evidence["result"] = "PASS_PROVIDER_CLAIM_RELEASE_RETRY_SHADOW"
            h.base.atomic_json(validation / "validation.json", evidence)

            h.safe_exit(
                OWNER,
                token,
                run,
                "v02152 provider claim release verified",
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
                    "transientReason": None
                    if row is None
                    else transient_completion.get("reason"),
                    "releaseReason": None
                    if row is None
                    else release.get("reason"),
                    "attempt2Reason": None
                    if row is None
                    else claim2.get("reason"),
                },
                indent=2,
            )
        )
        return 0 if evidence["result"] == "PASS_PROVIDER_CLAIM_RELEASE_RETRY_SHADOW" else 2

    except Exception as ex:
        evidence["result"] = "FAIL"
        evidence["error"] = type(ex).__name__ + ": " + str(ex)
        evidence["failed_local"] = datetime.datetime.now().astimezone().isoformat()
        h.base.atomic_json(validation / "validation.json", evidence)

        if run is not None and h.base.process_pids():
            try:
                h.safe_exit(OWNER, token, run, "v02152 provider claim release failure")
                run = None
            except Exception as e:
                evidence["exit_cleanup_error"] = type(e).__name__ + ": " + str(e)

        if run is not None and not h.base.process_pids():
            try:
                h.base.bus.close_run("v02152 pre-ready failure")
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
