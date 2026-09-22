from __future__ import annotations

import base64
import hashlib
import json
from dataclasses import dataclass
from typing import Any, Dict, List, Optional, Tuple

import provider_request_contract_v02154 as request_contract

TRANSPORT_ID = "deterministic_local_loopback"
RECEIPT_SCHEMA = "BannerlordAI.PatrolDefenseProviderTransportReceipt.v1"
RESULT_SCHEMA = "BannerlordAI.PatrolDefenseProviderResult.v2"
ADVISORY_SCHEMA = "BannerlordAI.PatrolDefenseDeliberationAdvisory.v1"


@dataclass
class TransportOutcome:
    success: bool
    result_json: Optional[str]
    result: Optional[Dict[str, Any]]
    receipt_json: str
    receipt: Dict[str, Any]
    reasons: List[str]


def _sha256_upper(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest().upper()


def _receipt(
    *,
    provider_request_id: Optional[str],
    request: Optional[Dict[str, Any]],
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
        "providerRequestId": provider_request_id,
        "providerId": None if request is None else request.get("providerId"),
        "modelId": None if request is None else request.get("modelId"),
        "attemptId": None if request is None else request.get("attemptId"),
        "requestFingerprint": None
        if request is None
        else request.get("requestFingerprint"),
        "requestInputSha256": None
        if request is None
        else request.get("inputSha256"),
        "resultStatus": None
        if result_json is None
        else "SUCCESS",
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


def send_loopback(
    provider_request_envelope_json: str,
    registration_receipt: Dict[str, Any],
) -> TransportOutcome:
    reasons: List[str] = []

    validation = request_contract.validate_provider_request(
        provider_request_envelope_json
    )
    if not validation.valid:
        reasons.extend(validation.reasons)

    try:
        request = json.loads(provider_request_envelope_json)
    except Exception:
        request = None

    if not isinstance(registration_receipt, dict):
        reasons.append("REGISTRATION_MISSING")
        registration_receipt = {}

    provider_request_id = registration_receipt.get("providerRequestId")
    if not isinstance(provider_request_id, str) or not provider_request_id:
        reasons.append("PROVIDER_REQUEST_ID_MISSING")
    else:
        computed_request_id = _sha256_upper(
            provider_request_envelope_json.encode("utf-8")
        )
        if provider_request_id != computed_request_id:
            reasons.append("PROVIDER_REQUEST_ID_MISMATCH")

    if isinstance(request, dict):
        for request_key, registration_key, reason in (
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
            (
                "responseSchema",
                "responseSchema",
                "REGISTRATION_RESPONSE_SCHEMA_MISMATCH",
            ),
            ("inputSha256", "inputSha256", "REGISTRATION_INPUT_SHA_MISMATCH"),
        ):
            if request.get(request_key) != registration_receipt.get(
                registration_key
            ):
                reasons.append(reason)

    if registration_receipt.get("reason") not in (
        "REGISTERED",
        "SAME_PROVIDER_REQUEST_ALREADY_REGISTERED",
    ):
        reasons.append("REGISTRATION_NOT_ACTIVE")

    if reasons:
        receipt, receipt_json = _receipt(
            provider_request_id=provider_request_id,
            request=request if isinstance(request, dict) else None,
            result_json=None,
            success=False,
            error_code=reasons[0],
        )
        return TransportOutcome(
            False,
            None,
            None,
            receipt_json,
            receipt,
            reasons,
        )

    advisory = {
        "schema": ADVISORY_SCHEMA,
        "requestFingerprint": request["requestFingerprint"],
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
        "providerId": request["providerId"],
        "attemptId": request["attemptId"],
        "requestFingerprint": request["requestFingerprint"],
        "providerRequestId": provider_request_id,
        "status": "SUCCESS",
        "advisoryBase64": advisory_base64,
    }
    result_json = json.dumps(
        result,
        separators=(",", ":"),
        ensure_ascii=False,
    )

    receipt, receipt_json = _receipt(
        provider_request_id=provider_request_id,
        request=request,
        result_json=result_json,
        success=True,
        error_code=None,
    )
    return TransportOutcome(
        True,
        result_json,
        result,
        receipt_json,
        receipt,
        [],
    )
