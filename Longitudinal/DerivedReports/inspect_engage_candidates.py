import csv,collections,pathlib,json
p=pathlib.Path(r"D:\BannerlordAIResearch\Telemetry\BannerlordInspector\ai\behavior-delta\20260918_010335_pid5640\behavior_changes.csv")
rows=list(csv.DictReader(p.open(encoding="utf-8",newline="")))
eng=[r for r in rows if r["afterBehavior"]=="EngageParty" or r["currentVanillaWinnerBehavior"]=="EngageParty" or r["beforeBehavior"]=="EngageParty"]
print("Engage-related Behavior Delta rows:",len(eng))
for r in eng[:40]:
    print({k:r[k] for k in ["realTime","partyName","beforeBehavior","afterBehavior","beforeTargetPartyName","afterTargetPartyName","currentVanillaWinnerBehavior","currentVanillaWinnerTargetName","reason"]})
