using System;
using System.Collections.Generic;
using System.IO;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;

namespace ClanAI
{
    public static class VisualWarDecisionLayer
    {
        private const string EnablePath =
            @"D:\BannerlordAIResearch\Data\ENABLE_VISUAL_WAR_LAB.txt";

        private static DateTime _nextSwitchCheck = DateTime.MinValue;
        private static bool _enabled;
        private static double _nextContextRefreshHour = double.MinValue;
        private static long _applications;
        private static long _winnerChanges;
        private static long _selectionChanges;
        private static long _repeatSelectionsSuppressed;
        private static long _weakActiveDefenseSkips;
        private static long _weakFrontierDefenseSkips;
        private static long _engagePartyCandidates;
        private static long _engagePartyMobileTargets;
        private static long _engagePartyNonBanditTargets;
        private static long _banditEngageCandidates;
        private static long _rearSecurityEligible;
        private static long _rearSecurityWeakSkips;

        private sealed class SettlementContext
        {
            public float FrontierScore;
            public IFaction NearestEnemyFaction;
            public bool UnderAttack;
        }

        private sealed class BorderDistance
        {
            public Settlement Settlement;
            public float DistanceSquared;
            public IFaction NearestEnemyFaction;
        }

        private sealed class PendingChange
        {
            public int Index;
            public AIBehaviorData Data;
            public float BaseScore;
            public float Factor;
            public float NewScore;
            public string Reason;
        }

        private static readonly Dictionary<string, SettlementContext>
            ContextBySettlement =
                new Dictionary<string, SettlementContext>(
                    StringComparer.Ordinal);

        private static readonly Dictionary<IFaction, List<Settlement>>
            WalledByFaction =
                new Dictionary<IFaction, List<Settlement>>();

        private static readonly HashSet<string> BanditPartyIds =
            new HashSet<string>(StringComparer.Ordinal);

        private static readonly Dictionary<string, string>
            LastVisualSelectionByParty =
                new Dictionary<string, string>(
                    StringComparer.Ordinal);

        public static bool Enabled
        {
            get
            {
                DateTime now = DateTime.UtcNow;

                if (now >= _nextSwitchCheck)
                {
                    _nextSwitchCheck = now.AddSeconds(2);
                    _enabled = File.Exists(EnablePath);
                }

                return _enabled;
            }
        }

        public static string StatusText
        {
            get { return Enabled ? "ON" : "OFF"; }
        }

        public static void Reset()
        {
            _nextSwitchCheck = DateTime.MinValue;
            _nextContextRefreshHour = double.MinValue;
            _applications = 0;
            _winnerChanges = 0;
            _selectionChanges = 0;
            _repeatSelectionsSuppressed = 0;
            _weakActiveDefenseSkips = 0;
            _weakFrontierDefenseSkips = 0;
            _engagePartyCandidates = 0;
            _engagePartyMobileTargets = 0;
            _engagePartyNonBanditTargets = 0;
            _banditEngageCandidates = 0;
            _rearSecurityEligible = 0;
            _rearSecurityWeakSkips = 0;

            ContextBySettlement.Clear();
            WalledByFaction.Clear();
            BanditPartyIds.Clear();
            LastVisualSelectionByParty.Clear();

            ClanAIPostVanilla.WriteExternalLog(
                "VISUAL_WAR_RESET enabled=" + Enabled);
        }

        internal static void Apply(
            MobileParty actor,
            PartyThinkParams thinkParams,
            StrategicDecisionComposer.Frame composer,
            ActorBlackboard.State blackboard)
        {
            if (!Enabled ||
                actor == null ||
                actor.LeaderHero == null ||
                actor.MapFaction == null ||
                thinkParams == null ||
                thinkParams.AIBehaviorScores.Count == 0)
            {
                return;
            }

            EnsureWorldContext();

            int beforeIndex =
                composer != null
                    ? composer.CurrentBestIndex(thinkParams)
                    : FindBestIndex(thinkParams);

            string before =
                CandidateLabel(
                    thinkParams,
                    beforeIndex);

            int men;
            float readiness;
            int foodDays;

            if (blackboard != null)
            {
                men = blackboard.Men;
                readiness = blackboard.Readiness;
                foodDays = blackboard.FoodDays;

                ActorBlackboard.NoteVisualWarRead();
            }
            else
            {
                men =
                    actor.MemberRoster != null
                        ? actor.MemberRoster.TotalManCount
                        : 0;

                readiness =
                    actor.PartySizeRatio;

                foodDays =
                    actor.GetNumDaysForFoodToLast();
            }

            bool weakForRecovery =
                readiness < 0.72f ||
                foodDays < 3;

            bool skippedWeakActiveDefense =
                false;

            bool skippedWeakFrontierDefense =
                false;

            var changes =
                new List<PendingChange>();

            for (int i = 0;
                 i < thinkParams.AIBehaviorScores.Count;
                 i++)
            {
                AIBehaviorData data =
                    thinkParams.AIBehaviorScores[i].Item1;

                float rawScore =
                    thinkParams.AIBehaviorScores[i].Item2;

                float baseScore =
                    composer != null
                        ? composer.CurrentScore(
                            i,
                            rawScore)
                        : rawScore;

                if (baseScore <= 0f)
                    continue;

                float factor = 1f;
                string reason = null;

                Settlement settlement =
                    data.Party as Settlement;

                if (settlement != null)
                {
                    SettlementContext context =
                        GetContext(settlement);

                    if (context != null)
                    {
                        if (SameFaction(
                                actor.MapFaction,
                                settlement.MapFaction) &&
                            IsDefensiveTravel(data.AiBehavior))
                        {
                            float pressure =
                                context.UnderAttack
                                    ? 1f
                                    : context.FrontierScore;

                            if (pressure > 0f)
                            {
                                if (context.UnderAttack &&
                                    !weakForRecovery)
                                {
                                    factor =
                                        1f +
                                        (0.22f * pressure) +
                                        0.10f;

                                    if (factor > 1.35f)
                                        factor = 1.35f;

                                    reason =
                                        "active-defense";
                                }
                                else if (!context.UnderAttack &&
                                    !weakForRecovery)
                                {
                                    factor =
                                        1f +
                                        (0.22f * pressure);

                                    if (factor > 1.35f)
                                        factor = 1.35f;

                                    reason =
                                        "frontier-defense";
                                }
                                else if (context.UnderAttack)
                                {
                                    skippedWeakActiveDefense =
                                        true;
                                }
                                else
                                {
                                    skippedWeakFrontierDefense =
                                        true;
                                }
                            }
                        }
                        else if (
                            actor.MapFaction.IsAtWarWith(
                                settlement.MapFaction) &&
                            IsAggressive(data.AiBehavior) &&
                            context.FrontierScore > 0f &&
                            SameFaction(
                                actor.MapFaction,
                                context.NearestEnemyFaction) &&
                            !weakForRecovery)
                        {
                            factor =
                                1f +
                                (0.16f *
                                 context.FrontierScore);

                            reason =
                                "frontier-offense";
                        }
                    }
                }
                else if (
                    data.AiBehavior ==
                        AiBehavior.EngageParty)
                {
                    _engagePartyCandidates++;

                    MobileParty targetParty =
                        data.Party as MobileParty;

                    if (targetParty != null)
                    {
                        _engagePartyMobileTargets++;

                        if (IsBandit(targetParty))
                        {
                            _banditEngageCandidates++;

                            if (!weakForRecovery &&
                                men > 0 &&
                                men <= 160)
                            {
                                _rearSecurityEligible++;

                                factor =
                                    men <= 90
                                        ? 1.25f
                                        : 1.15f;

                                reason =
                                    "rear-security";
                            }
                            else if (weakForRecovery &&
                                men > 0 &&
                                men <= 160)
                            {
                                _rearSecurityWeakSkips++;
                            }
                        }
                        else
                        {
                            _engagePartyNonBanditTargets++;
                        }
                    }
                }

                if (factor <= 1.001f)
                    continue;

                changes.Add(
                    new PendingChange
                    {
                        Index = i,
                        Data = data,
                        BaseScore = baseScore,
                        Factor = factor,
                        NewScore = baseScore * factor,
                        Reason = reason
                    });
            }

            if (skippedWeakActiveDefense)
            {
                _weakActiveDefenseSkips++;
            }

            if (skippedWeakFrontierDefense)
            {
                _weakFrontierDefenseSkips++;
            }

            if (changes.Count == 0)
                return;

            _applications++;

            for (int i = 0; i < changes.Count; i++)
            {
                PendingChange change =
                    changes[i];

                AIBehaviorData data =
                    change.Data;

                if (composer == null)
                {
                    ClanAIPostVanilla.WriteExternalLog(
                        "STRATEGIC_COMPOSER_FAILURE" +
                        " reason=missing-frame" +
                        " source=visual-war" +
                        " actor=" +
                        actor.LeaderHero.Name.ToString());

                    return;
                }

                composer.ApplyFactor(
                    change.Index,
                    "visual-war",
                    change.BaseScore,
                    change.Factor,
                    change.Reason);
            }

            int afterIndex =
                composer.CurrentBestIndex(
                    thinkParams);

            if (afterIndex == beforeIndex)
                return;

            _winnerChanges++;

            string after =
                CandidateLabel(
                    thinkParams,
                    afterIndex);

            string winnerReason =
                FindReasonForWinner(
                    thinkParams,
                    afterIndex,
                    changes);

            string partyKey =
                string.IsNullOrEmpty(actor.StringId)
                    ? actor.Name.ToString()
                    : actor.StringId;

            string visualSelection =
                winnerReason +
                "|" +
                after;

            string previousVisualSelection;

            if (LastVisualSelectionByParty.TryGetValue(
                    partyKey,
                    out previousVisualSelection) &&
                string.Equals(
                    previousVisualSelection,
                    visualSelection,
                    StringComparison.Ordinal))
            {
                _repeatSelectionsSuppressed++;
                return;
            }

            LastVisualSelectionByParty[partyKey] =
                visualSelection;

            _selectionChanges++;

            string text =
                "VISUAL_WAR_WINNER_CHANGE" +
                " actor=" +
                actor.LeaderHero.Name.ToString() +
                " party=" +
                actor.Name.ToString() +
                " men=" +
                men +
                " readiness=" +
                readiness.ToString("0.000") +
                " foodDays=" +
                foodDays +
                " before=" +
                before +
                " after=" +
                after +
                " reason=" +
                winnerReason +
                " previousVisual=" +
                (previousVisualSelection ?? "<none>") +
                " applications=" +
                _applications +
                " winnerChanges=" +
                _winnerChanges +
                " selectionChanges=" +
                _selectionChanges +
                " repeatsSuppressed=" +
                _repeatSelectionsSuppressed +
                " weakFrontierSkips=" +
                _weakFrontierDefenseSkips;

            ClanAIPostVanilla.WriteExternalLog(text);
        }

        private static void EnsureWorldContext()
        {
            if (Campaign.Current == null)
                return;

            double hour =
                CampaignTime.Now.ToHours;

            if (ContextBySettlement.Count > 0 &&
                hour < _nextContextRefreshHour)
            {
                return;
            }

            RefreshWorldContext(hour);
        }

        private static void RefreshWorldContext(
            double currentHour)
        {
            ContextBySettlement.Clear();
            WalledByFaction.Clear();
            BanditPartyIds.Clear();

            var allWalled =
                new List<Settlement>();

            foreach (Settlement settlement
                     in Settlement.All)
            {
                if (settlement == null ||
                    settlement.MapFaction == null)
                {
                    continue;
                }

                if (!settlement.IsVillage &&
                    settlement.Town != null)
                {
                    allWalled.Add(settlement);

                    List<Settlement> list;

                    if (!WalledByFaction.TryGetValue(
                            settlement.MapFaction,
                            out list))
                    {
                        list =
                            new List<Settlement>();

                        WalledByFaction.Add(
                            settlement.MapFaction,
                            list);
                    }

                    list.Add(settlement);
                }
            }

            foreach (var pair in WalledByFaction)
            {
                IFaction faction = pair.Key;
                List<Settlement> own = pair.Value;

                var distances =
                    new List<BorderDistance>();

                for (int i = 0; i < own.Count; i++)
                {
                    Settlement center = own[i];

                    float best =
                        float.MaxValue;

                    IFaction nearestEnemy =
                        null;

                    Vec2 p =
                        center.GetPosition2D;

                    for (int j = 0;
                         j < allWalled.Count;
                         j++)
                    {
                        Settlement other =
                            allWalled[j];

                        if (other == null ||
                            other.MapFaction == null ||
                            SameFaction(
                                faction,
                                other.MapFaction))
                        {
                            continue;
                        }

                        if (!faction.IsAtWarWith(
                                other.MapFaction))
                        {
                            continue;
                        }

                        float d =
                            DistanceSquared(
                                p,
                                other.GetPosition2D);

                        if (d < best)
                        {
                            best = d;
                            nearestEnemy =
                                other.MapFaction;
                        }
                    }

                    if (nearestEnemy != null)
                    {
                        distances.Add(
                            new BorderDistance
                            {
                                Settlement = center,
                                DistanceSquared = best,
                                NearestEnemyFaction =
                                    nearestEnemy
                            });
                    }
                }

                distances.Sort(
                    delegate(
                        BorderDistance a,
                        BorderDistance b)
                    {
                        return
                            a.DistanceSquared.CompareTo(
                                b.DistanceSquared);
                    });

                int frontierCount =
                    (int)Math.Ceiling(
                        distances.Count * 0.40);

                if (frontierCount > 0 &&
                    frontierCount < 2 &&
                    distances.Count >= 2)
                {
                    frontierCount = 2;
                }

                if (frontierCount >
                    distances.Count)
                {
                    frontierCount =
                        distances.Count;
                }

                for (int i = 0;
                     i < distances.Count;
                     i++)
                {
                    float score = 0f;

                    if (i < frontierCount &&
                        frontierCount > 0)
                    {
                        if (frontierCount == 1)
                        {
                            score = 1f;
                        }
                        else
                        {
                            score =
                                1f -
                                (0.50f *
                                 ((float)i /
                                  (frontierCount - 1)));
                        }
                    }

                    Settlement center =
                        distances[i].Settlement;

                    bool underAttack =
                        center.IsUnderSiege ||
                        center.IsUnderRaid;

                    if (underAttack)
                        score = 1f;

                    ContextBySettlement[
                        center.StringId] =
                        new SettlementContext
                        {
                            FrontierScore = score,
                            NearestEnemyFaction =
                                distances[i]
                                    .NearestEnemyFaction,
                            UnderAttack =
                                underAttack
                        };
                }
            }

            foreach (Settlement settlement
                     in Settlement.All)
            {
                if (settlement == null ||
                    settlement.MapFaction == null ||
                    string.IsNullOrEmpty(
                        settlement.StringId) ||
                    ContextBySettlement.ContainsKey(
                        settlement.StringId))
                {
                    continue;
                }

                List<Settlement> ownCenters;

                if (!WalledByFaction.TryGetValue(
                        settlement.MapFaction,
                        out ownCenters) ||
                    ownCenters.Count == 0)
                {
                    continue;
                }

                Settlement nearest =
                    null;

                float best =
                    float.MaxValue;

                Vec2 p =
                    settlement.GetPosition2D;

                for (int i = 0;
                     i < ownCenters.Count;
                     i++)
                {
                    Settlement center =
                        ownCenters[i];

                    float d =
                        DistanceSquared(
                            p,
                            center.GetPosition2D);

                    if (d < best)
                    {
                        best = d;
                        nearest = center;
                    }
                }

                SettlementContext parent;

                if (nearest != null &&
                    ContextBySettlement.TryGetValue(
                        nearest.StringId,
                        out parent))
                {
                    ContextBySettlement[
                        settlement.StringId] =
                        new SettlementContext
                        {
                            FrontierScore =
                                parent.FrontierScore,
                            NearestEnemyFaction =
                                parent
                                    .NearestEnemyFaction,
                            UnderAttack =
                                settlement.IsUnderSiege ||
                                settlement.IsUnderRaid
                        };
                }
            }

            foreach (MobileParty bandit
                     in MobileParty.AllBanditParties)
            {
                if (bandit != null &&
                    !string.IsNullOrEmpty(
                        bandit.StringId))
                {
                    BanditPartyIds.Add(
                        bandit.StringId);
                }
            }

            _nextContextRefreshHour =
                currentHour + 6.0;

            ClanAIPostVanilla.WriteExternalLog(
                "VISUAL_WAR_CONTEXT_REFRESH" +
                " walled=" +
                allWalled.Count +
                " settlements=" +
                ContextBySettlement.Count +
                " bandits=" +
                BanditPartyIds.Count +
                " applications=" +
                _applications +
                " winnerChanges=" +
                _winnerChanges +
                " selectionChanges=" +
                _selectionChanges +
                " repeatsSuppressed=" +
                _repeatSelectionsSuppressed +
                " weakActiveSkips=" +
                _weakActiveDefenseSkips +
                " weakFrontierSkips=" +
                _weakFrontierDefenseSkips +
                " engagePartyCandidates=" +
                _engagePartyCandidates +
                " engagePartyMobileTargets=" +
                _engagePartyMobileTargets +
                " engagePartyNonBanditTargets=" +
                _engagePartyNonBanditTargets +
                " banditEngageCandidates=" +
                _banditEngageCandidates +
                " rearSecurityEligible=" +
                _rearSecurityEligible +
                " rearSecurityWeakSkips=" +
                _rearSecurityWeakSkips +
                " nextHour=" +
                _nextContextRefreshHour
                    .ToString("0.0"));
        }

        private static SettlementContext
            GetContext(Settlement settlement)
        {
            if (settlement == null ||
                string.IsNullOrEmpty(
                    settlement.StringId))
            {
                return null;
            }

            SettlementContext context;

            return
                ContextBySettlement.TryGetValue(
                    settlement.StringId,
                    out context)
                    ? context
                    : null;
        }

        private static bool IsBandit(
            MobileParty party)
        {
            return
                party != null &&
                !string.IsNullOrEmpty(
                    party.StringId) &&
                BanditPartyIds.Contains(
                    party.StringId);
        }

        private static bool IsDefensiveTravel(
            AiBehavior behavior)
        {
            return
                behavior ==
                    AiBehavior.GoToSettlement ||
                behavior ==
                    AiBehavior.DefendSettlement ||
                behavior ==
                    AiBehavior.PatrolAroundPoint;
        }

        private static bool IsAggressive(
            AiBehavior behavior)
        {
            return
                behavior ==
                    AiBehavior.RaidSettlement ||
                behavior ==
                    AiBehavior.BesiegeSettlement ||
                behavior ==
                    AiBehavior.AssaultSettlement;
        }

        private static bool SameFaction(
            IFaction a,
            IFaction b)
        {
            if (a == null || b == null)
                return false;

            if (object.ReferenceEquals(a, b))
                return true;

            return string.Equals(
                a.Name.ToString(),
                b.Name.ToString(),
                StringComparison.Ordinal);
        }

        private static float DistanceSquared(
            Vec2 a,
            Vec2 b)
        {
            float dx = a.x - b.x;
            float dy = a.y - b.y;

            return
                (dx * dx) +
                (dy * dy);
        }

        private static int FindBestIndex(
            PartyThinkParams thinkParams)
        {
            int bestIndex = -1;
            float bestScore =
                float.MinValue;

            for (int i = 0;
                 i <
                 thinkParams
                    .AIBehaviorScores.Count;
                 i++)
            {
                float score =
                    thinkParams
                        .AIBehaviorScores[i]
                        .Item2;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestIndex = i;
                }
            }

            return bestIndex;
        }

        private static string CandidateLabel(
            PartyThinkParams thinkParams,
            int index)
        {
            if (index < 0 ||
                index >=
                thinkParams
                    .AIBehaviorScores.Count)
            {
                return "NONE";
            }

            AIBehaviorData data =
                thinkParams
                    .AIBehaviorScores[index]
                    .Item1;

            object target =
                data.Party;

            string targetName =
                target == null
                    ? "<none>"
                    : target.ToString();

            Settlement settlement =
                target as Settlement;

            if (settlement != null)
                targetName =
                    settlement.Name.ToString();

            MobileParty party =
                target as MobileParty;

            if (party != null)
                targetName =
                    party.Name.ToString();

            return
                data.AiBehavior.ToString() +
                ":" +
                targetName;
        }

        private static string FindReasonForWinner(
            PartyThinkParams thinkParams,
            int winnerIndex,
            List<PendingChange> changes)
        {
            if (winnerIndex < 0 ||
                winnerIndex >=
                    thinkParams
                        .AIBehaviorScores.Count)
            {
                return "unknown";
            }

            string winner =
                CandidateLabel(
                    thinkParams,
                    winnerIndex);

            for (int i = 0;
                 i < changes.Count;
                 i++)
            {
                AIBehaviorData data =
                    changes[i].Data;

                object target =
                    data.Party;

                string targetName =
                    target == null
                        ? "<none>"
                        : target.ToString();

                Settlement settlement =
                    target as Settlement;

                if (settlement != null)
                    targetName =
                        settlement.Name.ToString();

                MobileParty party =
                    target as MobileParty;

                if (party != null)
                    targetName =
                        party.Name.ToString();

                string label =
                    data.AiBehavior.ToString() +
                    ":" +
                    targetName;

                if (label == winner)
                    return changes[i].Reason;
            }

            return "combined";
        }


    }
}

