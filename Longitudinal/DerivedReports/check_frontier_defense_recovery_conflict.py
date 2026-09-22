import re, pathlib, json
logp=pathlib.Path(r"D:\BannerlordAIResearch\Telemetry\BannerlordInspector\logs\inspector.log")
lines=logp.read_text(encoding="utf-8",errors="ignore").splitlines()
events=[]
for i,l in enumerate(lines):
    if "VISUAL_WAR_WINNER_CHANGE" not in l or "reason=frontier-defense" not in l: continue
    m=re.search(r"actor=(.*?) party=(.*?) men=(\d+) readiness=([0-9.]+) before=(.*?) after=(.*?) reason=frontier-defense",l)
    if not m: continue
    actor,party,men,readiness,before,after=m.groups()
    tail=lines[i+1:i+8]
    recovery=[x for x in tail if "WINNER_CHANGED party="+party in x]
    events.append({
      "actor":actor,"party":party,"readiness":float(readiness),
      "weak_by_threshold":float(readiness)<0.72,
      "visual_before":before,"visual_after":after,
      "immediate_recovery_override":recovery[0] if recovery else None
    })
print(json.dumps(events,indent=2))
