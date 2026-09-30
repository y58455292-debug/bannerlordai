# LW2 land-control-zone and stored-route observer checkpoint — 2026-09-30

**The pure model and native adapter are implemented; an Evidence-profile observer samples the stored route passively. Focused/preservation tests pass and the Release build succeeds. No border enforcement, campaign runtime validation, package, or deployment is claimed.**

This work resumes the retained WIP commit `2313acb5310fa1e929c5558171c3c84e123e7bef` and builds on authoritative `main` `0d78ccfc28546966c4e4fa5ca89e18aaff7ae790`.

## Current observer checkpoint

- `src/ClanAI/src/ClanAI/StoredRouteObserver.cs` is called from `ClanAIPostVanilla.Postfix` only when the Evidence profile is enabled. It independently checks for an active, ordinary NPC lord party, excluding the player, armies, caravans, attached/battle/siege parties, disbanding/waiting parties, prisoners, stopped/disabled AI, children/templates, and invalid leaders. Parties currently at sea are not excluded; each route point is checked separately.
- A process-local budget admits at most 4 distinct party IDs per campaign hour and suppresses duplicate IDs. It resets when campaign session resets and on hour changes. This is bounded sampling, not a representative scan of all parties.
- It reads only `MobileParty.Path` by indexed access, starting at `PathBegin`, and reads no more than 8 points. Native v1.5.3 source shows movement consumes `Path[PathBegin]` and advances `PathBegin` as points are reached. Therefore these are stored-path samples, not proof of a future route or that Bannerlord will traverse it unchanged.
- Each point is validated independently under native land and sea navigation modes; points valid under both/neither fail open as unknown. For a kingdom-owned sample, the selected fortification is resolved by StringId and its current owner Kingdom is read directly. Closure is checked only in the direction `zoneKingdomId>visitorKingdomId`; war exempts only war with that sampled zone Kingdom.
- The observer does not call `BlocksVisit` or affect its counters; it does not scan all actors/settlements/kingdoms, perform a path computation, mutate the path, change candidate/score/target/movement, or add save data. Per-observation I/O is Evidence-gated and limited by the 4-party hourly quota.
- Evidence lines report `closed_zone_sample_observed`, `no_closed_zone_in_samples`, `unavailable`, or `partial`, plus path size/begin, sample count, truncation, sea/unknown samples, sampled zone/settlement IDs, directional closures, zone-war skips, errors, and cumulative quota/duplicate skips. “No closed zone in samples” does not prove the entire route is open. The 5-unit radius and at most 8 nearby settlements plus one overflow probe bound calls/results, not native execution time.

## Implemented

- `src/ClanAI/src/ClanAI/KingdomLandControlPolicy.cs` - pure nearby-fortification classifier; initial 5 map-unit radius; 8-result cap; deterministic nearest/tie behavior; explicit unknown, ambiguous, independent, and sea states; explicit directional-closure predicate.
- `src/ClanAI/src/ClanAI/KingdomLandControlZoneNativeAdapter.cs` - on-demand native `Settlement.StartFindingLocatablesAroundPosition` query, at most 8 returned settlements plus one overflow probe; current `OwnerClan.Kingdom` is read live. It requires land-valid/sea-invalid native navigation for a point and fails open on overlap, unknown, sea, or exhausted search.
- `src/ClanAI/src/ClanAI/StoredRouteObserverPolicy.cs` and `StoredRouteObserver.cs` - pure window/quota/outcome logic plus Evidence-gated passive indexed sampling of the existing active path.
- `Tests/KingdomBorders` - tests the pure model, closure predicate, route window/quota/outcome. The native adapter/observer are compile-checked, not executed in a campaign fixture.

The classifier uses actual local fortification ownership, so an enclave follows its owning clan's current kingdom rather than the surrounding kingdom. Same-owner exact-distance ties select the ordinal-lowest settlement ID; different-owner ties are ambiguous. The native spatial locator excludes entries at exactly its radius boundary. These are heuristic control zones, not political polygons; 5 units are not runtime-calibrated.

## Earlier foundation validation at 0d78ccf

- `Tests/KingdomBorders`: **40 PASS** (14 existing destination-policy cases, 26 land-zone/route-policy cases).
- Release `netstandard2.0` build against installed v1.5.3 assemblies: **0 errors**, 1 inherited `System.ValueTuple` `MSB3277` warning. Informational build identity: `v0.1.1-THEMATRIX-land-zone-foundation-dev`. Local built DLL SHA-256: `38F80B8E4083E73813F5DFACAE791BE7F95FC31ECB452BAAAF1A77FBE540FEAF`.
- Exact command output and scope: [foundation validation receipt](evidence/lw2_land_zone_foundation_20260930.txt).

## Current validation

- `Tests/KingdomBorders`: **77 PASS**: 14 destination-policy, 31 land-zone/ownership identity, and 32 stored-route observer policy cases.
- Preservation suites: HomeAssignmentRuntime/roster/adapter **104 PASS**; HomeAssignment/D1 **43 PASS**; Phase 3 Home Responsibility **10 PASS**.
- `netstandard2.0` Release build against installed Bannerlord v1.5.3 references: **0 errors**, 1 inherited `System.ValueTuple` MSB3277 warning. Identity `v0.1.2-THEMATRIX-stored-route-observer-dev`; final local DLL SHA-256 `1279BDF8881C6BD6B726AEAA54FDF3143B994B0880802E53628099D1AAB15DF7`.
- Exact commands and validation limits: [stored-route observer receipt](evidence/lw2_stored_route_observer_20260930.txt).

The pure policy tests cover sample-window bounds, campaign-hour quota/reset/duplicate logic, both/neither native navigation validity, exception fail-open, sea and unknown outcomes, directional closure, zone-war versus unrelated-war behavior, identity reporting, and path truncation. They do not call Bannerlord navigation or the native settlement locator in a campaign fixture. The Release compile checks the native adapter and runtime observer signatures only.

## Explicit limits and handoff

The observer does not reject route candidates, score destinations, or implement passage restrictions. It does not establish the complete active route, and no campaign runtime result is known. It is not packaged or deployed. Sparse samples cannot prove that an entire route is open or closed. Physical enforcement still needs candidate-to-route association and a safe native reselection policy; do not infer those from this observer.

The installed native route evidence permits passive reads of `MobileParty.Path`, but native path points contain no sovereign identity. A full path query has no work-limit parameter; distance-only queries provide no waypoints; land/sea transition semantics are not established for a whole stored path. No full-path or private movement computation was added. A safe active-route integration remains unresolved.

No save key/schema, Home Assignment behavior, 1.25 factor, RC1 artifact, C2 installation, original save, or C3 history was changed. No new package was created; no game launch, deployment, save operation, or runtime test occurred. The existing destination-only package remains unchanged and distinct from this observer build.
