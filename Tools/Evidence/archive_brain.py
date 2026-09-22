from pathlib import Path
import datetime
import hashlib
import json
import os
import re
import sys

ROOT=Path(r"D:\BannerlordAIResearch")
BRAIN=ROOT/r"Longitudinal\EvidenceIndex\ChatArchiveBrain"
FRONT_DIR=BRAIN/"00_FRONT"
HOT_DIR=BRAIN/"10_HOT"
WARM_DIR=BRAIN/"20_WARM"
INDEX_DIR=BRAIN/"30_INDEX"
RAW_DIR=BRAIN/"90_RAW_POINTERS"
OFFICE=ROOT/r"Automation\Office"

SOURCE_ROOTS=[
    ROOT/r"Longitudinal\EvidenceIndex\HandoffV3",
    ROOT/r"Longitudinal\EvidenceIndex\ContextPyramid",
    ROOT/r"Longitudinal\DerivedReports",
    ROOT/r"Longitudinal\LiveValidation",
    ROOT/r"Automation\Office",
    Path(r"C:\Users\csala\Documents\Codex\2026-09-13\realtime-voice-chat\outputs"),
]

KNOWN_CHATS=[
    {
      "title":"Read handoff instructions",
      "domain":"BannerlordAI engineering / recovery",
      "coverage":[
        r"D:\BannerlordAIResearch\Longitudinal\EvidenceIndex\HandoffV3\PROTOCOL.md",
        r"D:\BannerlordAIResearch\Longitudinal\EvidenceIndex\HandoffV3\current.json"
      ]
    },
    {
      "title":"BannerlordAI Progress Watch",
      "domain":"live engineering progress / handoffs / testing",
      "coverage":[
        r"D:\BannerlordAIResearch\Longitudinal\EvidenceIndex\HandoffV3\events.jsonl",
        r"D:\BannerlordAIResearch\Longitudinal\EvidenceIndex\HandoffV3\ARCHITECTURE_LEARNING.md",
        r"D:\BannerlordAIResearch\Longitudinal\LiveValidation"
      ]
    },
    {
      "title":"Resume Test Closeout",
      "domain":"testing / research / workflow improvements",
      "coverage":[
        r"D:\BannerlordAIResearch\Longitudinal\EvidenceIndex\HandoffV3\ARCHITECTURE_LEARNING.md",
        r"D:\BannerlordAIResearch\Longitudinal\DerivedReports"
      ]
    },
    {
      "title":"Research AI Architecture",
      "domain":"architecture research / prior art",
      "coverage":[
        r"D:\BannerlordAIResearch\Longitudinal\EvidenceIndex\HandoffV3\REFERENCE_REGISTRY.md",
        r"D:\BannerlordAIResearch\Longitudinal\EvidenceIndex\HandoffV3\ARCHITECTURE_LEARNING.md"
      ]
    },
    {
      "title":"Collect Data and Continue",
      "domain":"testing / DeepSeek workflow / handoffs",
      "coverage":[
        r"D:\BannerlordAIResearch\Longitudinal\EvidenceIndex\HandoffV3\events.jsonl",
        r"D:\BannerlordAIResearch\Longitudinal\EvidenceIndex\HandoffV3\ARCHITECTURE_LEARNING.md"
      ]
    },
    {
      "title":"Continue From Memory File",
      "domain":"Mem / recovery",
      "coverage":[
        r"D:\BannerlordAIResearch\Longitudinal\EvidenceIndex\HandoffV3\PROTOCOL.md",
        r"D:\BannerlordAIResearch\Longitudinal\EvidenceIndex\HandoffV3\MEM_ROADMAP_SYNC_PENDING.md"
      ]
    },
    {
      "title":"Blood Feud Era Update",
      "domain":"campaign canon / dynasty",
      "coverage":[
        r"C:\Users\csala\Documents\Codex\2026-09-13\realtime-voice-chat\outputs\campaign-record.json",
        r"C:\Users\csala\Documents\Codex\2026-09-13\realtime-voice-chat\outputs\Chronicle-Source-Index.json"
      ]
    },
    {
      "title":"File Analysis Options",
      "domain":"comparative AI architecture / research",
      "coverage":[
        r"D:\BannerlordAIResearch\Longitudinal\EvidenceIndex\HandoffV3\ARCHITECTURE_LEARNING.md",
        r"D:\BannerlordAIResearch\Longitudinal\EvidenceIndex\HandoffV3\REFERENCE_REGISTRY.md"
      ]
    },
    {
      "title":"Analytical Psychology For AI",
      "domain":"psychology research / organic decision making",
      "coverage":[
        "Mem note: BannerlordAI — Analytical Psychology Source & Application Gate",
        r"D:\BannerlordAIResearch\Longitudinal\EvidenceIndex\HandoffV3\ARCHITECTURE_LEARNING.md"
      ]
    }
]

def now():
    return datetime.datetime.now().astimezone().isoformat()

def sha(path):
    try:
        h=hashlib.sha256()
        with path.open("rb") as f:
            for chunk in iter(lambda:f.read(1024*1024),b""):
                h.update(chunk)
        return h.hexdigest().upper()
    except Exception:
        return None

def atomic_json(path,obj):
    path.parent.mkdir(parents=True,exist_ok=True)
    tmp=path.with_suffix(path.suffix+".tmp")
    tmp.write_text(json.dumps(obj,indent=2,ensure_ascii=False)+"\n",encoding="utf-8")
    os.replace(tmp,path)

def layer_for(path):
    s=str(path).lower()
    if "front_page" in s or "front_status" in s:
        return "FRONT"
    if "current.json" in s or "hot_context" in s or "office_clock_status" in s:
        return "HOT"
    if "derivedreports" in s or "architecture_learning" in s or "reference_registry" in s:
        return "WARM"
    if "livevalidation" in s or path.suffix.lower() in (".log",".sav",".png",".jpg",".jpeg",".mp4"):
        return "RAW"
    return "INDEX"

def classify_ext(path):
    ext=path.suffix.lower()
    if ext in (".json",".jsonl"): return "structured"
    if ext in (".md",".txt",".log"): return "text"
    if ext in (".py",".cs",".ps1",".cmd"): return "code"
    if ext in (".png",".jpg",".jpeg"): return "image"
    if ext in (".sav",): return "save"
    if ext in (".mp4",".avi"): return "video"
    return "other"

def manifest():
    rows=[]
    for root in SOURCE_ROOTS:
        if not root.exists():
            continue
        if root.is_file():
            paths=[root]
        else:
            paths=[p for p in root.rglob("*") if p.is_file()]
        for p in paths:
            try:
                st=p.stat()
            except Exception:
                continue
            rows.append({
                "path":str(p),
                "layer":layer_for(p),
                "kind":classify_ext(p),
                "size_bytes":st.st_size,
                "modified_local":datetime.datetime.fromtimestamp(st.st_mtime).astimezone().isoformat(),
                "sha256":sha(p) if st.st_size<=50*1024*1024 else None
            })
    return rows

def coverage(chats):
    out=[]
    for item in chats:
        checks=[]
        for src in item["coverage"]:
            if src.startswith("Mem note:"):
                checks.append({"source":src,"exists":"MEM_VERIFY_REQUIRED"})
            else:
                p=Path(src)
                checks.append({"source":src,"exists":p.exists()})
        local_ok=all(x["exists"] is True or x["exists"]=="MEM_VERIFY_REQUIRED" for x in checks)
        out.append({
            **item,
            "coverage_checks":checks,
            "local_brain_coverage":"PROVISIONAL_PASS" if local_ok else "GAP",
            "analyzer_verification":"PENDING",
            "safe_to_archive":False,
            "safe_to_delete":False,
            "reason":"Requires Analyzer verification + Mem mirror before retirement."
        })
    return out

def rebuild():
    for p in (FRONT_DIR,HOT_DIR,WARM_DIR,INDEX_DIR,RAW_DIR):
        p.mkdir(parents=True,exist_ok=True)
    rows=manifest()
    manifest_path=INDEX_DIR/"source_manifest.jsonl"
    with manifest_path.open("w",encoding="utf-8") as f:
        for row in rows:
            f.write(json.dumps(row,ensure_ascii=False)+"\n")

    cov=coverage(KNOWN_CHATS)
    atomic_json(INDEX_DIR/"known_chat_coverage.json",{
        "schema":"BannerlordAI.ChatCoverage.v1",
        "updated_at":now(),
        "note":"Logical coverage map only. Actual ChatGPT UI archive/delete requires user action.",
        "chats":cov
    })
    atomic_json(RAW_DIR/"raw_roots.json",{
        "schema":"BannerlordAI.RawRoots.v1",
        "updated_at":now(),
        "roots":[str(x) for x in SOURCE_ROOTS],
        "rule":"Raw sources are preserved; indexes/summaries never replace authoritative evidence."
    })
    by_layer={}
    for row in rows:
        by_layer[row["layer"]]=by_layer.get(row["layer"],0)+1
    front={
        "schema":"BannerlordAI.ArchiveBrain.Front.v1",
        "updated_at":now(),
        "source_count":len(rows),
        "layer_counts":by_layer,
        "known_chat_count":len(cov),
        "archive_safe_count":sum(1 for x in cov if x["safe_to_archive"]),
        "delete_safe_count":sum(1 for x in cov if x["safe_to_delete"]),
        "current_policy":"No chat deletion until Analyzer verifies coverage and Mem mirror; user performs final UI action.",
        "lookup_order":["FRONT","HOT","WARM","INDEX","RAW"]
    }
    atomic_json(FRONT_DIR/"archive_status.json",front)
    return {
        "brain_root":str(BRAIN),
        "manifest":str(manifest_path),
        "known_chat_coverage":str(INDEX_DIR/"known_chat_coverage.json"),
        **front
    }

def main():
    if len(sys.argv)>1 and sys.argv[1]=="rebuild":
        out=rebuild()
    else:
        out=rebuild()
    print(json.dumps(out,indent=2,ensure_ascii=False))
    return 0

if __name__=="__main__":
    raise SystemExit(main())
