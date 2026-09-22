from pathlib import Path

root=Path(r"D:\BannerlordAIResearch\workspace\_PatchStaging\AutonomousOperator_v002163_ResultSizePolicy_20260921_2208")
dispatch=root/"candidate"/"PatrolDefenseProviderDispatch.cs"
t=dispatch.read_text(encoding="utf-8-sig")

type_anchor='''    internal sealed class PatrolDefenseProviderExecutionPolicyMatchResult
    {
        internal string RequestFingerprint;
        internal string ProviderId;
        internal string ModelId;
        internal string AttemptId;
        internal string ProviderRequestId;
        internal string TransportRequestId;
        internal string ExecutionPolicyId;
        internal string RegisteredExecutionPolicyId;
        internal bool Matched;
        internal string Reason;
    }

'''
type_repl=type_anchor+'''    internal sealed class PatrolDefenseProviderResultSizePolicyResult
    {
        internal string RequestFingerprint;
        internal string ExecutionPolicyId;
        internal int ResultByteCount;
        internal int MaxResultBytes;
        internal bool Allowed;
        internal string Reason;
    }

'''
if "PatrolDefenseProviderResultSizePolicyResult" not in t:
    if type_anchor not in t:
        raise SystemExit("result size type anchor missing")
    t=t.replace(type_anchor,type_repl,1)

method_anchor='''        internal PatrolDefenseProviderExecutionPolicyRegistrationResult
            RegisterExecutionPolicy(
'''
method='''        internal PatrolDefenseProviderResultSizePolicyResult
            EvaluateRegisteredResultSizePolicy(
                string requestFingerprint,
                string executionPolicyId,
                int resultByteCount)
        {
            PatrolDefenseProviderResultSizePolicyResult result =
                new PatrolDefenseProviderResultSizePolicyResult();

            result.RequestFingerprint = requestFingerprint;
            result.ExecutionPolicyId = executionPolicyId;
            result.ResultByteCount = resultByteCount;

            if (string.IsNullOrWhiteSpace(requestFingerprint) ||
                string.IsNullOrWhiteSpace(executionPolicyId) ||
                resultByteCount < 0)
            {
                result.Allowed = false;
                result.Reason = "RESULT_SIZE_IDENTITY_INVALID";
                return result;
            }

            PatrolDefenseProviderDispatchJob job;
            if (!_pending.TryGetValue(
                    requestFingerprint,
                    out job))
            {
                result.Allowed = false;
                result.Reason = "DISPATCH_JOB_NOT_FOUND";
                return result;
            }

            if (string.IsNullOrWhiteSpace(
                    job.ExecutionPolicyId))
            {
                result.Allowed = false;
                result.Reason =
                    "EXECUTION_POLICY_NOT_REGISTERED";
                return result;
            }

            if (!string.Equals(
                    job.ExecutionPolicyId,
                    executionPolicyId,
                    StringComparison.Ordinal))
            {
                result.Allowed = false;
                result.Reason =
                    "EXECUTION_POLICY_ID_MISMATCH";
                return result;
            }

            int maxResultBytes;
            if (!int.TryParse(
                    job.ExecutionPolicyMaxResultBytes,
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out maxResultBytes) ||
                maxResultBytes <= 0)
            {
                result.Allowed = false;
                result.Reason =
                    "RESULT_SIZE_POLICY_INVALID";
                return result;
            }

            result.MaxResultBytes =
                maxResultBytes;

            if (resultByteCount >
                maxResultBytes)
            {
                result.Allowed = false;
                result.Reason =
                    "RESULT_SIZE_LIMIT_EXCEEDED";
                return result;
            }

            result.Allowed = true;
            result.Reason =
                "RESULT_SIZE_WITHIN_LIMIT";
            return result;
        }


'''
if "EvaluateRegisteredResultSizePolicy(" not in t:
    if method_anchor not in t:
        raise SystemExit("result size method anchor missing")
    t=t.replace(method_anchor,method+method_anchor,1)

dispatch.write_text(t,encoding="utf-8")

binding=root/"candidate"/"PatrolDefenseProviderTransportReceiptResultV4Binding.cs"
b=binding.read_text(encoding="utf-8-sig")

field_anchor='''        internal bool BindingAccepted;
        internal string TransportReceiptMatchReason;
        internal string ExecutionPolicyMatchReason;
'''
field_repl='''        internal bool BindingAccepted;
        internal string TransportReceiptMatchReason;
        internal string ExecutionPolicyMatchReason;
        internal int ResultByteCount;
        internal int MaxResultBytes;
        internal string ResultSizePolicyReason;
'''
if "internal string ResultSizePolicyReason;" not in b:
    if field_anchor not in b:
        raise SystemExit("binding field anchor missing")
    b=b.replace(field_anchor,field_repl,1)

insert_anchor='''            data.ExecutionPolicyMatchReason =
                "EXECUTION_POLICY_MATCHED";
            data.BindingAccepted = true;

            data.ProviderResultAdmission =
'''
insert_repl='''            data.ExecutionPolicyMatchReason =
                "EXECUTION_POLICY_MATCHED";

            PatrolDefenseProviderResultSizePolicyResult
                resultSizePolicy =
                    queue.EvaluateRegisteredResultSizePolicy(
                        data.RequestFingerprint,
                        data.ExecutionPolicyId,
                        resultBytes.Length);

            data.ResultByteCount =
                resultSizePolicy == null
                    ? resultBytes.Length
                    : resultSizePolicy.ResultByteCount;
            data.MaxResultBytes =
                resultSizePolicy == null
                    ? 0
                    : resultSizePolicy.MaxResultBytes;
            data.ResultSizePolicyReason =
                resultSizePolicy == null
                    ? "RESULT_SIZE_POLICY_UNAVAILABLE"
                    : resultSizePolicy.Reason;

            if (resultSizePolicy == null ||
                !resultSizePolicy.Allowed)
            {
                Reject(
                    data,
                    data.ResultSizePolicyReason);
                return data;
            }

            data.ResultSizePolicyReason =
                "RESULT_SIZE_WITHIN_LIMIT";
            data.BindingAccepted = true;

            data.ProviderResultAdmission =
'''
if insert_anchor not in b:
    raise SystemExit("binding size insertion anchor missing")
b=b.replace(insert_anchor,insert_repl,1)

json_anchor='''            sb.Append(",\"executionPolicyMatchReason\":");
            sb.Append(Json(data.ExecutionPolicyMatchReason));
            sb.Append(",\"bindingAccepted\":");
'''
json_repl='''            sb.Append(",\"executionPolicyMatchReason\":");
            sb.Append(Json(data.ExecutionPolicyMatchReason));
            sb.Append(",\"resultByteCount\":");
            sb.Append(
                data.ResultByteCount.ToString(
                    CultureInfo.InvariantCulture));
            sb.Append(",\"maxResultBytes\":");
            sb.Append(
                data.MaxResultBytes.ToString(
                    CultureInfo.InvariantCulture));
            sb.Append(",\"resultSizePolicyReason\":");
            sb.Append(Json(data.ResultSizePolicyReason));
            sb.Append(",\"bindingAccepted\":");
'''
if json_anchor not in b:
    raise SystemExit("binding serializer anchor missing")
b=b.replace(json_anchor,json_repl,1)

binding.write_text(b,encoding="utf-8")

print("v02163 queue result-size policy + v4 binding audit patched")
