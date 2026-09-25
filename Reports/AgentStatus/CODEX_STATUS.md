# Codex Status

## Current checkpoint

Phase 4C Troop Quality v1 is now **implemented and build-proven offline**.

**Runtime-observed: no. Balance-proven: no.**

Phase 4A remains closed. Phase 4B remains unchanged and retains its implemented/build-proven/runtime-observed status. Phase 5 has not started.

## Phase 4C-v1 implementation

New source:

- `TroopQualityProbabilityPolicy.cs` — pure TaleWorlds-free quality first-gate policy;
- `TroopQualityVolunteerEligibility.cs` — read-only mirror of native occupied-slot eligibility;
- existing `LocalManpowerVolunteerModel` extended in-place.

No second `VolunteerModel` wrapper was added.

The selected wrapper still calls the inner production method exactly once.

Branching is:

- invalid slot/context -> native-safe passthrough;
- empty slot -> existing Phase 4B Local Manpower path;
- occupied non-upgradeable -> native probability passthrough;
- occupied native-upgrade-eligible -> Phase 4C quality first-gate policy.

Native eligibility requires only volunteer exists, direct `UpgradeTargets` exist, and current tier < selected inner `MaxVolunteerTier`.

## Quality policy

Population/security/acute components remain 1.00/0.95/0.80, the audited security curve, and 0.50 acute disruption.

Phase 4C uses its own:

`qualityMultiplier >= 0.50`

Phase 4B's 0.35 minimum remains unchanged.

For native-upgrade-eligible occupied slots:

`p_quality = clamp(p_native * qualityMultiplier, 0, 1)`

Notable power and current tier are not custom multipliers. Bannerlord's native second gate remains untouched.

## Phase 4B preservation

`LocalManpowerProbabilityPolicy.cs` remains byte-for-byte at Git blob:

`f8b21dabb9df38df85fc0f83e6cfcad1a083f713`

All original Phase 4B tests/invariants still pass.

## Validation

```
PASS Phase 4C troop quality probability policy checks=69
PASS Phase 4C VolunteerModel branch/delegation checks=23
PASS Phase 4C selected-wrapper branch invariant
PASS Phase 4C native eligibility read-only invariant
PASS Phase 4C no-mutation invariant
PASS Phase 4C standalone-path invariant
PASS Phase 4B policy byte-for-byte preservation invariant
```

Preserved Phase 4B:

```
PASS Phase 4B local manpower probability policy checks=50
PASS Phase 4B VolunteerModel delegation checks=13
PASS Phase 4B Local Manpower selected-model delegation invariant
PASS Phase 4B Local Manpower no-mutation invariant
PASS Phase 4B Local Manpower standalone-path invariant
```

All Phase 4A tests remain green. Relevant Home Responsibility, Kingdom Objective, Visual War and Strategic Commitment invariants also pass.

Release:

```
Build succeeded.
1 Warning(s)
0 Error(s)
```

The warning remains the inherited MSB3277 `System.ValueTuple` conflict.

Offline Phase 4C DLL SHA-256:

`A266E8A77E1BFA44EE8420C7F4023A8C470932340168B0A813E6C952DCFB6D6C`

Installed live DLL remained:

`9CB64EA90774A391D442FC32F460EF09E1D2901AEE3EBB194386E37A96F1C896`

Bannerlord was closed. The Phase 4C build was not deployed.

## Native authority

Phase 4C does not duplicate or alter Bannerlord's second quality gate, notable power, tier, `UpgradeTargets`, target selection, RNG, culture/tree, slot mutation, party XP/upgrades, garrison XP, prisoners, mercenaries or post-defeat recreation.

## Evidence

- `Reports/Manpower/PHASE4C_TROOP_QUALITY_V1_OFFLINE_IMPLEMENTATION_RESULT.md`
- `Reports/Manpower/evidence/phase4c_troop_quality_v1_offline_implementation_20260925.txt`
- design: `Reports/Manpower/PHASE4C_TROOP_QUALITY_NATIVE_CAPABILITY_AUDIT.md`

## Capability status

Implemented: **yes**.  
Build-proven: **yes**.  
Runtime-observed: **no**.  
Balance-proven: **no**.

## Next milestone

The next milestone is a **separately authorized bounded Phase 4C runtime characterization** of this exact validated candidate.

Do not launch that runtime proof from this checkpoint. Do not begin Phase 5.

Final product direction remains a standalone, installable, offline Bannerlord mod with no runtime dependency on ChatGPT, Codex, Desktop Commander, TestRunner, watchdogs, external IO or development-machine absolute paths.
