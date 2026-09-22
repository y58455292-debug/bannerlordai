using System;
using System.Collections.Generic;
using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace ClanAI
{
    public static class NobleMemory
    {
        private sealed class Record
        {
            public int Decisions;
            public int Changes;
            public int RepeatStreak;

            public string LastBehavior;
            public string LastTarget;
        }

        private static readonly Dictionary<string, Record> Records =
            new Dictionary<string, Record>();

        private static bool _loadedFromSave;

        public static void BeginSession()
        {
            if (!_loadedFromSave)
            {
                Records.Clear();
            }

            ClanAIPostVanilla.WriteExternalLog(
                "NOBLE_MEMORY_SESSION_READY records=" +
                Records.Count +
                " restored=" +
                _loadedFromSave);

            _loadedFromSave = false;
        }

        public static List<string> ExportSaveLines()
        {
            List<string> lines =
                new List<string>();

            foreach (var pair in Records)
            {
                Record r = pair.Value;

                lines.Add(
                    B64(pair.Key) + "|" +
                    r.Decisions + "|" +
                    r.Changes + "|" +
                    r.RepeatStreak + "|" +
                    B64(r.LastBehavior) + "|" +
                    B64(r.LastTarget)
                );
            }

            ClanAIPostVanilla.WriteExternalLog(
                "NOBLE_MEMORY_SAVE records=" +
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

                    string[] p = line.Split('|');

                    if (p.Length != 6)
                        continue;

                    string heroKey =
                        FromB64(p[0]);

                    if (string.IsNullOrEmpty(heroKey))
                        continue;

                    int decisions;
                    int changes;
                    int streak;

                    if (!int.TryParse(
                        p[1],
                        out decisions))
                        continue;

                    if (!int.TryParse(
                        p[2],
                        out changes))
                        continue;

                    if (!int.TryParse(
                        p[3],
                        out streak))
                        continue;

                    Record record =
                        new Record();

                    record.Decisions = decisions;
                    record.Changes = changes;
                    record.RepeatStreak = streak;
                    record.LastBehavior =
                        EmptyToNull(FromB64(p[4]));
                    record.LastTarget =
                        EmptyToNull(FromB64(p[5]));

                    Records[heroKey] = record;
                }
            }

            _loadedFromSave = true;

            ClanAIPostVanilla.WriteExternalLog(
                "NOBLE_MEMORY_LOAD records=" +
                Records.Count);
        }

        public static void Observe(
            MobileParty party,
            PartyThinkParams thinkParams)
        {
            if (party == null ||
                party.LeaderHero == null ||
                thinkParams == null ||
                thinkParams.AIBehaviorScores.Count == 0)
            {
                return;
            }

            int bestIndex = -1;
            float bestScore = float.MinValue;

            for (int i = 0;
                 i < thinkParams.AIBehaviorScores.Count;
                 i++)
            {
                float score =
                    thinkParams.AIBehaviorScores[i].Item2;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestIndex = i;
                }
            }

            if (bestIndex < 0)
                return;

            AIBehaviorData best =
                thinkParams.AIBehaviorScores[bestIndex].Item1;

            string behavior =
                best.AiBehavior.ToString();

            string target =
                GetTargetName(best.Party);

            string heroKey =
                party.LeaderHero.StringId;

            if (string.IsNullOrEmpty(heroKey))
            {
                heroKey =
                    party.LeaderHero.Name.ToString();
            }

            Record record;

            if (!Records.TryGetValue(
                heroKey,
                out record))
            {
                record = new Record();

                Records.Add(
                    heroKey,
                    record);
            }

            record.Decisions++;

            if (record.LastBehavior == null)
            {
                record.LastBehavior = behavior;
                record.LastTarget = target;
                record.RepeatStreak = 1;

                ClanAIPostVanilla.WriteExternalLog(
                    "MEMORY_BORN noble=" +
                    party.LeaderHero.Name.ToString() +
                    " behavior=" +
                    behavior +
                    " target=" +
                    target);

                return;
            }

            bool same =
                record.LastBehavior == behavior &&
                record.LastTarget == target;

            if (same)
            {
                record.RepeatStreak++;

                if (record.RepeatStreak == 3 ||
                    record.RepeatStreak == 6 ||
                    record.RepeatStreak == 10)
                {
                    ClanAIPostVanilla.WriteExternalLog(
                        "MEMORY_REPEAT noble=" +
                        party.LeaderHero.Name.ToString() +
                        " streak=" +
                        record.RepeatStreak +
                        " behavior=" +
                        behavior +
                        " target=" +
                        target);
                }

                return;
            }

            record.Changes++;

            ClanAIPostVanilla.WriteExternalLog(
                "MEMORY_CHANGE noble=" +
                party.LeaderHero.Name.ToString() +
                " decisions=" +
                record.Decisions +
                " changes=" +
                record.Changes +
                " from=" +
                record.LastBehavior +
                ":" +
                record.LastTarget +
                " to=" +
                behavior +
                ":" +
                target);

            record.LastBehavior = behavior;
            record.LastTarget = target;
            record.RepeatStreak = 1;
        }

        private static string GetTargetName(
            object target)
        {
            if (target == null)
                return "<none>";

            Settlement settlement =
                target as Settlement;

            if (settlement != null)
                return settlement.Name.ToString();

            MobileParty mobileParty =
                target as MobileParty;

            if (mobileParty != null)
                return mobileParty.Name.ToString();

            return target.ToString();
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

        private static string EmptyToNull(
            string value)
        {
            return string.IsNullOrEmpty(value)
                ? null
                : value;
        }
    }
}
