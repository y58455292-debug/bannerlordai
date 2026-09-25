# Codex Status

## Current checkpoint
Phase 4A same-hero defeat-to-native-recreation runtime characterization is closed with a **bounded null**. The single retained natural run advanced 164.28120388884 campaign hours inside the 168-hour maximum. No complete qualifying same-hero chain was observed. Stop here; do not extend or repeat the run and do not start Phase 4B.

On resumption, GitHub main was verified at `0b58b55d7ffd42381340a61a510ba4eaa040754b`. The already-completed runtime artifacts were found locally, verified against the original game log, and checkpointed rather than launching a second characterization. Local staging was not treated as authoritative without verification.

## Runtime result
The 93-line scoped observer capture exactly matches the original game log from its session reset: 1 reset, 28 `PHASE4A_LINK_DEFEAT_UNRESOLVED`, 28 `PHASE4A_LINK_PARTY_DESTROYED`, and 36 `PHASE4A_LINK_CREATION` records. All creations have `linked=False` and classification `GenericNativePartyCreation`. There are zero accepted defeat-link records, linked creations, first-settlement link boundaries, complete chains, or `PHASE4A_LINK_OBSERVER_ERROR` records.

Every unresolved defeat record reports `reason=no-native-leader-at-defeat`. Cached hero IDs and later destruction evidence do not satisfy the existing native-leader-at-defeat identity rule. Mikri's later generic creation with 28 regular troops is not promoted to a post-defeat recreation claim. No claim of free respawn troops, zero-troop linked recreation, or linked-but-incomplete recreation is supported by this run.

## Safety and unchanged candidate
The retained deployment record documents Bannerlord closed before deployment and rollback SHA-256 `5CA45B7AE095E9B3F6F77E06FCCA287414FBAC7D345648AAAD58F39EA1919BF6`. The tested candidate and installed DLL remain SHA-256 `0FE236EDA21FFED93F14003A588C5A97712A5CED66341413612244C40BF1F755`.

The run used the protected fixture without saving, acknowledged PAUSE, issued `EXIT_NOSAVE`, and closed Bannerlord. Fresh verification confirmed fixture SHA-256 `A91F15BF1403F1D29F942C56A1162F431113942CDACCAFE52F4D80303B4CB427` and modification time `2026-09-24T17:20:12.2384633Z` unchanged, rollback intact, installed Strategic Commitment `Mode=Observe`, and Visual War marker absent. No observer/source/configuration changes, forced battles, forced recreations, or gameplay mutations were made for this characterization or its checkpoint recovery.

## Preserved checkpoints
Phase 3 Strategic Commitment remains a bounded runtime null. Phase 4A's first characterization remains accepted: Kulyat 37->69 with +28 recruitment-supported and +4 unresolved; Iara's +7 party/-7 garrison supported a withdrawal; generic native creations were not proven post-defeat respawns. These proofs were not reopened.

The linked observer's existing offline results remain unchanged: 35 linkage policy checks; wiring/original-observer preservation and no-mutation/standalone invariants; the prior 20 recovery policy checks and both invariants; Release 0 errors with the inherited System.ValueTuple warning. No rebuild was necessary for this evidence-only checkpoint.

## Evidence and limits
- `Reports/Manpower/PHASE4A_LINKED_RECREATION_RUNTIME_RESULT.md`
- `Reports/Manpower/evidence/phase4a_linked_recreation_runtime_20260925.txt`
- Existing seam/validation: `Reports/Manpower/PHASE4A_LINKED_RECREATION_OBSERVER_RESULT.md`

The new evidence file retains exact selected event lines, full-capture counts/hash, deployment and cleanup records, control chronology, and fresh verification. The complete 93-line capture remains in the recorded local evidence directory; it was verified line-for-line, not silently relabeled. This null constrains what this run and identity seam proved; it does not establish that native post-defeat recreation never happens.

Final product direction is unchanged: a standalone, installable, offline mod with no runtime dependency on ChatGPT, Codex, Desktop Commander, TestRunner, watchdogs, or development-machine absolute paths. Development harnesses are evidence-only. This checkpoint changes documentation/evidence only and adds no release/runtime dependency.
