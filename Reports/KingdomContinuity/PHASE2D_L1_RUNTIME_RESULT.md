# Phase 2D-L1 implementation and runtime result

Date: 2026-09-24 / 2026-09-25 UTC

## Implementation

Added `KingdomContinuityBehavior` and registered it with the campaign. It listens to native kingdom creation, ruling-clan change, destruction, new-game, session-launch, and load-finished events. Its versioned per-kingdom ledger stores observed names, the initially recorded culture ID, ruling-clan IDs, succession count/time, and terminal destruction state/time. New/load reconciliation fills records from active native kingdoms and does not emit notices. Duplicate ruler callbacks are ignored when the recorded ruler already matches native current state; destroyed records are terminal. Succession/destruction produce one player notice per accepted state transition. No political mutation API is called.

Focused source-invariant checks are in `Tests/KingdomContinuity/test_ledger_invariants.py`.

## Build and deployment

- Release build: `dotnet build src/ClanAI/src/ClanAI/ClanAI.csproj -c Release` — passed, 0 errors; one inherited `System.ValueTuple` warning.
- DLL SHA-256: `5A88AC72C6154680A6B009F7C9A5E262CA4BE8F71AFC54B7A0C1F3057AA64B9C`.
- Deployed only with Bannerlord closed. The previous live DLL was backed up and verified before replacement at `Builds/Rollback_Phase2D_L1_20260924_ClanAI` on the local research machine.
- The deployed runtime reported zero Inspector errors during this observation.

## Bounded natural observation

Loaded the protected `ClanAI V020V PERSIST DEMO GATE V021M 20260924` fixture without saving over it. On session load the behavior reconciled seven active kingdoms into seven records (`KINGDOM_CONTINUITY_RECONCILED active=7 records=7 mutation=False`). A bounded fast-forward advanced campaign time from 649491.270 to 649567.671 hours (76.401 hours). The TestRunner repeatedly encountered the native town-wait menu and escape-menu blocker; time was paused at that boundary. No native kingdom creation, ruler-change, or destruction callback occurred in the inspected runtime log. Result: **natural lifecycle event = null**; no synthetic event was used.

## Save/load blocker

An isolated test save was requested through TestRunner as `ClanAI V020V PERSIST PHASE2D L1 CONTINUITY 20260924`. TestRunner logged `SAVE_TEST_BEGIN` and `SAVE_TEST_RETURNED` but no file appeared; its bounded verifier recorded `SAVE_TEST_FAILED ... reason=verify_timeout`. The protected baseline save timestamp and contents remained untouched. Therefore ledger save/load persistence is **not runtime-proven** in this run. Static build and source-invariant coverage are not substitutes for an actual save/load test.

### Follow-up diagnosis (2026-09-24)

The failed save was scheduled while TestRunner reported `escape_menu_open;menu_active:town_wait_menus`. In the earlier successful demo-gate round trip, the same `SAVE_AS_TEST` mechanism logged `SAVE_TEST_END ... exists=True` about 2.12 seconds after `SAVE_TEST_RETURNED`; in the failed L1 attempt it reached the 30-second verifier deadline. This makes the active native menu state a plausible contributor, but not a proven root cause. The retry should issue the save only after the campaign is ready, the escape/menu blockers are cleared, and time control is stopped, then verify the uniquely named file before reloading.

A second launch attempt was made from the game's installation directory. The Bannerlord.Native process remained present/responding and the movement bridge listed a foreground target, but the app inventory exposed no application and no fresh TestRunner status, Inspector log, ClanAI log, or listener on the expected Inspector ports appeared for over a minute. No save/load command was sent because runtime readiness could not be verified. The process was stopped without saving. The protected demo-gate file still has SHA-256 `A91F15BF1403F1D29F942C56A1162F431113942CDACCAFE52F4D80303B4CB427`; no `*PHASE2D*CONTINUITY*` save exists. This is a local runtime initialization blocker, not evidence about the ledger's serialization.

### Readiness retry (2026-09-24)

The installed launcher configuration showed exactly ten selected modules, including `BannerlordInspector`, `ClanAI`, and `NavalDLC`. Starting `Bannerlord.Native.exe` or `Bannerlord.exe` without that module list produced empty native logs and no runtime telemetry. Starting the game entry executable with the saved selected module IDs in the normal `/singleplayer _MODULES_*...*_MODULES_` handoff resolved that readiness failure. Inspector freshly listened on 8420, `/screen` reached initialized `GauntletInitialScreen`, and TestRunner emitted a fresh `RUNNER_LOAD`/`module_loaded` status. The hash-verified protected fixture then loaded successfully; TestRunner reported `campaignReady=True`, `timeControl=Stop`, and campaign hour `649491.27044636116`. ClanAI emitted fresh startup/load telemetry and `KINGDOM_CONTINUITY_RECONCILED active=7 records=7 mutation=False` twice.

The fixture continues to present a native campaign `MenuContext` (`town_wait_menus`); TestRunner's `CLOSE_ESCAPE_MENU` returned `escape_menu_already_closed`, and Inspector still reported `campaignMenu.open=true`. No `SAVE_AS_TEST` was issued in this active-menu state. The in-app computer-use surface in this session exposed browsers only; the separate Bannerlord input tool requires visual verification via that computer-use surface before sending Escape, which was unavailable. The campaign remains stopped. The protected fixture SHA-256 remains `A91F15BF1403F1D29F942C56A1162F431113942CDACCAFE52F4D80303B4CB427`, and no Phase 2D test save exists.

## Boundary

This checkpoint proves campaign registration and initial active-kingdom reconciliation both on the original session and this fresh protected-fixture load. The original bounded observation advanced 76.401 campaign hours with no native lifecycle callback (honest null). This fresh session adds no natural-event observation time. Save/load round-trip proof remains blocked only at clearing the native town-wait menu; no save attempt was made after the successful fixture load. Natural-notice/event acceptance remains open. Do not claim Phase 2D-L1 complete until those are verified. No native ruler, faction, kingdom, settlement, war, or timer state was changed by this behavior.
