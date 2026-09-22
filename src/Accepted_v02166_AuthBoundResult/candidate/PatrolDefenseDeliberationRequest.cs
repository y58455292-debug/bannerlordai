using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace BannerlordAITestRunner
{
    internal sealed class PatrolDefenseDeliberationRequestData
    {
        internal string RequestFingerprint;
        internal string ActorKnownContextJson;
        internal string ReconsiderationEvidenceJson;
        internal bool WouldInterrupt;
        internal bool SafeToIntervene;
        internal string Reason;
        internal string CandidateAction;
        internal string ResumeAction;
    }

    internal static class PatrolDefenseDeliberationRequestBuilder
    {
        internal const string Source =
            "PatrolDefenseShadow.v02141.deliberation_request";

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

        private static void AppendPart(
            StringBuilder sb,
            string value)
        {
            if (value == null)
            {
                sb.Append("-1:;");
                return;
            }

            byte[] bytes =
                Encoding.UTF8.GetBytes(value);
            sb.Append(
                bytes.Length.ToString(
                    CultureInfo.InvariantCulture));
            sb.Append(":");
            sb.Append(value);
            sb.Append(";");
        }

        private static string Fingerprint(
            PatrolDefenseReconsiderationEvidenceData reconsideration,
            bool wouldInterrupt,
            bool safeToIntervene,
            string reason,
            string candidateAction,
            string resumeAction)
        {
            StringBuilder material =
                new StringBuilder();

            AppendPart(
                material,
                reconsideration.CurrentIdentityFingerprint);
            AppendPart(
                material,
                reconsideration.CurrentThreatFingerprint);
            AppendPart(
                material,
                reconsideration.CurrentPersistentMemoryFingerprint);
            AppendPart(
                material,
                reconsideration.CurrentRecentOutcomeFingerprint);
            AppendPart(
                material,
                wouldInterrupt ? "1" : "0");
            AppendPart(
                material,
                safeToIntervene ? "1" : "0");
            AppendPart(material, reason);
            AppendPart(material, candidateAction);
            AppendPart(material, resumeAction);

            byte[] payload =
                Encoding.UTF8.GetBytes(
                    material.ToString());
            byte[] hash;
            using (
                SHA256 sha =
                    SHA256.Create())
            {
                hash =
                    sha.ComputeHash(payload);
            }

            StringBuilder hex =
                new StringBuilder(
                    hash.Length * 2);
            for (int i = 0; i < hash.Length; i++)
            {
                hex.Append(
                    hash[i].ToString(
                        "X2",
                        CultureInfo.InvariantCulture));
            }
            return hex.ToString();
        }

        internal static PatrolDefenseDeliberationRequestData Build(
            string actorKnownContextJson,
            string reconsiderationEvidenceJson,
            PatrolDefenseReconsiderationEvidenceData reconsideration,
            bool wouldInterrupt,
            bool safeToIntervene,
            string reason,
            string candidateAction,
            string resumeAction)
        {
            if (string.IsNullOrWhiteSpace(
                    actorKnownContextJson) ||
                string.IsNullOrWhiteSpace(
                    reconsiderationEvidenceJson) ||
                reconsideration == null ||
                !reconsideration.ReconsiderationCandidate)
            {
                return null;
            }

            PatrolDefenseDeliberationRequestData data =
                new PatrolDefenseDeliberationRequestData();

            data.ActorKnownContextJson =
                actorKnownContextJson;
            data.ReconsiderationEvidenceJson =
                reconsiderationEvidenceJson;
            data.WouldInterrupt =
                wouldInterrupt;
            data.SafeToIntervene =
                safeToIntervene;
            data.Reason =
                reason;
            data.CandidateAction =
                candidateAction;
            data.ResumeAction =
                resumeAction;
            data.RequestFingerprint =
                Fingerprint(
                    reconsideration,
                    wouldInterrupt,
                    safeToIntervene,
                    reason,
                    candidateAction,
                    resumeAction);

            return data;
        }

        internal static string ToJson(
            PatrolDefenseDeliberationRequestData data)
        {
            if (data == null)
                return null;

            StringBuilder sb =
                new StringBuilder();

            sb.Append("{");
            sb.Append(
                "\"schema\":\"BannerlordAI.PatrolDefenseDeliberationRequest.v1\"");
            sb.Append(",\"mode\":\"observe\"");
            sb.Append(",\"source\":");
            sb.Append(Json(Source));
            sb.Append(",\"requestEligible\":true");
            sb.Append(",\"requestFingerprint\":");
            sb.Append(Json(data.RequestFingerprint));
            sb.Append(",\"actorKnownContext\":");
            sb.Append(data.ActorKnownContextJson);
            sb.Append(",\"reconsiderationEvidence\":");
            sb.Append(data.ReconsiderationEvidenceJson);
            sb.Append(",\"baselineDecision\":{");
            sb.Append("\"wouldInterrupt\":");
            sb.Append(
                data.WouldInterrupt
                    ? "true"
                    : "false");
            sb.Append(",\"safeToIntervene\":");
            sb.Append(
                data.SafeToIntervene
                    ? "true"
                    : "false");
            sb.Append(",\"reason\":");
            sb.Append(Json(data.Reason));
            sb.Append(",\"candidateAction\":");
            sb.Append(Json(data.CandidateAction));
            sb.Append(",\"resumeAction\":");
            sb.Append(Json(data.ResumeAction));
            sb.Append("}");
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

        internal static string AttachToShadow(
            string shadowReceiptJson,
            string requestJson)
        {
            if (string.IsNullOrWhiteSpace(
                    shadowReceiptJson) ||
                string.IsNullOrWhiteSpace(
                    requestJson) ||
                shadowReceiptJson.Length < 2 ||
                shadowReceiptJson[
                    shadowReceiptJson.Length - 1] != '}' ||
                requestJson[0] != '{')
            {
                return shadowReceiptJson;
            }

            return
                shadowReceiptJson.Substring(
                    0,
                    shadowReceiptJson.Length - 1) +
                ",\"deliberationRequest\":" +
                requestJson +
                "}";
        }
    }
}
