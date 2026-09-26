# Phase 8B-I4R2P — Release-Smoke Control Protocol Hardening

Date: 2026-09-26 UTC

Parent checkpoint: `39272c1881c22e236ba7e08d4695e90acbba2fee`

Scope: offline failure audit, development-only control protocol, and read-only DayLong measurement helper. Bannerlord was not launched. ClanAI was not built, deployed, or changed. No live smoke was started.

## Classification

Both prior smokes are closed **test-control failures**, not demonstrated ClanAI defects.

### I4 — `cf259e62f3854bae5eb044659d4d58999d186427`

The operator used ordinary time controls and screenshots, then performed other UI/focus/menu work without a positive native stopped-state acknowledgement. The first authoritative elapsed-time check was the guarded save metadata: protected `DayLong=1039.761`, guarded `DayLong=1041.941`, or `52.320` campaign hours. The 48-hour bound had already been crossed, and further unsaved advancement followed. The operator discovered the failure only after reading the materialized save because calendar appearance, screenshots, and wall-clock timing had been treated as adequate control. Reload was correctly not attempted after discovery.

### I4R1 — `39272c1881c22e236ba7e08d4695e90acbba2fee`

The first two probe measurements were controlled at `2.160` and `7.368` cumulative hours. For burst 3, a four-second fast-forward interval was requested, followed by a native pause-button click and Escape request. The retained image labelled `b03_paused.png` actually showed fast-forward active and no pause menu. The operator nevertheless returned control to the tool/review loop. A later Escape restored the menu; only then did the next probe reveal `DayLong=1041.662`, or `45.624` hours. Thus the unacknowledged interval added about `38.256` hours. A sent key/click and a screenshot filename were mistaken for state; the image content was not used as a blocking acknowledgement soon enough.

Neither run produced evidence justifying a ClanAI product fix.

## Selected hard-pause acknowledgement

The next smoke uses the native **Escape/menu overlay** as its sole hard-pause acknowledgement.

`PAUSED_CONFIRMED` means a fresh view of the live Bannerlord window visibly shows the native Escape menu overlay and its menu controls over the campaign. The observer must inspect the displayed pixels/content; an input receipt, screenshot filename, old screenshot, time-speed icon, calendar date, or elapsed wall time is not acknowledgement.

If the overlay is absent, remain in `PAUSE_REQUESTED`. Do not switch windows, inspect files, review tools, write notes, or issue a save/load action. Observe before each retry; issue one Escape/menu action only when the overlay is absent, then immediately observe again. Never blindly double-tap Escape because a second press can close an already-open menu.

The mandatory state machine is:

`PAUSED_CONFIRMED -> ADVANCE_AUTHORIZED -> ADVANCING -> PAUSE_REQUESTED -> PAUSED_CONFIRMED`

No other transition from `ADVANCING` is permitted. In particular, `ADVANCING` cannot transition to file inspection, metadata review, tool review, saving, loading, window switching, or another burst.

- `PAUSED_CONFIRMED -> ADVANCE_AUTHORIZED`: allowed only when the last DayLong is known, the helper says `decision=ADVANCE`, remaining target/cutoff margins are recorded, and a measured burst limit has been chosen.
- `ADVANCE_AUTHORIZED -> ADVANCING`: deliberately close the confirmed menu and select the intended native speed.
- `ADVANCING -> PAUSE_REQUESTED`: at the end of the authorized burst, request the Escape/menu state.
- `PAUSE_REQUESTED -> PAUSED_CONFIRMED`: allowed only after visual inspection positively confirms the live native menu overlay.

Loss of focus, uncertain pixels, delayed capture, automation error, or an obscured window leaves the state at `PAUSE_REQUESTED`. The only permitted work is reacquiring the live game view and confirming/requesting the menu.

## DayLong measurement helper

`Automation/ReleaseSmoke/measure_daylong.py` is a repository-only, read-only calculator. It reads JSON or text metadata containing `DayLong`, uses decimal arithmetic, and prints:

- baseline DayLong;
- current DayLong;
- elapsed campaign hours;
- distance to target;
- distance to hard cutoff;
- optional total elapsed/distance;
- optional measured hours per second and a bounded next-burst suggestion;
- `ADVANCE` or a `STOP_*` decision.

It does not open `.sav` containers itself; the live operator must use the same already-proven read-only native-header extraction that produced the retained I4/I4R1 metadata JSON/text, then pass those extracted metadata files to this helper. The helper opens inputs read-only, issues no game command, does not save/load, imports no network/process/filesystem-mutation facility, and is outside `src/ClanAI/package/ClanAI`.

Pre-save example:

```text
python Automation/ReleaseSmoke/measure_daylong.py \
  --baseline protected_metadata.json \
  --previous previous_probe_metadata.json \
  --current current_probe_metadata.json \
  --burst-seconds <actual-authorized-seconds> \
  --target-min-hours 20 --target-max-hours 28 --hard-cutoff-hours 30
```

Post-reload example, additionally enforcing the total bound:

```text
python Automation/ReleaseSmoke/measure_daylong.py \
  --baseline guarded_metadata.json \
  --current post_reload_probe_metadata.json \
  --target-min-hours 4 --target-max-hours 8 --hard-cutoff-hours 10 \
  --total-baseline protected_metadata.json --total-hard-cutoff-hours 40
```

The helper never authorizes an action by itself. Its `ADVANCE` output is only one prerequisite for the explicit `ADVANCE_AUTHORIZED` transition.

## Safe measured-burst strategy

Native DayLong remains authoritative:

`elapsedHours = (currentDayLong - baselineDayLong) * 24`

1. Load the protected campaign and positively confirm the Escape menu.
2. While `PAUSED_CONFIRMED`, create the initial disposable probe through the normal menu, reconfirm the menu after saving, extract/read its metadata, and verify the probe matches protected `DayLong=1039.761`.
3. Calibration starts from `PAUSED_CONFIRMED` at normal 1x speed. Use one minimal deliberate close-menu/pause-request cycle—not a fixed multi-second high-speed burst. Positively confirm the menu before leaving the game, overwrite the one probe, and measure the resulting DayLong delta.
4. Calculate measured campaign-hours per actual authorized second. No high-speed duration is authorized before this measurement.
5. For ordinary bursts, choose a duration whose measured expectation is at most 2 campaign hours (never more than 3). The helper reports a 2-hour maximum suggestion.
6. When within 3 campaign hours of the target window, reduce the expected burst to at most 1 campaign hour. If the measurement is already in or beyond the target window, stop; do not chase an exact value.
7. Every burst is attended continuously. No unattended fast-forward is permitted.

The controlling bounds are:

| Stage | Target | Hard cutoff |
|---|---:|---:|
| Pre-save from protected baseline | 20–28 campaign hours | 30 campaign hours |
| Post-reload from guarded save | 4–8 campaign hours | 10 campaign hours |
| Total from protected baseline | — | 40 campaign hours |

Any `STOP_*` result, non-positive safety margin, missing/ambiguous metadata, or inability to prove the menu is open ends advancement. Near a cutoff, stop rather than trying to hit the target exactly.

## Exact next-live runbook

1. Verify the packaged DLL SHA-256 is `A18341FB2CD6B8623B45B52155C1A7D987DBEE7AAE14AE7DBF704514EC7F37B5` and the committed package manifest is unchanged. Do not rebuild or substitute it.
2. Use the protected campaign and exactly one disposable probe slot. Preserve the protected fixture and both prior failure artifacts.
3. Launch/load through normal Bannerlord UI with no TestRunner, Inspector, third-party command bus, or in-game research harness.
4. Establish `PAUSED_CONFIRMED` by inspecting the live native Escape menu. Record the protected/probe DayLong baseline.
5. Before **every** burst require all three: current state `PAUSED_CONFIRMED`; last DayLong known; remaining target, stage cutoff, and total-cutoff margins calculated.
6. For calibration and each later burst, record the authorized duration/rate/expected hours, deliberately close the confirmed menu, advance, and immediately enter `PAUSE_REQUESTED`.
7. After **every** burst, positively inspect the live Escape menu. If acknowledgement fails, do not switch windows. Keep observing and requesting a single Escape only when the overlay is absent until `PAUSED_CONFIRMED` is established.
8. Only from `PAUSED_CONFIRMED`, enter Save through the native menu and overwrite the disposable probe. After save completion, positively reconfirm the native Escape menu before switching away.
9. Extract the probe's native metadata read-only, run the helper, and record the DayLong arithmetic and decision. Only `decision=ADVANCE` permits consideration of another burst; return to step 5.
10. At 20–28 pre-save hours, or earlier if safety margin advises, remain hard-paused and create the separately named guarded save through normal UI. Never advance at or beyond 30 pre-save hours.
11. Keep the menu positively confirmed while reading guarded metadata. Reload that exact guarded save through normal UI. After campaign readiness, immediately re-establish `PAUSED_CONFIRMED` before any inspection or transfer.
12. Repeat the measured-burst/probe cycle relative to guarded DayLong, stopping within 4–8 hours, never reaching 10 post-reload hours or 40 total hours.
13. No control transfer occurs while the campaign may be advancing. Normal Bannerlord UI performs all actual save/load actions. External automation may click that UI and inspect files only after hard-pause acknowledgement; it remains development-only and is never a ClanAI runtime dependency.

## Offline validation and preservation

- Packaged DLL Git blob remains `a5fc3923b31e482960026a0044dd1b21d6e6d00a`; required SHA-256 remains `A18341FB2CD6B8623B45B52155C1A7D987DBEE7AAE14AE7DBF704514EC7F37B5`.
- Package manifest Git blob remains `1addeceb9435952162b1c63fe60282775c1a081e` and is not edited.
- I4 report/evidence blobs remain `1626fb486c97b19f97d30c578cb24ffdfcc2bd73` / `1fa4ca0701eb1ec4d5a8cd9f6f88d80042a63dac`.
- I4R1 report/evidence blobs remain `3db335dbaaae7792b4af5eb8f38d887876adcb30` / `218e1a9d434f841fcb6d55c82cf2f97bd7eb3277`.
- No `src/` file, package file, save key, or schema is changed.
- Helper deterministic tests cover decimal arithmetic, target/cutoff decisions, input-byte non-mutation, forbidden imports/write calls, repository-only placement, state-machine language, and required bounds.

Focused validation record: `Reports/Release/evidence/phase8b_i4r2p_smoke_control_validation_20260926.txt`.

## Status

- Previous failures classified as test-control failures: **YES**
- ClanAI product fix justified: **NO**
- Package/DLL changed: **NO**
- Gameplay changed: **NO**
- Save schema changed: **NO**
- Bannerlord launched: **NO**
- Positive pause acknowledgement protocol defined: **YES**
- Safe measured-burst strategy defined: **YES**
- Repository-only measurement helper added/reused: **YES — added**
- Ready for fresh live I4R2 attempt: **YES**

This checkpoint stops here. It does not authorize or begin the live I4R2 run.

