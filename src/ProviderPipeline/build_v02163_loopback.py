from pathlib import Path

src=Path(r"D:\BannerlordAIResearch\workspace\provider_transport_loopback_v02162.py")
dst=Path(r"D:\BannerlordAIResearch\workspace\provider_transport_loopback_v02163.py")
t=src.read_text(encoding="utf-8-sig")

# Add maxResultBytes policy validation before the existing reasons gate.
anchor='''    if isinstance(execution_policy_registration, dict):
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

    if reasons:
'''
repl='''    max_result_bytes: Optional[int] = None
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

        raw_max = execution_policy_registration.get("maxResultBytes")
        try:
            max_result_bytes = int(raw_max)
        except (TypeError, ValueError):
            reasons.append("RESULT_SIZE_POLICY_INVALID")
        else:
            if max_result_bytes <= 0 or str(max_result_bytes) != str(raw_max):
                reasons.append("RESULT_SIZE_POLICY_INVALID")

    if reasons:
'''
if anchor not in t:
    raise SystemExit("loopback max policy anchor missing")
t=t.replace(anchor,repl,1)

# Insert oversize rejection after result bytes/hash are computed.
anchor='''    result_json = json.dumps(result, separators=(",", ":"), ensure_ascii=False)
    result_sha = _sha256_upper(result_json.encode("utf-8"))

    receipt = {
'''
repl='''    result_json = json.dumps(result, separators=(",", ":"), ensure_ascii=False)
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
'''
if anchor not in t:
    raise SystemExit("loopback result size insertion anchor missing")
t=t.replace(anchor,repl,1)

dst.write_text(t,encoding="utf-8")
print(dst)
