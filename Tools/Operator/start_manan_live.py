from pathlib import Path
import csv, io, json, os, subprocess, time, urllib.request
import xml.etree.ElementTree as ET

ROOT=Path(r"D:\BannerlordAIResearch")
GAME=Path(r"C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord")
BIN=GAME/"bin/Win64_Shipping_Client"
EXE=BIN/"Bannerlord.exe"
CFG=Path(r"C:\Users\csala\OneDrive\Documents\Mount and Blade II Bannerlord\Configs\LauncherData.xml")
AUTO=ROOT/"Automation/TestRunner"
STATUS=AUTO/"status.txt"
COMMAND=AUTO/"command.txt"
TARGET="ClanAI MANAN BRANCH ROOT"

def bannerlord_pids():
    cp=subprocess.run(["tasklist","/FI","IMAGENAME eq Bannerlord.exe","/FO","CSV","/NH"],capture_output=True,text=True)
    out=[]
    for row in csv.reader(io.StringIO(cp.stdout)):
        if len(row)>=2 and row[0].lower()=="bannerlord.exe":
            try: out.append(int(row[1]))
            except: pass
    return out

def selected_modules():
    tree=ET.parse(CFG); ids=[]
    for node in tree.findall(".//SingleplayerData/ModDatas/UserModData"):
        if node.findtext("IsSelected","").strip().lower()=="true":
            ident=node.findtext("Id","").strip()
            if ident and ident not in ids: ids.append(ident)
    return ids
def write_command(text):
    tmp=COMMAND.with_suffix(".tmp")
    tmp.write_text(text+"\n",encoding="utf-8")
    os.replace(tmp,COMMAND)

def read_status():
    if not STATUS.exists(): return {}
    d={}
    for line in STATUS.read_text(encoding="utf-8-sig",errors="replace").splitlines():
        if "=" in line:
            k,v=line.split("=",1); d[k]=v
    return d

def http(path):
    with urllib.request.urlopen("http://127.0.0.1:8420"+path,timeout=3) as r:
        return json.loads(r.read().decode("utf-8"))

existing=bannerlord_pids()
if existing:
    raise RuntimeError("Preflight found existing Bannerlord processes: "+repr(existing))

for p in (STATUS,COMMAND):
    try:p.unlink()
    except FileNotFoundError:pass

mods=selected_modules()
if "ClanAI" not in mods or "BannerlordInspector" not in mods:
    raise RuntimeError("Expected ClanAI + BannerlordInspector selected")
arg="_MODULES_*"+"*".join(mods)+"*_MODULES_"
bootstrap=subprocess.Popen([str(EXE),"/singleplayer",arg],cwd=str(BIN))
print(json.dumps({"phase":"bootstrap_started","pid":bootstrap.pid,"modules":mods}),flush=True)
end=time.time()+150
ready=None
while time.time()<end:
    pids=bannerlord_pids()
    s=read_status()
    if s.get("schema")=="BannerlordAI.TestRunner.v2":
        ready=s; break
    if not pids and bootstrap.poll() is not None:
        raise RuntimeError("All Bannerlord processes exited before operator module loaded")
    time.sleep(.25)
if ready is None:
    raise RuntimeError("Operator v2 status did not appear")

# A fresh runner status only proves the module loaded. Do not issue LOAD_SAVE until
# Bannerlord's actual main-menu screen exists; v0.2.5 can report status much earlier.
end=time.time()+120
main_menu=None
while time.time()<end:
    pids=bannerlord_pids()
    if not pids:
        raise RuntimeError("Bannerlord exited before main menu became ready")
    try:
        scr=http("/screen")
        top=((scr.get("screen") or {}).get("topScreen") or "")
        if top.endswith("GauntletInitialScreen"):
            main_menu=scr
            break
    except Exception:
        pass
    time.sleep(.25)
if main_menu is None:
    raise RuntimeError("Bannerlord main menu did not become ready")

# Steam may briefly hand off between processes. Require one stable survivor before save load.
stable_since=None
end=time.time()+30
while time.time()<end:
    pids=bannerlord_pids()
    if len(pids)==1:
        if stable_since is None: stable_since=time.time()
        if time.time()-stable_since>=2.0: break
    else:
        stable_since=None
    time.sleep(.25)
else:
    raise RuntimeError("Bannerlord process count did not stabilize to one: "+repr(bannerlord_pids()))

write_command("LOAD_SAVE "+TARGET)
print(json.dumps({"phase":"load_save_queued","target":TARGET,"pids":bannerlord_pids()}),flush=True)
end=time.time()+180
last=None
while time.time()<end:
    pids=bannerlord_pids()
    if not pids:
        raise RuntimeError("Bannerlord exited while loading named save")
    try:
        st=http("/status")
        if st.get("campaignLoaded"):
            pl=http("/player")
            last={"status":st,"player":pl}
            hero=((pl.get("hero") or {}).get("name"))
            date=st.get("date")
            if hero=="Manan" and date and date!="Autumn 10, 1084":
                write_command("PAUSE")
                result={"phase":"LIVE","pids":pids,"hero":hero,"date":date,"status":st,"player":pl}
                print(json.dumps(result,indent=2),flush=True)
                out=ROOT/"Longitudinal/LiveValidation"/"MANAN_LIVE_NOW.json"
                out.write_text(json.dumps(result,indent=2),encoding="utf-8")
                raise SystemExit(0)
    except Exception:
        pass
    time.sleep(.5)

print(json.dumps({"phase":"VERIFY_TIMEOUT","last":last,"pids":bannerlord_pids()},indent=2),flush=True)
raise SystemExit(2)
