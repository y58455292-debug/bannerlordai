import re,pathlib,json,collections
lines=pathlib.Path(r"D:\BannerlordAIResearch\Telemetry\BannerlordInspector\logs\inspector.log").read_text(encoding="utf-8",errors="ignore").splitlines()
for reason in ("frontier-offense","rear-security"):
    out=[]
    for i,l in enumerate(lines):
        if "VISUAL_WAR_WINNER_CHANGE" not in l or ("reason="+reason) not in l: continue
        m=re.search(r"actor=(.*?) party=(.*?) men=(\d+) readiness=([0-9.]+) before=(.*?) after=(.*?) reason="+re.escape(reason),l)
        if not m: continue
        actor,party,men,readiness,before,after=m.groups()
        tail=lines[i+1:i+8]
        recovery=[x for x in tail if "WINNER_CHANGED party="+party in x]
        out.append({"actor":actor,"party":party,"readiness":float(readiness),"weak":float(readiness)<0.72,
                    "before":before,"after":after,"recovery_override":recovery[0] if recovery else None})
    print("\n",reason,len(out))
    print(json.dumps(out,indent=2))
