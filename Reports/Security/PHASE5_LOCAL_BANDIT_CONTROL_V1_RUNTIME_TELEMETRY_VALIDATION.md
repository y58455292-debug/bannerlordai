# Phase 5-v1 Local Bandit Control — runtime telemetry validation

Date: 2026-09-25 UTC  
Gameplay-policy base: `0093c8d3d27d2252b9e13572441ab73bbe121dea`

## Result

**PASS — minimum observation-only telemetry is build-validated for the bounded Phase 5-v1 runtime characterization. No gameplay policy, multiplier, target method, or postfix authority changed.**

The validated gameplay policy remains byte-for-byte identical to the offline Phase 5-v1 checkpoint.

- `LocalBanditControlPolicy.cs` Git blob: `7b3d119f274ddf884167f8fd337815d8ba6e6c47`
- policy source SHA-256: `890DED9CAA10B1A4E84ACE80FBE5788E96E00FCE17654325E5EB94B5494A3FC2`
- multiplier remains `clamp(1.25 - 0.005 * clamp(Security,0,100), 0.75, 1.25)`.

## Telemetry added

`LocalBanditControlRuntimeTelemetry` is observation-only.

The existing Phase 5 postfix now records the original native `__result` before any adjustment and passes read-only context to telemetry. It still modifies `__result` only for supported town/village candidates with usable Security.

Telemetry can record first useful samples for:

- town;
- village plus bound fortification;
- Security above 50;
- Security below 50;
- naturally near-neutral Security;
- unsupported hideout/other candidate;
- missing Security.

Each evaluation record includes campaign hour, settlement identity, candidate kind, bound settlement where applicable, Security availability/value, native weight, control multiplier, final weight, applied state, reason, `hook=postfix`, `originalExecuted=True`, and `mutationByClanAI=False`.

A separate `MobilePartyCreated` listener can record the first naturally created looter party after Bannerlord has already created it. It reads party/clan/culture/template/home context only. It does not create, destroy, move, target, or mutate any party.

## Invariants

Phase 5 runtime source remains guarded against direct creation/destruction, movement/order changes, settlement/world mutation, broader bandit-spawn seams, extra v1 inputs, external IO/network access, development-machine paths, and development-tool runtime dependencies.

Validation includes the new telemetry source itself.

Passed:

```
PASS Phase 5 local bandit control policy checks=24
PASS Phase 5 exact postfix/result-only wiring invariant
PASS Phase 5 town/village-only Security input invariant
PASS Phase 5 no-mutation invariant
PASS Phase 5 standalone-path invariant
PASS Phase 5 pure policy byte-for-byte preservation invariant
PASS Phase 4B/4C pure policy preservation invariant
PASS Phase 5 private patch resolution checks=14
```

All retained Phase 4 tests/invariants and relevant Home Responsibility, Kingdom Objective, Visual War, and Strategic Commitment tests/invariants also passed.

## Supported native binary

`TaleWorlds.CampaignSystem.dll`  
SHA-256 `5B23C3E36D7A5D6D47C47EAB075E9E2B1CC8D1AA53C79E135FA2B5EF434EBC5F`

The exact private target still resolves as:

`BanditSpawnCampaignBehavior.GetSpawnChanceInSettlement(Settlement)`

with a postfix-only result modifier.

## Release build

```
Build succeeded.
1 Warning(s)
0 Error(s)
```

The warning is the inherited `System.ValueTuple` MSB3277 conflict.

Runtime-characterization candidate DLL SHA-256:

`E4DB15DAF5C437F45F559CADDD3F385CE062416D2896EE451826D961D9DDDF7A`

Telemetry source SHA-256:

`FAF6C566EA0BF2907D7B91F4E67CA43BC3DF1231247DAFC259432AEA9006FC81`

Patch source SHA-256:

`438E908477CD8C864510BBF0D364C1D65E0F2E76A7C099F53D05A2D333E63E85`

Exact validation output is preserved in:

`Reports/Security/evidence/phase5_local_bandit_control_v1_runtime_telemetry_validation_20260925.txt`

## Runtime status

No runtime claim is made by this telemetry-validation checkpoint.

The candidate has not yet been deployed. Bannerlord has not been launched for Phase 5 runtime characterization from this checkpoint.

The next action is the separately bounded runtime characterization using this exact telemetry candidate, after its source/evidence commit is checkpointed and deployment safety checks pass.
