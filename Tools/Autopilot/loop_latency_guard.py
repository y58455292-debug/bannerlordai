from pathlib import Path
import datetime, json, os

ROOT=Path(r"D:\BannerlordAIResearch")
OFFICE=ROOT/r"Automation\Office"
CURRENT=ROOT/r"Longitudinal\EvidenceIndex\HandoffV3\current.json"
LOOP=OFFICE/"thinking_loop.json"
OUT=OFFICE/"loop_latency_state.json"
RECEIPTS=OFFICE/"ProtocolReceipts"
POWER=OFFICE/"office_power.json"

def load(p):
    try: return json.loads(p.read_text(encoding="utf-8-sig")) if p.exists() else {}
    except Exception: return {}

def atomic(p,obj):
    p.parent.mkdir(parents=True,exist_ok=True)
    t=p.with_suffix(p.suffix+".tmp")
    t.write_text(json.dumps(obj,indent=2)+"\n",encoding="utf-8")
    os.replace(t,p)

def parse(v):
    try: return datetime.datetime.fromisoformat(v)
    except Exception: return None

def main():
    cur=load(CURRENT); loop=load(LOOP); power=load(POWER)
    office_mode=str(power.get("mode") or "OFFLINE").upper()
    role=loop.get("active_role") or "front"
    seq=cur.get("checkpoint_seq")
    updated=parse(cur.get("updated_at"))
    now=datetime.datetime.now().astimezone()
    age_min=max(0.0,(now-updated.astimezone(now.tzinfo)).total_seconds()/60) if updated else None
    claimed=True; receipt_seq=None
    if role in ("coder","analyzer"):
        r=load(RECEIPTS/f"{role}_entry.json")
        receipt_seq=r.get("recovered_checkpoint_seq")
        claimed=isinstance(receipt_seq,int) and isinstance(seq,int) and receipt_seq>=seq and r.get("status")=="PASS"
    warn=bool(office_mode=="OPEN" and role in ("coder","analyzer") and not claimed and age_min is not None and age_min>=5)
    state={
      "schema":"BannerlordAI.LoopLatencyState.v1",
      "updated_at":now.isoformat(),
      "checkpoint_seq":seq,
      "office_mode":office_mode,
      "active_role":role,
      "turn_seq":loop.get("turn_seq"),
      "turn_age_minutes":round(age_min,2) if age_min is not None else None,
      "claimed":claimed,
      "claim_receipt_checkpoint_seq":receipt_seq,
      "claim_sla_minutes":5,
      "warning":"%s_TURN_UNCLAIMED_GT5M"%role.upper() if warn else None,
      "needs_attention":warn,
      "activation_command":(
        "Coder desk — recover current assignment and continue."
        if role=="coder" else
        "Analyzer desk — recover current intake and continue."
        if role=="analyzer" else None
      ),
      "rule":"A worker handoff must be claimed within five minutes or surface as an explicit continuity warning."
    }
    atomic(OUT,state)
    print(json.dumps(state,indent=2))
    return 2 if warn else 0

if __name__=="__main__":
    raise SystemExit(main())
