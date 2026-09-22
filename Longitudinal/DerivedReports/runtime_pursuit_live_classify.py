import re, pathlib, collections, json
p=pathlib.Path(r"D:\BannerlordAIResearch\Telemetry\BannerlordInspector\logs\inspector.log")
lines=p.read_text(encoding="utf-8",errors="ignore").splitlines()
rows=[]
pat=re.compile(r"REAR_SECURITY_BATTLE_START lord=(.*?) lordId=(.*?) bandit=(.*?) banditId=(.*?) defaultBehavior=(.*?) shortTermBehavior=(.*?) targetParty=(.*?) shortTermTargetParty=(.*?) targetSettlement=(.*?) shortTermTargetSettlement=(.*?) engaging=(\w+) readiness=([0-9.]+) men=(\d+)")
for l in lines:
    m=pat.search(l)
    if m:
        g=m.groups()
        rows.append(dict(lord=g[0],lordId=g[1],bandit=g[2],banditId=g[3],default=g[4],short=g[5],target=g[6],shortTarget=g[7],settlement=g[8],shortSettlement=g[9],engaging=g[10],readiness=float(g[11]),men=int(g[12])))
print("count",len(rows))
print("shortTerm",dict(collections.Counter(r["short"] for r in rows)))
print("engaging",dict(collections.Counter(r["engaging"] for r in rows)))
print("default",dict(collections.Counter(r["default"] for r in rows)))
print("target_match_bandit",sum(r["shortTarget"]==r["bandit"] for r in rows),"/",len(rows))
print("readiness_lt_072",sum(r["readiness"]<.72 for r in rows),"/",len(rows))
print("men_le_160",sum(r["men"]<=160 for r in rows),"/",len(rows))
print("both_rear_eligible",sum(r["readiness"]>=.72 and r["men"]<=160 for r in rows),"/",len(rows))
print("unique_lords",len(set(r["lordId"] for r in rows)))
