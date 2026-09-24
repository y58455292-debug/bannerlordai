# Codex Status

## Current task
Phase 2C — independent clans as real political actors (ROADMAP.md).

## Last completed checkpoint
Phase 2B passed its preregistered strong runtime win condition with the `v0.22A-ruler-courtship-native-v1` candidate.

## Current state
PHASE 2C NATIVE CAPABILITY AUDITED; RUNTIME REBUILD OBSERVATION NEXT

## Current candidate
`v0.22A-ruler-courtship-native-v1`. Built and deployed with verified rollback.

## Proven / observed result
- Implemented: an NPC ruling clan scans eligible independent clans, chooses the highest native `GetScoreOfKingdomToGetClan`, evaluates both native join barter values, and invokes only `ExecuteAiBarter` for positive native combined value.
- Build-proven: `dotnet build src/ClanAI/src/ClanAI/ClanAI.csproj -c Release` succeeds with 0 errors and the inherited `System.ValueTuple` warning.
- Runtime-observed / causal / boundary-crossing / committed in-world: Vlandia selected naturally independent Banu Ruwaid, native combined surplus was +8,050, native AI barter was attempted, and post-state confirmed Banu Ruwaid in Vlandia.
- The 2B protocol requires native candidate eligibility, native clan and kingdom values, positive combined surplus, `ExecuteAiBarter`, and post-state confirmation. No direct faction transfer or synthetic score/relation manipulation is allowed.

## Current blocker / uncertainty
Bannerlord's native daily clan loop supports independent NPC finance and lord-party rebuilding, but ordinary independent NPC clans are explicitly excluded from native military target scoring. No ordinary NPC kingdom-creation loop was identified. These are native boundaries, not reasons to fabricate territory or a successor kingdom.

## Local work warning
No known unrelated local work. The Phase 2B implementation checkpoint should be clean after its focused commit.

## Exact next action
Run one bounded observation from the existing independent Banu Ruwaid boundary: record its clan/party state, advance a limited independent interval, and record survival or native respawn/rebuilding. Preserve a null. Do not alter party limits, spawn scores, recruitment, courtship valuations, or membership.
