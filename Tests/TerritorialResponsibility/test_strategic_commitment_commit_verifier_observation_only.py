from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[2]
LAYER = (
    ROOT / "src" / "ClanAI" / "src" / "ClanAI" /
    "StrategicCommitmentLayer.cs"
).read_text(encoding="utf-8")

start = LAYER.find("private static void VerifyPendingCommit(")
end = LAYER.find("private static string PartyKey(", start)

failed = []
if start < 0 or end < 0:
    failed.append("verify-method-not-found")
    body = ""
else:
    body = LAYER[start:end]

required_reads = (
    "actor.DefaultBehavior",
    "actor.ShortTermBehavior",
    "actor.TargetSettlement",
    "actor.ShortTermTargetSettlement",
    "actor.BesiegedSettlement",
    "actor.CurrentSettlement",
    "actor.TargetParty",
    "actor.ShortTermTargetParty",
    "StrategicCommitmentCommitPolicy.Evaluate",
)

for item in required_reads:
    if item not in body:
        failed.append("missing-native-read:" + item)

for forbidden in (
    "composer.ApplyFactor",
    "SetBehaviorScore",
    "SetMoveGoToSettlement",
    "SetMoveGoToPoint",
    "SetMoveEngageParty",
    "SetMovePatrolAroundSettlement",
    "SetMoveBesiegeSettlement",
    "AIBehaviorScores.Add",
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
):
    if forbidden in body:
        failed.append("forbidden-verifier-mutation:" + forbidden)

if "PendingCommitByParty.Remove" not in body:
    failed.append("missing-bounded-pending-removal")

if '"STRATEGIC_COMMITMENT_COMMIT_CHECK"' not in body:
    failed.append("missing-commit-check-log")

if failed:
    print("FAIL Strategic Commitment verifier observation-only invariant")
    for item in failed:
        print(" -", item)
    sys.exit(1)

print("PASS Strategic Commitment verifier observation-only invariant")
print("later verifier reads native behavior/target state only and removes pending state on policy match/expiry")