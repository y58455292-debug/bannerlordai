from pathlib import Path
import datetime, json, os, subprocess, sys

ROOT=Path(r"D:\BannerlordAIResearch")
OFFICE=ROOT/r"Automation\Office"
POWER=OFFICE/"office_power.json"
CLOCK=ROOT/r"Tools\Autopilot\office_clock.py"
PLAN=ROOT/r"Tools\Autopilot\front_office_daily_plan.py"
PYRAMID=ROOT/r"Tools\Evidence\context_pyramid.py"
SESSIONS=OFFICE/"chat_sessions.json"
LIFECYCLE=ROOT/r"Tools\Autopilot\chat_lifecycle.py"

def now():
    return datetime.datetime.now().astimezone().isoformat()

def load(p):
    try: return json.loads(p.read_text(encoding="utf-8-sig")) if p.exists() else {}
    except Exception: return {}

def atomic(p,obj):
    p.parent.mkdir(parents=True,exist_ok=True)
    t=p.with_suffix(p.suffix+".tmp")
    t.write_text(json.dumps(obj,indent=2,ensure_ascii=False)+"\n",encoding="utf-8")
    os.replace(t,p)

def refresh():
    subprocess.run([sys.executable,"-X","utf8",str(CLOCK)],cwd=str(ROOT),capture_output=True,text=True)
    subprocess.run([sys.executable,"-X","utf8",str(PYRAMID),"rebuild"],cwd=str(ROOT),capture_output=True,text=True)
    subprocess.run([sys.executable,"-X","utf8",str(PLAN)],cwd=str(ROOT),capture_output=True,text=True)

def clock_out_active(reason):
    data=load(SESSIONS)
    closed=[]
    for alias,item in list((data.get("sessions") or {}).items()):
        if item.get("state")=="CLOCKED_IN":
            cp=subprocess.run([sys.executable,"-X","utf8",str(LIFECYCLE),"clock-out",alias,reason],cwd=str(ROOT),capture_output=True,text=True,errors="replace")
            closed.append({"alias":alias,"return_code":cp.returncode})
    return closed

def set_mode(mode,source):
    if source!="front":
        raise SystemExit("office power may only be changed by Front")
    mode=mode.upper()
    if mode not in ("OPEN","OFFLINE","CLOSED"):
        raise SystemExit("mode must be OPEN, OFFLINE, or CLOSED")
    closed=[]
    if mode in ("OFFLINE","CLOSED"):
        refresh()
        closed=clock_out_active("front_offline_mode" if mode=="OFFLINE" else "front_close_office")
    state={
      "schema":"BannerlordAI.OfficePower.v2",
      "mode":mode,
      "mode_source":"USER_FRONT_COMMAND",
      "updated_at":now(),
      "last_command":("open office" if mode=="OPEN" else "offline mode" if mode=="OFFLINE" else "close office"),
      "changed_by":"front",
      "rules":{
        "OPEN":"Coder advances the normal version roadmap; Analyzer reviews evidence and determines the next breakthrough/gate.",
        "OFFLINE":"Deliberate evidence-lab mode: only SIMULATION and DATA_GATHER are permitted. No code edits, builds, architecture promotion, breakthrough analysis, or roadmap mutation.",
        "CLOSED":"All project work is paused. No engineering, simulation, data gathering, analysis, or roadmap mutation; only Front power/status and safety continuity remain available."
      }
    }
    atomic(POWER,state)
    refresh()
    return {"power":state,"clocked_out_chats":closed,"activation_commands":(["Coder desk — recover current assignment and continue.","Analyzer desk — recover current intake and continue."] if mode=="OPEN" else []),"offline_lab":(str(OFFICE/"offline_lab_policy.json") if mode=="OFFLINE" else None),"closed":mode=="CLOSED"}

def main():
    if len(sys.argv)<2:
        raise SystemExit("usage: office_power.py open|offline|close|status [front]")
    cmd=sys.argv[1].lower()
    if cmd=="status":
        out={"power":load(POWER),"clock":load(OFFICE/"office_clock_status.json")}
    elif cmd=="open":
        out=set_mode("OPEN",sys.argv[2].lower() if len(sys.argv)>2 else "")
    elif cmd=="offline":
        out=set_mode("OFFLINE",sys.argv[2].lower() if len(sys.argv)>2 else "")
    elif cmd=="close":
        out=set_mode("CLOSED",sys.argv[2].lower() if len(sys.argv)>2 else "")
    else:
        raise SystemExit("unknown command")
    print(json.dumps(out,indent=2,ensure_ascii=False))
    return 0

if __name__=="__main__":
    raise SystemExit(main())
