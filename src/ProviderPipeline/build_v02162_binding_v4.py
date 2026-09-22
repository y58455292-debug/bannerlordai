from pathlib import Path

root=Path(r"D:\BannerlordAIResearch\workspace\_PatchStaging\AutonomousOperator_v002162_PolicyBoundResultV4_20260921_2051")
src=(root/"candidate"/"PatrolDefenseProviderTransportReceiptResultBinding.cs").read_text(encoding="utf-8-sig")
v4=src

v4=v4.replace(
    "PatrolDefenseProviderTransportReceiptResultBindingData",
    "PatrolDefenseProviderTransportReceiptResultV4BindingData")
v4=v4.replace(
    "PatrolDefenseProviderTransportReceiptResultBinding",
    "PatrolDefenseProviderTransportReceiptResultV4Binding")
v4=v4.replace(
    "PatrolDefenseProviderResultV3AdmissionData",
    "PatrolDefenseProviderResultV4AdmissionData")
v4=v4.replace(
    "PatrolDefenseProviderResultV3Admission",
    "PatrolDefenseProviderResultV4Admission")
v4=v4.replace(
    "BannerlordAI.PatrolDefenseProviderTransportReceipt.v2",
    "BannerlordAI.PatrolDefenseProviderTransportReceipt.v3")
v4=v4.replace(
    "BannerlordAI.PatrolDefenseProviderResult.v3",
    "BannerlordAI.PatrolDefenseProviderResult.v4")
v4=v4.replace(
    "BannerlordAI.PatrolDefenseProviderTransportReceiptResultBinding.v1",
    "BannerlordAI.PatrolDefenseProviderTransportReceiptResultV4Binding.v1")

# Data field
anchor='''        internal string ResultStatus;
        internal string ResultSha256;
'''
repl='''        internal string ResultStatus;
        internal string ExecutionPolicyId;
        internal string ResultSha256;
'''
if anchor not in v4:
    raise SystemExit("v4 binding data field anchor missing")
v4=v4.replace(anchor,repl,1)

anchor='''        internal bool BindingAccepted;
        internal string TransportReceiptMatchReason;
'''
repl='''        internal bool BindingAccepted;
        internal string TransportReceiptMatchReason;
        internal string ExecutionPolicyMatchReason;
'''
if anchor not in v4:
    raise SystemExit("v4 binding match field anchor missing")
v4=v4.replace(anchor,repl,1)

# Receipt allowed field
anchor='''                    "resultStatus",
                    "resultSha256",
'''
repl='''                    "resultStatus",
                    "executionPolicyId",
                    "resultSha256",
'''
if anchor not in v4:
    raise SystemExit("receipt field anchor missing")
v4=v4.replace(anchor,repl,1)

# Parse receipt field
anchor='''            data.ResultStatus =
                GetString(receipt, "resultStatus");
            data.ResultSha256 =
                GetString(receipt, "resultSha256");
'''
repl='''            data.ResultStatus =
                GetString(receipt, "resultStatus");
            data.ExecutionPolicyId =
                GetString(receipt, "executionPolicyId");
            data.ResultSha256 =
                GetString(receipt, "resultSha256");
'''
if anchor not in v4:
    raise SystemExit("parse receipt exec policy anchor missing")
v4=v4.replace(anchor,repl,1)

# identity missing require exec policy
anchor='''                !Present(data.RequestInputSha256) ||
                !Present(data.ResultStatus) ||
                !Present(data.ResultSha256))
'''
repl='''                !Present(data.RequestInputSha256) ||
                !Present(data.ResultStatus) ||
                !Present(data.ExecutionPolicyId) ||
                !Present(data.ResultSha256))
'''
if anchor not in v4:
    raise SystemExit("identity require anchor missing")
v4=v4.replace(anchor,repl,1)

# result field parse
anchor='''            string resultStatus =
                GetString(result, "status");
'''
repl='''            string resultStatus =
                GetString(result, "status");
            string resultExecutionPolicyId =
                GetString(result, "executionPolicyId");
'''
if anchor not in v4:
    raise SystemExit("result exec parse anchor missing")
v4=v4.replace(anchor,repl,1)

# compare receipt/result exec policy after status compare
anchor='''            if (!string.Equals(
                    data.ResultStatus,
                    resultStatus,
                    StringComparison.Ordinal))
                Reject(data, "RECEIPT_RESULT_STATUS_MISMATCH");

            if (data.RejectionReasons.Count > 0)
'''
repl='''            if (!string.Equals(
                    data.ResultStatus,
                    resultStatus,
                    StringComparison.Ordinal))
                Reject(data, "RECEIPT_RESULT_STATUS_MISMATCH");

            if (!string.Equals(
                    data.ExecutionPolicyId,
                    resultExecutionPolicyId,
                    StringComparison.Ordinal))
                Reject(data, "RECEIPT_RESULT_EXECUTION_POLICY_MISMATCH");

            if (data.RejectionReasons.Count > 0)
'''
if anchor not in v4:
    raise SystemExit("receipt/result exec compare anchor missing")
v4=v4.replace(anchor,repl,1)

# Add execution-policy match after receipt match
anchor='''            if (receiptMatch == null ||
                !receiptMatch.Matched)
            {
                Reject(
                    data,
                    data.TransportReceiptMatchReason);
                return data;
            }

            data.TransportReceiptMatchReason =
                "TRANSPORT_RECEIPT_MATCHED";
            data.BindingAccepted = true;

            data.ProviderResultAdmission =
'''
repl='''            if (receiptMatch == null ||
                !receiptMatch.Matched)
            {
                Reject(
                    data,
                    data.TransportReceiptMatchReason);
                return data;
            }

            data.TransportReceiptMatchReason =
                "TRANSPORT_RECEIPT_MATCHED";

            PatrolDefenseProviderExecutionPolicyMatchResult
                executionPolicyMatch =
                    queue.MatchRegisteredExecutionPolicy(
                        data.RequestFingerprint,
                        data.ProviderId,
                        data.AttemptId,
                        data.ProviderRequestId,
                        data.TransportRequestId,
                        data.ExecutionPolicyId);

            data.ExecutionPolicyMatchReason =
                executionPolicyMatch == null
                    ? "EXECUTION_POLICY_MATCH_UNAVAILABLE"
                    : executionPolicyMatch.Reason;

            if (executionPolicyMatch == null ||
                !executionPolicyMatch.Matched)
            {
                Reject(
                    data,
                    data.ExecutionPolicyMatchReason);
                return data;
            }

            data.ExecutionPolicyMatchReason =
                "EXECUTION_POLICY_MATCHED";
            data.BindingAccepted = true;

            data.ProviderResultAdmission =
'''
if anchor not in v4:
    raise SystemExit("execution policy match insertion anchor missing")
v4=v4.replace(anchor,repl,1)

# Serializer fields
anchor='''            sb.Append(",\\"resultStatus\\":");
            sb.Append(Json(data.ResultStatus));
            sb.Append(",\\"resultSha256\\":");
'''
repl='''            sb.Append(",\\"resultStatus\\":");
            sb.Append(Json(data.ResultStatus));
            sb.Append(",\\"executionPolicyId\\":");
            sb.Append(Json(data.ExecutionPolicyId));
            sb.Append(",\\"resultSha256\\":");
'''
if anchor not in v4:
    raise SystemExit("serializer exec id anchor missing")
v4=v4.replace(anchor,repl,1)

anchor='''            sb.Append(",\\"transportReceiptMatchReason\\":");
            sb.Append(Json(data.TransportReceiptMatchReason));
            sb.Append(",\\"bindingAccepted\\":");
'''
repl='''            sb.Append(",\\"transportReceiptMatchReason\\":");
            sb.Append(Json(data.TransportReceiptMatchReason));
            sb.Append(",\\"executionPolicyMatchReason\\":");
            sb.Append(Json(data.ExecutionPolicyMatchReason));
            sb.Append(",\\"bindingAccepted\\":");
'''
if anchor not in v4:
    raise SystemExit("serializer exec match anchor missing")
v4=v4.replace(anchor,repl,1)

(root/"candidate"/"PatrolDefenseProviderTransportReceiptResultV4Binding.cs").write_text(
    v4,encoding="utf-8")

print("v02162 receipt v3/result v4 binding generated")
