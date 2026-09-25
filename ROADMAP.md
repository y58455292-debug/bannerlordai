# BannerlordAI Roadmap

## North star

BannerlordAI should create a living Bannerlord world that can remain coherent for **years of campaign time and across generations**.

The mod should structure what actors are capable of, what they remember, what responsibilities they have, and what consequences follow from world events. It should **not script the campaign plot**.

Kingdoms may rise, fall, reform, split, merge, rename, or be replaced by successor powers. Dynasties and leaders should change. **Culture remains a deeper persistent identity** rather than being renamed every time political ownership changes.

The player should be able to see the same world logic acting on NPCs and on themselves. Whenever practical, systems should avoid hidden AI-only cheats and should make major victories, defeats, security failures, recruitment shortages, political betrayals, and local protection matter over time.

## Roadmap operating rule

This roadmap is ordered.

At a mission boundary, sync current `main`, read this file, and continue with the highest-priority unfinished milestone that is not explicitly blocked.

This is **not an approval gate**. Do not wait for user permission at every checkpoint when the next milestone is already defined.

Evidence and instrumentation exist to prove gameplay behavior. They are not the final product.

A feature is not considered complete merely because a score changed or code compiled. The intended endpoint is an observable in-world behavior that can occur during a normal campaign.

---

# Phase 0 — Current proven foundation

Preserve and do not regress the systems already proven or substantially established in the repository:

- autonomous world-scope lord behavior;
- Home Responsibility;
- companion duty / experience / negative-outcome memory;
- Holdback;
- WarState / WarScar tracking;
- Kingdom Orders;
- War Strain recruitment/economy coupling;
- Prisoner/Mercy with release threshold 20;
- clan social memory;
- voluntary leave-current-kingdom behavior;
- player-facing visibility for loyalty events and war strain.

Current known unresolved political boundary:

- target-kingdom defection/join remains unproven;
- the first Phase 2 characterization found a score-scale mismatch and sparse memory/target alignment;
- do not inflate the 25,000 social-defection cap merely to force a positive result.

---

# Phase 1 — Demo-first integration gate

## Goal

Reach a coherent, stable build that the user can play as a **full integrated demo** before spending time debugging every remaining research gap.

The demo should show the strongest proven systems working together in a normal campaign.

**Status — PASSED 2026-09-24.** The v0.21M integrated candidate passed build/invariant checks, loaded the protected Syronea baseline after proper main-menu readiness, advanced 100.987 campaign hours without a crash, emitted proven player-visible state, created and reloaded a separate guarded demo-gate save, restored persisted ClanAI systems, and advanced again after reload. See `Reports/Demo/PHASE1_DEMO_GATE_RESULT.md`. The initial premature `LOAD_SAVE` NullReference was traced to a development-runner sequencing error (issuing the command before `GauntletInitialScreen` existed), not a ClanAI campaign-load failure.

## Demo blockers

Fix only issues that prevent:

- loading a campaign reliably;
- normal time progression;
- saving/loading without corrupting protected state;
- major proven ClanAI behaviors from running;
- player-visible messages from functioning;
- ordinary battles, sieges, recruitment, diplomacy, and settlement interaction;
- a reasonable continuous play session without a reproducible crash.

## Not required to block the first integrated demo

These may remain clearly documented research gaps if they do not destabilize the build:

- a naturally committed target-kingdom defection;
- every WarScar subtype being naturally proven;
- perfect balance of every modifier;
- complete NPC parity for every vanilla feature;
- long-horizon generational systems;
- final manpower/bandit ecology;
- deep provider/external reasoning integration.

Do not manufacture a proof just to unblock the demo.

## Demo acceptance

Before calling the first integrated demo candidate ready:

1. build succeeds;
2. repository invariant tests pass;
3. current proven systems initialize;
4. a protected baseline loads;
5. campaign time can advance;
6. no immediate repeatable crash appears during ordinary play;
7. player-visible ClanAI state is present where already proven;
8. rollback exists;
9. known incomplete systems are documented honestly.

After this gate, return to the ordered roadmap and deeper debugging.

---

# Phase 2 — Political agency and kingdom evolution

## 2A. Finish target-kingdom defection

**Status — TIMEBOXED / UNPROVEN 2026-09-24.** One evidence-backed M3 implementation attempt carried the already-proven leave-memory modifier into Bannerlord's embedded native leave component while keeping target-specific memory under the existing 25,000 cap. A bounded natural run produced 27 `ConsiderDefection` samples, four target-memory modifiers, zero non-zero leave-memory carries, zero adjusted YES decisions, and zero commits. Closest observed candidate was Vezhoving → Nord at -180,954 with affordability satisfied. Per the Phase 2A timebox, preserve this blocker/null and do not continue instrumenting or tuning the surface now. Target-kingdom autonomous defection remains unproven. **Next milestone: 2B ruler recruitment / clan courtship.**

Objective:

A clan can naturally decide that its current kingdom is no longer preferable, evaluate a specific target kingdom, and actually join that kingdom through Bannerlord's native action path.

Required shape:

- natural target selection;
- real remembered causes;
- native NO;
- bounded ClanAI contribution;
- adjusted YES;
- affordability satisfied;
- native commit;
- post-state confirms membership in the target kingdom.

Current work should first explain the observed score-scale mismatch. Any stronger influence must come from real world events with an explainable native-value scale where possible.

## 2B. Ruler recruitment / clan courtship

**Status — PROVEN 2026-09-24.** The first native-only ruler-courtship candidate selected naturally independent Banu Ruwaid for Vlandia by native kingdom desirability. Native clan value +649 and kingdom value +7,401 produced +8,050 combined surplus; ClanAI invoked only `ExecuteAiBarter`; and post-state confirmed membership in Vlandia. See `Reports/ClanRecruitment/PHASE2B_RUNTIME_RESULT.md`. **Next milestone: 2C independent clans as real political actors.**

Rulers should actively identify and attempt to recruit useful clans.

Potential considerations:

- military strength;
- clan tier and parties;
- holdings;
- geography;
- culture;
- relationship history;
- grievances with current ruler;
- prior cooperation;
- kingdom war strain;
- border need;
- available wealth / affordability;
- whether the clan is independent.

The ruler making an offer does not guarantee acceptance.

Clans retain their own evaluation.

## 2C. Independent clans as real political actors

**Status — PROVEN AT NATIVE BOUNDARY 2026-09-24.** Native code confirms that non-eliminated independent NPC clans retain daily finance and lord-party spawning/rebuilding. In a bounded 81.003-hour run, independent Banu Ruwaid retained two active parties, used settlements, fought bandits, and grew from 195 to 205 combined troops without custom spawning or recruitment. Existing evidence also proves evaluation by multiple kingdoms, native rejection, and later positive native recruitment. Ordinary independent NPC clans are explicitly excluded from native raid/siege/defense target scoring, and no ordinary NPC kingdom-creation loop was identified; those gaps were preserved rather than bypassed. See `Reports/IndependentClans/PHASE2C_RUNTIME_RESULT.md`. **Next milestone: 2D kingdom continuity and successor states.**

A clan that leaves a kingdom should be able to:

- remain independent;
- rebuild;
- hold or seek territory where native systems allow;
- receive offers from multiple kingdoms;
- join later for believable reasons;
- potentially become part of a new political entity where Bannerlord mechanics permit.

## 2D. Kingdom continuity and successor states

**Status — 2D-L1 SAVE/RELOAD CONTINUITY PROVEN 2026-09-25.** Preserve native ruler elections and terminal kingdom destruction. Treat matured, naturally landed rebel clans as provenance for possible later successor work, not as automatic kingdoms. Preserve the 28-day landless-independent destruction boundary, native ownership, wars, membership, and culture. The guarded L1 test save materialized with `exists=True`; a fresh direct configured-module reload restored seven serialized records (`found=True`), reconciled seven active kingdoms into seven records, emitted zero succession/destruction callbacks during load reconciliation, and reported `mutation=False`. The protected demo-gate fixture was not overwritten. Natural lifecycle-event observation remains a separate honest null. **Next ordered work: Phase 3 Home Responsibility deterministic eligibility/factor tests.** See `Reports/KingdomContinuity/PHASE2D_L1_RUNTIME_RESULT.md`, `Reports/KingdomContinuity/evidence/phase2d_l1_reload_20260925.txt`, and `Reports/AgentStatus/CODEX_STATUS.md`.

Long-horizon political identity should support:

- rulers dying;
- new generations inheriting leadership;
- clans rising and falling;
- kingdoms changing rulers;
- kingdoms changing names where appropriate;
- successor states emerging;
- political identity changing without automatically rewriting deep culture.

**Culture is persistent. Political names and dynasties may change.**

Do not predetermine which kingdom survives.

---

# Phase 3 — Territorial responsibility, defense, and ruler strategy

## Goal

Make territory feel owned, defended, neglected, or contested based on actual noble behavior.

**Deterministic seam — PASSED 2026-09-25.** Existing Home Responsibility remains bounded to Bannerlord-provided, positive-scoring candidates at clan-owned settlements for eligible independent lords at war. Its ownership/score/factor selection is now isolated in a pure `HomeResponsibilityPolicy`; ten deterministic cases cover actor eligibility, foreign ownership, non-positive scores, threat precedence, weak recovery, defense, patrol, ordinary home movement, and unrelated behavior. A wiring invariant preserves the native candidate list and post-vanilla behavior/target commit check. Release build: 0 errors with the inherited `System.ValueTuple` warning. No DLL was deployed and this checkpoint makes no new runtime claim. See `Reports/TerritorialResponsibility/PHASE3_DETERMINISTIC_TEST_RESULT.md`.

**Kingdom Objective deterministic seam — PASSED 2026-09-25.** The existing `KingdomObjectiveLayer` now delegates only its pure refusal/factor/winner-margin rules to `KingdomObjectivePolicy`. The runtime still uses Bannerlord-provided candidates, native settlement targets, `StrategicDecisionComposer`, final composer winner selection, and the existing post-vanilla behavior/target commit check. Thirty-four deterministic cases cover low readiness/food, urgent home-threat refusal, direct/staging handling, factor tables, the 0.45 competitive threshold, 1.025 winner margin, 2.50/2.25 bounds, and pending-winner conditions. Wiring invariant and Release build pass with 0 errors. No DLL was deployed and no new runtime behavior is claimed. **Next milestone: one bounded runtime proof that a natural ruler objective changes a native candidate winner and Bannerlord commits the selected behavior/target.** See `Reports/TerritorialResponsibility/PHASE3_KINGDOM_OBJECTIVE_DETERMINISTIC_TEST_RESULT.md`.

**Kingdom Objective bounded runtime proof — PASSED 2026-09-25.** On the unchanged deterministic candidate, Battania ruler Rath's natural `BorderSecurity / CaptureSpecificSettlement / Uthelaim Castle` objective was evaluated against Bannerlord-provided native candidates. For Culharn, the native winner was `RaidSettlement:Stathymos` at score 3.756; the existing staging candidate `GoToSettlement:Pendraic Castle` was competitive at ratio 0.981 and the unchanged 1.55 staging factor changed the composer winner. Bannerlord then committed `GoToSettlement` to Pendraic Castle, and the existing post-vanilla verifier recorded `matched=True`. The run stopped after 28.224 campaign hours inside a 72-hour bound, used no synthetic target/action or mutation, exited without saving, and preserved the protected fixture hash. See `Reports/TerritorialResponsibility/PHASE3_KINGDOM_OBJECTIVE_RUNTIME_RESULT.md`.

**Visual War deterministic / standalone seam — PASSED 2026-09-25.** The existing defensive-strategy layer now delegates only its current weak/exclusion and factor rules to pure `VisualWarPolicy`: active settlement defense, frontier defense, frontier offense, and rear-security/bandit response retain their current thresholds and factors. Thirty-two deterministic checks cover the required weak, defense, offense, rear-security, party-size, non-bandit, non-positive-score, unrelated-behavior, factor and cap boundaries. A wiring guard preserves Bannerlord-native candidates/targets, runtime world-context classification, and `StrategicDecisionComposer` score ownership. The former absolute `D:\BannerlordAIResearch\Data\ENABLE_VISUAL_WAR_LAB.txt` switch is now an optional module-local `ClanAI/Data/ENABLE_VISUAL_WAR_LAB.txt` marker; missing marker remains OFF, and no marker is added by default. Standalone-path guard and Release build pass with 0 errors. No DLL was deployed and this checkpoint makes no runtime claim. **Next milestone: one bounded runtime proof that an existing Visual War defensive/security contribution changes a Bannerlord-native candidate winner and Bannerlord commits the selected native behavior/target.** See `Reports/TerritorialResponsibility/PHASE3_VISUAL_WAR_DETERMINISTIC_TEST_RESULT.md`.

**Visual War observation-only commit verifier — PASSED OFFLINE 2026-09-25.** The existing scoring path is unchanged. A pending expectation is now created only when Visual War changes the current composer winner, storing only actor/party identity, expected native behavior/target, reason, and creation campaign hour. On a later normal AI tick the layer reads Bannerlord's `DefaultBehavior`, `ShortTermBehavior`, native settlement/mobile targets and settlement arrival state, logs `VISUAL_WAR_COMMIT_CHECK`, and removes the record on match or after an 18-hour expiry. Twenty-two pure verifier checks, observation-only wiring and no-mutation invariants, the prior Visual War policy/wiring/standalone guards, and Release build all pass. No DLL was deployed and no runtime success is claimed. **Next milestone: one bounded runtime proof ending in `VISUAL_WAR_COMMIT_CHECK matched=True`.** See `Reports/TerritorialResponsibility/PHASE3_VISUAL_WAR_COMMIT_VERIFIER_RESULT.md`.

**Visual War bounded runtime proof — PASSED 2026-09-25.** On the unchanged candidate, Visual War was enabled only through the module-local optional marker. Arthamund naturally produced a `frontier-defense` winner change from `PatrolAroundPoint:Sibir` to existing native candidate `PatrolAroundPoint:Goleryn`; the final composed blackboard retained Goleryn. A later normal AI observation recorded `VISUAL_WAR_COMMIT_CHECK` with `actualDefault=PatrolAroundPoint`, `actualTarget=Goleryn`, `behaviorMatch=True`, `targetMatch=True`, and `matched=True`. The run stopped after 29.915 campaign hours inside a 72-hour bound, used no tuning/synthetic target/direct order/mutation, exited without saving, preserved the protected fixture, and removed the marker so Visual War is OFF by default again. See `Reports/TerritorialResponsibility/PHASE3_VISUAL_WAR_RUNTIME_RESULT.md`.

**Strategic Commitment deterministic / standalone seam — PASSED 2026-09-25.** The existing commitment layer now delegates only its current retention eligibility/math to pure `StrategicCommitmentPolicy`: factor remains 1.10, maximum age remains 12 campaign hours, same-state and existing-positive-previous-candidate requirements remain unchanged, cross-state changes and expired objectives pass through, and retention requires `previousScore * 1.10 > naturalScore` strictly. Observe remains observation-only; Apply can rescale only the existing previous native candidate through `StrategicDecisionComposer`. Thirty-four policy checks, thirteen config/path checks, runtime-wiring/no-mutation/standalone-path invariants, and Release build pass with 0 errors. The former absolute `D:\BannerlordAIResearch\Data\StrategicCommitment.cfg` path now resolves to module-local `ClanAI/Data/StrategicCommitment.cfg`; missing/invalid/tracked config remains Observe and Apply requires explicit `Mode=Apply`. No DLL was deployed and no runtime success is claimed. **Next milestone: one bounded runtime proof that unchanged same-state retention makes the previous existing native candidate the composer winner and Bannerlord commits its native behavior/target.** See `Reports/TerritorialResponsibility/PHASE3_STRATEGIC_COMMITMENT_DETERMINISTIC_TEST_RESULT.md`.

**Strategic Commitment observation-only commit verifier — PASSED OFFLINE 2026-09-25.** The existing retention logic is unchanged. Pending verifier state is created only after explicit Apply, an actual factor application to the existing previous native candidate, recomputation of the final composer winner, and `retained=True`. It stores only actor/party identity, expected native behavior/target, previous objective label/signature, and creation campaign hour. A later layer evaluation reads only Bannerlord native behavior/target state and logs `STRATEGIC_COMMITMENT_COMMIT_CHECK`; pending state is removed on match or after an 18-campaign-hour verifier lifetime, which is separate from the unchanged 12-hour retention-eligibility age. Twenty pure verifier checks, creation/wiring and observation-only invariants, all prior Strategic Commitment policy/config/wiring/no-mutation/standalone tests, and Release build pass with 0 errors. Tracked config remains `Mode=Observe`; no DLL was deployed and no runtime success is claimed. **Next milestone: one bounded temporary-`Mode=Apply` runtime proof ending in `STRATEGIC_COMMITMENT_COMMIT_CHECK matched=True`, then return config to Observe.** See `Reports/TerritorialResponsibility/PHASE3_STRATEGIC_COMMITMENT_COMMIT_VERIFIER_RESULT.md`.

**Strategic Commitment bounded runtime proof — NULL 2026-09-25.** The unchanged candidate was deployed with verified rollback and only the installed module-local `StrategicCommitment.cfg` temporarily set to explicit `Mode=Apply`; fresh reset confirmed `configured_apply factor=1.1 maxAgeHours=12`. The strict 72-campaign-hour qualifying window produced zero `STRATEGIC_COMMITMENT_APPLY` and zero `STRATEGIC_COMMITMENT_COMMIT_CHECK` records, so no retained native winner or native commit was proven. The first status poll crossed the bound by 0.198 campaign hours; TestRunner pause latency allowed extra non-qualifying time afterward, which is excluded from the proof window and also contained no Apply/commit event. Per protocol, factor 1.10, 12-hour eligibility, coarse-state classification, candidate scores, and scenarios were not changed. The run exited without saving, installed config was restored to `Mode=Observe`, tracked config remained Observe, Visual War stayed OFF, protected fixture remained unchanged, and rollback was preserved. Preserve this null rather than tuning the system to force success. See `Reports/TerritorialResponsibility/PHASE3_STRATEGIC_COMMITMENT_RUNTIME_RESULT.md`.

Rulers and nobles should not all behave as if joining the largest offensive army is always the correct choice.

Possible strategic roles:

- campaign army;
- border defense;
- settlement defense;
- garrison support;
- local patrol;
- reserve / rebuilding;
- raiding / disruption;
- escort / route protection;
- home-territory response.

Factors should include:

- ownership;
- threatened holdings;
- border exposure;
- war objectives;
- party strength;
- garrison condition;
- local security;
- clan interests;
- ruler orders;
- personality/history;
- loyalty to ruler;
- previous losses.

A lord staying near a frontier castle may therefore be performing useful strategic work rather than behaving passively.

---

# Phase 4 — Manpower and troop-quality ecology

## Goal

Make casualties strategically meaningful without making the world unable to recover.

Avoid a simple global scarcity slider as the core solution.

Recruit availability should emerge locally from world conditions.

## 4A. Prove vanilla AI recovery behavior first

**Phase 4A recovery observer — OFFLINE READY 2026-09-25.** A minimal observation-only campaign behavior now tracks naturally created or severely depleted NPC lord parties (including post-battle subjects), logs roster health/tier/limit/food and settlement chronology, snapshots notable volunteer pools and garrisons before/after visits where available, and classifies growth conservatively as recruitment-supported, garrison-withdrawal-supported, mixed, other/unknown, or unknown. Troop growth alone never proves a source. Twenty deterministic classification checks, native-event wiring, no-mutation/standalone invariants, and Release build pass with 0 errors. No recruitment/manpower behavior changed. **Next: one natural characterization capped at 168 campaign hours; record either a supported recovery chain or an honest null.** See `Reports/Manpower/PHASE4A_RECOVERY_OBSERVER_OFFLINE_RESULT.md`.

**Phase 4A first natural recovery characterization — PASSED 2026-09-25.** The observer ran for 39.623 campaign hours inside a 168-hour cap. Primary subject Kulyat of the Forest People began severely depleted at 37/133 troops and still had 37 before the first observed settlement visit. Sibir produced +7 while volunteers fell 20->13 and garrison stayed 423; Kvol produced +9 while volunteers fell 18->9; a later Sibir visit produced +4 while garrison stayed 424; Radakmed produced +8 while volunteers fell 18->10. These +28 troops are recruitment-supported. Four separate outside-settlement +1 gains remain `OtherOrUnknownNativeSource`, bringing Kulyat to 69. The same run observed native created lord parties with troops before settlement interaction, defeated-side parties with wounded rosters, and Iara gaining +7 while Ab Comer Castle garrison fell 219->212 with volunteers unchanged, supporting garrison withdrawal. No source is inferred from headcount alone, and no gameplay mutation occurred. **Stop at this characterization checkpoint; do not implement Phase 4B from this single sample.** See `Reports/Manpower/PHASE4A_RECOVERY_CHARACTERIZATION_RESULT.md`.

**Phase 4A corrected same-hero recreation characterization — PASSED 2026-09-25.** The corrected early-participant identity seam captured a complete natural Megenhelda chain. Her defeat at campaign hour 649520.378 was accepted from the exact same-battle participant captured at `MapEventStarted` after the live end-time leader reference had disappeared; the old native party was destroyed at that hour. A distinct native party for the same hero StringId was created 83.533 hours later outside settlement with 22 total members / 21 regular troops and classification `PostDefeatNativeRecreationInitialTroopsSupported`. At the first pre-entry boundary, Pravend, the roster was still 22 with zero observed pre-settlement headcount growth and `completeChain=True`. The run stopped after 122.234 campaign hours, exited without saving, and preserved the protected fixture. This proves one natural native post-defeat recreation can begin with regular troops before first settlement interaction; it does not establish a universal amount or formula. **Stop at this Phase 4A checkpoint; Phase 4B is not started.** See `Reports/Manpower/PHASE4A_CORRECTED_LINKED_RECREATION_RUNTIME_RESULT.md`.

Before changing recruitment, use observation-only evidence to determine how AI lords actually rebuild on the supported Bannerlord version.

Measure:

- party roster immediately after respawn;
- troops present before the first settlement visit;
- settlement visits;
- recruit-pool changes;
- garrison withdrawals;
- party-size growth;
- troop-tier composition;
- elapsed campaign and wall time;
- recovery after major battle losses.

Distinguish:

- free respawn troops;
- settlement recruits;
- garrison transfers;
- other native recovery sources.

## 4B. Local manpower

**Phase 4B local-manpower native-capability/design audit — PASSED OFFLINE 2026-09-25.** Native daily notable volunteer regeneration is controlled through the selected `VolunteerModel`; player recruitment, ordinary AI lord recruitment, and garrison auto recruitment consume the same six notable `VolunteerTypes` slots. The selected v1 boundary is a delegating `VolunteerModel` that adjusts only empty-slot `GetDailyVolunteerProductionProbability`: native occupied-slot upgrading, culture/troop type, relation gates, costs, AI/player decisions and all actual mutations remain authoritative. The first candidate uses native town prosperity/village hearth level, local/bound security and active raid/siege, clamped to 0.35..1.00 so it can only slow vanilla refill, never boost or stop it. Direct loyalty/militia/garrison/history/pressure inputs and War Strain are deferred; the latter already has an active AI-only recruitment throttle and must not be double-applied. No gameplay code, runtime run or deployment occurred. **Next bounded milestone: offline-only pure policy + delegating VolunteerModel wrapper + deterministic/no-mutation/standalone tests; do not deploy until that passes.** See `Reports/Manpower/PHASE4B_LOCAL_MANPOWER_NATIVE_CAPABILITY_AUDIT.md`.

**Phase 4B Local Manpower v1 offline implementation — PASSED 2026-09-25.** The pure probability policy and selected-model `VolunteerModel` wrapper are implemented. Occupied slots and unsupported contexts preserve normal native probability; only empty slots receive the bounded population/security/acute multiplier. All non-production model members delegate unchanged, inner production is called exactly once, no RNG/direct volunteer/roster/world mutation is added, and War Strain remains unchanged/excluded. Policy checks=50, delegation checks=13, wiring/no-mutation/standalone invariants pass, all Phase 4A tests remain green, relevant territorial invariants pass, and Release builds with 0 errors plus the inherited `System.ValueTuple` warning. The built DLL was not deployed and no campaign was launched, so this is **implemented + build-proven, not runtime-observed**. **Next milestone: separately authorized bounded runtime characterization of this exact model; do not tune or start Phase 4C before that evidence.** See `Reports/Manpower/PHASE4B_LOCAL_MANPOWER_V1_OFFLINE_IMPLEMENTATION_RESULT.md`.

**Phase 4B Local Manpower v1 bounded runtime characterization — PASSED 2026-09-25.** Runtime selected `LocalManpowerVolunteerModel` around native `DefaultVolunteerModel`. Vinela supplied both required passthrough examples: an occupied slot remained native 0.525 -> 0.525, and a High-hearth empty slot with security 99.16 remained native 0.08823674 -> 0.08823674. Natural Mid-hearth Lysia exercised the actual Local Manpower effect without any manufactured degradation: native 0.525 * population 0.95 * security 1 * acute 1 = final 0.498749971. The native daily volunteer update then transitioned Sanion's observed empty slot to `imperial_vigla_recruit` (tier 2, Empire culture) with `mutationByClanAI=False`, demonstrating native mutation authority. Required proof completed after 0.180 campaign hours; pause acknowledgement was at 0.415 hours, far inside the 168-hour bound. No telemetry errors or save commands occurred and the protected fixture was unchanged. Low-population, sub-50-security, acute raid/siege and shared AI/garrison consumption were not runtime-exercised, and balance is not proven. **Stop at this Phase 4B checkpoint; do not start Phase 4C in the same milestone.** See `Reports/Manpower/PHASE4B_LOCAL_MANPOWER_V1_RUNTIME_RESULT.md`.

Recruit availability should reflect conditions such as:

- settlement type;
- prosperity;
- hearth/population proxies available in native systems;
- security;
- recent raids;
- recent battles;
- war duration;
- recruitment pressure;
- local garrison/militia;
- military development;
- peace duration;
- cultural troop sources.

Apply the same underlying ecology to player and AI where practical.

## 4C. Troop tiers matter

**Phase 4C troop-quality native-capability/design audit — PASSED OFFLINE 2026-09-25.** Native occupied volunteer quality uses the same `VolunteerModel.GetDailyVolunteerProductionProbability` first gate as refill, then independently requires `UpgradeTargets`, current tier below native `MaxVolunteerTier=4`, a second `log2(notable.Power/currentTier)*0.01` roll, and native direct target selection. Volunteer reordering also moves higher-quality troops toward later, slower native slots. Therefore the smallest v1 seam is **not** a new wrapper and not direct mutation: later extend the existing `LocalManpowerVolunteerModel` so empty slots keep Phase 4B exactly, non-upgradeable occupied slots pass through, and only occupied upgrade-eligible slots receive a pure quality first-gate multiplier. Notable power, tier, culture/tree, RNG and mutation remain native. Candidate local components remain High/Mid/Low 1.00/0.95/0.80, security 0.80..1.00, active raid/siege 0.50, but quality uses a separate candidate floor `0.50` instead of Phase 4B's 0.35 because native quality already has two additional scarcity gates. This is a safety bound, not balance proof. Party XP/upgrades, garrison training/transfers, prisoners, mercenaries and post-defeat recreation remain separate veteran-recovery paths. **Next bounded milestone: offline-only pure quality policy + occupied wrapper branch + deterministic/no-mutation/standalone tests; no runtime or deployment until that passes.** See `Reports/Manpower/PHASE4C_TROOP_QUALITY_NATIVE_CAPABILITY_AUDIT.md`.

**Phase 4C Troop Quality v1 offline implementation — PASSED 2026-09-25.** The audited seam is implemented in the existing single `LocalManpowerVolunteerModel`. Empty slots still use the unchanged Phase 4B policy; occupied slots without native upgrade eligibility pass through; occupied slots with direct `UpgradeTargets` and current tier below the selected inner `MaxVolunteerTier` use the pure Phase 4C first-gate policy. Quality reuses the audited High/Mid/Low and security/acute components but has its own 0.50 floor; Phase 4B's 0.35 floor and pure policy blob remain unchanged. Bannerlord's notable-power/current-tier second gate, direct target choice, RNG, culture/tree and actual mutation remain native. Phase 4C policy checks=69, compiled wrapper branch/delegation checks=23, no-mutation/standalone/native-eligibility invariants pass, all Phase 4B/4A tests remain green, relevant territorial invariants pass, and Release builds with 0 errors plus the inherited `System.ValueTuple` warning. The candidate DLL was not deployed and no campaign was launched, so this is **implemented + build-proven, not runtime-observed; balance not proven**. **Next milestone: separately authorized bounded Phase 4C runtime characterization of this exact candidate; do not begin Phase 5 first.** See `Reports/Manpower/PHASE4C_TROOP_QUALITY_V1_OFFLINE_IMPLEMENTATION_RESULT.md`.

**Phase 4C Troop Quality v1 bounded runtime characterization — STRONG PASS 2026-09-25.** Runtime selected the single `LocalManpowerVolunteerModel` around native `DefaultVolunteerModel`. Phase 4B empty behavior remained intact. A healthy native-upgrade-eligible Vinela `imperial_recruit` used quality multiplier 1 and preserved native 0.525; natural Mid-hearth Lysia reduced an upgrade-eligible `imperial_vigla_recruit` first gate from 0.367499977 to 0.349124968 via quality multiplier 0.95; and a tier-4 Tarcutis `imperial_heavy_horseman` at native max tier 4 bypassed Phase 4C with exact native passthrough. Bannerlord then naturally upgraded a Dradios `imperial_archer` tier 2 to its direct native `imperial_trained_archer` tier 3 target, with `directUpgradeTarget=True` and `mutationByClanAI=False`. Minimum proof completed after 0.775 campaign hours; pause acknowledgement was at 1.072 hours. Low-population, sub-50-security, acute raid/siege and the 0.50 quality floor remain deterministic/build-proven rather than runtime-exercised. **Phase 4C is implemented + build-proven + runtime-observed; balance is not proven. Stop here; do not begin Phase 5 in this checkpoint.** See `Reports/Manpower/PHASE4C_TROOP_QUALITY_V1_RUNTIME_RESULT.md`.

Rebuilding numbers should be easier than rebuilding veteran quality.

Conceptual sources:

- villages: common levies and basic local manpower;
- towns: broader urban manpower, militia-oriented recruits, specialists where appropriate;
- castles: stronger connection to trained military manpower, garrison systems, noble/elite military sources.

High-tier troops should be rarer and should depend on believable sources rather than rapidly reappearing everywhere.

A major defeat should be able to create:

- temporary manpower shortage;
- lower replacement quality;
- depleted garrisons if troops are withdrawn;
- a period of rebuilding;
- reduced offensive capability;
- stronger incentives to defend and recover.

Recovery must remain possible over time in healthy, secure, prosperous territory.

---

# Phase 5 — Security, patrols, banditry, and local control

**Phase 5 native-capability/design audit — PASSED OFFLINE 2026-09-25.** Native bandit pressure is not one loop: looters refill against a global support limit, culture bandits refill against infested-hideout population, hideouts re-infest under their own caps, and native Guard House patrols/lord initiative provide real suppression. The smallest v1 seam is the existing private local score `BanditSpawnCampaignBehavior.GetSpawnChanceInSettlement(Settlement)`, applied only to normal town/village candidates used by ambient looter spawn selection; hideout calls remain exact native passthrough. Use native Security as the only first local-control input (town own Security; village bound-town Security; missing context passthrough) because native Security already aggregates garrison, looted villages, siege, nearby infested hideouts, patrol-party bonuses and other order effects. Candidate factor: `clamp(1.25 - 0.005*Security,0.75,1.25)`, neutral at Security 50. This changes only relative looter location weight, not total looter cap/refill, templates, creation, movement or destruction. Home Responsibility and Visual War remain independent and unchanged. See `Reports/Security/PHASE5_SECURITY_BANDITRY_NATIVE_CAPABILITY_AUDIT.md`.

**Phase 5-v1 Local Bandit Control offline implementation — PASSED 2026-09-25.** `LocalBanditControlPolicy` implements the audited Security-only 0.75..1.25 relative weight factor, and a single Harmony postfix modifies only the returned result of private `BanditSpawnCampaignBehavior.GetSpawnChanceInSettlement(Settlement)`. Unsupported candidates—including hideouts/castles—and town/village candidates without usable Security return before policy application and preserve the native result exactly. The supported private method resolves against the audited `TaleWorlds.CampaignSystem.dll`; 24 deterministic policy checks, 14 compiled target/signature checks, postfix/wiring/no-mutation/standalone guards, all retained Phase 4 tests, and relevant Home Responsibility/Visual War/Strategic Commitment validations pass. Release builds with 0 errors plus the inherited `System.ValueTuple` warning. See `Reports/Security/PHASE5_LOCAL_BANDIT_CONTROL_V1_OFFLINE_IMPLEMENTATION_RESULT.md`.

**Phase 5-v1 Local Bandit Control bounded runtime characterization — STRONG PASS 2026-09-25.** Observation-only telemetry preserved the Phase 5 gameplay policy byte-for-byte and was checkpointed before deployment as `08e82ac8cf0f3fc126129fbc77206fb8733ba636`; runtime DLL SHA-256 was `E4DB15DAF5C437F45F559CADDD3F385CE062416D2896EE451826D961D9DDDF7A`. Minimum proof completed naturally after 19.527 campaign hours: Syronea Security 100 changed native weight 1 -> 0.75, Tepes village resolved bound-castle Security 43.85594 and changed native weight 1 -> 1.03072023, and village-bound Security was directly exercised. Bannerlord naturally created looter party `looters_50217`, while Phase 5 only observed the native creation event; a hideout invocation preserved native 0.111111112 exactly with `applied=False`. A near-neutral Security 50.7775078 sample also produced the expected 0.996112466 multiplier. There were zero telemetry errors and zero save commands; the run exited with `EXIT_NOSAVE`, protected fixture hash/timestamp stayed unchanged, Strategic Commitment remained Observe, Visual War remained OFF, and rollback remained intact. Phase 5-v1 is now **implemented + build-proven + runtime-observed; native looter creation observed: YES; hideout passthrough runtime-observed: YES; balance-proven: NO**. Stop at this checkpoint; do not retune Phase 5 or begin Phase 6 in the same task. See `Reports/Security/PHASE5_LOCAL_BANDIT_CONTROL_V1_RUNTIME_RESULT.md`.

## Goal

Bandit pressure should increasingly reflect weak control rather than feeling like an unlimited disconnected spawn loop.

Investigate vanilla spawning and suppression first.

Desired relationship:

- low security;
- war devastation;
- repeated raids;
- weak patrol coverage;
- nearby hideout activity;
- disrupted trade / villages;
- absent nobles;

should increase local lawlessness pressure.

Conversely:

- active lord parties;
- patrols;
- strong garrisons;
- secure settlements;
- protected roads;
- sustained local presence;

should suppress it.

Small criminal groups can still exist in stable regions. Large recurring bandit pressure should increasingly indicate governance or security failure.

This system should connect directly to noble territorial responsibility.

---

# Phase 6 — Settlement society and vanilla capability audit

**Phase 6 Settlement Society / Vanilla Capability Audit — COMPLETE OFFLINE 2026-09-25.** The twelve requested domains were mapped from player and NPC perspectives using targeted source inspection of the supported `TaleWorlds.CampaignSystem.dll` and `SandBox.dll`; no campaign was launched and no gameplay source was changed. The deeper issues/notables/governor review found that NPC settlement visitors already resolve eligible issues opportunistically, issue effects already feed native settlement/notable models, NPC clans already assign governors, and workshops/caravans/alleys already have meaningful native lifecycle behavior. Three candidate seams were ranked. The selected first Phase 6-v1 target is a bounded selected-model decision seam: when an **NPC town is idle and below the selected native rebellious-state loyalty threshold**, prefer its existing native **Festival and Games** daily project at the normal `BuildingScoreCalculationModel.GetNextDailyBuilding(Town)` opportunity. The audited default daily selector is random; Bannerlord already owns the 1% consideration cadence, existing project identities/effects, construction queue, save state, and final `BuildingHelper.ChangeDefaultBuilding` commit. **Phase 6 gameplay implementation: NOT STARTED. Phase 7: NOT STARTED. Next milestone: separate offline-only implementation/validation of this exact seam; do not launch or deploy from this audit checkpoint.** See `Reports/SettlementSociety/PHASE6_SETTLEMENT_SOCIETY_CAPABILITY_AUDIT.md`.

**Phase 6-v1 Civic Project Choice offline implementation — PASSED 2026-09-25.** A pure `CivicProjectSelectionPolicy` now expresses only the audited NPC-town, idle-construction, Festival-available, loyalty-below-native-threshold decision. A delegating `CivicProjectBuildingScoreCalculationModel` calls the selected inner daily selector exactly once, preserves the exact native result by default, and can return only the town's existing `DefaultBuildingTypes.SettlementDailyFestivalAndGames` reference. The wrapper installs only around the exact audited `DefaultBuildingScoreCalculationModel`; foreign/unknown selectors are left untouched, and the rebellious-state threshold comes from the selected native loyalty model rather than a custom constant. Twenty-three policy checks, 14 compiled delegation checks, NPC-only/idle-town/no-mutation/standalone invariants, all retained Phase 4 and Phase 5 tests, relevant Phase 3 preservation checks, and Release build pass with 0 errors plus the inherited `System.ValueTuple` warning. Candidate DLL SHA-256: `2C2C89D5D1086CF119E7D439F08538F4552645A047738B32240144283CD12CE3`. Bannerlord was not launched and no DLL was deployed. Phase 6-v1 is **implemented + build-proven; runtime-observed: NO; native final commit observed: NO; player-legible: NOT YET PROVEN; balance-proven: NO**. **Next milestone: separately authorized bounded runtime characterization of this exact candidate.** See `Reports/SettlementSociety/PHASE6_CIVIC_PROJECT_V1_OFFLINE_IMPLEMENTATION_RESULT.md`.

**Phase 6-v1 Civic Project Choice bounded runtime characterization — BOUNDED NULL TARGET BRANCH 2026-09-25.** Observation-only telemetry preserved the Phase 6 policy byte-for-byte and was committed before deployment as `f0fe5b86aa71bd0df349d05858fc6ab203fcf30b`; runtime DLL SHA-256 was `2909ADE31B427E122FDDE539229BD0A4A2B4FA91C84CFD3AB63D0C8EB6F729D7`. The loaded module stack selected `CivicProjectBuildingScoreCalculationModel` around the exact audited `DefaultBuildingScoreCalculationModel`. Over 155.788 campaign hours, 12 civic evaluations occurred, including two NPC-town controls: active-construction Zeonica preserved native Festival and Games, and idle high-loyalty Marunath (94.79668 vs threshold 25) preserved native Train Militia. Bannerlord's own `BuildingHelper.ChangeDefaultBuilding` path committed the returned choices. No evaluated NPC town was below the native threshold, so genuine Festival substitution and a native commit after such a substitution remain unobserved. Zero saves/telemetry errors; `EXIT_NOSAVE` preserved the protected fixture, rollback, Observe mode and Visual War OFF. Phase 6-v1 is **implemented + build-proven + runtime-observed for integration/passthrough/native authority; genuine Festival substitution: NO; substituted-Festival native commit: NO; player-legible: NOT YET PROVEN; balance-proven: NO**. Preserve this bounded null; do not manufacture low loyalty or retune the seam. See `Reports/SettlementSociety/PHASE6_CIVIC_PROJECT_V1_RUNTIME_RESULT.md`.

## Goal

Audit Bannerlord from both perspectives:

1. what can the player do?
2. what can NPCs do?
3. where is NPC agency missing, shallow, or disconnected from consequences?

Cover not only kings and nobles, but also the people and systems encountered in:

- towns;
- villages;
- castles;
- notable households;
- gangs/crime;
- militia;
- garrisons;
- workshops/economy;
- trade;
- local issues;
- recruitment;
- prisoners;
- companions;
- local relationships.

For each meaningful vanilla capability, document:

- vanilla player capability;
- vanilla NPC capability;
- BannerlordAI difference;
- actor that initiates it;
- inputs/memory;
- possible in-world action;
- native authority retained;
- player-visible consequence;
- feature-complete proof.

Do not automatically add AI behavior to every menu action. Prioritize interactions that materially improve the living-world simulation.

---

# Phase 7 — Multi-year and generational continuity

## Goal

Prove the simulation remains coherent across long campaigns and new generations.

Long-run tests should examine:

- ruler death and succession;
- clan leadership succession;
- marriage/family continuity where native systems support it;
- inherited holdings and responsibilities;
- persistent or decaying social memory;
- whether children/new leaders inherit appropriate structural context without inheriting impossible personal memories;
- kingdom collapse and successor politics;
- changing kingdom names/political identities;
- stable culture identity;
- recruitment/manpower recovery across years;
- bandit/security equilibrium;
- economic recovery after war;
- whether one system creates runaway permanent collapse.

The campaign should be able to tell a different story each run.

No kingdom is protected because of plot.

No kingdom is selected to collapse because of plot.

---

# Phase 8 — Balance, release hardening, and presentation

Only after the major world loops work:

- tune magnitudes using accumulated evidence;
- remove obsolete experiment scaffolding;
- harden save compatibility;
- reduce unnecessary logging;
- verify performance;
- improve player-facing explanations/messages;
- document configuration;
- run long stability campaigns;
- prepare standalone release packaging.

External/provider reasoning should only be integrated if it improves the final game behavior without making the mod dependent on development infrastructure.

---

# Feature-completion standard

For major autonomous systems, use this ladder:

1. **Implemented** — code exists.
2. **Build-proven** — compiles/tests.
3. **Runtime-observed** — code executes in campaign.
4. **Causal** — real event demonstrably changes the intended native decision surface.
5. **Boundary-crossing** — ClanAI changes a native NO/YES decision where relevant.
6. **Committed in-world** — Bannerlord performs the actual action.
7. **Player-legible** — player can perceive the important result where appropriate.
8. **Long-run stable** — behavior survives normal save/load and extended campaign play.

Do not collapse these into a single "working" label.

---

# Design principles

- **Structure, not plot.**
- **Consequences, not arbitrary randomness.**
- **Native authority wherever practical.**
- **Player and NPC worlds should obey comparable rules where practical.**
- **Major victories and defeats should matter.**
- **Local conditions should matter.**
- **Troop quality should matter, not only troop count.**
- **Territory should create responsibility.**
- **Memory should influence future decisions without dictating them.**
- **Kingdoms and dynasties may change; culture persists.**
- **Evidence serves gameplay, not the other way around.**
- **The first integrated demo comes before perfection of every remaining research problem.**
