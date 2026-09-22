from pathlib import Path
import json, subprocess, sys

ROOT=Path(r"D:\BannerlordAIResearch")
OFFICE=ROOT/r"Automation\Office"
CURRENT=ROOT/r"Longitudinal\EvidenceIndex\HandoffV3\current.json"
PROTOCOL_CHECK=ROOT/r"Tools\Autopilot\check_worker_protocol.py"
CODER_INTAKE=OFFICE/r"Handoffs\coder_to_analyzer.json"

def read_json(p):
    try: return json.loads(p.read_text(encoding="utf-8-sig")) if p.exists() else {}
    except Exception as ex: return {"_error":type(ex).__name__+": "+str(ex)}

def latest():
    candidates=[OFFICE/r"Handoffs\analyzer_to_front.json",OFFICE/r"Handoffs\analyzer_to_coder.json"]
    ranked=[]
    for p in candidates:
        h=read_json(p); seq=(h.get("completion_receipt") or {}).get("checkpoint_seq")
        ranked.append((seq if isinstance(seq,int) else -1,p,h))
    return max(ranked,key=lambda x:x[0])

def protocol_ok():
    p=subprocess.run([sys.executable,"-X","utf8",str(PROTOCOL_CHECK),"analyzer"],capture_output=True,text=True)
    try: data=json.loads(p.stdout)
    except Exception: data={"pass":False,"raw":p.stdout[-500:]}
    return p.returncode==0 and data.get("pass") is True, data

def main():
    seq,hpath,h=latest(); cur=read_json(CURRENT); r=h.get("completion_receipt") or {}
    intake=read_json(CODER_INTAKE); intake_seq=(intake.get("completion_receipt") or {}).get("checkpoint_seq")
    pok,pdetail=protocol_ok()
    checks={
        "handoff_exists":hpath.exists(),
        "terminal_status_valid":r.get("terminal_status") in ("PASS","FAIL","BLOCKED"),
        "result_pointer_present":bool(r.get("result_pointer")),
        "checkpoint_seq_valid":isinstance(r.get("checkpoint_seq"),int),
        "durable_not_older":isinstance(r.get("checkpoint_seq"),int) and isinstance(cur.get("checkpoint_seq"),int) and cur.get("checkpoint_seq")>=r.get("checkpoint_seq"),
        "owner_released":r.get("owner_released") is True,
        "to_role_valid":h.get("to_role") in ("front","coder"),
        "covers_latest_coder_intake":isinstance(r.get("checkpoint_seq"),int) and (not isinstance(intake_seq,int) or r.get("checkpoint_seq")>=intake_seq),
        "protocol_compliance":pok
    }
    ok=all(checks.values())
    print(json.dumps({"schema":"BannerlordAI.AnalyzerCompletionCheck.v4","pass":ok,"checks":checks,"protocol":pdetail,"handoff":str(hpath),"durable_checkpoint_seq":cur.get("checkpoint_seq"),"latest_coder_intake_seq":intake_seq,"receipt":r},indent=2))
    return 0 if ok else 2
if __name__=="__main__": raise SystemExit(main())
