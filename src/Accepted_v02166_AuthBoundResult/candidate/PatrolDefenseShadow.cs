using System;
using System.Globalization;
using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;

namespace BannerlordAITestRunner
{
    internal sealed class PatrolDefenseShadowResult
    {
        public string Fingerprint;
        public string Json;
        public bool WouldInterrupt;
        public bool ThreatFound;
        public string BanditId;
        public string VillagerId;
        public string ProposalId;
        public double ProposalCampaignHours;
        public string PatrolSettlementId;
        public float ActorStrength;
        public int ActorHealthy;
        public float BanditStrength;
        public int BanditHealthy;
        public float ActorToBanditStrengthRatio;
    }

    internal static class PatrolDefenseShadow
    {
        public static PatrolDefenseShadowResult Evaluate(
            Settlement patrolSettlement)
        {
            if (Campaign.Current == null ||
                patrolSettlement == null ||
                MobileParty.MainParty == null)
            {
                return null;
            }

            MobileParty actor = MobileParty.MainParty;
            float radius = GetNativeThreatRadius(
                patrolSettlement);
            if (radius <= 0f)
                return null;

            Vec2 center = patrolSettlement.GetPosition2D;
            float radiusSq = radius * radius;

            MobileParty bestBandit = null;
            MobileParty bestVictim = null;
            string bestTargetSource = null;
            float bestVictimDistance = float.MaxValue;
            float bestActorDistance = float.MaxValue;
            int scanned = 0;
            int localThreats = 0;

            var search =
                MobileParty.StartFindingLocatablesAroundPosition(
                    center,
                    radius);

            while (true)
            {
                MobileParty candidate =
                    MobileParty.FindNextLocatable(
                        ref search);

                if (candidate == null)
                    break;

                scanned++;

                if (!IsActiveBandit(candidate))
                    continue;

                MobileParty victim =
                    GetVillagerTarget(
                        candidate,
                        out string targetSource);

                if (victim == null ||
                    !IsLocalVillager(
                        victim,
                        patrolSettlement))
                {
                    continue;
                }

                float settlementDistanceSq =
                    DistanceSquared(
                        candidate.GetPosition2D,
                        center);

                if (settlementDistanceSq > radiusSq)
                    continue;

                localThreats++;

                float victimDistance =
                    Distance(
                        candidate.GetPosition2D,
                        victim.GetPosition2D);
                float actorDistance =
                    Distance(
                        actor.GetPosition2D,
                        candidate.GetPosition2D);

                if (victimDistance < bestVictimDistance ||
                    (Math.Abs(
                        victimDistance -
                        bestVictimDistance) < 0.001f &&
                     actorDistance < bestActorDistance))
                {
                    bestBandit = candidate;
                    bestVictim = victim;
                    bestTargetSource = targetSource;
                    bestVictimDistance = victimDistance;
                    bestActorDistance = actorDistance;
                }
            }

            return BuildResult(
                actor,
                patrolSettlement,
                radius,
                scanned,
                localThreats,
                bestBandit,
                bestVictim,
                bestTargetSource,
                bestVictimDistance,
                bestActorDistance);
        }

        private static PatrolDefenseShadowResult BuildResult(
            MobileParty actor,
            Settlement patrolSettlement,
            float radius,
            int scanned,
            int localThreats,
            MobileParty bandit,
            MobileParty victim,
            string targetSource,
            float victimDistance,
            float actorDistance)
        {
            float actorStrength =
                SafeStrength(actor);
            int actorHealthy =
                SafeHealthy(actor);

            bool threatFound =
                bandit != null &&
                victim != null;

            float banditStrength =
                threatFound
                    ? SafeStrength(bandit)
                    : 0f;

            int banditHealthy =
                threatFound
                    ? SafeHealthy(bandit)
                    : 0;

            float strengthRatio =
                threatFound &&
                banditStrength > 0.001f
                    ? actorStrength /
                        banditStrength
                    : 0f;

            bool safeToIntervene =
                threatFound &&
                actorHealthy > 0 &&
                actorStrength > 0f &&
                banditStrength > 0f &&
                actorStrength >= banditStrength;

            bool wouldInterrupt =
                threatFound &&
                safeToIntervene;

            string reason;

            if (!threatFound)
            {
                reason =
                    "no_local_bandit_targeting_local_villager";
            }
            else if (!safeToIntervene)
            {
                reason =
                    "threat_found_but_actor_outmatched_or_unready";
            }
            else
            {
                reason =
                    "local_villager_under_bandit_threat_and_actor_not_outmatched";
            }

            string banditId =
                threatFound
                    ? Safe(bandit.StringId)
                    : "";
            string victimId =
                threatFound
                    ? Safe(victim.StringId)
                    : "";

            string fingerprint =
                threatFound
                    ? banditId + "|" +
                        victimId + "|" +
                        wouldInterrupt
                    : "none";

            double campaignHours =
                CampaignTime.Now.ToHours;
            string settlementId =
                Safe(patrolSettlement.StringId);
            string proposalId =
                "patrol-defense|" +
                settlementId + "|" +
                banditId + "|" +
                victimId + "|" +
                campaignHours.ToString(
                    "R",
                    CultureInfo.InvariantCulture);

            StringBuilder sb =
                new StringBuilder();

            sb.Append("{");
            sb.Append("\"schema\":\"BannerlordAI.PatrolDefenseShadow.v1\",");
            sb.Append("\"mode\":\"observe\",");
            sb.Append("\"decisionFamily\":\"patrol_local_defense\",");
            sb.Append("\"proposalId\":");
            sb.Append(Json(proposalId));
            sb.Append(",\"proposalFingerprint\":");
            sb.Append(Json(fingerprint));
            sb.Append(",\"actor\":");
            sb.Append(Json(
                actor.Name == null
                    ? "<unnamed>"
                    : actor.Name.ToString()));
            sb.Append(",\"actorId\":");
            sb.Append(Json(Safe(actor.StringId)));
            sb.Append(",\"campaignHours\":");
            sb.Append(
                campaignHours.ToString(
                    "R",
                    CultureInfo.InvariantCulture));
            sb.Append(",\"patrolSettlement\":");
            sb.Append(Json(
                patrolSettlement.Name == null
                    ? "<unnamed>"
                    : patrolSettlement.Name.ToString()));
            sb.Append(",\"patrolSettlementId\":");
            sb.Append(Json(
                Safe(patrolSettlement.StringId)));
            sb.Append(",\"nativeThreatRadius\":");
            sb.Append(
                radius.ToString(
                    "R",
                    CultureInfo.InvariantCulture));
            sb.Append(",\"nearbyPartiesScanned\":");
            sb.Append(
                scanned.ToString(
                    CultureInfo.InvariantCulture));
            sb.Append(",\"localThreatCount\":");
            sb.Append(
                localThreats.ToString(
                    CultureInfo.InvariantCulture));
            sb.Append(",\"threatFound\":");
            sb.Append(
                threatFound
                    ? "true"
                    : "false");

            sb.Append(",\"wouldInterrupt\":");
            sb.Append(
                wouldInterrupt
                    ? "true"
                    : "false");
            sb.Append(",\"safeToIntervene\":");
            sb.Append(
                safeToIntervene
                    ? "true"
                    : "false");
            sb.Append(",\"reason\":");
            sb.Append(Json(reason));
            sb.Append(",\"candidateAction\":");
            sb.Append(Json(
                wouldInterrupt
                    ? "ENGAGE_BANDIT"
                    : "CONTINUE_PATROL"));
            sb.Append(",\"resumeAction\":");
            sb.Append(Json(
                "PATROL_SETTLEMENT:" +
                Safe(patrolSettlement.StringId)));
            sb.Append(",\"actorStrength\":");
            sb.Append(
                actorStrength.ToString(
                    "R",
                    CultureInfo.InvariantCulture));
            sb.Append(",\"actorHealthy\":");
            sb.Append(
                actorHealthy.ToString(
                    CultureInfo.InvariantCulture));

            if (threatFound)
            {
                sb.Append(",\"threat\":{");
                sb.Append("\"banditId\":");
                sb.Append(Json(banditId));
                sb.Append(",\"banditName\":");
                sb.Append(Json(
                    bandit.Name == null
                        ? "<unnamed>"
                        : bandit.Name.ToString()));
                sb.Append(",\"banditFaction\":");
                sb.Append(Json(
                    bandit.MapFaction == null ||
                    bandit.MapFaction.Name == null
                        ? "<none>"
                        : bandit.MapFaction.Name.ToString()));
                sb.Append(",\"victimId\":");
                sb.Append(Json(victimId));
                sb.Append(",\"victimName\":");
                sb.Append(Json(
                    victim.Name == null
                        ? "<unnamed>"
                        : victim.Name.ToString()));
                sb.Append(",\"victimFaction\":");
                sb.Append(Json(
                    victim.MapFaction == null ||
                    victim.MapFaction.Name == null
                        ? "<none>"
                        : victim.MapFaction.Name.ToString()));
                sb.Append(",\"targetSource\":");
                sb.Append(Json(
                    targetSource ?? "<none>"));
                sb.Append(",\"banditStrength\":");
                sb.Append(
                    banditStrength.ToString(
                        "R",
                        CultureInfo.InvariantCulture));
                sb.Append(",\"banditHealthy\":");
                sb.Append(
                    banditHealthy.ToString(
                        CultureInfo.InvariantCulture));
                sb.Append(",\"actorToBanditDistance\":");
                sb.Append(
                    actorDistance.ToString(
                        "R",
                        CultureInfo.InvariantCulture));
                sb.Append(",\"banditToVillagerDistance\":");
                sb.Append(
                    victimDistance.ToString(
                        "R",
                        CultureInfo.InvariantCulture));
                sb.Append(",\"actorToBanditStrengthRatio\":");
                sb.Append(
                    strengthRatio.ToString(
                        "R",
                        CultureInfo.InvariantCulture));
                sb.Append("}");
            }

            sb.Append(",\"behaviorMutation\":false");
            sb.Append(",\"intentMutation\":false");
            sb.Append(",\"scoreMutation\":false");
            sb.Append("}");

            return
                new PatrolDefenseShadowResult
                {
                    Fingerprint = fingerprint,
                    Json = sb.ToString(),
                    WouldInterrupt =
                        wouldInterrupt,
                    ThreatFound =
                        threatFound,
                    BanditId = banditId,
                    VillagerId = victimId,
                    ProposalId = proposalId,
                    ProposalCampaignHours = campaignHours,
                    PatrolSettlementId = settlementId,
                    ActorStrength = actorStrength,
                    ActorHealthy = actorHealthy,
                    BanditStrength = banditStrength,
                    BanditHealthy = banditHealthy,
                    ActorToBanditStrengthRatio = strengthRatio,
                };
        }

        private static float GetNativeThreatRadius(
            Settlement settlement)
        {
            try
            {
                if (Campaign.Current == null ||
                    Campaign.Current.Models == null ||
                    Campaign.Current.Models
                        .MobilePartyAIModel == null)
                {
                    return 0f;
                }

                return
                    Campaign.Current.Models
                        .MobilePartyAIModel
                        .GetSettlementNearbyThreatAndAllyCheckRadius(
                            settlement,
                            false);
            }
            catch
            {
                return 0f;
            }
        }

        private static bool IsActiveBandit(
            MobileParty party)
        {
            try
            {
                return
                    party != null &&
                    party.IsActive &&
                    party.IsBandit;
            }
            catch
            {
                return false;
            }
        }

        private static MobileParty GetVillagerTarget(
            MobileParty bandit,
            out string source)
        {
            source = null;

            try
            {
                MobileParty target =
                    bandit.TargetParty;

                if (target != null &&
                    target.IsVillager)
                {
                    source = "TargetParty";
                    return target;
                }

                target =
                    bandit.ShortTermTargetParty;

                if (target != null &&
                    target.IsVillager)
                {
                    source =
                        "ShortTermTargetParty";
                    return target;
                }
            }
            catch
            {
            }

            return null;
        }

        private static bool IsLocalVillager(
            MobileParty victim,
            Settlement patrolSettlement)
        {
            try
            {
                if (victim == null ||
                    patrolSettlement == null ||
                    !victim.IsVillager)
                {
                    return false;
                }

                if (victim.MapFaction == null ||
                    patrolSettlement.MapFaction == null)
                {
                    return false;
                }

                return
                    object.ReferenceEquals(
                        victim.MapFaction,
                        patrolSettlement.MapFaction) ||
                    string.Equals(
                        victim.MapFaction.StringId,
                        patrolSettlement
                            .MapFaction.StringId,
                        StringComparison.Ordinal);
            }
            catch
            {
                return false;
            }
        }

        private static float SafeStrength(
            MobileParty party)
        {
            try
            {
                return
                    party == null ||
                    party.Party == null
                        ? 0f
                        : party.Party
                            .EstimatedStrength;
            }
            catch
            {
                return 0f;
            }
        }

        private static int SafeHealthy(
            MobileParty party)
        {
            try
            {
                return
                    party == null ||
                    party.Party == null
                        ? 0
                        : party.Party
                            .NumberOfHealthyMembers;
            }
            catch
            {
                return 0;
            }
        }

        private static float Distance(
            Vec2 a,
            Vec2 b)
        {
            return
                (float)Math.Sqrt(
                    DistanceSquared(
                        a,
                        b));
        }

        private static float DistanceSquared(
            Vec2 a,
            Vec2 b)
        {
            float dx = a.x - b.x;
            float dy = a.y - b.y;

            return
                dx * dx +
                dy * dy;
        }

        private static string Safe(
            string value)
        {
            return value ?? "";
        }

        private static string Json(
            string value)
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
