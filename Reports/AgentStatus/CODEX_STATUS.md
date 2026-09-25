# Codex Status

## Current checkpoint

Phase 4B Local Manpower v1 is **implemented and build-proven offline, but not runtime-observed**.

The implementation follows the audited native seam: a delegating `VolunteerModel` wraps Bannerlord's already-selected model and modifies only empty-slot daily volunteer production probability. Bannerlord was not launched, no DLL was deployed, and no campaign was run.

Phase 4A remains closed and unchanged. Phase 4C is not started.

## Phase 4B-v1 implementation

New source:

- `LocalManpowerProbabilityPolicy.cs`
- `LocalManpowerVolunteerModel.cs`
- selected-model registration in `SubModule.cs`

For normal native probabilities, occupied slots and unsupported contexts pass through unchanged. Only supported empty slots receive the audited population/security/acute multiplier.

The wrapper uses native town/village `GetProsperityLevel()`, town/bound-town `Security`, and active `IsUnderRaid`/`IsUnderSiege`. It does not add historical `IsRaided`, War Strain, culture/tier, militia/garrison, political/social, or Home Responsibility inputs.

All other `VolunteerModel` members delegate unchanged, and the inner production method is called exactly once.

## Validation

```
PASS Phase 4B local manpower probability policy checks=50
PASS Phase 4B VolunteerModel delegation checks=13
PASS Phase 4B Local Manpower selected-model delegation invariant
PASS Phase 4B Local Manpower no-mutation invariant
PASS Phase 4B Local Manpower standalone-path invariant
```

All Phase 4A tests remain green. Relevant Home Responsibility, Kingdom Objective, Visual War, and Strategic Commitment invariants also pass.

Release:

```
Build succeeded.
1 Warning(s)
0 Error(s)
```

The warning is the inherited MSB3277 `System.ValueTuple` conflict.

Offline build SHA-256:

`7A90733AA467127720ABA9426BC6BCE6975B7A514043A677FDBFEA19B56CAEE9`

Installed live DLL remained:

`0D23F4A66E0A1E4413E879C97E963C7DB923D8D6B4421291105D8565120CD5A8`

Bannerlord was not running. The Phase 4B build was not deployed.

War Strain and Phase 4A source remain byte-for-byte unchanged.

## Capability status

Implemented: **yes**.  
Build-proven: **yes**.  
Runtime-observed: **no**.

No campaign-effect or balance claim is made.

## Evidence

- `Reports/Manpower/PHASE4B_LOCAL_MANPOWER_V1_OFFLINE_IMPLEMENTATION_RESULT.md`
- `Reports/Manpower/evidence/phase4b_local_manpower_v1_offline_implementation_20260925.txt`
- design: `Reports/Manpower/PHASE4B_LOCAL_MANPOWER_NATIVE_CAPABILITY_AUDIT.md`

## Next milestone

The next milestone is a **separately authorized bounded runtime characterization** of this exact validated Phase 4B model.

Do not launch that proof from this checkpoint. Do not start Phase 4C.

Final product direction remains a standalone, installable, offline Bannerlord mod with no runtime dependency on ChatGPT, Codex, Desktop Commander, TestRunner, watchdogs, external IO, or development-machine absolute paths.
