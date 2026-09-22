from pathlib import Path
import argparse, datetime, hashlib, json, os, subprocess, time, urllib.request
import xml.etree.ElementTree as ET

ROOT=Path(r"D:\BannerlordAIResearch")
GAME=Path(r"C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord")
BIN=GAME/"bin/Win64_Shipping_Client"
EXE=BIN/"Bannerlord.exe"
SAVES=Path(r"C:\Users\csala\OneDrive\Documents\Mount and Blade II Bannerlord\Game Saves")
LAUNCHER_DATA=Path(r"C:\Users\csala\OneDrive\Documents\Mount and Blade II Bannerlord\Configs\LauncherData.xml")
AUTO=ROOT/"Automation/TestRunner"
COMMAND=AUTO/"command.txt"
STATUS=AUTO/"status.txt"
LOG_ROOT=ROOT/"Longitudinal/LiveValidation"

def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest().upper()
def selected_modules():
    tree=ET.parse(LAUNCHER_DATA); ids=[]
    for node in tree.findall(".//SingleplayerData/ModDatas/UserModData"):
        if node.findtext("IsSelected","").strip().lower()=="true":
            ident=node.findtext("Id","").strip()
            if ident and ident not in ids: ids.append(ident)
    return ids
def write_command(s):
    tmp=COMMAND.with_suffix(".tmp"); tmp.write_text(s+"\n",encoding="utf-8"); os.replace(tmp,COMMAND)
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
def wait(pred,timeout=120,step=.25):
    end=time.time()+timeout
    while time.time()<end:
        try:
            x=pred()
            if x: return x
        except Exception:
            pass
        time.sleep(step)
    return None
def game_running():
    cp=subprocess.run(["tasklist","/fo","csv"],capture_output=True,text=True)
    return any("Bannerlord.exe" in x for x in cp.stdout.splitlines())

def main():
    ap=argparse.ArgumentParser()
    ap.add_argument("--slot",required=True)
    ap.add_argument("--file",required=True)
    ap.add_argument("--expected-sha",required=True)
    args=ap.parse_args()

    save=SAVES/args.file
    if not save.exists(): raise FileNotFoundError(save)
    before=sha(save)
    if before!=args.expected_sha.upper(): raise RuntimeError("save hash mismatch")
    if game_running(): raise RuntimeError("Bannerlord already running")

    for p in (COMMAND,STATUS):
        try:p.unlink()
        except FileNotFoundError:pass

    modules=selected_modules()
    if "ClanAI" not in modules or "BannerlordInspector" not in modules:
        raise RuntimeError("required modules not selected")

    stamp=datetime.datetime.now().strftime("%Y%m%d_%H%M%S_%f")
    outdir=LOG_ROOT/("NamedSaveLaunch_"+stamp); outdir.mkdir(parents=True)
    arg="_MODULES_*"+"*".join(modules)+"*_MODULES_"
    launch=[str(EXE),"/singleplayer",arg]
    proc=subprocess.Popen(launch,cwd=str(BIN))
    launch_time=time.time()

    module_ready=wait(lambda: (lambda s:s if STATUS.exists() and STATUS.stat().st_mtime>=launch_time and s.get("schema")=="BannerlordAI.TestRunner.v2" else None)(read_status()),90,.25)
    if not module_ready:
        raise RuntimeError("fresh TestRunner v2 status did not appear at main menu")

    write_command("LOAD_SAVE "+args.slot)
    load_ack=wait(lambda:(lambda s:s if (s.get("lastResult") or "")=="operator_load_save_started:"+args.slot else None)(read_status()),30,.2)
    if not load_ack:
        raise RuntimeError("named save load was not acknowledged")

    loaded=wait(lambda: (lambda s,p:{"status":s,"player":p} if s.get("campaignLoaded") and s.get("timeReady") else None)(http("/status"),http("/player")),180,.5)
    if not loaded:
        raise RuntimeError("named save did not reach loaded campaign")

    after=sha(save)
    result={
        "schema":"BannerlordAI.NamedSaveLaunch.v1",
        "success":True,
        "pid":proc.pid,
        "slot":args.slot,
        "file":str(save),
        "save_sha_before":before,
        "save_sha_after":after,
        "save_unchanged":before==after,
        "module_ready_status":module_ready,
        "load_ack":load_ack,
        "loaded":loaded,
        "launch":launch,
        "manual_intervention":False,
    }
    (outdir/"final_result.json").write_text(json.dumps(result,indent=2),encoding="utf-8")
    print(json.dumps({"validation":str(outdir),**result},indent=2))
    if not result["save_unchanged"]: raise SystemExit(3)

if __name__=="__main__":
    main()
