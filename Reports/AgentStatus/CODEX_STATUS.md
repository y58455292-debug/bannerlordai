# Codex Status

## Current task
Phase 2D — kingdom continuity and successor states (ROADMAP.md).

## Last completed checkpoint
Phase 2C passed at Bannerlord's supported native boundary.

## Current state
READY FOR PHASE 2D NATIVE CONTINUITY AUDIT

## Current candidate
`v0.22A-ruler-courtship-native-v1`. Built and deployed with verified rollback.

## Proven / observed result
- Implemented: an NPC ruling clan scans eligible independent clans, chooses the highest native `GetScoreOfKingdomToGetClan`, evaluates both native join barter values, and invokes only `ExecuteAiBarter` for positive native combined value.
- Build-proven: `dotnet build src/ClanAI/src/ClanAI/ClanAI.csproj -c Release` succeeds with 0 errors and the inherited `System.ValueTuple` warning.
- Runtime-observed / causal / boundary-crossing / committed in-world: Vlandia selected naturally independent Banu Ruwaid, native combined surplus was +8,050, native AI barter was attempted, and post-state confirmed Banu Ruwaid in Vlandia.
- The 2B protocol requires native candidate eligibility, native clan and kingdom values, positive combined surplus, `ExecuteAiBarter`, and post-state confirmation. No direct faction transfer or synthetic score/relation manipulation is allowed.
- Phase 2C runtime-observed: Banu Ruwaid stayed independent for 81.003 campaign hours; both parties survived, used settlements, fought bandits, and grew from 195 to 205 combined troops with zero custom spawn/recruitment changes and zero Inspector errors.

## Current blocker / uncertainty
Ordinary independent NPC clans are explicitly excluded from native military target scoring, and no ordinary NPC kingdom-creation loop was identified. These are now preserved Phase 2C native boundaries. Phase 2D must first audit native ruler succession, kingdom destruction, rebel continuity, and kingdom identity mutation before proposing successor-state architecture.

## Local work warning
No known unrelated local work. The current v0.22A DLL was restored after the isolated no-save Phase 2C observation.

## Exact next action
Begin Phase 2D with a read-only native audit of ruler succession, kingdom destruction, rebel-clan transition, and safe kingdom-name/identity surfaces. Do not implement a successor state until its native ownership and lifecycle boundaries are understood.
