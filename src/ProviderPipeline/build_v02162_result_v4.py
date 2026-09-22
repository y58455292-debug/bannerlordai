from pathlib import Path

root=Path(r"D:\BannerlordAIResearch\workspace\_PatchStaging\AutonomousOperator_v002162_PolicyBoundResultV4_20260921_2051")

# 1) Queue execution-policy match
p=root/"candidate"/"PatrolDefenseProviderDispatch.cs"
t=p.read_text(encoding="utf-8-sig")

type_anchor='''    internal sealed class PatrolDefenseProviderExecutionPolicyRegistrationResult
    {
        internal string RequestFingerprint;
        internal string ProviderId;
        internal string ModelId;
        internal string AttemptId;
        internal string ProviderRequestId;
        internal string TransportRequestId;
        internal string TimeoutMs;
        internal string MaxAttempts;
        internal string MaxResultBytes;
        internal string CredentialRef;
        internal string ExecutionPolicyId;
        internal bool Registered;
        internal bool Idempotent;
        internal string Reason;
        internal int QueueCount;
    }

'''
type_repl=type_anchor+'''    internal sealed class PatrolDefenseProviderExecutionPolicyMatchResult
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
if "PatrolDefenseProviderExecutionPolicyMatchResult" not in t:
    if type_anchor not in t:
        raise SystemExit("execution policy match type anchor missing")
    t=t.replace(type_anchor,type_repl,1)

method_anchor='''        internal PatrolDefenseProviderExecutionPolicyRegistrationResult
            RegisterExecutionPolicy(
'''
match_method='''        internal PatrolDefenseProviderExecutionPolicyMatchResult
            MatchRegisteredExecutionPolicy(
                string requestFingerprint,
                string providerId,
                string modelId,
                string attemptId,
                string providerRequestId,
                string transportRequestId,
                string executionPolicyId)
        {
            PatrolDefenseProviderExecutionPolicyMatchResult result =
                new PatrolDefenseProviderExecutionPolicyMatchResult();

            result.RequestFingerprint = requestFingerprint;
            result.ProviderId = providerId;
            result.ModelId = modelId;
            result.AttemptId = attemptId;
            result.ProviderRequestId = providerRequestId;
            result.TransportRequestId = transportRequestId;
            result.ExecutionPolicyId = executionPolicyId;

            PatrolDefenseProviderTransportMatchResult transportMatch =
                MatchRegisteredTransportRequest(
                    requestFingerprint,
                    providerId,
                    attemptId,
                    providerRequestId,
                    transportRequestId);

            if (transportMatch == null ||
                !transportMatch.Matched)
            {
                result.Matched = false;
                result.Reason =
                    transportMatch == null
                        ? "TRANSPORT_REQUEST_MATCH_UNAVAILABLE"
                        : transportMatch.Reason;
                return result;
            }

            if (string.IsNullOrWhiteSpace(
                    executionPolicyId))
            {
                result.Matched = false;
                result.Reason =
                    "EXECUTION_POLICY_IDENTITY_INVALID";
                return result;
            }

            PatrolDefenseProviderDispatchJob job;
            if (!_pending.TryGetValue(
                    requestFingerprint,
                    out job))
            {
                result.Matched = false;
                result.Reason =
                    "DISPATCH_JOB_NOT_FOUND";
                return result;
            }

            if (!string.Equals(
                    job.ProviderRequestModelId,
                    modelId,
                    StringComparison.Ordinal))
            {
                result.Matched = false;
                result.Reason =
                    "PROVIDER_REQUEST_MODEL_MISMATCH";
                return result;
            }

            result.RegisteredExecutionPolicyId =
                job.ExecutionPolicyId;

            if (string.IsNullOrWhiteSpace(
                    job.ExecutionPolicyId))
            {
                result.Matched = false;
                result.Reason =
                    "EXECUTION_POLICY_NOT_REGISTERED";
                return result;
            }

            if (!string.Equals(
                    job.ExecutionPolicyId,
                    executionPolicyId,
                    StringComparison.Ordinal))
            {
                result.Matched = false;
                result.Reason =
                    "EXECUTION_POLICY_ID_MISMATCH";
                return result;
            }

            result.Matched = true;
            result.Reason =
                "EXECUTION_POLICY_MATCHED";
            return result;
        }


'''
if "MatchRegisteredExecutionPolicy(" not in t:
    if method_anchor not in t:
        raise SystemExit("execution policy match method anchor missing")
    t=t.replace(method_anchor,match_method+method_anchor,1)

p.write_text(t,encoding="utf-8")

# 2) Derive ProviderResult.v4 from v3
src=(root/"candidate"/"PatrolDefenseProviderResultV3Admission.cs").read_text(encoding="utf-8-sig")
v4=src
v4=v4.replace("PatrolDefenseProviderResultV3AdmissionData","PatrolDefenseProviderResultV4AdmissionData")
v4=v4.replace("PatrolDefenseProviderResultV3Admission","PatrolDefenseProviderResultV4Admission")
v4=v4.replace("BannerlordAI.PatrolDefenseProviderResult.v3","BannerlordAI.PatrolDefenseProviderResult.v4")
v4=v4.replace("BannerlordAI.PatrolDefenseProviderResultV3Admission.v1","BannerlordAI.PatrolDefenseProviderResultV4Admission.v1")

# data fields
anchor='''        internal string TransportRequestId;
        internal string Status;
        internal string ClaimMatchReason;
        internal string ProviderRequestMatchReason;
        internal string TransportRequestMatchReason;
'''
repl='''        internal string TransportRequestId;
        internal string ExecutionPolicyId;
        internal string Status;
        internal string ClaimMatchReason;
        internal string ProviderRequestMatchReason;
        internal string TransportRequestMatchReason;
        internal string ExecutionPolicyMatchReason;
'''
if anchor not in v4:
    raise SystemExit("v4 data fields anchor missing")
v4=v4.replace(anchor,repl,1)

# allowed field
anchor='''                        "transportRequestId",
                        "status",
'''
repl='''                        "transportRequestId",
                        "executionPolicyId",
                        "status",
'''
if anchor not in v4:
    raise SystemExit("v4 allowed field anchor missing")
v4=v4.replace(anchor,repl,1)

# parse field
anchor='''            fields.TryGetValue(
                "transportRequestId",
                out data.TransportRequestId);
            if (!Present(data.TransportRequestId))
                Reject(data, "TRANSPORT_REQUEST_ID_MISSING");

            fields.TryGetValue(
                "status",
'''
repl='''            fields.TryGetValue(
                "transportRequestId",
                out data.TransportRequestId);
            if (!Present(data.TransportRequestId))
                Reject(data, "TRANSPORT_REQUEST_ID_MISSING");

            fields.TryGetValue(
                "executionPolicyId",
                out data.ExecutionPolicyId);
            if (!Present(data.ExecutionPolicyId))
                Reject(data, "EXECUTION_POLICY_ID_MISSING");

            fields.TryGetValue(
                "status",
'''
if anchor not in v4:
    raise SystemExit("v4 parse field anchor missing")
v4=v4.replace(anchor,repl,1)

# execution policy match after transport match
anchor='''            if (transportMatch == null ||
                !transportMatch.Matched)
            {
                Reject(
                    data,
                    data.TransportRequestMatchReason);
                return data;
            }

            if (gate == null)
'''
repl='''            if (transportMatch == null ||
                !transportMatch.Matched)
            {
                Reject(
                    data,
                    data.TransportRequestMatchReason);
                return data;
            }

            PatrolDefenseProviderExecutionPolicyMatchResult
                executionPolicyMatch =
                    dispatchQueue.MatchRegisteredExecutionPolicy(
                        data.RequestFingerprint,
                        data.ProviderId,
                        requestMatch == null
                            ? null
                            : requestMatch.RegisteredProviderRequestId == null
                                ? null
                                : GetRegisteredModelId(
                                    dispatchQueue,
                                    data.RequestFingerprint),
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

            if (gate == null)
'''
# We don't have GetRegisteredModelId. Better simplify: MatchRegisteredExecutionPolicy modelId requirement problematic because result has no modelId.
# We'll revise queue method signature to not require modelId, deriving from job. Do it below before replacing.
if anchor not in v4:
    raise SystemExit("v4 execution match insertion anchor missing")
# Do not use above repl; define proper with no model.
repl='''            if (transportMatch == null ||
                !transportMatch.Matched)
            {
                Reject(
                    data,
                    data.TransportRequestMatchReason);
                return data;
            }

            PatrolDefenseProviderExecutionPolicyMatchResult
                executionPolicyMatch =
                    dispatchQueue.MatchRegisteredExecutionPolicy(
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

            if (gate == null)
'''
v4=v4.replace(anchor,repl,1)

# serializer fields
anchor='''            sb.Append(",\\"transportRequestId\\":");
            sb.Append(Json(data.TransportRequestId));
            sb.Append(",\\"status\\":");
'''
repl='''            sb.Append(",\\"transportRequestId\\":");
            sb.Append(Json(data.TransportRequestId));
            sb.Append(",\\"executionPolicyId\\":");
            sb.Append(Json(data.ExecutionPolicyId));
            sb.Append(",\\"status\\":");
'''
if anchor not in v4:
    raise SystemExit("v4 serializer exec id anchor missing")
v4=v4.replace(anchor,repl,1)

anchor='''            sb.Append(",\\"transportRequestMatchReason\\":");
            sb.Append(Json(data.TransportRequestMatchReason));
            sb.Append(",\\"providerResultAccepted\\":");
'''
repl='''            sb.Append(",\\"transportRequestMatchReason\\":");
            sb.Append(Json(data.TransportRequestMatchReason));
            sb.Append(",\\"executionPolicyMatchReason\\":");
            sb.Append(Json(data.ExecutionPolicyMatchReason));
            sb.Append(",\\"providerResultAccepted\\":");
'''
if anchor not in v4:
    raise SystemExit("v4 serializer match anchor missing")
v4=v4.replace(anchor,repl,1)

(root/"candidate"/"PatrolDefenseProviderResultV4Admission.cs").write_text(v4,encoding="utf-8")

# 3) adjust queue MatchRegisteredExecutionPolicy to omit modelId from public signature
p=root/"candidate"/"PatrolDefenseProviderDispatch.cs"
t=p.read_text(encoding="utf-8")
old='''            MatchRegisteredExecutionPolicy(
                string requestFingerprint,
                string providerId,
                string modelId,
                string attemptId,
                string providerRequestId,
                string transportRequestId,
                string executionPolicyId)
'''
new='''            MatchRegisteredExecutionPolicy(
                string requestFingerprint,
                string providerId,
                string attemptId,
                string providerRequestId,
                string transportRequestId,
                string executionPolicyId)
'''
if old not in t:
    raise SystemExit("match signature old not found")
t=t.replace(old,new,1)
t=t.replace('''            result.ModelId = modelId;
''','',1)
# Remove model comparison block inside match only; locate unique block before RegisteredExecutionPolicyId.
old='''            if (!string.Equals(
                    job.ProviderRequestModelId,
                    modelId,
                    StringComparison.Ordinal))
            {
                result.Matched = false;
                result.Reason =
                    "PROVIDER_REQUEST_MODEL_MISMATCH";
                return result;
            }

            result.RegisteredExecutionPolicyId =
'''
if old not in t:
    raise SystemExit("model match block not found")
t=t.replace(old,'''            result.ModelId =
                job.ProviderRequestModelId;

            result.RegisteredExecutionPolicyId =
''',1)
p.write_text(t,encoding="utf-8")

print("v02162 queue match + ProviderResult.v4 generated")
