import json, re, sys, math
from pathlib import Path
from datetime import datetime, timezone

ROOT=Path(r"D:\BannerlordAIResearch")
LONG=ROOT/"Longitudinal"
DERIVED=LONG/"DerivedReports"
EVID=LONG/"EvidenceIndex"
PYR=EVID/"ContextPyramid"
WARM=PYR/"warm"
HOT=PYR/"hot_context.json"
GLOBAL=PYR/"global_state.json"
CAT=PYR/"warm_catalog.jsonl"
HANDOFF=EVID/"HandoffV3"
CURRENT=HANDOFF/"current.json"
FRONT=PYR/"front_page.json"
FAST=HANDOFF/"Continuity"/"fast_boot.json"
CHECKPOINTS=LONG/"SaveCheckpoints"

STOP=set("""the a an and or of to in for with on at by from is are was were be been this that it as into
we our they their party parties lord lords ai model behavior campaign test run build current result
""".split())

TAG_PATTERNS={
 "rear-security":r"rear[- ]security|bandit|looter|deserter|EngageParty",
 "initiative-model":r"MobilePartyAIModel|initiative|ShouldConsiderAttacking|GetBestInitiativeBehavior",
 "visual-war":r"VisualWar|frontier-defense|frontier-offense|active-defense",
 "memory":r"memory|ledger|episode|grievance|trust|capture|mercy",
 "performance":r"performance|fps|frame|latency|writer|hot path|spatial locator",
 "causality":r"causal|commit|committed|NEW_COMMIT|proof chain",
 "global-model":r"global.*model|DelegatingMobilePartyAIModel|wrapper|mirror",
 "save-lineage":r"checkpoint|save hash|save_sha|world state|lineage",
 "runtime-discovery":r"runtime|reflection|public.*API|TaleWorlds",
 "compression":r"DeepSeek|compression|context pyramid|index|retriev"
}

def now():
    return datetime.now(timezone.utc).astimezone().isoformat()

def tokens(s):
    return [x for x in re.findall(r"[a-z0-9_.-]+",s.lower()) if len(x)>1 and x not in STOP]

def tags(text):
    return sorted(k for k,p in TAG_PATTERNS.items() if re.search(p,text,re.I))

def title_from_text(path,text):
    for line in text.splitlines():
        x=line.strip(" #\t")
        if x and len(x)<180:
            return x
    return path.stem

def extract_verified_lines(text,limit=24):
    lines=[]
    for raw in text.splitlines():
        s=raw.strip()
        if not s: continue
        if len(s)>500: s=s[:500]+"…"
        if re.search(r"(?i)^(conclusion|major result|result|final evidence|proof|finding|next|status|goal|build|deployed|sha256|observed|classification|active|rear|performance|memory|frontier|global|vanilla|save|checkpoint)",s) or s.startswith("-"):
            lines.append(s)
        if len(lines)>=limit: break
    if not lines:
        lines=[x.strip()[:500] for x in text.splitlines() if x.strip()][:limit]
    return lines

def block_from_report(path):
    text=path.read_text(encoding="utf-8",errors="ignore")
    st=path.stat()
    return {
      "id":"report:"+path.stem,
      "kind":"derived_report",
      "title":title_from_text(path,text),
      "source_path":str(path),
      "modified_local":datetime.fromtimestamp(st.st_mtime).astimezone().isoformat(),
      "size_bytes":st.st_size,
      "tags":tags(text+" "+path.name),
      "keywords":sorted(set(tokens(path.stem+" "+text[:12000])))[:350],
      "verified_summary_lines":extract_verified_lines(text),
      "evidence_refs":[str(path)],
      "resolution":"warm",
      "immutable_source":True
    }

def latest_checkpoint():
    if not CHECKPOINTS.exists(): return None
    dirs=[p for p in CHECKPOINTS.iterdir() if p.is_dir()]
    if not dirs: return None
    p=max(dirs,key=lambda x:x.stat().st_mtime)
    mf=p/"manifest.json"
    if mf.exists():
        try:return json.loads(mf.read_text(encoding="utf-8"))
        except: pass
    return {"checkpoint":str(p)}

def rebuild():
    PYR.mkdir(parents=True,exist_ok=True)
    WARM.mkdir(parents=True,exist_ok=True)
    current={}
    if CURRENT.exists():
        try: current=json.loads(CURRENT.read_text(encoding="utf-8"))
        except: current={}
    fast={}
    if FAST.exists():
        try: fast=json.loads(FAST.read_text(encoding="utf-8"))
        except: fast={}
    blocks=[]
    if DERIVED.exists():
        for p in sorted(DERIVED.glob("*.txt"),key=lambda x:x.stat().st_mtime):
            try: blocks.append(block_from_report(p))
            except Exception as e:
                blocks.append({"id":"error:"+p.stem,"kind":"error","source_path":str(p),"error":str(e),"tags":[],"keywords":[]})
    with CAT.open("w",encoding="utf-8") as f:
        for b in blocks: f.write(json.dumps(b,ensure_ascii=False)+"\n")
    cp=latest_checkpoint()
    newest=blocks[-1] if blocks else None
    milestone=current.get("last_accepted_milestone") or {}
    candidate=current.get("active_candidate") or {}
    run_state=current.get("run_state") or {}
    hot={
      "schema":"BannerlordAI.ContextPyramid.Hot.v2",
      "updated":now(),
      "checkpoint_seq":current.get("checkpoint_seq"),
      "accepted_milestone":{
        "version":milestone.get("version"),
        "name":milestone.get("name"),
        "status":milestone.get("status"),
        "result":milestone.get("result")
      },
      "active_candidate":{
        "version":candidate.get("version"),
        "name":candidate.get("name"),
        "status":candidate.get("status")
      },
      "active_question":current.get("active_question"),
      "next_action":current.get("next_action"),
      "blockers":current.get("blockers") or [],
      "run_state":{
        "bannerlord_running":(
          ((fast.get("runtime") or {}).get("bannerlord_running"))
          if fast else run_state.get("bannerlord_running")
        ),
        "campaign_ready":(
          (((fast.get("runtime") or {}).get("runner") or {}).get("campaignReady"))
          if fast else run_state.get("campaign_ready")
        ),
        "player_hero":(
          (((fast.get("runtime") or {}).get("runner") or {}).get("playerHero"))
          if fast else None
        ),
        "campaign_hours":(
          (((fast.get("runtime") or {}).get("runner") or {}).get("campaignHours"))
          if fast else None
        ),
        "time_control":(
          (((fast.get("runtime") or {}).get("runner") or {}).get("timeControl"))
          if fast else None
        ),
        "control_run_state":(
          (((fast.get("runtime") or {}).get("active_control") or {}).get("state"))
          if fast else None
        ),
        "deployment_owner":(
          (((fast.get("runtime") or {}).get("lease") or {}).get("owner"))
          if fast else run_state.get("deployment_owner")
        ),
        "operation_id":(
          (((fast.get("runtime") or {}).get("lease") or {}).get("operation_id"))
          if fast else run_state.get("operation_id")
        ),
        "desired_intent":(
          ((fast.get("runtime") or {}).get("desired_intent"))
          if fast else None
        )
      },
      "installed_build":current.get("installed_build") or {},
      "decisive_evidence":(current.get("decisive_evidence") or [])[:6],
      "do_not_repeat_count":len(current.get("do_not_repeat") or []),
      "workflow_improvement":current.get("workflow_improvement"),
      "latest_warm_block":(
        {
          "id":newest.get("id"),
          "title":newest.get("title"),
          "source_path":newest.get("source_path"),
          "tags":newest.get("tags"),
          "verified_summary_lines":newest.get("verified_summary_lines",[])[:6]
        }
        if newest else None
      ),
      "reasoning_order":["FRONT","HOT","retrieve WARM","RAW only if needed"],
      "raw_default":"do not reread full raw history unless retrieved evidence is insufficient"
    }
    HOT.write_text(json.dumps(hot,indent=2,ensure_ascii=False),encoding="utf-8")

    front={
      "schema":"BannerlordAI.ContextPyramid.Front.v1",
      "updated":now(),
      "checkpoint_seq":hot.get("checkpoint_seq"),
      "accepted":hot.get("accepted_milestone"),
      "active":hot.get("active_candidate"),
      "question":hot.get("active_question"),
      "next":hot.get("next_action"),
      "blockers":hot.get("blockers"),
      "runtime":hot.get("run_state"),
      "installed_build":hot.get("installed_build"),
      "fast_commands":{
        "resume":r"python -X utf8 D:\BannerlordAIResearch\Tools\Handoff\follow_protocol.py",
        "retrieve":r"python -X utf8 D:\BannerlordAIResearch\Tools\Evidence\context_pyramid.py retrieve <query>"
      },
      "core_rules":[
        "disk/runtime evidence outranks chat assumptions",
        "fast path by default; deep path only on drift/blocker/new architecture",
        "never repeat accepted proof",
        "single live deployment owner",
        "protect original saves; disposable branches only for tests",
        "coder generates proof; analyzer compresses; front coordinates"
      ],
      "lookup_order":[
        "FRONT for immediate action",
        "HOT for current verified context",
        "WARM retrieval for related findings/policies",
        "RAW evidence only when WARM is insufficient"
      ]
    }
    FRONT.write_text(json.dumps(front,indent=2,ensure_ascii=False),encoding="utf-8")

    # global state is compact; durable tags are inferred from all warm blocks
    tag_counts={}
    for b in blocks:
        for t in b.get("tags",[]): tag_counts[t]=tag_counts.get(t,0)+1
    global_state={
      "schema":"BannerlordAI.ContextPyramid.Global.v1",
      "updated":now(),
      "project":"BannerlordAI",
      "method":"native simulation preserved; modify judgment/context/memory; observe before modify; causal proof required",
      "context_architecture":"exact hot + heavily compressed global + indexed warm blocks + raw decompression on demand",
      "warm_block_count":len(blocks),
      "dominant_topics":sorted(tag_counts.items(),key=lambda kv:(-kv[1],kv[0]))[:20],
      "accepted_milestone":milestone,
      "active_candidate":candidate,
      "active_question":current.get("active_question"),
      "current_checkpoint":hot.get("current_checkpoint"),
      "current_builds":hot.get("current_builds"),
      "durable_rules":[
        "raw evidence immutable",
        "unknown stays UNKNOWN",
        "NATURAL != INDUCED != RESPONSE",
        "candidate score != committed behavior",
        "reassertion != new decision",
        "performance is correctness",
        "graphics quality is not traded for diagnostics",
        "one hypothesis -> one intervention -> one measurable result"
      ]
    }
    GLOBAL.write_text(json.dumps(global_state,indent=2,ensure_ascii=False),encoding="utf-8")
    print(json.dumps({"warm_blocks":len(blocks),"front":str(FRONT),"hot":str(HOT),"global":str(GLOBAL),"catalog":str(CAT)},indent=2))

def load_blocks():
    if not CAT.exists(): rebuild()
    out=[]
    with CAT.open(encoding="utf-8") as f:
        for line in f:
            try: out.append(json.loads(line))
            except: pass
    return out

def retrieve(query,n=8):
    q=set(tokens(query))
    blocks=load_blocks()
    nowts=datetime.now().timestamp()
    scored=[]
    for b in blocks:
        kw=set(b.get("keywords",[]))
        tg=set(b.get("tags",[]))
        overlap=len(q & kw)
        tag_overlap=sum(1 for t in tg if any(x in t for x in q))
        # phrase/entity bonus from title/path/summary
        hay=(b.get("title","")+" "+b.get("source_path","")+" "+" ".join(b.get("verified_summary_lines",[]))).lower()
        phrase_bonus=sum(2 for x in q if x in hay)
        try:
            mt=Path(b["source_path"]).stat().st_mtime
            age_days=max(0,(nowts-mt)/86400)
            recency=2/(1+age_days)
        except: recency=0
        causal=1.5 if "causality" in tg else 0
        score=overlap*2.5+tag_overlap*3+phrase_bonus+recency+causal
        if score>0: scored.append((score,b))
    scored.sort(key=lambda x:-x[0])
    result=[]
    for score,b in scored[:max(1,n)]:
        result.append({
          "score":round(score,3),
          "id":b.get("id"),
          "title":b.get("title"),
          "tags":b.get("tags"),
          "source_path":b.get("source_path"),
          "verified_summary_lines":b.get("verified_summary_lines",[])[:12]
        })
    print(json.dumps({"query":query,"results":result},indent=2,ensure_ascii=False))

if __name__=="__main__":
    cmd=sys.argv[1] if len(sys.argv)>1 else "rebuild"
    if cmd=="rebuild": rebuild()
    elif cmd=="retrieve": retrieve(" ".join(sys.argv[2:]) or "current milestone")
    else: raise SystemExit("usage: context_pyramid.py rebuild | retrieve <query>")
