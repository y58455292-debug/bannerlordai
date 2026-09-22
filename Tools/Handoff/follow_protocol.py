from pathlib import Path
import json, subprocess, sys

ROOT = Path(r"D:\BannerlordAIResearch")
FAST = ROOT / r"Tools\Handoff\fast_resume.py"
DEEP = ROOT / r"Tools\Handoff\resume_state.py"

def run(cmd):
    return subprocess.run(
        [sys.executable, "-X", "utf8", str(cmd)],
        capture_output=True, text=True, errors="replace"
    )

fast = run(FAST)
line = (fast.stdout or "").strip().splitlines()
payload = {}
if line:
    try:
        payload = json.loads(line[-1])
    except Exception:
        payload = {}

if fast.returncode == 0 and payload.get("mode") == "FAST_OK":
    print(json.dumps({
        "protocol": "FOLLOW_PROTOCOL",
        "status": "FAST_OK",
        "resume_ms": payload.get("ms"),
        "checkpoint_seq": payload.get("seq"),
        "game_running": payload.get("game"),
        "hero": payload.get("hero"),
        "owner": payload.get("owner"),
        "run_state": payload.get("run"),
        "intent": payload.get("intent"),
        "detail": payload.get("detail")
    }, separators=(",", ":")))
    raise SystemExit(0)

deep = run(DEEP)
print(json.dumps({
    "protocol": "FOLLOW_PROTOCOL",
    "status": "DEEP_RECOVERY",
    "fast_returncode": fast.returncode,
    "fast_payload": payload,
    "deep_returncode": deep.returncode,
    "deep_snapshot": str(ROOT / r"Longitudinal\EvidenceIndex\HandoffV3\last_resume_snapshot.json")
}, separators=(",", ":")))
raise SystemExit(deep.returncode)
