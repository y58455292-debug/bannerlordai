from pathlib import Path
import json, os, sys, datetime

ROOT=Path(r"D:\BannerlordAIResearch\Longitudinal\EvidenceIndex\ResearchInbox")
POLICY=ROOT/"routing_policy.json"
QUEUES=ROOT/"Queues"
INDEX=ROOT/"routing_index.json"
VALID_ROLES={"coder","analyzer","front","library"}
VALID_PRIORITY={"high":3,"medium":2,"low":1}

def load(p, default=None):
    if not p.exists():
        return {} if default is None else default
    return json.loads(p.read_text(encoding="utf-8-sig"))

def atomic(p, data):
    p.parent.mkdir(parents=True,exist_ok=True)
    tmp=p.with_suffix(p.suffix+".tmp")
    tmp.write_text(json.dumps(data,indent=2)+"\n",encoding="utf-8")
    os.replace(tmp,p)

def default_reason(role, namespace):
    if role=="coder": return f"Concrete implementation relevance from {namespace}."
    if role=="analyzer": return f"Architecture/evaluation interpretation relevance from {namespace}."
    if role=="front": return f"Cross-cutting roadmap or decision relevance from {namespace}."
    return "Preserve source and provenance in the Research Library."

def normalize_routes(item, policy):
    namespace=str(item.get("namespace") or "other")
    category=namespace.split("/",1)[0]
    raw=item.get("routing")
    source="explicit"
    if not isinstance(raw,list) or not raw:
        raw=(policy.get("default_routes") or {}).get(category) or (policy.get("default_routes") or {}).get("other") or []
        source="policy_default"

    merged={}
    for r in raw:
        if not isinstance(r,dict): continue
        role=str(r.get("role") or "").lower()
        priority=str(r.get("priority") or "medium").lower()
        if role not in VALID_ROLES or priority not in VALID_PRIORITY: continue
        reason=str(r.get("reason") or default_reason(role,namespace))
        old=merged.get(role)
        if old is None or VALID_PRIORITY[priority] > VALID_PRIORITY[old["priority"]]:
            merged[role]={"role":role,"priority":priority,"reason":reason}

    if "library" not in merged:
        merged["library"]={"role":"library","priority":"medium","reason":"Preserve source and provenance in the Research Library."}

    order={"coder":0,"analyzer":1,"front":2,"library":3}
    routes=sorted(merged.values(),key=lambda x:(order[x["role"]],-VALID_PRIORITY[x["priority"]]))
    return routes,source

def upsert_queue(role, entry):
    p=QUEUES/f"{role}.json"
    q=load(p,{"schema":"BannerlordAI.ResearchRoleQueue.v1","role":role,"items":[]})
    items=[x for x in (q.get("items") or []) if x.get("id")!=entry["id"]]
    items.append(entry)
    items.sort(key=lambda x:(-VALID_PRIORITY.get(x.get("priority","low"),1),x.get("captured_at_local","")))
    q["items"]=items
    q["updated_at"]=datetime.datetime.now().astimezone().isoformat()
    atomic(p,q)

def main():
    if len(sys.argv)!=2:
        print(json.dumps({"status":"FAIL","reason":"usage: route_research_item.py <item.json>"}))
        return 2
    item_path=Path(sys.argv[1])
    if not item_path.exists():
        print(json.dumps({"status":"FAIL","reason":"item_not_found","path":str(item_path)}))
        return 2

    item=load(item_path)
    missing=[k for k in ("id","title","namespace","provenance","status") if not item.get(k)]
    if missing:
        print(json.dumps({"status":"FAIL","reason":"missing_required_fields","fields":missing}))
        return 3

    policy=load(POLICY)
    routes,source=normalize_routes(item,policy)
    now=datetime.datetime.now().astimezone().isoformat()
    item["routing"]=routes
    item["routing_status"]="ROUTED_POINTERS_ONLY"
    item["routing_source"]=source
    item["routed_at"]=now
    item["promotion_rule"]="Research Library/Analyzer review required before any engineering-truth promotion"
    atomic(item_path,item)

    for r in routes:
        entry={
            "id":item["id"],
            "title":item["title"],
            "namespace":item["namespace"],
            "priority":r["priority"],
            "reason":r["reason"],
            "item_pointer":str(item_path),
            "captured_at_local":item.get("captured_at_local"),
            "source_kind":item.get("source_kind"),
            "status":"PENDING_CONSIDERATION"
        }
        upsert_queue(r["role"],entry)

    idx=load(INDEX,{"schema":"BannerlordAI.ResearchRoutingIndex.v1","items":[]})
    idx["items"]=[x for x in (idx.get("items") or []) if x.get("id")!=item["id"]]
    idx["items"].append({
        "id":item["id"],"title":item["title"],"namespace":item["namespace"],
        "item_pointer":str(item_path),"routes":routes,"routed_at":now
    })
    idx["updated_at"]=now
    atomic(INDEX,idx)

    print(json.dumps({"status":"PASS","id":item["id"],"item_pointer":str(item_path),"routes":routes},indent=2))
    return 0

if __name__=="__main__":
    raise SystemExit(main())
