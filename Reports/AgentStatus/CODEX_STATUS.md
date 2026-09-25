# Codex Status

## Current task
Stop at the completed bounded Phase 3 Strategic Commitment runtime attempt. The result is an honest null; do not tune or repeat this surface as part of the same milestone.

## Current state
PHASE 2D-L1 ROUND TRIP PROVEN; PHASE 3 HOME RESPONSIBILITY DETERMINISTIC SEAM PASSED; PHASE 3 KINGDOM OBJECTIVE WINNER-CHANGE + NATIVE COMMIT RUNTIME PROVEN; PHASE 3 VISUAL WAR WINNER-CHANGE + NATIVE COMMIT RUNTIME PROVEN; PHASE 3 STRATEGIC COMMITMENT DETERMINISTIC / STANDALONE SEAM PASSED; PHASE 3 STRATEGIC COMMITMENT COMMIT VERIFIER OFFLINE-PROVEN; PHASE 3 STRATEGIC COMMITMENT RUNTIME PROOF BOUNDED NULL.

## Strategic Commitment runtime attempt
Candidate source remained unchanged from commit `681f73e7f96fdd813534e05e3297f178d1eb0aac`.

Temporary installed module config was explicitly set to `Mode=Apply`. Fresh reset confirmed:
`STRATEGIC_COMMITMENT_RESET mode=Apply status=configured_apply factor=1.1 maxAgeHours=12`.

The strict qualifying window was 72 campaign hours, from `649491.27044636116` to `649563.27044636116`.

The first status poll after the boundary was `649563.46863080561`, a 0.198-hour polling overshoot. At that point:
- `STRATEGIC_COMMITMENT_APPLY=0`;
- `STRATEGIC_COMMITMENT_COMMIT_CHECK=0`.

TestRunner's asynchronous pause consumption allowed campaign time to continue after the qualifying boundary before PAUSE was processed. That extra interval is excluded from the proof window. There were still zero Apply and zero commit-check records through stop and exit, so no after-bound event is being used or discarded as a success.

Current-session counts:
- reset 1;
- apply 0;
- commit check 0;
- failure 0;
- would-retain 0.

Therefore the required retention/commit chain was not observed. The runtime Strategic Commitment proof remains unproven.

## Safety / restoration
- candidate DLL SHA-256: `0267089ABE829248F145A468355041C17A91B8945816A8210BA0C6AD23CB4B5E`;
- rollback: `D:\BannerlordAIResearch\Builds\Rollback_Phase3_StrategicCommitment_20260925_ClanAI`;
- rollback SHA-256: `F8C5E2AE389ADB29034CF18DE6DAA293C6F13571A4AA3F15CABA13E744A0B338`;
- `EXIT_NOSAVE` completed and Bannerlord is closed;
- installed config restored to `Mode=Observe`;
- tracked repository config remains `Mode=Observe`;
- Visual War marker remains absent;
- protected fixture remains SHA-256 `A91F15BF1403F1D29F942C56A1162F431113942CDACCAFE52F4D80303B4CB427` with unchanged timestamp.

No factor/age/coarse-state/candidate tuning or synthetic action/target/order/mutation was used.

## Evidence
- `Reports/TerritorialResponsibility/PHASE3_STRATEGIC_COMMITMENT_DETERMINISTIC_TEST_RESULT.md`
- `Reports/TerritorialResponsibility/PHASE3_STRATEGIC_COMMITMENT_COMMIT_VERIFIER_RESULT.md`
- `Reports/TerritorialResponsibility/PHASE3_STRATEGIC_COMMITMENT_RUNTIME_RESULT.md`
- `Reports/TerritorialResponsibility/evidence/phase3_strategic_commitment_runtime_null_20260925.txt`

## Resource discipline
Preserve this bounded null. Do not raise the 1.10 factor, extend the 12-hour eligibility age, change coarse-state classification, tune candidate scores, or synthesize a scenario to force the missing proof.

Do not rerun/re-debug Visual War, Kingdom Objective, Home Responsibility, Phase 2D, launch/TestRunner/save handling, or the Strategic Commitment offline seams unless new evidence establishes a defect.

The final product remains a standalone installable offline Bannerlord mod. TestRunner, Inspector, Codex, ChatGPT, Desktop Commander, watchdogs, and other development infrastructure are validation-only and are not runtime dependencies.
