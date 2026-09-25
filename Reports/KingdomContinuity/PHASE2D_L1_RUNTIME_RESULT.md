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

The fixture continues to present a native campaign `MenuContext` (`town_wait_menus`). When TestRunner's status reported `escape_menu_open`, `CLOSE_ESCAPE_MENU` returned `escape_menu_closed`; the latest status has only `menu_active:town_wait_menus`, and Inspector still reports `campaignMenu.open=true`. No `SAVE_AS_TEST` was issued in this active-menu state. The in-app computer-use surface in this session exposed browsers only; the separate Bannerlord input tool requires visual verification via that computer-use surface before sending Escape, which was unavailable. The campaign remains stopped. The protected fixture SHA-256 remains `A91F15BF1403F1D29F942C56A1162F431113942CDACCAFE52F4D80303B4CB427`, and no Phase 2D test save exists.

### Successful save materialization and reload continuation (2026-09-25)

Using the established Desktop Commander-backed TestRunner command bus, the escape overlay was cleared with `CLOSE_ESCAPE_MENU`, campaign time was advanced with `FAST`, then paused for the save. A uniquely named test save was requested as `ClanAI V020V PERSIST PHASE2D L1 CONTINUITY 20260925 0130`. TestRunner completed verification with `SAVE_TEST_END ... exists=True`. The escape overlay reopened once during save verification and was cleared once; the native `town_wait_menus` context was treated separately. This proves test-save materialization for the L1 candidate and supersedes the earlier save-verification blocker.

`EXIT_NOSAVE` then ended the Bannerlord/TestRunner/watchdog processes as expected. The session was relaunched through the already-proven configured-module path rather than an alternate launch experiment. TestRunner reached `campaignReady=True` on the protected demo-gate fixture and accepted a load request for the new L1 continuity save.

The latest fresh status names `ClanAI V020V PERSIST PHASE2D L1 CONTINUITY 20260925 0130` but still reports `screen_active:GameLoadingScreen`. No duplicate load was issued. No escape close was sent because `escape_menu_open` was not reported. At this checkpoint, save materialization is proven; post-load ledger restoration, seven-record verification, duplicate-notice absence, and political-state verification remain pending until the current load completes.

### Successful direct fresh-menu reload proof (2026-09-25)

A fresh configured-module launch reached initialized `GauntletInitialScreen` with no campaign loaded. The uniquely named L1 continuity save was then loaded directly once through the established TestRunner command bus. TestRunner reported `CAMPAIGN_INSTANCE generation=1` and `CAMPAIGN_READY timeControl=Stop` within six seconds, with `campaignHours=649514.89141922223` and `loadedSave=ClanAI V020V PERSIST PHASE2D L1 CONTINUITY 20260925 0130`. Inspector had returned to the active Naval campaign map; `GameLoadingScreen` was no longer present.

ClanAI emitted `KINGDOM_CONTINUITY_RESTORE records=7 found=True schema=1 mutation=False`, followed by two load/session reconciliations at `active=7 records=7 mutation=False`. A bounded log filter found zero `KINGDOM_CONTINUITY_SUCCESSION` or `KINGDOM_CONTINUITY_DESTROYED` entries after the fresh reload. In the current implementation those callbacks are the only paths that emit succession/destruction notices, while reconciliation itself emits no notice. This therefore closes the missing reload proof: all seven records restored, no duplicate succession/destruction notice was generated by load reconciliation, and the continuity behavior performed no native political mutation.

Exact runtime excerpts are preserved in `Reports/KingdomContinuity/evidence/phase2d_l1_reload_20260925.txt`.

## Boundary

Phase 2D-L1 is now runtime-proven for initial reconciliation plus the guarded save/load round trip. The uniquely named test save materialized successfully, restored seven serialized continuity records with `found=True`, reconciled seven active kingdoms into seven records, produced no succession/destruction callback or notice during load reconciliation, and logged `mutation=False` throughout continuity restore/reconcile. The protected demo-gate fixture was not overwritten.

The earlier bounded natural observation still remains an honest null for a real kingdom creation, succession, or destruction event. Natural lifecycle-event notice acceptance is therefore still open as a broader Phase 2D observation, but it no longer blocks the Phase 2D-L1 persistence checkpoint. Proceed to the ordered Phase 3 Home Responsibility deterministic test seam after the required mission-boundary sync.
