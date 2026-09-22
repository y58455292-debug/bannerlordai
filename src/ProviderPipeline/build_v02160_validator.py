from pathlib import Path

src=Path(r"D:\BannerlordAIResearch\workspace\validate_v02159_transport_bound_result_v3.py")
out=Path(r"D:\BannerlordAIResearch\workspace\validate_v02160_transport_receipt_result_hash.py")
t=src.read_text(encoding="utf-8-sig")

repls={
r'RUNNER_SRC = ROOT / r"workspace\_PatchStaging\AutonomousOperator_v002159_TransportBoundResultV3_20260921_1658\candidate"':
r'RUNNER_SRC = ROOT / r"workspace\_PatchStaging\AutonomousOperator_v002160_TransportReceiptResultHash_20260921_1948\candidate"',
'RUNNER_SHA = "3445F0843F2CB739641CF5448E011D97CC385DD85C2B0FAC80B912473ED5EAED"':
'RUNNER_SHA = "629C5A24D09CF25A5EB9D0625546B580E0221595D67B158B2464FE16642177FE"',
'OWNER = "sol_transport_bound_result_v3_v02159"':
'OWNER = "sol_transport_receipt_result_hash_v02160"',
'SOURCE_PREFIX = "ClanAI V02159 RESULT V3 SOURCE "':
'SOURCE_PREFIX = "ClanAI V02160 RECEIPT HASH SOURCE "',
r'BACKCOMPAT_FIXTURES = ROOT / r"workspace\_PatchStaging\AutonomousOperator_v002159_TransportBoundResultV3_20260921_1658\fixtures\Fixtures.csproj"':
r'BACKCOMPAT_FIXTURES = ROOT / r"workspace\_PatchStaging\AutonomousOperator_v002160_TransportReceiptResultHash_20260921_1948\fixtures\Fixtures.csproj"',
r'V3_FIXTURES = ROOT / r"workspace\_PatchStaging\AutonomousOperator_v002159_TransportBoundResultV3_20260921_1658\fixtures_v3\FixturesV3.csproj"':
r'V3_FIXTURES = ROOT / r"workspace\_PatchStaging\AutonomousOperator_v002160_TransportReceiptResultHash_20260921_1948\fixtures_v3\FixturesV3.csproj"',
}
for a,b in repls.items():
    if a not in t:
        raise SystemExit("missing replacement: "+a)
    t=t.replace(a,b)

# Add binding path constant.
anchor='RESULT_V3 = ROOT / r"Automation\\TestRunner\\patrol_defense_provider_result_v3_admissions.jsonl"\n'
if anchor not in t:
    raise SystemExit("result path anchor missing")
t=t.replace(
    anchor,
    anchor+'BINDINGS = ROOT / r"Automation\\TestRunner\\patrol_defense_provider_transport_result_bindings.jsonl"\n',
    1,
)

# Replace/add binding check helper before main.
main_anchor="\ndef main():\n"
if main_anchor not in t:
    raise SystemExit("main anchor missing")
helpers=r'''
def binding_success_checks(row, transport_receipt, result_json, result):
    nested = row.get("providerResultAdmission")
    expected_sha = hashlib.sha256(result_json.encode("utf-8")).hexdigest().upper()
    checks = {
        "schema": row.get("schema")
        == "BannerlordAI.PatrolDefenseProviderTransportReceiptResultBinding.v1",
        "mode": row.get("mode") == "observe",
        "transport": row.get("transportId") == transport_receipt.get("transportId"),
        "transport_request_id": row.get("transportRequestId")
        == transport_receipt.get("transportRequestId")
        == result.get("transportRequestId"),
        "provider_request_id": row.get("providerRequestId")
        == transport_receipt.get("providerRequestId")
        == result.get("providerRequestId"),
        "provider": row.get("providerId") == transport_receipt.get("providerId"),
        "model": row.get("modelId") == transport_receipt.get("modelId"),
        "attempt": row.get("attemptId") == transport_receipt.get("attemptId"),
        "fingerprint": row.get("requestFingerprint")
        == transport_receipt.get("requestFingerprint"),
        "request_input_sha": row.get("requestInputSha256")
        == transport_receipt.get("requestInputSha256"),
        "status": row.get("resultStatus") == transport_receipt.get("resultStatus")
        == result.get("status"),
        "receipt_sha": row.get("resultSha256") == expected_sha,
        "computed_sha": row.get("computedResultSha256") == expected_sha,
        "sha_equal": row.get("resultSha256") == row.get("computedResultSha256"),
        "receipt_match": row.get("transportReceiptMatchReason")
        == "TRANSPORT_RECEIPT_MATCHED",
        "binding_accepted": row.get("bindingAccepted") is True,
        "no_rejections": row.get("rejectionReasons") == [],
        "network_false": row.get("externalNetworkUsed") is False,
        "model_false": row.get("modelInvoked") is False,
        "receipt_success": row.get("receiptSuccess") is True,
        "error_null": row.get("errorCode") is None,
        "nested_present": isinstance(nested, dict),
        "execution_false": row.get("executionAuthorized") is False,
        "behavior_false": row.get("behaviorMutation") is False,
        "score_false": row.get("scoreMutation") is False,
        "movement_zero": row.get("nativeMovementCalls") == 0,
    }
    if isinstance(nested, dict):
        nested_checks = result_v3_checks(nested, result, transport_receipt)
        checks.update({"nested_" + k: v for k, v in nested_checks.items()})
    return checks


def binding_replay_checks(row, fingerprint):
    reasons = row.get("rejectionReasons")
    return {
        "schema": row.get("schema")
        == "BannerlordAI.PatrolDefenseProviderTransportReceiptResultBinding.v1",
        "fingerprint": row.get("requestFingerprint") == fingerprint,
        "binding_false": row.get("bindingAccepted") is False,
        "match_job_missing": row.get("transportReceiptMatchReason")
        == "DISPATCH_JOB_NOT_FOUND",
        "job_missing_reason": isinstance(reasons, list)
        and "DISPATCH_JOB_NOT_FOUND" in reasons,
        "nested_null": row.get("providerResultAdmission") is None,
        "execution_false": row.get("executionAuthorized") is False,
        "movement_zero": row.get("nativeMovementCalls") == 0,
    }

'''
t=t.replace(main_anchor,"\n"+helpers+main_anchor,1)

# Static setup: add binding source and checks.
source_anchor='''    v3_source = (RUNNER_SRC / "PatrolDefenseProviderResultV3Admission.cs").read_text(
        encoding="utf-8", errors="replace"
    )
'''
source_repl=source_anchor+'''    binding_source = (
        RUNNER_SRC / "PatrolDefenseProviderTransportReceiptResultBinding.cs"
    ).read_text(encoding="utf-8", errors="replace")
'''
if source_anchor not in t:
    raise SystemExit("source anchor missing")
t=t.replace(source_anchor,source_repl,1)

static_anchor='''        "v3_command": "PATROL_DEFENSE_PROVIDER_RESULT_V3_ADMIT " in runner_source,
'''
static_repl=static_anchor+'''        "binding_command": "PATROL_DEFENSE_PROVIDER_TRANSPORT_RESULT_ADMIT "
        in runner_source,
        "binding_path": "patrol_defense_provider_transport_result_bindings.jsonl"
        in runner_source,
        "binding_schema":
        "BannerlordAI.PatrolDefenseProviderTransportReceiptResultBinding.v1"
        in binding_source,
        "receipt_schema_v2":
        "BannerlordAI.PatrolDefenseProviderTransportReceipt.v2"
        in binding_source,
        "result_hash": "Sha256Upper(resultBytes)" in binding_source,
        "receipt_match_method": "MatchRegisteredTransportReceipt("
        in dispatch_source,
        "receipt_match_reason": "TRANSPORT_RECEIPT_MATCHED"
        in dispatch_source,
'''
if static_anchor not in t:
    raise SystemExit("static anchor missing")
t=t.replace(static_anchor,static_repl,1)

# Update v3 fixture expected count.
t=t.replace(
    '"PASS_V3_FIXTURES checks=40"',
    '"PASS_V3_FIXTURES checks=63"',
    1,
)

# Validation path/schema/operation/owner strings.
for a,b in {
    '"PatrolDefense_v02159_TransportBoundProviderResultV3_"':
    '"PatrolDefense_v02160_TransportReceiptResultHash_"',
    '"Before_TransportBoundProviderResultV3_"':
    '"Before_TransportReceiptResultHash_"',
    '"transport-bound-provider-result-v3-v02159-"':
    '"transport-receipt-result-hash-v02160-"',
    '"BannerlordAI.TransportBoundProviderResultV3Validation.v1"':
    '"BannerlordAI.TransportReceiptResultHashValidation.v1"',
    '"patrol_defense_transport_bound_result_v3_shadow"':
    '"patrol_defense_transport_receipt_result_hash_shadow"',
    'issuer="transport_bound_result_v3_live"':
    'issuer="transport_receipt_result_hash_live"',
}.items():
    if a not in t:
        raise SystemExit("missing live metadata replacement "+a)
    t=t.replace(a,b)

# Clear binding file at runtime.
clear_anchor='''            RESULT_V3,
            COMPLETIONS,
'''
if clear_anchor not in t:
    raise SystemExit("clear anchor missing")
t=t.replace(
    clear_anchor,
    '''            RESULT_V3,
            BINDINGS,
            COMPLETIONS,
''',
    1,
)

# Replace admission through replay block.
start=t.index('            result_command = (')
end=t.index('            checks = {}', start)
new_live=r'''            binding_command = (
                "PATROL_DEFENSE_PROVIDER_TRANSPORT_RESULT_ADMIT "
                + base64.b64encode(outcome.receipt_json.encode("utf-8")).decode("ascii")
                + " "
                + base64.b64encode(result_json.encode("utf-8")).decode("ascii")
            )

            before_binding = len(h.read_jsonl(BINDINGS))
            before_result = len(h.read_jsonl(RESULT_V3))
            before_completion = len(h.read_jsonl(COMPLETIONS))
            h.send_wait(
                OWNER,
                run,
                binding_command,
                "patrol_defense_provider_transport_result_admitted",
                10,
            )
            binding_rows = h.read_jsonl(BINDINGS)
            result_rows = h.read_jsonl(RESULT_V3)
            completion_rows = h.read_jsonl(COMPLETIONS)
            if len(binding_rows) <= before_binding:
                raise RuntimeError("transport/result binding receipt missing")
            if len(result_rows) <= before_result:
                raise RuntimeError("nested v3 admission receipt missing")
            if len(completion_rows) <= before_completion:
                raise RuntimeError("completion receipt missing")

            binding = binding_rows[-1]
            admitted = result_rows[-1]
            completion = completion_rows[-1]

            binding_checks = binding_success_checks(
                binding,
                transport_receipt,
                result_json,
                result,
            )
            admitted_checks = result_v3_checks(
                admitted,
                result,
                transport_receipt,
            )
            completion_checks = v49.completion_success_checks(
                completion,
                fingerprint,
            )
            if not all(binding_checks.values()):
                raise RuntimeError(
                    "binding checks "
                    + json.dumps(binding_checks, sort_keys=True)
                )
            if not all(admitted_checks.values()):
                raise RuntimeError(
                    "v3 admission checks "
                    + json.dumps(admitted_checks, sort_keys=True)
                )
            if not all(completion_checks.values()):
                raise RuntimeError(
                    "completion checks "
                    + json.dumps(completion_checks, sort_keys=True)
                )

            before_replay_binding = len(binding_rows)
            before_replay_result = len(result_rows)
            before_replay_completion = len(completion_rows)
            h.send_wait(
                OWNER,
                run,
                binding_command,
                "patrol_defense_provider_transport_result_rejected:DISPATCH_JOB_NOT_FOUND",
                10,
            )
            replay_binding_rows = h.read_jsonl(BINDINGS)
            replay_result_rows = h.read_jsonl(RESULT_V3)
            replay_completion_rows = h.read_jsonl(COMPLETIONS)
            if len(replay_binding_rows) <= before_replay_binding:
                raise RuntimeError("binding replay receipt missing")
            if len(replay_result_rows) != before_replay_result:
                raise RuntimeError("replay unexpectedly produced nested v3 admission")
            if len(replay_completion_rows) <= before_replay_completion:
                raise RuntimeError("binding replay completion missing")

            replay_binding = replay_binding_rows[-1]
            replay_completion = replay_completion_rows[-1]
            replay_binding_checks = binding_replay_checks(
                replay_binding,
                fingerprint,
            )
            replay_completion_checks = v49.completion_replay_checks(
                replay_completion,
                fingerprint,
            )
            if not all(replay_binding_checks.values()):
                raise RuntimeError(
                    "binding replay checks "
                    + json.dumps(replay_binding_checks, sort_keys=True)
                )
            if not all(replay_completion_checks.values()):
                raise RuntimeError(
                    "binding replay completion checks "
                    + json.dumps(replay_completion_checks, sort_keys=True)
                )

'''
t=t[:start]+new_live+t[end:]

# Replace checks block references replay.
old_checks='''            checks.update({"result_" + k: v for k, v in admitted_checks.items()})
            checks.update({"completion_" + k: v for k, v in completion_checks.items()})
            checks.update({"replay_" + k: v for k, v in replay_checks.items()})
            checks.update({
                "replay_completion_" + k: v
                for k, v in replay_completion_checks.items()
            })
'''
new_checks='''            checks.update({"binding_" + k: v for k, v in binding_checks.items()})
            checks.update({"result_" + k: v for k, v in admitted_checks.items()})
            checks.update({"completion_" + k: v for k, v in completion_checks.items()})
            checks.update({
                "replay_binding_" + k: v
                for k, v in replay_binding_checks.items()
            })
            checks.update({
                "replay_completion_" + k: v
                for k, v in replay_completion_checks.items()
            })
'''
if old_checks not in t:
    raise SystemExit("checks block anchor missing")
t=t.replace(old_checks,new_checks,1)

# Evidence fields/result.
old_evidence='''            evidence["provider_result_v3_admission"] = admitted
            evidence["dispatch_completion"] = completion
            evidence["provider_result_v3_replay"] = replay
            evidence["dispatch_completion_replay"] = replay_completion
            evidence["live_checks"] = checks
            evidence["result"] = "PASS_TRANSPORT_BOUND_PROVIDER_RESULT_V3_SHADOW"
'''
new_evidence='''            evidence["transport_result_binding"] = binding
            evidence["provider_result_v3_admission"] = admitted
            evidence["dispatch_completion"] = completion
            evidence["transport_result_binding_replay"] = replay_binding
            evidence["dispatch_completion_replay"] = replay_completion
            evidence["live_checks"] = checks
            evidence["result"] = "PASS_TRANSPORT_RECEIPT_RESULT_HASH_BINDING_SHADOW"
'''
if old_evidence not in t:
    raise SystemExit("evidence block anchor missing")
t=t.replace(old_evidence,new_evidence,1)

t=t.replace(
    '"v02159 transport-bound provider result v3 verified"',
    '"v02160 transport receipt/result hash binding verified"',
    1,
)
t=t.replace(
    '"v02159 transport-bound v3 environmental inconclusive"',
    '"v02160 receipt/result hash environmental inconclusive"',
    1,
)
t=t.replace(
    '"v02159 transport-bound v3 failure"',
    '"v02160 receipt/result hash failure"',
    1,
)
t=t.replace(
    '"v02159 pre-ready failure"',
    '"v02160 pre-ready failure"',
    1,
)
t=t.replace(
    '== "PASS_TRANSPORT_BOUND_PROVIDER_RESULT_V3_SHADOW"',
    '== "PASS_TRANSPORT_RECEIPT_RESULT_HASH_BINDING_SHADOW"',
    1,
)

out.write_text(t,encoding="utf-8")
print(out)
