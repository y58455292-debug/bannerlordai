# Codex Status

## Current task
Stop at the completed Phase 3 Visual War deterministic / standalone-safe checkpoint. Do not deploy or run the Visual War campaign proof as part of this checkpoint.

## Current state
PHASE 2D-L1 ROUND TRIP PROVEN; PHASE 3 HOME RESPONSIBILITY DETERMINISTIC SEAM PASSED; PHASE 3 KINGDOM OBJECTIVE WINNER-CHANGE + NATIVE COMMIT RUNTIME PROVEN; PHASE 3 VISUAL WAR DETERMINISTIC / STANDALONE SEAM PASSED.

## Visual War checkpoint
The existing `VisualWarDecisionLayer` was not redesigned and no new strategic role was added.

The layer now delegates only its existing pure decision rules to `VisualWarPolicy`:
- weak threshold remains readiness <0.72 or food <3 days;
- active settlement defense remains capped by the existing 1.35 defense cap;
- frontier defense remains `1 + 0.22 * frontierScore`, capped at 1.35;
- frontier offense remains `1 + 0.16 * frontierScore`;
- rear-security remains bandit-only, >0 and <=160 men, with 1.25 at <=90 men and 1.15 at 91-160;
- non-positive native scores and unrelated behaviors remain untouched.

Runtime wiring still preserves Bannerlord candidate generation and native targets, the existing settlement/world-context and bandit classification, `StrategicDecisionComposer` score ownership, and ActorStrategicBlackboard's existing Visual War reason classification.

## Standalone activation
The absolute development path `D:\BannerlordAIResearch\Data\ENABLE_VISUAL_WAR_LAB.txt` is removed from this layer.

The optional switch now resolves from the installed ClanAI assembly to:
`<module-root>/Data/ENABLE_VISUAL_WAR_LAB.txt`.

Missing marker remains OFF. No marker was added, so this cleanup does not silently enable Visual War. No development-tool dependency was introduced.

## Validation
- `dotnet run --project Tests/TerritorialResponsibility/VisualWarPolicyTests.csproj -c Release` -> `PASS VisualWar policy tests checks=32`;
- `python Tests/TerritorialResponsibility/test_visual_war_runtime_wiring.py` -> PASS;
- `python Tests/TerritorialResponsibility/test_visual_war_standalone_path.py` -> PASS;
- Release build -> 0 errors, 1 inherited `System.ValueTuple` warning;
- built DLL SHA-256: `F038F0160B3A39B4175DBE0BD7C6B81F6EB7AD052D904EA413DC71682076DEA1`.

No DLL was deployed and no Visual War runtime behavior is newly claimed.

## Evidence
- `Reports/TerritorialResponsibility/PHASE3_VISUAL_WAR_DETERMINISTIC_TEST_RESULT.md`
- `Reports/TerritorialResponsibility/evidence/phase3_visual_war_deterministic_20260925.txt`

## Exact next milestone
One bounded runtime proof that an existing Visual War defensive/security contribution changes a Bannerlord-native candidate winner and Bannerlord commits the selected native behavior/target.

Do not tune factors, add roles, add candidates/targets, or broaden into `StrategicCommitmentLayer` for that proof.

## Resource discipline
Do not revisit Phase 2D, launch/TestRunner/save handling, Home Responsibility, or the Kingdom Objective proof unless new evidence establishes a defect.

The final product remains a standalone installable offline Bannerlord mod. TestRunner, Inspector, Codex, ChatGPT, Desktop Commander, watchdogs, and other development infrastructure are validation-only and are not runtime dependencies.
