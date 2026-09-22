import re, pathlib, json
lines=pathlib.Path(r"D:\BannerlordAIResearch\Telemetry\BannerlordInspector\logs\inspector.log").read_text(encoding="utf-8",errors="ignore").splitlines()
out=[]
for i,l in enumerate(lines):
    if "VISUAL_WAR_WINNER_CHANGE" not in l or "reason=active-defense" not in l: continue
    m=re.search(r"actor=(.*?) party=(.*?) men=(\d+) readiness=([0-9.]+) before=(.*?) after=(.*?) reason=active-defense",l)
    if not m: continue
    actor,party,men,readiness,before,after=m.groups()
    tail=lines[i+1:i+8]
    recovery=[x for x in tail if "WINNER_CHANGED party="+party in x]
    out.append({
      "actor":actor,"party":party,"readiness":float(readiness),
      "weak":float(readiness)<0.72,
      "before":before,"after":after,
      "recovery_override":recovery[0] if recovery else None
    })
print(json.dumps(out,indent=2))
print("weak",sum(x["weak"] for x in out),"of",len(out),"recovery_overrides",sum(bool(x["recovery_override"]) for x in out))
