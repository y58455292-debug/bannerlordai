using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace ClanAI
{
    public sealed class WarStateBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(
                this, new Action<CampaignGameStarter>(OnSessionLaunched));
            CampaignEvents.OnSiegeEventStartedEvent.AddNonSerializedListener(
                this, new Action<SiegeEvent>(WarStateTracker.OnSiegeStarted));
            CampaignEvents.OnSiegeEventEndedEvent.AddNonSerializedListener(
                this, new Action<SiegeEvent>(WarStateTracker.OnSiegeEnded));
            CampaignEvents.RaidCompletedEvent.AddNonSerializedListener(
                this, new Action<BattleSideEnum, RaidEventComponent>(WarStateTracker.OnRaidCompleted));
            CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(
                this,
                new Action<Settlement, bool, Hero, Hero, Hero,
                    ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail>(
                        WarStateTracker.OnSettlementOwnerChanged));
            CampaignEvents.WarDeclared.AddNonSerializedListener(
                this,
                new Action<IFaction, IFaction, DeclareWarAction.DeclareWarDetail>(
                    WarStateTracker.OnWarDeclared));
            CampaignEvents.MakePeace.AddNonSerializedListener(
                this,
                new Action<IFaction, IFaction, MakePeaceAction.MakePeaceDetail>(
                    WarStateTracker.OnPeace));
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(
                this, new Action(WarStateTracker.OnHourlyTick));
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(
                this, new Action(WarStateTracker.OnDailyTick));
        }

        public override void SyncData(IDataStore dataStore)
        {
            WarStateTracker.SyncData(dataStore);
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            WarStateTracker.BeginSession();
        }
    }

    internal static class WarStateTracker
    {
        internal enum ScarCause
        {
            SIEGE_SURVIVED = 0,
            SIEGE_CAPTURED = 1,
            SUSTAINED_RAIDING = 2
        }

        internal enum WarReason
        {
            ReclaimLostHolding = 0,
            RetaliationForRaids = 1,
            BorderSecurity = 2,
            SuccessionClaim = 3
        }

        internal enum ObjectiveType
        {
            CaptureSpecificSettlement = 0,
            PunishEnemyEconomy = 1,
            BreakEnemyFieldPower = 2,
            ForceTribute = 3
        }

        private sealed class WarScar
        {
            internal string SettlementId;
            internal float ProsperityPenalty;
            internal float LoyaltyPenalty;
            internal double AppliedAt;
            internal float DecayRate;
            internal ScarCause Cause;
        }

        private sealed class SiegeTrack
        {
            internal string SettlementId;
            internal string StartOwnerClanId;
            internal string StartFactionId;
            internal double StartedAt;
            internal double EndedAt;
        }

        private sealed class RaidRecord
        {
            internal string VillageId;
            internal string BoundSettlementId;
            internal string AttackerFactionId;
            internal string DefenderFactionId;
            internal double CompletedAt;
            internal float RaidDamage;
        }

        private sealed class LossRecord
        {
            internal string SettlementId;
            internal string OldFactionId;
            internal string NewFactionId;
            internal double LostAt;
        }

        private sealed class WarObjective
        {
            internal string PairKey;
            internal string AggressorId;
            internal string DefenderId;
            internal WarReason Reason;
            internal ObjectiveType Objective;
            internal string TargetSettlementId;
            internal string DeclarationDetail;
            internal double CreatedAt;
            internal float Progress;
            internal float BaselineEnemyStrength;
            internal float BaselineEnemyEconomy;
            internal string Status;
        }

        private sealed class StrainState
        {
            internal int ActiveWars;
            internal int BesiegedSettlements;
            internal float ScarLoad;
            internal float WarStrain;
            internal double CalculatedAt;
        }

        internal sealed class ObjectiveView
        {
            internal string PairKey;
            internal string KingdomId;
            internal string KingdomName;
            internal string RulerName;
            internal string EnemyId;
            internal string EnemyName;
            internal string Reason;
            internal string Objective;
            internal Settlement Target;
            internal float Progress;
        }

        private const double RaidWindowHours = 30.0 * 24.0;
        private const double LostHoldingWindowHours = 120.0 * 24.0;
        private const float W1 = 0.10f;
        private const float W2 = 0.12f;
        private const float W3 = 0.55f;

        private static readonly List<WarScar> Scars = new List<WarScar>();
        private static readonly Dictionary<string, SiegeTrack> ActiveSieges =
            new Dictionary<string, SiegeTrack>(StringComparer.Ordinal);
        private static readonly List<RaidRecord> Raids = new List<RaidRecord>();
        private static readonly List<LossRecord> Losses = new List<LossRecord>();
        private static readonly Dictionary<string, WarObjective> Objectives =
            new Dictionary<string, WarObjective>(StringComparer.Ordinal);
        private static readonly Dictionary<string, StrainState> StrainByKingdom =
            new Dictionary<string, StrainState>(StringComparer.Ordinal);

        private static bool _loadedFromSave;

        internal static void BeginSession()
        {
            if (!_loadedFromSave)
            {
                Scars.Clear();
                ActiveSieges.Clear();
                Raids.Clear();
                Losses.Clear();
                Objectives.Clear();
            }

            StrainByKingdom.Clear();
            PruneHistories();
            SeedActiveSiegesFromWorld();
            EnsureActiveWarObjectives(true);
            RecomputeAllStrain("session-start");
            LogCurrentScars();

            ClanAIPostVanilla.WriteExternalLog(
                "WAR_STATE_SESSION_READY scars=" + Scars.Count +
                " activeSieges=" + ActiveSieges.Count +
                " raids=" + Raids.Count +
                " losses=" + Losses.Count +
                " objectives=" + Objectives.Count +
                " restored=" + _loadedFromSave +
                " mutation=False");

            _loadedFromSave = false;
        }

        internal static void SyncData(IDataStore dataStore)
        {
            List<string> lines = new List<string>();
            if (dataStore.IsSaving)
                lines = ExportLines();

            bool found = dataStore.SyncData("ClanAI_WarState_v1", ref lines);
            if (dataStore.IsLoading)
                ImportLines(found ? lines : null);
        }

        private static void SeedActiveSiegesFromWorld()
        {
            int seeded = 0;
            foreach (Settlement settlement in Settlement.All)
            {
                if (settlement == null || settlement.SiegeEvent == null ||
                    string.IsNullOrEmpty(settlement.StringId) ||
                    ActiveSieges.ContainsKey(settlement.StringId))
                    continue;

                SiegeEvent siege = settlement.SiegeEvent;
                double startedAt = siege.SiegeStartTime.ToHours;
                if (startedAt <= 0.0)
                    startedAt = CampaignTime.Now.ToHours;

                ActiveSieges[settlement.StringId] = new SiegeTrack
                {
                    SettlementId = settlement.StringId,
                    StartOwnerClanId = settlement.OwnerClan == null
                        ? null
                        : settlement.OwnerClan.StringId,
                    StartFactionId = FactionId(settlement.MapFaction),
                    StartedAt = startedAt,
                    EndedAt = 0.0
                };
                seeded++;

                ClanAIPostVanilla.WriteExternalLog(
                    "WAR_SIEGE_TRACK_SEEDED settlement=" + settlement.Name +
                    " settlementId=" + settlement.StringId +
                    " faction=" + FactionName(settlement.MapFaction) +
                    " startedAt=" + F(startedAt) +
                    " ageDays=" + F(Math.Max(0.0, CampaignTime.Now.ToHours - startedAt) / 24.0) +
                    " mutation=False");
            }

            ClanAIPostVanilla.WriteExternalLog(
                "WAR_SIEGE_SEED_SCAN seeded=" + seeded +
                " activeSieges=" + ActiveSieges.Count +
                " mutation=False");
        }

        internal static void OnSiegeStarted(SiegeEvent siege)
        {
            Settlement settlement = siege == null ? null : siege.BesiegedSettlement;
            if (settlement == null || string.IsNullOrEmpty(settlement.StringId))
                return;

            Clan ownerClan = settlement.OwnerClan;
            IFaction faction = settlement.MapFaction;
            double startedAt = CampaignTime.Now.ToHours;
            if (siege.SiegeStartTime.ToHours > 0.0)
                startedAt = siege.SiegeStartTime.ToHours;

            ActiveSieges[settlement.StringId] = new SiegeTrack
            {
                SettlementId = settlement.StringId,
                StartOwnerClanId = ownerClan == null ? null : ownerClan.StringId,
                StartFactionId = FactionId(faction),
                StartedAt = startedAt,
                EndedAt = 0.0
            };

            ClanAIPostVanilla.WriteExternalLog(
                "WAR_SIEGE_TRACK_START settlement=" + settlement.Name +
                " settlementId=" + settlement.StringId +
                " ownerClan=" + (ownerClan == null ? "<none>" : ownerClan.Name.ToString()) +
                " faction=" + FactionName(faction) +
                " startedAt=" + F(startedAt) +
                " mutation=False");
        }

        internal static void OnSiegeEnded(SiegeEvent siege)
        {
            Settlement settlement = siege == null ? null : siege.BesiegedSettlement;
            if (settlement == null || string.IsNullOrEmpty(settlement.StringId))
                return;

            SiegeTrack track;
            if (!ActiveSieges.TryGetValue(settlement.StringId, out track))
            {
                ClanAIPostVanilla.WriteExternalLog(
                    "WAR_SIEGE_TRACK_END_IGNORED settlement=" + settlement.Name +
                    " settlementId=" + settlement.StringId +
                    " reason=no-active-track mutation=False");
                return;
            }

            track.EndedAt = CampaignTime.Now.ToHours;
            ClanAIPostVanilla.WriteExternalLog(
                "WAR_SIEGE_TRACK_ENDED_PENDING settlement=" + settlement.Name +
                " settlementId=" + settlement.StringId +
                " startFaction=" + (track.StartFactionId ?? "<none>") +
                " currentFaction=" + FactionId(settlement.MapFaction) +
                " durationDays=" + F(Math.Max(0.0, track.EndedAt - track.StartedAt) / 24.0) +
                " mutation=False");
        }

        internal static void OnRaidCompleted(
            BattleSideEnum winnerSide,
            RaidEventComponent raid)
        {
            if (raid == null || raid.AttackerSide == null ||
                winnerSide != raid.AttackerSide.MissionSide)
                return;

            Settlement villageSettlement = raid.MapEventSettlement;
            if (villageSettlement == null || villageSettlement.Village == null)
                return;

            Settlement bound = villageSettlement.Village.Bound;
            if (bound == null)
                return;

            IFaction attacker = raid.AttackerSide == null
                ? null
                : raid.AttackerSide.MapFaction;
            IFaction defender = raid.DefenderSide == null
                ? villageSettlement.MapFaction
                : raid.DefenderSide.MapFaction;

            RaidRecord record = new RaidRecord
            {
                VillageId = villageSettlement.StringId,
                BoundSettlementId = bound.StringId,
                AttackerFactionId = FactionId(attacker),
                DefenderFactionId = FactionId(defender),
                CompletedAt = CampaignTime.Now.ToHours,
                RaidDamage = raid.RaidDamage
            };
            Raids.Add(record);
            PruneHistories();

            int burstCount = CountRecentRaids(
                record.BoundSettlementId,
                record.DefenderFactionId,
                record.CompletedAt);

            ClanAIPostVanilla.WriteExternalLog(
                "WAR_RAID_RECORDED village=" + villageSettlement.Name +
                " bound=" + bound.Name +
                " attacker=" + FactionName(attacker) +
                " defender=" + FactionName(defender) +
                " raidDamage=" + record.RaidDamage.ToString("0.###", CultureInfo.InvariantCulture) +
                " trailingCount=" + burstCount +
                " mutation=False");

            if (burstCount >= 2)
                ApplyOrReinforceRaidScar(bound, burstCount);
        }

        internal static void OnSettlementOwnerChanged(
            Settlement settlement,
            bool openToClaim,
            Hero newOwner,
            Hero oldOwner,
            Hero capturerHero,
            ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
        {
            if (settlement == null || oldOwner == null || oldOwner.Clan == null ||
                newOwner == null || newOwner.Clan == null)
                return;

            string oldFaction = FactionId(oldOwner.Clan.MapFaction);
            string newFaction = FactionId(newOwner.Clan.MapFaction);

            if (detail == ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail.BySiege)
                FinalizePendingSiege(settlement, true, "owner-change-by-siege");
            if (string.IsNullOrEmpty(oldFaction) || string.IsNullOrEmpty(newFaction) ||
                string.Equals(oldFaction, newFaction, StringComparison.Ordinal))
                return;

            Losses.Add(new LossRecord
            {
                SettlementId = settlement.StringId,
                OldFactionId = oldFaction,
                NewFactionId = newFaction,
                LostAt = CampaignTime.Now.ToHours
            });
            PruneHistories();

            ClanAIPostVanilla.WriteExternalLog(
                "WAR_HOLDING_LOSS_RECORDED settlement=" + settlement.Name +
                " oldFaction=" + oldOwner.Clan.MapFaction.Name +
                " newFaction=" + newOwner.Clan.MapFaction.Name +
                " detail=" + detail +
                " mutation=False");
        }

        internal static void OnWarDeclared(
            IFaction faction1,
            IFaction faction2,
            DeclareWarAction.DeclareWarDetail detail)
        {
            Kingdom aggressor = faction1 as Kingdom;
            Kingdom defender = faction2 as Kingdom;
            if (aggressor == null || defender == null)
            {
                ClanAIPostVanilla.WriteExternalLog(
                    "WAR_OBJECTIVE_SKIPPED reason=non-kingdom-war a=" +
                    FactionName(faction1) + " b=" + FactionName(faction2) +
                    " detail=" + detail);
                return;
            }

            CreateOrReplaceObjective(
                aggressor,
                defender,
                detail,
                false);
            RecomputeAllStrain("war-declared");
        }

        internal static void OnPeace(
            IFaction faction1,
            IFaction faction2,
            MakePeaceAction.MakePeaceDetail detail)
        {
            string key = PairKey(faction1, faction2);
            WarObjective objective;
            if (Objectives.TryGetValue(key, out objective))
            {
                ClanAIPostVanilla.WriteExternalLog(
                    "WAR_OBJECTIVE_ENDED pair=" + key +
                    " aggressor=" + objective.AggressorId +
                    " defender=" + objective.DefenderId +
                    " reason=" + objective.Reason +
                    " objective=" + objective.Objective +
                    " progress=" + objective.Progress.ToString("0.###", CultureInfo.InvariantCulture) +
                    " peaceDetail=" + detail +
                    " mutation=False");
                Objectives.Remove(key);
            }
            RecomputeAllStrain("peace");
        }

        internal static void OnHourlyTick()
        {
            FinalizePendingSieges(false);
            KingdomObjectiveLayer.VerifyPendingCommitsFromWorld();
            PlayerVisibilityLayer.NotifyWarStrain();
        }

        internal static void OnDailyTick()
        {
            FinalizePendingSieges(true);
            PruneHistories();
            PruneExpiredScars();
            EnsureActiveWarObjectives(false);
            UpdateObjectiveProgress();
            RecomputeAllStrain("daily");
            PlayerVisibilityLayer.NotifyWarStrain();
        }

        internal static float GetWarStrain(Kingdom kingdom)
        {
            if (kingdom == null)
                return 0f;
            StrainState state;
            if (!StrainByKingdom.TryGetValue(kingdom.StringId, out state))
            {
                RecomputeStrain(kingdom, "on-demand");
                StrainByKingdom.TryGetValue(kingdom.StringId, out state);
            }
            return state == null ? 0f : state.WarStrain;
        }

        internal static bool TryGetWarStrainState(
            Kingdom kingdom,
            out float strain,
            out int activeWars,
            out int besiegedSettlements,
            out float scarLoad)
        {
            strain = 0f;
            activeWars = 0;
            besiegedSettlements = 0;
            scarLoad = 0f;

            if (kingdom == null ||
                string.IsNullOrEmpty(kingdom.StringId))
                return false;

            StrainState state;
            if (!StrainByKingdom.TryGetValue(
                kingdom.StringId,
                out state))
            {
                RecomputeStrain(
                    kingdom,
                    "visibility");
                StrainByKingdom.TryGetValue(
                    kingdom.StringId,
                    out state);
            }

            if (state == null)
                return false;

            strain = state.WarStrain;
            activeWars = state.ActiveWars;
            besiegedSettlements =
                state.BesiegedSettlements;
            scarLoad = state.ScarLoad;
            return true;
        }

        internal static bool TryGetActiveObjectiveFor(
            Kingdom kingdom,
            out ObjectiveView view)
        {
            view = null;
            if (kingdom == null || string.IsNullOrEmpty(kingdom.StringId))
                return false;

            WarObjective selected = null;
            foreach (var pair in Objectives)
            {
                WarObjective objective = pair.Value;
                if (objective == null ||
                    !string.Equals(objective.AggressorId, kingdom.StringId, StringComparison.Ordinal) ||
                    !string.Equals(objective.Status, "Active", StringComparison.Ordinal))
                    continue;

                if (selected == null ||
                    string.CompareOrdinal(objective.PairKey, selected.PairKey) < 0)
                    selected = objective;
            }

            if (selected == null)
                return false;

            Kingdom enemy = FindKingdom(selected.DefenderId);
            view = new ObjectiveView
            {
                PairKey = selected.PairKey,
                KingdomId = kingdom.StringId,
                KingdomName = kingdom.Name == null ? kingdom.StringId : kingdom.Name.ToString(),
                RulerName = kingdom.Leader == null ? "<none>" : kingdom.Leader.Name.ToString(),
                EnemyId = selected.DefenderId,
                EnemyName = enemy == null || enemy.Name == null ? selected.DefenderId : enemy.Name.ToString(),
                Reason = selected.Reason.ToString(),
                Objective = selected.Objective.ToString(),
                Target = FindSettlement(selected.TargetSettlementId),
                Progress = selected.Progress
            };
            return true;
        }

        private static void FinalizePendingSieges(bool force)
        {
            if (ActiveSieges.Count == 0)
                return;

            double nowHours = CampaignTime.Now.ToHours;
            var ids = new List<string>(ActiveSieges.Keys);
            for (int i = 0; i < ids.Count; i++)
            {
                SiegeTrack track;
                if (!ActiveSieges.TryGetValue(ids[i], out track) ||
                    track.EndedAt <= 0.0)
                    continue;

                if (!force && (nowHours - track.EndedAt) < 1.0)
                    continue;

                Settlement settlement = FindSettlement(track.SettlementId);
                if (settlement == null)
                {
                    ActiveSieges.Remove(ids[i]);
                    continue;
                }

                FinalizePendingSiege(
                    settlement,
                    false,
                    force ? "daily-survived-finalize" : "hourly-survived-finalize");
            }
        }

        private static void FinalizePendingSiege(
            Settlement settlement,
            bool captured,
            string source)
        {
            if (settlement == null || string.IsNullOrEmpty(settlement.StringId))
                return;

            SiegeTrack track;
            if (!ActiveSieges.TryGetValue(settlement.StringId, out track))
                return;

            ActiveSieges.Remove(settlement.StringId);

            double endHours = track.EndedAt > 0.0
                ? track.EndedAt
                : CampaignTime.Now.ToHours;
            double durationDays = Math.Max(0.0, endHours - track.StartedAt) / 24.0;

            if (!captured && durationDays < 0.5)
            {
                ClanAIPostVanilla.WriteExternalLog(
                    "WAR_SCAR_SKIPPED settlement=" + settlement.Name +
                    " cause=short-survived-siege durationDays=" + F(durationDays) +
                    " source=" + source +
                    " mutation=False");
                return;
            }

            float durationNorm = Clamp01((float)(durationDays / 14.0));
            if (captured)
            {
                AddScar(
                    settlement,
                    ScarCause.SIEGE_CAPTURED,
                    0.08f + 0.12f * durationNorm,
                    0.35f + 0.55f * durationNorm,
                    1f / 120f,
                    durationDays,
                    source);
            }
            else
            {
                AddScar(
                    settlement,
                    ScarCause.SIEGE_SURVIVED,
                    0.03f + 0.07f * durationNorm,
                    0.15f + 0.30f * durationNorm,
                    1f / 60f,
                    durationDays,
                    source);
            }
        }

        private static void AddScar(
            Settlement settlement,
            ScarCause cause,
            float prosperityPenalty,
            float loyaltyPenalty,
            float decayRate,
            double durationDays,
            string source)
        {
            WarScar scar = new WarScar
            {
                SettlementId = settlement.StringId,
                ProsperityPenalty = Clamp01(prosperityPenalty),
                LoyaltyPenalty = Math.Max(0f, loyaltyPenalty),
                AppliedAt = CampaignTime.Now.ToHours,
                DecayRate = Math.Max(0.0001f, decayRate),
                Cause = cause
            };
            Scars.Add(scar);
            TrimSettlementScars(settlement.StringId);

            ClanAIPostVanilla.WriteExternalLog(
                "WAR_SCAR_APPLIED settlement=" + settlement.Name +
                " settlementId=" + settlement.StringId +
                " cause=" + cause +
                " prosperityPenalty=" + scar.ProsperityPenalty.ToString("0.###", CultureInfo.InvariantCulture) +
                " loyaltyPenalty=" + scar.LoyaltyPenalty.ToString("0.###", CultureInfo.InvariantCulture) +
                " decayPerDay=" + scar.DecayRate.ToString("0.#####", CultureInfo.InvariantCulture) +
                " durationDays=" + F(durationDays) +
                " source=" + source +
                " mutation=False");
        }

        private static void ApplyOrReinforceRaidScar(Settlement bound, int burstCount)
        {
            double nowHours = CampaignTime.Now.ToHours;
            WarScar existing = null;
            for (int i = Scars.Count - 1; i >= 0; i--)
            {
                WarScar scar = Scars[i];
                if (scar.Cause != ScarCause.SUSTAINED_RAIDING ||
                    !string.Equals(scar.SettlementId, bound.StringId, StringComparison.Ordinal))
                    continue;
                if ((nowHours - scar.AppliedAt) <= RaidWindowHours)
                {
                    existing = scar;
                    break;
                }
            }

            float intensity = Clamp01((burstCount - 1) / 3f);
            float p = 0.02f + 0.06f * intensity;
            float l = 0.10f + 0.25f * intensity;

            if (existing == null)
            {
                AddScar(
                    bound,
                    ScarCause.SUSTAINED_RAIDING,
                    p,
                    l,
                    1f / 75f,
                    0.0,
                    "raid-burst");
                return;
            }

            existing.ProsperityPenalty =
                Math.Min(0.08f, Math.Max(existing.ProsperityPenalty, p));
            existing.LoyaltyPenalty =
                Math.Min(0.35f, Math.Max(existing.LoyaltyPenalty, l));
            existing.AppliedAt = nowHours;

            ClanAIPostVanilla.WriteExternalLog(
                "WAR_SCAR_REINFORCED settlement=" + bound.Name +
                " cause=" + existing.Cause +
                " trailingRaids=" + burstCount +
                " prosperityPenalty=" + existing.ProsperityPenalty.ToString("0.###", CultureInfo.InvariantCulture) +
                " loyaltyPenalty=" + existing.LoyaltyPenalty.ToString("0.###", CultureInfo.InvariantCulture) +
                " mutation=False");
        }

        private static void EnsureActiveWarObjectives(bool sessionStart)
        {
            for (int i = 0; i < Kingdom.All.Count; i++)
            {
                Kingdom a = Kingdom.All[i];
                if (a == null || a.IsEliminated)
                    continue;

                for (int j = i + 1; j < Kingdom.All.Count; j++)
                {
                    Kingdom b = Kingdom.All[j];
                    if (b == null || b.IsEliminated || !a.IsAtWarWith(b))
                        continue;

                    string key = PairKey(a, b);
                    if (Objectives.ContainsKey(key))
                        continue;

                    Kingdom aggressor;
                    Kingdom defender;
                    ChooseLoadedAggressor(a, b, out aggressor, out defender);
                    CreateOrReplaceObjective(
                        aggressor,
                        defender,
                        DeclareWarAction.DeclareWarDetail.Default,
                        true);
                }
            }

            if (sessionStart)
            {
                ClanAIPostVanilla.WriteExternalLog(
                    "WAR_OBJECTIVE_ACTIVE_SCAN count=" + Objectives.Count +
                    " mutation=False");
            }
        }

        private static void CreateOrReplaceObjective(
            Kingdom aggressor,
            Kingdom defender,
            DeclareWarAction.DeclareWarDetail detail,
            bool inferredOnLoad)
        {
            if (aggressor == null || defender == null)
                return;

            WarReason reason;
            ObjectiveType objectiveType;
            Settlement target;
            DetermineReasonAndObjective(
                aggressor,
                defender,
                detail,
                out reason,
                out objectiveType,
                out target);

            WarObjective objective = new WarObjective
            {
                PairKey = PairKey(aggressor, defender),
                AggressorId = aggressor.StringId,
                DefenderId = defender.StringId,
                Reason = reason,
                Objective = objectiveType,
                TargetSettlementId = target == null ? null : target.StringId,
                DeclarationDetail = detail.ToString(),
                CreatedAt = CampaignTime.Now.ToHours,
                Progress = 0f,
                BaselineEnemyStrength = Math.Max(1f, defender.CurrentTotalStrength),
                BaselineEnemyEconomy = Math.Max(1f, WeightedEconomy(defender)),
                Status = "Active"
            };
            Objectives[objective.PairKey] = objective;

            ClanAIPostVanilla.WriteExternalLog(
                "WAR_OBJECTIVE_ASSIGNED pair=" + objective.PairKey +
                " aggressor=" + aggressor.Name +
                " ruler=" + (aggressor.Leader == null ? "<none>" : aggressor.Leader.Name.ToString()) +
                " defender=" + defender.Name +
                " reason=" + reason +
                " objective=" + objectiveType +
                " target=" + (target == null ? "<none>" : target.Name.ToString()) +
                " detail=" + detail +
                " inferredOnLoad=" + inferredOnLoad +
                " baselineEnemyStrength=" + objective.BaselineEnemyStrength.ToString("0.###", CultureInfo.InvariantCulture) +
                " baselineEnemyEconomy=" + objective.BaselineEnemyEconomy.ToString("0.###", CultureInfo.InvariantCulture) +
                " mutation=False");
        }

        private static void DetermineReasonAndObjective(
            Kingdom aggressor,
            Kingdom defender,
            DeclareWarAction.DeclareWarDetail detail,
            out WarReason reason,
            out ObjectiveType objective,
            out Settlement target)
        {
            target = null;

            if (detail == DeclareWarAction.DeclareWarDetail.CausedByClaimOnThrone)
            {
                reason = WarReason.SuccessionClaim;
                objective = ObjectiveType.BreakEnemyFieldPower;
                return;
            }

            LossRecord reclaim = MostRecentLossToEnemy(aggressor, defender);
            if (reclaim != null)
            {
                target = FindSettlement(reclaim.SettlementId);
                if (target != null && SameFaction(target.MapFaction, defender))
                {
                    reason = WarReason.ReclaimLostHolding;
                    objective = ObjectiveType.CaptureSpecificSettlement;
                    return;
                }
            }

            Settlement raidTarget;
            int raidCount = CountEnemyRaidsAgainst(aggressor, defender, out raidTarget);
            if (raidCount >= 2)
            {
                reason = WarReason.RetaliationForRaids;
                target = raidTarget;
                objective = raidTarget == null
                    ? ObjectiveType.PunishEnemyEconomy
                    : ObjectiveType.CaptureSpecificSettlement;
                return;
            }

            reason = WarReason.BorderSecurity;
            objective = ObjectiveType.CaptureSpecificSettlement;
            target = FindBorderTarget(aggressor, defender);
            if (target == null)
                objective = ObjectiveType.BreakEnemyFieldPower;
        }

        private static void UpdateObjectiveProgress()
        {
            var keys = new List<string>(Objectives.Keys);
            for (int i = 0; i < keys.Count; i++)
            {
                WarObjective objective = Objectives[keys[i]];
                Kingdom aggressor = FindKingdom(objective.AggressorId);
                Kingdom defender = FindKingdom(objective.DefenderId);
                if (aggressor == null || defender == null ||
                    !aggressor.IsAtWarWith(defender))
                    continue;

                float before = objective.Progress;
                float progress = 0f;

                if (objective.Objective == ObjectiveType.CaptureSpecificSettlement)
                {
                    Settlement target = FindSettlement(objective.TargetSettlementId);
                    progress = target != null && SameFaction(target.MapFaction, aggressor)
                        ? 1f
                        : 0f;
                }
                else if (objective.Objective == ObjectiveType.BreakEnemyFieldPower)
                {
                    float current = Math.Max(0f, defender.CurrentTotalStrength);
                    float denominator = Math.Max(1f, objective.BaselineEnemyStrength * 0.40f);
                    progress = Clamp01(
                        (objective.BaselineEnemyStrength - current) / denominator);
                }
                else if (objective.Objective == ObjectiveType.PunishEnemyEconomy)
                {
                    float current = Math.Max(0f, WeightedEconomy(defender));
                    float denominator = Math.Max(1f, objective.BaselineEnemyEconomy * 0.15f);
                    progress = Clamp01(
                        (objective.BaselineEnemyEconomy - current) / denominator);
                }

                objective.Progress = progress;
                if (progress >= 0.999f &&
                    !string.Equals(objective.Status, "Achieved", StringComparison.Ordinal))
                {
                    objective.Status = "Achieved";
                    ClanAIPostVanilla.WriteExternalLog(
                        "WAR_OBJECTIVE_ACHIEVED pair=" + objective.PairKey +
                        " aggressor=" + aggressor.Name +
                        " defender=" + defender.Name +
                        " reason=" + objective.Reason +
                        " objective=" + objective.Objective +
                        " target=" + SettlementName(objective.TargetSettlementId) +
                        " mutation=False");
                }
                else if (Math.Abs(progress - before) >= 0.05f)
                {
                    ClanAIPostVanilla.WriteExternalLog(
                        "WAR_OBJECTIVE_PROGRESS pair=" + objective.PairKey +
                        " aggressor=" + aggressor.Name +
                        " defender=" + defender.Name +
                        " objective=" + objective.Objective +
                        " before=" + before.ToString("0.###", CultureInfo.InvariantCulture) +
                        " after=" + progress.ToString("0.###", CultureInfo.InvariantCulture) +
                        " mutation=False");
                }
            }
        }

        private static void RecomputeAllStrain(string source)
        {
            for (int i = 0; i < Kingdom.All.Count; i++)
            {
                Kingdom kingdom = Kingdom.All[i];
                if (kingdom == null || kingdom.IsEliminated)
                    continue;
                RecomputeStrain(kingdom, source);
            }
        }

        private static void RecomputeStrain(Kingdom kingdom, string source)
        {
            int activeWars = 0;
            for (int i = 0; i < kingdom.FactionsAtWarWith.Count; i++)
            {
                Kingdom enemy = kingdom.FactionsAtWarWith[i] as Kingdom;
                if (enemy != null && !enemy.IsEliminated)
                    activeWars++;
            }

            int besieged = 0;
            float weightedScar = 0f;
            float totalWeight = 0f;

            for (int i = 0; i < kingdom.Settlements.Count; i++)
            {
                Settlement settlement = kingdom.Settlements[i];
                if (settlement == null || settlement.IsVillage)
                    continue;

                float weight = settlement.IsTown ? 1f : 0.65f;
                totalWeight += weight;
                if (settlement.IsUnderSiege)
                    besieged++;

                float p;
                float l;
                ActiveScarPenalty(settlement.StringId, out p, out l);
                float normalizedLoyalty = Clamp01(l / 1.5f);
                weightedScar += weight * Clamp01(p + 0.25f * normalizedLoyalty);
            }

            float scarLoad = totalWeight <= 0f
                ? 0f
                : Clamp01(weightedScar / totalWeight);
            float strain = Clamp01(
                activeWars * W1 +
                besieged * W2 +
                scarLoad * W3);

            StrainState previous;
            bool hadPrevious =
                StrainByKingdom.TryGetValue(kingdom.StringId, out previous);

            StrainState state = new StrainState
            {
                ActiveWars = activeWars,
                BesiegedSettlements = besieged,
                ScarLoad = scarLoad,
                WarStrain = strain,
                CalculatedAt = CampaignTime.Now.ToHours
            };
            StrainByKingdom[kingdom.StringId] = state;

            if (!hadPrevious ||
                Math.Abs(previous.WarStrain - strain) >= 0.01f ||
                !string.Equals(source, "on-demand", StringComparison.Ordinal))
            {
                ClanAIPostVanilla.WriteExternalLog(
                    "WAR_STRAIN faction=" + kingdom.Name +
                    " activeWars=" + activeWars +
                    " besiegedSettlements=" + besieged +
                    " scarLoad=" + scarLoad.ToString("0.###", CultureInfo.InvariantCulture) +
                    " warStrain=" + strain.ToString("0.###", CultureInfo.InvariantCulture) +
                    " source=" + source +
                    " mutation=False");
            }
        }

        private static void LogCurrentScars()
        {
            double now = CampaignTime.Now.ToHours;
            for (int i = 0; i < Scars.Count; i++)
            {
                WarScar scar = Scars[i];
                float remaining = ScarRemaining(scar, now);
                Settlement settlement = FindSettlement(scar.SettlementId);
                ClanAIPostVanilla.WriteExternalLog(
                    "WAR_SCAR_STATE settlement=" +
                    (settlement == null ? scar.SettlementId : settlement.Name.ToString()) +
                    " settlementId=" + scar.SettlementId +
                    " cause=" + scar.Cause +
                    " remaining=" + remaining.ToString("0.###", CultureInfo.InvariantCulture) +
                    " activeProsperityPenalty=" +
                    (scar.ProsperityPenalty * remaining).ToString("0.###", CultureInfo.InvariantCulture) +
                    " activeLoyaltyPenalty=" +
                    (scar.LoyaltyPenalty * remaining).ToString("0.###", CultureInfo.InvariantCulture) +
                    " ageDays=" + F(Math.Max(0.0, now - scar.AppliedAt) / 24.0) +
                    " mutation=False");
            }
        }

        private static void ActiveScarPenalty(
            string settlementId,
            out float prosperityPenalty,
            out float loyaltyPenalty)
        {
            prosperityPenalty = 0f;
            loyaltyPenalty = 0f;
            double now = CampaignTime.Now.ToHours;

            for (int i = 0; i < Scars.Count; i++)
            {
                WarScar scar = Scars[i];
                if (!string.Equals(
                        scar.SettlementId,
                        settlementId,
                        StringComparison.Ordinal))
                    continue;

                float remaining = ScarRemaining(scar, now);
                prosperityPenalty += scar.ProsperityPenalty * remaining;
                loyaltyPenalty += scar.LoyaltyPenalty * remaining;
            }

            prosperityPenalty = Math.Min(0.35f, prosperityPenalty);
            loyaltyPenalty = Math.Min(1.5f, loyaltyPenalty);
        }

        private static float ScarRemaining(WarScar scar, double nowHours)
        {
            double ageDays = Math.Max(0.0, nowHours - scar.AppliedAt) / 24.0;
            return Clamp01(1f - scar.DecayRate * (float)ageDays);
        }

        private static void PruneExpiredScars()
        {
            double now = CampaignTime.Now.ToHours;
            for (int i = Scars.Count - 1; i >= 0; i--)
            {
                if (ScarRemaining(Scars[i], now) <= 0f)
                    Scars.RemoveAt(i);
            }
        }

        private static void TrimSettlementScars(string settlementId)
        {
            int count = 0;
            for (int i = 0; i < Scars.Count; i++)
            {
                if (string.Equals(Scars[i].SettlementId, settlementId, StringComparison.Ordinal))
                    count++;
            }

            while (count > 8)
            {
                int oldestIndex = -1;
                double oldest = double.MaxValue;
                for (int i = 0; i < Scars.Count; i++)
                {
                    WarScar scar = Scars[i];
                    if (!string.Equals(scar.SettlementId, settlementId, StringComparison.Ordinal))
                        continue;
                    if (scar.AppliedAt < oldest)
                    {
                        oldest = scar.AppliedAt;
                        oldestIndex = i;
                    }
                }
                if (oldestIndex < 0)
                    break;
                Scars.RemoveAt(oldestIndex);
                count--;
            }
        }

        private static void PruneHistories()
        {
            double now = CampaignTime.Now.ToHours;
            for (int i = Raids.Count - 1; i >= 0; i--)
            {
                if ((now - Raids[i].CompletedAt) > RaidWindowHours)
                    Raids.RemoveAt(i);
            }
            for (int i = Losses.Count - 1; i >= 0; i--)
            {
                if ((now - Losses[i].LostAt) > LostHoldingWindowHours)
                    Losses.RemoveAt(i);
            }
        }

        private static int CountRecentRaids(
            string boundSettlementId,
            string defenderFactionId,
            double now)
        {
            int count = 0;
            for (int i = 0; i < Raids.Count; i++)
            {
                RaidRecord r = Raids[i];
                if ((now - r.CompletedAt) > RaidWindowHours)
                    continue;
                if (!string.Equals(r.BoundSettlementId, boundSettlementId, StringComparison.Ordinal))
                    continue;
                if (!string.Equals(r.DefenderFactionId, defenderFactionId, StringComparison.Ordinal))
                    continue;
                count++;
            }
            return count;
        }

        private static LossRecord MostRecentLossToEnemy(
            Kingdom aggressor,
            Kingdom defender)
        {
            LossRecord best = null;
            double bestTime = double.MinValue;
            for (int i = 0; i < Losses.Count; i++)
            {
                LossRecord r = Losses[i];
                if (!string.Equals(r.OldFactionId, aggressor.StringId, StringComparison.Ordinal) ||
                    !string.Equals(r.NewFactionId, defender.StringId, StringComparison.Ordinal))
                    continue;
                if (r.LostAt > bestTime)
                {
                    best = r;
                    bestTime = r.LostAt;
                }
            }
            return best;
        }

        private static int CountEnemyRaidsAgainst(
            Kingdom defender,
            Kingdom enemy,
            out Settlement strongestBound)
        {
            strongestBound = null;
            var byBound = new Dictionary<string, int>(StringComparer.Ordinal);
            int total = 0;
            int bestCount = 0;
            string bestId = null;

            for (int i = 0; i < Raids.Count; i++)
            {
                RaidRecord r = Raids[i];
                if (!string.Equals(r.DefenderFactionId, defender.StringId, StringComparison.Ordinal) ||
                    !string.Equals(r.AttackerFactionId, enemy.StringId, StringComparison.Ordinal))
                    continue;

                total++;
                int c;
                byBound.TryGetValue(r.BoundSettlementId, out c);
                c++;
                byBound[r.BoundSettlementId] = c;
                if (c > bestCount)
                {
                    bestCount = c;
                    bestId = r.BoundSettlementId;
                }
            }

            strongestBound = FindSettlement(bestId);
            return total;
        }

        private static void ChooseLoadedAggressor(
            Kingdom a,
            Kingdom b,
            out Kingdom aggressor,
            out Kingdom defender)
        {
            int scoreA = RecentPressureScore(a, b);
            int scoreB = RecentPressureScore(b, a);
            if (scoreA > scoreB)
            {
                aggressor = a;
                defender = b;
                return;
            }
            if (scoreB > scoreA)
            {
                aggressor = b;
                defender = a;
                return;
            }

            if (string.CompareOrdinal(a.StringId, b.StringId) <= 0)
            {
                aggressor = a;
                defender = b;
            }
            else
            {
                aggressor = b;
                defender = a;
            }
        }

        private static int RecentPressureScore(Kingdom side, Kingdom enemy)
        {
            int score = 0;
            for (int i = 0; i < Losses.Count; i++)
            {
                if (string.Equals(Losses[i].OldFactionId, side.StringId, StringComparison.Ordinal) &&
                    string.Equals(Losses[i].NewFactionId, enemy.StringId, StringComparison.Ordinal))
                    score += 3;
            }
            for (int i = 0; i < Raids.Count; i++)
            {
                if (string.Equals(Raids[i].DefenderFactionId, side.StringId, StringComparison.Ordinal) &&
                    string.Equals(Raids[i].AttackerFactionId, enemy.StringId, StringComparison.Ordinal))
                    score += 1;
            }
            return score;
        }

        private static Settlement FindBorderTarget(
            Kingdom aggressor,
            Kingdom defender)
        {
            Settlement best = null;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < defender.Settlements.Count; i++)
            {
                Settlement enemy = defender.Settlements[i];
                if (enemy == null || enemy.IsVillage)
                    continue;

                Vec2 enemyPos = enemy.GetPosition2D;
                for (int j = 0; j < aggressor.Settlements.Count; j++)
                {
                    Settlement own = aggressor.Settlements[j];
                    if (own == null || own.IsVillage)
                        continue;

                    Vec2 ownPos = own.GetPosition2D;
                    float dx = enemyPos.x - ownPos.x;
                    float dy = enemyPos.y - ownPos.y;
                    float d = dx * dx + dy * dy;
                    if (d < bestDistance)
                    {
                        bestDistance = d;
                        best = enemy;
                    }
                }
            }
            return best;
        }

        private static float WeightedEconomy(Kingdom kingdom)
        {
            if (kingdom == null)
                return 0f;

            float total = 0f;
            for (int i = 0; i < kingdom.Settlements.Count; i++)
            {
                Settlement settlement = kingdom.Settlements[i];
                if (settlement == null)
                    continue;

                if (settlement.Town != null)
                {
                    float weight = settlement.IsTown ? 1f : 0.65f;
                    total += Math.Max(0f, settlement.Town.Prosperity) * weight;
                }
                else if (settlement.Village != null)
                {
                    total += Math.Max(0f, settlement.Village.Hearth) * 0.15f;
                }
            }
            return total;
        }

        private static Kingdom FindKingdom(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;
            for (int i = 0; i < Kingdom.All.Count; i++)
            {
                Kingdom k = Kingdom.All[i];
                if (k != null && string.Equals(k.StringId, id, StringComparison.Ordinal))
                    return k;
            }
            return null;
        }

        private static Settlement FindSettlement(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;
            foreach (Settlement settlement in Settlement.All)
            {
                if (settlement != null &&
                    string.Equals(settlement.StringId, id, StringComparison.Ordinal))
                    return settlement;
            }
            return null;
        }

        private static string SettlementName(string id)
        {
            Settlement settlement = FindSettlement(id);
            return settlement == null ? "<none>" : settlement.Name.ToString();
        }

        private static bool SameFaction(IFaction a, IFaction b)
        {
            if (a == null || b == null)
                return false;
            if (ReferenceEquals(a, b))
                return true;
            return string.Equals(FactionId(a), FactionId(b), StringComparison.Ordinal);
        }

        private static string PairKey(IFaction a, IFaction b)
        {
            string x = FactionId(a) ?? "<null-a>";
            string y = FactionId(b) ?? "<null-b>";
            return string.CompareOrdinal(x, y) <= 0
                ? x + "|" + y
                : y + "|" + x;
        }

        private static string FactionId(IFaction faction)
        {
            if (faction == null)
                return null;
            if (!string.IsNullOrEmpty(faction.StringId))
                return faction.StringId;
            return faction.Name == null ? null : faction.Name.ToString();
        }

        private static string FactionName(IFaction faction)
        {
            return faction == null || faction.Name == null
                ? "<none>"
                : faction.Name.ToString();
        }

        private static float Clamp01(float value)
        {
            if (value < 0f) return 0f;
            if (value > 1f) return 1f;
            return value;
        }

        private static string F(double value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static List<string> ExportLines()
        {
            var lines = new List<string>();

            for (int i = 0; i < Scars.Count; i++)
            {
                WarScar s = Scars[i];
                lines.Add(
                    "SCAR|" + B64(s.SettlementId) + "|" +
                    s.ProsperityPenalty.ToString("R", CultureInfo.InvariantCulture) + "|" +
                    s.LoyaltyPenalty.ToString("R", CultureInfo.InvariantCulture) + "|" +
                    s.AppliedAt.ToString("R", CultureInfo.InvariantCulture) + "|" +
                    s.DecayRate.ToString("R", CultureInfo.InvariantCulture) + "|" +
                    ((int)s.Cause).ToString(CultureInfo.InvariantCulture));
            }

            foreach (var pair in ActiveSieges)
            {
                SiegeTrack s = pair.Value;
                lines.Add(
                    "SIEGE|" + B64(s.SettlementId) + "|" +
                    B64(s.StartOwnerClanId) + "|" +
                    B64(s.StartFactionId) + "|" +
                    s.StartedAt.ToString("R", CultureInfo.InvariantCulture) + "|" +
                    s.EndedAt.ToString("R", CultureInfo.InvariantCulture));
            }

            for (int i = 0; i < Raids.Count; i++)
            {
                RaidRecord r = Raids[i];
                lines.Add(
                    "RAID|" + B64(r.VillageId) + "|" +
                    B64(r.BoundSettlementId) + "|" +
                    B64(r.AttackerFactionId) + "|" +
                    B64(r.DefenderFactionId) + "|" +
                    r.CompletedAt.ToString("R", CultureInfo.InvariantCulture) + "|" +
                    r.RaidDamage.ToString("R", CultureInfo.InvariantCulture));
            }

            for (int i = 0; i < Losses.Count; i++)
            {
                LossRecord l = Losses[i];
                lines.Add(
                    "LOSS|" + B64(l.SettlementId) + "|" +
                    B64(l.OldFactionId) + "|" +
                    B64(l.NewFactionId) + "|" +
                    l.LostAt.ToString("R", CultureInfo.InvariantCulture));
            }

            foreach (var pair in Objectives)
            {
                WarObjective o = pair.Value;
                lines.Add(
                    "OBJ|" + B64(o.PairKey) + "|" +
                    B64(o.AggressorId) + "|" +
                    B64(o.DefenderId) + "|" +
                    ((int)o.Reason).ToString(CultureInfo.InvariantCulture) + "|" +
                    ((int)o.Objective).ToString(CultureInfo.InvariantCulture) + "|" +
                    B64(o.TargetSettlementId) + "|" +
                    B64(o.DeclarationDetail) + "|" +
                    o.CreatedAt.ToString("R", CultureInfo.InvariantCulture) + "|" +
                    o.Progress.ToString("R", CultureInfo.InvariantCulture) + "|" +
                    o.BaselineEnemyStrength.ToString("R", CultureInfo.InvariantCulture) + "|" +
                    o.BaselineEnemyEconomy.ToString("R", CultureInfo.InvariantCulture) + "|" +
                    B64(o.Status));
            }

            ClanAIPostVanilla.WriteExternalLog(
                "WAR_STATE_SAVE scars=" + Scars.Count +
                " activeSieges=" + ActiveSieges.Count +
                " raids=" + Raids.Count +
                " losses=" + Losses.Count +
                " objectives=" + Objectives.Count);
            return lines;
        }

        private static void ImportLines(List<string> lines)
        {
            Scars.Clear();
            ActiveSieges.Clear();
            Raids.Clear();
            Losses.Clear();
            Objectives.Clear();

            if (lines != null)
            {
                for (int i = 0; i < lines.Count; i++)
                {
                    string line = lines[i];
                    if (string.IsNullOrEmpty(line))
                        continue;
                    string[] p = line.Split('|');
                    if (p.Length < 2)
                        continue;

                    try
                    {
                        if (p[0] == "SCAR" && p.Length == 7)
                        {
                            float prosperity;
                            float loyalty;
                            double appliedAt;
                            float decay;
                            int cause;
                            if (!float.TryParse(p[2], NumberStyles.Float, CultureInfo.InvariantCulture, out prosperity) ||
                                !float.TryParse(p[3], NumberStyles.Float, CultureInfo.InvariantCulture, out loyalty) ||
                                !double.TryParse(p[4], NumberStyles.Float, CultureInfo.InvariantCulture, out appliedAt) ||
                                !float.TryParse(p[5], NumberStyles.Float, CultureInfo.InvariantCulture, out decay) ||
                                !int.TryParse(p[6], NumberStyles.Integer, CultureInfo.InvariantCulture, out cause))
                                continue;
                            Scars.Add(new WarScar
                            {
                                SettlementId = FromB64(p[1]),
                                ProsperityPenalty = prosperity,
                                LoyaltyPenalty = loyalty,
                                AppliedAt = appliedAt,
                                DecayRate = decay,
                                Cause = (ScarCause)cause
                            });
                        }
                        else if (p[0] == "SIEGE" && (p.Length == 5 || p.Length == 6))
                        {
                            double startedAt;
                            double endedAt = 0.0;
                            if (!double.TryParse(p[4], NumberStyles.Float, CultureInfo.InvariantCulture, out startedAt))
                                continue;
                            if (p.Length == 6 &&
                                !double.TryParse(p[5], NumberStyles.Float, CultureInfo.InvariantCulture, out endedAt))
                                endedAt = 0.0;
                            SiegeTrack track = new SiegeTrack
                            {
                                SettlementId = FromB64(p[1]),
                                StartOwnerClanId = FromB64(p[2]),
                                StartFactionId = FromB64(p[3]),
                                StartedAt = startedAt,
                                EndedAt = endedAt
                            };
                            if (!string.IsNullOrEmpty(track.SettlementId))
                                ActiveSieges[track.SettlementId] = track;
                        }
                        else if (p[0] == "RAID" && p.Length == 7)
                        {
                            double completedAt;
                            float raidDamage;
                            if (!double.TryParse(p[5], NumberStyles.Float, CultureInfo.InvariantCulture, out completedAt) ||
                                !float.TryParse(p[6], NumberStyles.Float, CultureInfo.InvariantCulture, out raidDamage))
                                continue;
                            Raids.Add(new RaidRecord
                            {
                                VillageId = FromB64(p[1]),
                                BoundSettlementId = FromB64(p[2]),
                                AttackerFactionId = FromB64(p[3]),
                                DefenderFactionId = FromB64(p[4]),
                                CompletedAt = completedAt,
                                RaidDamage = raidDamage
                            });
                        }
                        else if (p[0] == "LOSS" && p.Length == 5)
                        {
                            double lostAt;
                            if (!double.TryParse(p[4], NumberStyles.Float, CultureInfo.InvariantCulture, out lostAt))
                                continue;
                            Losses.Add(new LossRecord
                            {
                                SettlementId = FromB64(p[1]),
                                OldFactionId = FromB64(p[2]),
                                NewFactionId = FromB64(p[3]),
                                LostAt = lostAt
                            });
                        }
                        else if (p[0] == "OBJ" && p.Length == 13)
                        {
                            int reason;
                            int objectiveType;
                            double createdAt;
                            float progress;
                            float baselineStrength;
                            float baselineEconomy;
                            if (!int.TryParse(p[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out reason) ||
                                !int.TryParse(p[5], NumberStyles.Integer, CultureInfo.InvariantCulture, out objectiveType) ||
                                !double.TryParse(p[8], NumberStyles.Float, CultureInfo.InvariantCulture, out createdAt) ||
                                !float.TryParse(p[9], NumberStyles.Float, CultureInfo.InvariantCulture, out progress) ||
                                !float.TryParse(p[10], NumberStyles.Float, CultureInfo.InvariantCulture, out baselineStrength) ||
                                !float.TryParse(p[11], NumberStyles.Float, CultureInfo.InvariantCulture, out baselineEconomy))
                                continue;

                            WarObjective o = new WarObjective
                            {
                                PairKey = FromB64(p[1]),
                                AggressorId = FromB64(p[2]),
                                DefenderId = FromB64(p[3]),
                                Reason = (WarReason)reason,
                                Objective = (ObjectiveType)objectiveType,
                                TargetSettlementId = FromB64(p[6]),
                                DeclarationDetail = FromB64(p[7]),
                                CreatedAt = createdAt,
                                Progress = progress,
                                BaselineEnemyStrength = baselineStrength,
                                BaselineEnemyEconomy = baselineEconomy,
                                Status = FromB64(p[12])
                            };
                            if (!string.IsNullOrEmpty(o.PairKey))
                                Objectives[o.PairKey] = o;
                        }
                    }
                    catch
                    {
                    }
                }
            }

            _loadedFromSave = true;
            ClanAIPostVanilla.WriteExternalLog(
                "WAR_STATE_LOAD scars=" + Scars.Count +
                " activeSieges=" + ActiveSieges.Count +
                " raids=" + Raids.Count +
                " losses=" + Losses.Count +
                " objectives=" + Objectives.Count);
        }

        private static string B64(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
        }

        private static string FromB64(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";
            try
            {
                return Encoding.UTF8.GetString(Convert.FromBase64String(value));
            }
            catch
            {
                return "";
            }
        }
    }
}
