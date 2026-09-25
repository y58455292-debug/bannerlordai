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
NPC kingdom creation remains intentionally unauthorized because no ordinary native loop or fully audited initialization boundary has been identified. The L1 behavior does not need kingdom creation: it observes native succession and destruction without mutating political authority. The protected campaign loaded with seven active kingdoms and seven continuity records; the bounded 76.401-hour natural observation produced no native lifecycle event (honest null). TestRunner's separately named save verification timed out and did not create a file. It was issued while both escape and native town-wait menu blockers were present; this is a plausible save-path contributor, not a proven cause. A retry launch failed to initialize fresh Inspector/TestRunner/ClanAI telemetry, so no second save command was sent. The protected baseline hash remains unchanged and no Phase 2D test save exists. Persistence/reload is not yet proven. See `Reports/KingdomContinuity/PHASE2D_L1_RUNTIME_RESULT.md`.

## Local work warning
No known unrelated local work. The L1 DLL is deployed locally with verified rollback at `D:\BannerlordAIResearch\Builds\Rollback_Phase2D_L1_20260924_ClanAI`; its SHA-256 is recorded in the runtime result. The protected demo-gate save was not overwritten.

## Exact next Luna task
Resume Phase 2D-L1 validation only when the game and Inspector/TestRunner have a fresh ready session: clear escape/town-wait menu blockers, stop time, create a uniquely named save through the existing TestRunner `SAVE_AS_TEST` path, confirm the file exists, exit without saving over the source fixture, reload the new save, and verify seven records restore/reconcile with no duplicate notices. The previous failed save was attempted with native menu blockers active (plausible, unproven cause); the retry process did not initialize runtime telemetry. Preserve the protected baseline hash. Continue one timeboxed natural observation only after a valid campaign reload and within the existing native time-control boundary; retain null if no succession/destruction occurs. Do not change the ledger design unless save/load evidence demonstrates a concrete defect. Do not create or rename kingdoms, select rulers, transfer settlements/membership, mutate wars, alter the 28-day timer, accelerate rebels, or use synthetic events as positive evidence. L1 remains incomplete until actual save/load proof and a natural native lifecycle event/notice are obtained.
