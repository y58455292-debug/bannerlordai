from pathlib import Path
import subprocess, json, hashlib, sys, datetime

ROOT=Path(r"D:\BannerlordAIResearch")
H=ROOT/r"Longitudinal\EvidenceIndex\HandoffV3"
OFFICE=ROOT/r"Automation\Office"
RECEIPTS=OFFICE/r"ProtocolReceipts"
FOLLOW=ROOT/r"Tools\Handoff\follow_protocol.py"
LOOP_SYNC=ROOT/r"Tools\Autopilot\sync_thinking_loop.py"
LOOP_STATE=OFFICE/r"thinking_loop.json"

ROLE_FILES={
    "coder": OFFICE/r"ChatGPTProjectInstructions\10_CODER_DESK.txt",
    "analyzer": OFFICE/r"ChatGPTProjectInstructions\20_ANALYZER_DESK.txt",
}
LAW=ROOT/"PROJECT_LAW.md"
PROTOCOL=H/"PROTOCOL.md"

def sha(p):
    return hashlib.sha256(p.read_bytes()).hexdigest().upper() if p.exists() else None

def load(p):
    return json.loads(p.read_text(encoding="utf-8-sig")) if p.exists() else {}

def main():
    if len(sys.argv)!=2 or sys.argv[1].lower() not in ROLE_FILES:
        print(json.dumps({"status":"FAIL","reason":"usage: agent_protocol_entry.py coder|analyzer"}))
        return 2
    role=sys.argv[1].lower()

    run=subprocess.run([sys.executable,"-X","utf8",str(FOLLOW)],capture_output=True,text=True)
    try:
        recovery=json.loads(run.stdout.strip().splitlines()[-1])
    except Exception:
        recovery={"status":"UNPARSEABLE","raw":run.stdout[-500:]}

    subprocess.run([sys.executable,"-X","utf8",str(LOOP_SYNC)],capture_output=True,text=True)
    current=load(H/"current.json")
    clock=load(OFFICE/"office_clock_status.json") if (OFFICE/"office_clock_status.json").exists() else {}
    loop=load(LOOP_STATE)

    next_action=str(current.get("next_action",""))
    target_ok=next_action.lower().startswith(role)
    loop_role_ok=(
        loop.get("active_role")==role or
        (
            loop.get("controller_mode")=="SINGLE_FRONT_CONTROLLER" and
            loop.get("active_role")=="front" and
            loop.get("logical_phase_owner")==role
        )
    )
    budget_ok=not bool(loop.get("repair_budget_exhausted"))

    role_clock=(clock.get("departments",{}).get(role,{}) or {})
    clocked_in=bool(role_clock.get("clocked_in",True))
    allowed_classes={str(x).upper() for x in (role_clock.get("allowed_work_classes") or [])}
    work_allowed = clocked_in
    entry_mode = "FULL" if clocked_in else "DENIED"
    owner_file=ROOT/r"workspace\DEPLOYMENT_OWNER.json"
    owner_clear=not owner_file.exists()
    recovery_ok=recovery.get("status")=="FAST_OK"
    safety_ok = owner_clear if role=="coder" else True

    ok=all([recovery_ok,target_ok,loop_role_ok,budget_ok,work_allowed,safety_ok])
    receipt={
        "schema":"BannerlordAI.AgentProtocolEntry.v2",
        "created_at":datetime.datetime.now().astimezone().isoformat(),
        "effective_after_checkpoint_seq":113,
        "role":role,
        "status":"PASS" if ok else "FAIL",
        "recovery_status":recovery.get("status"),
        "recovered_checkpoint_seq":recovery.get("checkpoint_seq"),
        "current_checkpoint_seq":current.get("checkpoint_seq"),
        "next_action":next_action,
        "next_action_targets_role":target_ok,
        "thinking_loop_active_role":loop.get("active_role"),
        "thinking_loop_role_matches":loop_role_ok,
        "thinking_loop_turn_seq":loop.get("turn_seq"),
        "thinking_loop_repair_iterations":loop.get("repair_iterations"),
        "thinking_loop_budget_ok":budget_ok,
        "clocked_in":clocked_in,
        "allowed_work_classes":sorted(allowed_classes),
        "work_allowed_at_entry":work_allowed,
        "entry_mode":entry_mode,
        "owner_clear_at_entry":owner_clear,
        "live_control_permitted": role=="coder" and clocked_in and clock.get("office_mode")=="OPEN",
        "project_law_path":str(LAW),
        "project_law_sha256":sha(LAW),
        "protocol_path":str(PROTOCOL),
        "protocol_sha256":sha(PROTOCOL),
        "role_instruction_path":str(ROLE_FILES[role]),
        "role_instruction_sha256":sha(ROLE_FILES[role]),
        "structured_handoff_schema_sha256":sha(OFFICE/"structured_handoff_schema.json"),
        "thinking_loop_path":str(LOOP_STATE)
    }
    RECEIPTS.mkdir(parents=True,exist_ok=True)
    out=RECEIPTS/f"{role}_entry.json"
    out.write_text(json.dumps(receipt,indent=2),encoding="utf-8")
    print(json.dumps(receipt,indent=2))
    return 0 if ok else 3

if __name__=="__main__":
    raise SystemExit(main())
