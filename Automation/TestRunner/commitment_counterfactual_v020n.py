from pathlib import Path
import re, json, collections, statistics

session = Path(r"D:\BannerlordAIResearch\Telemetry\ClanAI\sessions\ClanAI_20260919_031537_169_v020N_review.log")
lines = [x for x in session.read_text(encoding="utf-8-sig", errors="replace").splitlines()
         if "ACTOR_INTENT_SWITCH " in x]

rx = re.compile(
    r"ACTOR_INTENT_SWITCH actor=(.*?) "
    r"fromState=(\S+) toState=(\S+) "
    r"from=(.*?) to=(.*?) "
    r"objectiveAgeHours=([\-0-9.Ee]+) "
    r"previousEligible=(True|False) "
    r"previousScore=(\S+) winnerScore=(\S+) "
    r"switchGapPct=(\S+) requiredFactor=(\S+) "
    r"runnerGapPct=(\S+) readiness=(\S+) "
    r"foodDays=(\S+) reason=(\S+)"
)

def n(v):
    if v == "NaN": return float("nan")
    if v == "+Inf": return float("inf")
    if v == "-Inf": return float("-inf")
    return float(v)

rows=[]
for x in lines:
    m=rx.search(x)
    if not m:
        continue
    a,fs,ts,fr,to,age,pe,ps,ws,gap,rf,rg,rd,food,reason=m.groups()
    rows.append(dict(
        actor=a, fromState=fs, toState=ts, fromObjective=fr, toObjective=to,
        age=n(age), eligible=(pe=="True"), previousScore=n(ps), winnerScore=n(ws),
        gap=n(gap), required=n(rf), runnerGap=n(rg), readiness=n(rd),
        foodDays=int(food), reason=reason
    ))

same=[r for r in rows if r["eligible"] and r["fromState"]==r["toState"]]
cross=[r for r in rows if r["eligible"] and r["fromState"]!=r["toState"]]

factors=[1.02,1.03,1.04,1.05,1.075,1.10,1.125,1.15]
def affected(rs, fac):
    return [r for r in rs if r["required"]==r["required"] and r["required"]<=fac]

out={
    "source": str(session),
    "parsedSwitches": len(rows),
    "sameStateEligible": len(same),
    "crossStateEligible": len(cross),
    "factors": {},
}
for fac in factors:
    ss=affected(same,fac)
    cs=affected(cross,fac)
    out["factors"][str(fac)]={
        "sameStateWouldRetain": len(ss),
        "crossStateWouldRetainIfUnrestricted": len(cs),
        "sameStatePctOfAllSwitches": round(100*len(ss)/len(rows),2) if rows else 0,
        "sameStateExamples":[
            {
                "actor":r["actor"], "state":r["fromState"],
                "from":r["fromObjective"], "to":r["toObjective"],
                "requiredFactor":r["required"], "gapPct":r["gap"],
                "ageHours":r["age"]
            } for r in ss
        ],
    }

# Count rapid exact reversals and which factor would block the first leg.
per_actor=collections.defaultdict(list)
for r in rows:
    per_actor[r["actor"]].append(r)
reversals=[]
for actor,rs in per_actor.items():
    for i in range(1,len(rs)):
        p,c=rs[i-1],rs[i]
        if p["fromObjective"]==c["toObjective"] and p["toObjective"]==c["fromObjective"]:
            reversals.append({
                "actor":actor,
                "firstFrom":p["fromObjective"],"firstTo":p["toObjective"],
                "firstFromState":p["fromState"],"firstToState":p["toState"],
                "firstRequired":p["required"],"firstAge":p["age"],
                "secondRequired":c["required"],"secondAge":c["age"],
                "sameStateFirst":p["fromState"]==p["toState"],
                "sameStateSecond":c["fromState"]==c["toState"],
            })
out["exactAdjacentReversals"]=reversals

# Selectivity recommendation purely from sample, not behavior verdict.
out["candidate"]={
    "factor":1.10,
    "scope":"previous objective remains eligible AND prior coarse state equals current natural winner coarse state",
    "sameStateWouldRetain":len(affected(same,1.10)),
    "crossStateWouldBeExcludedByScope":len(affected(cross,1.10)),
    "reason":"1.10 is the smallest tested factor that catches all three observed same-state near-switches <=10% while excluding state changes by design."
}
print(json.dumps(out,indent=2,ensure_ascii=False))
