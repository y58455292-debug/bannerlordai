from pathlib import Path
import json, subprocess, sys, time

ROOT=Path(r"D:\BannerlordAIResearch")
OFFICE=ROOT/r"Automation\Office"
POWER=OFFICE/"office_power.json"
CLOCK=OFFICE/"office_clock_status.json"
REG=ROOT/r"Automation\Autopilot\registry.json"
CLOCK_SCRIPT=ROOT/r"Tools\Autopilot\office_clock.py"
POLICY=ROOT/r"Tools\Autopilot\office_dispatch_policy.py"

def load(p):
    return json.loads(p.read_text(encoding="utf-8-sig"))

def write(p,obj):
    p.write_text(json.dumps(obj,indent=2)+"\n",encoding="utf-8")

def run(*args):
    cp=subprocess.run([sys.executable,"-X","utf8",*map(str,args)],cwd=str(ROOT),capture_output=True,text=True)
    try: data=json.loads(cp.stdout)
    except Exception: data={"raw":(cp.stdout+cp.stderr)[-1000:]}
    return cp.returncode,data

orig_power=load(POWER)
orig_reg=load(REG)
started=time.perf_counter()
results={}

try:
    reg=json.loads(json.dumps(orig_reg))
    actions=reg.setdefault("actions",{})
    actions["_test_open_code"]={"enabled":True,"command":["python","-c","print('noop')"],"owner":"coder","work_class":"OFFLINE_CODE","may_launch_game":False}
    actions["_test_offline_sim"]={"enabled":True,"command":["python","-c","print('noop')"],"owner":"supervisor","work_class":"SIMULATION","may_launch_game":True}
    actions["_test_offline_gather"]={"enabled":True,"command":["python","-c","print('noop')"],"owner":"supervisor","work_class":"DATA_GATHER","may_launch_game":False}
    write(REG,reg)

    for mode in ("OPEN","OFFLINE","CLOSED"):
        p=dict(orig_power); p["mode"]=mode; p["mode_source"]="TEST"
        write(POWER,p)
        rc_clock,_=run(CLOCK_SCRIPT)
        clock=load(CLOCK)
        row={"clock_rc":rc_clock,"office_mode":clock.get("office_mode"),"departments":clock.get("departments")}
        for aid in ("_test_open_code","_test_offline_sim","_test_offline_gather"):
            rc,data=run(POLICY,aid)
            row[aid]={"allowed":rc==0 and data.get("allowed") is True,"reason":data.get("reason")}
        results[mode]=row

    checks={
      "open_mode":results["OPEN"]["office_mode"]=="OPEN",
      "open_coder_clocked":results["OPEN"]["departments"]["coder"]["clocked_in"] is True,
      "open_analyzer_clocked":results["OPEN"]["departments"]["analyzer"]["clocked_in"] is True,
      "open_code_allowed":results["OPEN"]["_test_open_code"]["allowed"] is True,
      "open_sim_blocked":results["OPEN"]["_test_offline_sim"]["allowed"] is False,
      "open_gather_blocked":results["OPEN"]["_test_offline_gather"]["allowed"] is False,
      "offline_mode":results["OFFLINE"]["office_mode"]=="OFFLINE",
      "offline_coder_out":results["OFFLINE"]["departments"]["coder"]["clocked_in"] is False,
      "offline_analyzer_out":results["OFFLINE"]["departments"]["analyzer"]["clocked_in"] is False,
      "offline_code_blocked":results["OFFLINE"]["_test_open_code"]["allowed"] is False,
      "offline_sim_allowed":results["OFFLINE"]["_test_offline_sim"]["allowed"] is True,
      "offline_gather_allowed":results["OFFLINE"]["_test_offline_gather"]["allowed"] is True,
      "closed_mode":results["CLOSED"]["office_mode"]=="CLOSED",
      "closed_coder_out":results["CLOSED"]["departments"]["coder"]["clocked_in"] is False,
      "closed_analyzer_out":results["CLOSED"]["departments"]["analyzer"]["clocked_in"] is False,
      "closed_supervisor_out":results["CLOSED"]["departments"]["supervisor"]["clocked_in"] is False,
      "closed_code_blocked":results["CLOSED"]["_test_open_code"]["allowed"] is False,
      "closed_sim_blocked":results["CLOSED"]["_test_offline_sim"]["allowed"] is False,
      "closed_gather_blocked":results["CLOSED"]["_test_offline_gather"]["allowed"] is False
    }
finally:
    write(REG,orig_reg); write(POWER,orig_power); run(CLOCK_SCRIPT)

elapsed=(time.perf_counter()-started)*1000
out={"schema":"BannerlordAI.OfficePowerProductivityTest.v2","pass":all(checks.values()),"elapsed_ms":round(elapsed,2),"checks":checks,"results":results,"restored_mode":load(POWER).get("mode")}
print(json.dumps(out,indent=2))
raise SystemExit(0 if out["pass"] else 2)
