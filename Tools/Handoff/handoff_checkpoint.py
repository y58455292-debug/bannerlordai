from pathlib import Path
import argparse
import datetime
import json
import os
import sys

HERE = Path(__file__).resolve().parent
if str(HERE) not in sys.path:
    sys.path.insert(0, str(HERE))

import durable_handoff as durable

CONTINUITY = durable.ROOT / "Continuity"
AGENT_ACTIVITY = CONTINUITY / "last_agent_activity.json"

def touch_activity(source, state):
    CONTINUITY.mkdir(parents=True, exist_ok=True)
    payload = {
        "schema": "BannerlordAI.AgentActivity.v1",
        "updated_at": datetime.datetime.now().astimezone().isoformat(),
        "source": source,
        "checkpoint_seq": state.get("checkpoint_seq") if state else None,
        "next_action": state.get("next_action") if state else None,
    }
    tmp = AGENT_ACTIVITY.with_suffix(".json.tmp")
    tmp.write_text(
        json.dumps(payload, indent=2, ensure_ascii=False) + "\n",
        encoding="utf-8",
    )
    os.replace(tmp, AGENT_ACTIVITY)

def load_patch(path):
    if not path:
        return {}
    return json.loads(
        Path(path).read_text(encoding="utf-8-sig")
    )

def main():
    parser = argparse.ArgumentParser()
    sub = parser.add_subparsers(dest="command", required=True)

    sub.add_parser("show")
    sub.add_parser("validate")
    sub.add_parser("reconstruct")

    cp = sub.add_parser("checkpoint")
    cp.add_argument("--patch-file")
    cp.add_argument("--event-type", required=True)
    cp.add_argument("--message", required=True)
    cp.add_argument("--action-id")
    cp.add_argument("--expected-seq", type=int)

    args = parser.parse_args()

    if args.command == "show":
        print(json.dumps(
            durable.load_json(durable.CURRENT, {}),
            indent=2,
            ensure_ascii=False,
        ))
        return 0
    if args.command == "reconstruct":
        print(json.dumps(
            durable.reconstruct_state(),
            indent=2,
            ensure_ascii=False,
        ))
        return 0

    if args.command == "validate":
        result = durable.validate_current()
        print(json.dumps(result, indent=2))
        return 0 if result["valid"] else 2

    result = durable.checkpoint(
        load_patch(args.patch_file),
        args.event_type,
        args.message,
        action_id=args.action_id,
        expected_seq=args.expected_seq,
    )
    state = result["state"]
    event = result["event"]
    touch_activity("handoff_checkpoint", state)
    print(json.dumps({
        "checkpoint_seq": state.get("checkpoint_seq"),
        "updated_at": state.get("updated_at"),
        "event_id": event.get("event_id"),
        "event_sha256": event.get("event_sha256"),
        "action_id": event.get("action_id"),
        "idempotent_replay": result.get("idempotent_replay"),
        "next_action": state.get("next_action"),
    }, indent=2))
    return 0

if __name__ == "__main__":
    raise SystemExit(main())
