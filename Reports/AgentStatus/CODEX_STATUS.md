# Codex Status

## Current checkpoint

Phase 4A same-hero defeat-to-native-recreation runtime characterization remains **closed with the existing bounded null**: 164.28120388884 campaign hours, zero accepted defeat links, zero linked recreations, and zero complete chains. Do not rerun or extend that characterization and do not start Phase 4B.

A narrow follow-up **offline defeat-identity boundary review is now complete**. Native sequencing proves why the prior observer lost identity: non-player battle result application can remove a defeated party leader before `MapEventEnded`. A minimal observation-only fix now captures exact native battle participants earlier through `MapEventStarted` and `OnPartyAddedToMapEventEvent`.

No runtime campaign was launched for this checkpoint. Runtime same-hero recreation proof remains unproven.

## Defeat identity seam

Each early participant capture binds:

- exact `MapEvent` reference;
- exact `PartyBase` reference;
- exact `MobileParty` reference;
- native battle side;
- stable leader hero `StringId`;
- capture hour/source.

At `MapEventEnded`, defeat identity is accepted only if the cached battle, `PartyBase`, `MobileParty`, and side match the exact defeated-side participant. If a live end-time hero identity still exists it must match the captured `StringId`. Another battle, another party instance, side mismatch, clan/name-only state, or later destruction alone cannot establish the defeat.

The downstream recreation rules are unchanged: ordinal same-hero StringId, distinct native recreated party, prior old-party destruction/absence, time bound, creation roster, and first-settlement boundary remain required.

## Native evidence

Reviewed `TaleWorlds.CampaignSystem.dll` SHA-256:

`5B23C3E36D7A5D6D47C47EAB075E9E2B1CC8D1AA53C79E135FA2B5EF434EBC5F`

Offline reflection/decompilation established:

- `MapEventStarted` exposes `MapEvent, PartyBase, PartyBase`;
- `OnPartyAddedToMapEventEvent` exposes a joined `PartyBase`;
- native event initialization assigns sides before the start callback;
- defeated-member result processing can call `MobileParty.RemovePartyLeader()`;
- `MapEventEnded` is dispatched later.

See `Reports/Manpower/PHASE4A_DEFEAT_IDENTITY_BOUNDARY_REVIEW.md` and its evidence file.

## Validation

Final results:

```
PASS Phase 4A same-hero recreation policy checks=43
PASS Phase 4A same-hero recreation wiring and original-observer preservation
PASS Phase 4A same-hero recreation no-mutation and standalone invariant
PASS Phase 4A recovery policy tests checks=20
PASS Phase 4A recovery wiring
PASS Phase 4A recovery no-mutation/standalone invariant
1 Warning(s)
0 Error(s)
```

The warning is the inherited MSB3277 `System.ValueTuple` conflict. Release DLL SHA-256 for this observer-only source:

`0D23F4A66E0A1E4413E879C97E963C7DB923D8D6B4421291105D8565120CD5A8`

No development-machine absolute runtime path, external IO, serialized observer state, gameplay mutation, or development-tool runtime dependency was introduced.

## Preserved evidence

The completed bounded-null report remains authoritative for its run:

- `Reports/Manpower/PHASE4A_LINKED_RECREATION_RUNTIME_RESULT.md`
- `Reports/Manpower/evidence/phase4a_linked_recreation_runtime_20260925.txt`

The earlier Phase 4A recovery characterization also remains accepted: Kulyat 37->69 with +28 recruitment-supported and +4 unresolved; Iara's +7 party/-7 garrison supported a withdrawal; generic native creations were not proven post-defeat respawns.

New offline boundary evidence:

- `Reports/Manpower/PHASE4A_DEFEAT_IDENTITY_BOUNDARY_REVIEW.md`
- `Reports/Manpower/evidence/phase4a_defeat_identity_boundary_review_20260925.txt`

## Stop condition

This checkpoint makes **no runtime recreation claim** and authorizes no campaign run by itself. Preserve the bounded null, preserve conservative identity semantics, and stop before Phase 4B.

Final product direction is unchanged: a standalone, installable, offline mod with no runtime dependency on ChatGPT, Codex, Desktop Commander, TestRunner, watchdogs, development-machine absolute paths, or other development harnesses.
