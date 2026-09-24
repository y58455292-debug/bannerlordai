using System;
using System.Collections.Generic;
using System.Globalization;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace ClanAI
{
    public sealed class PrisonerMercyDecisionBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(
                this,
                new Action<CampaignGameStarter>(OnSessionLaunched));

            CampaignEvents.HeroPrisonerTaken.AddNonSerializedListener(
                this,
                new Action<PartyBase, Hero>(OnHeroPrisonerTaken));

            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(
                this,
                new Action(OnHourlyTick));
        }

        public override void SyncData(IDataStore dataStore)
        {
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            PrisonerMercyDecision.Reset();
        }

        private void OnHeroPrisonerTaken(PartyBase capturer, Hero prisoner)
        {
            PrisonerMercyDecision.QueueCapture(capturer, prisoner);
        }

        private void OnHourlyTick()
        {
            PrisonerMercyDecision.ProcessPending();
        }
    }

    internal static class PrisonerMercyDecision
    {
        private const float ReleaseThreshold = 20f;
        private const double MaxPendingAgeHours = 12.0;

        private sealed class PendingCapture
        {
            internal string PrisonerId;
            internal string PrisonerName;
            internal string CapturerPartyId;
            internal string CapturerHeroId;
            internal string CapturerName;
            internal double CapturedAtHours;
            internal string Source;
        }

        private static readonly Dictionary<string, PendingCapture> Pending =
            new Dictionary<string, PendingCapture>(StringComparer.Ordinal);

        private static long _queued;
        private static long _evaluated;
        private static long _releaseDecisions;
        private static long _keepDecisions;
        private static long _releaseCommits;

        internal static void Reset()
        {
            Pending.Clear();
            _queued = 0;
            _evaluated = 0;
            _releaseDecisions = 0;
            _keepDecisions = 0;
            _releaseCommits = 0;

            ClanAIPostVanilla.WriteExternalLog(
                "MERCY_DECISION_SESSION_READY" +
                " scope=autonomous-kingdom-lord-custody" +
                " releaseThreshold=" + F(ReleaseThreshold));

            SeedExistingPrisoners();
        }

        private static void SeedExistingPrisoners()
        {
            int mobileSeeded = 0;
            int settlementSeeded = 0;

            foreach (MobileParty party in MobileParty.AllLordParties)
            {
                if (party == null || !party.IsActive || !party.IsLordParty ||
                    party == MobileParty.MainParty || party.LeaderHero == null)
                    continue;

                foreach (var element in party.PrisonRoster.GetTroopRoster())
                {
                    if (element.Number <= 0 || element.Character == null ||
                        !element.Character.IsHero || element.Character.HeroObject == null)
                        continue;

                    int before = Pending.Count;
                    QueueCapture(
                        party.Party,
                        element.Character.HeroObject,
                        "session-seed-mobile");
                    if (Pending.Count > before)
                        mobileSeeded++;
                }
            }

            foreach (Settlement settlement in Settlement.All)
            {
                if (settlement == null ||
                    (!settlement.IsTown && !settlement.IsCastle) ||
                    settlement.Party == null)
                    continue;

                foreach (var element in settlement.Party.PrisonRoster.GetTroopRoster())
                {
                    if (element.Number <= 0 || element.Character == null ||
                        !element.Character.IsHero || element.Character.HeroObject == null)
                        continue;

                    int before = Pending.Count;
                    QueueCapture(
                        settlement.Party,
                        element.Character.HeroObject,
                        "session-seed-settlement");
                    if (Pending.Count > before)
                        settlementSeeded++;
                }
            }

            ClanAIPostVanilla.WriteExternalLog(
                "MERCY_DECISION_SEED_SCAN mobileSeeded=" + mobileSeeded +
                " settlementSeeded=" + settlementSeeded +
                " totalSeeded=" + (mobileSeeded + settlementSeeded) +
                " pending=" + Pending.Count);
        }

        internal static void QueueCapture(
            PartyBase capturer,
            Hero prisoner,
            string source = "capture-event")
        {
            if (capturer == null ||
                prisoner == null ||
                prisoner == Hero.MainHero ||
                prisoner.Clan == null ||
                prisoner.Clan.Kingdom == null)
                return;

            Hero capturerHero;
            if (!IsEligibleCustody(capturer, prisoner, out capturerHero))
                return;

            string prisonerId = HeroKey(prisoner);
            string partyId = CustodyId(capturer);

            if (string.IsNullOrEmpty(prisonerId) ||
                string.IsNullOrEmpty(partyId))
                return;

            Pending[prisonerId] = new PendingCapture
            {
                PrisonerId = prisonerId,
                PrisonerName = prisoner.Name.ToString(),
                CapturerPartyId = partyId,
                CapturerHeroId = HeroKey(capturerHero),
                CapturerName = capturerHero.Name.ToString(),
                CapturedAtHours = CampaignTime.Now.ToHours,
                Source = source ?? "<none>"
            };

            _queued++;

            ClanAIPostVanilla.WriteExternalLog(
                "MERCY_DECISION_QUEUED prisoner=" + prisoner.Name +
                " prisonerClan=" + prisoner.Clan.Name +
                " capturer=" + capturerHero.Name +
                " capturerClan=" + capturerHero.Clan.Name +
                " custodyId=" + partyId +
                " custodyType=" + CustodyType(capturer) +
                " source=" + (source ?? "<none>") +
                " queued=" + _queued);
        }

        internal static void ProcessPending()
        {
            if (Pending.Count == 0)
                return;

            var keys = new List<string>(Pending.Keys);
            for (int i = 0; i < keys.Count; i++)
            {
                PendingCapture pending;
                if (!Pending.TryGetValue(keys[i], out pending))
                    continue;

                Hero prisoner = FindHero(pending.PrisonerId);
                double ageHours = CampaignTime.Now.ToHours - pending.CapturedAtHours;
                if (ageHours < 0.0)
                    ageHours = 0.0;

                if (prisoner == null ||
                    !prisoner.IsPrisoner ||
                    ageHours > MaxPendingAgeHours)
                {
                    Pending.Remove(keys[i]);
                    ClanAIPostVanilla.WriteExternalLog(
                        "MERCY_DECISION_DROPPED prisoner=" + pending.PrisonerName +
                        " reason=" +
                        (prisoner == null
                            ? "hero-missing"
                            : !prisoner.IsPrisoner
                                ? "no-longer-prisoner"
                                : "pending-expired") +
                        " ageHours=" + F(ageHours));
                    continue;
                }

                PartyBase prisonParty = prisoner.PartyBelongedToAsPrisoner;
                string currentCustodyId = CustodyId(prisonParty);

                if (prisonParty == null ||
                    string.IsNullOrEmpty(currentCustodyId) ||
                    !string.Equals(
                        currentCustodyId,
                        pending.CapturerPartyId,
                        StringComparison.Ordinal))
                {
                    Pending.Remove(keys[i]);
                    ClanAIPostVanilla.WriteExternalLog(
                        "MERCY_DECISION_DROPPED prisoner=" + pending.PrisonerName +
                        " reason=left-original-custody" +
                        " expectedCustody=" + pending.CapturerPartyId +
                        " actualCustody=" + (currentCustodyId ?? "<none>") +
                        " ageHours=" + F(ageHours));
                    continue;
                }

                Hero capturer;
                if (!IsEligibleCustody(prisonParty, prisoner, out capturer))
                {
                    Pending.Remove(keys[i]);
                    continue;
                }

                EvaluateAndApply(
                    prisonParty,
                    capturer,
                    prisoner,
                    ageHours,
                    pending.Source);
                Pending.Remove(keys[i]);
            }
        }

        private static void EvaluateAndApply(
            PartyBase custodyParty,
            Hero capturer,
            Hero prisoner,
            double ageHours,
            string source)
        {
            int mercy = capturer.GetTraitLevel(DefaultTraits.Mercy);
            int honor = capturer.GetTraitLevel(DefaultTraits.Honor);
            int generosity = capturer.GetTraitLevel(DefaultTraits.Generosity);
            int calculating = capturer.GetTraitLevel(DefaultTraits.Calculating);
            int relation = capturer.GetRelation(prisoner);

            int trust = 0;
            int grievance = 0;
            int bloodDebt = 0;
            int obligation = 0;
            int tension = 0;

            SocialLedger.TryGetState(
                capturer,
                prisoner.Clan,
                out trust,
                out grievance,
                out bloodDebt,
                out obligation,
                out tension);

            float score = 0f;
            score += mercy * 22f;
            score += honor * 8f;
            score += generosity * 4f;
            score -= calculating * 4f;
            score += Math.Max(-50, Math.Min(50, relation)) * 0.30f;
            score += trust * 0.15f;
            score += obligation * 0.25f;
            score -= grievance * 0.18f;
            score -= bloodDebt * 0.25f;
            score -= tension * 0.08f;

            bool clanLeader = prisoner.Clan.Leader == prisoner;
            bool kingdomLeader =
                prisoner.Clan.Kingdom != null &&
                prisoner.Clan.Kingdom.Leader == prisoner;

            if (clanLeader)
                score -= 8f;
            if (kingdomLeader)
                score -= 18f;

            bool release = score >= ReleaseThreshold;
            _evaluated++;
            if (release)
                _releaseDecisions++;
            else
                _keepDecisions++;

            ClanAIPostVanilla.WriteExternalLog(
                "MERCY_DECISION actor=" + capturer.Name +
                " actorClan=" + capturer.Clan.Name +
                " custodyId=" + CustodyId(custodyParty) +
                " custodyType=" + CustodyType(custodyParty) +
                " prisoner=" + prisoner.Name +
                " prisonerClan=" + prisoner.Clan.Name +
                " relation=" + relation +
                " mercy=" + mercy +
                " honor=" + honor +
                " generosity=" + generosity +
                " calculating=" + calculating +
                " trust=" + trust +
                " grievance=" + grievance +
                " bloodDebt=" + bloodDebt +
                " obligation=" + obligation +
                " tension=" + tension +
                " clanLeader=" + clanLeader +
                " kingdomLeader=" + kingdomLeader +
                " score=" + F(score) +
                " threshold=" + F(ReleaseThreshold) +
                " decision=" + (release ? "RELEASE" : "KEEP") +
                " source=" + (source ?? "<none>") +
                " ageHours=" + F(ageHours) +
                " evaluated=" + _evaluated +
                " releases=" + _releaseDecisions +
                " keeps=" + _keepDecisions);

            if (!release)
                return;

            bool wasPrisoner = prisoner.IsPrisoner;
            string prisonPartyId =
                CustodyId(prisoner.PartyBelongedToAsPrisoner) ?? "<none>";

            EndCaptivityAction.ApplyByReleasedByChoice(
                prisoner,
                capturer);

            bool released = wasPrisoner && !prisoner.IsPrisoner;
            if (released)
            {
                _releaseCommits++;
                SocialLedger.RecordMercy(prisoner, capturer);
            }

            ClanAIPostVanilla.WriteExternalLog(
                "MERCY_DECISION_COMMIT actor=" + capturer.Name +
                " prisoner=" + prisoner.Name +
                " expected=ReleasedByChoice" +
                " wasPrisoner=" + wasPrisoner +
                " prisonPartyId=" + prisonPartyId +
                " isPrisonerAfter=" + prisoner.IsPrisoner +
                " released=" + released +
                " commits=" + _releaseCommits);
        }

        private static bool IsEligibleCustody(
            PartyBase custody,
            Hero prisoner,
            out Hero custodian)
        {
            custodian = null;

            if (custody == null ||
                prisoner == null ||
                prisoner == Hero.MainHero ||
                prisoner.Clan == null ||
                prisoner.Clan.Kingdom == null)
                return false;

            if (custody.IsMobile)
            {
                MobileParty mobile = custody.MobileParty;
                if (mobile == null ||
                    !mobile.IsActive ||
                    !mobile.IsLordParty ||
                    mobile == MobileParty.MainParty)
                    return false;

                custodian = mobile.LeaderHero ?? custody.Owner;
            }
            else if (custody.IsSettlement)
            {
                Settlement settlement = custody.Settlement;
                if (settlement == null ||
                    (!settlement.IsTown && !settlement.IsCastle))
                    return false;

                custodian = custody.Owner ?? settlement.Owner;
            }
            else
            {
                return false;
            }

            if (custodian == null ||
                custodian == Hero.MainHero ||
                custodian.Clan == null ||
                custodian.Clan.Kingdom == null ||
                custodian.Clan == prisoner.Clan ||
                custody.MapFaction == null ||
                prisoner.MapFaction == null ||
                !custody.MapFaction.IsAtWarWith(prisoner.MapFaction))
                return false;

            return true;
        }

        private static string CustodyId(PartyBase custody)
        {
            if (custody == null)
                return null;

            if (custody.IsMobile && custody.MobileParty != null)
            {
                string id = custody.MobileParty.StringId;
                if (string.IsNullOrEmpty(id))
                    id = HeroKey(custody.MobileParty.LeaderHero ?? custody.Owner);
                return string.IsNullOrEmpty(id) ? null : "mobile:" + id;
            }

            if (custody.IsSettlement && custody.Settlement != null)
            {
                string id = custody.Settlement.StringId;
                return string.IsNullOrEmpty(id) ? null : "settlement:" + id;
            }

            return null;
        }

        private static string CustodyType(PartyBase custody)
        {
            if (custody == null)
                return "<none>";
            if (custody.IsMobile)
                return "mobile-lord-party";
            if (custody.IsSettlement)
                return "settlement";
            return "other";
        }

        private static Hero FindHero(string heroId)
        {
            if (string.IsNullOrEmpty(heroId))
                return null;

            foreach (Hero hero in Hero.AllAliveHeroes)
            {
                if (hero != null &&
                    string.Equals(HeroKey(hero), heroId, StringComparison.Ordinal))
                    return hero;
            }
            return null;
        }

        private static string HeroKey(Hero hero)
        {
            if (hero == null)
                return null;
            return string.IsNullOrEmpty(hero.StringId)
                ? hero.Name.ToString()
                : hero.StringId;
        }

        private static string F(float value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static string F(double value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }
    }
}
