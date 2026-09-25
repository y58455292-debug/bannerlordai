# Phase 4C — Troop Quality v1 bounded runtime characterization

Date: 2026-09-25 UTC  
Offline policy/source checkpoint: `51084dd61928308373b72f0fd252720601fd2756`  
Observation-only telemetry checkpoint: `7fb5355b1db215a7bd0164baf082ebae13c76704`  
Runtime DLL SHA-256: `F8EC94C8973C180F7BEA688016B272C690728A188D61EF05166E3ED51AE445DC`

## Result

**STRONG PASS — Phase 4C Troop Quality v1 is now runtime-observed for the required probability contract, and native direct-target quality mutation was also observed. Balance remains unproven.**

The validated Phase 4B and Phase 4C policy files were not changed for this runtime proof. Minimum observation-only telemetry was added, fully revalidated offline, checkpointed, and then deployed.

The run naturally produced:

- selected-wrapper proof;
- Phase 4B empty-slot non-regression;
- occupied non-upgradeable native passthrough;
- healthy native-upgrade-eligible Phase 4C passthrough;
- naturally degraded native-upgrade-eligible Phase 4C slowdown;
- a Bannerlord-owned direct native volunteer upgrade after a Phase 4C evaluation;
- zero telemetry errors;
- zero direct ClanAI volunteer/troop mutation.

No settlement conditions, volunteer slots, troop trees, rosters, raids, sieges, recruitment decisions, or other campaign state were manufactured to obtain the result.

Phase 5 was not started.

## Candidate provenance

Authoritative offline Phase 4C implementation:

`51084dd61928308373b72f0fd252720601fd2756`

The offline DLL for that checkpoint was:

`A266E8A77E1BFA44EE8420C7F4023A8C470932340168B0A813E6C952DCFB6D6C`

That DLL did not expose enough runtime evidence for Phase 4C eligibility, first-gate quality factors, or direct-target lineage.

Observation-only telemetry was added and revalidated at:

`7fb5355b1db215a7bd0164baf082ebae13c76704`

The deployed runtime DLL became:

`F8EC94C8973C180F7BEA688016B272C690728A188D61EF05166E3ED51AE445DC`

Policy blobs remained unchanged:

- Phase 4B `LocalManpowerProbabilityPolicy.cs`: `f8b21dabb9df38df85fc0f83e6cfcad1a083f713`;
- Phase 4C `TroopQualityProbabilityPolicy.cs`: `d7b997d7c99e2d768bff97bf10057748547a32e8`.

No probability factor, native eligibility rule, or selected-model registration behavior was changed to make the runtime proof easier.

## Bound

Protected-fixture campaign start:

`649491.27044636116`

Healthy Phase 4C and Phase 4B-empty samples:

`649491.38230336108`

Elapsed:

**0.11185699992 campaign hours**

Native direct-target quality upgrade:

`649491.62230336107`

Elapsed:

**0.35185699991 campaign hours**

Minimum A–E proof completed at the occupied non-upgradeable sample:

`649492.04582108336`

Elapsed:

**0.77537472220 campaign hours**

Pause acknowledgement:

`649492.34227611113`

Elapsed:

**1.07182974997 campaign hours**

Maximum permitted bound: **168 campaign hours**.

Stop reason:

`minimum-phase4c-proof`

The run therefore stopped immediately once the minimum required proof had been observed. The native quality upgrade had already occurred before the minimum proof cutoff.

## A. Wrapper selection — PASS

Exact runtime line:

```
2026-09-25T19:50:30.5530397Z [ClanAI v0.22A-ruler-courtship-native-v1] LOCAL_MANPOWER_MODEL_SELECTED inner=TaleWorlds.CampaignSystem.GameComponents.DefaultVolunteerModel wrapper=ClanAI.LocalManpowerVolunteerModel selected=ClanAI.LocalManpowerVolunteerModel selectedWrapper=True policy=phase4b-empty+phase4c-occupied-native-eligible mutation=False
```

Observed:

- one selected wrapper: `LocalManpowerVolunteerModel`;
- wrapped inner model: `DefaultVolunteerModel`;
- selected wrapper reference confirmed;
- combined branch policy identified;
- telemetry declares no mutation.

## B. Phase 4B empty-slot preservation — PASS

Natural sample: Vinela, Calytos of Vinela, slot 5.

```
2026-09-25T19:51:51.1374519Z [ClanAI v0.22A-ruler-courtship-native-v1] LOCAL_MANPOWER_EVAL_SAMPLE sample=healthy-empty campaignHour=649491.38230336108 settlementId=village_EW4_3 settlement=Vinela kind=Village notableId=CharacterObject_2344 notable=Calytos_of_Vinela notableCulture=empire slot=5 slotKnown=True slotState=empty populationBand=High prosperity=<n/a> hearth=624.9296 securityAvailable=True security=99.15879 underRaid=False underSiege=False nativeProbability=0.08823674 populationFactor=1 securityFactor=1 acuteFactor=1 localMultiplier=1 finalProbability=0.08823674 applied=False reason=healthy-native-rate mutationByClanAI=False
```

The existing Phase 4B branch remains active for an empty slot:

- High population;
- security 99.15879;
- no raid/siege;
- native probability `0.08823674`;
- final probability `0.08823674`.

This is regression evidence only; Phase 4B was not retuned.

## C. Occupied non-upgradeable passthrough — PASS

Natural sample: Tarcutis, Hophtalamos of Tarcutis, slot 5.

```
2026-09-25T19:51:51.7599024Z [ClanAI v0.22A-ruler-courtship-native-v1] TROOP_QUALITY_EVAL_SAMPLE sample=quality-nonupgradeable campaignHour=649492.04582108336 settlementId=castle_village_EW3_2 settlement=Tarcutis kind=Village notableId=CharacterObject_13766 notable=Hophtalamos_of_Tarcutis notableCulture=empire slot=5 slotState=occupied volunteer=imperial_heavy_horseman[tier=4,culture=empire] currentTier=4 maxVolunteerTier=4 upgradeTargetsCount=1 directUpgradeTargets=imperial_cataphract[tier=5,culture=empire] nativeUpgradeEligible=False eligibilityReason=tier-at-or-above-max populationBand=High prosperity=<n/a> hearth=695.640137 securityAvailable=True security=97.50808 underRaid=False underSiege=False nativeProbability=0.08823674 populationFactor=1 securityFactor=1 acuteFactor=1 qualityMultiplier=1 finalProbability=0.08823674 applied=False branch=occupied-native-passthrough reason=invalid-or-unsupported-context mutationByClanAI=False
```

Observed:

- current tier = 4;
- selected inner max volunteer tier = 4;
- a direct target exists, but native eligibility is false because current tier is already at the max;
- native probability = `0.08823674`;
- final probability = `0.08823674`;
- Phase 4C is not applied.

This demonstrates that Phase 4C does not invent broader quality eligibility.

## D. Healthy native-upgrade-eligible quality passthrough — PASS

Natural sample: Vinela, Calytos of Vinela, slot 0.

```
2026-09-25T19:51:51.1364546Z [ClanAI v0.22A-ruler-courtship-native-v1] TROOP_QUALITY_EVAL_SAMPLE sample=quality-healthy campaignHour=649491.38230336108 settlementId=village_EW4_3 settlement=Vinela kind=Village notableId=CharacterObject_2344 notable=Calytos_of_Vinela notableCulture=empire slot=0 slotState=occupied volunteer=imperial_recruit[tier=1,culture=empire] currentTier=1 maxVolunteerTier=4 upgradeTargetsCount=2 directUpgradeTargets=imperial_infantryman[tier=2,culture=empire],imperial_archer[tier=2,culture=empire] nativeUpgradeEligible=True eligibilityReason=native-upgrade-eligible populationBand=High prosperity=<n/a> hearth=624.9296 securityAvailable=True security=99.15879 underRaid=False underSiege=False nativeProbability=0.525 populationFactor=1 securityFactor=1 acuteFactor=1 qualityMultiplier=1 finalProbability=0.525 applied=False branch=phase4c-quality-first-gate reason=healthy-native-quality-rate mutationByClanAI=False
```

Observed:

- occupied Empire recruit, tier 1;
- selected max tier 4;
- two direct native targets;
- native-upgrade-eligible = true;
- High population;
- security 99.15879;
- no raid/siege;
- native probability `0.525`;
- quality multiplier `1`;
- final probability `0.525`.

Healthy secure territory therefore preserves exact native first-gate quality progression.

## E. Naturally degraded native-upgrade-eligible quality slowdown — PASS

Natural sample: Lysia, Sanion of Lysia, slot 1.

```
2026-09-25T19:51:51.1384512Z [ClanAI v0.22A-ruler-courtship-native-v1] TROOP_QUALITY_EVAL_SAMPLE sample=quality-degraded campaignHour=649491.38230336108 settlementId=castle_village_EW1_2 settlement=Lysia kind=Village notableId=CharacterObject_2340 notable=Sanion_of_Lysia notableCulture=empire slot=1 slotState=occupied volunteer=imperial_vigla_recruit[tier=2,culture=empire] currentTier=2 maxVolunteerTier=4 upgradeTargetsCount=1 directUpgradeTargets=imperial_equite[tier=3,culture=empire] nativeUpgradeEligible=True eligibilityReason=native-upgrade-eligible populationBand=Mid prosperity=<n/a> hearth=412.8728 securityAvailable=True security=91.290535 underRaid=False underSiege=False nativeProbability=0.367499977 populationFactor=0.95 securityFactor=1 acuteFactor=1 qualityMultiplier=0.95 finalProbability=0.349124968 applied=True branch=phase4c-quality-first-gate reason=troop-quality-slowdown mutationByClanAI=False
```

The degradation was natural and came only from the native Mid hearth/population band:

- hearth `412.8728`;
- security remained high at `91.290535`;
- no raid/siege;
- native `0.367499977`;
- population factor `0.95`;
- security factor `1`;
- acute factor `1`;
- quality multiplier `0.95`;
- final `0.349124968`.

Thus the runtime contract `finalProbability < nativeProbability` is directly observed without manufacturing degradation.

This run does not runtime-prove the Low-population, sub-50-security, acute raid/siege, missing-security, or 0.50-floor branches. Those remain deterministic/build-proven.

## F. Native quality mutation authority — STRONG PASS

At Dradios, the native daily volunteer update naturally converted an occupied Empire archer into its direct native upgrade target.

```
2026-09-25T19:51:51.4413900Z [ClanAI v0.22A-ruler-courtship-native-v1] TROOP_QUALITY_NATIVE_UPGRADE campaignHour=649491.62230336107 settlementId=village_EW3_3 settlement=Dradios kind=Village notableId=CharacterObject_20265 notable=Diocosos_of_Dradios notableCulture=empire evaluatedSlot=4 before=imperial_archer[tier=2,culture=empire] after=imperial_trained_archer[tier=3,culture=empire] directUpgradeTarget=True sourceCountBefore=1 sourceCountAfter=0 targetCountBefore=0 targetCountAfter=1 qualityEvaluation=True evaluationSlotState=occupied evaluatedVolunteer=imperial_archer[tier=2,culture=empire] currentTier=2 maxVolunteerTier=4 upgradeTargetsCount=1 directUpgradeTargets=imperial_trained_archer[tier=3,culture=empire] nativeUpgradeEligible=True nativeProbability=0.1260525 populationFactor=1 securityFactor=1 acuteFactor=1 qualityMultiplier=1 finalProbability=0.1260525 evaluationReason=healthy-native-quality-rate source=native-daily-volunteer-update mutationByClanAI=False
```

This evidence is robust to native slot reordering because the observer compares the notable's before/after troop counts:

- source troop count: 1 -> 0;
- direct target count: 0 -> 1;
- target is explicitly in the prior troop's direct `UpgradeTargets`;
- source = native daily volunteer update;
- `mutationByClanAI=False`.

Phase 4C supplied only the first-gate probability. Bannerlord retained the second notable-power/current-tier roll, direct target selection, RNG, culture/tree and actual slot mutation.

## Supporting shared-pool observation

The same brief run also naturally logged native AI notable-slot consumption, for example:

```
2026-09-25T19:51:51.5683458Z [ClanAI v0.22A-ruler-courtship-native-v1] LOCAL_MANPOWER_SHARED_POOL_CONSUMPTION campaignHour=649491.80645263894 consumerPartyId=CharacterObject_3145_party_1 consumer=Ascytala's_Party notableId=CharacterObject_5088 notable=Anjikhin_the_Spider notableCulture=khuzait slot=0 troop=khuzait_nomad[tier=1,culture=khuzait] after=<empty> source=native-ai-notable-recruitment mutationByClanAI=False
```

The full filtered trace contains 31 native AI notable-slot consumption lines.

This is supporting shared-pool evidence only; it is not part of the Phase 4C pass condition and was not used to extend the run. No separate garrison consumption event is claimed.

## Trace / observer health

Full relevant trace through no-save exit:

- 70 filtered Local Manpower / Troop Quality lines;
- 3 Phase 4C evaluation samples: healthy, degraded, non-upgradeable;
- 3 Phase 4B evaluation samples;
- 30 native slot-transition observations;
- 1 explicit native direct-target quality upgrade;
- 31 native AI shared-pool consumption observations;
- 0 telemetry errors.

Normalized-LF SHA-256 of the full 70-line relevant trace:

`460c511b4eea8e0e694c0952669424438cba4db3cbaddaa2a85677430239d57b`

Minimum-proof window SHA-256:

`86a454fbf9edc7efb5189261abd324e8472e16749943607056fe7116f2cc6dae`

The minimum proof completed on the Tarcutis non-upgradeable line at `649492.04582108336`. Later natural pause-latency lines are retained in evidence but are not required for the pass.

## Scope

This result proves only the shared notable-volunteer quality first-gate behavior.

It does **not** claim control over:

- party XP/upgrades;
- garrison training/upgrades;
- prisoner recruitment;
- mercenary quality;
- post-defeat recreation quality.

Those remain separate native veteran-recovery paths exactly as defined by the Phase 4C audit.

## Safety / cleanup

Before deployment:

- Bannerlord closed;
- candidate hash verified;
- rollback created and hash-verified;
- Strategic Commitment = `Mode=Observe`;
- Visual War OFF;
- protected fixture baseline recorded.

Rollback SHA-256:

`9CB64EA90774A391D442FC32F460EF09E1D2901AEE3EBB194386E37A96F1C896`

The run used `EXIT_NOSAVE`.

Fresh TestRunner chronology contains zero save commands.

After cleanup:

- Bannerlord closed;
- fixture SHA-256 unchanged: `A91F15BF1403F1D29F942C56A1162F431113942CDACCAFE52F4D80303B4CB427`;
- fixture timestamp unchanged: `2026-09-24T17:20:12.2384633Z`;
- rollback hash unchanged;
- Strategic Commitment still `Mode=Observe`;
- Visual War still OFF.

## Capability status

- **Implemented:** yes.
- **Build-proven:** yes.
- **Runtime-observed:** yes.
- **Native quality mutation observed:** yes.
- **Balance-proven:** **no**.

The result demonstrates mechanism correctness, not long-run campaign balance.

## Closure

This completes the requested Phase 4C runtime characterization.

Stop at this Phase 4C checkpoint.

Do not begin Phase 5 in the same task.
