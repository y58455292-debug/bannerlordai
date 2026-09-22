using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace ClanAI
{
    // Facts only. Emotions and strategic effects are NOT inferred here.
    public sealed class SocialEpisode
    {
        public string Id, Kind, CampaignHours, ObservedUtc;
        public string RemembererId, RemembererName, RemembererClanId;
        public string OtherHeroId, OtherHeroName, OtherClanId, OtherClanName;
        public string ContextId, ContextName, Source;
        public string Theme { get { return Kind == "RaidStarted" ? "Territory" : "Captivity"; } }

        public string Encode()
        {
            string[] fields = { Id, Kind, CampaignHours, ObservedUtc,
                RemembererId, RemembererName, RemembererClanId,
                OtherHeroId, OtherHeroName, OtherClanId, OtherClanName,
                ContextId, ContextName, Source };
            for (int i = 0; i < fields.Length; i++)
                fields[i] = Convert.ToBase64String(Encoding.UTF8.GetBytes(fields[i] ?? ""));
            return "E1|" + string.Join("|", fields);
        }
        public static bool TryDecode(string row, out SocialEpisode e)
        {
            e = null;
            if (string.IsNullOrEmpty(row)) return false;
            string[] f = row.Split('|');
            if (f.Length != 15 || f[0] != "E1") return false;
            try
            {
                for (int i = 1; i < f.Length; i++)
                    f[i] = Encoding.UTF8.GetString(Convert.FromBase64String(f[i]));
            }
            catch (FormatException) { return false; }
            Guid id;
            DateTime utc;
            double hours;
            if (!Guid.TryParse(f[1], out id) || id == Guid.Empty) return false;
            if (f[2] != "HeroCaptured" && f[2] != "MercyRelease" && f[2] != "RaidStarted") return false;
            if (!DateTime.TryParse(f[4], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out utc)) return false;
            if (f[3] != "unknown" && (!double.TryParse(f[3], NumberStyles.Float,
                CultureInfo.InvariantCulture, out hours) || double.IsNaN(hours) || double.IsInfinity(hours))) return false;
            if (string.IsNullOrWhiteSpace(f[5]) || string.IsNullOrWhiteSpace(f[7]) ||
                string.IsNullOrWhiteSpace(f[8]) || string.IsNullOrWhiteSpace(f[10]) ||
                string.IsNullOrWhiteSpace(f[14])) return false;
            e = new SocialEpisode { Id = f[1], Kind = f[2], CampaignHours = f[3], ObservedUtc = f[4],
                RemembererId = f[5], RemembererName = f[6], RemembererClanId = f[7],
                OtherHeroId = f[8], OtherHeroName = f[9], OtherClanId = f[10], OtherClanName = f[11],
                ContextId = f[12], ContextName = f[13], Source = f[14] };
            return true;
        }
    }

    public sealed class EpisodeGroupCounts
    {
        public int Captures, Mercies, Raids;
        public bool BothCaptureAndMercy { get { return Captures > 0 && Mercies > 0; } }
    }

    public sealed class SocialEpisodeStore
    {
        private readonly int _capacity;
        private readonly List<string> _rows = new List<string>();
        private readonly HashSet<string> _ids = new HashSet<string>(StringComparer.Ordinal);
        private readonly Dictionary<string, EpisodeGroupCounts> _groups =
            new Dictionary<string, EpisodeGroupCounts>(StringComparer.Ordinal);
        public int Count { get { return _rows.Count; } }
        public int GroupCount { get { return _groups.Count; } }
        public int MixedGroupCount { get; private set; }

        public SocialEpisodeStore(int capacity = 20000)
        {
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            _capacity = capacity;
        }

        // Each stored row is immutable; later mercy never rewrites a capture.
        public List<string> Export() { return new List<string>(_rows); }
        public string Add(SocialEpisode e) { return AddRow(e == null ? null : e.Encode()); }
        private string AddRow(string row)
        {
            SocialEpisode e;
            if (!SocialEpisode.TryDecode(row, out e)) return "INVALID";
            if (_ids.Contains(e.Id)) return "DUPLICATE_ID";
            if (_rows.Count >= _capacity) return "CAPACITY";
            string key = Key(e.RemembererId, e.OtherClanId, e.Theme);
            EpisodeGroupCounts group;
            if (!_groups.TryGetValue(key, out group))
            {
                group = new EpisodeGroupCounts();
                _groups.Add(key, group);
            }
            bool wasMixed = group.BothCaptureAndMercy;
            if (e.Kind == "HeroCaptured") group.Captures++;
            if (e.Kind == "MercyRelease") group.Mercies++;
            if (e.Kind == "RaidStarted") group.Raids++;
            if (!wasMixed && group.BothCaptureAndMercy) MixedGroupCount++;
            _rows.Add(row);
            _ids.Add(e.Id);
            return "ADDED";
        }

        // Duplicate saved IDs are rejected; distinct callbacks retain distinct IDs.
        public void Import(List<string> rows, out int rejected, out int duplicateIds)
        {
            _rows.Clear(); _ids.Clear(); _groups.Clear(); MixedGroupCount = 0;
            rejected = 0; duplicateIds = 0;
            if (rows == null) return;
            foreach (string row in rows)
            {
                string status = AddRow(row);
                if (status == "DUPLICATE_ID") duplicateIds++;
                else if (status != "ADDED") rejected++;
            }
        }

        public EpisodeGroupCounts GetCounts(string heroId, string clanId, string theme)
        {
            EpisodeGroupCounts g;
            if (!_groups.TryGetValue(Key(heroId, clanId, theme), out g)) return new EpisodeGroupCounts();
            return new EpisodeGroupCounts { Captures = g.Captures, Mercies = g.Mercies, Raids = g.Raids };
        }

        private static string Key(string heroId, string clanId, string theme)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(heroId ?? "")) + "|" +
                Convert.ToBase64String(Encoding.UTF8.GetBytes(clanId ?? "")) + "|" + theme;
        }

        public string Digest()
        {
            using (SHA256 sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(
                    string.Join("\n", _rows)))).Replace("-", "");
        }
    }
}
