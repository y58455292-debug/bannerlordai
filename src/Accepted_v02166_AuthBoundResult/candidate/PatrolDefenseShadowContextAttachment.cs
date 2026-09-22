using System;

namespace BannerlordAITestRunner
{
    internal static class PatrolDefenseShadowContextAttachment
    {
        internal static string Attach(
            string receiptJson,
            string recentOutcomeContextJson)
        {
            if (string.IsNullOrWhiteSpace(receiptJson) ||
                string.IsNullOrWhiteSpace(recentOutcomeContextJson) ||
                receiptJson.Length < 2 ||
                receiptJson[receiptJson.Length - 1] != '}' ||
                recentOutcomeContextJson[0] != '{')
                return receiptJson;

            return
                receiptJson.Substring(0, receiptJson.Length - 1) +
                ",\"recentOutcomeContext\":" +
                recentOutcomeContextJson +
                "}";
        }
    }
}
