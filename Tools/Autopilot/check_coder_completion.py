from pathlib import Path
import json, subprocess, sys

ROOT=Path(r"D:\BannerlordAIResearch")
HANDOFF=ROOT/r"Automation\Office\Handoffs\coder_to_analyzer.json"
CURRENT=ROOT/r"Longitudinal\EvidenceIndex\HandoffV3\current.json"
OWNER=ROOT/r"workspace\DEPLOYMENT_OWNER.json"
PROTOCOL_CHECK=ROOT/r"Tools\Autopilot\check_worker_protocol.py"

def read_json(p):
    try:
        return json.loads(p.read_text(encoding="utf-8-sig")) if p.exists() else {}
    except Exception as ex:
        return {"_error":type(ex).__name__+": "+str(ex)}

def protocol_ok():
    p=subprocess.run([sys.executable,"-X","utf8",str(PROTOCOL_CHECK),"coder"],capture_output=True,text=True)
    try:
        data=json.loads(p.stdout)
    except Exception:
        data={"pass":False,"raw":p.stdout[-500:]}
    return p.returncode==0 and data.get("pass") is True, data

def main():
    h=read_json(HANDOFF); cur=read_json(CURRENT); r=h.get("completion_receipt") or {}
    pok,pdetail=protocol_ok()
    checks={
        "handoff_exists":HANDOFF.exists(),
        "terminal_status_valid":r.get("terminal_status") in ("PASS","FAIL","BLOCKED"),
        "result_pointer_present":bool(r.get("result_pointer")),
        "handoff_pointer_matches":str(r.get("handoff_pointer") or "").lower()==str(HANDOFF).lower(),
        "checkpoint_seq_valid":isinstance(r.get("checkpoint_seq"),int),
        "durable_not_older":isinstance(r.get("checkpoint_seq"),int) and isinstance(cur.get("checkpoint_seq"),int) and cur.get("checkpoint_seq")>=r.get("checkpoint_seq"),
        "owner_released":r.get("owner_released") is True,
        "no_owner_file":not OWNER.exists(),
        "protocol_compliance":pok
    }
    ok=all(checks.values())
    print(json.dumps({"schema":"BannerlordAI.CoderCompletionCheck.v2","pass":ok,"checks":checks,"protocol":pdetail,"handoff":str(HANDOFF),"durable_checkpoint_seq":cur.get("checkpoint_seq"),"receipt":r},indent=2))
    return 0 if ok else 2
if __name__=="__main__": raise SystemExit(main())
