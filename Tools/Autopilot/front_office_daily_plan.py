from pathlib import Path
import datetime
import json
import os
import subprocess
import sys

ROOT=Path(r"D:\BannerlordAIResearch")
OFFICE=ROOT/r"Automation\Office"
CLOCK_SCRIPT=ROOT/r"Tools\Autopilot\office_clock.py"
CLOCK=OFFICE/"office_clock_status.json"
FRONT=ROOT/r"Longitudinal\EvidenceIndex\ContextPyramid\front_page.json"
CODER=OFFICE/"coder_status.json"
ANALYZER=OFFICE/"analyzer_status.json"
SUPERVISOR=ROOT/r"Automation\Autopilot\state.json"
POLICY=OFFICE/"office_policy.json"
PLAN=OFFICE/"daily_plan.json"
PLAN_MD=OFFICE/"daily_plan.md"
LOOP=OFFICE/"thinking_loop.json"

def now():
    return datetime.datetime.now().astimezone()

def read_json(p):
    try:
        return json.loads(p.read_text(encoding="utf-8-sig")) if p.exists() else {}
    except Exception as ex:
        return {"_error":type(ex).__name__+": "+str(ex)}

def atomic_json(p,obj):
    p.parent.mkdir(parents=True,exist_ok=True)
    tmp=p.with_suffix(p.suffix+".tmp")
    tmp.write_text(json.dumps(obj,indent=2,ensure_ascii=False)+"\n",encoding="utf-8")
    os.replace(tmp,p)

def refresh_clock():
    subprocess.run(
        [sys.executable,"-X","utf8",str(CLOCK_SCRIPT)],
        cwd=str(ROOT),capture_output=True,text=True,errors="replace"
    )
    return read_json(CLOCK)

def choose_bottleneck(front,coder,analyzer,supervisor,loop):
    blockers=front.get("blockers") or []
    if blockers:
        return {
            "kind":"BLOCKER",
            "owner":"front",
            "reason":"Explicit project blockers exist",
            "detail":blockers
        }
    loop_role=loop.get("logical_phase_owner") or loop.get("active_role")
    if loop_role=="analyzer":
        return {
            "kind":"ANALYZER_REVIEW",
            "owner":"analyzer",
            "reason":"Front Controller is in VERIFY phase against Analyzer-grade evidence gates",
            "detail":loop.get("derived_from_next_action")
        }
    if loop_role=="coder":
        return {
            "kind":"CODER_EXECUTION",
            "owner":"coder",
            "reason":"Front Controller is in EXECUTE phase under Coder safeguards",
            "detail":loop.get("derived_from_next_action")
        }
    if analyzer.get("state") in ("INTAKE_PENDING","INDEXED_PENDING_REASONING_REVIEW"):
        return {
            "kind":"ANALYZER_REVIEW",
            "owner":"analyzer",
            "reason":"Coder evidence exists but semantic promotion is unfinished",
            "detail":analyzer.get("classification")
        }
    if supervisor.get("status")=="READY":
        return {
            "kind":"CODER_EXECUTION",
            "owner":"coder",
            "reason":"Registered deterministic work is ready and review gates are clear",
            "detail":supervisor.get("pending_action_id")
        }
    active=front.get("active") or {}
    if active.get("version"):
        return {
            "kind":"ROADMAP_NEXT_GATE",
            "owner":"front",
            "reason":"Active candidate exists but no deterministic action is currently ready",
            "detail":front.get("next")
        }
    return {
        "kind":"PLANNING",
        "owner":"front",
        "reason":"No active execution/review packet is queued",
        "detail":front.get("next")
    }

def assignment(role,state,objective,classes,completion,escalation,wip):
    return {
        "role":role,
        "shift_state":state,
        "objective":objective,
        "allowed_work_classes":classes,
        "wip_limit":wip,
        "completion_condition":completion,
        "escalation_condition":escalation
    }

def build(preview=False):
    clock=refresh_clock()
    front=read_json(FRONT)
    coder=read_json(CODER)
    analyzer=read_json(ANALYZER)
    supervisor=read_json(SUPERVISOR)
    loop=read_json(LOOP)
    policy=read_json(POLICY)
    mode=clock.get("office_mode")
    bottleneck=choose_bottleneck(front,coder,analyzer,supervisor,loop)
    offline=(mode=="OFFLINE")
    closed=(mode=="CLOSED")
    date=now().date().isoformat()

    if offline:
        coder_state="CLOCKED_OUT"
        analyzer_state="CLOCKED_OUT"
        supervisor_state="OFFLINE_LAB"
        front_state="OFFLINE_CONTROL"
    elif closed:
        coder_state="CLOCKED_OUT"
        analyzer_state="CLOCKED_OUT"
        supervisor_state="CLOCKED_OUT"
        front_state="CLOSED_CONTROL"
    else:
        coder_state="LOGICAL_EXECUTE_PHASE" if bottleneck.get("owner")=="coder" else "INTERNAL_STANDBY"
        analyzer_state="LOGICAL_VERIFY_PHASE" if bottleneck.get("owner")=="analyzer" else "INTERNAL_STANDBY"
        supervisor_state="CLOCKED_IN"
        front_state="CLOCKED_IN_CONTROLLER"

    assignments={
      "front":assignment(
        "Front Office / Coordinator",front_state,
        ("Expose power/status only; engineering remains paused while offline." if offline else "Expose power/status only; all project work remains paused while CLOSED." if closed else "Own the full PLAN -> EXECUTE -> VERIFY roadmap loop, maintain durable state, and publish only material updates."),
        ["status","coordination","routing","chat_registry","planning"],
        "Daily plan current, bottleneck explicit, next handoff assigned.",
        "Integrity incident, conflicting priorities, or work cannot be routed without new user decision.",
        1
      ),
      "coder":assignment(
        "Coder / Operator",coder_state,
        (
          "Clocked out while OFFLINE; no code/build/static-analysis work."
          if offline
          else "Clocked out while CLOSED; all Coder work is paused."
          if closed
          else "Front Controller executes the current packet under Coder-phase safeguards and produces reproducible evidence."
          if bottleneck.get("owner")=="coder"
          else "Stand by for the next roadmap packet."
        ),
        ((clock.get("departments") or {}).get("coder") or {}).get("allowed_work_classes") or [],
        "One terminal work packet; coder_status + Analyzer intake written.",
        "New architecture required, integrity drift, repeated failure, or task exceeds shift/WIP boundary.",
        1
      ),
      "analyzer":assignment(
        "Analyzer / Archivist",analyzer_state,
        (
          "Clocked out while OFFLINE; offline lab evidence is stored for later review after OPEN."
          if offline
          else "Clocked out while CLOSED; analysis is paused."
          if closed
          else "Front Controller performs the independent VERIFY phase against raw evidence and prepares the next clean acceptance gate."
          if bottleneck.get("owner")=="analyzer"
          else "Internal VERIFY capability stands by; no separate Analyzer chat is required for routine review."
        ),
        ((clock.get("departments") or {}).get("analyzer") or {}).get("allowed_work_classes") or [],
        "Evidence classified; durable lessons/index updated; next gate handed to Front/Coder.",
        "Contradictory evidence, unknown causality, or major architecture/policy change requiring collaboration.",
        1
      ),
      "supervisor":assignment(
        "Supervisor / Platform",supervisor_state,
        ("Run only registered SIMULATION/DATA_GATHER evidence-lab work; do not promote conclusions." if offline else "Remain safety/continuity-only while CLOSED; dispatch no project work." if closed else "Enforce manual office power, WIP, ownership, integrity and registered-action dispatch. Never invent work."),
        ((clock.get("departments") or {}).get("supervisor") or {}).get("allowed_work_classes") or [],
        "No unauthorized/duplicate work; registered queue accurately reflected.",
        "Clock/lease/hash conflict, dispatch failure, or incident trigger.",
        1
      )
    }

    sequence=[]
    if offline:
        sequence=[
          "Run only registered SIMULATION and DATA_GATHER work on the latest validated mod build.",
          "Store telemetry/evidence without code changes, semantic promotion, breakthrough analysis, or roadmap mutation.",
          "Wait for explicit Front command `open office` before Coder/Analyzer roadmap work resumes."
        ]
    elif closed:
        sequence=[
          "Run no engineering, simulation, data gathering, analysis, or roadmap work.",
          "Preserve continuity/integrity only and wait for explicit Front command `open office` or `offline mode`."
        ]
    elif bottleneck.get("owner")=="analyzer":
        sequence=[
          "Analyzer completes pending review.",
          "Front accepts/promotes result and refreshes roadmap.",
          "Front registers the next safe deterministic Coder packet.",
          "Supervisor dispatches only if office/work-class gates permit."
        ]
    elif bottleneck.get("owner")=="coder":
        sequence=[
          "Supervisor dispatches one registered Coder packet.",
          "Coder completes and writes evidence.",
          "Analyzer intake/review follows.",
          "Front updates user only for material outcome."
        ]
    else:
        sequence=[
          "Front resolves/defines next smallest gate.",
          "Analyzer facilitates evidence/prior-art review if needed.",
          "Front registers deterministic work only after acceptance criteria are explicit."
        ]

    plan={
      "schema":"BannerlordAI.DailyOfficePlan.v1",
      "generated_at":now().isoformat(),
      "date":date,
      "office_mode":mode,
      "preview":bool(preview),
      "accepted":front.get("accepted"),
      "active":front.get("active"),
      "current_bottleneck":bottleneck,
      "one_primary_objective":front.get("question") or front.get("next"),
      "assignments":assignments,
      "sequence":sequence,
      "chat_update_policy":(policy.get("chat_policy") or {}),
      "information_order":policy.get("information_order") or ["FRONT","HOT","WARM","RAW"],
      "launch_blocked":closed,
      "engineering_blocked":offline or closed,
      "simulation_allowed":offline,
      "all_work_paused":closed
    }
    atomic_json(PLAN,plan)
    lines=[
      "# BannerlordAI Daily Office Plan — "+date,
      "",
      f"Office mode: **{mode}**",
      f"Primary bottleneck: **{bottleneck.get('kind')} / {bottleneck.get('owner')}**",
      "",
      "## Sequence",
    ]+[f"- {x}" for x in sequence]+["","## Departments"]
    for key,a in assignments.items():
        lines += [
          f"### {a['role']}",
          f"- State: {a['shift_state']}",
          f"- Objective: {a['objective']}",
          f"- Completion: {a['completion_condition']}",
          f"- Escalation: {a['escalation_condition']}",
        ]
    PLAN_MD.write_text("\n".join(lines)+"\n",encoding="utf-8")
    return plan

def main():
    plan=build("--preview" in sys.argv)
    print(json.dumps(plan,indent=2,ensure_ascii=False))
    return 0

if __name__=="__main__":
    raise SystemExit(main())
