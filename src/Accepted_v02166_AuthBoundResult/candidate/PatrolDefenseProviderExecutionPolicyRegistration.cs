using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace BannerlordAITestRunner
{
    internal sealed class PatrolDefenseProviderExecutionPolicyRegistrationData
    {
        internal string ExecutionPolicyId;
        internal string TransportRequestId;
        internal string ProviderRequestId;
        internal string ProviderId;
        internal string ModelId;
        internal string AttemptId;
        internal string RequestFingerprint;
        internal string TimeoutMs;
        internal string MaxAttempts;
        internal string MaxResultBytes;
        internal string CredentialRef;
        internal bool Registered;
        internal bool Idempotent;
        internal string Reason;
        internal int QueueCount;
    }

    internal static class PatrolDefenseProviderExecutionPolicyRegistration
    {
        internal const string PolicySchema =
            "BannerlordAI.PatrolDefenseProviderExecutionPolicy.v1";

        private static readonly HashSet<string> AllowedFields =
            new HashSet<string>(
                new[]
                {
                    "schema",
                    "transportRequestId",
                    "providerRequestId",
                    "providerId",
                    "modelId",
                    "attemptId",
                    "requestFingerprint",
                    "timeoutMs",
                    "maxAttempts",
                    "maxResultBytes",
                    "credentialRef"
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

        private static bool PositiveInt(
            string value)
        {
            long parsed;

            return
                long.TryParse(
                    value,
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out parsed) &&
                parsed > 0;
        }

        private static bool ValidCredentialRef(
            string value)
        {
            if (string.IsNullOrWhiteSpace(value) ||
                !value.StartsWith(
                    "env:",
                    StringComparison.Ordinal))
                return false;

            string name =
                value.Substring(4);

            if (name.Length == 0)
                return false;

            for (int i = 0; i < name.Length; i++)
            {
                char ch = name[i];

                bool allowed =
                    (ch >= 'A' && ch <= 'Z') ||
                    (ch >= 'a' && ch <= 'z') ||
                    (ch >= '0' && ch <= '9') ||
                    ch == '_';

                if (!allowed)
                    return false;
            }

            return true;
        }

        internal static PatrolDefenseProviderExecutionPolicyRegistrationData
            Evaluate(
                PatrolDefenseProviderDispatchQueue queue,
                string policyJson)
        {
            PatrolDefenseProviderExecutionPolicyRegistrationData data =
                new PatrolDefenseProviderExecutionPolicyRegistrationData();

            Dictionary<string, string> fields;
            if (!TryParseFlatStringObject(
                    policyJson,
                    out fields))
            {
                data.Reason =
                    "MALFORMED_JSON";
                return data;
            }

            foreach (
                KeyValuePair<string, string> pair
                in fields)
            {
                if (!AllowedFields.Contains(pair.Key))
                {
                    data.Reason =
                        "UNEXPECTED_FIELD:" +
                        pair.Key;
                    return data;
                }
            }

            foreach (string field in AllowedFields)
            {
                if (!fields.ContainsKey(field))
                {
                    data.Reason =
                        "MISSING_FIELD:" +
                        field;
                    return data;
                }
            }

            if (!string.Equals(
                    fields["schema"],
                    PolicySchema,
                    StringComparison.Ordinal))
            {
                data.Reason =
                    "SCHEMA_INVALID";
                return data;
            }

            data.TransportRequestId =
                fields["transportRequestId"];
            data.ProviderRequestId =
                fields["providerRequestId"];
            data.ProviderId =
                fields["providerId"];
            data.ModelId =
                fields["modelId"];
            data.AttemptId =
                fields["attemptId"];
            data.RequestFingerprint =
                fields["requestFingerprint"];
            data.TimeoutMs =
                fields["timeoutMs"];
            data.MaxAttempts =
                fields["maxAttempts"];
            data.MaxResultBytes =
                fields["maxResultBytes"];
            data.CredentialRef =
                fields["credentialRef"];

            if (!Present(data.TransportRequestId) ||
                !Present(data.ProviderRequestId) ||
                !Present(data.ProviderId) ||
                !Present(data.ModelId) ||
                !Present(data.AttemptId) ||
                !Present(data.RequestFingerprint))
            {
                data.Reason =
                    "PROVENANCE_IDENTITY_MISSING";
                return data;
            }

            if (!PositiveInt(data.TimeoutMs))
            {
                data.Reason =
                    "TIMEOUT_MS_INVALID";
                return data;
            }

            if (!PositiveInt(data.MaxAttempts))
            {
                data.Reason =
                    "MAX_ATTEMPTS_INVALID";
                return data;
            }

            if (!PositiveInt(data.MaxResultBytes))
            {
                data.Reason =
                    "MAX_RESULT_BYTES_INVALID";
                return data;
            }

            if (!ValidCredentialRef(
                    data.CredentialRef))
            {
                data.Reason =
                    "CREDENTIAL_REF_INVALID";
                return data;
            }

            data.ExecutionPolicyId =
                Sha256Upper(
                    Encoding.UTF8.GetBytes(
                        policyJson));

            if (queue == null)
            {
                data.Reason =
                    "DISPATCH_QUEUE_MISSING";
                return data;
            }

            PatrolDefenseProviderExecutionPolicyRegistrationResult
                registration =
                    queue.RegisterExecutionPolicy(
                        data.RequestFingerprint,
                        data.ProviderId,
                        data.ModelId,
                        data.AttemptId,
                        data.ProviderRequestId,
                        data.TransportRequestId,
                        data.TimeoutMs,
                        data.MaxAttempts,
                        data.MaxResultBytes,
                        data.CredentialRef,
                        data.ExecutionPolicyId);

            if (registration == null)
            {
                data.Reason =
                    "REGISTRATION_UNAVAILABLE";
                return data;
            }

            data.Registered =
                registration.Registered;
            data.Idempotent =
                registration.Idempotent;
            data.Reason =
                registration.Reason;
            data.QueueCount =
                registration.QueueCount;

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
            PatrolDefenseProviderExecutionPolicyRegistrationData data)
        {
            if (data == null)
                return null;

            StringBuilder sb =
                new StringBuilder();

            sb.Append("{");
            sb.Append(
                "\"schema\":\"BannerlordAI.PatrolDefenseProviderExecutionPolicyRegistration.v1\"");
            sb.Append(",\"mode\":\"observe\"");
            sb.Append(",\"executionPolicyId\":");
            sb.Append(Json(data.ExecutionPolicyId));
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
            sb.Append(",\"timeoutMs\":");
            sb.Append(Json(data.TimeoutMs));
            sb.Append(",\"maxAttempts\":");
            sb.Append(Json(data.MaxAttempts));
            sb.Append(",\"maxResultBytes\":");
            sb.Append(Json(data.MaxResultBytes));
            sb.Append(",\"credentialRef\":");
            sb.Append(Json(data.CredentialRef));
            sb.Append(",\"registered\":");
            sb.Append(
                data.Registered
                    ? "true"
                    : "false");
            sb.Append(",\"idempotent\":");
            sb.Append(
                data.Idempotent
                    ? "true"
                    : "false");
            sb.Append(",\"reason\":");
            sb.Append(Json(data.Reason));
            sb.Append(",\"queueCount\":");
            sb.Append(
                data.QueueCount.ToString(
                    CultureInfo.InvariantCulture));
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
