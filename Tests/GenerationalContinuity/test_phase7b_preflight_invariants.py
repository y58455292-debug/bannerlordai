from pathlib import Path
import hashlib
import re

ROOT = Path(__file__).resolve().parents[2]
BASE = ROOT / "src" / "ClanAI" / "src" / "ClanAI"

telemetry = (BASE / "GenerationalContinuityPreflightBehavior.cs").read_text(encoding="utf-8")
submodule = (BASE / "SubModule.cs").read_text(encoding="utf-8")
social = (BASE / "SocialLedger.cs").read_text(encoding="utf-8")
duty = (BASE / "CompanionDutyMemory.cs").read_text(encoding="utf-8")
experience = (BASE / "CompanionExperienceMemory.cs").read_text(encoding="utf-8")
negative = (BASE / "CompanionNegativeOutcomeMemory.cs").read_text(encoding="utf-8")
causal = (BASE / "SocialMemoryCausalLayer.cs").read_text(encoding="utf-8")
loyalty = (BASE / "SocialLoyaltyPatch.cs").read_text(encoding="utf-8")

failed = []

required = [
    "BeforeHeroKilledEvent",
    "HeroKilledEvent",
    "OnClanLeaderChangedEvent",
    "RulingClanChanged",
    "OnBeforeSaveEvent",
    "OnSaveOverEvent",
    "OnGameLoadFinishedEvent",
    "GENCONT_RULER_SNAPSHOT",
    "GENCONT_NATIVE_HERO_DEATH",
    "GENCONT_NATIVE_CLAN_LEADER_CHANGED",
    "GENCONT_NATIVE_RULING_CLAN_CHANGED",
    "GENCONT_HERO_MEMORY_RESOLUTION",
    "targetRulerHeroId=",
    "rulingClanId=",
    "kingdomId=",
    "kingdomCulture=",
    "currentClanLeaderId=",
    "clanOwnedSettlements=",
    "spouseId=",
    "childIds=",
    "familyIds=",
    "kingdomContinuitySuccessionCount=",
    "clanLossMemoryCount=",
    "nobleMemoryCount=",
    "socialLedgerActorCount=",
    "companionDutyMemoryCount=",
    "companionExperienceMemoryCount=",
    "companionNegativeOutcomeCount=",
    "dynastyBranchActorEpisodeCount=",
    "warScarCount=",
    "objectiveIds=",
    "crossHeroMemoryAppliedDetected=",
    "mutationByClanAI=False",
]
for token in required:
    if token not in telemetry:
        failed.append("missing telemetry token: " + token)

forbidden = [
    "KillCharacterAction.Apply",
    "ChangeClanLeaderAction",
    "ChangeRulingClan",
    "ChangeOwnerOfSettlementAction.Apply",
    "ChangeRelationAction",
    "ChangeKingdomAction.Apply",
    ".Age =",
    "BirthDay =",
    "DeathDay =",
    "IsDead =",
    ".Leader =",
    "BuildingsInProgress",
    "dataStore.SyncData",
    "[Saveable",
    "System.IO",
    "File.",
    "Directory.",
    "HttpClient",
    "System.Net",
    "Socket",
    "Process.",
    "ChatGPT",
    "Codex",
    "TestRunner",
    "watchdog",
]
for token in forbidden:
    if token in telemetry:
        failed.append("forbidden mutation/persistence/dependency token: " + token)

if re.search(r'@?"[A-Za-z]:[\\/]', telemetry):
    failed.append("absolute development-machine path in Phase 7B telemetry")

for pattern, label in [
    (r"\.RulingClan\s*=(?!=)", "RulingClan assignment"),
    (r"\.OwnerClan\s*=(?!=)", "OwnerClan assignment"),
    (r"\.Leader\s*=(?!=)", "Leader assignment"),
]:
    if re.search(pattern, telemetry):
        failed.append("forbidden lifecycle/political assignment: " + label)

if submodule.count("new GenerationalContinuityPreflightBehavior()") != 1:
    failed.append("preflight behavior must be installed exactly once")

for name, text in [
    ("SocialLedger", social),
    ("CompanionDutyMemory", duty),
    ("CompanionExperienceMemory", experience),
    ("CompanionNegativeOutcomeMemory", negative),
    ("SocialMemoryCausalLayer", causal),
    ("SocialLoyaltyPatch", loyalty),
]:
    if "GenerationalContinuityRuntimeTelemetry" not in text:
        failed.append("missing identity telemetry call in " + name)

expected_sha256 = {
    "CivicProjectSelectionPolicy.cs": "2AE3008F7FF5E3F13135425B4B970C0C9E5F24869228F43DCDB19BEF215C41A3",
    "LocalManpowerProbabilityPolicy.cs": "CCA6ABF387D98F02E97E54FFD1D68B0E23AABE6450672CFAED30AC8AB67FFD49",
    "TroopQualityProbabilityPolicy.cs": "F1B683AD4169054F73816ED052C51FCD4F46412D74CB0AC6E6BFA274E72A6534",
    "LocalBanditControlPolicy.cs": "890DED9CAA10B1A4E84ACE80FBE5788E96E00FCE17654325E5EB94B5494A3FC2",
}
for name, expected in expected_sha256.items():
    actual = hashlib.sha256((BASE / name).read_bytes()).hexdigest().upper()
    if actual != expected:
        failed.append(f"preserved source changed: {name} {actual}")

dynasty = (BASE / "DynastyBranchEpisodeMemory.cs").read_text(encoding="utf-8")
for token in [
    '"ClanAI_DynastyBranchEpisodes_v1"',
    '"D1"',
    '"D2"',
    '"D2|"',
    'RecordIncidentOpened',
    'RecordIncidentChoice',
    'BuildLatestRetrievalReceipt',
    'BuildLatestChoiceRetrievalReceipt',
    'SelectLatestActorEpisode',
    'SelectLatestActorChoice',
]:
    if token not in dynasty:
        failed.append("Phase 7A/7B dynasty actor/save semantic missing: " + token)

if failed:
    for item in failed:
        print("FAIL", item)
    raise SystemExit(1)

print("PASS Phase 7B succession preflight observation-only invariant")
print("PASS Phase 7B no save-schema invariant")
print("PASS Phase 7B no lifecycle/succession mutation invariant")
print("PASS Phase 7B hero-memory identity-resolution invariant")
print("PASS Phase 7A/7B DynastyBranchEpisodeMemory actor/save semantics preserved")
print("PASS Phase 4B/4C, Phase 5 and Phase 6 policy preservation invariant")

