# Phase 7C-I2 — Structural Dynasty-History Event Audit

Date: 2026-09-25

Authoritative base checkpoint: `51b5b6d090bd7678ffabcc0e6dd1da47adb3a2c7`

Scope: offline semantic selection only. No writer, gameplay change, DLL deployment, Bannerlord launch, succession experiment, save migration, or Phase 8 work occurred.

## Decision

- Structural production event selected: **YES**
- Selected Kind: **`KingdomRulingClanChanged`**
- Native event source: **`CampaignEvents.RulingClanChanged`**
- Save-schema change required: **NO**
- Gameplay implementation started: **NO**
- Runtime work performed: **NO**

The first production structural event should record Bannerlord's completed ruling-clan transition. The name deliberately describes the exact native fact. It does not claim that a ruler died, that inheritance caused the transition, or that the observing player personally made a choice.

## Why this event is structural

`CampaignEvents.RulingClanChanged` reports a change to a kingdom's ruling structure after Bannerlord has selected and applied the new ruling clan. The fact remains true regardless of which player hero observed it, and a later player heir may legitimately read it as third-person dynasty/world history.

This differs from the existing episode kinds:

- `IncidentChoice` remains person-bound and excluded from branch history.
- `IncidentOpened` remains ambiguous and excluded from branch history.
- Existing rows are not reclassified.

The event is semantically cleaner than generic "ruler succession" because `RulingClanChanged` does not prove a death, inheritance, election, abdication, or other cause. The production kind must therefore remain `KingdomRulingClanChanged`, not a causal label such as `RulerInheritedThrone`.

## Native authority and recording point

The authorizing event is:

`CampaignEvents.RulingClanChanged(Kingdom kingdom, Clan oldRulingClan)`

The new ruling clan is read from `kingdom.RulingClan` after the native event. Bannerlord remains the sole authority for the political transition.

The future writer should be invoked from the existing accepted-event path in `KingdomContinuityLedger.OnRulingClanChanged`, only after its current guard has established a genuine transition:

`record.CurrentRulingClanId != newRulingClanId`

This reuses the ledger's already-proven initialization/duplicate protection instead of adding a competing event subscriber or inferring succession from polling. ClanAI may observe and serialize the resulting fact only. It must not call political actions, change leaders, change ruling clans, or affect selection scores.

## Exact proposed episode contract

### Kind

`KingdomRulingClanChanged`

This is the only production kind selected by this audit.

### Required subject identity

The writer must preserve these IDs at event time:

- `kingdomId`: `kingdom.StringId`;
- `oldRulingClanId`: `oldRulingClan.StringId`, or an explicit empty/none value when native data supplies no former clan;
- `newRulingClanId`: `kingdom.RulingClan.StringId`, or an explicit empty/none value when leadership is vacant;
- `oldRulerHeroId`: `oldRulingClan.Leader.StringId` when available;
- `newRulerHeroId`: `kingdom.RulingClan.Leader.StringId` when available;
- `successionOrdinal`: the post-increment `KingdomContinuityRecord.SuccessionCount` for the accepted transition.

Names may be retained for display, but IDs are authoritative provenance. A leader ID is descriptive and must not replace the ruling-clan IDs because the native event concerns ruling-clan authority.

### BranchId

Use the current `DynastyMindSeed.BranchId` exactly as other dynasty episodes do. It identifies the player's continuing save branch at observation time. It is never rewritten on heir transition, and no row is copied to an heir.

### ActorId and ActorName

Use the then-current `Hero.MainHero` ID/name as **observer provenance only**. Actor identity is not the subject of the event and must not participate in branch-history eligibility. The history receipt must continue to display the original actor provenance and `personalMemory=false`.

The future writer should return without recording if a stable current-main-hero ID is unavailable; it must not invent an actor or rewrite ownership later.

### ContextId and ContextName

- `ContextId`: `kingdom:` plus the stable `kingdomId`.
- `ContextName`: the kingdom's native display name at event time, with the existing unknown-name fallback where required.

This makes the kingdom the episode context rather than either ruler or observer.

### Source

Exact value: `CampaignEvents.RulingClanChanged`

### Detail payload

Use one deterministic invariant-culture payload containing:

`kingdomId=<id>;oldRulingClanId=<id-or-none>;newRulingClanId=<id-or-none>;oldRulerHeroId=<id-or-none>;newRulerHeroId=<id-or-none>;successionOrdinal=<integer>`

The payload contains identities and the accepted transition ordinal only. It must not infer cause, legitimacy, inheritance, death, election, or player intent.

### Deduplication identity

The semantic identity is:

`BranchId | KingdomRulingClanChanged | kingdomId | successionOrdinal | oldRulingClanId | newRulingClanId`

The future implementation should reject an already-recorded identity both within the current session and against restored episode rows. The ledger's existing native-state guard remains the first defense; episode-level deduplication makes the writer idempotent without suppressing a later legitimate transition back to a previously ruling clan because that transition has a different succession ordinal.

## Persistence and retrieval

Existing D2 fields are sufficient:

- `BranchId`, campaign time, observation UTC, actor provenance, Kind, context, Source, and Detail already carry the complete contract;
- `OptionIndex=-1` and `OptionText=""` remain appropriate for a non-choice structural event;
- no field, row version, save key, migration, ActorId rewrite, BranchId rewrite, or row duplication is required.

The implementation checkpoint must add `KingdomRulingClanChanged` to both:

1. the accepted production-kind logic used by D2 import; and
2. the closed `IsBranchHistoryKind` allowlist.

Adding it to only one table must fail deterministic validation. D1/D2 shape and D2 export remain unchanged. Old rows retain their original semantics.

## Player legibility

The event should render as third-person history, for example:

"In `<kingdom name>`, ruling authority passed from clan `<old clan>` to clan `<new clan>`."

The receipt must identify the original player hero as observer/provenance, not speaker or decision-maker. Vacant old/new leadership must be shown plainly rather than converted into an invented ruler. The UI must not say "you chose," "you remember," or imply that the current heir personally witnessed the event.

## Deterministic test plan

The later writer implementation should add pure/in-memory tests proving:

1. An accepted native transition produces one `KingdomRulingClanChanged` descriptor with the exact Kind, Source, branch, subject IDs, ordinal, context, and observer provenance.
2. The same semantic identity is rejected on a repeated call.
3. A later legitimate transition between the same clans with a higher ordinal is not rejected.
4. A no-change callback (`recorded new clan == native new clan`) produces no episode.
5. Missing kingdom ID or stable main-hero ID produces no episode.
6. The structural row is retrievable across actor IDs in the same branch.
7. Different-branch and future-time structural rows remain excluded.
8. The receipt preserves the original actor ID/name and labels the result historical/non-personal.
9. `IncidentChoice` and `IncidentOpened` remain excluded from branch history.
10. Personal retrieval semantics and tie behavior remain unchanged.
11. D1 import and existing D2 `IncidentOpened`/`IncidentChoice` round trips remain unchanged.
12. A D2 `KingdomRulingClanChanged` row round-trips without a new field or schema tag.
13. Unknown kinds remain rejected.
14. Static invariants prove no political/gameplay mutation API is introduced.

## Future runtime proof plan

Do not manufacture a succession and do not rerun the Phase 7B Nemos hunt solely for this proof. At the next suitable guarded campaign or natural native ruling-clan transition:

1. capture the existing kingdom-continuity `KINGDOM_CONTINUITY_SUCCESSION` evidence and the new structural episode record from the same native callback;
2. verify exactly one structural row with matching kingdom, old/new clan IDs, ordinal, campaign time, and observer provenance;
3. save and reload through the already-proven guarded flow;
4. verify the D2 row restores unchanged;
5. after a native player-heir transition when one is independently available, verify the heir's personal retrieval does not expose the former actor's rows;
6. verify branch-history retrieval exposes the structural row as third-person history with the original actor provenance;
7. verify no duplicate structural row, succession notice, or native political mutation occurs.

A natural ruling-clan transition without a player-heir transition can prove writer correctness and save/load persistence. Cross-heir visibility requires a later native heir transition but does not justify forcing one.

## Next milestone

The justified next implementation milestone is a separate, bounded **Phase 7C-I3 structural writer checkpoint**:

- add only the `KingdomRulingClanChanged` writer at the guarded kingdom-continuity callback;
- accept that Kind in unchanged D2 serialization;
- add it to the branch-history allowlist;
- implement deterministic writer/deduplication/compatibility tests;
- make no political or gameplay mutation.

No part of that implementation is included in this audit commit.

