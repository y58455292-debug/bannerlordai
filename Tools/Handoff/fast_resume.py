from pathlib import Path
import datetime, hashlib, json, os, subprocess, sys, time

BASE = Path(r"D:\BannerlordAIResearch")
HANDOFF = BASE / r"Longitudinal\EvidenceIndex\HandoffV3"
CURRENT = HANDOFF / "current.json"
EVENTS = HANDOFF / "events.jsonl"
FAST = HANDOFF / r"Continuity\fast_boot.json"
LATEST = HANDOFF / r"Continuity\latest_runtime.json"
ATTENTION = HANDOFF / r"Continuity\attention.json"
ACTIVE_RUN = BASE / r"Automation\Control\active_run.json"
HEARTBEAT = BASE / r"Automation\Control\low_overhead_heartbeat.json"
INTENT = BASE / r"Automation\Control\desired_intent.json"
STATUS = BASE / r"Automation\TestRunner\status.txt"
OWNER = BASE / r"workspace\DEPLOYMENT_OWNER.json"
ARCH = HANDOFF / "ARCHITECTURE_LEARNING.md"
INSTALLED = Path(r"C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\Modules\ClanAI\bin\Win64_Shipping_Client\ClanAI.dll")

def read_json(p):
    try:
        return json.loads(p.read_text(encoding="utf-8-sig")) if p.exists() else {}
    except Exception as e:
        return {"_read_error": type(e).__name__ + ": " + str(e)}

def read_status():
    d = {}
    try:
        for line in STATUS.read_text(encoding="utf-8-sig", errors="replace").splitlines():
            if "=" in line:
                k, v = line.split("=", 1)
                d[k.strip()] = v.strip()
    except Exception:
        pass
    return d

def sha(p):
    try:
        return hashlib.sha256(p.read_bytes()).hexdigest().upper()
    except Exception:
        return None

def last_event():
    if not EVENTS.exists() or EVENTS.stat().st_size == 0:
        return {}
    with EVENTS.open("rb") as f:
        size = f.seek(0, 2)
        take = min(size, 131072)
        f.seek(size - take)
        lines = f.read(take).decode("utf-8", errors="replace").splitlines()
    for line in reversed(lines):
        if line.strip():
            try:
                return json.loads(line)
            except Exception:
                return {"_read_error": "last event JSON parse failed"}
    return {}

def process_rows():
    cp = subprocess.run(
        ["tasklist", "/fi", "IMAGENAME eq Bannerlord.exe", "/fo", "csv", "/nh"],
        capture_output=True, text=True, errors="replace"
    )
    return [x for x in cp.stdout.splitlines() if "Bannerlord.exe" in x]

def atomic_json(p, obj):
    p.parent.mkdir(parents=True, exist_ok=True)
    tmp = p.with_suffix(p.suffix + ".tmp")
    tmp.write_text(json.dumps(obj, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    os.replace(tmp, p)

def main():
    t0 = time.perf_counter()
    cur = read_json(CURRENT)
    ev = last_event()
    latest = read_json(LATEST)
    attention = read_json(ATTENTION)
    owner = read_json(OWNER)
    active = read_json(ACTIVE_RUN)
    heartbeat = read_json(HEARTBEAT)
    intent = read_json(INTENT)
    runner = read_status()
    prows = process_rows()
    installed = sha(INSTALLED)
    expected = ((cur.get("installed_build") or {}).get("sha256"))

    warnings = []
    cur_seq = cur.get("checkpoint_seq")
    ev_seq = ev.get("checkpoint_seq")
    ev_hash = ev.get("event_sha256")
    cur_head = cur.get("journal_head_sha256")

    if not cur:
        warnings.append("MISSING_CURRENT")
    if ev_seq != cur_seq:
        warnings.append("JOURNAL_AHEAD_OR_SEQ_MISMATCH")
    if ev_hash and cur_head and ev_hash != cur_head:
        warnings.append("JOURNAL_HEAD_HASH_MISMATCH")
    if expected and installed != expected:
        warnings.append("INSTALLED_HASH_MISMATCH")
    for w in (attention.get("warnings") or []):
        if w != "DURABLE_CHECKPOINT_OLDER_THAN_15_MIN":
            warnings.append("CONTINUITY_" + w)

    game_running = bool(prows)
    owner_present = bool(owner and not owner.get("_read_error"))
    if game_running and not owner_present:
        warnings.append("GAME_RUNNING_WITHOUT_OWNER_LEASE")
    if active.get("state") == "ACTIVE" and not game_running:
        warnings.append("ACTIVE_CONTROL_RUN_WITHOUT_GAME")
    if game_running and runner.get("campaignReady") not in ("True", "true", True):
        warnings.append("GAME_RUNNING_RUNNER_NOT_READY")

    forced = os.environ.get("BANNERLORDAI_FAST_RESUME_FORCE_WARNING")
    if forced:
        warnings.append("SELFTEST_" + forced)

    run_state = cur.get("run_state") or {}
    milestone = cur.get("last_accepted_milestone") or {}
    mode = "FAST_OK" if not warnings else "ESCALATE_DEEP"

    full = {
        "schema": "BannerlordAI.FastResume.v2",
        "generated_at": datetime.datetime.now().astimezone().isoformat(),
        "mode": mode,
        "deep_recovery_required": bool(warnings),
        "checkpoint": {
            "seq": cur_seq,
            "updated_at": cur.get("updated_at"),
            "journal_tail_seq": ev_seq,
            "journal_head_hash_match": bool(ev_hash and cur_head and ev_hash == cur_head)
        },
        "runtime": {
            "bannerlord_running": game_running,
            "process_rows": prows,
            "runner": {k: runner.get(k) for k in (
                "campaignReady", "playerHero", "campaignHours", "timeControl", "blockers",
                "lastCommand", "lastResult", "decisionType", "decisionTitle",
                "partySettlement", "partyPosition"
            )},
            "active_control": {
                "run_id": active.get("run_id"), "issuer": active.get("issuer"),
                "sequence": active.get("sequence"), "state": active.get("state")
            },
            "lease": {
                "owner": owner.get("owner"), "operation_id": owner.get("operation_id"),
                "phase": owner.get("phase")
            },
            "watcher": {
                "kind": heartbeat.get("kind"), "campaignHours": heartbeat.get("campaignHours"),
                "timeControl": heartbeat.get("timeControl"), "blockers": heartbeat.get("blockers"),
                "decision": heartbeat.get("decision")
            },
            "desired_intent": {
                "actor": intent.get("actor"), "kind": intent.get("kind"),
                "target": intent.get("target"), "sticky": intent.get("sticky")
            }
        },
        "integrity": {
            "installed_clanai_sha256": installed,
            "expected_clanai_sha256": expected,
            "installed_matches_expected": (installed == expected) if installed and expected else None,
            "architecture_learning_sha256": sha(ARCH),
            "continuity_observed_at": latest.get("observed_at")
        },
        "resume": {
            "accepted_milestone": {
                "version": milestone.get("version"), "name": milestone.get("name"),
                "status": milestone.get("status")
            },
            "active_question": cur.get("active_question"),
            "operation_phase": run_state.get("operation_phase"),
            "automation": run_state.get("automation"),
            "next_action": cur.get("next_action"),
            "blockers": cur.get("blockers") or [],
            "decisive_evidence": (cur.get("decisive_evidence") or [])[:4],
            "do_not_repeat_count": len(cur.get("do_not_repeat") or [])
        },
        "warnings": sorted(set(warnings)),
        "deep_recovery_command": r"python -X utf8 D:\BannerlordAIResearch\Tools\Handoff\resume_state.py"
    }
    full["elapsed_ms"] = round((time.perf_counter() - t0) * 1000, 3)
    atomic_json(FAST, full)

    compact = {
        "mode": mode,
        "ms": full["elapsed_ms"],
        "seq": cur_seq,
        "game": game_running,
        "hero": runner.get("playerHero"),
        "time": runner.get("timeControl"),
        "owner": owner.get("owner"),
        "run": active.get("state"),
        "intent": (intent.get("kind") + ":" + str(intent.get("target"))) if intent.get("kind") else None,
        "warnings": full["warnings"],
        "next": cur.get("next_action") if mode != "FAST_OK" else None,
        "detail": str(FAST)
    }

    if "--full" in sys.argv:
        print(json.dumps(full, indent=2, ensure_ascii=False))
    else:
        print(json.dumps(compact, separators=(",", ":"), ensure_ascii=False))

if __name__ == "__main__":
    main()
