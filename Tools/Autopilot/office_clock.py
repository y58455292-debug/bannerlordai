from pathlib import Path
import datetime
import json
import os

ROOT=Path(r"D:\BannerlordAIResearch")
OFFICE=ROOT/r"Automation\Office"
POWER=OFFICE/"office_power.json"
STATUS=OFFICE/"office_clock_status.json"

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

def compute():
    now=datetime.datetime.now().astimezone()
    power=read_json(POWER)
    mode=str(power.get("mode") or "CLOSED").upper()
    if mode not in ("OPEN","OFFLINE","CLOSED"):
        mode="CLOSED"
    if mode=="OPEN":
        permissions={
          "front":{"clocked_in":True,"clock_policy":"USER_POWERED","allowed_work_classes":["status","coordination","routing","chat_registry","planning"]},
          "supervisor":{"clocked_in":True,"clock_policy":"USER_POWERED","allowed_work_classes":["scheduling","continuity","integrity","dispatch"]},
          "analyzer":{"clocked_in":True,"clock_policy":"USER_POWERED","allowed_work_classes":["evidence_review","indexing","policy","research","architecture_review","breakthrough_analysis"],"forbidden_work_classes":["live_game","deployment","save_mutation"]},
          "coder":{"clocked_in":True,"clock_policy":"USER_POWERED","allowed_work_classes":["offline_code","build","static_analysis","live_game","deployment","validator"]}
        }
    elif mode=="OFFLINE":
        permissions={
          "front":{"clocked_in":False,"clock_policy":"OFFLINE_CONTROL_SURFACE","allowed_work_classes":["status","office_power"]},
          "supervisor":{"clocked_in":True,"clock_policy":"OFFLINE_LAB","allowed_work_classes":["simulation","data_gather","continuity","integrity"]},
          "analyzer":{"clocked_in":False,"clock_policy":"OFFLINE","allowed_work_classes":[],"forbidden_work_classes":["evidence_review","research","architecture_review","breakthrough_analysis","live_game","deployment","save_mutation"]},
          "coder":{"clocked_in":False,"clock_policy":"OFFLINE","allowed_work_classes":[],"forbidden_work_classes":["offline_code","build","static_analysis","live_game","deployment","validator"]}
        }
    else:
        permissions={
          "front":{"clocked_in":False,"clock_policy":"CLOSED_CONTROL_SURFACE","allowed_work_classes":["status","office_power"]},
          "supervisor":{"clocked_in":False,"clock_policy":"CLOSED_SAFETY_ONLY","allowed_work_classes":["continuity","integrity"]},
          "analyzer":{"clocked_in":False,"clock_policy":"CLOSED","allowed_work_classes":[],"forbidden_work_classes":["evidence_review","research","architecture_review","breakthrough_analysis","live_game","deployment","save_mutation"]},
          "coder":{"clocked_in":False,"clock_policy":"CLOSED","allowed_work_classes":[],"forbidden_work_classes":["offline_code","build","static_analysis","live_game","deployment","validator"]}
        }
    for p in permissions.values():
        p.setdefault("forbidden_work_classes",[])
    out={
      "schema":"BannerlordAI.OfficeClock.v2",
      "updated_at":now.isoformat(),
      "timezone_policy":"America/Los_Angeles",
      "system_utc_offset":now.strftime("%z"),
      "office_mode":mode,
      "mode_source":"USER_FRONT_COMMAND",
      "local_time":now.strftime("%Y-%m-%d %H:%M:%S %Z"),
      "departments":permissions,
      "power":power
    }
    atomic_json(STATUS,out)
    return out

def main():
    out=compute()
    print(json.dumps(out,indent=2,ensure_ascii=False))
    return 0

if __name__=="__main__":
    raise SystemExit(main())
