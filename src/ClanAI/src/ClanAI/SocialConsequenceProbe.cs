using System;
using System.Collections.Generic;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace ClanAI
{
    public static class SocialConsequenceProbe
    {
        private static readonly Dictionary<string, string> LastPrediction =
            new Dictionary<string, string>();

        private static MethodInfo _relationMethod;
        private static bool _relationMethodResolved;

        public static void Reset()
        {
            LastPrediction.Clear();

            ClanAIPostVanilla.WriteExternalLog(
                "SOCIAL_CONSEQUENCE_RESET");
        }

        internal static void Observe(
            MobileParty actor,
            PartyThinkParams thinkParams,
            StrategicDecisionComposer.Frame composer)
        {
            if (actor == null ||
                actor.LeaderHero == null ||
                thinkParams == null ||
                thinkParams.AIBehaviorScores.Count == 0)
            {
                return;
            }

            int overallBestIndex = -1;
            float overallBestScore = float.MinValue;

            int aggressiveBestIndex = -1;
            float aggressiveBestScore = float.MinValue;

            for (int i = 0;
                 i < thinkParams.AIBehaviorScores.Count;
                 i++)
            {
                AIBehaviorData data =
                    thinkParams.AIBehaviorScores[i].Item1;

                float rawScore =
                    thinkParams.AIBehaviorScores[i].Item2;

                float score =
                    composer != null
                        ? composer.CurrentScore(
                            i,
                            rawScore)
                        : rawScore;

                if (score > overallBestScore)
                {
                    overallBestScore = score;
                    overallBestIndex = i;
                }

                if (!IsSociallyAggressive(
                    data.AiBehavior))
                {
                    continue;
                }

                if (data.Party == null)
                    continue;

                if (score > aggressiveBestScore)
                {
                    aggressiveBestScore = score;
                    aggressiveBestIndex = i;
                }
            }

            if (aggressiveBestIndex < 0)
                return;

            AIBehaviorData aggressive =
                thinkParams
                .AIBehaviorScores[aggressiveBestIndex]
                .Item1;

            object target =
                aggressive.Party;

            string targetType = "unknown";
            string targetName = "<unknown>";
            string ownerHeroName = "<none>";
            string ownerClanName = "<none>";
            string factionName = "<none>";
            string relation = "n/a";

            bool? atWar = null;

            Hero ownerHero = null;
            Clan ownerClan = null;

            Settlement settlement =
                target as Settlement;

            if (settlement != null)
            {
                if (settlement.IsVillage)
                    targetType = "village";
                else if (settlement.IsTown)
                    targetType = "town";
                else if (settlement.IsCastle)
                    targetType = "castle";
                else
                    targetType = "settlement";

                targetName =
                    settlement.Name.ToString();

                ownerHero =
                    settlement.Owner;

                ownerClan =
                    settlement.OwnerClan;

                if (settlement.MapFaction != null)
                {
                    factionName =
                        settlement.MapFaction
                        .Name.ToString();

                    if (actor.MapFaction != null)
                    {
                        atWar =
                            actor.MapFaction.IsAtWarWith(
                                settlement.MapFaction);
                    }
                }
            }
            else
            {
                MobileParty targetParty =
                    target as MobileParty;

                if (targetParty == null)
                    return;

                if (targetParty.IsCaravan)
                    targetType = "caravan";
                else if (targetParty.IsLordParty)
                    targetType = "lord_party";
                else if (targetParty.IsVillager)
                    targetType = "villager";
                else
                    targetType = "mobile_party";

                targetName =
                    targetParty.Name.ToString();

                ownerHero =
                    targetParty.Owner;

                if (ownerHero == null)
                {
                    ownerHero =
                        targetParty.LeaderHero;
                }

                if (ownerHero != null)
                {
                    ownerClan =
                        ownerHero.Clan;
                }

                if (targetParty.MapFaction != null)
                {
                    factionName =
                        targetParty.MapFaction
                        .Name.ToString();

                    if (actor.MapFaction != null)
                    {
                        atWar =
                            actor.MapFaction.IsAtWarWith(
                                targetParty.MapFaction);
                    }
                }
            }

            if (ownerHero != null)
            {
                ownerHeroName =
                    ownerHero.Name.ToString();

                relation =
                    TryGetRelation(
                        actor.LeaderHero,
                        ownerHero);
            }

            if (ownerClan != null)
            {
                ownerClanName =
                    ownerClan.Name.ToString();
            }

            string actorClanName =
                actor.LeaderHero.Clan != null
                    ? actor.LeaderHero.Clan.Name.ToString()
                    : "<none>";

            bool isOverallTop =
                aggressiveBestIndex ==
                overallBestIndex;

            string warText =
                atWar.HasValue
                    ? atWar.Value.ToString()
                    : "unknown";

            string signature =
                aggressive.AiBehavior.ToString() +
                "|" +
                targetType +
                "|" +
                targetName +
                "|" +
                ownerHeroName +
                "|" +
                ownerClanName +
                "|" +
                factionName +
                "|" +
                relation +
                "|" +
                warText +
                "|" +
                isOverallTop;

            string actorKey =
                actor.LeaderHero.StringId;

            if (string.IsNullOrEmpty(actorKey))
            {
                actorKey =
                    actor.LeaderHero.Name.ToString();
            }

            string previous;

            if (LastPrediction.TryGetValue(
                actorKey,
                out previous) &&
                previous == signature)
            {
                return;
            }

            LastPrediction[actorKey] =
                signature;

            ClanAIPostVanilla.WriteExternalLog(
                "CONSEQUENCE_CONSIDER" +
                " actor=" +
                actor.LeaderHero.Name.ToString() +
                " actorClan=" +
                actorClanName +
                " action=" +
                aggressive.AiBehavior.ToString() +
                " targetType=" +
                targetType +
                " target=" +
                targetName +
                " ownerHero=" +
                ownerHeroName +
                " ownerClan=" +
                ownerClanName +
                " faction=" +
                factionName +
                " relation=" +
                relation +
                " atWar=" +
                warText +
                " aggressiveScore=" +
                aggressiveBestScore.ToString("0.000") +
                " overallTop=" +
                isOverallTop);
        }

        private static bool IsSociallyAggressive(
            AiBehavior behavior)
        {
            return
                behavior == AiBehavior.RaidSettlement ||
                behavior == AiBehavior.BesiegeSettlement ||
                behavior == AiBehavior.AssaultSettlement ||
                behavior == AiBehavior.EngageParty;
        }

        private static string TryGetRelation(
            Hero actor,
            Hero target)
        {
            if (actor == null ||
                target == null)
            {
                return "n/a";
            }

            try
            {
                if (!_relationMethodResolved)
                {
                    _relationMethodResolved = true;

                    Type type =
                        typeof(Hero)
                        .Assembly
                        .GetType(
                            "TaleWorlds.CampaignSystem.CharacterRelationManager",
                            false);

                    if (type != null)
                    {
                        _relationMethod =
                            type.GetMethod(
                                "GetHeroRelation",
                                BindingFlags.Public |
                                BindingFlags.NonPublic |
                                BindingFlags.Static,
                                null,
                                new Type[]
                                {
                                    typeof(Hero),
                                    typeof(Hero)
                                },
                                null);
                    }
                }

                if (_relationMethod == null)
                    return "n/a";

                object value =
                    _relationMethod.Invoke(
                        null,
                        new object[]
                        {
                            actor,
                            target
                        });

                if (value == null)
                    return "n/a";

                return value.ToString();
            }
            catch
            {
                return "n/a";
            }
        }
    }
}
