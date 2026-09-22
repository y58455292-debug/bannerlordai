using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace BannerlordAITestRunner
{
    internal static class PatrolDefenseApplyGate
    {
        private const string ReceiptPath =
            @"D:\BannerlordAIResearch\Automation\TestRunner\patrol_defense_apply.jsonl";

        private static bool _enabled;
        private static bool _active;
        private static bool _consumed;
        private static string _consumedBanditId;
        private static string _consumedSettlementId;
        private static int _rearmCount;
        private static string _settlementId;
        private static string _banditId;
        private static string _proposalId;
        private static string _proposalFingerprint;
        private static string _proposalVictimId;
        private static double _proposalCampaignHours;
        private static DateTime _deadlineUtc;

        public static bool IsConsumed { get { return _consumed; } }
        public static bool IsActive { get { return _active; } }
        public static string ConsumedBanditId { get { return _consumedBanditId; } }
        public static string ConsumedSettlementId { get { return _consumedSettlementId; } }
        public static int RearmCount { get { return _rearmCount; } }

        public static void ResetForCampaign()
        {
            _consumed = false;
            _consumedBanditId = null;
            _consumedSettlementId = null;
            _rearmCount = 0;
            ResetActiveState();
        }

        public static void EnableAutomatic(string settlementId)
        {
            _active = false;
            _settlementId = settlementId;
            _banditId = null;
            _deadlineUtc = DateTime.MinValue;
            _enabled =
                !_consumed &&
                !string.IsNullOrWhiteSpace(settlementId);
            if (_enabled)
                Append("AUTO_ENABLED", "patrol_intent_active", null, null, null, null);
        }

        public static void Cancel(string reason)
        {
            if (_enabled || _active)
                Append("CANCELLED", reason, null, null, null, null);
            ResetActiveState();
        }

        public static void TryIssue(
            PatrolDefenseShadowResult proposal,
            Settlement settlement)
        {
            if (!_enabled || _active || _consumed || proposal == null ||
                !proposal.WouldInterrupt ||
                string.IsNullOrWhiteSpace(proposal.BanditId) ||
                settlement == null ||
                !string.Equals(settlement.StringId, _settlementId,
                    StringComparison.Ordinal))
            {
                return;
            }

            MobileParty actor = MobileParty.MainParty;
            MobileParty bandit = FindParty(proposal.BanditId);
            if (actor == null || bandit == null ||
                !bandit.IsActive || !bandit.IsBandit)
            {
                Append("RESOLVE_FAILED", "exact_bandit_unavailable",
                    proposal.BanditId, null, null, proposal);
                _enabled = false;
                return;
            }

            actor.SetMoveEngageParty(
                bandit,
                MobileParty.NavigationType.Default);

            _enabled = false;
            _active = true;
            _consumed = true;
            _consumedBanditId = proposal.BanditId;
            _consumedSettlementId = proposal.PatrolSettlementId;
            _banditId = proposal.BanditId;
            _proposalId = proposal.ProposalId;
            _proposalFingerprint = proposal.Fingerprint;
            _proposalVictimId = proposal.VillagerId;
            _proposalCampaignHours = proposal.ProposalCampaignHours;
            _deadlineUtc = DateTime.UtcNow.AddSeconds(15.0);

            Append(
                "ENGAGE_ISSUED",
                "fresh_safe_proposal_auto_apply",
                _banditId,
                PartyId(actor.TargetParty),
                PartyId(actor.ShortTermTargetParty),
                proposal);
        }

        public static bool TryRearmFromEligibility(
            PatrolDefenseShadowResult proposal)
        {
            if (!_consumed || _active || _rearmCount >= 1 ||
                proposal == null || !proposal.WouldInterrupt ||
                string.IsNullOrWhiteSpace(proposal.BanditId) ||
                string.IsNullOrWhiteSpace(proposal.PatrolSettlementId) ||
                !string.Equals(
                    proposal.PatrolSettlementId,
                    _consumedSettlementId,
                    StringComparison.Ordinal) ||
                string.Equals(
                    proposal.BanditId,
                    _consumedBanditId,
                    StringComparison.Ordinal))
            {
                return false;
            }

            MobileParty actor = MobileParty.MainParty;
            MobileParty bandit = FindParty(proposal.BanditId);
            if (actor == null || bandit == null ||
                !bandit.IsActive || !bandit.IsBandit)
            {
                Append(
                    "REARM_RESOLVE_FAILED",
                    "eligible_bandit_unavailable",
                    proposal.BanditId,
                    null,
                    null,
                    proposal);
                return false;
            }

            actor.SetMoveEngageParty(
                bandit,
                MobileParty.NavigationType.Default);

            _enabled = false;
            _active = true;
            _rearmCount = 1;
            _settlementId = proposal.PatrolSettlementId;
            _banditId = proposal.BanditId;
            _proposalId = proposal.ProposalId;
            _proposalFingerprint = proposal.Fingerprint;
            _proposalVictimId = proposal.VillagerId;
            _proposalCampaignHours = proposal.ProposalCampaignHours;
            _deadlineUtc = DateTime.UtcNow.AddSeconds(15.0);

            Append(
                "ENGAGE_ISSUED",
                "fresh_rearm_eligible_auto_apply",
                _banditId,
                PartyId(actor.TargetParty),
                PartyId(actor.ShortTermTargetParty),
                proposal);
            return true;
        }

        public static void Tick()
        {
            if (!_active)
                return;

            MobileParty bandit = FindParty(_banditId);
            bool disappeared =
                bandit == null || !bandit.IsActive;
            bool timedOut = DateTime.UtcNow >= _deadlineUtc;

            if (!disappeared && !timedOut)
                return;

            MobileParty actor = MobileParty.MainParty;
            Settlement settlement = FindSettlement(_settlementId);
            if (actor == null || settlement == null)
            {
                Append(
                    "RESTORE_FAILED",
                    "actor_or_settlement_unavailable",
                    _banditId,
                    null,
                    null,
                    null);
                ResetActiveState();
                return;
            }

            string restoreReason =
                disappeared
                    ? "bandit_disappeared_or_resolved"
                    : "bounded_timeout";

            actor.SetMovePatrolAroundSettlement(
                settlement,
                MobileParty.NavigationType.Default,
                false);

            Append(
                "PATROL_RESTORED",
                restoreReason,
                _banditId,
                PartyId(actor.TargetParty),
                PartyId(actor.ShortTermTargetParty),
                null);

            string memoryResult =
                TryRecordPatrolDefenseMemory(
                    restoreReason);

            Append(
                "MEMORY_RECORDED",
                memoryResult,
                _banditId,
                PartyId(actor.TargetParty),
                PartyId(actor.ShortTermTargetParty),
                null);

            ResetActiveState();
        }


        private static string TryRecordPatrolDefenseMemory(
            string restoreReason)
        {
            try
            {
                Type bridge =
                    Type.GetType(
                        "ClanAI.DynastyMindOperatorBridge, ClanAI",
                        false);

                if (bridge == null)
                    return "memory_bridge_missing";

                MethodInfo method =
                    bridge.GetMethod(
                        "RecordPatrolDefenseEpisode",
                        BindingFlags.Public |
                        BindingFlags.Static);

                if (method == null)
                    return "memory_method_missing";

                string result =
                    method.Invoke(
                        null,
                        new object[]
                        {
                            _settlementId ?? "",
                            _banditId ?? "",
                            _proposalVictimId ?? "",
                            _proposalId ?? "",
                            _proposalFingerprint ?? "",
                            _proposalCampaignHours,
                            restoreReason ?? "",
                            "BannerlordAITestRunner.PatrolDefenseApplyGate"
                        }) as string;

                return
                    string.IsNullOrWhiteSpace(result)
                        ? "memory_empty"
                        : result;
            }
            catch (Exception ex)
            {
                return
                    "memory_failed_" +
                    ex.GetType().Name;
            }
        }

        private static MobileParty FindParty(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return null;

            return MobileParty.All.FirstOrDefault(
                p => p != null &&
                    string.Equals(
                        p.StringId,
                        id,
                        StringComparison.Ordinal));
        }

        private static Settlement FindSettlement(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return null;

            return Settlement.All.FirstOrDefault(
                s => s != null &&
                    string.Equals(
                        s.StringId,
                        id,
                        StringComparison.Ordinal));
        }

        private static string PartyId(MobileParty party)
        {
            try
            {
                return party == null
                    ? null
                    : party.StringId;
            }
            catch
            {
                return null;
            }
        }

        private static string ReadPartyMemberId(
            MobileParty party,
            string member)
        {
            if (party == null)
                return null;
            try
            {
                Type t = party.GetType();
                PropertyInfo p = t.GetProperty(member,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                object value = p == null ? null : p.GetValue(party, null);
                if (value == null)
                {
                    FieldInfo f = t.GetField(member,
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    value = f == null ? null : f.GetValue(party);
                }
                MobileParty target = value as MobileParty;
                return target == null ? null : target.StringId;
            }
            catch { return null; }
        }

        private static void ResetActiveState()
        {
            _enabled = false;
            _active = false;
            _settlementId = null;
            _banditId = null;
            _proposalId = null;
            _proposalFingerprint = null;
            _proposalVictimId = null;
            _proposalCampaignHours = 0.0;
            _deadlineUtc = DateTime.MinValue;
        }

        private static void Append(
            string phase,
            string reason,
            string banditId,
            string targetParty,
            string shortTermTargetParty,
            PatrolDefenseShadowResult proposal)
        {
            string proposalId =
                proposal == null ? _proposalId : proposal.ProposalId;
            string proposalFingerprint =
                proposal == null ? _proposalFingerprint : proposal.Fingerprint;
            string proposalVictimId =
                proposal == null ? _proposalVictimId : proposal.VillagerId;
            double proposalCampaignHours =
                proposal == null
                    ? _proposalCampaignHours
                    : proposal.ProposalCampaignHours;
            string proposalSettlementId =
                proposal == null
                    ? _settlementId
                    : proposal.PatrolSettlementId;

            StringBuilder sb = new StringBuilder();
            sb.Append("{");
            sb.Append("\"schema\":\"BannerlordAI.PatrolDefenseApply.v1\",");
            sb.Append("\"phase\":");
            sb.Append(Json(phase));
            sb.Append(",\"reason\":");
            sb.Append(Json(reason));
            sb.Append(",\"automatic\":true");
            sb.Append(",\"proposalId\":");
            sb.Append(Json(proposalId));
            sb.Append(",\"proposalFingerprint\":");
            sb.Append(Json(proposalFingerprint));
            sb.Append(",\"proposalSettlementId\":");
            sb.Append(Json(proposalSettlementId));
            sb.Append(",\"proposalBanditId\":");
            sb.Append(Json(banditId));
            sb.Append(",\"proposalVictimId\":");
            sb.Append(Json(proposalVictimId));
            sb.Append(",\"proposalCampaignHours\":");
            sb.Append(
                proposalCampaignHours.ToString(
                    "R",
                    CultureInfo.InvariantCulture));
            sb.Append(",\"exactlyOnceConsumed\":");
            sb.Append(_consumed ? "true" : "false");
            sb.Append(",\"rearmCount\":");
            sb.Append(_rearmCount.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"patrolSettlementId\":");
            sb.Append(Json(_settlementId));
            sb.Append(",\"banditId\":");
            sb.Append(Json(banditId));
            sb.Append(",\"targetParty\":");
            sb.Append(Json(targetParty));
            sb.Append(",\"shortTermTargetParty\":");
            sb.Append(Json(shortTermTargetParty));
            sb.Append(",\"moveTargetParty\":");
            sb.Append(Json(ReadPartyMemberId(
                MobileParty.MainParty,
                "MoveTargetParty")));
            sb.Append(",\"nativeCall\":");
            sb.Append(Json(
                phase == "ENGAGE_ISSUED"
                    ? "SetMoveEngageParty"
                    : phase == "PATROL_RESTORED"
                        ? "SetMovePatrolAroundSettlement"
                        : null));
            sb.Append(",\"boundedTimeoutSeconds\":15");
            sb.Append(",\"scoreMutation\":false");
            sb.Append(",\"nativeScoreWrites\":0");
            sb.Append(",\"utc\":");
            sb.Append(Json(
                DateTime.UtcNow.ToString(
                    "O",
                    CultureInfo.InvariantCulture)));
            sb.Append("}");

            File.AppendAllText(
                ReceiptPath,
                sb.ToString() + Environment.NewLine);
        }

        private static string Json(string value)
        {
            return
                "\"" +
                (value ?? "")
                    .Replace("\\", "\\\\")
                    .Replace("\"", "\\\"")
                    .Replace("\r", "\\r")
                    .Replace("\n", "\\n") +
                "\"";
        }
    }
}
