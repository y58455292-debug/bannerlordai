using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace BannerlordAITestRunner
{
    internal sealed class PatrolDefenseReconsiderationEvidenceData
    {
        internal bool FirstObservation;
        internal bool FactualContextChanged;
        internal bool ReconsiderationCandidate;
        internal string[] ChangedComponents;

        internal string PreviousIdentityFingerprint;
        internal string PreviousThreatFingerprint;
        internal string PreviousPersistentMemoryFingerprint;
        internal string PreviousRecentOutcomeFingerprint;

        internal string CurrentIdentityFingerprint;
        internal string CurrentThreatFingerprint;
        internal string CurrentPersistentMemoryFingerprint;
        internal string CurrentRecentOutcomeFingerprint;
    }

    internal sealed class PatrolDefenseReconsiderationTracker
    {
        private bool _hasPrevious;
        private string _identity;
        private string _threat;
        private string _persistentMemory;
        private string _recentOutcome;

        internal void Reset()
        {
            _hasPrevious = false;
            _identity = null;
            _threat = null;
            _persistentMemory = null;
            _recentOutcome = null;
        }

        private static string HashMaterial(string material)
        {
            if (material == null)
                return null;

            byte[] bytes = Encoding.UTF8.GetBytes(material);
            byte[] hash;
            using (SHA256 sha = SHA256.Create())
            {
                hash = sha.ComputeHash(bytes);
            }

            StringBuilder sb = new StringBuilder(hash.Length * 2);
            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString(
                    "X2",
                    CultureInfo.InvariantCulture));
            }
            return sb.ToString();
        }

        private static bool Same(string a, string b)
        {
            return string.Equals(a, b, StringComparison.Ordinal);
        }

        internal PatrolDefenseReconsiderationEvidenceData Evaluate(
            string identityMaterial,
            string threatMaterial,
            string persistentMemoryMaterial,
            string recentOutcomeMaterial)
        {
            string identity = HashMaterial(identityMaterial);
            string threat = HashMaterial(threatMaterial);
            string persistentMemory = HashMaterial(persistentMemoryMaterial);
            string recentOutcome = HashMaterial(recentOutcomeMaterial);

            PatrolDefenseReconsiderationEvidenceData data =
                new PatrolDefenseReconsiderationEvidenceData();

            data.FirstObservation = !_hasPrevious;

            data.PreviousIdentityFingerprint = _identity;
            data.PreviousThreatFingerprint = _threat;
            data.PreviousPersistentMemoryFingerprint = _persistentMemory;
            data.PreviousRecentOutcomeFingerprint = _recentOutcome;

            data.CurrentIdentityFingerprint = identity;
            data.CurrentThreatFingerprint = threat;
            data.CurrentPersistentMemoryFingerprint = persistentMemory;
            data.CurrentRecentOutcomeFingerprint = recentOutcome;

            List<string> changed = new List<string>();

            if (_hasPrevious)
            {
                if (!Same(_identity, identity))
                    changed.Add("identity");
                if (!Same(_threat, threat))
                    changed.Add("currentThreat");
                if (!Same(_persistentMemory, persistentMemory))
                    changed.Add("persistentMemory");
                if (!Same(_recentOutcome, recentOutcome))
                    changed.Add("recentOutcome");
            }

            data.ChangedComponents = changed.ToArray();
            data.FactualContextChanged = changed.Count > 0;
            data.ReconsiderationCandidate =
                data.FirstObservation ||
                data.FactualContextChanged;

            _identity = identity;
            _threat = threat;
            _persistentMemory = persistentMemory;
            _recentOutcome = recentOutcome;
            _hasPrevious = true;

            return data;
        }
    }

    internal static class PatrolDefenseReconsiderationEvidenceBuilder
    {
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

        private static void AppendPart(StringBuilder sb, string value)
        {
            if (value == null)
            {
                sb.Append("-1:");
                sb.Append(";");
                return;
            }

            byte[] bytes = Encoding.UTF8.GetBytes(value);
            sb.Append(bytes.Length.ToString(
                CultureInfo.InvariantCulture));
            sb.Append(":");
            sb.Append(value);
            sb.Append(";");
        }

        internal static string BuildIdentityMaterial(
            string actorId,
            string branchId,
            string settlementId,
            string banditId)
        {
            StringBuilder sb = new StringBuilder();
            AppendPart(sb, actorId);
            AppendPart(sb, branchId);
            AppendPart(sb, settlementId);
            AppendPart(sb, banditId);
            return sb.ToString();
        }

        internal static string BuildThreatMaterial(
            double actorStrength,
            int actorHealthy,
            double banditStrength,
            int banditHealthy,
            double ratio)
        {
            StringBuilder sb = new StringBuilder();
            AppendPart(
                sb,
                actorStrength.ToString(
                    "R",
                    CultureInfo.InvariantCulture));
            AppendPart(
                sb,
                actorHealthy.ToString(
                    CultureInfo.InvariantCulture));
            AppendPart(
                sb,
                banditStrength.ToString(
                    "R",
                    CultureInfo.InvariantCulture));
            AppendPart(
                sb,
                banditHealthy.ToString(
                    CultureInfo.InvariantCulture));
            AppendPart(
                sb,
                ratio.ToString(
                    "R",
                    CultureInfo.InvariantCulture));
            return sb.ToString();
        }

        internal static string BuildPersistentMemoryMaterial(
            string recognition,
            string requestedSettlementId,
            string requestedBanditId,
            string priorEpisodeJson)
        {
            if (string.IsNullOrWhiteSpace(recognition) &&
                string.IsNullOrWhiteSpace(requestedSettlementId) &&
                string.IsNullOrWhiteSpace(requestedBanditId) &&
                string.IsNullOrWhiteSpace(priorEpisodeJson))
                return null;

            StringBuilder sb = new StringBuilder();
            AppendPart(sb, recognition);
            AppendPart(sb, requestedSettlementId);
            AppendPart(sb, requestedBanditId);
            AppendPart(sb, priorEpisodeJson);
            return sb.ToString();
        }

        internal static string BuildRecentOutcomeMaterial(
            string result,
            string eventKey)
        {
            if (string.IsNullOrWhiteSpace(result) &&
                string.IsNullOrWhiteSpace(eventKey))
                return null;

            StringBuilder sb = new StringBuilder();
            AppendPart(sb, result);
            AppendPart(sb, eventKey);
            return sb.ToString();
        }

        internal static string ToJson(
            PatrolDefenseReconsiderationEvidenceData data)
        {
            if (data == null)
                return null;

            StringBuilder sb = new StringBuilder();
            sb.Append("{");
            sb.Append(
                "\"schema\":\"BannerlordAI.PatrolDefenseReconsiderationEvidence.v1\"");
            sb.Append(",\"mode\":\"observe\"");
            sb.Append(",\"firstObservation\":");
            sb.Append(data.FirstObservation ? "true" : "false");
            sb.Append(",\"factualContextChanged\":");
            sb.Append(data.FactualContextChanged ? "true" : "false");
            sb.Append(",\"reconsiderationCandidate\":");
            sb.Append(data.ReconsiderationCandidate ? "true" : "false");
            sb.Append(",\"changedComponents\":[");
            for (int i = 0; i < data.ChangedComponents.Length; i++)
            {
                if (i > 0)
                    sb.Append(",");
                sb.Append(Json(data.ChangedComponents[i]));
            }
            sb.Append("]");

            sb.Append(",\"previousComponentFingerprints\":{");
            sb.Append("\"identity\":");
            sb.Append(Json(data.PreviousIdentityFingerprint));
            sb.Append(",\"currentThreat\":");
            sb.Append(Json(data.PreviousThreatFingerprint));
            sb.Append(",\"persistentMemory\":");
            sb.Append(Json(data.PreviousPersistentMemoryFingerprint));
            sb.Append(",\"recentOutcome\":");
            sb.Append(Json(data.PreviousRecentOutcomeFingerprint));
            sb.Append("}");

            sb.Append(",\"currentComponentFingerprints\":{");
            sb.Append("\"identity\":");
            sb.Append(Json(data.CurrentIdentityFingerprint));
            sb.Append(",\"currentThreat\":");
            sb.Append(Json(data.CurrentThreatFingerprint));
            sb.Append(",\"persistentMemory\":");
            sb.Append(Json(data.CurrentPersistentMemoryFingerprint));
            sb.Append(",\"recentOutcome\":");
            sb.Append(Json(data.CurrentRecentOutcomeFingerprint));
            sb.Append("}");

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
            string reconsiderationEvidenceJson)
        {
            if (string.IsNullOrWhiteSpace(shadowReceiptJson) ||
                string.IsNullOrWhiteSpace(reconsiderationEvidenceJson) ||
                shadowReceiptJson.Length < 2 ||
                shadowReceiptJson[shadowReceiptJson.Length - 1] != '}' ||
                reconsiderationEvidenceJson[0] != '{')
                return shadowReceiptJson;

            return
                shadowReceiptJson.Substring(
                    0,
                    shadowReceiptJson.Length - 1) +
                ",\"reconsiderationEvidence\":" +
                reconsiderationEvidenceJson +
                "}";
        }
    }
}
