from pathlib import Path
import json
import subprocess
import sys

ROOT=Path(r"D:\BannerlordAIResearch")
OFFICE=ROOT/r"Automation\Office"
CLOCK_SCRIPT=ROOT/r"Tools\Autopilot\office_clock.py"
CLOCK=OFFICE/"office_clock_status.json"
POLICY=OFFICE/"office_policy.json"
ANALYZER=OFFICE/"analyzer_status.json"
REGISTRY=ROOT/r"Automation\Autopilot\registry.json"

def read_json(p):
    try:
        return json.loads(p.read_text(encoding="utf-8-sig")) if p.exists() else {}
    except Exception as ex:
        return {"_error":type(ex).__name__+": "+str(ex)}

def refresh_clock():
    subprocess.run(
        [sys.executable,"-X","utf8",str(CLOCK_SCRIPT)],
        cwd=str(ROOT),capture_output=True,text=True,errors="replace"
    )
    return read_json(CLOCK)

def evaluate(action_id):
    registry=read_json(REGISTRY)
    action=(registry.get("actions") or {}).get(action_id)
    if not isinstance(action,dict):
        return {"allowed":False,"reason":"UNKNOWN_ACTION","action_id":action_id}

    clock=refresh_clock()
    policy=read_json(POLICY)
    analyzer=read_json(ANALYZER)
    mode=clock.get("office_mode")
    work_class=action.get("work_class")
    owner=action.get("owner")
    work=(policy.get("work_classes") or {}).get(work_class) or {}

    if owner not in ("coder","supervisor"):
        return {"allowed":False,"reason":"REGISTERED_ACTION_OWNER_INVALID","action_id":action_id}

    if mode=="CLOSED":
        return {"allowed":False,"reason":"OFFICE_CLOSED","action_id":action_id,"office_mode":mode,"work_class":work_class}
    if mode=="OFFLINE" and owner=="coder":
        return {"allowed":False,"reason":"CODER_DISABLED_WHILE_OFFLINE","action_id":action_id,"office_mode":mode,"work_class":work_class}
    if mode=="OPEN" and owner=="supervisor" and work_class in ("SIMULATION","DATA_GATHER"):
        return {"allowed":False,"reason":"OFFLINE_LAB_ACTION_REQUIRES_OFFLINE","action_id":action_id,"office_mode":mode,"work_class":work_class}

    if work.get("requires_open") and mode!="OPEN":
        return {
            "allowed":False,
            "reason":"WORK_CLASS_REQUIRES_OPEN",
            "action_id":action_id,
            "office_mode":mode,
            "work_class":work_class
        }

    if work.get("requires_offline") and mode!="OFFLINE":
        return {"allowed":False,"reason":"WORK_CLASS_REQUIRES_OFFLINE","action_id":action_id,"office_mode":mode,"work_class":work_class}

    if action.get("may_launch_game") and mode=="OFFLINE" and not (owner=="supervisor" and work_class=="SIMULATION"):
        return {"allowed":False,"reason":"OFFLINE_GAME_ONLY_ALLOWED_FOR_SIMULATION","action_id":action_id,"office_mode":mode,"work_class":work_class}

    if action.get("requires_analyzer_clear"):
        # Single-controller architecture: analyzer_status is an evidence receipt,
        # not an independent worker lock. Only block when durable next_action is
        # actually in VERIFY and has not already routed the current action back
        # to EXECUTE.
        current=read_json(ROOT/r"Longitudinal\EvidenceIndex\HandoffV3\current.json")
        next_action=str(current.get("next_action") or "")
        if analyzer.get("state") in (
            "INTAKE_PENDING",
            "INDEXED_PENDING_REASONING_REVIEW",
        ) and next_action.startswith("Analyzer VERIFY"):
            return {
                "allowed":False,
                "reason":"ANALYZER_REVIEW_PENDING",
                "action_id":action_id,
                "office_mode":mode,
                "work_class":work_class
            }

    owner_clock=((clock.get("departments") or {}).get(owner) or {})
    allowed_classes={str(x).upper() for x in (owner_clock.get("allowed_work_classes") or [])}
    if work_class and work_class.upper() not in allowed_classes:
        return {
            "allowed":False,
            "reason":"OWNER_NOT_CLOCKED_FOR_WORK_CLASS",
            "action_id":action_id,
            "office_mode":mode,
            "work_class":work_class,
            "owner":owner,
            "owner_allowed":sorted(allowed_classes)
        }

    return {
        "allowed":True,
        "reason":"ALLOWED",
        "action_id":action_id,
        "office_mode":mode,
        "work_class":work_class,
        "owner":owner
    }

def main():
    if len(sys.argv)!=2:
        raise SystemExit("usage: office_dispatch_policy.py <action_id>")
    result=evaluate(sys.argv[1])
    print(json.dumps(result,indent=2))
    return 0 if result.get("allowed") else 2

if __name__=="__main__":
    raise SystemExit(main())
