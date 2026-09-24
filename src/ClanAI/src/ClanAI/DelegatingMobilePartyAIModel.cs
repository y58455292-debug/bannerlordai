using System;
using System.Collections.Generic;
using System.Threading;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;

namespace ClanAI
{
    /// <summary>
    /// v0.20B global judgment wrapper around TaleWorlds' currently registered
    /// MobilePartyAIModel.
    ///
    /// All scalar properties and decision methods delegate unchanged except one
    /// proven recovery conflict:
    /// when vanilla's final initiative result is EngageParty against a bandit,
    /// a weak independent hero-led lord yields that short-term initiative so its
    /// existing strategic/recovery behavior can continue.
    ///
    /// Movement, pathfinding, combat, target discovery, and healthy-lord pursuit
    /// remain TaleWorlds-owned.
    /// </summary>
    public sealed class DelegatingMobilePartyAIModel
        : MobilePartyAIModel
    {
        private readonly MobilePartyAIModel _inner;
        private readonly RecoveryGateMode _recoveryGateMode;

        private static long _getBestCalls;
        private static long _weakLordBanditEngageSuppressed;
        private static long _weakLordBanditEngageControlPassed;
        private static long _healthyLordBanditEngagePassed;
        private static long _weakLordBanditNonEngage;
        private static long _nextSummaryAt = 100000;
        private static string _activeModeName = "Suppress";

        private static readonly object DetailSync =
            new object();

        private static readonly HashSet<string>
            LoggedSuppressionPairs =
                new HashSet<string>(
                    StringComparer.Ordinal);

        private static readonly HashSet<string>
            LoggedControlPairs =
                new HashSet<string>(
                    StringComparer.Ordinal);

        // v0.20C-review: diagnostics only; decision methods are unchanged.
        public static void ResetDiagnostics()
        {
            Interlocked.Exchange(ref _getBestCalls, 0);
            Interlocked.Exchange(ref _weakLordBanditEngageSuppressed, 0);
            Interlocked.Exchange(ref _weakLordBanditEngageControlPassed, 0);
            Interlocked.Exchange(ref _healthyLordBanditEngagePassed, 0);
            Interlocked.Exchange(ref _weakLordBanditNonEngage, 0);
            Interlocked.Exchange(ref _nextSummaryAt, 100000);
            lock (DetailSync)
            {
                LoggedSuppressionPairs.Clear();
                LoggedControlPairs.Clear();
            }
        }

        public static string DiagnosticSummary(string reason)
        {
            return "GLOBAL_AI_JUDGMENT_SNAPSHOT reason=" + reason
                + " scope=campaign_session"
                + " gateMode=" + _activeModeName
                + " getBestCalls=" + Interlocked.Read(ref _getBestCalls)
                + " weakBanditEngageSuppressed=" + Interlocked.Read(ref _weakLordBanditEngageSuppressed)
                + " weakBanditEngageControlPassed=" + Interlocked.Read(ref _weakLordBanditEngageControlPassed)
                + " healthyBanditEngagePassed=" + Interlocked.Read(ref _healthyLordBanditEngagePassed)
                + " weakBanditNonEngage=" + Interlocked.Read(ref _weakLordBanditNonEngage)
                + SocialMemoryCausalLayer.SummaryFields()
                + StrategicDecisionComposer.SummaryFields()
                + ActorBlackboard.SummaryFields()
                + ActorStrategicBlackboard.SummaryFields()
                + StrategicCommitmentLayer.SummaryFields()
                + DynastyMindSeed.SummaryFields()
                + DynastyBranchEpisodeMemory.SummaryFields();
        }

        public MobilePartyAIModel InnerModel
        {
            get { return _inner; }
        }

        public DelegatingMobilePartyAIModel(
            MobilePartyAIModel inner)
        {
            if (inner == null)
                throw new ArgumentNullException("inner");

            _inner = inner;

            string configStatus =
                "forced_suppress_only";

            _recoveryGateMode =
                RecoveryGateMode.Suppress;

            _activeModeName =
                "SuppressOnly";

            ClanAIPostVanilla.WriteExternalLog(
                "GLOBAL_AI_RECOVERY_GATE_MODE" +
                " mode=" + _activeModeName +
                " configStatus=" + configStatus +
                " configPath=" +
                RecoveryGateConfig.ConfigPath);
        }

        public override float AiCheckInterval
        {
            get { return _inner.AiCheckInterval; }
        }

        public override float FleeToNearbyPartyRadius
        {
            get { return _inner.FleeToNearbyPartyRadius; }
        }

        public override float FleeToNearbySettlementRadius
        {
            get { return _inner.FleeToNearbySettlementRadius; }
        }

        public override float FortificationPatrolDistanceAsDays
        {
            get { return _inner.FortificationPatrolDistanceAsDays; }
        }

        public override float FortificationPortPatrolDistanceAsDays
        {
            get { return _inner.FortificationPortPatrolDistanceAsDays; }
        }

        public override float HideoutPatrolDistanceAsDays
        {
            get { return _inner.HideoutPatrolDistanceAsDays; }
        }

        public override float NeededFoodsInDaysThresholdForRaid
        {
            get { return _inner.NeededFoodsInDaysThresholdForRaid; }
        }

        public override float NeededFoodsInDaysThresholdForSiege
        {
            get { return _inner.NeededFoodsInDaysThresholdForSiege; }
        }

        public override float SettlementDefendingNearbyPartyCheckRadius
        {
            get { return _inner.SettlementDefendingNearbyPartyCheckRadius; }
        }

        public override float SettlementDefendingWaitingPositionRadius
        {
            get { return _inner.SettlementDefendingWaitingPositionRadius; }
        }

        public override float VillagePatrolDistanceAsDays
        {
            get { return _inner.VillagePatrolDistanceAsDays; }
        }

        public override bool ShouldPartyCheckInitiativeBehavior(
            MobileParty party)
        {
            return _inner
                .ShouldPartyCheckInitiativeBehavior(
                    party);
        }

        public override bool ShouldConsiderAttacking(
            MobileParty party,
            MobileParty targetParty)
        {
            return _inner.ShouldConsiderAttacking(
                party,
                targetParty);
        }

        public override bool ShouldConsiderAvoiding(
            MobileParty party,
            MobileParty targetParty)
        {
            return _inner.ShouldConsiderAvoiding(
                party,
                targetParty);
        }

        public override void GetBestInitiativeBehavior(
            MobileParty party,
            out AiBehavior bestBehavior,
            out MobileParty bestParty,
            out float bestScore,
            out Vec2 bestInitiativeDirection)
        {
            _inner.GetBestInitiativeBehavior(
                party,
                out bestBehavior,
                out bestParty,
                out bestScore,
                out bestInitiativeDirection);

            Interlocked.Increment(
                ref _getBestCalls);

            bool banditTarget =
                IsBandit(bestParty);

            if (banditTarget &&
                IsIndependentAiLord(party))
            {
                bool weakForRecovery =
                    IsWeakForRecovery(party);

                if (weakForRecovery)
                {
                    if (bestBehavior ==
                        AiBehavior.EngageParty)
                    {
                        if (_recoveryGateMode ==
                            RecoveryGateMode.Observe)
                        {
                            Interlocked.Increment(
                                ref _weakLordBanditEngageControlPassed);

                            LogControlPassOnce(
                                party,
                                bestParty,
                                bestScore);
                        }
                        else
                        {
                            Interlocked.Increment(
                                ref _weakLordBanditEngageSuppressed);

                            LogSuppressionOnce(
                                party,
                                bestParty,
                                bestScore);

                            bestBehavior =
                                AiBehavior.None;

                            bestParty = null;
                            bestScore = 0f;
                            bestInitiativeDirection =
                                new Vec2(0f, 0f);
                        }
                    }
                    else
                    {
                        Interlocked.Increment(
                            ref _weakLordBanditNonEngage);
                    }
                }
                else if (
                    bestBehavior ==
                        AiBehavior.EngageParty)
                {
                    Interlocked.Increment(
                        ref _healthyLordBanditEngagePassed);
                }
            }

            MaybeLogSummary();
        }

        public override float GetPatrolRadius(
            MobileParty party,
            CampaignVec2 center)
        {
            return _inner.GetPatrolRadius(
                party,
                center);
        }

        public override float GetSettlementNearbyThreatAndAllyCheckRadius(
            Settlement settlement,
            bool isNaval)
        {
            return _inner
                .GetSettlementNearbyThreatAndAllyCheckRadius(
                    settlement,
                    isNaval);
        }
        private static bool IsWeakForRecovery(
            MobileParty party)
        {
            try
            {
                return
                    party != null &&
                    (
                        party.PartySizeRatio < 0.72f ||
                        party.GetNumDaysForFoodToLast() < 3
                    );
            }
            catch
            {
                return false;
            }
        }

        private static bool IsIndependentAiLord(
            MobileParty party)
        {
            try
            {
                return
                    party != null &&
                    party.IsActive &&
                    party.IsLordParty &&
                    !party.IsMainParty &&
                    party.LeaderHero != null &&
                    party.Army == null;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsBandit(
            MobileParty party)
        {
            try
            {
                return
                    party != null &&
                    party.IsBandit;
            }
            catch
            {
                return false;
            }
        }

        private static void LogSuppressionOnce(
            MobileParty actor,
            MobileParty target,
            float vanillaScore)
        {
            try
            {
                string actorId =
                    actor == null
                        ? "<null>"
                        : actor.StringId;

                string targetId =
                    target == null
                        ? "<null>"
                        : target.StringId;

                string key =
                    actorId +
                    "|" +
                    targetId;

                lock (DetailSync)
                {
                    if (!LoggedSuppressionPairs.Add(key))
                        return;
                }

                float readiness =
                    actor.PartySizeRatio;

                int foodDays =
                    actor.GetNumDaysForFoodToLast();

                ClanAIPostVanilla.WriteExternalLog(
                    "GLOBAL_AI_WEAK_BANDIT_ENGAGE_SUPPRESSED" +
                    " actor=" +
                    actor.Name.ToString() +
                    " actorId=" +
                    actorId +
                    " target=" +
                    target.Name.ToString() +
                    " targetId=" +
                    targetId +
                    " readiness=" +
                    readiness.ToString("0.000") +
                    " readinessExact=" +
                    readiness.ToString(
                        "R",
                        System.Globalization.CultureInfo.InvariantCulture) +
                    " gateMode=Suppress" +
                    " campaignHours=" +
                    CampaignTime.Now.ToHours.ToString(
                        "R",
                        System.Globalization.CultureInfo.InvariantCulture) +
                    " foodDays=" +
                    foodDays +
                    " vanillaScore=" +
                    vanillaScore.ToString("0.000") +
                    " replacement=None");
            }
            catch
            {
            }
        }

        private static void LogControlPassOnce(
            MobileParty actor,
            MobileParty target,
            float vanillaScore)
        {
            try
            {
                string actorId =
                    actor == null
                        ? "<null>"
                        : actor.StringId;

                string targetId =
                    target == null
                        ? "<null>"
                        : target.StringId;

                string key =
                    actorId +
                    "|" +
                    targetId;

                lock (DetailSync)
                {
                    if (!LoggedControlPairs.Add(key))
                        return;
                }

                float readiness =
                    actor.PartySizeRatio;

                int foodDays =
                    actor.GetNumDaysForFoodToLast();

                ClanAIPostVanilla.WriteExternalLog(
                    "GLOBAL_AI_WEAK_BANDIT_ENGAGE_CONTROL_PASS" +
                    " actor=" +
                    actor.Name.ToString() +
                    " actorId=" +
                    actorId +
                    " target=" +
                    target.Name.ToString() +
                    " targetId=" +
                    targetId +
                    " readiness=" +
                    readiness.ToString("0.000") +
                    " readinessExact=" +
                    readiness.ToString(
                        "R",
                        System.Globalization.CultureInfo.InvariantCulture) +
                    " gateMode=Observe" +
                    " campaignHours=" +
                    CampaignTime.Now.ToHours.ToString(
                        "R",
                        System.Globalization.CultureInfo.InvariantCulture) +
                    " foodDays=" +
                    foodDays +
                    " vanillaScore=" +
                    vanillaScore.ToString("0.000") +
                    " replacement=NativePassThrough");
            }
            catch
            {
            }
        }

        private static void MaybeLogSummary()
        {
            long calls =
                Interlocked.Read(
                    ref _getBestCalls);

            long threshold =
                Interlocked.Read(
                    ref _nextSummaryAt);

            if (calls < threshold)
                return;

            if (Interlocked.CompareExchange(
                    ref _nextSummaryAt,
                    threshold + 100000,
                    threshold) != threshold)
            {
                return;
            }

            ClanAIPostVanilla.WriteExternalLog(
                "GLOBAL_AI_JUDGMENT_SUMMARY" +
                " gateMode=" +
                _activeModeName +
                " getBestCalls=" +
                calls +
                " weakBanditEngageSuppressed=" +
                Interlocked.Read(
                    ref _weakLordBanditEngageSuppressed) +
                " weakBanditEngageControlPassed=" +
                Interlocked.Read(
                    ref _weakLordBanditEngageControlPassed) +
                " healthyBanditEngagePassed=" +
                Interlocked.Read(
                    ref _healthyLordBanditEngagePassed) +
                " weakBanditNonEngage=" +
                Interlocked.Read(
                    ref _weakLordBanditNonEngage));
        }

    }
}
