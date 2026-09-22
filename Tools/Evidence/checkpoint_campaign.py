import os, re, sys, json, shutil, hashlib, time
from pathlib import Path
from datetime import datetime, timezone

ROOT = Path(r"D:\BannerlordAIResearch")
CONFIG = Path(r"C:\Users\csala\OneDrive\Documents\Mount and Blade II Bannerlord\Configs\BannerlordConfig.txt")
SAVE_ROOT = Path(r"C:\Users\csala\OneDrive\Documents\Mount and Blade II Bannerlord\Game Saves")
CHECKPOINT_ROOT = ROOT / "Longitudinal" / "SaveCheckpoints"
INDEX_ROOT = ROOT / "Longitudinal" / "EvidenceIndex"

INSPECTOR_DLL = Path(r"C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\Modules\BannerlordInspector\bin\Win64_Shipping_Client\BannerlordInspector.dll")
CLANAI_DLL = Path(r"C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\Modules\ClanAI\bin\Win64_Shipping_Client\ClanAI.dll")
CLANAI_SESSIONS = ROOT / "Telemetry" / "ClanAI" / "sessions"
BEHAVIOR_ROOT = ROOT / "Telemetry" / "BannerlordInspector" / "ai" / "behavior-delta"
RUNTIME_INDEX = ROOT / "Research" / "RuntimeApiIndex" / "2026-09-18"
METHOD_FILE = ROOT / "Research" / "Methods" / "DeepSeek_FileRead_Compression_Method.txt"

def sha256(path):
    h=hashlib.sha256()
    with open(path,"rb") as f:
        for chunk in iter(lambda:f.read(1024*1024),b""):
            h.update(chunk)
    return h.hexdigest().upper()

def latest_entry(path, dirs=False, pattern=None):
    if not path.exists():
        return None
    items=[p for p in path.iterdir() if (p.is_dir() if dirs else p.is_file())]
    if pattern:
        items=[p for p in items if re.search(pattern,p.name,re.I)]
    return max(items,key=lambda p:p.stat().st_mtime) if items else None

def sanitize(s):
    s=re.sub(r"[^A-Za-z0-9._-]+","_",s.strip())
    return s.strip("_") or "checkpoint"

label=" ".join(sys.argv[1:]).strip() or "manual"
text=CONFIG.read_text(encoding="utf-8-sig",errors="ignore")
m=re.search(r"(?m)^LatestSaveGameName=(.*)$",text)
if not m:
    raise SystemExit("LatestSaveGameName not found")
save_name=m.group(1).strip()
save_file=SAVE_ROOT/(save_name if save_name.lower().endswith(".sav") else save_name+".sav")
if not save_file.exists():
    raise SystemExit(f"Save not found: {save_file}")

stamp=datetime.now().strftime("%Y%m%d_%H%M%S")
folder=CHECKPOINT_ROOT/f"{stamp}_{sanitize(label)}"
folder.mkdir(parents=True,exist_ok=False)
dest=folder/save_file.name
shutil.copy2(save_file,dest)

latest_session=latest_entry(CLANAI_SESSIONS,False,r"\.log$")
latest_behavior=latest_entry(BEHAVIOR_ROOT,True)

manifest={
    "schema":"BannerlordAI.SaveCheckpoint.v1",
    "created_local":datetime.now().astimezone().isoformat(),
    "created_utc":datetime.now(timezone.utc).isoformat(),
    "label":label,
    "latest_save_name":save_name,
    "source_save":str(save_file),
    "checkpoint_save":str(dest),
    "save_sha256":sha256(dest),
    "save_size_bytes":dest.stat().st_size,
    "save_mtime_local":datetime.fromtimestamp(dest.stat().st_mtime).astimezone().isoformat(),
    "builds":{
        "ClanAI":{
            "path":str(CLANAI_DLL),
            "sha256":sha256(CLANAI_DLL) if CLANAI_DLL.exists() else None,
            "version":"v0.19F"
        },
        "BannerlordInspector":{
            "path":str(INSPECTOR_DLL),
            "sha256":sha256(INSPECTOR_DLL) if INSPECTOR_DLL.exists() else None,
            "version":"v0.30"
        }
    },
    "evidence":{
        "latest_clanai_session":str(latest_session) if latest_session else None,
        "latest_behavior_delta":str(latest_behavior) if latest_behavior else None,
        "runtime_api_index":str(RUNTIME_INDEX) if RUNTIME_INDEX.exists() else None,
        "compression_method":str(METHOD_FILE) if METHOD_FILE.exists() else None
    },
    "classification_note":"Save checkpoint is world-state evidence. It does not by itself prove causality; pair with build hash, session telemetry, and interventions."
}
(folder/"manifest.json").write_text(json.dumps(manifest,indent=2),encoding="utf-8")

INDEX_ROOT.mkdir(parents=True,exist_ok=True)
current={
    "schema":"BannerlordAI.RollingEvidenceIndex.v1",
    "updated_local":datetime.now().astimezone().isoformat(),
    "current_checkpoint":str(folder),
    "current_save_sha256":manifest["save_sha256"],
    "current_builds":manifest["builds"],
    "latest_evidence":manifest["evidence"],
    "active_milestone":"Runtime pursuit discovery via TaleWorlds MobilePartyAIModel",
    "active_question":"Where does vanilla lord-vs-bandit pursuit originate and commit: initiative model, default behavior, short-term behavior, or execution?",
    "proof_chain":[
        "MobilePartyAIModel judgment",
        "default/short-term behavior assignment",
        "SetMove execution",
        "MapEvent battle start"
    ],
    "method":"Raw evidence remains immutable. Use hierarchical compression and evidence-linked summaries for active reasoning."
}
(INDEX_ROOT/"current_state.json").write_text(json.dumps(current,indent=2),encoding="utf-8")

print("CHECKPOINT="+str(folder))
print("SAVE="+save_file.name)
print("SAVE_SHA256="+manifest["save_sha256"])
print("INDEX="+str(INDEX_ROOT/"current_state.json"))
