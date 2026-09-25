# Phase 4B — Local Manpower v1 offline implementation result

Date: 2026-09-25 UTC  
Base: `13a9541743f7f5243a15f4c18a63220421b5974c`

## Result

**PASSED — implemented and build-proven offline; not runtime-observed.**

The audited Phase 4B-v1 seam is implemented as a pure probability policy plus a delegating `VolunteerModel` wrapper around Bannerlord's already-selected model. The wrapper is registered explicitly through `CampaignGameStarter`.

Bannerlord was not launched, the new DLL was not deployed, and no campaign was run. This checkpoint makes no runtime or balance claim.

## Implementation

New runtime source:

- `LocalManpowerProbabilityPolicy.cs` — TaleWorlds-free numeric policy;
- `LocalManpowerVolunteerModel.cs` — selected-model delegation;
- `SubModule.cs` — smallest explicit model registration.

For normal finite native probabilities:

- occupied slot: exact native passthrough;
- invalid/unsupported context: native passthrough;
- empty supported slot: apply Local Manpower multiplier.

The wrapper asks native town/village components for `GetProsperityLevel()`; it does not duplicate Bannerlord's prosperity/hearth thresholds.

Policy constants are the audited v1 values:

- population: High 1.00, Mid 0.95, Low 0.80;
- security: `clamp(0.80 + 0.20 * clamp(security,0,50)/50,0.80,1.00)`;
- active raid/siege: 0.50;
- combined multiplier: 0.35..1.00.

Town security uses the town's `Security`. Village security uses the bound fortification's `Town.Security` when available; missing security contributes factor 1.00.

There is no separate long-lived `IsRaided`/raid-history scalar.

For an empty slot:

`p_final = clamp(p_native * localMultiplier, 0, 1)`

The model cannot increase a normal native probability. Healthy/high-population, security >=50, non-disrupted context preserves native probability exactly.

Non-finite numeric inputs are sanitized so the wrapper cannot emit NaN/Infinity.

## Native delegation

`LocalManpowerVolunteerModel` delegates unchanged:

- `MaxVolunteerTier`;
- `MaximumIndexHeroCanRecruitFromHero`;
- `MaximumIndexGarrisonCanRecruitFromHero`;
- `GetBasicVolunteer`;
- `CanHaveRecruits`.

The inner `GetDailyVolunteerProductionProbability` appears exactly once and is evaluated exactly once per wrapper evaluation.

Registration reads `starter.GetModel<VolunteerModel>()`, wraps that exact instance, adds the wrapper as `VolunteerModel`, and verifies the selected model is the wrapper. No `DefaultVolunteerModel` assumption is introduced.

## Safety / exclusions

The Phase 4B source contains no:

- RNG;
- `VolunteerTypes[index] = ...` assignment;
- roster mutation;
- garrison/militia mutation;
- prosperity/hearth/security mutation;
- troop type/tier/culture modification;
- AI/player recruitment decision override;
- settlement/faction/war mutation;
- external IO;
- development-machine path;
- development-tool runtime dependency.

War Strain remains excluded and unchanged. The existing `WarStrainRecruitmentPatch.cs` Git blob is still:

`dc25c2f333badb1d059c0ba1b2c1a4e9d6d2370d`

Phase 4A recreation source is byte-for-byte unchanged:

- `Phase4ARecoveryObserverBehavior.Recreation.cs`: `0c735316e3ce9a0c32189909518ed8f99d4670e0`
- `Phase4ARecreationLinkPolicy.cs`: `b926af2d6495005fca62d2d40757e82553c706c0`

## Validation

Pure policy:

`PASS Phase 4B local manpower probability policy checks=50`

The deterministic cases cover occupied/invalid passthrough, population bands, security 0/25/50/>50, missing security, acute disruption, multiplier floor, final <= native, [0,1] bounds, native zero, positive refill floor, unsupported population, non-finite numeric safety, and absence of culture/troop/tier policy inputs.

Compiled delegation:

`PASS Phase 4B VolunteerModel delegation checks=13`

A fake inner `VolunteerModel` proves exact inner-reference preservation, all non-production delegation, and exactly one native production call for an evaluation.

Static invariants:

```
PASS Phase 4B Local Manpower selected-model delegation invariant
PASS Phase 4B Local Manpower no-mutation invariant
PASS Phase 4B Local Manpower standalone-path invariant
```

All Phase 4A tests remain green:

```
PASS Phase 4A same-hero recreation policy checks=43
PASS Phase 4A recovery policy tests checks=20
PASS Phase 4A same-hero recreation wiring and original-observer preservation
PASS Phase 4A same-hero recreation no-mutation and standalone invariant
PASS Phase 4A recovery wiring
PASS Phase 4A recovery no-mutation/standalone invariant
```

Relevant Home Responsibility, Kingdom Objective, Visual War, and Strategic Commitment runtime-wiring/no-mutation/standalone invariants also pass.

## Release build / no deployment

Release result:

```
Build succeeded.
1 Warning(s)
0 Error(s)
```

The warning is the inherited MSB3277 `System.ValueTuple` conflict.

Offline built DLL SHA-256:

`7A90733AA467127720ABA9426BC6BCE6975B7A514043A677FDBFEA19B56CAEE9`

Installed live DLL SHA-256 remained:

`0D23F4A66E0A1E4413E879C97E963C7DB923D8D6B4421291105D8565120CD5A8`

Bannerlord was confirmed not running. The hashes differ, proving the Phase 4B offline build was not deployed.

The delegation test uses the development-only `BANNERLORD_GAME_DIR` MSBuild property to resolve test references; ClanAI runtime source has no dependency on it.

## Capability status

- **Implemented:** yes.
- **Build-proven:** yes.
- **Runtime-observed:** no.
- **Balance-proven:** no.

Do not claim Local Manpower affects a campaign until a later bounded runtime proof.

## Next milestone

The next milestone is a separately authorized bounded runtime characterization of this exact validated Phase 4B model. Do not tune values from deterministic tests and do not start Phase 4C.
