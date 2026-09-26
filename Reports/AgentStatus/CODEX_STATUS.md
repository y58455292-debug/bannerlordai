# Codex Status

## Current checkpoint

Phase 7A continuity audit is complete, and Phase 7B has completed its **runtime-preflight telemetry checkpoint**.

**Preflight result: READY — the existing candidate can now observe one natural ruler/clan-leader succession plus a guarded save/reload without changing gameplay or persistence semantics. No succession run has been performed.**

The preflight adds only session-only observation of native hero death, clan-leader change, ruling-clan change, pre-save and post-reload snapshots. Each ruler snapshot records stable hero/clan/kingdom/family/settlement identities, kingdom-continuity succession count, clan-bound and hero-bound memory counts, WarState/WarScar structural IDs/counts, and a cross-hero person-memory application guard. Existing person-bound memory reads emit requested-vs-resolved hero identity telemetry without changing keys, values, scores, or factors.

The Phase 7A `DynastyBranchEpisodeMemory` actor-ID retrieval defect remains intentionally unfixed and byte-for-byte unchanged. No age, death chance, succession, leadership, family, settlement ownership, kingdom state, memory inheritance, or save schema was changed. Release build is green with 0 errors and the inherited `System.ValueTuple` warning. Preflight candidate DLL SHA-256: `B15B7B4D3F77EF40D6080EECF07B0077BA4B0E5ABD176C3C4328166BFB4A3ED0`. Natural target ruler/clan: **NOT YET SELECTED**; select from live session-start snapshots in the future bounded run rather than stale historical state.

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

## Next bounded milestone

The Phase 7B runtime preflight checkpoint is complete.

The next separate milestone may run the Phase 7A-selected **bounded natural ruler/clan-leader succession + guarded save/reload** using this exact preflight candidate. Select the target from current live session-start snapshots and preserve the Phase 7A pass/fail rules.

Do not alter age, death chance, native succession, leadership, family, settlements, memory inheritance, or `DynastyBranchEpisodeMemory` to obtain a transition. A run with no natural qualifying succession remains an honest bounded null.

Final product direction remains standalone, installable, and offline with no runtime development-tool dependency.