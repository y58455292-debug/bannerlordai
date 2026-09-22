from pathlib import Path
import datetime
import hashlib
import json
import os
import subprocess
import sys

HERE = Path(__file__).resolve().parent
if str(HERE) not in sys.path:
    sys.path.insert(0, str(HERE))

import durable_handoff as durable

BASE = Path(r"D:\BannerlordAIResearch")
HANDOFF = BASE / r"Longitudinal\EvidenceIndex\HandoffV3"
CURRENT = HANDOFF / "current.json"
SNAPSHOT = HANDOFF / "last_resume_snapshot.json"
CONTINUITY = HANDOFF / "Continuity"
CONTINUITY_LATEST = CONTINUITY / "latest_runtime.json"
CONTINUITY_ATTENTION = CONTINUITY / "attention.json"
AGENT_ACTIVITY = CONTINUITY / "last_agent_activity.json"
REPORTS = BASE / r"Longitudinal\DerivedReports"
VALIDATIONS = BASE / r"Longitudinal\LiveValidation"
STAGING = BASE / r"workspace\_PatchStaging"
STATUS = BASE / r"Automation\TestRunner\status.txt"
DEPLOYMENT_OWNER = BASE / r"workspace\DEPLOYMENT_OWNER.json"
INSTALLED = Path(r"C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\Modules\ClanAI\bin\Win64_Shipping_Client\ClanAI.dll")
SOCIAL_CFG = BASE / r"Data\SocialMemoryCausal.cfg"
RECOVERY_CFG = BASE / r"Data\WeakBanditRecovery.cfg"

def now_iso():
    return datetime.datetime.now().astimezone().isoformat()

def sha256(path):
    if not path.exists():
        return None
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()

def read_json(path):
    if not path.exists():
        return {}
    return json.loads(path.read_text(encoding="utf-8-sig"))

def read_text(path):
    if not path.exists():
        return None
    return path.read_text(encoding="utf-8-sig", errors="replace")

def newest(root, count=5):
    if not root.exists():
        return []
    items = sorted(root.iterdir(), key=lambda p: p.stat().st_mtime, reverse=True)
    return [{
        "name": p.name,
        "path": str(p),
        "mtime": datetime.datetime.fromtimestamp(
            p.stat().st_mtime, tz=datetime.datetime.now().astimezone().tzinfo
        ).isoformat(),
        "is_dir": p.is_dir(),
    } for p in items[:count]]

def running_bannerlord():
    text = subprocess.run(
        ["tasklist", "/fo", "csv"],
        capture_output=True, text=True, errors="replace"
    ).stdout
    return "Bannerlord.exe" in text

def parse_status(text):
    data = {}
    for line in (text or "").splitlines():
        if "=" in line:
            key, value = line.split("=", 1)
            data[key.strip()] = value.strip()
    return data

def candidate_hash_matches(target_hash):
    if not target_hash or not STAGING.exists():
        return []
    matches = []
    pattern = "*/candidate/bin/Release/netstandard2.0/ClanAI.dll"
    for dll in STAGING.glob(pattern):
        try:
            if sha256(dll) == target_hash:
                matches.append(str(dll.parents[4]))
        except Exception:
            pass
    return matches

def parse_time(value):
    try:
        return datetime.datetime.fromisoformat(value)
    except Exception:
        return None

def main():
    capsule = durable.reconstruct_state()
    journal_validation = durable.validate_current()
    materialized = read_json(CURRENT)
    installed_hash = sha256(INSTALLED)
    reports = newest(REPORTS)
    validations = newest(VALIDATIONS)
    staging = newest(STAGING)
    status_text = read_text(STATUS)
    runner = parse_status(status_text)
    game_running = running_bannerlord()
    continuity_latest = read_json(CONTINUITY_LATEST)
    continuity_attention = read_json(CONTINUITY_ATTENTION)

    warnings = []
    expected_hash = (capsule.get("installed_build") or {}).get("sha256")
    if not capsule:
        warnings.append("MISSING_CAPSULE")
    if not journal_validation.get("valid", False):
        warnings.append("HANDOFF_JOURNAL_INVALID")
    if capsule and materialized and durable.sha256_data(capsule) != durable.sha256_data(materialized):
        warnings.append("MATERIALIZED_VIEW_STALE")
    if expected_hash and installed_hash != expected_hash:
        warnings.append("INSTALLED_BUILD_DIFFERS_FROM_CAPSULE")
    if game_running:
        warnings.append("BANNERLORD_IS_RUNNING")
    if continuity_attention.get("needs_attention"):
        warnings.append("CONTINUITY_WATCHDOG_ATTENTION")

    capsule_time = parse_time(capsule.get("updated_at"))
    for label, entries in (("REPORT", reports), ("VALIDATION", validations), ("STAGING", staging)):
        if capsule_time and entries:
            newest_time = parse_time(entries[0]["mtime"])
            if newest_time and newest_time > capsule_time:
                warnings.append("NEWER_" + label + "_THAN_CAPSULE")

    if game_running:
        action = "Inspect inherited TestRunner/game state before any new launch."
    elif warnings:
        action = "Reconcile newer disk/runtime evidence, then write a fresh checkpoint before new work."
    else:
        action = capsule.get("next_action", "No next_action recorded; inspect latest evidence.")

    snapshot = {
        "schema": "BannerlordAI.HandoffV3.ResumeSnapshot.v1",
        "generated_at": now_iso(),
        "capsule": capsule,
        "runtime": {
            "bannerlord_running": game_running,
            "runner_status": runner,
            "deployment_owner": read_json(DEPLOYMENT_OWNER),
            "installed_clanai_sha256": installed_hash,
            "installed_hash_candidate_matches": candidate_hash_matches(installed_hash),
        },
        "journal": journal_validation,
        "continuity": {
            "latest_runtime": continuity_latest,
            "attention": continuity_attention,
        },
        "newest": {
            "derived_reports": reports,
            "live_validations": validations,
            "patch_staging": staging,
        },
        "configs": {
            "social_memory": read_text(SOCIAL_CFG),
            "weak_recovery": read_text(RECOVERY_CFG),
        },
        "warnings": sorted(set(warnings)),
        "recommended_action": action,
    }

    HANDOFF.mkdir(parents=True, exist_ok=True)
    CONTINUITY.mkdir(parents=True, exist_ok=True)
    activity = {
        "schema": "BannerlordAI.AgentActivity.v1",
        "updated_at": snapshot["generated_at"],
        "source": "resume_state",
        "checkpoint_seq": capsule.get("checkpoint_seq"),
        "recommended_action": action,
    }
    activity_tmp = AGENT_ACTIVITY.with_suffix(".json.tmp")
    activity_tmp.write_text(
        json.dumps(activity, indent=2, ensure_ascii=False) + "\n",
        encoding="utf-8",
    )
    os.replace(activity_tmp, AGENT_ACTIVITY)

    tmp = SNAPSHOT.with_suffix(".json.tmp")
    tmp.write_text(json.dumps(snapshot, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    try:
        os.replace(tmp, SNAPSHOT)
    except PermissionError:
        # Windows can deny replace when a read-only observer still has the
        # non-canonical resume snapshot open. Preserve recovery progress by
        # overwriting from the fully-written temp file and verifying bytes.
        expected = tmp.read_bytes()
        with SNAPSHOT.open("wb") as f:
            f.write(expected)
            f.flush()
            os.fsync(f.fileno())
        if SNAPSHOT.read_bytes() != expected:
            raise RuntimeError("resume snapshot fallback verification failed")
        try:
            tmp.unlink()
        except PermissionError:
            pass
    print(json.dumps(snapshot, indent=2, ensure_ascii=False))

if __name__ == "__main__":
    main()
