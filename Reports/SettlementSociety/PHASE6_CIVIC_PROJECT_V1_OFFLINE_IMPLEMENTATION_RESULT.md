# Phase 6-v1 Civic Project Choice — offline implementation result

Date: 2026-09-25 UTC  
Capability-audit checkpoint: `9758c65e1afe88eef769843316f21e113700381a`  
Validated source checkpoint: `3bd55ce1d54de3739d049598e5c1497a2f8f6ff8`

## Result

**PASS — the selected Phase 6-v1 civic-project decision seam is implemented and build-proven offline. Runtime observation, native final commit, player legibility, and balance proof have NOT been performed.**

The implementation changes only the selected daily-project **choice** for a narrow supported NPC-town context. It does not directly change loyalty, construction progress, queues, project flags, project effects, relations, notable/governor state, ownership, prosperity, security, militia, garrison, economy, or save data.

Bannerlord remains responsible for when daily projects are considered, all native project instances, the actual `BuildingHelper.ChangeDefaultBuilding` commit, project effects, persistence, and player settlement management.

## Implementation

### Pure policy

`CivicProjectSelectionPolicy` is game-assembly-free and consumes only:

- context validity;
- NPC-town status;
- construction-idle status;
- Festival and Games availability;
- current loyalty;
- the native rebellious-state loyalty threshold.

It prefers Festival and Games only when all gates are valid and:

`loyalty < nativeRebelliousThreshold`

Equality is exact passthrough. Non-finite loyalty or threshold values fail conservatively.

No prosperity, security, militia, garrison, raid history, patrol count, War State, War Strain, Home Responsibility, Visual War, notable, governor, social-memory, or external-state input is used.

### Selected-model wrapper

`CivicProjectBuildingScoreCalculationModel` delegates an existing `BuildingScoreCalculationModel`.

For `GetNextDailyBuilding(Town)` it:

1. calls the inner native selector exactly once;
2. preserves that exact native result by default;
3. requires the audited exact inner type `DefaultBuildingScoreCalculationModel`;
4. requires a normal NPC-owned town;
5. requires an empty construction queue and an existing native daily default project;
6. requires the native result itself to be an existing daily project in the town;
7. reads the threshold from the currently selected `SettlementLoyaltyModel.RebelliousStateStartLoyaltyThreshold`;
8. identifies Festival and Games using the strong native identity `DefaultBuildingTypes.SettlementDailyFestivalAndGames`;
9. returns the town's existing Festival and Games `Building` reference only when the pure policy says to prefer it.

`GetNextBuilding(Town)` delegates unchanged.

### Compatibility boundary

The audit established the random-choice behavior specifically for the supported `DefaultBuildingScoreCalculationModel`. Therefore v1 installs the wrapper only when the currently selected inner model is that exact audited type.

Unknown, foreign, derived, or already-wrapped building selectors are not silently overridden. If another model becomes selected later in the module chain, runtime characterization must detect that the Phase 6 wrapper is not the active selected model rather than claiming success.

The loyalty threshold is **not** hardcoded; it comes from the selected native loyalty model.

## Native authority retained

The implementation does not call `BuildingHelper.ChangeDefaultBuilding` and does not patch the final mutation.

It never constructs a new `Building`, never enqueues/dequeues a project, and never writes `Town.Loyalty`, `CurrentDefaultBuilding`, prosperity, security, militia, garrison, relations, ownership, or save state.

If the existing current daily default is already Festival and Games, returning the same existing reference leaves the native caller's own identity check responsible for avoiding a redundant commit.

## Deterministic validation

Phase 6 policy tests:

`PASS Phase 6 civic-project policy checks=23`

Coverage includes invalid context, player/non-NPC ownership, active construction, missing Festival and Games, above/equal/just-below threshold, very low loyalty, non-finite loyalty, non-finite threshold, the selected high-rebellion threshold case, reason codes, and exact minimal input shape.

Compiled wrapper/delegation tests:

`PASS Phase 6 civic-project delegation checks=14`

They prove:

- exact inner-model reference retention;
- ordinary building selection delegates unchanged;
- foreign inner daily selection is exact passthrough;
- inner daily selection is called exactly once;
- the audited default model is recognized while a foreign model is not;
- default result selection returns the exact native `Building` reference;
- special result selection returns the exact existing Festival and Games reference;
- missing Festival and Games falls back to the exact native result;
- null inner model is rejected.

Static invariants passed:

```
PASS Phase 6 civic-project NPC-only/idle-town wiring invariant
PASS Phase 6 civic-project no-mutation invariant
PASS Phase 6 civic-project standalone-path invariant
PASS Phase 4B/4C and Phase 5 preservation invariant
```

## Preservation validation

All requested retained Phase 4 checks passed, including Phase 4A recovery/recreation, Phase 4B Local Manpower, and Phase 4C Troop Quality deterministic and delegation coverage.

All Phase 5 Local Bandit Control policy, private-target resolution, wiring, no-mutation, standalone, and preservation checks passed.

Relevant Phase 3 Kingdom Objective, Home Responsibility, Strategic Commitment, and Visual War wiring/no-mutation/standalone/policy checks all passed.

Accepted Phase 4B, Phase 4C, and Phase 5 policy/source blobs remain unchanged.

## Release build

```
Build succeeded.
1 Warning(s)
0 Error(s)
```

The warning is the inherited `System.ValueTuple` MSB3277 conflict. The preview .NET SDK notice is an environment message, not a product error.

Offline candidate DLL SHA-256:

`2C2C89D5D1086CF119E7D439F08538F4552645A047738B32240144283CD12CE3`

Pure policy source SHA-256:

`2AE3008F7FF5E3F13135425B4B970C0C9E5F24869228F43DCDB19BEF215C41A3`

Wrapper source SHA-256:

`8BDC5D04487C958C008F97FCAC07AD5AED4A9B4F5F38097C9BE7C93DE982686C`

Exact command output is preserved in:

`Reports/SettlementSociety/evidence/phase6_civic_project_v1_offline_validation_20260925.txt`

## Runtime status

Bannerlord was not launched. No DLL was deployed. No campaign was run. No native final `ChangeDefaultBuilding` commit is claimed.

No player-facing message or UI proof is claimed.

Phase 7 was not started.

## Capability status

Phase 6-v1:

- **Implemented:** yes.
- **Build-proven:** yes.
- **Runtime-observed:** **NO**.
- **Native final commit observed:** **NO**.
- **Player-legible:** **NOT YET PROVEN**.
- **Balance-proven:** **NO**.

## Next bounded milestone

The next milestone is a separately authorized bounded runtime characterization of this exact validated candidate.

It should prove that the wrapper is actually selected in the loaded module stack, a naturally low-loyalty idle NPC town can produce a Festival and Games substitution, Bannerlord's native caller performs the actual `ChangeDefaultBuilding` commit, and naturally available player/high-loyalty/active-construction cases remain passthrough.

Do not launch that runtime proof from this checkpoint. Do not retune Phase 4 or Phase 5, broaden Phase 6 inputs, patch the final mutation, or begin Phase 7.

[executed on device: DESKTOP-JO4B7VH (fd6618f4-5715-46b1-8665-68172ef15169)]