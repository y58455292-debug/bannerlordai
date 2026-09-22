using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace BannerlordAITestRunner
{
    internal sealed class PatrolDefenseProviderRequestRegistrationData
    {
        internal string ProviderRequestId;
        internal string ProviderId;
        internal string ModelId;
        internal string AttemptId;
        internal string RequestFingerprint;
        internal string PromptContractVersion;
        internal string ResponseSchema;
        internal string InputSha256;
        internal bool Registered;
        internal bool Idempotent;
        internal string Reason;
        internal int QueueCount;
    }

    internal static class PatrolDefenseProviderRequestRegistration
    {
        internal const string RequestSchema =
            "BannerlordAI.PatrolDefenseProviderRequest.v1";
        internal const string ExpectedPromptContractVersion =
            "BannerlordAI.PatrolDefenseAdvisoryPrompt.v1";
        internal const string ExpectedResponseSchema =
            "BannerlordAI.PatrolDefenseDeliberationAdvisory.v1";

        private static readonly HashSet<string> AllowedFields =
            new HashSet<string>(
                new[]
                {
                    "schema",
                    "providerId",
                    "modelId",
                    "attemptId",
                    "requestFingerprint",
                    "promptContractVersion",
                    "responseSchema",
                    "deliberationRequestBase64",
                    "inputSha256"
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

        private static string Sha256Upper(byte[] bytes)
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

        internal static PatrolDefenseProviderRequestRegistrationData
            Evaluate(
                PatrolDefenseProviderDispatchQueue queue,
                string envelopeJson)
        {
            PatrolDefenseProviderRequestRegistrationData data =
                new PatrolDefenseProviderRequestRegistrationData();

            Dictionary<string, string> fields;
            if (!TryParseFlatStringObject(
                    envelopeJson,
                    out fields))
            {
                data.Reason = "MALFORMED_JSON";
                return data;
            }

            foreach (
                KeyValuePair<string, string> pair
                in fields)
            {
                if (!AllowedFields.Contains(pair.Key))
                {
                    data.Reason =
                        "UNEXPECTED_FIELD:" + pair.Key;
                    return data;
                }
            }

            foreach (string field in AllowedFields)
            {
                if (!fields.ContainsKey(field))
                {
                    data.Reason =
                        "MISSING_FIELD:" + field;
                    return data;
                }
            }

            if (!string.Equals(
                    fields["schema"],
                    RequestSchema,
                    StringComparison.Ordinal))
            {
                data.Reason = "SCHEMA_INVALID";
                return data;
            }

            data.ProviderId = fields["providerId"];
            data.ModelId = fields["modelId"];
            data.AttemptId = fields["attemptId"];
            data.RequestFingerprint =
                fields["requestFingerprint"];
            data.PromptContractVersion =
                fields["promptContractVersion"];
            data.ResponseSchema =
                fields["responseSchema"];
            data.InputSha256 =
                fields["inputSha256"];

            if (!Present(data.ProviderId))
            {
                data.Reason = "PROVIDER_ID_MISSING";
                return data;
            }
            if (!Present(data.ModelId))
            {
                data.Reason = "MODEL_ID_MISSING";
                return data;
            }
            if (!Present(data.AttemptId))
            {
                data.Reason = "ATTEMPT_ID_MISSING";
                return data;
            }
            if (!Present(data.RequestFingerprint))
            {
                data.Reason =
                    "REQUEST_FINGERPRINT_MISSING";
                return data;
            }
            if (!string.Equals(
                    data.PromptContractVersion,
                    ExpectedPromptContractVersion,
                    StringComparison.Ordinal))
            {
                data.Reason =
                    "PROMPT_CONTRACT_VERSION_INVALID";
                return data;
            }
            if (!string.Equals(
                    data.ResponseSchema,
                    ExpectedResponseSchema,
                    StringComparison.Ordinal))
            {
                data.Reason =
                    "RESPONSE_SCHEMA_INVALID";
                return data;
            }

            byte[] requestBytes;
            try
            {
                requestBytes =
                    Convert.FromBase64String(
                        fields["deliberationRequestBase64"]);
            }
            catch (FormatException)
            {
                data.Reason =
                    "DELIBERATION_REQUEST_BASE64_INVALID";
                return data;
            }

            string decodedRequestJson;
            try
            {
                decodedRequestJson =
                    new UTF8Encoding(false, true)
                        .GetString(requestBytes);
            }
            catch (DecoderFallbackException)
            {
                data.Reason =
                    "DELIBERATION_REQUEST_UTF8_INVALID";
                return data;
            }

            string actualInputSha =
                Sha256Upper(requestBytes);
            if (!string.Equals(
                    data.InputSha256,
                    actualInputSha,
                    StringComparison.Ordinal))
            {
                data.Reason =
                    "INPUT_SHA256_MISMATCH";
                return data;
            }

            data.ProviderRequestId =
                Sha256Upper(
                    Encoding.UTF8.GetBytes(
                        envelopeJson));

            if (queue == null)
            {
                data.Reason =
                    "DISPATCH_QUEUE_MISSING";
                return data;
            }

            PatrolDefenseProviderRequestRegistrationResult
                registration =
                    queue.RegisterProviderRequest(
                        data.RequestFingerprint,
                        data.ProviderId,
                        data.ModelId,
                        data.AttemptId,
                        data.PromptContractVersion,
                        data.ResponseSchema,
                        decodedRequestJson,
                        data.InputSha256,
                        data.ProviderRequestId);

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
            PatrolDefenseProviderRequestRegistrationData data)
        {
            if (data == null)
                return null;

            StringBuilder sb =
                new StringBuilder();

            sb.Append("{");
            sb.Append(
                "\"schema\":\"BannerlordAI.PatrolDefenseProviderRequestRegistration.v1\"");
            sb.Append(",\"mode\":\"observe\"");
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
            sb.Append(",\"promptContractVersion\":");
            sb.Append(
                Json(data.PromptContractVersion));
            sb.Append(",\"responseSchema\":");
            sb.Append(Json(data.ResponseSchema));
            sb.Append(",\"inputSha256\":");
            sb.Append(Json(data.InputSha256));
            sb.Append(",\"registered\":");
            sb.Append(data.Registered ? "true" : "false");
            sb.Append(",\"idempotent\":");
            sb.Append(data.Idempotent ? "true" : "false");
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
