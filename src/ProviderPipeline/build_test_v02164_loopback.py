from pathlib import Path

src=Path(r"D:\BannerlordAIResearch\workspace\test_provider_transport_loopback_v02163.py")
dst=Path(r"D:\BannerlordAIResearch\workspace\test_provider_transport_loopback_v02164.py")
t=src.read_text(encoding="utf-8-sig")
t=t.replace(
    "import provider_transport_loopback_v02163 as loop",
    "import provider_transport_loopback_v02164 as loop",
    1,
)

anchor='''policy_reg = {
    "schema": "BannerlordAI.PatrolDefenseProviderExecutionPolicyRegistration.v1",
    "mode": "observe",
    "executionPolicyId": policy_id,
    "transportRequestId": transport_id,
    "providerRequestId": provider_id,
    "providerId": provider["providerId"],
    "modelId": provider["modelId"],
    "attemptId": provider["attemptId"],
    "requestFingerprint": fp,
    "timeoutMs": "1000",
    "maxAttempts": "1",
    "maxResultBytes": "65536",
    "credentialRef": "env:BANNERLORDAI_TEST_PROVIDER_KEY",
    "registered": True,
    "idempotent": False,
    "reason": "REGISTERED",
    "queueCount": 1,
}
'''
insert=anchor+'''
authorization = {
    "schema": "BannerlordAI.PatrolDefenseProviderTransportExecutionAuthorization.v1",
    "mode": "observe",
    "transportExecutionAuthorizationId": "E" * 64,
    "requestFingerprint": fp,
    "executionPolicyId": policy_id,
    "attemptOrdinal": 1,
    "attemptCount": 1,
    "maxAttempts": 1,
    "authorized": True,
    "reason": "EXECUTION_ATTEMPT_AUTHORIZED",
    "externalNetworkUsed": False,
    "modelInvoked": False,
    "llmInvoked": False,
    "plannerInvoked": False,
    "executionAuthorized": False,
    "interpretationApplied": False,
    "behaviorMutation": False,
    "intentMutation": False,
    "scoreMutation": False,
    "nativeMovementCalls": 0,
}
'''
if "transportExecutionAuthorizationId" not in t:
    if anchor not in t:
        raise SystemExit("policy reg anchor missing")
    t=t.replace(anchor,insert,1)

repls={
'''out = loop.send_loopback_v4(
    provider_json,
    provider_reg,
    transport_json,
    transport_reg,
    policy_reg,
)
''':
'''out = loop.send_loopback_v4(
    provider_json,
    provider_reg,
    transport_json,
    transport_reg,
    policy_reg,
    authorization,
)
''',
'''bad = loop.send_loopback_v4(
    provider_json, provider_reg, transport_json, transport_reg, bad_policy
)
''':
'''bad = loop.send_loopback_v4(
    provider_json, provider_reg, transport_json, transport_reg, bad_policy, authorization
)
''',
'''missing_policy = loop.send_loopback_v4(
    provider_json, provider_reg, transport_json, transport_reg, {}
)
''':
'''missing_policy = loop.send_loopback_v4(
    provider_json, provider_reg, transport_json, transport_reg, {}, authorization
)
''',
'''bad_transport = loop.send_loopback_v4(
    provider_json, provider_reg, transport_json, tampered_transport, policy_reg
)
''':
'''bad_transport = loop.send_loopback_v4(
    provider_json, provider_reg, transport_json, tampered_transport, policy_reg, authorization
)
''',
'''tiny = loop.send_loopback_v4(
    provider_json,
    provider_reg,
    transport_json,
    transport_reg,
    tiny_policy,
)
''':
'''tiny = loop.send_loopback_v4(
    provider_json,
    provider_reg,
    transport_json,
    transport_reg,
    tiny_policy,
    authorization,
)
''',
'''invalid = loop.send_loopback_v4(
    provider_json,
    provider_reg,
    transport_json,
    transport_reg,
    invalid_policy,
)
''':
'''invalid = loop.send_loopback_v4(
    provider_json,
    provider_reg,
    transport_json,
    transport_reg,
    invalid_policy,
    authorization,
)
''',
}
for old,new in repls.items():
    if old not in t:
        raise SystemExit("call replacement anchor missing: "+old[:40])
    t=t.replace(old,new,1)

anchor='''print(f"PASS_FIXTURES checks={checks}")
'''
extra=r'''missing_auth = loop.send_loopback_v4(
    provider_json,
    provider_reg,
    transport_json,
    transport_reg,
    policy_reg,
    None,
)
check(not missing_auth.success, "missing_auth_rejected")
check("EXECUTION_AUTHORIZATION_MISSING" in missing_auth.reasons, "missing_auth_reason")

unauthorized = dict(authorization)
unauthorized["authorized"] = False
unauthorized["reason"] = "EXECUTION_ATTEMPT_LIMIT_EXCEEDED"
bad_auth = loop.send_loopback_v4(
    provider_json,
    provider_reg,
    transport_json,
    transport_reg,
    policy_reg,
    unauthorized,
)
check(not bad_auth.success, "unauthorized_rejected")
check("EXECUTION_AUTHORIZATION_NOT_AUTHORIZED" in bad_auth.reasons, "unauthorized_flag_reason")

wrong_auth_policy = dict(authorization)
wrong_auth_policy["executionPolicyId"] = "F" * 64
wrong_policy_auth = loop.send_loopback_v4(
    provider_json,
    provider_reg,
    transport_json,
    transport_reg,
    policy_reg,
    wrong_auth_policy,
)
check(not wrong_policy_auth.success, "wrong_auth_policy_rejected")
check("EXECUTION_AUTHORIZATION_POLICY_MISMATCH" in wrong_policy_auth.reasons, "wrong_auth_policy_reason")

wrong_auth_fp = dict(authorization)
wrong_auth_fp["requestFingerprint"] = "B" * 64
wrong_fp_auth = loop.send_loopback_v4(
    provider_json,
    provider_reg,
    transport_json,
    transport_reg,
    policy_reg,
    wrong_auth_fp,
)
check(not wrong_fp_auth.success, "wrong_auth_fp_rejected")
check("EXECUTION_AUTHORIZATION_FINGERPRINT_MISMATCH" in wrong_fp_auth.reasons, "wrong_auth_fp_reason")

bad_ordinal = dict(authorization)
bad_ordinal["attemptOrdinal"] = 2
bad_ordinal["attemptCount"] = 2
bad_attempt_auth = loop.send_loopback_v4(
    provider_json,
    provider_reg,
    transport_json,
    transport_reg,
    policy_reg,
    bad_ordinal,
)
check(not bad_attempt_auth.success, "bad_auth_attempt_rejected")
check("EXECUTION_AUTHORIZATION_ATTEMPT_LIMIT_MISMATCH" in bad_attempt_auth.reasons, "bad_auth_attempt_reason")

print(f"PASS_FIXTURES checks={checks}")
'''
if "missing_auth_rejected" not in t:
    if anchor not in t:
        raise SystemExit("print anchor missing")
    t=t.replace(anchor,extra,1)

dst.write_text(t,encoding="utf-8")
print(dst)
