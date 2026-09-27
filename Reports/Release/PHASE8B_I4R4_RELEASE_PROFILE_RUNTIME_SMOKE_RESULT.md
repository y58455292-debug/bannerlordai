# Phase 8B-I4R4 — Release-Profile Runtime Smoke

## Classification: PRELIGHT BLOCKED

**Execution status: NOT EXECUTED.** The fresh preflight stopped at the first required gate: the game machine was unreachable. Bannerlord was not launched by this task. This is an environment-access blocker before launch, not a runtime/control FAIL and not a demonstrated ClanAI defect.

Authoritative starting GitHub `main`: `fca92b005d87d04d7c7677d2b2dd7865db6e5da1`.
Protocol read at that immutable checkpoint: `Reports/Release/PHASE8B_I4R4P_TIME_PAUSE_PROTOCOL.md`, blob `44fb3282c3d6c2d0fde2b2952f295ee94659006a`.

The latest user handoff explicitly classifies unavailable machine/runtime access before launch as `PRELIGHT BLOCKED`. The earlier unreachable I4R4 request is likewise treated as blocked/not executed; no previous runtime chronology was continued. I4, I4R1, I4R2, and I4R3 remain closed historical test-control failures.

## Fresh access check

Before repository inspection or any game command, Remote Desktop Commander `list_devices` returned:

| Returned field | Value |
|---|---|
| Device | `DESKTOP-JO4B7VH` |
| Device ID | `fd6618f4-5715-46b1-8665-68172ef15169` |
| Status | `offline` |
| Last seen | `2026-09-26T07:23:24.636+00:00` |

A direct `ping` targeting that device also failed, with `is_error=true` and the exact message suffix:

> please connect a device to use remote tools

The response's account-identifying prefix is omitted. The reported last-seen value is a historical heartbeat, not the time of this fresh check. Neither connectivity response supplied a successful pong or current device-side observation timestamp.

Preflight was stopped. No remote shell/process command, screenshot capture, game launch, input action, save, load, or metadata extraction followed. Subsequent work was limited to GitHub reads and assembling/publishing this blocked-preflight documentation. The available command tools were not used to bypass the failed connectivity gate.

## Immutable requirements — not fresh measurements

| Required item | Required value | Fresh verification |
|---|---|---|
| Packaged/installed ClanAI DLL SHA-256 | `A18341FB2CD6B8623B45B52155C1A7D987DBEE7AAE14AE7DBF704514EC7F37B5` | NOT PERFORMED; installed file inaccessible; no binary hash recomputed |
| Protected save | `ClanAI V020V PERSIST DEMO GATE V021M 20260924` | NOT READ |
| Protected SHA-256 | `A91F15BF1403F1D29F942C56A1162F431113942CDACCAFE52F4D80303B4CB427` | NOT VERIFIED |
| Protected timestamp | `2026-09-24T17:20:12.2384633Z` | NOT VERIFIED |
| Protected native DayLong | `1039.761` | NOT VERIFIED |
| Bannerlord not already running | Process absence required before launch | UNKNOWN; no process inspection possible |
| Harmony dependency and exact installed file set | Must satisfy committed package | NOT INSPECTED |
| RuntimeProfile/Evidence/Visual War/Strategic Commitment overrides | Must remain absent/off as directed | NOT INSPECTED; none changed by this task |

Repository protocol declarations and past successful checks are not substituted for present machine verification. No mismatch was observed; the checks were unavailable.

## Required runtime fields

| Field | Result |
|---|---|
| Fresh Bannerlord launch / runtime PID | NOT PERFORMED / NONE CREATED BY THIS TASK |
| Main menu / module load / protected campaign load | NOT EXERCISED |
| Initial `BI4R4 PROBE` / every probe DayLong | NOT CREATED / NO MEASUREMENTS |
| Authorized burst durations / measured burst deltas | NONE / NONE |
| Calibration campaign-hours-per-second | NOT MEASURED; no historical rate reused |
| `TIME_PAUSED_CONFIRMED` acknowledgements | NONE; no live campaign observed |
| `MENU_OPEN_CONFIRMED` Save/Load acknowledgements | NONE |
| Post-Save time-pause verification | NOT EXERCISED; no Save performed |
| Pre-save elapsed hours / 20–28 h target / 30 h cutoff | NOT MEASURED / NOT EXERCISED / NOT EXERCISED |
| Guarded save name / DayLong / save result | NOT CREATED / NOT APPLICABLE / NOT PERFORMED |
| Guarded reload / post-reload probes | NOT PERFORMED / NONE |
| Post-reload elapsed / 4–8 h target / 10 h cutoff | NOT MEASURED / NOT EXERCISED / NOT EXERCISED |
| Total elapsed / 40 h total cutoff | NOT MEASURED / NOT EXERCISED |
| Runtime time-control failure | NOT OBSERVED; runtime never started |
| Runtime exceptions/errors / ClanAI-Harmony traces | NOT EXERCISED; no current-run log scan |
| Forbidden development references observed | NOT CHECKED; not a negative scan result |
| Research telemetry created | NOT CHECKED on machine; no runtime launched by this task |
| Duplicate initialization/lifecycle issue observed | NOT EXERCISED |
| Protected fixture unchanged | NOT REVERIFIED; no save/file writes issued to the machine |
| Final installed DLL unchanged | NOT REVERIFIED; no rebuild/deploy/substitution performed |
| Native exit / process gone | NOT PERFORMED / UNKNOWN; no process created by this task |
| Gameplay or policy changed by this task | NO |
| Save schema changed by this task | NO |

There is no numeric elapsed-hour measurement, including no measured zero-hour total. Zero launch/save/load/burst actions are operation counts, not observations of the unreachable campaign. No current machine safety, telemetry absence, clean logs, or exhaustive network/file-access result is claimed.

## Closure

**Final classification: PRELIGHT BLOCKED. Live I4R4 smoke: NOT EXECUTED. Ready for next Phase 8 release-hardening checkpoint: NO.**

Only this report, focused evidence, and a CODEX_STATUS checkpoint insertion are changed. The existing protocol/helper, package/source, and previous evidence are preserved. No repair, reconnect attempt, replacement runtime, or further milestone is undertaken as part of this closure.

Focused evidence: `Reports/Release/evidence/phase8b_i4r4_release_profile_smoke_20260927.txt`. The `20260927` suffix preserves the user's requested artifact name; it is not a claimed Bannerlord execution date. Any later live attempt must start from a fresh preflight after authorized machine access is restored. Stop here.
