using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace BannerlordAITestRunner
{
    internal sealed class
        PatrolDefenseDeliberationAdvisoryAdmissionData
    {
        internal bool Admitted;
        internal string RequestFingerprint;
        internal string Disposition;
        internal List<string> RejectionReasons =
            new List<string>();
    }

    internal static class
        PatrolDefenseDeliberationAdvisoryAdmission
    {
        internal const string AdvisorySchema =
            "BannerlordAI.PatrolDefenseDeliberationAdvisory.v1";

        private static readonly HashSet<string>
            AllowedDispositions =
                new HashSet<string>(
                    new[]
                    {
                        "KEEP_BASELINE",
                        "REVIEW_ALTERNATIVE",
                        "ABSTAIN"
                    },
                    StringComparer.Ordinal);

        private static readonly HashSet<string>
            AllowedFields =
                new HashSet<string>(
                    new[]
                    {
                        "schema",
                        "requestFingerprint",
                        "disposition"
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
            {
                return false;
            }

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
                        {
                            return false;
                        }
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
            {
                return false;
            }

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
                {
                    return false;
                }

                if (fields.ContainsKey(key))
                    return false;

                SkipWhitespace(json, ref index);

                if (
                    index >= json.Length ||
                    json[index] != ':')
                {
                    return false;
                }

                index++;
                SkipWhitespace(json, ref index);

                string value;
                if (!TryReadJsonString(
                        json,
                        ref index,
                        out value))
                {
                    return false;
                }

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
            PatrolDefenseDeliberationAdvisoryAdmissionData data,
            string reason)
        {
            if (
                data != null &&
                !data.RejectionReasons.Contains(reason))
            {
                data.RejectionReasons.Add(reason);
            }
        }

        internal static
            PatrolDefenseDeliberationAdvisoryAdmissionData
            Evaluate(
                string expectedRequestFingerprint,
                string responseJson)
        {
            PatrolDefenseDeliberationAdvisoryAdmissionData data =
                new PatrolDefenseDeliberationAdvisoryAdmissionData();

            data.RequestFingerprint =
                expectedRequestFingerprint;

            if (string.IsNullOrWhiteSpace(
                    expectedRequestFingerprint))
            {
                Reject(
                    data,
                    "EXPECTED_REQUEST_FINGERPRINT_MISSING");
            }

            Dictionary<string, string> fields;
            if (!TryParseFlatStringObject(
                    responseJson,
                    out fields))
            {
                Reject(
                    data,
                    "MALFORMED_JSON");
                data.Admitted = false;
                return data;
            }

            foreach (
                KeyValuePair<string, string> pair
                in fields)
            {
                if (!AllowedFields.Contains(pair.Key))
                {
                    Reject(
                        data,
                        "UNEXPECTED_FIELD:" + pair.Key);
                }
            }

            string schema;
            if (!fields.TryGetValue(
                    "schema",
                    out schema))
            {
                Reject(
                    data,
                    "SCHEMA_MISSING");
            }
            else if (!string.Equals(
                        schema,
                        AdvisorySchema,
                        StringComparison.Ordinal))
            {
                Reject(
                    data,
                    "SCHEMA_INVALID");
            }

            string responseFingerprint;
            if (!fields.TryGetValue(
                    "requestFingerprint",
                    out responseFingerprint))
            {
                Reject(
                    data,
                    "REQUEST_FINGERPRINT_MISSING");
            }
            else if (!string.Equals(
                        responseFingerprint,
                        expectedRequestFingerprint,
                        StringComparison.Ordinal))
            {
                Reject(
                    data,
                    "REQUEST_FINGERPRINT_MISMATCH");
            }

            string disposition;
            if (!fields.TryGetValue(
                    "disposition",
                    out disposition))
            {
                Reject(
                    data,
                    "DISPOSITION_MISSING");
            }
            else if (!AllowedDispositions.Contains(
                        disposition))
            {
                Reject(
                    data,
                    "DISPOSITION_INVALID");
            }

            data.Admitted =
                data.RejectionReasons.Count == 0;
            data.Disposition =
                data.Admitted
                    ? disposition
                    : null;

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
            PatrolDefenseDeliberationAdvisoryAdmissionData data)
        {
            if (data == null)
                return null;

            StringBuilder sb =
                new StringBuilder();

            sb.Append("{");
            sb.Append(
                "\"schema\":\"BannerlordAI.PatrolDefenseDeliberationAdvisoryAdmission.v1\"");
            sb.Append(",\"mode\":\"observe\"");
            sb.Append(",\"requestFingerprint\":");
            sb.Append(
                Json(
                    data.RequestFingerprint));
            sb.Append(",\"admitted\":");
            sb.Append(
                data.Admitted
                    ? "true"
                    : "false");
            sb.Append(",\"disposition\":");
            sb.Append(
                Json(
                    data.Disposition));
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
            sb.Append(",\"advisoryOnly\":true");
            sb.Append(",\"executionAuthorized\":false");
            sb.Append(",\"modelInvoked\":false");
            sb.Append(",\"llmInvoked\":false");
            sb.Append(",\"plannerInvoked\":false");
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
