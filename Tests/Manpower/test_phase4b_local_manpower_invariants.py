from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[2]
BASE = ROOT / "src" / "ClanAI" / "src" / "ClanAI"

policy = (BASE / "LocalManpowerProbabilityPolicy.cs").read_text(encoding="utf-8")
wrapper = (BASE / "LocalManpowerVolunteerModel.cs").read_text(encoding="utf-8")
submodule = (BASE / "SubModule.cs").read_text(encoding="utf-8")
combined = policy + "\n" + wrapper

failed = []

# Selected-model wiring is explicit and wraps Bannerlord's already-selected model.
for token in (
    "starter.GetModel<VolunteerModel>()",
    "new LocalManpowerVolunteerModel(",
    "starter.AddModel<VolunteerModel>(",
    "starter.GetModel<VolunteerModel>()",
    "InstallLocalManpowerVolunteerModel(starter);",
):
    if token not in submodule:
        failed.append("missing selected-model wiring: " + token)

if submodule.count("InstallLocalManpowerVolunteerModel(starter);") != 1:
    failed.append("local manpower installer must be called exactly once")

# VolunteerModel delegation: every non-production member must call the exact inner member.
delegates = (
    "_inner.MaxVolunteerTier",
    "_inner.MaximumIndexHeroCanRecruitFromHero(",
    "_inner.MaximumIndexGarrisonCanRecruitFromHero(",
    "_inner.GetBasicVolunteer(",
    "_inner.CanHaveRecruits(",
)
for token in delegates:
    if token not in wrapper:
        failed.append("missing native delegation: " + token)

if wrapper.count("_inner.GetDailyVolunteerProductionProbability(") != 1:
    failed.append("inner production method must appear exactly once")

# Empty-slot read is allowed; direct mutation is forbidden.
if "hero.VolunteerTypes[index] == null" not in wrapper:
    failed.append("empty-slot check missing")
if re.search(r"VolunteerTypes\s*\[[^\]]+\]\s*=(?!=)", combined):
    failed.append("direct VolunteerTypes mutation")
if re.search(r"VolunteerTypes\s*\[[^\]]+\]\s*\+=", combined):
    failed.append("direct VolunteerTypes increment")

# Town/village source selection must remain native.
for token in (
    "settlement.Town.GetProsperityLevel()",
    "settlement.Village.GetProsperityLevel()",
    "settlement.Town.Security",
    "settlement.Village.Bound",
    "settlement.IsUnderRaid",
    "settlement.IsUnderSiege",
):
    if token not in wrapper:
        failed.append("missing native local input: " + token)

# Occupied slots must return native without entering local context calculations.
occupied_pattern = re.compile(
    r"if\s*\(!slotIsEmpty\)\s*\{\s*return\s+LocalManpowerProbabilityPolicy\s*\.SanitizeProbability\(nativeProbability\);",
    re.S,
)
if not occupied_pattern.search(wrapper):
    failed.append("occupied slot is not an immediate native passthrough")

# Wrapper must not introduce its own RNG or direct world mutation.
for forbidden in (
    r"\bMBRandom\b",
    r"\bRandom\s*\(",
    r"\bAddToCounts\s*\(",
    r"\bAddElementToMemberRoster\s*\(",
    r"\bRemoveTroop\s*\(",
    r"\bWoundTroop\s*\(",
    r"\bGiveGoldAction\b",
    r"\bChangeOwnerOfSettlementAction\b",
    r"\bDeclareWarAction\b",
    r"\bMakePeaceAction\b",
    r"\.\s*(?:Militia|Prosperity|Hearth|Security)\s*=(?!=)",
    r"\.\s*GarrisonParty\s*=(?!=)",
):
    if re.search(forbidden, combined):
        failed.append("mutation/RNG forbidden pattern: " + forbidden)

# V1 exclusions: no War Strain, political state, troop-quality or culture factor input.
for forbidden in (
    ".IsRaided",
    "WarStrain",
    "WarStateTracker",
    "HomeResponsibility",
    "Social",
    ".Culture",
    ".Tier",
    "GarrisonParty",
    "Militia",
    "Loyalty",
):
    if forbidden in combined:
        failed.append("v1 excluded coupling present: " + forbidden)

# No external IO or development-tool/runtime dependencies in new Phase 4B source.
for forbidden in (
    r"\bFile\.",
    r"\bDirectory\.",
    r"\bProcess\.",
    r"\bHttpClient\b",
    r"\bWebRequest\b",
    r"\bSocket\b",
):
    if re.search(forbidden, combined):
        failed.append("external IO in Phase 4B source: " + forbidden)

for forbidden in (
    "ChatGPT",
    "Codex",
    "Desktop Commander",
    "DesktopCommander",
    "TestRunner",
    "watchdog",
    "D:\\BannerlordAIResearch",
):
    if forbidden in combined:
        failed.append("development runtime dependency: " + forbidden)

if re.search(r'@?"[A-Za-z]:[\\/]', combined):
    failed.append("absolute development-machine path in Phase 4B source")

# Policy shape stays numeric/pure.
if "TaleWorlds" in policy:
    failed.append("pure policy references TaleWorlds")
for required in (
    "MinimumLocalMultiplier = 0.35f",
    "HighPopulationFactor = 1.00f",
    "MidPopulationFactor = 0.95f",
    "LowPopulationFactor = 0.80f",
    "MinimumSecurityFactor = 0.80f",
    "FullSecurityThreshold = 50.0f",
    "AcuteDisruptionFactor = 0.50f",
):
    if required not in policy:
        failed.append("missing policy bound: " + required)

if failed:
    print("FAIL Phase 4B Local Manpower wiring/no-mutation/standalone invariant")
    for item in failed:
        print(" -", item)
    sys.exit(1)

print("PASS Phase 4B Local Manpower selected-model delegation invariant")
print("PASS Phase 4B Local Manpower no-mutation invariant")
print("PASS Phase 4B Local Manpower standalone-path invariant")
