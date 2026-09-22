using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace BannerlordAITestRunner
{
    internal sealed class PatrolDefenseProviderTransportReceiptResultBindingData
    {
        internal string TransportId;
        internal string TransportRequestId;
        internal string ProviderRequestId;
        internal string ProviderId;
        internal string ModelId;
        internal string AttemptId;
        internal string RequestFingerprint;
        internal string RequestInputSha256;
        internal string ResultStatus;
        internal string ResultSha256;
        internal string ComputedResultSha256;
        internal bool ExternalNetworkUsed;
        internal bool ModelInvoked;
        internal bool ReceiptSuccess;
        internal string ErrorCode;
        internal bool BindingAccepted;
        internal string TransportReceiptMatchReason;
        internal List<string> RejectionReasons =
            new List<string>();
        internal PatrolDefenseProviderResultV3AdmissionData
            ProviderResultAdmission;
    }

    internal static class PatrolDefenseProviderTransportReceiptResultBinding
    {
        internal const string ReceiptSchema =
            "BannerlordAI.PatrolDefenseProviderTransportReceipt.v2";
        internal const string ResultSchema =
            "BannerlordAI.PatrolDefenseProviderResult.v3";

        private sealed class Scalar
        {
            internal int Kind;
            internal string StringValue;
            internal bool BooleanValue;
        }

        private static readonly HashSet<string> ReceiptFields =
            new HashSet<string>(
                new[]
                {
                    "schema",
                    "transportId",
                    "transportRequestId",
                    "providerRequestId",
                    "providerId",
                    "modelId",
                    "attemptId",
                    "requestFingerprint",
                    "requestInputSha256",
                    "resultStatus",
                    "resultSha256",
                    "externalNetworkUsed",
                    "modelInvoked",
                    "success",
                    "errorCode"
                },
                StringComparer.Ordinal);

        private static void Reject(
            PatrolDefenseProviderTransportReceiptResultBindingData data,
            string reason)
        {
            if (data != null &&
                !data.RejectionReasons.Contains(reason))
            {
                data.RejectionReasons.Add(reason);
            }
        }

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

            if (index >= text.Length ||
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

        private static bool TryParseScalarObject(
            string json,
            out Dictionary<string, Scalar> fields)
        {
            fields =
                new Dictionary<string, Scalar>(
                    StringComparer.Ordinal);

            if (string.IsNullOrWhiteSpace(json))
                return false;

            int index = 0;
            SkipWhitespace(json, ref index);

            if (index >= json.Length ||
                json[index] != '{')
                return false;

            index++;
            SkipWhitespace(json, ref index);

            if (index < json.Length &&
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
                if (index >= json.Length ||
                    json[index] != ':')
                    return false;

                index++;
                SkipWhitespace(json, ref index);

                Scalar scalar =
                    new Scalar();

                if (index < json.Length &&
                    json[index] == '"')
                {
                    string value;
                    if (!TryReadJsonString(
                            json,
                            ref index,
                            out value))
                        return false;
                    scalar.Kind = 1;
                    scalar.StringValue = value;
                }
                else if (index + 4 <= json.Length &&
                         string.CompareOrdinal(
                             json,
                             index,
                             "true",
                             0,
                             4) == 0)
                {
                    scalar.Kind = 2;
                    scalar.BooleanValue = true;
                    index += 4;
                }
                else if (index + 5 <= json.Length &&
                         string.CompareOrdinal(
                             json,
                             index,
                             "false",
                             0,
                             5) == 0)
                {
                    scalar.Kind = 2;
                    scalar.BooleanValue = false;
                    index += 5;
                }
                else if (index + 4 <= json.Length &&
                         string.CompareOrdinal(
                             json,
                             index,
                             "null",
                             0,
                             4) == 0)
                {
                    scalar.Kind = 3;
                    index += 4;
                }
                else
                {
                    return false;
                }

                fields.Add(key, scalar);
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

        private static string GetString(
            Dictionary<string, Scalar> fields,
            string key)
        {
            Scalar value;
            if (!fields.TryGetValue(key, out value) ||
                value == null ||
                value.Kind != 1)
                return null;

            return value.StringValue;
        }

        private static bool TryGetBoolean(
            Dictionary<string, Scalar> fields,
            string key,
            out bool value)
        {
            value = false;
            Scalar scalar;

            if (!fields.TryGetValue(key, out scalar) ||
                scalar == null ||
                scalar.Kind != 2)
                return false;

            value = scalar.BooleanValue;
            return true;
        }

        private static bool IsNull(
            Dictionary<string, Scalar> fields,
            string key)
        {
            Scalar scalar;
            return fields.TryGetValue(key, out scalar) &&
                scalar != null &&
                scalar.Kind == 3;
        }

        private static string Sha256Upper(
            byte[] bytes)
        {
            using (SHA256 sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(bytes);
                StringBuilder sb =
                    new StringBuilder(hash.Length * 2);

                for (int i = 0; i < hash.Length; i++)
                {
                    sb.Append(
                        hash[i].ToString(
                            "X2",
                            CultureInfo.InvariantCulture));
                }

                return sb.ToString();
            }
        }

        private static bool Present(string value)
        {
            return !string.IsNullOrWhiteSpace(value);
        }

        internal static PatrolDefenseProviderTransportReceiptResultBindingData
            Evaluate(
                PatrolDefenseAdvisoryRuntimeGate gate,
                PatrolDefenseProviderDispatchQueue queue,
                string receiptJson,
                string resultJson,
                byte[] resultBytes)
        {
            PatrolDefenseProviderTransportReceiptResultBindingData data =
                new PatrolDefenseProviderTransportReceiptResultBindingData();

            Dictionary<string, Scalar> receipt;
            if (!TryParseScalarObject(
                    receiptJson,
                    out receipt))
            {
                Reject(data, "TRANSPORT_RECEIPT_MALFORMED_JSON");
                return data;
            }

            foreach (
                KeyValuePair<string, Scalar> pair
                in receipt)
            {
                if (!ReceiptFields.Contains(pair.Key))
                {
                    Reject(
                        data,
                        "TRANSPORT_RECEIPT_UNEXPECTED_FIELD:" +
                        pair.Key);
                }
            }

            foreach (string field in ReceiptFields)
            {
                if (!receipt.ContainsKey(field))
                {
                    Reject(
                        data,
                        "TRANSPORT_RECEIPT_MISSING_FIELD:" +
                        field);
                }
            }

            if (data.RejectionReasons.Count > 0)
                return data;

            string schema =
                GetString(receipt, "schema");
            if (!string.Equals(
                    schema,
                    ReceiptSchema,
                    StringComparison.Ordinal))
            {
                Reject(
                    data,
                    "TRANSPORT_RECEIPT_SCHEMA_INVALID");
                return data;
            }

            data.TransportId =
                GetString(receipt, "transportId");
            data.TransportRequestId =
                GetString(receipt, "transportRequestId");
            data.ProviderRequestId =
                GetString(receipt, "providerRequestId");
            data.ProviderId =
                GetString(receipt, "providerId");
            data.ModelId =
                GetString(receipt, "modelId");
            data.AttemptId =
                GetString(receipt, "attemptId");
            data.RequestFingerprint =
                GetString(receipt, "requestFingerprint");
            data.RequestInputSha256 =
                GetString(receipt, "requestInputSha256");
            data.ResultStatus =
                GetString(receipt, "resultStatus");
            data.ResultSha256 =
                GetString(receipt, "resultSha256");

            if (!Present(data.TransportId) ||
                !Present(data.TransportRequestId) ||
                !Present(data.ProviderRequestId) ||
                !Present(data.ProviderId) ||
                !Present(data.ModelId) ||
                !Present(data.AttemptId) ||
                !Present(data.RequestFingerprint) ||
                !Present(data.RequestInputSha256) ||
                !Present(data.ResultStatus) ||
                !Present(data.ResultSha256))
            {
                Reject(
                    data,
                    "TRANSPORT_RECEIPT_IDENTITY_MISSING");
                return data;
            }

            if (!TryGetBoolean(
                    receipt,
                    "externalNetworkUsed",
                    out data.ExternalNetworkUsed) ||
                !TryGetBoolean(
                    receipt,
                    "modelInvoked",
                    out data.ModelInvoked) ||
                !TryGetBoolean(
                    receipt,
                    "success",
                    out data.ReceiptSuccess))
            {
                Reject(
                    data,
                    "TRANSPORT_RECEIPT_BOOLEAN_INVALID");
                return data;
            }

            if (IsNull(
                    receipt,
                    "errorCode"))
            {
                data.ErrorCode = null;
            }
            else
            {
                data.ErrorCode =
                    GetString(
                        receipt,
                        "errorCode");
            }

            if (!data.ReceiptSuccess)
            {
                Reject(
                    data,
                    "TRANSPORT_RECEIPT_NOT_SUCCESS");
                return data;
            }

            if (!string.IsNullOrWhiteSpace(
                    data.ErrorCode))
            {
                Reject(
                    data,
                    "TRANSPORT_RECEIPT_ERROR_PRESENT");
                return data;
            }

            if (resultBytes == null ||
                resultBytes.Length == 0 ||
                string.IsNullOrWhiteSpace(resultJson))
            {
                Reject(
                    data,
                    "PROVIDER_RESULT_BYTES_MISSING");
                return data;
            }

            data.ComputedResultSha256 =
                Sha256Upper(resultBytes);

            if (!string.Equals(
                    data.ResultSha256,
                    data.ComputedResultSha256,
                    StringComparison.Ordinal))
            {
                Reject(
                    data,
                    "RESULT_SHA256_MISMATCH");
                return data;
            }

            Dictionary<string, Scalar> result;
            if (!TryParseScalarObject(
                    resultJson,
                    out result))
            {
                Reject(
                    data,
                    "PROVIDER_RESULT_MALFORMED_JSON");
                return data;
            }

            if (!string.Equals(
                    GetString(result, "schema"),
                    ResultSchema,
                    StringComparison.Ordinal))
            {
                Reject(
                    data,
                    "PROVIDER_RESULT_SCHEMA_INVALID");
                return data;
            }

            string resultProviderId =
                GetString(result, "providerId");
            string resultAttemptId =
                GetString(result, "attemptId");
            string resultFingerprint =
                GetString(result, "requestFingerprint");
            string resultProviderRequestId =
                GetString(result, "providerRequestId");
            string resultTransportRequestId =
                GetString(result, "transportRequestId");
            string resultStatus =
                GetString(result, "status");

            if (!string.Equals(
                    data.ProviderId,
                    resultProviderId,
                    StringComparison.Ordinal))
                Reject(data, "RECEIPT_RESULT_PROVIDER_MISMATCH");

            if (!string.Equals(
                    data.AttemptId,
                    resultAttemptId,
                    StringComparison.Ordinal))
                Reject(data, "RECEIPT_RESULT_ATTEMPT_MISMATCH");

            if (!string.Equals(
                    data.RequestFingerprint,
                    resultFingerprint,
                    StringComparison.Ordinal))
                Reject(data, "RECEIPT_RESULT_FINGERPRINT_MISMATCH");

            if (!string.Equals(
                    data.ProviderRequestId,
                    resultProviderRequestId,
                    StringComparison.Ordinal))
                Reject(data, "RECEIPT_RESULT_PROVIDER_REQUEST_MISMATCH");

            if (!string.Equals(
                    data.TransportRequestId,
                    resultTransportRequestId,
                    StringComparison.Ordinal))
                Reject(data, "RECEIPT_RESULT_TRANSPORT_REQUEST_MISMATCH");

            if (!string.Equals(
                    data.ResultStatus,
                    resultStatus,
                    StringComparison.Ordinal))
                Reject(data, "RECEIPT_RESULT_STATUS_MISMATCH");

            if (data.RejectionReasons.Count > 0)
                return data;

            if (queue == null)
            {
                Reject(
                    data,
                    "DISPATCH_QUEUE_MISSING");
                return data;
            }

            PatrolDefenseProviderTransportReceiptMatchResult
                receiptMatch =
                    queue.MatchRegisteredTransportReceipt(
                        data.RequestFingerprint,
                        data.ProviderId,
                        data.ModelId,
                        data.AttemptId,
                        data.ProviderRequestId,
                        data.RequestInputSha256,
                        data.TransportId,
                        data.TransportRequestId);

            data.TransportReceiptMatchReason =
                receiptMatch == null
                    ? "TRANSPORT_RECEIPT_MATCH_UNAVAILABLE"
                    : receiptMatch.Reason;

            if (receiptMatch == null ||
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
                PatrolDefenseProviderResultV3Admission.EvaluateRequestBound(
                    gate,
                    queue,
                    resultJson);

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
            PatrolDefenseProviderTransportReceiptResultBindingData data)
        {
            if (data == null)
                return null;

            StringBuilder sb =
                new StringBuilder();

            sb.Append("{");
            sb.Append(
                "\"schema\":\"BannerlordAI.PatrolDefenseProviderTransportReceiptResultBinding.v1\"");
            sb.Append(",\"mode\":\"observe\"");
            sb.Append(",\"transportId\":");
            sb.Append(Json(data.TransportId));
            sb.Append(",\"transportRequestId\":");
            sb.Append(Json(data.TransportRequestId));
            sb.Append(",\"providerRequestId\":");
            sb.Append(Json(data.ProviderRequestId));
            sb.Append(",\"providerId\":");
            sb.Append(Json(data.ProviderId));
            sb.Append(",\"modelId\":");
            sb.Append(Json(data.ModelId));
            sb.Append(",\"attemptId\":");
            sb.Append(Json(data.AttemptId));
            sb.Append(",\"requestFingerprint\":");
            sb.Append(Json(data.RequestFingerprint));
            sb.Append(",\"requestInputSha256\":");
            sb.Append(Json(data.RequestInputSha256));
            sb.Append(",\"resultStatus\":");
            sb.Append(Json(data.ResultStatus));
            sb.Append(",\"resultSha256\":");
            sb.Append(Json(data.ResultSha256));
            sb.Append(",\"computedResultSha256\":");
            sb.Append(Json(data.ComputedResultSha256));
            sb.Append(",\"transportReceiptMatchReason\":");
            sb.Append(Json(data.TransportReceiptMatchReason));
            sb.Append(",\"bindingAccepted\":");
            sb.Append(
                data.BindingAccepted
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
            sb.Append(",\"externalNetworkUsed\":");
            sb.Append(
                data.ExternalNetworkUsed
                    ? "true"
                    : "false");
            sb.Append(",\"modelInvoked\":");
            sb.Append(
                data.ModelInvoked
                    ? "true"
                    : "false");
            sb.Append(",\"receiptSuccess\":");
            sb.Append(
                data.ReceiptSuccess
                    ? "true"
                    : "false");
            sb.Append(",\"errorCode\":");
            sb.Append(Json(data.ErrorCode));
            sb.Append(",\"providerResultAdmission\":");
            string nested =
                PatrolDefenseProviderResultV3Admission.ToJson(
                    data.ProviderResultAdmission);
            sb.Append(
                string.IsNullOrWhiteSpace(nested)
                    ? "null"
                    : nested);
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
