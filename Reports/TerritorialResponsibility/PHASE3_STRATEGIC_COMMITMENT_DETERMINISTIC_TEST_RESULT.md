# Phase 3 Strategic Commitment — Deterministic / Standalone Result

Date: 2026-09-25
Repository base: `231a317b15f81a0d6afb0a1d517ec5fe5b6e8fea`

## Scope

This checkpoint implements only the bounded offline seam for the existing `StrategicCommitmentLayer`. It does not add strategic roles, change retention magnitude/age, alter coarse-state classification, deploy a DLL, or run a campaign proof.

## Existing behavior preserved

The existing commitment contract remains:

- retention factor: `1.10`;
- maximum previous-objective age: `12.0` campaign hours;
- commitment is considered only after the current natural/composer winner changes away from the previous objective signature;
- a cross-state natural change is allowed through with no retention;
- the previous objective must still resolve to an existing positive Bannerlord native candidate;
- the previous and natural objectives must remain in the same existing `ActorStrategicBlackboard.CoarseState`;
- a previous objective older than 12 hours is not retained;
- Apply mode rescales only the already-existing previous native candidate through `StrategicDecisionComposer.ApplyFactor`;
- Observe mode records only the existing WOULD_RETAIN telemetry and does not apply a composer factor;
- Bannerlord retains candidate generation, targets, legality, and final-winner authority.

## Pure policy extraction

Added `StrategicCommitmentPolicy`, which contains no Bannerlord types.

It owns only the existing deterministic decision rules:

### Context gate

`EvaluateContext(hasPriorState, sameObjective, sameCoarseState, ageHours)`

covers:
- no prior state;
- same natural objective;
- cross-state change;
- negative age;
- exact 12-hour boundary;
- >12-hour expiry.

Negative age remains a failure/no-retention condition in the runtime layer. Exact 12 hours remains eligible; only age strictly greater than 12 hours expires.

### Score / mode gate

`EvaluateScores(previousCandidateFound, previousScore, naturalScore, mode)`

preserves:
- missing previous native candidate -> no retention;
- previous score must be finite and >0;
- natural score must be finite and >0;
- `retainedScore = previousScore * 1.10`;
- retention requires `retainedScore > naturalScore` strictly;
- exact equality does not retain;
- Observe may report WOULD_RETAIN but never applies;
- Apply may apply only after all gates pass.

The runtime still obtains the previous candidate via `ActorStrategicBlackboard.FindObjectiveIndex`, reads scores through `StrategicDecisionComposer`, and passes only `previousIndex` to `composer.ApplyFactor`.

## Standalone-safe config

The previous absolute path:

`D:\BannerlordAIResearch\Data\StrategicCommitment.cfg`

was removed.

`StrategicCommitmentConfig` now resolves from its installed assembly location to:

`<module-root>/Data/StrategicCommitment.cfg`

for the standard `ClanAI/bin/<platform>/ClanAI.dll` layout.

Safe defaults remain:
- unresolved module layout -> Observe;
- missing config -> Observe;
- missing `Mode=` -> Observe;
- invalid `Mode=` -> Observe;
- only `Mode=Apply` explicitly enables Apply;
- tracked `Data/StrategicCommitment.cfg` remains `Mode=Observe`.

No development-tool or development-machine path dependency is introduced.

## Deterministic policy validation

`dotnet run --project Tests/TerritorialResponsibility/StrategicCommitmentPolicy/StrategicCommitmentPolicyTests.csproj -c Release`

Result:

`PASS Strategic Commitment policy tests checks=34`

`same-state age/score gates, strict retention boundary, and Observe/Apply semantics preserved`

Coverage includes:
- no prior state;
- same natural objective;
- cross-state change;
- negative age;
- exact 12-hour boundary;
- >12-hour expiry;
- previous candidate missing;
- previous score zero/negative/NaN/infinite;
- natural score zero/negative/NaN/infinite;
- retained-score overflow protection;
- valid same-state previous candidate;
- exact 1.10 retained-score math;
- retained score strictly exceeding natural;
- exact equality -> no retention;
- just-above/below strict boundary;
- Observe -> no Apply;
- Apply -> allowed only after valid retention decision.

## Config parsing / path validation

`dotnet run --project Tests/TerritorialResponsibility/StrategicCommitmentConfig/StrategicCommitmentConfigTests.csproj -c Release`

Result:

`PASS Strategic Commitment config tests checks=13`

`module-local path and missing/invalid Observe defaults preserved`

Coverage includes:
- null/missing config semantics;
- comments/unknown keys;
- `Mode=Observe`;
- `Mode=Apply`;
- case-insensitive parsing;
- invalid mode -> Observe;
- module-local path resolution;
- invalid/unexpected layout -> no resolved path and safe Observe;
- no development root in resolved path.

## Runtime-wiring invariant

`python Tests/TerritorialResponsibility/test_strategic_commitment_runtime_wiring.py`

Result:

`PASS Strategic Commitment runtime wiring`

`existing native candidate lookup and composer-owned previous-candidate rescale preserved`

The guard confirms:
- prior state still comes from `ActorStrategicBlackboard`;
- current natural winner still comes from the composer;
- coarse-state classification remains in `ActorStrategicBlackboard`;
- previous objective lookup still scans the native candidate list by signature;
- natural/current scores remain composer-owned;
- there is exactly one commitment `ApplyFactor` call;
- it targets only `previousIndex`;
- `StrategicDecisionComposer` remains the native score writer.

## No-mutation invariant

`python Tests/TerritorialResponsibility/test_strategic_commitment_no_mutation.py`

Result:

`PASS Strategic Commitment no-mutation invariant`

`no order/target/candidate/faction/settlement/war mutation APIs introduced`

No direct movement, target assignment, candidate insertion, faction mutation, settlement mutation, or war mutation API was added.

## Standalone-path invariant

`python Tests/TerritorialResponsibility/test_strategic_commitment_standalone_path.py`

Result:

`PASS Strategic Commitment standalone path`

`config resolves module-locally and tracked/missing/invalid defaults remain Observe`

The commitment source/config/policy no longer references `D:\BannerlordAIResearch`. The tracked config is still `Mode=Observe`.

## Release build

`dotnet build src/ClanAI/src/ClanAI/ClanAI.csproj -c Release`

Result:
- Build succeeded;
- 0 errors;
- 1 inherited `System.ValueTuple` version-conflict warning;
- built DLL SHA-256: `B9FAADFF957E8412934D8FD16EAF2BD498F2F1D431AF78DDABC08354B4F6F5D0`.

No DLL was deployed.

## Boundary / next milestone

This is an offline deterministic / standalone-safe checkpoint only. No runtime success is claimed.

The expected next milestone is one bounded runtime proof where the existing Strategic Commitment layer, in explicit Apply mode, retains an already-existing Bannerlord-native previous objective over a new same-state natural winner and Bannerlord later commits the retained native behavior/target. No new role, candidate, target, order, or world mutation is required.
