from pathlib import Path
import datetime
import json
import os
import subprocess
import sys
import traceback

ROOT = Path(r"D:\BannerlordAIResearch")
REGISTRY = ROOT / r"Automation\Autopilot\registry.json"
STATE = ROOT / r"Automation\Autopilot\state.json"
LOGS = ROOT / r"Automation\Autopilot\logs"
OFFICE = ROOT / r"Automation\Office"
CODER_STATUS = OFFICE / "coder_status.json"
ANALYZER_STATUS = OFFICE / "analyzer_status.json"

def read_json(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))

def atomic_json(path, data):
    tmp = path.with_suffix(path.suffix + ".tmp")
    tmp.write_text(json.dumps(data, indent=2) + "\n", encoding="utf-8")
    os.replace(tmp, path)

def stamp():
    return datetime.datetime.now().astimezone().isoformat()

def main():
    if len(sys.argv) != 2:
        raise SystemExit("usage: run_registered_action.py <action_id>")
    action_id = sys.argv[1]
    registry = read_json(REGISTRY)
    state = read_json(STATE)
    action = (registry.get("actions") or {}).get(action_id)
    if not isinstance(action, dict):
        raise SystemExit("unknown action")

    if state.get("pending_action_id") != action_id:
        raise SystemExit("state/action mismatch")

    state["status"] = "RUNNING"
    state["runner_pid"] = os.getpid()
    state["started_at"] = stamp()
    atomic_json(STATE, state)

    LOGS.mkdir(parents=True, exist_ok=True)
    log_path = LOGS / (
        datetime.datetime.now().strftime("%Y%m%d_%H%M%S") +
        "_" + action_id + ".log"
    )

    command = action.get("command")
    if not isinstance(command, list) or not command:
        raise SystemExit("invalid command registry")

    rc = 999
    try:
        with log_path.open("w", encoding="utf-8") as log:
            cp = subprocess.run(
                command,
                cwd=str(ROOT),
                stdout=log,
                stderr=subprocess.STDOUT,
                text=True,
            )
            rc = cp.returncode
    except Exception:
        with log_path.open("a", encoding="utf-8") as log:
            log.write(traceback.format_exc())
        rc = 998

    state = read_json(STATE)
    state["completed_at"] = stamp()
    state["return_code"] = rc
    state["log_path"] = str(log_path)
    state["runner_pid"] = None

    if rc == 0:
        state["status"] = "DONE"
        state["last_result"] = "SUCCESS"
        next_id = action.get("next_action_id")
        if next_id:
            state["pending_action_id"] = next_id
            state["status"] = "READY"
            state["attempts"] = 0
    else:
        state["status"] = "FAILED"
        state["last_result"] = "FAILED"

    atomic_json(STATE, state)

    OFFICE.mkdir(parents=True, exist_ok=True)
    result_id = (
        action_id + "|" + str(state.get("completed_at") or "")
    )
    coder = {
        "schema": "BannerlordAI.Office.CoderStatus.v1",
        "updated_at": stamp(),
        "state": "COMPLETE" if rc == 0 else "FAILED",
        "result_id": result_id,
        "action_id": action_id,
        "description": action.get("description"),
        "return_code": rc,
        "result": state.get("last_result"),
        "log_path": str(log_path),
        "command": command,
        "handoff_to": "Analyzer / Archivist",
        "required_next": (
            "Review validator/log evidence and update durable knowledge."
        )
    }
    atomic_json(CODER_STATUS, coder)

    analyzer = {
        "schema": "BannerlordAI.Office.AnalyzerStatus.v1",
        "updated_at": stamp(),
        "state": "INTAKE_PENDING",
        "pending_result_id": result_id,
        "source_action_id": action_id,
        "source_log": str(log_path),
        "coder_return_code": rc,
        "classification": (
            "EXECUTION_PASS_PENDING_EVIDENCE_REVIEW"
            if rc == 0
            else "EXECUTION_FAIL_PENDING_EVIDENCE_REVIEW"
        ),
        "required_tasks": [
            "inspect raw/validator evidence",
            "separate observation from inference",
            "refresh context pyramid/index",
            "update lessons/policies/do-not-repeat if warranted",
            "define next acceptance gate or blocker"
        ]
    }
    atomic_json(ANALYZER_STATUS, analyzer)

    analyzer_intake = (
        ROOT / "Tools" / "Autopilot" / "analyzer_intake.py"
    )
    try:
        intake = subprocess.run(
            [sys.executable, "-X", "utf8", str(analyzer_intake)],
            cwd=str(ROOT),
            capture_output=True,
            text=True,
            errors="replace",
            timeout=30,
        )
        state = read_json(STATE)
        state["analyzer_intake_return_code"] = intake.returncode
        state["analyzer_intake_tail"] = (
            (intake.stdout or "") + (intake.stderr or "")
        )[-1200:]
        atomic_json(STATE, state)
    except Exception as ex:
        state = read_json(STATE)
        state["analyzer_intake_return_code"] = 998
        state["analyzer_intake_tail"] = (
            type(ex).__name__ + ": " + str(ex)
        )
        atomic_json(STATE, state)
    return rc

if __name__ == "__main__":
    raise SystemExit(main())
