from pathlib import Path
import datetime
import hashlib
import json
import os
import subprocess
import sys
import uuid

ROOT = Path(r"D:\BannerlordAIResearch")
LOCK = ROOT / r"workspace\DEPLOYMENT_OWNER.json"
ARCHIVE = ROOT / r"Longitudinal\EvidenceIndex\HandoffV3\deployment_leases"
INSTALLED = Path(
    r"C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord"
    r"\Modules\ClanAI\bin\Win64_Shipping_Client\ClanAI.dll"
)
DEFAULT_LEASE_SECONDS = 900

def now_dt():
    return datetime.datetime.now().astimezone()

def now_iso():
    return now_dt().isoformat()

def parse_iso(value):
    if not value:
        return None
    try:
        dt = datetime.datetime.fromisoformat(value)
        if dt.tzinfo is None:
            dt = dt.replace(tzinfo=now_dt().tzinfo)
        return dt.astimezone(now_dt().tzinfo)
    except Exception:
        return None

def sha256(path):
    if not path.exists():
        return None
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()

def atomic_json(path, data):
    path.parent.mkdir(parents=True, exist_ok=True)
    tmp = path.with_suffix(path.suffix + ".tmp")
    with tmp.open("w", encoding="utf-8") as f:
        f.write(json.dumps(data, indent=2, ensure_ascii=False) + "\n")
        f.flush()
        os.fsync(f.fileno())
    os.replace(tmp, path)

def read_lock():
    if not LOCK.exists():
        return None
    try:
        return json.loads(LOCK.read_text(encoding="utf-8-sig"))
    except Exception as ex:
        return {
            "status": "INVALID_LOCK_FILE",
            "path": str(LOCK),
            "error": type(ex).__name__ + ": " + str(ex),
        }

def lease_state(data):
    if not isinstance(data, dict):
        return {
            "exists": False,
            "expired": False,
            "remaining_seconds": None,
        }

    duration = int(
        data.get("lease_duration_seconds")
        or DEFAULT_LEASE_SECONDS
    )
    renew = (
        parse_iso(data.get("renewed_local"))
        or parse_iso(data.get("acquired_local"))
    )
    if renew is None:
        return {
            "exists": True,
            "expired": True,
            "remaining_seconds": 0,
            "lease_duration_seconds": duration,
            "reason": "missing_or_invalid_renew_time",
        }

    expires = renew + datetime.timedelta(seconds=duration)
    remaining = (expires - now_dt()).total_seconds()
    return {
        "exists": True,
        "expired": remaining <= 0,
        "remaining_seconds": max(0, int(remaining)),
        "lease_duration_seconds": duration,
        "renewed_local": renew.isoformat(),
        "expires_local": expires.isoformat(),
    }

def game_process_rows():
    text = subprocess.run(
        ["tasklist", "/fo", "csv"],
        capture_output=True,
        text=True,
        errors="replace",
    ).stdout.splitlines()
    return [
        row for row in text
        if "Bannerlord.exe" in row or "TaleWorlds" in row
    ]

def archive_lock(data, reason):
    ARCHIVE.mkdir(parents=True, exist_ok=True)
    stamp = now_dt().strftime("%Y%m%d_%H%M%S_%f")
    owner = str((data or {}).get("owner") or "unknown").replace(" ", "_")
    path = ARCHIVE / f"{stamp}_{owner}_{reason}.json"
    payload = {
        "archived_at": now_iso(),
        "archive_reason": reason,
        "lock": data,
    }
    atomic_json(path, payload)
    return str(path)

def new_payload(owner, milestone, expected_hash, operation_id, lease_seconds, transition_count=0):
    t = now_iso()
    return {
        "schema": "BannerlordAI.DeploymentLease.v2",
        "owner": owner,
        "operation_id": operation_id or str(uuid.uuid4()),
        "milestone": milestone,
        "expected_installed_clanai_sha256": expected_hash.upper(),
        "acquired_local": t,
        "renewed_local": t,
        "lease_duration_seconds": int(lease_seconds),
        "lease_revision": 1,
        "lease_transitions": int(transition_count),
        "phase": "ACQUIRED",
        "rule": (
            "Research/build may be parallel. Live ClanAI deployment/runtime is single-owner. "
            "Owner must renew before expiry. Takeover requires expiry plus no live Bannerlord/TaleWorlds process."
        ),
    }

def status():
    data = read_lock()
    print(json.dumps({
        "lock": data,
        "lease": lease_state(data),
        "installed_clanai_sha256": sha256(INSTALLED),
        "game_processes": game_process_rows(),
    }, indent=2))
    return 0

def acquire(owner, milestone, expected_hash, operation_id=None, lease_seconds=DEFAULT_LEASE_SECONDS):
    existing = read_lock()
    if existing is not None:
        print(json.dumps({
            "acquired": False,
            "reason": "lease_exists",
            "lock": existing,
            "lease": lease_state(existing),
        }, indent=2))
        return 2

    payload = new_payload(
        owner,
        milestone,
        expected_hash,
        operation_id,
        lease_seconds,
    )
    try:
        fd = os.open(str(LOCK), os.O_WRONLY | os.O_CREAT | os.O_EXCL)
    except FileExistsError:
        print(json.dumps({
            "acquired": False,
            "reason": "lease_raced",
            "lock": read_lock(),
        }, indent=2))
        return 2

    with os.fdopen(fd, "w", encoding="utf-8") as f:
        f.write(json.dumps(payload, indent=2) + "\n")
        f.flush()
        os.fsync(f.fileno())

    print(json.dumps({"acquired": True, "lock": payload}, indent=2))
    return 0

def owner_match(data, owner, operation_id=None):
    if not isinstance(data, dict):
        return False
    if data.get("owner") != owner:
        return False
    if operation_id and data.get("operation_id") != operation_id:
        return False
    return True

def renew(owner, operation_id=None):
    data = read_lock()
    if not owner_match(data, owner, operation_id):
        print(json.dumps({
            "renewed": False,
            "reason": "owner_or_operation_mismatch",
            "lock": data,
        }, indent=2))
        return 2

    state = lease_state(data)
    if state.get("expired"):
        print(json.dumps({
            "renewed": False,
            "reason": "lease_expired_use_takeover_or_reacquire",
            "lock": data,
            "lease": state,
        }, indent=2))
        return 2

    data["renewed_local"] = now_iso()
    data["lease_revision"] = int(data.get("lease_revision", 0)) + 1
    atomic_json(LOCK, data)
    print(json.dumps({
        "renewed": True,
        "lock": data,
        "lease": lease_state(data),
    }, indent=2))
    return 0

def set_phase(owner, phase, operation_id=None):
    data = read_lock()
    if not owner_match(data, owner, operation_id):
        print(json.dumps({
            "updated": False,
            "reason": "owner_or_operation_mismatch",
            "lock": data,
        }, indent=2))
        return 2

    state = lease_state(data)
    if state.get("expired"):
        print(json.dumps({
            "updated": False,
            "reason": "lease_expired",
            "lock": data,
            "lease": state,
        }, indent=2))
        return 2

    data["phase"] = phase
    data["renewed_local"] = now_iso()
    data["lease_revision"] = int(data.get("lease_revision", 0)) + 1
    atomic_json(LOCK, data)
    print(json.dumps({
        "updated": True,
        "lock": data,
        "lease": lease_state(data),
    }, indent=2))
    return 0

def release(owner, operation_id=None):
    data = read_lock()
    if data is None:
        print(json.dumps({"released": False, "reason": "no_lease"}, indent=2))
        return 1
    if not owner_match(data, owner, operation_id):
        print(json.dumps({
            "released": False,
            "reason": "owner_or_operation_mismatch",
            "lock": data,
        }, indent=2))
        return 2

    data["phase"] = "RELEASED"
    data["released_local"] = now_iso()
    archive = archive_lock(data, "released")
    LOCK.unlink()
    print(json.dumps({
        "released": True,
        "owner": owner,
        "operation_id": data.get("operation_id"),
        "archive": archive,
    }, indent=2))
    return 0

def takeover(owner, milestone, expected_hash, operation_id=None, lease_seconds=DEFAULT_LEASE_SECONDS):
    old = read_lock()
    if old is None:
        return acquire(
            owner, milestone, expected_hash, operation_id, lease_seconds
        )

    state = lease_state(old)
    if not state.get("expired"):
        print(json.dumps({
            "acquired": False,
            "reason": "existing_lease_not_expired",
            "lock": old,
            "lease": state,
        }, indent=2))
        return 2

    running = game_process_rows()
    if running:
        print(json.dumps({
            "acquired": False,
            "reason": "expired_but_game_process_active",
            "lock": old,
            "lease": state,
            "game_processes": running,
        }, indent=2))
        return 2

    archive = archive_lock(old, "expired_takeover")
    LOCK.unlink()

    transitions = int(old.get("lease_transitions", 0)) + 1
    payload = new_payload(
        owner,
        milestone,
        expected_hash,
        operation_id,
        lease_seconds,
        transition_count=transitions,
    )
    atomic_json(LOCK, payload)
    print(json.dumps({
        "acquired": True,
        "takeover": True,
        "prior_archive": archive,
        "lock": payload,
    }, indent=2))
    return 0

def usage():
    return (
        "usage:\n"
        "  deployment_guard.py status\n"
        "  deployment_guard.py acquire OWNER MILESTONE EXPECTED_SHA [OPERATION_ID] [LEASE_SECONDS]\n"
        "  deployment_guard.py renew OWNER [OPERATION_ID]\n"
        "  deployment_guard.py phase OWNER PHASE [OPERATION_ID]\n"
        "  deployment_guard.py release OWNER [OPERATION_ID]\n"
        "  deployment_guard.py takeover OWNER MILESTONE EXPECTED_SHA [OPERATION_ID] [LEASE_SECONDS]"
    )

if __name__ == "__main__":
    if len(sys.argv) < 2:
        raise SystemExit(usage())

    cmd = sys.argv[1].lower()
    if cmd == "status":
        raise SystemExit(status())

    if cmd in ("acquire", "takeover") and 5 <= len(sys.argv) <= 7:
        owner = sys.argv[2]
        milestone = sys.argv[3]
        expected = sys.argv[4]
        operation = sys.argv[5] if len(sys.argv) >= 6 else None
        seconds = int(sys.argv[6]) if len(sys.argv) >= 7 else DEFAULT_LEASE_SECONDS
        if cmd == "acquire":
            raise SystemExit(acquire(owner, milestone, expected, operation, seconds))
        raise SystemExit(takeover(owner, milestone, expected, operation, seconds))

    if cmd == "renew" and 3 <= len(sys.argv) <= 4:
        raise SystemExit(renew(sys.argv[2], sys.argv[3] if len(sys.argv) == 4 else None))

    if cmd == "phase" and 4 <= len(sys.argv) <= 5:
        raise SystemExit(set_phase(
            sys.argv[2],
            sys.argv[3],
            sys.argv[4] if len(sys.argv) == 5 else None,
        ))

    if cmd == "release" and 3 <= len(sys.argv) <= 4:
        raise SystemExit(release(
            sys.argv[2],
            sys.argv[3] if len(sys.argv) == 4 else None,
        ))

    raise SystemExit(usage())
