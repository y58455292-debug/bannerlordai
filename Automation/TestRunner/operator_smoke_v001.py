from pathlib import Path
import subprocess,time,json,urllib.request,hashlib,os,datetime,shutil

ROOT=Path(r"D:\BannerlordAIResearch")
GAME=Path(r"C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord")
BIN=GAME/"bin/Win64_Shipping_Client"
EXE=BIN/"Bannerlord.exe"
SAVES=Path(r"C:\Users\csala\OneDrive\Documents\Mount and Blade II Bannerlord\Game Saves")
TARGET=SAVES/"ClanAI OPERATOR SMOKE TEST.sav"
AUTO=ROOT/"Automation/TestRunner"
COMMAND=AUTO/"command.txt"
STATUS=AUTO/"status.txt"
RUNNER_LOG=AUTO/"runner.log"
CFG=Path(r"C:\Users\csala\OneDrive\Documents\Mount and Blade II Bannerlord\Configs\BannerlordConfig.txt")
EXPECTED_SAVE="11E3F8C04A1DF3ACA4C0DA4D51F9BB912C4D2970114C66B403729F2E5577FA6B"
VALID=ROOT/"Longitudinal/LiveValidation"/("AutonomousOperator_v001_Smoke_"+datetime.datetime.now().strftime("%Y%m%d_%H%M%S_%f"))
VALID.mkdir(parents=True)

def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest().upper()
def read_status():
    if not STATUS.exists(): return {}
    d={}
    for line in STATUS.read_text(encoding="utf-8-sig",errors="replace").splitlines():
        if "=" in line:
            k,v=line.split("=",1); d[k]=v
    return d
def cmd(s):
    tmp=COMMAND.with_suffix(".tmp")
    tmp.write_text(s+"\n",encoding="utf-8")
    os.replace(tmp,COMMAND)
def wait(pred,timeout=30,step=.2):
    end=time.time()+timeout
    while time.time()<end:
        v=pred()
        if v: return v
        time.sleep(step)
    return None
def http(path):
    with urllib.request.urlopen("http://127.0.0.1:8420"+path,timeout=5) as r:
        return json.loads(r.read().decode("utf-8"))
def set_focus(value):
    t=CFG.read_text(encoding="utf-8-sig",errors="replace")
    lines=t.splitlines(); found=False
    for i,l in enumerate(lines):
        if l.startswith("StopGameOnFocusLost="):
            lines[i]="StopGameOnFocusLost="+value; found=True; break
    if not found: lines.append("StopGameOnFocusLost="+value)
    CFG.write_text("\n".join(lines)+"\n",encoding="utf-8")
def get_focus():
    for l in CFG.read_text(encoding="utf-8-sig",errors="replace").splitlines():
        if l.startswith("StopGameOnFocusLost="): return l.split("=",1)[1].strip()
    return None

assert TARGET.exists() and sha(TARGET)==EXPECTED_SAVE
for p in [COMMAND,STATUS]:
    try:p.unlink()
    except FileNotFoundError:pass
if RUNNER_LOG.exists():
    shutil.copy2(RUNNER_LOG,VALID/"runner_preexisting.log")
    RUNNER_LOG.unlink()
now=time.time(); os.utime(TARGET,(now,now))
assert max(SAVES.glob("*.sav"),key=lambda p:p.stat().st_mtime).resolve()==TARGET.resolve()
old_focus=get_focus(); set_focus("False")
modules=["Bannerlord.Harmony","Native","SandBoxCore","BirthAndDeath","CustomBattle","Sandbox","StoryMode","BannerlordInspector","NavalDLC","ClanAI"]
arg="_MODULES_*"+"*".join(modules)+"*_MODULES_"
launch=[str(EXE),"/singleplayer",arg,"/continuegame"]
(VALID/"launch.json").write_text(json.dumps({"launch":launch,"save":str(TARGET),"save_sha":sha(TARGET)},indent=2),encoding="utf-8")
proc=subprocess.Popen(launch,cwd=str(BIN))
result={"pid":proc.pid}
try:
    ready=wait(lambda:(lambda s:s if s.get("schema")=="BannerlordAI.TestRunner.v2" and s.get("campaignReady")=="True" else None)(read_status()),180,.5)
    if not ready: raise RuntimeError("operator v2 campaign not ready")
    result["ready_status"]=ready
    player=wait(lambda: http("/player"),15,.5)
    settlements=http("/settlements?limit=200")
    result["player_before"]=player
    plist=settlements.get("settlements",[])
    current=((player.get("party") or {}).get("settlement")) if isinstance(player,dict) else None
    # Prefer a town; avoid current settlement when name comparison matches.
    target=None
    for s in plist:
        if s.get("type")=="town" and s.get("name")!=current:
            target=s; break
    if target is None and plist: target=plist[0]
    if target is None: raise RuntimeError("no settlement target")
    tid=target["StringId"]
    result["target"]=target

    cmd("HOLD")
    hold=wait(lambda:(lambda s:s if s.get("lastResult")=="operator_hold" else None)(read_status()),10,.1)
    if not hold: raise RuntimeError("HOLD not acknowledged")

    cmd("MOVE_SETTLEMENT "+tid)
    move=wait(lambda:(lambda s:s if (s.get("lastResult") or "").startswith("operator_move_settlement:") else None)(read_status()),10,.1)
    if not move: raise RuntimeError("MOVE_SETTLEMENT not acknowledged")
    pos0=move.get("partyPosition")

    cmd("PLAY")
    play=wait(lambda:(lambda s:s if s.get("lastResult")=="playing" else None)(read_status()),10,.1)
    if not play: raise RuntimeError("PLAY not acknowledged")
    positions=[pos0]
    for _ in range(12):
        time.sleep(.5)
        s=read_status(); positions.append(s.get("partyPosition"))
    result["move_positions"]=positions
    moved=len(set(x for x in positions if x and x!="<none>"))>1
    result["move_detected"]=moved

    cmd("PATROL_SETTLEMENT "+tid)
    patrol=wait(lambda:(lambda s:s if (s.get("lastResult") or "").startswith("operator_patrol_settlement:") else None)(read_status()),10,.1)
    if not patrol: raise RuntimeError("PATROL_SETTLEMENT not acknowledged")
    result["patrol_ack"]=patrol.get("lastResult")

    time.sleep(2)
    cmd("HOLD")
    wait(lambda:(lambda s:s if s.get("lastResult")=="operator_hold" else None)(read_status()),10,.1)
    cmd("PAUSE")
    wait(lambda:(lambda s:s if s.get("lastResult")=="paused" else None)(read_status()),10,.1)
    result["final_status"]=read_status()
    result["player_after"]=http("/player")
finally:
    try:
        if proc.poll() is None:
            cmd("EXIT_NOSAVE")
            end=time.time()+30
            while time.time()<end and proc.poll() is None: time.sleep(.25)
    finally:
        set_focus(old_focus or "True")
result["exit_code"]=proc.poll()
result["save_sha_post"]=sha(TARGET)
result["save_unchanged"]=result["save_sha_post"]==EXPECTED_SAVE
if RUNNER_LOG.exists(): shutil.copy2(RUNNER_LOG,VALID/"runner.log")
if STATUS.exists(): shutil.copy2(STATUS,VALID/"status_final.txt")
(VALID/"final_result.json").write_text(json.dumps(result,indent=2),encoding="utf-8")
print(json.dumps({"validation":str(VALID),**result},indent=2))
if not result.get("move_detected"): raise SystemExit(3)
if not result["save_unchanged"]: raise SystemExit(4)
