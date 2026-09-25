from pathlib import Path
import hashlib
import re
import sys

ROOT = Path(__file__).resolve().parents[2]
BASE = ROOT / "src" / "ClanAI" / "src" / "ClanAI"

policy_path = BASE / "LocalBanditControlPolicy.cs"
patch_path = BASE / "LocalBanditControlPatch.cs"
telemetry_path = BASE / "LocalBanditControlRuntimeTelemetry.cs"
submodule_path = BASE / "SubModule.cs"
phase4b_path = BASE / "LocalManpowerProbabilityPolicy.cs"
phase4c_path = BASE / "TroopQualityProbabilityPolicy.cs"

policy = policy_path.read_text(encoding="utf-8")
patch = patch_path.read_text(encoding="utf-8")
telemetry = telemetry_path.read_text(encoding="utf-8")
submodule = submodule_path.read_text(encoding="utf-8")
phase5_runtime = policy + "\n" + patch + "\n" + telemetry

failed = []

def git_blob_sha(path):
    data = path.read_bytes()
    header = ("blob " + str(len(data))).encode("ascii") + b"\0"
    return hashlib.sha1(header + data).hexdigest()

# Exact private native seam and postfix-only wiring.
for token in (
    "TaleWorlds.CampaignSystem.CampaignBehaviors.BanditSpawnCampaignBehavior",
    'TargetMethodName =\n            "GetSpawnChanceInSettlement"',
    "new Type[] { typeof(Settlement) }",
    "target.IsPrivate",
    "target.IsStatic",
    "target.ReturnType != typeof(float)",
    "harmony.Patch(",
    "postfix: new HarmonyMethod(postfix)",
    "private static void SpawnWeightPostfix(",
    "Settlement __0",
    "ref float __result",
    "float nativeWeight = __result;",
    "LocalBanditCandidateKind.Unsupported ||",
    "!hasSecurity ||",
    "!LocalBanditControlPolicy.IsFinite(security)",
    "return;",
):
    if token not in patch:
        failed.append("missing exact postfix seam token: " + token)

if patch.count("harmony.Patch(") != 1:
    failed.append("Phase 5 must patch exactly one native method")
for forbidden in (
    "prefix:",
    "transpiler:",
    "finalizer:",
    "return false;",
):
    if forbidden in patch:
        failed.append("original-skipping/non-postfix wiring: " + forbidden)

if submodule.count("LocalBanditControlPatch.Install();") != 1:
    failed.append("Phase 5 installer must be called exactly once")

# Town/village-only filtering and Security-only context.
for token in (
    "LocalBanditCandidateKind.Unsupported",
    "settlement.IsTown",
    "LocalBanditCandidateKind.Town",
    "settlement.Town.Security",
    "else if (settlement.IsVillage)",
    "LocalBanditCandidateKind.Village",
    "settlement.Village.Bound",
    "boundSettlement.Town.Security",
):
    if token not in patch:
        failed.append("missing candidate/security filter: " + token)

if "IsHideout" in patch or "Hideout" in patch:
    failed.append("Phase 5 patch must not special-case or modify hideouts")

# Do not broaden into population, hideout spawn, creation, movement or AI order seams.
for forbidden in (
    "SpawnLooters",
    "SpawnBanditsAroundHideout",
    "AddNewHideouts",
    "GetMaxSupportedNumberOfLootersForClan",
    "SelectARandomSettlementForLooterParty",
    "HourlyTickClanEvent",
    "DefaultBanditDensityModel",
):
    if forbidden in phase5_runtime:
        failed.append("broader bandit seam reference: " + forbidden)

# Pure policy is TaleWorlds-free and contains exactly the selected v1 bounds.
if "TaleWorlds" in policy:
    failed.append("pure policy references TaleWorlds")
for required in (
    "MinimumControlMultiplier = 0.75f",
    "MaximumControlMultiplier = 1.25f",
    "MinimumSecurity = 0.0f",
    "MaximumSecurity = 100.0f",
    "NeutralSecurity = 50.0f",
    "1.25f - (0.005f * boundedSecurity)",
):
    if required not in policy:
        failed.append("missing Phase 5 policy bound: " + required)

# No direct world mutation or spawning/destruction/movement/order actions.
for pattern in (
    r"\bMobileParty\.CreateParty\b",
    r"\bBanditPartyComponent\.Create\w*\b",
    r"\bDestroyPartyAction\b",
    r"\bRemoveParty\b",
    r"\.SetMove\w*\s*\(",
    r"\.TargetSettlement\s*=(?!=)",
    r"\.TargetParty\s*=(?!=)",
    r"\.Security\s*=(?!=)",
    r"\.Prosperity\s*=(?!=)",
    r"\.Hearth\s*=(?!=)",
    r"\.Militia\s*=(?!=)",
    r"\.GarrisonParty\s*=(?!=)",
    r"\bDeclareWarAction\b",
    r"\bMakePeaceAction\b",
    r"\bChangeOwnerOfSettlementAction\b",
    r"\bChangeKingdomAction\b",
    r"\.IsInfested\s*=(?!=)",
):
    if re.search(pattern, phase5_runtime):
        failed.append("Phase 5 mutation forbidden pattern: " + pattern)

# V1 must not depend on correlated/external state.
for forbidden in (
    "LocalManpower",
    "TroopQuality",
    "WarStrain",
    "WarState",
    "WarScar",
    "HomeResponsibility",
    "VisualWar",
    "SocialLedger",
    "Prosperity",
    "Hearth",
    "Garrison",
    "Militia",
    "Patrol",
    "Raid",
):
    if forbidden in phase5_runtime:
        failed.append("Phase 5 excluded coupling present: " + forbidden)

# Standalone: no external IO, network, harness, or development path.
for pattern in (
    r"\bFile\.",
    r"\bDirectory\.",
    r"\bProcess\.",
    r"\bHttpClient\b",
    r"\bWebRequest\b",
    r"\bSocket\b",
    r"\bSystem\.Net\b",
):
    if re.search(pattern, phase5_runtime):
        failed.append("Phase 5 external IO/network pattern: " + pattern)

for forbidden in (
    "ChatGPT",
    "Codex",
    "Desktop Commander",
    "DesktopCommander",
    "TestRunner",
    "watchdog",
    "D:\\BannerlordAIResearch",
):
    if forbidden in phase5_runtime:
        failed.append("Phase 5 development runtime dependency: " + forbidden)

if re.search(r'@?"[A-Za-z]:[\\/]', phase5_runtime):
    failed.append("Phase 5 absolute development-machine path")

# The accepted Phase 5 gameplay policy and Phase 4 policies must remain
# byte-for-byte unchanged while runtime telemetry is added.
if git_blob_sha(policy_path) != "7b3d119f274ddf884167f8fd337815d8ba6e6c47":
    failed.append("Phase 5 pure policy blob changed")
if git_blob_sha(phase4b_path) != "f8b21dabb9df38df85fc0f83e6cfcad1a083f713":
    failed.append("Phase 4B pure policy blob changed")
if git_blob_sha(phase4c_path) != "d7b997d7c99e2d768bff97bf10057748547a32e8":
    failed.append("Phase 4C pure policy blob changed")

if failed:
    print("FAIL Phase 5 local bandit control invariants")
    for item in failed:
        print(" -", item)
    sys.exit(1)

print("PASS Phase 5 exact postfix/result-only wiring invariant")
print("PASS Phase 5 town/village-only Security input invariant")
print("PASS Phase 5 no-mutation invariant")
print("PASS Phase 5 standalone-path invariant")
print("PASS Phase 5 pure policy byte-for-byte preservation invariant")
print("PASS Phase 4B/4C pure policy preservation invariant")
