from pathlib import Path
import hashlib
import re
import sys

ROOT = Path(__file__).resolve().parents[2]
BASE = ROOT / "src" / "ClanAI" / "src" / "ClanAI"

lm_policy_path = BASE / "LocalManpowerProbabilityPolicy.cs"
lm_policy = lm_policy_path.read_text(encoding="utf-8")
quality_policy = (BASE / "TroopQualityProbabilityPolicy.cs").read_text(encoding="utf-8")
eligibility = (BASE / "TroopQualityVolunteerEligibility.cs").read_text(encoding="utf-8")
wrapper = (BASE / "LocalManpowerVolunteerModel.cs").read_text(encoding="utf-8")
submodule = (BASE / "SubModule.cs").read_text(encoding="utf-8")

phase4c_runtime = quality_policy + "\n" + eligibility + "\n" + wrapper
failed = []

def git_blob_sha1(path: Path) -> str:
    data = path.read_bytes()
    return hashlib.sha1(
        b"blob " + str(len(data)).encode("ascii") + b"\0" + data
    ).hexdigest()

# Phase 4B pure policy must remain byte-for-byte unchanged.
expected_phase4b_policy_blob = "f8b21dabb9df38df85fc0f83e6cfcad1a083f713"
actual_phase4b_policy_blob = git_blob_sha1(lm_policy_path)
if actual_phase4b_policy_blob != expected_phase4b_policy_blob:
    failed.append(
        "Phase 4B policy changed: " +
        actual_phase4b_policy_blob)

# No second VolunteerModel wrapper: only the existing selected wrapper derives from it.
wrapper_types = []
for path in BASE.glob("*.cs"):
    text = path.read_text(encoding="utf-8", errors="ignore")
    if re.search(r"class\s+\w+\s*\n?\s*:\s*VolunteerModel\b", text):
        wrapper_types.append(path.name)
if wrapper_types != ["LocalManpowerVolunteerModel.cs"]:
    failed.append("unexpected VolunteerModel wrapper set: " + repr(wrapper_types))

for token in (
    "starter.GetModel<VolunteerModel>()",
    "new LocalManpowerVolunteerModel(",
    "starter.AddModel<VolunteerModel>(",
):
    if token not in submodule:
        failed.append("selected-model registration changed/missing: " + token)

# Inner production still exactly once.
if wrapper.count("_inner.GetDailyVolunteerProductionProbability(") != 1:
    failed.append("inner production method must be called exactly once in wrapper source")

# Empty path must remain Phase 4B.
for token in (
    "return EvaluateEmptySlot(",
    "private static float EvaluateEmptySlot(",
    "LocalManpowerProbabilityPolicy.Evaluate(",
    "LocalManpowerProbabilityPolicy.IsFinite(",
):
    if token not in wrapper:
        failed.append("Phase 4B empty path token missing: " + token)

# Occupied quality branch must use the exact native eligibility mirror and inner max tier.
for token in (
    "int maxVolunteerTier =",
    "_inner.MaxVolunteerTier",
    "TroopQualityVolunteerEligibility",
    ".IsNativeUpgradeEligible(",
    "TroopQualityProbabilityPolicy",
    ".ShouldApplyToOccupiedSlot(",
    "return EvaluateOccupiedQualitySlot(",
):
    if token not in wrapper:
        failed.append("Phase 4C wrapper branch missing: " + token)

for token in (
    "volunteer.UpgradeTargets",
    "volunteer.UpgradeTargets.Length == 0",
    "volunteer.Tier",
    "currentTier < maxVolunteerTier",
):
    if token not in (eligibility + "\n" + quality_policy):
        failed.append("native quality eligibility token missing: " + token)

# Native eligibility must only read target/tier state, never assign it.
if re.search(r"UpgradeTargets\s*=(?!=)", eligibility):
    failed.append("UpgradeTargets assignment in eligibility helper")
if re.search(r"\.Tier\s*=(?!=)", eligibility):
    failed.append("tier assignment in eligibility helper")

# Quality multiplier must not take notable power/tier/culture/tree inputs.
if "TaleWorlds" in quality_policy:
    failed.append("pure Phase 4C policy references TaleWorlds")

eval_match = re.search(
    r"internal static TroopQualityProbabilityResult Evaluate\((.*?)\)\s*\{",
    quality_policy,
    re.S,
)
if not eval_match:
    failed.append("Phase 4C Evaluate signature missing")
else:
    signature = eval_match.group(1)
    parameter_names = []
    for part in signature.split(","):
        tokens = part.strip().split()
        if tokens:
            parameter_names.append(tokens[-1].lower())
    for name in parameter_names:
        for forbidden in ("power", "tier", "culture", "troop", "tree", "target"):
            if forbidden in name:
                failed.append(
                    "forbidden quality multiplier parameter: " + name)

for required in (
    r"MinimumQualityMultiplier\s*=\s*0\.50f",
    r"MaximumQualityMultiplier\s*=\s*1\.00f",
    r"LocalManpowerProbabilityPolicy\s*\.PopulationFactor\s*\(",
    r"LocalManpowerProbabilityPolicy\s*\.SecurityFactor\s*\(",
    r"LocalManpowerProbabilityPolicy\s*\.AcuteDisruptionFactor",
):
    if not re.search(required, quality_policy):
        failed.append("quality policy requirement missing: " + required)

# Phase 4C must not mutate volunteer slots, rosters, XP, world state, or troop trees.
for pattern in (
    r"VolunteerTypes\s*\[[^\]]+\]\s*=(?!=)",
    r"\bSetElementAtIndex\s*\(",
    r"\bAddToCounts\s*\(",
    r"\bRemoveTroop\s*\(",
    r"\bAddXpToTroop\s*\(",
    r"\bAddXpToTroopAtIndex\s*\(",
    r"\bSetElementXp\s*\(",
    r"UpgradeTargets\s*=(?!=)",
    r"\.Culture\s*=(?!=)",
    r"\.\s*(?:Militia|Prosperity|Hearth|Security)\s*=(?!=)",
    r"\.\s*GarrisonParty\s*=(?!=)",
    r"\bGiveGoldAction\b",
    r"\bChangeOwnerOfSettlementAction\b",
    r"\bDeclareWarAction\b",
    r"\bMakePeaceAction\b",
    r"\bMBRandom\b",
    r"\bRandom\s*\(",
):
    if re.search(pattern, phase4c_runtime):
        failed.append("Phase 4C forbidden mutation/RNG pattern: " + pattern)

# Explicit exclusions.
for forbidden in (
    "WarStrain",
    "WarStateTracker",
    "HomeResponsibility",
    ".IsRaided",
    "Loyalty",
    "GarrisonParty",
    "Militia",
):
    if forbidden in (quality_policy + "\n" + eligibility):
        failed.append("Phase 4C excluded coupling present: " + forbidden)

# No external IO / dev runtime dependency / absolute path.
for pattern in (
    r"\bFile\.",
    r"\bDirectory\.",
    r"\bProcess\.",
    r"\bHttpClient\b",
    r"\bWebRequest\b",
    r"\bSocket\b",
):
    if re.search(pattern, phase4c_runtime):
        failed.append("Phase 4C external IO pattern: " + pattern)

for forbidden in (
    "ChatGPT",
    "Codex",
    "Desktop Commander",
    "DesktopCommander",
    "TestRunner",
    "watchdog",
    "D:\\BannerlordAIResearch",
):
    if forbidden in phase4c_runtime:
        failed.append("Phase 4C dev runtime dependency: " + forbidden)

if re.search(r'@?"[A-Za-z]:[\\/]', phase4c_runtime):
    failed.append("Phase 4C absolute development-machine path")

if failed:
    print("FAIL Phase 4C troop quality invariants")
    for item in failed:
        print(" -", item)
    sys.exit(1)

print("PASS Phase 4C selected-wrapper branch invariant")
print("PASS Phase 4C native eligibility read-only invariant")
print("PASS Phase 4C no-mutation invariant")
print("PASS Phase 4C standalone-path invariant")
print("PASS Phase 4B policy byte-for-byte preservation invariant")
