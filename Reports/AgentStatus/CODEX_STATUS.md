# Codex Status

## Current task
Phase 2D — kingdom continuity and successor states (ROADMAP.md).

## Last completed checkpoint
Phase 2C passed at Bannerlord's supported native boundary.

## Current state
PHASE 2D-L1 IMPLEMENTED; BUILD/INITIAL RUNTIME RECONCILIATION PROVEN; SAVE/LOAD AND NATIVE EVENT NOTICE NOT YET PROVEN

## Current candidate
`v0.22A-ruler-courtship-native-v1`. Built and deployed with verified rollback.

## Proven / observed result
- Implemented: an NPC ruling clan scans eligible independent clans, chooses the highest native `GetScoreOfKingdomToGetClan`, evaluates both native join barter values, and invokes only `ExecuteAiBarter` for positive native combined value.
- Build-proven: `dotnet build src/ClanAI/src/ClanAI/ClanAI.csproj -c Release` succeeds with 0 errors and the inherited `System.ValueTuple` warning.
- Runtime-observed / causal / boundary-crossing / committed in-world: Vlandia selected naturally independent Banu Ruwaid, native combined surplus was +8,050, native AI barter was attempted, and post-state confirmed Banu Ruwaid in Vlandia.
- The 2B protocol requires native candidate eligibility, native clan and kingdom values, positive combined surplus, `ExecuteAiBarter`, and post-state confirmation. No direct faction transfer or synthetic score/relation manipulation is allowed.
- Phase 2C runtime-observed: Banu Ruwaid stayed independent for 81.003 campaign hours; both parties survived, used settlements, fought bandits, and grew from 195 to 205 combined troops with zero custom spawn/recruitment changes and zero Inspector errors.

## Current blocker / uncertainty
NPC kingdom creation remains intentionally unauthorized because no ordinary native loop or fully audited initialization boundary has been identified. The L1 behavior does not need kingdom creation: it observes native succession and destruction without mutating political authority. The original protected-fixture session loaded and reconciled seven kingdoms; its bounded 76.401-hour observation produced no native lifecycle event (honest null). Follow-up identified that direct executable launches without the selected module-list handoff do not initialize the harness. Launching Bannerlord with the ten configured selected module IDs restored fresh Inspector, TestRunner, and ClanAI telemetry; the protected fixture again loaded and reconciled `active=7 records=7`, with campaign time stopped and source hash unchanged. The escape overlay has now been closed, but Inspector and TestRunner still report the native town-wait `MenuContext`; no save command has been issued in this active-menu state. The save/load round trip remains unproven; no Phase 2D test save exists. See `Reports/KingdomContinuity/PHASE2D_L1_RUNTIME_RESULT.md`.

## Local work warning
No known unrelated local work. The L1 DLL is deployed locally with verified rollback at `D:\BannerlordAIResearch\Builds\Rollback_Phase2D_L1_20260924_ClanAI`; its SHA-256 is recorded in the runtime result. The protected demo-gate save was not overwritten.

## Exact next Luna task
Resume Phase 2D-L1 save/load validation only from the current fresh, stopped, protected-fixture campaign. The configured module-list launch handoff is now known; do not start binaries bare. The escape overlay is clear. The remaining immediate step is to dismiss the native town-wait `MenuContext` safely, then issue one uniquely named `SAVE_AS_TEST`, verify the actual save file exists, exit without saving over the source fixture, reload the test save, and verify seven records restore/reconcile with no duplicate notices. Preserve the protected baseline hash. Do not continue natural-event observation before this round trip passes. Do not change the ledger design unless save/load evidence demonstrates a concrete defect. Do not create or rename kingdoms, select rulers, transfer settlements/membership, mutate wars, alter the 28-day timer, accelerate rebels, or use synthetic events as positive evidence. L1 remains incomplete until actual save/load proof and a natural native lifecycle event/notice are obtained.
