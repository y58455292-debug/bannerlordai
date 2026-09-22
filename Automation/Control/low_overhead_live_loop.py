import json, pathlib, time, subprocess, sys, urllib.request, urllib.parse, traceback
ROOT=pathlib.Path(r"D:\BannerlordAIResearch")
TR=ROOT/"Automation/TestRunner"
DP=ROOT/"Automation/DecisionPolicy"
VAL=ROOT/"Longitudinal/LiveValidation"
CTRL=ROOT/"Automation/Control"
sys.path.insert(0,str(CTRL))
from command_bus import send_active

STATUS=TR/"status.txt"
DECISION=TR/"decision_state.txt"
LOG=CTRL/"low_overhead_loop.jsonl"
HEART=CTRL/"low_overhead_heartbeat.json"
STOP=CTRL/"STOP_LOW_OVERHEAD_LOOP"

def kv(path):
    d={}
    try:
        for line in path.read_text(encoding="utf-8-sig",errors="replace").splitlines():
            if "=" in line:
                k,v=line.split("=",1); d[k]=v
    except Exception: pass
    return d

def emit(kind,**kw):
    row={"wall_unix":time.time(),"kind":kind,**kw}
    with LOG.open("a",encoding="utf-8") as f:
        f.write(json.dumps(row,separators=(",",":"))+"\n")
    HEART.write_text(json.dumps(row,indent=2),encoding="utf-8")

def http(path,params=None):
    if params:path+="?"+urllib.parse.urlencode(params)
    with urllib.request.urlopen("http://127.0.0.1:8420"+path,timeout=2) as r:
        return json.loads(r.read().decode())

def deep_failure_packet(reason,st):
    packet={"wall_unix":time.time(),"reason":reason,"status":st}
    for p in [
        "type:TaleWorlds.Core.Game.Current.GameStateManager.ActiveStateDisabledByUser",
        "Campaign.Current.IsMainPartyWaiting",
        "Campaign.Current.MapTimeTracker.NumTicks",
        "Campaign.Current.MapTimeTracker.DeltaTimeInTicks",
        "type:TaleWorlds.CampaignSystem.CampaignTime.Now.ToHours",
        "Campaign.Current.TimeControlMode",
        "Campaign.Current.TimeControlModeLock"
    ]:
        try: packet[p]=http("/eval",{"path":p})
        except Exception as e: packet[p]={"error":type(e).__name__+": "+str(e)}
    try: packet["screen"]=http("/screen")
    except Exception as e: packet["screen"]={"error":str(e)}
    p=VAL/f"failure_packet_{int(time.time()*1000)}.json"
    p.write_text(json.dumps(packet,indent=2),encoding="utf-8")
    return str(p)

if STOP.exists(): STOP.unlink()
emit("loop_started")
last_decision=None
last_hours=None
same_since=None
last_heart=0.0

while not STOP.exists():
    st=kv(STATUS)
    ds=kv(DECISION)

    # Fast path: decisions only.
    if ds.get("type")=="incident":
        sig=(ds.get("title"),ds.get("description"),ds.get("optionCount"))
        if sig!=last_decision:
            last_decision=sig
            t0=time.perf_counter()
            try:
                cp=subprocess.run(
                    [sys.executable,"-X","utf8",str(DP/"fast_live_decider.py")],
                    capture_output=True,text=True,timeout=6
                )
                emit("incident_processed",title=ds.get("title"),
                     elapsed_ms=round((time.perf_counter()-t0)*1000,3),
                     rc=cp.returncode,stdout=cp.stdout[-2500:],stderr=cp.stderr[-1000:])
            except Exception as e:
                emit("incident_error",title=ds.get("title"),error=type(e).__name__+": "+str(e))
    else:
        last_decision=None

    # Cheap no-progress invariant from status file only.
    playing=(st.get("timeControl") not in (None,"<none>","Stop"))
    unblocked=(st.get("blockers") in (None,"<none>"))
    hours=st.get("campaignHours")
    if playing and unblocked and hours not in (None,"<none>"):
        if hours==last_hours:
            same_since=same_since or time.monotonic()
            if time.monotonic()-same_since>=2.5:
                try: send_active("PAUSE","low_overhead_watchdog")
                except Exception: pass
                packet=deep_failure_packet("NO_PROGRESS_2_5S",st)
                emit("stall_captured",packet=packet,hours=hours)
                same_since=None
                time.sleep(1.0)
        else:
            same_since=None
    else:
        same_since=None
    last_hours=hours

    if time.monotonic()-last_heart>=5.0:
        HEART.write_text(json.dumps({
            "wall_unix":time.time(),"kind":"watching",
            "campaignHours":hours,"timeControl":st.get("timeControl"),
            "blockers":st.get("blockers"),"decision":ds.get("type")
        },indent=2),encoding="utf-8")
        last_heart=time.monotonic()

    time.sleep(0.10)

emit("loop_stopped")
