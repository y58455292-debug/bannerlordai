# Codex Status

## Current task
Stop at the completed Phase 3 Visual War observation-only commit-verifier checkpoint. Do not deploy or run the Visual War campaign proof as part of this checkpoint.

## Current state
PHASE 2D-L1 ROUND TRIP PROVEN; PHASE 3 HOME RESPONSIBILITY DETERMINISTIC SEAM PASSED; PHASE 3 KINGDOM OBJECTIVE WINNER-CHANGE + NATIVE COMMIT RUNTIME PROVEN; PHASE 3 VISUAL WAR DETERMINISTIC / STANDALONE SEAM PASSED; PHASE 3 VISUAL WAR COMMIT VERIFIER OFFLINE-PROVEN.

## Visual War commit verifier
The existing strategy behavior is unchanged.

When and only when Visual War changes the current composer winner, the layer now stores a small pending expectation containing only actor/party identity, expected native behavior, expected existing Bannerlord target identity, Visual War reason, and creation campaign hour.

On a later normal AI observation, before the Visual War activation gate, it reads Bannerlord's native state:
- `DefaultBehavior`;
- `ShortTermBehavior`;
- settlement target/arrival state where applicable;
- mobile-party target state for rear-security where applicable.

It emits `VISUAL_WAR_COMMIT_CHECK` with expected/actual behavior and target, component matches, `matched`, `expired`, age, reason, and counters. Pending records are removed after match or after the 18-campaign-hour bound.

The verifier is observation-only and does not use the composer to change scores.

## Validation
- Visual War commit policy: `PASS ... checks=22`;
- commit-verifier wiring invariant: PASS;
- no-mutation invariant: PASS;
- existing Visual War policy: `PASS ... checks=32`;
- existing runtime-wiring invariant: PASS;
- existing standalone activation-path invariant: PASS;
- Release build: 0 errors, 1 inherited `System.ValueTuple` warning;
- build SHA-256: `F8C5E2AE389ADB29034CF18DE6DAA293C6F13571A4AA3F15CABA13E744A0B338`.

The optional activation marker remains module-local at `ClanAI/Data/ENABLE_VISUAL_WAR_LAB.txt`; missing marker remains OFF.

No DLL was deployed and no Visual War runtime success is claimed.

## Evidence
- `Reports/TerritorialResponsibility/PHASE3_VISUAL_WAR_DETERMINISTIC_TEST_RESULT.md`
- `Reports/TerritorialResponsibility/PHASE3_VISUAL_WAR_COMMIT_VERIFIER_RESULT.md`
- `Reports/TerritorialResponsibility/evidence/phase3_visual_war_commit_verifier_20260925.txt`

## Exact next milestone
One bounded runtime proof where an existing Visual War defensive/security contribution changes a Bannerlord-native candidate winner and Bannerlord later commits the expected behavior/target, with `VISUAL_WAR_COMMIT_CHECK matched=True`.

Do not tune factors, change world classification, add candidates/targets, broaden Phase 3, or enable Visual War by default for that proof.

## Resource discipline
Do not revisit Kingdom Objective, Home Responsibility, Phase 2D, launch/TestRunner/save handling, or prior Visual War deterministic work unless new evidence establishes a defect.

The final product remains a standalone installable offline Bannerlord mod. TestRunner, Inspector, Codex, ChatGPT, Desktop Commander, watchdogs, and other development infrastructure are validation-only and are not runtime dependencies.
