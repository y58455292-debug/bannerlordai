using TaleWorlds.CampaignSystem;

namespace ClanAI
{
    public static class DynastyMindOperatorBridge
    {
        public static string Version =>
            ReleaseIdentity.Version;

        public static string ObserveDeliberation(
            string decisionType,
            string decisionKey,
            string decisionTitle,
            string selectedOrTarget)
        {
            return
                DynastyMindSeed.BuildOperatorShadowReceipt(
                    Hero.MainHero,
                    decisionType,
                    decisionKey,
                    decisionTitle,
                    selectedOrTarget);
        }

        public static string EvaluateSettlementDefense(
            string settlementName,
            bool atTarget)
        {
            return
                DynastyMindSeed.BuildSettlementDefenseDecision(
                    Hero.MainHero,
                    settlementName,
                    atTarget);
        }

        public static string RecordIncidentOpened(
            string title,
            int optionCount,
            string source)
        {
            return
                DynastyBranchEpisodeMemory.RecordIncidentOpened(
                    title,
                    optionCount,
                    source);
        }

        public static string RetrieveLatestBranchEpisode(
            string retrievalContext)
        {
            return
                DynastyBranchEpisodeMemory.BuildLatestRetrievalReceipt(
                    retrievalContext);
        }

        public static string RecordIncidentChoice(
            string title,
            int optionIndex,
            string optionText,
            string source)
        {
            return
                DynastyBranchEpisodeMemory.RecordIncidentChoice(
                    title,
                    optionIndex,
                    optionText,
                    source);
        }

        public static string RetrieveLatestBranchChoice(
            string retrievalContext)
        {
            return
                DynastyBranchEpisodeMemory.BuildLatestChoiceRetrievalReceipt(
                    retrievalContext);
        }

        public static string RetrieveLatestDynastyHistory(
            string retrievalContext)
        {
            return
                DynastyBranchEpisodeMemory.BuildLatestBranchHistoryReceipt(
                    retrievalContext);
        }
    }
}


