# Codex Status

## Current task
Stop at the completed Phase 3 Strategic Commitment observation-only commit-verifier checkpoint. Do not deploy or run the commitment campaign proof as part of this checkpoint.

## Current state
PHASE 2D-L1 ROUND TRIP PROVEN; PHASE 3 HOME RESPONSIBILITY DETERMINISTIC SEAM PASSED; PHASE 3 KINGDOM OBJECTIVE WINNER-CHANGE + NATIVE COMMIT RUNTIME PROVEN; PHASE 3 VISUAL WAR WINNER-CHANGE + NATIVE COMMIT RUNTIME PROVEN; PHASE 3 STRATEGIC COMMITMENT DETERMINISTIC / STANDALONE SEAM PASSED; PHASE 3 STRATEGIC COMMITMENT COMMIT VERIFIER OFFLINE-PROVEN.

## Strategic Commitment commit verifier
The existing retention behavior is unchanged:
- factor 1.10;
- eligibility age 12 campaign hours;
- same-state only;
- existing positive previous native candidate only;
- strict retained score > natural score;
- Observe remains observation-only;
- Apply remains explicit only.

A pending expectation is now created only after explicit Apply, an actual composer factor application, recomputation of the final winner, and `retained=True` for the previous objective.

It stores only actor/party identity, expected native behavior, expected existing target identity, previous objective label/signature, and creation campaign hour.

On a later layer evaluation it reads only Bannerlord native behavior/target state and emits `STRATEGIC_COMMITMENT_COMMIT_CHECK`.

The verifier pending lifetime is 18 campaign hours. This does not change the separate 12-hour retention-eligibility rule.

## Validation
- commit-verifier policy: `PASS ... checks=20`;
- commit-verifier creation/wiring invariant: PASS;
- observation-only invariant: PASS;
- existing policy tests: `PASS ... checks=34`;
- existing config/path tests: `PASS ... checks=13`;
- existing runtime-wiring invariant: PASS;
- no-mutation invariant: PASS;
- standalone-path invariant: PASS;
- Release build: 0 errors, 1 inherited `System.ValueTuple` warning;
- built DLL SHA-256: `0267089ABE829248F145A468355041C17A91B8945816A8210BA0C6AD23CB4B5E`.

Tracked `Data/StrategicCommitment.cfg` remains `Mode=Observe`. Apply is not enabled by default.

No DLL was deployed and no Strategic Commitment runtime success is claimed.

## Evidence
- `Reports/TerritorialResponsibility/PHASE3_STRATEGIC_COMMITMENT_DETERMINISTIC_TEST_RESULT.md`
- `Reports/TerritorialResponsibility/PHASE3_STRATEGIC_COMMITMENT_COMMIT_VERIFIER_RESULT.md`
- `Reports/TerritorialResponsibility/evidence/phase3_strategic_commitment_commit_verifier_20260925.txt`

## Exact next milestone
One bounded runtime proof using temporary explicit module-local `Mode=Apply`: a previous objective remains an existing positive native candidate, a same-state natural winner appears within the unchanged 12-hour eligibility window, the unchanged 1.10 factor retains the previous candidate as composer winner, Bannerlord later commits the expected native behavior/target, and `STRATEGIC_COMMITMENT_COMMIT_CHECK matched=True`. Return the temporary config to Observe after the run.

Do not tune factor/age, change coarse-state classification, add categories/candidates/targets, or issue direct orders.

## Resource discipline
Do not revisit Visual War, Kingdom Objective, Home Responsibility, Phase 2D, launch/TestRunner/save handling, or prior proven seams unless new evidence establishes a defect.

The final product remains a standalone installable offline Bannerlord mod. TestRunner, Inspector, Codex, ChatGPT, Desktop Commander, watchdogs, and other development infrastructure are validation-only and are not runtime dependencies.
