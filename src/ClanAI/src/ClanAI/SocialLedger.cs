using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace ClanAI
{
    public static class SocialLedger
    {
        private sealed class Record
        {
            public string ActorHeroId;
            public string ActorHeroName;

            public string ActorClanId;
            public string ActorClanName;

            public string TargetClanId;
            public string TargetClanName;

            public int Considerations;
            public int LastRelation;

            public int Trust;
            public int Grievance;
            public int BloodDebt;
            public int Obligation;
            public int Tension;

            public string LastAction;
            public string LastTarget;
        }

        private static readonly Dictionary<string, Record> Records =
            new Dictionary<string, Record>();

        private static bool _loadedFromSave;

        private static MethodInfo _relationMethod;
        private static bool _relationResolved;

        public static void BeginSession()
        {
            if (!_loadedFromSave)
                Records.Clear();

            ClanAIPostVanilla.WriteExternalLog(
                "SOCIAL_LEDGER_SESSION_READY records=" +
                Records.Count +
                " restored=" +
                _loadedFromSave);

            _loadedFromSave = false;
        }

        // =================================================
        // READ-ONLY AI CONSIDERATION OBSERVATION
        // =================================================

        public static void Observe(
            MobileParty actor,
            PartyThinkParams thinkParams)
        {
            if (actor == null ||
                actor.LeaderHero == null ||
                actor.LeaderHero.Clan == null ||
                thinkParams == null ||
                thinkParams.AIBehaviorScores.Count == 0)
            {
                return;
            }

            int bestIndex = -1;
            float bestScore = 0.10f;

            for (int i = 0;
                 i < thinkParams.AIBehaviorScores.Count;
                 i++)
            {
                AIBehaviorData candidate =
                    thinkParams.AIBehaviorScores[i].Item1;

                float score =
                    thinkParams.AIBehaviorScores[i].Item2;

                if (!IsAggressive(
                    candidate.AiBehavior))
                {
                    continue;
                }

                // Ignore dead / meaningless candidates.
                if (score <= bestScore)
                    continue;

                if (candidate.Party == null)
                    continue;

                bestScore = score;
                bestIndex = i;
            }

            if (bestIndex < 0)
                return;

            AIBehaviorData chosen =
                thinkParams
                .AIBehaviorScores[bestIndex]
                .Item1;

            Hero targetOwner = null;
            Clan targetClan = null;
            string targetName = "<unknown>";

            Settlement settlement =
                chosen.Party as Settlement;

            if (settlement != null)
            {
                targetName =
                    settlement.Name.ToString();

                targetOwner =
                    settlement.Owner;

                targetClan =
                    settlement.OwnerClan;
            }
            else
            {
                MobileParty targetParty =
                    chosen.Party as MobileParty;

                if (targetParty == null)
                    return;

                targetName =
                    targetParty.Name.ToString();

                targetOwner =
                    targetParty.Owner;

                if (targetOwner == null)
                {
                    targetOwner =
                        targetParty.LeaderHero;
                }

                if (targetOwner != null)
                {
                    targetClan =
                        targetOwner.Clan;
                }
            }

            if (targetClan == null)
                return;

            Clan actorClan =
                actor.LeaderHero.Clan;

            if (actorClan == targetClan)
                return;

            Record record =
                GetOrCreate(
                    actor.LeaderHero,
                    targetClan);

            if (record == null)
                return;

            record.Considerations++;

            record.LastRelation =
                GetRelation(
                    actor.LeaderHero,
                    targetOwner);

            record.LastAction =
                chosen.AiBehavior.ToString();

            record.LastTarget =
                targetName;

            if (record.Considerations == 1)
            {
                ClanAIPostVanilla.WriteExternalLog(
                    "SOCIAL_LEDGER_BORN" +
                    " actor=" +
                    record.ActorHeroName +
                    " actorClan=" +
                    record.ActorClanName +
                    " towardClan=" +
                    record.TargetClanName +
                    " relation=" +
                    record.LastRelation +
                    " action=" +
                    record.LastAction +
                    " target=" +
                    record.LastTarget +
                    " trust=" +
                    record.Trust +
                    " grievance=" +
                    record.Grievance +
                    " bloodDebt=" +
                    record.BloodDebt +
                    " obligation=" +
                    record.Obligation +
                    " tension=" +
                    record.Tension);

                return;
            }

            if (record.Considerations == 3 ||
                record.Considerations == 10 ||
                record.Considerations == 25)
            {
                ClanAIPostVanilla.WriteExternalLog(
                    "SOCIAL_LEDGER_TOUCH" +
                    " actor=" +
                    record.ActorHeroName +
                    " towardClan=" +
                    record.TargetClanName +
                    " considerations=" +
                    record.Considerations +
                    " relation=" +
                    record.LastRelation +
                    " action=" +
                    record.LastAction +
                    " target=" +
                    record.LastTarget +
                    " trust=" +
                    record.Trust +
                    " grievance=" +
                    record.Grievance +
                    " bloodDebt=" +
                    record.BloodDebt +
                    " obligation=" +
                    record.Obligation +
                    " tension=" +
                    record.Tension);
            }
        }

        // =================================================
        // REAL WORLD EVENT: RAID BEGINS
        //
        // The victim remembers the aggressor.
        // This changes MEMORY ONLY, not vanilla relation
        // and not AI candidate scores.
        // =================================================

        public static void RecordRaidStarted(
            MobileParty raider,
            Settlement target)
        {
            if (raider == null ||
                raider.LeaderHero == null ||
                raider.LeaderHero.Clan == null ||
                target == null ||
                target.OwnerClan == null)
            {
                return;
            }

            Clan aggressorClan =
                raider.LeaderHero.Clan;

            Clan victimClan =
                target.OwnerClan;

            if (aggressorClan == victimClan)
                return;

            Hero rememberer =
                victimClan.Leader;

            if (rememberer == null)
                return;

            Record record =
                GetOrCreate(
                    rememberer,
                    aggressorClan);

            if (record == null)
                return;

            int oldTrust =
                record.Trust;

            int oldGrievance =
                record.Grievance;

            int oldTension =
                record.Tension;

            record.LastRelation =
                GetRelation(
                    rememberer,
                    raider.LeaderHero);

            // Initial conservative calibration.
            record.Grievance =
                Clamp(
                    record.Grievance + 12,
                    0,
                    100);

            record.Tension =
                Clamp(
                    record.Tension + 8,
                    0,
                    100);

            record.Trust =
                Clamp(
                    record.Trust - 4,
                    -100,
                    100);

            record.LastAction =
                "SufferedRaid";

            record.LastTarget =
                target.Name.ToString();

            SocialEpisodeMemory.Record(
                "RaidStarted", rememberer, raider.LeaderHero,
                target.StringId, target.Name.ToString(),
                "SocialWorldObserver.PolledRaid_OwnerAttribution");

            ClanAIPostVanilla.WriteExternalLog(
                "WORLD_MEMORY" +
                " event=RaidStarted" +
                " rememberer=" +
                rememberer.Name.ToString() +
                " remembererClan=" +
                victimClan.Name.ToString() +
                " againstClan=" +
                aggressorClan.Name.ToString() +
                " raider=" +
                raider.LeaderHero.Name.ToString() +
                " target=" +
                target.Name.ToString() +
                " relation=" +
                record.LastRelation +
                " grievance=" +
                oldGrievance +
                "->" +
                record.Grievance +
                " tension=" +
                oldTension +
                "->" +
                record.Tension +
                " trust=" +
                oldTrust +
                "->" +
                record.Trust +
                " bloodDebt=" +
                record.BloodDebt +
                " obligation=" +
                record.Obligation);
        }

        // =================================================

        // =================================================
        // REAL WORLD EVENT: HERO CAPTURED
        //
        // Direct personal memory:
        // the prisoner remembers the clan that captured them.
        //
        // Still does NOT alter Bannerlord AI scores.
        // =================================================

        public static void RecordCapture(
            Hero prisoner,
            PartyBase capturer)
        {
            if (prisoner == null ||
                prisoner.Clan == null ||
                capturer == null)
            {
                return;
            }

            Hero capturerHero =
                capturer.Owner;

            if (capturerHero == null &&
                capturer.MobileParty != null)
            {
                capturerHero =
                    capturer.MobileParty.LeaderHero;
            }

            if (capturerHero == null ||
                capturerHero.Clan == null)
            {
                return;
            }

            Clan prisonerClan =
                prisoner.Clan;

            Clan capturerClan =
                capturerHero.Clan;

            if (prisonerClan == capturerClan)
                return;

            Record record =
                GetOrCreate(
                    prisoner,
                    capturerClan);

            if (record == null)
                return;

            int oldTrust =
                record.Trust;

            int oldGrievance =
                record.Grievance;

            int oldTension =
                record.Tension;

            record.LastRelation =
                GetRelation(
                    prisoner,
                    capturerHero);

            // Moderate grievance:
            // capture during war is serious,
            // but not equivalent to killing kin
            // or destroying home territory.
            record.Grievance =
                Clamp(
                    record.Grievance + 8,
                    0,
                    100);

            record.Tension =
                Clamp(
                    record.Tension + 6,
                    0,
                    100);

            record.Trust =
                Clamp(
                    record.Trust - 3,
                    -100,
                    100);

            record.LastAction =
                "CapturedBy";

            record.LastTarget =
                capturerHero.Name.ToString();

            SocialEpisodeMemory.Record(
                "HeroCaptured", prisoner, capturerHero,
                capturer.MobileParty != null ? capturer.MobileParty.StringId :
                    capturer.Settlement != null ? capturer.Settlement.StringId : "",
                capturer.Name.ToString(), "CampaignEvents.HeroPrisonerTaken");

            ClanAIPostVanilla.WriteExternalLog(
                "WORLD_MEMORY" +
                " event=HeroCaptured" +
                " rememberer=" +
                prisoner.Name.ToString() +
                " remembererClan=" +
                prisonerClan.Name.ToString() +
                " againstClan=" +
                capturerClan.Name.ToString() +
                " capturer=" +
                capturerHero.Name.ToString() +
                " capturerParty=" +
                capturer.Name.ToString() +
                " relation=" +
                record.LastRelation +
                " grievance=" +
                oldGrievance +
                "->" +
                record.Grievance +
                " tension=" +
                oldTension +
                "->" +
                record.Tension +
                " trust=" +
                oldTrust +
                "->" +
                record.Trust +
                " bloodDebt=" +
                record.BloodDebt +
                " obligation=" +
                record.Obligation);
        }

        // =================================================
        // REAL WORLD EVENT: INTENTIONAL HERO RELEASE
        //
        // Only used from the verified
        // EndCaptivityAction.ApplyByReleasedByChoice
        // Hero/Hero pathway.
        //
        // Prisoner remembers the facilitator's clan.
        // =================================================

        public static void RecordMercy(
            Hero prisoner,
            Hero facilitator)
        {
            if (prisoner == null ||
                facilitator == null ||
                prisoner.Clan == null ||
                facilitator.Clan == null)
            {
                return;
            }

            Clan prisonerClan =
                prisoner.Clan;

            Clan facilitatorClan =
                facilitator.Clan;

            if (prisonerClan == facilitatorClan)
                return;

            Record record =
                GetOrCreate(
                    prisoner,
                    facilitatorClan);

            if (record == null)
                return;

            int oldTrust =
                record.Trust;

            int oldGrievance =
                record.Grievance;

            int oldTension =
                record.Tension;

            int oldObligation =
                record.Obligation;

            record.LastRelation =
                GetRelation(
                    prisoner,
                    facilitator);

            // Initial calibration values.
            //
            // Intentional mercy is stronger than
            // simply ending captivity through ransom,
            // escape, peace, compensation, or battle.
            record.Trust =
                Clamp(
                    record.Trust + 10,
                    -100,
                    100);

            record.Obligation =
                Clamp(
                    record.Obligation + 12,
                    0,
                    100);

            record.Grievance =
                Clamp(
                    record.Grievance - 5,
                    0,
                    100);

            record.Tension =
                Clamp(
                    record.Tension - 4,
                    0,
                    100);

            record.LastAction =
                "ReleasedByChoice";

            record.LastTarget =
                facilitator.Name.ToString();

            SocialEpisodeMemory.Record(
                "MercyRelease", prisoner, facilitator,
                "", "VoluntaryRelease",
                "EndCaptivityAction.ApplyInternal_ReleasedByChoice");

            ClanAIPostVanilla.WriteExternalLog(
                "WORLD_MEMORY" +
                " event=MercyRelease" +
                " rememberer=" +
                prisoner.Name.ToString() +
                " remembererClan=" +
                prisonerClan.Name.ToString() +
                " towardClan=" +
                facilitatorClan.Name.ToString() +
                " facilitator=" +
                facilitator.Name.ToString() +
                " relation=" +
                record.LastRelation +
                " grievance=" +
                oldGrievance +
                "->" +
                record.Grievance +
                " tension=" +
                oldTension +
                "->" +
                record.Tension +
                " trust=" +
                oldTrust +
                "->" +
                record.Trust +
                " obligation=" +
                oldObligation +
                "->" +
                record.Obligation +
                " bloodDebt=" +
                record.BloodDebt);
        }
        // SAVE / LOAD
        // =================================================

        public static List<string> ExportSaveLines()
        {
            List<string> lines =
                new List<string>();

            foreach (Record r in Records.Values)
            {
                lines.Add(
                    B64(r.ActorHeroId) + "|" +
                    B64(r.ActorHeroName) + "|" +
                    B64(r.ActorClanId) + "|" +
                    B64(r.ActorClanName) + "|" +
                    B64(r.TargetClanId) + "|" +
                    B64(r.TargetClanName) + "|" +
                    r.Considerations + "|" +
                    r.LastRelation + "|" +
                    r.Trust + "|" +
                    r.Grievance + "|" +
                    r.BloodDebt + "|" +
                    r.Obligation + "|" +
                    r.Tension + "|" +
                    B64(r.LastAction) + "|" +
                    B64(r.LastTarget)
                );
            }

            ClanAIPostVanilla.WriteExternalLog(
                "SOCIAL_LEDGER_SAVE records=" +
                lines.Count);

            return lines;
        }

        public static void ImportSaveLines(
            List<string> lines)
        {
            Records.Clear();

            if (lines != null)
            {
                foreach (string line in lines)
                {
                    if (string.IsNullOrEmpty(line))
                        continue;

                    string[] p =
                        line.Split('|');

                    if (p.Length != 15)
                        continue;

                    Record r =
                        new Record();

                    r.ActorHeroId =
                        FromB64(p[0]);

                    r.ActorHeroName =
                        FromB64(p[1]);

                    r.ActorClanId =
                        FromB64(p[2]);

                    r.ActorClanName =
                        FromB64(p[3]);

                    r.TargetClanId =
                        FromB64(p[4]);

                    r.TargetClanName =
                        FromB64(p[5]);

                    if (!int.TryParse(
                        p[6],
                        out r.Considerations))
                        continue;

                    if (!int.TryParse(
                        p[7],
                        out r.LastRelation))
                        continue;

                    if (!int.TryParse(
                        p[8],
                        out r.Trust))
                        continue;

                    if (!int.TryParse(
                        p[9],
                        out r.Grievance))
                        continue;

                    if (!int.TryParse(
                        p[10],
                        out r.BloodDebt))
                        continue;

                    if (!int.TryParse(
                        p[11],
                        out r.Obligation))
                        continue;

                    if (!int.TryParse(
                        p[12],
                        out r.Tension))
                        continue;

                    r.LastAction =
                        FromB64(p[13]);

                    r.LastTarget =
                        FromB64(p[14]);

                    if (string.IsNullOrEmpty(
                        r.ActorHeroId) ||
                        string.IsNullOrEmpty(
                        r.TargetClanId))
                    {
                        continue;
                    }

                    string key =
                        r.ActorHeroId +
                        ">" +
                        r.TargetClanId;

                    Records[key] = r;
                }
            }

            _loadedFromSave = true;

            int nonzeroRecords = 0;
            int totalGrievance = 0;
            int totalTension = 0;
            int negativeTrustRecords = 0;
            int totalNegativeTrust = 0;
            int totalBloodDebt = 0;
            int totalObligation = 0;

            foreach (Record r in Records.Values)
            {
                totalGrievance += r.Grievance;
                totalTension += r.Tension;
                totalBloodDebt += r.BloodDebt;
                totalObligation += r.Obligation;

                if (r.Trust < 0)
                {
                    negativeTrustRecords++;
                    totalNegativeTrust += -r.Trust;
                }

                if (r.Trust != 0 ||
                    r.Grievance != 0 ||
                    r.BloodDebt != 0 ||
                    r.Obligation != 0 ||
                    r.Tension != 0)
                {
                    nonzeroRecords++;
                }
            }

            ClanAIPostVanilla.WriteExternalLog(
                "SOCIAL_MEMORY_RESTORED" +
                " records=" +
                Records.Count +
                " nonzeroRecords=" +
                nonzeroRecords +
                " totalGrievance=" +
                totalGrievance +
                " totalTension=" +
                totalTension +
                " negativeTrustRecords=" +
                negativeTrustRecords +
                " totalNegativeTrust=" +
                totalNegativeTrust +
                " totalBloodDebt=" +
                totalBloodDebt +
                " totalObligation=" +
                totalObligation);

            ClanAIPostVanilla.WriteExternalLog(
                "SOCIAL_LEDGER_LOAD records=" +
                Records.Count);
        }

        public static bool TryGetState(
            Hero actor,
            Clan targetClan,
            out int trust,
            out int grievance,
            out int bloodDebt,
            out int obligation,
            out int tension)
        {
            trust = 0;
            grievance = 0;
            bloodDebt = 0;
            obligation = 0;
            tension = 0;

            if (actor == null ||
                actor.Clan == null ||
                targetClan == null)
            {
                return false;
            }

            string actorHeroId =
                SafeId(
                    actor.StringId,
                    actor.Name.ToString());

            string targetClanId =
                SafeId(
                    targetClan.StringId,
                    targetClan.Name.ToString());

            string key =
                actorHeroId +
                ">" +
                targetClanId;

            Record record;

            if (!Records.TryGetValue(
                key,
                out record))
            {
                return false;
            }

            trust = record.Trust;
            grievance = record.Grievance;
            bloodDebt = record.BloodDebt;
            obligation = record.Obligation;
            tension = record.Tension;

            return true;
        }

        // =================================================
        // INTERNAL
        // =================================================

        private static Record GetOrCreate(
            Hero actor,
            Clan targetClan)
        {
            if (actor == null ||
                actor.Clan == null ||
                targetClan == null)
            {
                return null;
            }

            string actorHeroId =
                SafeId(
                    actor.StringId,
                    actor.Name.ToString());

            string actorClanId =
                SafeId(
                    actor.Clan.StringId,
                    actor.Clan.Name.ToString());

            string targetClanId =
                SafeId(
                    targetClan.StringId,
                    targetClan.Name.ToString());

            string key =
                actorHeroId +
                ">" +
                targetClanId;

            Record record;

            if (Records.TryGetValue(
                key,
                out record))
            {
                return record;
            }

            record =
                new Record();

            record.ActorHeroId =
                actorHeroId;

            record.ActorHeroName =
                actor.Name.ToString();

            record.ActorClanId =
                actorClanId;

            record.ActorClanName =
                actor.Clan.Name.ToString();

            record.TargetClanId =
                targetClanId;

            record.TargetClanName =
                targetClan.Name.ToString();

            record.Considerations = 0;
            record.LastRelation = 0;

            record.Trust = 0;
            record.Grievance = 0;
            record.BloodDebt = 0;
            record.Obligation = 0;
            record.Tension = 0;

            record.LastAction = "";
            record.LastTarget = "";

            Records.Add(
                key,
                record);

            return record;
        }

        private static bool IsAggressive(
            AiBehavior behavior)
        {
            return
                behavior == AiBehavior.RaidSettlement ||
                behavior == AiBehavior.BesiegeSettlement ||
                behavior == AiBehavior.AssaultSettlement ||
                behavior == AiBehavior.EngageParty;
        }

        private static int GetRelation(
            Hero actor,
            Hero target)
        {
            if (actor == null ||
                target == null)
            {
                return 0;
            }

            try
            {
                if (!_relationResolved)
                {
                    _relationResolved = true;

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
                    return 0;

                object result =
                    _relationMethod.Invoke(
                        null,
                        new object[]
                        {
                            actor,
                            target
                        });

                if (result == null)
                    return 0;

                return Convert.ToInt32(result);
            }
            catch
            {
                return 0;
            }
        }

        private static int Clamp(
            int value,
            int min,
            int max)
        {
            if (value < min)
                return min;

            if (value > max)
                return max;

            return value;
        }

        private static string SafeId(
            string id,
            string fallback)
        {
            return string.IsNullOrEmpty(id)
                ? fallback
                : id;
        }

        private static string B64(
            string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            return Convert.ToBase64String(
                Encoding.UTF8.GetBytes(value));
        }

        private static string FromB64(
            string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            try
            {
                return Encoding.UTF8.GetString(
                    Convert.FromBase64String(value));
            }
            catch
            {
                return "";
            }
        }
    }
}



