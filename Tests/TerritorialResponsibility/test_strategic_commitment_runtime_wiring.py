from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[2]
BASE = ROOT / "src" / "ClanAI" / "src" / "ClanAI"
LAYER = (BASE / "StrategicCommitmentLayer.cs").read_text(encoding="utf-8")
POLICY = (BASE / "StrategicCommitmentPolicy.cs").read_text(encoding="utf-8")
BLACKBOARD = (BASE / "ActorStrategicBlackboard.cs").read_text(encoding="utf-8")
COMPOSER = (BASE / "StrategicDecisionComposer.cs").read_text(encoding="utf-8")

checks = {
    "previous_state_from_blackboard":
        r"ActorStrategicBlackboard\.TryGetCommitmentState\(",
    "natural_winner_from_composer":
        r"composer\.CurrentBestIndex\(",
    "native_natural_candidate":
        r"thinkParams\s*\.AIBehaviorScores\[naturalWinner\]",
    "same_objective_signature_gate":
        r"naturalSignature[\s\S]*?previous\.ObjectiveSignature",
    "coarse_state_classification_preserved":
        r"ActorStrategicBlackboard\.ClassifyState\(",
    "pure_context_policy":
        r"StrategicCommitmentPolicy\.EvaluateContext\(",
    "previous_candidate_lookup_from_native_list":
        r"ActorStrategicBlackboard\.FindObjectiveIndex\(",
    "previous_positive_score_policy":
        r"StrategicCommitmentPolicy\.ValidPositiveScore\(",
    "natural_score_from_composer":
        r"composer\.CurrentScore\(",
    "pure_score_policy":
        r"StrategicCommitmentPolicy\.EvaluateScores\(",
    "apply_guarded_by_policy":
        r"if \(scoreDecision\.Apply\)[\s\S]*?composer\.ApplyFactor\(",
    "apply_only_previous_index":
        r"composer\.ApplyFactor\(\s*previousIndex,",
    "existing_retention_factor":
        r"RetentionFactor,\s*\"same-state-objective-retention\"",
    "blackboard_find_existing_candidate":
        r"for \(int i = 0;[\s\S]*?AIBehaviorScores\.Count[\s\S]*?Signature\(data\)[\s\S]*?objectiveSignature",
    "composer_owns_native_score_write":
        r"thinkParams\.SetBehaviorScore\(",
}

failed = []
for name, pattern in checks.items():
    if name == "blackboard_find_existing_candidate":
        source = BLACKBOARD
    elif name == "composer_owns_native_score_write":
        source = COMPOSER
    else:
        source = LAYER

    if not re.search(pattern, source, re.S):
        failed.append(name)

if "SetBehaviorScore(" in LAYER:
    failed.append("commitment_layer_must_not_write_native_scores_directly")

apply_calls = len(re.findall(r"composer\.ApplyFactor\(", LAYER))
if apply_calls != 1:
    failed.append("commitment_apply_factor_call_count=" + str(apply_calls))

if not re.search(
    r"result\.Apply\s*=\s*mode == StrategicCommitmentMode\.Apply",
    POLICY,
    re.S,
):
    failed.append("observe_apply_policy_gate")

if failed:
    print("FAIL Strategic Commitment runtime wiring")
    for name in failed:
        print(" -", name)
    sys.exit(1)

print("PASS Strategic Commitment runtime wiring")
print("existing native candidate lookup and composer-owned previous-candidate rescale preserved")