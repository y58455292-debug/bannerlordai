# Phase 7C-I1 Retrieval-Scope Implementation Result

Date: 2026-09-25

Base checkpoint: `63e4aa2209be088e9276a6072e478e863d31ca2d`

## Result

- Actor-specific retrieval preserved: **YES**
- Explicit branch-history API added: **YES**
- Current production branch-history kinds: **NONE**
- Save-schema changed: **NO**
- Gameplay mutation added: **NO**
- Runtime-observed: **NO**
- Phase 7B bounded null preserved: **YES**

`BuildLatestRetrievalReceipt` remains current actor + current branch + known-by-now. Its existing first-inserted tie behavior is preserved. `BuildLatestChoiceRetrievalReceipt` remains current actor + current branch + `IncidentChoice` + known-by-now, including its existing observed-UTC tie break.

`BuildLatestBranchHistoryReceipt` is a separate, observation-only path. It selects only the current branch, excludes future rows, does not filter by current actor, preserves the selected row's actor ID/name as provenance, and labels any result `branch-history` with `personalMemory=false`. It returns empty without falling back to personal retrieval when no eligible row exists. `RetrieveLatestDynastyHistory` exposes this path without changing either existing bridge method.

The production semantic allowlist is closed and presently excludes both `IncidentOpened` and `IncidentChoice`. No production structural kind or writer was added. A pure test-only selector proves that a deliberately allowlisted structural descriptor can cross actor identity within the same branch while retaining provenance and excluding other-branch/future rows.

## Compatibility

D1/D2 import acceptance, D2 export, episode ownership, actor/branch IDs, and serialized fields are unchanged. Unknown persisted kinds remain rejected. No row duplication, migration, or new schema tag was added.

## Validation

- Focused deterministic retrieval checks: **PASS** (19 checks).
- Actor/history isolation invariant: **PASS**.
- D1/D2 save compatibility invariant: **PASS**.
- No gameplay mutation/development dependency invariant: **PASS**.
- Phase 7B and Phase 4B/4C–6 preservation checks: **PASS**.
- Release build: **PASS**, 0 errors; inherited `System.ValueTuple` warning remains.

Evidence: `Reports/GenerationalContinuity/evidence/phase7c_i1_offline_validation_20260925.txt`.

