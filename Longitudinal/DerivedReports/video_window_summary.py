import re, pathlib, datetime as dt, json, collections
p=pathlib.Path(r"D:\BannerlordAIResearch\Telemetry\BannerlordInspector\logs\inspector.log")
lines=p.read_text(encoding="utf-8", errors="ignore").splitlines()
windows=[
("video1",dt.datetime.fromisoformat("2026-09-18T09:30:40+00:00"),dt.datetime.fromisoformat("2026-09-18T09:30:54.2+00:00")),
("video2",dt.datetime.fromisoformat("2026-09-18T09:31:42+00:00"),dt.datetime.fromisoformat("2026-09-18T09:32:26.2+00:00")),
]
for name,a,b in windows:
    xs=[]
    for l in lines:
        m=re.match(r"(\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d+)?Z)",l)
        if not m: continue
        try:t=dt.datetime.fromisoformat(m.group(1).replace("Z","+00:00"))
        except:continue
        if a<=t<=b and "[ClanAI v0.19C]" in l: xs.append(l)
    visual=[l for l in xs if "VISUAL_WAR_WINNER_CHANGE" in l]
    memories=[l for l in xs if "WORLD_MEMORY event=" in l]
    reasons=collections.Counter()
    targets=collections.Counter()
    actors=collections.Counter()
    mem=collections.Counter()
    for l in visual:
        q=re.search(r"reason=([^ ]+)",l)
        r=re.search(r"after=(.*?) reason=",l)
        s=re.search(r"actor=(.*?) party=",l)
        if q:reasons[q.group(1)]+=1
        if r:targets[r.group(1)]+=1
        if s:actors[s.group(1)]+=1
    for l in memories:
        q=re.search(r"WORLD_MEMORY event=([^ ]+)",l)
        if q:mem[q.group(1)]+=1
    print("\n==",name,"==")
    print("clanai_lines",len(xs),"visual_changes",len(visual),"memory_events",len(memories))
    print("reasons",dict(reasons))
    print("top_targets",targets.most_common(12))
    print("memory_types",dict(mem))
    print("visual:")
    for l in visual:
        m=re.search(r"Z \[ClanAI v0\.19C\] VISUAL_WAR_WINNER_CHANGE actor=(.*?) party=.*? before=(.*?) after=(.*?) reason=([^ ]+)",l)
        if m: print(" ",m.groups())
