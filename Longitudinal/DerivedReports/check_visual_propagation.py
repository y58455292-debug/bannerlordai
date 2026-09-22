import csv,re,datetime as dt,pathlib,json,collections
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

rows=[]
with csvp.open(encoding="utf-8",newline="") as f:
    for r in csv.DictReader(f):
        try:t=dt.datetime.strptime(r["realTime"],"%Y-%m-%d %H:%M:%S.%f").replace(tzinfo=dt.timezone(dt.timedelta(hours=-7))).astimezone(dt.timezone.utc)
        except:continue
        r["_t"]=t
        rows.append(r)

def cand(beh,target):
    return (beh or "") + ((":"+target) if target else "")

matches=[]
for e in visual:
    candidates=[r for r in rows if r["partyName"]==e["party"] and 0 <= (r["_t"]-e["t"]).total_seconds() <= 3.0]
    exact=[]
    for r in candidates:
        after=cand(r["afterBehavior"],r["afterTargetSettlementName"] or r["afterTargetPartyName"])
        vanilla=cand(r["currentVanillaWinnerBehavior"],r["currentVanillaWinnerTargetName"])
        if after==e["after"] or vanilla==e["after"]:
            exact.append((r,after,vanilla))
    chosen=(exact[0] if exact else None)
    matches.append({
      "actor":e["actor"],"party":e["party"],"reason":e["reason"],"ai_after":e["after"],
      "candidate_rows_next3s":len(candidates),
      "exact_match":bool(chosen),
      "match_delay_s":round((chosen[0]["_t"]-e["t"]).total_seconds(),3) if chosen else None,
      "recorded_after":chosen[1] if chosen else None,
      "recorded_vanilla_winner":chosen[2] if chosen else None,
      "event_reason":chosen[0]["reason"] if chosen else None
    })

c=collections.Counter((m["reason"],m["exact_match"]) for m in matches)
out={
 "visual_events":len(matches),
 "exact_behavior_delta_matches":sum(m["exact_match"] for m in matches),
 "match_rate":round(sum(m["exact_match"] for m in matches)/len(matches),3) if matches else None,
 "by_reason":{f"{k[0]}|{'matched' if k[1] else 'unmatched'}":v for k,v in c.items()},
 "matches":matches
}
path=pathlib.Path(r"D:\BannerlordAIResearch\Longitudinal\DerivedReports\visual_to_behavior_propagation.json")
path.write_text(json.dumps(out,indent=2,default=str),encoding="utf-8")
print(json.dumps({k:v for k,v in out.items() if k!="matches"},indent=2))
print("SAMPLE")
for m in matches[:15]: print(m)
