from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[2]
BASE = ROOT / "src" / "ClanAI" / "src" / "ClanAI"
LAYER = (BASE / "StrategicCommitmentLayer.cs").read_text(encoding="utf-8")
POLICY = (BASE / "StrategicCommitmentPolicy.cs").read_text(encoding="utf-8")
COMMIT_POLICY = (BASE / "StrategicCommitmentCommitPolicy.cs").read_text(encoding="utf-8")
CONFIG = (BASE / "StrategicCommitmentConfig.cs").read_text(encoding="utf-8")
combined = LAYER + "\n" + POLICY + "\n" + COMMIT_POLICY + "\n" + CONFIG

forbidden = (
    "AIBehaviorScores.Add",
    "SetMoveGoToSettlement",
    "SetMoveGoToPoint",
    "SetMoveEngageParty",
    "SetMovePatrolAroundSettlement",
    "SetMoveBesiegeSettlement",
    "ChangeOwnerOfSettlementAction",
    "ChangeKingdomAction",
    "DeclareWarAction",
    "MakePeaceAction",
    "DefaultBehavior =",
    "ShortTermBehavior =",
    "TargetSettlement =",
    "ShortTermTargetSettlement =",
    "TargetParty =",
    "ShortTermTargetParty =",
)

failed = [item for item in forbidden if item in combined]

if "SetBehaviorScore(" in LAYER:
    failed.append("direct_native_score_write")

if "composer.ApplyFactor(" not in LAYER:
    failed.append("missing_composer_rescale")

if failed:
    print("FAIL Strategic Commitment no-mutation invariant")
    for item in failed:
        print(" -", item)
    sys.exit(1)

print("PASS Strategic Commitment no-mutation invariant")
print("no order/target/candidate/faction/settlement/war mutation APIs introduced")