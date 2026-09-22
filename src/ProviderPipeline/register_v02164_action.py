from pathlib import Path
import datetime, json, os
ROOT = Path(r"D:\BannerlordAIResearch")
REG = ROOT / r"Automation\Autopilot\registry.json"
STATE = ROOT / r"Automation\Autopilot\state.json"
def load(p): return json.loads(p.read_text(encoding="utf-8-sig"))
def save(p, obj):
    t = p.with_suffix(p.suffix + ".tmp")
    t.write_text(json.dumps(obj, indent=2) + "\n", encoding="utf-8")
    os.replace(t, p)
reg = load(REG)
reg.setdefault("actions", {})["v02164_max_attempts_policy"] = {
    "enabled": True,
    "description": "v0.2.10.64 registered maxAttempts transport-execution authorization/count enforcement before deterministic loopback ProviderResult.v4.",
    "command": ["python","-X","utf8",r"D:\BannerlordAIResearch\workspace\validate_v02164_max_attempts_policy.py"],
    "required_candidate_version": "v0.2.10.64-execution-policy-max-attempts-enforcement-shadow",
    "required_next_action_contains": "v0.2.10.64", "max_attempts": 1, "next_action_id": None,
    "owner": "coder", "work_class": "LIVE_GAME", "may_launch_game": True, "requires_analyzer_clear": True}
save(REG, reg)
state = load(STATE)
state.update({"pending_action_id":"v02164_max_attempts_policy","status":"READY","attempts":0,"runner_pid":None,
              "rearmed_reason":"v0.2.10.64 maxAttempts candidate/validator preflighted; all fixture layers green.",
              "rearmed_at":datetime.datetime.now().astimezone().isoformat()})
save(STATE, state)
print(json.dumps({"registered": True, "action_id": state["pending_action_id"], "status": state["status"]}))
