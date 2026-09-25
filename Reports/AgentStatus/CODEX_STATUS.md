# Codex Status

## Current checkpoint

Phase 6 has completed its first **offline-only Settlement Society / Vanilla Capability Audit**.

**Audit result: COMPLETE — the twelve requested settlement/society domains were mapped from both player and NPC perspectives, current ClanAI overlap was checked, three bounded candidate seams were ranked, and one first Phase 6-v1 target was identified. No Phase 6 gameplay source has been implemented.**

Selected first target: **needs-aware native daily civic-project selection for low-loyalty, idle NPC towns**, using the existing `BuildingScoreCalculationModel.GetNextDailyBuilding(Town)` decision seam and the existing native Festival and Games project. The audited default selector chooses daily projects randomly; the native caller already owns cadence and commits the selected existing project through `BuildingHelper.ChangeDefaultBuilding`. The proposed future wrapper would preserve native project objects/effects, the construction queue, player choices, save state and final mutation authority.

The deeper issues/notables audit found that NPC settlement visitors already have a native opportunistic issue-resolution path, NPC clans already assign governors, workshops/caravans/gangs already have meaningful native lifecycle behavior, and several tempting Phase 6 features would duplicate native or existing ClanAI systems. Phase 4A remains closed. Phase 4B/4C and Phase 5-v1 remain unchanged and runtime-observed; their balance remains unproven. Phase 7 has not started.

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

## Next bounded milestone

The Phase 6 capability-audit checkpoint is complete. Phase 6 gameplay implementation is **NOT STARTED**.

The next bounded milestone is a separate **offline-only implementation and validation** of the selected low-loyalty idle-town civic-project choice seam. It must preserve the audited default-model compatibility boundary, call the selected native daily selector exactly once, return only an existing native daily-project reference, and introduce no direct settlement mutation or new save state.

Do not launch Bannerlord or deploy from this audit checkpoint. Do not retune Phase 4/5. Do not begin Phase 7.

Final product direction remains standalone, installable, and offline with no runtime development-tool dependency.
