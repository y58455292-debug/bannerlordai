from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[2]
BASE = ROOT / "src" / "ClanAI" / "src" / "ClanAI"
LAYER = (BASE / "VisualWarDecisionLayer.cs").read_text(encoding="utf-8")
POLICY = (BASE / "VisualWarCommitPolicy.cs").read_text(encoding="utf-8")
COMPOSER = (BASE / "StrategicDecisionComposer.cs").read_text(encoding="utf-8")
STRATEGIC = (BASE / "ClanAIStrategicBehavior.cs").read_text(encoding="utf-8")

checks = {
    "later-normal-ai-tick-verifies_before_activation_gate":
        r"VerifyPendingCommit\(actor\);[\s\S]*?if \(!Enabled",
    "pending_created_only_after_winner_change_guard":
        r"if \(afterIndex == beforeIndex\)\s*return;[\s\S]*?RecordPendingCommit\(",
    "expected_winner_is_existing_native_candidate":
        r"AIBehaviorData winner\s*=\s*thinkParams\.AIBehaviorScores\[winnerIndex\]\.Item1;",
    "target_identity_from_existing_candidate":
        r"CandidateTargetKey\([\s\S]*?winner",
    "pure_expectation_creation":
        r"VisualWarCommitPolicy\.CreateExpectation\(",
    "creation_hour_is_campaign_time":
        r"CampaignTime\.Now\.ToHours",
    "default_behavior_observed":
        r"actor\.DefaultBehavior",
    "short_behavior_observed":
        r"actor\.ShortTermBehavior",
    "target_settlement_observed":
        r"actor\.TargetSettlement",
    "short_target_settlement_observed":
        r"actor\.ShortTermTargetSettlement",
    "besieged_settlement_observed":
        r"actor\.BesiegedSettlement",
    "current_settlement_observed":
        r"actor\.CurrentSettlement",
    "target_party_observed":
        r"actor\.TargetParty",
    "short_target_party_observed":
        r"actor\.ShortTermTargetParty",
    "pure_match_expiry_evaluation":
        r"VisualWarCommitPolicy\.Evaluate\(",
    "remove_only_from_policy_result":
        r"if \(result\.Remove\)[\s\S]*?PendingCommitByParty\.Remove",
    "clear_commit_log":
        r'"VISUAL_WAR_COMMIT_CHECK"',
    "log_expected_behavior":
        r'" expectedBehavior="',
    "log_expected_target":
        r'" expectedTarget="',
    "log_actual_behavior":
        r'" actualDefault="',
    "log_actual_target":
        r'" actualTarget="',
    "log_matched":
        r'" matched=" \+ result\.Matched',
    "log_expired":
        r'" expired=" \+ result\.Expired',
    "log_age":
        r'" ageHours="',
    "reason_logged":
        r'" reason=" \+ pending\.Reason',
    "normal_ai_path_invokes_visual_war":
        r"VisualWarDecisionLayer\.Apply\([\s\S]*?party,[\s\S]*?thinkParams",
    "composer_still_owns_score_application":
        r"composer\.ApplyFactor\(",
    "composer_still_only_native_score_writer":
        r"thinkParams\.SetBehaviorScore\(",
    "expiry_is_bounded":
        r"PendingLifetimeHours\s*=\s*18\.0",
}

failed = []
for name, pattern in checks.items():
    if name == "composer_still_only_native_score_writer":
        source = COMPOSER
    elif name == "normal_ai_path_invokes_visual_war":
        source = STRATEGIC
    elif name == "expiry_is_bounded":
        source = POLICY
    else:
        source = LAYER
    if not re.search(pattern, source, re.S):
        failed.append(name)

if "SetBehaviorScore(" in LAYER:
    failed.append("verifier_layer_must_not_write_scores_directly")

if failed:
    print("FAIL VisualWar commit verifier wiring")
    for name in failed:
        print(" -", name)
    sys.exit(1)

print("PASS VisualWar commit verifier wiring")
print("expectation-on-winner-change and later native-state observation preserved")