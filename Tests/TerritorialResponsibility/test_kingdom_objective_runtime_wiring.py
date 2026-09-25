from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[2]
LAYER = ROOT / "src" / "ClanAI" / "src" / "ClanAI" / "KingdomObjectiveLayer.cs"
text = LAYER.read_text(encoding="utf-8")

checks = {
    "native_actor_eligibility": r"WorldScopeContext\.EligibleIndependentLordAtWar\(actor\)",
    "native_ruler_objective_source": r"WarStateTracker\.TryGetActiveObjectiveFor\(",
    "urgent_home_threat_gate": r"WorldScopeContext\.HasOtherUrgentClanThreat\(",
    "native_candidate_iteration": r"thinkParams\.AIBehaviorScores\[i\]\.Item1",
    "settlement_from_native_candidate": r"Settlement settlement = data\.Party as Settlement;",
    "direct_target_match": r"settlement\.StringId,[\s\S]*?objective\.Target\.StringId",
    "staging_matches_existing_candidate": r"stageRank\.TryGetValue\(settlement\.StringId, out rank\)",
    "composer_current_score": r"composer\.CurrentScore\(i, raw\)",
    "pure_policy_candidate_decision": r"KingdomObjectivePolicy\.EvaluateCandidate\(",
    "composer_factor_write": r"composer\.ApplyFactor\(",
    "composer_selects_final_winner": r"composer\.CurrentBestIndex\(thinkParams\)",
    "pure_pending_gate": r"KingdomObjectivePolicy\.ShouldCreatePendingCommit\(",
    "pending_expectation_only": r"PendingByParty\[partyId\]\s*=\s*new PendingCommit",
    "post_vanilla_commit_check": r"VerifyPendingCommit\(actor, \"ai-think\"\)",
    "native_behavior_match": r"actor\.DefaultBehavior == pending\.Behavior",
    "native_target_match": r"pending\.TargetSettlementId",
}

failed = [name for name, pattern in checks.items()
          if not re.search(pattern, text, re.S)]

for forbidden in (
    "AIBehaviorScores.Add",
    "SetMoveGoToSettlement",
    "SetMoveGoToPoint",
    "SetMoveEngageParty",
    "ChangeOwnerOfSettlementAction",
    "ChangeKingdomAction",
    "DeclareWarAction",
):
    if forbidden in text:
        failed.append("forbidden_runtime_mutation:" + forbidden)

if failed:
    print("FAIL KingdomObjective runtime wiring")
    for name in failed:
        print(" -", name)
    sys.exit(1)

print("PASS KingdomObjective runtime wiring")
print("native candidates, composer ownership, and post-vanilla commit verification preserved")