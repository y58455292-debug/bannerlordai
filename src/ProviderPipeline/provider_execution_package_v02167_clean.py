from __future__ import annotations

import base64
import hashlib
import json
from dataclasses import dataclass
from typing import Any, Dict, List, Optional

import provider_request_contract_v02154 as request_contract
import provider_transport_loopback_v02166 as loop

SCHEMA = "BannerlordAI.ProviderExecutionPackage.v1"
FIELDS = {
    "schema",
    "executionPackageId",
    "providerRequestBase64",
    "providerRequestId",
    "providerId",
    "modelId",
    "attemptId",
    "requestFingerprint",
    "promptContractVersion",
    "responseSchema",
    "inputSha256",
    "transportRequestBase64",
    "transportRequestId",
    "transportId",
    "executionPolicyBase64",
    "executionPolicyId",
    "timeoutMs",
    "maxAttempts",
    "maxResultBytes",
    "credentialRef",
    "transportExecutionAuthorizationBase64",
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


@dataclass
class AdapterExecution:
    success: bool
    adapter_invoked: bool
    validation: PackageValidation
    transport_outcome: Optional[loop.TransportV5Outcome]


def _sha(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest().upper()


def _part(value: str) -> bytes:
    raw = value.encode("utf-8")
    return str(len(raw)).encode("ascii") + b":" + raw + b";"


def _auth_id(fp: str, policy_id: str, ordinal: int) -> str:
    return _sha(_part(fp) + _part(policy_id) + _part(str(ordinal)))


def _enc(text: str) -> str:
    return base64.b64encode(text.encode("utf-8")).decode("ascii")


def _dec(value: Any, prefix: str, reasons: List[str]) -> Optional[str]:
    if not isinstance(value, str) or not value:
        reasons.append(prefix + "_BASE64_MISSING")
        return None
    try:
        return base64.b64decode(value, validate=True).decode("utf-8", errors="strict")
    except Exception:
        reasons.append(prefix + "_BASE64_INVALID")
        return None


def _package_id(provider_raw: str, transport_raw: str, policy_raw: str, auth_raw: str) -> str:
    return _sha(
        _part(provider_raw)
        + _part(transport_raw)
        + _part(policy_raw)
        + _part(auth_raw)
    )


def build_execution_package(
    provider_request_json: str,
    transport_request_json: str,
    execution_policy_json: str,
    authorization: Dict[str, Any],
) -> Dict[str, Any]:
    provider = json.loads(provider_request_json)
    transport = json.loads(transport_request_json)
    policy = json.loads(execution_policy_json)
    auth_raw = json.dumps(
        authorization,
        separators=(",", ":"),
        ensure_ascii=False,
        sort_keys=True,
    )
    package = {
        "schema": SCHEMA,
        "executionPackageId": _package_id(
            provider_request_json,
            transport_request_json,
            execution_policy_json,
            auth_raw,
        ),
        "providerRequestBase64": _enc(provider_request_json),
        "providerRequestId": _sha(provider_request_json.encode("utf-8")),
        "providerId": provider.get("providerId"),
        "modelId": provider.get("modelId"),
        "attemptId": provider.get("attemptId"),
        "requestFingerprint": provider.get("requestFingerprint"),
        "promptContractVersion": provider.get("promptContractVersion"),
        "responseSchema": provider.get("responseSchema"),
        "inputSha256": provider.get("inputSha256"),
        "transportRequestBase64": _enc(transport_request_json),
        "transportRequestId": _sha(transport_request_json.encode("utf-8")),
        "transportId": transport.get("transportId"),
        "executionPolicyBase64": _enc(execution_policy_json),
        "executionPolicyId": _sha(execution_policy_json.encode("utf-8")),
        "timeoutMs": policy.get("timeoutMs"),
        "maxAttempts": policy.get("maxAttempts"),
        "maxResultBytes": policy.get("maxResultBytes"),
        "credentialRef": policy.get("credentialRef"),
        "transportExecutionAuthorizationBase64": _enc(auth_raw),
        "transportExecutionAuthorizationId": authorization.get(
            "transportExecutionAuthorizationId"
        ),
        "transportExecutionAttemptOrdinal": authorization.get("attemptOrdinal"),
        "transportExecutionAttemptCount": authorization.get("attemptCount"),
    }
    return package


def validate_execution_package(
    package: Any,
    latest_authorization: Optional[Dict[str, Any]] = None,
) -> PackageValidation:
    reasons: List[str] = []
    if not isinstance(package, dict):
        return PackageValidation(False, ["PACKAGE_NOT_OBJECT"])

    if set(package.keys()) != FIELDS:
        for extra in sorted(set(package.keys()) - FIELDS):
            reasons.append("PACKAGE_UNEXPECTED_FIELD:" + extra)
        for missing in sorted(FIELDS - set(package.keys())):
            reasons.append("PACKAGE_MISSING_FIELD:" + missing)
        return PackageValidation(False, reasons)

    if package.get("schema") != SCHEMA:
        reasons.append("PACKAGE_SCHEMA_INVALID")

    provider_raw = _dec(package.get("providerRequestBase64"), "PROVIDER_REQUEST", reasons)
    transport_raw = _dec(package.get("transportRequestBase64"), "TRANSPORT_REQUEST", reasons)
    policy_raw = _dec(package.get("executionPolicyBase64"), "EXECUTION_POLICY", reasons)
    auth_raw = _dec(
        package.get("transportExecutionAuthorizationBase64"),
        "EXECUTION_AUTHORIZATION",
        reasons,
    )

    provider = transport = policy = auth = None
    for raw, name in (
        (provider_raw, "PROVIDER_REQUEST"),
        (transport_raw, "TRANSPORT_REQUEST"),
        (policy_raw, "EXECUTION_POLICY"),
        (auth_raw, "EXECUTION_AUTHORIZATION"),
    ):
        if raw is None:
            continue
        try:
            parsed = json.loads(raw)
        except Exception:
            reasons.append(name + "_JSON_INVALID")
            continue
        if not isinstance(parsed, dict):
            reasons.append(name + "_NOT_OBJECT")
            continue
        if name == "PROVIDER_REQUEST":
            provider = parsed
        elif name == "TRANSPORT_REQUEST":
            transport = parsed
        elif name == "EXECUTION_POLICY":
            policy = parsed
        else:
            auth = parsed

    if provider_raw is not None:
        result = request_contract.validate_provider_request(provider_raw)
        reasons.extend("PROVIDER_REQUEST_" + x for x in result.reasons)

    if provider is not None and provider_raw is not None:
        expected = {
            "providerRequestId": _sha(provider_raw.encode("utf-8")),
            "providerId": provider.get("providerId"),
            "modelId": provider.get("modelId"),
            "attemptId": provider.get("attemptId"),
            "requestFingerprint": provider.get("requestFingerprint"),
            "promptContractVersion": provider.get("promptContractVersion"),
            "responseSchema": provider.get("responseSchema"),
            "inputSha256": provider.get("inputSha256"),
        }
        for key, value in expected.items():
            if package.get(key) != value:
                reasons.append("PACKAGE_" + key.upper() + "_MISMATCH")

    if transport is not None and transport_raw is not None:
        if package.get("transportRequestId") != _sha(transport_raw.encode("utf-8")):
            reasons.append("TRANSPORT_REQUEST_ID_MISMATCH")
        if package.get("transportId") != transport.get("transportId"):
            reasons.append("TRANSPORT_ID_MISMATCH")
        expected = {
            "providerRequestId": package.get("providerRequestId"),
            "providerId": package.get("providerId"),
            "modelId": package.get("modelId"),
            "attemptId": package.get("attemptId"),
            "requestFingerprint": package.get("requestFingerprint"),
            "requestInputSha256": package.get("inputSha256"),
        }
        for key, value in expected.items():
            if transport.get(key) != value:
                reasons.append("TRANSPORT_" + key.upper() + "_MISMATCH")

    max_attempts = timeout_ms = max_result_bytes = None
    if policy is not None and policy_raw is not None:
        if package.get("executionPolicyId") != _sha(policy_raw.encode("utf-8")):
            reasons.append("EXECUTION_POLICY_ID_MISMATCH")
        expected = {
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
        for key, value in expected.items():
            if policy.get(key) != value:
                reasons.append("EXECUTION_POLICY_" + key.upper() + "_MISMATCH")
        try:
            timeout_ms = int(package.get("timeoutMs"))
            max_attempts = int(package.get("maxAttempts"))
            max_result_bytes = int(package.get("maxResultBytes"))
        except Exception:
            reasons.append("EXECUTION_POLICY_BOUNDS_INVALID")
        else:
            if (
                timeout_ms <= 0
                or max_attempts <= 0
                or max_result_bytes <= 0
                or str(timeout_ms) != str(package.get("timeoutMs"))
                or str(max_attempts) != str(package.get("maxAttempts"))
                or str(max_result_bytes) != str(package.get("maxResultBytes"))
            ):
                reasons.append("EXECUTION_POLICY_BOUNDS_INVALID")
        ref = package.get("credentialRef")
        if not isinstance(ref, str) or not ref.startswith("env:") or len(ref) <= 4:
            reasons.append("CREDENTIAL_REF_INVALID")

    if auth is not None:
        if (
            auth.get("schema")
            != "BannerlordAI.PatrolDefenseProviderTransportExecutionAuthorization.v1"
        ):
            reasons.append("EXECUTION_AUTHORIZATION_SCHEMA_INVALID")
        if auth.get("authorized") is not True:
            reasons.append("EXECUTION_AUTHORIZATION_NOT_AUTHORIZED")
        if auth.get("reason") != "EXECUTION_ATTEMPT_AUTHORIZED":
            reasons.append("EXECUTION_AUTHORIZATION_REASON_INVALID")
        if auth.get("requestFingerprint") != package.get("requestFingerprint"):
            reasons.append("EXECUTION_AUTHORIZATION_FINGERPRINT_MISMATCH")
        if auth.get("executionPolicyId") != package.get("executionPolicyId"):
            reasons.append("EXECUTION_AUTHORIZATION_POLICY_MISMATCH")
        if auth.get("transportExecutionAuthorizationId") != package.get(
            "transportExecutionAuthorizationId"
        ):
            reasons.append("EXECUTION_AUTHORIZATION_ID_MISMATCH")
        if auth.get("attemptOrdinal") != package.get("transportExecutionAttemptOrdinal"):
            reasons.append("EXECUTION_AUTHORIZATION_ORDINAL_MISMATCH")
        if auth.get("attemptCount") != package.get("transportExecutionAttemptCount"):
            reasons.append("EXECUTION_AUTHORIZATION_COUNT_MISMATCH")
        ordinal = auth.get("attemptOrdinal")
        if isinstance(ordinal, int) and ordinal > 0:
            expected_id = _auth_id(
                package.get("requestFingerprint"),
                package.get("executionPolicyId"),
                ordinal,
            )
            if auth.get("transportExecutionAuthorizationId") != expected_id:
                reasons.append("EXECUTION_AUTHORIZATION_ID_DERIVATION_MISMATCH")
        else:
            reasons.append("EXECUTION_AUTHORIZATION_ORDINAL_INVALID")
        if max_attempts is not None and auth.get("maxAttempts") != max_attempts:
            reasons.append("EXECUTION_AUTHORIZATION_MAX_ATTEMPTS_MISMATCH")
        if timeout_ms is not None and auth.get("timeoutMs") != timeout_ms:
            reasons.append("EXECUTION_AUTHORIZATION_TIMEOUT_MISMATCH")
        if auth.get("externalNetworkUsed") is not False or auth.get("modelInvoked") is not False:
            reasons.append("EXECUTION_AUTHORIZATION_ZERO_AUTHORITY_VIOLATION")

    if all(x is not None for x in (provider_raw, transport_raw, policy_raw, auth_raw)):
        expected_package_id = _package_id(
            provider_raw,
            transport_raw,
            policy_raw,
            auth_raw,
        )
        if package.get("executionPackageId") != expected_package_id:
            reasons.append("EXECUTION_PACKAGE_ID_MISMATCH")

    if latest_authorization is not None:
        if package.get("transportExecutionAuthorizationId") != latest_authorization.get(
            "transportExecutionAuthorizationId"
        ):
            reasons.append("LATEST_AUTHORIZATION_ID_MISMATCH")
        if package.get("transportExecutionAttemptOrdinal") != latest_authorization.get(
            "attemptOrdinal"
        ):
            reasons.append("LATEST_AUTHORIZATION_ORDINAL_MISMATCH")

    return PackageValidation(
        not reasons,
        reasons,
        package,
        provider,
        transport,
        policy,
        auth,
    )


class DeterministicProviderAdapter:
    def __init__(self):
        self.invocations = 0

    def execute(
        self,
        package: Dict[str, Any],
        latest_authorization: Dict[str, Any],
    ) -> AdapterExecution:
        validation = validate_execution_package(package, latest_authorization)
        if not validation.valid:
            return AdapterExecution(False, False, validation, None)

        self.invocations += 1
        provider = validation.provider_request or {}
        transport = validation.transport_request or {}
        policy = validation.execution_policy or {}
        auth = validation.authorization or {}

        provider_reg = {
            "schema": "BannerlordAI.PatrolDefenseProviderRequestRegistration.v1",
            "providerRequestId": package["providerRequestId"],
            "providerId": package["providerId"],
            "modelId": package["modelId"],
            "attemptId": package["attemptId"],
            "requestFingerprint": package["requestFingerprint"],
            "reason": "REGISTERED",
        }
        transport_reg = {
            "schema": "BannerlordAI.PatrolDefenseProviderTransportRegistration.v1",
            "transportRequestId": package["transportRequestId"],
            "transportId": package["transportId"],
            "providerRequestId": package["providerRequestId"],
            "providerId": package["providerId"],
            "modelId": package["modelId"],
            "attemptId": package["attemptId"],
            "requestFingerprint": package["requestFingerprint"],
            "reason": "REGISTERED",
        }
        policy_reg = {
            "schema": "BannerlordAI.PatrolDefenseProviderExecutionPolicyRegistration.v1",
            "executionPolicyId": package["executionPolicyId"],
            "transportRequestId": package["transportRequestId"],
            "providerRequestId": package["providerRequestId"],
            "providerId": package["providerId"],
            "modelId": package["modelId"],
            "attemptId": package["attemptId"],
            "requestFingerprint": package["requestFingerprint"],
            "timeoutMs": package["timeoutMs"],
            "maxAttempts": package["maxAttempts"],
            "maxResultBytes": package["maxResultBytes"],
            "credentialRef": package["credentialRef"],
            "reason": "REGISTERED",
        }
        outcome = loop.send_loopback_v5(
            json.dumps(provider, separators=(",", ":"), ensure_ascii=False),
            provider_reg,
            json.dumps(transport, separators=(",", ":"), ensure_ascii=False),
            transport_reg,
            policy_reg,
            auth,
        )
        return AdapterExecution(outcome.success, True, validation, outcome)
