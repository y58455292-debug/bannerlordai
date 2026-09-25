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

## Boundary

This checkpoint proves campaign registration, active-kingdom initial reconciliation, and the bounded no-event observation only. The save/load acceptance item and natural-notice/event item remain open. Do not claim Phase 2D-L1 complete until those are verified. No native ruler, faction, kingdom, settlement, war, or timer state was changed by this behavior.
