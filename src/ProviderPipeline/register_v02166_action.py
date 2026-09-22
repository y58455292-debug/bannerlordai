from pathlib import Path
import datetime, json, os

ROOT=Path(r"D:\BannerlordAIResearch")
REG=ROOT/r"Automation\Autopilot\registry.json"
STATE=ROOT/r"Automation\Autopilot\state.json"
def load(p): return json.loads(p.read_text(encoding="utf-8-sig"))
def save(p,obj):
    tmp=p.with_suffix(p.suffix+".tmp")
    tmp.write_text(json.dumps(obj,indent=2)+"\n",encoding="utf-8")
    os.replace(tmp,p)

reg=load(REG)
reg.setdefault("actions",{})["v02166_authorization_bound_result"]={
    "enabled":True,
    "description":"v0.2.10.66 exact transport-execution authorization-bound TransportReceipt.v4 + ProviderResult.v5 stale-result rejection.",
    "command":["python","-X","utf8",r"D:\BannerlordAIResearch\workspace\validate_v02166_authorization_bound_result.py"],
    "required_candidate_version":"v0.2.10.66-transport-execution-authorization-bound-result-shadow",
    "required_next_action_contains":"v0.2.10.66",
    "max_attempts":1,
    "next_action_id":None,
    "owner":"coder",
    "work_class":"LIVE_GAME",
    "may_launch_game":True,
    "requires_analyzer_clear":True,
}
save(REG,reg)

state=load(STATE)
state.update({
    "pending_action_id":"v02166_authorization_bound_result",
    "status":"READY",
    "attempts":0,
    "runner_pid":None,
    "rearmed_reason":"v0.2.10.66 candidate/validator preflighted; v1-v5/backcompat and loopback fixtures green.",
    "rearmed_at":datetime.datetime.now().astimezone().isoformat(),
})
save(STATE,state)
print(json.dumps({"registered":True,"action_id":state["pending_action_id"],"status":state["status"]}))
