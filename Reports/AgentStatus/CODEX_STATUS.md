# Codex Status

## Current task
Phase 2B — ruler recruitment / clan courtship (ROADMAP.md).

## Last completed checkpoint
Phase 2B native recruitment surface and preregistered protocol recorded in commit `9766a003bb02fbb44e53e9c5bf8e5c9c6921ab2d`.

## Current state
BLOCKED

## Current candidate
None. No Phase 2B implementation candidate or DLL has been built or deployed. The current main source identifies itself as `v0.21M3-defection-leave-carry-v1`.

## Proven / observed result
- Implemented: Phase 2B design/protocol documents describe the native `JoinKingdomAsClanBarterable` and ruler-side recruitment opportunity.
- Build-proven: no Phase 2B code exists to build.
- Runtime-observed / causal / boundary-crossing / committed in-world: no Phase 2B behavior has been tested.
- The 2B protocol requires native candidate eligibility, native clan and kingdom values, positive combined surplus, `ExecuteAiBarter`, and post-state confirmation. No direct faction transfer or synthetic score/relation manipulation is allowed.

## Current blocker / uncertainty
This execution has no repository checkout in the workspace, no .NET SDK, and no Bannerlord assemblies or runtime. The GitHub connector provides repository read/write access but cannot provide a local build or campaign runtime here. The native surface is documented in `Reports/ClanRecruitment/PHASE2B_NATIVE_RECRUITMENT_SURFACE.md`; the accepted test criteria are in `Reports/ClanRecruitment/PHASE2B_RECRUITMENT_PROTOCOL.md`.

## Local work warning
No important uncommitted local work. The workspace contains no BannerlordAI Git checkout.

## Exact next action
In an environment with the project checkout, .NET SDK, supported Bannerlord assemblies, and campaign runtime, fetch/pull current `main`; inspect `SubModule.cs`, campaign behavior registration, and available tests; then implement the smallest ruler-side consideration that selects the best eligible independent clan by native `GetScoreOfKingdomToGetClan`, evaluates both native join barter values, and invokes only `BarterManager.ExecuteAiBarter` for positive combined value. Build before committing. Do not add custom bonuses before characterizing this base loop. Run the preregistered campaign protocol only after the implementation and protocol are committed.
