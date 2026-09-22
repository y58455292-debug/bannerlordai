import copy
import hashlib
import json
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).parent))

import provider_request_contract_v02154 as request_contract
import provider_transport_loopback_v02159 as transport

checks = 0


def check(value, name):
    global checks
    checks += 1
    if not value:
        raise AssertionError(name)


fingerprint = "D" * 64
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

provider_request, provider_request_json = request_contract.build_provider_request(
    job_line=job_line,
    claim=claim,
    provider_id="deterministic_external_worker",
    model_id="deterministic_mock_no_model",
)
provider_request_id = hashlib.sha256(
    provider_request_json.encode("utf-8")
).hexdigest().upper()

provider_registration = {
    "schema": "BannerlordAI.PatrolDefenseProviderRequestRegistration.v1",
    "mode": "observe",
    "providerRequestId": provider_request_id,
    "providerId": provider_request["providerId"],
    "modelId": provider_request["modelId"],
    "attemptId": provider_request["attemptId"],
    "requestFingerprint": provider_request["requestFingerprint"],
    "promptContractVersion": provider_request["promptContractVersion"],
    "responseSchema": provider_request["responseSchema"],
    "inputSha256": provider_request["inputSha256"],
    "registered": True,
    "idempotent": False,
    "reason": "REGISTERED",
    "queueCount": 1,
}

transport_request = {
    "schema": "BannerlordAI.PatrolDefenseProviderTransportRequest.v1",
    "transportId": transport.TRANSPORT_ID,
    "providerRequestId": provider_request_id,
    "providerId": provider_request["providerId"],
    "modelId": provider_request["modelId"],
    "attemptId": provider_request["attemptId"],
    "requestFingerprint": provider_request["requestFingerprint"],
    "requestInputSha256": provider_request["inputSha256"],
}
transport_request_json = json.dumps(
    transport_request,
    separators=(",", ":"),
    ensure_ascii=False,
)
transport_request_id = hashlib.sha256(
    transport_request_json.encode("utf-8")
).hexdigest().upper()

transport_registration = {
    "schema": "BannerlordAI.PatrolDefenseProviderTransportRegistration.v1",
    "mode": "observe",
    "transportRequestId": transport_request_id,
    "transportId": transport.TRANSPORT_ID,
    "providerRequestId": provider_request_id,
    "providerId": provider_request["providerId"],
    "modelId": provider_request["modelId"],
    "attemptId": provider_request["attemptId"],
    "requestFingerprint": provider_request["requestFingerprint"],
    "requestInputSha256": provider_request["inputSha256"],
    "registered": True,
    "idempotent": False,
    "reason": "REGISTERED",
    "queueCount": 1,
}

outcome = transport.send_loopback_v3(
    provider_request_json,
    provider_registration,
    transport_request_json,
    transport_registration,
)
check(outcome.success, "success")
check(outcome.reasons == [], "no_reasons")
check(outcome.result["schema"] == transport.RESULT_SCHEMA, "result_schema")
check(outcome.result["providerId"] == provider_request["providerId"], "provider")
check(outcome.result["attemptId"] == provider_request["attemptId"], "attempt")
check(
    outcome.result["requestFingerprint"] == provider_request["requestFingerprint"],
    "fingerprint",
)
check(
    outcome.result["providerRequestId"] == provider_request_id,
    "provider_request_id",
)
check(
    outcome.result["transportRequestId"] == transport_request_id,
    "transport_request_id",
)
check(outcome.result["status"] == "SUCCESS", "status")

advisory = json.loads(
    __import__("base64")
    .b64decode(outcome.result["advisoryBase64"])
    .decode("utf-8")
)
check(advisory["schema"] == transport.ADVISORY_SCHEMA, "advisory_schema")
check(advisory["requestFingerprint"] == fingerprint, "advisory_fingerprint")
check(advisory["disposition"] == "KEEP_BASELINE", "advisory_disposition")

expected_result_sha = hashlib.sha256(
    outcome.result_json.encode("utf-8")
).hexdigest().upper()
check(outcome.receipt["schema"] == transport.RECEIPT_SCHEMA, "receipt_schema")
check(
    outcome.receipt["transportId"] == transport.TRANSPORT_ID,
    "receipt_transport",
)
check(
    outcome.receipt["transportRequestId"] == transport_request_id,
    "receipt_transport_request_id",
)
check(
    outcome.receipt["providerRequestId"] == provider_request_id,
    "receipt_provider_request_id",
)
check(
    outcome.receipt["requestInputSha256"] == provider_request["inputSha256"],
    "receipt_input_sha",
)
check(outcome.receipt["resultStatus"] == "SUCCESS", "receipt_status")
check(outcome.receipt["resultSha256"] == expected_result_sha, "receipt_result_sha")
check(outcome.receipt["externalNetworkUsed"] is False, "network_false")
check(outcome.receipt["modelInvoked"] is False, "model_false")
check(outcome.receipt["success"] is True, "receipt_success")
check(outcome.receipt["errorCode"] is None, "error_null")

outcome2 = transport.send_loopback_v3(
    provider_request_json,
    provider_registration,
    transport_request_json,
    transport_registration,
)
check(outcome2.result_json == outcome.result_json, "result_deterministic")
check(outcome2.receipt_json == outcome.receipt_json, "receipt_deterministic")

bad = transport.send_loopback_v3(
    provider_request_json,
    provider_registration,
    transport_request_json,
    None,
)
check(not bad.success, "missing_transport_registration")
check(
    "TRANSPORT_REGISTRATION_MISSING" in bad.reasons,
    "missing_transport_registration_reason",
)

wrong_transport_id = copy.deepcopy(transport_registration)
wrong_transport_id["transportRequestId"] = "0" * 64
bad = transport.send_loopback_v3(
    provider_request_json,
    provider_registration,
    transport_request_json,
    wrong_transport_id,
)
check(not bad.success, "wrong_transport_id")
check(
    "TRANSPORT_REQUEST_ID_MISMATCH" in bad.reasons,
    "wrong_transport_id_reason",
)

wrong_provider_id = copy.deepcopy(provider_registration)
wrong_provider_id["providerRequestId"] = "1" * 64
bad = transport.send_loopback_v3(
    provider_request_json,
    wrong_provider_id,
    transport_request_json,
    transport_registration,
)
check(not bad.success, "wrong_provider_request_id")
check(
    "PROVIDER_REQUEST_ID_MISMATCH" in bad.reasons,
    "wrong_provider_request_id_reason",
)

inactive_provider = copy.deepcopy(provider_registration)
inactive_provider["reason"] = "PROVIDER_REQUEST_ALREADY_REGISTERED"
bad = transport.send_loopback_v3(
    provider_request_json,
    inactive_provider,
    transport_request_json,
    transport_registration,
)
check(not bad.success, "inactive_provider_registration")
check(
    "PROVIDER_REGISTRATION_NOT_ACTIVE" in bad.reasons,
    "inactive_provider_reason",
)

inactive_transport = copy.deepcopy(transport_registration)
inactive_transport["reason"] = "TRANSPORT_REQUEST_ALREADY_REGISTERED"
bad = transport.send_loopback_v3(
    provider_request_json,
    provider_registration,
    transport_request_json,
    inactive_transport,
)
check(not bad.success, "inactive_transport_registration")
check(
    "TRANSPORT_REGISTRATION_NOT_ACTIVE" in bad.reasons,
    "inactive_transport_reason",
)

for key, reason in (
    ("transportId", "TRANSPORT_REGISTRATION_TRANSPORT_MISMATCH"),
    ("providerRequestId", "TRANSPORT_REGISTRATION_PROVIDER_REQUEST_MISMATCH"),
    ("providerId", "TRANSPORT_REGISTRATION_PROVIDER_MISMATCH"),
    ("modelId", "TRANSPORT_REGISTRATION_MODEL_MISMATCH"),
    ("attemptId", "TRANSPORT_REGISTRATION_ATTEMPT_MISMATCH"),
    ("requestFingerprint", "TRANSPORT_REGISTRATION_FINGERPRINT_MISMATCH"),
    ("requestInputSha256", "TRANSPORT_REGISTRATION_INPUT_SHA_MISMATCH"),
):
    mutated = copy.deepcopy(transport_registration)
    mutated[key] = "wrong"
    bad = transport.send_loopback_v3(
        provider_request_json,
        provider_registration,
        transport_request_json,
        mutated,
    )
    check(not bad.success, "registration_mismatch_" + key)
    check(reason in bad.reasons, "registration_mismatch_reason_" + key)

for key, reason in (
    ("providerId", "PROVIDER_REGISTRATION_PROVIDER_MISMATCH"),
    ("modelId", "PROVIDER_REGISTRATION_MODEL_MISMATCH"),
    ("attemptId", "PROVIDER_REGISTRATION_ATTEMPT_MISMATCH"),
    ("requestFingerprint", "PROVIDER_REGISTRATION_FINGERPRINT_MISMATCH"),
    ("promptContractVersion", "PROVIDER_REGISTRATION_PROMPT_MISMATCH"),
    ("responseSchema", "PROVIDER_REGISTRATION_RESPONSE_SCHEMA_MISMATCH"),
    ("inputSha256", "PROVIDER_REGISTRATION_INPUT_SHA_MISMATCH"),
):
    mutated = copy.deepcopy(provider_registration)
    mutated[key] = "wrong"
    bad = transport.send_loopback_v3(
        provider_request_json,
        mutated,
        transport_request_json,
        transport_registration,
    )
    check(not bad.success, "provider_registration_mismatch_" + key)
    check(reason in bad.reasons, "provider_registration_mismatch_reason_" + key)

bad_transport_json = json.dumps(
    dict(transport_request, action="ATTACK"),
    separators=(",", ":"),
)
bad_transport_registration = copy.deepcopy(transport_registration)
bad_transport_registration["transportRequestId"] = hashlib.sha256(
    bad_transport_json.encode("utf-8")
).hexdigest().upper()
bad = transport.send_loopback_v3(
    provider_request_json,
    provider_registration,
    bad_transport_json,
    bad_transport_registration,
)
check(not bad.success, "transport_extra_field")
check(
    "TRANSPORT_REQUEST_UNEXPECTED_FIELD:action" in bad.reasons,
    "transport_extra_field_reason",
)

malformed = transport.send_loopback_v3(
    provider_request_json,
    provider_registration,
    '{"schema":',
    transport_registration,
)
check(not malformed.success, "transport_malformed")
check(
    "TRANSPORT_REQUEST_MALFORMED_JSON" in malformed.reasons,
    "transport_malformed_reason",
)

bad_provider_json = '{"schema":'
bad = transport.send_loopback_v3(
    bad_provider_json,
    provider_registration,
    transport_request_json,
    transport_registration,
)
check(not bad.success, "provider_malformed")
check("MALFORMED_JSON" in bad.reasons, "provider_malformed_reason")

altered_transport = copy.deepcopy(transport_request)
altered_transport["requestInputSha256"] = "F" * 64
altered_transport_json = json.dumps(
    altered_transport,
    separators=(",", ":"),
)
altered_transport_reg = copy.deepcopy(transport_registration)
altered_transport_reg["transportRequestId"] = hashlib.sha256(
    altered_transport_json.encode("utf-8")
).hexdigest().upper()
altered_transport_reg["requestInputSha256"] = "F" * 64
bad = transport.send_loopback_v3(
    provider_request_json,
    provider_registration,
    altered_transport_json,
    altered_transport_reg,
)
check(not bad.success, "transport_input_mismatch")
check(
    "TRANSPORT_INPUT_SHA_MISMATCH" in bad.reasons,
    "transport_input_mismatch_reason",
)

wrong_transport_provider = copy.deepcopy(transport_request)
wrong_transport_provider["providerId"] = "other_worker"
wrong_transport_provider_json = json.dumps(
    wrong_transport_provider,
    separators=(",", ":"),
)
wrong_transport_provider_reg = copy.deepcopy(transport_registration)
wrong_transport_provider_reg["transportRequestId"] = hashlib.sha256(
    wrong_transport_provider_json.encode("utf-8")
).hexdigest().upper()
wrong_transport_provider_reg["providerId"] = "other_worker"
bad = transport.send_loopback_v3(
    provider_request_json,
    provider_registration,
    wrong_transport_provider_json,
    wrong_transport_provider_reg,
)
check(not bad.success, "cross_provider_mismatch")
check(
    "TRANSPORT_PROVIDER_MISMATCH" in bad.reasons,
    "cross_provider_mismatch_reason",
)

print(f"PASS_FIXTURES checks={checks}")
