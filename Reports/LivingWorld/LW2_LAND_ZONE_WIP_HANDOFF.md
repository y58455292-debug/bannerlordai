# LW2 land-control-zone foundation checkpoint — 2026-09-30

**Model: implemented and tested. Native adapter: compiles against installed Bannerlord v1.5.3 references. Active gameplay and route enforcement: not implemented. Not packaged, deployed, or runtime-tested.**

This work resumed from WIP commit `2313acb5310fa1e929c5558171c3c84e123e7bef`, based on authoritative `main` `c244bea8e39feaba7be43d5bee00c88442ab9a1c`. The updated checkpoint is published on `main`; the earlier WIP branch is retained as history.

## Implemented

- `src/ClanAI/src/ClanAI/KingdomLandControlPolicy.cs` — pure nearby-fortification classifier; initial 5 map-unit radius; 8-result cap; deterministic nearest/tie behavior; explicit unknown, ambiguous, independent, and sea states; explicit directional-closure predicate.
- `src/ClanAI/src/ClanAI/KingdomLandControlZoneNativeAdapter.cs` — on-demand native `Settlement.StartFindingLocatablesAroundPosition` query, at most 8 returned settlements plus one overflow probe; current `OwnerClan.Kingdom` is read live. It requires land-valid/sea-invalid native navigation for a point and fails open on overlap, unknown, sea, or exhausted search.
- `Tests/KingdomBorders/Program.cs` and its project file — tests the pure model and closure predicate. The native adapter is compile-checked, not executed in a campaign fixture.

The classifier uses actual local fortification ownership, so an enclave follows its owning clan's current kingdom rather than the surrounding kingdom. Same-owner exact-distance ties select the ordinal-lowest settlement ID; different-owner ties are ambiguous. The native spatial locator excludes entries at exactly its radius boundary. These are heuristic control zones, not political polygons; 5 units are not runtime-calibrated.

## Validation

- `Tests/KingdomBorders`: **40 PASS** (14 existing destination-policy cases, 26 land-zone/route-policy cases).
- Release `netstandard2.0` build against installed v1.5.3 assemblies: **0 errors**, 1 inherited `System.ValueTuple` `MSB3277` warning. Informational build identity: `v0.1.1-THEMATRIX-land-zone-foundation-dev`. Local built DLL SHA-256: `38F80B8E4083E73813F5DFACAE791BE7F95FC31ECB452BAAAF1A77FBE540FEAF`.
- Exact command output and scope: [validation receipt](evidence/lw2_land_zone_foundation_20260930.txt).

## Explicit limits

The model and native adapter are not called by the AI visit patch or another runtime behavior. The existing distributed candidate remains the destination-only filter. No route-crossing decision, arrival restriction, or other actor coverage is claimed.

The installed native route evidence permits passive reads of `MobileParty.Path`, but native path points contain no sovereign identity. A full path query has no work-limit parameter; distance-only queries provide no waypoints; land/sea transition semantics are not established for a whole stored path. No full-path or private movement computation was added. A safe active-route integration remains unresolved.

No save key/schema, Home Assignment behavior, 1.25 factor, RC1 artifact, C2 installation, original save, or C3 history was changed. No package was created; no game launch, deployment, save operation, or runtime test occurred. Continue only after a bounded production route seam is justified; do not promote this classifier as complete border behavior.
