from pathlib import Path
import datetime
import hashlib
import json
import os
import subprocess
import sys

HERE = Path(__file__).resolve().parent
if str(HERE) not in sys.path:
    sys.path.insert(0, str(HERE))

import durable_handoff as durable

BASE = Path(r"D:\BannerlordAIResearch")
HANDOFF = BASE / r"Longitudinal\EvidenceIndex\HandoffV3"
OUT = HANDOFF / "Continuity"
LATEST = OUT / "latest_runtime.json"
HISTORY = OUT / "runtime_events.jsonl"
ATTENTION = OUT / "attention.json"
AGENT_ACTIVITY = OUT / "last_agent_activity.json"
STATUS = BASE / r"Automation\TestRunner\status.txt"
OWNER = BASE / r"workspace\DEPLOYMENT_OWNER.json"
INSTALLED = Path(r"C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\Modules\ClanAI\bin\Win64_Shipping_Client\ClanAI.dll")
REPORTS = BASE / r"Longitudinal\DerivedReports"
VALIDATIONS = BASE / r"Longitudinal\LiveValidation"
STAGING = BASE / r"workspace\_PatchStaging"
LOOP_GUARD = BASE / r"Tools\Autopilot\loop_latency_guard.py"
LOOP_STATE = BASE / r"Automation\Office\loop_latency_state.json"

def now():
    return datetime.datetime.now().astimezone()

def now_iso():
    return now().isoformat()

def read_json(path):
    if not path.exists():
        return None
    try:
        return json.loads(path.read_text(encoding="utf-8-sig"))
    except Exception as ex:
        return {"_read_error": type(ex).__name__ + ": " + str(ex)}

def read_text(path):
    if not path.exists():
        return None
    return path.read_text(encoding="utf-8-sig", errors="replace")

def sha(path):
    if not path.exists():
        return None
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()

def atomic_json(path, obj):
    path.parent.mkdir(parents=True, exist_ok=True)
    tmp = path.with_suffix(path.suffix + ".tmp")
    with tmp.open("w", encoding="utf-8") as f:
        f.write(json.dumps(obj, indent=2, ensure_ascii=False) + "\n")
        f.flush()
        os.fsync(f.fileno())
    os.replace(tmp, path)

def append_jsonl(path, obj):
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("a", encoding="utf-8") as f:
        f.write(json.dumps(obj, ensure_ascii=False, sort_keys=True) + "\n")
        f.flush()
        os.fsync(f.fileno())

def parse_status(text):
    out = {}
    for line in (text or "").splitlines():
        if "=" in line:
            k, v = line.split("=", 1)
            out[k.strip()] = v.strip()
    return out

def process_snapshot():
    raw = subprocess.run(
        ["tasklist", "/fo", "csv"],
        capture_output=True,
        text=True,
        errors="replace",
    ).stdout.splitlines()
    names = ("Bannerlord.exe", "TaleWorlds")
    rows = [x for x in raw if any(n in x for n in names)]
    return rows

def newest(root):
    if not root.exists():
        return None
    items = sorted(root.iterdir(), key=lambda p: p.stat().st_mtime, reverse=True)
    if not items:
        return None
    p = items[0]
    return {
        "name": p.name,
        "path": str(p),
        "mtime": datetime.datetime.fromtimestamp(
            p.stat().st_mtime,
            tz=datetime.datetime.now().astimezone().tzinfo,
        ).isoformat(),
        "is_dir": p.is_dir(),
    }

def parse_iso(value):
    if not value:
        return None
    try:
        return datetime.datetime.fromisoformat(value)
    except Exception:
        return None

def age_seconds(value):
    dt = parse_iso(value)
    if dt is None:
        return None
    if dt.tzinfo is None:
        dt = dt.replace(tzinfo=now().tzinfo)
    return max(0.0, (now() - dt.astimezone(now().tzinfo)).total_seconds())

def stable_fingerprint(snapshot):
    body = dict(snapshot)
    body.pop("observed_at", None)
    body.pop("watchdog_run_id", None)
    body.pop("agent_activity_age_seconds", None)
    return hashlib.sha256(
        json.dumps(body, sort_keys=True, separators=(",", ":")).encode("utf-8")
    ).hexdigest().upper()

def main():
    subprocess.run([sys.executable,"-X","utf8",str(LOOP_GUARD)],cwd=str(BASE),capture_output=True,text=True,errors="replace")
    loop_state = read_json(LOOP_STATE) or {}
    state = durable.reconstruct_state()
    runtime_processes = process_snapshot()
    game_running = any("Bannerlord.exe" in x or "TaleWorlds" in x for x in runtime_processes)
    owner = read_json(OWNER)
    agent_activity = read_json(AGENT_ACTIVITY)
    installed = sha(INSTALLED)
    expected_owner_hash = None if not isinstance(owner, dict) else owner.get("expected_installed_clanai_sha256")
    state_hash = ((state.get("installed_build") or {}).get("sha256") if state else None)

    snapshot = {
        "schema": "BannerlordAI.ContinuityRuntime.v1",
        "observed_at": now_iso(),
        "watchdog_run_id": hashlib.sha256(now_iso().encode("utf-8")).hexdigest()[:16],
        "durable_checkpoint_seq": state.get("checkpoint_seq") if state else None,
        "durable_updated_at": state.get("updated_at") if state else None,
        "durable_next_action": state.get("next_action") if state else None,
        "durable_operation_phase": ((state.get("run_state") or {}).get("operation_phase") if state else None),
        "installed_clanai_sha256": installed,
        "durable_installed_sha256": state_hash,
        "installed_matches_durable": (installed == state_hash) if installed and state_hash else None,
        "deployment_owner": owner,
        "owner_expected_matches_installed": (
            expected_owner_hash == installed
            if expected_owner_hash and installed
            else None
        ),
        "bannerlord_running": game_running,
        "active_process_rows": runtime_processes,
        "runner_status": parse_status(read_text(STATUS)),
        "agent_activity": agent_activity,
        "agent_activity_age_seconds": (
            age_seconds(agent_activity.get("updated_at"))
            if isinstance(agent_activity, dict)
            else None
        ),
        "newest_report": newest(REPORTS),
        "newest_validation": newest(VALIDATIONS),
        "newest_staging": newest(STAGING),
    }

    warnings = []
    durable_age = age_seconds(snapshot["durable_updated_at"])
    if durable_age is not None and durable_age > 900:
        warnings.append("DURABLE_CHECKPOINT_OLDER_THAN_15_MIN")
    if snapshot["installed_matches_durable"] is False:
        warnings.append("INSTALLED_HASH_DIFFERS_FROM_DURABLE_STATE")
    if owner and snapshot["owner_expected_matches_installed"] is False:
        warnings.append("DEPLOYMENT_OWNER_HASH_MISMATCH")
    if owner and not game_running:
        renewed = owner.get("renewed_local") or owner.get("acquired_local")
        owner_age = age_seconds(renewed)
        lease_seconds = int(owner.get("lease_duration_seconds") or 900)
        if owner_age is not None and owner_age > lease_seconds:
            warnings.append("DEPLOYMENT_LEASE_EXPIRED_WITH_NO_GAME")
    loop_warning = loop_state.get("warning") if isinstance(loop_state, dict) else None
    if loop_warning:
        warnings.append(loop_warning)
    activity_age = snapshot["agent_activity_age_seconds"]
    if activity_age is None:
        warnings.append("NO_AGENT_ACTIVITY_HEARTBEAT")
    elif activity_age > 4500:
        warnings.append("NO_AGENT_ACTIVITY_OLDER_THAN_75_MIN")
    snapshot["warnings"] = warnings

    previous = read_json(LATEST)
    snapshot["fingerprint"] = stable_fingerprint(snapshot)

    if not previous or previous.get("fingerprint") != snapshot["fingerprint"]:
        append_jsonl(HISTORY, snapshot)

    atomic_json(LATEST, snapshot)

    attention = {
        "schema": "BannerlordAI.ContinuityAttention.v1",
        "updated_at": snapshot["observed_at"],
        "needs_attention": bool(warnings),
        "warnings": warnings,
        "durable_checkpoint_seq": snapshot["durable_checkpoint_seq"],
        "next_action": snapshot["durable_next_action"],
        "latest_runtime": str(LATEST),
        "loop_latency_state": str(LOOP_STATE),
        "activation_command": loop_state.get("activation_command") if isinstance(loop_state, dict) else None,
    }
    atomic_json(ATTENTION, attention)

    print(json.dumps({
        "ok": True,
        "latest": str(LATEST),
        "attention": str(ATTENTION),
        "checkpoint_seq": snapshot["durable_checkpoint_seq"],
        "warnings": warnings,
        "fingerprint": snapshot["fingerprint"],
    }, indent=2))

if __name__ == "__main__":
    main()
