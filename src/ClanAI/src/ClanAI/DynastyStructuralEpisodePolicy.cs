using System;
using System.Collections.Generic;
using System.Globalization;

namespace ClanAI
{
    internal sealed class DynastyStructuralEpisodeDraft
    {
        internal string SemanticIdentity;
        internal string BranchId;
        internal double CampaignHours;
        internal string ObservedUtc;
        internal string ActorId;
        internal string ActorName;
        internal string Kind;
        internal string ContextId;
        internal string ContextName;
        internal string Source;
        internal string Detail;
        internal int OptionIndex;
        internal string OptionText;
    }

    internal static class DynastyStructuralEpisodePolicy
    {
        internal const string RulingClanChangedKind =
            "KingdomRulingClanChanged";
        internal const string RulingClanChangedSource =
            "CampaignEvents.RulingClanChanged";

        internal static bool IsAcceptedProductionKind(string kind)
        {
            return kind == "IncidentOpened" ||
                kind == "IncidentChoice" ||
                kind == RulingClanChangedKind;
        }

        internal static bool IsAcceptedNativeTransition(
            string recordedRulingClanId,
            string nativeRulingClanId)
        {
            return !string.Equals(
                recordedRulingClanId,
                nativeRulingClanId,
                StringComparison.Ordinal);
        }

        internal static bool TryCreateRulingClanChanged(
            string branchId,
            string actorId,
            string actorName,
            string kingdomId,
            string kingdomName,
            string oldRulingClanId,
            string newRulingClanId,
            string oldRulerHeroId,
            string newRulerHeroId,
            int successionOrdinal,
            double campaignHours,
            string observedUtc,
            ISet<string> existingSemanticIdentities,
            out DynastyStructuralEpisodeDraft draft)
        {
            draft = null;

            if (string.IsNullOrWhiteSpace(branchId) ||
                string.IsNullOrWhiteSpace(actorId) ||
                string.IsNullOrWhiteSpace(kingdomId) ||
                successionOrdinal <= 0 ||
                double.IsNaN(campaignHours) ||
                double.IsInfinity(campaignHours))
            {
                return false;
            }

            string identity = BuildRulingClanChangedIdentity(
                branchId,
                kingdomId,
                successionOrdinal,
                oldRulingClanId,
                newRulingClanId);

            if (existingSemanticIdentities != null &&
                existingSemanticIdentities.Contains(identity))
            {
                return false;
            }

            string detail =
                "kingdomId=" + Value(kingdomId) +
                ";oldRulingClanId=" + Value(oldRulingClanId) +
                ";newRulingClanId=" + Value(newRulingClanId) +
                ";oldRulerHeroId=" + Value(oldRulerHeroId) +
                ";newRulerHeroId=" + Value(newRulerHeroId) +
                ";successionOrdinal=" +
                successionOrdinal.ToString(CultureInfo.InvariantCulture);

            draft = new DynastyStructuralEpisodeDraft
            {
                SemanticIdentity = identity,
                BranchId = branchId,
                CampaignHours = campaignHours,
                ObservedUtc = observedUtc ?? "",
                ActorId = actorId,
                ActorName = string.IsNullOrWhiteSpace(actorName)
                    ? "<unnamed>"
                    : actorName,
                Kind = RulingClanChangedKind,
                ContextId = "kingdom:" + kingdomId,
                ContextName = string.IsNullOrWhiteSpace(kingdomName)
                    ? "<unknown kingdom>"
                    : kingdomName,
                Source = RulingClanChangedSource,
                Detail = detail,
                OptionIndex = -1,
                OptionText = ""
            };

            return true;
        }

        internal static string BuildRulingClanChangedIdentity(
            string branchId,
            string kingdomId,
            int successionOrdinal,
            string oldRulingClanId,
            string newRulingClanId)
        {
            return Value(branchId) + "|" +
                RulingClanChangedKind + "|" +
                Value(kingdomId) + "|" +
                successionOrdinal.ToString(CultureInfo.InvariantCulture) + "|" +
                Value(oldRulingClanId) + "|" +
                Value(newRulingClanId);
        }

        internal static string IdentityFromStoredRow(
            string branchId,
            string kind,
            string detail)
        {
            if (kind != RulingClanChangedKind)
                return null;

            string kingdomId = DetailValue(detail, "kingdomId");
            string oldClanId = DetailValue(detail, "oldRulingClanId");
            string newClanId = DetailValue(detail, "newRulingClanId");
            string ordinalText = DetailValue(detail, "successionOrdinal");
            int ordinal;

            if (string.IsNullOrWhiteSpace(kingdomId) ||
                !int.TryParse(
                    ordinalText,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out ordinal) ||
                ordinal <= 0)
            {
                return null;
            }

            return BuildRulingClanChangedIdentity(
                branchId,
                kingdomId,
                ordinal,
                NoneToEmpty(oldClanId),
                NoneToEmpty(newClanId));
        }

        private static string DetailValue(string detail, string key)
        {
            string prefix = key + "=";
            string[] parts = (detail ?? "").Split(';');
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].StartsWith(prefix, StringComparison.Ordinal))
                    return parts[i].Substring(prefix.Length);
            }
            return null;
        }

        private static string NoneToEmpty(string value)
        {
            return value == "<none>" ? "" : value;
        }

        private static string Value(string value)
        {
            return string.IsNullOrEmpty(value) ? "<none>" : value;
        }
    }
}

