from pathlib import Path
import datetime
import json
import os
import sys

ROOT = Path(r"D:\BannerlordAIResearch")
OFFICE = ROOT / r"Automation\Office"
CURRENT = ROOT / r"Longitudinal\EvidenceIndex\HandoffV3\current.json"
FRONT_PAGE = ROOT / r"Longitudinal\EvidenceIndex\ContextPyramid\front_page.json"
STATE = OFFICE / "front_rollover_state.json"
POLICY = OFFICE / "chat_shift_policy.json"
HANDOFF = OFFICE / r"Handoffs\front_to_next_front.json"
LOOP = OFFICE / "thinking_loop.json"
POWER = OFFICE / "office_power.json"

def now():
    return datetime.datetime.now().astimezone()

def load(path):
    try:
        return json.loads(path.read_text(encoding="utf-8-sig")) if path.exists() else {}
    except Exception:
        return {}

def atomic(path, obj):
    path.parent.mkdir(parents=True, exist_ok=True)
    tmp = path.with_suffix(path.suffix + ".tmp")
    tmp.write_text(json.dumps(obj, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    os.replace(tmp, path)

def parse_iso(value):
    try:
        return datetime.datetime.fromisoformat(value) if value else None
    except Exception:
        return None

def pressure(state, current, front_policy):
    started = parse_iso(state.get("session_started_at")) or now()
    if started.tzinfo is None:
        started = started.replace(tzinfo=now().tzinfo)
    age_minutes = max(0.0, (now() - started.astimezone(now().tzinfo)).total_seconds() / 60.0)
    max_minutes = float(front_policy.get("max_wall_minutes") or 180)
    max_updates = float(front_policy.get("max_material_updates") or 6)
    max_touches = float(front_policy.get("max_front_touches") or 18)
    max_checkpoints = float(front_policy.get("max_checkpoint_delta") or 12)
    start_seq = int(state.get("start_checkpoint_seq") or current.get("checkpoint_seq") or 0)
    current_seq = int(current.get("checkpoint_seq") or start_seq)
    checkpoint_delta = max(0, current_seq - start_seq)
    ratios = {
        "wall_time": age_minutes / max_minutes,
        "material_updates": float(state.get("material_updates") or 0) / max_updates,
        "front_touches": float(state.get("front_touches") or 0) / max_touches,
        "checkpoint_delta": float(checkpoint_delta) / max_checkpoints,
    }
    score = max(ratios.values())
    amber = float(front_policy.get("prepare_successor_ratio") or 0.65)
    red = float(front_policy.get("rotate_ratio") or 0.85)
    level = "RED" if score >= red else ("AMBER" if score >= amber else "GREEN")
    return level, score, ratios, age_minutes, checkpoint_delta

def successor_packet(current, state, level, score):
    checkpoint = current.get("checkpoint_seq")
    next_action = current.get("next_action")
    loop = load(LOOP)
    power = load(POWER)
    return {
        "schema": "BannerlordAI.StructuredHandoff.v1",
        "from_role": "front",
        "to_role": "next_front",
        "intent": "Continue Front Watch from durable state without replaying the prior chat.",
        "current_state": f"Successor packet prepared at checkpoint {checkpoint}; context pressure {level} ({score:.2f}).",
        "constraints": [
            "Run follow_protocol.py before doing any work.",
            "Disk/runtime evidence outranks chat history.",
            "Do not activate a duplicate Front worker; this packet is dormant until the current Front chat rotates.",
            "Keep Front as the single live execution controller and preserve PLAN -> EXECUTE -> VERIFY phase separation.",
            "Large logs stay external; dereference only what the active question requires."
        ],
        "evidence_pointers": [
            str(CURRENT),
            str(FRONT_PAGE),
            str(OFFICE / "front_status.json"),
            str(OFFICE / "daily_plan.json"),
            str(OFFICE / "thinking_loop.json"),
            str(OFFICE / "office_power.json"),
            str(OFFICE / "coder_status.json"),
            str(OFFICE / "analyzer_status.json"),
            str(OFFICE / r"Handoffs\coder_to_analyzer.json"),
            str(OFFICE / r"Handoffs\analyzer_to_front.json"),
            str(STATE)
        ],
        "next_action": next_action,
        "controller_state": {
            "mode": loop.get("controller_mode"),
            "active_phase": loop.get("active_phase"),
            "logical_phase_owner": loop.get("logical_phase_owner"),
            "office_power": power.get("mode"),
            "repair_iterations": loop.get("repair_iterations"),
            "repair_budget_exhausted": loop.get("repair_budget_exhausted")
        },
        "acceptance_context": {
            "last_accepted_milestone": current.get("last_accepted_milestone"),
            "active_candidate": current.get("active_candidate"),
            "active_question": current.get("active_question"),
            "blockers": current.get("blockers") or []
        },
        "completion_receipt": {
            "terminal_status": "ROLLOVER_READY",
            "result_pointer": str(STATE),
            "handoff_pointer": str(HANDOFF),
            "checkpoint_seq": checkpoint,
            "owner_released": (current.get("run_state") or {}).get("deployment_owner") is None
        },
        "rollover": {
            "prepared_at": now().isoformat(),
            "pressure_level": level,
            "pressure_ratio": round(score, 4),
            "activation_command": "Front watch — recover current assignment and continue.",
            "dormant_until_rotation": True
        }
    }

def main():
    mode = (sys.argv[1].lower() if len(sys.argv) > 1 else "touch")
    forced_saturated = mode in ("saturated", "rotate", "red")
    current = load(CURRENT)
    policy = load(POLICY)
    front_policy = ((policy.get("roles") or {}).get("front") or {})
    state = load(STATE)
    if mode != "activate" and state.get("forced_saturation_signal") is True:
        forced_saturated = True
    if mode == "activate" or not state:
        state = {
            "schema": "BannerlordAI.FrontRolloverState.v1",
            "session_started_at": now().isoformat(),
            "start_checkpoint_seq": current.get("checkpoint_seq"),
            "front_touches": 0,
            "material_updates": 0,
        }
    if mode in ("touch", "material"):
        state["front_touches"] = int(state.get("front_touches") or 0) + 1
    if mode == "material":
        state["material_updates"] = int(state.get("material_updates") or 0) + 1

    level, score, ratios, age_minutes, checkpoint_delta = pressure(state, current, front_policy)
    if forced_saturated:
        level = "RED"
        score = max(score, 1.0)
    state.update({
        "updated_at": now().isoformat(),
        "last_checkpoint_seq": current.get("checkpoint_seq"),
        "checkpoint_delta": checkpoint_delta,
        "age_minutes": round(age_minutes, 2),
        "pressure_level": level,
        "pressure_ratio": round(score, 4),
        "pressure_components": {k: round(v, 4) for k, v in ratios.items()},
        "successor_ready": True,
        "successor_handoff": str(HANDOFF),
        "rotation_requested": level == "RED",
        "forced_saturation_signal": forced_saturated,
        "activation_command": "Front watch — recover current assignment and continue.",
        "exact_token_signal_available": False,
        "rule": "Use conservative operational pressure proxies; keep a dormant successor packet ready at all times."
    })
    atomic(STATE, state)
    atomic(HANDOFF, successor_packet(current, state, level, score))
    print(json.dumps(state, indent=2, ensure_ascii=False))
    return 2 if level == "RED" else 0

if __name__ == "__main__":
    raise SystemExit(main())
