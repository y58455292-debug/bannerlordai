from pathlib import Path
import datetime
import json
import os
import subprocess
import sys

ROOT=Path(r"D:\BannerlordAIResearch")
OFFICE=ROOT/r"Automation\Office"
CODER=OFFICE/"coder_status.json"
ANALYZER=OFFICE/"analyzer_status.json"
PYRAMID=ROOT/r"Tools\Evidence\context_pyramid.py"
VALIDATIONS=ROOT/r"Longitudinal\LiveValidation"
AUTOPILOT=ROOT/r"Automation\Autopilot\state.json"
CURRENT=ROOT/r"Longitudinal\EvidenceIndex\HandoffV3\current.json"
HANDOFF_CHECKPOINT=ROOT/r"Tools\Handoff\handoff_checkpoint.py"
LOOP_SYNC=ROOT/r"Tools\Autopilot\sync_thinking_loop.py"

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

def newest_validation(not_before_epoch=None):
    if not VALIDATIONS.exists():
        return None
    dirs=[x for x in VALIDATIONS.iterdir() if x.is_dir()]
    if isinstance(not_before_epoch,(int,float)):
        dirs=[x for x in dirs if x.stat().st_mtime >= float(not_before_epoch)-2.0]
    if not dirs:
        return None
    d=max(dirs,key=lambda x:x.stat().st_mtime)
    v=d/"validation.json"
    result={"path":str(d),"validation_json":str(v) if v.exists() else None}
    if v.exists():
        j=read_json(v)
        result["schema"]=j.get("schema")
        result["result"]=j.get("result")
        result["error"]=j.get("error")
    return result

def main():
    coder=read_json(CODER)
    analyzer=read_json(ANALYZER)
    result_id=coder.get("result_id")
    if not result_id:
        print(json.dumps({"processed":False,"reason":"NO_CODER_RESULT"}))
        return 0
    if analyzer.get("last_processed_result_id")==result_id:
        print(json.dumps({"processed":False,"reason":"ALREADY_INDEXED","result_id":result_id}))
        return 0

    cp=subprocess.run(
        [sys.executable,"-X","utf8",str(PYRAMID),"rebuild"],
        cwd=str(ROOT),capture_output=True,text=True,errors="replace"
    )
    auto=read_json(AUTOPILOT)
    dispatch_epoch=None
    try:
        dispatched=auto.get("dispatched_at")
        if dispatched:
            dispatch_epoch=datetime.datetime.fromisoformat(dispatched).timestamp()
    except Exception:
        dispatch_epoch=None
    validation=newest_validation(dispatch_epoch)
    vresult=(validation or {}).get("result")
    rc=coder.get("return_code")

    if isinstance(vresult,str) and vresult.startswith("PASS_"):
        classification="VALIDATOR_PASS_PENDING_ANALYZER_REVIEW"
    elif vresult=="FAIL":
        classification="VALIDATOR_FAIL_PENDING_ANALYZER_REVIEW"
    elif rc==0:
        classification="EXECUTION_PASS_PENDING_ANALYZER_REVIEW"
    else:
        classification="EXECUTION_FAIL_PENDING_ANALYZER_REVIEW"

    out={
        "schema":"BannerlordAI.Office.AnalyzerStatus.v1",
        "updated_at":now(),
        "state":"INDEXED_PENDING_REASONING_REVIEW",
        "last_processed_result_id":result_id,
        "source_action_id":coder.get("action_id"),
        "source_log":coder.get("log_path"),
        "coder_return_code":rc,
        "classification":classification,
        "latest_validation":validation,
        "context_pyramid_refresh":{
            "return_code":cp.returncode,
            "stdout_tail":cp.stdout[-1200:],
            "stderr_tail":cp.stderr[-1200:]
        },
        "agent_review_required":True,
        "required_tasks":[
            "verify semantic result against raw validator/log evidence",
            "separate observation/inference and detect harness false negatives",
            "promote or reject milestone only when evidence supports it",
            "update architecture lessons/do-not-repeat/policies/efficiency cheatsheets when warranted",
            "set the next smallest acceptance gate and register safe deterministic work if possible"
        ]
    }
    atomic_json(ANALYZER,out)

    # Terminal execution must immediately transition durable control to VERIFY.
    # Indexing alone is not a completed handoff and must never strand the loop.
    cur=read_json(CURRENT)
    seq=cur.get("checkpoint_seq")
    patch={
      "next_action":(
        "Analyzer VERIFY phase reviews the latest Coder terminal evidence for " +
        str(coder.get("action_id") or "<unknown>") +
        ". Classify PASS/FAIL/HARNESS_FALSE_NEGATIVE/UNKNOWN from raw evidence, "
        "then route the smallest safe next step without waiting for a user check-in."
      ),
      "blockers":cur.get("blockers") or []
    }
    patch_path=ROOT/r"workspace\auto_analyzer_verify_transition.json"
    atomic_json(patch_path,patch)
    checkpoint=None
    if isinstance(seq,int):
        hcp=subprocess.run(
            [sys.executable,"-X","utf8",str(HANDOFF_CHECKPOINT),
             "checkpoint","--patch-file",str(patch_path),
             "--event-type","AUTO_ANALYZER_INTAKE",
             "--message","Terminal Coder evidence indexed; automatically transition unified Front Controller to Analyzer VERIFY.",
             "--action-id","auto-analyzer-intake-"+str(result_id).replace("|","-"),
             "--expected-seq",str(seq)],
            cwd=str(ROOT),capture_output=True,text=True,errors="replace")
        if hcp.returncode==0:
            try: checkpoint=json.loads(hcp.stdout).get("checkpoint_seq")
            except Exception: checkpoint=None
        out["verify_transition"]={
          "return_code":hcp.returncode,
          "checkpoint_seq":checkpoint,
          "stdout_tail":hcp.stdout[-1000:],
          "stderr_tail":hcp.stderr[-1000:]
        }
        atomic_json(ANALYZER,out)
        if hcp.returncode==0:
            subprocess.run(
                [sys.executable,"-X","utf8",str(LOOP_SYNC)],
                cwd=str(ROOT),capture_output=True,text=True,errors="replace")

    print(json.dumps({"processed":True,"classification":classification,"validation":validation,"verify_checkpoint":checkpoint},indent=2))
    return 0

if __name__=="__main__":
    raise SystemExit(main())
