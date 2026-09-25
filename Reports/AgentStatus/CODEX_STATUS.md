# Codex Status

## Current task
Phase 3 — territorial responsibility, defense, and ruler strategy (ROADMAP.md), beginning with the bounded offline source/test slice below.

## Last completed checkpoint
Phase 2C passed at Bannerlord's supported native boundary.

## Current state
PHASE 2D-L1 IMPLEMENTATION/BUILD/INITIAL RECONCILIATION PROVEN; SAVE/LOAD VALIDATION BLOCKED BY TOOL ACCESS (NOT GAMEPLAY FAILURE); PHASE 3 OFFLINE SOURCE AUDIT COMPLETE

## Current candidate
`v0.22A-ruler-courtship-native-v1`. Built and deployed with verified rollback.

## Proven / observed result
- Implemented: an NPC ruling clan scans eligible independent clans, chooses the highest native `GetScoreOfKingdomToGetClan`, evaluates both native join barter values, and invokes only `ExecuteAiBarter` for positive native combined value.
- Build-proven: `dotnet build src/ClanAI/src/ClanAI/ClanAI.csproj -c Release` succeeds with 0 errors and the inherited `System.ValueTuple` warning.
- Runtime-observed / causal / boundary-crossing / committed in-world: Vlandia selected naturally independent Banu Ruwaid, native combined surplus was +8,050, native AI barter was attempted, and post-state confirmed Banu Ruwaid in Vlandia.
- The 2B protocol requires native candidate eligibility, native clan and kingdom values, positive combined surplus, `ExecuteAiBarter`, and post-state confirmation. No direct faction transfer or synthetic score/relation manipulation is allowed.
- Phase 2C runtime-observed: Banu Ruwaid stayed independent for 81.003 campaign hours; both parties survived, used settlements, fought bandits, and grew from 195 to 205 combined troops with zero custom spawn/recruitment changes and zero Inspector errors.

## Current blocker / uncertainty
Phase 2D-L1's code, focused tests, Release build, safe deployment with rollback, and initial seven-kingdom load reconciliation are preserved as completed evidence. The protected fixture remains unchanged. Save/load round-trip and duplicate-notice validation are **BLOCKED BY TOOL ACCESS, NOT GAMEPLAY FAILURE**: this Codex session has no established Inspector/TestRunner command interface. No save command was issued from this session. The presence of native `town_wait_menus` is valid and is not a blocker; the save path has not been tested under that condition. Do not touch the live campaign or retry runtime proof until the established interface is available. Timebox: suspend this proof now; resume only when Inspector/TestRunner access is restored. The prior 76.401-hour natural observation remains an honest null for native lifecycle events. See `Reports/KingdomContinuity/PHASE2D_L1_RUNTIME_RESULT.md`.

Phase 3 offline source audit found no established defect in existing Home Responsibility behavior; it documented the current candidate/ownership/native-commit boundary and roadmap gaps. See `Reports/TerritorialResponsibility/PHASE3_SOURCE_AUDIT.md`. No Bannerlord runtime, tests, or build were run in this session because shell/build tools must not be launched while the protected live session is present.

## Local work warning
No known unrelated local work. The L1 DLL is deployed locally with verified rollback at `D:\BannerlordAIResearch\Builds\Rollback_Phase2D_L1_20260924_ClanAI`; its SHA-256 is recorded in the runtime result. The protected demo-gate save was not overwritten.

## Exact next offline task
Implement the first Phase 3 source-only test seam described in `Reports/TerritorialResponsibility/PHASE3_SOURCE_AUDIT.md`: minimally extract the pure eligibility/factor calculation from `HomeResponsibilityLayer`, add focused deterministic tests for owned versus foreign candidates, threatened owned settlements, weak recovery, and the ordinary home/patrol/defense factors, and preserve native candidate generation and post-vanilla commit verification. Run focused tests and the Release build offline. Do not deploy or claim campaign behavior from tests. Do not revisit Phase 2B/2C or redesign Phase 2D-L1. Park its save/load proof until Inspector/TestRunner tools are available, then verify a named test save/reload, seven continuity records, and duplicate-notice absence without modifying the protected fixture.
