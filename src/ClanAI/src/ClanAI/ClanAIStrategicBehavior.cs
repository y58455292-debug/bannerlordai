using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace ClanAI
{
    public sealed class ClanAIStrategicBehavior : CampaignBehaviorBase
    {
        private static bool _patchInstalled;

        public override void RegisterEvents()
        {
            InstallPostVanillaPatch();

            CampaignEvents.OnSessionLaunchedEvent
                .AddNonSerializedListener(
                    this,
                    new Action<CampaignGameStarter>(
                        OnSessionLaunched));
        }

        public override void SyncData(IDataStore dataStore)
        {
            List<string> memoryLines =
                new List<string>();

            if (dataStore.IsSaving)
            {
                memoryLines =
                    NobleMemory.ExportSaveLines();
            }

            bool found =
                dataStore.SyncData(
                    "ClanAI_NobleMemory_v1",
                    ref memoryLines);

            if (dataStore.IsLoading)
            {
                NobleMemory.ImportSaveLines(
                    found ? memoryLines : null);
            }

            List<string> socialLines =
                new List<string>();

            if (dataStore.IsSaving)
            {
                socialLines =
                    SocialLedger.ExportSaveLines();
            }

            bool socialFound =
                dataStore.SyncData(
                    "ClanAI_SocialLedger_v1",
                    ref socialLines);

            if (dataStore.IsLoading)
            {
                SocialLedger.ImportSaveLines(
                    socialFound ? socialLines : null);
            }
            SocialEpisodeMemory.SyncData(dataStore);
        }

        private void OnSessionLaunched(
            CampaignGameStarter starter)
        {
            ClanAIPostVanilla.Reset();

            LogActiveGlobalAiModel();

            NobleMemory.BeginSession();
            SocialLedger.BeginSession();
            SocialEpisodeMemory.BeginSession();
            VisualWarDecisionLayer.Reset();
            SocialWorldObserver.BeginSession();
            SocialConsequenceProbe.Reset();
            SocialMemoryDecisionProbe.Reset();
        }

        private static void LogActiveGlobalAiModel()
        {
            try
            {
                var active =
                    Campaign.Current == null ||
                    Campaign.Current.Models == null
                        ? null
                        : Campaign.Current.Models
                            .MobilePartyAIModel;

                var mirror =
                    active as DelegatingMobilePartyAIModel;

                ClanAIPostVanilla.WriteExternalLog(
                    "GLOBAL_AI_MODEL_ACTIVE" +
                    " type=" +
                    (
                        active == null
                            ? "<null>"
                            : active.GetType().FullName
                    ) +
                    " mirror=" +
                    (mirror != null) +
                    " inner=" +
                    (
                        mirror == null ||
                        mirror.InnerModel == null
                            ? "<none>"
                            : mirror.InnerModel
                                .GetType()
                                .FullName
                    ));
            }
            catch (Exception ex)
            {
                ClanAIPostVanilla.WriteExternalLog(
                    "GLOBAL_AI_MODEL_ACTIVE_FAILED type=" +
                    ex.GetType().Name +
                    " message=" +
                    ex.Message);
            }
        }

        private static void InstallPostVanillaPatch()
        {
            if (_patchInstalled)
                return;

            Type dispatcherType =
                AccessTools.TypeByName(
                    "TaleWorlds.CampaignSystem.CampaignEventDispatcher");

            if (dispatcherType == null)
                throw new Exception(
                    "ClanAI v0.9: CampaignEventDispatcher not found");

            MethodInfo target =
                AccessTools.Method(
                    dispatcherType,
                    "AiHourlyTick",
                    new Type[]
                    {
                        typeof(MobileParty),
                        typeof(PartyThinkParams)
                    });

            if (target == null)
                throw new Exception(
                    "ClanAI v0.9: AiHourlyTick method not found");

            MethodInfo postfix =
                typeof(ClanAIPostVanilla)
                    .GetMethod(
                        "Postfix",
                        BindingFlags.Static |
                        BindingFlags.Public);

            if (postfix == null)
                throw new Exception(
                    "ClanAI v0.9: postfix not found");

            Harmony harmony =
                new Harmony(
                    "com.bannerlordairesearch.clanai.v09");

            harmony.Patch(
                target,
                postfix: new HarmonyMethod(postfix));

            _patchInstalled = true;

            ClanAIPostVanilla.WriteExternalLog(
                "PATCH_INSTALLED target=" +
                target.DeclaringType.FullName +
                "." +
                target.Name
            );
        }
    }

    public static class ClanAIPostVanilla
    {
        private const string LogPath =
            @"D:\BannerlordAIResearch\Telemetry\BannerlordInspector\logs\inspector.log";

        private const string SessionLogDirectory =
            @"D:\BannerlordAIResearch\Telemetry\ClanAI\sessions";

        private const string Version =
            "v0.20B";

        private static string _sessionLogPath;

        private static long _dispatcherCalls;
        private static long _targeted;
        private static long _candidateLists;
        private static long _candidateTotal;
        private static long _weak;
        private static long _weakRescored;
        private static long _rescoredCandidates;
        private static long _recoveryBoosts;
        private static long _offenseCuts;
        private static long _winnerChanges;

        public static void Reset()
        {
            try
            {
                Directory.CreateDirectory(
                    SessionLogDirectory);

                _sessionLogPath =
                    Path.Combine(
                        SessionLogDirectory,
                        "ClanAI_" +
                        DateTime.UtcNow.ToString(
                            "yyyyMMdd_HHmmss_fff") +
                        "_v020B.log");
            }
            catch
            {
                _sessionLogPath = null;
            }

            _dispatcherCalls = 0;
            _targeted = 0;
            _candidateLists = 0;
            _candidateTotal = 0;
            _weak = 0;
            _weakRescored = 0;
            _rescoredCandidates = 0;
            _recoveryBoosts = 0;
            _offenseCuts = 0;
            _winnerChanges = 0;

            WriteExternalLog(
                "SESSION_START " +
                "dispatcherCalls=0 " +
                "targeted=0 " +
                "candidateLists=0 " +
                "candidateTotal=0 " +
                "weak=0 " +
                "weakRescored=0 " +
                "rescoredCandidates=0 " +
                "recoveryBoosts=0 " +
                "offenseCuts=0 " +
                "winnerChanges=0"
            );

            WriteExternalLog(
                "SESSION_LOG path=" +
                (_sessionLogPath ?? "<unavailable>"));
        }

        // __0 and __1 bind by ORIGINAL ARGUMENT INDEX.
        // This avoids depending on TaleWorlds' parameter names.
        public static void Postfix(
            MobileParty __0,
            PartyThinkParams __1)
        {
            _dispatcherCalls++;

            MobileParty party = __0;
            PartyThinkParams thinkParams = __1;

            if (!ClanAISwitch.Enabled ||
                party == null ||
                thinkParams == null ||
                !party.IsLordParty ||
                party.LeaderHero == null ||
                party.MapFaction == null ||
                party.Army != null)
            {
                return;
            }

            int allCount =
                thinkParams.AIBehaviorScores.Count;

            if (allCount > 0)
            {
                SocialMemoryDecisionProbe.Analyze(
                    party,
                    thinkParams);

                VisualWarDecisionLayer.Apply(
                    party,
                    thinkParams);
            }

            string faction =
                party.MapFaction.Name.ToString();

            if (faction != "Sturgia" &&
                faction != "Vlandia")
            {
                return;
            }

            _targeted++;

            int count =
                allCount;

            _candidateTotal += count;

            if (count > 0)
            {
                _candidateLists++;
                NobleMemory.Observe(party, thinkParams);
                SocialConsequenceProbe.Observe(party, thinkParams);
                SocialLedger.Observe(party, thinkParams);
                SocialWorldObserver.Observe(party);
            }

            int beforeWinner =
                FindBestIndex(thinkParams);

            string beforeBehavior =
                GetBehaviorName(
                    thinkParams,
                    beforeWinner);

            float readiness =
                party.PartySizeRatio;

            int foodDays =
                party.GetNumDaysForFoodToLast();

            bool weak =
                readiness < 0.72f ||
                foodDays < 3;

            if (weak)
            {
                _weak++;

                var changes =
                    new List<
                        ValueTuple<
                            AIBehaviorData,
                            float>>();

                int thisRecoveryBoosts = 0;
                int thisOffenseCuts = 0;

                for (int i = 0;
                     i < count;
                     i++)
                {
                    var entry =
                        thinkParams
                            .AIBehaviorScores[i];

                    AIBehaviorData data =
                        entry.Item1;

                    float score =
                        entry.Item2;

                    if (score <= 0f)
                        continue;

                    float factor = 1f;

                    if (data.AiBehavior ==
                        AiBehavior.GoToSettlement)
                    {
                        factor = 1.35f;
                        thisRecoveryBoosts++;
                    }
                    else if (
                        data.AiBehavior ==
                            AiBehavior.RaidSettlement ||
                        data.AiBehavior ==
                            AiBehavior.BesiegeSettlement ||
                        data.AiBehavior ==
                            AiBehavior.AssaultSettlement ||
                        data.AiBehavior ==
                            AiBehavior.EngageParty)
                    {
                        factor = 0.45f;
                        thisOffenseCuts++;
                    }

                    if (Math.Abs(
                        factor - 1f) < 0.001f)
                    {
                        continue;
                    }

                    changes.Add(
                        new ValueTuple<
                            AIBehaviorData,
                            float>(
                                data,
                                score * factor
                            )
                    );
                }

                if (changes.Count > 0)
                    _weakRescored++;

                _rescoredCandidates +=
                    changes.Count;

                _recoveryBoosts +=
                    thisRecoveryBoosts;

                _offenseCuts +=
                    thisOffenseCuts;

                for (int i = 0;
                     i < changes.Count;
                     i++)
                {
                    AIBehaviorData data =
                        changes[i].Item1;

                    float score =
                        changes[i].Item2;

                    thinkParams.SetBehaviorScore(
                        in data,
                        score
                    );
                }

                int afterWinner =
                    FindBestIndex(thinkParams);

                string afterBehavior =
                    GetBehaviorName(
                        thinkParams,
                        afterWinner);

                if (beforeWinner != afterWinner)
                {
                    _winnerChanges++;

                    WriteExternalLog(
                        "WINNER_CHANGED " +
                        "party=" +
                        party.Name.ToString() +
                        " faction=" +
                        faction +
                        " readiness=" +
                        readiness.ToString("0.000") +
                        " foodDays=" +
                        foodDays +
                        " candidates=" +
                        count +
                        " before=" +
                        beforeBehavior +
                        " after=" +
                        afterBehavior
                    );
                }
            }

            if ((_targeted % 25) == 0)
            {
                WriteSummary();
            }
        }

        private static int FindBestIndex(
            PartyThinkParams thinkParams)
        {
            int bestIndex = -1;
            float bestScore =
                float.MinValue;

            for (int i = 0;
                 i <
                 thinkParams
                    .AIBehaviorScores.Count;
                 i++)
            {
                float score =
                    thinkParams
                        .AIBehaviorScores[i]
                        .Item2;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestIndex = i;
                }
            }

            return bestIndex;
        }

        private static string GetBehaviorName(
            PartyThinkParams thinkParams,
            int index)
        {
            if (index < 0 ||
                index >=
                thinkParams
                    .AIBehaviorScores.Count)
            {
                return "NONE";
            }

            return thinkParams
                .AIBehaviorScores[index]
                .Item1
                .AiBehavior
                .ToString();
        }

        private static void WriteSummary()
        {
            WriteExternalLog(
                "SUMMARY " +
                "dispatcherCalls=" +
                _dispatcherCalls +
                " targeted=" +
                _targeted +
                " candidateLists=" +
                _candidateLists +
                " candidateTotal=" +
                _candidateTotal +
                " weak=" +
                _weak +
                " weakRescored=" +
                _weakRescored +
                " rescoredCandidates=" +
                _rescoredCandidates +
                " recoveryBoosts=" +
                _recoveryBoosts +
                " offenseCuts=" +
                _offenseCuts +
                " winnerChanges=" +
                _winnerChanges
            );
        }

        public static void WriteExternalLog(
            string text)
        {
            try
            {
                string line =
                    DateTime.UtcNow
                        .ToString("O") +
                    " [ClanAI " +
                    Version +
                    "] " +
                    text;

                AppendLine(
                    LogPath,
                    line);

                if (!string.IsNullOrEmpty(
                    _sessionLogPath))
                {
                    AppendLine(
                        _sessionLogPath,
                        line);
                }
            }
            catch
            {
                // Diagnostics must never
                // interfere with campaign AI.
            }
        }

        private static void AppendLine(
            string path,
            string line)
        {
            ClanAIEvidenceWriter.AppendLine(
                path,
                line);
        }
    }
}










