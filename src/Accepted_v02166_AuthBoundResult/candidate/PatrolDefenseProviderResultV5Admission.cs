using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace BannerlordAITestRunner
{
    internal sealed class PatrolDefenseProviderResultV5AdmissionData
    {
        internal string ProviderId;
        internal string AttemptId;
        internal string RequestFingerprint;
        internal string ProviderRequestId;
        internal string TransportRequestId;
        internal string ExecutionPolicyId;
        internal string TransportExecutionAuthorizationId;
        internal int TransportExecutionAttemptOrdinal;
        internal string Status;
        internal string ClaimMatchReason;
        internal string ProviderRequestMatchReason;
        internal string TransportRequestMatchReason;
        internal string ExecutionPolicyMatchReason;
        internal string TransportExecutionAuthorizationMatchReason;
        internal bool ProviderResultAccepted;
        internal bool Retryable;
        internal PatrolDefenseDeliberationAdvisoryAdmissionData
            AdvisoryAdmission;
        internal List<string> RejectionReasons =
            new List<string>();
    }

    internal static class PatrolDefenseProviderResultV5Admission
    {
        internal const string ResultSchema =
            "BannerlordAI.PatrolDefenseProviderResult.v5";

        private static readonly HashSet<string>
            AllowedFields =
                new HashSet<string>(
                    new[]
                    {
                        "schema",
                        "providerId",
                        "attemptId",
                        "requestFingerprint",
                        "providerRequestId",
                        "transportRequestId",
                        "executionPolicyId",
                        "transportExecutionAuthorizationId",
                        "transportExecutionAttemptOrdinal",
                        "status",
                        "advisoryBase64"
                    },
                    StringComparer.Ordinal);

        private static readonly HashSet<string>
            AllowedStatuses =
                new HashSet<string>(
                    new[]
                    {
                        "SUCCESS",
                        "TRANSIENT_FAILURE",
                        "PERMANENT_FAILURE"
                    },
                    StringComparer.Ordinal);

        private static void SkipWhitespace(
            string text,
            ref int index)
        {
            while (
                index < text.Length &&
                char.IsWhiteSpace(text[index]))
            {
                index++;
            }
        }

        private static bool TryReadHex4(
            string text,
            ref int index,
            out char value)
        {
            value = '\0';
            if (index + 4 > text.Length)
                return false;

            int code = 0;
            for (int i = 0; i < 4; i++)
            {
                char ch = text[index + i];
                int digit;

                if (ch >= '0' && ch <= '9')
                    digit = ch - '0';
                else if (ch >= 'A' && ch <= 'F')
                    digit = 10 + ch - 'A';
                else if (ch >= 'a' && ch <= 'f')
                    digit = 10 + ch - 'a';
                else
                    return false;

                code = (code << 4) | digit;
            }

            index += 4;
            value = (char)code;
            return true;
        }

        private static bool TryReadJsonString(
            string text,
            ref int index,
            out string value)
        {
            value = null;

            if (
                index >= text.Length ||
                text[index] != '"')
                return false;

            index++;
            StringBuilder sb =
                new StringBuilder();

            while (index < text.Length)
            {
                char ch = text[index++];

                if (ch == '"')
                {
                    value = sb.ToString();
                    return true;
                }

                if (ch < 0x20)
                    return false;

                if (ch != '\\')
                {
                    sb.Append(ch);
                    continue;
                }

                if (index >= text.Length)
                    return false;

                char escape = text[index++];
                switch (escape)
                {
                    case '"':
                        sb.Append('"');
                        break;
                    case '\\':
                        sb.Append('\\');
                        break;
                    case '/':
                        sb.Append('/');
                        break;
                    case 'b':
                        sb.Append('\b');
                        break;
                    case 'f':
                        sb.Append('\f');
                        break;
                    case 'n':
                        sb.Append('\n');
                        break;
                    case 'r':
                        sb.Append('\r');
                        break;
                    case 't':
                        sb.Append('\t');
                        break;
                    case 'u':
                        char unicode;
                        if (!TryReadHex4(
                                text,
                                ref index,
                                out unicode))
                            return false;
                        sb.Append(unicode);
                        break;
                    default:
                        return false;
                }
            }

            return false;
        }

        private static bool TryParseFlatStringObject(
            string json,
            out Dictionary<string, string> fields)
        {
            fields =
                new Dictionary<string, string>(
                    StringComparer.Ordinal);

            if (string.IsNullOrWhiteSpace(json))
                return false;

            int index = 0;
            SkipWhitespace(json, ref index);

            if (
                index >= json.Length ||
                json[index] != '{')
                return false;

            index++;
            SkipWhitespace(json, ref index);

            if (
                index < json.Length &&
                json[index] == '}')
            {
                index++;
                SkipWhitespace(json, ref index);
                return index == json.Length;
            }

            while (index < json.Length)
            {
                string key;
                if (!TryReadJsonString(
                        json,
                        ref index,
                        out key))
                    return false;

                if (fields.ContainsKey(key))
                    return false;

                SkipWhitespace(json, ref index);

                if (
                    index >= json.Length ||
                    json[index] != ':')
                    return false;

                index++;
                SkipWhitespace(json, ref index);

                string value;
                if (!TryReadJsonString(
                        json,
                        ref index,
                        out value))
                    return false;

                fields.Add(key, value);
                SkipWhitespace(json, ref index);

                if (index >= json.Length)
                    return false;

                if (json[index] == '}')
                {
                    index++;
                    SkipWhitespace(json, ref index);
                    return index == json.Length;
                }

                if (json[index] != ',')
                    return false;

                index++;
                SkipWhitespace(json, ref index);
            }

            return false;
        }

        private static void Reject(
            PatrolDefenseProviderResultV5AdmissionData data,
            string reason)
        {
            if (
                data != null &&
                !data.RejectionReasons.Contains(reason))
            {
                data.RejectionReasons.Add(reason);
            }
        }

        private static bool Present(string value)
        {
            return !string.IsNullOrWhiteSpace(value);
        }

        internal static PatrolDefenseProviderResultV5AdmissionData
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

        private static PatrolDefenseProviderResultV5AdmissionData
            EvaluateInternal(
                PatrolDefenseAdvisoryRuntimeGate gate,
                PatrolDefenseProviderDispatchQueue dispatchQueue,
                string envelopeJson)
        {
            PatrolDefenseProviderResultV5AdmissionData data =
                new PatrolDefenseProviderResultV5AdmissionData();

            Dictionary<string, string> fields;
            if (!TryParseFlatStringObject(
                    envelopeJson,
                    out fields))
            {
                Reject(data, "MALFORMED_JSON");
                return data;
            }

            foreach (
                KeyValuePair<string, string> pair
                in fields)
            {
                if (!AllowedFields.Contains(pair.Key))
                    Reject(
                        data,
                        "UNEXPECTED_FIELD:" + pair.Key);
            }

            string schema;
            if (!fields.TryGetValue("schema", out schema))
                Reject(data, "SCHEMA_MISSING");
            else if (!string.Equals(
                         schema,
                         ResultSchema,
                         StringComparison.Ordinal))
                Reject(data, "SCHEMA_INVALID");

            fields.TryGetValue(
                "providerId",
                out data.ProviderId);
            if (!Present(data.ProviderId))
                Reject(data, "PROVIDER_ID_MISSING");

            fields.TryGetValue(
                "attemptId",
                out data.AttemptId);
            if (!Present(data.AttemptId))
                Reject(data, "ATTEMPT_ID_MISSING");

            fields.TryGetValue(
                "requestFingerprint",
                out data.RequestFingerprint);
            if (!Present(data.RequestFingerprint))
                Reject(data, "REQUEST_FINGERPRINT_MISSING");

            fields.TryGetValue(
                "providerRequestId",
                out data.ProviderRequestId);
            if (!Present(data.ProviderRequestId))
                Reject(data, "PROVIDER_REQUEST_ID_MISSING");

            fields.TryGetValue(
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
                "transportExecutionAuthorizationId",
                out data.TransportExecutionAuthorizationId);
            if (!Present(data.TransportExecutionAuthorizationId))
                Reject(data, "EXECUTION_AUTHORIZATION_ID_MISSING");

            string transportExecutionAttemptOrdinal;
            fields.TryGetValue(
                "transportExecutionAttemptOrdinal",
                out transportExecutionAttemptOrdinal);
            if (!int.TryParse(
                    transportExecutionAttemptOrdinal,
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out data.TransportExecutionAttemptOrdinal) ||
                data.TransportExecutionAttemptOrdinal <= 0)
            {
                Reject(data, "EXECUTION_AUTHORIZATION_ORDINAL_INVALID");
            }

            fields.TryGetValue(
                "status",
                out data.Status);
            if (!Present(data.Status))
                Reject(data, "STATUS_MISSING");
            else if (!AllowedStatuses.Contains(data.Status))
                Reject(data, "STATUS_INVALID");

            string advisoryBase64 = null;
            fields.TryGetValue(
                "advisoryBase64",
                out advisoryBase64);

            if (data.RejectionReasons.Count > 0)
                return data;

            if (dispatchQueue == null)
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

            PatrolDefenseProviderTransportMatchResult
                transportMatch =
                    dispatchQueue.MatchRegisteredTransportRequest(
                        data.RequestFingerprint,
                        data.ProviderId,
                        data.AttemptId,
                        data.ProviderRequestId,
                        data.TransportRequestId);

            data.TransportRequestMatchReason =
                transportMatch == null
                    ? "TRANSPORT_REQUEST_MATCH_UNAVAILABLE"
                    : transportMatch.Reason;

            if (transportMatch == null ||
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

            PatrolDefenseProviderTransportExecutionAuthorizationMatchResult
                authorizationMatch =
                    dispatchQueue.MatchRegisteredTransportExecutionAuthorization(
                        data.RequestFingerprint,
                        data.ExecutionPolicyId,
                        data.TransportExecutionAuthorizationId,
                        data.TransportExecutionAttemptOrdinal);

            data.TransportExecutionAuthorizationMatchReason =
                authorizationMatch == null
                    ? "EXECUTION_AUTHORIZATION_MATCH_UNAVAILABLE"
                    : authorizationMatch.Reason;

            if (authorizationMatch == null ||
                !authorizationMatch.Matched)
            {
                Reject(
                    data,
                    data.TransportExecutionAuthorizationMatchReason);
                return data;
            }

            if (gate == null)
            {
                Reject(data, "ADMISSION_GATE_MISSING");
                return data;
            }

            string pending =
                gate.PendingRequestFingerprint;

            if (!Present(pending))
            {
                Reject(data, "NO_PENDING_ROUTE_REQUEST");
                return data;
            }

            if (!string.Equals(
                    data.RequestFingerprint,
                    pending,
                    StringComparison.Ordinal))
            {
                Reject(
                    data,
                    "PENDING_REQUEST_FINGERPRINT_MISMATCH");
                return data;
            }

            if (string.Equals(
                    data.Status,
                    "TRANSIENT_FAILURE",
                    StringComparison.Ordinal))
            {
                data.Retryable = true;
                Reject(data, "PROVIDER_TRANSIENT_FAILURE");
                return data;
            }

            if (string.Equals(
                    data.Status,
                    "PERMANENT_FAILURE",
                    StringComparison.Ordinal))
            {
                data.Retryable = false;
                Reject(data, "PROVIDER_PERMANENT_FAILURE");
                return data;
            }

            if (!Present(advisoryBase64))
            {
                data.Retryable = true;
                Reject(data, "ADVISORY_BASE64_MISSING");
                return data;
            }

            string advisoryJson;
            try
            {
                byte[] bytes =
                    Convert.FromBase64String(
                        advisoryBase64);
                advisoryJson =
                    Encoding.UTF8.GetString(bytes);
            }
            catch (FormatException)
            {
                data.Retryable = true;
                Reject(data, "ADVISORY_BASE64_INVALID");
                return data;
            }

            data.AdvisoryAdmission =
                gate.Evaluate(
                    data.RequestFingerprint,
                    advisoryJson);

            if (
                data.AdvisoryAdmission != null &&
                data.AdvisoryAdmission.Admitted)
            {
                data.ProviderResultAccepted = true;
                data.Retryable = false;
                return data;
            }

            data.ProviderResultAccepted = false;
            data.Retryable = true;
            Reject(data, "ADVISORY_NOT_ADMITTED");
            return data;
        }

        private static string Json(string value)
        {
            if (value == null)
                return "null";

            return "\"" +
                value
                    .Replace("\\", "\\\\")
                    .Replace("\"", "\\\"")
                    .Replace("\r", "\\r")
                    .Replace("\n", "\\n") +
                "\"";
        }

        internal static string ToJson(
            PatrolDefenseProviderResultV5AdmissionData data)
        {
            if (data == null)
                return null;

            StringBuilder sb =
                new StringBuilder();

            sb.Append("{");
            sb.Append(
                "\"schema\":\"BannerlordAI.PatrolDefenseProviderResultV5Admission.v1\"");
            sb.Append(",\"mode\":\"observe\"");
            sb.Append(",\"providerId\":");
            sb.Append(Json(data.ProviderId));
            sb.Append(",\"attemptId\":");
            sb.Append(Json(data.AttemptId));
            sb.Append(",\"requestFingerprint\":");
            sb.Append(Json(data.RequestFingerprint));
            sb.Append(",\"providerRequestId\":");
            sb.Append(Json(data.ProviderRequestId));
            sb.Append(",\"transportRequestId\":");
            sb.Append(Json(data.TransportRequestId));
            sb.Append(",\"executionPolicyId\":");
            sb.Append(Json(data.ExecutionPolicyId));
            sb.Append(",\"transportExecutionAuthorizationId\":");
            sb.Append(Json(data.TransportExecutionAuthorizationId));
            sb.Append(",\"transportExecutionAttemptOrdinal\":");
            sb.Append(
                data.TransportExecutionAttemptOrdinal.ToString(
                    CultureInfo.InvariantCulture));
            sb.Append(",\"status\":");
            sb.Append(Json(data.Status));
            sb.Append(",\"claimMatchReason\":");
            sb.Append(Json(data.ClaimMatchReason));
            sb.Append(",\"providerRequestMatchReason\":");
            sb.Append(Json(data.ProviderRequestMatchReason));
            sb.Append(",\"transportRequestMatchReason\":");
            sb.Append(Json(data.TransportRequestMatchReason));
            sb.Append(",\"executionPolicyMatchReason\":");
            sb.Append(Json(data.ExecutionPolicyMatchReason));
            sb.Append(",\"transportExecutionAuthorizationMatchReason\":");
            sb.Append(Json(data.TransportExecutionAuthorizationMatchReason));
            sb.Append(",\"providerResultAccepted\":");
            sb.Append(
                data.ProviderResultAccepted
                    ? "true"
                    : "false");
            sb.Append(",\"retryable\":");
            sb.Append(
                data.Retryable
                    ? "true"
                    : "false");
            sb.Append(",\"rejectionReasons\":[");
            for (
                int i = 0;
                i < data.RejectionReasons.Count;
                i++)
            {
                if (i > 0)
                    sb.Append(",");
                sb.Append(
                    Json(
                        data.RejectionReasons[i]));
            }
            sb.Append("]");
            sb.Append(",\"advisoryAdmission\":");
            string advisoryAdmissionJson =
                PatrolDefenseDeliberationAdvisoryAdmission.ToJson(
                    data.AdvisoryAdmission);
            sb.Append(
                string.IsNullOrWhiteSpace(advisoryAdmissionJson)
                    ? "null"
                    : advisoryAdmissionJson);
            sb.Append(",\"externalNetworkUsed\":false");
            sb.Append(",\"modelInvoked\":false");
            sb.Append(",\"llmInvoked\":false");
            sb.Append(",\"plannerInvoked\":false");
            sb.Append(",\"executionAuthorized\":false");
            sb.Append(",\"interpretationApplied\":false");
            sb.Append(",\"behaviorMutation\":false");
            sb.Append(",\"intentMutation\":false");
            sb.Append(",\"scoreMutation\":false");
            sb.Append(",\"nativeMovementCalls\":0");
            sb.Append("}");

            return sb.ToString();
        }
    }
}

