using System;
using System.Collections.Generic;
using System.Globalization;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace ClanAI
{
    internal static class HomeResponsibilityLayer
    {
        private const float WarGoHomeFactor = 1.15f;
        private const float WarPatrolFactor = 1.25f;
        private const float WarDefendFactor = 1.30f;
        private const float ThreatFactor = 1.60f;
        private const float RecoveryFactor = 1.35f;

        private static long _evaluations;
        private static long _applications;
        private static long _winnerChanges;
        private static long _commitChecks;
        private static long _commitMatches;

        private sealed class PendingCommit
        {
            internal AiBehavior Behavior;
            internal string SettlementId;
            internal string Label;
            internal string ActorName;
        }

        private static readonly Dictionary<string, PendingCommit> PendingByParty =
            new Dictionary<string, PendingCommit>(StringComparer.Ordinal);
        private static readonly HashSet<string> SeenScopedParties =
            new HashSet<string>(StringComparer.Ordinal);
        internal static void Reset()
        {
            _evaluations = 0;
            _applications = 0;
            _winnerChanges = 0;
            _commitChecks = 0;
            _commitMatches = 0;
            PendingByParty.Clear();
            SeenScopedParties.Clear();
            ClanAIPostVanilla.WriteExternalLog(
                "HOME_RESPONSIBILITY_RESET scope=all_independent_ai_lords_actor_clan");
        }

        internal static void Apply(
            MobileParty actor,
            PartyThinkParams thinkParams,
            StrategicDecisionComposer.Frame composer,
            ActorBlackboard.State blackboard)
        {
            VerifyPendingCommit(actor);

            if (actor == null || actor.LeaderHero == null ||
                thinkParams == null || composer == null)
                return;

            Clan clan = actor.ActualClan;
            if (actor.IsMainParty || clan == null)
                return;

            string actorName = actor.LeaderHero.Name.ToString();
            string partyId = actor.StringId ?? actorName;
            if (SeenScopedParties.Add(partyId))
            {
                ClanAIPostVanilla.WriteExternalLog(
                    "HOME_RESPONSIBILITY_SCOPE actor=" + actorName +
                    " partyId=" + partyId +
                    " clan=" + clan.Name.ToString());
            }

            if (!WorldScopeContext.EligibleIndependentLordAtWar(actor))
                return;

            _evaluations++;

            int beforeIndex = composer.CurrentBestIndex(thinkParams);
            string before = CandidateLabel(thinkParams, beforeIndex);
            float readiness = blackboard != null
                ? blackboard.Readiness
                : actor.PartySizeRatio;
            int foodDays = blackboard != null
                ? blackboard.FoodDays
                : actor.GetNumDaysForFoodToLast();
            bool weak = readiness < 0.72f || foodDays < 3;

            var changes = new List<Tuple<int, float, float, string>>();
            for (int i = 0; i < thinkParams.AIBehaviorScores.Count; i++)
            {
                AIBehaviorData data = thinkParams.AIBehaviorScores[i].Item1;
                Settlement settlement = data.Party as Settlement;
                if (settlement == null || !SameClan(settlement.OwnerClan, clan))
                    continue;
                float rawScore = thinkParams.AIBehaviorScores[i].Item2;
                float baseScore = composer.CurrentScore(i, rawScore);
                if (baseScore <= 0f)
                    continue;

                float factor = 1f;
                string reason = null;
                bool threatened = settlement.IsUnderSiege || settlement.IsUnderRaid;

                if (threatened)
                {
                    factor = ThreatFactor;
                    reason = settlement.IsUnderSiege ? "home-under-siege" : "home-under-raid";
                }
                else if (weak && data.AiBehavior == AiBehavior.GoToSettlement)
                {
                    factor = RecoveryFactor;
                    reason = "recover-at-home";
                }
                else if (data.AiBehavior == AiBehavior.DefendSettlement)
                {
                    factor = WarDefendFactor;
                    reason = "defend-home-at-war";
                }
                else if (data.AiBehavior == AiBehavior.PatrolAroundPoint)
                {
                    factor = WarPatrolFactor;
                    reason = "patrol-home-at-war";
                }
                else if (data.AiBehavior == AiBehavior.GoToSettlement)
                {
                    factor = WarGoHomeFactor;
                    reason = "stay-near-home-at-war";
                }
                if (factor <= 1.001f)
                    continue;

                changes.Add(Tuple.Create(i, baseScore, factor, reason));
            }

            if (changes.Count == 0)
            {
                if (_evaluations <= 5 || (_evaluations % 50) == 0)
                    ClanAIPostVanilla.WriteExternalLog(
                        "HOME_RESPONSIBILITY_EVALUATE actor=" + actorName +
                        " result=no-actor-clan-candidate before=" + before +
                        " readiness=" + F(readiness) + " foodDays=" + foodDays);
                return;
            }

            _applications++;
            for (int i = 0; i < changes.Count; i++)
            {
                var c = changes[i];
                composer.ApplyFactor(
                    c.Item1,
                    "home-responsibility",
                    c.Item2,
                    c.Item3,
                    c.Item4);
            }

            int afterIndex = composer.CurrentBestIndex(thinkParams);
            string after = CandidateLabel(thinkParams, afterIndex);
            if (afterIndex != beforeIndex)
            {
                _winnerChanges++;
                AIBehaviorData winner = thinkParams.AIBehaviorScores[afterIndex].Item1;
                Settlement target = winner.Party as Settlement;

                PendingByParty[partyId] = new PendingCommit
                {
                    Behavior = winner.AiBehavior,
                    SettlementId = target == null ? null : target.StringId,
                    Label = after,
                    ActorName = actorName
                };

                ClanAIPostVanilla.WriteExternalLog(
                    "HOME_RESPONSIBILITY_WINNER_CHANGE" +
                    " actor=" + actorName +
                    " partyId=" + (actor.StringId ?? "<null>") +
                    " clan=" + (clan == null ? "<null>" : clan.Name.ToString()) +
                    " before=" + before +
                    " after=" + after +
                    " readiness=" + F(readiness) +
                    " foodDays=" + foodDays +
                    " applications=" + _applications +
                    " winnerChanges=" + _winnerChanges);
            }
            else if (_applications <= 5 || (_applications % 50) == 0)
            {
                ClanAIPostVanilla.WriteExternalLog(
                    "HOME_RESPONSIBILITY_APPLIED actor=" + actorName +
                    " before=" + before + " after=" + after +
                    " winnerChanged=False changes=" + changes.Count);
            }
        }
        private static void VerifyPendingCommit(MobileParty actor)
        {
            if (actor == null || string.IsNullOrEmpty(actor.StringId))
                return;

            PendingCommit pending;
            if (!PendingByParty.TryGetValue(actor.StringId, out pending))
                return;

            PendingByParty.Remove(actor.StringId);
            _commitChecks++;
            Settlement target = actor.TargetSettlement ?? actor.ShortTermTargetSettlement;
            bool behaviorMatch = actor.DefaultBehavior == pending.Behavior ||
                                 actor.ShortTermBehavior == pending.Behavior;
            bool targetMatch = string.IsNullOrEmpty(pending.SettlementId) ||
                               (target != null && string.Equals(
                                   target.StringId, pending.SettlementId, StringComparison.Ordinal));
            bool matched = behaviorMatch && targetMatch;
            if (matched)
            {
                _commitMatches++;
                CompanionDutyMemory.RecordSuccessfulHomeDuty(
                    actor,
                    pending.Behavior,
                    target,
                    "home-responsibility-commit");
            }

            ClanAIPostVanilla.WriteExternalLog(
                "HOME_RESPONSIBILITY_COMMIT_CHECK actor=" + pending.ActorName +
                " partyId=" + actor.StringId +
                " expected=" + (pending.Label ?? "<none>") +
                " actualDefault=" + actor.DefaultBehavior +
                " actualShort=" + actor.ShortTermBehavior +
                " actualTarget=" + (target == null ? "<none>" : target.Name.ToString()) +
                " matched=" + matched +
                " checks=" + _commitChecks +
                " matches=" + _commitMatches);
        }
        private static bool FactionAtWar(IFaction faction)
        {
            if (faction == null)
                return false;

            foreach (Settlement settlement in Settlement.All)
            {
                if (settlement == null || settlement.MapFaction == null)
                    continue;
                if (ReferenceEquals(settlement.MapFaction, faction))
                    continue;
                if (faction.IsAtWarWith(settlement.MapFaction))
                    return true;
            }
            return false;
        }

        private static bool SameClan(Clan a, Clan b)
        {
            if (a == null || b == null)
                return false;
            if (ReferenceEquals(a, b))
                return true;
            return string.Equals(a.StringId, b.StringId, StringComparison.Ordinal);
        }

        private static string CandidateLabel(PartyThinkParams thinkParams, int index)
        {
            if (thinkParams == null || index < 0 ||
                index >= thinkParams.AIBehaviorScores.Count)
                return "<none>";

            AIBehaviorData data = thinkParams.AIBehaviorScores[index].Item1;
            Settlement settlement = data.Party as Settlement;
            return data.AiBehavior + ":" +
                (settlement == null ? (data.Party == null ? "<none>" : data.Party.ToString()) : settlement.Name.ToString());
        }

        private static string F(float value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }
    }
}
