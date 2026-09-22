from pathlib import Path

src=Path(r"D:\BannerlordAIResearch\workspace\provider_transport_loopback_v02165.py")
dst=Path(r"D:\BannerlordAIResearch\workspace\provider_transport_loopback_v02166.py")
t=src.read_text(encoding="utf-8-sig")
t=t.replace('RESULT_SCHEMA = "BannerlordAI.PatrolDefenseProviderResult.v4"', 'RESULT_SCHEMA = "BannerlordAI.PatrolDefenseProviderResult.v5"')
t=t.replace('RECEIPT_SCHEMA = "BannerlordAI.PatrolDefenseProviderTransportReceipt.v3"', 'RECEIPT_SCHEMA = "BannerlordAI.PatrolDefenseProviderTransportReceipt.v4"')
t=t.replace("TransportV4Outcome", "TransportV5Outcome")
t=t.replace("send_loopback_v4", "send_loopback_v5")

anchor='''def _sha256_upper(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest().upper()
'''
extra=anchor+'''

def _authorization_id(request_fingerprint: str, execution_policy_id: str, ordinal: int) -> str:
    def part(value: str) -> str:
        raw=value.encode("utf-8")
        return f"{len(raw)}:{value};"
    material=(
        part(request_fingerprint)
        + part(execution_policy_id)
        + part(str(ordinal))
    )
    return _sha256_upper(material.encode("utf-8"))
'''
if "_authorization_id(" not in t:
    if anchor not in t: raise SystemExit("sha anchor missing")
    t=t.replace(anchor,extra,1)

anchor='''        if (
            not isinstance(auth_id, str)
            or len(auth_id) != 64
            or any(ch not in "0123456789ABCDEF" for ch in auth_id)
        ):
            reasons.append("EXECUTION_AUTHORIZATION_ID_INVALID")
'''
repl=anchor+'''        auth_ordinal_for_id = transport_execution_authorization.get("attemptOrdinal")
        if (
            isinstance(auth_id, str)
            and isinstance(auth_ordinal_for_id, int)
            and isinstance(execution_policy_id, str)
            and isinstance(provider_request, dict)
            and auth_id != _authorization_id(
                provider_request.get("requestFingerprint"),
                execution_policy_id,
                auth_ordinal_for_id,
            )
        ):
            reasons.append("EXECUTION_AUTHORIZATION_ID_MISMATCH")
'''
if "auth_ordinal_for_id" not in t:
    if anchor not in t: raise SystemExit("auth id anchor missing")
    t=t.replace(anchor,repl,1)

needle='''        "executionPolicyId": execution_policy_id,
'''
replacement='''        "executionPolicyId": execution_policy_id,
        "transportExecutionAuthorizationId": (
            transport_execution_authorization.get("transportExecutionAuthorizationId")
            if isinstance(transport_execution_authorization, dict)
            else None
        ),
        "transportExecutionAttemptOrdinal": (
            str(transport_execution_authorization.get("attemptOrdinal"))
            if isinstance(transport_execution_authorization, dict)
            and isinstance(transport_execution_authorization.get("attemptOrdinal"), int)
            else None
        ),
'''
t=t.replace(needle,replacement)

dst.write_text(t,encoding="utf-8")
print(dst)
