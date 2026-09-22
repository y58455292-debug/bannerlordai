import csv,collections,pathlib,json
p=pathlib.Path(r"D:\BannerlordAIResearch\Telemetry\BannerlordInspector\ai\behavior-delta\20260918_041745_pid10544\behavior_changes.csv")
rows=list(csv.DictReader(p.open(encoding="utf-8",newline="")))
dist=collections.Counter()
examples=[]
for r in rows:
    if r.get("afterTargetPartyId"):
        dist[r.get("afterBehavior","")]+=1
        if len(examples)<40:
            examples.append({
                "time":r.get("realTime"),"party":r.get("partyName"),"behavior":r.get("afterBehavior"),
                "targetParty":r.get("afterTargetPartyName"),"targetPartyId":r.get("afterTargetPartyId"),
                "vanillaBehavior":r.get("currentVanillaWinnerBehavior"),
                "vanillaTarget":r.get("currentVanillaWinnerTargetName"),
                "reason":r.get("reason")
            })
vdist=collections.Counter()
vexamples=[]
for r in rows:
    if r.get("currentVanillaWinnerTargetId") and not r.get("afterTargetSettlementId"):
        vdist[r.get("currentVanillaWinnerBehavior","")]+=1
        if len(vexamples)<40:
            vexamples.append({
                "time":r.get("realTime"),"party":r.get("partyName"),"behavior":r.get("currentVanillaWinnerBehavior"),
                "target":r.get("currentVanillaWinnerTargetName"),"id":r.get("currentVanillaWinnerTargetId"),
                "afterBehavior":r.get("afterBehavior"),"afterTargetParty":r.get("afterTargetPartyName"),
                "afterTargetSettlement":r.get("afterTargetSettlementName")
            })
print("rows",len(rows))
print("afterTargetParty behavior dist",dict(dist))
print(json.dumps(examples,indent=2))
print("vanilla non-settlement target dist",dict(vdist))
print(json.dumps(vexamples,indent=2))
