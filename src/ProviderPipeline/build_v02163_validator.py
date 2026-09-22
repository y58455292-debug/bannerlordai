from pathlib import Path

src=Path(r"D:\BannerlordAIResearch\workspace\validate_v02162_policy_bound_transport_result_v4.py")
out=Path(r"D:\BannerlordAIResearch\workspace\validate_v02163_result_size_policy.py")
t=src.read_text(encoding="utf-8-sig")

repls={
'import provider_transport_loopback_v02162 as loop':
'import provider_transport_loopback_v02163 as loop',
r'RUNNER_SRC = ROOT / r"workspace\_PatchStaging\AutonomousOperator_v002162_PolicyBoundResultV4_20260921_2051\candidate"':
r'RUNNER_SRC = ROOT / r"workspace\_PatchStaging\AutonomousOperator_v002163_ResultSizePolicy_20260921_2208\candidate"',
'RUNNER_SHA = "1A08ACA2F41F8F6C1675D510E829A85039CC8B17711FE2091E893429DEDFAB9E"':
'RUNNER_SHA = "AC561AB57585EAE55ED5949F246DC1D60B22ECF98B3B1B1A0FF9210339807423"',
'OWNER = "sol_policy_bound_transport_result_v4_v02162"':
'OWNER = "sol_result_size_policy_v02163"',
'SOURCE_PREFIX = "ClanAI V02162 POLICY BOUND RESULT V4 SOURCE "':
'SOURCE_PREFIX = "ClanAI V02163 RESULT SIZE POLICY SOURCE "',
r'MAIN_FIXTURES = ROOT / r"workspace\_PatchStaging\AutonomousOperator_v002162_PolicyBoundResultV4_20260921_2051\fixtures\Fixtures.csproj"':
r'MAIN_FIXTURES = ROOT / r"workspace\_PatchStaging\AutonomousOperator_v002163_ResultSizePolicy_20260921_2208\fixtures\Fixtures.csproj"',
r'V3_FIXTURES = ROOT / r"workspace\_PatchStaging\AutonomousOperator_v002162_PolicyBoundResultV4_20260921_2051\fixtures_v3\FixturesV3.csproj"':
r'V3_FIXTURES = ROOT / r"workspace\_PatchStaging\AutonomousOperator_v002163_ResultSizePolicy_20260921_2208\fixtures_v3\FixturesV3.csproj"',
r'V4_FIXTURES = ROOT / r"workspace\_PatchStaging\AutonomousOperator_v002162_PolicyBoundResultV4_20260921_2051\fixtures_v4\FixturesV4.csproj"':
r'V4_FIXTURES = ROOT / r"workspace\_PatchStaging\AutonomousOperator_v002163_ResultSizePolicy_20260921_2208\fixtures_v4\FixturesV4.csproj"',
r'LOOPBACK_FIXTURE = ROOT / r"workspace\test_provider_transport_loopback_v02162.py"':
r'LOOPBACK_FIXTURE = ROOT / r"workspace\test_provider_transport_loopback_v02163.py"',
'"PASS_V4_FIXTURES checks=41"':'"PASS_V4_FIXTURES checks=72"',
'"PASS_FIXTURES checks=31"':'"PASS_FIXTURES checks=40"',
'"PatrolDefense_v02162_PolicyBoundTransportResultV4_"':
'"PatrolDefense_v02163_ResultSizePolicy_"',
'"Before_PolicyBoundTransportResultV4_"':
'"Before_ResultSizePolicy_"',
'"policy-bound-transport-result-v4-v02162-"':
'"result-size-policy-v02163-"',
'"BannerlordAI.PolicyBoundTransportResultV4Validation.v1"':
'"BannerlordAI.ResultSizePolicyValidation.v1"',
'"patrol_defense_policy_bound_transport_result_v4_shadow"':
'"patrol_defense_result_size_policy_shadow"',
'"v02162 policy-bound v4 environmental inconclusive"':
'"v02163 result-size policy environmental inconclusive"',
'"v02162 policy-bound transport result v4 verified"':
'"v02163 result-size policy verified"',
'"v02162 policy-bound transport result v4 failure"':
'"v02163 result-size policy failure"',
'"v02162 pre-ready failure"':
'"v02163 pre-ready failure"',
'"PASS_POLICY_BOUND_TRANSPORT_RESULT_V4_SHADOW"':
'"PASS_RESULT_SIZE_POLICY_ENFORCEMENT_SHADOW"',
}
for a,b in repls.items():
    if a not in t:
        raise SystemExit("missing replacement: "+a)
    t=t.replace(a,b)

# Static checks: insert runtime size policy invariants after result_hash.
anchor='''        "result_hash": "Sha256Upper(resultBytes)" in binding_source,
'''
insert=anchor+'''        "result_size_policy_method": "EvaluateRegisteredResultSizePolicy("
        in dispatch_source,
        "result_size_policy_reasons":
        "RESULT_SIZE_WITHIN_LIMIT" in dispatch_source
        and "RESULT_SIZE_LIMIT_EXCEEDED" in dispatch_source
        and "RESULT_SIZE_POLICY_INVALID" in dispatch_source,
        "binding_size_audit":
        "resultSizePolicyReason" in binding_source
        and "resultByteCount" in binding_source
        and "maxResultBytes" in binding_source,
'''
if anchor not in t:
    raise SystemExit("static result hash anchor missing")
t=t.replace(anchor,insert,1)

# Binding success checks: insert byte policy assertions after policy_match.
anchor='''        "policy_match": row.get("executionPolicyMatchReason")
        == "EXECUTION_POLICY_MATCHED",
        "binding": row.get("bindingAccepted") is True,
'''
insert='''        "policy_match": row.get("executionPolicyMatchReason")
        == "EXECUTION_POLICY_MATCHED",
        "size_reason": row.get("resultSizePolicyReason")
        == "RESULT_SIZE_WITHIN_LIMIT",
        "size_count": row.get("resultByteCount")
        == len(result_json.encode("utf-8")),
        "size_max": row.get("maxResultBytes")
        == int(v61.TEST_MAX_RESULT_BYTES),
        "size_within": row.get("resultByteCount") <= row.get("maxResultBytes"),
        "binding": row.get("bindingAccepted") is True,
'''
if anchor not in t:
    raise SystemExit("binding check insertion anchor missing")
t=t.replace(anchor,insert,1)

# Add explicit live evidence summary fields next to transport binding.
anchor='''            evidence["transport_result_v4_binding"] = binding
            evidence["provider_result_v4_admission"] = admitted
'''
insert='''            evidence["transport_result_v4_binding"] = binding
            evidence["result_size_policy"] = {
                "reason": binding.get("resultSizePolicyReason"),
                "resultByteCount": binding.get("resultByteCount"),
                "maxResultBytes": binding.get("maxResultBytes"),
            }
            evidence["provider_result_v4_admission"] = admitted
'''
if anchor not in t:
    raise SystemExit("evidence insertion anchor missing")
t=t.replace(anchor,insert,1)

# Update printed summary to include size reason.
anchor='''            "executionPolicyId": None if row is None else expected_policy_id,
            "binding": None if row is None else binding.get("bindingAccepted"),
            "completion": None if row is None else completion.get("reason"),
'''
insert='''            "executionPolicyId": None if row is None else expected_policy_id,
            "binding": None if row is None else binding.get("bindingAccepted"),
            "sizeReason": None if row is None else binding.get("resultSizePolicyReason"),
            "resultByteCount": None if row is None else binding.get("resultByteCount"),
            "maxResultBytes": None if row is None else binding.get("maxResultBytes"),
            "completion": None if row is None else completion.get("reason"),
'''
if anchor not in t:
    raise SystemExit("print summary anchor missing")
t=t.replace(anchor,insert,1)

out.write_text(t,encoding="utf-8")
print(out)
