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
