import json, pathlib, time, re, urllib.request, urllib.parse, sys

ROOT=pathlib.Path(r"D:\BannerlordAIResearch")
TR=ROOT/"Automation/TestRunner"
DP=ROOT/"Automation/DecisionPolicy"
VAL=ROOT/"Longitudinal/LiveValidation"
sys.path.insert(0,str(ROOT/"Automation/Control"))
from command_bus import send_active
POLICY=json.loads((DP/"manan_live_policy.json").read_text(encoding="utf-8"))
WEIGHTS=POLICY["weights"]
REASONING=json.loads((DP/"reasoning_levels.json").read_text(encoding="utf-8"))

def read_kv(path):
    d={}
    try:
        for line in pathlib.Path(path).read_text(encoding="utf-8-sig",errors="replace").splitlines():
            if "=" in line:
                k,v=line.split("=",1); d[k]=v
    except FileNotFoundError:
        pass
    return d

def write_cmd(cmd):
    return send_active(cmd, issuer="fast_live_decider")

def wait_status_result(expected, timeout=2.0):
    deadline=time.time()+timeout
    while time.time()<deadline:
        st=read_kv(TR/"status.txt")
        if st.get("lastResult")==expected:
            return True
        time.sleep(0.025)
    return False

def http_json(path):
    with urllib.request.urlopen("http://127.0.0.1:8420"+path,timeout=2) as r:
        return json.loads(r.read().decode())

def snapshot(tag, incident_id, chosen=None):
    out={
        "wall_unix":time.time(),
        "status":read_kv(TR/"status.txt"),
        "player":http_json("/player"),
        "incident_id":incident_id,
        "chosen":chosen
    }
    p=VAL/f"{incident_id}_{tag}_{int(time.time()*1000)}.json"
    p.write_text(json.dumps(out,indent=2),encoding="utf-8")
    return str(p)

def parse_decision():
    kv=read_kv(TR/"decision_state.txt")
    if kv.get("type")!="incident":
        return None
    n=int(kv.get("optionCount","0"))
    return {
        "title":kv.get("title",""),
        "description":kv.get("description",""),
        "options":[kv.get(f"option{i}Text","") for i in range(n)]
    }

def fv():
    return {
        "family_survival":0.0,"territorial_defense":0.0,"military_readiness":0.0,
        "logistics_food_supply":0.0,"wealth_reserve":0.0,"local_stability":0.0,
        "legitimacy_influence":0.0,"personal_relationships":0.0,"mercy_honor":0.0,
        "fear_deterrence":0.0,"long_term_risk":0.0,"immediate_cost":0.0,
        "doctrine_alignment":0.0,"memory_resonance":0.0,"uncertainty_penalty":-0.08
    }

def clamp(x): return max(-1.0,min(1.0,x))

def infer(text, desc):
    t=(text+" "+desc).lower()
    f=fv(); hits=0

    def add(name,val):
        nonlocal hits
        f[name]=clamp(f[name]+val); hits+=1

    # Costs / resources
    if any(w in t for w in ("gold","coin","reward","pay","purse","generously","shower him")):
        add("wealth_reserve",-0.18); add("immediate_cost",-0.18)
    if any(w in t for w in ("little amount of coin","small amount","little coin")):
        add("wealth_reserve",-0.06); add("immediate_cost",-0.06)

    # Reputation / legitimacy
    if any(w in t for w in ("renown","heroic deeds","epic ballad","recount your","fame","reputation")):
        add("legitimacy_influence",0.30)
    if "embellish" in t:
        add("legitimacy_influence",0.22); add("mercy_honor",-0.22); add("long_term_risk",-0.10)
    if any(w in t for w in ("losses and faults","faults as well","describe your losses")):
        add("mercy_honor",0.34); add("legitimacy_influence",0.10); add("long_term_risk",0.08)
    if any(w in t for w in ("don't need a bard","do not need a bard","you don't need")):
        add("wealth_reserve",0.14); add("immediate_cost",0.12); add("legitimacy_influence",-0.06)

    # Stability / social tone
    if any(w in t for w in ("reward him","generously","honest","truth","faults")):
        add("personal_relationships",0.16); add("local_stability",0.10)
    if any(w in t for w in ("madness","daemon","witch","terrible but heroic death","tragic tales")):
        add("legitimacy_influence",-0.20); add("long_term_risk",-0.28); add("uncertainty_penalty",-0.10)

    # Manan-specific durable preference: truth over invented certainty.
    if any(w in t for w in ("faults","losses","truth","honest")):
        add("memory_resonance",0.18)
    if "embellish" in t:
        add("memory_resonance",-0.18)

    return f,hits

def score_option(text,desc,multipliers=None):
    f,hits=infer(text,desc)
    multipliers=multipliers or {}
    contrib={}
    total=0.0
    for k,v in f.items():
        effective_weight=float(WEIGHTS.get(k,0.0))*float(multipliers.get(k,1.0))
        c=v*effective_weight
        contrib[k]=round(c,4); total+=c
    confidence=min(0.92,0.48+0.035*hits)
    return round(total,4),confidence,f,contrib

def decide(d,multipliers=None):
    rows=[]
    for i,opt in enumerate(d["options"]):
        s,c,f,con=score_option(opt,d["description"],multipliers)
        rows.append({"index":i,"text":opt,"score":s,"confidence":c,"features":f,"contrib":con})
    rows.sort(key=lambda x:x["score"],reverse=True)
    margin=rows[0]["score"]-rows[1]["score"] if len(rows)>1 else 9
    return rows,round(margin,4)

def reasoning_tournament(d):
    shadows={}
    for level,meta in REASONING["levels"].items():
        rows,margin=decide(d,meta.get("feature_multipliers") or {})
        shadows[str(level)]={
            "name":meta.get("name",str(level)),
            "choice":rows[0]["index"],
            "choice_text":rows[0]["text"],
            "margin":margin,
            "ranking":rows
        }
    active=str(REASONING.get("active_level",2))
    if active not in shadows:
        active="2" if "2" in shadows else sorted(shadows.keys())[0]
    return active,shadows

d=parse_decision()
if not d:
    print(json.dumps({"status":"no_incident"}))
    raise SystemExit(2)

incident_id="incident_"+re.sub(r"[^a-z0-9]+","_",d["title"].lower()).strip("_")
active_level,shadow=reasoning_tournament(d)
active_result=shadow[active_level]
rows=active_result["ranking"]
margin=active_result["margin"]
top=rows[0]

shadow_summary={
    lvl:{
        "name":x["name"],
        "choice":x["choice"],
        "choice_text":x["choice_text"],
        "margin":x["margin"]
    }
    for lvl,x in shadow.items()
}
shadow_out={
    "schema":"BannerlordAI.ReasoningTournament.v1",
    "wall_unix":time.time(),
    "actor":"Manan",
    "incident":incident_id,
    "title":d["title"],
    "active_level":active_level,
    "levels":shadow_summary
}
(DP/"current_reasoning_tournament.json").write_text(json.dumps(shadow_out,indent=2),encoding="utf-8")

receipt={
    "schema":"BannerlordAI.FastDecisionReceipt.v2",
    "wall_unix":time.time(),
    "actor":"Manan",
    "incident":incident_id,
    "title":d["title"],
    "reasoning_level":active_level,
    "reasoning_level_name":active_result["name"],
    "shadow_choices":shadow_summary,
    "ranking":rows,
    "margin":margin,
    "selected":top["index"],
    "fast_path":True,
    "presentation":{
        "runner_version":read_kv(TR/"status.txt").get("runnerVersion","<none>"),
        "preview_hold_seconds":2.0,
        "result_hold_seconds":0.8
    }
}
(DP/"current_decision_receipt.json").write_text(json.dumps(receipt,indent=2),encoding="utf-8")

before=snapshot("before",incident_id,top["index"])

runner_version=read_kv(TR/"status.txt").get("runnerVersion","")
visible_deliberation=runner_version.startswith("v0.2.9-visible-deliberation")
presentation_text=re.sub(r"\s+"," ",f"{d['title']}: {top['text']}").strip()[:240]

if visible_deliberation:
    write_cmd("DECISION_PREVIEW "+presentation_text)
    wait_status_result("decision_preview_shown",2.0)
    time.sleep(2.0)

write_cmd(f"INCIDENT_SELECT {top['index']}")

deadline=time.time()+5
selected=False
while time.time()<deadline:
    st=read_kv(TR/"status.txt")
    ds=read_kv(TR/"decision_state.txt")
    if st.get("lastResult")==f"incident_selected:{top['index']}" and ds.get("type")=="<none>":
        selected=True
        break
    time.sleep(0.05)

after=snapshot("after",incident_id,top["index"])

if visible_deliberation and selected:
    write_cmd("DECISION_RESULT "+presentation_text)
    wait_status_result("decision_result_shown",2.0)
    time.sleep(0.8)

# Restore the actor's sticky external intent after UI/Incident transitions.
intent_path=ROOT/"Automation/Control/desired_intent.json"
restored_intent=None
if intent_path.exists():
    try:
        intent=json.loads(intent_path.read_text(encoding="utf-8"))
        if intent.get("sticky") and intent.get("command"):
            restored_intent=intent["command"]
            write_cmd(restored_intent)

            # Command transport is single-slot and runner polls at ~200 ms.
            # Wait for explicit acknowledgement so AUTO_PLAY cannot overwrite
            # the restored movement intent before Bannerlord consumes it.
            ack_deadline=time.time()+2.0
            while time.time()<ack_deadline:
                ack=read_kv(TR/"status.txt")
                if ack.get("lastCommand")==restored_intent:
                    break
                time.sleep(0.025)
    except Exception:
        pass

write_cmd("AUTO_PLAY")

print(json.dumps({
    "status":"executed" if selected else "selection_unconfirmed",
    "title":d["title"],
    "selected":top["index"],
    "text":top["text"],
    "score":top["score"],
    "margin":margin,
    "confidence":top["confidence"],
    "before":before,
    "after":after,
    "restored_intent":restored_intent,
    "resumed":True
},indent=2))
