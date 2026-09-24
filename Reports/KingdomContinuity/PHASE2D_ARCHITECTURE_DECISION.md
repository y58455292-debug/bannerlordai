# Phase 2D architecture decision — native continuity before successor formation

Date: 2026-09-24
Status: Sol architecture resolved; implementation intentionally deferred to Luna

## Decision

Phase 2D will be built as an event-driven continuity layer over Bannerlord's native political lifecycle. ClanAI may remember, explain, and later offer bounded identity choices after native political facts exist. It must not create those facts by directly assigning rulers, settlements, kingdoms, wars, or faction membership.

The architecture separates three cases that must not be conflated:

1. **Continuing kingdom.** The kingdom remains active and Bannerlord changes its ruling clan through `KingSelectionKingdomDecision` and `ChangeRulingClanAction`. This is succession inside the same political entity, not a new kingdom.
2. **Destroyed kingdom.** Bannerlord discontinues and deactivates the kingdom. Its destruction is final for that object. ClanAI records the ending but does not revive it or intercept destruction.
3. **Potential successor lineage.** A different, naturally landed political actor may later establish a distinct identity. On the supported native build, a rebellion is the clearest lawful origin: Bannerlord creates the rebel clan, gives it the rebelling settlement through `ApplyByRebellion`, declares its war, and matures it into an ordinary clan after 30 or 60 days if it still holds territory. Maturation is not itself kingdom creation.

No ordinary NPC kingdom-creation loop has been identified. Therefore this decision does not authorize ClanAI to create a kingdom. Successor formation remains a later, separately reviewed slice after a natural landed candidate and a native-safe creation boundary are demonstrated.

## Native authority matrix

| Concern | Authority and Phase 2D rule |
|---|---|
| Ruler succession | Bannerlord alone selects and applies the ruling clan. ClanAI observes `RulingClanChanged`; it never replaces the election or calls `ChangeRulingClanAction` for proof. |
| Kingdom destruction | Bannerlord's discontinuation and `DestroyKingdomAction` remain final. ClanAI records the terminal event and performs no resurrection. |
| Landless independence | Preserve the native 28-day destruction boundary. Do not extend, reset, or bypass it. Courtship may naturally resolve independence before expiry; a null may end in destruction. |
| Rebel maturation | Bannerlord creates the clan, settlement ownership, war, parties, and later removes rebel status if the clan retains a settlement. ClanAI may observe maturation as successor provenance; it must not grant the settlement or force maturation. |
| Settlement ownership | Only native ownership-change actions establish legitimacy. A successor candidate must already hold a town or castle through a recorded native event. |
| Wars | Existing native wars remain attached to their native factions. A continuity record may reference them but cannot copy, clear, or synthesize them. Any future new polity must use a reviewed native initialization path. |
| Faction membership | Native actions and barters remain final. Continuity metadata never implies membership and never calls a direct transfer. |
| Political identity | Kingdom object identity and a persisted continuity record are distinct from display names. A future rename may use `Kingdom.ChangeKingdomName` only after its trigger and naming policy are separately approved and proven save-safe. |
| Culture | `Kingdom.Culture`, clan culture, hero culture, settlement culture, and troop culture are never rewritten by continuity or naming. Culture is recorded as provenance, not derived from the current ruler. |

## Persistent continuity model

Add one save-synchronized record per observed kingdom identity, keyed by the native kingdom object or stable `StringId` according to the repository's established save pattern. The minimum durable fields are:

- schema version;
- native kingdom ID;
- original name and current observed name;
- immutable recorded culture ID;
- current ruling clan ID;
- native succession count;
- last native succession campaign time;
- destroyed flag and native destruction campaign time;
- optional origin kind: `native_existing`, `native_created`, or `native_rebel_lineage` only when directly evidenced.

The record is descriptive state, not a second faction model. On load it must reconcile conservatively:

- create a missing record for an active native kingdom;
- update the observed ruler only from current native state;
- never restore a missing/destroyed kingdom;
- never write recorded culture back into Bannerlord;
- preserve unknown/null IDs rather than guessing;
- version fields additively and tolerate records from earlier schema versions.

This allows ruler and clan generations to change while the political entity's history remains coherent. Personal memories continue to belong to actual heroes/clans; structural continuity facts may outlive individuals.

## Identity and naming policy

Phase 2D v1 does **not** rename kingdoms. A ruler change is not automatically a successor state, and automatic dynasty-based renaming would create churn and confuse political identity with culture.

A later rename slice requires all of the following:

- a native event that justifies political re-identification rather than ordinary succession;
- a deterministic, player-legible naming rule;
- preservation of the kingdom's culture and object identity;
- collision handling and localization-safe `TextObject` construction;
- save/load proof and exactly-once application;
- no claim that a rename created a new faction.

## Successor-state eligibility for later work

A future successor proposal may be evaluated only if all are true:

- the predecessor kingdom was natively destroyed or the candidate is otherwise a distinct native faction;
- the candidate clan is alive, non-bandit, non-minor, and holds a town or castle obtained through a recorded native ownership event;
- if rebel-origin, it has natively matured and still holds its settlement;
- it is not inside the ordinary 28-day landless destruction path;
- no direct grant, forced membership, timer change, or synthetic war is needed;
- the supported game exposes a native-safe kingdom initialization path whose downstream diplomacy, decisions, armies, policies, save registration, and UI behavior have been audited.

Failure of any condition is an honest null. A destroyed kingdom need not have a successor.

## One Luna implementation slice

Implement **Phase 2D-L1: native succession continuity ledger and player notice**.

Scope:

1. Add a small `KingdomContinuityBehavior` registered with the campaign.
2. Subscribe only to native `RulingClanChanged`, `KingdomDestroyedEvent`, `KingdomCreatedEvent`, and load/new-game reconciliation events needed by the established behavior pattern.
3. Persist the minimum continuity record above with an explicit schema version.
4. On a native ruling-clan change, increment exactly once, retain the original culture ID, record old/new ruling clans, and show one concise player-facing notification naming the kingdom and new ruling clan/leader.
5. On native kingdom destruction, mark the record terminal and show at most one concise player-facing notice.
6. On save/load, reconcile without emitting duplicate notifications or mutating native political state.
7. Add focused unit/invariant coverage for duplicate-event handling, load reconciliation, culture immutability, null rulers, and terminal destroyed records.
8. Build and perform one bounded runtime observation of either a naturally occurring native succession/destruction event or, if none occurs in the timebox, a committed null plus save/load proof of initialized records.

Explicit exclusions:

- no kingdom creation;
- no kingdom rename;
- no ruler selection;
- no settlement or faction transfer;
- no war mutation;
- no 28-day timer changes;
- no rebel acceleration;
- no synthetic event used as positive gameplay evidence.

This is gameplay-capable because a normal native succession or destruction becomes persistent and player-legible across save/load. It is also the smallest foundation that later identity or successor work can safely consume without inventing political facts.

## Acceptance boundary

L1 is complete only when the behavior builds, its record survives save/load, a native event produces exactly one correct notice and continuity update, and no prohibited native state changes occur. If no natural event appears in the bounded runtime window, retain the null and leave L1 at build/save-load proof rather than manufacturing a succession.
