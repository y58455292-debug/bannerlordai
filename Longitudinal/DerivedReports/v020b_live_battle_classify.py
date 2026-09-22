import re, pathlib, collections
p=pathlib.Path(r"D:\BannerlordAIResearch\Telemetry\BannerlordInspector\logs\inspector.log")
lines=p.read_text(encoding="utf-8",errors="ignore").splitlines()
rows=[]
pat=re.compile(r'^\[(05:3[3-9]:\d\d\.\d+)\].*REAR_SECURITY_BATTLE_START lord=(.*?) lordId=(.*?) bandit=(.*?) banditId=(.*?) defaultBehavior=(.*?) shortTermBehavior=(.*?) targetParty=(.*?) shortTermTargetParty=(.*?) targetSettlement=(.*?) shortTermTargetSettlement=(.*?) engaging=(\w+) readiness=([0-9.]+) foodDays=(-?\d+) inArmy=(\w+) men=(\d+)')
for l in lines:
    m=pat.search(l)
    if not m: continue
    g=m.groups()
    r=dict(time=g[0],lord=g[1],lordId=g[2],bandit=g[3],banditId=g[4],default=g[5],short=g[6],target=g[7],shortTarget=g[8],settlement=g[9],shortSettlement=g[10],engaging=g[11],readiness=float(g[12]),foodDays=int(g[13]),inArmy=g[14],men=int(g[15]))
    r["weak"]=r["readiness"]<.72 or r["foodDays"]<3
    rows.append(r)
print("current_run_battles",len(rows))
print("weak",sum(r["weak"] for r in rows),"healthy",sum(not r["weak"] for r in rows))
print("weak_engaging_true",sum(r["weak"] and r["engaging"]=="True" for r in rows))
print("weak_engaging_false",sum(r["weak"] and r["engaging"]=="False" for r in rows))
print("healthy_engaging_true",sum((not r["weak"]) and r["engaging"]=="True" for r in rows))
print("weak details:")
for r in rows:
    if r["weak"]: print(r)
