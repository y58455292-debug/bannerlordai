import base64
import copy
import hashlib
import json
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).parent))
import provider_request_contract_v02154 as c

checks = 0

def check(value, name):
    global checks
    checks += 1
    if not value:
        raise AssertionError(name)

fingerprint = "A" * 64
request_raw = (
    '{"schema":"BannerlordAI.PatrolDefenseDeliberationRequest.v1",'
    '"requestEligible":true,'
    '"requestFingerprint":"' + fingerprint + '",'
    '"baselineDecision":{"wouldInterrupt":false,"candidateAction":"CONTINUE_PATROL"}}'
)
job_line = (
    '{"schema":"BannerlordAI.PatrolDefenseProviderDispatchJob.v1",'
    '"sequence":7,'
    '"requestFingerprint":"' + fingerprint + '",'
    '"state":"PENDING",'
    '"enqueuedUtcTicks":12345,'
    '"deliberationRequest":' + request_raw + '}'
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
    "claimedUtcTicks": 22222,
}

envelope, envelope_json = c.build_provider_request(
    job_line=job_line,
    claim=claim,
    provider_id="deterministic_external_worker",
    model_id="deterministic_mock_no_model",
)
check(envelope["schema"] == c.SCHEMA, "schema")
check(envelope["providerId"] == claim["providerId"], "provider")
check(envelope["attemptId"] == claim["attemptId"], "attempt")
check(envelope["requestFingerprint"] == fingerprint, "fingerprint")
check(envelope["promptContractVersion"] == c.PROMPT_CONTRACT_VERSION, "prompt_version")
check(envelope["responseSchema"] == c.RESPONSE_SCHEMA, "response_schema")
check(
    base64.b64decode(envelope["deliberationRequestBase64"]).decode("utf-8") == request_raw,
    "exact_request_bytes",
)
check(
    envelope["inputSha256"] == hashlib.sha256(request_raw.encode("utf-8")).hexdigest().upper(),
    "hash_exact",
)

valid = c.validate_provider_request(
    envelope_json,
    expected_job_line=job_line,
    expected_claim=claim,
)
check(valid.valid, "valid_envelope")
check(valid.reasons == [], "valid_no_reasons")
check(valid.decoded_request_json == request_raw, "valid_raw_exact")
check(valid.decoded_request["requestFingerprint"] == fingerprint, "valid_decoded_fp")

check(
    c.validate_provider_request('{"schema":', expected_job_line=job_line, expected_claim=claim).reasons == ["MALFORMED_JSON"],
    "malformed_json",
)

for key in ["action", "target", "score", "scoreDelta", "movement", "apply", "command", "note"]:
    e = copy.deepcopy(envelope)
    e[key] = "x"
    result = c.validate_provider_request(json.dumps(e, separators=(",", ":")))
    check(("UNEXPECTED_FIELD:" + key) in result.reasons, "extra_" + key)

for key in sorted(c.ALLOWED_FIELDS):
    e = copy.deepcopy(envelope)
    e.pop(key)
    result = c.validate_provider_request(json.dumps(e, separators=(",", ":")))
    check(("MISSING_FIELD:" + key) in result.reasons, "missing_" + key)

altered_request = request_raw.replace("CONTINUE_PATROL", "WAIT")
e = copy.deepcopy(envelope)
e["deliberationRequestBase64"] = base64.b64encode(altered_request.encode("utf-8")).decode("ascii")
result = c.validate_provider_request(
    json.dumps(e, separators=(",", ":")),
    expected_job_line=job_line,
    expected_claim=claim,
)
check("INPUT_SHA256_MISMATCH" in result.reasons, "altered_hash_mismatch")
check("DELIBERATION_REQUEST_BYTES_MISMATCH" in result.reasons, "altered_bytes_mismatch")

bad_hash = copy.deepcopy(envelope)
bad_hash["inputSha256"] = "0" * 64
result = c.validate_provider_request(json.dumps(bad_hash, separators=(",", ":")))
check("INPUT_SHA256_MISMATCH" in result.reasons, "bad_hash")

wrong_fp = copy.deepcopy(envelope)
wrong_fp["requestFingerprint"] = "B" * 64
result = c.validate_provider_request(
    json.dumps(wrong_fp, separators=(",", ":")),
    expected_job_line=job_line,
    expected_claim=claim,
)
check("DELIBERATION_REQUEST_FINGERPRINT_MISMATCH" in result.reasons, "wrong_fp_request")
check("JOB_REQUEST_FINGERPRINT_MISMATCH" in result.reasons, "wrong_fp_job")
check("CLAIM_REQUEST_FINGERPRINT_MISMATCH" in result.reasons, "wrong_fp_claim")

wrong_attempt = copy.deepcopy(envelope)
wrong_attempt["attemptId"] = "attempt-2"
result = c.validate_provider_request(
    json.dumps(wrong_attempt, separators=(",", ":")),
    expected_claim=claim,
)
check("CLAIM_ATTEMPT_MISMATCH" in result.reasons, "wrong_attempt")

wrong_provider = copy.deepcopy(envelope)
wrong_provider["providerId"] = "other_worker"
result = c.validate_provider_request(
    json.dumps(wrong_provider, separators=(",", ":")),
    expected_claim=claim,
)
check("CLAIM_PROVIDER_MISMATCH" in result.reasons, "wrong_provider")

wrong_response = copy.deepcopy(envelope)
wrong_response["responseSchema"] = "Wrong.Response"
result = c.validate_provider_request(json.dumps(wrong_response, separators=(",", ":")))
check("RESPONSE_SCHEMA_INVALID" in result.reasons, "wrong_response_schema")

wrong_prompt = copy.deepcopy(envelope)
wrong_prompt["promptContractVersion"] = "Wrong.Prompt"
result = c.validate_provider_request(json.dumps(wrong_prompt, separators=(",", ":")))
check("PROMPT_CONTRACT_VERSION_INVALID" in result.reasons, "wrong_prompt")

bad64 = copy.deepcopy(envelope)
bad64["deliberationRequestBase64"] = "%%%"
result = c.validate_provider_request(json.dumps(bad64, separators=(",", ":")))
check("DELIBERATION_REQUEST_BASE64_INVALID" in result.reasons, "invalid_base64")

not_json = copy.deepcopy(envelope)
raw = b"not-json"
not_json["deliberationRequestBase64"] = base64.b64encode(raw).decode("ascii")
not_json["inputSha256"] = hashlib.sha256(raw).hexdigest().upper()
result = c.validate_provider_request(json.dumps(not_json, separators=(",", ":")))
check("DELIBERATION_REQUEST_JSON_INVALID" in result.reasons, "decoded_not_json")

array_request = copy.deepcopy(envelope)
raw = b"[]"
array_request["deliberationRequestBase64"] = base64.b64encode(raw).decode("ascii")
array_request["inputSha256"] = hashlib.sha256(raw).hexdigest().upper()
result = c.validate_provider_request(json.dumps(array_request, separators=(",", ":")))
check("DELIBERATION_REQUEST_NOT_OBJECT" in result.reasons, "decoded_not_object")

inactive_claim = copy.deepcopy(claim)
inactive_claim["reason"] = "ALREADY_CLAIMED"
result = c.validate_provider_request(envelope_json, expected_claim=inactive_claim)
check("CLAIM_NOT_ACTIVE" in result.reasons, "inactive_claim")

for bad_claim, expected in [
    (dict(claim, providerId="other"), "CLAIM_PROVIDER_MISMATCH"),
    (dict(claim, requestFingerprint="B" * 64), "CLAIM_REQUEST_FINGERPRINT_MISMATCH"),
    (dict(claim, reason="ALREADY_CLAIMED"), "CLAIM_NOT_ACTIVE"),
    (dict(claim, attemptId=""), "CLAIM_ATTEMPT_MISSING"),
]:
    try:
        c.build_provider_request(
            job_line=job_line,
            claim=bad_claim,
            provider_id="deterministic_external_worker",
            model_id="deterministic_mock_no_model",
        )
    except ValueError as ex:
        check(str(ex) == expected, "builder_" + expected)
    else:
        raise AssertionError("builder should reject " + expected)

request_raw_spaced = '{ "schema" : "x", "requestFingerprint" : "' + fingerprint + '" }'
job_spaced = (
    '{"schema":"BannerlordAI.PatrolDefenseProviderDispatchJob.v1",'
    '"requestFingerprint":"' + fingerprint + '",'
    '"state":"PENDING","deliberationRequest":' + request_raw_spaced + '}'
)
check(c.extract_deliberation_request_raw(job_spaced) == request_raw_spaced, "raw_whitespace_preserved")

print(f"PASS_FIXTURES checks={checks}")
