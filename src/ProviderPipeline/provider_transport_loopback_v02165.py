from __future__ import annotations

import base64
import hashlib
import json
from dataclasses import dataclass
from typing import Any, Dict, List, Optional

import provider_request_contract_v02154 as request_contract

TRANSPORT_ID = "deterministic_local_loopback"
RESULT_SCHEMA = "BannerlordAI.PatrolDefenseProviderResult.v4"
RECEIPT_SCHEMA = "BannerlordAI.PatrolDefenseProviderTransportReceipt.v3"
ADVISORY_SCHEMA = "BannerlordAI.PatrolDefenseDeliberationAdvisory.v1"

PROVIDER_REG_SCHEMA = "BannerlordAI.PatrolDefenseProviderRequestRegistration.v1"
TRANSPORT_REG_SCHEMA = "BannerlordAI.PatrolDefenseProviderTransportRegistration.v1"
POLICY_REG_SCHEMA = "BannerlordAI.PatrolDefenseProviderExecutionPolicyRegistration.v1"


@dataclass
class TransportV4Outcome:
    success: bool
    result_json: Optional[str]
    result: Optional[Dict[str, Any]]
    receipt_json: str
    receipt: Dict[str, Any]
    reasons: List[str]


def _sha256_upper(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest().upper()


def send_loopback_v4(
    provider_request_json: str,
    provider_registration: Dict[str, Any],
    transport_request_json: str,
    transport_registration: Dict[str, Any],
    execution_policy_registration: Dict[str, Any],
    transport_execution_authorization: Dict[str, Any],
) -> TransportV4Outcome:
    reasons: List[str] = []

    provider_validation = request_contract.validate_provider_request(
        provider_request_json
    )
    if not provider_validation.valid:
        reasons.extend(provider_validation.reasons)

    try:
        provider_request = json.loads(provider_request_json)
    except Exception:
        provider_request = None
        reasons.append("PROVIDER_REQUEST_MALFORMED_JSON")

    try:
        transport_request = json.loads(transport_request_json)
    except Exception:
        transport_request = None
        reasons.append("TRANSPORT_REQUEST_MALFORMED_JSON")

    for receipt, schema, prefix in (
        (provider_registration, PROVIDER_REG_SCHEMA, "PROVIDER"),
        (transport_registration, TRANSPORT_REG_SCHEMA, "TRANSPORT"),
        (execution_policy_registration, POLICY_REG_SCHEMA, "EXECUTION_POLICY"),
    ):
        if not isinstance(receipt, dict):
            reasons.append(prefix + "_REGISTRATION_MISSING")
            continue
        if receipt.get("schema") != schema:
            reasons.append(prefix + "_REGISTRATION_SCHEMA_INVALID")
        if receipt.get("reason") not in (
            "REGISTERED",
            "SAME_PROVIDER_REQUEST_ALREADY_REGISTERED",
            "SAME_TRANSPORT_REQUEST_ALREADY_REGISTERED",
            "SAME_EXECUTION_POLICY_ALREADY_REGISTERED",
        ):
            reasons.append(prefix + "_REGISTRATION_NOT_ACTIVE")

    provider_request_id = (
        provider_registration.get("providerRequestId")
        if isinstance(provider_registration, dict)
        else None
    )
    transport_request_id = (
        transport_registration.get("transportRequestId")
        if isinstance(transport_registration, dict)
        else None
    )
    execution_policy_id = (
        execution_policy_registration.get("executionPolicyId")
        if isinstance(execution_policy_registration, dict)
        else None
    )

    for value, reason in (
        (provider_request_id, "PROVIDER_REQUEST_ID_MISSING"),
        (transport_request_id, "TRANSPORT_REQUEST_ID_MISSING"),
        (execution_policy_id, "EXECUTION_POLICY_ID_MISSING"),
    ):
        if not isinstance(value, str) or not value:
            reasons.append(reason)

    if isinstance(provider_request, dict):
        expected_provider_request_id = _sha256_upper(
            provider_request_json.encode("utf-8")
        )
        if provider_request_id != expected_provider_request_id:
            reasons.append("PROVIDER_REQUEST_ID_MISMATCH")

    if isinstance(transport_request, dict):
        expected_transport_request_id = _sha256_upper(
            transport_request_json.encode("utf-8")
        )
        if transport_request_id != expected_transport_request_id:
            reasons.append("TRANSPORT_REQUEST_ID_MISMATCH")

    if isinstance(provider_request, dict):
        for key, reg_key, reason in (
            ("providerId", "providerId", "PROVIDER_ID_MISMATCH"),
            ("modelId", "modelId", "MODEL_ID_MISMATCH"),
            ("attemptId", "attemptId", "ATTEMPT_ID_MISMATCH"),
            ("requestFingerprint", "requestFingerprint", "FINGERPRINT_MISMATCH"),
            ("inputSha256", "inputSha256", "REQUEST_INPUT_SHA_MISMATCH"),
        ):
            if provider_request.get(key) != execution_policy_registration.get(
                reg_key if reg_key in execution_policy_registration else key
            ):
                # Policy receipt intentionally has no inputSha256/model aliases beyond its own fields.
                if key not in ("inputSha256",):
                    reasons.append(reason)

    if isinstance(transport_request, dict):
        if transport_request.get("transportId") != TRANSPORT_ID:
            reasons.append("TRANSPORT_ID_INVALID")
        for key, expected, reason in (
            ("providerRequestId", provider_request_id, "TRANSPORT_PROVIDER_REQUEST_MISMATCH"),
            ("providerId", provider_request.get("providerId") if isinstance(provider_request, dict) else None, "TRANSPORT_PROVIDER_MISMATCH"),
            ("modelId", provider_request.get("modelId") if isinstance(provider_request, dict) else None, "TRANSPORT_MODEL_MISMATCH"),
            ("attemptId", provider_request.get("attemptId") if isinstance(provider_request, dict) else None, "TRANSPORT_ATTEMPT_MISMATCH"),
            ("requestFingerprint", provider_request.get("requestFingerprint") if isinstance(provider_request, dict) else None, "TRANSPORT_FINGERPRINT_MISMATCH"),
            ("requestInputSha256", provider_request.get("inputSha256") if isinstance(provider_request, dict) else None, "TRANSPORT_INPUT_SHA_MISMATCH"),
        ):
            if transport_request.get(key) != expected:
                reasons.append(reason)

    max_result_bytes: Optional[int] = None
    policy_timeout_ms: Optional[int] = None
    if isinstance(execution_policy_registration, dict):
        for key, expected, reason in (
            ("transportRequestId", transport_request_id, "POLICY_TRANSPORT_REQUEST_MISMATCH"),
            ("providerRequestId", provider_request_id, "POLICY_PROVIDER_REQUEST_MISMATCH"),
            ("providerId", provider_request.get("providerId") if isinstance(provider_request, dict) else None, "POLICY_PROVIDER_MISMATCH"),
            ("modelId", provider_request.get("modelId") if isinstance(provider_request, dict) else None, "POLICY_MODEL_MISMATCH"),
            ("attemptId", provider_request.get("attemptId") if isinstance(provider_request, dict) else None, "POLICY_ATTEMPT_MISMATCH"),
            ("requestFingerprint", provider_request.get("requestFingerprint") if isinstance(provider_request, dict) else None, "POLICY_FINGERPRINT_MISMATCH"),
        ):
            if execution_policy_registration.get(key) != expected:
                reasons.append(reason)

        raw_timeout = execution_policy_registration.get("timeoutMs")
        try:
            policy_timeout_ms = int(raw_timeout)
        except (TypeError, ValueError):
            reasons.append("EXECUTION_TIMEOUT_POLICY_INVALID")
        else:
            if policy_timeout_ms <= 0 or str(policy_timeout_ms) != str(raw_timeout):
                reasons.append("EXECUTION_TIMEOUT_POLICY_INVALID")

        raw_max = execution_policy_registration.get("maxResultBytes")
        try:
            max_result_bytes = int(raw_max)
        except (TypeError, ValueError):
            reasons.append("RESULT_SIZE_POLICY_INVALID")
        else:
            if max_result_bytes <= 0 or str(max_result_bytes) != str(raw_max):
                reasons.append("RESULT_SIZE_POLICY_INVALID")

    if not isinstance(transport_execution_authorization, dict):
        reasons.append("EXECUTION_AUTHORIZATION_MISSING")
    else:
        if (
            transport_execution_authorization.get("schema")
            != "BannerlordAI.PatrolDefenseProviderTransportExecutionAuthorization.v1"
        ):
            reasons.append("EXECUTION_AUTHORIZATION_SCHEMA_INVALID")
        if transport_execution_authorization.get("authorized") is not True:
            reasons.append("EXECUTION_AUTHORIZATION_NOT_AUTHORIZED")
        if (
            transport_execution_authorization.get("reason")
            != "EXECUTION_ATTEMPT_AUTHORIZED"
        ):
            reasons.append("EXECUTION_AUTHORIZATION_REASON_INVALID")
        auth_id = transport_execution_authorization.get(
            "transportExecutionAuthorizationId"
        )
        if (
            not isinstance(auth_id, str)
            or len(auth_id) != 64
            or any(ch not in "0123456789ABCDEF" for ch in auth_id)
        ):
            reasons.append("EXECUTION_AUTHORIZATION_ID_INVALID")
        if (
            transport_execution_authorization.get("requestFingerprint")
            != (
                provider_request.get("requestFingerprint")
                if isinstance(provider_request, dict)
                else None
            )
        ):
            reasons.append("EXECUTION_AUTHORIZATION_FINGERPRINT_MISMATCH")
        if (
            transport_execution_authorization.get("executionPolicyId")
            != execution_policy_id
        ):
            reasons.append("EXECUTION_AUTHORIZATION_POLICY_MISMATCH")
        if transport_execution_authorization.get("externalNetworkUsed") is not False:
            reasons.append("EXECUTION_AUTHORIZATION_NETWORK_FLAG_INVALID")
        if transport_execution_authorization.get("modelInvoked") is not False:
            reasons.append("EXECUTION_AUTHORIZATION_MODEL_FLAG_INVALID")

        auth_timeout = transport_execution_authorization.get("timeoutMs")
        if not isinstance(auth_timeout, int) or auth_timeout <= 0:
            reasons.append("EXECUTION_AUTHORIZATION_TIMEOUT_INVALID")
        elif policy_timeout_ms is None or auth_timeout != policy_timeout_ms:
            reasons.append("EXECUTION_AUTHORIZATION_TIMEOUT_MISMATCH")

        auth_ordinal = transport_execution_authorization.get("attemptOrdinal")
        auth_count = transport_execution_authorization.get("attemptCount")
        auth_max = transport_execution_authorization.get("maxAttempts")
        try:
            policy_max_attempts = int(
                execution_policy_registration.get("maxAttempts")
            )
        except (TypeError, ValueError):
            policy_max_attempts = None

        if (
            not isinstance(auth_ordinal, int)
            or not isinstance(auth_count, int)
            or not isinstance(auth_max, int)
            or auth_ordinal <= 0
            or auth_count <= 0
            or auth_max <= 0
            or auth_count != auth_ordinal
        ):
            reasons.append("EXECUTION_AUTHORIZATION_ATTEMPT_INVALID")
        elif (
            policy_max_attempts is None
            or auth_max != policy_max_attempts
            or auth_ordinal > auth_max
        ):
            reasons.append("EXECUTION_AUTHORIZATION_ATTEMPT_LIMIT_MISMATCH")

    if reasons:
        receipt = {
            "schema": RECEIPT_SCHEMA,
            "transportId": TRANSPORT_ID,
            "transportRequestId": transport_request_id,
            "providerRequestId": provider_request_id,
            "executionPolicyId": execution_policy_id,
            "providerId": None if provider_request is None else provider_request.get("providerId"),
            "modelId": None if provider_request is None else provider_request.get("modelId"),
            "attemptId": None if provider_request is None else provider_request.get("attemptId"),
            "requestFingerprint": None if provider_request is None else provider_request.get("requestFingerprint"),
            "requestInputSha256": None if provider_request is None else provider_request.get("inputSha256"),
            "resultStatus": None,
            "resultSha256": None,
            "externalNetworkUsed": False,
            "modelInvoked": False,
            "success": False,
            "errorCode": reasons[0],
        }
        return TransportV4Outcome(
            False,
            None,
            None,
            json.dumps(receipt, separators=(",", ":"), ensure_ascii=False),
            receipt,
            reasons,
        )

    advisory = {
        "schema": ADVISORY_SCHEMA,
        "requestFingerprint": provider_request["requestFingerprint"],
        "disposition": "KEEP_BASELINE",
    }
    advisory_json = json.dumps(advisory, separators=(",", ":"), ensure_ascii=False)
    advisory_b64 = base64.b64encode(advisory_json.encode("utf-8")).decode("ascii")

    result = {
        "schema": RESULT_SCHEMA,
        "providerId": provider_request["providerId"],
        "attemptId": provider_request["attemptId"],
        "requestFingerprint": provider_request["requestFingerprint"],
        "providerRequestId": provider_request_id,
        "transportRequestId": transport_request_id,
        "executionPolicyId": execution_policy_id,
        "status": "SUCCESS",
        "advisoryBase64": advisory_b64,
    }
    result_json = json.dumps(result, separators=(",", ":"), ensure_ascii=False)
    result_bytes = result_json.encode("utf-8")
    result_sha = _sha256_upper(result_bytes)

    if max_result_bytes is None or max_result_bytes <= 0:
        receipt = {
            "schema": RECEIPT_SCHEMA,
            "transportId": TRANSPORT_ID,
            "transportRequestId": transport_request_id,
            "providerRequestId": provider_request_id,
            "executionPolicyId": execution_policy_id,
            "providerId": provider_request["providerId"],
            "modelId": provider_request["modelId"],
            "attemptId": provider_request["attemptId"],
            "requestFingerprint": provider_request["requestFingerprint"],
            "requestInputSha256": provider_request["inputSha256"],
            "resultStatus": None,
            "resultSha256": None,
            "externalNetworkUsed": False,
            "modelInvoked": False,
            "success": False,
            "errorCode": "RESULT_SIZE_POLICY_INVALID",
        }
        return TransportV4Outcome(
            False,
            None,
            None,
            json.dumps(receipt, separators=(",", ":"), ensure_ascii=False),
            receipt,
            ["RESULT_SIZE_POLICY_INVALID"],
        )

    if len(result_bytes) > max_result_bytes:
        receipt = {
            "schema": RECEIPT_SCHEMA,
            "transportId": TRANSPORT_ID,
            "transportRequestId": transport_request_id,
            "providerRequestId": provider_request_id,
            "executionPolicyId": execution_policy_id,
            "providerId": provider_request["providerId"],
            "modelId": provider_request["modelId"],
            "attemptId": provider_request["attemptId"],
            "requestFingerprint": provider_request["requestFingerprint"],
            "requestInputSha256": provider_request["inputSha256"],
            "resultStatus": "SUCCESS",
            "resultSha256": result_sha,
            "externalNetworkUsed": False,
            "modelInvoked": False,
            "success": False,
            "errorCode": "RESULT_SIZE_LIMIT_EXCEEDED",
        }
        return TransportV4Outcome(
            False,
            None,
            None,
            json.dumps(receipt, separators=(",", ":"), ensure_ascii=False),
            receipt,
            ["RESULT_SIZE_LIMIT_EXCEEDED"],
        )

    receipt = {
        "schema": RECEIPT_SCHEMA,
        "transportId": TRANSPORT_ID,
        "transportRequestId": transport_request_id,
        "providerRequestId": provider_request_id,
        "executionPolicyId": execution_policy_id,
        "providerId": provider_request["providerId"],
        "modelId": provider_request["modelId"],
        "attemptId": provider_request["attemptId"],
        "requestFingerprint": provider_request["requestFingerprint"],
        "requestInputSha256": provider_request["inputSha256"],
        "resultStatus": "SUCCESS",
        "resultSha256": result_sha,
        "externalNetworkUsed": False,
        "modelInvoked": False,
        "success": True,
        "errorCode": None,
    }
    receipt_json = json.dumps(receipt, separators=(",", ":"), ensure_ascii=False)

    return TransportV4Outcome(
        True,
        result_json,
        result,
        receipt_json,
        receipt,
        [],
    )
