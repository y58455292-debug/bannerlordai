using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;

namespace ClanAI
{
    internal static class PlayerVisibilityLayer
    {
        private const double LoyaltyNoticeCooldownHours = 72.0;
        private const int MajorLoyaltyModifier = 75000;
        private const int NearLeaveBoundary = 250000;
        private const float WarStrainDeltaThreshold = 0.05f;

        private static readonly Dictionary<string, double>
            LastNoticeByKey =
                new Dictionary<string, double>(
                    StringComparer.Ordinal);

        private static string _lastWarKingdomId;
        private static float _lastWarStrain = -1f;
        private static int _lastWarBucket = -1;

        internal static void BeginSession()
        {
            LastNoticeByKey.Clear();
            _lastWarKingdomId = null;
            _lastWarStrain = -1f;
            _lastWarBucket = -1;
            ClanAIPostVanilla.WriteExternalLog(
                "PLAYER_VISIBILITY_SESSION_READY mode=native-message-feed");
        }

        internal static void NotifyHoldingLoss(
            Clan clan,
            Settlement settlement)
        {
            if (clan == null ||
                settlement == null ||
                string.IsNullOrEmpty(clan.StringId))
            {
                return;
            }

            string key =
                "loss:" + clan.StringId + ":" +
                settlement.StringId;

            if (!AllowNotice(
                key,
                12.0))
            {
                return;
            }

            Show(
                clan.Name +
                "'s loyalty is shaken by the loss of " +
                settlement.Name + ".");
        }

        internal static void NotifyLoyaltyConsider(
            Clan clan,
            Kingdom oldKingdom,
            int nativeValue,
            int modifier,
            int adjustedValue,
            bool nativeWouldLeave,
            bool adjustedWouldLeave,
            bool committed)
        {
            if (clan == null ||
                oldKingdom == null)
            {
                return;
            }

            if (committed)
            {
                Show(
                    clan.Name +
                    " has left " +
                    oldKingdom.Name +
                    " after mounting losses and grievances.");
                return;
            }

            if (modifier == 0)
                return;

            bool crossedBoundary =
                !nativeWouldLeave &&
                adjustedWouldLeave;

            bool nearBoundary =
                Math.Abs(adjustedValue) <=
                NearLeaveBoundary;

            bool majorShift =
                Math.Abs(modifier) >=
                MajorLoyaltyModifier;

            if (!crossedBoundary &&
                !(majorShift && nearBoundary))
            {
                return;
            }

            string key =
                "loyalty:" +
                (string.IsNullOrEmpty(clan.StringId)
                    ? clan.Name.ToString()
                    : clan.StringId);

            if (!AllowNotice(
                key,
                LoyaltyNoticeCooldownHours))
            {
                return;
            }

            if (modifier > 0)
            {
                Show(
                    clan.Name +
                    " is wavering in " +
                    oldKingdom.Name +
                    " after recent events.");
            }
            else
            {
                Show(
                    clan.Name +
                    " is drawing closer to " +
                    oldKingdom.Name +
                    " after recent events.");
            }
        }

        internal static void NotifyDefectionConsider(
            Clan clan,
            Kingdom oldKingdom,
            Kingdom targetKingdom,
            int modifier,
            bool committed)
        {
            if (clan == null ||
                oldKingdom == null ||
                targetKingdom == null)
            {
                return;
            }

            if (!committed)
                return;

            Show(
                clan.Name +
                " has defected from " +
                oldKingdom.Name +
                " to " +
                targetKingdom.Name +
                (modifier == 0
                    ? "."
                    : " as remembered relationships shifted the decision."));
        }

        internal static void NotifyWarStrain()
        {
            Clan playerClan =
                Clan.PlayerClan;

            Kingdom kingdom =
                playerClan == null
                    ? null
                    : playerClan.Kingdom;

            if (kingdom == null ||
                string.IsNullOrEmpty(kingdom.StringId))
            {
                return;
            }

            float strain;
            int activeWars;
            int besiegedSettlements;
            float scarLoad;

            if (!WarStateTracker.TryGetWarStrainState(
                kingdom,
                out strain,
                out activeWars,
                out besiegedSettlements,
                out scarLoad))
            {
                return;
            }

            int bucket =
                strain < 0.15f ? 0 :
                strain < 0.30f ? 1 :
                strain < 0.50f ? 2 : 3;

            bool kingdomChanged =
                !string.Equals(
                    _lastWarKingdomId,
                    kingdom.StringId,
                    StringComparison.Ordinal);

            bool materiallyChanged =
                _lastWarStrain < 0f ||
                Math.Abs(
                    strain -
                    _lastWarStrain) >=
                    WarStrainDeltaThreshold;

            if (!kingdomChanged &&
                !materiallyChanged &&
                bucket == _lastWarBucket)
            {
                return;
            }

            _lastWarKingdomId =
                kingdom.StringId;
            _lastWarStrain =
                strain;
            _lastWarBucket =
                bucket;

            Show(
                kingdom.Name +
                " war strain: " +
                Math.Round(
                    strain * 100f) +
                "% (" +
                activeWars +
                " active wars, " +
                besiegedSettlements +
                " besieged holdings, " +
                Math.Round(
                    scarLoad * 100f) +
                "% scar load).");
        }

        private static bool AllowNotice(
            string key,
            double cooldownHours)
        {
            if (string.IsNullOrEmpty(key))
                return false;

            double now =
                CampaignTime.Now.ToHours;

            double last;
            if (LastNoticeByKey.TryGetValue(
                key,
                out last) &&
                now - last <
                    cooldownHours)
            {
                return false;
            }

            LastNoticeByKey[key] =
                now;
            return true;
        }

        private static void Show(
            object text)
        {
            string message =
                "ClanAI: " +
                (text == null
                    ? ""
                    : text.ToString());

            InformationManager.DisplayMessage(
                new InformationMessage(
                    message));

            ClanAIPostVanilla.WriteExternalLog(
                "PLAYER_VISIBILITY message=" +
                message);
        }
    }
}
