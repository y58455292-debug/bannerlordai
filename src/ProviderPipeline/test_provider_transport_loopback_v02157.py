import copy
import hashlib
import json
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).parent))
import provider_request_contract_v02154 as request_contract
import provider_transport_loopback_v02157 as transport

checks = 0


def check(value, name):
    global checks
    checks += 1
    if not value:
        raise AssertionError(name)


fingerprint = "C" * 64
raw_request = (
    '{"schema":"BannerlordAI.PatrolDefenseDeliberationRequest.v1",'
    '"requestEligible":true,'
    '"requestFingerprint":"' + fingerprint + '",'
    '"baselineDecision":{"wouldInterrupt":false,"candidateAction":"CONTINUE_PATROL"}}'
)
job_line = (
    '{"schema":"BannerlordAI.PatrolDefenseProviderDispatchJob.v1",'
    '"sequence":1,'
    '"requestFingerprint":"' + fingerprint + '",'
    '"state":"PENDING",'
    '"enqueuedUtcTicks":123,'
    '"deliberationRequest":' + raw_request + '}'
)
claim = {
    "schema": "BannerlordAI.PatrolDefenseProviderJobClaim.v1",
    "mode": "observe",
    "requestFingerprint": fingerprint,
    "providerId": "deterministic_external_worker",
    "attemptId": "attempt-1",
    "claimApplied": True,
    "idempotent": False,
    "reason": "CLAIMED",
    "stateBefore": "PENDING",
    "stateAfter": "CLAIMED",
    "queueCount": 1,
    "claimedUtcTicks": 456,
}
request, request_json = request_contract.build_provider_request(
    job_line=job_line,
    claim=claim,
    provider_id="deterministic_external_worker",
    model_id="deterministic_mock_no_model",
)
provider_request_id = hashlib.sha256(
    request_json.encode("utf-8")
).hexdigest().upper()
registration = {
    "schema": "BannerlordAI.PatrolDefenseProviderRequestRegistration.v1",
    "mode": "observe",
    "providerRequestId": provider_request_id,
    "providerId": request["providerId"],
    "modelId": request["modelId"],
    "attemptId": request["attemptId"],
    "requestFingerprint": request["requestFingerprint"],
    "promptContractVersion": request["promptContractVersion"],
    "responseSchema": request["responseSchema"],
    "inputSha256": request["inputSha256"],
    "registered": True,
    "idempotent": False,
    "reason": "REGISTERED",
    "queueCount": 1,
}

outcome = transport.send_loopback(request_json, registration)
check(outcome.success, "success")
check(outcome.reasons == [], "no_reasons")
check(outcome.result["schema"] == transport.RESULT_SCHEMA, "result_schema")
check(outcome.result["providerId"] == request["providerId"], "provider")
check(outcome.result["attemptId"] == request["attemptId"], "attempt")
check(
    outcome.result["requestFingerprint"] == request["requestFingerprint"],
    "fingerprint",
)
check(
    outcome.result["providerRequestId"] == provider_request_id,
    "provider_request_id",
)
check(outcome.result["status"] == "SUCCESS", "status")
advisory = json.loads(
    __import__("base64").b64decode(outcome.result["advisoryBase64"]).decode("utf-8")
)
check(advisory["schema"] == transport.ADVISORY_SCHEMA, "advisory_schema")
check(advisory["requestFingerprint"] == fingerprint, "advisory_fp")
check(advisory["disposition"] == "KEEP_BASELINE", "advisory_disposition")

expected_result_sha = hashlib.sha256(
    outcome.result_json.encode("utf-8")
).hexdigest().upper()
check(outcome.receipt["schema"] == transport.RECEIPT_SCHEMA, "receipt_schema")
check(
    outcome.receipt["transportId"] == transport.TRANSPORT_ID,
    "transport_id",
)
check(
    outcome.receipt["providerRequestId"] == provider_request_id,
    "receipt_request_id",
)
check(outcome.receipt["providerId"] == request["providerId"], "receipt_provider")
check(outcome.receipt["modelId"] == request["modelId"], "receipt_model")
check(outcome.receipt["attemptId"] == request["attemptId"], "receipt_attempt")
check(
    outcome.receipt["requestFingerprint"] == fingerprint,
    "receipt_fingerprint",
)
check(
    outcome.receipt["requestInputSha256"] == request["inputSha256"],
    "receipt_input_sha",
)
check(outcome.receipt["resultStatus"] == "SUCCESS", "receipt_status")
check(
    outcome.receipt["resultSha256"] == expected_result_sha,
    "result_sha",
)
check(outcome.receipt["externalNetworkUsed"] is False, "network_false")
check(outcome.receipt["modelInvoked"] is False, "model_false")
check(outcome.receipt["success"] is True, "receipt_success")
check(outcome.receipt["errorCode"] is None, "error_null")

outcome2 = transport.send_loopback(request_json, registration)
check(outcome2.result_json == outcome.result_json, "result_deterministic")
check(outcome2.receipt_json == outcome.receipt_json, "receipt_deterministic")

wrong_id = copy.deepcopy(registration)
wrong_id["providerRequestId"] = "0" * 64
bad = transport.send_loopback(request_json, wrong_id)
check(not bad.success, "wrong_id_fail")
check(
    "PROVIDER_REQUEST_ID_MISMATCH" in bad.reasons,
    "wrong_id_reason",
)
check(bad.result_json is None, "wrong_id_no_result")
check(bad.receipt["externalNetworkUsed"] is False, "wrong_id_network_false")
check(bad.receipt["modelInvoked"] is False, "wrong_id_model_false")

missing_id = copy.deepcopy(registration)
missing_id["providerRequestId"] = None
bad = transport.send_loopback(request_json, missing_id)
check(not bad.success, "missing_id_fail")
check(
    "PROVIDER_REQUEST_ID_MISSING" in bad.reasons,
    "missing_id_reason",
)

for reg_key, req_key, reason in (
    ("providerId", "providerId", "REGISTRATION_PROVIDER_MISMATCH"),
    ("modelId", "modelId", "REGISTRATION_MODEL_MISMATCH"),
    ("attemptId", "attemptId", "REGISTRATION_ATTEMPT_MISMATCH"),
    (
        "requestFingerprint",
        "requestFingerprint",
        "REGISTRATION_FINGERPRINT_MISMATCH",
    ),
    (
        "promptContractVersion",
        "promptContractVersion",
        "REGISTRATION_PROMPT_MISMATCH",
    ),
    ("responseSchema", "responseSchema", "REGISTRATION_RESPONSE_SCHEMA_MISMATCH"),
    ("inputSha256", "inputSha256", "REGISTRATION_INPUT_SHA_MISMATCH"),
):
    mutated = copy.deepcopy(registration)
    mutated[reg_key] = "wrong"
    bad = transport.send_loopback(request_json, mutated)
    check(not bad.success, "mismatch_fail_" + reg_key)
    check(reason in bad.reasons, "mismatch_reason_" + reg_key)

inactive = copy.deepcopy(registration)
inactive["reason"] = "PROVIDER_REQUEST_ALREADY_REGISTERED"
bad = transport.send_loopback(request_json, inactive)
check(not bad.success, "inactive_registration")
check("REGISTRATION_NOT_ACTIVE" in bad.reasons, "inactive_reason")

malformed = transport.send_loopback('{"schema":', registration)
check(not malformed.success, "malformed_fail")
check("MALFORMED_JSON" in malformed.reasons, "malformed_reason")
check(malformed.result_json is None, "malformed_no_result")

extra_obj = copy.deepcopy(request)
extra_obj["action"] = "ATTACK"
extra_json = json.dumps(extra_obj, separators=(",", ":"))
extra_reg = copy.deepcopy(registration)
extra_reg["providerRequestId"] = hashlib.sha256(
    extra_json.encode("utf-8")
).hexdigest().upper()
bad = transport.send_loopback(extra_json, extra_reg)
check(not bad.success, "extra_field_fail")
check("UNEXPECTED_FIELD:action" in bad.reasons, "extra_field_reason")

bad_hash_obj = copy.deepcopy(request)
bad_hash_obj["inputSha256"] = "F" * 64
bad_hash_json = json.dumps(bad_hash_obj, separators=(",", ":"))
bad_hash_reg = copy.deepcopy(registration)
bad_hash_reg["providerRequestId"] = hashlib.sha256(
    bad_hash_json.encode("utf-8")
).hexdigest().upper()
bad_hash_reg["inputSha256"] = bad_hash_obj["inputSha256"]
bad = transport.send_loopback(bad_hash_json, bad_hash_reg)
check(not bad.success, "bad_input_hash_fail")
check("INPUT_SHA256_MISMATCH" in bad.reasons, "bad_input_hash_reason")

wrong_prompt_obj = copy.deepcopy(request)
wrong_prompt_obj["promptContractVersion"] = "Wrong.Prompt"
wrong_prompt_json = json.dumps(wrong_prompt_obj, separators=(",", ":"))
wrong_prompt_reg = copy.deepcopy(registration)
wrong_prompt_reg["providerRequestId"] = hashlib.sha256(
    wrong_prompt_json.encode("utf-8")
).hexdigest().upper()
wrong_prompt_reg["promptContractVersion"] = "Wrong.Prompt"
bad = transport.send_loopback(wrong_prompt_json, wrong_prompt_reg)
check(not bad.success, "bad_prompt_fail")
check(
    "PROMPT_CONTRACT_VERSION_INVALID" in bad.reasons,
    "bad_prompt_reason",
)

wrong_response_obj = copy.deepcopy(request)
wrong_response_obj["responseSchema"] = "Wrong.Response"
wrong_response_json = json.dumps(wrong_response_obj, separators=(",", ":"))
wrong_response_reg = copy.deepcopy(registration)
wrong_response_reg["providerRequestId"] = hashlib.sha256(
    wrong_response_json.encode("utf-8")
).hexdigest().upper()
wrong_response_reg["responseSchema"] = "Wrong.Response"
bad = transport.send_loopback(wrong_response_json, wrong_response_reg)
check(not bad.success, "bad_response_fail")
check("RESPONSE_SCHEMA_INVALID" in bad.reasons, "bad_response_reason")

print(f"PASS_FIXTURES checks={checks}")
