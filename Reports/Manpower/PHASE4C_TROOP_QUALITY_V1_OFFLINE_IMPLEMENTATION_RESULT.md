# Phase 4C — Troop Quality v1 offline implementation result

Date: 2026-09-25 UTC  
Base: `1dfdc48323c65c350bfaa8353e8d8f83c5e70542`

## Result

**PASSED — Phase 4C Troop Quality v1 is implemented and build-proven offline; it is not runtime-observed and balance is not proven.**

The implementation follows the accepted native-capability audit exactly:

- no second `VolunteerModel` wrapper;
- existing selected `LocalManpowerVolunteerModel` remains the single wrapper;
- empty volunteer slots retain the Phase 4B Local Manpower path;
- occupied slots that are not native-upgrade-eligible pass through the selected native probability;
- occupied native-upgrade-eligible slots use a separate pure Phase 4C first-gate quality policy;
- Bannerlord still owns notable power, current tier, `MaxVolunteerTier`, `UpgradeTargets`, RNG, culture/tree, second quality roll, target selection, actual mutation/reordering and post-recruitment upgrading.

Bannerlord was not launched and the candidate DLL was not deployed.

## Source added

### Pure quality policy

`src/ClanAI/src/ClanAI/TroopQualityProbabilityPolicy.cs`

This class contains no TaleWorlds types.

Its quality multiplier is:

`qualityMultiplier = clamp(populationFactor * securityFactor * acuteFactor, 0.50, 1.00)`

For an occupied native-upgrade-eligible slot:

`p_quality = clamp(p_native * qualityMultiplier, 0, 1)`

It reuses the already-validated Phase 4B component functions for population, security and acute disruption.

It does **not** reuse Phase 4B's 0.35 minimum. Phase 4C has its own 0.50 minimum.

The pure quality multiplier does not take notable power, current tier, culture, troop type/tree or `UpgradeTargets` as multiplier inputs. Current tier participates only in the separate native eligibility mirror.

### Read-only native eligibility helper

`src/ClanAI/src/ClanAI/TroopQualityVolunteerEligibility.cs`

Quality eligibility requires:

- the volunteer exists;
- `UpgradeTargets` exists and is non-empty;
- current troop tier is below the selected inner model's `MaxVolunteerTier`.

The helper only reads those values. It does not select an upgrade target or mutate a volunteer.

### Existing selected wrapper extended

`LocalManpowerVolunteerModel.GetDailyVolunteerProductionProbability` still calls the wrapped inner production method exactly once.

Then it branches:

1. invalid slot -> existing native-safe passthrough;
2. empty slot -> existing Phase 4B `EvaluateEmptySlot` path;
3. occupied slot -> read selected inner `MaxVolunteerTier`;
4. native-non-upgradeable occupied slot -> Phase 4B-compatible native passthrough;
5. native-upgrade-eligible occupied slot -> Phase 4C `EvaluateOccupiedQualitySlot`.

No registration semantics changed. `SubModule.cs` remains unchanged from the Phase 4B selected-wrapper checkpoint.

## Phase 4B preservation

The Phase 4B pure policy file is byte-for-byte unchanged.

Git blob:

`f8b21dabb9df38df85fc0f83e6cfcad1a083f713`

The new Phase 4C invariant checks that exact blob.

The original Phase 4B tests and invariant file still pass:

```
PASS Phase 4B local manpower probability policy checks=50
PASS Phase 4B VolunteerModel delegation checks=13
PASS Phase 4B Local Manpower selected-model delegation invariant
PASS Phase 4B Local Manpower no-mutation invariant
PASS Phase 4B Local Manpower standalone-path invariant
```

The Phase 4C deterministic tests also re-check accepted empty-slot outputs: healthy High exact native, Mid 0.95, Low 0.80, the existing Phase 4B 0.35 minimum, and invalid-context passthrough.

## Phase 4C deterministic validation

`PASS Phase 4C troop quality probability policy checks=69`

Coverage includes native eligibility boundaries, healthy/Mid/Low contexts, security 0/25/50/>50, acute disruption, the Phase 4C 0.50 floor, probability bounds, missing security, non-finite numeric safety, exact healthy passthrough, and absence of notable-power/tier/culture/tree/target multiplier parameters.

Compiled wrapper validation:

`PASS Phase 4C VolunteerModel branch/delegation checks=23`

This proves:

- exact inner model reference is preserved;
- every non-production `VolunteerModel` member still delegates;
- invalid, empty, and occupied-nonupgradeable evaluations each call inner production exactly once;
- empty slots do not consult quality `MaxVolunteerTier`;
- occupied eligibility reads selected inner `MaxVolunteerTier`;
- occupied nonupgradeable state returns exact normal native probability;
- the compiled quality branch selector selects only occupied native-upgrade-eligible state.

Static invariants:

```
PASS Phase 4C selected-wrapper branch invariant
PASS Phase 4C native eligibility read-only invariant
PASS Phase 4C no-mutation invariant
PASS Phase 4C standalone-path invariant
PASS Phase 4B policy byte-for-byte preservation invariant
```

The Phase 4C invariant rejects direct volunteer-slot assignment, roster mutation, troop XP mutation, `UpgradeTargets` assignment, culture/tree mutation, garrison/militia/prosperity/hearth/security mutation, faction/settlement/war actions, RNG, War Strain/WarState/Home Responsibility coupling, external IO, development-tool dependencies and absolute development-machine runtime paths.

## Existing validation preserved

All required Phase 4A tests passed:

```
PASS Phase 4A same-hero recreation policy checks=43
PASS Phase 4A recovery policy tests checks=20
PASS Phase 4A same-hero recreation wiring and original-observer preservation
PASS Phase 4A same-hero recreation no-mutation and standalone invariant
PASS Phase 4A recovery wiring
PASS Phase 4A recovery no-mutation/standalone invariant
```

Relevant territorial guards also passed for Home Responsibility, Kingdom Objective, Visual War and Strategic Commitment, including no-mutation/standalone checks.

## Release build

```
Build succeeded.
1 Warning(s)
0 Error(s)
```

The sole warning remains the inherited MSB3277 `System.ValueTuple` conflict.

Offline Phase 4C candidate DLL SHA-256:

`A266E8A77E1BFA44EE8420C7F4023A8C470932340168B0A813E6C952DCFB6D6C`

Installed live DLL remained:

`9CB64EA90774A391D442FC32F460EF09E1D2901AEE3EBB194386E37A96F1C896`

Bannerlord was confirmed closed. The differing hashes prove the Phase 4C candidate was **not deployed**.

## Native authority retained

Phase 4C does not duplicate or alter Bannerlord's second occupied-volunteer quality gate:

`log2(notable.Power/currentTier) * 0.01`

It does not choose from `UpgradeTargets` and does not change culture, troop tree, basic/elite source, max volunteer tier, notable power, current tier, RNG, recruitment choices, actual slot mutation, party XP/upgrades, garrison XP, prisoners, mercenaries or post-defeat recreation.

## Capability status

- **Implemented:** yes.
- **Build-proven:** yes.
- **Runtime-observed:** no.
- **Balance-proven:** no.

The existing Phase 4B runtime proof is not a Phase 4C runtime proof.

## Next milestone

The next milestone is a separately authorized bounded Phase 4C runtime characterization of this exact validated candidate.

A later runtime proof should preserve Phase 4B empty-slot behavior, capture healthy and naturally degraded native-upgrade-eligible occupied evaluations, and preferably observe Bannerlord transition an exact slot to one of the previous troop's native direct `UpgradeTargets`.

Do not launch that proof from this checkpoint. Do not begin Phase 5.
