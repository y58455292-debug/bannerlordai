# Codex Status

## Current task
Stop at the completed Phase 3 Strategic Commitment deterministic / standalone-safe checkpoint. Do not deploy or run the commitment campaign proof as part of this checkpoint.

## Current state
PHASE 2D-L1 ROUND TRIP PROVEN; PHASE 3 HOME RESPONSIBILITY DETERMINISTIC SEAM PASSED; PHASE 3 KINGDOM OBJECTIVE WINNER-CHANGE + NATIVE COMMIT RUNTIME PROVEN; PHASE 3 VISUAL WAR WINNER-CHANGE + NATIVE COMMIT RUNTIME PROVEN; PHASE 3 STRATEGIC COMMITMENT DETERMINISTIC / STANDALONE SEAM PASSED.

## Strategic Commitment checkpoint
The existing commitment behavior was not redesigned and no strategic category was added.

Pure `StrategicCommitmentPolicy` now isolates the existing retention rules:
- factor remains 1.10;
- maximum age remains 12 campaign hours;
- no prior state or same objective -> no retention;
- cross-state change -> no retention;
- negative age -> failure/no retention;
- age exactly 12 hours remains eligible; >12 expires;
- previous objective must still be an existing finite positive native candidate;
- natural score must be finite and positive;
- retained score is `previousScore * 1.10`;
- retention requires retained score strictly greater than natural score;
- exact equality does not retain;
- Observe never applies;
- Apply may rescale only the existing previous native candidate.

`ActorStrategicBlackboard` coarse-state classification and candidate-signature lookup remain unchanged. `StrategicDecisionComposer` remains the score owner and final-winner authority.

## Standalone config
The absolute development path `D:\BannerlordAIResearch\Data\StrategicCommitment.cfg` is removed.

The config now resolves to:
`<module-root>/Data/StrategicCommitment.cfg`.

Safe defaults remain:
- missing/unresolved config -> Observe;
- invalid mode -> Observe;
- tracked config -> `Mode=Observe`;
- only explicit `Mode=Apply` enables Apply.

No development-tool dependency was introduced.

## Validation
- policy tests: `PASS ... checks=34`;
- config/path tests: `PASS ... checks=13`;
- runtime-wiring invariant: PASS;
- no-mutation invariant: PASS;
- standalone-path invariant: PASS;
- Release build: 0 errors, 1 inherited `System.ValueTuple` warning;
- built DLL SHA-256: `B9FAADFF957E8412934D8FD16EAF2BD498F2F1D431AF78DDABC08354B4F6F5D0`.

No DLL was deployed and no Strategic Commitment runtime success is claimed.

## Evidence
- `Reports/TerritorialResponsibility/PHASE3_STRATEGIC_COMMITMENT_DETERMINISTIC_TEST_RESULT.md`
- `Reports/TerritorialResponsibility/evidence/phase3_strategic_commitment_deterministic_20260925.txt`

## Exact next milestone
One bounded runtime proof with the existing layer explicitly in Apply mode: a previous objective remains an already-existing positive Bannerlord candidate, a new natural winner appears in the same coarse state within 12 campaign hours, the unchanged 1.10 retention factor makes the previous candidate the composer winner, and Bannerlord later commits that retained native behavior/target.

Do not tune the factor/age, add categories, create targets/candidates, or issue orders to manufacture the proof.

## Resource discipline
Do not revisit Visual War, Kingdom Objective, Home Responsibility, Phase 2D, launch/TestRunner/save handling, or prior proven seams unless new evidence establishes a defect.

The final product remains a standalone installable offline Bannerlord mod. TestRunner, Inspector, Codex, ChatGPT, Desktop Commander, watchdogs, and other development infrastructure are validation-only and are not runtime dependencies.
