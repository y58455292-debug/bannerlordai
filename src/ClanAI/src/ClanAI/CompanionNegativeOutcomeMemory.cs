using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace ClanAI
{
    internal static class CompanionNegativeOutcomeMemory
    {
        private const double MemoryLifetimeHours = 336.0;
        private const double MaxObservationGapHours = 12.5;
        private const double ReinforceCooldownHours = 12.0;
        private const float SeriousReadinessDrop = 0.12f;
        private const float SevereReadinessDrop = 0.20f;
        private const int MinimumMenLost = 8;

        private sealed class OutcomeRecord
        {
            internal string HeroKey;
            internal string ActorName;
            internal string Kind;
            internal string Context;
            internal AiBehavior PriorBehavior;
            internal double LastOutcomeHours;
            internal int Count;
            internal float Severity;
            internal float ReadinessBefore;
            internal float ReadinessAfter;
            internal int MenBefore;
            internal int MenAfter;
        }
        private sealed class Observation
        {
            internal double CampaignHours;
            internal float Readiness;
            internal int Men;
            internal AiBehavior Behavior;
            internal string Target;
        }

        private static readonly Dictionary<string, OutcomeRecord> Records =
            new Dictionary<string, OutcomeRecord>(StringComparer.Ordinal);
        private static readonly Dictionary<string, Observation> LastByHero =
            new Dictionary<string, Observation>(StringComparer.Ordinal);

        private static bool _loadedFromSave;
        private static long _observations;
        private static long _outcomesRecorded;
        private static long _captureOutcomes;
        private static long _seriousLossOutcomes;

        internal static void BeginSession()
        {
            if (!_loadedFromSave)
                Records.Clear();

            LastByHero.Clear();
            _observations = 0;
            _outcomesRecorded = 0;
            _captureOutcomes = 0;
            _seriousLossOutcomes = 0;

            ClanAIPostVanilla.WriteExternalLog(                "NEGATIVE_OUTCOME_MEMORY_SESSION_READY records=" +
                Records.Count +
                " restored=" + _loadedFromSave +
                " lifetimeHours=" + MemoryLifetimeHours);

            _loadedFromSave = false;
        }

        internal static void SyncData(IDataStore dataStore)
        {
            List<string> lines = new List<string>();
            if (dataStore.IsSaving)
                lines = ExportSaveLines();

            bool found = dataStore.SyncData(
                "ClanAI_CompanionNegativeOutcomeMemory_v1",
                ref lines);

            if (dataStore.IsLoading)
                ImportSaveLines(found ? lines : null);
        }

        private static List<string> ExportSaveLines()
        {
            var lines = new List<string>();
            foreach (var pair in Records)
            {
                OutcomeRecord r = pair.Value;
                lines.Add(
                    B64(r.HeroKey) + "|" +
                    B64(r.ActorName) + "|" +
                    B64(r.Kind) + "|" +
                    B64(r.Context) + "|" +                    ((int)r.PriorBehavior).ToString(CultureInfo.InvariantCulture) + "|" +
                    r.LastOutcomeHours.ToString("R", CultureInfo.InvariantCulture) + "|" +
                    r.Count.ToString(CultureInfo.InvariantCulture) + "|" +
                    r.Severity.ToString("R", CultureInfo.InvariantCulture) + "|" +
                    r.ReadinessBefore.ToString("R", CultureInfo.InvariantCulture) + "|" +
                    r.ReadinessAfter.ToString("R", CultureInfo.InvariantCulture) + "|" +
                    r.MenBefore.ToString(CultureInfo.InvariantCulture) + "|" +
                    r.MenAfter.ToString(CultureInfo.InvariantCulture));
            }

            ClanAIPostVanilla.WriteExternalLog(
                "NEGATIVE_OUTCOME_MEMORY_SAVE records=" + lines.Count);
            return lines;
        }

        private static void ImportSaveLines(List<string> lines)
        {
            Records.Clear();
            if (lines != null)
            {
                foreach (string line in lines)
                {
                    if (string.IsNullOrEmpty(line))
                        continue;

                    string[] p = line.Split('|');
                    if (p.Length != 12)
                        continue;

                    int behaviorValue;
                    double hours;
                    int count;
                    float severity;                    float before;
                    float after;
                    int menBefore;
                    int menAfter;
                    if (!int.TryParse(p[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out behaviorValue) ||
                        !double.TryParse(p[5], NumberStyles.Float, CultureInfo.InvariantCulture, out hours) ||
                        !int.TryParse(p[6], NumberStyles.Integer, CultureInfo.InvariantCulture, out count) ||
                        !float.TryParse(p[7], NumberStyles.Float, CultureInfo.InvariantCulture, out severity) ||
                        !float.TryParse(p[8], NumberStyles.Float, CultureInfo.InvariantCulture, out before) ||
                        !float.TryParse(p[9], NumberStyles.Float, CultureInfo.InvariantCulture, out after) ||
                        !int.TryParse(p[10], NumberStyles.Integer, CultureInfo.InvariantCulture, out menBefore) ||
                        !int.TryParse(p[11], NumberStyles.Integer, CultureInfo.InvariantCulture, out menAfter))
                        continue;

                    string heroKey = FromB64(p[0]);
                    if (string.IsNullOrEmpty(heroKey))
                        continue;

                    Records[heroKey] = new OutcomeRecord
                    {
                        HeroKey = heroKey,
                        ActorName = FromB64(p[1]),
                        Kind = FromB64(p[2]),
                        Context = FromB64(p[3]),
                        PriorBehavior = (AiBehavior)behaviorValue,
                        LastOutcomeHours = hours,
                        Count = count,
                        Severity = severity,
                        ReadinessBefore = before,
                        ReadinessAfter = after,
                        MenBefore = menBefore,
                        MenAfter = menAfter
                    };                }
            }

            _loadedFromSave = true;
            ClanAIPostVanilla.WriteExternalLog(
                "NEGATIVE_OUTCOME_MEMORY_LOAD records=" + Records.Count);
        }

        internal static void Observe(MobileParty actor)
        {
            if (!Eligible(actor))
                return;

            string heroKey = HeroKey(actor.LeaderHero);
            if (string.IsNullOrEmpty(heroKey))
                return;

            double nowHours = CampaignTime.Now.ToHours;
            float readiness = actor.PartySizeRatio;
            int men = CurrentMen(actor);
            AiBehavior behavior = CurrentCommittedBehavior(actor);
            string target = CurrentTargetLabel(actor);

            Observation previous;
            if (LastByHero.TryGetValue(heroKey, out previous))
            {
                double gap = nowHours - previous.CampaignHours;
                if (gap > 0.0 && gap <= MaxObservationGapHours)
                    DetectSeriousLoss(actor, previous, readiness, men, nowHours);
            }

            LastByHero[heroKey] = new Observation
            {
                CampaignHours = nowHours,
                Readiness = readiness,
                Men = men,
                Behavior = behavior,                Target = target
            };
            _observations++;
        }

        private static void DetectSeriousLoss(
            MobileParty actor,
            Observation previous,
            float readiness,
            int men,
            double nowHours)
        {
            if (!IsRecklessExposure(previous.Behavior))
                return;

            float drop = previous.Readiness - readiness;
            int menLost = previous.Men - men;
            bool serious =
                drop >= SevereReadinessDrop ||
                (drop >= SeriousReadinessDrop &&
                 (menLost >= MinimumMenLost || readiness < 0.65f));

            if (!serious)
                return;

            string context =
                previous.Behavior + ":" +
                (string.IsNullOrEmpty(previous.Target) ? "<none>" : previous.Target) +
                " readiness=" + F(previous.Readiness) + "->" + F(readiness) +
                " men=" + previous.Men + "->" + men;

            RecordOutcome(
                actor.LeaderHero,
                "serious-loss-after-exposure",
                context,
                previous.Behavior,
                nowHours,
                Math.Max(0f, drop),
                previous.Readiness,
                readiness,
                previous.Men,
                men);
            _seriousLossOutcomes++;
        }
        internal static void RecordHomeLoss(
            Settlement settlement,
            Hero oldOwner,
            Hero newOwner,
            ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
        {
            if (settlement == null || oldOwner == null || oldOwner.Clan == null)
                return;

            double nowHours = CampaignTime.Now.ToHours;
            IFaction oldFaction = oldOwner.Clan.MapFaction;
            foreach (MobileParty party in MobileParty.AllLordParties)
            {
                if (!Eligible(party))
                    continue;
                if (oldFaction == null || party.MapFaction == null ||
                    (!ReferenceEquals(oldFaction, party.MapFaction) &&
                     !string.Equals(oldFaction.StringId, party.MapFaction.StringId, StringComparison.Ordinal)))
                    continue;

                string rememberedSettlement;
                string threatType;
                double threatAgeHours;
                int threatCount;
                if (!CompanionExperienceMemory.TryGetRecentThreatForSettlement(
                        party, settlement.StringId,
                        out rememberedSettlement, out threatType,
                        out threatAgeHours, out threatCount))
                    continue;

                bool directClanLoss = SameClan(oldOwner.Clan, party.ActualClan);
                float severity = Math.Min(0.50f, 0.30f + Math.Max(0, threatCount - 1) * 0.02f);
                AiBehavior priorBehavior = CurrentCommittedBehavior(party);
                string lossKind = directClanLoss
                    ? "home-loss-after-threat"
                    : "allied-holding-loss-after-threat";
                string context =
                    "lost=" + settlement.Name.ToString() +
                    " scope=" + (directClanLoss ? "clan" : "faction") +
                    " rememberedThreat=" + (threatType ?? "<none>") +
                    " threatAgeHours=" + threatAgeHours.ToString("0.###", CultureInfo.InvariantCulture) +
                    " threatCount=" + threatCount +
                    " detail=" + detail +
                    " oldOwner=" + (oldOwner == null ? "<none>" : oldOwner.Name.ToString()) +
                    " newOwner=" + (newOwner == null ? "<none>" : newOwner.Name.ToString());

                RecordOutcome(
                    party.LeaderHero,
                    lossKind,
                    context,
                    priorBehavior,
                    nowHours,
                    severity,
                    party.PartySizeRatio,
                    party.PartySizeRatio,
                    CurrentMen(party),
                    CurrentMen(party));
            }
        }

        internal static void RecordCapture(Hero prisoner, PartyBase capturer)
        {
            if (prisoner == null || prisoner.Clan == null ||
                prisoner == Hero.MainHero)
                return;

            string context = "captured-by=" + PartyName(capturer);
            AiBehavior priorBehavior = AiBehavior.None;
            string heroKey = HeroKey(prisoner);
            Observation previous = null;
            if (!string.IsNullOrEmpty(heroKey) &&
                LastByHero.TryGetValue(heroKey, out previous))
            {
                priorBehavior = previous.Behavior;
                context += " after=" + previous.Behavior + ":" +
                    (string.IsNullOrEmpty(previous.Target) ? "<none>" : previous.Target);
            }

            double nowHours = CampaignTime.Now.ToHours;
            RecordOutcome(
                prisoner,
                "capture",
                context,
                priorBehavior,
                nowHours,
                1.0f,
                previous == null ? 0f : previous.Readiness,
                0f,
                previous == null ? 0 : previous.Men,
                0);
            _captureOutcomes++;
        }

        private static void RecordOutcome(
            Hero hero,
            string kind,
            string context,
            AiBehavior priorBehavior,
            double nowHours,
            float severity,
            float readinessBefore,
            float readinessAfter,
            int menBefore,
            int menAfter)
        {
            string heroKey = HeroKey(hero);
            if (string.IsNullOrEmpty(heroKey))
                return;

            OutcomeRecord record;
            bool exists = Records.TryGetValue(heroKey, out record);
            if (!exists)
            {
                record = new OutcomeRecord
                {
                    HeroKey = heroKey,
                    ActorName = hero.Name.ToString(),
                    Kind = kind,
                    Context = context,
                    PriorBehavior = priorBehavior,
                    LastOutcomeHours = nowHours,
                    Count = 1,
                    Severity = severity,
                    ReadinessBefore = readinessBefore,
                    ReadinessAfter = readinessAfter,
                    MenBefore = menBefore,
                    MenAfter = menAfter
                };
                Records[heroKey] = record;
                _outcomesRecorded++;

                ClanAIPostVanilla.WriteExternalLog(
                    "NEGATIVE_OUTCOME_LEARNED actor=" + record.ActorName +
                    " kind=" + record.Kind +
                    " priorBehavior=" + record.PriorBehavior +
                    " severity=" + F(record.Severity) +
                    " context=" + Clean(record.Context) +
                    " count=" + record.Count);
                return;
            }
            bool sameKind = string.Equals(record.Kind, kind, StringComparison.Ordinal);
            if (sameKind && (nowHours - record.LastOutcomeHours) < ReinforceCooldownHours)
                return;

            record.ActorName = hero.Name.ToString();
            record.Kind = kind;
            record.Context = context;
            record.PriorBehavior = priorBehavior;
            record.LastOutcomeHours = nowHours;
            record.Count++;
            record.Severity = Math.Max(record.Severity, severity);
            record.ReadinessBefore = readinessBefore;
            record.ReadinessAfter = readinessAfter;
            record.MenBefore = menBefore;
            record.MenAfter = menAfter;
            _outcomesRecorded++;

            ClanAIPostVanilla.WriteExternalLog(
                "NEGATIVE_OUTCOME_REINFORCED actor=" + record.ActorName +
                " kind=" + record.Kind +
                " priorBehavior=" + record.PriorBehavior +
                " severity=" + F(record.Severity) +
                " context=" + Clean(record.Context) +
                " count=" + record.Count);
        }

        internal static bool TryGetRecentOutcome(
            MobileParty actor,
            out string kind,
            out string context,
            out double ageHours,
            out int count,
            out float severity,
            out AiBehavior priorBehavior)
        {
            kind = null;
            context = null;
            ageHours = double.MaxValue;
            count = 0;
            severity = 0f;
            priorBehavior = AiBehavior.None;
            if (actor == null || actor.LeaderHero == null)
                return false;

            string heroKey = HeroKey(actor.LeaderHero);
            OutcomeRecord record;
            if (string.IsNullOrEmpty(heroKey) || !Records.TryGetValue(heroKey, out record))
                return false;

            double nowHours = CampaignTime.Now.ToHours;
            double age = nowHours - record.LastOutcomeHours;
            if (age < 0.0)
                age = 0.0;
            if (age > MemoryLifetimeHours)
            {
                ClanAIPostVanilla.WriteExternalLog(
                    "NEGATIVE_OUTCOME_EXPIRED actor=" + record.ActorName +
                    " kind=" + record.Kind +
                    " ageHours=" + age.ToString("0.###", CultureInfo.InvariantCulture));
                Records.Remove(heroKey);
                return false;
            }

            kind = record.Kind;
            context = record.Context;
            ageHours = age;
            count = record.Count;
            severity = record.Severity;
            priorBehavior = record.PriorBehavior;
            return true;
        }

        private static AiBehavior CurrentCommittedBehavior(MobileParty actor)
        {
            AiBehavior shortBehavior = actor.ShortTermBehavior;
            if (IsRecklessExposure(shortBehavior))
                return shortBehavior;
            return actor.DefaultBehavior;
        }

        private static string CurrentTargetLabel(MobileParty actor)
        {
            var settlement = actor.TargetSettlement ?? actor.ShortTermTargetSettlement;
            if (settlement != null)
                return settlement.Name.ToString();
            if (actor.TargetParty != null)
                return actor.TargetParty.Name.ToString();
            return "<none>";
        }
        private static int CurrentMen(MobileParty actor)
        {
            try
            {
                return actor.MemberRoster == null
                    ? 0
                    : actor.MemberRoster.TotalManCount;
            }
            catch
            {
                return 0;
            }
        }

        private static bool IsRecklessExposure(AiBehavior behavior)
        {
            return behavior == AiBehavior.RaidSettlement ||
                   behavior == AiBehavior.BesiegeSettlement ||
                   behavior == AiBehavior.AssaultSettlement ||
                   behavior == AiBehavior.EngageParty ||
                   behavior == AiBehavior.PatrolAroundPoint;
        }

        private static bool Eligible(MobileParty actor)
        {
            if (actor == null || actor.LeaderHero == null || actor.IsMainParty ||
                actor.Army != null || actor.MapFaction == null)
                return false;

            return actor.ActualClan != null;
        }

        private static bool SameClan(Clan a, Clan b)
        {
            if (a == null || b == null)
                return false;
            return ReferenceEquals(a, b) ||
                string.Equals(a.StringId, b.StringId, StringComparison.Ordinal);
        }

        private static string HeroKey(Hero hero)
        {
            if (hero == null)
                return null;
            return string.IsNullOrEmpty(hero.StringId)
                ? hero.Name.ToString()
                : hero.StringId;
        }
        private static string PartyName(PartyBase party)
        {
            if (party == null)
                return "<none>";
            try
            {
                return party.Name == null ? "<none>" : party.Name.ToString();
            }
            catch
            {
                return "<none>";
            }
        }

        private static string F(float value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static string Clean(string value)
        {
            return (value ?? "<none>")
                .Replace("\r", " ")
                .Replace("\n", " ")
                .Replace("\t", " ");
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

        internal static string SummaryFields()
        {
            return " negativeOutcomeRecords=" + Records.Count +
                   " negativeOutcomeObservations=" + _observations +
                   " negativeOutcomeRecorded=" + _outcomesRecorded +
                   " negativeOutcomeCaptures=" + _captureOutcomes +
                   " negativeOutcomeSeriousLosses=" + _seriousLossOutcomes;
        }
    }
}


