import json,pathlib,time
ROOT=pathlib.Path(r"D:\BannerlordAIResearch")
levels=json.loads((ROOT/"Automation/DecisionPolicy/reasoning_levels.json").read_text(encoding="utf-8"))
weights=json.loads((ROOT/"Automation/DecisionPolicy/manan_live_policy.json").read_text(encoding="utf-8"))["weights"]

# Live policy evidence, captured from current KingdomPolicyDecision.
state={
  "policy_id":"policy_fraternal_fleet_doctrine",
  "policy_name":"Auxiliaries to the Fleet",
  "proposer_clan":"Oburit",
  "description":"Troops train at sea.",
  "effects":{"naval_morale_pct":20,"troop_xp_pct":-15},
  "native_support":-52.8000069,
  "proposal_influence_cost":100,
  "current_context":{
    "home":"Syronea",
    "role":"border/front defense",
    "land_force_dependency":1.0,
    "naval_relevance":0.25
  }
}

candidates={
 "SUPPORT":{
   "military_readiness":-0.22,    # XP penalty hits land army development
   "territorial_defense":-0.12,
   "long_term_risk":-0.18,
   "immediate_cost":-0.03,
   "doctrine_alignment":-0.08,
   "local_stability":0.02,
   "legitimacy_influence":0.02,
   "uncertainty_penalty":-0.04
 },
 "OPPOSE":{
   "military_readiness":0.24,
   "territorial_defense":0.14,
   "long_term_risk":0.17,
   "immediate_cost":0.0,
   "doctrine_alignment":0.09,
   "local_stability":0.0,
   "legitimacy_influence":0.0,
   "uncertainty_penalty":-0.03
 },
 "ABSTAIN":{
   "military_readiness":0.0,
   "territorial_defense":0.0,
   "long_term_risk":-0.02,
   "immediate_cost":0.04,
   "doctrine_alignment":-0.02,
   "local_stability":0.0,
   "legitimacy_influence":0.0,
   "uncertainty_penalty":-0.09
 }
}

results={}
for lvl,meta in levels["levels"].items():
    mult=meta["feature_multipliers"]
    rows=[]
    for action,fv in candidates.items():
        total=0
        contrib={}
        for k,v in fv.items():
            c=v*float(weights.get(k,0))*float(mult.get(k,1))
            contrib[k]=round(c,4); total+=c
        # More deliberative levels account more strongly for observed vanilla clan opposition
        # as evidence, not as authority.
        if action=="OPPOSE":
            total += min(0.35,0.05*meta.get("counterfactual_checks",0))
        elif action=="SUPPORT":
            total -= min(0.20,0.03*meta.get("counterfactual_checks",0))
        rows.append({"action":action,"score":round(total,4),"contrib":contrib})
    rows.sort(key=lambda x:x["score"],reverse=True)
    results[str(lvl)]={
      "name":meta["name"],
      "choice":rows[0]["action"],
      "margin":round(rows[0]["score"]-rows[1]["score"],4),
      "ranking":rows
    }

out={
 "schema":"BannerlordAI.PolicyVoteReasoningTournament.v1",
 "wall_unix":time.time(),
 "state":state,
 "active_level":str(levels.get("active_level",2)),
 "levels":results
}
p=ROOT/"Automation/DecisionPolicy/current_policy_vote_tournament.json"
p.write_text(json.dumps(out,indent=2),encoding="utf-8")
print(json.dumps(out,indent=2))
