import json
from pathlib import Path
p=Path(r"D:\BannerlordAIResearch\Longitudinal\EvidenceIndex\current_state.json")
x=json.loads(p.read_text(encoding="utf-8"))
x["active_milestone"]="v0.20B first global causal judgment intervention"
x["active_question"]="Does suppressing weak independent lord bandit EngageParty at returned MobilePartyAIModel initiative judgment reduce weak lord-bandit battles while preserving healthy native rear-security and performance?"
x["proof_chain"]=[
  "inner NavalDLC GetBestInitiativeBehavior result",
  "v0.20B weak-bandit suppression event",
  "native short-term AI result",
  "lord-vs-bandit MapEvent battle starts",
  "performance + writer health"
]
x["current_builds"]={
  "ClanAI":{"version":"v0.20B","sha256":"50C15CADAE4290B3132713ADBC588876187F21C57C1B4A97F636614C43CFFFD9"},
  "BannerlordInspector":{"version":"v0.32","sha256":"E239BE1FBB579E6DC5264FEAC5B011B4EB9256E479B36B9C658FE81E6D1DF8BD"}
}
x["baseline_checkpoint"]=r"D:\BannerlordAIResearch\Longitudinal\SaveCheckpoints\20260918_051711_global_model_mirror_equivalence_pass"
x["baseline_save_sha256"]="DA864489DD7C6A23879B270B08ECD257E0A13BD97201EB4F80541C4413220FF1"
p.write_text(json.dumps(x,indent=2),encoding="utf-8")
print(p)
