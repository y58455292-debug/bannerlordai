import hashlib
import json
import shutil
import sys
from pathlib import Path

ROOT = Path(r"D:\BannerlordAIResearch")
SAVE = Path(
    r"C:\Users\csala\OneDrive\Documents\Mount and Blade II Bannerlord\Game Saves\ClanAI v020D DIAGNOSTIC TEST.sav"
)
SAVE_SHA = "A3DAA0C37C0F8F6966DFB6DB2A57002BED70978A6C253F29DF395F171F9043AC"


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()


def latest_session():
    sessions = sorted(
        (ROOT / "Telemetry/ClanAI/sessions").glob(
            "*_v020E_review.log"
        ),
        key=lambda p: p.stat().st_mtime,
    )
    if not sessions:
        raise SystemExit("No v0.20E session log found")
    return sessions[-1]


def inspector_segment():
    path = (
        ROOT
        / "Telemetry/BannerlordInspector/logs/inspector.log"
    )
    lines = path.read_text(
        encoding="utf-8-sig",
        errors="replace",
    ).splitlines()
    starts = [
        i for i, line in enumerate(lines)
        if "=== Bannerlord Inspector loading ===" in line
    ]
    if not starts:
        raise SystemExit("No Inspector load marker found")
    return path, lines[starts[-1]:]


def main():
    if len(sys.argv) != 2:
        raise SystemExit(
            "usage: freeze_recovery_ab_arm.py <arm_dir>"
        )

    arm = Path(sys.argv[1])
    preflight = json.loads(
        (arm / "preflight.json").read_text(
            encoding="utf-8-sig"
        )
    )

    session = latest_session()
    inspector_path, segment = inspector_segment()

    shutil.copy2(
        session,
        arm / "clanai_session.log",
    )

    (arm / "inspector_segment.log").write_text(
        "\n".join(segment) + "\n",
        encoding="utf-8",
    )
    diagnostic = session.with_suffix(
        ".diagnostics.txt"
    )
    if diagnostic.exists():
        shutil.copy2(
            diagnostic,
            arm / diagnostic.name,
        )

    finalhealth = session.with_suffix(
        ".diagnostics.finalhealth.txt"
    )
    if finalhealth.exists():
        shutil.copy2(
            finalhealth,
            arm / finalhealth.name,
        )

    clan_lines = (
        arm / "clanai_session.log"
    ).read_text(
        encoding="utf-8-sig",
        errors="replace",
    ).splitlines()

    mode_lines = [
        line for line in clan_lines
        if "GLOBAL_AI_RECOVERY_GATE_MODE " in line
    ]
    control = sum(
        "GLOBAL_AI_WEAK_BANDIT_ENGAGE_CONTROL_PASS " in line
        for line in clan_lines
    )
    suppressed = sum(
        "GLOBAL_AI_WEAK_BANDIT_ENGAGE_SUPPRESSED " in line
        for line in clan_lines
    )

    outcome = sum(
        "OUTCOME_V1 " in line
        for line in segment
    )
    lifecycle = sum(
        "RECOVERY_LIFECYCLE_V1 " in line
        for line in segment
    )
    observer_warnings = [
        line for line in segment
        if (
            "OUTCOME_V1_ERROR" in line
            or "RECOVERY_LIFECYCLE_V1_ERROR" in line
            or "REGISTER_FAILED" in line
        )
    ]

    closeout = {
        "schema": "BannerlordAI.RecoveryABArmCloseout.v1",
        "arm": preflight.get("arm"),
        "expectedMode": preflight.get("mode"),
        "modeRecords": mode_lines,
        "distinctControlPairLogs": control,
        "distinctSuppressionPairLogs": suppressed,
        "outcomeRecords": outcome,
        "lifecycleRecords": lifecycle,
        "observerWarnings": observer_warnings,
        "saveSha256": sha(SAVE),
        "saveUnchanged": sha(SAVE) == SAVE_SHA,
        "sourceClanSession": str(session),
        "sourceInspectorLog": str(inspector_path),
    }

    (arm / "closeout.json").write_text(
        json.dumps(
            closeout,
            indent=2,
            ensure_ascii=False,
        ),
        encoding="utf-8",
    )

    print(json.dumps(
        closeout,
        indent=2,
        ensure_ascii=False,
    ))

    if not closeout["saveUnchanged"]:
        raise SystemExit("Protected save hash changed")
    if observer_warnings:
        raise SystemExit("Observer warning present")


if __name__ == "__main__":
    main()
