#!/usr/bin/env python3
"""Read-only DayLong arithmetic for bounded release-profile smoke tests."""

from __future__ import annotations

import argparse
import json
import re
from decimal import Decimal, InvalidOperation
from pathlib import Path
from typing import Any


DAYLONG_PATTERN = re.compile(r'(?i)\bDayLong\b\s*[=:]\s*["\']?(-?\d+(?:\.\d+)?)')


def _find_json_daylong(value: Any) -> Decimal | None:
    if isinstance(value, dict):
        for key, item in value.items():
            if str(key).casefold() == "daylong":
                try:
                    return Decimal(str(item))
                except InvalidOperation as error:
                    raise ValueError(f"invalid DayLong value: {item!r}") from error
        for item in value.values():
            found = _find_json_daylong(item)
            if found is not None:
                return found
    elif isinstance(value, list):
        for item in value:
            found = _find_json_daylong(item)
            if found is not None:
                return found
    return None


def read_daylong(path: Path) -> Decimal:
    text = path.read_text(encoding="utf-8")
    try:
        found = _find_json_daylong(json.loads(text))
    except json.JSONDecodeError:
        found = None
    if found is None:
        match = DAYLONG_PATTERN.search(text)
        if match:
            found = Decimal(match.group(1))
    if found is None:
        raise ValueError(f"DayLong not found in {path}")
    return found


def format_decimal(value: Decimal) -> str:
    return format(value.quantize(Decimal("0.001")), "f")


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--baseline", type=Path, required=True)
    parser.add_argument("--current", type=Path, required=True)
    parser.add_argument("--target-min-hours", type=Decimal, required=True)
    parser.add_argument("--target-max-hours", type=Decimal, required=True)
    parser.add_argument("--hard-cutoff-hours", type=Decimal, required=True)
    parser.add_argument("--previous", type=Path)
    parser.add_argument("--burst-seconds", type=Decimal)
    parser.add_argument("--total-baseline", type=Path)
    parser.add_argument("--total-hard-cutoff-hours", type=Decimal)
    args = parser.parse_args()

    if not (Decimal("0") <= args.target_min_hours <= args.target_max_hours < args.hard_cutoff_hours):
        parser.error("require 0 <= target-min <= target-max < hard-cutoff")
    if (args.previous is None) != (args.burst_seconds is None):
        parser.error("--previous and --burst-seconds must be supplied together")
    if args.burst_seconds is not None and args.burst_seconds <= 0:
        parser.error("--burst-seconds must be positive")
    if (args.total_baseline is None) != (args.total_hard_cutoff_hours is None):
        parser.error("--total-baseline and --total-hard-cutoff-hours must be supplied together")

    baseline = read_daylong(args.baseline)
    current = read_daylong(args.current)
    elapsed = (current - baseline) * Decimal("24")
    if elapsed < 0:
        raise ValueError("current DayLong precedes baseline DayLong")

    distance_target = args.target_min_hours - elapsed
    distance_cutoff = args.hard_cutoff_hours - elapsed
    total_elapsed = None
    total_distance = None
    if args.total_baseline is not None:
        total_baseline = read_daylong(args.total_baseline)
        total_elapsed = (current - total_baseline) * Decimal("24")
        total_distance = args.total_hard_cutoff_hours - total_elapsed

    if total_distance is not None and total_distance <= 0:
        decision = "STOP_TOTAL_HARD_CUTOFF"
    elif distance_cutoff <= 0:
        decision = "STOP_HARD_CUTOFF"
    elif elapsed >= args.target_min_hours:
        decision = "STOP_TARGET_REACHED"
    else:
        decision = "ADVANCE"

    print(f"baseline_daylong={baseline}")
    print(f"current_daylong={current}")
    print(f"elapsed_hours={format_decimal(elapsed)}")
    print(f"distance_to_target_hours={format_decimal(distance_target)}")
    print(f"distance_to_hard_cutoff_hours={format_decimal(distance_cutoff)}")
    if total_elapsed is not None:
        print(f"total_elapsed_hours={format_decimal(total_elapsed)}")
        print(f"distance_to_total_hard_cutoff_hours={format_decimal(total_distance)}")

    if args.previous is not None:
        previous = read_daylong(args.previous)
        burst_hours = (current - previous) * Decimal("24")
        if burst_hours < 0:
            raise ValueError("current DayLong precedes previous DayLong")
        rate = burst_hours / args.burst_seconds
        safe_hours = Decimal("1") if distance_target <= Decimal("3") else Decimal("2")
        safe_hours = min(safe_hours, max(Decimal("0"), distance_cutoff))
        if total_distance is not None:
            safe_hours = min(safe_hours, max(Decimal("0"), total_distance))
        suggested_seconds = Decimal("0") if rate <= 0 else safe_hours / rate
        print(f"last_burst_hours={format_decimal(burst_hours)}")
        print(f"measured_hours_per_second={format_decimal(rate)}")
        print(f"next_burst_max_hours={format_decimal(safe_hours)}")
        print(f"suggested_next_burst_seconds={format_decimal(suggested_seconds)}")

    print("CONTROL_REQUIREMENTS_BEFORE_ADVANCE=")
    print("CHECK_TIME_PAUSED_CONFIRMED=[ ]")
    print("CHECK_MENU_PAUSED_CONFIRMED=[ ]")
    print("CHECK_DUAL_PAUSED_CONFIRMED=[ ]")
    print("CHECK_LAST_DAYLONG_KNOWN=[ ]")
    print("CHECK_CUTOFF_MARGIN_POSITIVE=[ ]")
    print(f"decision={decision}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

