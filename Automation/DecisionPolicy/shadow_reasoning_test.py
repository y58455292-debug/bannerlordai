import json, urllib.request, pathlib, time, math

ROOT=pathlib.Path(r"D:\BannerlordAIResearch")
CFG=json.loads((ROOT/"Automation/DecisionPolicy/reasoning_levels.json").read_text(encoding="utf-8"))

def get_json(url):
    with urllib.request.urlopen(url,timeout=4) as r:
        return json.loads(r.read().decode())

lp=get_json("http://127.0.0.1:8420/lordparties")
main=next(p for p in lp["parties"] if p.get("isMainParty"))
clan=[p for p in lp["parties"] if p.get("clan")==main.get("clan") and not p.get("isMainParty")]

food_days=float(main.get("foodDays") or 0)
food=float(main.get("food") or 0)
morale=float(main.get("morale") or 0)
in_town=bool(main.get("currentSettlement"))
moving_cover=sum(1 for p in clan if p.get("moving"))
near_home_cover=sum(1 for p in clan if p.get("targetSettlement")=="Syronea" or p.get("homeSettlement")=="Syronea")
army_cover=sum(1 for p in clan if p.get("army"))

# Candidate feature vectors are causal pressures, not rules.
# Positive = good for that goal, negative = bad.
candidates={
  "STAY_RESUPPLY":{
    "logistics_food_supply": max(-1,min(1,(6-food_days)/6)),
    "territorial_defense": 0.35 if in_town else -0.2,
    "military_readiness": 0.25,
    "local_stability": 0.15,
    "immediate_cost": -0.08,
    "long_term_risk": 0.25 if food_days<5 else 0.0,
    "doctrine_alignment": 0.15,
    "memory_resonance": 0.1,
    "uncertainty_penalty": -0.05
  },
  "PATROL_SYRONEA":{
    "logistics_food_supply": -0.45 if food_days<5 else -0.1,
    "territorial_defense": 0.8,
    "military_readiness": 0.35,
    "local_stability": 0.2,
    "immediate_cost": -0.12,
    "long_term_risk": -0.35 if food_days<4 else -0.15,
    "doctrine_alignment": 0.35,
    "memory_resonance": 0.25,
    "uncertainty_penalty": -0.05
  },
  "HOLD_IN_TOWN":{
    "logistics_food_supply": 0.35,
    "territorial_defense": 0.25,
    "military_readiness": 0.1,
    "local_stability": 0.1,
    "immediate_cost": 0.0,
    "long_term_risk": 0.05,
    "doctrine_alignment": 0.0,
    "memory_resonance": 0.0,
    "uncertainty_penalty": -0.18
  }
}

base_weights=json.loads((ROOT/"Automation/DecisionPolicy/manan_live_policy.json").read_text(encoding="utf-8"))["weights"]
results={}
for lvl,meta in CFG["levels"].items():
    mult=meta["feature_multipliers"]
    rows=[]
    for name,fv in candidates.items():
        total=0
        contrib={}
        for k,v in fv.items():
            w=float(base_weights.get(k,0))*float(mult.get(k,1))
            c=v*w
            contrib[k]=round(c,4); total+=c
        # Reasoning depth bonus is not "better"; it rewards evidence integration only when evidence exists.
        evidence_bonus=min(0.18,0.02*meta.get("counterfactual_checks",0)) if near_home_cover>0 else 0
        if name=="STAY_RESUPPLY" and moving_cover>0:
            total += evidence_bonus
        if name=="PATROL_SYRONEA" and army_cover>=2:
            total -= min(0.1,0.02*meta.get("lookahead_steps",0))
        rows.append({"action":name,"score":round(total,4),"contrib":contrib})
    rows.sort(key=lambda x:x["score"],reverse=True)
    results[lvl]={
        "name":meta["name"],
        "ranking":rows,
        "choice":rows[0]["action"],
        "margin":round(rows[0]["score"]-rows[1]["score"],4)
    }

out={
 "schema":"BannerlordAI.ShadowReasoningTest.v1",
 "wall_unix":time.time(),
 "state":{
   "food":food,"foodDays":food_days,"morale":morale,"inTown":in_town,
   "clanParties":len(clan),"movingCover":moving_cover,"homeCoverageSignals":near_home_cover,"armyCover":army_cover
 },
 "results":results,
 "activeLevel":str(CFG.get("active_level",2))
}
active=out["results"][out["activeLevel"]]["choice"]
out["activeChoice"]=active
p=ROOT/"Longitudinal/LiveValidation"/f"reasoning_shadow_{int(time.time()*1000)}.json"
p.write_text(json.dumps(out,indent=2),encoding="utf-8")
print(json.dumps(out,indent=2))
