from pathlib import Path

src=Path(r"D:\BannerlordAIResearch\workspace\test_provider_transport_loopback_v02165.py")
dst=Path(r"D:\BannerlordAIResearch\workspace\test_provider_transport_loopback_v02166.py")
t=src.read_text(encoding="utf-8-sig")
t=t.replace("provider_transport_loopback_v02165 as loop","provider_transport_loopback_v02166 as loop")
t=t.replace('"maxAttempts": "1"', '"maxAttempts": "2"')
t=t.replace('"transportExecutionAuthorizationId": "E" * 64,', '"transportExecutionAuthorizationId": loop._authorization_id(fp, policy_id, 2),')
t=t.replace('"attemptOrdinal": 1,\n    "attemptCount": 1,\n    "maxAttempts": 1,', '"attemptOrdinal": 2,\n    "attemptCount": 2,\n    "maxAttempts": 2,')
t=t.replace("send_loopback_v4(", "send_loopback_v5(")
t=t.replace('check(authorization["timeoutMs"] == 1000, "authorization_timeout_bound")',
'''check(authorization["timeoutMs"] == 1000, "authorization_timeout_bound")
check(out.result["transportExecutionAuthorizationId"] == authorization["transportExecutionAuthorizationId"], "result_auth_id")
check(out.result["transportExecutionAttemptOrdinal"] == "2", "result_auth_ordinal")
check(out.receipt["transportExecutionAuthorizationId"] == authorization["transportExecutionAuthorizationId"], "receipt_auth_id")
check(out.receipt["transportExecutionAttemptOrdinal"] == "2", "receipt_auth_ordinal")''')
t=t.replace('bad_ordinal["attemptOrdinal"] = 2\nbad_ordinal["attemptCount"] = 2',
'''bad_ordinal["attemptOrdinal"] = 3
bad_ordinal["attemptCount"] = 3''')

anchor='''wrong_auth_fp = dict(authorization)
wrong_auth_fp["requestFingerprint"] = "B" * 64
'''
extra='''tampered_auth_id = dict(authorization)
tampered_auth_id["transportExecutionAuthorizationId"] = "F" * 64
tampered_id_out = loop.send_loopback_v5(
    provider_json,
    provider_reg,
    transport_json,
    transport_reg,
    policy_reg,
    tampered_auth_id,
)
check(not tampered_id_out.success, "tampered_auth_id_rejected")
check("EXECUTION_AUTHORIZATION_ID_MISMATCH" in tampered_id_out.reasons, "tampered_auth_id_reason")

'''+anchor
if "tampered_auth_id_rejected" not in t:
    if anchor not in t: raise SystemExit("tamper anchor missing")
    t=t.replace(anchor,extra,1)

dst.write_text(t,encoding="utf-8")
print(dst)
