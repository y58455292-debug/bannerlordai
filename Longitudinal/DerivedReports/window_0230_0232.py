import re, json, pathlib, datetime as dt
p=pathlib.Path(r"D:\BannerlordAIResearch\Telemetry\BannerlordInspector\logs\inspector.log")
lines=p.read_text(encoding="utf-8", errors="ignore").splitlines()
start=dt.datetime.fromisoformat("2026-09-18T09:29:30+00:00")
end=dt.datetime.fromisoformat("2026-09-18T09:33:00+00:00")
picked=[]
for line in lines:
    m=re.match(r"(\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d+)?Z)", line)
    if not m: continue
    try: t=dt.datetime.fromisoformat(m.group(1).replace("Z","+00:00"))
    except: continue
    if start <= t <= end:
        if any(k in line for k in [
            "ClanAI","VISUAL_WAR","WORLD_MEMORY","MEMORY_",
            "BEHAVIOR","decision","Decision","Raid","Siege","Capture"
        ]):
            picked.append(line)
print("COUNT",len(picked))
for x in picked[-250:]:
    print(x)
