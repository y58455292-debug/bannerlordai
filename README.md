# BannerlordAI

BannerlordAI is an experimental Mount & Blade II: Bannerlord mod project focused on autonomous world-scope clan and lord behavior while keeping Bannerlord's native eligibility, action, and campaign systems authoritative.

The active mod source is `src/ClanAI`. `src/BannerlordInspector` is the observation harness. `src/ProviderPipeline` contains the retained provider-pipeline milestone work, but provider reasoning is not currently part of the proven clan-loyalty behavior described below.

## Current state

The current ClanAI source builds on the Phase 2B candidate `v0.22A-ruler-courtship-native-v1` and now also includes the Phase 2D-L1 kingdom-continuity ledger. Phase 2B ruler recruitment and Phase 2C independent-clan activity are runtime-proven. Phase 2D-L1 is implemented, build-proven, deployed with rollback, and runtime-proven for its guarded save/load continuity checkpoint. The uniquely named continuity save (`ClanAI V020V PERSIST PHASE2D L1 CONTINUITY 20260925 0130`) materialized successfully and, on a fresh direct reload, restored seven serialized records (`found=True`), reconciled seven active kingdoms into seven records, emitted no succession/destruction callback or duplicate notice, and reported `mutation=False`. Exact reload evidence is under `Reports/KingdomContinuity/evidence/phase2d_l1_reload_20260925.txt`. Natural kingdom lifecycle-event observation remains an honest null and a separate broader Phase 2D follow-up. Phase 3 has now completed its first offline Home Responsibility deterministic seam: factor/eligibility logic is isolated in a pure policy, ten deterministic cases pass, runtime wiring invariants pass, and the Release build remains at 0 errors. No DLL was deployed and no new campaign behavior is claimed by that checkpoint. The next bounded Phase 3 seam is also complete for the existing Kingdom Objective layer: readiness/supply and urgent-home-threat refusals, direct/staging factor selection, competitive-ratio/winner-margin logic, factor bounds, and pending-winner conditions are isolated in `KingdomObjectivePolicy`; 34 deterministic cases and a wiring invariant pass, with Release still at 0 errors. Its bounded runtime proof now also passes: under Rath's natural Battania `BorderSecurity` objective for Uthelaim Castle, Culharn's native winner changed from `RaidSettlement:Stathymos` to the existing staging candidate `GoToSettlement:Pendraic Castle` under the unchanged 1.55 factor, and the existing post-vanilla check observed Bannerlord committed `GoToSettlement` to Pendraic Castle with `matched=True`. The proof stopped after 28.224 campaign hours, used no synthetic target/action or direct mutation, exited without saving, and left the protected fixture unchanged. The next bounded Phase 3 offline seam is also complete for `VisualWarDecisionLayer`: its existing active-defense, frontier-defense, frontier-offense, rear-security, and weak/recovery rules are isolated in pure `VisualWarPolicy`; 32 deterministic checks and wiring/standalone-path guards pass; and Release builds with 0 errors. Its old `D:\\BannerlordAIResearch` activation marker was replaced with the optional module-local `ClanAI/Data/ENABLE_VISUAL_WAR_LAB.txt` marker, which remains OFF when absent. No DLL was deployed and no Visual War runtime behavior is newly claimed by this checkpoint. A follow-up offline seam now adds an observation-only Visual War commit verifier: only after a Visual War composer winner change it stores the expected existing native behavior/target, then on a later normal AI tick reads Bannerlord's actual behavior/target and logs `VISUAL_WAR_COMMIT_CHECK` with match/expiry state. Twenty-two pure verifier checks, observation-only/no-mutation invariants, all prior Visual War guards, and the Release build pass. The Visual War bounded runtime proof now also passes: with Visual War enabled only by the module-local marker, Arthamund's existing `frontier-defense` contribution changed the current winner from `PatrolAroundPoint:Sibir` to the existing native candidate `PatrolAroundPoint:Goleryn`; the final composed blackboard retained Goleryn, and a later `VISUAL_WAR_COMMIT_CHECK` observed Bannerlord's native `PatrolAroundPoint` behavior targeting Goleryn with `matched=True`. The run stopped after 29.915 campaign hours inside a 72-hour cap, exited without saving, preserved the protected fixture, and removed the temporary marker so Visual War is OFF by default again. The next bounded Phase 3 offline seam is complete for the existing `StrategicCommitmentLayer`: the unchanged 1.10 same-state retention rule and 12-campaign-hour age bound are isolated in pure `StrategicCommitmentPolicy`; 34 policy checks, 13 config/path checks, native-wiring/no-mutation/standalone invariants, and Release build all pass. Its former `D:\\BannerlordAIResearch` config path is now module-local `ClanAI/Data/StrategicCommitment.cfg`; missing/invalid config and the tracked config remain `Observe`, so Apply is never silently enabled. No DLL was deployed and no Strategic Commitment runtime behavior is claimed yet. A follow-up offline seam now adds an observation-only Strategic Commitment commit verifier: pending state is created only after explicit Apply actually rescales the existing previous candidate and the recomputed composer winner is that previous objective with `retained=True`; a later layer observation reads Bannerlord's native behavior/target and logs `STRATEGIC_COMMITMENT_COMMIT_CHECK`. Twenty verifier-policy checks plus creation/wiring and observation-only invariants pass, all prior commitment tests/invariants still pass, and Release builds with 0 errors. The verifier uses an 18-campaign-hour pending lifetime separate from the unchanged 12-hour commitment-eligibility age. A bounded runtime attempt then ran the unchanged commitment candidate with only the installed module-local config temporarily set to `Mode=Apply`. Fresh reset confirmed `configured_apply`, but the strict 72-campaign-hour qualifying window produced zero `STRATEGIC_COMMITMENT_APPLY` and zero `STRATEGIC_COMMITMENT_COMMIT_CHECK` records, so the Strategic Commitment runtime proof remains unproven. The first status poll crossed the bound by 0.198 campaign hours due polling granularity; asynchronous pause handling allowed additional non-qualifying time afterward, which was explicitly excluded from the proof window. No tuning or synthetic scenario was used. The run exited without saving, the installed config was restored to `Mode=Observe`, the tracked config remained Observe, Visual War remained OFF, and the protected fixture was unchanged. Phase 4A now has its first natural vanilla-recovery characterization. The observation-only seam tracks NPC lord parties after native creation or severe depletion, including post-battle snapshots, with roster health/tier, party limit/ratio, food, settlement visits, volunteer-pool and garrison deltas, and conservative source labels. In a 39.623-hour bounded run, Kulyat of the Forest People grew 37->69: +28 troops occurred across settlement visits with supporting volunteer-pool decreases (and unchanged garrison where observed), while +4 outside-settlement growth remains explicitly unresolved. The same run observed native created lord parties already carrying troops before settlement interaction, defeated-side parties with wounded rosters, and an Iara castle visit where party +7 matched garrison -7 with volunteers unchanged, supporting garrison withdrawal. No recruitment/manpower behavior was changed. The separate target-kingdom defection boundary remains timeboxed and unproven.

A corrected Phase 4A same-hero defeat/recreation characterization now also passes. Megenhelda's exact defeated participant identity was accepted from the early `MapEventStarted` capture after the live end-time leader had disappeared; the old native party was then destroyed. 83.533 campaign hours later the same hero StringId received a distinct native party creation outside settlement with 22 total members, including 21 regular troops, and the first pre-entry boundary at Pravend preserved the same roster with `completeChain=True`. The run stopped after 122.234 campaign hours at the first complete chain, exited without saving, and preserved the protected fixture. This supports `PostDefeatNativeRecreationInitialTroopsSupported` for one natural causal chain; it does not establish a universal respawn amount. Phase 4B remains unstarted. See `Reports/Manpower/PHASE4A_CORRECTED_LINKED_RECREATION_RUNTIME_RESULT.md`.

Phase 4B has now completed its first required offline design/native-capability audit. Bannerlord's daily notable volunteer regeneration is centralized through the selected `VolunteerModel`, while the same six notable volunteer slots are consumed by player recruitment, ordinary AI lord recruitment, and garrison auto recruitment. The selected v1 seam is a delegating `VolunteerModel` wrapper that changes only **empty-slot refill probability**; occupied-slot upgrade probability, culture, troop type/tier, relation gates, AI choices, player choices, and all pool/roster mutations remain native. The first candidate uses native prosperity/hearth level, local security, and active raid/siege with a 0.35..1.00 multiplier, so healthy secure territory remains vanilla and damaged territory recovers more slowly without permanent collapse. War Strain is deliberately deferred because an existing AI-only recruitment throttle already consumes it. No Phase 4B gameplay code was added or deployed at this audit checkpoint. See `Reports/Manpower/PHASE4B_LOCAL_MANPOWER_NATIVE_CAPABILITY_AUDIT.md`.

Phase 4B-v1 has now completed its offline implementation checkpoint. `LocalManpowerProbabilityPolicy` implements the audited empty-slot multiplier, while `LocalManpowerVolunteerModel` wraps the already-selected native `VolunteerModel`, calls native production exactly once, and delegates all other volunteer behavior unchanged. Fifty deterministic policy checks, 13 compiled delegation checks, wiring/no-mutation/standalone invariants, all Phase 4A tests, and relevant territorial invariants pass; Release builds with 0 errors plus the inherited `System.ValueTuple` warning. The new DLL was not deployed and Bannerlord was not launched, so Local Manpower is implemented/build-proven rather than runtime-observed. See `Reports/Manpower/PHASE4B_LOCAL_MANPOWER_V1_OFFLINE_IMPLEMENTATION_RESULT.md`.

Phase 4B-v1 now also has its first bounded runtime characterization. The selected wrapper was `LocalManpowerVolunteerModel` around native `DefaultVolunteerModel`. A healthy High-hearth Vinela empty slot at security 99.16 preserved native probability exactly; a natural Mid-hearth Lysia empty slot applied the unchanged 0.95 population factor, moving native 0.525 to 0.498749971; and an occupied Vinela slot remained exactly 0.525. Immediately after the Lysia evaluation, Bannerlord's native daily volunteer update changed the observed empty slot to an Empire `imperial_vigla_recruit`, with `mutationByClanAI=False`. Required proof completed after only 0.180 campaign hours and the run paused after 0.415 hours, exited without saving, and preserved the protected fixture. Low-population, low-security, acute raid/siege and shared AI/garrison-consumption branches were not exercised in this brief run, and **balance remains unproven**. See `Reports/Manpower/PHASE4B_LOCAL_MANPOWER_V1_RUNTIME_RESULT.md`.

Phase 4C has now completed its first offline native-capability/design audit. Bannerlord's notable volunteer quality progression uses the same daily `VolunteerModel` probability as its first gate, followed by a separate native notable-power/current-tier roll and native direct `UpgradeTargets` selection, with native `MaxVolunteerTier=4`. Higher-quality volunteers are also sorted toward later slots whose native first-gate probability is lower. The selected v1 seam is therefore to extend the existing selected wrapper only for **occupied, native-upgrade-eligible** slots while leaving Phase 4B empty-slot behavior unchanged and leaving notable power, tier, culture, troop trees, RNG and actual slot mutation native. The audit proposes the same local population/security/acute context components but a separate quality floor of 0.50 to avoid compounding native quality scarcity too aggressively; this is a safety bound, not a balance claim. No Phase 4C gameplay source was added, Bannerlord was not launched, and no DLL was deployed. See `Reports/Manpower/PHASE4C_TROOP_QUALITY_NATIVE_CAPABILITY_AUDIT.md`.

Phase 4C-v1 has now completed its offline implementation checkpoint. A pure `TroopQualityProbabilityPolicy` and read-only native eligibility helper extend the existing single `LocalManpowerVolunteerModel`: empty slots keep Phase 4B exactly, occupied non-upgradeable slots pass through, and only occupied native-upgrade-eligible slots receive the separate quality first-gate multiplier with a 0.50 floor. Bannerlord still owns notable power/current-tier second gating, `UpgradeTargets`, RNG, culture/tree and actual mutation. Phase 4B's pure policy blob is unchanged; 69 Phase 4C policy checks, 23 compiled branch/delegation checks, Phase 4C no-mutation/standalone guards, all Phase 4B/4A tests and relevant territorial invariants pass. Release builds with 0 errors plus the inherited `System.ValueTuple` warning. The Phase 4C DLL was not deployed and Bannerlord was not launched, so Phase 4C is implemented/build-proven but not runtime-observed or balance-proven. See `Reports/Manpower/PHASE4C_TROOP_QUALITY_V1_OFFLINE_IMPLEMENTATION_RESULT.md`.

Phase 4C-v1 now also has a bounded runtime characterization. The single selected `LocalManpowerVolunteerModel` wrapped native `DefaultVolunteerModel`. A healthy upgrade-eligible Vinela slot preserved native 0.525 exactly; a naturally Mid-hearth Lysia slot applied the unchanged 0.95 quality multiplier, reducing native 0.367499977 to 0.349124968; and a tier-4 Tarcutis volunteer at native `MaxVolunteerTier=4` correctly bypassed Phase 4C with exact native passthrough. Bannerlord also naturally upgraded Diocosos of Dradios's `imperial_archer` tier 2 to its direct native `imperial_trained_archer` tier 3 target, with source/target counts 1→0 and 0→1 and `mutationByClanAI=False`. Minimum proof completed after 0.775 campaign hours and pause acknowledgement was at 1.072 hours, far inside the 168-hour bound. The run exited without saving and preserved the protected fixture. Low-population, sub-50-security, acute raid/siege and the 0.50 floor were not runtime-exercised, and **balance remains unproven**. See `Reports/Manpower/PHASE4C_TROOP_QUALITY_V1_RUNTIME_RESULT.md`.

Phase 5 has completed its first offline native-capability/design audit. Bannerlord's ambient bandit ecology is split between globally bounded looter refill, hideout-linked culture bandits, hideout infestation/replacement, native patrols and normal combat suppression. The selected Phase 5-v1 seam is deliberately narrow: modify only native `BanditSpawnCampaignBehavior.GetSpawnChanceInSettlement(Settlement)` for **town/village looter spawn candidates**, leaving hideout calls and global population logic untouched. The first local-control signal is native Security only—town Security or a village's bound-town Security—because Security already reflects garrison strength, looted villages, siege pressure, nearby hideouts and native patrol presence. Candidate spawn-site factor is bounded 0.75..1.25 around neutral Security 50, so secure territory remains a possible looter source and native global caps cannot run away. No Phase 5 source was added, Bannerlord was not launched, and no DLL was deployed. See `Reports/Security/PHASE5_SECURITY_BANDITRY_NATIVE_CAPABILITY_AUDIT.md`.

Phase 5-v1 Local Bandit Control has now completed its offline implementation checkpoint. A pure `LocalBanditControlPolicy` applies only the audited Security factor, while a single Harmony postfix modifies only the returned weight from private `BanditSpawnCampaignBehavior.GetSpawnChanceInSettlement(Settlement)` after Bannerlord's native method runs. Towns use own `Town.Security`; villages use bound-fortification `Town.Security` when safely available; unsupported candidates including hideouts/castles and missing/non-finite Security contexts return without touching the native result. The audited private signature resolves on the supported CampaignSystem binary. Twenty-four deterministic policy checks, 14 compiled target/signature checks, wiring/no-mutation/standalone guards, all retained Phase 4 tests, and relevant Home Responsibility/Visual War/Strategic Commitment validations pass; Release builds with 0 errors plus the inherited `System.ValueTuple` warning. The candidate DLL was not deployed and Bannerlord was not launched. Phase 5-v1 is **implemented + build-proven; runtime-observed: NO; balance-proven: NO**. The next milestone is a separately authorized bounded runtime characterization of this exact candidate; Phase 6 has not started. See `Reports/Security/PHASE5_LOCAL_BANDIT_CONTROL_V1_OFFLINE_IMPLEMENTATION_RESULT.md`.

The Phase 1 demo-first integration gate passed on 2026-09-24. The integrated candidate loaded the protected Syronea baseline, advanced 100.987 campaign hours without a crash, emitted proven player-visible state, created and reloaded a separate guarded demo-gate save, restored persisted ClanAI systems, and advanced again after reload. The playable validation fixture is `ClanAI V020V PERSIST DEMO GATE V021M 20260924` (SHA-256 `A91F15BF1403F1D29F942C56A1162F431113942CDACCAFE52F4D80303B4CB427`). See `Reports/Demo/PHASE1_DEMO_GATE_RESULT.md`.

A real causal chain has been demonstrated:

1. an AI clan experiences a real campaign event;
2. ClanAI records bounded memory from that event;
3. Bannerlord later reaches its own autonomous diplomacy consideration;
4. ClanAI biases only the clan-side native barter value;
5. Bannerlord keeps authority over the >0 decision threshold and the actual `ChangeKingdomAction`.

Phase 1 now proves the voluntary leave-current-kingdom boundary: a natural holding loss can push a native negative leave value above zero and Bannerlord's own leave path can commit the departure. The separate target-kingdom switch/join boundary remains a different surface.
### Rhemtoil / L5 result

fen Morcar lost Rhemtoil Castle in a real siege. 317.338 campaign hours later Bannerlord naturally evaluated whether the clan should leave Battania. The recorded exact-clan holding loss moved the native leave value from -3,955,367 to -3,955,259: a +108 adjustment.

The decision remained no. No faction transfer was forced.

This run demonstrated real-event -> memory -> native loyalty-score causality, but also exposed the scale mismatch in the first direct-loss implementation.

### Nevyansk / L6 result

L6 values a lost clan-owned fief using Bannerlord's native settlement-value model rather than a flat castle/town weight.

fen Morcar later lost Nevyansk Castle in a real siege. Bannerlord valued that holding at 532,054.6 native settlement-value units. L6 recorded 133,013.7 pressure at loss. The record persisted through save/load (`records=1` before and after reload).

At a natural loyalty check 50.017 campaign hours after the loss:

- native leave value: -2,298,743
- decayed direct-loss pressure: 123,773.5
- applied direct-loss modifier: +123,773
- adjusted leave value: -2,174,970
- nativeWouldLeave: false
- adjustedWouldLeave: false
- committed: false
An earlier natural sample of 60 leave considerations produced no native leave decisions, no adjusted leave decisions, and no commits. The least-loyal sampled clan was still about 191,000 points below the >0 leave boundary.

### Phase 1 boundary proof / Banu Ruwaid

On 2026-09-24, Banu Ruwaid naturally lost Vladiv Castle by siege. Bannerlord valued the lost holding at 1,801,322 native settlement-value units. At a later natural leave check while the clan was in Aserai, its native leave value was -250,332. The decayed direct-loss memory was capped to +312,915, producing an adjusted value of +62,583. The log recorded `nativeWouldLeave=False`, `adjustedWouldLeave=True`, and `committed=True`; read-only Inspector verification then showed the clan independent.

This satisfies the preregistered Phase 1 win condition for the **voluntary leave-current-kingdom** path. It does not prove the separate target-kingdom defection/join bias can cross its own boundary.

### Phase 2A target-kingdom defection

Phase 2A was timeboxed after the initial scale-mismatch evidence. Native-score inspection showed that Bannerlord's clan-side defection value already contains the current-kingdom leave score, so M3 made one evidence-backed implementation attempt: target-specific social memory remains separately capped at 25,000, while the already-proven voluntary-leave memory modifier is carried only into Bannerlord's embedded leave component.

The bounded M3 proof advanced 486.070 campaign hours and produced 27 natural `ConsiderDefection` samples:

- target-specific modifier non-zero: 4;
- leave-memory carry non-zero: 0;
- adjusted YES: 0;
- committed target switch: 0.

The closest candidate was Vezhoving → Nord at -180,954 with affordability satisfied. No sampled clan had an active non-zero leave-memory modifier during the M3 window, so the new carry path was not naturally exercised.

**Target-kingdom autonomous defection remains unproven.** The project will not keep instrumenting or tuning this surface in an open-ended loop. Phase 2A is preserved as a timeboxed blocker/null and the roadmap moves to **Phase 2B — ruler recruitment / clan courtship**.

See `Reports/ClanLoyalty/PHASE2_DEFECTION_BOUNDARY_PROTOCOL.md`, `PHASE2_DEFECTION_IMPLEMENTATION_DECISION.md`, and `PHASE2_DEFECTION_M3_ATTEMPT_RESULT.md`.

### Phase 2B ruler recruitment / clan courtship

The first Phase 2B candidate is now implemented and build-proven. On each NPC ruling clan's daily tick it mirrors the relevant native eligibility and safety gates, selects the eligible independent clan with the highest native `GetScoreOfKingdomToGetClan`, evaluates both sides of the ordinary non-defecting `JoinKingdomAsClanBarterable`, and calls only `BarterManager.ExecuteAiBarter` when the native combined surplus is positive. It adds no custom score or direct faction transfer.

Runtime validation passed the preregistered strong win condition. Vlandia selected naturally independent Banu Ruwaid; native values were clan +649 and kingdom +7,401 for a combined +8,050; ClanAI invoked only the native AI barter; and post-state confirmed Banu Ruwaid joined Vlandia. See `Reports/ClanRecruitment/PHASE2B_RUNTIME_RESULT.md`.

## In-game visibility

`v0.21M-player-visibility-v1` adds native Bannerlord message-feed visibility for major clan loyalty shocks, committed voluntary departures, committed target-kingdom defections, and the player kingdom's war-strain state.

Runtime proof now covers the player kingdom's war-strain message, natural clan-owned fief-loss messages, a fresh loyalty-shift message, and a native committed voluntary-leave message. In the strongest fresh example, Gauting naturally lost Skarthness Castle, memory moved its leave value from -401,733 to +100,433, Bannerlord committed the departure from Nord, and the game emitted `ClanAI: Gauting has left Nord after mounting losses and grievances.`

A fresh natural target-kingdom defection commit has **not** yet been observed under the visibility build, so that notification remains implemented/build-proven rather than runtime-proven.

See `Reports/Visibility/2026-09-24-player-visibility-v1.md` and `Reports/Visibility/2026-09-24-loyalty-runtime-proof.md`.

## Loyalty memory and clamps

Ordinary current-kingdom social memory uses:

`social_index = grievance*0.50 + bloodDebt*0.70 + tension*0.25 - trust*0.35 - obligation*0.45`

When both sources exist, leader-direct memory and clan aggregate kingdom memory are blended 65% / 35%. The index is clamped to [-100, 100].

`social_cap = min(25000, max(5000, abs(native_leave_value) * 0.25))`

`social_modifier = social_cap * social_index / 100`

Direct clan-owned fief losses use Bannerlord's native settlement valuation:

`raw_loss_pressure = native_settlement_value * 0.25`

Each loss decays linearly over 720 campaign hours (30 days). Multiple direct losses accumulate for the same clan, with aggregate pressure capped at 750,000.
At the leave-kingdom hook:

`direct_loss_cap = min(750000, max(75000, abs(native_leave_value) * 1.25))`

`direct_loss_modifier = min(decayed_direct_loss_pressure, direct_loss_cap)`

`adjusted_leave_value = native_leave_value + social_modifier + direct_loss_modifier`

The leave patch is context-gated to Bannerlord's own `DiplomaticBartersBehavior.ConsiderClanLeaveKingdom` path and patches the clan-side `LeaveKingdomAsClanBarterable.GetUnitValueForFaction` result. The kingdom-switch patch is similarly scoped to `ConsiderDefection` / `JoinKingdomAsClanBarterable`. These patches do not directly call a faction-transfer action.

## Other retained ClanAI systems

The current source also contains the previously developed Home Responsibility, companion duty/experience/negative-outcome memory, Holdback, world-scope lord behavior, WarState/WarScar tracking, Kingdom Orders, War Strain recruitment/economy coupling, and Prisoner/Mercy behavior. Prisoner/Mercy remains at release threshold 20.

Not all retained experimental layers are release-ready. In particular, external/provider reasoning is not integrated into the proven loyalty path, and a natural completed `SUSTAINED_RAIDING` WarScar proof remains separate outstanding evidence.

## Repository layout

- `src/ClanAI/` — current mod source and package metadata.
- `src/BannerlordInspector/` — current observation harness.
- `src/ProviderPipeline/` — retained provider-pipeline milestone source/tests.
- `Reports/` — consolidated experiment evidence, including null results.
- `Tests/` — retained test entry points.
- `Data/` — small runtime configuration files still referenced by current ClanAI source.
The old Front Office/controller, handoff, historical dump, longitudinal dump, video-update, and abandoned research trees are intentionally not part of the active repository. The pre-cleanup tracked state is preserved on branch `archive/pre-cleanup-2026-09-23`.

## Reviewer entry points

For the current loyalty slice, start with:

- `src/ClanAI/src/ClanAI/SocialLoyaltyPatch.cs`
- `src/ClanAI/src/ClanAI/SocialLoyaltyClanLossMemory.cs`
- `src/ClanAI/src/ClanAI/SocialDefectionPatch.cs`
- `src/ClanAI/src/ClanAI/SocialLedger.cs`
- `src/ClanAI/src/ClanAI/ClanAIStrategicBehavior.cs`
- `Reports/ClanLoyalty/PHASE1_BOUNDARY_RESULT.md`
- `Reports/ClanLoyalty/2026-09-23-clan-loyalty.md`

Machine-readable supporting evidence is under `Reports/ClanLoyalty/evidence/`.

## Build

From the repository root:

`dotnet build src/ClanAI/src/ClanAI/ClanAI.csproj -c Release`

The current local build has zero compilation errors. The Bannerlord/Harmony dependency set still emits the inherited `System.ValueTuple` version-conflict warning.

## Working rules

Working code must be committed at least daily in small focused commits with descriptive messages. Experimental claims, including null results, must have matching source/evidence artifacts in the repository before they are treated as project progress. Snapshot mega-commits and chat-only progress are not acceptable project state.

Do not interrupt an active experiment or implementation merely to check for repository updates. At a natural mission checkpoint—completed milestone, committed null result, or committed blocker—sync current `main`, re-read the project law/README/current roadmap guidance, reconcile newer commits or user direction, and only then begin the next major milestone. GitHub is authoritative at mission boundaries.
