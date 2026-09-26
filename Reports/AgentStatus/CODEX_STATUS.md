# Codex Status

## Current checkpoint

Phase 8B-I4R2P's offline release-smoke control protocol is **COMPLETE**. The prior I4 and I4R1 runs remain closed test-control failures, not ClanAI defects. I4 first measured 52.320 campaign hours only after its guarded save; I4R1 treated sent pause inputs and a filename-labelled screenshot as acknowledgement, allowing its third interval to grow from 7.368 to 45.624 hours before the next probe.

The next live retry must use the native Escape/menu overlay as a positive hard-pause acknowledgement with the strict state machine `PAUSED_CONFIRMED -> ADVANCE_AUTHORIZED -> ADVANCING -> PAUSE_REQUESTED -> PAUSED_CONFIRMED`. No other transition from ADVANCING is allowed, and no window/tool/file switch is permitted until the live overlay is visibly confirmed. One repository-only read-only DayLong helper now calculates target/cutoff margins and measured burst guidance; it issues no game/save command and has no network or shipped-runtime role.

The exact packaged DLL remains SHA-256 `A18341FB2CD6B8623B45B52155C1A7D987DBEE7AAE14AE7DBF704514EC7F37B5`; package manifest, gameplay source, save schema, I4 evidence, and I4R1 evidence are unchanged. Bannerlord was not launched, ClanAI was not built/deployed, and no live attempt started. See `Reports/Release/PHASE8B_I4R2P_SMOKE_CONTROL_PROTOCOL.md`.

Next milestone: one separately authorized fresh **Phase 8B-I4R2 live release-profile smoke** using this hard-pause/measured-burst protocol.
## Preserved Phase 8B-I4 failed attempt

Phase 8B-I4's release-profile runtime smoke attempt is closed as **FAIL — invalid/incomplete test-control attempt**, not a demonstrated ClanAI package defect.

The exact packaged v0.22.0 DLL from `fa91c05f9523dd2000883bd84e573d0d7dddf467` was deployed without rebuilding: SHA-256 `A18341FB2CD6B8623B45B52155C1A7D987DBEE7AAE14AE7DBF704514EC7F37B5`. Harmony satisfied the declared version, the normal main menu was reached, and the protected campaign loaded with no Evidence profile, Visual War marker, Strategic Commitment override, Inspector, or TestRunner in the selected runtime module set. NavalDLC was retained only for the protected fixture's compatibility.

The operator failed to enforce the 48-campaign-hour maximum. The one successful guarded save, `BI4 20260926 0407.sav`, contains native `DayLong=1041.941` versus the protected fixture's `1039.761`, giving approximately **52.320 pre-save hours**. Further unsaved progression occurred before final pause; exact final elapsed time was not captured. Guarded reload, six-hour post-reload progression, and duplicate-initialization checks were **NOT PERFORMED**. No replacement run or product fix was attempted.

No emitted exception trace attributable to ClanAI/Harmony was observed in the available current-run logs. Native GPU-cache and shutdown device-reference errors are retained without attributing them to ClanAI. The module remained exactly the three package files with no Data/Logs directory; all 84 previously tracked research telemetry files retained their sizes and last-write times. This is not an exhaustive file-access/network trace or proof that every gameplay initializer ran.

The campaign was exited through the normal no-save confirmation and Bannerlord closed gracefully. The protected fixture SHA-256/timestamp and full rollback manifest are unchanged. Exactly one new native save succeeded; it was not reloaded. Gameplay/package source, policies, and save schema were not changed. **Ready for next Phase 8 release-hardening checkpoint: NO.**

See `Reports/Release/PHASE8B_I4_RELEASE_PROFILE_RUNTIME_SMOKE_RESULT.md` and `Reports/Release/evidence/phase8b_i4_release_profile_smoke_20260926.txt`. Stop at this failed smoke checkpoint; do not infer a gameplay fix, claim a release PASS, or rerun it as part of this task.

## Preserved Phase 8B-I3 offline checkpoint

Phase 8B-I3's offline installable-package checkpoint is **COMPLETE**. The committed `src/ClanAI/package/ClanAI` module contains exactly `SubModule.xml`, install notes, and `bin/Win64_Shipping_Client/ClanAI.dll`. The DLL SHA-256 is `A18341FB2CD6B8623B45B52155C1A7D987DBEE7AAE14AE7DBF704514EC7F37B5`; deterministic size/hash manifest and exact validation evidence are under `Reports/Release/evidence/`.

The package declares external `Bannerlord.Harmony`, excludes NavalDLC and every bundled Harmony/runtime research artifact, ships no PDB or configuration override, and defaults Evidence telemetry OFF, Strategic Commitment to Observe, and Visual War OFF. Release debug-symbol emission was disabled after the offline scan detected an embedded local PDB path; the rebuilt DLL is clean. Phase 8B-I1/I2, save-key, gameplay-policy, and Phase 3–7 preservation gates pass. Release build has 0 errors plus the inherited `System.ValueTuple` warning.

No Bannerlord launch, package deployment, runtime test, gameplay tuning, or save-schema change occurred in that I3 offline checkpoint. See `Reports/Release/PHASE8B_I3_PACKAGE_ASSEMBLY_RESULT.md`.

## Phase 8A finding (historical audit)

Phase 7 is sufficiently characterized for roadmap purposes. Phase 7D synthesized the accepted evidence, preserved the Phase 6 Festival, Phase 7B succession, and Phase 7C-I3 ruling-clan natural-event nulls, and recommended proceeding to release hardening rather than repeating rare-event hunts.

Phase 8 has started. Phase 8A's offline release-readiness audit is **COMPLETE**. It found no production network/API, ChatGPT, Codex, Desktop Commander, watchdog, command-bus, or TestRunner runtime integration, but its then-current main was not yet a standalone release candidate: active runtime paths still targeted `D:\BannerlordAIResearch`; experiment-only observers and proof telemetry were installed by default; Harmony use was not declared or packaged; module and assembly versions disagreed; and the committed package contained only `SubModule.xml`, not an installable DLL/module.

No gameplay source, balance, save schema, or accepted evidence changed in that audit. Bannerlord was not launched, no DLL was deployed, and no runtime work was performed. See `Reports/Release/PHASE8A_RELEASE_READINESS_AUDIT.md`.

That audit selected **Phase 8B-I1 — standalone path and release-profile seam** as its next step: a deterministic module-local path resolver and default release diagnostics gate, removal of development-machine path dependence, default exclusion of experiment-only observers/proof telemetry, and offline preservation of save keys/gameplay wiring. I3's later package checkpoint is preserved above; I4 and I4R1's failed runtime attempts are preserved above.

## Preserved Phase 7C-I3 bounded null

Phase 7C-I3's already-completed runtime characterization remains closed as a **BOUNDED NULL** using retained local evidence. No campaign was launched, rerun, extended, or redeployed for its closure.

The experiment watched for the next natural `CampaignEvents.RulingClanChanged` in **any surviving kingdom**, not Nemos or a particular ruler. Structural-writer checkpoint: `12f8f430a8f192e100133a70787e7596e412099b`. Observation-only runtime checkpoint: `3056a69f114b0acc85dfb4ed12a89fc6347b70d9`. Runtime/final DLL SHA-256: `6B3DC310C7491C2D976F34C9DBF64A3CF9A756B1077BCAD29C4E6577767C4364`.

The retained chronology started at campaign hour `649491.27044636116` and paused at `650632.31218391669`, with exact recorded elapsed time `1141.0417375555262` hours against the `1152`-hour maximum. Stop reason: `two-native-year-bound-conservative-margin`. No qualifying ruling-clan event was observed, and no new `KingdomRulingClanChanged` row was recorded. Final episode count was 2: the existing personal `IncidentOpened` and `IncidentChoice` rows. Final structural count, `_structuralRecorded`, `_duplicates`, and `_rejected` were all 0; the kingdom-continuity ledger contained 7 records.

The initial branch-history receipt was null. Personal/choice receipts only read existing personal history. Therefore structural-event branch-history retrieval and personal-retrieval exclusion remain runtime-unproven. No guarded save/reload occurred; structural-row persistence, post-reload duplicate detection, exactly-once succession increment, and reload-notice suppression were **NOT EXERCISED**. The initial fixture's `post-load` receipts are not post-event reload proof. Absence of a natural event is not a failure.

The retained complete session scan found zero emitted telemetry-error records; the exact run command interval contained zero saves. `EXIT_NOSAVE` completed and Bannerlord was verified closed. Protected fixture SHA-256 `A91F15BF1403F1D29F942C56A1162F431113942CDACCAFE52F4D80303B4CB427` and timestamp `2026-09-24T17:20:12.2384633Z` were unchanged. Rollback SHA-256 remained `B15B7B4D3F77EF40D6080EECF07B0077BA4B0E5ABD176C3C4328166BFB4A3ED0`; Strategic Commitment remained `Mode=Observe`, and Visual War remained OFF. Writer/retrieval policy, ActorId/BranchId, D1/D2 schema, gameplay, and saved world state were not changed by this closure.

The focused evidence preserves the earlier preparation log's separate Phase 7B civic-policy hash-check failure without claiming it was fixed; historical focused I3 validation is distinguished from runtime proof. No validation suite or prior Phase 4/5/6 experiment was rerun. The earlier Phase 7B Nemos bounded null remains preserved in its own report.

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

- `Reports/Release/PHASE8B_I4R1_RELEASE_PROFILE_RUNTIME_SMOKE_RESULT.md`
- `Reports/Release/evidence/phase8b_i4r1_release_profile_smoke_20260926.txt`
- `Reports/Release/PHASE8B_I4_RELEASE_PROFILE_RUNTIME_SMOKE_RESULT.md`
- `Reports/Release/evidence/phase8b_i4_release_profile_smoke_20260926.txt`
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
- `Reports/GenerationalContinuity/PHASE7C_I3_RUNTIME_RESULT.md`
- `Reports/GenerationalContinuity/evidence/phase7c_i3_runtime_20260926.txt`

## Next bounded milestone

The requested Phase 8B-I4 and I4R1 smokes have not passed. Both attempts are closed with exact failure evidence; neither establishes a reproducible ClanAI defect or justifies a gameplay change. The guarded reload/post-reload checks remain unproven.

Stop at this checkpoint. Do not rebuild, substitute the package, repeat this attempt, start a product fix, or begin another release-hardening milestone as part of the same task. Further runtime work requires a separately bounded task with reliable campaign-hour stop control.

Final product direction remains standalone, installable, and offline with no runtime development-tool dependency.
