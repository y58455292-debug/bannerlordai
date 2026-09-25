# Codex Status

## Current checkpoint

Phase 4B Local Manpower v1 is now **implemented, build-proven, and runtime-observed for the minimum required probability contract**. A native volunteer-slot mutation after a Local Manpower evaluation was also observed.

**Balance is not proven. Phase 4C has not started.**

Validated policy checkpoint: `88aff823d055b0b951d263f8259f5c3537ed6bfd`  
Observation-only runtime checkpoint: `5a6b9757b33a33f356f537cced6a74051ca04e30`  
Runtime DLL SHA-256: `9CB64EA90774A391D442FC32F460EF09E1D2901AEE3EBB194386E37A96F1C896`

The pure policy blob remained unchanged from the offline implementation checkpoint. The runtime hash differs from the previous offline DLL only because minimum observation-only telemetry was added and revalidated.

## Runtime proof

The protected fixture started at `649491.27044636116`. Required proof was complete by `649491.450873`, only **0.18042663884 campaign hours** later. Pause acknowledgement occurred at `649491.6858864167`, **0.41544005554 hours** after start, far inside the 168-hour maximum.

### Selected model

Runtime logged:

- inner: `DefaultVolunteerModel`;
- wrapper: `LocalManpowerVolunteerModel`;
- selected wrapper: true;
- policy: empty slots only.

### Healthy empty passthrough

Vinela, High hearth `624.9296`, security `99.15879`, no raid/siege:

- native `0.08823674`;
- multiplier `1`;
- final `0.08823674`.

### Degraded empty slowdown

Lysia, Mid hearth `412.8728`, security `91.290535`, no raid/siege:

- native `0.525`;
- population factor `0.95`;
- security factor `1`;
- acute factor `1`;
- multiplier `0.95`;
- final `0.498749971`.

This was a natural degraded locality; no state was manufactured.

### Occupied passthrough

Vinela occupied slot:

- native `0.525`;
- final `0.525`;
- reason `occupied-slot-native-passthrough`.

### Native mutation authority

Immediately after the Lysia degraded evaluation, the native daily volunteer update changed Sanion's slot from empty to:

`imperial_vigla_recruit[tier=2,culture=empire]`

The telemetry records `source=native-daily-volunteer-update` and `mutationByClanAI=False`.

Additional native fills/transitions occurred naturally in Lysia, Arpotis, Marunath, and Aegosca.

No telemetry error occurred.

## Runtime scope not exercised

This run did **not** runtime-exercise:

- Low population;
- security below 50;
- missing-security passthrough;
- active raid/siege factor 0.50;
- minimum multiplier 0.35;
- natural AI/garrison consumption of a visible notable slot under the new wrapper.

Shared-pool consumption therefore remains supported by the native-capability audit but was not newly runtime-observed in this brief run.

No extension was performed after the required proof plus native fill completed.

## Safety

Bannerlord was closed before deployment. Rollback hash:

`0D23F4A66E0A1E4413E879C97E963C7DB923D8D6B4421291105D8565120CD5A8`

Strategic Commitment remained `Mode=Observe`; Visual War remained OFF.

The run used `EXIT_NOSAVE`; fresh command-history scan found zero save commands.

Protected fixture remained unchanged:

`A91F15BF1403F1D29F942C56A1162F431113942CDACCAFE52F4D80303B4CB427`  
`2026-09-24T17:20:12.2384633Z`

Bannerlord is closed and rollback remains intact.

## Evidence

- `Reports/Manpower/PHASE4B_LOCAL_MANPOWER_V1_RUNTIME_RESULT.md`
- `Reports/Manpower/evidence/phase4b_local_manpower_v1_runtime_20260925.txt`
- offline implementation: `Reports/Manpower/PHASE4B_LOCAL_MANPOWER_V1_OFFLINE_IMPLEMENTATION_RESULT.md`
- native audit: `Reports/Manpower/PHASE4B_LOCAL_MANPOWER_NATIVE_CAPABILITY_AUDIT.md`

## Capability status

Implemented: **yes**.  
Build-proven: **yes**.  
Runtime-observed: **yes**.  
Native mutation observed: **yes**.  
Shared AI/garrison consumption observed: **no**.  
Balance proven: **no**.

Stop at this Phase 4B checkpoint. Do not begin Phase 4C in the same task.

Final product direction remains a standalone, installable, offline Bannerlord mod with no runtime dependency on ChatGPT, Codex, Desktop Commander, TestRunner, watchdogs, external IO, or development-machine absolute paths.
