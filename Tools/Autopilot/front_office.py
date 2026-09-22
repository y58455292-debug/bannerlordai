from pathlib import Path
import datetime
import json
import os
import subprocess
import sys

ROOT=Path(r"D:\BannerlordAIResearch")
HANDOFF=ROOT/r"Longitudinal\EvidenceIndex\HandoffV3"
FOLLOW=ROOT/r"Tools\Handoff\follow_protocol.py"
PYRAMID=ROOT/r"Tools\Evidence\context_pyramid.py"
FRONT=ROOT/r"Longitudinal\EvidenceIndex\ContextPyramid\front_page.json"
OFFICE=ROOT/r"Automation\Office"
DEPARTMENTS=OFFICE/"departments.json"
CODER=OFFICE/"coder_status.json"
ANALYZER=OFFICE/"analyzer_status.json"
FRONT_STATUS=OFFICE/"front_status.json"
SUPERVISOR=ROOT/r"Automation\Autopilot\state.json"
VALIDATIONS=ROOT/r"Longitudinal\LiveValidation"
ROLLOVER=ROOT/r"Tools\Autopilot\front_rollover_guard.py"
ROLLOVER_STATE=OFFICE/"front_rollover_state.json"

def now():
    return datetime.datetime.now().astimezone().isoformat()

def read_json(p):
    try:
        return json.loads(p.read_text(encoding="utf-8-sig")) if p.exists() else {}
    except Exception as ex:
        return {"_error":type(ex).__name__+": "+str(ex)}

def atomic_json(p,obj):
    p.parent.mkdir(parents=True,exist_ok=True)
    tmp=p.with_suffix(p.suffix+".tmp")
    tmp.write_text(json.dumps(obj,indent=2,ensure_ascii=False)+"\n",encoding="utf-8")
    os.replace(tmp,p)

def newest_validation():
    if not VALIDATIONS.exists():
        return None
    dirs=[x for x in VALIDATIONS.iterdir() if x.is_dir()]
    if not dirs:
        return None
    d=max(dirs,key=lambda x:x.stat().st_mtime)
    v=d/"validation.json"
    out={"path":str(d)}
    if v.exists():
        j=read_json(v)
        out.update({
            "schema":j.get("schema"),
            "result":j.get("result"),
            "error":j.get("error")
        })
    return out

def main():
    follow=subprocess.run(
        [sys.executable,"-X","utf8",str(FOLLOW)],
        cwd=str(ROOT),capture_output=True,text=True,errors="replace"
    )
    rebuild=subprocess.run(
        [sys.executable,"-X","utf8",str(PYRAMID),"rebuild"],
        cwd=str(ROOT),capture_output=True,text=True,errors="replace"
    )
    rollover=subprocess.run(
        [sys.executable,"-X","utf8",str(ROLLOVER),"touch"],
        cwd=str(ROOT),capture_output=True,text=True,errors="replace"
    )

    front=read_json(FRONT)
    coder=read_json(CODER)
    analyzer=read_json(ANALYZER)
    supervisor=read_json(SUPERVISOR)
    rollover_state=read_json(ROLLOVER_STATE)

    blockers=front.get("blockers") or []
    runtime=front.get("runtime") or {}
    if rollover_state.get("pressure_level")=="RED":
        office_state="ROLLOVER_REQUIRED"
    elif blockers:
        office_state="BLOCKED"
    elif runtime.get("bannerlord_running") or supervisor.get("status")=="RUNNING":
        office_state="WORKING"
    elif analyzer.get("state") in ("INTAKE_PENDING","INDEXED_PENDING_REASONING_REVIEW"):
        office_state="ANALYZING"
    elif supervisor.get("status")=="READY":
        office_state="READY_TO_CONTINUE"
    else:
        office_state="READY_FOR_NEXT_ASSIGNMENT"

    snapshot={
        "schema":"BannerlordAI.FrontOffice.v1",
        "updated_at":now(),
        "office_state":office_state,
        "checkpoint_seq":front.get("checkpoint_seq"),
        "accepted":front.get("accepted"),
        "active":front.get("active"),
        "question":front.get("question"),
        "next":front.get("next"),
        "blockers":blockers,
        "runtime":runtime,
        "departments":{
            "coder":{
                "state":coder.get("state") or "NO_DESK_REPORT",
                "action_id":coder.get("action_id"),
                "result":coder.get("result"),
                "return_code":coder.get("return_code"),
                "log_path":coder.get("log_path")
            },
            "analyzer":{
                "state":analyzer.get("state") or "NO_DESK_REPORT",
                "classification":analyzer.get("classification"),
                "source_action_id":analyzer.get("source_action_id"),
                "agent_review_required":analyzer.get("agent_review_required"),
                "latest_validation":analyzer.get("latest_validation")
            },
            "supervisor":{
                "enabled":supervisor.get("enabled"),
                "state":supervisor.get("status"),
                "pending_action_id":supervisor.get("pending_action_id"),
                "attempts":supervisor.get("attempts"),
                "last_result":supervisor.get("last_result"),
                "last_supervisor_result":supervisor.get("last_supervisor_result")
            },
            "front":{
                "state":"ACTIVE",
                "role":"User-facing whole-office coordination and concise verified updates"
            }
        },
        "latest_validation":newest_validation(),
        "rollover":rollover_state,
        "recovery":{
            "follow_protocol_return_code":follow.returncode,
            "follow_protocol_tail":(follow.stdout+follow.stderr)[-1000:],
            "context_rebuild_return_code":rebuild.returncode
        }
    }
    atomic_json(FRONT_STATUS,snapshot)
    print(json.dumps(snapshot,indent=2,ensure_ascii=False))
    return 0

if __name__=="__main__":
    raise SystemExit(main())
