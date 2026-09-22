import csv, pathlib, datetime as dt, re, json
log=pathlib.Path(r"D:\BannerlordAIResearch\Telemetry\ClanAI\sessions\ClanAI_20260918_105517_707_v019D.log")
csvp=pathlib.Path(r"D:\BannerlordAIResearch\Telemetry\BannerlordInspector\ai\behavior-delta\20260918_035454_pid9200\behavior_changes.csv")
lines=log.read_text(encoding="utf-8",errors="ignore").splitlines()
events=[]
for l in lines:
    if "VISUAL_WAR_WINNER_CHANGE" not in l: continue
    mt=re.match(r"(\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d+)?Z)",l)
    m=re.search(r"actor=(.*?) party=(.*?) men=.*? readiness=([0-9.]+) foodDays=(\d+) before=(.*?) after=(.*?) reason=([^ ]+)",l)
    if mt and m:
        t=dt.datetime.fromisoformat(mt.group(1).replace("Z","+00:00"))
        actor,party,ready,food,before,after,reason=m.groups()
        events.append(dict(t=t,actor=actor,party=party,readiness=float(ready),foodDays=int(food),before=before,after=after,reason=reason))
rows=[]
with csvp.open(encoding="utf-8",newline="") as f:
    for r in csv.DictReader(f):
        try:
            t=dt.datetime.strptime(r["realTime"],"%Y-%m-%d %H:%M:%S.%f").replace(tzinfo=dt.timezone(dt.timedelta(hours=-7))).astimezone(dt.timezone.utc)
        except: continue
        r["_t"]=t
        rows.append(r)
def cand(beh,target):
    return (beh or "") + ((":"+target) if target else "")
out=[]
for e in events[-40:]:
    candidates=[r for r in rows if r["partyName"]==e["party"] and 0 <= (r["_t"]-e["t"]).total_seconds() <= 3]
    exact=None
    for r in candidates:
        st=cand(r["afterBehavior"],r["afterTargetSettlementName"] or r["afterTargetPartyName"])
        if st==e["after"]:
            exact=r;break
    out.append({
      "actor":e["actor"],"reason":e["reason"],"ready":e["readiness"],"food":e["foodDays"],"after":e["after"],
      "committed":bool(exact),
      "delay": round((exact["_t"]-e["t"]).total_seconds(),3) if exact else None,
      "next":[cand(r["afterBehavior"],r["afterTargetSettlementName"] or r["afterTargetPartyName"]) for r in candidates[:4]]
    })
print(json.dumps(out,indent=2))
