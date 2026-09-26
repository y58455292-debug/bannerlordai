# Phase 8B-I4R3P — Dual-Pause Release-Smoke Protocol

Date: 2026-09-26 UTC

Parent checkpoint: `59719f4cedf45e83bc3cd2187b5a3905bd3924b8`

Scope: offline audit and development-only control hardening. Bannerlord was not launched. ClanAI was not built, deployed, or changed. No live I4R3 attempt was started.

## Classification

I4, I4R1, and I4R2 remain closed **test-control failures**. None demonstrates a ClanAI defect or justifies a product change.

## Exact I4R2 control failure

I4R2 correctly loaded the protected `DayLong=1039.761` fixture and created a matching baseline probe. The only authorized advancement was a one-second normal-speed calibration beginning at `05:15:24.269915Z`. Escape was requested at `05:15:25.279450Z`, and a fresh observation at `05:15:26.209282Z` visibly confirmed the native Escape menu. That acknowledgement succeeded.

At `05:15:46.301968Z`, the Escape menu was acknowledged before overwriting `BI4R2 PROBE`. At `05:15:50.024746Z`, the live view still showed Bannerlord's transient Saving overlay. Escape was then issued without first observing a completed-save state. By `05:16:07.980673Z`, the Escape overlay was absent and the campaign was visibly advancing at normal speed. Menu recovery was not visibly achieved until `05:16:27.889707Z` and was formally acknowledged at `05:17:26.002512Z`.

The saved calibration probe contained `DayLong=1039.770`, or `0.216` hours from baseline, but it predated the uncontrolled post-save interval and was stale before metadata inspection. A subsequent diagnostic probe captured `DayLong=1040.184`, or `10.152` saved hours, after `9.936` unplanned hours beyond the calibration save. That diagnostic Save then caused a second post-save interval: its immediate observation showed no Escape overlay, recovery took until `05:20:49.343586Z`, and the final unsaved elapsed time remained unknown.

The procedural gap was exact: **Save was allowed while campaign safety depended only on the Escape menu remaining open.** Native Save could dismiss or bypass that overlay, and no independently confirmed native zero-speed latch protected the campaign on return.

## Dual pause latch

Two independent acknowledgements are mandatory.

### `TIME_PAUSED_CONFIRMED`

A fresh live observation must positively show Bannerlord's native campaign time control in its actual paused/zero-speed state. A click receipt, key receipt, assumption, old screenshot, screenshot filename, calendar/date, wall-clock delay, or Save completion receipt is not confirmation.

### `MENU_PAUSED_CONFIRMED`

A fresh live observation must positively show the native Escape menu overlay open. A sent Escape, old image, or filename is not confirmation.

Only simultaneous current confirmation of both latches establishes `DUAL_PAUSED_CONFIRMED`.

The advancement state machine is:

`DUAL_PAUSED_CONFIRMED -> ADVANCE_AUTHORIZED -> ADVANCING -> TIME_PAUSE_REQUESTED -> TIME_PAUSED_CONFIRMED -> MENU_PAUSE_REQUESTED -> DUAL_PAUSED_CONFIRMED`

No other transition from `ADVANCING` is allowed. After a burst, native time pause is requested and visually confirmed first; only then is the Escape overlay requested and visually confirmed.

The Save state machine is separate:

`DUAL_PAUSED_CONFIRMED -> SAVE_AUTHORIZED -> SAVE_IN_PROGRESS -> POST_SAVE_TIME_VERIFY -> TIME_PAUSED_CONFIRMED -> MENU_PAUSE_REQUESTED -> DUAL_PAUSED_CONFIRMED`

Save completion does not preserve or acknowledge either latch. On return from every probe or guarded Save, observe the live campaign immediately. If native time is not visibly paused, restore it before any other work and obtain a fresh positive native time-pause acknowledgement. Only then request and obtain a positive Escape-menu acknowledgement. Metadata inspection is forbidden until `DUAL_PAUSED_CONFIRMED` is re-established.

## Save-safety rule

Before entering Save:

1. Positively confirm native time control is paused/zero-speed.
2. Positively confirm the Escape menu overlay.
3. Record `DUAL_PAUSED_CONFIRMED`.
4. Enter native Save through normal Bannerlord UI.

After Save:

1. Enter `POST_SAVE_TIME_VERIFY` and immediately observe the current live state.
2. Do not assume either latch survived.
3. If time is moving, request native time pause immediately; do nothing else.
4. Positively confirm `TIME_PAUSED_CONFIRMED` from a fresh view.
5. Request the Escape menu only after time pause is confirmed.
6. Positively confirm the overlay and record `DUAL_PAUSED_CONFIRMED`.
7. Only then may control leave the game for metadata extraction or helper execution.

This applies identically to the disposable probe and guarded save.

## Unsafe states and failure handling

No window, tool, or file switching is permitted while in `ADVANCING`, `TIME_PAUSE_REQUESTED`, `MENU_PAUSE_REQUESTED`, `SAVE_IN_PROGRESS`, or `POST_SAVE_TIME_VERIFY`. The prohibition includes metadata extraction, helper execution, log inspection, notes, GitHub/tool review, and control transfer.

Window switching is permitted only from `DUAL_PAUSED_CONFIRMED`.

If either latch cannot be positively established, advancement is over. Do not attempt to reconstruct uncertain time with another Save. **Diagnostic saves after control loss are forbidden.** Restore `DUAL_PAUSED_CONFIRMED` only if possible for a safe native no-save exit; do not resume advancement, do not create a guarded/diagnostic probe, exit without saving, and classify the run FAIL.

## Advancement and DayLong control

Native DayLong remains authoritative:

`elapsedHours = (currentDayLong - baselineDayLong) * 24`

I4R2 observed approximately `0.216` campaign hours per one authorized second at normal speed. This is historical evidence, not a reusable rate. I4R3 must recalibrate in its fresh process at normal speed.

- Use exactly one disposable probe slot.
- Use attended bursts only.
- Do not use high speed before a fresh measured-safe calibration.
- Expected normal burst advance must be no more than 2 campaign hours and never approximately more than 3.
- Within 3 hours of the target window, expected advance must be no more than 1 campaign hour.
- If the margin or live state is uncertain, STOP rather than advance.

| Stage | Target | Hard cutoff |
|---|---:|---:|
| Pre-save | 20–28 campaign hours | 30 campaign hours |
| Post-reload | 4–8 campaign hours | 10 campaign hours |
| Total | — | 40 campaign hours |

## Repository-only helper

`Automation/ReleaseSmoke/measure_daylong.py` remains read-only and repository-only. It does not inspect pixels, issue UI/game commands, launch Bannerlord, save/load, modify saves, or access a network/API. Its calculation behavior is unchanged. A small operator checklist is now printed with each result:

- `TIME_PAUSED_CONFIRMED`;
- `MENU_PAUSED_CONFIRMED`;
- `DUAL_PAUSED_CONFIRMED`;
- last DayLong known;
- cutoff margin positive.

Unchecked checklist lines are reminders, not automatic evidence or authorization. The helper remains outside the shipped module.

## Exact I4R3 live runbook

1. Verify the packaged DLL SHA-256 is `A18341FB2CD6B8623B45B52155C1A7D987DBEE7AAE14AE7DBF704514EC7F37B5`; do not rebuild or redeploy it. Preserve the protected fixture and all prior attempts.
2. Launch/load with normal Bannerlord UI, external Harmony, and Release defaults. No TestRunner, Inspector, in-game command bus, or Evidence profile.
3. Establish native zero-speed visually, then establish the Escape overlay visually. Record `DUAL_PAUSED_CONFIRMED`.
4. Create the initial disposable probe only through the Save state machine. Reacquire both latches after Save before leaving the game. Verify its DayLong equals the protected baseline.
5. Before **every** burst require: `DUAL_PAUSED_CONFIRMED`; last DayLong known; target margin calculated; stage and total hard-cutoff margins calculated; burst duration authorized from the fresh measured rate.
6. Deliberately leave the menu and advance. At burst end request native time pause first and positively confirm the actual paused/zero-speed control. Then request Escape and positively confirm its overlay. Only now is `DUAL_PAUSED_CONFIRMED` restored.
7. Save the probe only from `DUAL_PAUSED_CONFIRMED`. On Save return, immediately enter `POST_SAVE_TIME_VERIFY`; reacquire time pause first, then menu pause, before switching windows or reading metadata.
8. Read native metadata and run the helper only from `DUAL_PAUSED_CONFIRMED`. Record DayLong, elapsed hours, target/cutoff margins, measured rate, checklist, and decision.
9. Stop within 20–28 pre-save hours. Never advance at or beyond 30. Create the guarded save through the same Save state machine and reacquire both latches before reading it.
10. Reload the exact guarded save with normal UI. Once campaign-ready, establish native time pause first and Escape menu second before any inspection. Repeat attended measured bursts, stopping within 4–8 post-reload hours and below 10 post-reload/40 total.
11. If either latch fails at any point, create no diagnostic save. Restore dual pause only for safe exit, exit without saving, and classify FAIL.

## Offline validation and preservation

- Packaged DLL Git blob remains `a5fc3923b31e482960026a0044dd1b21d6e6d00a`; required SHA-256 remains `A18341FB2CD6B8623B45B52155C1A7D987DBEE7AAE14AE7DBF704514EC7F37B5`.
- Package manifest blob remains `1addeceb9435952162b1c63fe60282775c1a081e`.
- I4 report/evidence remain `1626fb486c97b19f97d30c578cb24ffdfcc2bd73` / `1fa4ca0701eb1ec4d5a8cd9f6f88d80042a63dac`.
- I4R1 report/evidence remain `3db335dbaaae7792b4af5eb8f38d887876adcb30` / `218e1a9d434f841fcb6d55c82cf2f97bd7eb3277`.
- I4R2 report/evidence remain `92d89f187e364b520f739a9e9f4da091aeccc07d` / `eeb9cb5d8b14f8c987e3c1f0eed424ce75578017`.
- No `src/`, package, gameplay, save-key, or schema file is changed.
- Focused tests prove helper non-mutation/no-network placement, checklist output, both positive acknowledgement requirements, both state machines, post-save reacquisition, unsafe-state switch prohibition, diagnostic-save prohibition, and bounds.

Focused evidence: `Reports/Release/evidence/phase8b_i4r3p_dual_pause_validation_20260926.txt`.

## Status

- Previous I4/I4R1/I4R2 failures remain test-control failures: **YES**
- ClanAI product fix justified: **NO**
- Package/DLL changed: **NO**
- Gameplay changed: **NO**
- Save schema changed: **NO**
- Bannerlord launched: **NO**
- Native time-pause acknowledgement required: **YES**
- Escape-menu acknowledgement required: **YES**
- Post-save dual-pause reacquisition required: **YES**
- Unsafe-state window switching forbidden: **YES**
- Diagnostic save after control loss forbidden: **YES**
- Ready for fresh live I4R3 attempt: **YES**

This checkpoint stops here. It does not begin or authorize the live run within this task.

