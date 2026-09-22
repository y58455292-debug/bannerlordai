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
import provider_request_contract_v02154 as contract

h = v46.h
CLAN_SRC = v46.CLAN_SRC
RUNNER_SRC = ROOT / r"workspace\_PatchStaging\AutonomousOperator_v002156_ProviderResultV2Binding_20260921_1549\candidate"
CLAN_SHA = v46.CLAN_SHA
RUNNER_SHA = "7732C135648EE1E81587EF72E742DEB29260E0CEBD3005009BF66774F413A39C"
OWNER = "sol_provider_result_v2_binding_v02156"
SOURCE_PREFIX = "ClanAI V02156 RESULT V2 SOURCE "
FIXTURE_PROJECT = ROOT / r"workspace\_PatchStaging\AutonomousOperator_v002156_ProviderResultV2Binding_20260921_1549\fixtures\Fixtures.csproj"
CONTRACT_FIXTURE = ROOT / r"workspace\test_provider_request_contract_v02154.py"
OUTBOX = v46.OUTBOX
CLAIMS = v50.CLAIMS
REGISTRATIONS = v55.REGISTRATIONS
RESULT_V2 = ROOT / r"Automation\TestRunner\patrol_defense_provider_result_v2_admissions.jsonl"
COMPLETIONS = v49.COMPLETIONS
PROVIDER_INVOCATIONS = v46.PROVIDER_INVOCATIONS
PROVIDER_ID = "deterministic_external_worker"
MODEL_ID = "deterministic_mock_no_model"

h.CLAN_SRC = CLAN_SRC
h.RUNNER_SRC = RUNNER_SRC
h.NEW_CLAN = CLAN_SHA
h.NEW_RUNNER = RUNNER_SHA


def build_v2_result_command(fingerprint, provider_request_id):
    advisory = {
        "schema": "BannerlordAI.PatrolDefenseDeliberationAdvisory.v1",
        "requestFingerprint": fingerprint,
        "disposition": "KEEP_BASELINE",
    }
    advisory_json = json.dumps(advisory, separators=(",", ":"))
    envelope = {
        "schema": "BannerlordAI.PatrolDefenseProviderResult.v2",
        "providerId": PROVIDER_ID,
        "attemptId": "attempt-1",
        "requestFingerprint": fingerprint,
        "providerRequestId": provider_request_id,
        "status": "SUCCESS",
        "advisoryBase64": base64.b64encode(
            advisory_json.encode("utf-8")
        ).decode("ascii"),
    }
    raw = json.dumps(envelope, separators=(",", ":"))
    return envelope, "PATROL_DEFENSE_PROVIDER_RESULT_V2_ADMIT " + base64.b64encode(
        raw.encode("utf-8")
    ).decode("ascii")


def v2_success_checks(row, fingerprint, provider_request_id):
    admission = row.get("advisoryAdmission")
    return {
        "schema": row.get("schema")
        == "BannerlordAI.PatrolDefenseProviderResultV2Admission.v1",
        "provider": row.get("providerId") == PROVIDER_ID,
        "attempt": row.get("attemptId") == "attempt-1",
        "fingerprint": row.get("requestFingerprint") == fingerprint,
        "provider_request_id": row.get("providerRequestId") == provider_request_id,
        "status": row.get("status") == "SUCCESS",
        "claim_match": row.get("claimMatchReason") == "CLAIM_MATCHED",
        "request_match": row.get("providerRequestMatchReason")
        == "PROVIDER_REQUEST_MATCHED",
        "accepted": row.get("providerResultAccepted") is True,
        "retryable_false": row.get("retryable") is False,
        "no_rejections": row.get("rejectionReasons") == [],
        "advisory_present": isinstance(admission, dict),
        "advisory_admitted": isinstance(admission, dict)
        and admission.get("admitted") is True,
        "advisory_disposition": isinstance(admission, dict)
        and admission.get("disposition") == "KEEP_BASELINE",
        "execution_false": row.get("executionAuthorized") is False,
        "model_false": row.get("modelInvoked") is False,
        "llm_false": row.get("llmInvoked") is False,
        "planner_false": row.get("plannerInvoked") is False,
        "behavior_false": row.get("behaviorMutation") is False,
        "intent_false": row.get("intentMutation") is False,
        "score_false": row.get("scoreMutation") is False,
        "movement_zero": row.get("nativeMovementCalls") == 0,
    }


def v2_replay_checks(row, fingerprint, provider_request_id):
    reasons = row.get("rejectionReasons")
    return {
        "schema": row.get("schema")
        == "BannerlordAI.PatrolDefenseProviderResultV2Admission.v1",
        "provider": row.get("providerId") == PROVIDER_ID,
        "attempt": row.get("attemptId") == "attempt-1",
        "fingerprint": row.get("requestFingerprint") == fingerprint,
        "provider_request_id": row.get("providerRequestId") == provider_request_id,
        "status": row.get("status") == "SUCCESS",
        "claim_job_missing": row.get("claimMatchReason") == "DISPATCH_JOB_NOT_FOUND",
        "request_match_null": row.get("providerRequestMatchReason") is None,
        "accepted_false": row.get("providerResultAccepted") is False,
        "reason_job_missing": isinstance(reasons, list)
        and "DISPATCH_JOB_NOT_FOUND" in reasons,
        "advisory_null": row.get("advisoryAdmission") is None,
        "execution_false": row.get("executionAuthorized") is False,
        "model_false": row.get("modelInvoked") is False,
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
    v1_source = (RUNNER_SRC / "PatrolDefenseProviderResultAdmission.cs").read_text(
        encoding="utf-8", errors="replace"
    )
    v2_source = (RUNNER_SRC / "PatrolDefenseProviderResultV2Admission.cs").read_text(
        encoding="utf-8", errors="replace"
    )

    parse_pos = v2_source.find("TryParseFlatStringObject(")
    claim_pos = v2_source.find("dispatchQueue.MatchClaim(")
    request_pos = v2_source.find("dispatchQueue.MatchRegisteredProviderRequest(")
    gate_pos = v2_source.find("gate.PendingRequestFingerprint")

    static_checks = {
        "v1_schema_preserved": "BannerlordAI.PatrolDefenseProviderResult.v1" in v1_source,
        "v1_command_preserved": "PATROL_DEFENSE_PROVIDER_RESULT_ADMIT " in runner_source,
        "v2_schema": "BannerlordAI.PatrolDefenseProviderResult.v2" in v2_source,
        "v2_receipt_schema": "BannerlordAI.PatrolDefenseProviderResultV2Admission.v1"
        in v2_source,
        "v2_command": "PATROL_DEFENSE_PROVIDER_RESULT_V2_ADMIT " in runner_source,
        "v2_path": "patrol_defense_provider_result_v2_admissions.jsonl"
        in runner_source,
        "request_match_method": "MatchRegisteredProviderRequest(" in dispatch_source,
        "request_not_registered": "PROVIDER_REQUEST_NOT_REGISTERED" in dispatch_source,
        "request_id_mismatch": "PROVIDER_REQUEST_ID_MISMATCH" in dispatch_source,
        "request_matched": "PROVIDER_REQUEST_MATCHED" in dispatch_source,
        "ordering": parse_pos >= 0
        and claim_pos >= 0
        and request_pos >= 0
        and gate_pos >= 0
        and parse_pos < claim_pos < request_pos < gate_pos,
        "no_http": "HttpClient" not in v2_source,
        "no_openai": "OpenAI" not in v2_source,
        "no_apply": "PatrolDefenseApplyGate" not in v2_source,
        "no_score": "SetBehaviorScore" not in v2_source,
        "no_memory_write": "RecordPatrolDefenseEpisode" not in v2_source,
    }
    if not all(static_checks.values()):
        raise RuntimeError("static checks " + json.dumps(static_checks, sort_keys=True))

    fixture = subprocess.run(
        ["dotnet", "run", "--project", str(FIXTURE_PROJECT), "-c", "Release"],
        capture_output=True,
        text=True,
    )
    fixture_result = (fixture.stdout or "").strip()
    if fixture.returncode != 0 or "PASS_FIXTURES checks=206" not in fixture_result:
        raise RuntimeError(
            "runner fixture gate failed " + fixture_result + " " + (fixture.stderr or "")
        )

    contract_fixture = subprocess.run(
        [sys.executable, "-X", "utf8", str(CONTRACT_FIXTURE)],
        capture_output=True,
        text=True,
    )
    contract_result = (contract_fixture.stdout or "").strip()
    if contract_fixture.returncode != 0 or "PASS_FIXTURES checks=49" not in contract_result:
        raise RuntimeError(
            "contract fixture gate failed " + contract_result + " " + (contract_fixture.stderr or "")
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
        "PatrolDefense_v02156_ProviderResultV2Binding_" + stamp
    )
    validation.mkdir(parents=True)
    backup = ROOT / "workspace/_PatchBackups" / (
        "Before_ProviderResultV2Binding_" + stamp
    )
    backup.mkdir(parents=True)

    operation = "provider-result-v2-binding-v02156-" + stamp
    token = secrets.token_hex(32)
    token_hash = hashlib.sha256(token.encode()).hexdigest().upper()

    evidence = {
        "schema": "BannerlordAI.ProviderResultV2BindingValidation.v1",
        "created_local": datetime.datetime.now().astimezone().isoformat(),
        "result": "IN_PROGRESS",
        "source_slot": source_slot,
        "clanai_test_sha256": CLAN_SHA,
        "runner_test_sha256": RUNNER_SHA,
        "isolated_fixtures": fixture_result,
        "contract_fixtures": contract_result,
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
            "patrol_defense_provider_result_v2_binding_shadow",
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
            issuer="provider_result_v2_binding_live",
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
                "v02156 provider result v2 environmental inconclusive",
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
            register_command = (
                "PATROL_DEFENSE_PROVIDER_REQUEST_REGISTER "
                + base64.b64encode(request_envelope_json.encode("utf-8")).decode("ascii")
            )

            before_reg = len(h.read_jsonl(REGISTRATIONS))
            h.send_wait(
                OWNER,
                run,
                register_command,
                "patrol_defense_provider_request_registered",
                10,
            )
            reg_rows = h.read_jsonl(REGISTRATIONS)
            if len(reg_rows) <= before_reg:
                raise RuntimeError("provider request registration receipt missing")
            registration = reg_rows[-1]
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
                    "registration checks " + json.dumps(registration_checks, sort_keys=True)
                )

            result_envelope, result_command = build_v2_result_command(
                fingerprint,
                registration.get("providerRequestId"),
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
            result_checks = v2_success_checks(
                result,
                fingerprint,
                registration.get("providerRequestId"),
            )
            completion_checks = v49.completion_success_checks(completion, fingerprint)
            if not all(result_checks.values()):
                raise RuntimeError(
                    "v2 success checks " + json.dumps(result_checks, sort_keys=True)
                )
            if not all(completion_checks.values()):
                raise RuntimeError(
                    "completion checks " + json.dumps(completion_checks, sort_keys=True)
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
            replay_checks = v2_replay_checks(
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
            checks["result_request_id_exact"] = (
                result_envelope.get("providerRequestId")
                == registration.get("providerRequestId")
                == expected_request_id
            )
            checks["provider_invocation_rows_empty"] = (
                len(h.read_jsonl(PROVIDER_INVOCATIONS)) == 0
            )
            checks["no_apply_side_effects"] = h.no_apply_side_effects()

            if not all(checks.values()):
                raise RuntimeError(
                    "provider result v2 binding live checks "
                    + json.dumps(checks, sort_keys=True)
                )

            evidence["shadow_receipt"] = row
            evidence["provider_job_claim"] = claim
            evidence["provider_request_registration"] = registration
            evidence["provider_result_v2_envelope"] = result_envelope
            evidence["provider_result_v2_admission"] = result
            evidence["dispatch_completion"] = completion
            evidence["provider_result_v2_replay"] = replay
            evidence["dispatch_completion_replay"] = replay_completion
            evidence["live_checks"] = checks
            evidence["result"] = "PASS_PROVIDER_RESULT_V2_REQUEST_BINDING_SHADOW"
            h.base.atomic_json(validation / "validation.json", evidence)

            h.safe_exit(
                OWNER,
                token,
                run,
                "v02156 provider result v2 binding verified",
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
                    "requestMatch": None
                    if row is None
                    else result.get("providerRequestMatchReason"),
                    "completion": None
                    if row is None
                    else completion.get("reason"),
                },
                indent=2,
            )
        )
        return (
            0
            if evidence["result"] == "PASS_PROVIDER_RESULT_V2_REQUEST_BINDING_SHADOW"
            else 2
        )

    except Exception as ex:
        evidence["result"] = "FAIL"
        evidence["error"] = type(ex).__name__ + ": " + str(ex)
        evidence["failed_local"] = datetime.datetime.now().astimezone().isoformat()
        h.base.atomic_json(validation / "validation.json", evidence)

        if run is not None and h.base.process_pids():
            try:
                h.safe_exit(OWNER, token, run, "v02156 provider result v2 failure")
                run = None
            except Exception as e:
                evidence["exit_cleanup_error"] = type(e).__name__ + ": " + str(e)

        if run is not None and not h.base.process_pids():
            try:
                h.base.bus.close_run("v02156 pre-ready failure")
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
