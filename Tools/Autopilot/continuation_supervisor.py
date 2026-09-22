from pathlib import Path
import datetime
import hashlib
import json
import os
import subprocess
import sys

ROOT = Path(r"D:\BannerlordAIResearch")
HANDOFF = ROOT / r"Longitudinal\EvidenceIndex\HandoffV3"
CURRENT = HANDOFF / "current.json"
ATTENTION = HANDOFF / r"Continuity\attention.json"
OWNER = ROOT / r"workspace\DEPLOYMENT_OWNER.json"
ACTIVE_RUN = ROOT / r"Automation\Control\active_run.json"
REGISTRY = ROOT / r"Automation\Autopilot\registry.json"
STATE = ROOT / r"Automation\Autopilot\state.json"
LOCK = ROOT / r"Automation\Autopilot\supervisor.lock"
RUNNER = ROOT / r"Tools\Autopilot\run_registered_action.py"
LAB_RUNNER = ROOT / r"Tools\Autopilot\run_offline_lab_action.py"
POLICY_ENGINE = ROOT / r"Tools\Autopilot\office_dispatch_policy.py"
LOOP_SYNC = ROOT / r"Tools\Autopilot\sync_thinking_loop.py"
ANALYZER_STATUS = ROOT / r"Automation\Office\analyzer_status.json"
ANALYZER_ENTRY = ROOT / r"Tools\Autopilot\agent_protocol_entry.py"
INSTALLED = Path(r"C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\Modules\ClanAI\bin\Win64_Shipping_Client\ClanAI.dll")

def read_json(path):
    try:
        return json.loads(path.read_text(encoding="utf-8-sig")) if path.exists() else {}
    except Exception as ex:
        return {"_error": type(ex).__name__ + ": " + str(ex)}

def sha(path):
    try:
        return hashlib.sha256(path.read_bytes()).hexdigest().upper()
    except Exception:
        return None

def atomic_json(path, data):
    path.parent.mkdir(parents=True, exist_ok=True)
    tmp = path.with_suffix(path.suffix + ".tmp")
    tmp.write_text(json.dumps(data, indent=2) + "\n", encoding="utf-8")
    os.replace(tmp, path)

def game_running():
    cp = subprocess.run(
        ["tasklist", "/fi", "IMAGENAME eq Bannerlord.exe", "/fo", "csv", "/nh"],
        capture_output=True, text=True, errors="replace"
    )
    return "Bannerlord.exe" in cp.stdout

def acquire_lock():
    try:
        fd = os.open(str(LOCK), os.O_WRONLY | os.O_CREAT | os.O_EXCL)
    except FileExistsError:
        return None
    os.write(fd, str(os.getpid()).encode("ascii"))
    return fd

def release_lock(fd):
    if fd is None:
        return
    try:
        os.close(fd)
    finally:
        try:
            LOCK.unlink()
        except FileNotFoundError:
            pass

def stop(reason, state=None):
    payload = state or read_json(STATE)
    payload["last_check"] = datetime.datetime.now().astimezone().isoformat()
    payload["last_supervisor_result"] = reason
    atomic_json(STATE, payload)
    print(json.dumps({"started": False, "reason": reason}))
    return 0

def process_alive(pid):
    if not pid:
        return False
    cp = subprocess.run(
        ["tasklist", "/fi", f"PID eq {int(pid)}", "/fo", "csv", "/nh"],
        capture_output=True, text=True, errors="replace"
    )
    return str(pid) in cp.stdout and "No tasks" not in cp.stdout

def main():
    fd = acquire_lock()
    if fd is None:
        return 0
    try:
        cur = read_json(CURRENT)
        attn = read_json(ATTENTION)
        registry = read_json(REGISTRY)
        state = read_json(STATE)

        if not state.get("enabled"):
            return stop("AUTOPILOT_DISABLED", state)

        # VERIFY is a real work phase, not a passive indexed state. When the
        # unified controller reaches Analyzer VERIFY, surface a durable
        # reasoning-ready state immediately so Front executes semantic review
        # instead of waiting for another user check-in.
        analyzer = read_json(ANALYZER_STATUS)
        next_action = str(cur.get("next_action") or "")
        if next_action.startswith("Analyzer VERIFY") and analyzer.get("state") == "INDEXED_PENDING_REASONING_REVIEW":
            entry = subprocess.run(
                [sys.executable, "-X", "utf8", str(ANALYZER_ENTRY), "analyzer"],
                cwd=str(ROOT), capture_output=True, text=True, errors="replace")
            if entry.returncode == 0:
                analyzer["state"] = "REASONING_READY"
                analyzer["reasoning_ready_at"] = datetime.datetime.now().astimezone().isoformat()
                analyzer["continuation_required"] = True
                analyzer["continuation_rule"] = "Unified Front Controller must execute Analyzer semantic review now; do not wait for a user check-in."
                atomic_json(ANALYZER_STATUS, analyzer)
                state["analyzer_reasoning_ready"] = True
                state["analyzer_reasoning_ready_at"] = analyzer["reasoning_ready_at"]
                atomic_json(STATE, state)
                return stop("ANALYZER_REASONING_READY", state)
            state["analyzer_entry_tail"] = ((entry.stdout or "") + (entry.stderr or ""))[-1600:]
            atomic_json(STATE, state)
            return stop("ANALYZER_ENTRY_FAILED", state)
        if state.get("status") in ("DISPATCHED", "RUNNING"):
            if process_alive(state.get("runner_pid")) or game_running():
                return stop("ACTION_ALREADY_ACTIVE", state)
            state["status"] = "FAILED"
            state["last_error"] = "runner disappeared before terminal state"
            atomic_json(STATE, state)
            return stop("STALE_RUNNING_ACTION", state)

        warnings = [
            w for w in (attn.get("warnings") or [])
            if w != "DURABLE_CHECKPOINT_OLDER_THAN_15_MIN"
        ]
        if warnings:
            return stop("CONTINUITY_WARNING:" + ",".join(warnings), state)
        if game_running():
            return stop("GAME_ALREADY_RUNNING", state)
        if OWNER.exists():
            return stop("DEPLOYMENT_OWNER_PRESENT", state)

        active = read_json(ACTIVE_RUN)
        if active.get("state") == "ACTIVE":
            return stop("CONTROL_RUN_ACTIVE", state)

        expected = ((cur.get("installed_build") or {}).get("sha256"))
        installed = sha(INSTALLED)
        if expected and installed != expected:
            return stop("INSTALLED_HASH_MISMATCH", state)

        action_id = state.get("pending_action_id")
        actions = registry.get("actions") or {}
        action = actions.get(action_id)
        if not action_id or not isinstance(action, dict):
            return stop("NO_REGISTERED_PENDING_ACTION", state)
        if not action.get("enabled", True):
            return stop("ACTION_DISABLED", state)

        policy_cp = subprocess.run(
            [sys.executable, "-X", "utf8", str(POLICY_ENGINE), action_id],
            cwd=str(ROOT),
            capture_output=True,
            text=True,
            errors="replace",
        )
        if policy_cp.returncode != 0:
            try:
                policy_result = json.loads(policy_cp.stdout)
                policy_reason = policy_result.get("reason") or "DENIED"
            except Exception:
                policy_reason = "DENIED_UNPARSEABLE"
            state["office_policy_last"] = (
                (policy_cp.stdout or "") + (policy_cp.stderr or "")
            )[-1600:]
            atomic_json(STATE, state)
            return stop("OFFICE_POLICY_DENY:" + policy_reason, state)

        required_version = action.get("required_candidate_version")
        active_version = ((cur.get("active_candidate") or {}).get("version"))
        if required_version and active_version != required_version:
            return stop("CANDIDATE_VERSION_MISMATCH", state)

        needle = action.get("required_next_action_contains")
        if needle and needle not in str(cur.get("next_action") or ""):
            return stop("NEXT_ACTION_MISMATCH", state)

        attempts = int(state.get("attempts") or 0)
        max_attempts = int(action.get("max_attempts") or 1)
        if attempts >= max_attempts:
            return stop("ATTEMPT_BUDGET_EXHAUSTED", state)

        state["status"] = "DISPATCHED"
        state["attempts"] = attempts + 1
        state["dispatched_at"] = datetime.datetime.now().astimezone().isoformat()
        atomic_json(STATE, state)

        flags = getattr(subprocess, "CREATE_NEW_PROCESS_GROUP", 0)
        flags |= getattr(subprocess, "DETACHED_PROCESS", 0)
        runner_script = LAB_RUNNER if action.get("owner")=="supervisor" else RUNNER
        proc = subprocess.Popen(
            [sys.executable, str(runner_script), action_id],
            creationflags=flags,
            close_fds=True,
        )
        state["runner_pid"] = proc.pid
        state["last_supervisor_result"] = "DISPATCHED"
        atomic_json(STATE, state)
        print(json.dumps({"started": True, "action_id": action_id, "pid": proc.pid}))
        return 0
    finally:
        release_lock(fd)

if __name__ == "__main__":
    raise SystemExit(main())
