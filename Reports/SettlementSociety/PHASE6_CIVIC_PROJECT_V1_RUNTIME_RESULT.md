# Phase 6-v1 Civic Project Choice — bounded runtime characterization

Date: 2026-09-25 UTC  
Offline gameplay checkpoint: `f9b873bdd416e24e2b2643631ab3bc1a7a03f6e3`  
Implementation checkpoint: `3bd55ce1d54de3739d049598e5c1497a2f8f6ff8`  
Observation-only telemetry checkpoint: `f0fe5b86aa71bd0df349d05858fc6ab203fcf30b`  
Runtime DLL SHA-256: `2909ADE31B427E122FDDE539229BD0A4A2B4FA91C84CFD3AB63D0C8EB6F729D7`

## Result

**BOUNDED NULL FOR THE TARGET SUBSTITUTION BRANCH — the Phase 6-v1 wrapper is runtime-observed and the native final commit authority is runtime-observed, but no naturally qualifying low-loyalty NPC-town evaluation occurred inside the bounded run. Therefore no genuine Festival and Games substitution or substituted-Festival native commit is claimed.**

The run established:

- the active selected model was exactly `ClanAI.CivicProjectBuildingScoreCalculationModel`;
- its inner model was exactly the audited `TaleWorlds.CampaignSystem.GameComponents.DefaultBuildingScoreCalculationModel`;
- the wrapper executed on native daily-project considerations;
- two natural NPC-town evaluations were observed;
- both NPC-town evaluations correctly preserved the exact native result;
- Bannerlord subsequently executed the native `BuildingHelper.ChangeDefaultBuilding` path using those returned choices;
- 12 total civic-project evaluations and 12 correlated native commit observations occurred;
- zero Phase 6 telemetry errors occurred;
- zero save commands were issued.

What was **not** observed:

- no evaluated NPC town had loyalty below the selected native rebellious-state threshold;
- no `preferFestival=True` evaluation occurred;
- no genuine `native != Festival -> final = Festival` substitution occurred;
- no native commit of a ClanAI-substituted Festival and Games project occurred.

No loyalty, queue, project, issue, governor, ownership, economy, or settlement state was manufactured to obtain a positive result.

## Candidate provenance

The accepted gameplay policy remained unchanged from the offline implementation checkpoint.

Pure policy source SHA-256:

`2AE3008F7FF5E3F13135425B4B970C0C9E5F24869228F43DCDB19BEF215C41A3`

Minimal runtime observation telemetry was added without changing the policy or decision gates and committed before deployment at:

`f0fe5b86aa71bd0df349d05858fc6ab203fcf30b`

The telemetry-validation build produced the deployed runtime DLL:

`2909ADE31B427E122FDDE539229BD0A4A2B4FA91C84CFD3AB63D0C8EB6F729D7`

Focused telemetry validation passed:

- Phase 6 policy: 23 checks;
- Phase 6 delegation: 14 checks;
- NPC-only/idle-town wiring invariant;
- no-mutation invariant;
- standalone-path invariant;
- observation-only telemetry invariant;
- Phase 6 pure policy byte-for-byte preservation invariant;
- Phase 4B/4C and Phase 5 preservation invariant;
- Release build: 0 errors, inherited `System.ValueTuple` warning only.

## Bound

Protected fixture:

`ClanAI V020V PERSIST DEMO GATE V021M 20260924`

Start campaign hour:

`649491.27044636116`

Pause acknowledgement:

`649647.05848927773`

Elapsed:

**155.78804291656706 campaign hours**

Maximum authorized bound:

**168 campaign hours**

The experiment stopped before the cap with a conservative control margin rather than risk pause-command latency crossing the hard boundary.

Stop reason:

`conservative-bound-margin-no-natural-qualifying-low-loyalty-town`

## A. Active model selection — PASS

Exact runtime line:

```
2026-09-25T23:41:10.1926554Z [ClanAI v0.22A-ruler-courtship-native-v1] CIVIC_PROJECT_MODEL_ACTIVE campaignHour=649491.27044636116 selected=ClanAI.CivicProjectBuildingScoreCalculationModel selectedWrapper=True inner=TaleWorlds.CampaignSystem.GameComponents.DefaultBuildingScoreCalculationModel exactAuditedInner=True mutation=False
```

This satisfies the compatibility gate:

- selected wrapper: exact Phase 6 wrapper;
- inner: exact audited default model;
- no unknown/foreign selector was overridden;
- no compatibility blocker was encountered.

## B. Natural NPC-town passthrough — PASS

### Zeonica: active-construction passthrough

```
2026-09-25T23:44:46.8587643Z [ClanAI v0.22A-ruler-courtship-native-v1] CIVIC_PROJECT_EVAL campaignHour=649589.59721172217 settlementId=town_EW2 settlement=Zeonica npcTown=True constructionIdle=False queueCount=1 contextValid=True festivalAvailable=True loyalty=91.3676147 nativeThreshold=25 nativeResultId=building_settlement_daily_festival_and_games nativeResult=Festival_and_Games nativeIsFestival=True finalResultId=building_settlement_daily_festival_and_games finalResult=Festival_and_Games finalIsFestival=True finalExistingReference=True preferFestival=False substitution=False reason=ConstructionActive mutationByClanAI=False
```

Observed:

- NPC town: true;
- construction idle: false;
- queue count: 1;
- Festival and Games exists;
- native result: Festival and Games;
- final result: exact same existing native project;
- preference: false;
- substitution: false.

The wrapper correctly did not interfere with active construction.

### Marunath: idle/high-loyalty passthrough

```
2026-09-25T23:45:32.7726865Z [ClanAI v0.22A-ruler-courtship-native-v1] CIVIC_PROJECT_EVAL campaignHour=649611.50918086106 settlementId=town_B1 settlement=Marunath npcTown=True constructionIdle=True queueCount=0 contextValid=True festivalAvailable=True loyalty=94.79668 nativeThreshold=25 nativeResultId=building_settlement_daily_train_militia nativeResult=Train_Militia nativeIsFestival=False finalResultId=building_settlement_daily_train_militia finalResult=Train_Militia finalIsFestival=False finalExistingReference=True preferFestival=False substitution=False reason=LoyaltyNotBelowThreshold mutationByClanAI=False
```

Observed:

- NPC town: true;
- construction idle: true;
- queue count: 0;
- Festival and Games exists;
- loyalty: 94.79668;
- selected native rebellious-state threshold: 25;
- native result: Train Militia;
- final result: exact same existing native Train Militia reference;
- preference: false;
- substitution: false.

This is the strongest natural passthrough control in the run.

## C. Low-loyalty qualifying branch — BOUNDED NULL

Across the bounded run:

- total civic-project evaluations: 12;
- NPC-town evaluations: 2;
- genuine substitutions: 0.

Neither evaluated NPC town had loyalty below the native threshold.

The run therefore does **not** prove the target condition:

`npc town + idle construction + Festival available + loyalty < native rebellious-state threshold`

No threshold was widened. No loyalty was lowered. No construction queue was cleared. No selector was invoked synthetically. No Festival result was forced.

Deterministic tests remain the proof of the unobserved low-loyalty branch's decision rule; they are not a substitute for runtime observation.

## D. Native final commit authority — PASS FOR THE OBSERVED PASSTHROUGH PATH

The observation-only commit postfix records Bannerlord after its native `BuildingHelper.ChangeDefaultBuilding` method has run. It does not call that method or mutate project state.

For Zeonica:

```
2026-09-25T23:44:46.8587643Z [ClanAI v0.22A-ruler-courtship-native-v1] CIVIC_PROJECT_NATIVE_COMMIT campaignHour=649589.59721172217 settlementId=town_EW2 settlement=Zeonica nativeResultId=building_settlement_daily_festival_and_games nativeResult=Festival_and_Games committedResultId=building_settlement_daily_festival_and_games committedResult=Festival_and_Games committedIsFestival=True preferredFestival=False substitution=False reason=ConstructionActive currentDefaultMatches=False ageHours=0 source=native-BuildingHelper.ChangeDefaultBuilding observer=postfix mutationByClanAI=False
```

For Marunath:

```
2026-09-25T23:45:32.7726865Z [ClanAI v0.22A-ruler-courtship-native-v1] CIVIC_PROJECT_NATIVE_COMMIT campaignHour=649611.50918086106 settlementId=town_B1 settlement=Marunath nativeResultId=building_settlement_daily_train_militia nativeResult=Train_Militia committedResultId=building_settlement_daily_train_militia committedResult=Train_Militia committedIsFestival=False preferredFestival=False substitution=False reason=LoyaltyNotBelowThreshold currentDefaultMatches=True ageHours=0 source=native-BuildingHelper.ChangeDefaultBuilding observer=postfix mutationByClanAI=False
```

Therefore the runtime authority chain is directly observed for natural passthrough decisions:

`native selector -> Phase 6 wrapper returns choice -> native BuildingHelper.ChangeDefaultBuilding -> observer reads committed state`

This proves Bannerlord owns the actual mutation path.

It does **not** prove a native final commit after a genuine Phase 6 Festival substitution, because no substitution occurred.

## E. No direct ClanAI mutation

The accepted static invariants and telemetry-validation checkpoint continue to apply:

- no `new Building(...)`;
- no Phase 6 call to `BuildingHelper.ChangeDefaultBuilding`;
- no direct Loyalty write;
- no queue/list mutation;
- no `CurrentDefaultBuilding` assignment;
- no prosperity/security/militia/garrison mutation;
- no relation/governor/ownership mutation;
- no save-schema addition;
- no runtime external IO/network/development-tool dependency.

Runtime evaluation and commit lines state `mutationByClanAI=False`.

## F. Telemetry health

Phase 6 runtime counts:

- `CIVIC_PROJECT_EVAL`: 12;
- NPC-town evaluations: 2;
- genuine substitutions: 0;
- `CIVIC_PROJECT_NATIVE_COMMIT`: 12;
- `CIVIC_PROJECT_TELEMETRY_ERROR`: 0.

Exact evidence is preserved in:

`Reports/SettlementSociety/evidence/phase6_civic_project_v1_runtime_20260925.txt`

## Safety / cleanup

Before deployment:

- Bannerlord closed;
- candidate hash verified;
- previous live DLL copied to a verified rollback;
- Strategic Commitment = `Mode=Observe`;
- Visual War marker absent;
- protected fixture hash/timestamp recorded.

Rollback SHA-256:

`E4DB15DAF5C437F45F559CADDD3F385CE062416D2896EE451826D961D9DDDF7A`

The runtime used only the protected fixture read-only.

TestRunner fresh chronology contains:

- load;
- time-control commands;
- escape-menu clearing;
- pause;
- `EXIT_NOSAVE`.

Save-command count:

**0**

After cleanup:

- Bannerlord closed: yes;
- protected fixture SHA-256 unchanged: `A91F15BF1403F1D29F942C56A1162F431113942CDACCAFE52F4D80303B4CB427`;
- protected fixture timestamp unchanged: `2026-09-24T17:20:12.2384633Z`;
- rollback SHA-256 unchanged;
- live DLL still exact runtime candidate: `2909ADE31B427E122FDDE539229BD0A4A2B4FA91C84CFD3AB63D0C8EB6F729D7`;
- Strategic Commitment still `Mode=Observe`;
- Visual War still OFF;
- Phase 6 telemetry errors: 0.

## Player legibility

No player-facing Phase 6 message or UI observation was part of this bounded run.

Although Bannerlord committed ordinary native civic projects, this run did not verify that the player could inspect or understand the relevant NPC-town project transition through normal UI.

**Player-legible: NOT YET PROVEN.**

## Capability status

Phase 6-v1:

- **Implemented:** YES.
- **Build-proven:** YES.
- **Runtime-observed:** YES — selected wrapper and real NPC-town evaluations executed.
- **Genuine Festival substitution observed:** **NO**.
- **Native final commit observed:** **YES** for naturally evaluated passthrough choices through the native `ChangeDefaultBuilding` path.
- **Native final commit after a genuine Festival substitution:** **NO**.
- **Player-legible:** **NOT YET PROVEN**.
- **Balance-proven:** **NO**.

This result proves runtime integration, conservative passthrough behavior, and Bannerlord's final mutation authority. It does not yet prove the intended low-loyalty boundary crossing.

## Checkpoint

Record this bounded null honestly and stop.

Do not lower the native threshold, alter loyalty, clear queues, increase native consideration frequency, force Festival and Games, broaden Phase 6 inputs, or patch the final mutation to manufacture a positive result.

Do not begin another Phase 6 feature or Phase 7 in this task.
