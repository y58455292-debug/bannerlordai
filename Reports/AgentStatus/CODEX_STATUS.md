# Codex Status

## Current task
Phase 3 Visual War bounded runtime proof is complete. Stop at this checkpoint; do not broaden or retune the system as part of this milestone.

## Current state
PHASE 2D-L1 ROUND TRIP PROVEN; PHASE 3 HOME RESPONSIBILITY DETERMINISTIC SEAM PASSED; PHASE 3 KINGDOM OBJECTIVE WINNER-CHANGE + NATIVE COMMIT RUNTIME PROVEN; PHASE 3 VISUAL WAR DETERMINISTIC / STANDALONE SEAM PASSED; PHASE 3 VISUAL WAR WINNER-CHANGE + NATIVE COMMIT RUNTIME PROVEN.

## Visual War runtime proof
Candidate source remained unchanged from commit `b79e64201914c97a354d5bc411a54a80d6fa7745`.

Deployment:
- candidate DLL SHA-256: `F8C5E2AE389ADB29034CF18DE6DAA293C6F13571A4AA3F15CABA13E744A0B338`;
- prior live / rollback SHA-256: `4124AA4F79D452E28B0C08892B1642CD77ED2DFA9F698129405C603A4E69AA2D`;
- rollback: `D:\BannerlordAIResearch\Builds\Rollback_Phase3_VisualWar_20260925_ClanAI`.

Visual War was enabled only through the module-local `ClanAI/Data/ENABLE_VISUAL_WAR_LAB.txt` marker. Fresh campaign reset reported `VISUAL_WAR_RESET enabled=True`. The marker was removed after `EXIT_NOSAVE`, so Visual War is OFF by default again.

The observation was capped at 72 campaign hours and stopped after 29.915 hours when a qualifying defensive proof was observed.

Selected proof:
- actor: Arthamund;
- state: 112 men, readiness 0.868, food 26;
- reason: `frontier-defense`;
- native/current winner before Visual War: `PatrolAroundPoint:Sibir`;
- existing native candidate after Visual War: `PatrolAroundPoint:Goleryn`;
- final composed blackboard objective: `PatrolAroundPoint:Goleryn`;
- verifier expectation: `PatrolAroundPoint` / Goleryn;
- Bannerlord actual default: `PatrolAroundPoint`;
- Bannerlord actual target: Goleryn with the same settlement id;
- `behaviorMatch=True`;
- `targetMatch=True`;
- `matched=True`;
- `expired=False`;
- commit-check age: 6.06 campaign hours.

No factor/threshold tuning, new role, synthetic target/action, direct party order, candidate insertion, faction mutation, settlement mutation, or war mutation was used.

The protected demo fixture remained unchanged at SHA-256 `A91F15BF1403F1D29F942C56A1162F431113942CDACCAFE52F4D80303B4CB427` with its original timestamp.

## Evidence
- `Reports/TerritorialResponsibility/PHASE3_VISUAL_WAR_DETERMINISTIC_TEST_RESULT.md`
- `Reports/TerritorialResponsibility/PHASE3_VISUAL_WAR_COMMIT_VERIFIER_RESULT.md`
- `Reports/TerritorialResponsibility/PHASE3_VISUAL_WAR_RUNTIME_RESULT.md`
- `Reports/TerritorialResponsibility/evidence/phase3_visual_war_runtime_20260925.txt`

## Resource discipline
Do not rerun or re-debug Phase 2D, launch/TestRunner/save handling, Home Responsibility, Kingdom Objective, the Visual War deterministic seam, commit verifier, or this runtime proof unless new evidence establishes a defect.

The final product remains a standalone installable offline Bannerlord mod. TestRunner, Inspector, Codex, ChatGPT, Desktop Commander, watchdogs, and other development infrastructure are validation-only and are not runtime dependencies.
