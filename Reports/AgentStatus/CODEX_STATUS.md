# Codex Status

## Current task
Phase 2B — ruler recruitment / clan courtship (ROADMAP.md).

## Last completed checkpoint
Phase 2B native recruitment surface and preregistered protocol recorded in commit `9766a003bb02fbb44e53e9c5bf8e5c9c6921ab2d`. The first ruler-courtship implementation candidate is implemented and build-proven; runtime validation is next.

## Current state
IN PROGRESS

## Current candidate
`v0.22A-ruler-courtship-native-v1`. The DLL has been built locally but has not been deployed.

## Proven / observed result
- Implemented: an NPC ruling clan scans eligible independent clans, chooses the highest native `GetScoreOfKingdomToGetClan`, evaluates both native join barter values, and invokes only `ExecuteAiBarter` for positive native combined value.
- Build-proven: `dotnet build src/ClanAI/src/ClanAI/ClanAI.csproj -c Release` succeeds with 0 errors and the inherited `System.ValueTuple` warning.
- Runtime-observed / causal / boundary-crossing / committed in-world: no Phase 2B behavior has been tested.
- The 2B protocol requires native candidate eligibility, native clan and kingdom values, positive combined surplus, `ExecuteAiBarter`, and post-state confirmation. No direct faction transfer or synthetic score/relation manipulation is allowed.

## Current blocker / uncertainty
The implementation has not been deployed or exercised in campaign. Natural eligible-independent-clan availability and positive native combined surplus are unknown.

## Local work warning
No known unrelated local work. The Phase 2B implementation checkpoint should be clean after its focused commit.

## Exact next action
Commit the build-proven candidate. Then, with Bannerlord stopped, preserve a verified rollback, deploy the candidate, and run the preregistered campaign protocol on a separate test fixture. Do not add custom bonuses before characterizing this base loop.
