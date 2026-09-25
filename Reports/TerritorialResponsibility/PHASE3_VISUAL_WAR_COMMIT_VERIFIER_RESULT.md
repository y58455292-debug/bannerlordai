# Phase 3 Visual War — Observation-Only Commit Verifier

Date: 2026-09-25
Repository base: `bc1ebe07e884f35b8fb218eab30c27e444a3d465`

## Scope

This checkpoint adds only the smallest post-vanilla observation seam needed to prove a future Visual War winner change actually commits in Bannerlord. It does not add or change strategy behavior, factors, thresholds, targets, world classification, native candidates, or activation defaults.

No DLL was deployed and no campaign runtime proof was run.

## Runtime shape

`VisualWarDecisionLayer.Apply` already runs on Bannerlord's normal AI hourly path.

The new verifier now:

1. checks any existing pending expectation at the start of a later normal Visual War AI observation, before the Visual War activation gate;
2. creates a pending expectation only after Visual War changes the current composer winner (`afterIndex != beforeIndex`);
3. stores only:
   - party id;
   - actor name;
   - expected native behavior;
   - expected existing target key/name;
   - Visual War reason;
   - creation campaign hour;
4. reads Bannerlord's later native state only;
5. removes the pending expectation after a confirmed match or bounded expiry.

The verifier does not call or alter `StrategicDecisionComposer`; score ownership remains unchanged.

## Expected target identities

Existing Bannerlord candidate targets are recorded as stable typed keys:

- settlement: `S:<StringId>`;
- mobile party: `P:<StringId>`.

No synthetic target is generated.

For settlement expectations, later observation reads:
- `TargetSettlement`;
- `ShortTermTargetSettlement`;
- `BesiegedSettlement`;
- `CurrentSettlement`.

For mobile-party expectations, later observation reads:
- `TargetParty`;
- `ShortTermTargetParty`.

Behavior observation reads:
- `DefaultBehavior`;
- `ShortTermBehavior`.

## Match and expiry rule

The Bannerlord-free `VisualWarCommitPolicy` evaluates:

`matched = (expected behavior matches default/short-term behavior AND expected target matches observed target) OR actor has arrived at expected settlement`.

Pending lifetime is 18 campaign hours. Age is clamped to zero if campaign time moves backward. A pending record is removed when `matched=True` or when age exceeds 18 hours.

A late native match can therefore log both `matched=True` and `expired=True`, and is still removed as a confirmed observation.

## Commit-check log

Later observations emit a clear `VISUAL_WAR_COMMIT_CHECK` record containing:

- actor and party id;
- Visual War reason;
- expected behavior;
- expected target name and stable target key;
- actual default and short-term behavior;
- actual target name/key;
- arrived settlement name/key;
- behavior/target/arrival match components;
- `matched=True/False`;
- `expired=True/False`;
- age in campaign hours;
- cumulative checks/matches/expiries.

This logging is observation-only.

## Deterministic validation

`dotnet run --project Tests/TerritorialResponsibility/VisualWarCommit/VisualWarCommitPolicyTests.csproj -c Release`

Result:

`PASS VisualWar commit policy tests checks=22`

`creation, native behavior/target matching, arrival matching, and expiry preserved`

Coverage includes:
- valid expectation creation and exact recorded fields;
- rejection of missing party, target, reason, or unsupported behavior;
- default-behavior match;
- short-term-behavior match;
- target mismatch retention;
- behavior mismatch retention;
- settlement-arrival match;
- mobile-party target identity match;
- exact 18-hour non-expiry;
- >18-hour expiry/removal;
- negative-age clamp;
- late native match plus expiry.

## Observation-only wiring invariant

`python Tests/TerritorialResponsibility/test_visual_war_commit_verifier_wiring.py`

Result:

`PASS VisualWar commit verifier wiring`

`expectation-on-winner-change and later native-state observation preserved`

The guard verifies expectation creation stays behind the existing Visual War winner-change boundary, later verification occurs on the normal AI path, native behavior/target properties are only read, the pure policy owns match/expiry logic, and `StrategicDecisionComposer` remains the score owner.

## No-mutation invariant

`python Tests/TerritorialResponsibility/test_visual_war_commit_verifier_no_mutation.py`

Result:

`PASS VisualWar commit verifier no-mutation invariant`

`no movement/order/target/faction/settlement/war mutation APIs introduced`

The guard rejects direct movement APIs, candidate insertion, direct target assignment, behavior assignment, settlement/faction mutation, and war mutation.

## Prior Visual War invariants revalidated

`dotnet run --project Tests/TerritorialResponsibility/VisualWarPolicyTests.csproj -c Release`

Result:

`PASS VisualWar policy tests checks=32`

`defense/offense/rear-security factors and weak exclusions preserved`

`python Tests/TerritorialResponsibility/test_visual_war_runtime_wiring.py`

Result:

`PASS VisualWar runtime wiring`

`native candidates/world classification/composer ownership preserved`

`python Tests/TerritorialResponsibility/test_visual_war_standalone_path.py`

Result:

`PASS VisualWar standalone activation path`

`activation is module-local Data/ENABLE_VISUAL_WAR_LAB.txt and OFF when absent`

The optional module-local marker remains unchanged and missing marker remains OFF.

## Release build

`dotnet build src/ClanAI/src/ClanAI/ClanAI.csproj -c Release`

Result:
- Build succeeded;
- 0 errors;
- 1 inherited `System.ValueTuple` warning;
- built DLL SHA-256: `F8C5E2AE389ADB29034CF18DE6DAA293C6F13571A4AA3F15CABA13E744A0B338`.

No DLL was deployed.

## Native-authority boundary

This checkpoint does not:
- issue a party order;
- change a target;
- add a native candidate;
- synthesize a target/action;
- change a candidate score;
- alter faction membership;
- alter settlement ownership;
- alter war state;
- change Visual War factors, thresholds, weak rules, world classification, or activation defaults.

The expectation is derived only from the existing Bannerlord candidate that Visual War made the current composer winner. Later checks only observe Bannerlord's state.

## Boundary / next milestone

No runtime success claim is made here.

The next milestone remains one bounded runtime proof where:
- an existing Visual War defensive/security contribution changes a Bannerlord-native candidate winner;
- Bannerlord later commits the expected native behavior/target;
- `VISUAL_WAR_COMMIT_CHECK` records `matched=True`.
