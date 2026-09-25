from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[2]
BASE = ROOT / "src" / "ClanAI" / "src" / "ClanAI"
OBSERVER = (BASE / "Phase4ARecoveryObserverBehavior.cs").read_text(encoding="utf-8")
POLICY = (BASE / "Phase4ARecoveryClassificationPolicy.cs").read_text(encoding="utf-8")
SUBMODULE = (BASE / "SubModule.cs").read_text(encoding="utf-8")
combined = OBSERVER + "\n" + POLICY + "\n" + SUBMODULE

forbidden = (
    "AddElementToMemberRoster",
    "AddMembers(",
    "RemoveTroop(",
    "SetElementNumber(",
    "SetElementWoundedNumber(",
    "WoundTroop(",
    "VolunteerTypes[",
    "GarrisonParty =",
    "SetMoveGoToSettlement",
    "SetMoveGoToPoint",
    "SetMoveEngageParty",
    "SetMovePatrolAroundSettlement",
    "SetMoveBesiegeSettlement",
    "AIBehaviorScores.Add",
    "SetBehaviorScore",
    "ChangeOwnerOfSettlementAction",
    "ChangeKingdomAction",
    "DeclareWarAction",
    "MakePeaceAction",
    "Gold =",
    "Food =",
)

failed = [item for item in forbidden if item in combined]

if re.search(r"\.VolunteerTypes\s*=(?!=)", combined):
    failed.append("VolunteerTypes assignment")
if re.search(r"\.GarrisonParty\s*=(?!=)", combined):
    failed.append("GarrisonParty assignment")

absolute_path = re.search(r'@"[A-Za-z]:\\', combined)
if absolute_path:
    failed.append("absolute-development-path")

if "D:\\BannerlordAIResearch" in combined:
    failed.append("development-root-reference")

for tool_name in (
    "ChatGPT",
    "Codex",
    "Desktop Commander",
    "DesktopCommander",
    "TestRunner",
    "watchdog",
):
    if tool_name in combined:
        failed.append("development-tool-runtime-dependency:" + tool_name)

if "public override void SyncData(IDataStore dataStore)" not in OBSERVER:
    failed.append("missing-empty-syncdata")
else:
    sync = OBSERVER.split("public override void SyncData(IDataStore dataStore)", 1)[1]
    sync_body = sync.split("}", 1)[0]
    if "SyncData(" in sync_body or "dataStore." in sync_body:
        failed.append("observer-persistence-introduced")

if failed:
    print("FAIL Phase 4A recovery no-mutation/standalone invariant")
    for item in failed:
        print(" -", item)
    sys.exit(1)

print("PASS Phase 4A recovery no-mutation/standalone invariant")
print("observer introduces no troop/recruit/garrison/order/world mutation and no dev-machine runtime path")