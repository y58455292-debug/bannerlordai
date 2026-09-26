using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using TaleWorlds.CampaignSystem;

namespace ClanAI
{
    // v0.19A: event-history foundation for complexes, not an emotion model.
    public static class SocialEpisodeMemory
    {
        private static readonly string AuditPath =
            ModuleRuntimePaths.Log("episodes.log");
        private static readonly object AuditLock = new object();
        private static SocialEpisodeStore _store = new SocialEpisodeStore();
        private static bool _prepared, _ready, _restored, _capacityReported;
        private static int _rejected, _duplicateIds, _preSessionSkipped;
        private static string _runId = "not-started";

        public static void SyncData(IDataStore dataStore)
        {
            List<string> rows = dataStore.IsSaving ? _store.Export() : new List<string>();
            bool found = dataStore.SyncData("ClanAI_SocialEpisodes_v1", ref rows);
            if (dataStore.IsLoading)
            {
                _ready = false;
                _store = new SocialEpisodeStore();
                _store.Import(found ? rows : null, out _rejected, out _duplicateIds);
                _restored = found;
                _prepared = true;
            }
            if (dataStore.IsSaving) Audit("EPISODE_SAVE" + Stats());
        }

        public static void BeginSession()
        {
            if (!_prepared)
            {
                _store = new SocialEpisodeStore();
                _restored = false; _rejected = 0; _duplicateIds = 0;
            }
            _prepared = false;
            _ready = true;
            _capacityReported = false;
            _runId = Guid.NewGuid().ToString("N");
            Audit("EPISODE_SESSION_READY" + Stats() + " restored=" + _restored +
                " rejected=" + _rejected + " duplicateIds=" + _duplicateIds +
                " preSessionSkipped=" + _preSessionSkipped +
                " historicalBackfill=False scoreMutation=False appraisal=NOT_IMPLEMENTED");
            _preSessionSkipped = 0;
        }

        public static void Record(string kind, Hero rememberer, Hero other,
            string contextId, string contextName, string source)
        {
            try
            {
                if (!_ready) { _preSessionSkipped++; return; }
                if (rememberer == null || other == null || rememberer.Clan == null || other.Clan == null)
                    return;
                string hours;
                try { hours = CampaignTime.Now.ToHours.ToString("R", CultureInfo.InvariantCulture); }
                catch { hours = "unknown"; }
                var e = new SocialEpisode {
                    Id = Guid.NewGuid().ToString("N"), Kind = kind,
                    CampaignHours = hours, ObservedUtc = DateTime.UtcNow.ToString("O"),
                    RemembererId = rememberer.StringId, RemembererName = rememberer.Name.ToString(),
                    RemembererClanId = rememberer.Clan.StringId,
                    OtherHeroId = other.StringId, OtherHeroName = other.Name.ToString(),
                    OtherClanId = other.Clan.StringId, OtherClanName = other.Clan.Name.ToString(),
                    ContextId = contextId, ContextName = contextName, Source = source };
                string status = _store.Add(e);
                if (status != "ADDED")
                {
                    if (status != "CAPACITY" || !_capacityReported)
                        Audit("EPISODE_NOT_RECORDED reason=" + status + " event=" + kind);
                    if (status == "CAPACITY") _capacityReported = true;
                    return;
                }
                EpisodeGroupCounts group = _store.GetCounts(e.RemembererId, e.OtherClanId, e.Theme);
                Audit("EPISODE_RECORDED id=" + e.Id + " event=" + kind + " theme=" + e.Theme +
                    " campaignHours=" + hours + " rememberer=" + Clean(e.RemembererName) +
                    " remembererId=" + e.RemembererId + " otherHero=" + Clean(e.OtherHeroName) +
                    " otherHeroId=" + e.OtherHeroId + " otherClan=" + Clean(e.OtherClanName) +
                    " otherClanId=" + e.OtherClanId + " contextId=" + Clean(e.ContextId) +
                    " context=" + Clean(e.ContextName) + " source=" + source +
                    " captures=" + group.Captures + " mercies=" + group.Mercies + " raids=" + group.Raids +
                    " bothCaptureAndMercy=" + group.BothCaptureAndMercy +
                    " scoreMutation=False appraisal=NOT_IMPLEMENTED");
            }
            catch (Exception ex) { Audit("EPISODE_ERROR type=" + ex.GetType().Name + " message=" + Clean(ex.Message)); }
        }

        private static string Stats()
        {
            return " records=" + _store.Count + " groups=" + _store.GroupCount +
                " mixedGroups=" + _store.MixedGroupCount + " sha256=" + _store.Digest();
        }
        private static string Clean(string text)
        {
            return (text ?? "<unknown>").Replace("\r", " ").Replace("\n", " ").Replace("\t", " ");
        }

        private static void Audit(string message)
        {
            if (!RuntimeProfile.EvidenceEnabled ||
                string.IsNullOrEmpty(AuditPath))
            {
                return;
            }

            try
            {
                lock (AuditLock)
                {
                    ClanAIEvidenceWriter.AppendLine(
                        AuditPath,
                        DateTime.UtcNow.ToString("O") +
                        " [ClanAI v0.20B] run=" +
                        _runId +
                        " " +
                        message);
                }
            }
            catch (Exception ex)
            {
                ClanAIPostVanilla.WriteExternalLog("EPISODE_AUDIT_WRITE_FAILED type=" + ex.GetType().Name);
            }
        }
    }
}

