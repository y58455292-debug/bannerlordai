using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace ClanAI
{
    internal static class HomeAssignmentLayer
    {
        private sealed class Pending
        {
            internal Hero Leader;
            internal Settlement Home;
            internal AiBehavior Behavior;
            internal long Revision;
            internal double Hour;
            internal string Before, After;
        }
        private static readonly Dictionary<MobileParty, Pending> PendingByParty =
            new Dictionary<MobileParty, Pending>();
        internal static void Reset() { PendingByParty.Clear(); }
        internal static void Prune()
        {
            var expired = new List<MobileParty>();
            foreach (var pair in PendingByParty)
                if (!pair.Key.IsActive || !ReferenceEquals(pair.Key.LeaderHero, pair.Value.Leader) ||
                    pair.Value.Revision != HomeAssignmentStore.Records.Revision ||
                    CampaignTime.Now.ToHours - pair.Value.Hour > 18d)
                    expired.Add(pair.Key);
            foreach (var actor in expired) { PendingByParty.Remove(actor); HomeAssignmentCounters.CommitExpired++; }
        }
        private static string Label(PartyThinkParams think, int index)
        {
            if (index < 0 || index >= think.AIBehaviorScores.Count) return "<none>";
            var data = think.AIBehaviorScores[index].Item1;
            var target = data.Party as Settlement;
            return data.AiBehavior + ":" + (target == null ? "<other>" : target.StringId);
        }
        internal static void Apply(MobileParty actor, PartyThinkParams think,
            StrategicDecisionComposer.Frame composer)
        {
            Verify(actor);
            Settlement home;
            if (composer == null || think == null || !HomeAssignmentStore.TryHome(actor, out home) ||
                !HomeAssignmentStore.Eligible(actor) || !HomeAssignmentStore.Peace(actor)) return;
            HomeAssignmentCounters.Evaluations++;
            bool urgent = HomeAssignmentStore.Urgent(actor, home);
            for (int i = 0; i < think.AIBehaviorScores.Count && !urgent; i++)
            {
                var candidate = think.AIBehaviorScores[i];
                var target = candidate.Item1.Party as Settlement;
                urgent = HomeAssignmentPolicy.PositiveFinite(candidate.Item2) &&
                    candidate.Item1.AiBehavior == AiBehavior.DefendSettlement && target != null &&
                    target.OwnerClan == Clan.PlayerClan;
            }
            int before = composer.CurrentBestIndex(think);
            for (int i = 0; i < think.AIBehaviorScores.Count; i++)
            {
                var data = think.AIBehaviorScores[i].Item1;
                if (!ReferenceEquals(data.Party as Settlement, home)) continue;
                HomeAssignmentCounters.HomeCandidates++;
                HomeAssignmentCandidateKind kind = data.AiBehavior == AiBehavior.GoToSettlement
                    ? HomeAssignmentCandidateKind.Visit : data.AiBehavior == AiBehavior.PatrolAroundPoint
                    ? HomeAssignmentCandidateKind.Patrol : HomeAssignmentCandidateKind.Other;
                float raw = think.AIBehaviorScores[i].Item2, current = composer.CurrentScore(i, raw);
                var decision = HomeAssignmentPolicy.Evaluate(true, true, true, urgent, true, kind, raw, current);
                if (!decision.Apply) continue;
                composer.ApplyFactor(i, "home-assignment", current, decision.Factor, decision.Reason);
                HomeAssignmentCounters.FactorApplications++;
            }
            int after = composer.CurrentBestIndex(think);
            if (HomeAssignmentCounters.Evaluations <= 5 || HomeAssignmentCounters.Evaluations % 50 == 0)
                Log("HOME_ASSIGNMENT_EVALUATE party=" + actor.StringId + " leader=" + actor.LeaderHero.StringId +
                    " home=" + home.StringId + " nativeWinner=" + Label(think, composer.BeforeWinner) +
                    " adjustedWinner=" + Label(think, after) + " hour=" + CampaignTime.Now.ToHours +
                    " " + HomeAssignmentCounters.Summary());
            if (after == before || after < 0 || !ReferenceEquals(think.AIBehaviorScores[after].Item1.Party, home))
                return;
            HomeAssignmentCounters.WinnerChanges++;
            Prune();
            if (PendingByParty.Count >= HomeAssignmentRecords.MaximumRows) return;
            PendingByParty[actor] = new Pending {
                Leader = actor.LeaderHero, Home = home, Behavior = think.AIBehaviorScores[after].Item1.AiBehavior,
                Revision = HomeAssignmentStore.Records.Revision, Hour = CampaignTime.Now.ToHours,
                Before = Label(think, before), After = Label(think, after)
            };
            Log("HOME_ASSIGNMENT_WINNER party=" + actor.StringId + " leader=" + actor.LeaderHero.StringId +
                " home=" + home.StringId + " nativeBefore=" + Label(think, composer.BeforeWinner) +
                " beforeContribution=" + Label(think, before) + " adjusted=" + Label(think, after) +
                " nativeScore=" + think.AIBehaviorScores[after].Item2 +
                " composedScore=" + composer.CurrentScore(after, think.AIBehaviorScores[after].Item2) +
                " factor=1.25 reason=assigned-home expectedBehavior=" + think.AIBehaviorScores[after].Item1.AiBehavior +
                " expectedTarget=" + home.StringId + " " + HomeAssignmentCounters.Summary());
        }
        private static void Verify(MobileParty actor)
        {
            if (actor == null) return;
            Pending pending;
            if (!PendingByParty.TryGetValue(actor, out pending)) return;
            PendingByParty.Remove(actor);
            if (pending.Revision != HomeAssignmentStore.Records.Revision ||
                !ReferenceEquals(pending.Leader, actor.LeaderHero) || !HomeAssignmentStore.Eligible(actor) ||
                !HomeAssignmentStore.ValidHome(pending.Home) || CampaignTime.Now.ToHours - pending.Hour > 18d)
            { HomeAssignmentCounters.CommitExpired++; Log("HOME_ASSIGNMENT_COMMIT expired party=" + actor.StringId); return; }
            HomeAssignmentCounters.CommitChecks++;
            bool match = (actor.DefaultBehavior == pending.Behavior && ReferenceEquals(actor.TargetSettlement, pending.Home)) ||
                (actor.ShortTermBehavior == pending.Behavior && ReferenceEquals(actor.ShortTermTargetSettlement, pending.Home));
            if (match) HomeAssignmentCounters.CommitMatches++; else HomeAssignmentCounters.CommitMismatches++;
            Log("HOME_ASSIGNMENT_COMMIT party=" + actor.StringId + " leader=" + pending.Leader.StringId +
                " home=" + pending.Home.StringId + " before=" + pending.Before + " expected=" + pending.After +
                " default=" + actor.DefaultBehavior + " defaultTarget=" + (actor.TargetSettlement == null ? "<none>" : actor.TargetSettlement.StringId) +
                " short=" + actor.ShortTermBehavior + " shortTarget=" + (actor.ShortTermTargetSettlement == null ? "<none>" : actor.ShortTermTargetSettlement.StringId) +
                " matched=" + match + " elapsedHours=" + (CampaignTime.Now.ToHours - pending.Hour) +
                " " + HomeAssignmentCounters.Summary());
        }
        private static void Log(string message)
        { if (RuntimeProfile.EvidenceEnabled) ClanAIPostVanilla.WriteExternalLog(message); }
    }
}
