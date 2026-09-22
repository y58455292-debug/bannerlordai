from pathlib import Path
import csv, io, json, os, subprocess, sys, time, urllib.request, xml.etree.ElementTree as ET, hashlib

ROOT=Path(r"D:\BannerlordAIResearch")
GAME=Path(r"C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord")
BIN=GAME/"bin/Win64_Shipping_Client"
EXE=BIN/"Bannerlord.exe"
CFG=Path(r"C:\Users\csala\OneDrive\Documents\Mount and Blade II Bannerlord\Configs\LauncherData.xml")
AUTO=ROOT/"Automation/TestRunner"
CONTROL=ROOT/"Automation/Control"
sys.path.insert(0,str(CONTROL))
from command_bus import start_run, send

RESTART_MANIFEST=CONTROL/"NEXT_RESTART_MANIFEST.json"
if not RESTART_MANIFEST.exists():
    raise RuntimeError("Missing NEXT_RESTART_MANIFEST.json")
BUNDLE=json.loads(RESTART_MANIFEST.read_text(encoding="utf-8"))

TARGET=BUNDLE["target_save"]
EXPECTED_CAMPAIGN=BUNDLE["expected_campaign_id"]
EXPECTED_CLANAI=BUNDLE["accepted_clanai_sha256"]
EXPECTED_RUNNER=BUNDLE["files"]["runner_dll"]["sha256"]
EXPECTED_RUNNER_VERSION=BUNDLE["bundle_version"]
EXPECTED_OWNER=BUNDLE.get("deployment_owner","sol_manan_branch_v001")
EXPECTED_OPERATION=BUNDLE.get("deployment_operation_id")
EXIT_CAPABILITY=os.environ.get("BANNERLORDAI_EXIT_CAPABILITY")
if BUNDLE.get("require_exit_capability") and not EXIT_CAPABILITY:
    raise RuntimeError("Missing BANNERLORDAI_EXIT_CAPABILITY for protected live run")
EXIT_CAPABILITY_SHA256=(
    hashlib.sha256(EXIT_CAPABILITY.encode("utf-8")).hexdigest().upper()
    if EXIT_CAPABILITY else None
)
RUNNER_SRC=Path(BUNDLE["runner_source"])

def sha(p):
    h=hashlib.sha256()
    with p.open("rb") as f:
        for chunk in iter(lambda:f.read(1024*1024),b""):h.update(chunk)
    return h.hexdigest().upper()

def verify_bundle():
    mismatches=[]
    for name,entry in BUNDLE.get("files",{}).items():
        path=Path(entry["path"])
        expected=entry["sha256"].upper()
        if not path.exists():
            mismatches.append({"name":name,"error":"missing","path":str(path)})
            continue
        actual=sha(path)
        if actual!=expected:
            mismatches.append({"name":name,"expected":expected,"actual":actual,"path":str(path)})
    if mismatches:
        raise RuntimeError("Restart bundle hash verification failed: "+json.dumps(mismatches))
    return True

def bannerlord_pids():
    cp=subprocess.run(["tasklist","/FI","IMAGENAME eq Bannerlord.exe","/FO","CSV","/NH"],capture_output=True,text=True)
    out=[]
    for row in csv.reader(io.StringIO(cp.stdout)):
        if len(row)>=2 and row[0].lower()=="bannerlord.exe":
            try:out.append(int(row[1]))
            except:pass
    return out

def selected_modules():
    tree=ET.parse(CFG); ids=[]
    for node in tree.findall(".//SingleplayerData/ModDatas/UserModData"):
        if node.findtext("IsSelected","").strip().lower()=="true":
            ident=node.findtext("Id","").strip()
            if ident and ident not in ids:ids.append(ident)
    return ids

def http(path):
    with urllib.request.urlopen("http://127.0.0.1:8420"+path,timeout=3) as r:
        return json.loads(r.read().decode("utf-8"))

def read_status():
    p=AUTO/"status.txt"; d={}
    if not p.exists():return d
    for line in p.read_text(encoding="utf-8-sig",errors="replace").splitlines():
        if "=" in line:
            k,v=line.split("=",1);d[k]=v
    return d

def guard():
    cp=subprocess.run([sys.executable,"-X","utf8",str(ROOT/"Tools/Deployment/deployment_guard.py"),"status"],capture_output=True,text=True)
    return json.loads(cp.stdout)

def ensure_lease():
    claim_path=CONTROL/"rapid_manan_lease_claim.json"
    g=guard()
    lease=g.get("lease") or {}
    lock=g.get("lock") or {}

    if (not lease.get("exists")) or lease.get("expired"):
        if bannerlord_pids():
            raise RuntimeError("Lease missing/expired but Bannerlord is still running")
        cp=subprocess.run([sys.executable,"-X","utf8",str(ROOT/"workspace/renew_manan_lease.py")],capture_output=True,text=True)
        if cp.returncode!=0:
            raise RuntimeError(cp.stdout+cp.stderr)
        g=guard()
        lock=g.get("lock") or {}
        claim={
            "owner":lock.get("owner"),
            "operation_id":lock.get("operation_id"),
            "acquired_local":lock.get("acquired_local"),
            "claimed_unix":time.time()
        }
        claim_path.write_text(json.dumps(claim,indent=2),encoding="utf-8")
    else:
        if not claim_path.exists():
            raise RuntimeError("Valid deployment lease exists but rapid launcher did not claim it")
        claim=json.loads(claim_path.read_text(encoding="utf-8"))
        if (claim.get("owner")!=lock.get("owner") or
            claim.get("operation_id")!=lock.get("operation_id") or
            claim.get("acquired_local")!=lock.get("acquired_local")):
            raise RuntimeError("Deployment lease changed since rapid launcher claim")

    if lock.get("owner")!=EXPECTED_OWNER:
        raise RuntimeError(
            "Deployment lease owned by "+
            repr(lock.get("owner"))+
            ", expected "+
            repr(EXPECTED_OWNER))
    if (EXPECTED_OPERATION and
        lock.get("operation_id")!=EXPECTED_OPERATION):
        raise RuntimeError(
            "Deployment operation mismatch: "+
            repr(lock.get("operation_id"))+
            " != "+
            repr(EXPECTED_OPERATION))
    return g

def deploy_runner():
    dest=GAME/"Modules/ClanAI/bin/Win64_Shipping_Client"
    clan=dest/"ClanAI.dll"
    if sha(clan)!=EXPECTED_CLANAI:
        raise RuntimeError("ClanAI hash mismatch")

    expected_pdb=BUNDLE["files"]["runner_pdb"]["sha256"]
    cur_dll=dest/"BannerlordAITestRunner.dll"
    cur_pdb=dest/"BannerlordAITestRunner.pdb"
    if (cur_dll.exists() and cur_pdb.exists() and
        sha(cur_dll)==EXPECTED_RUNNER and
        sha(cur_pdb)==expected_pdb):
        return False

    backup=ROOT/"workspace/_PatchBackups"/("Before_Rapid_"+EXPECTED_RUNNER_VERSION.replace(".","_")+"_"+time.strftime("%Y%m%d_%H%M%S"))
    backup.mkdir(parents=True,exist_ok=True)

    import shutil
    for n in ("BannerlordAITestRunner.dll","BannerlordAITestRunner.pdb"):
        if (dest/n).exists():
            shutil.copy2(dest/n,backup/n)
        shutil.copy2(RUNNER_SRC/n,dest/n)

    if sha(cur_dll)!=EXPECTED_RUNNER:
        raise RuntimeError("Runner DLL deploy hash mismatch")
    if sha(cur_pdb)!=expected_pdb:
        raise RuntimeError("Runner PDB deploy hash mismatch")
    return True

def wait_until(fn,seconds,interval=.1,desc="condition"):
    end=time.time()+seconds
    last=None
    while time.time()<end:
        try:
            last=fn()
            if last:return last
        except Exception as e:
            last=e
        if not bannerlord_pids() and desc!="runner module":
            raise RuntimeError("Bannerlord exited while waiting for "+desc)
        time.sleep(interval)
    raise RuntimeError("Timeout waiting for "+desc+": "+repr(last))

def main():
    t0=time.perf_counter(); marks={}
    if bannerlord_pids():raise RuntimeError("Existing Bannerlord process")
    verify_bundle();marks["bundle_verified_ms"]=(time.perf_counter()-t0)*1000
    ensure_lease();marks["lease_ms"]=(time.perf_counter()-t0)*1000
    deployed=deploy_runner();marks["deploy_ms"]=(time.perf_counter()-t0)*1000

    for p in (AUTO/"status.txt",AUTO/"command.txt",AUTO/"command_meta.json"):
        try:p.unlink()
        except FileNotFoundError:pass

    run=start_run(
        issuer="rapid_manan_lab",
        expected_campaign_id=EXPECTED_CAMPAIGN,
        expected_runner_version=EXPECTED_RUNNER_VERSION,
        deployment_owner=EXPECTED_OWNER,
        deployment_operation_id=EXPECTED_OPERATION,
        exit_capability_sha256=EXIT_CAPABILITY_SHA256)
    mods=selected_modules()
    for req in ("ClanAI","BannerlordInspector","Bannerlord.Harmony"):
        if req not in mods:raise RuntimeError("Required module missing: "+req)

    arg="_MODULES_*"+"*".join(mods)+"*_MODULES_"
    bootstrap=subprocess.Popen([str(EXE),"/singleplayer",arg],cwd=str(BIN))
    marks["process_started_ms"]=(time.perf_counter()-t0)*1000
    print(json.dumps({"phase":"bootstrap","run_id":run["run_id"],"pid":bootstrap.pid,"deployed":deployed}),flush=True)

    wait_until(lambda: read_status().get("schema")=="BannerlordAI.TestRunner.v2",90,.1,"runner module")
    marks["runner_loaded_ms"]=(time.perf_counter()-t0)*1000

    wait_until(lambda: ((http("/screen").get("screen") or {}).get("topScreen") or "").endswith("GauntletInitialScreen"),120,.1,"real main menu")
    marks["main_menu_ms"]=(time.perf_counter()-t0)*1000

    # A real initialized main menu is stronger than an arbitrary 2-second process-stability sleep.
    wait_until(lambda: len(bannerlord_pids())==1,10,.1,"single game process")
    send("LOAD_SAVE "+TARGET,"rapid_manan_lab",run["run_id"],EXPECTED_CAMPAIGN)
    marks["load_queued_ms"]=(time.perf_counter()-t0)*1000

    def identity():
        st=http("/status")
        if not st.get("campaignLoaded") or st.get("campaignId")!=EXPECTED_CAMPAIGN:return False
        pl=http("/player")
        if ((pl.get("hero") or {}).get("name"))!="Manan":return False
        return {"status":st,"player":pl}
    ident=wait_until(identity,120,.15,"exact Manan identity")
    marks["identity_ms"]=(time.perf_counter()-t0)*1000

    stable=[]
    def hydrated():
        pl=http("/player"); members=((pl.get("party") or {}).get("members") or 0)
        if members<=0:
            stable.clear();return False
        stable.append(members)
        if len(stable)>3:stable.pop(0)
        return pl if len(stable)>=2 and stable[-1]==stable[-2] else False
    player=wait_until(hydrated,20,.15,"party hydration")
    marks["hydrated_ms"]=(time.perf_counter()-t0)*1000

    send("PAUSE","rapid_manan_lab",run["run_id"],EXPECTED_CAMPAIGN)
    marks["paused_ms"]=(time.perf_counter()-t0)*1000

    # Start the persistent low-overhead watcher automatically on every successful
    # exact-load recovery so Incidents and stall detection are never left orphaned.
    watcher_script=ROOT/"Automation/Control/low_overhead_live_loop.py"
    watcher=subprocess.Popen(
        [sys.executable,"-X","utf8",str(watcher_script)],
        cwd=str(ROOT),
        creationflags=getattr(subprocess,"CREATE_NO_WINDOW",0)
    )
    time.sleep(.15)
    if watcher.poll() is not None:
        raise RuntimeError("Low-overhead watcher exited during startup")
    marks["watcher_started_ms"]=(time.perf_counter()-t0)*1000

    result={
        "schema":"BannerlordAI.RapidMananLaunch.v1",
        "run_id":run["run_id"],
        "pid":bannerlord_pids(),
        "hero":((player.get("hero") or {}).get("name")),
        "party_members":((player.get("party") or {}).get("members")),
        "date":ident["status"].get("date"),
        "campaignId":ident["status"].get("campaignId"),
        "timings_ms":{k:round(v,1) for k,v in marks.items()},
        "total_ms":round((time.perf_counter()-t0)*1000,1)
    }
    out=ROOT/"Longitudinal/LiveValidation"/"RAPID_MANAN_LIVE_NOW.json"
    out.write_text(json.dumps(result,indent=2),encoding="utf-8")
    print(json.dumps(result,indent=2),flush=True)

if __name__=="__main__":
    main()
