# Codex Status

## Current checkpoint

Phase 4C Troop Quality v1 is now **implemented, build-proven, and runtime-observed**.

A native direct-target volunteer quality upgrade after a Phase 4C evaluation was also observed.

**Balance remains unproven. Phase 5 has not started.**

Offline policy/source checkpoint:

`51084dd61928308373b72f0fd252720601fd2756`

Observation-only telemetry checkpoint:

`7fb5355b1db215a7bd0164baf082ebae13c76704`

Runtime DLL SHA-256:

`F8EC94C8973C180F7BEA688016B272C690728A188D61EF05166E3ED51AE445DC`

The Phase 4B and Phase 4C policy blobs remained unchanged from the offline implementation checkpoint.

## Runtime proof

Protected-fixture start:

`649491.27044636116`

Minimum A-E proof completed at:

`649492.04582108336`

Elapsed:

**0.77537472220 campaign hours**

Pause acknowledgement:

`649492.34227611113`

Elapsed:

**1.07182974997 campaign hours**

Stop reason:

`minimum-phase4c-proof`

### Wrapper

Runtime selected:

- inner `DefaultVolunteerModel`;
- wrapper `LocalManpowerVolunteerModel`;
- selected wrapper = true.

### Phase 4B preservation

Vinela empty slot:

- High population;
- security 99.15879;
- native `0.08823674`;
- final `0.08823674`.

### Occupied non-upgradeable

Tarcutis:

- `imperial_heavy_horseman`, tier 4;
- selected max tier 4;
- direct target exists but native eligibility is false at max tier;
- native `0.08823674`;
- final `0.08823674`;
- branch = native passthrough.

### Healthy quality

Vinela:

- `imperial_recruit`, tier 1;
- two direct native targets;
- native-upgrade-eligible;
- High population, security 99.15879;
- quality multiplier 1;
- native/final `0.525`.

### Naturally degraded quality

Lysia:

- `imperial_vigla_recruit`, tier 2;
- direct target `imperial_equite`;
- Mid hearth `412.8728`;
- security 91.290535;
- no raid/siege;
- native `0.367499977`;
- quality multiplier `0.95`;
- final `0.349124968`.

No degradation was manufactured.

### Native quality mutation

Dradios:

- before `imperial_archer` tier 2;
- direct native target `imperial_trained_archer` tier 3;
- source count 1 -> 0;
- target count 0 -> 1;
- `directUpgradeTarget=True`;
- source = native daily volunteer update;
- `mutationByClanAI=False`.

This is a strong runtime proof that Bannerlord retains second-gate RNG, target selection and actual volunteer mutation.

## Runtime scope not exercised

This run did not independently runtime-exercise:

- Low-population quality factor;
- security below 50;
- missing-security quality passthrough;
- active raid/siege quality factor;
- Phase 4C 0.50 floor.

Those remain deterministic/build-proven, not newly runtime-proven.

No tuning is justified from this short characterization.

## Supporting shared-pool evidence

The run naturally logged native AI consumption of visible notable volunteer slots. The full relevant trace contains 31 `LOCAL_MANPOWER_SHARED_POOL_CONSUMPTION` lines.

This is supporting parity evidence only and was not a Phase 4C pass requirement. No separate garrison-consumption claim is made.

## Safety

Bannerlord was closed before deployment.

Rollback SHA-256:

`9CB64EA90774A391D442FC32F460EF09E1D2901AEE3EBB194386E37A96F1C896`

The run used `EXIT_NOSAVE`; fresh command history contains zero save commands.

Protected fixture remained unchanged:

`A91F15BF1403F1D29F942C56A1162F431113942CDACCAFE52F4D80303B4CB427`  
`2026-09-24T17:20:12.2384633Z`

Strategic Commitment remained `Mode=Observe`; Visual War remained OFF; Bannerlord is closed; rollback remains intact.

## Evidence

- `Reports/Manpower/PHASE4C_TROOP_QUALITY_V1_RUNTIME_RESULT.md`
- `Reports/Manpower/evidence/phase4c_troop_quality_v1_runtime_20260925.txt`
- offline implementation: `Reports/Manpower/PHASE4C_TROOP_QUALITY_V1_OFFLINE_IMPLEMENTATION_RESULT.md`
- audit: `Reports/Manpower/PHASE4C_TROOP_QUALITY_NATIVE_CAPABILITY_AUDIT.md`

## Capability status

Implemented: **yes**.  
Build-proven: **yes**.  
Runtime-observed: **yes**.  
Native quality mutation observed: **yes**.  
Balance-proven: **no**.

Stop at this Phase 4C checkpoint.

Do not begin Phase 5 in the same task.

Final product direction remains a standalone, installable, offline Bannerlord mod with no runtime dependency on ChatGPT, Codex, Desktop Commander, TestRunner, watchdogs, external IO or development-machine absolute paths.
