# LW2 land-zone WIP handoff — 2026-09-30

**State: unverified work in progress.** Authoritative `main` at the start of this handoff was `c244bea8e39feaba7be43d5bee00c88442ab9a1c`. No untested gameplay code has been committed to `main`.

## Local changes preserved

- `src/ClanAI/src/ClanAI/KingdomLandControlPolicy.cs` — pure nearby-fortification classifier, 5 map-unit initial radius, 8-result local query cap, explicit unknown/independent/ambiguous/sea states, and directional closure predicate.
- `src/ClanAI/src/ClanAI/KingdomLandControlZoneNativeAdapter.cs` — native `Settlement.StartFindingLocatablesAroundPosition` adapter, capped at 8 returned settlements plus one overflow probe; checks land/sea navigation validity and fails open for ambiguous points.
- `Tests/KingdomBorders/KingdomBorderPolicyTests.csproj` — links the pure classifier, but no classifier assertions have been added or run.

These files are not yet validated against the exact installed API by a build. No tests, Release build, package, or runtime/game check was performed for this WIP. Do not deploy it.

## Design limits and resume steps

The native adapter labels a **local heuristic control zone** from nearby fortifications and their live `OwnerClan.Kingdom`. It does not create political geometry, infer enclave ownership from surrounding kingdoms, or assign sovereignty to open water. The 5-unit radius is an initial conservative constant, not runtime-calibrated.

Active-route integration is **not implemented**. The retained native evidence establishes passive path reads, but does not prove a bounded complete route scan or consistent land/sea tagging for every route point. Do not convert this classifier into route rejection until that seam is proven. Current `main` remains the previously verified destination-only, default-open filter.

Next: compile the adapter against installed v1.5.3 references; add pure tests for ownership transfer, enclaves, ties, independent/unowned zones, query exhaustion, and sea/unknown fail-open; then inspect the production route boundary narrowly. Reconcile `main` before publishing any tested advancement. Preserve RC1, C2 deployment/save history, save keys/schema, and existing destination-filter behavior.
