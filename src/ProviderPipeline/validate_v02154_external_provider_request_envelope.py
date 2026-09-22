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
RUNNER_SRC = ROOT / r"workspace\_PatchStaging\AutonomousOperator_v002152_ProviderClaimRelease_20260921_1510\candidate"
CLAN_SHA = v46.CLAN_SHA
RUNNER_SHA = "CECAFBCD366E82A50D16A803621909E3C930A8BB62A5BC432FF3B4377B0424B8"
OWNER = "sol_external_provider_request_envelope_v02154"
SOURCE_PREFIX = "ClanAI V02154 PROVIDER REQUEST SOURCE "
FIXTURE_SCRIPT = ROOT / r"workspace\test_provider_request_contract_v02154.py"
OUTBOX = v46.OUTBOX
CLAIMS = v50.CLAIMS
PROVIDER_INVOCATIONS = v46.PROVIDER_INVOCATIONS
RESULT_ADMISSIONS = ROOT / r"Automation\TestRunner\patrol_defense_provider_result_admissions.jsonl"
PROVIDER_ID = "deterministic_external_worker"
MODEL_ID = "deterministic_mock_no_model"

h.CLAN_SRC = CLAN_SRC
h.RUNNER_SRC = RUNNER_SRC
h.NEW_CLAN = CLAN_SHA
h.NEW_RUNNER = RUNNER_SHA


def independent_checks(envelope, envelope_json, job_line, claim):
    encoded = envelope.get("deliberationRequestBase64")
    decoded = base64.b64decode(encoded, validate=True)
    raw = contract.extract_deliberation_request_raw(job_line)
    parsed_request = json.loads(decoded.decode("utf-8"))
    expected_hash = hashlib.sha256(decoded).hexdigest().upper()
    job = json.loads(job_line)

    return {
        "schema": envelope.get("schema") == contract.SCHEMA,
        "provider": envelope.get("providerId") == PROVIDER_ID,
        "model": envelope.get("modelId") == MODEL_ID,
        "attempt": envelope.get("attemptId") == claim.get("attemptId") == "attempt-1",
        "fingerprint": envelope.get("requestFingerprint")
        == claim.get("requestFingerprint")
        == job.get("requestFingerprint")
        == parsed_request.get("requestFingerprint"),
        "prompt_contract": envelope.get("promptContractVersion")
        == contract.PROMPT_CONTRACT_VERSION,
        "response_schema": envelope.get("responseSchema") == contract.RESPONSE_SCHEMA,
        "exact_request_bytes": decoded == raw.encode("utf-8"),
        "input_sha256": envelope.get("inputSha256") == expected_hash,
        "input_sha256_hex64": isinstance(envelope.get("inputSha256"), str)
        and len(envelope.get("inputSha256")) == 64
        and all(ch in "0123456789ABCDEF" for ch in envelope.get("inputSha256")),
        "envelope_json_roundtrip": json.loads(envelope_json) == envelope,
    }


def main():
    if h.base.process_pids():
        raise RuntimeError("Bannerlord already running")

    if h.sha(RUNNER_SRC / "BannerlordAITestRunner.dll") != RUNNER_SHA:
        raise RuntimeError("unchanged v02152 runner drift")

    fixture = subprocess.run(
        [sys.executable, "-X", "utf8", str(FIXTURE_SCRIPT)],
        capture_output=True,
        text=True,
    )
    fixture_result = (fixture.stdout or "").strip()
    if fixture.returncode != 0 or "PASS_FIXTURES checks=49" not in fixture_result:
        raise RuntimeError(
            "provider request contract fixture gate failed "
            + fixture_result
            + " "
            + (fixture.stderr or "")
        )

    helper_source = (ROOT / r"workspace\provider_request_contract_v02154.py").read_text(
        encoding="utf-8", errors="replace"
    )
    static_checks = {
        "schema": contract.SCHEMA in helper_source,
        "response_schema": contract.RESPONSE_SCHEMA in helper_source,
        "prompt_contract": contract.PROMPT_CONTRACT_VERSION in helper_source,
        "sha256": "hashlib.sha256" in helper_source,
        "base64": "base64.b64encode" in helper_source
        and "base64.b64decode" in helper_source,
        "strict_fields": "ALLOWED_FIELDS" in helper_source,
        "no_requests": "import requests" not in helper_source,
        "no_httpx": "import httpx" not in helper_source,
        "no_urllib_request": "urllib.request" not in helper_source,
        "no_openai": "OpenAI" not in helper_source and "openai" not in helper_source.lower(),
        "no_model_call": "ChatCompletion" not in helper_source
        and ".generate_content(" not in helper_source,
    }
    if not all(static_checks.values()):
        raise RuntimeError("static checks " + json.dumps(static_checks, sort_keys=True))

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
        "PatrolDefense_v02154_ExternalProviderRequestEnvelope_" + stamp
    )
    validation.mkdir(parents=True)
    backup = ROOT / "workspace/_PatchBackups" / (
        "Before_ExternalProviderRequestEnvelope_" + stamp
    )
    backup.mkdir(parents=True)

    operation = "external-provider-request-envelope-v02154-" + stamp
    token = secrets.token_hex(32)
    token_hash = hashlib.sha256(token.encode()).hexdigest().upper()

    evidence = {
        "schema": "BannerlordAI.ExternalProviderRequestEnvelopeValidation.v1",
        "created_local": datetime.datetime.now().astimezone().isoformat(),
        "result": "IN_PROGRESS",
        "source_slot": source_slot,
        "feature_binary_reused_from": "v0.2.10.52-provider-claim-release-retry-shadow",
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
            "patrol_defense_external_provider_request_envelope_shadow",
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
        for path in (OUTBOX, CLAIMS, PROVIDER_INVOCATIONS, RESULT_ADMISSIONS):
            if path.exists():
                path.unlink()

        run = h.base.bus.start_run(
            issuer="external_provider_request_envelope_live",
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
                "v02154 provider request environmental inconclusive",
            )
            run = None
        else:
            fingerprint = request.get("requestFingerprint")
            outbox_text = OUTBOX.read_text(encoding="utf-8", errors="strict")
            raw_lines = [line for line in outbox_text.splitlines() if line.strip()]
            if len(raw_lines) != 1:
                raise RuntimeError("expected exactly one raw outbox row")
            job_line = raw_lines[0]
            job = json.loads(job_line)

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
            validation_result = contract.validate_provider_request(
                envelope_json,
                expected_job_line=job_line,
                expected_claim=claim,
            )
            independent = independent_checks(
                envelope,
                envelope_json,
                job_line,
                claim,
            )

            checks = {}
            checks.update({"claim_" + k: v for k, v in claim_checks.items()})
            checks.update({"independent_" + k: v for k, v in independent.items()})
            checks["contract_valid"] = validation_result.valid
            checks["contract_no_reasons"] = validation_result.reasons == []
            checks["outbox_request_object_exact"] = job.get("deliberationRequest") == request
            checks["provider_invocation_rows_empty"] = (
                len(h.read_jsonl(PROVIDER_INVOCATIONS)) == 0
            )
            checks["provider_result_rows_empty"] = (
                len(h.read_jsonl(RESULT_ADMISSIONS)) == 0
            )
            checks["no_apply_side_effects"] = h.no_apply_side_effects()

            if not all(checks.values()):
                raise RuntimeError(
                    "provider request envelope live checks "
                    + json.dumps(checks, sort_keys=True)
                )

            evidence["shadow_receipt"] = row
            evidence["outbox_job_raw"] = job_line
            evidence["outbox_job"] = job
            evidence["provider_job_claim"] = claim
            evidence["provider_request_envelope"] = envelope
            evidence["provider_request_envelope_json"] = envelope_json
            evidence["independent_input_sha256"] = hashlib.sha256(
                base64.b64decode(envelope["deliberationRequestBase64"], validate=True)
            ).hexdigest().upper()
            evidence["live_checks"] = checks
            evidence["result"] = "PASS_EXTERNAL_PROVIDER_REQUEST_ENVELOPE_SHADOW"
            h.base.atomic_json(validation / "validation.json", evidence)

            h.safe_exit(
                OWNER,
                token,
                run,
                "v02154 provider request envelope verified",
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
                    "inputSha256": None
                    if row is None
                    else envelope.get("inputSha256"),
                    "providerId": None
                    if row is None
                    else envelope.get("providerId"),
                    "modelId": None
                    if row is None
                    else envelope.get("modelId"),
                },
                indent=2,
            )
        )
        return 0 if evidence["result"] == "PASS_EXTERNAL_PROVIDER_REQUEST_ENVELOPE_SHADOW" else 2

    except Exception as ex:
        evidence["result"] = "FAIL"
        evidence["error"] = type(ex).__name__ + ": " + str(ex)
        evidence["failed_local"] = datetime.datetime.now().astimezone().isoformat()
        h.base.atomic_json(validation / "validation.json", evidence)

        if run is not None and h.base.process_pids():
            try:
                h.safe_exit(OWNER, token, run, "v02154 provider request failure")
                run = None
            except Exception as e:
                evidence["exit_cleanup_error"] = type(e).__name__ + ": " + str(e)

        if run is not None and not h.base.process_pids():
            try:
                h.base.bus.close_run("v02154 pre-ready failure")
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
