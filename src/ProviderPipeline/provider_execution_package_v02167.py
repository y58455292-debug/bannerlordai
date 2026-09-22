from __future__ import annotations

import base64
import hashlib
import json
from dataclasses import dataclass
from typing import Any, Dict, List, Optional, Tuple

import provider_request_contract_v02154 as request_contract
import provider_transport_loopback_v02166 as loop


PACKAGE_SCHEMA = "BannerlordAI.ProviderExecutionPackage.v1"
POLICY_SCHEMA = "BannerlordAI.PatrolDefenseProviderExecutionPolicy.v1"
TRANSPORT_SCHEMA = "BannerlordAI.PatrolDefenseProviderTransportRequest.v1"
AUTH_SCHEMA = "BannerlordAI.PatrolDefenseProviderTransportExecutionAuthorization.v1"

ALLOWED_FIELDS = {
    "schema",
    "executionPackageId",
    "providerRequestJsonBase64",
    "providerRequestId",
    "providerId",
    "modelId",
    "attemptId",
    "requestFingerprint",
    "promptContractVersion",
    "responseSchema",
    "inputSha256",
    "transportRequestJsonBase64",
    "transportRequestId",
    "transportId",
    "executionPolicyJsonBase64",
    "executionPolicyId",
    "timeoutMs",
    "maxAttempts",
    "maxResultBytes",
    "credentialRef",
    "transportExecutionAuthorizationJsonBase64",
    "transportExecutionAuthorizationId",
    "transportExecutionAttemptOrdinal",
    "transportExecutionAttemptCount",
}


@dataclass
class PackageValidation:
    valid: bool
    reasons: List[str]
    package: Optional[Dict[str, Any]] = None
    provider_request: Optional[Dict[str, Any]] = None
    transport_request: Optional[Dict[str, Any]] = None
    execution_policy: Optional[Dict[str, Any]] = None
    authorization: Optional[Dict[str, Any]] = None
    provider_request_json: Optional[str] = None
    transport_request_json: Optional[str] = None
    execution_policy_json: Optional[str] = None
    authorization_json: Optional[str] = None


@dataclass
class AdapterExecution:
    invoked: bool
    validation: PackageValidation
    outcome: Optional[loop.TransportV5Outcome]


def _sha(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest().upper()


def _part(raw: bytes) -> bytes:
    return str(len(raw)).encode("ascii") + b":" + raw + b";"


def _package_id(
    provider: bytes,
    transport: bytes,
    policy: bytes,
    authorization: bytes,
) -> str:
    material = (
        _part(b"provider")
        + _part(provider)
        + _part(b"transport")
        + _part(transport)
        + _part(b"policy")
        + _part(policy)
        + _part(b"authorization")
        + _part(authorization)
    )
    return _sha(material)


def _authorization_id(
    request_fingerprint: str,
    execution_policy_id: str,
    attempt_ordinal: int,
) -> str:
    material = (
        _part(request_fingerprint.encode("utf-8"))
        + _part(execution_policy_id.encode("utf-8"))
        + _part(str(attempt_ordinal).encode("utf-8"))
    )
    return _sha(material)


def _enc(text: str) -> str:
    return base64.b64encode(text.encode("utf-8")).decode("ascii")


def _decode(
    value: Any,
    label: str,
    reasons: List[str],
) -> Tuple[Optional[str], Optional[Dict[str, Any]]]:
    if not isinstance(value, str) or not value:
        reasons.append(label + "_MISSING")
        return None, None

    try:
        text = base64.b64decode(value, validate=True).decode("utf-8")
        obj = json.loads(text)
    except Exception:
        reasons.append(label + "_INVALID")
        return None, None

    if not isinstance(obj, dict):
        reasons.append(label + "_NOT_OBJECT")
        return None, None

    return text, obj


def build_execution_package(
    provider_request_json: str,
    transport_request_json: str,
    execution_policy_json: str,
    authorization: Dict[str, Any],
) -> Tuple[Dict[str, Any], str]:
    provider = json.loads(provider_request_json)
    transport = json.loads(transport_request_json)
    policy = json.loads(execution_policy_json)
    authorization_json = json.dumps(
        authorization,
        separators=(",", ":"),
        ensure_ascii=False,
    )

    provider_bytes = provider_request_json.encode("utf-8")
    transport_bytes = transport_request_json.encode("utf-8")
    policy_bytes = execution_policy_json.encode("utf-8")
    authorization_bytes = authorization_json.encode("utf-8")

    package = {
        "schema": PACKAGE_SCHEMA,
        "executionPackageId": _package_id(
            provider_bytes,
            transport_bytes,
            policy_bytes,
            authorization_bytes,
        ),
        "providerRequestJsonBase64": _enc(provider_request_json),
        "providerRequestId": _sha(provider_bytes),
        "providerId": provider.get("providerId"),
        "modelId": provider.get("modelId"),
        "attemptId": provider.get("attemptId"),
        "requestFingerprint": provider.get("requestFingerprint"),
        "promptContractVersion": provider.get("promptContractVersion"),
        "responseSchema": provider.get("responseSchema"),
        "inputSha256": provider.get("inputSha256"),
        "transportRequestJsonBase64": _enc(transport_request_json),
        "transportRequestId": _sha(transport_bytes),
        "transportId": transport.get("transportId"),
        "executionPolicyJsonBase64": _enc(execution_policy_json),
        "executionPolicyId": _sha(policy_bytes),
        "timeoutMs": policy.get("timeoutMs"),
        "maxAttempts": policy.get("maxAttempts"),
        "maxResultBytes": policy.get("maxResultBytes"),
        "credentialRef": policy.get("credentialRef"),
        "transportExecutionAuthorizationJsonBase64": _enc(
            authorization_json
        ),
        "transportExecutionAuthorizationId": authorization.get(
            "transportExecutionAuthorizationId"
        ),
        "transportExecutionAttemptOrdinal": authorization.get(
            "attemptOrdinal"
        ),
        "transportExecutionAttemptCount": authorization.get(
            "attemptCount"
        ),
    }
    package_json = json.dumps(
        package,
        separators=(",", ":"),
        ensure_ascii=False,
    )
    return package, package_json


def validate_execution_package(
    package_json: str,
    latest_authorization: Optional[Dict[str, Any]] = None,
) -> PackageValidation:
    reasons: List[str] = []

    try:
        package = json.loads(package_json)
    except Exception:
        return PackageValidation(
            valid=False,
            reasons=["PACKAGE_MALFORMED_JSON"],
        )

    if not isinstance(package, dict):
        return PackageValidation(
            valid=False,
            reasons=["PACKAGE_NOT_OBJECT"],
        )

    for key in sorted(set(package) - ALLOWED_FIELDS):
        reasons.append("PACKAGE_UNEXPECTED_FIELD:" + key)
    for key in sorted(ALLOWED_FIELDS - set(package)):
        reasons.append("PACKAGE_MISSING_FIELD:" + key)

    if package.get("schema") != PACKAGE_SCHEMA:
        reasons.append("PACKAGE_SCHEMA_INVALID")

    provider_raw, provider = _decode(
        package.get("providerRequestJsonBase64"),
        "PROVIDER_REQUEST_BYTES",
        reasons,
    )
    transport_raw, transport = _decode(
        package.get("transportRequestJsonBase64"),
        "TRANSPORT_REQUEST_BYTES",
        reasons,
    )
    policy_raw, policy = _decode(
        package.get("executionPolicyJsonBase64"),
        "EXECUTION_POLICY_BYTES",
        reasons,
    )
    authorization_raw, authorization = _decode(
        package.get("transportExecutionAuthorizationJsonBase64"),
        "EXECUTION_AUTHORIZATION_BYTES",
        reasons,
    )

    if provider_raw is not None:
        provider_validation = request_contract.validate_provider_request(
            provider_raw
        )
        reasons.extend(
            "PROVIDER_REQUEST:" + reason
            for reason in provider_validation.reasons
        )
        provider_request_id = _sha(provider_raw.encode("utf-8"))
        if package.get("providerRequestId") != provider_request_id:
            reasons.append("PROVIDER_REQUEST_ID_MISMATCH")

    if transport_raw is not None:
        transport_request_id = _sha(transport_raw.encode("utf-8"))
        if package.get("transportRequestId") != transport_request_id:
            reasons.append("TRANSPORT_REQUEST_ID_MISMATCH")
        if (
            transport is not None
            and transport.get("schema") != TRANSPORT_SCHEMA
        ):
            reasons.append("TRANSPORT_SCHEMA_INVALID")

    if policy_raw is not None:
        execution_policy_id = _sha(policy_raw.encode("utf-8"))
        if package.get("executionPolicyId") != execution_policy_id:
            reasons.append("EXECUTION_POLICY_ID_MISMATCH")
        if (
            policy is not None
            and policy.get("schema") != POLICY_SCHEMA
        ):
            reasons.append("EXECUTION_POLICY_SCHEMA_INVALID")

    if (
        authorization is not None
        and authorization.get("schema") != AUTH_SCHEMA
    ):
        reasons.append("EXECUTION_AUTHORIZATION_SCHEMA_INVALID")

    if (
        provider is not None
        and transport is not None
        and policy is not None
        and authorization is not None
    ):
        package_identity = {
            "providerId": provider.get("providerId"),
            "modelId": provider.get("modelId"),
            "attemptId": provider.get("attemptId"),
            "requestFingerprint": provider.get("requestFingerprint"),
            "promptContractVersion": provider.get(
                "promptContractVersion"
            ),
            "responseSchema": provider.get("responseSchema"),
            "inputSha256": provider.get("inputSha256"),
        }
        for key, expected in package_identity.items():
            if package.get(key) != expected:
                reasons.append(
                    "PACKAGE_" + key.upper() + "_MISMATCH"
                )

        if package.get("transportId") != transport.get("transportId"):
            reasons.append("PACKAGE_TRANSPORT_ID_MISMATCH")

        transport_identity = {
            "providerRequestId": package.get("providerRequestId"),
            "providerId": package.get("providerId"),
            "modelId": package.get("modelId"),
            "attemptId": package.get("attemptId"),
            "requestFingerprint": package.get("requestFingerprint"),
            "requestInputSha256": package.get("inputSha256"),
        }
        for key, expected in transport_identity.items():
            if transport.get(key) != expected:
                reasons.append(
                    "TRANSPORT_" + key.upper() + "_MISMATCH"
                )

        policy_identity = {
            "transportRequestId": package.get("transportRequestId"),
            "providerRequestId": package.get("providerRequestId"),
            "providerId": package.get("providerId"),
            "modelId": package.get("modelId"),
            "attemptId": package.get("attemptId"),
            "requestFingerprint": package.get("requestFingerprint"),
            "timeoutMs": package.get("timeoutMs"),
            "maxAttempts": package.get("maxAttempts"),
            "maxResultBytes": package.get("maxResultBytes"),
            "credentialRef": package.get("credentialRef"),
        }
        for key, expected in policy_identity.items():
            if policy.get(key) != expected:
                reasons.append(
                    "EXECUTION_POLICY_" + key.upper() + "_MISMATCH"
                )

        if (
            authorization.get("authorized") is not True
            or authorization.get("reason")
            != "EXECUTION_ATTEMPT_AUTHORIZED"
        ):
            reasons.append("EXECUTION_AUTHORIZATION_NOT_ACTIVE")

        if (
            authorization.get("requestFingerprint")
            != package.get("requestFingerprint")
        ):
            reasons.append(
                "EXECUTION_AUTHORIZATION_FINGERPRINT_MISMATCH"
            )

        if (
            authorization.get("executionPolicyId")
            != package.get("executionPolicyId")
        ):
            reasons.append(
                "EXECUTION_AUTHORIZATION_POLICY_MISMATCH"
            )

        if (
            authorization.get("transportExecutionAuthorizationId")
            != package.get("transportExecutionAuthorizationId")
        ):
            reasons.append("EXECUTION_AUTHORIZATION_ID_MISMATCH")

        if (
            authorization.get("attemptOrdinal")
            != package.get("transportExecutionAttemptOrdinal")
        ):
            reasons.append(
                "EXECUTION_AUTHORIZATION_ORDINAL_MISMATCH"
            )

        if (
            authorization.get("attemptCount")
            != package.get("transportExecutionAttemptCount")
        ):
            reasons.append(
                "EXECUTION_AUTHORIZATION_COUNT_MISMATCH"
            )

        ordinal = authorization.get("attemptOrdinal")
        count = authorization.get("attemptCount")
        if (
            not isinstance(ordinal, int)
            or not isinstance(count, int)
            or ordinal <= 0
            or count != ordinal
        ):
            reasons.append(
                "EXECUTION_AUTHORIZATION_ATTEMPT_INVALID"
            )
        else:
            expected_authorization_id = _authorization_id(
                package.get("requestFingerprint"),
                package.get("executionPolicyId"),
                ordinal,
            )
            if (
                authorization.get(
                    "transportExecutionAuthorizationId"
                )
                != expected_authorization_id
            ):
                reasons.append(
                    "EXECUTION_AUTHORIZATION_ID_MISMATCH"
                )

        try:
            max_attempts = int(package.get("maxAttempts"))
            timeout_ms = int(package.get("timeoutMs"))
            max_result_bytes = int(package.get("maxResultBytes"))
        except Exception:
            max_attempts = -1
            timeout_ms = -1
            max_result_bytes = -1
            reasons.append("EXECUTION_POLICY_BOUNDS_INVALID")

        if (
            max_attempts <= 0
            or timeout_ms <= 0
            or max_result_bytes <= 0
        ):
            reasons.append("EXECUTION_POLICY_BOUNDS_NONPOSITIVE")

        if authorization.get("maxAttempts") != max_attempts:
            reasons.append(
                "EXECUTION_AUTHORIZATION_MAX_ATTEMPTS_MISMATCH"
            )

        if authorization.get("timeoutMs") != timeout_ms:
            reasons.append(
                "EXECUTION_AUTHORIZATION_TIMEOUT_MISMATCH"
            )

        if (
            authorization.get("externalNetworkUsed") is not False
            or authorization.get("modelInvoked") is not False
        ):
            reasons.append(
                "EXECUTION_AUTHORIZATION_ZERO_AUTHORITY_VIOLATION"
            )

    if all(
        value is not None
        for value in (
            provider_raw,
            transport_raw,
            policy_raw,
            authorization_raw,
        )
    ):
        expected_package_id = _package_id(
            provider_raw.encode("utf-8"),
            transport_raw.encode("utf-8"),
            policy_raw.encode("utf-8"),
            authorization_raw.encode("utf-8"),
        )
        if package.get("executionPackageId") != expected_package_id:
            reasons.append("EXECUTION_PACKAGE_ID_MISMATCH")

    if latest_authorization is not None:
        if (
            package.get("transportExecutionAuthorizationId")
            != latest_authorization.get(
                "transportExecutionAuthorizationId"
            )
        ):
            reasons.append("LATEST_AUTHORIZATION_ID_MISMATCH")

        if (
            package.get("transportExecutionAttemptOrdinal")
            != latest_authorization.get("attemptOrdinal")
        ):
            reasons.append(
                "LATEST_AUTHORIZATION_ORDINAL_MISMATCH"
            )

        if (
            package.get("transportExecutionAttemptCount")
            != latest_authorization.get("attemptCount")
        ):
            reasons.append(
                "LATEST_AUTHORIZATION_COUNT_MISMATCH"
            )

    return PackageValidation(
        valid=not reasons,
        reasons=reasons,
        package=package,
        provider_request=provider,
        transport_request=transport,
        execution_policy=policy,
        authorization=authorization,
        provider_request_json=provider_raw,
        transport_request_json=transport_raw,
        execution_policy_json=policy_raw,
        authorization_json=authorization_raw,
    )


class DeterministicProviderAdapter:
    def __init__(self) -> None:
        self.invocation_count = 0

    def execute(
        self,
        package_json: str,
        latest_authorization: Dict[str, Any],
    ) -> AdapterExecution:
        validation = validate_execution_package(
            package_json,
            latest_authorization,
        )
        if not validation.valid:
            return AdapterExecution(
                invoked=False,
                validation=validation,
                outcome=None,
            )

        self.invocation_count += 1

        package = validation.package or {}
        authorization = validation.authorization or {}

        provider_registration = {
            "schema": (
                "BannerlordAI."
                "PatrolDefenseProviderRequestRegistration.v1"
            ),
            "providerRequestId": package.get("providerRequestId"),
            "providerId": package.get("providerId"),
            "modelId": package.get("modelId"),
            "attemptId": package.get("attemptId"),
            "requestFingerprint": package.get(
                "requestFingerprint"
            ),
            "reason": "REGISTERED",
        }

        transport_registration = {
            "schema": (
                "BannerlordAI."
                "PatrolDefenseProviderTransportRegistration.v1"
            ),
            "transportRequestId": package.get(
                "transportRequestId"
            ),
            "transportId": package.get("transportId"),
            "providerRequestId": package.get(
                "providerRequestId"
            ),
            "providerId": package.get("providerId"),
            "modelId": package.get("modelId"),
            "attemptId": package.get("attemptId"),
            "requestFingerprint": package.get(
                "requestFingerprint"
            ),
            "reason": "REGISTERED",
        }

        policy_registration = {
            "schema": (
                "BannerlordAI."
                "PatrolDefenseProviderExecutionPolicyRegistration.v1"
            ),
            "executionPolicyId": package.get(
                "executionPolicyId"
            ),
            "transportRequestId": package.get(
                "transportRequestId"
            ),
            "providerRequestId": package.get(
                "providerRequestId"
            ),
            "providerId": package.get("providerId"),
            "modelId": package.get("modelId"),
            "attemptId": package.get("attemptId"),
            "requestFingerprint": package.get(
                "requestFingerprint"
            ),
            "timeoutMs": package.get("timeoutMs"),
            "maxAttempts": package.get("maxAttempts"),
            "maxResultBytes": package.get("maxResultBytes"),
            "credentialRef": package.get("credentialRef"),
            "reason": "REGISTERED",
        }

        outcome = loop.send_loopback_v5(
            validation.provider_request_json or "",
            provider_registration,
            validation.transport_request_json or "",
            transport_registration,
            policy_registration,
            authorization,
        )

        return AdapterExecution(
            invoked=True,
            validation=validation,
            outcome=outcome,
        )
