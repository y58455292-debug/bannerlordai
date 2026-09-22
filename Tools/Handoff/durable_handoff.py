from pathlib import Path
import copy
import datetime
import hashlib
import json
import os
import time
import uuid
import msvcrt
from contextlib import contextmanager

ROOT = Path(r"D:\BannerlordAIResearch\Longitudinal\EvidenceIndex\HandoffV3")
CURRENT = ROOT / "current.json"
PREVIOUS = ROOT / "previous.json"
EVENTS = ROOT / "events.jsonl"
SNAPSHOTS = ROOT / "snapshots"
WRITER_LOCK = ROOT / "writer.lock"

EVENT_SCHEMA = "BannerlordAI.HandoffV4.Event.v1"
STATE_SCHEMA = "BannerlordAI.HandoffV4.Current.v1"

def now_iso():
    return datetime.datetime.now().astimezone().isoformat()

def load_json(path, default=None):
    if not path.exists():
        return {} if default is None else default
    return json.loads(path.read_text(encoding="utf-8-sig"))

def canonical_json_bytes(data):
    return json.dumps(
        data,
        ensure_ascii=False,
        sort_keys=True,
        separators=(",", ":"),
    ).encode("utf-8")

def sha256_data(data):
    return hashlib.sha256(canonical_json_bytes(data)).hexdigest().upper()

def deep_merge(base, patch):
    for key, value in patch.items():
        if isinstance(value, dict) and isinstance(base.get(key), dict):
            deep_merge(base[key], value)
        else:
            base[key] = value
    return base

def atomic_json(path, data):
    path.parent.mkdir(parents=True, exist_ok=True)
    tmp = path.with_suffix(path.suffix + ".tmp")
    payload = json.dumps(data, indent=2, ensure_ascii=False) + "\n"
    with tmp.open("w", encoding="utf-8") as f:
        f.write(payload)
        f.flush()
        os.fsync(f.fileno())

    last_error = None
    for attempt in range(20):
        try:
            os.replace(tmp, path)
            return
        except PermissionError as ex:
            last_error = ex
            time.sleep(0.025 * (attempt + 1))

    # current/previous are materialized cache views. The append-only journal
    # remains authoritative, so a direct fsync rewrite is safe when Windows
    # sharing locks repeatedly block rename-over-existing.
    if path == CURRENT or path == PREVIOUS:
        with path.open("w", encoding="utf-8") as f:
            f.write(payload)
            f.flush()
            os.fsync(f.fileno())
        try:
            tmp.unlink()
        except FileNotFoundError:
            pass
        return

    if last_error is not None:
        raise last_error
    os.replace(tmp, path)
@contextmanager
def writer_lock():
    WRITER_LOCK.parent.mkdir(parents=True, exist_ok=True)
    f = WRITER_LOCK.open("a+b")
    try:
        f.seek(0, os.SEEK_END)
        if f.tell() == 0:
            f.write(b"0")
            f.flush()
            os.fsync(f.fileno())
        f.seek(0)
        msvcrt.locking(f.fileno(), msvcrt.LK_LOCK, 1)
        try:
            yield
        finally:
            f.seek(0)
            msvcrt.locking(f.fileno(), msvcrt.LK_UNLCK, 1)
    finally:
        f.close()

def append_jsonl_fsync(path, item):
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("a", encoding="utf-8") as f:
        line = json.dumps(item, ensure_ascii=False, sort_keys=True)
        f.write(line + "\n")
        f.flush()
        os.fsync(f.fileno())

def iter_events():
    if not EVENTS.exists():
        return []
    out = []
    with EVENTS.open("r", encoding="utf-8-sig", errors="replace") as f:
        for lineno, line in enumerate(f, 1):
            line = line.strip()
            if not line:
                continue
            try:
                item = json.loads(line)
            except Exception as ex:
                raise RuntimeError(
                    f"events.jsonl parse failure at line {lineno}: {ex}"
                )
            out.append(item)
    return out

def event_hash(event):
    body = dict(event)
    body.pop("event_sha256", None)
    # result_state_sha256 depends on the journal-head hash, so keep it
    # outside the event's own hash to avoid a circular checksum.
    body.pop("result_state_sha256", None)
    return sha256_data(body)

def latest_v4_event(events=None):
    events = iter_events() if events is None else events
    v4 = [x for x in events if x.get("schema") == EVENT_SCHEMA]
    return v4[-1] if v4 else None

def verify_event_chain(events=None):
    events = iter_events() if events is None else events
    v4 = [x for x in events if x.get("schema") == EVENT_SCHEMA]
    errors = []
    prev_hash = None
    prev_seq = None
    for item in v4:
        actual = event_hash(item)
        if actual != item.get("event_sha256"):
            errors.append(
                f"event hash mismatch seq={item.get('checkpoint_seq')}"
            )
        if item.get("prev_event_sha256") != prev_hash:
            errors.append(
                f"event chain mismatch seq={item.get('checkpoint_seq')}"
            )
        if prev_seq is not None and item.get("checkpoint_seq") != prev_seq + 1:
            errors.append(
                f"event seq gap {prev_seq}->{item.get('checkpoint_seq')}"
            )
        prev_hash = item.get("event_sha256")
        prev_seq = item.get("checkpoint_seq")
    return errors

def snapshot_path(seq):
    return SNAPSHOTS / f"{int(seq):08d}.json"

def write_snapshot(state):
    seq = int(state.get("checkpoint_seq", 0))
    path = snapshot_path(seq)
    if not path.exists():
        atomic_json(path, state)
    return path

def latest_snapshot():
    if not SNAPSHOTS.exists():
        return None
    candidates = sorted(SNAPSHOTS.glob("*.json"))
    return candidates[-1] if candidates else None
def apply_event(state, event):
    if event.get("schema") != EVENT_SCHEMA:
        return state
    expected_seq = int(state.get("checkpoint_seq", 0)) + 1
    if int(event["checkpoint_seq"]) != expected_seq:
        raise RuntimeError(
            f"cannot replay seq={event['checkpoint_seq']} after "
            f"state seq={state.get('checkpoint_seq')}"
        )
    before_hash = sha256_data(state)
    if event.get("prev_state_sha256") != before_hash:
        raise RuntimeError(
            f"prev state hash mismatch at seq={event['checkpoint_seq']}"
        )
    updated = deep_merge(copy.deepcopy(state), event.get("patch", {}))
    updated["schema"] = STATE_SCHEMA
    updated["updated_at"] = event["timestamp"]
    updated["checkpoint_seq"] = int(event["checkpoint_seq"])
    updated["authority"] = "EVENT_LOG_PLUS_RUNTIME"
    updated["handoff_mode"] = "DURABLE_REPLAY"
    updated["journal_head_event_id"] = event["event_id"]
    updated["journal_head_sha256"] = event["event_sha256"]
    if sha256_data(updated) != event.get("result_state_sha256"):
        raise RuntimeError(
            f"result state hash mismatch at seq={event['checkpoint_seq']}"
        )
    return updated

def reconstruct_state():
    snap = latest_snapshot()
    if snap is None:
        state = load_json(CURRENT, {})
        if not state:
            return {}
        write_snapshot(state)
        return state

    state = load_json(snap, {})
    events = iter_events()
    for event in events:
        if event.get("schema") != EVENT_SCHEMA:
            continue
        if int(event.get("checkpoint_seq", 0)) <= int(
            state.get("checkpoint_seq", 0)
        ):
            continue
        state = apply_event(state, event)
    return state

def find_action(events, action_id):
    if not action_id:
        return None
    for item in reversed(events):
        if (
            item.get("schema") == EVENT_SCHEMA
            and item.get("action_id") == action_id
        ):
            return item
    return None
def _checkpoint_unlocked(
    patch,
    event_type,
    message,
    action_id=None,
    snapshot_every=10,
):
    ROOT.mkdir(parents=True, exist_ok=True)
    SNAPSHOTS.mkdir(parents=True, exist_ok=True)

    events = iter_events()
    chain_errors = verify_event_chain(events)
    if chain_errors:
        raise RuntimeError("; ".join(chain_errors))

    current = reconstruct_state()
    if not current:
        current = load_json(CURRENT, {})

    if action_id:
        prior = find_action(events, action_id)
        if prior is not None:
            return {
                "state": reconstruct_state(),
                "event": prior,
                "idempotent_replay": True,
            }

    if not latest_snapshot():
        write_snapshot(current)

    seq = int(current.get("checkpoint_seq", 0)) + 1
    timestamp = now_iso()
    prev_state_sha = sha256_data(current)

    updated = deep_merge(copy.deepcopy(current), patch)
    updated["schema"] = STATE_SCHEMA
    updated["updated_at"] = timestamp
    updated["checkpoint_seq"] = seq
    updated["authority"] = "EVENT_LOG_PLUS_RUNTIME"
    updated["handoff_mode"] = "DURABLE_REPLAY"

    prior_event = latest_v4_event(events)
    prev_event_sha = (
        prior_event.get("event_sha256") if prior_event is not None else None
    )

    event = {
        "schema": EVENT_SCHEMA,
        "event_id": str(uuid.uuid4()),
        "timestamp": timestamp,
        "checkpoint_seq": seq,
        "prev_checkpoint_seq": int(current.get("checkpoint_seq", 0)),
        "event_type": event_type,
        "message": message,
        "action_id": action_id,
        "patch": patch,
        "prev_state_sha256": prev_state_sha,
        "prev_event_sha256": prev_event_sha,
    }

    event["event_sha256"] = event_hash(event)
    updated["journal_head_event_id"] = event["event_id"]
    updated["journal_head_sha256"] = event["event_sha256"]
    event["result_state_sha256"] = sha256_data(updated)

    append_jsonl_fsync(EVENTS, event)

    if current:
        atomic_json(PREVIOUS, current)
    atomic_json(CURRENT, updated)

    if seq % int(snapshot_every) == 0:
        write_snapshot(updated)

    return {
        "state": updated,
        "event": event,
        "idempotent_replay": False,
    }

def checkpoint(
    patch,
    event_type,
    message,
    action_id=None,
    snapshot_every=10,
    expected_seq=None,
):
    with writer_lock():
        current = reconstruct_state()
        actual_seq = int(current.get("checkpoint_seq", 0))
        if expected_seq is not None and actual_seq != int(expected_seq):
            raise RuntimeError(
                f"stale writer: expected seq={expected_seq} "
                f"but durable state is seq={actual_seq}; "
                "re-run recovery before writing"
            )
        return _checkpoint_unlocked(
            patch,
            event_type,
            message,
            action_id=action_id,
            snapshot_every=snapshot_every,
        )

def validate_current():
    events = iter_events()
    errors = verify_event_chain(events)
    replayed = reconstruct_state()
    current = load_json(CURRENT, {})

    if replayed and current:
        if sha256_data(replayed) != sha256_data(current):
            errors.append("current.json differs from replayed durable state")

    return {
        "valid": not errors,
        "errors": errors,
        "checkpoint_seq": current.get("checkpoint_seq"),
        "current_sha256": sha256_data(current) if current else None,
        "replayed_sha256": sha256_data(replayed) if replayed else None,
        "snapshot": str(latest_snapshot()) if latest_snapshot() else None,
        "v4_event_count": len([
            x for x in events if x.get("schema") == EVENT_SCHEMA
        ]),
    }

def ensure_genesis_snapshot():
    ROOT.mkdir(parents=True, exist_ok=True)
    SNAPSHOTS.mkdir(parents=True, exist_ok=True)
    current = load_json(CURRENT, {})
    if not current:
        raise RuntimeError("current.json missing; cannot bootstrap")
    path = snapshot_path(current.get("checkpoint_seq", 0))
    if not path.exists():
        atomic_json(path, current)
    return path
