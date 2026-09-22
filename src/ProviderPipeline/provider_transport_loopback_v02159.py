from __future__ import annotations

import base64
import hashlib
import json
from dataclasses import dataclass
from typing import Any, Dict, List, Optional, Tuple

import provider_request_contract_v02154 as request_contract

TRANSPORT_ID = "deterministic_local_loopback"
TRANSPORT_REQUEST_SCHEMA = "BannerlordAI.PatrolDefenseProviderTransportRequest.v1"
TRANSPORT_REGISTRATION_SCHEMA = (
    "BannerlordAI.PatrolDefenseProviderTransportRegistration.v1"
)
PROVIDER_REGISTRATION_SCHEMA = (
    "BannerlordAI.PatrolDefenseProviderRequestRegistration.v1"
)
RESULT_SCHEMA = "BannerlordAI.PatrolDefenseProviderResult.v3"
RECEIPT_SCHEMA = "BannerlordAI.PatrolDefenseProviderTransportReceipt.v2"
ADVISORY_SCHEMA = "BannerlordAI.PatrolDefenseDeliberationAdvisory.v1"

TRANSPORT_REQUEST_FIELDS = {
    "schema",
    "transportId",
    "providerRequestId",
    "providerId",
    "modelId",
    "attemptId",
    "requestFingerprint",
    "requestInputSha256",
}


@dataclass
class TransportV3Outcome:
    success: bool
    result_json: Optional[str]
    result: Optional[Dict[str, Any]]
    receipt_json: str
    receipt: Dict[str, Any]
    reasons: List[str]


def _sha256_upper(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest().upper()


def _transport_request_validation(
    transport_request_json: str,
) -> Tuple[Optional[Dict[str, Any]], List[str]]:
    reasons: List[str] = []
    try:
        request = json.loads(transport_request_json)
    except Exception:
        return None, ["TRANSPORT_REQUEST_MALFORMED_JSON"]

    if not isinstance(request, dict):
        return None, ["TRANSPORT_REQUEST_NOT_OBJECT"]

    extra = sorted(set(request.keys()) - TRANSPORT_REQUEST_FIELDS)
    for key in extra:
        reasons.append("TRANSPORT_REQUEST_UNEXPECTED_FIELD:" + key)

    missing = sorted(TRANSPORT_REQUEST_FIELDS - set(request.keys()))
    for key in missing:
        reasons.append("TRANSPORT_REQUEST_MISSING_FIELD:" + key)

    if reasons:
        return request, reasons

    if request.get("schema") != TRANSPORT_REQUEST_SCHEMA:
        reasons.append("TRANSPORT_REQUEST_SCHEMA_INVALID")

    for key, reason in (
        ("transportId", "TRANSPORT_ID_MISSING"),
        ("providerRequestId", "PROVIDER_REQUEST_ID_MISSING"),
        ("providerId", "PROVIDER_ID_MISSING"),
        ("modelId", "MODEL_ID_MISSING"),
        ("attemptId", "ATTEMPT_ID_MISSING"),
        ("requestFingerprint", "REQUEST_FINGERPRINT_MISSING"),
        ("requestInputSha256", "REQUEST_INPUT_SHA256_MISSING"),
    ):
        if not isinstance(request.get(key), str) or not request.get(key):
            reasons.append(reason)

    if request.get("transportId") != TRANSPORT_ID:
        reasons.append("TRANSPORT_ID_INVALID")

    return request, reasons


def _receipt(
    *,
    provider_request: Optional[Dict[str, Any]],
    provider_request_id: Optional[str],
    transport_request_id: Optional[str],
    result_json: Optional[str],
    success: bool,
    error_code: Optional[str],
) -> Tuple[Dict[str, Any], str]:
    result_sha = (
        _sha256_upper(result_json.encode("utf-8"))
        if isinstance(result_json, str)
        else None
    )
    receipt = {
        "schema": RECEIPT_SCHEMA,
        "transportId": TRANSPORT_ID,
        "transportRequestId": transport_request_id,
        "providerRequestId": provider_request_id,
        "providerId": None
        if provider_request is None
        else provider_request.get("providerId"),
        "modelId": None
        if provider_request is None
        else provider_request.get("modelId"),
        "attemptId": None
        if provider_request is None
        else provider_request.get("attemptId"),
        "requestFingerprint": None
        if provider_request is None
        else provider_request.get("requestFingerprint"),
        "requestInputSha256": None
        if provider_request is None
        else provider_request.get("inputSha256"),
        "resultStatus": None if result_json is None else "SUCCESS",
        "resultSha256": result_sha,
        "externalNetworkUsed": False,
        "modelInvoked": False,
        "success": success,
        "errorCode": error_code,
    }
    receipt_json = json.dumps(
        receipt,
        separators=(",", ":"),
        ensure_ascii=False,
    )
    return receipt, receipt_json


def send_loopback_v3(
    provider_request_envelope_json: str,
    provider_registration_receipt: Dict[str, Any],
    transport_request_envelope_json: str,
    transport_registration_receipt: Dict[str, Any],
) -> TransportV3Outcome:
    reasons: List[str] = []

    provider_validation = request_contract.validate_provider_request(
        provider_request_envelope_json
    )
    if not provider_validation.valid:
        reasons.extend(provider_validation.reasons)

    try:
        provider_request = json.loads(provider_request_envelope_json)
    except Exception:
        provider_request = None

    if not isinstance(provider_registration_receipt, dict):
        reasons.append("PROVIDER_REGISTRATION_MISSING")
        provider_registration_receipt = {}

    provider_request_id = provider_registration_receipt.get("providerRequestId")
    if (
        provider_registration_receipt.get("schema")
        != PROVIDER_REGISTRATION_SCHEMA
    ):
        reasons.append("PROVIDER_REGISTRATION_SCHEMA_INVALID")
    if not isinstance(provider_request_id, str) or not provider_request_id:
        reasons.append("PROVIDER_REQUEST_ID_MISSING")
    else:
        expected_provider_request_id = _sha256_upper(
            provider_request_envelope_json.encode("utf-8")
        )
        if provider_request_id != expected_provider_request_id:
            reasons.append("PROVIDER_REQUEST_ID_MISMATCH")

    if provider_registration_receipt.get("reason") not in (
        "REGISTERED",
        "SAME_PROVIDER_REQUEST_ALREADY_REGISTERED",
    ):
        reasons.append("PROVIDER_REGISTRATION_NOT_ACTIVE")

    if isinstance(provider_request, dict):
        for req_key, reg_key, reason in (
            ("providerId", "providerId", "PROVIDER_REGISTRATION_PROVIDER_MISMATCH"),
            ("modelId", "modelId", "PROVIDER_REGISTRATION_MODEL_MISMATCH"),
            ("attemptId", "attemptId", "PROVIDER_REGISTRATION_ATTEMPT_MISMATCH"),
            (
                "requestFingerprint",
                "requestFingerprint",
                "PROVIDER_REGISTRATION_FINGERPRINT_MISMATCH",
            ),
            (
                "promptContractVersion",
                "promptContractVersion",
                "PROVIDER_REGISTRATION_PROMPT_MISMATCH",
            ),
            (
                "responseSchema",
                "responseSchema",
                "PROVIDER_REGISTRATION_RESPONSE_SCHEMA_MISMATCH",
            ),
            ("inputSha256", "inputSha256", "PROVIDER_REGISTRATION_INPUT_SHA_MISMATCH"),
        ):
            if provider_request.get(req_key) != provider_registration_receipt.get(
                reg_key
            ):
                reasons.append(reason)

    transport_request, transport_reasons = _transport_request_validation(
        transport_request_envelope_json
    )
    reasons.extend(transport_reasons)

    if not isinstance(transport_registration_receipt, dict):
        reasons.append("TRANSPORT_REGISTRATION_MISSING")
        transport_registration_receipt = {}

    transport_request_id = transport_registration_receipt.get("transportRequestId")
    if (
        transport_registration_receipt.get("schema")
        != TRANSPORT_REGISTRATION_SCHEMA
    ):
        reasons.append("TRANSPORT_REGISTRATION_SCHEMA_INVALID")
    if not isinstance(transport_request_id, str) or not transport_request_id:
        reasons.append("TRANSPORT_REQUEST_ID_MISSING")
    else:
        expected_transport_request_id = _sha256_upper(
            transport_request_envelope_json.encode("utf-8")
        )
        if transport_request_id != expected_transport_request_id:
            reasons.append("TRANSPORT_REQUEST_ID_MISMATCH")

    if transport_registration_receipt.get("reason") not in (
        "REGISTERED",
        "SAME_TRANSPORT_REQUEST_ALREADY_REGISTERED",
    ):
        reasons.append("TRANSPORT_REGISTRATION_NOT_ACTIVE")

    if isinstance(transport_request, dict):
        for req_key, reg_key, reason in (
            ("transportId", "transportId", "TRANSPORT_REGISTRATION_TRANSPORT_MISMATCH"),
            (
                "providerRequestId",
                "providerRequestId",
                "TRANSPORT_REGISTRATION_PROVIDER_REQUEST_MISMATCH",
            ),
            ("providerId", "providerId", "TRANSPORT_REGISTRATION_PROVIDER_MISMATCH"),
            ("modelId", "modelId", "TRANSPORT_REGISTRATION_MODEL_MISMATCH"),
            ("attemptId", "attemptId", "TRANSPORT_REGISTRATION_ATTEMPT_MISMATCH"),
            (
                "requestFingerprint",
                "requestFingerprint",
                "TRANSPORT_REGISTRATION_FINGERPRINT_MISMATCH",
            ),
            (
                "requestInputSha256",
                "requestInputSha256",
                "TRANSPORT_REGISTRATION_INPUT_SHA_MISMATCH",
            ),
        ):
            if transport_request.get(req_key) != transport_registration_receipt.get(
                reg_key
            ):
                reasons.append(reason)

    if isinstance(provider_request, dict) and isinstance(transport_request, dict):
        for provider_key, transport_key, reason in (
            ("providerId", "providerId", "TRANSPORT_PROVIDER_MISMATCH"),
            ("modelId", "modelId", "TRANSPORT_MODEL_MISMATCH"),
            ("attemptId", "attemptId", "TRANSPORT_ATTEMPT_MISMATCH"),
            (
                "requestFingerprint",
                "requestFingerprint",
                "TRANSPORT_FINGERPRINT_MISMATCH",
            ),
            ("inputSha256", "requestInputSha256", "TRANSPORT_INPUT_SHA_MISMATCH"),
        ):
            if provider_request.get(provider_key) != transport_request.get(
                transport_key
            ):
                reasons.append(reason)

        if transport_request.get("providerRequestId") != provider_request_id:
            reasons.append("TRANSPORT_PROVIDER_REQUEST_ID_MISMATCH")

    if reasons:
        receipt, receipt_json = _receipt(
            provider_request=provider_request
            if isinstance(provider_request, dict)
            else None,
            provider_request_id=provider_request_id,
            transport_request_id=transport_request_id,
            result_json=None,
            success=False,
            error_code=reasons[0],
        )
        return TransportV3Outcome(
            False,
            None,
            None,
            receipt_json,
            receipt,
            reasons,
        )

    advisory = {
        "schema": ADVISORY_SCHEMA,
        "requestFingerprint": provider_request["requestFingerprint"],
        "disposition": "KEEP_BASELINE",
    }
    advisory_json = json.dumps(
        advisory,
        separators=(",", ":"),
        ensure_ascii=False,
    )
    advisory_base64 = base64.b64encode(
        advisory_json.encode("utf-8")
    ).decode("ascii")

    result = {
        "schema": RESULT_SCHEMA,
        "providerId": provider_request["providerId"],
        "attemptId": provider_request["attemptId"],
        "requestFingerprint": provider_request["requestFingerprint"],
        "providerRequestId": provider_request_id,
        "transportRequestId": transport_request_id,
        "status": "SUCCESS",
        "advisoryBase64": advisory_base64,
    }
    result_json = json.dumps(
        result,
        separators=(",", ":"),
        ensure_ascii=False,
    )

    receipt, receipt_json = _receipt(
        provider_request=provider_request,
        provider_request_id=provider_request_id,
        transport_request_id=transport_request_id,
        result_json=result_json,
        success=True,
        error_code=None,
    )
    return TransportV3Outcome(
        True,
        result_json,
        result,
        receipt_json,
        receipt,
        [],
    )
