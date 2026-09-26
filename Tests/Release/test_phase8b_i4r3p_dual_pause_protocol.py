#!/usr/bin/env python3
from __future__ import annotations

import ast
import subprocess
import sys
import tempfile
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
HELPER = ROOT / "Automation" / "ReleaseSmoke" / "measure_daylong.py"
PROTOCOL = ROOT / "Reports" / "Release" / "PHASE8B_I4R3P_DUAL_PAUSE_PROTOCOL.md"


def main() -> int:
    helper_source = HELPER.read_text(encoding="utf-8")
    tree = ast.parse(helper_source)
    imported = {
        alias.name.split(".")[0]
        for node in ast.walk(tree)
        if isinstance(node, ast.Import)
        for alias in node.names
    }
    imported.update(
        node.module.split(".")[0]
        for node in ast.walk(tree)
        if isinstance(node, ast.ImportFrom) and node.module
    )
    forbidden = {"socket", "urllib", "http", "requests", "subprocess", "os", "shutil"}
    assert not imported.intersection(forbidden)
    assert "write_text" not in helper_source and "write_bytes" not in helper_source
    assert "src/ClanAI/package/ClanAI" not in HELPER.as_posix()
    print("PASS Phase 8B-I4R3P helper repository-only/read-only/no-network invariant")

    with tempfile.TemporaryDirectory() as temporary:
        temp = Path(temporary)
        baseline = temp / "baseline.json"
        current = temp / "current.txt"
        baseline.write_text('{"List":{"DayLong":"1039.761"}}', encoding="utf-8")
        current.write_text("DayLong=1039.770\n", encoding="utf-8")
        before = (baseline.read_bytes(), current.read_bytes())
        result = subprocess.run(
            [
                sys.executable,
                str(HELPER),
                "--baseline", str(baseline),
                "--current", str(current),
                "--target-min-hours", "20",
                "--target-max-hours", "28",
                "--hard-cutoff-hours", "30",
            ],
            check=True,
            capture_output=True,
            text=True,
        )
        output = result.stdout
        assert "elapsed_hours=0.216" in output
        assert "CONTROL_REQUIREMENTS_BEFORE_ADVANCE=" in output
        assert "CHECK_TIME_PAUSED_CONFIRMED=[ ]" in output
        assert "CHECK_MENU_PAUSED_CONFIRMED=[ ]" in output
        assert "CHECK_DUAL_PAUSED_CONFIRMED=[ ]" in output
        assert "CHECK_LAST_DAYLONG_KNOWN=[ ]" in output
        assert "CHECK_CUTOFF_MARGIN_POSITIVE=[ ]" in output
        assert "decision=ADVANCE" in output
        assert before == (baseline.read_bytes(), current.read_bytes())
    print("PASS Phase 8B-I4R3P helper arithmetic/checklist/non-mutation invariant")

    protocol = PROTOCOL.read_text(encoding="utf-8")
    required = (
        "DUAL_PAUSED_CONFIRMED -> ADVANCE_AUTHORIZED -> ADVANCING -> TIME_PAUSE_REQUESTED -> TIME_PAUSED_CONFIRMED -> MENU_PAUSE_REQUESTED -> DUAL_PAUSED_CONFIRMED",
        "DUAL_PAUSED_CONFIRMED -> SAVE_AUTHORIZED -> SAVE_IN_PROGRESS -> POST_SAVE_TIME_VERIFY -> TIME_PAUSED_CONFIRMED -> MENU_PAUSE_REQUESTED -> DUAL_PAUSED_CONFIRMED",
        "positive native time-pause acknowledgement",
        "positive Escape-menu acknowledgement",
        "Metadata inspection is forbidden until `DUAL_PAUSED_CONFIRMED`",
        "No window, tool, or file switching",
        "ADVANCING`, `TIME_PAUSE_REQUESTED`, `MENU_PAUSE_REQUESTED`, `SAVE_IN_PROGRESS`, or `POST_SAVE_TIME_VERIFY`",
        "Diagnostic saves after control loss are forbidden",
        "20–28",
        "30 campaign hours",
        "4–8",
        "10 campaign hours",
        "40 campaign hours",
        "A18341FB2CD6B8623B45B52155C1A7D987DBEE7AAE14AE7DBF704514EC7F37B5",
    )
    for phrase in required:
        assert phrase in protocol, phrase
    print("PASS Phase 8B-I4R3P dual-latch/save-state/unsafe-switch invariant")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

