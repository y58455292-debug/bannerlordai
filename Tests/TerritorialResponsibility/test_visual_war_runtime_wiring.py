from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[2]
BASE = ROOT / "src" / "ClanAI" / "src" / "ClanAI"
LAYER = (BASE / "VisualWarDecisionLayer.cs").read_text(encoding="utf-8")
COMPOSER = (BASE / "StrategicDecisionComposer.cs").read_text(encoding="utf-8")
BLACKBOARD = (BASE / "ActorStrategicBlackboard.cs").read_text(encoding="utf-8")

checks = {
    "native_candidate_iteration":
        r"thinkParams\.AIBehaviorScores\[i\]\.Item1",
    "native_candidate_score":
        r"composer\.CurrentScore\(",
    "settlement_target_from_native_candidate":
        r"Settlement settlement\s*=\s*data\.Party as Settlement;",
    "mobile_party_target_from_native_candidate":
        r"MobileParty targetParty\s*=\s*data\.Party as MobileParty;",
    "world_context_stays_runtime_owned":
        r"SettlementContext context\s*=\s*GetContext\(settlement\);",
    "bandit_classification_stays_runtime_owned":
        r"bool banditTarget\s*=\s*IsBandit\(targetParty\);",
    "pure_weak_rule":
        r"VisualWarPolicy\.IsWeakForRecovery\(",
    "pure_settlement_rule":
        r"VisualWarPolicy\.EvaluateSettlementCandidate\(",
    "pure_rear_security_rule":
        r"VisualWarPolicy\.EvaluateEngagePartyCandidate\(",
    "composer_factor_application":
        r"composer\.ApplyFactor\(",
    "composer_selects_final_winner":
        r"composer\.CurrentBestIndex\(",
    "composer_is_single_score_writer":
        r"thinkParams\.SetBehaviorScore\(",
    "blackboard_active_defense_reason":
        r'"visual-war"[\s\S]*?"active-defense"',
    "blackboard_frontier_defense_reason":
        r'"visual-war"[\s\S]*?"frontier-defense"',
    "blackboard_rear_security_reason":
        r'"visual-war"[\s\S]*?"rear-security"',
}

failed = []
for name, pattern in checks.items():
    source = COMPOSER if name == "composer_is_single_score_writer" else (
        BLACKBOARD if name.startswith("blackboard_") else LAYER
    )
    if not re.search(pattern, source, re.S):
        failed.append(name)

if "SetBehaviorScore(" in LAYER:
    failed.append("layer_must_not_write_native_scores_directly")

for forbidden in (
    "AIBehaviorScores.Add",
    "SetMoveGoToSettlement",
    "SetMoveGoToPoint",
    "SetMoveEngageParty",
    "ChangeOwnerOfSettlementAction",
    "ChangeKingdomAction",
    "DeclareWarAction",
):
    if forbidden in LAYER:
        failed.append("forbidden_runtime_mutation:" + forbidden)

if failed:
    print("FAIL VisualWar runtime wiring")
    for name in failed:
        print(" -", name)
    sys.exit(1)

print("PASS VisualWar runtime wiring")
print("native candidates/world classification/composer ownership preserved")