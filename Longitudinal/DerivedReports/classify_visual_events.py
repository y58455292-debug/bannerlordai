import csv,re,datetime as dt,pathlib,json,collections,bisect
logp=pathlib.Path(r"D:\BannerlordAIResearch\Telemetry\BannerlordInspector\logs\inspector.log")
csvp=pathlib.Path(r"D:\BannerlordAIResearch\Telemetry\BannerlordInspector\ai\behavior-delta\20260918_010335_pid5640\behavior_changes.csv")
lines=logp.read_text(encoding="utf-8",errors="ignore").splitlines()
visual=[]
for l in lines:
    if "[ClanAI v0.19C] VISUAL_WAR_WINNER_CHANGE" not in l: continue
    mt=re.match(r"(\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d+)?Z)",l)
    m=re.search(r"actor=(.*?) party=(.*?) men=.*? before=(.*?) after=(.*?) reason=([^ ]+)",l)
    if mt and m:
        t=dt.datetime.fromisoformat(mt.group(1).replace("Z","+00:00"))
        actor,party,before,after,reason=m.groups()
        visual.append(dict(t=t,actor=actor,party=party,before=before,after=after,reason=reason))
rows_by_party=collections.defaultdict(list)
with csvp.open(encoding="utf-8",newline="") as f:
    for r in csv.DictReader(f):
        try:
            t=dt.datetime.strptime(r["realTime"],"%Y-%m-%d %H:%M:%S.%f").replace(tzinfo=dt.timezone(dt.timedelta(hours=-7))).astimezone(dt.timezone.utc)
        except: continue
        r["_t"]=t
        rows_by_party[r["partyName"]].append(r)
for rs in rows_by_party.values(): rs.sort(key=lambda r:r["_t"])

def cand(beh,target):
    return (beh or "") + ((":"+target) if target else "")

def state(r):
    return cand(r["afterBehavior"],r["afterTargetSettlementName"] or r["afterTargetPartyName"])

classified=[]
for e in visual:
    rs=rows_by_party.get(e["party"],[])
    before_rows=[r for r in rs if r["_t"]<=e["t"]]
    prev=before_rows[-1] if before_rows else None
    after_rows=[r for r in rs if 0 <= (r["_t"]-e["t"]).total_seconds() <= 3]
    exact_next=next((r for r in after_rows if state(r)==e["after"]),None)
    if exact_next:
        cls="NEW_COMMITTED_CHANGE"
        delay=(exact_next["_t"]-e["t"]).total_seconds()
    elif prev and state(prev)==e["after"]:
        cls="ALREADY_ACTIVE_REASSERTION"
        delay=None
    else:
        cls="UNPROVEN"
        delay=None
    classified.append({
      **{k:e[k] for k in ("actor","party","before","after","reason")},
      "class":cls,
      "delay_s":round(delay,3) if delay is not None else None,
      "prev_recorded_state":state(prev) if prev else None,
      "prev_age_s":round((e["t"]-prev["_t"]).total_seconds(),3) if prev else None,
      "next_states":[state(r) for r in after_rows[:5]]
    })
counts=collections.Counter(x["class"] for x in classified)
by_reason=collections.defaultdict(collections.Counter)
for x in classified: by_reason[x["reason"]][x["class"]]+=1
print("COUNTS",dict(counts))
print("BY_REASON",{k:dict(v) for k,v in by_reason.items()})
print("\nUNPROVEN")
for x in classified:
    if x["class"]=="UNPROVEN": print(x)
outp=pathlib.Path(r"D:\BannerlordAIResearch\Longitudinal\DerivedReports\visual_event_classification.json")
outp.write_text(json.dumps({"counts":dict(counts),"by_reason":{k:dict(v) for k,v in by_reason.items()},"events":classified},indent=2),encoding="utf-8")
