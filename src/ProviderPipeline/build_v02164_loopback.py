from pathlib import Path

src=Path(r"D:\BannerlordAIResearch\workspace\provider_transport_loopback_v02163.py")
dst=Path(r"D:\BannerlordAIResearch\workspace\provider_transport_loopback_v02164.py")
t=src.read_text(encoding="utf-8-sig")

sig='''def send_loopback_v4(
    provider_request_json: str,
    provider_registration: Dict[str, Any],
    transport_request_json: str,
    transport_registration: Dict[str, Any],
    execution_policy_registration: Dict[str, Any],
) -> TransportV4Outcome:
'''
repl='''def send_loopback_v4(
    provider_request_json: str,
    provider_registration: Dict[str, Any],
    transport_request_json: str,
    transport_registration: Dict[str, Any],
    execution_policy_registration: Dict[str, Any],
    transport_execution_authorization: Dict[str, Any],
) -> TransportV4Outcome:
'''
if sig not in t:
    raise SystemExit("loopback signature anchor missing")
t=t.replace(sig,repl,1)

anchor='''    if reasons:
        receipt = {
'''
validation=r'''    if not isinstance(transport_execution_authorization, dict):
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
'''
if anchor not in t:
    raise SystemExit("loopback reasons anchor missing")
t=t.replace(anchor,validation,1)

dst.write_text(t,encoding="utf-8")
print(dst)
