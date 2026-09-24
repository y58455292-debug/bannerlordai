using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace ClanAI
{
    internal static class ActorBlackboard
    {
        internal sealed class State
        {
            internal MobileParty Actor;
            internal Hero LeaderHero;
            internal Clan Clan;
            internal IFaction MapFaction;
            internal Army Army;

            internal Settlement CurrentSettlement;
            internal Settlement TargetSettlement;
            internal Settlement ShortTermTargetSettlement;

            internal MobileParty TargetParty;
            internal MobileParty ShortTermTargetParty;

            internal AiBehavior DefaultBehavior;
            internal AiBehavior ShortTermBehavior;

            internal int Men;
            internal float Readiness;
            internal int FoodDays;

            internal bool IsFleeing;
            internal bool IsCurrentlyAtSea;
            internal bool IsTargetingPort;
            internal double CampaignHours;
            internal long Revision;
        }

        private static readonly Dictionary<string, State>
            StateByActor =
                new Dictionary<string, State>(
                    StringComparer.Ordinal);

        private static long _captures;
        private static long _statesCreated;
        private static long _statesReused;
        private static long _validations;
        private static long _visualWarReads;
        private static long _weakRecoveryReads;
        private static long _mismatches;
        private static long _failures;
        internal static void Reset()
        {
            StateByActor.Clear();

            Interlocked.Exchange(ref _captures, 0);
            Interlocked.Exchange(ref _statesCreated, 0);
            Interlocked.Exchange(ref _statesReused, 0);
            Interlocked.Exchange(ref _validations, 0);
            Interlocked.Exchange(ref _visualWarReads, 0);
            Interlocked.Exchange(ref _weakRecoveryReads, 0);
            Interlocked.Exchange(ref _mismatches, 0);
            Interlocked.Exchange(ref _failures, 0);

            ClanAIPostVanilla.WriteExternalLog(
                "ACTOR_BLACKBOARD_RESET mode=strategic-consume");
        }

        internal static State Capture(
            MobileParty actor)
        {
            if (actor == null ||
                actor.LeaderHero == null)
            {
                return null;
            }

            try
            {
                string key =
                    ActorKey(actor);

                State state;

                if (!StateByActor.TryGetValue(
                        key,
                        out state) ||
                    state.Actor != actor)
                {
                    state = new State
                    {
                        Actor = actor
                    };

                    StateByActor[key] = state;

                    Interlocked.Increment(
                        ref _statesCreated);
                }
                else
                {
                    Interlocked.Increment(
                        ref _statesReused);
                }

                Fill(state, actor);

                state.Revision++;

                Interlocked.Increment(
                    ref _captures);

                return state;
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref _failures);

                ClanAIPostVanilla.WriteExternalLog(
                    "ACTOR_BLACKBOARD_FAILURE" +
                    " phase=capture" +
                    " actor=" +
                    ActorName(actor) +
                    " type=" +
                    ex.GetType().Name);

                return null;
            }
        }
        internal static void Validate(
            State state,
            MobileParty actor)
        {
            if (state == null ||
                actor == null)
            {
                return;
            }

            try
            {
                Interlocked.Increment(
                    ref _validations);

                string mismatch =
                    FindMismatch(
                        state,
                        actor);

                if (mismatch == null)
                    return;

                long count =
                    Interlocked.Increment(
                        ref _mismatches);

                if (count <= 20 ||
                    (count % 100) == 0)
                {
                    ClanAIPostVanilla.WriteExternalLog(
                        "ACTOR_BLACKBOARD_MISMATCH" +
                        " actor=" +
                        ActorName(actor) +
                        " field=" +
                        mismatch +
                        " revision=" +
                        state.Revision +
                        " capturedHours=" +
                        state.CampaignHours.ToString(
                            "R",
                            CultureInfo.InvariantCulture) +
                        " liveHours=" +
                        CampaignTime.Now.ToHours.ToString(
                            "R",
                            CultureInfo.InvariantCulture) +
                        " mismatchCount=" +
                        count);
                }
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref _failures);

                ClanAIPostVanilla.WriteExternalLog(
                    "ACTOR_BLACKBOARD_FAILURE" +
                    " phase=validate" +
                    " actor=" +
                    ActorName(actor) +
                    " type=" +
                    ex.GetType().Name);
            }
        }

        internal static void NoteVisualWarRead()
        {
            Interlocked.Increment(
                ref _visualWarReads);
        }

        internal static void NoteWeakRecoveryRead()
        {
            Interlocked.Increment(
                ref _weakRecoveryReads);
        }

        internal static string SummaryFields()
        {
            return
                " blackboardStates=" +
                StateByActor.Count +
                " blackboardCaptures=" +
                Interlocked.Read(ref _captures) +
                " blackboardStatesCreated=" +
                Interlocked.Read(ref _statesCreated) +
                " blackboardStatesReused=" +
                Interlocked.Read(ref _statesReused) +
                " blackboardValidations=" +
                Interlocked.Read(ref _validations) +
                " blackboardVisualWarReads=" +
                Interlocked.Read(ref _visualWarReads) +
                " blackboardWeakRecoveryReads=" +
                Interlocked.Read(ref _weakRecoveryReads) +
                " blackboardMismatches=" +
                Interlocked.Read(ref _mismatches) +
                " blackboardFailures=" +
                Interlocked.Read(ref _failures);
        }
        private static void Fill(
            State state,
            MobileParty actor)
        {
            state.Actor = actor;
            state.LeaderHero = actor.LeaderHero;
            state.Clan = actor.ActualClan;
            state.MapFaction = actor.MapFaction;
            state.Army = actor.Army;

            state.CurrentSettlement =
                actor.CurrentSettlement;

            state.TargetSettlement =
                actor.TargetSettlement;

            state.ShortTermTargetSettlement =
                actor.ShortTermTargetSettlement;

            state.TargetParty =
                actor.TargetParty;

            state.ShortTermTargetParty =
                actor.ShortTermTargetParty;

            state.DefaultBehavior =
                actor.DefaultBehavior;

            state.ShortTermBehavior =
                actor.ShortTermBehavior;

            state.Men =
                actor.MemberRoster != null
                    ? actor.MemberRoster.TotalManCount
                    : 0;

            state.Readiness =
                actor.PartySizeRatio;

            state.FoodDays =
                actor.GetNumDaysForFoodToLast();

            state.IsFleeing =
                actor.IsFleeing();

            state.IsCurrentlyAtSea =
                actor.IsCurrentlyAtSea;

            state.IsTargetingPort =
                actor.IsTargetingPort;

            state.CampaignHours =
                CampaignTime.Now.ToHours;
        }
        private static string FindMismatch(
            State state,
            MobileParty actor)
        {
            if (state.Actor != actor)
                return "actor";

            if (state.LeaderHero != actor.LeaderHero)
                return "leaderHero";

            if (state.Clan != actor.ActualClan)
                return "clan";

            if (state.MapFaction != actor.MapFaction)
                return "mapFaction";

            if (state.Army != actor.Army)
                return "army";

            if (state.CurrentSettlement !=
                actor.CurrentSettlement)
            {
                return "currentSettlement";
            }

            if (state.TargetSettlement !=
                actor.TargetSettlement)
            {
                return "targetSettlement";
            }

            if (state.ShortTermTargetSettlement !=
                actor.ShortTermTargetSettlement)
            {
                return "shortTermTargetSettlement";
            }

            if (state.TargetParty !=
                actor.TargetParty)
            {
                return "targetParty";
            }

            if (state.ShortTermTargetParty !=
                actor.ShortTermTargetParty)
            {
                return "shortTermTargetParty";
            }

            if (state.DefaultBehavior !=
                actor.DefaultBehavior)
            {
                return "defaultBehavior";
            }

            if (state.ShortTermBehavior !=
                actor.ShortTermBehavior)
            {
                return "shortTermBehavior";
            }
            int men =
                actor.MemberRoster != null
                    ? actor.MemberRoster.TotalManCount
                    : 0;

            if (state.Men != men)
                return "men";

            if (!NearlyEqual(
                    state.Readiness,
                    actor.PartySizeRatio))
            {
                return "readiness";
            }

            if (state.FoodDays !=
                actor.GetNumDaysForFoodToLast())
            {
                return "foodDays";
            }

            if (state.IsFleeing !=
                actor.IsFleeing())
            {
                return "isFleeing";
            }

            if (state.IsCurrentlyAtSea !=
                actor.IsCurrentlyAtSea)
            {
                return "isCurrentlyAtSea";
            }

            if (state.IsTargetingPort !=
                actor.IsTargetingPort)
            {
                return "isTargetingPort";
            }

            return null;
        }

        private static bool NearlyEqual(
            float a,
            float b)
        {
            return Math.Abs(a - b) <= 0.000001f;
        }

        private static string ActorKey(
            MobileParty actor)
        {
            if (!string.IsNullOrEmpty(
                    actor.StringId))
            {
                return actor.StringId;
            }

            if (actor.LeaderHero != null &&
                !string.IsNullOrEmpty(
                    actor.LeaderHero.StringId))
            {
                return actor.LeaderHero.StringId;
            }

            return actor.Name.ToString();
        }

        private static string ActorName(
            MobileParty actor)
        {
            if (actor == null)
                return "<null>";

            if (actor.LeaderHero != null)
                return actor.LeaderHero.Name.ToString();

            return actor.Name.ToString();
        }
    }
}
