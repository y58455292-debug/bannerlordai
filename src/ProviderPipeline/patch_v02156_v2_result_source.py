from pathlib import Path
import re

root=Path(r"D:\BannerlordAIResearch\workspace\_PatchStaging\AutonomousOperator_v002156_ProviderResultV2Binding_20260921_1549")
dispatch=root/"candidate"/"PatrolDefenseProviderDispatch.cs"
t=dispatch.read_text(encoding="utf-8-sig")

type_anchor='''    internal sealed class PatrolDefenseProviderRequestRegistrationResult
    {
        internal string RequestFingerprint;
        internal string ProviderId;
        internal string ModelId;
        internal string AttemptId;
        internal string ProviderRequestId;
        internal string PromptContractVersion;
        internal string ResponseSchema;
        internal string InputSha256;
        internal bool Registered;
        internal bool Idempotent;
        internal string Reason;
        internal int QueueCount;
    }

'''
type_repl=type_anchor+r'''    internal sealed class PatrolDefenseProviderRequestMatchResult
    {
        internal string RequestFingerprint;
        internal string ProviderId;
        internal string AttemptId;
        internal string ProviderRequestId;
        internal string RegisteredProviderRequestId;
        internal bool Matched;
        internal string Reason;
    }

'''
if "PatrolDefenseProviderRequestMatchResult" not in t:
    if type_anchor not in t:
        raise SystemExit("provider request match type anchor missing")
    t=t.replace(type_anchor,type_repl,1)

method_anchor='''        internal PatrolDefenseProviderRequestRegistrationResult
            RegisterProviderRequest(
'''
match_method=r'''        internal PatrolDefenseProviderRequestMatchResult
            MatchRegisteredProviderRequest(
                string requestFingerprint,
                string providerId,
                string attemptId,
                string providerRequestId)
        {
            PatrolDefenseProviderRequestMatchResult result =
                new PatrolDefenseProviderRequestMatchResult();

            result.RequestFingerprint = requestFingerprint;
            result.ProviderId = providerId;
            result.AttemptId = attemptId;
            result.ProviderRequestId = providerRequestId;

            if (string.IsNullOrWhiteSpace(requestFingerprint) ||
                string.IsNullOrWhiteSpace(providerId) ||
                string.IsNullOrWhiteSpace(attemptId) ||
                string.IsNullOrWhiteSpace(providerRequestId))
            {
                result.Matched = false;
                result.Reason = "PROVIDER_REQUEST_IDENTITY_INVALID";
                return result;
            }

            PatrolDefenseProviderDispatchJob job;
            if (!_pending.TryGetValue(
                    requestFingerprint,
                    out job))
            {
                result.Matched = false;
                result.Reason = "DISPATCH_JOB_NOT_FOUND";
                return result;
            }

            if (!string.Equals(
                    job.State,
                    "CLAIMED",
                    StringComparison.Ordinal))
            {
                result.Matched = false;
                result.Reason = "PROVIDER_JOB_NOT_CLAIMED";
                return result;
            }

            if (!string.Equals(
                    job.ProviderId,
                    providerId,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    job.AttemptId,
                    attemptId,
                    StringComparison.Ordinal))
            {
                result.Matched = false;
                result.Reason = "PROVIDER_CLAIM_MISMATCH";
                return result;
            }

            result.RegisteredProviderRequestId =
                job.ProviderRequestId;

            if (string.IsNullOrWhiteSpace(
                    job.ProviderRequestId))
            {
                result.Matched = false;
                result.Reason =
                    "PROVIDER_REQUEST_NOT_REGISTERED";
                return result;
            }

            if (!string.Equals(
                    job.ProviderRequestId,
                    providerRequestId,
                    StringComparison.Ordinal))
            {
                result.Matched = false;
                result.Reason =
                    "PROVIDER_REQUEST_ID_MISMATCH";
                return result;
            }

            result.Matched = true;
            result.Reason =
                "PROVIDER_REQUEST_MATCHED";
            return result;
        }


'''
if "MatchRegisteredProviderRequest(" not in t:
    if method_anchor not in t:
        raise SystemExit("provider request match method anchor missing")
    t=t.replace(method_anchor,match_method+method_anchor,1)

dispatch.write_text(t,encoding="utf-8")

# Build ProviderResult v2 source from accepted v1 source.
src=(root/"candidate"/"PatrolDefenseProviderResultAdmission.cs").read_text(encoding="utf-8-sig")
v2=src
v2=v2.replace("PatrolDefenseProviderResultAdmissionData","PatrolDefenseProviderResultV2AdmissionData")
v2=v2.replace("PatrolDefenseProviderResultAdmission","PatrolDefenseProviderResultV2Admission")
v2=v2.replace("BannerlordAI.PatrolDefenseProviderResult.v1","BannerlordAI.PatrolDefenseProviderResult.v2")
v2=v2.replace("BannerlordAI.PatrolDefenseProviderResultAdmission.v1","BannerlordAI.PatrolDefenseProviderResultV2Admission.v1")

# Data fields.
v2=v2.replace(
'''        internal string RequestFingerprint;
        internal string Status;
        internal string ClaimMatchReason;
''',
'''        internal string RequestFingerprint;
        internal string ProviderRequestId;
        internal string Status;
        internal string ClaimMatchReason;
        internal string ProviderRequestMatchReason;
''',1)

# Allowed field.
v2=v2.replace(
'''                        "requestFingerprint",
                        "status",
''',
'''                        "requestFingerprint",
                        "providerRequestId",
                        "status",
''',1)

# Replace evaluator wrapper block.
start=v2.index('''        internal static PatrolDefenseProviderResultV2AdmissionData
            Evaluate(
''')
end=v2.index('''        private static PatrolDefenseProviderResultV2AdmissionData
            EvaluateInternal(
''',start)
wrapper='''        internal static PatrolDefenseProviderResultV2AdmissionData
            EvaluateRequestBound(
                PatrolDefenseAdvisoryRuntimeGate gate,
                PatrolDefenseProviderDispatchQueue dispatchQueue,
                string envelopeJson)
        {
            return EvaluateInternal(
                gate,
                dispatchQueue,
                envelopeJson);
        }

'''
v2=v2[:start]+wrapper+v2[end:]

# Internal signature remove bool.
old_sig='''        private static PatrolDefenseProviderResultV2AdmissionData
            EvaluateInternal(
                PatrolDefenseAdvisoryRuntimeGate gate,
                PatrolDefenseProviderDispatchQueue dispatchQueue,
                bool requireClaimMatch,
                string envelopeJson)
'''
new_sig='''        private static PatrolDefenseProviderResultV2AdmissionData
            EvaluateInternal(
                PatrolDefenseAdvisoryRuntimeGate gate,
                PatrolDefenseProviderDispatchQueue dispatchQueue,
                string envelopeJson)
'''
if old_sig not in v2:
    raise SystemExit("v2 internal signature anchor missing")
v2=v2.replace(old_sig,new_sig,1)

# Parse providerRequestId.
parse_anchor='''            fields.TryGetValue(
                "status",
                out data.Status);
'''
parse_repl='''            fields.TryGetValue(
                "providerRequestId",
                out data.ProviderRequestId);
            if (!Present(data.ProviderRequestId))
                Reject(data, "PROVIDER_REQUEST_ID_MISSING");

            fields.TryGetValue(
                "status",
                out data.Status);
'''
if parse_anchor not in v2:
    raise SystemExit("v2 providerRequestId parse anchor missing")
v2=v2.replace(parse_anchor,parse_repl,1)

# Replace optional claim block with unconditional claim + registered request match.
claim_start=v2.index('''            if (requireClaimMatch)
            {
''')
claim_end=v2.index('''            if (gate == null)
''',claim_start)
claim_block='''            if (dispatchQueue == null)
            {
                data.ClaimMatchReason =
                    "DISPATCH_QUEUE_MISSING";
                Reject(
                    data,
                    data.ClaimMatchReason);
                return data;
            }

            PatrolDefenseProviderClaimMatchResult
                claimMatch =
                    dispatchQueue.MatchClaim(
                        data.RequestFingerprint,
                        data.ProviderId,
                        data.AttemptId);

            data.ClaimMatchReason =
                claimMatch == null
                    ? "CLAIM_MATCH_UNAVAILABLE"
                    : claimMatch.Reason;

            if (claimMatch == null ||
                !claimMatch.Matched)
            {
                Reject(
                    data,
                    data.ClaimMatchReason);
                return data;
            }

            PatrolDefenseProviderRequestMatchResult
                requestMatch =
                    dispatchQueue.MatchRegisteredProviderRequest(
                        data.RequestFingerprint,
                        data.ProviderId,
                        data.AttemptId,
                        data.ProviderRequestId);

            data.ProviderRequestMatchReason =
                requestMatch == null
                    ? "PROVIDER_REQUEST_MATCH_UNAVAILABLE"
                    : requestMatch.Reason;

            if (requestMatch == null ||
                !requestMatch.Matched)
            {
                Reject(
                    data,
                    data.ProviderRequestMatchReason);
                return data;
            }

'''
v2=v2[:claim_start]+claim_block+v2[claim_end:]

# Receipt fields.
receipt_anchor='''            sb.Append(",\\"status\\":");
            sb.Append(Json(data.Status));
            sb.Append(",\\"claimMatchReason\\":");
            sb.Append(Json(data.ClaimMatchReason));
            sb.Append(",\\"providerResultAccepted\\":");
'''
receipt_repl='''            sb.Append(",\\"providerRequestId\\":");
            sb.Append(Json(data.ProviderRequestId));
            sb.Append(",\\"status\\":");
            sb.Append(Json(data.Status));
            sb.Append(",\\"claimMatchReason\\":");
            sb.Append(Json(data.ClaimMatchReason));
            sb.Append(",\\"providerRequestMatchReason\\":");
            sb.Append(Json(data.ProviderRequestMatchReason));
            sb.Append(",\\"providerResultAccepted\\":");
'''
if receipt_anchor not in v2:
    raise SystemExit("v2 receipt anchor missing")
v2=v2.replace(receipt_anchor,receipt_repl,1)

(root/"candidate"/"PatrolDefenseProviderResultV2Admission.cs").write_text(v2,encoding="utf-8")

print("v02156 queue request-match + v2 result source created")
