import hashlib
import json
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).parent))
import provider_request_contract_v02154 as req
import provider_transport_loopback_v02165 as loop

checks = 0

def check(v, name):
    global checks
    checks += 1
    if not v:
        raise AssertionError(name)

fp = "A" * 64
request_raw = (
    '{"schema":"BannerlordAI.PatrolDefenseDeliberationRequest.v1",'
    '"requestFingerprint":"' + fp + '",'
    '"baselineDecision":{"candidateAction":"CONTINUE_PATROL"}}'
)
job_line = (
    '{"schema":"BannerlordAI.PatrolDefenseProviderDispatchJob.v1",'
    '"sequence":1,"requestFingerprint":"' + fp + '","state":"PENDING",'
    '"enqueuedUtcTicks":1,"deliberationRequest":' + request_raw + '}'
)
claim = {
    "schema": "BannerlordAI.PatrolDefenseProviderJobClaim.v1",
    "mode": "observe",
    "requestFingerprint": fp,
    "providerId": "deterministic_external_worker",
    "attemptId": "attempt-1",
    "claimApplied": True,
    "idempotent": False,
    "reason": "CLAIMED",
    "stateBefore": "PENDING",
    "stateAfter": "CLAIMED",
    "queueCount": 1,
    "claimedUtcTicks": 2,
}

provider, provider_json = req.build_provider_request(
    job_line=job_line,
    claim=claim,
    provider_id="deterministic_external_worker",
    model_id="deterministic_mock_no_model",
)
provider_id = hashlib.sha256(provider_json.encode("utf-8")).hexdigest().upper()
provider_reg = {
    "schema": "BannerlordAI.PatrolDefenseProviderRequestRegistration.v1",
    "mode": "observe",
    "providerRequestId": provider_id,
    "providerId": provider["providerId"],
    "modelId": provider["modelId"],
    "attemptId": provider["attemptId"],
    "requestFingerprint": fp,
    "promptContractVersion": provider["promptContractVersion"],
    "responseSchema": provider["responseSchema"],
    "inputSha256": provider["inputSha256"],
    "registered": True,
    "idempotent": False,
    "reason": "REGISTERED",
    "queueCount": 1,
}

transport = {
    "schema": "BannerlordAI.PatrolDefenseProviderTransportRequest.v1",
    "transportId": "deterministic_local_loopback",
    "providerRequestId": provider_id,
    "providerId": provider["providerId"],
    "modelId": provider["modelId"],
    "attemptId": provider["attemptId"],
    "requestFingerprint": fp,
    "requestInputSha256": provider["inputSha256"],
}
transport_json = json.dumps(transport, separators=(",", ":"))
transport_id = hashlib.sha256(transport_json.encode("utf-8")).hexdigest().upper()
transport_reg = {
    "schema": "BannerlordAI.PatrolDefenseProviderTransportRegistration.v1",
    "mode": "observe",
    "transportRequestId": transport_id,
    "transportId": "deterministic_local_loopback",
    "providerRequestId": provider_id,
    "providerId": provider["providerId"],
    "modelId": provider["modelId"],
    "attemptId": provider["attemptId"],
    "requestFingerprint": fp,
    "requestInputSha256": provider["inputSha256"],
    "registered": True,
    "idempotent": False,
    "reason": "REGISTERED",
    "queueCount": 1,
}

policy_json = json.dumps(
    {
        "schema": "BannerlordAI.PatrolDefenseProviderExecutionPolicy.v1",
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
    },
    separators=(",", ":"),
)
policy_id = hashlib.sha256(policy_json.encode("utf-8")).hexdigest().upper()
policy_reg = {
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

authorization = {
    "schema": "BannerlordAI.PatrolDefenseProviderTransportExecutionAuthorization.v1",
    "mode": "observe",
    "transportExecutionAuthorizationId": "E" * 64,
    "requestFingerprint": fp,
    "executionPolicyId": policy_id,
    "attemptOrdinal": 1,
    "attemptCount": 1,
    "maxAttempts": 1,
    "timeoutMs": 1000,
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

out = loop.send_loopback_v4(
    provider_json,
    provider_reg,
    transport_json,
    transport_reg,
    policy_reg,
    authorization,
)
check(out.success, "success")
check(out.reasons == [], "no_reasons")
check(out.result["schema"] == loop.RESULT_SCHEMA, "result_schema")
check(out.receipt["schema"] == loop.RECEIPT_SCHEMA, "receipt_schema")
check(out.result["providerRequestId"] == provider_id, "result_prid")
check(out.result["transportRequestId"] == transport_id, "result_trid")
check(out.result["executionPolicyId"] == policy_id, "result_policy")
check(out.receipt["providerRequestId"] == provider_id, "receipt_prid")
check(out.receipt["transportRequestId"] == transport_id, "receipt_trid")
check(out.receipt["executionPolicyId"] == policy_id, "receipt_policy")
check(out.receipt["providerId"] == provider["providerId"], "receipt_provider")
check(out.receipt["modelId"] == provider["modelId"], "receipt_model")
check(out.receipt["attemptId"] == "attempt-1", "receipt_attempt")
check(out.receipt["requestFingerprint"] == fp, "receipt_fp")
check(out.receipt["requestInputSha256"] == provider["inputSha256"], "receipt_input_sha")
check(out.receipt["resultStatus"] == "SUCCESS", "receipt_status")
check(
    out.receipt["resultSha256"]
    == hashlib.sha256(out.result_json.encode("utf-8")).hexdigest().upper(),
    "result_hash",
)
check(out.receipt["externalNetworkUsed"] is False, "network_false")
check(out.receipt["modelInvoked"] is False, "model_false")
check(out.receipt["success"] is True, "receipt_success")
check(out.receipt["errorCode"] is None, "error_null")
check(out.result["status"] == "SUCCESS", "result_status")
check(out.result["advisoryBase64"], "advisory_present")
check(authorization["timeoutMs"] == 1000, "authorization_timeout_bound")

bad_policy = dict(policy_reg)
bad_policy["providerRequestId"] = "B" * 64
bad = loop.send_loopback_v4(
    provider_json, provider_reg, transport_json, transport_reg, bad_policy, authorization
)
check(not bad.success, "bad_policy_rejected")
check("POLICY_PROVIDER_REQUEST_MISMATCH" in bad.reasons, "bad_policy_reason")
check(bad.result is None, "bad_policy_no_result")
check(bad.receipt["success"] is False, "bad_policy_receipt_fail")

missing_policy = loop.send_loopback_v4(
    provider_json, provider_reg, transport_json, transport_reg, {}, authorization
)
check(not missing_policy.success, "missing_policy_reject")
check(
    any(r.startswith("EXECUTION_POLICY_") for r in missing_policy.reasons),
    "missing_policy_reason",
)

tampered_transport = dict(transport_reg)
tampered_transport["transportRequestId"] = "C" * 64
bad_transport = loop.send_loopback_v4(
    provider_json, provider_reg, transport_json, tampered_transport, policy_reg, authorization
)
check(not bad_transport.success, "bad_transport_reject")
check("TRANSPORT_REQUEST_ID_MISMATCH" in bad_transport.reasons, "bad_transport_reason")

tiny_policy = dict(policy_reg)
tiny_policy["maxResultBytes"] = "10"
tiny = loop.send_loopback_v4(
    provider_json,
    provider_reg,
    transport_json,
    transport_reg,
    tiny_policy,
    authorization,
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
    authorization,
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

missing_auth = loop.send_loopback_v4(
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

missing_timeout = dict(authorization)
missing_timeout.pop("timeoutMs")
missing_timeout_out = loop.send_loopback_v4(
    provider_json, provider_reg, transport_json, transport_reg, policy_reg, missing_timeout
)
check(not missing_timeout_out.success, "missing_auth_timeout_rejected")
check("EXECUTION_AUTHORIZATION_TIMEOUT_INVALID" in missing_timeout_out.reasons, "missing_auth_timeout_reason")

zero_timeout = dict(authorization)
zero_timeout["timeoutMs"] = 0
zero_timeout_out = loop.send_loopback_v4(
    provider_json, provider_reg, transport_json, transport_reg, policy_reg, zero_timeout
)
check(not zero_timeout_out.success, "zero_auth_timeout_rejected")
check("EXECUTION_AUTHORIZATION_TIMEOUT_INVALID" in zero_timeout_out.reasons, "zero_auth_timeout_reason")

wrong_timeout = dict(authorization)
wrong_timeout["timeoutMs"] = 999
wrong_timeout_out = loop.send_loopback_v4(
    provider_json, provider_reg, transport_json, transport_reg, policy_reg, wrong_timeout
)
check(not wrong_timeout_out.success, "wrong_auth_timeout_rejected")
check("EXECUTION_AUTHORIZATION_TIMEOUT_MISMATCH" in wrong_timeout_out.reasons, "wrong_auth_timeout_reason")

print(f"PASS_FIXTURES checks={checks}")
