using System;
using System.Text;

namespace BannerlordAITestRunner
{
    internal static class PatrolDefenseActorKnownContextBuilder
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

        private static string RawOrNull(string raw)
        {
            return string.IsNullOrWhiteSpace(raw)
                ? "null"
                : raw;
        }

        internal static string Build(
            string actorId,
            string branchId,
            string settlementId,
            string banditId,
            string currentThreatJson,
            string persistentMemoryContextJson,
            string memoryStateComparisonJson,
            string recentOutcomeContextJson)
        {
            StringBuilder sb=new StringBuilder();
            sb.Append("{");
            sb.Append("\"schema\":\"BannerlordAI.PatrolDefenseActorKnownContext.v1\"");
            sb.Append(",\"mode\":\"observe\"");
            sb.Append(",\"actorId\":"); sb.Append(Json(actorId));
            sb.Append(",\"branchId\":"); sb.Append(Json(branchId));
            sb.Append(",\"settlementId\":"); sb.Append(Json(settlementId));
            sb.Append(",\"banditId\":"); sb.Append(Json(banditId));
            sb.Append(",\"currentThreat\":");
            sb.Append(RawOrNull(currentThreatJson));
            sb.Append(",\"persistentMemoryContext\":");
            sb.Append(RawOrNull(persistentMemoryContextJson));
            sb.Append(",\"memoryStateComparison\":");
            sb.Append(RawOrNull(memoryStateComparisonJson));
            sb.Append(",\"recentOutcomeContext\":");
            sb.Append(RawOrNull(recentOutcomeContextJson));
            sb.Append(",\"hasPersistentMemory\":");
            sb.Append(string.IsNullOrWhiteSpace(persistentMemoryContextJson) ? "false" : "true");
            sb.Append(",\"hasStateComparison\":");
            sb.Append(string.IsNullOrWhiteSpace(memoryStateComparisonJson) ? "false" : "true");
            sb.Append(",\"hasRecentOutcome\":");
            sb.Append(string.IsNullOrWhiteSpace(recentOutcomeContextJson) ? "false" : "true");
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
            string actorKnownContextJson)
        {
            if (string.IsNullOrWhiteSpace(shadowReceiptJson) ||
                string.IsNullOrWhiteSpace(actorKnownContextJson) ||
                shadowReceiptJson.Length < 2 ||
                shadowReceiptJson[shadowReceiptJson.Length-1] != '}' ||
                actorKnownContextJson[0] != '{')
                return shadowReceiptJson;

            return
                shadowReceiptJson.Substring(
                    0,
                    shadowReceiptJson.Length-1) +
                ",\"actorKnownContext\":" +
                actorKnownContextJson +
                "}";
        }
    }
}


