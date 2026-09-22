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
import validate_v02149_provider_work_lifecycle as v49
import validate_v02150_provider_job_claim as v50

h = v46.h
CLAN_SRC = v46.CLAN_SRC
RUNNER_SRC = ROOT / r"workspace\_PatchStaging\AutonomousOperator_v002151_ClaimBoundProviderResult_20260921_1504\candidate"
CLAN_SHA = v46.CLAN_SHA
RUNNER_SHA = "029074490AF832FC0B403CFCED7F6A22E607AB5A08B9115A46BFA5A29E3A89A6"
OWNER = "sol_claim_bound_provider_result_v02151"
SOURCE_PREFIX = "ClanAI V02151 CLAIM BOUND RESULT SOURCE "
FIXTURE_PROJECT = ROOT / r"workspace\_PatchStaging\AutonomousOperator_v002151_ClaimBoundProviderResult_20260921_1504\fixtures\Fixtures.csproj"
OUTBOX = v46.OUTBOX
CLAIMS = v50.CLAIMS
RESULT_ADMISSIONS = v48.RESULT_ADMISSIONS
COMPLETIONS = v49.COMPLETIONS
PROVIDER_INVOCATIONS = v46.PROVIDER_INVOCATIONS

h.CLAN_SRC = CLAN_SRC
h.RUNNER_SRC = RUNNER_SRC
h.NEW_CLAN = CLAN_SHA
h.NEW_RUNNER = RUNNER_SHA


def result_success_claim_checks(receipt, fingerprint):
    checks = dict(v48.result_success_checks(receipt, fingerprint))
    checks["claim_match"] = receipt.get("claimMatchReason") == "CLAIM_MATCHED"
    return checks


def result_replay_claim_checks(receipt, fingerprint):
    reasons = receipt.get("rejectionReasons")
    return {
        "schema": receipt.get("schema")
        == "BannerlordAI.PatrolDefenseProviderResultAdmission.v1",
        "fingerprint": receipt.get("requestFingerprint") == fingerprint,
        "provider": receipt.get("providerId") == "deterministic_external_worker",
        "attempt": receipt.get("attemptId") == "attempt-1",
        "status": receipt.get("status") == "SUCCESS",
        "accepted_false": receipt.get("providerResultAccepted") is False,
        "claim_job_missing": receipt.get("claimMatchReason")
        == "DISPATCH_JOB_NOT_FOUND",
        "reason_job_missing": isinstance(reasons, list)
        and "DISPATCH_JOB_NOT_FOUND" in reasons,
        "advisory_null": receipt.get("advisoryAdmission") is None,
        "execution_false": receipt.get("executionAuthorized") is False,
        "model_false": receipt.get("modelInvoked") is False,
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

    parse_pos = result_source.find("TryParseFlatStringObject(")
    match_pos = result_source.find("dispatchQueue.MatchClaim(")
    gate_pos = result_source.find("gate.PendingRequestFingerprint")

    static_checks = {
        "match_type": "PatrolDefenseProviderClaimMatchResult" in dispatch_source,
        "match_method": "MatchClaim(" in dispatch_source,
        "not_claimed_reason": "PROVIDER_JOB_NOT_CLAIMED" in dispatch_source,
        "mismatch_reason": "PROVIDER_CLAIM_MISMATCH" in dispatch_source,
        "matched_reason": "CLAIM_MATCHED" in dispatch_source,
        "claim_bound_evaluator": "EvaluateClaimBound(" in result_source,
        "product_uses_claim_bound": "EvaluateClaimBound(" in runner_source,
        "claim_reason_receipt": 'claimMatchReason' in result_source,
        "ordering": parse_pos >= 0
        and match_pos >= 0
        and gate_pos >= 0
        and parse_pos < match_pos < gate_pos,
        "no_http": "HttpClient" not in result_source,
        "no_openai": "OpenAI" not in result_source,
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
    if fixture.returncode != 0 or "PASS_FIXTURES checks=160" not in fixture_result:
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
        "PatrolDefense_v02151_ClaimBoundProviderResult_" + stamp
    )
    validation.mkdir(parents=True)
    backup = ROOT / "workspace/_PatchBackups" / (
        "Before_ClaimBoundProviderResult_" + stamp
    )
    backup.mkdir(parents=True)

    operation = "claim-bound-provider-result-v02151-" + stamp
    token = secrets.token_hex(32)
    token_hash = hashlib.sha256(token.encode()).hexdigest().upper()

    evidence = {
        "schema": "BannerlordAI.ClaimBoundProviderResultValidation.v1",
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
            "patrol_defense_claim_bound_provider_result_shadow",
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
        for path in (OUTBOX, CLAIMS, RESULT_ADMISSIONS, COMPLETIONS, PROVIDER_INVOCATIONS):
            if path.exists():
                path.unlink()

        run = h.base.bus.start_run(
            issuer="claim_bound_provider_result_live",
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
                "v02151 claim-bound result environmental inconclusive",
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

            before_claims = len(h.read_jsonl(CLAIMS))
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
            if len(claim_rows) <= before_claims:
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
            if not all(claim_checks.values()):
                raise RuntimeError(
                    "claim checks " + json.dumps(claim_checks, sort_keys=True)
                )

            (
                worker_fp,
                worker_request,
                worker_advisory,
                worker_envelope,
                command,
            ) = v48.worker_build_result_envelope(job)
            if worker_fp != fingerprint:
                raise RuntimeError("worker fingerprint mismatch")

            before_results = len(h.read_jsonl(RESULT_ADMISSIONS))
            before_completions = len(h.read_jsonl(COMPLETIONS))
            h.send_wait(
                OWNER,
                run,
                command,
                "patrol_defense_provider_result_admitted",
                10,
            )

            result_rows = h.read_jsonl(RESULT_ADMISSIONS)
            completion_rows = h.read_jsonl(COMPLETIONS)
            if len(result_rows) <= before_results:
                raise RuntimeError("provider result receipt missing")
            if len(completion_rows) <= before_completions:
                raise RuntimeError("completion receipt missing")

            admitted = result_rows[-1]
            completion = completion_rows[-1]
            admitted_checks = result_success_claim_checks(admitted, fingerprint)
            completion_checks = v49.completion_success_checks(completion, fingerprint)
            if not all(admitted_checks.values()):
                raise RuntimeError(
                    "claim-bound success checks "
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
                "patrol_defense_provider_result_rejected:DISPATCH_JOB_NOT_FOUND",
                10,
            )

            result_rows2 = h.read_jsonl(RESULT_ADMISSIONS)
            completion_rows2 = h.read_jsonl(COMPLETIONS)
            if len(result_rows2) <= before_results2:
                raise RuntimeError("replay result receipt missing")
            if len(completion_rows2) <= before_completions2:
                raise RuntimeError("replay completion receipt missing")

            replay = result_rows2[-1]
            replay_completion = completion_rows2[-1]
            replay_checks = result_replay_claim_checks(replay, fingerprint)
            replay_completion_checks = v49.completion_replay_checks(
                replay_completion, fingerprint
            )
            if not all(replay_checks.values()):
                raise RuntimeError(
                    "claim-bound replay checks "
                    + json.dumps(replay_checks, sort_keys=True)
                )
            if not all(replay_completion_checks.values()):
                raise RuntimeError(
                    "replay completion checks "
                    + json.dumps(replay_completion_checks, sort_keys=True)
                )

            provider_rows = h.read_jsonl(PROVIDER_INVOCATIONS)

            checks = {}
            checks.update({"dispatch_" + k: v for k, v in dispatch_checks.items()})
            checks.update({"claim_" + k: v for k, v in claim_checks.items()})
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
            checks["worker_envelope_provider"] = (
                worker_envelope.get("providerId") == "deterministic_external_worker"
            )
            checks["worker_envelope_attempt"] = (
                worker_envelope.get("attemptId") == "attempt-1"
            )
            checks["provider_invocation_rows_empty"] = len(provider_rows) == 0
            checks["no_apply_side_effects"] = h.no_apply_side_effects()

            if not all(checks.values()):
                raise RuntimeError(
                    "claim-bound result live checks "
                    + json.dumps(checks, sort_keys=True)
                )

            evidence["shadow_receipt"] = row
            evidence["provider_dispatch"] = dispatch
            evidence["outbox_job"] = job
            evidence["provider_job_claim"] = claim
            evidence["provider_result_admission"] = admitted
            evidence["dispatch_completion"] = completion
            evidence["provider_result_replay"] = replay
            evidence["dispatch_completion_replay"] = replay_completion
            evidence["provider_invocation_rows"] = len(provider_rows)
            evidence["live_checks"] = checks
            evidence["result"] = "PASS_CLAIM_BOUND_PROVIDER_RESULT_SHADOW"
            h.base.atomic_json(validation / "validation.json", evidence)

            h.safe_exit(
                OWNER,
                token,
                run,
                "v02151 claim-bound provider result verified",
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
                    "claimMatch": None
                    if row is None
                    else admitted.get("claimMatchReason"),
                    "completion": None
                    if row is None
                    else completion.get("reason"),
                    "replayClaimMatch": None
                    if row is None
                    else replay.get("claimMatchReason"),
                },
                indent=2,
            )
        )
        return 0 if evidence["result"] == "PASS_CLAIM_BOUND_PROVIDER_RESULT_SHADOW" else 2

    except Exception as ex:
        evidence["result"] = "FAIL"
        evidence["error"] = type(ex).__name__ + ": " + str(ex)
        evidence["failed_local"] = datetime.datetime.now().astimezone().isoformat()
        h.base.atomic_json(validation / "validation.json", evidence)

        if run is not None and h.base.process_pids():
            try:
                h.safe_exit(
                    OWNER, token, run, "v02151 claim-bound provider result failure"
                )
                run = None
            except Exception as e:
                evidence["exit_cleanup_error"] = type(e).__name__ + ": " + str(e)

        if run is not None and not h.base.process_pids():
            try:
                h.base.bus.close_run("v02151 pre-ready failure")
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
