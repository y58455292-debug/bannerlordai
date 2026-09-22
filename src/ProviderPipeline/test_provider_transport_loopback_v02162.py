import hashlib
import json
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).parent))
import provider_request_contract_v02154 as req
import provider_transport_loopback_v02162 as loop

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

out = loop.send_loopback_v4(
    provider_json,
    provider_reg,
    transport_json,
    transport_reg,
    policy_reg,
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

bad_policy = dict(policy_reg)
bad_policy["providerRequestId"] = "B" * 64
bad = loop.send_loopback_v4(
    provider_json, provider_reg, transport_json, transport_reg, bad_policy
)
check(not bad.success, "bad_policy_rejected")
check("POLICY_PROVIDER_REQUEST_MISMATCH" in bad.reasons, "bad_policy_reason")
check(bad.result is None, "bad_policy_no_result")
check(bad.receipt["success"] is False, "bad_policy_receipt_fail")

missing_policy = loop.send_loopback_v4(
    provider_json, provider_reg, transport_json, transport_reg, {}
)
check(not missing_policy.success, "missing_policy_reject")
check(
    any(r.startswith("EXECUTION_POLICY_") for r in missing_policy.reasons),
    "missing_policy_reason",
)

tampered_transport = dict(transport_reg)
tampered_transport["transportRequestId"] = "C" * 64
bad_transport = loop.send_loopback_v4(
    provider_json, provider_reg, transport_json, tampered_transport, policy_reg
)
check(not bad_transport.success, "bad_transport_reject")
check("TRANSPORT_REQUEST_ID_MISMATCH" in bad_transport.reasons, "bad_transport_reason")

print(f"PASS_FIXTURES checks={checks}")
