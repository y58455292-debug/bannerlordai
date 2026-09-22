import csv, pathlib, collections, json
root=pathlib.Path(r"D:\BannerlordAIResearch\Telemetry\BannerlordInspector\ai\behavior-delta")
hits=[]
sessions=0
for p in root.glob("*/behavior_changes.csv"):
    sessions+=1
    try:
        with p.open(encoding="utf-8",newline="") as f:
            for r in csv.DictReader(f):
                vals=[r.get("beforeBehavior",""),r.get("afterBehavior",""),r.get("currentVanillaWinnerBehavior","")]
                if "EngageParty" in vals:
                    hits.append({
                      "session":p.parent.name,
                      "realTime":r.get("realTime",""),
                      "party":r.get("partyName",""),
                      "leader":r.get("leaderName",""),
                      "kingdom":r.get("kingdomName",""),
                      "before":r.get("beforeBehavior",""),
                      "after":r.get("afterBehavior",""),
                      "beforeTarget":r.get("beforeTargetPartyName","") or r.get("beforeTargetSettlementName",""),
                      "afterTarget":r.get("afterTargetPartyName","") or r.get("afterTargetSettlementName",""),
                      "vanilla":r.get("currentVanillaWinnerBehavior",""),
                      "vanillaTarget":r.get("currentVanillaWinnerTargetName",""),
                      "reason":r.get("reason","")
                    })
    except Exception:
        pass
print("sessions",sessions,"engage_rows",len(hits))
for x in hits[:100]: print(json.dumps(x))
