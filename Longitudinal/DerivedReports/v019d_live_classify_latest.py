import csv,re,datetime as dt,pathlib,collections,json
logp=pathlib.Path(r"D:\BannerlordAIResearch\Telemetry\ClanAI\sessions\ClanAI_20260918_105517_707_v019D.log")
csvp=pathlib.Path(r"D:\BannerlordAIResearch\Telemetry\BannerlordInspector\ai\behavior-delta\20260918_035454_pid9200\behavior_changes.csv")
lines=logp.read_text(encoding="utf-8",errors="ignore").splitlines()
events=[]
for l in lines:
    if "VISUAL_WAR_WINNER_CHANGE" not in l: continue
    mt=re.match(r"(\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d+)?Z)",l)
    m=re.search(r"actor=(.*?) party=(.*?) men=.*? readiness=([0-9.]+) foodDays=(\d+) before=(.*?) after=(.*?) reason=([^ ]+)",l)
    if mt and m:
        t=dt.datetime.fromisoformat(mt.group(1).replace("Z","+00:00"))
        actor,party,ready,food,before,after,reason=m.groups()
        events.append(dict(t=t,actor=actor,party=party,ready=float(ready),food=int(food),before=before,after=after,reason=reason))
rows_by=collections.defaultdict(list)
with csvp.open(encoding="utf-8",newline="") as f:
    for r in csv.DictReader(f):
        try:t=dt.datetime.strptime(r["realTime"],"%Y-%m-%d %H:%M:%S.%f").replace(tzinfo=dt.timezone(dt.timedelta(hours=-7))).astimezone(dt.timezone.utc)
        except:continue
        r["_t"]=t; rows_by[r["partyName"]].append(r)
for rs in rows_by.values(): rs.sort(key=lambda r:r["_t"])
def cand(r):
    beh=r["afterBehavior"] or ""
    tar=r["afterTargetSettlementName"] or r["afterTargetPartyName"] or ""
    return beh + ((":"+tar) if tar else "")
classified=[]
for e in events:
    rs=rows_by.get(e["party"],[])
    prevs=[r for r in rs if r["_t"]<=e["t"]]
    prev=prevs[-1] if prevs else None
    nxt=[r for r in rs if 0 <= (r["_t"]-e["t"]).total_seconds() <= 3]
    exact=next((r for r in nxt if cand(r)==e["after"]),None)
    if exact: cls="NEW_COMMIT"
    elif prev and cand(prev)==e["after"]: cls="ALREADY_ACTIVE"
    else: cls="UNPROVEN"
    classified.append((e,cls))
by=collections.defaultdict(collections.Counter)
for e,c in classified: by[e["reason"]][c]+=1
ctx=[l for l in lines if "VISUAL_WAR_CONTEXT_REFRESH" in l]
mem=[l for l in lines if "WORLD_MEMORY event=" in l]
probes=[l for l in lines if "MEMORY_SCORE_PROBE" in l]
flips=[l for l in probes if "wouldFlip=True" in l]
print("events",len(events))
print("by_reason",json.dumps({k:dict(v) for k,v in by.items()}))
print("memory_events",len(mem),"probes",len(probes),"wouldFlip",len(flips))
print("latest_context",ctx[-1] if ctx else "NONE")
print("latest_unproven")
for e,c in classified[-20:]:
    if c=="UNPROVEN": print(e["actor"],e["reason"],e["ready"],e["food"],e["after"])
