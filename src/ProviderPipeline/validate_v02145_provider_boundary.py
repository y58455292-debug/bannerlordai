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

import validate_v02142_route_eligibility as v42

h = v42.h
CLAN_SRC = v42.CLAN_SRC
RUNNER_SRC = ROOT / r"workspace\_PatchStaging\AutonomousOperator_v002145_ProviderBoundary_20260921_1416\candidate"
CLAN_SHA = v42.CLAN_SHA
RUNNER_SHA = "00E89D468708C4614ABE36A229B89C78BE249BE4C9996C1400D2786B4ED8A088"
OWNER = "sol_deliberation_provider_boundary_v02145"
SOURCE_PREFIX = "ClanAI V02145 PROVIDER SOURCE "
FIXTURE_PROJECT = ROOT / r"workspace\_PatchStaging\AutonomousOperator_v002145_ProviderBoundary_20260921_1416\fixtures\Fixtures.csproj"
PROVIDER_RECEIPTS = ROOT / r"Automation\TestRunner\patrol_defense_deliberation_provider_invocations.jsonl"

h.CLAN_SRC = CLAN_SRC
h.RUNNER_SRC = RUNNER_SRC
h.NEW_CLAN = CLAN_SHA
h.NEW_RUNNER = RUNNER_SHA


def read_provider_receipts():
    return h.read_jsonl(PROVIDER_RECEIPTS)



def provider_success_checks(receipt, fingerprint):
    admission = receipt.get("admission")
    raw = receipt.get("rawAdvisory")
    return {
        "schema": receipt.get("schema")
        == "BannerlordAI.PatrolDefenseDeliberationProviderInvocation.v1",
        "mode": receipt.get("mode") == "observe",
        "provider_id": receipt.get("providerId") == "deterministic_local_mock",
        "provider_invoked": receipt.get("providerInvoked") is True,
        "fingerprint": receipt.get("requestFingerprint") == fingerprint,
        "raw_schema": isinstance(raw, dict)
        and raw.get("schema")
        == "BannerlordAI.PatrolDefenseDeliberationAdvisory.v1",
        "raw_fingerprint": isinstance(raw, dict)
        and raw.get("requestFingerprint") == fingerprint,
        "raw_disposition": isinstance(raw, dict)
        and raw.get("disposition") == "KEEP_BASELINE",
        "admission_present": isinstance(admission, dict),
        "admitted": isinstance(admission, dict)
        and admission.get("admitted") is True,
        "admission_disposition": isinstance(admission, dict)
        and admission.get("disposition") == "KEEP_BASELINE",
        "admission_execution_false": isinstance(admission, dict)
        and admission.get("executionAuthorized") is False,
        "external_network_false": receipt.get("externalNetworkUsed") is False,
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


def provider_replay_checks(receipt, fingerprint):
    admission = receipt.get("admission")
    reasons = admission.get("rejectionReasons") if isinstance(admission, dict) else None
    return {
        "schema": receipt.get("schema")
        == "BannerlordAI.PatrolDefenseDeliberationProviderInvocation.v1",
        "provider_id": receipt.get("providerId") == "deterministic_local_mock",
        "provider_not_invoked": receipt.get("providerInvoked") is False,
        "fingerprint": receipt.get("requestFingerprint") == fingerprint,
        "raw_null": receipt.get("rawAdvisory") is None,
        "admission_present": isinstance(admission, dict),
        "admitted_false": isinstance(admission, dict)
        and admission.get("admitted") is False,
        "no_pending_reason": isinstance(reasons, list)
        and "NO_PENDING_ROUTE_REQUEST" in reasons,
        "external_network_false": receipt.get("externalNetworkUsed") is False,
        "model_false": receipt.get("modelInvoked") is False,
        "llm_false": receipt.get("llmInvoked") is False,
        "planner_false": receipt.get("plannerInvoked") is False,
        "execution_false": receipt.get("executionAuthorized") is False,
        "behavior_false": receipt.get("behaviorMutation") is False,
        "intent_false": receipt.get("intentMutation") is False,
        "score_false": receipt.get("scoreMutation") is False,
        "movement_zero": receipt.get("nativeMovementCalls") == 0,
    }


def main():
    if h.base.process_pids():
        raise RuntimeError("Bannerlord already running")

    runner_source = (RUNNER_SRC / "SubModule.cs").read_text(
        encoding="utf-8", errors="replace"
    )
    gate_source = (RUNNER_SRC / "PatrolDefenseAdvisoryRuntimeGate.cs").read_text(
        encoding="utf-8", errors="replace"
    )
    admission_source = (
        RUNNER_SRC / "PatrolDefenseDeliberationAdvisoryAdmission.cs"
    ).read_text(encoding="utf-8", errors="replace")
    provider_source = (
        RUNNER_SRC / "PatrolDefenseDeliberationProvider.cs"
    ).read_text(encoding="utf-8", errors="replace")

    static_checks = {
        "provider_interface": "IPatrolDefenseDeliberationProvider" in provider_source,
        "mock_provider": "PatrolDefenseMockDeliberationProvider" in provider_source,
        "provider_runtime": "PatrolDefenseDeliberationProviderRuntime" in provider_source,
        "pending_gate": "_patrolDefenseAdvisoryRuntimeGate" in runner_source,
        "provider_command": "PATROL_DEFENSE_PROVIDER_MOCK_RUN " in runner_source,
        "provider_path": "patrol_defense_deliberation_provider_invocations.jsonl"
        in runner_source,
        "pending_request_json": "_pendingPatrolDefenseDeliberationRequestJson"
        in runner_source,
        "strict_admission_via_runtime_gate":
        "admissionGate.Evaluate" in provider_source
        and "PatrolDefenseDeliberationAdvisoryAdmission.Evaluate"
        in gate_source,
        "no_pending_reason": "NO_PENDING_ROUTE_REQUEST" in gate_source,
        "execution_false": 'executionAuthorized\\":false' in provider_source,
        "network_false": 'externalNetworkUsed\\":false' in provider_source,
        "model_false": 'modelInvoked\\":false' in provider_source,
        "no_http": "HttpClient" not in provider_source
        and "HttpClient" not in runner_source,
        "no_openai": "OpenAI" not in provider_source
        and "OpenAI" not in runner_source,
        "no_chat_completion": "ChatCompletion" not in provider_source
        and "ChatCompletion" not in runner_source,
        "no_apply": "PatrolDefenseApplyGate" not in provider_source,
        "no_score": "SetBehaviorScore" not in provider_source,
        "no_memory_write": "RecordPatrolDefenseEpisode" not in provider_source,
    }
    if not all(static_checks.values()):
        raise RuntimeError("static checks " + json.dumps(static_checks, sort_keys=True))

    fixture = subprocess.run(
        ["dotnet", "run", "--project", str(FIXTURE_PROJECT), "-c", "Release"],
        capture_output=True,
        text=True,
    )
    fixture_result = (fixture.stdout or "").strip()
    if fixture.returncode != 0 or "PASS_FIXTURES checks=97" not in fixture_result:
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
    if h.sha(source) != h.SAVE_HASH:
        raise RuntimeError("source clone mismatch")

    validation = (
        ROOT
        / "Longitudinal/LiveValidation"
        / ("PatrolDefense_v02145_ProviderBoundary_" + stamp)
    )
    validation.mkdir(parents=True)
    backup = ROOT / "workspace/_PatchBackups" / ("Before_ProviderBoundary_" + stamp)
    backup.mkdir(parents=True)

    operation = "deliberation-provider-boundary-v02145-" + stamp
    token = secrets.token_hex(32)
    token_hash = hashlib.sha256(token.encode()).hexdigest().upper()

    evidence = {
        "schema": "BannerlordAI.DeliberationProviderBoundaryValidation.v1",
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
            "patrol_defense_deliberation_provider_boundary_shadow",
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
        if PROVIDER_RECEIPTS.exists():
            PROVIDER_RECEIPTS.unlink()

        run = h.base.bus.start_run(
            issuer="deliberation_provider_boundary_live",
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

        row, request, route, last_hours = v42.wait_route(start_hours, 180, 120.0)
        evidence["campaign_start_hours"] = start_hours
        evidence["campaign_end_hours"] = last_hours

        if row is None:
            evidence["result"] = "ENVIRONMENTAL_INCONCLUSIVE"
            evidence["reason"] = (
                "No natural route-eligible deliberation request within bounded window."
            )
            h.base.atomic_json(validation / "validation.json", evidence)
            h.safe_exit(
                OWNER,
                token,
                run,
                "v02145 provider boundary environmental inconclusive",
            )
            run = None
        else:
            route_checks = v42.validate_route(row, request, route)
            if not all(route_checks.values()):
                raise RuntimeError(
                    "route checks " + json.dumps(route_checks, sort_keys=True)
                )

            fingerprint = request.get("requestFingerprint")
            command = (
                "PATROL_DEFENSE_PROVIDER_MOCK_RUN "
                + fingerprint
                + " KEEP_BASELINE"
            )

            before = len(read_provider_receipts())
            h.send_wait(
                OWNER,
                run,
                command,
                "patrol_defense_provider_admitted",
                10,
            )
            rows = read_provider_receipts()
            if len(rows) <= before:
                raise RuntimeError("provider invocation receipt missing")
            provider_receipt = rows[-1]
            provider_checks = provider_success_checks(
                provider_receipt,
                fingerprint,
            )
            if not all(provider_checks.values()):
                raise RuntimeError(
                    "provider checks "
                    + json.dumps(provider_checks, sort_keys=True)
                )

            before_replay = len(rows)
            h.send_wait(
                OWNER,
                run,
                command,
                "patrol_defense_provider_rejected:NO_PENDING_ROUTE_REQUEST",
                10,
            )
            rows2 = read_provider_receipts()
            if len(rows2) <= before_replay:
                raise RuntimeError("provider replay receipt missing")
            replay_receipt = rows2[-1]
            replay_checks_result = provider_replay_checks(
                replay_receipt,
                fingerprint,
            )
            if not all(replay_checks_result.values()):
                raise RuntimeError(
                    "provider replay checks "
                    + json.dumps(replay_checks_result, sort_keys=True)
                )

            checks = {}
            checks.update({"route_" + k: v for k, v in route_checks.items()})
            checks.update(
                {"provider_" + k: v for k, v in provider_checks.items()}
            )
            checks.update(
                {"replay_" + k: v for k, v in replay_checks_result.items()}
            )
            checks["no_apply_side_effects"] = h.no_apply_side_effects()

            if not all(checks.values()):
                raise RuntimeError(
                    "provider boundary live checks "
                    + json.dumps(checks, sort_keys=True)
                )

            evidence["shadow_receipt"] = row
            evidence["deliberation_request"] = request
            evidence["route_eligibility"] = route
            evidence["provider_invocation"] = provider_receipt
            evidence["provider_replay_invocation"] = replay_receipt
            evidence["live_checks"] = checks
            evidence["result"] = "PASS_DELIBERATION_PROVIDER_BOUNDARY_SHADOW"
            h.base.atomic_json(validation / "validation.json", evidence)

            h.safe_exit(
                OWNER,
                token,
                run,
                "v02145 provider boundary verified",
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
                    "providerInvoked": None
                    if row is None
                    else provider_receipt.get("providerInvoked"),
                    "admitted": None
                    if row is None
                    else (provider_receipt.get("admission") or {}).get("admitted"),
                    "replayProviderInvoked": None
                    if row is None
                    else replay_receipt.get("providerInvoked"),
                },
                indent=2,
            )
        )
        return (
            0
            if evidence["result"] == "PASS_DELIBERATION_PROVIDER_BOUNDARY_SHADOW"
            else 2
        )

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
                    "v02145 provider boundary failure",
                )
                run = None
            except Exception as e:
                evidence["exit_cleanup_error"] = type(e).__name__ + ": " + str(e)

        if run is not None and not h.base.process_pids():
            try:
                h.base.bus.close_run("v02145 pre-ready failure")
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
