# Phase 4B — Local Manpower v1 bounded runtime characterization

Date: 2026-09-25 UTC  
Validated policy checkpoint: `88aff823d055b0b951d263f8259f5c3537ed6bfd`  
Observation-only telemetry checkpoint: `5a6b9757b33a33f356f537cced6a74051ca04e30`  
Runtime DLL SHA-256: `9CB64EA90774A391D442FC32F460EF09E1D2901AEE3EBB194386E37A96F1C896`

## Result

**PASSED — Local Manpower v1 is now runtime-observed for the minimum required probability contract, with a native volunteer-slot mutation also observed. Balance remains unproven.**

The underlying Phase 4B-v1 policy file remained byte-for-byte unchanged from the offline-validated checkpoint. The runtime DLL differs from the prior offline DLL only because minimal observation-only telemetry was added around the selected model and native recruitment pipeline.

The run stopped after the minimum A/B/C proof plus a native daily volunteer transition was observed. No threshold, factor, recruitment decision, volunteer pool, troop, settlement, faction, or war state was changed to manufacture evidence.

Phase 4C was not started.

## Bound

Protected-fixture campaign start:

`649491.27044636116`

First healthy/occupied samples:

`649491.370873`

Elapsed:

**0.10042663884 campaign hours**

Degraded sample and native transition:

`649491.450873`

Elapsed:

**0.18042663884 campaign hours**

Pause acknowledgement:

`649491.6858864167`

Elapsed:

**0.41544005554 campaign hours**

Maximum permitted bound: **168 campaign hours**.

Stop reason:

`minimum-proof-plus-native-fill`

The run therefore stopped extremely early once the required evidence existed.

## 1. Wrapper/model selection — PASS

Exact runtime line:

```
2026-09-25T09:04:41.0617285Z [ClanAI v0.22A-ruler-courtship-native-v1] LOCAL_MANPOWER_MODEL_SELECTED inner=TaleWorlds.CampaignSystem.GameComponents.DefaultVolunteerModel wrapper=ClanAI.LocalManpowerVolunteerModel selected=ClanAI.LocalManpowerVolunteerModel selectedWrapper=True policy=empty-slots-only mutation=False
```

Observed fact:

- wrapped inner model: `DefaultVolunteerModel`;
- selected model: `ClanAI.LocalManpowerVolunteerModel`;
- `selectedWrapper=True`;
- policy mode: `empty-slots-only`.

The telemetry hooks were also installed around native `RecruitmentCampaignBehavior.UpdateVolunteersOfNotablesInSettlement` and `GetRecruitVolunteerFromIndividual`, with `mutation=False`.

## 2. Healthy empty-slot passthrough — PASS

Natural sample: **Vinela**, village, notable **Calytos of Vinela**, slot 5.

Exact line:

```
2026-09-25T09:05:53.2157445Z [ClanAI v0.22A-ruler-courtship-native-v1] LOCAL_MANPOWER_EVAL_SAMPLE sample=healthy-empty campaignHour=649491.370873 settlementId=village_EW4_3 settlement=Vinela kind=Village notableId=CharacterObject_2344 notable=Calytos_of_Vinela notableCulture=empire slot=5 slotKnown=True slotState=empty populationBand=High prosperity=<n/a> hearth=624.9296 securityAvailable=True security=99.15879 underRaid=False underSiege=False nativeProbability=0.08823674 populationFactor=1 securityFactor=1 acuteFactor=1 localMultiplier=1 finalProbability=0.08823674 applied=False reason=healthy-native-rate mutationByClanAI=False
```

Required contract is satisfied:

- population band: **High**;
- hearth: **624.9296**;
- security: **99.15879**, above 50;
- active raid: false;
- active siege: false;
- native probability: **0.08823674**;
- multiplier: **1**;
- final probability: **0.08823674**.

Thus a healthy supported empty slot preserved the selected native probability exactly.

## 3. Naturally degraded empty-slot slowdown — PASS

Natural sample: **Lysia**, castle-bound village, notable **Sanion of Lysia**, slot 0.

Exact evaluation:

```
2026-09-25T09:05:53.3998055Z [ClanAI v0.22A-ruler-courtship-native-v1] LOCAL_MANPOWER_EVAL_SAMPLE sample=degraded-empty campaignHour=649491.450873 settlementId=castle_village_EW1_2 settlement=Lysia kind=Village notableId=CharacterObject_2340 notable=Sanion_of_Lysia notableCulture=empire slot=0 slotKnown=True slotState=empty populationBand=Mid prosperity=<n/a> hearth=412.8728 securityAvailable=True security=91.290535 underRaid=False underSiege=False nativeProbability=0.525 populationFactor=0.95 securityFactor=1 acuteFactor=1 localMultiplier=0.95 finalProbability=0.498749971 applied=True reason=local-manpower-slowdown mutationByClanAI=False
```

Observed fact:

- degradation was **natural Mid population**, not manufactured;
- hearth: **412.8728**;
- security remained healthy at **91.290535**;
- no raid/siege contribution was involved;
- native probability: **0.525**;
- population factor: **0.95**;
- security factor: **1**;
- acute factor: **1**;
- local multiplier: **0.95**;
- final probability: **0.498749971**.

Therefore `finalProbability < nativeProbability` exactly as designed.

This runtime sample validates the Mid-population branch. It does **not** runtime-prove Low population, sub-50 security, or acute raid/siege branches.

## 4. Occupied-slot passthrough — PASS

Natural sample: **Vinela**, notable **Calytos of Vinela**, occupied slot 0.

Exact line:

```
2026-09-25T09:05:53.2147456Z [ClanAI v0.22A-ruler-courtship-native-v1] LOCAL_MANPOWER_EVAL_SAMPLE sample=occupied campaignHour=649491.370873 settlementId=village_EW4_3 settlement=Vinela kind=Village notableId=CharacterObject_2344 notable=Calytos_of_Vinela notableCulture=empire slot=0 slotKnown=True slotState=occupied populationBand=High prosperity=<n/a> hearth=624.9296 securityAvailable=True security=99.15879 underRaid=False underSiege=False nativeProbability=0.525 populationFactor=1 securityFactor=1 acuteFactor=1 localMultiplier=1 finalProbability=0.525 applied=False reason=occupied-slot-native-passthrough mutationByClanAI=False
```

Native and final probability are both **0.525**.

This confirms the Phase 4C-preserving rule at runtime: occupied volunteer slots are not slowed by Local Manpower v1.

## 5. Native mutation authority — OBSERVED

Immediately after Sanion's degraded empty-slot evaluation, the observation-only postfix around Bannerlord's native daily volunteer update captured:

```
2026-09-25T09:05:53.4008062Z [ClanAI v0.22A-ruler-courtship-native-v1] LOCAL_MANPOWER_NATIVE_SLOT_TRANSITION campaignHour=649491.450873 settlementId=castle_village_EW1_2 settlement=Lysia kind=Village notableId=CharacterObject_2340 notable=Sanion_of_Lysia notableCulture=empire slot=0 before=<empty> after=imperial_vigla_recruit[tier=2,culture=empire] transition=fill-or-reorder-into-empty-slot evaluationSlotState=empty nativeProbability=0.525 populationFactor=0.95 securityFactor=1 acuteFactor=1 localMultiplier=0.95 finalProbability=0.498749971 evaluationReason=local-manpower-slowdown source=native-daily-volunteer-update mutationByClanAI=False
```

Observed fact:

- the wrapper supplied the adjusted probability;
- the native daily recruitment behavior then changed the notable's slot state;
- before: empty;
- after: `imperial_vigla_recruit`, tier 2, Empire culture;
- notable culture: Empire;
- source is explicitly the native daily volunteer update;
- `mutationByClanAI=False`.

The telemetry compares every slot before and after the native method. For Sanion, this was the only recorded slot transition in that native update, making a direct native fill the conservative interpretation despite the native method's possible internal slot reordering.

Additional natural native transitions occurred in the same brief run, including Empire recruits in Lysia/Aegosca and Battanian volunteers in Marunath.

This is direct runtime evidence that ClanAI did not fill the slot itself; Bannerlord remained the mutation authority.

## 6. Shared-pool AI/garrison consumption — NOT OBSERVED

The run stopped as soon as the required proof plus native fill was complete.

No `LOCAL_MANPOWER_SHARED_POOL_CONSUMPTION` record appeared before the stop.

This is **not** a null against shared-pool parity. The native-capability audit already established the shared notable-slot pipeline statically, but this particular bounded runtime characterization did not naturally observe an AI/garrison consumption event before the early stop.

No extension was performed merely to obtain supporting parity evidence.

## 7. Observer health / no direct mutation

Runtime flags:

- model selected: true;
- healthy sample: true;
- degraded sample: true;
- occupied sample: true;
- native fill transition: true;
- shared consumption: false;
- telemetry error: **false**.

No `LOCAL_MANPOWER_TELEMETRY_ERROR` line occurred.

The telemetry source had already passed the Phase 4B no-mutation and standalone invariants before deployment. The policy source and factors were not changed for this runtime run.

## 8. Runtime-observed versus offline-only branches

### Runtime-observed

- selected wrapper around `DefaultVolunteerModel`;
- empty High-population / high-security / no-disruption exact passthrough;
- empty Mid-population slowdown at factor 0.95;
- occupied-slot exact passthrough;
- native daily volunteer-slot transition after a Local Manpower evaluation;
- native troop culture/type remained Bannerlord-selected.

### Still only offline/build-proven

- Low-population factor 0.80;
- security penalties below 50;
- missing-security passthrough;
- acute raid/siege factor 0.50;
- minimum multiplier 0.35;
- naturally observed AI/garrison consumption under the new wrapper.

No tuning is justified from this single brief characterization.

## 9. Candidate provenance

The user's offline-validated policy checkpoint was:

`88aff823d055b0b951d263f8259f5c3537ed6bfd`

Its pure policy Git blob remained unchanged:

`f8b21dabb9df38df85fc0f83e6cfcad1a083f713`

Minimal observation telemetry was then added and revalidated at:

`5a6b9757b33a33f356f537cced6a74051ca04e30`

The telemetry candidate DLL is therefore:

`9CB64EA90774A391D442FC32F460EF09E1D2901AEE3EBB194386E37A96F1C896`

The earlier offline DLL hash `7A90733AA467127720ABA9426BC6BCE6975B7A514043A677FDBFEA19B56CAEE9` was not the deployed runtime artifact because it lacked the minimum observation telemetry required by this milestone. The policy/factors themselves are unchanged.

## 10. Safety / cleanup

Before deployment:

- Bannerlord closed;
- rollback created and hash-verified;
- Strategic Commitment: `Mode=Observe`;
- Visual War: OFF;
- protected fixture baseline recorded.

Rollback SHA-256:

`0D23F4A66E0A1E4413E879C97E963C7DB923D8D6B4421291105D8565120CD5A8`

The run ended with `EXIT_NOSAVE`.

Final TestRunner status recorded:

- `lastCommand=EXIT_NOSAVE`;
- `lastResult=exit_scheduled`;
- `timeControl=Stop`;
- `campaignHours=649491.6858864167`.

Fresh command-history scan found **0 save commands**.

After cleanup:

- Bannerlord closed;
- protected fixture SHA-256 unchanged: `A91F15BF1403F1D29F942C56A1162F431113942CDACCAFE52F4D80303B4CB427`;
- fixture timestamp unchanged: `2026-09-24T17:20:12.2384633Z`;
- rollback hash unchanged;
- Strategic Commitment still Observe;
- Visual War still OFF.

## Capability status

- **Implemented:** yes.
- **Build-proven:** yes.
- **Runtime-observed:** yes, for the minimum required probability contract and native mutation authority.
- **Native mutation observed:** yes.
- **Shared AI/garrison consumption observed:** no.
- **Balance proven:** **no**.

## Closure

This completes the requested Phase 4B Local Manpower v1 runtime characterization.

The result proves the mechanism executes in a real campaign and preserves native mutation authority. It does not establish that the constants are balanced over long campaigns.

Stop at this Phase 4B checkpoint. Do not start Phase 4C in the same task.
