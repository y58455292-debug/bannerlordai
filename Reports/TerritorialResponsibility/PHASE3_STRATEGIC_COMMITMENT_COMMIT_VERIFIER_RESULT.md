# Phase 3 Strategic Commitment — Observation-Only Commit Verifier

Date: 2026-09-25
Repository base: `5e44a6f3606acb8d06d71f4b3e3e2b3f0ad0bf75`

## Scope

This checkpoint adds only the smallest post-vanilla observation seam needed to prove a future Strategic Commitment Apply retention actually commits in Bannerlord.

It does not change:
- `RetentionFactor = 1.10`;
- `MaxAgeHours = 12.0`;
- same-state-only eligibility;
- previous-candidate eligibility;
- the strict `retainedScore > naturalScore` boundary;
- coarse-state classification;
- native candidate generation;
- native targets or action legality;
- explicit-only Apply semantics;
- tracked `Mode=Observe`.

No DLL was deployed and no campaign proof was run.

## Pending expectation creation

A pending expectation is created only after all existing Apply retention conditions have already completed successfully:

1. `scoreDecision.Apply` is true, which requires explicit `Mode=Apply`;
2. `composer.ApplyFactor` has actually been called on the existing previous native candidate;
3. the layer recomputes the current composer winner;
4. the final winner signature equals the previous objective signature;
5. `retained=True`.

The pure `StrategicCommitmentCommitPolicy.ShouldCreateExpectation` requires all four verifier gates:
- Apply mode;
- factor applied;
- final winner is the previous objective;
- retained is true.

The runtime stores only:
- party id;
- actor name;
- expected native `AiBehavior`;
- expected existing Bannerlord target key/name;
- previous objective label;
- previous objective signature;
- creation campaign hour.

The expected behavior/target are taken from the final retained existing native candidate in `PartyThinkParams`. No candidate or target is synthesized.

## Later native-state observation

At the start of a later `StrategicCommitmentLayer.Evaluate` call, before new commitment evaluation, the verifier checks any pending expectation for that actor.

It reads Bannerlord native state only.

Behavior:
- `DefaultBehavior`;
- `ShortTermBehavior`.

Settlement target state:
- `TargetSettlement`;
- `ShortTermTargetSettlement`;
- `BesiegedSettlement`;
- `CurrentSettlement`.

Mobile-party target state:
- `TargetParty`;
- `ShortTermTargetParty`.

Typed expected target keys are:
- settlement: `S:<StringId>`;
- mobile party: `P:<StringId>`;
- point-style objective: the existing `V:...` target portion of the blackboard objective signature.

Point-style objectives are not falsely confirmed from behavior alone. If Bannerlord does not expose a comparable target through the bounded native fields above, the expectation remains unmatched until expiry.

## Match rule

The Bannerlord-free verifier policy computes:

`matched = (behaviorMatch && targetMatch) || arrivedMatch`

where `arrivedMatch` applies only to settlement targets.

A behavior match may come from either native default or short-term behavior. Target identity must match the expected stable settlement/mobile-party key.

## Pending lifetime

Verifier pending lifetime is:

`18.0 campaign hours`.

This is deliberately separate from, and does not modify, the existing commitment eligibility rule:

`MaxAgeHours = 12.0 campaign hours`.

The 12-hour rule controls whether a previous objective is eligible for retention. The 18-hour verifier lifetime controls only how long the observation-only pending commit check remains available after an already-completed retention.

Pending age is clamped to zero if campaign time moves backward. A pending expectation is removed after:
- a confirmed native match; or
- age strictly greater than 18 campaign hours.

## Commit-check log

Later observations emit:

`STRATEGIC_COMMITMENT_COMMIT_CHECK`

with:
- actor;
- party id;
- previous objective label/signature;
- expected behavior;
- expected target name/key;
- actual default behavior;
- actual short-term behavior;
- actual target name/key;
- arrived settlement name/key;
- `behaviorMatch`;
- `targetMatch`;
- `arrivedMatch`;
- `matched`;
- `expired`;
- age in campaign hours;
- cumulative checks/matches/expiries.

The verifier does not call `StrategicDecisionComposer.ApplyFactor` and does not change Bannerlord state.

## Deterministic verifier validation

`dotnet run --project Tests/TerritorialResponsibility/StrategicCommitmentCommit/StrategicCommitmentCommitPolicyTests.csproj -c Release`

Result:

`PASS Strategic Commitment commit policy tests checks=20`

`retained-Apply creation gates, native behavior/target matching, and 18-hour expiry preserved`

Coverage includes:
- all four expectation-creation gates;
- Observe mode never creates pending verifier state;
- factor must actually be applied;
- final winner must be the previous objective;
- `retained=True` is required;
- default behavior match;
- short-term behavior match;
- settlement target identity;
- wrong target retention;
- wrong behavior retention;
- settlement arrival match;
- mobile-party target identity;
- point target cannot be falsely confirmed;
- exact 18-hour non-expiry;
- >18-hour expiry;
- negative age clamp;
- late native match plus expiry.

## Creation/wiring invariant

`python Tests/TerritorialResponsibility/test_strategic_commitment_commit_verifier_wiring.py`

Result:

`PASS Strategic Commitment commit verifier wiring`

`pending expectation creation is downstream of explicit Apply, applied factor, final previous-objective winner, and retained=True`

The guard verifies pending creation remains structurally downstream of the existing Apply factor and retained-winner check, and records the final retained native candidate.

## Observation-only invariant

`python Tests/TerritorialResponsibility/test_strategic_commitment_commit_verifier_observation_only.py`

Result:

`PASS Strategic Commitment verifier observation-only invariant`

`later verifier reads native behavior/target state only and removes pending state on policy match/expiry`

The verifier method is checked for native reads and rejects composer writes, behavior/target assignments, movement/order APIs, candidate insertion, and world-mutation APIs.

## Existing Strategic Commitment validation re-run

`dotnet run --project Tests/TerritorialResponsibility/StrategicCommitmentPolicy/StrategicCommitmentPolicyTests.csproj -c Release`

Result:

`PASS Strategic Commitment policy tests checks=34`

`same-state age/score gates, strict retention boundary, and Observe/Apply semantics preserved`

`dotnet run --project Tests/TerritorialResponsibility/StrategicCommitmentConfig/StrategicCommitmentConfigTests.csproj -c Release`

Result:

`PASS Strategic Commitment config tests checks=13`

`module-local path and missing/invalid Observe defaults preserved`

`python Tests/TerritorialResponsibility/test_strategic_commitment_runtime_wiring.py`

Result:

`PASS Strategic Commitment runtime wiring`

`existing native candidate lookup and composer-owned previous-candidate rescale preserved`

`python Tests/TerritorialResponsibility/test_strategic_commitment_no_mutation.py`

Result:

`PASS Strategic Commitment no-mutation invariant`

`no order/target/candidate/faction/settlement/war mutation APIs introduced`

The no-mutation guard now also includes the new commit policy.

`python Tests/TerritorialResponsibility/test_strategic_commitment_standalone_path.py`

Result:

`PASS Strategic Commitment standalone path`

`config resolves module-locally and tracked/missing/invalid defaults remain Observe`

## Release build

`dotnet build src/ClanAI/src/ClanAI/ClanAI.csproj -c Release`

Result:
- Build succeeded;
- 0 errors;
- 1 inherited `System.ValueTuple` version-conflict warning;
- built DLL SHA-256: `0267089ABE829248F145A468355041C17A91B8945816A8210BA0C6AD23CB4B5E`.

No DLL was deployed.

## Native-authority boundary

This checkpoint does not:
- issue a party order;
- assign a target;
- create a native candidate;
- synthesize an action or target;
- write a native behavior;
- alter composer score ownership;
- alter coarse-state classification;
- mutate faction membership;
- mutate settlement ownership;
- mutate war state;
- change retention factor or eligibility age;
- enable Apply by default.

The tracked `Data/StrategicCommitment.cfg` remains `Mode=Observe`.

## Boundary / next milestone

No runtime success is claimed here.

The expected next milestone is one bounded runtime proof using a temporary explicit module-local `Mode=Apply` configuration where:
- the previous objective remains an existing positive Bannerlord candidate;
- a new natural winner appears in the same coarse state within the unchanged 12-hour eligibility window;
- the unchanged 1.10 retention factor makes the previous candidate the composer winner;
- Bannerlord later commits the retained native behavior/target;
- `STRATEGIC_COMMITMENT_COMMIT_CHECK` records `matched=True`;
- the temporary Apply configuration is returned to Observe after the run.
