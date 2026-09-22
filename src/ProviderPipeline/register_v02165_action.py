from pathlib import Path
import datetime, json, os
ROOT=Path(r"D:\BannerlordAIResearch")
REG=ROOT/r"Automation\Autopilot\registry.json"; STATE=ROOT/r"Automation\Autopilot\state.json"
def load(p): return json.loads(p.read_text(encoding="utf-8-sig"))
def save(p,o):
    t=p.with_suffix(p.suffix+".tmp"); t.write_text(json.dumps(o,indent=2)+"\n",encoding="utf-8"); os.replace(t,p)
reg=load(REG)
reg.setdefault("actions",{})["v02165_timeout_budget"]={
 "enabled":True,
 "description":"v0.2.10.65 exact registered timeoutMs binding into transport-execution authorization and deterministic loopback.",
 "command":["python","-X","utf8",r"D:\BannerlordAIResearch\workspace\validate_v02165_timeout_budget.py"],
 "required_candidate_version":"v0.2.10.65-execution-policy-timeout-budget-binding-shadow",
 "required_next_action_contains":"v0.2.10.65","max_attempts":1,"next_action_id":None,
 "owner":"coder","work_class":"LIVE_GAME","may_launch_game":True,"requires_analyzer_clear":True}
save(REG,reg)
s=load(STATE)
s.update({"pending_action_id":"v02165_timeout_budget","status":"READY","attempts":0,"runner_pid":None,
 "rearmed_reason":"v0.2.10.65 timeout-budget candidate/validator preflighted; all fixture layers green.",
 "rearmed_at":datetime.datetime.now().astimezone().isoformat()})
save(STATE,s)
print(json.dumps({"registered":True,"action_id":s["pending_action_id"],"status":s["status"]}))
