# Codex Status

## Current checkpoint

Phase 7B's first natural-succession experiment is complete as a **BOUNDED NULL**.

The original Bannerlord process (PID 20740, campaign generation 1) was resumed without restarting, redeploying, resetting telemetry, or selecting a new ruler. Nemos (`lord_1_44`) remained alive and leader of `clan_empire_west_1`, ruling the surviving `calradian_empire` kingdom. No selected-target death, clan-leader change, or ruling-clan succession occurred. No guarded post-succession save/reload was performed.

The original chronology began at campaign hour `649491.27044636116` and paused at `652360.40016455553`: **2869.1297181943664 hours** elapsed. Read-only inspection of the loaded native calendar established **2880 hours for five campaign years**, correcting the earlier 10080-hour estimate without changing the calendar or extending the five-year authorization. The stop guard retained a 10.870281805633567-hour margin below the cap.

The same preflight DLL remained installed throughout: SHA-256 `B15B7B4D3F77EF40D6080EECF07B0077BA4B0E5ABD176C3C4328166BFB4A3ED0`, from `e6293f191c84b8db51a343ec82c3676a962d179b`. Nemos's final paused live age was `86.4927139` and `IsDead=false`. None of 615 emitted hero-memory-resolution lines reported a cross-hero/deceased-key application; this is not a proof of successor isolation because there was no successor. Guarded reload and duplicate-increment checks remain unexercised. Preflight WarState snapshot fields remained unavailable; the previously recovered selection record retains the 18 initial WarScars and two objectives without claiming a persistence round trip.

`EXIT_NOSAVE` completed. Bannerlord is closed, zero save commands were issued, the protected fixture hash/timestamp are unchanged, rollback is intact, Strategic Commitment remains Observe, and Visual War remains OFF. No gameplay or save-schema changes were made; `DynastyBranchEpisodeMemory` remains unfixed. Phase 4/5/6 were not rerun or retuned. No additional Phase 7 feature was started.

## Native ecology finding

Bannerlord does not use one universal bandit spawn path.

Ambient native systems include separate:

- global looter population refill;
- culture-bandit populations tied to infested hideouts;
- hideout infestation/replacement;
- quest/incident/deserter bandits;
- native Guard House patrol parties;
- generic lord/patrol combat suppression.

Native bandit counts are already bounded by `DefaultBanditDensityModel` and `BanditSpawnCampaignBehavior`.

## Selected Phase 5-v1 seam

The implemented v1 seam is native:

`BanditSpawnCampaignBehavior.GetSpawnChanceInSettlement(Settlement)`

A narrow postfix/result modifier applies **only for town/village candidates** with usable Security, which are the ambient looter spawn anchors.

Hideout candidates pass through native exactly.

This changes only **where** native looter pressure is relatively likely to appear. It does not change global looter quantity, hideout-bandit production, culture/templates, party creation, movement, or removal.

## Local-control input

Use **Security only**.

Town:

`Town.Security`

Village:

bound fortification `Town.Security` when safely available.

Missing Security -> native passthrough.

Security is deliberately the only v1 input because native Security already aggregates garrison, looted villages, siege, nearby infested hideouts, patrol-party bonuses, policies/projects/issues/perks and native drift.

Native bandit victories/defeats and hideout clearing also change nearby Security, creating an existing control feedback loop.

## Candidate first safety bounds

`controlMultiplier = clamp(1.25 - 0.005 * clamp(Security,0,100), 0.75, 1.25)`

Examples:

- Security 0 -> 1.25;
- 25 -> 1.125;
- 50 -> 1.00;
- 75 -> 0.875;
- 100 -> 0.75.

`finalSpawnWeight = nativeSpawnWeight * controlMultiplier`

This is a relative **selection weight**, not a spawn-rate multiplier.

The bounds are first-candidate safety limits, not balance claims.

Native global looter caps/refill remain untouched and secure territory never receives a zero weight.

## Native patrol/lord suppression

Native towns with Guard Houses already generate culture-native patrol parties.

Native AI already gives patrols stronger willingness/range against bandits, while independent lords can engage bandits through the generic initiative system when strength/distance/readiness permit.

No custom patrol party type is needed.

## Existing BannerlordAI interactions

Home Responsibility already biases native home patrol/defend candidates during war but does not spawn or target bandits directly.

Visual War already contains an optional rear-security bandit `EngageParty` modifier (weak recovery skip; approximately 1.25 for <=90 men and 1.15 for <=160). Phase 5-v1 should not retune or require that system.

WarState raid/scar memory is deferred to avoid double counting damage already represented by native Security.

## Native binaries

`TaleWorlds.CampaignSystem.dll`  
SHA-256 `5B23C3E36D7A5D6D47C47EAB075E9E2B1CC8D1AA53C79E135FA2B5EF434EBC5F`

`SandBox.dll`  
SHA-256 `16AF436C569675EB30E22514BB755E6FB3612AFCE38F6CC55D1748D079C19C1A`

## Evidence

- `Reports/Security/PHASE5_SECURITY_BANDITRY_NATIVE_CAPABILITY_AUDIT.md`
- `Reports/Security/evidence/phase5_security_banditry_native_capability_audit_20260925.txt`
- `Reports/Security/PHASE5_LOCAL_BANDIT_CONTROL_V1_OFFLINE_IMPLEMENTATION_RESULT.md`
- `Reports/Security/evidence/phase5_local_bandit_control_v1_validation_20260925.txt`
- `Reports/Security/PHASE5_LOCAL_BANDIT_CONTROL_V1_RUNTIME_TELEMETRY_VALIDATION.md`
- `Reports/Security/evidence/phase5_local_bandit_control_v1_runtime_telemetry_validation_20260925.txt`
- `Reports/Security/PHASE5_LOCAL_BANDIT_CONTROL_V1_RUNTIME_RESULT.md`
- `Reports/Security/evidence/phase5_local_bandit_control_v1_runtime_20260925.txt`
- `Reports/SettlementSociety/PHASE6_SETTLEMENT_SOCIETY_CAPABILITY_AUDIT.md`
- `Reports/SettlementSociety/evidence/phase6_native_audit_evidence_20260925.md`
- `Reports/SettlementSociety/evidence/phase6_native_audit_manifest_20260925.json`
- `Reports/SettlementSociety/PHASE6_CIVIC_PROJECT_V1_OFFLINE_IMPLEMENTATION_RESULT.md`
- `Reports/SettlementSociety/evidence/phase6_civic_project_v1_offline_validation_20260925.txt`
- `Reports/SettlementSociety/evidence/phase6_civic_project_v1_runtime_telemetry_validation_20260925.txt`
- `Reports/SettlementSociety/PHASE6_CIVIC_PROJECT_V1_RUNTIME_RESULT.md`
- `Reports/SettlementSociety/evidence/phase6_civic_project_v1_runtime_20260925.txt`
- `Reports/GenerationalContinuity/PHASE7A_CONTINUITY_NATIVE_AUDIT.md`
- `Reports/GenerationalContinuity/PHASE7B_SUCCESSION_RUNTIME_PREFLIGHT.md`
- `Reports/GenerationalContinuity/evidence/phase7b_succession_runtime_preflight_validation_20260925.txt`
- `Reports/GenerationalContinuity/PHASE7B_SUCCESSION_RUNTIME_RESULT.md`
- `Reports/GenerationalContinuity/evidence/phase7b_succession_runtime_20260926.txt`

## Next bounded milestone

This Phase 7B attempt is closed as a bounded null. The selected natural-succession and guarded save/reload boundary remains unproven.

Stop here. Do not extend or restart this attempt, change the target, force conditions, fix `DynastyBranchEpisodeMemory`, or start another feature merely to obtain a positive result. Further experiments require a new bounded task.

Final product direction remains standalone, installable, and offline with no runtime development-tool dependency.
