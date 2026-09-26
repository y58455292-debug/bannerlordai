# Phase 7C — DynastyBranchEpisodeMemory continuity-semantics audit

Date: 2026-09-25 UTC  
Authoritative source checkpoint: `0d94f508e516cf4c7c617de832cce1a75e160c49`  
Scope: offline semantic audit only; no Bannerlord launch, DLL deployment, save mutation, gameplay implementation, or succession run.

## Result

- Phase 7C audit: **COMPLETE**
- Semantic fix identified: **YES**
- Gameplay implementation: **NOT STARTED**
- Save-schema change required: **NO**

Phase 7B's bounded Nemos succession null is preserved. No new succession hunt was started.

The safe fix is to keep all existing actor-specific retrieval behavior unchanged and add a separate, explicitly named branch-history retrieval path whose eligible episode kinds are allowlisted by semantics. Existing rows remain owned by their original `ActorId`; no row is duplicated, rewritten, reassigned, or interpreted as the heir's first-person memory.

Current data does not contain an episode kind that can safely be exposed as cross-actor branch history. `IncidentChoice` is person-bound. `IncidentOpened` is semantically ambiguous because the row records only a generic title/context/source and cannot distinguish a personal encounter from a realm-level occurrence. The ambiguity must remain visible rather than being guessed away.

## Sources inspected

Only the task-authorized material and direct source call chain were inspected:

- `PROJECT_LAW.md`
- `Reports/GenerationalContinuity/PHASE7A_CONTINUITY_NATIVE_AUDIT.md`
- `Reports/GenerationalContinuity/PHASE7B_SUCCESSION_RUNTIME_RESULT.md`
- `src/ClanAI/src/ClanAI/DynastyBranchEpisodeMemory.cs`
- `src/ClanAI/src/ClanAI/DynastyMindOperatorBridge.cs`
- an exact-name scan of current ClanAI C# source to prove the direct caller inventory is complete

No Phase 4, 5, or 6 source was reopened or changed.

## Current serialized episode shape

Each row contains:

- stable episode ID;
- `BranchId`;
- campaign time and observed UTC;
- original `ActorId` and actor name;
- `Kind`;
- context ID/name;
- source and detail;
- optional choice index/text.

Current export writes `D2` rows. Import remains backward-compatible with `D1` and `D2`.

The row already has enough identity information for safe retrieval separation. The defect is query semantics, not missing persistence data.

## Episode-kind inventory and classification

Only two episode kinds are currently accepted by `TryDecode` and written by the class.

| Kind | Writer | Stored meaning | Classification | Cross-actor branch retrieval |
|---|---|---|---|---|
| `IncidentOpened` | `RecordIncidentOpened` | The current main hero was presented an incident identified by generic title/context/source; detail stores option count | **AMBIGUOUS** | **NO by default.** The row cannot prove whether the incident was personal, dynasty-wide, or world-structural. |
| `IncidentChoice` | `RecordIncidentChoice` | The current main hero selected a specific option in an incident | **PERSON-BOUND** | **NEVER as inherited memory.** It remains attributable and retrievable only for the choosing actor. |

No current kind is safely classified as **BRANCH/DYNASTY-BOUND** or **STRUCTURAL/WORLD HISTORY**.

### Why IncidentOpened remains ambiguous

An incident opening could describe:

- something only the current hero personally encountered;
- a ruler-facing event relevant to the office or dynasty;
- a realm/world event that later heirs may legitimately know as history.

The stored row does not carry a semantic scope, subject identity, kingdom/clan identity, or a trustworthy event taxonomy. Title, source, and context slug are insufficient to infer inheritance semantics. Therefore Phase 7C does not promote it to branch history.

## Writer and caller inventory

The exact-name source scan found no direct users outside these two classes.

### Writers

1. `DynastyBranchEpisodeMemory.RecordIncidentOpened(title, optionCount, source)`
   - resolves `Hero.MainHero`;
   - stamps current branch ID and current actor ID;
   - writes `Kind=IncidentOpened`;
   - returns the episode ID/status.

2. `DynastyBranchEpisodeMemory.RecordIncidentChoice(title, optionIndex, optionText, source)`
   - resolves `Hero.MainHero`;
   - stamps current branch ID and current actor ID;
   - writes `Kind=IncidentChoice`;
   - returns the episode ID/status.

Both are exposed unchanged through `DynastyMindOperatorBridge.RecordIncidentOpened` and `RecordIncidentChoice`.

### Readers

1. `BuildLatestRetrievalReceipt(retrievalContext)`
   - selects any supported kind;
   - requires current `Hero.MainHero.StringId`;
   - requires current `BranchId`;
   - excludes future campaign times;
   - returns the latest matching actor-owned episode.

2. `BuildLatestChoiceRetrievalReceipt(retrievalContext)`
   - selects only `IncidentChoice`;
   - requires current `Hero.MainHero.StringId`;
   - requires current `BranchId`;
   - excludes future campaign times;
   - returns the latest current-actor choice.

They are exposed through:

- `DynastyMindOperatorBridge.RetrieveLatestBranchEpisode`;
- `DynastyMindOperatorBridge.RetrieveLatestBranchChoice`.

Despite their bridge names, both calls currently require **current actor history**, not whole-branch history.

### Other direct access

`GenerationalContinuityPreflightBehavior` counts total episodes and episodes attributed to a specific actor through reflection for observation. It does not retrieve episode content, reinterpret ownership, or provide gameplay history to an heir.

## Caller requirements

| Caller | Actual requirement supported by current behavior | Required semantic scope |
|---|---|---|
| `RetrieveLatestBranchChoice` | Retrieve the current main hero's own latest executed choice | **Current actor history** only |
| `RetrieveLatestBranchEpisode` | Retrieve the current main hero's latest episode of either current kind | **Current actor history** under current evidence; its name is broader than its behavior |
| Generational preflight counts | Compare actor-owned count with total serialized count | Observation only; no history exposure |
| Future heir/dynasty context consumer | Not implemented | Must use a separate explicit branch/structural API, never the actor API |

No current caller demonstrably requires whole-branch episode content. No current caller requires structural history content.

## Exact defect mechanics

Before succession, suppose branch `B` has rows owned by main hero `A`:

`(BranchId=B, ActorId=A, Kind=...)`

After native player succession, `Hero.MainHero` becomes heir `H`, while the branch ID remains `B`.

Both existing retrieval methods apply:

`row.BranchId == B && row.ActorId == H`

Rows for `A` remain serialized and correctly attributed but are invisible. This is not save corruption. It is the absence of a separate branch-history query.

Changing the existing filter to branch-only would be unsafe:

- `IncidentChoice` would appear to the heir as if it were the heir's choice;
- ambiguous `IncidentOpened` rows could be misrepresented as inherited knowledge;
- existing callers expecting current-actor semantics would silently change;
- old rows would gain semantics they never declared.

The defect must be fixed by API separation, not by loosening the personal lookup.

## Selected minimal API/design

### 1. Preserve personal APIs exactly

Keep these semantics unchanged:

- `BuildLatestRetrievalReceipt`: current actor + current branch;
- `BuildLatestChoiceRetrievalReceipt`: current actor + current branch + `IncidentChoice`.

For clarity, implementation may add internal aliases such as `BuildLatestActorEpisodeReceipt`, but existing public/bridge behavior must remain backward-compatible.

### 2. Add one explicit branch-history API

Add a separately named method:

`BuildLatestBranchHistoryReceipt(string retrievalContext)`

Required selection rules:

- current `BranchId`;
- `CampaignHours <= now`;
- no `ActorId == Hero.MainHero.StringId` filter;
- include only kinds accepted by an explicit `IsBranchHistoryKind(kind)` allowlist;
- preserve original actor ID/name in the receipt as provenance;
- label the result as historical/third-person context, never personal memory;
- return empty when no eligible row exists;
- never affect scores or game state.

Expose it separately through a bridge method with an equally explicit name, for example:

`RetrieveLatestDynastyHistory`.

Do not overload or repurpose `RetrieveLatestBranchEpisode`.

### 3. Initial allowlist is empty

For current kinds:

- `IncidentChoice` -> false;
- `IncidentOpened` -> false until its semantic scope is made explicit by its writer.

An empty initial allowlist is intentional. It establishes the safe API boundary without reclassifying old data.

### 4. Future structural kinds must be explicit

A later writer may introduce a clearly named kind such as a native succession/world-history observation only after its semantics and provenance are defined. Eligibility should be determined by a closed code allowlist keyed by `Kind`, not by title text, source text, actor death, or current leadership.

The historical receipt must continue to show the original actor as observer/provenance. It must not claim that the heir experienced the event.

## Backward compatibility

### Save data

No save-schema change is required.

- Existing `D1` and `D2` imports remain unchanged.
- Existing `D2` exports remain unchanged.
- No ActorId or BranchId rewrite occurs.
- No episode duplication occurs.
- No migration is needed.
- Existing ambiguous rows remain ambiguous and excluded from branch history.
- Existing personal choices remain actor-only.

### API behavior

Existing methods and bridge entry points retain their outputs and filters.

The new branch-history method is additive. Because its initial allowlist excludes both current kinds, it cannot leak former-actor personal data.

### Standalone requirement

The policy and retrieval must remain in the offline mod. Test bridges may observe it during development, but the finished runtime behavior must not depend on Codex, TestRunner, watchdogs, command buses, external files, or network access.

## Deterministic test plan

The next implementation checkpoint should be offline and source-focused.

### Personal retrieval preservation

1. Same branch, same actor, past `IncidentOpened` -> existing actor episode retrieval returns it.
2. Same branch, same actor, past `IncidentChoice` -> existing choice retrieval returns it.
3. Same branch, former actor, current heir -> both existing personal retrievals return empty for former-actor rows.
4. Different branch -> excluded.
5. Future campaign time -> excluded.
6. Latest/tie behavior remains deterministic and compatible.

### Branch-history isolation

7. Former actor `IncidentChoice` -> excluded from branch history.
8. Former actor `IncidentOpened` -> excluded while ambiguous.
9. Current actor personal rows -> also excluded from branch history when their kinds are not allowlisted.
10. No eligible row -> empty receipt, no fallback to personal retrieval.
11. A test-only in-memory episode with an explicitly allowlisted structural kind -> selected across actor IDs within the same branch.
12. Structural receipt preserves original actor/provenance and labels history as non-personal.
13. Different branch and future structural rows -> excluded.
14. Personal retrieval never calls or falls back to branch retrieval.

### Serialization compatibility

15. Existing `D1` row imports unchanged.
16. Existing `D2` `IncidentOpened` and `IncidentChoice` rows round-trip byte/field-equivalently.
17. No new serialized field or schema tag is required.
18. Unknown kinds remain rejected unless deliberately added to the accepted-kind and semantic-scope tables together.

A completely inactive test helper may construct in-memory rows or pure descriptors. It must not add a gameplay writer or alter save data.

## Runtime proof plan

Do not rerun Nemos and do not begin another succession hunt for Phase 7C.

Runtime proof should wait until an independently justified natural player-heir transition or guarded succession fixture is available. Then:

1. before transition, record one personal choice for actor `A` and, only if a future explicitly structural writer exists, one structural history row;
2. save through the existing guarded process;
3. after native succession to heir `H`, reload once;
4. verify actor retrieval for `H` does not return `A`'s choice;
5. verify branch-history retrieval does not return `A`'s personal/ambiguous rows;
6. verify an explicitly structural row, if one exists, is visible as third-person branch history with original actor provenance;
7. verify no rows were rewritten or duplicated and no score/game-state mutation occurred.

Without an explicitly structural episode kind, runtime acceptance is limited to proving non-leakage. Deterministic tests remain the correct proof of the additive selector boundary.

## Exact next implementation milestone

**Phase 7C-I1 — offline explicit retrieval-scope policy and deterministic tests.**

Implement only:

- a pure retrieval-scope/selection helper;
- unchanged delegation from existing actor-specific retrieval methods;
- additive `BuildLatestBranchHistoryReceipt`;
- additive explicit bridge entry point;
- closed semantic allowlist with both current kinds excluded from branch history;
- deterministic tests listed above;
- D1/D2 save compatibility invariants.

Do not add a structural gameplay writer, change episode ownership, modify save rows, launch Bannerlord, deploy a DLL, or begin Phase 8 in that checkpoint.

## Checkpoint

- Phase 7C audit: **COMPLETE**
- Semantic fix identified: **YES**
- Gameplay implementation: **NOT STARTED**
- Save-schema change required: **NO**
- Phase 7B bounded null preserved: **YES**
- Bannerlord launched: **NO**
- DLL deployed: **NO**
