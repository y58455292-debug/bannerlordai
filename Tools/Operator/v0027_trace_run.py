from pathlib import Path
import json, subprocess, sys, time, urllib.request, urllib.parse, hashlib, os

ROOT=Path(r"D:\BannerlordAIResearch")
TR=ROOT/"Automation/TestRunner"
DP=ROOT/"Automation/DecisionPolicy"
CTRL=ROOT/"Automation/Control"
VAL=ROOT/"Longitudinal/LiveValidation"
sys.path.insert(0,str(CTRL))
from command_bus import send_active

LOG=TR/"runner.log"
STATUS=TR/"status.txt"
DECISION=TR/"decision_state.txt"
GAME_SAVE=Path(r"C:\Users\csala\OneDrive\Documents\Mount and Blade II Bannerlord\Game Saves\ClanAI MANAN BRANCH ROOT.sav")
ORIGINAL=Path(r"C:\Users\csala\OneDrive\Documents\Mount and Blade II Bannerlord\Game Saves\BLOOD FUED.sav")
EXPECTED_SAVE_SHA="94DFE7F05D89C6E4CC328A416943608713ECFFDF96A572AAEA43A9BDD5CD2DAA"

def sha(p):
    h=hashlib.sha256()
    with p.open("rb") as f:
        for c in iter(lambda:f.read(1024*1024),b""):h.update(c)
    return h.hexdigest().upper()

def read_kv(p):
    d={}
    try:
        for line in p.read_text(encoding="utf-8-sig",errors="replace").splitlines():
            if "=" in line:
                k,v=line.split("=",1);d[k]=v
    except FileNotFoundError:pass
    return d

def http(path,params=None):
    if params:path+="?"+urllib.parse.urlencode(params)
    with urllib.request.urlopen("http://127.0.0.1:8420"+path,timeout=2) as r:
        return json.loads(r.read().decode())

def pids():
    cp=subprocess.run(["tasklist","/FI","IMAGENAME eq Bannerlord.exe","/FO","CSV","/NH"],capture_output=True,text=True)
    return "Bannerlord.exe" in cp.stdout

def toggle_recording():
    ps=ROOT/"workspace/_trace_record_toggle.ps1"
    ps.write_text("""$w=New-Object -ComObject WScript.Shell
$p=Get-Process Bannerlord -ErrorAction SilentlyContinue | Select-Object -First 1
if ($p) { $null=$w.AppActivate($p.Id); Start-Sleep -Milliseconds 300; $w.SendKeys('%{F9}'); Start-Sleep -Milliseconds 500 }
""",encoding="utf-8")
    subprocess.run(["powershell","-NoProfile","-ExecutionPolicy","Bypass","-File",str(ps)],
                   capture_output=True,text=True,timeout=8)

def eval_path(path):
    return http("/eval",{"path":path})

def wait(cond,seconds,interval=.05):
    end=time.time()+seconds
    while time.time()<end:
        try:
            x=cond()
            if x:return x
        except Exception:pass
        time.sleep(interval)
    return None

def main():
    if not pids():raise RuntimeError("Bannerlord must already be live before trace controller starts")
    started=time.time()
    base_log=LOG.stat().st_size if LOG.exists() else 0
    trace={"schema":"BannerlordAI.v0027.TraceRun.v1","started_unix":started}

    toggle_recording()
    trace["recording_started_unix"]=time.time()

    send_active("PATROL_SETTLEMENT town_ES7","v0027_trace_controller")
    time.sleep(.35)
    send_active("AUTO_PLAY","v0027_trace_controller")

    incident=wait(lambda: read_kv(DECISION) if read_kv(DECISION).get("type")=="incident" else None,60)
    if not incident:
        raise RuntimeError("No Incident appeared within 60 seconds")
    trace["incident_open_unix"]=time.time()
    trace["incident_title"]=incident.get("title")

    cp=subprocess.run([sys.executable,"-X","utf8",str(DP/"fast_live_decider.py")],
                      capture_output=True,text=True,timeout=8)
    trace["decider_returncode"]=cp.returncode
    trace["decider_stdout"]=cp.stdout[-6000:]
    trace["decision_done_unix"]=time.time()

    # Observe for delayed disable/stall. v0.2.7 logs Register/Unregister caller stacks.
    samples=[]
    last_hours=None
    same_since=None
    stall=False
    end=time.time()+45
    while time.time()<end and pids():
        st=read_kv(STATUS)
        hours=st.get("campaignHours")
        disabled=None
        try:
            disabled=(eval_path("type:TaleWorlds.Core.Game.Current.GameStateManager.ActiveStateDisabledByUser").get("value"))
        except Exception:pass
        samples.append({"t":time.time(),"hours":hours,"disabled":disabled,
                        "mode":st.get("timeControl"),"blockers":st.get("blockers")})
        if hours==last_hours and st.get("timeControl")!="Stop" and st.get("blockers")=="<none>":
            same_since=same_since or time.time()
            if time.time()-same_since>=2.0:
                stall=True;break
        else:
            same_since=None
        last_hours=hours
        time.sleep(.2)
    trace["stall_detected"]=stall
    trace["samples_tail"]=samples[-50:]

    # Freeze before collecting logs.
    send_active("PAUSE","v0027_trace_controller")
    time.sleep(.4)
    if LOG.exists():
        with LOG.open("rb") as f:
            f.seek(base_log)
            trace["runner_log_delta"]=f.read().decode("utf-8",errors="replace")[-30000:]

    out=VAL/f"v0027_state_disable_trace_{int(time.time())}.json"
    out.write_text(json.dumps(trace,indent=2),encoding="utf-8")

    send_active("EXIT_NOSAVE","v0027_trace_controller")
    wait(lambda: not pids(),20,.1)
    toggle_recording()  # If game is already gone this is a no-op.

    clone_sha=sha(GAME_SAVE); orig_sha=sha(ORIGINAL)
    trace["clone_sha256"]=clone_sha
    trace["original_sha256"]=orig_sha
    trace["save_integrity_pass"]=(clone_sha==EXPECTED_SAVE_SHA and orig_sha==EXPECTED_SAVE_SHA)
    trace["finished_unix"]=time.time()
    out.write_text(json.dumps(trace,indent=2),encoding="utf-8")
    print(json.dumps({"report":str(out),"incident":trace.get("incident_title"),
                      "stall":stall,"save_integrity":trace["save_integrity_pass"]},indent=2))

if __name__=="__main__":
    main()
