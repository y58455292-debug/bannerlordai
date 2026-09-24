# Codex Status

## Current task
Phase 2D — kingdom continuity and successor states (ROADMAP.md).

## Last completed checkpoint
Phase 2C passed at Bannerlord's supported native boundary.

## Current state
PHASE 2D ARCHITECTURE RESOLVED; READY FOR LUNA SLICE 2D-L1

## Current candidate
`v0.22A-ruler-courtship-native-v1`. Built and deployed with verified rollback.

## Proven / observed result
- Implemented: an NPC ruling clan scans eligible independent clans, chooses the highest native `GetScoreOfKingdomToGetClan`, evaluates both native join barter values, and invokes only `ExecuteAiBarter` for positive native combined value.
- Build-proven: `dotnet build src/ClanAI/src/ClanAI/ClanAI.csproj -c Release` succeeds with 0 errors and the inherited `System.ValueTuple` warning.
- Runtime-observed / causal / boundary-crossing / committed in-world: Vlandia selected naturally independent Banu Ruwaid, native combined surplus was +8,050, native AI barter was attempted, and post-state confirmed Banu Ruwaid in Vlandia.
- The 2B protocol requires native candidate eligibility, native clan and kingdom values, positive combined surplus, `ExecuteAiBarter`, and post-state confirmation. No direct faction transfer or synthetic score/relation manipulation is allowed.
- Phase 2C runtime-observed: Banu Ruwaid stayed independent for 81.003 campaign hours; both parties survived, used settlements, fought bandits, and grew from 195 to 205 combined troops with zero custom spawn/recruitment changes and zero Inspector errors.

## Current blocker / uncertainty
The architectural blocker is resolved. NPC kingdom creation remains intentionally unauthorized because no ordinary native loop or fully audited initialization boundary has been identified. Phase 2D-L1 does not need kingdom creation: it establishes persistent, player-visible continuity over native succession and destruction without mutating political authority.

## Local work warning
No known unrelated local work. The current v0.22A DLL was restored after the isolated no-save Phase 2C observation.

## Exact next Luna task
Implement Phase 2D-L1 `KingdomContinuityBehavior`: observe native `RulingClanChanged`, `KingdomDestroyedEvent`, and `KingdomCreatedEvent`; persist a versioned per-kingdom continuity record containing original/current observed name, immutable recorded culture ID, current ruling clan ID, native succession count/time, and terminal destruction state/time; reconcile safely on new game/load without duplicate notices; emit exactly one concise player-facing notice for a native ruler change or destruction; add focused duplicate/load/null-ruler/culture-immutability tests; build; then run one bounded natural observation, preserving a null if no event occurs. Do not create or rename kingdoms, select rulers, transfer settlements/membership, mutate wars, alter the 28-day timer, accelerate rebels, or use synthetic events as positive evidence.
