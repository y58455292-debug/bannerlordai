from pathlib import Path

root=Path(r"D:\BannerlordAIResearch\workspace\_PatchStaging\AutonomousOperator_v002160_TransportReceiptResultHash_20260921_1948")
dispatch=root/"candidate"/"PatrolDefenseProviderDispatch.cs"
t=dispatch.read_text(encoding="utf-8-sig")

type_anchor='''    internal sealed class PatrolDefenseProviderTransportMatchResult
    {
        internal string RequestFingerprint;
        internal string ProviderId;
        internal string AttemptId;
        internal string ProviderRequestId;
        internal string TransportRequestId;
        internal string RegisteredTransportRequestId;
        internal bool Matched;
        internal string Reason;
    }

'''
type_repl=type_anchor+'''    internal sealed class PatrolDefenseProviderTransportReceiptMatchResult
    {
        internal string RequestFingerprint;
        internal string ProviderId;
        internal string ModelId;
        internal string AttemptId;
        internal string ProviderRequestId;
        internal string RequestInputSha256;
        internal string TransportId;
        internal string TransportRequestId;
        internal bool Matched;
        internal string Reason;
    }

'''
if "PatrolDefenseProviderTransportReceiptMatchResult" not in t:
    if type_anchor not in t:
        raise SystemExit("transport receipt type anchor missing")
    t=t.replace(type_anchor,type_repl,1)

method_anchor='''        internal PatrolDefenseProviderTransportMatchResult
            MatchRegisteredTransportRequest(
'''
method='''        internal PatrolDefenseProviderTransportReceiptMatchResult
            MatchRegisteredTransportReceipt(
                string requestFingerprint,
                string providerId,
                string modelId,
                string attemptId,
                string providerRequestId,
                string requestInputSha256,
                string transportId,
                string transportRequestId)
        {
            PatrolDefenseProviderTransportReceiptMatchResult result =
                new PatrolDefenseProviderTransportReceiptMatchResult();

            result.RequestFingerprint = requestFingerprint;
            result.ProviderId = providerId;
            result.ModelId = modelId;
            result.AttemptId = attemptId;
            result.ProviderRequestId = providerRequestId;
            result.RequestInputSha256 = requestInputSha256;
            result.TransportId = transportId;
            result.TransportRequestId = transportRequestId;

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
                    job.ProviderRequestModelId,
                    modelId,
                    StringComparison.Ordinal))
            {
                result.Matched = false;
                result.Reason =
                    "TRANSPORT_RECEIPT_MODEL_MISMATCH";
                return result;
            }

            if (!string.Equals(
                    job.ProviderRequestInputSha256,
                    requestInputSha256,
                    StringComparison.Ordinal))
            {
                result.Matched = false;
                result.Reason =
                    "TRANSPORT_RECEIPT_INPUT_SHA_MISMATCH";
                return result;
            }

            if (string.IsNullOrWhiteSpace(job.TransportId))
            {
                result.Matched = false;
                result.Reason =
                    "TRANSPORT_REQUEST_NOT_REGISTERED";
                return result;
            }

            if (!string.Equals(
                    job.TransportId,
                    transportId,
                    StringComparison.Ordinal))
            {
                result.Matched = false;
                result.Reason =
                    "TRANSPORT_RECEIPT_TRANSPORT_ID_MISMATCH";
                return result;
            }

            result.Matched = true;
            result.Reason = "TRANSPORT_RECEIPT_MATCHED";
            return result;
        }


'''
if "MatchRegisteredTransportReceipt(" not in t:
    if method_anchor not in t:
        raise SystemExit("transport receipt method anchor missing")
    t=t.replace(method_anchor,method+method_anchor,1)

dispatch.write_text(t,encoding="utf-8")

# Add class to v3 fixture project
proj=root/"fixtures_v3"/"FixturesV3.csproj"
x=proj.read_text(encoding="utf-8-sig")
include='''    <Compile Include="..\\candidate\\PatrolDefenseProviderTransportReceiptResultBinding.cs" Link="PatrolDefenseProviderTransportReceiptResultBinding.cs" />
'''
if "PatrolDefenseProviderTransportReceiptResultBinding.cs" not in x:
    anchor='''    <Compile Include="..\\candidate\\PatrolDefenseProviderResultV3Admission.cs" Link="PatrolDefenseProviderResultV3Admission.cs" />
'''
    if anchor not in x:
        raise SystemExit("v3 project anchor missing")
    x=x.replace(anchor,anchor+include,1)
proj.write_text(x,encoding="utf-8")

print("dispatch queue receipt match + v3 project include patched")
