from pathlib import Path
import datetime
import json
import os
import sys

ROOT=Path(r"D:\BannerlordAIResearch")
OFFICE=ROOT/r"Automation\Office"
REGISTRY=OFFICE/"chat_sessions.json"
QUEUE=OFFICE/"chat_retirement_queue.json"
HANDOFF_DIR=OFFICE/"ChatHandoffs"
FRONT=ROOT/r"Longitudinal\EvidenceIndex\ContextPyramid\front_page.json"
CURRENT=ROOT/r"Longitudinal\EvidenceIndex\HandoffV3\current.json"
CODER=OFFICE/"coder_status.json"
ANALYZER=OFFICE/"analyzer_status.json"
FRONT_STATUS=OFFICE/"front_status.json"
SHIFT_POLICY=OFFICE/"chat_shift_policy.json"
MEM_SYNC=OFFICE/"mem_archive_sync.json"

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

def sessions():
    data=read_json(REGISTRY)
    if not data:
        data={"schema":"BannerlordAI.ChatSessions.v1","sessions":{}}
    data.setdefault("sessions",{})
    return data

def role_status(role):
    if role=="coder":
        return read_json(CODER)
    if role=="analyzer":
        return read_json(ANALYZER)
    if role=="front":
        return read_json(FRONT_STATUS)
    return {}

def clock_in(role,alias):
    data=sessions()
    existing=data["sessions"].get(alias)
    if existing and existing.get("state") not in ("RETIRED","SAFE_TO_ARCHIVE","SAFE_TO_DELETE"):
        raise RuntimeError("chat alias already active")
    current=read_json(CURRENT)
    item={
        "alias":alias,
        "role":role,
        "state":"CLOCKED_IN",
        "clocked_in_at":now(),
        "clocked_out_at":None,
        "start_checkpoint_seq":current.get("checkpoint_seq"),
        "end_checkpoint_seq":None,
        "work_packets":0,
        "material_updates":0,
        "handoff_path":None,
        "downstream_review":"NOT_REQUIRED" if role=="front" else "PENDING",
        "brain_capture":{
            "local_durable":False,
            "context_pyramid":False,
            "mem_mirror":False
        },
        "archive_safe":False,
        "delete_safe":False
    }
    data["sessions"][alias]=item
    data["updated_at"]=now()
    atomic_json(REGISTRY,data)
    return item

def clock_out(alias,reason):
    data=sessions()
    item=data["sessions"].get(alias)
    if not item:
        raise RuntimeError("unknown chat alias")
    role=item.get("role")
    current=read_json(CURRENT)
    front=read_json(FRONT)
    desk=role_status(role)
    HANDOFF_DIR.mkdir(parents=True,exist_ok=True)
    stamp=datetime.datetime.now().strftime("%Y%m%d_%H%M%S")
    handoff=HANDOFF_DIR/f"{stamp}_{alias.replace(' ','_')}.json"
    payload={
        "schema":"BannerlordAI.ChatClockoutHandoff.v1",
        "created_at":now(),
        "chat_alias":alias,
        "role":role,
        "reason":reason,
        "checkpoint_seq":current.get("checkpoint_seq"),
        "accepted":front.get("accepted"),
        "active":front.get("active"),
        "question":front.get("question"),
        "next":front.get("next"),
        "blockers":front.get("blockers") or [],
        "runtime":front.get("runtime"),
        "desk_status":desk,
        "raw_authority":[
            str(CURRENT),
            str(FRONT)
        ],
        "handoff_to":(
            "analyzer" if role=="coder"
            else "front" if role=="analyzer"
            else "next_front" if role=="front"
            else None
        ),
        "do_not_assume_chat_history":True
    }
    atomic_json(handoff,payload)
    item["state"]="HANDOFF_WRITTEN"
    item["clocked_out_at"]=now()
    item["end_checkpoint_seq"]=current.get("checkpoint_seq")
    item["clockout_reason"]=reason
    item["handoff_path"]=str(handoff)
    item["brain_capture"]["local_durable"]=bool(current)
    item["brain_capture"]["context_pyramid"]=bool(front)
    item["downstream_review"]="PENDING" if role in ("coder","analyzer") else "NOT_REQUIRED"
    data["sessions"][alias]=item
    data["updated_at"]=now()
    atomic_json(REGISTRY,data)
    verify(alias)
    return payload

def review_complete(alias,reviewer,notes=""):
    data=sessions()
    item=data["sessions"].get(alias)
    if not item:
        raise RuntimeError("unknown chat alias")
    item["downstream_review"]="COMPLETE"
    item["reviewed_by"]=reviewer
    item["reviewed_at"]=now()
    item["review_notes"]=notes
    data["sessions"][alias]=item
    data["updated_at"]=now()
    atomic_json(REGISTRY,data)
    return verify(alias)

def mark_mem(alias,confirmed=True):
    data=sessions()
    item=data["sessions"].get(alias)
    if not item:
        raise RuntimeError("unknown chat alias")
    item["brain_capture"]["mem_mirror"]=bool(confirmed)
    data["sessions"][alias]=item
    data["updated_at"]=now()
    atomic_json(REGISTRY,data)
    return verify(alias)

def verify(alias):
    data=sessions()
    item=data["sessions"].get(alias)
    if not item:
        raise RuntimeError("unknown chat alias")
    current=read_json(CURRENT)
    front=read_json(FRONT)
    handoff=Path(item.get("handoff_path") or "")
    gates={
        "handoff_exists":handoff.is_file(),
        "durable_checkpoint_not_older":(
            isinstance(current.get("checkpoint_seq"),int)
            and isinstance(item.get("end_checkpoint_seq"),int)
            and current.get("checkpoint_seq")>=item.get("end_checkpoint_seq")
        ),
        "local_durable_capture":bool(item.get("brain_capture",{}).get("local_durable")),
        "context_pyramid_capture":bool(item.get("brain_capture",{}).get("context_pyramid")),
        "front_available":bool(front),
        "downstream_ack":item.get("downstream_review") in ("COMPLETE","NOT_REQUIRED")
    }
    archive_safe=all(gates.values())
    delete_gates=dict(gates)
    delete_gates["mem_mirror"]=bool(item.get("brain_capture",{}).get("mem_mirror"))
    delete_safe=all(delete_gates.values())
    item["archive_gates"]=gates
    item["delete_gates"]=delete_gates
    item["archive_safe"]=archive_safe
    item["delete_safe"]=delete_safe
    if delete_safe:
        item["state"]="SAFE_TO_DELETE"
    elif archive_safe:
        item["state"]="SAFE_TO_ARCHIVE"
    elif item.get("handoff_path"):
        item["state"]="DOWNSTREAM_REVIEW_PENDING"
    data["sessions"][alias]=item
    data["updated_at"]=now()
    atomic_json(REGISTRY,data)
    rebuild_queue(data)
    return item

def rebuild_queue(data=None):
    data=data or sessions()
    rows=[]
    for alias,item in data.get("sessions",{}).items():
        if item.get("state")!="CLOCKED_IN":
            rows.append({
                "alias":alias,
                "role":item.get("role"),
                "state":item.get("state"),
                "archive_safe":item.get("archive_safe"),
                "delete_safe":item.get("delete_safe"),
                "handoff_path":item.get("handoff_path"),
                "missing_archive_gates":[k for k,v in (item.get("archive_gates") or {}).items() if not v],
                "missing_delete_gates":[k for k,v in (item.get("delete_gates") or {}).items() if not v]
            })
    atomic_json(QUEUE,{
        "schema":"BannerlordAI.ChatRetirementQueue.v1",
        "updated_at":now(),
        "ui_action_required":True,
        "note":"Office verifies safety; user performs actual ChatGPT archive/delete UI action.",
        "items":rows
    })

def report():
    data=sessions()
    rebuild_queue(data)
    return {
        "sessions":data,
        "retirement_queue":read_json(QUEUE),
        "shift_policy":read_json(SHIFT_POLICY)
    }

def main():
    if len(sys.argv)<2:
        raise SystemExit("usage: chat_lifecycle.py clock-in|clock-out|review-complete|mark-mem|verify|report ...")
    cmd=sys.argv[1]
    if cmd=="clock-in":
        out=clock_in(sys.argv[2],sys.argv[3])
    elif cmd=="clock-out":
        out=clock_out(sys.argv[2]," ".join(sys.argv[3:]) or "shift_end")
    elif cmd=="review-complete":
        out=review_complete(sys.argv[2],sys.argv[3]," ".join(sys.argv[4:]))
    elif cmd=="mark-mem":
        out=mark_mem(sys.argv[2],True)
    elif cmd=="verify":
        out=verify(sys.argv[2])
    elif cmd=="report":
        out=report()
    else:
        raise SystemExit("unknown command")
    print(json.dumps(out,indent=2,ensure_ascii=False))
    return 0

if __name__=="__main__":
    raise SystemExit(main())
