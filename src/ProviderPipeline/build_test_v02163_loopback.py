from pathlib import Path

src=Path(r"D:\BannerlordAIResearch\workspace\test_provider_transport_loopback_v02162.py")
dst=Path(r"D:\BannerlordAIResearch\workspace\test_provider_transport_loopback_v02163.py")
t=src.read_text(encoding="utf-8-sig")
t=t.replace(
    "import provider_transport_loopback_v02162 as loop",
    "import provider_transport_loopback_v02163 as loop",
    1,
)
anchor='''print(f"PASS_FIXTURES checks={checks}")
'''
extra=r'''tiny_policy = dict(policy_reg)
tiny_policy["maxResultBytes"] = "10"
tiny = loop.send_loopback_v4(
    provider_json,
    provider_reg,
    transport_json,
    transport_reg,
    tiny_policy,
)
check(not tiny.success, "tiny_limit_rejected")
check(tiny.result is None and tiny.result_json is None, "tiny_limit_no_result")
check(
    "RESULT_SIZE_LIMIT_EXCEEDED" in tiny.reasons,
    "tiny_limit_reason",
)
check(
    tiny.receipt["errorCode"] == "RESULT_SIZE_LIMIT_EXCEEDED",
    "tiny_limit_error_code",
)
check(tiny.receipt["externalNetworkUsed"] is False, "tiny_limit_network_false")
check(tiny.receipt["modelInvoked"] is False, "tiny_limit_model_false")

invalid_policy = dict(policy_reg)
invalid_policy["maxResultBytes"] = "0"
invalid = loop.send_loopback_v4(
    provider_json,
    provider_reg,
    transport_json,
    transport_reg,
    invalid_policy,
)
check(not invalid.success, "invalid_limit_rejected")
check(
    "RESULT_SIZE_POLICY_INVALID" in invalid.reasons,
    "invalid_limit_reason",
)
check(
    invalid.receipt["errorCode"] == "RESULT_SIZE_POLICY_INVALID",
    "invalid_limit_error_code",
)

print(f"PASS_FIXTURES checks={checks}")
'''
if anchor not in t:
    raise SystemExit("loopback test print anchor missing")
t=t.replace(anchor,extra,1)
dst.write_text(t,encoding="utf-8")
print(dst)
