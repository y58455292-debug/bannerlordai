import csv, json, math, pathlib, collections
src=pathlib.Path(r"D:\BannerlordAIResearch\Telemetry\BannerlordInspector\ai\behavior-delta\20260918_010335_pid5640\radius_settlements.csv")
rows={}
with src.open(encoding="utf-8",newline="") as f:
    for r in csv.DictReader(f):
        sid=r["settlementId"]
        if sid and sid not in rows:
            try:x=float(r["x"]);y=float(r["y"])
            except:continue
            rows[sid]={
              "id":sid,"name":r["settlementName"],"type":r["settlementType"],
              "x":x,"y":y,"kingdom":r.get("kingdomName","")
            }
towns=[r for r in rows.values() if r["type"]=="Town"]
mapping={}
for r in rows.values():
    if r["type"]=="Town":
        t=r; dist=0.0
    else:
        t=min(towns,key=lambda q:(q["x"]-r["x"])**2+(q["y"]-r["y"])**2)
        dist=math.hypot(t["x"]-r["x"],t["y"]-r["y"])
    mapping[r["name"]]={"town":t["name"],"townId":t["id"],"distance":round(dist,3),"sourceType":r["type"]}
out=pathlib.Path(r"D:\BannerlordAIResearch\Tools\FieldCommander\Releases\v2.2-regional\settlement_to_town.json")
out.write_text(json.dumps(mapping,indent=2),encoding="utf-8")
print("settlements",len(rows),"towns",len(towns),"mapped",len(mapping))
for name in ["Rhesos","Durn","Ataconia Castle","Swenryn","Cantrec","Diantogmail","Amycon","Ab Comer Castle","Ath Cafal","Seonon"]:
    print(name,"=>",mapping.get(name))
