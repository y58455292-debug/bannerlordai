# Phase 7B — natural succession runtime preflight

Date: 2026-09-25 UTC  
Authoritative base: `3bd2e278fc80f30be4f0e3f96c5416cfb83aca03`  
Scope: telemetry/preflight only; no succession run.

## Result

**TELEMETRY / PREFLIGHT READY: YES. GAMEPLAY CHANGED: NO. SUCCESSION RUN PERFORMED: NO. SELECTED NATURAL TARGET RULER / CLAN: NOT YET SELECTED.**

Phase 7B now has the minimum observation surface needed for the Phase 7A-selected test: one natural ruler-and-clan-leader succession followed by one guarded save/reload.

No age, death, ruler, clan-leader, family, settlement, kingdom, memory-inheritance, or succession behavior was changed. No save schema was added. `DynastyBranchEpisodeMemory` was not changed or fixed.

## Observation behavior

New session-only behavior:

`GenerationalContinuityPreflightBehavior`

It listens only to native campaign events:

- `BeforeHeroKilledEvent`;
- `HeroKilledEvent`;
- `OnClanLeaderChangedEvent`;
- `RulingClanChanged`;
- `OnBeforeSaveEvent`;
- `OnSaveOverEvent`;
- `OnGameLoadFinishedEvent`;
- session launch.

Its `SyncData` override is intentionally empty.

## Snapshot coverage

For each active kingdom ruler snapshot, telemetry records:

- target ruler hero ID/name/dead state;
- ruling clan ID;
- kingdom ID and culture;
- current clan-leader ID;
- clan-owned settlement IDs;
- spouse ID;
- children IDs;
- family IDs where available;
- kingdom-continuity succession count;
- clan-bound holding-loss record count;
- hero-bound NobleMemory count;
- hero-bound SocialLedger record count;
- companion duty/experience/negative-outcome counts;
- DynastyBranchEpisodeMemory actor count and total count;
- total SocialEpisode count;
- WarScar count and settlement IDs;
- active siege IDs/count;
- raid/loss structural IDs/counts;
- active objective IDs/count;
- WarStrain kingdom IDs;
- whether any cross-hero memory application has been detected;
- `mutationByClanAI=False`.

Snapshot stages are prepared for:

- `session-start`;
- `clan-leader-change`;
- `ruling-clan-change`;
- `pre-save-post-succession`;
- `post-reload`.

This allows the future guarded run to compare the same native and ClanAI identities before save and after reload without persisting telemetry state.

## Hero-memory identity guard

Existing person-bound memory reads now emit observation-only identity resolution to:

`GenerationalContinuityRuntimeTelemetry.ObserveHeroMemoryResolution(...)`

Covered paths include:

- SocialLedger direct hero lookup;
- SocialMemory strategic application;
- SocialLoyalty direct leader-memory application;
- CompanionDutyMemory application;
- CompanionExperienceMemory application;
- CompanionNegativeOutcomeMemory lookup.

The observer records:

- store;
- requested hero ID;
- resolved record hero ID;
- whether influence was applied;
- whether the IDs cross hero identity;
- whether the resolved hero is known dead in the current session;
- whether a dead hero key was applied to another hero.

It does not alter the lookup result, score, factor, record, or key.

## Dynasty branch defect preserved

`DynastyBranchEpisodeMemory.cs` SHA-256 remains:

`C9321A57A5BC82EB2ED6D03474454968E680C5210700EDFFEA7CB54F94097E08`

The Phase 7A actor-ID retrieval defect is therefore still present and intentionally unfixed.

## Target selection

**NOT YET SELECTED.**

The preflight does not pick a ruler from stale historical state. At the start of the future natural run, the session-start ruler snapshots will enumerate current living rulers/clans and allow selection of a ruler who safely satisfies the Phase 7A criteria.

No campaign was launched to manufacture or preselect a target in this checkpoint.

## Validation

Exact evidence:

`Reports/GenerationalContinuity/evidence/phase7b_succession_runtime_preflight_validation_20260925.txt`

Passed:

- Phase 7B observation-only invariant;
- no-save-schema invariant;
- no lifecycle/succession mutation invariant;
- hero-memory identity-resolution invariant;
- Phase 7A DynastyBranchEpisodeMemory preservation invariant;
- existing KingdomContinuity ledger invariants;
- Phase 6 preservation invariants;
- Phase 5 preservation invariants;
- Phase 4B preservation invariants;
- Phase 4C preservation invariants;
- Strategic Commitment no-mutation invariant;
- Release build.

Release build:

```
Build succeeded.
1 Warning(s)
0 Error(s)
```

The warning is the inherited `System.ValueTuple` conflict.

Preflight candidate DLL SHA-256:

`B15B7B4D3F77EF40D6080EECF07B0077BA4B0E5ABD176C3C4328166BFB4A3ED0`

Preflight telemetry source SHA-256:

`1D558E44129910457CFDBF50CC331B0D2921F4BD25F4DFC2BC409783E1BB2D77`

## Checkpoint status

- Phase 7A continuity audit: **COMPLETE**
- Phase 7B telemetry/preflight ready: **YES**
- Phase 7 gameplay behavior changed: **NO**
- Save schema changed: **NO**
- Succession run performed: **NO**
- DLL deployed: **NO**
- Natural ruler/clan target: **NOT YET SELECTED**
- DynastyBranchEpisodeMemory fix: **NOT STARTED**
- Phase 7 long-run experiment: **NOT STARTED**

Stop here. The next task may use this exact candidate for the bounded natural succession + guarded save/reload experiment, but this checkpoint does not begin that run.