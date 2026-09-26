#!/usr/bin/env python3
from __future__ import annotations

import ast
import subprocess
import sys
import tempfile
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
HELPER = ROOT / "Automation" / "ReleaseSmoke" / "measure_daylong.py"
PROTOCOL = ROOT / "Reports" / "Release" / "PHASE8B_I4R2P_SMOKE_CONTROL_PROTOCOL.md"


def run_helper(directory: Path, *arguments: str) -> dict[str, str]:
    command = [sys.executable, str(HELPER), *arguments]
    result = subprocess.run(command, cwd=directory, check=True, capture_output=True, text=True)
    return dict(line.split("=", 1) for line in result.stdout.splitlines())


def main() -> int:
    source = HELPER.read_text(encoding="utf-8")
    tree = ast.parse(source)
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
    forbidden_imports = {"socket", "urllib", "http", "requests", "subprocess", "os", "shutil"}
    assert not imported.intersection(forbidden_imports)
    assert ".write_" not in source and "write_text" not in source and "write_bytes" not in source
    print("PASS Phase 8B-I4R2P helper read-only/no-network invariant")

    with tempfile.TemporaryDirectory() as temporary:
        directory = Path(temporary)
        baseline = directory / "baseline.json"
        previous = directory / "previous.txt"
        current = directory / "current.json"
        baseline.write_text('{"header":{"DayLong":"1039.761"}}', encoding="utf-8")
        previous.write_text("DayLong=1039.851\n", encoding="utf-8")
        current.write_text('{"DayLong": 1040.068}', encoding="utf-8")
        before = {path: path.read_bytes() for path in (baseline, previous, current)}
        values = run_helper(
            directory,
            "--baseline", str(baseline),
            "--current", str(current),
            "--target-min-hours", "20",
            "--target-max-hours", "28",
            "--hard-cutoff-hours", "30",
            "--previous", str(previous),
            "--burst-seconds", "4",
        )
        assert values["baseline_daylong"] == "1039.761"
        assert values["current_daylong"] == "1040.068"
        assert values["elapsed_hours"] == "7.368"
        assert values["distance_to_target_hours"] == "12.632"
        assert values["distance_to_hard_cutoff_hours"] == "22.632"
        assert values["last_burst_hours"] == "5.208"
        assert values["decision"] == "ADVANCE"
        assert before == {path: path.read_bytes() for path in (baseline, previous, current)}

        target = directory / "target.txt"
        target.write_text("DayLong=1040.600\n", encoding="utf-8")
        target_values = run_helper(
            directory,
            "--baseline", str(baseline),
            "--current", str(target),
            "--target-min-hours", "20",
            "--target-max-hours", "28",
            "--hard-cutoff-hours", "30",
        )
        assert target_values["elapsed_hours"] == "20.136"
        assert target_values["decision"] == "STOP_TARGET_REACHED"

        post_reload_baseline = directory / "guarded.json"
        post_reload_current = directory / "post_reload.json"
        post_reload_baseline.write_text('{"DayLong":"1040.600"}', encoding="utf-8")
        post_reload_current.write_text('{"DayLong":"1041.450"}', encoding="utf-8")
        total_values = run_helper(
            directory,
            "--baseline", str(post_reload_baseline),
            "--current", str(post_reload_current),
            "--target-min-hours", "4",
            "--target-max-hours", "8",
            "--hard-cutoff-hours", "10",
            "--total-baseline", str(baseline),
            "--total-hard-cutoff-hours", "40",
        )
        assert total_values["elapsed_hours"] == "20.400"
        assert total_values["total_elapsed_hours"] == "40.536"
        assert total_values["decision"] == "STOP_TOTAL_HARD_CUTOFF"
    print("PASS Phase 8B-I4R2P DayLong arithmetic/non-mutation invariant")

    protocol = PROTOCOL.read_text(encoding="utf-8")
    required = (
        "PAUSED_CONFIRMED -> ADVANCE_AUTHORIZED -> ADVANCING -> PAUSE_REQUESTED -> PAUSED_CONFIRMED",
        "No other transition from `ADVANCING` is permitted",
        "Do not switch windows",
        "20–28",
        "30 campaign hours",
        "4–8",
        "10 campaign hours",
        "40 campaign hours",
        "A18341FB2CD6B8623B45B52155C1A7D987DBEE7AAE14AE7DBF704514EC7F37B5",
    )
    for text in required:
        assert text in protocol, text
    print("PASS Phase 8B-I4R2P hard-pause/state-machine/bounds invariant")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

