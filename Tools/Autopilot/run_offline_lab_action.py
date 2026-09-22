from pathlib import Path
import datetime, json, os, subprocess, sys, traceback

ROOT=Path(r"D:\BannerlordAIResearch")
REG=ROOT/r"Automation\Autopilot\registry.json"
STATE=ROOT/r"Automation\Autopilot\state.json"
LOGS=ROOT/r"Automation\Autopilot\logs"
LAB=ROOT/r"Automation\Office\offline_lab_status.json"

def load(p):
    return json.loads(p.read_text(encoding="utf-8-sig"))

def atomic(p,obj):
    p.parent.mkdir(parents=True,exist_ok=True)
    t=p.with_suffix(p.suffix+".tmp")
    t.write_text(json.dumps(obj,indent=2)+"\n",encoding="utf-8")
    os.replace(t,p)

def stamp():
    return datetime.datetime.now().astimezone().isoformat()

def main():
    if len(sys.argv)!=2: raise SystemExit("usage: run_offline_lab_action.py <action_id>")
    aid=sys.argv[1]; reg=load(REG); state=load(STATE)
    action=(reg.get("actions") or {}).get(aid)
    if not isinstance(action,dict): raise SystemExit("unknown action")
    if action.get("owner")!="supervisor" or action.get("work_class") not in ("SIMULATION","DATA_GATHER"):
        raise SystemExit("not an offline lab action")
    state["status"]="RUNNING"; state["runner_pid"]=os.getpid(); state["started_at"]=stamp()
    atomic(STATE,state)
    LOGS.mkdir(parents=True,exist_ok=True)
    log=LOGS/(datetime.datetime.now().strftime("%Y%m%d_%H%M%S")+"_"+aid+".log")
    rc=999
    try:
        with log.open("w",encoding="utf-8") as f:
            cp=subprocess.run(action.get("command") or [],cwd=str(ROOT),stdout=f,stderr=subprocess.STDOUT,text=True)
            rc=cp.returncode
    except Exception:
        with log.open("a",encoding="utf-8") as f: f.write(traceback.format_exc())
        rc=998
    state=load(STATE); state["completed_at"]=stamp(); state["return_code"]=rc; state["log_path"]=str(log); state["runner_pid"]=None
    state["status"]="DONE" if rc==0 else "FAILED"; state["last_result"]="SUCCESS" if rc==0 else "FAILED"
    atomic(STATE,state)
    out={
      "schema":"BannerlordAI.OfflineLabStatus.v1","updated_at":stamp(),"action_id":aid,
      "work_class":action.get("work_class"),"return_code":rc,"result":state["last_result"],
      "log_path":str(log),"evidence_only":True,
      "promotion_allowed":False,
      "rule":"Offline lab results require later OPEN Analyzer review before any roadmap/code/architecture promotion."
    }
    atomic(LAB,out)
    print(json.dumps(out,indent=2))
    return rc

if __name__=="__main__":
    raise SystemExit(main())
