# Phase 8B-I4R4P — Native Time-Pause Release-Smoke Protocol

Date: 2026-09-26 UTC

Parent checkpoint: `184242d8db0671375805f71899bf89ab82ab35e4`

Scope: offline audit and development-only control simplification. Bannerlord was not launched. ClanAI was not built, deployed, or changed. No live I4R4 attempt was started.

## Classification

I4, I4R1, I4R2, and I4R3 remain closed **test-control failures**. None demonstrates a ClanAI defect or justifies a product change.

## Exact I4R3 audit

The protected fixture loaded successfully and the initial `BI4R3 PROBE` matched native `DayLong=1039.761`. Initial native time pause and the menu were positively observed before Save, and time pause was positively reconfirmed after Save before metadata inspection.

Exactly one one-second burst at native normal speed was authorized. It began at `05:54:47.520281Z`; native time pause was requested at `05:54:48.520891Z`. The fresh `05:54:49.802509Z` observation visibly showed `PAUSED` and the selected native pause control, and `TIME_PAUSED_CONFIRMED` was recorded at `05:55:09.281497Z`.

Two observed synthetic Escape-key requests then failed to show the menu:

- `05:55:10.398343Z`: native time remained visibly paused; menu absent;
- `05:55:37.193129Z`: after focus reacquisition and one retry, native time remained visibly paused; menu absent.

Advancement correctly stopped permanently. No post-calibration probe, diagnostic Save, guarded Save, reload, second burst, or replacement run followed. For safe exit only, another Escape request still failed, but the native clickable lower-left campaign menu button succeeded at `05:56:38.216319Z`: the menu opened while native time pause remained selected.

The procedural lesson is narrow. The campaign-time safety mechanism worked. The unreliable mechanism was synthetic keyboard menu invocation. This is an operator/UI-control result, not a ClanAI defect.

## Primary safety latch

`TIME_PAUSED_CONFIRMED` is the sole required campaign-time safety latch.

It means a fresh live observation visibly shows Bannerlord's native campaign time control in its actual paused/zero-speed state. None of these qualifies:

- click or key receipt;
- screenshot filename or old screenshot;
- calendar/date;
- wall-clock delay;
- assumption.

When `TIME_PAUSED_CONFIRMED` exists, campaign advancement is considered stopped. If it cannot be positively established, stop advancement permanently. Do not create a diagnostic Save, resume, recalibrate, or reconstruct elapsed time through further saves. Restore pause only if possible for safe native no-save exit, then classify FAIL.

## Menu navigation

`MENU_OPEN_CONFIRMED` is a UI-navigation state, not a campaign-safety latch. It means a fresh live observation visibly shows the campaign menu overlay.

The preferred path is the **native clickable lower-left campaign menu button** that succeeded during I4R3 while time remained paused. Synthetic Escape is optional and may be used only as an observed fallback; it is not required and its failure alone is not a test failure while native time remains paused and the native button can open the menu.

Normal Save/Load navigation requires both `TIME_PAUSED_CONFIRMED` and `MENU_OPEN_CONFIRMED`. File inspection does not require an open menu when time pause is positively confirmed and no Save/Load UI action is in progress.

## State machines

Advancement:

`TIME_PAUSED_CONFIRMED -> ADVANCE_AUTHORIZED -> ADVANCING -> TIME_PAUSE_REQUESTED -> TIME_PAUSED_CONFIRMED`

No other transition from `ADVANCING` is allowed. The next action after every burst is native time-pause request, followed by a fresh positive observation.

Save:

`TIME_PAUSED_CONFIRMED -> MENU_OPEN_CONFIRMED -> SAVE_AUTHORIZED -> SAVE_IN_PROGRESS -> POST_SAVE_TIME_VERIFY -> TIME_PAUSED_CONFIRMED`

After `TIME_PAUSED_CONFIRMED`, `MENU_OPEN_CONFIRMED` may be re-established if further UI navigation is needed. Save completion is never a pause acknowledgement. After every Save, immediately observe native time; if moving, restore pause before anything else and positively reconfirm it.

Reload uses normal Bannerlord UI from confirmed time pause and menu navigation. Once campaign-ready after load, immediately establish `TIME_PAUSED_CONFIRMED` before inspection, metadata work, notes, or advancement.

## Window-switching and metadata rule

No window, tool, file, note, GitHub, log, metadata, helper, or control transfer is allowed while in `ADVANCING`, `TIME_PAUSE_REQUESTED`, `SAVE_IN_PROGRESS`, or `POST_SAVE_TIME_VERIFY`.

Window switching is allowed only from `TIME_PAUSED_CONFIRMED`, provided no Save/Load UI action is in progress. Menu overlay presence is not required for file inspection. Metadata inspection requires `TIME_PAUSED_CONFIRMED`.

## Save/Load safety

Before every disposable probe or guarded Save:

1. Positively confirm `TIME_PAUSED_CONFIRMED`.
2. Click the native lower-left menu button.
3. Positively confirm `MENU_OPEN_CONFIRMED`.
4. Perform Save through normal Bannerlord UI.

After every Save:

1. Enter `POST_SAVE_TIME_VERIFY` and observe the live campaign immediately.
2. Verify native time is actually paused.
3. If moving, restore native pause immediately and do nothing else.
4. Positively confirm `TIME_PAUSED_CONFIRMED`.
5. Only then inspect metadata, run the helper, write notes, switch windows, or reopen menu navigation.

Diagnostic saves after time-pause control loss are forbidden.

For guarded reload, begin from `TIME_PAUSED_CONFIRMED`, open/confirm the native menu, load the exact guarded save, wait for campaign-ready, and immediately establish `TIME_PAUSED_CONFIRMED` before any other work.

## DayLong and advancement bounds

Native DayLong remains authoritative:

`elapsedHours = (currentDayLong - baselineDayLong) * 24`

`Automation/ReleaseSmoke/measure_daylong.py` remains repository-only, read-only, no-network, and no-game-command. I4R4 must recalibrate in its fresh process. Historical rates—including I4R2's approximately `0.216` campaign hours per authorized normal-speed second—are reference only.

- normal-speed calibration;
- attended bursts only;
- no high speed until fresh calibration proves it safe;
- expected normal burst no more than 2 campaign hours;
- expected burst within 3 hours of target no more than 1 campaign hour;
- uncertain state or margin means STOP.

| Stage | Target | Hard cutoff |
|---|---:|---:|
| Pre-save | 20–28 campaign hours | 30 campaign hours |
| Post-reload | 4–8 campaign hours | 10 campaign hours |
| Total | — | 40 campaign hours |

The helper checklist is simplified to match this protocol. Before advancement it lists time pause, last DayLong, and positive cutoff margin. A separate Save/Load checklist lists time pause plus menu-open confirmation. Escape and dual-pause are no longer advancement prerequisites. Calculation behavior is unchanged.

## Failure handling

If native time pause cannot be positively confirmed, stop and fail. Do not create diagnostic saves, resume advancement, try another calibration, or reconstruct elapsed time through additional writes.

If menu navigation fails while time remains paused, do not confuse it with time-control failure. Use the native clickable menu button and make reasonable observed UI-navigation retries while continuously preserving `TIME_PAUSED_CONFIRMED`. Only inability to achieve required Save/Load navigation becomes a UI-control failure.

## Exact I4R4 live runbook

1. Verify packaged DLL SHA-256 `A18341FB2CD6B8623B45B52155C1A7D987DBEE7AAE14AE7DBF704514EC7F37B5`; do not rebuild/redeploy. Preserve the protected fixture and all prior attempts.
2. Launch/load through normal Bannerlord UI with Release defaults. No TestRunner, Inspector, in-game command bus, or Evidence profile.
3. Establish `TIME_PAUSED_CONFIRMED` from a fresh live observation. Create the initial disposable probe using the native lower-left menu button and confirmed menu state. After Save, reconfirm time pause before reading metadata.
4. Before **every** burst require: `TIME_PAUSED_CONFIRMED`; last DayLong known; target margin calculated; stage cutoff margin calculated; total cutoff margin calculated; burst duration explicitly authorized from the fresh measured rate.
5. Advance only for the attended authorized burst. Then request native time pause, observe the live game, and positively confirm `TIME_PAUSED_CONFIRMED`. Only afterward open the menu if a probe Save is needed.
6. For **every** Save: confirm time pause; click the native menu button; confirm `MENU_OPEN_CONFIRMED`; Save; enter `POST_SAVE_TIME_VERIFY`; positively reconfirm time pause; only then inspect metadata.
7. Run the helper only while time pause is confirmed. Record DayLong, elapsed hours, margins, fresh rate, checklist, and decision. Stop rather than chase an exact target.
8. Stop within 20–28 pre-save hours and below 30. Create the guarded save through the same Save state machine.
9. For guarded reload: confirm time pause; use native menu-button navigation; load the exact guarded save; wait for campaign-ready; immediately establish `TIME_PAUSED_CONFIRMED`; only then continue.
10. Repeat attended measured bursts, stopping within 4–8 post-reload hours and below 10 post-reload/40 total.
11. If time pause integrity is lost, make no diagnostic Save. Restore pause only for safe no-save exit, exit, and classify FAIL.

## Offline validation and preservation

- Packaged DLL blob remains `a5fc3923b31e482960026a0044dd1b21d6e6d00a`; SHA-256 remains `A18341FB2CD6B8623B45B52155C1A7D987DBEE7AAE14AE7DBF704514EC7F37B5`.
- Package manifest blob remains `1addeceb9435952162b1c63fe60282775c1a081e`.
- I4 report/evidence remain `1626fb486c97b19f97d30c578cb24ffdfcc2bd73` / `1fa4ca0701eb1ec4d5a8cd9f6f88d80042a63dac`.
- I4R1 report/evidence remain `3db335dbaaae7792b4af5eb8f38d887876adcb30` / `218e1a9d434f841fcb6d55c82cf2f97bd7eb3277`.
- I4R2 report/evidence remain `92d89f187e364b520f739a9e9f4da091aeccc07d` / `eeb9cb5d8b14f8c987e3c1f0eed424ce75578017`.
- I4R3 report/evidence remain `8a1ad8073c93fa38ae31a56ce66934ec4de6d0eb` / `8267b91c00946f6dee14b56d6123df5795e849e9`.
- No `src/`, package, gameplay, save-key, or schema file is changed.
- Focused tests prove helper non-mutation/no-network placement, simplified checklist, time pause as primary latch, optional Escape, native button preference, mandatory post-Save verification, unsafe-state switching prohibition, diagnostic-save prohibition, and bounds.

Focused evidence: `Reports/Release/evidence/phase8b_i4r4p_time_pause_validation_20260926.txt`.

## Status

- I4/I4R1/I4R2/I4R3 remain test-control failures: **YES**
- ClanAI product fix justified: **NO**
- Package/DLL changed: **NO**
- Gameplay changed: **NO**
- Save schema changed: **NO**
- Bannerlord launched: **NO**
- Native time pause is primary safety latch: **YES**
- Synthetic Escape required: **NO**
- Native clickable menu button preferred: **YES**
- Post-Save time verification required: **YES**
- Diagnostic save after time-control loss forbidden: **YES**
- Ready for fresh live I4R4 attempt: **YES**

This checkpoint stops here. It does not begin the live run.

