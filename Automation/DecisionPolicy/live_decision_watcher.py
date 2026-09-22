import pathlib,time,subprocess,sys,json,traceback
ROOT=pathlib.Path(r"D:\BannerlordAIResearch")
TR=ROOT/"Automation/TestRunner"
DP=ROOT/"Automation/DecisionPolicy"
LOG=DP/"live_decision_watcher.jsonl"
HEART=DP/"live_decision_watcher_heartbeat.json"
STOP=DP/"STOP_WATCHER"

def read_kv(path):
    d={}
    try:
        for line in pathlib.Path(path).read_text(encoding="utf-8-sig",errors="replace").splitlines():
            if "=" in line:
                k,v=line.split("=",1); d[k]=v
    except Exception:
        pass
    return d

def emit(kind,**kw):
    row={"wall_unix":time.time(),"kind":kind,**kw}
    with LOG.open("a",encoding="utf-8") as f:
        f.write(json.dumps(row,ensure_ascii=False)+"\n")
    HEART.write_text(json.dumps(row,indent=2),encoding="utf-8")

last_sig=None
if STOP.exists():
    STOP.unlink()

emit("watcher_started")
while not STOP.exists():
    ds=read_kv(TR/"decision_state.txt")
    st=read_kv(TR/"status.txt")
    if ds.get("type")=="incident":
        sig=(ds.get("title"),ds.get("description"),ds.get("optionCount"))
        if sig!=last_sig:
            last_sig=sig
            t0=time.perf_counter()
            try:
                cp=subprocess.run(
                    [sys.executable,"-X","utf8",str(DP/"fast_live_decider.py")],
                    capture_output=True,text=True,timeout=8
                )
                elapsed=(time.perf_counter()-t0)*1000
                emit("decision_processed",title=ds.get("title"),
                     elapsed_ms=round(elapsed,3),returncode=cp.returncode,
                     stdout=cp.stdout[-4000:],stderr=cp.stderr[-2000:])
            except Exception as e:
                emit("decision_error",title=ds.get("title"),
                     error=type(e).__name__+": "+str(e),
                     trace=traceback.format_exc()[-4000:])
    else:
        last_sig=None
        # Lightweight heartbeat, not every poll.
        if int(time.time()*2)%20==0:
            HEART.write_text(json.dumps({
                "wall_unix":time.time(),
                "kind":"watching",
                "campaignHours":st.get("campaignHours"),
                "blockers":st.get("blockers")
            },indent=2),encoding="utf-8")
    time.sleep(0.05)

emit("watcher_stopped")
