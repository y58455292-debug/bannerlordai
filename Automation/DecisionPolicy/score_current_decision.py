import json, pathlib, time
ROOT=pathlib.Path(r"D:\BannerlordAIResearch\Automation\DecisionPolicy")
policy=json.loads((ROOT/"manan_live_policy.json").read_text(encoding="utf-8"))
decision=json.loads((ROOT/"current_incident_features.json").read_text(encoding="utf-8"))
weights=policy["weights"]
rows=[]
for opt in decision["options"]:
    contrib=[]
    total=0.0
    for name,val in opt["features"].items():
        w=float(weights.get(name,0.0))
        c=float(val)*w
        total+=c
        contrib.append({"feature":name,"value":val,"weight":w,"contribution":round(c,4)})
    contrib.sort(key=lambda x:abs(x["contribution"]), reverse=True)
    rows.append({
        "index":opt["index"],
        "text":opt["text"],
        "confidence":opt["confidence"],
        "raw_score":round(total,4),
        "top_contributions":contrib[:6]
    })
rows.sort(key=lambda x:x["raw_score"], reverse=True)
margin=rows[0]["raw_score"]-rows[1]["raw_score"] if len(rows)>1 else None
receipt={
    "schema":"BannerlordAI.DecisionReceipt.v1",
    "generated_unix":time.time(),
    "actor":policy["actor"],
    "incident":decision["incident"],
    "assessment_status":decision["assessment_status"],
    "ranking":rows,
    "selected_candidate":rows[0]["index"],
    "score_margin":round(margin,4) if margin is not None else None,
    "candidate_meets_confidence":rows[0]["confidence"]>=policy["rules"]["min_confidence_to_recommend"],
    "execution_requires_explicit_command":True,
    "note":"Candidate only. Execution is separate; expected effects are not actual outcomes."
}
(ROOT/"current_decision_receipt.json").write_text(json.dumps(receipt,indent=2),encoding="utf-8")
print(json.dumps(receipt,indent=2))
