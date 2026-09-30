using System;
using System.Collections.Generic;
using System.Text;

namespace ClanAI
{
    // Save data only. Never serializes native objects, party IDs or display names.
    internal sealed class HomeAssignmentRecords
    {
        internal const string SaveKey = "ClanAI_HomeAssignment_v1";
        internal const int MaximumRows = 256;
        private readonly Dictionary<string, string> _homes =
            new Dictionary<string, string>(StringComparer.Ordinal);
        internal long Revision { get; private set; }
        internal int RejectedRows { get; private set; }
        internal IEnumerable<KeyValuePair<string, string>> Entries { get { return _homes; } }
        internal bool TryGet(string hero, out string home)
        { home = null; return hero != null && _homes.TryGetValue(hero, out home); }
        internal bool Set(string hero, string home)
        {
            if (!ValidId(hero) || !ValidId(home)) return false;
            string previous;
            if (_homes.TryGetValue(hero, out previous) && previous == home) return false;
            if (!_homes.ContainsKey(hero) && _homes.Count >= MaximumRows) return false;
            _homes[hero] = home; Revision++; return true;
        }
        internal bool Clear(string hero)
        {
            if (hero == null || !_homes.Remove(hero)) return false;
            Revision++; return true;
        }
        internal List<string> Export()
        {
            var ids = new List<string>(_homes.Keys); ids.Sort(StringComparer.Ordinal);
            var rows = new List<string>(ids.Count);
            foreach (string id in ids) rows.Add("D1|" + Encode(id) + "|" + Encode(_homes[id]));
            return rows;
        }
        internal void Import(List<string> rows)
        {
            _homes.Clear(); Revision++; RejectedRows = 0;
            if (rows == null) return;
            if (rows.Count > MaximumRows) { RejectedRows = rows.Count; return; }
            var conflicts = new HashSet<string>(StringComparer.Ordinal);
            foreach (string row in rows)
            {
                try
                {
                    if (row == null || row.Length > 1400) { RejectedRows++; continue; }
                    string[] p = row.Split('|');
                    if (p.Length != 3 || p[0] != "D1") { RejectedRows++; continue; }
                    string hero = Decode(p[1]), home = Decode(p[2]);
                    if (!ValidId(hero) || !ValidId(home)) { RejectedRows++; continue; }
                    if (conflicts.Contains(hero)) { RejectedRows++; continue; }
                    string previous;
                    if (_homes.TryGetValue(hero, out previous) && previous != home)
                    { _homes.Remove(hero); conflicts.Add(hero); RejectedRows++; continue; }
                    _homes[hero] = home;
                }
                catch (FormatException) { RejectedRows++; }
                catch (DecoderFallbackException) { RejectedRows++; }
            }
        }
        private static bool ValidId(string id)
        { return !string.IsNullOrWhiteSpace(id) && Encoding.UTF8.GetByteCount(id) <= 512; }
        private static string Encode(string value)
        { return Convert.ToBase64String(Encoding.UTF8.GetBytes(value)); }
        private static string Decode(string value)
        { return new UTF8Encoding(false, true).GetString(Convert.FromBase64String(value)); }
    }
}
