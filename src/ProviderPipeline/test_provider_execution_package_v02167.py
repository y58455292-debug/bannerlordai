import copy
import hashlib
import json
from pathlib import Path

import provider_request_contract_v02154 as request_contract
import provider_execution_package_v02167_clean as pkg

checks = 0

def check(value, name):
    global checks
    checks += 1
    if not value:
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
provider, provider_json = request_contract.build_provider_request(
    job_line=job_line,
    claim=claim,
    provider_id="deterministic_external_worker",
    model_id="deterministic_mock_no_model",
)
provider_id = hashlib.sha256(provider_json.encode("utf-8")).hexdigest().upper()

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

policy = {
    "schema": "BannerlordAI.PatrolDefenseProviderExecutionPolicy.v1",
    "transportRequestId": transport_id,
    "providerRequestId": provider_id,
    "providerId": provider["providerId"],
    "modelId": provider["modelId"],
    "attemptId": provider["attemptId"],
    "requestFingerprint": fp,
    "timeoutMs": "1000",
    "maxAttempts": "2",
    "maxResultBytes": "65536",
    "credentialRef": "env:BANNERLORDAI_TEST_PROVIDER_KEY",
}
policy_json = json.dumps(policy, separators=(",", ":"))
policy_id = hashlib.sha256(policy_json.encode("utf-8")).hexdigest().upper()

def auth(ordinal):
    return {
        "schema": "BannerlordAI.PatrolDefenseProviderTransportExecutionAuthorization.v1",
        "mode": "observe",
        "transportExecutionAuthorizationId": pkg._auth_id(fp, policy_id, ordinal),
        "requestFingerprint": fp,
        "executionPolicyId": policy_id,
        "attemptOrdinal": ordinal,
        "attemptCount": ordinal,
        "maxAttempts": 2,
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

auth1 = auth(1)
auth2 = auth(2)
package = pkg.build_execution_package(
    provider_json,
    transport_json,
    policy_json,
    auth2,
)
validation = pkg.validate_execution_package(package, auth2)
check(validation.valid, "exact_valid")
check(validation.reasons == [], "exact_no_reasons")
check(package["providerRequestId"] == provider_id, "provider_id")
check(package["transportRequestId"] == transport_id, "transport_id")
check(package["executionPolicyId"] == policy_id, "policy_id")
check(package["transportExecutionAuthorizationId"] == auth2["transportExecutionAuthorizationId"], "auth_id")
check(package["transportExecutionAttemptOrdinal"] == 2, "auth_ordinal")
check(package["transportExecutionAttemptCount"] == 2, "auth_count")
check(package["credentialRef"] == "env:BANNERLORDAI_TEST_PROVIDER_KEY", "credential_ref")
check(
    pkg.build_execution_package(provider_json, transport_json, policy_json, auth2)["executionPackageId"]
    == package["executionPackageId"],
    "package_id_stable",
)

tampered = copy.deepcopy(package)
tampered["executionPackageId"] = "0" * 64
check("EXECUTION_PACKAGE_ID_MISMATCH" in pkg.validate_execution_package(tampered, auth2).reasons, "tamper_package_id")

tampered = copy.deepcopy(package)
tampered["providerId"] = "other_provider"
check(any("PROVIDERID" in x for x in pkg.validate_execution_package(tampered, auth2).reasons), "tamper_provider")

tampered = copy.deepcopy(package)
tampered["transportRequestId"] = "B" * 64
check(any("TRANSPORT" in x for x in pkg.validate_execution_package(tampered, auth2).reasons), "tamper_transport")

tampered = copy.deepcopy(package)
tampered["executionPolicyId"] = "C" * 64
check(any("POLICY" in x or "AUTHORIZATION" in x for x in pkg.validate_execution_package(tampered, auth2).reasons), "tamper_policy")

tampered = copy.deepcopy(package)
tampered["transportExecutionAuthorizationId"] = "D" * 64
check(any("AUTHORIZATION" in x for x in pkg.validate_execution_package(tampered, auth2).reasons), "tamper_auth_id")

tampered = copy.deepcopy(package)
tampered["transportExecutionAttemptOrdinal"] = 1
check(any("AUTHORIZATION" in x for x in pkg.validate_execution_package(tampered, auth2).reasons), "tamper_auth_ordinal")

tampered = copy.deepcopy(package)
tampered["timeoutMs"] = "999"
check(any("POLICY" in x or "AUTHORIZATION" in x for x in pkg.validate_execution_package(tampered, auth2).reasons), "tamper_timeout")

tampered = copy.deepcopy(package)
tampered["maxAttempts"] = "1"
check(any("POLICY" in x or "AUTHORIZATION" in x for x in pkg.validate_execution_package(tampered, auth2).reasons), "tamper_attempts")

tampered = copy.deepcopy(package)
tampered["maxResultBytes"] = "10"
check(any("POLICY" in x for x in pkg.validate_execution_package(tampered, auth2).reasons), "tamper_size")

tampered = copy.deepcopy(package)
tampered["credentialRef"] = "env:OTHER"
check("EXECUTION_POLICY_CREDENTIALREF_MISMATCH" in pkg.validate_execution_package(tampered, auth2).reasons, "tamper_credential_ref")

tampered = copy.deepcopy(package)
tampered["unexpected"] = True
check(any(x.startswith("PACKAGE_UNEXPECTED_FIELD") for x in pkg.validate_execution_package(tampered, auth2).reasons), "unknown_field")

stale_package = pkg.build_execution_package(
    provider_json,
    transport_json,
    policy_json,
    auth1,
)
stale = pkg.validate_execution_package(stale_package, auth2)
check(not stale.valid, "stale_invalid")
check("LATEST_AUTHORIZATION_ID_MISMATCH" in stale.reasons, "stale_id_reason")
check("LATEST_AUTHORIZATION_ORDINAL_MISMATCH" in stale.reasons, "stale_ordinal_reason")

adapter = pkg.DeterministicProviderAdapter()
execution = adapter.execute(package, auth2)
check(execution.success, "adapter_success")
check(execution.adapter_invoked, "adapter_invoked")
check(adapter.invocations == 1, "adapter_count")
check(execution.transport_outcome is not None, "adapter_outcome")
check(
    execution.transport_outcome.receipt["schema"]
    == "BannerlordAI.PatrolDefenseProviderTransportReceipt.v4",
    "adapter_receipt_schema",
)
check(
    execution.transport_outcome.result["schema"]
    == "BannerlordAI.PatrolDefenseProviderResult.v5",
    "adapter_result_schema",
)
check(
    execution.transport_outcome.receipt["transportExecutionAuthorizationId"]
    == auth2["transportExecutionAuthorizationId"],
    "adapter_receipt_auth",
)
check(
    execution.transport_outcome.result["transportExecutionAuthorizationId"]
    == auth2["transportExecutionAuthorizationId"],
    "adapter_result_auth",
)
check(
    execution.transport_outcome.result["transportExecutionAttemptOrdinal"] == "2",
    "adapter_result_ordinal",
)
check(
    execution.transport_outcome.receipt["externalNetworkUsed"] is False
    and execution.transport_outcome.receipt["modelInvoked"] is False,
    "adapter_zero_network_model",
)

bad_adapter = pkg.DeterministicProviderAdapter()
bad_execution = bad_adapter.execute(stale_package, auth2)
check(not bad_execution.success, "bad_adapter_reject")
check(not bad_execution.adapter_invoked, "bad_adapter_not_invoked")
check(bad_adapter.invocations == 0, "bad_adapter_count_zero")

source = Path(pkg.__file__).read_text(encoding="utf-8")
for forbidden in ("os.environ", "getenv(", "requests.", "httpx", "socket.", "OpenAI", "ChatCompletion"):
    check(forbidden not in source, "forbidden_" + forbidden)

print(f"PASS_FIXTURES checks={checks}")
