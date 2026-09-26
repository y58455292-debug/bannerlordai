from pathlib import Path
import hashlib
import re

ROOT = Path(__file__).resolve().parents[2]
BASE = ROOT / "src" / "ClanAI" / "src" / "ClanAI"

policy_path = BASE / "CivicProjectSelectionPolicy.cs"
wrapper_path = BASE / "CivicProjectBuildingScoreCalculationModel.cs"
submodule_path = BASE / "SubModule.cs"
telemetry_path = BASE / "CivicProjectRuntimeTelemetry.cs"

policy = policy_path.read_text(encoding="utf-8")
wrapper = wrapper_path.read_text(encoding="utf-8")
submodule = submodule_path.read_text(encoding="utf-8")
telemetry = telemetry_path.read_text(encoding="utf-8")
gameplay_runtime = policy + "\n" + wrapper
runtime = gameplay_runtime + "\n" + telemetry

failed = []

def git_blob_sha(path: Path) -> str:
    data = path.read_bytes()
    header = f"blob {len(data)}\0".encode("ascii")
    return hashlib.sha1(header + data).hexdigest()

# Pure policy must remain game-assembly-free and use only the selected inputs.
if "TaleWorlds" in policy:
    failed.append("pure policy references TaleWorlds")

for token in (
    "bool contextValid",
    "bool isNpcTown",
    "bool isConstructionIdle",
    "bool hasFestivalAndGamesProject",
    "float loyalty",
    "float nativeRebelliousThreshold",
    "loyalty >= nativeRebelliousThreshold",
):
    if token not in policy:
        failed.append("missing pure-policy boundary: " + token)

for forbidden in (
    "prosperity",
    "security",
    "militia",
    "garrison",
    "raid",
    "patrol",
    "warstrain",
    "war state",
    "notable",
    "governor",
    "social",
):
    if forbidden in policy.lower():
        failed.append("unrelated policy input/system: " + forbidden)

# Wrapper must delegate first, preserve ordinary selection, and use strong native identities.
if wrapper.count("_inner.GetNextDailyBuilding(") != 1:
    failed.append("inner daily selector is not called exactly once")

for token in (
    "return _inner.GetNextBuilding(town);",
    "inner.GetType() ==",
    "typeof(DefaultBuildingScoreCalculationModel)",
    "DefaultBuildingTypes",
    ".SettlementDailyFestivalAndGames",
    ".SettlementLoyaltyModel",
    ".RebelliousStateStartLoyaltyThreshold",
    "town.OwnerClan != Clan.PlayerClan",
    "town.BuildingsInProgress.Count == 0",
    "town.CurrentDefaultBuilding",
    "object.ReferenceEquals(",
    "return festivalAndGames;",
    "return nativeResult;",
):
    if token not in wrapper:
        failed.append("missing wrapper/native-authority token: " + token)

# SubModule must install once and refuse duplicate/foreign wrapping.
if submodule.count("InstallCivicProjectBuildingScoreCalculationModel(starter);") != 1:
    failed.append("Phase 6 model installer call count")
for token in (
    "current is CivicProjectBuildingScoreCalculationModel",
    ".SupportsInnerModel(current)",
    "starter.AddModel<BuildingScoreCalculationModel>(",
    "object.ReferenceEquals(",
):
    if token not in submodule:
        failed.append("missing installer compatibility token: " + token)

# Gameplay Phase 6 source must not mutate world state or bypass the native commit.
for forbidden in (
    "new Building(",
    "BuildingHelper.ChangeDefaultBuilding",
    "Town.Loyalty =",
    ".Loyalty =",
    "CurrentDefaultBuilding =",
    "BuildingsInProgress.Enqueue",
    "BuildingsInProgress.Dequeue",
    "BuildingsInProgress.Clear",
    "Buildings.Add(",
    "SetBuildingProgress",
    "ChangeRelationAction",
    ".Prosperity =",
    ".Security =",
    ".Militia =",
    "ChangeOwner",
    "ChangeGovernorAction",
    "MobileParty.CreateParty",
    "BanditPartyComponent.Create",
    "DestroyPartyAction",
    "[Saveable",
    "SyncData(",
):
    if forbidden in gameplay_runtime:
        failed.append("Phase 6 direct mutation/save token: " + forbidden)

# Runtime telemetry may observe the exact native commit seam, but only through a postfix.
for token in (
    "typeof(BuildingHelper)",
    "nameof(BuildingHelper.ChangeDefaultBuilding)",
    "nameof(ChangeDefaultBuildingPostfix)",
    "postfix: new HarmonyMethod(postfix)",
    "source=native-BuildingHelper.ChangeDefaultBuilding",
    "mutationByClanAI=False",
):
    if token not in telemetry:
        failed.append("missing observation-only telemetry token: " + token)

for forbidden in (
    "prefix:",
    "transpiler:",
    "finalizer:",
    "new Building(",
    "Town.Loyalty =",
    ".Loyalty =",
    "CurrentDefaultBuilding =",
    "BuildingsInProgress.Enqueue",
    "BuildingsInProgress.Dequeue",
    "BuildingsInProgress.Clear",
    "Buildings.Add(",
    "SetBuildingProgress",
    "ChangeRelationAction",
    ".Prosperity =",
    ".Security =",
    ".Militia =",
    "ChangeOwner",
    "ChangeGovernorAction",
    "[Saveable",
    "SyncData(",
):
    if forbidden in telemetry:
        failed.append("Phase 6 telemetry mutation/control token: " + forbidden)

# Standalone runtime path: no direct external IO, network, processes, harnesses, or dev paths.
for forbidden in (
    "System.IO",
    "File.",
    "Directory.",
    "HttpClient",
    "WebRequest",
    "Socket",
    "System.Net",
    "Process.",
    "ChatGPT",
    "Codex",
    "Desktop Commander",
    "DesktopCommander",
    "TestRunner",
    "watchdog",
):
    if forbidden in runtime:
        failed.append("Phase 6 standalone dependency: " + forbidden)

if re.search(r'@?"[A-Za-z]:[\\/]', runtime):
    failed.append("Phase 6 absolute development-machine path")

if hashlib.sha256(policy_path.read_bytes()).hexdigest().upper() != "D1EE6FEFF19D696ED66D99BEF2175A73F1F4198982EFBE1279F22A5B48A31D92":
    failed.append("Phase 6 pure policy byte-for-byte preservation invariant")

# Preserve accepted policy blobs from Phase 4B, Phase 4C and Phase 5.
expected_blobs = {
    "LocalManpowerProbabilityPolicy.cs": "f8b21dabb9df38df85fc0f83e6cfcad1a083f713",
    "TroopQualityProbabilityPolicy.cs": "d7b997d7c99e2d768bff97bf10057748547a32e8",
    "LocalBanditControlPolicy.cs": "7b3d119f274ddf884167f8fd337815d8ba6e6c47",
    # Phase 8B adds only the explicit evidence-profile telemetry gate;
    # the gameplay postfix and policy call remain unchanged.
    "LocalBanditControlPatch.cs": "e6ac790f10ccbaa263a8d5471425fba626064886",
}
for name, expected in expected_blobs.items():
    actual = git_blob_sha(BASE / name)
    if actual != expected:
        failed.append(f"preserved source changed: {name} {actual}")

if failed:
    for item in failed:
        print("FAIL", item)
    raise SystemExit(1)

print("PASS Phase 6 civic-project NPC-only/idle-town wiring invariant")
print("PASS Phase 6 civic-project no-mutation invariant")
print("PASS Phase 6 civic-project standalone-path invariant")
print("PASS Phase 6 civic-project observation-only telemetry invariant")
print("PASS Phase 6 pure policy byte-for-byte preservation invariant")
print("PASS Phase 4B/4C and Phase 5 preservation invariant")


