# Codex Status

## Current checkpoint - THEMATRIX v0.1 limited developer border filter

**LIMITED DEVELOPER DEMO PACKAGE BUILT AND TESTED; NOT DEPLOYED OR RUNTIME-TESTED. This is not full kingdom-border behavior.**

- Candidate: `THEMATRIX v0.1.0`, SHA-256 `F85382892B293FFABF78B57923A152735E812A033FE9593DEC9B7AEEBE88C8F9`. Package folder: `DevBuilds/THEMATRIX-v0.1.0-border-filter-dev/`; distributable ZIP: `DevBuilds/THEMATRIX-v0.1.0-border-filter-dev.zip`. Module ID, DLL name, assembly, and namespaces remain ClanAI.
- Policy is default-open. Config lines are `ClosedTerritoryKingdomId>ExcludedVisitorKingdomId`; the territory kingdom excludes the visitor kingdom from choosing its settlements as ordinary visit candidates. Uses live kingdom/settlement owner data; no inferred control zones.
- Production parser and filter are tested directly, not a fixture stub: 98 HomeAssignmentRuntime checks cover missing/empty/malformed/self/reverse/reset/directional rules, ownership changes, actors/exemptions, current target preservation and empty candidate-list diagnostics. Kingdom policy 14 PASS, HomeAssignment policy/persistence 43 PASS, Phase 3 preservation 10 PASS.
- Actual `netstandard2.0` Release against installed Bannerlord v1.5.3 references: 0 errors, 1 inherited `System.ValueTuple` warning. Existing native diagnostic snapshot includes filter counters without per-candidate file IO.
- Scope: ordinary eligible NPC lord parties and native visit destinations only. No route crossing/frontier enforcement, entry veto, villagers, caravans, convoys, trade or diplomacy vote integration. Empty native visit list yields no GoToSettlement score; another action is not guaranteed. End-to-end no-stall behavior is unverified. Config is loaded at behavior registration and takes effect after campaign/session reload.
- No v0.1 deployment, game launch or save action occurred. Do not deploy while the user's current game may be running.
- Diagnostic access is automatic only in the Evidence runtime profile; there is no public on-demand command. The package defaults to Release and does not ship RuntimeProfile.cfg. The corrected Evidence opt-in, restart/cache behavior, output paths and broader telemetry side effects are in the package runbook.
- Corrected package ZIP SHA-256: `E9A8CDDB4A8217CCA30E208A61E4172E45B1823752A0F8865DB2D42FD5F13612`; DLL bytes and SHA remain unchanged.
- Report/evidence: [border checkpoint](../LivingWorld/LW2_KINGDOM_BORDER_FOUNDATION.md), [validation receipt](../LivingWorld/evidence/thematrix_v0_1_border_destination_filter_20260930.txt), [diagnostic/runbook correction receipt](../LivingWorld/evidence/thematrix_v0_1_diagnostics_runbook_20260930.txt), and package [runbook](../../DevBuilds/THEMATRIX-v0.1.0-border-filter-dev/DEMO_RUNBOOK.md).

## Restored LW1-C3 deployment and disposable-save status

**The earlier C2 candidate is installed; roster/save-reload runtime verification remains incomplete.**

- Installed path: `C:\\Program Files (x86)\\Steam\\steamapps\\common\\Mount & Blade II Bannerlord\\Modules\\ClanAI\\bin\\Win64_Shipping_Client\\ClanAI.dll`. Installed C2 SHA-256: `B3760F027476C89555414C38D94CECB47C4AC0D08EA2B7E16E594BA516DA9563`.
- Pre-deployment installed DLL SHA-256: `B1911B17A04F42C11AB6E23DB3543AFE788FDFA07EFCF34DCF3638D4AB241271`; matching backup and rollback evidence retained in the authorized task workspace. Installed SubModule.xml was unchanged (SHA-256 `287609EEBE39EE00D693CD1C354362C64878F40950BCD787D505F20F065B333D`).
- User-selected original save `LW1C LAND FIX 20260930 0436.sav` remains unchanged. Disposable copy `LW1C LAND FIX 20260930 0436 - LW1-C3 DEMO COPY.sav` is byte-identical at 7,855,918 bytes, SHA-256 `C792B001164A9AF911AF5571C092AC7AC7C1C586FE0FC09D7B1988675D4F8D38`.
- User reported that the Home Assignment menu is present and described its presentation as messy. No stable roster/status audit, AI movement proof, or save/reload verification is claimed. Do not touch the current game.
- Details and receipts: [C3 deployment status](../LivingWorld/LW1C3_DEPLOYMENT_STATUS.md), [deployment receipt](../LivingWorld/evidence/lw1c3_deployment_20260930.txt), [disposable-copy receipt](../LivingWorld/evidence/lw1c3_disposable_save_20260930.txt).

## Current checkpoint - LW1-C2 Persistent Household Responsibility Roster

**IMPLEMENTATION + FOCUSED TESTS + GAME-TARGET RELEASE BUILD COMPLETE, 2026-09-30 UTC. Runtime is not yet tested.**

- Exact source base: GitHub `main` commit `5b9e47b2813373fe0b03bae58d41c88a8dfcff83`; materialized source was verified against its Git tree. The separate legacy checkout `D:\BannerlordAIResearch` was left untouched.
- The owned-town/castle Manage Home Assignments menu now uses a bounded roster from direct player-clan hero/companion collections plus valid assigned identities. Temporary states stay visible with explicit suspended-responsibility statuses; active behavior still uses the original strict `Eligible` predicate.
- Stable `Hero.StringId` identity, D1 schema, `ClanAI_HomeAssignment_v1`, 1.25 policy factor, native visit seam, and Phase 3 war Home Responsibility remain unchanged. RC1 package/archive bytes were not written.
- Focused deterministic suites: **68 roster/runtime PASS, 43 persistence/D1 PASS, 10 Phase 3 preservation PASS**.
- `NETStandard.Library` 2.0.3 was found in the signed-in user's NuGet cache, verified via package metadata/hash sidecar, and restored through a read-only local feed into the task-local cache.
- Required actual `netstandard2.0` Release build: **0 errors, 1 inherited MSB3277 `System.ValueTuple` reference-conflict warning**. Focused API compile also passes.
- Dev candidate: `DevBuilds/ClanAI-v0.23.0-LW1C2-dev/ClanAI.dll`, SHA-256 **`B3760F027476C89555414C38D94CECB47C4AC0D08EA2B7E16E594BA516DA9563`**.
- Acceptance audit maps all 25 requested cases to deterministic assertions and static baseline checks in the report. RC1 artifacts, D1 key/schema, factor 1.25, visit seam, and Phase 3 sources remain unchanged.
- At the C2 checkpoint, runtime had not been tested and no deployment/save activity had yet occurred. Later C3 deployment and disposable-copy preparation are recorded in the current status section above.
- Report: [LW1-C2 persistent household roster](../LivingWorld/LW1C2_PERSISTENT_HOUSEHOLD_ROSTER.md); evidence: [C2 validation receipt](../LivingWorld/evidence/lw1c2_validation_20260930.txt).

**Historical next checkpoint at the original C2 report: LW1-C3 - deploy persistent-roster dev candidate, verify stable names/statuses in player's organic campaign, finish bounded runtime/save-reload closure.** Deployment and disposable-save prep are recorded above; stable roster/status and save/reload closure remain incomplete.

## Previous checkpoint - LW1-B Persistent Home Assignment

## Current checkpoint — LW1-B Persistent Home Assignment

**COMPLETE — IMPLEMENTED + TESTED + RELEASE BUILD READY FOR DEPLOYMENT, 2026-09-30 UTC.**

- Development identity: **v0.23.0-LW1B-dev**, assembly/file version 0.23.0.0.
- Multiple independent player-clan adult family/companion-led land lord parties can have distinct owned-town/castle homes; main/army/caravan/disabled/stopped/disbanding/special parties are excluded.
- Native owned-settlement menu: **Manage Home Assignments** → party/current home → Assign here or Clear. Assignment/change/clear messages are one-shot.
- Persistence: **ClanAI_HomeAssignment_v1**, D1 base64 leader/home StringIds. Existing 12 save keys unchanged; new schema is isolated/additive.
- Exact approved native visit-list postfix uses supported native suitability/navigation helpers. Native visit scoring, candidate creation, composer indices, final winner and movement commit remain native.
- HomeAssignmentPolicy exact-home visit/patrol factor **1.25** during peace; urgent native defense and war bypass this policy. Phase 3 war code/composer/context remain byte-identical.
- Deterministic policy/save tests: **43 PASS**. Runtime adapter boundary fixtures: **29 PASS**. Existing Phase 3 Home Responsibility: **10 PASS**. Native metadata, source invariants, release/standalone/preservation checks passed.
- Release build: **0 errors**, one existing System.ValueTuple/Harmony reference-conflict warning.
- Dev DLL: `DevBuilds/ClanAI-v0.23.0-LW1B-dev/ClanAI.dll`.
- Dev DLL SHA-256: **EEE566615AA72BFEB01C95D05AE8FCF022271F90A2519319D95AF3C90D6A4622**.
- Runtime status: **NOT YET TESTED**.
- Bannerlord launched: **NO**.
- At the time of this LW1-B checkpoint: deployed: **NO**.
- RC1 changed: **NO**. RC1 remains frozen/playable at `9ec113e738af35254e113fbde7c05babbf3c405d`; archive and tested package/DLL unchanged.
- No direct movement, synthetic AIBehaviorData, teleportation or mandatory external IO added.
- Broader pre-existing test limitations are recorded in the implementation report; they are not claimed green.

Report: [LW1-B implementation](../LivingWorld/LW1B_HOME_ASSIGNMENT_IMPLEMENTATION.md).

**Historical next checkpoint at the LW1-B checkpoint: LW1-C — deploy the LW1-B dev candidate and run the bounded playable Home Assignment demo/runtime proof.** Later C3 deployment/copy prep is recorded above; runtime closure remains incomplete.

## Previous checkpoint — LW1-A2R source decision

## Archived checkpoint — LW1-A2R Peacetime home seam decision

**COMPLETE — source interpretation only, 2026-09-30.** Supported Bannerlord v1.5.3 native source now closes the Persistent Home Assignment candidate/control question.

A normal non-main, AI-enabled, non-army player-clan lord party uses `AiPartyThinkBehavior.PartyHourlyAiTick`, native `CampaignEventDispatcher.AiHourlyTick`, `PartyThinkParams.AIBehaviorScores`, and native `SetPartyAiAction` commit. Main party and disabled/decision-stopped AI are explicit selector exclusions; army membership materially changes native producers/escort control, so LW1-B keeps `Army == null`. No separate ordinary secondary-party manual-order selector gate is established by the retained exact-assembly evidence.

Native `GoToSettlement` is legal for friendly owned towns/castles in peace and an exact home can reach `AIBehaviorScores` when the visit producer processes it. It is not reliably available arbitrarily far: `AiVisitSettlementBehavior.FillSettlementsToVisitWithDistancesAsDays` applies a hard maximum-distance filter before scoring, and the sorted scoring loop may stop on an earlier good-enough settlement. A retained distant visit inside the list gets native score `0.025`. Native defensive `PatrolAroundPoint` can use towns but explicitly excludes castles, so patrol is not a universal home carrier. `DefendSettlement` requires an active hostile attacker and remains the separate urgent war path.

**Direct composer sufficient: NO.** Selected one earlier native-compatible seam: `AiVisitSettlementBehavior.FillSettlementsToVisitWithDistancesAsDays`. LW1-B may expose/retain only the exact assigned native settlement after native suitability/navigation, then leave native visit scoring, `AIBehaviorData` creation, `PartyThinkParams`, final winner and `SetPartyAiAction` untouched. No synthetic candidate, target assignment, move command, teleport or second movement engine is authorized. The v1 peacetime exact-home factor is **1.25**; the retained native far-floor arithmetic is `0.025 × 1.25 = 0.03125`, just above the non-army visit/patrol selector threshold `0.03`, while stronger native purposes remain free to win.

Persistence `Hero.StringId → Settlement.StringId`, the `ClanAI_HomeAssignment_v1` D1 contract, owned-town/castle Home Assignment menu, O(1) assignment path, land-only first scope, and existing War Home Responsibility preservation all remain valid. A2R itself changes no save schema.

Gameplay changed: **NO**. Save schema changed: **NO**. RC1 changed: **NO**. Bannerlord launched: **NO**. ClanAI built/deployed: **NO**.

Report: [LW1A2R_PEACETIME_SEAM_DECISION.md](../LivingWorld/LW1A2R_PEACETIME_SEAM_DECISION.md).
Evidence: [lw1a2r_peacetime_seam_decision_20260929.txt](../LivingWorld/evidence/lw1a2r_peacetime_seam_decision_20260929.txt).

**Implementation clearance: GO — ONE NATIVE SEAM.**

**Exact next checkpoint: LW1-B — implement and build the playable Persistent Home Assignment / Peacetime Responsibility vertical slice.** Do not begin LW1-B in this A2R checkpoint. Stop after commit/main verification.


## Preserved LW1-A2S Native source acquisition

**COMPLETE — 2026-09-29.** The connected RC1 Bannerlord machine `DESKTOP-JO4B7VH` is reachable again. Read-only acquisition from the exact installed runtime recomputed the native input identities: `TaleWorlds.CampaignSystem.dll` SHA-256 `5B23C3E36D7A5D6D47C47EAB075E9E2B1CC8D1AA53C79E135FA2B5EF434EBC5F` and `SandBox.dll` SHA-256 `16AF436C569675EB30E22514BB755E6FB3612AFCE38F6CC55D1748D079C19C1A`. Supported runtime provenance is Bannerlord `v1.5.3`, retained engine build `122374`, Steam buildid `25302170`.

Existing local `ilspycmd 11.0.0.9375` / `ICSharpCode.Decompiler 11.0.0.9375` was used without downloads. Targeted, hash-provenanced excerpts retain the exact native selector entry, `PartyThinkParams.AIBehaviorScores` storage/selection path, `GoToSettlement`, `PatrolAroundPoint`, `DefendSettlement` generation, army-member behavior, player/main-party/AI-control gates, and land/naval navigation inputs. `SandBox.dll` was independently inventoried; no full proprietary DLL or full assembly decompile is committed.

This is **source acquisition only**. LW1-A2S does not decide exact peacetime home availability and does not select an implementation seam. The acquired evidence is sufficient to resume that interpretation in the next checkpoint.

RC1 remains frozen/playable at `9ec113e738af35254e113fbde7c05babbf3c405d`; tested ClanAI DLL SHA-256 remains `A18341FB2CD6B8623B45B52155C1A7D987DBEE7AAE14AE7DBF704514EC7F37B5`. Bannerlord launched: **NO**. Gameplay/save schema/RC1 changed: **NO**. ClanAI built/deployed: **NO**.

Report: [LW1A2S_NATIVE_SOURCE_ACQUISITION.md](../LivingWorld/LW1A2S_NATIVE_SOURCE_ACQUISITION.md).
Evidence directory: [native](../LivingWorld/evidence/native/).

**Exact next checkpoint: LW1-A2R — resume supported-native peacetime candidate-generation/control-seam closure using acquired source evidence.** Do not begin LW1-B.

Stop after this acquisition commit/main verification.

## Preserved LW1-A2 Native source closure blocker

**BLOCKED / INCOMPLETE — 2026-09-29.** Fresh source-access checks confirm DESKTOP-JO4B7VH remains offline and the inspected workspace/current GitHub tree has no supported native assemblies/full peacetime selector. No native-generation/control question has been closed; exact near/far visit/patrol home availability remains **UNKNOWN**. No native-compatible seam is selected.

This is an evidence-access blocker, not proof that native home candidates or lawful seams are impossible. LW1-A's NO-GO remains an implementation hold pending source evidence. **Do not begin LW1-B.**

RC1 remains frozen/playable at `9ec113e738af35254e113fbde7c05babbf3c405d`. Bannerlord launched: **NO**. Gameplay/save schema/RC1 changed: **NO**. Gameplay DLL built: **NO**. ClanAI deployed: **NO**.

**Next checkpoint: resume LW1-A2 with the exact supported TaleWorlds.CampaignSystem.dll and SandBox.dll or complete targeted source with matching binary-hash provenance.** ROADMAP already identifies A2; no sequence change is needed.

Report: [LW1A2_NATIVE_SOURCE_CLOSURE.md](../LivingWorld/LW1A2_NATIVE_SOURCE_CLOSURE.md).
Evidence: [lw1a2_native_source_access_20260929.txt](../LivingWorld/evidence/lw1a2_native_source_access_20260929.txt).

Stop after this documentation commit/main verification.

## Preserved LW1-A Home Assignment audit checkpoint

**INCOMPLETE — source-access blocker, 2026-09-29.** Current ClanAI candidate-preserving composer, war Home Responsibility, save-key inventory and proposed assignment design are audited. Supported native peacetime candidate generation is not fully accessible; near/far exact-home availability is unresolved. Do not infer a missing candidate can be expressed through score multiplication.

| Field | Status |
|---|---|
| LW1-A audit | INCOMPLETE |
| Bannerlord launched | NO |
| Gameplay changed | NO |
| Save schema changed | NO |
| RC1 changed | NO |
| Native peacetime home candidate availability sufficient | PARTIAL — present-candidate influence only; generation unresolved |
| Persistence contract selected | YES — design only |
| UI seam selected | YES — town/castle menu surface; selector signature pending |
| Performance design bounded | YES |
| War Home Responsibility preservation plan defined | YES |
| Implementation authorized by audit | NO-GO |
| Exact next checkpoint | LW1-A2 — supported-native peacetime candidate-generation and control-seam source closure |

RC1 remains frozen/playable at `9ec113e738af35254e113fbde7c05babbf3c405d`. No ClanAI build/deployment, policy tuning or package/DLL change. The game machine is offline; no supported native binary/full selector was resolved from available offline inputs. This is an evidence-access blocker, not proof that native home candidates are impossible.

Audit: [LW1A_HOME_ASSIGNMENT_NATIVE_AUDIT.md](../LivingWorld/LW1A_HOME_ASSIGNMENT_NATIVE_AUDIT.md).
Evidence: [lw1a_home_assignment_native_audit_20260929.txt](../LivingWorld/evidence/lw1a_home_assignment_native_audit_20260929.txt).

Stop after the documentation commit/main verification. Do not begin LW1-A2 or LW1-B in this task.

## Preserved LW0 Living World Direction checkpoint

**COMPLETE — documentation only, 2026-09-29.** RC1 remains frozen/playable at `9ec113e738af35254e113fbde7c05babbf3c405d`. The post-RC1 Living World direction is documented in [LIVING_WORLD_DIRECTION.md](../../LIVING_WORLD_DIRECTION.md), with an ordered LW1–LW9 line in the roadmap. Phase 8C remains active through normal user play as ongoing field validation.

Gameplay changed: **NO**. Policy tuned: **NO**. Save key/schema changed: **NO**. RC archive/tested package/DLL changed: **NO**. Bannerlord launched: **NO**. ClanAI rebuilt or deployed: **NO**.

**Next development checkpoint: LW1-A — Persistent Home Assignment native-seam/design audit.** LW1 is not implemented or started by this task. Stop after the LW0 documentation commit; do not begin LW1 in this task.

Evidence: [LW0_DIRECTION_CHECKPOINT.md](../LivingWorld/LW0_DIRECTION_CHECKPOINT.md).

## Preserved RC1 freeze checkpoint

Phase 8B-I5 RC freeze / player-playable internal release candidate: **COMPLETE**

The exact Phase 8B-I4R4-tested v0.22.0 package payload is frozen without rebuilding or source changes. The three-file installable archive is `Releases/ClanAI-v0.22.0-RC1.zip`, size `129579` bytes, SHA-256 `A03D183934BCF1C974EA6BCEBBF35CFF3A5413080D6F6634942811AE54C971D6`. Its ClanAI DLL remains byte-identical to the passed runtime candidate: SHA-256 `A18341FB2CD6B8623B45B52155C1A7D987DBEE7AAE14AE7DBF704514EC7F37B5`.

Offline package validation passes: exact three-file contents; v0.22.0 module identity; external Bannerlord.Harmony dependency; no bundled Harmony, NavalDLC dependency, development path, external-service/runtime-tool dependency, Evidence opt-in, Visual War marker, or unexpected artifact. The inherited standalone-profile, dependency/version, package, save-key, and Phase 3–7 preservation gates remain green. Phase 8B-I4R4's completed runtime PASS remains authoritative and untouched.

Player installation notes and the short organic-campaign beta plan are committed at `Reports/Release/CLANAI_V0220_RC1_INSTALL.md` and `Reports/Release/CLANAI_V0220_RC1_PLAYER_BETA.md`. No gameplay, policy math, save schema, DLL, or package payload changed; Bannerlord was not launched or deployed in I5.

**Release classification: RC1 READY FOR PLAYER CAMPAIGN.**

**At RC1 freeze, the next milestone was Phase 8C field validation. Current direction is recorded above: Phase 8C through normal user play alongside the Living World development line.**

## Preserved earlier I4R4 blocked preflight — historical

The following section describes only the earlier inaccessible-machine request at `2344ea02c229e9dfa80d649c02902fa794bddc19`, not the later executed runtime. Its former report remains in that commit; its exact focused evidence is copied to the historical path above.

Phase 8B-I4R4's fresh preflight is closed as **PRELIGHT BLOCKED / NOT EXECUTED**. Starting GitHub main: `fca92b005d87d04d7c7677d2b2dd7865db6e5da1`. Remote Desktop Commander reported `DESKTOP-JO4B7VH` offline; a direct device ping also failed with `please connect a device to use remote tools`. This is a before-launch environment-access blocker, not an executed runtime/control FAIL or a demonstrated ClanAI defect. The previous unreachable I4R4 request is likewise blocked/not executed.

No remote process or UI command was issued. No Bannerlord launch, campaign load, probe, calibration, advancement, Save, reload, diagnostic Save, build, deployment, or package substitution occurred. The installed DLL, protected fixture hash/timestamp/DayLong, current process state, configuration, and telemetry were **NOT REVERIFIED**. Runtime measurements and checks are **NOT EXERCISED**, not passed and not a measured zero-hour total.

Only the blocked-preflight report, focused evidence, and this checkpoint insertion are changed. Earlier status text and evidence are preserved. The native-time-pause protocol remains unchanged. See `Reports/Release/PHASE8B_I4R4_RELEASE_PROFILE_RUNTIME_SMOKE_RESULT.md` and `Reports/Release/evidence/phase8b_i4r4_release_profile_smoke_20260927.txt`; the date suffix is the requested artifact name, not a runtime date.

**Ready for next Phase 8 release-hardening checkpoint: NO.** Stop here; no access repair, restart, extension, or substitute run is part of this closure. A later authorized live attempt must begin a fresh preflight once the game machine is reachable. The I4R4 smoke remains unexecuted.

## Preserved Phase 8B-I4R4P offline preparation

Phase 8B-I4R4P's offline native-time-pause protocol is **COMPLETE**. I4, I4R1, I4R2, and I4R3 remain closed test-control failures; none demonstrates a ClanAI defect. I4R3 proved that native campaign time pause stayed visibly reliable after its one-second calibration, while two observed synthetic Escape requests failed to open the menu. Advancement stopped permanently, and the native lower-left menu button later opened the menu for safe exit while time pause remained selected.

`TIME_PAUSED_CONFIRMED` is now the sole campaign-safety latch. The Escape overlay is no longer required for advancement or metadata inspection. `MENU_OPEN_CONFIRMED` is required only for Save/Load navigation, with the native clickable lower-left menu button preferred and synthetic Escape optional. Every Save enters a mandatory post-Save live time verification before metadata, notes, helper use, or window switching. Loss of time-pause integrity ends advancement and forbids diagnostic saves.

The repository-only DayLong helper remains read-only/no-network/no-game-command; its checklist now separates advancement safety from Save/Load navigation. The exact packaged DLL remains SHA-256 `A18341FB2CD6B8623B45B52155C1A7D987DBEE7AAE14AE7DBF704514EC7F37B5`; package manifest, gameplay source, save schema, and I4/I4R1/I4R2/I4R3 evidence are unchanged. Bannerlord was not launched and ClanAI was not built/deployed. See `Reports/Release/PHASE8B_I4R4P_TIME_PAUSE_PROTOCOL.md`.

Next milestone: one separately authorized fresh **Phase 8B-I4R4 live release-profile smoke** using native time pause as the primary latch and native menu-button navigation.
## Preserved Phase 8B-I4R3P offline preparation

Phase 8B-I4R3P's offline dual-pause release-smoke protocol is **COMPLETE**. I4, I4R1, and I4R2 remain closed operator/test-control failures; none demonstrates a ClanAI defect. I4R2 proved the Escape overlay alone is insufficient: its one-second calibration stopped correctly, but native Save returned to a moving campaign before the menu was reacquired, making the saved measurement stale. A later diagnostic Save repeated the uncontrolled post-save interval.

The next live retry requires two independent positive latches: a fresh live observation of Bannerlord's native time control at paused/zero speed and a fresh live observation of the Escape menu. Only both together establish `DUAL_PAUSED_CONFIRMED`. Advancement and Save now have separate mandatory state machines; after every Save, time pause must be reacquired and confirmed before the Escape menu, and no metadata/tool/window switching is permitted until dual pause is restored. Loss of either latch ends advancement and forbids diagnostic saves.

The repository-only DayLong helper remains read-only/no-network/no-game-command and now prints an operator dual-pause checklist. The exact packaged DLL remains SHA-256 `A18341FB2CD6B8623B45B52155C1A7D987DBEE7AAE14AE7DBF704514EC7F37B5`; package manifest, gameplay source, save schema, and I4/I4R1/I4R2 evidence are unchanged. Bannerlord was not launched and ClanAI was not built or deployed. See `Reports/Release/PHASE8B_I4R3P_DUAL_PAUSE_PROTOCOL.md`.

Next milestone: one separately authorized fresh **Phase 8B-I4R3 live release-profile smoke** using this dual-pause/post-save verification protocol.
## Preserved Phase 8B-I4R2P offline preparation

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

- `Reports/Release/PHASE8B_I4R2_RELEASE_PROFILE_RUNTIME_SMOKE_RESULT.md`
- `Reports/Release/evidence/phase8b_i4r2_release_profile_smoke_20260926.txt`
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

The earlier I4/I4R1/I4R2/I4R3 test-control failures and the I4R4 blocked preflight remain historical. The later I4R4 runtime has now passed the bounded release-profile smoke; its guarded save/reload and post-reload progression are proven within the scope of the corrected report.

Recommended next checkpoint: **Phase 8B-I5 — RC freeze / player-playable release-candidate checkpoint**. **Ready for RC freeze: YES.** I5 is not started by this reporting-only closure. Do not rerun the smoke, rebuild, change gameplay, or begin I5 in this task; stop after the GitHub commit.

Final product direction remains standalone, installable, and offline with no runtime development-tool dependency.
