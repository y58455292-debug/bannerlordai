# Phase 5 — Local Bandit Control v1 offline implementation result

Date: 2026-09-25 UTC  
Base: `9ee0d3ce5b1b80a795a82fc2e2123d44bf73794f`

## Result

**PASSED — Phase 5-v1 Local Bandit Control is implemented and build-proven offline. It is not runtime-observed and balance is not proven.**

This checkpoint implements only the selected Phase 5-v1 local ambient-looter spawn-site weighting seam from the accepted native-capability audit.

Bannerlord was not launched. No campaign was run. The candidate DLL was not deployed. Phase 6 was not started.

## Implemented policy

New pure source:

`src/ClanAI/src/ClanAI/LocalBanditControlPolicy.cs`

The policy is game-assembly-free and accepts only:

- native spawn-site weight;
- whether Security is available;
- Security value;
- candidate kind: unsupported, town, or village.

For a supported town/village with usable Security:

`boundedSecurity = clamp(Security, 0, 100)`

`controlMultiplier = clamp(1.25 - 0.005 * boundedSecurity, 0.75, 1.25)`

`finalWeight = sanitize(nativeWeight * controlMultiplier)`

The implemented examples are therefore unchanged from the approved design:

- Security 0 -> 1.25;
- Security 25 -> 1.125;
- Security 50 -> 1.00;
- Security 75 -> 0.875;
- Security 100 -> 0.75.

Security 50 preserves a normal finite native weight exactly. Native zero remains zero. A positive finite native weight remains positive under the normal 0.75..1.25 multiplier range.

For supported context, non-finite or negative native weights are sanitized to zero so unusable native state cannot become positive spawn pressure. If finite multiplication would overflow, the policy conservatively retains the finite native weight rather than emitting NaN/Infinity.

The policy does not clamp ordinary positive weights to probability range; this seam is a relative native selection weight.

## Exact native seam

New patch source:

`src/ClanAI/src/ClanAI/LocalBanditControlPatch.cs`

The patch resolves exactly:

`TaleWorlds.CampaignSystem.CampaignBehaviors.BanditSpawnCampaignBehavior.GetSpawnChanceInSettlement(Settlement)`

It uses the repository's established Harmony/reflection pattern:

- `AccessTools.TypeByName`;
- `AccessTools.Method` with exactly one `Settlement` parameter;
- one Harmony postfix;
- no prefix;
- no transpiler;
- no finalizer;
- no original skip.

The resolver also verifies offline that the supported target is still:

- private;
- instance;
- returns `float`;
- takes exactly one `Settlement`.

The postfix signature is result-only:

`SpawnWeightPostfix(Settlement __0, ref float __result)`

Bannerlord's original method therefore executes first and Phase 5 can only inspect the candidate and replace its returned weight.

## Candidate filtering

The postfix applies only when the candidate is a normal town or village and usable Security resolves.

Town:

`settlement.Town.Security`

Village:

`settlement.Village.Bound?.Town?.Security` using explicit null-safe reads.

Unsupported settlement kinds, including hideouts and castles, return from the postfix before the policy is invoked. Their native result is therefore bit-for-bit untouched.

Town/village candidates with missing Security or non-finite Security also return before modification and preserve the native result exactly.

No hideout-specific mapping or nearest-controller heuristic was added.

## Native authority preserved

Phase 5-v1 changes only the relative returned spawn-site weight.

It does not alter:

- global looter population limits;
- looter refill ratio;
- culture-bandit population;
- hideout-linked spawning;
- hideout infestation/replacement;
- bandit culture or templates;
- party creation;
- party destruction;
- party movement;
- combat/removal;
- native patrol generation;
- lord target scoring;
- settlement Security;
- prosperity/hearth;
- garrison/militia;
- war/faction/settlement state;
- Home Responsibility;
- Visual War;
- War State / War Scar;
- War Strain;
- Strategic Commitment;
- Phase 4 policies.

No Phase 5 runtime source contains external IO, network access, development-machine absolute paths, or dependencies on ChatGPT, Codex, Desktop Commander, TestRunner, watchdogs, or other development harnesses.

## Deterministic policy validation

`Tests/Security/Phase5LocalBanditControlPolicy`

Result:

`PASS Phase 5 local bandit control policy checks=24`

Coverage includes:

- unsupported candidate finite native passthrough;
- missing Security finite native passthrough;
- Security 0/25/50/75/100;
- Security below 0 and above 100 clamps;
- zero native weight;
- positive scaling;
- non-negative output;
- NaN/Infinity safety;
- secure positive result remains positive;
- low/high multiplier bounds;
- exact neutral passthrough;
- village/town mathematical parity;
- no-Security no-effect;
- non-finite Security passthrough;
- finite overflow safety.

## Patch and invariant validation

Compiled supported-binary resolution:

`PASS Phase 5 private patch resolution checks=14`

Static Phase 5 invariants:

```
PASS Phase 5 exact postfix/result-only wiring invariant
PASS Phase 5 town/village-only Security input invariant
PASS Phase 5 no-mutation invariant
PASS Phase 5 standalone-path invariant
PASS Phase 4B/4C pure policy preservation invariant
```

The supported campaign binary remained the audited binary:

`TaleWorlds.CampaignSystem.dll`  
SHA-256 `5B23C3E36D7A5D6D47C47EAB075E9E2B1CC8D1AA53C79E135FA2B5EF434EBC5F`

Phase 5 source hashes at validation:

- policy SHA-256: `890DED9CAA10B1A4E84ACE80FBE5788E96E00FCE17654325E5EB94B5494A3FC2`;
- patch SHA-256: `DBBA71F9AF3B17957EAF7AB00CD192A6EFABEAD0A00E1F59880D92A619F82B15`.

## Existing behavior preservation

All retained Phase 4 tests/invariants pass, including:

- Phase 4A recreation policy checks=43;
- Phase 4A recovery policy checks=20;
- Phase 4B policy checks=50;
- Phase 4B compiled delegation checks=13;
- Phase 4C policy checks=69;
- Phase 4C compiled branch/delegation checks=23;
- Phase 4A/4B/4C wiring, no-mutation, standalone, and policy-preservation guards.

Relevant territorial validations also remain green:

- Home Responsibility runtime wiring and policy cases=10;
- Kingdom Objective runtime wiring and policy cases=34;
- Visual War runtime/wiring/no-mutation/standalone guards plus policy checks=32 and commit-policy checks=22;
- Strategic Commitment runtime/wiring/no-mutation/standalone/config guards plus policy checks=34 and commit-policy checks=20.

No Phase 4 source or safety bounds were retuned.

## Release build

```
Build succeeded.
1 Warning(s)
0 Error(s)
```

The only warning remains the inherited `System.ValueTuple` MSB3277 conflict.

Offline Phase 5 candidate DLL SHA-256:

`D104BA8F0246C25E23FC9BF78FB73B57CBEEC22568F3797CE77365C8094C8AB2`

Read-only live installed DLL SHA-256 remained:

`F8EC94C8973C180F7BEA688016B272C690728A188D61EF05166E3ED51AE445DC`

The differing hashes and no-copy workflow confirm this Phase 5 candidate was not deployed. Bannerlord process guards were clear before and after validation.

## Evidence

Exact validation evidence:

`Reports/Security/evidence/phase5_local_bandit_control_v1_validation_20260925.txt`

The evidence records the authoritative base SHA, audited native DLL hash, Phase 5 source hashes, Phase 5 test commands/results, preserved Phase 4/territorial results, Release build summary, candidate/live DLL hashes, process guards, and explicit no-deployment/no-runtime status.

## Capability status

Phase 5-v1:

- **Implemented:** yes.
- **Build-proven:** yes.
- **Runtime-observed:** NO.
- **Balance-proven:** NO.

This checkpoint makes no runtime gameplay or balance claim.

## Next milestone

The next milestone is a **separately authorized bounded Phase 5 runtime characterization of this exact validated candidate**.

That later proof should characterize native town/village candidate evaluations at neutral, secure, and naturally weak Security where available, preserve hideout passthrough, and verify native looter creation authority without direct Phase 5 party mutation.

Do not launch that runtime proof from this checkpoint. Do not retune the 0.75..1.25 bounds. Do not add inputs or broaden the seam. Do not begin Phase 6.
