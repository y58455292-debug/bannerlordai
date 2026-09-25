# Codex Status

## Current task
Phase 3 Kingdom Objective bounded runtime proof is complete. Stop at this checkpoint; do not broaden or retune the system as part of this milestone.

## Current state
PHASE 2D-L1 ROUND TRIP PROVEN; PHASE 3 HOME RESPONSIBILITY DETERMINISTIC SEAM PASSED; PHASE 3 KINGDOM OBJECTIVE DETERMINISTIC SEAM PASSED; PHASE 3 KINGDOM OBJECTIVE WINNER-CHANGE + NATIVE COMMIT RUNTIME PROVEN.

## Runtime proof
Candidate source remained unchanged from commit `96813b4e9fe4951118f2dc5b4427bffc4cb58fc1`.

The deployed DLL was built from authoritative source with 0 errors and SHA-256:
`4124AA4F79D452E28B0C08892B1642CD77ED2DFA9F698129405C603A4E69AA2D`.

A verified rollback of the prior installed DLL is at:
`D:\BannerlordAIResearch\Builds\Rollback_Phase3_KingdomObjective_20260925_ClanAI`
with SHA-256:
`5A88AC72C6154680A6B009F7C9A5E262CA4BE8F71AFC54B7A0C1F3057AA64B9C`.

Using the protected demo fixture read-only, the observation was capped at 72 campaign hours and stopped after 28.224 hours when a qualifying proof was observed.

Selected proof:
- natural ruler objective: Battania / ruler Rath / `BorderSecurity` / `CaptureSpecificSettlement` / Uthelaim Castle;
- Bannerlord-provided objective-related candidates: 3, all staging;
- native before winner: `RaidSettlement:Stathymos`, score 3.756;
- existing competitive staging candidate: `GoToSettlement:Pendraic Castle`, ratio 0.981;
- unchanged applied staging factor: 1.55;
- adjusted winner: `GoToSettlement:Pendraic Castle`;
- `winnerChanged=True`, `objectiveWon=True`;
- post-vanilla commit: `actualDefault=GoToSettlement`, `actualShort=GoToSettlement`, `actualTarget=Pendraic Castle`, `matched=True`.

No direct party order, faction mutation, settlement mutation, war mutation, or synthetic target/action was used. The run exited with `EXIT_NOSAVE`. The protected fixture retained SHA-256 `A91F15BF1403F1D29F942C56A1162F431113942CDACCAFE52F4D80303B4CB427` and its original timestamp.

## Evidence
- `Reports/TerritorialResponsibility/PHASE3_KINGDOM_OBJECTIVE_DETERMINISTIC_TEST_RESULT.md`
- `Reports/TerritorialResponsibility/PHASE3_KINGDOM_OBJECTIVE_RUNTIME_RESULT.md`
- `Reports/TerritorialResponsibility/evidence/phase3_kingdom_objective_runtime_20260925.txt`

## Resource discipline
Do not re-run or re-debug Phase 2D, launch flow, TestRunner, save/load, Home Responsibility, the Kingdom Objective deterministic seam, or this runtime proof unless new evidence establishes a defect.

The final product remains a standalone installable offline Bannerlord mod. TestRunner, Inspector, Codex, ChatGPT, Desktop Commander, watchdogs, and other development infrastructure are validation-only and are not runtime dependencies.
