from __future__ import annotations

import base64
import hashlib
import json
from dataclasses import dataclass
from typing import Any, Dict, List, Optional, Tuple

SCHEMA = "BannerlordAI.PatrolDefenseProviderRequest.v1"
RESPONSE_SCHEMA = "BannerlordAI.PatrolDefenseDeliberationAdvisory.v1"
PROMPT_CONTRACT_VERSION = "BannerlordAI.PatrolDefenseAdvisoryPrompt.v1"
ALLOWED_FIELDS = {
    "schema",
    "providerId",
    "modelId",
    "attemptId",
    "requestFingerprint",
    "promptContractVersion",
    "responseSchema",
    "deliberationRequestBase64",
    "inputSha256",
}


@dataclass
class ValidationResult:
    valid: bool
    reasons: List[str]
    decoded_request_json: Optional[str]
    decoded_request: Optional[Dict[str, Any]]


def _sha256_upper(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest().upper()


def extract_deliberation_request_raw(job_line: str) -> str:
    if not isinstance(job_line, str) or not job_line.strip():
        raise ValueError("JOB_LINE_MISSING")

    marker = '"deliberationRequest":'
    index = job_line.find(marker)
    if index < 0:
        raise ValueError("DELIBERATION_REQUEST_FIELD_MISSING")

    start = index + len(marker)
    while start < len(job_line) and job_line[start].isspace():
        start += 1

    decoder = json.JSONDecoder()
    try:
        value, end = decoder.raw_decode(job_line[start:])
    except json.JSONDecodeError as ex:
        raise ValueError("DELIBERATION_REQUEST_JSON_INVALID") from ex

    if not isinstance(value, dict):
        raise ValueError("DELIBERATION_REQUEST_NOT_OBJECT")

    return job_line[start : start + end]


def build_provider_request(
    *,
    job_line: str,
    claim: Dict[str, Any],
    provider_id: str,
    model_id: str,
    prompt_contract_version: str = PROMPT_CONTRACT_VERSION,
    response_schema: str = RESPONSE_SCHEMA,
) -> Tuple[Dict[str, Any], str]:
    if not provider_id or not isinstance(provider_id, str):
        raise ValueError("PROVIDER_ID_MISSING")
    if not model_id or not isinstance(model_id, str):
        raise ValueError("MODEL_ID_MISSING")
    if not prompt_contract_version or not isinstance(prompt_contract_version, str):
        raise ValueError("PROMPT_CONTRACT_VERSION_MISSING")

    raw_request = extract_deliberation_request_raw(job_line)
    request_bytes = raw_request.encode("utf-8")
    request_obj = json.loads(raw_request)

    try:
        job_obj = json.loads(job_line)
    except json.JSONDecodeError as ex:
        raise ValueError("JOB_JSON_INVALID") from ex

    if job_obj.get("schema") != "BannerlordAI.PatrolDefenseProviderDispatchJob.v1":
        raise ValueError("JOB_SCHEMA_INVALID")
    if job_obj.get("state") not in ("PENDING", "CLAIMED"):
        raise ValueError("JOB_STATE_INVALID")

    fingerprint = job_obj.get("requestFingerprint")
    if not fingerprint or fingerprint != request_obj.get("requestFingerprint"):
        raise ValueError("JOB_REQUEST_FINGERPRINT_MISMATCH")

    if not isinstance(claim, dict):
        raise ValueError("CLAIM_MISSING")
    if claim.get("schema") != "BannerlordAI.PatrolDefenseProviderJobClaim.v1":
        raise ValueError("CLAIM_SCHEMA_INVALID")
    if claim.get("reason") not in ("CLAIMED", "SAME_ATTEMPT_ALREADY_CLAIMED"):
        raise ValueError("CLAIM_NOT_ACTIVE")
    if claim.get("requestFingerprint") != fingerprint:
        raise ValueError("CLAIM_REQUEST_FINGERPRINT_MISMATCH")
    if claim.get("providerId") != provider_id:
        raise ValueError("CLAIM_PROVIDER_MISMATCH")

    attempt_id = claim.get("attemptId")
    if not attempt_id or not isinstance(attempt_id, str):
        raise ValueError("CLAIM_ATTEMPT_MISSING")

    envelope = {
        "schema": SCHEMA,
        "providerId": provider_id,
        "modelId": model_id,
        "attemptId": attempt_id,
        "requestFingerprint": fingerprint,
        "promptContractVersion": prompt_contract_version,
        "responseSchema": response_schema,
        "deliberationRequestBase64": base64.b64encode(request_bytes).decode("ascii"),
        "inputSha256": _sha256_upper(request_bytes),
    }
    envelope_json = json.dumps(
        envelope,
        separators=(",", ":"),
        ensure_ascii=False,
    )
    return envelope, envelope_json


def validate_provider_request(
    envelope_json: str,
    *,
    expected_job_line: Optional[str] = None,
    expected_claim: Optional[Dict[str, Any]] = None,
) -> ValidationResult:
    reasons: List[str] = []
    decoded_request_json: Optional[str] = None
    decoded_request: Optional[Dict[str, Any]] = None

    try:
        envelope = json.loads(envelope_json)
    except Exception:
        return ValidationResult(False, ["MALFORMED_JSON"], None, None)

    if not isinstance(envelope, dict):
        return ValidationResult(False, ["ENVELOPE_NOT_OBJECT"], None, None)

    unknown = sorted(set(envelope.keys()) - ALLOWED_FIELDS)
    for key in unknown:
        reasons.append("UNEXPECTED_FIELD:" + key)

    missing = sorted(ALLOWED_FIELDS - set(envelope.keys()))
    for key in missing:
        reasons.append("MISSING_FIELD:" + key)

    if reasons:
        return ValidationResult(False, reasons, None, None)

    if envelope.get("schema") != SCHEMA:
        reasons.append("SCHEMA_INVALID")

    for field, reason in (
        ("providerId", "PROVIDER_ID_MISSING"),
        ("modelId", "MODEL_ID_MISSING"),
        ("attemptId", "ATTEMPT_ID_MISSING"),
        ("requestFingerprint", "REQUEST_FINGERPRINT_MISSING"),
        ("promptContractVersion", "PROMPT_CONTRACT_VERSION_MISSING"),
    ):
        if not isinstance(envelope.get(field), str) or not envelope.get(field):
            reasons.append(reason)

    if envelope.get("responseSchema") != RESPONSE_SCHEMA:
        reasons.append("RESPONSE_SCHEMA_INVALID")

    if envelope.get("promptContractVersion") != PROMPT_CONTRACT_VERSION:
        reasons.append("PROMPT_CONTRACT_VERSION_INVALID")

    encoded = envelope.get("deliberationRequestBase64")
    decoded_bytes: Optional[bytes] = None
    if not isinstance(encoded, str) or not encoded:
        reasons.append("DELIBERATION_REQUEST_BASE64_MISSING")
    else:
        try:
            decoded_bytes = base64.b64decode(encoded, validate=True)
        except Exception:
            reasons.append("DELIBERATION_REQUEST_BASE64_INVALID")

    if decoded_bytes is not None:
        try:
            decoded_request_json = decoded_bytes.decode("utf-8")
        except UnicodeDecodeError:
            reasons.append("DELIBERATION_REQUEST_UTF8_INVALID")
        else:
            try:
                parsed = json.loads(decoded_request_json)
                if isinstance(parsed, dict):
                    decoded_request = parsed
                else:
                    reasons.append("DELIBERATION_REQUEST_NOT_OBJECT")
            except Exception:
                reasons.append("DELIBERATION_REQUEST_JSON_INVALID")

        actual_hash = _sha256_upper(decoded_bytes)
        if envelope.get("inputSha256") != actual_hash:
            reasons.append("INPUT_SHA256_MISMATCH")

    if decoded_request is not None:
        if decoded_request.get("requestFingerprint") != envelope.get(
            "requestFingerprint"
        ):
            reasons.append("DELIBERATION_REQUEST_FINGERPRINT_MISMATCH")

    if expected_job_line is not None:
        try:
            expected_raw = extract_deliberation_request_raw(expected_job_line)
            expected_job = json.loads(expected_job_line)
        except Exception:
            reasons.append("EXPECTED_JOB_INVALID")
        else:
            if decoded_request_json != expected_raw:
                reasons.append("DELIBERATION_REQUEST_BYTES_MISMATCH")
            if envelope.get("requestFingerprint") != expected_job.get(
                "requestFingerprint"
            ):
                reasons.append("JOB_REQUEST_FINGERPRINT_MISMATCH")

    if expected_claim is not None:
        if not isinstance(expected_claim, dict):
            reasons.append("EXPECTED_CLAIM_INVALID")
        else:
            if envelope.get("requestFingerprint") != expected_claim.get(
                "requestFingerprint"
            ):
                reasons.append("CLAIM_REQUEST_FINGERPRINT_MISMATCH")
            if envelope.get("providerId") != expected_claim.get("providerId"):
                reasons.append("CLAIM_PROVIDER_MISMATCH")
            if envelope.get("attemptId") != expected_claim.get("attemptId"):
                reasons.append("CLAIM_ATTEMPT_MISMATCH")
            if expected_claim.get("reason") not in (
                "CLAIMED",
                "SAME_ATTEMPT_ALREADY_CLAIMED",
            ):
                reasons.append("CLAIM_NOT_ACTIVE")

    return ValidationResult(
        len(reasons) == 0,
        reasons,
        decoded_request_json,
        decoded_request,
    )
