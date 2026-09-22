from pathlib import Path
import json, os, datetime, sys

ROOT=Path(r"D:\BannerlordAIResearch")
CURRENT=ROOT/r"Longitudinal\EvidenceIndex\HandoffV3\current.json"
STATE=ROOT/r"Automation\Office\thinking_loop.json"

def load(p):
    return json.loads(p.read_text(encoding="utf-8-sig")) if p.exists() else {}

def atomic(path,data):
    path.parent.mkdir(parents=True,exist_ok=True)
    tmp=path.with_suffix(path.suffix+".tmp")
    tmp.write_text(json.dumps(data,indent=2)+"\n",encoding="utf-8")
    os.replace(tmp,path)

def phase_owner_from_next(s):
    t=(s or "").strip().lower()
    if t.startswith("coder") or " execute phase" in t: return "coder"
    if t.startswith("analyzer") or " verify phase" in t: return "analyzer"
    return "front"

def phase_from_owner(owner):
    if owner=="coder": return "EXECUTE"
    if owner=="analyzer": return "VERIFY"
    return "PLAN"

def main():
    cur=load(CURRENT)
    old=load(STATE)
    target=phase_owner_from_next(cur.get("next_action"))
    prev=old.get("logical_phase_owner") or old.get("active_role")
    turn_seq=int(old.get("turn_seq") or 0)
    repair_iterations=int(old.get("repair_iterations") or 0)
    milestone_version=((cur.get("last_accepted_milestone") or {}).get("version") or None)
    previous_milestone_version=old.get("milestone_version")
    if milestone_version and milestone_version != previous_milestone_version:
        repair_iterations=0
    if target != prev:
        turn_seq += 1
        if prev=="analyzer" and target=="coder":
            repair_iterations += 1
    max_repairs=int(old.get("max_direct_repair_iterations") or 3)
    exhausted=repair_iterations >= max_repairs and target=="coder"
    state={
      "schema":"BannerlordAI.ThinkingLoop.v1",
      "updated_at":datetime.datetime.now().astimezone().isoformat(),
      "checkpoint_seq":cur.get("checkpoint_seq"),
      "active_role":"front",
      "controller_mode":"SINGLE_FRONT_CONTROLLER",
      "active_phase":"PLAN" if exhausted else phase_from_owner(target),
      "logical_phase_owner":"front" if exhausted else target,
      "derived_from_next_action":cur.get("next_action"),
      "previous_phase_owner":prev,
      "turn_seq":turn_seq,
      "repair_iterations":repair_iterations,
      "milestone_version":milestone_version,
      "max_direct_repair_iterations":max_repairs,
      "repair_budget_exhausted":exhausted,
      "transition_policy":{
        "execute_terminal":"VERIFY",
        "verify_pass":"PLAN",
        "verify_bounded_fail_or_harness_false_negative":"EXECUTE",
        "verify_unknown_architecture_integrity_or_user_dependency":"PLAN",
        "plan_routed_work":"EXECUTE"
      },
      "rule":"One Front Controller owns the live chat and runs PLAN -> EXECUTE -> VERIFY as distinct evidence phases. Separate Coder/Analyzer chats are optional escalation/review surfaces, not routine handoff dependencies."
    }
    atomic(STATE,state)
    print(json.dumps(state,indent=2))
    return 0

if __name__=="__main__":
    raise SystemExit(main())
