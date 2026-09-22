from pathlib import Path
import json, hashlib, sys

ROOT=Path(r"D:\BannerlordAIResearch")
OFFICE=ROOT/r"Automation\Office"
H=ROOT/r"Longitudinal\EvidenceIndex\HandoffV3"
EFFECTIVE_AFTER=113
ROLE_FILES={
 "coder":OFFICE/r"ChatGPTProjectInstructions\10_CODER_DESK.txt",
 "analyzer":OFFICE/r"ChatGPTProjectInstructions\20_ANALYZER_DESK.txt"
}

def load(p):
    return json.loads(p.read_text(encoding="utf-8-sig")) if p.exists() else {}

def sha(p):
    return hashlib.sha256(p.read_bytes()).hexdigest().upper() if p.exists() else None

def latest_handoff(role):
    if role=="coder":
        files=[OFFICE/r"Handoffs\coder_to_analyzer.json"]
    else:
        files=[OFFICE/r"Handoffs\analyzer_to_front.json",OFFICE/r"Handoffs\analyzer_to_coder.json"]
    ranked=[]
    for p in files:
        h=load(p)
        seq=(h.get("completion_receipt") or {}).get("checkpoint_seq")
        ranked.append((seq if isinstance(seq,int) else -1,p,h))
    return max(ranked,key=lambda x:x[0])

def main():
    if len(sys.argv)!=2 or sys.argv[1].lower() not in ROLE_FILES:
        print(json.dumps({"pass":False,"reason":"usage: check_worker_protocol.py coder|analyzer"}))
        return 2
    role=sys.argv[1].lower()
    terminal,hpath,handoff=latest_handoff(role)
    if isinstance(terminal,int) and terminal<=EFFECTIVE_AFTER:
        print(json.dumps({"schema":"BannerlordAI.WorkerProtocolCheck.v2","pass":True,"role":role,"grandfathered":True,"terminal_checkpoint_seq":terminal},indent=2)); return 0
    receipt_path=OFFICE/r"ProtocolReceipts"/f"{role}_entry.json"
    receipt=load(receipt_path)
    current=load(H/"current.json")
    loop=load(OFFICE/"thinking_loop.json")
    checks={
      "receipt_exists":receipt_path.exists(),
      "receipt_status_pass":receipt.get("status")=="PASS",
      "role_matches":receipt.get("role")==role,
      "recovery_status_fast_ok":receipt.get("recovery_status")=="FAST_OK",
      "next_action_targeted_role":receipt.get("next_action_targets_role") is True,
      "thinking_loop_role_matched_at_entry":receipt.get("thinking_loop_role_matches") is True,
      "thinking_loop_budget_ok_at_entry":receipt.get("thinking_loop_budget_ok") is True,
      "work_allowed_at_entry":receipt.get("work_allowed_at_entry") is True,
      "entry_mode_valid":receipt.get("entry_mode")=="FULL",
      "entry_before_terminal":isinstance(terminal,int) and isinstance(receipt.get("recovered_checkpoint_seq"),int) and receipt.get("recovered_checkpoint_seq")<=terminal,
      "project_law_hash_current":receipt.get("project_law_sha256")==sha(ROOT/"PROJECT_LAW.md"),
      "protocol_hash_current":receipt.get("protocol_sha256")==sha(H/"PROTOCOL.md"),
      "role_instruction_hash_current":receipt.get("role_instruction_sha256")==sha(ROLE_FILES[role]),
      "handoff_schema_hash_current":receipt.get("structured_handoff_schema_sha256")==sha(OFFICE/"structured_handoff_schema.json")
    }
    if role=="coder":
        checks["owner_clear_at_entry"]=receipt.get("owner_clear_at_entry") is True
    ok=all(checks.values())
    print(json.dumps({"schema":"BannerlordAI.WorkerProtocolCheck.v2","pass":ok,"role":role,"checks":checks,"receipt":str(receipt_path),"handoff":str(hpath),"terminal_checkpoint_seq":terminal,"current_checkpoint_seq":current.get("checkpoint_seq"),"current_loop_role":loop.get("active_role")},indent=2))
    return 0 if ok else 3
if __name__=="__main__": raise SystemExit(main())
