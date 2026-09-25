from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[2]
LAYER = ROOT / "src" / "ClanAI" / "src" / "ClanAI" / "HomeResponsibilityLayer.cs"
text = LAYER.read_text(encoding="utf-8")

checks = {
    "native_war_eligibility_gate": r"WorldScopeContext\.EligibleIndependentLordAtWar\(actor\)",
    "native_candidate_score_used": r"composer\.CurrentScore\(i, rawScore\)",
    "pure_policy_delegation": r"HomeResponsibilityPolicy\.Evaluate\(",
    "actor_clan_ownership_passed": r"SameClan\(settlement\.OwnerClan, clan\)",
    "bounded_factor_composer": r"composer\.ApplyFactor\(",
    "pending_only_after_winner_change": r"PendingByParty\[partyId\]\s*=\s*new PendingCommit",
    "post_vanilla_commit_check": r"VerifyPendingCommit\(actor\)",
    "memory_only_after_match": r"if \(matched\)[\s\S]*?CompanionDutyMemory\.RecordSuccessfulHomeDuty",
}

failed = [name for name, pattern in checks.items() if not re.search(pattern, text, re.S)]

for forbidden in (
    "ChangeOwnerOfSettlementAction",
    "ChangeKingdomAction",
    "DeclareWarAction",
    "SetMoveGoToSettlement",
):
    if forbidden in text:
        failed.append("direct_native_mutation:" + forbidden)

if failed:
    print("FAIL HomeResponsibility runtime wiring")
    for name in failed:
        print(" -", name)
    sys.exit(1)

print("PASS HomeResponsibility runtime wiring")
print("native candidate list and post-vanilla commit verification preserved")