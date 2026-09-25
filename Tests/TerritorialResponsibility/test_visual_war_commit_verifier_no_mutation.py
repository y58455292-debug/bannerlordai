from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[2]
BASE = ROOT / "src" / "ClanAI" / "src" / "ClanAI"
LAYER = (BASE / "VisualWarDecisionLayer.cs").read_text(encoding="utf-8")
POLICY = (BASE / "VisualWarCommitPolicy.cs").read_text(encoding="utf-8")
combined = LAYER + "\n" + POLICY

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
    "actor.DefaultBehavior =",
    "actor.ShortTermBehavior =",
    "actor.TargetSettlement =",
    "actor.ShortTermTargetSettlement =",
    "actor.TargetParty =",
    "actor.ShortTermTargetParty =",
)

failed = [item for item in forbidden if item in combined]

required_observation_only = (
    "actor.DefaultBehavior",
    "actor.ShortTermBehavior",
    "actor.TargetSettlement",
    "actor.ShortTermTargetSettlement",
    "actor.TargetParty",
    "actor.ShortTermTargetParty",
    "VisualWarCommitPolicy.Evaluate",
    "PendingCommitByParty.Remove",
)

for item in required_observation_only:
    if item not in LAYER:
        failed.append("missing_observation:" + item)

if failed:
    print("FAIL VisualWar commit verifier no-mutation invariant")
    for item in failed:
        print(" -", item)
    sys.exit(1)

print("PASS VisualWar commit verifier no-mutation invariant")
print("no movement/order/target/faction/settlement/war mutation APIs introduced")