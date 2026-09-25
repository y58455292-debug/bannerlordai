from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[2]
BASE = ROOT / "src" / "ClanAI" / "src" / "ClanAI"
LAYER = (BASE / "StrategicCommitmentLayer.cs").read_text(encoding="utf-8")
POLICY = (BASE / "StrategicCommitmentCommitPolicy.cs").read_text(encoding="utf-8")
STRATEGIC = (BASE / "ClanAIStrategicBehavior.cs").read_text(encoding="utf-8")

checks = {
    "later-evaluate-observes-pending-first":
        r"try\s*\{\s*VerifyPendingCommit\(actor\);[\s\S]*?if \(actor == null",
    "apply-branch-required":
        r"if \(scoreDecision\.Apply\)[\s\S]*?composer\.ApplyFactor\(",
    "final-winner-evaluated-after-factor":
        r"composer\.ApplyFactor\([\s\S]*?int afterWinner\s*=\s*composer\.CurrentBestIndex",
    "retained-is-previous-objective":
        r"bool retained\s*=\s*string\.Equals\([\s\S]*?afterSignature,[\s\S]*?previous\.ObjectiveSignature",
    "pending-behind-retained-guard":
        r"if \(retained\)[\s\S]*?ShouldCreateExpectation\(",
    "creation-requires-explicit-apply":
        r"ShouldCreateExpectation\([\s\S]*?mode ==[\s\S]*?StrategicCommitmentMode\.Apply",
    "creation-requires-factor-applied":
        r"ShouldCreateExpectation\([\s\S]*?scoreDecision\.Apply",
    "creation-requires-final-previous-retained":
        r"ShouldCreateExpectation\([\s\S]*?retained,[\s\S]*?retained\)",
    "record-final-winner-candidate":
        r"RecordPendingCommit\([\s\S]*?afterWinner,[\s\S]*?previous\)",
    "winner-data-from-existing-native-list":
        r"AIBehaviorData winner\s*=\s*thinkParams[\s\S]*?AIBehaviorScores\[winnerIndex\][\s\S]*?Item1",
    "stores-expected-native-behavior":
        r"ExpectedBehavior\s*=\s*winner\.AiBehavior",
    "stores-previous-label":
        r"PreviousObjectiveLabel\s*=\s*previous\.ObjectiveLabel",
    "stores-previous-signature":
        r"PreviousObjectiveSignature\s*=\s*previous\.ObjectiveSignature",
    "stores-campaign-hour":
        r"CreatedAtHours\s*=\s*CampaignTime\.Now\.ToHours",
    "pure-creation-gate":
        r"StrategicCommitmentCommitPolicy[\s\S]*?ShouldCreateExpectation\(",
    "pure-match-expiry":
        r"StrategicCommitmentCommitPolicy\.Evaluate\(",
    "remove-only-by-result":
        r"if \(result\.Remove\)[\s\S]*?PendingCommitByParty\.Remove",
    "clear-log-record":
        r'"STRATEGIC_COMMITMENT_COMMIT_CHECK"',
    "normal-strategic-path-still-calls-layer":
        r"StrategicCommitmentLayer\.Evaluate\(",
}

failed = []
for name, pattern in checks.items():
    source = STRATEGIC if name == "normal-strategic-path-still-calls-layer" else LAYER
    if not re.search(pattern, source, re.S):
        failed.append(name)

creation_gate = re.search(
    r"internal static bool ShouldCreateExpectation\([\s\S]*?\n        \}",
    POLICY,
    re.S,
)
if creation_gate is None:
    failed.append("pure-creation-policy-missing")
else:
    gate = creation_gate.group(0)
    for required in ("applyMode", "factorApplied", "finalWinnerIsPrevious", "retained"):
        if required not in gate:
            failed.append("pure-creation-gate-missing:" + required)

if failed:
    print("FAIL Strategic Commitment commit verifier wiring")
    for name in failed:
        print(" -", name)
    sys.exit(1)

print("PASS Strategic Commitment commit verifier wiring")
print("pending expectation creation is downstream of explicit Apply, applied factor, final previous-objective winner, and retained=True")