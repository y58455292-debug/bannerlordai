from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / "src" / "ClanAI" / "src" / "ClanAI" / "PrisonerMercyDecisionBehavior.cs"

text = SOURCE.read_text(encoding="utf-8")

checks = {
    "release_threshold_is_20": r"private const float ReleaseThreshold = 20f;",
    "locked_threshold_is_20": r"private const float LockedReleaseThreshold = 20f;",
    "reset_calls_lock": r"AssertReleaseThresholdLock\(\);",
    "decision_uses_threshold": r"bool release = score >= ReleaseThreshold;",
    "startup_logs_threshold": r'" releaseThreshold=" \+ F\(ReleaseThreshold\)',
    "loud_drift_marker": r"PRISONER_MERCY_THRESHOLD_DRIFT",
}

failed = [name for name, pattern in checks.items() if not re.search(pattern, text)]

if re.search(r"ReleaseThreshold\s*=\s*15f", text):
    failed.append("threshold_15_drift_present")

if failed:
    print("FAIL PrisonerMercy threshold-20 lock")
    for name in failed:
        print(" -", name)
    sys.exit(1)

print("PASS PrisonerMercy threshold-20 lock")
print("source:", SOURCE)
print("threshold: 20")
print("decision rule: score >= ReleaseThreshold")
print("runtime guard: PRISONER_MERCY_THRESHOLD_DRIFT")
