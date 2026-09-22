# BannerlordAI Architecture & Agent-Learning Ledger

Purpose: preserve BOTH system architecture breakthroughs and improvements in how agents research, reason, test, hand off, and use tools.
This file is durable cross-agent inheritance. It complements the volatile current.json resume capsule.
Future agents must read this after recovery when starting substantial engineering work.

## Core inheritance rule
Every agent inherits two responsibilities:
1. Improve BannerlordAI.
2. Improve the process used to improve BannerlordAI.

A milestone is incomplete if a reusable architecture or workflow lesson was discovered but not preserved here, in a derived report, or in the Mem mirror.

## Architecture progress already established
- Preserve TaleWorlds native simulation where possible; modify judgment/context/memory rather than replacing movement/pathfinding/combat.
- LLM/advanced reasoning belongs at intent/judgment layers; deterministic/native code validates and executes.
- One canonical action/write owner per decision surface.
- v0.20M proved StrategicDecisionComposer as the single strategic SetBehaviorScore authority.
- Strategic factors enter as named contributions with source/reason attribution.
- v0.20N proved a compact per-actor blackboard can hold authoritative runtime facts with reuse and zero mismatch in shadow validation.
- Blackboard facts stay separate from memory, personality, emotion/appraisal, commitment, role/permissions, and planner-predicted state.
- Different timing boundaries may legitimately require direct native reads; do not force one cache across semantically different phases.
- Persistent memory can affect strategic reasoning, but memory influence must be bounded, observable, and proven causally before behavior ownership changes.
## Target architecture direction
WORLD / FACTION PRESSURES
-> AUTHORITATIVE GAME STATE ADAPTER
-> ACTOR PERCEPTION / BLACKBOARD
-> ACTOR-KNOWN / ACTOR-BELIEVED STATE
-> MEMORY + RELATIONSHIPS + PERSONALITY + GOALS
-> COARSE PERSISTENT STATE
-> HIGH-LEVEL INTENT
-> HARD ELIGIBILITY
-> UTILITY / BOUNDED PLANNING
-> CANONICAL ACTION / SCORE OWNER
-> TALEWORLDS NATIVE EXECUTION
-> OUTCOME
-> MEMORY / RELATIONSHIP / GOAL UPDATE

Use expensive cognition event-driven / AI-LOD style, not every tick.
Use planning only for rare high-value multi-step decisions.
Use classic game-AI structure and learned/LLM cognition together rather than replacing one with the other.

## Agent-method breakthroughs already established
- DeepSeek-style compression: active context must shrink as raw research grows.
- HOT/volatile state should be tiny; deeper evidence remains indexed and recoverable.
- Raw evidence is immutable; summaries never outrank raw proof.
- One hypothesis -> one intervention -> one measurable result.
- NATURAL != INDUCED != RESPONSE.
- Candidate score change != committed behavior.
- Reassertion != new decision.
- Performance is part of correctness.
- No-save integrity and save lineage are part of correctness.
- Prior art should solve generic agent/game-AI problems before Bannerlord-specific experimentation begins.
- If a blocker recurs twice, convert it into infrastructure: guard, preflight, adapter, cache, or automation.
- Automated tests should freeze evidence before clean native no-save exit.
- Do not rely on screenshots when an engine/runtime/log/API signal exists.
- Do not accumulate answered diagnostics in the hot path.
## Handoff-learning breakthrough — 2026-09-18
Failure observed: Mem/HOT could become stale while the actual desktop work advanced beyond them, and a chat could fill before a final handoff was written.

New rule:
- Never create the handoff only at chat end.
- Maintain a crash-safe rolling local resume capsule continuously.
- Mem stores durable roadmap/rules/architecture, not exact volatile runtime state.
- Local HandoffV3 + process/hash inspection is authoritative for live state.
- Checkpoint before/after every live test and whenever the next action changes materially.
- A replacement agent begins with recovery, not research/coding.

New tools:
- D:\BannerlordAIResearch\Tools\Handoff\resume_state.py
- D:\BannerlordAIResearch\Tools\Handoff\handoff_checkpoint.py
- D:\BannerlordAIResearch\Longitudinal\EvidenceIndex\HandoffV3\current.json
- D:\BannerlordAIResearch\Longitudinal\EvidenceIndex\HandoffV3\events.jsonl

## User-interaction / work-continuity rule
During an active turn, do not stop safe authorized work merely to wait for acknowledgment of progress updates.
Keep the user informed, but continue through routine reversible steps.
Never claim work continues after the response ends unless a real automation/process exists.
Never ask the user to repeat project history that can be recovered from Mem/HandoffV3/desktop evidence.

## Active-agent ownership language
Once an inherited process/test has been recovered and adopted, the current agent owns the workflow. Status updates should say what the agent is doing (for example: "I am running the calibration", "I am watching these counters", "I will stop/freeze evidence at this gate") rather than distancing language such as "the test is doing" or "the previous agent's run". Preserve provenance internally, but present a continuous first-person engineering workflow to the user.

## Improvement duty for every future agent
At meaningful milestones, ask:
- What became reusable?
- What repeated work can become a tool/script?
- What evidence can be compressed/indexed?
- What source or prior art eliminates future experimentation?
- What failure mode needs a preflight?
- What diagnostic can now be retired?
- What architecture rule became stable?
- What instruction did the user have to repeat that can become a durable rule?
## Promotion rule
Promote a finding into durable cross-agent architecture only when it:
- eliminates a meaningful branch of uncertainty,
- proves a reusable engine/API boundary,
- changes milestone order,
- establishes one-owner/write-authority semantics,
- creates reusable automation/control,
- produces a causal behavior result,
- prevents a recurring failure,
- or materially improves reasoning/retrieval/testing efficiency.

Do NOT promote:
- duplicate observations,
- anecdotes without causal proof,
- temporary debugging noise,
- stale build state,
- raw transcript volume.

## Required handoff contents
Every future handoff/checkpoint must preserve:
- exact volatile state in current.json,
- accepted/rejected milestone result,
- next action,
- blockers,
- do-not-repeat tests,
- newly proven architecture rules,
- newly proven method/tooling improvements,
- rollback/evidence paths,
- pending Mem synchronization if Mem is unavailable.

## Mem mirror
This ledger should be mirrored into the durable BannerlordAI Mem architecture/method notes.
If Mem is rate-limited or unavailable, local disk remains authoritative and the sync remains explicitly pending until a later agent succeeds.

## Evidence-before-workflow architecture rule — 2026-09-18
User standing direction: when asked to tune workflow/rules/automation/handoff for faster or more efficient progress, do not invent doctrine from preference alone. Search reliable prior art first. If credible support is absent, stop adoption and keep the idea experimental. If credible architecture exists, adapt the proven mechanism, state limits, validate locally, then promote.

Validated prior art for durable handoff:
- Temporal: durable workflow execution and resume after failure.
- LangGraph: thread checkpoints, cross-thread durable stores, pending-write preservation, explicit resume boundaries.
- Microsoft Event Sourcing pattern: append-only immutable history plus snapshots/materialized state for replay/recovery.
- Kubernetes Lease/leader election: single active control owner with explicit ownership identity and renewal/expiry semantics.

BannerlordAI adaptation:
- append-only transition journal + immutable snapshots + materialized current capsule;
- write-ahead event before replacing current state;
- hash/sequence chain to detect drift/corruption;
- stable operation/action IDs to prevent accidental repeated tests/deployments;
- PREPARED/ACTIVE/EVIDENCE_FROZEN/COMPLETED operation phases;
- runtime/process/hash inspection before retrying incomplete external side effects;
- one live deployment/runtime owner; future guard upgrade toward renewable lease semantics.

Why this matters:
The handoff should reproduce explicit engineering state and unfinished work, not merely summarize history. Hidden model reasoning cannot be replayed, but project state, evidence, decisions, operations, ownership, and exact next steps can be made durable enough that a replacement agent continues with minimal semantic discontinuity.

## Durable writer concurrency proof — 2026-09-18
The V4 journal was strengthened with a Windows cross-process writer lock plus optimistic expected-sequence checks.
Validation:
- retrying an already-recorded action_id at expected seq=9 returned idempotent replay and did not increment sequence;
- a deliberately stale writer using expected seq=8 was rejected before append with a stale-writer error;
- journal validation remained clean at seq=9 afterward.
Rule: state-critical handoff writes should pass the checkpoint sequence they read. A mismatch means recover/reconcile first; never overwrite newer state from a stale agent view.
This maps directly to event-sourcing concurrency requirements: ordered per-stream events and optimistic rejection of conflicting updates.

## Bottleneck Escalation Protocol — evidence-backed — 2026-09-18
Trigger: a task/problem is taking materially longer than expected, repeated attempts are not reducing uncertainty, or two attempts hit the same blocker.

Method:
1. STOP broad trial-and-error.
2. Define the exact blocked outcome.
3. Decompose it into the smallest unresolved key questions/fault domains.
4. Rank those questions by expected information gain / ability to eliminate branches.
5. Search reliable prior art specifically for those narrowed mechanisms, APIs, failure modes, or architectures.
6. If no credible evidence/mechanism is found for a branch, do not keep forcing it; mark UNKNOWN/UNSUPPORTED and choose a different path or stop.
7. If reliable proof exists, adapt the proven mechanism to BannerlordAI with the smallest testable implementation.
8. Run one discriminating test whose result changes the next engineering decision.
9. Preserve what was ruled out so later agents do not repeat the same search/test.

Evidence basis:
- Google SRE: scientific troubleshooting through hypotheses and ruling them out.
- NASA fault management: fault identification/isolation to a defined level of granularity before response/recovery.

Goal: convert time spent stuck into uncertainty reduction, narrower prior-art search, fewer launches/rebuilds, and faster accurate execution.

## Dynasty Save Benchmark Suite — evidence-backed — 2026-09-18
User has ~5 historical dynasty checkpoint saves from different campaign times, originally preserved to allow alternate story choices.
Adopt them as immutable longitudinal benchmark anchors, never as disposable test saves.

Two uses:
1. Historical forensic baseline: compare checkpoint-to-checkpoint world-state changes to reconstruct what vanilla actually produced over long natural play intervals, while explicitly separating player-caused events from AI-caused reactions.
2. Paired forward experiment: clone the SAME checkpoint into Vanilla-control and ClanAI-treatment copies, then observe equal campaign-time horizons and compare divergence.

Method rules:
- Originals are read-only/canonical evidence; create hashed test clones.
- Record in-game date, world/faction state, wars, fiefs, parties/armies, player allegiance/location, and mod/loadout for every anchor.
- Treat equal starting save as paired initial condition, not guaranteed deterministic replay.
- Divergence after treatment can desynchronize RNG/world events; compare distributions/trajectories, not frame-exact identity.
- Where possible repeat runs and/or use multiple checkpoint anchors so one unusual war/event does not dominate conclusions.
- Log player interventions during demo play; distinguish player-caused, natural AI, and ClanAI-caused effects.
- Compare objective stability/churn, defense/offense response, army behavior, settlement targeting, logistics/recovery, geography, social-memory effects, campaign outcomes, and performance.
- Long human demo playthroughs are field evidence/discovery; controlled paired runs remain causal proof.

Evidence basis:
- NIST comparative and paired experimental design: hold comparable conditions and analyze paired differences.
- Simulation research common-random-number principle: matched conditions improve sensitivity when comparing alternative configurations, while synchronization limits must be acknowledged.

Goal: turn the user's real dynasty history into a reusable Vanilla-vs-ClanAI benchmark suite across several campaign eras and feed long-play discoveries back into focused controlled tests.

## Reference-First Reuse Rule — user-directed — 2026-09-18
For complex tasks, blockers, or proposed improvements, consult the accumulated BannerlordAI source stack before designing from scratch.

Source stack includes:
- accepted live-validation evidence and derived reports;
- recovered Calastides chronicle/chat/source index and native-history archive;
- dynasty/world/economy/supply/traffic/market censuses;
- archived analyzers and instrumentation;
- verified milestone source/builds;
- researched external prior art, papers, mods, and game-agent architectures.

Method:
1. Define the exact problem.
2. Search local project evidence for an existing mechanism, dataset, failed branch, or reusable tool.
3. Search credible external prior art when the local archive does not settle the mechanism.
4. Prefer adaptation/composition over duplicate subsystems.
5. Preserve provenance and do not merge incompatible timelines/datasets merely because their schemas match.
6. Only build new machinery when reuse is insufficient or would violate layer ownership.

Purpose: make project scale an asset—more accumulated evidence should reduce uncertainty and duplicate engineering rather than increase clutter.

## Autonomous launcher failure -> protocol improvement — 2026-09-19
Observed failure:
- First Operator smoke launch method had already proven Python subprocess.Popen can launch Bannerlord autonomously.
- New launch supervisor failed with FileNotFoundError / path-not-visible despite the game being installed.

Evidence comparison found the exact regression:
- known-good Steam install path: "Mount & Blade II Bannerlord"
- broken supervisor path: "Mount and Blade II Bannerlord"
- user Documents save/config path legitimately uses "Mount and Blade II Bannerlord"

Repair:
- separated Steam install and Documents path constants;
- added path/spawn visibility diagnostics and bounded retries;
- required fresh TestRunner v2 status rather than trusting stale state;
- validated autonomous lifecycle at LaunchSupervisor_20260919_001107_903355:
  autonomous launch succeeded, campaignReady=True, planned EXIT_NOSAVE completed, save hash unchanged, no manual intervention.

Promoted rule:
Autonomous launch is infrastructure, not a convenience. A long-run bot test is not accepted if a human must relaunch or rescue the game.

## Fast recovery/materialized boot path — 2026-09-19
Observed inefficiency:
- Normal replacement-agent recovery reopened/replayed more evidence than necessary and printed large explanatory payloads.
- The common case only needs proof that materialized state is caught up plus current runtime/owner/hash facts.

Accepted adaptation:
- Default boot command is now `python -X utf8 D:\BannerlordAIResearch\Tools\Handoff\fast_resume.py`.
- Fast path verifies current checkpoint sequence against the event-journal tail, journal-head hash, installed ClanAI hash, Bannerlord process, TestRunner readiness, active control run, deployment owner, watcher heartbeat, and sticky intent.
- Normal output is one compact machine-readable line; full details are silently persisted to `Continuity\fast_boot.json`.
- Deep `resume_state.py` is an escalation path only when fast boot reports warnings/mismatch or when the compact state is insufficient for a specific investigation.
- Explanatory prose belongs in reports/architecture ledgers, not the hot boot/index path.

Validation:
- Three-run benchmark on this machine: fast path avg 315.8 ms; deep path avg 954.3 ms (~3.0x faster).
- Forced-drift self-test produced `ESCALATE_DEEP` in 212.5 ms.
- Live Manan state, lease owner, active control run, sticky patrol intent, installed hash, and journal consistency were all recovered on the fast path.

Rule:
Optimize the common recovery path for compact verified state; pay deep-recovery cost only on mismatch/ambiguity. Preserve safety by escalating rather than weakening integrity checks.

## 2026-09-19 — Dynasty canon bootstrap live proof (v0.20R)
- Date-scoped canon bootstrapping is viable: exact Manan root produced parsed=15, available=14, futureHidden=1, malformed=0, mainAvailable=8 with scoreMutation=False and futureCanonLeak=False.
- Temporal firewall was causally checked against the seed config: the sole hidden item was Vadrios succession/inheritance_responsibility at campaignHours 649331.3949947777, later than the Manan root cutoff 648650.9850730833.
- Older dynasty saves can legitimately have zero NobleMemory/SocialLedger records while the memory engine is installed; canon bootstrap must therefore be separate from learned episodic memory and provenance-labeled.
- Important integration gap: bounded Manan run advanced 13.874 campaign-hours with dynastyMindRetrievals=0. The current retrieval hook lives on AI hourly decision context and does not naturally fire for the player-controlled/operator path. Add an explicit high-value controller/player deliberation retrieval hook before any dynasty-memory score influence is enabled.
- Preserve the staged order: bootstrap + read-only retrieval -> shadow feature influence -> bounded causal Apply. Do not jump directly from seed presence to score mutation.
- Live roadmap rule: use one evolving dynasty branch as the testbed and treat naturally occurring events as interchangeable capability tests; do not block progress on recreating a specific historical event.

## 2026-09-19 — Player/operator dynasty-memory shadow retrieval live proof (v0.20S)
- Exact-load identity must be proven independently from /continuegame bootstrap. Accepted lifecycle: Steam-ready bootstrap -> fresh TestRunner campaign generation -> explicit command-bus LOAD_SAVE -> generation increments -> loadedSave matches -> hero/id and campaignHours match expected anchor.
- v0.20S proved the player/operator path can retrieve the date-filtered Manan canon packet at a high-value deliberation boundary without writing behavior: HOLD retrieved 8/8 Manan memories with provenance, shadowFeature names, proposedDelta values, applied=false, and scoreMutation=false.
- Temporal firewall remained intact at the exact Manan root: parsed=15, available=14, futureHidden=1, malformed=0, mainAvailable=8, futureCanonLeak=False.
- Game-end evidence retained memoryCausalScoreWrites=0 and memoryCausalFailures=0; protected root, BLOOD FUED, and disposable clone hashes all remained byte-identical after EXIT_NOSAVE.
- Exact named-load can rotate ClanAI session logs. Validation must search all operation-local session logs after the exact-load boundary rather than assuming seed, deliberation marker, and game-end summary share one file.
- Launch supervisor semantics are now explicit: /continuegame proves bootstrap readiness only; target save identity requires explicit named load plus identity/time gates.
- Next staged rule remains conservative: shadow retrieval is accepted; any memory-driven Apply must be bounded, decision-context-specific, causally observable, and retain deterministic/native execution ownership.

## 2026-09-19 — First bounded dynasty-memory causal Apply live proof (v0.20T)
- v0.20T proved that date-valid dynasty memory can causally change a real player/operator strategic intent while native TaleWorlds execution remains the executor.
- Scope was intentionally narrow: Manan at her known territorial anchor Syronea, settlement-defense posture only. Baseline HOLD=1.0; PATROL base=0.985.
- Only four context-relevant memories were eligible: Syronea territorial anchor, protect_line, survival_capacity, and courage_vs_waste. Their weighted evidence produced a 0.0349124968 (3.491%) patrol adjustment, below the hard 4% cap, yielding PATROL adjusted=1.01938879 and flipping HOLD -> PATROL.
- Causal receipt proved wouldFlip=true, applied=true, intentMutation=true, scoreMutation=false, causalEvaluations=1, causalWouldFlip=1, causalApplied=1, causalActualFlip=1, causalFailures=0.
- Native execution proof: after the memory-selected PATROL intent, the existing operator resolved waiting and Bannerlord executed its own town Leave action. A Donative Demand incident can legitimately interrupt later movement and must not be treated as failure of the already-proven intent/execution handoff.
- Game-end global memory scoring remained untouched: memoryCausalScoreWrites=0, memoryCausalFailures=0. Protected Manan root, BLOOD FUED, and disposable clone remained byte-identical after EXIT_NOSAVE.
- Runtime safety: DynastyMindCausal.cfg is restored to Mode=Observe after the proof. Apply remains explicit, bounded, context-gated, reversible, and off by default.
- Testing lesson: acceptance criteria should prove the exact responsibility boundary (decision changed -> native execution initiated), not require unrelated downstream continuity when legitimate game events can preempt it.

## 2026-09-19 — New-branch episodic memory live proof (v0.20U)
- v0.20U proved a naturally occurring event in the new Manan branch can become a new actor-valid memory rather than relying only on pre-seeded historical canon.
- Event: Donative Demand incident opened naturally after Manan left Syronea. ClanAI recorded one immutable IncidentOpened episode with actor=Manan/lord_3_12, current branchId, campaignHours=648650.98507308331, UTC observation time, contextId=incident:donative-demand, optionCount=4, and source=TestRunner.GauntletMapIncidentView.CreateLayout.
- Retrieval later in the same live branch returned visibleCount=1 with actorValid=true, branchValid=true, futureLeak=false, scoreMutation=false and matching provenance/time.
- Diagnostic counters closed at dynastyBranchEpisodes=1, recorded=1, retrievals=1, duplicates=0, rejected=0. Global memory scoring remained untouched: memoryCausalScoreWrites=0 and memoryCausalFailures=0.
- Protected Manan root/BLOOD FUED/disposable clone hashes remained byte-identical after EXIT_NOSAVE; causal Apply config remained Mode=Observe.
- Architecture rule: social episodes keep their strict hero/clan schema. General dynasty/world incidents use a separate immutable actor-episode store rather than inventing fake counterpart heroes.
- The branch store is save-synced through CampaignBehavior SyncData, keyed by persisted DynastyMind branchId, and filters retrieval by actor + branch + campaign time.
- Next unresolved requirement for a true evolving branch is persistence proof across an intentionally saved disposable branch and reload, followed by later retrieval/use. Do not add another causal decision family before persistence is proven.

## 2026-09-19 — Evolving branch memory persistence across full restart (v0.20U)
- Persistence proof passed across two fresh Bannerlord processes using only disposable save slots.
- Stage 1: Manan naturally learned Donative Demand; pre-save branchId=2a2aa1d974d24e1da08415efe144449a and episodeId=974bcca48adf4f96b627d54aa94b4023. Guarded native SaveAs serialized dynastyBranchEpisodes=1 with zero duplicates/rejections and zero memory score writes.
- Test-only runner SaveAs safety: only names starting with 'ClanAI V020U PERSIST ' are accepted, existing slots are refused, and SaveAs is deferred outside command processing. Post-SaveAs verification must poll asynchronously because the OneDrive-backed save list can lag the synchronous SaveAs return.
- Stage 2: full game exit -> second fresh process -> explicit exact-load of persisted disposable branch. The same persisted branchId and exact episodeId were restored and retrieved, visibleCount=1, futureLeak=false, scoreMutation=false.
- Protected Manan root and BLOOD FUED hashes remained unchanged. Disposable source + persisted slots were deleted after proof. Test-only runner v0.2.9.6 was removed and accepted runner v0.2.9.5 restored.
- This closes the core evolving-memory durability loop: lived event -> immutable actor/branch/time memory -> native game save serialization -> full restart -> same identity restore -> later retrieval.
- Next milestone: use one persisted lived episode as a bounded, provenance-bearing input to a later decision. Do not add a second broad decision family or global score writes; prefer one explicit decision context and an Observe-first causal receipt.

## 2026-09-19 — Executed lived choice becomes actor-valid memory (v0.20V)
- v0.20V passed the first executed-choice memory proof.
- Natural event: Donative Demand opened for Manan after Syronea egress.
- Bannerlord executed INCIDENT_SELECT 0 first. Only after Incident.InvokeOption returned successfully did ClanAI record an immutable IncidentChoice episode.
- Exact recorded choice: index 0 — "Convince your men that the money would be better spent on supplies and recruits to ensure their survival".
- Choice memory provenance: TestRunner.Incident.InvokeOption; actor Manan/lord_3_12; branchId=1cf595a3cc734b6bac16d20c96c65582; campaignHours=648650.98507308331.
- Choice-only retrieval returned visibleChoiceCount=1 with the exact same option index/text/provenance, actorValid=true, branchValid=true, futureLeak=false, scoreMutation=false.
- Session close counters: dynastyBranchEpisodes=2 (open + choice), recorded=2, generic episode retrievals=0, choiceRecorded=1, choiceRetrievals=1, duplicates=0, rejected=0; memoryCausalScoreWrites=0 and failures=0.
- Protected Manan root/BLOOD FUED/disposable clone stayed byte-identical and EXIT_NOSAVE preserved integrity.
- Architecture rule: remember actual executed choices, not previews or attempted commands. IncidentOpened and IncidentChoice share the actor-episode store but use kind-specific retrieval.
- Next gate: prove the exact lived choice persists across native save -> full restart -> exact reload with same branchId + same choice episode id before using it as a causal reasoning feature.

## 2026-09-19 — Executed lived choice persistence across full restart (v0.20V)
- Choice persistence passed across two fresh Bannerlord processes.
- Stage 1: Donative Demand opened, option 0 executed, one IncidentOpened + one IncidentChoice were recorded, and guarded native SaveAs serialized records=2.
- Pre-save branchId=25f69dd868d347fab916e45b74b29440; choiceId=34d0cbeb66b7403ba3806faad450fbaf.
- Stage 2 full restart + exact reload restored the same branchId and exact choiceId. Choice-only retrieval returned visibleChoiceCount=1 with exact option index/text/source, futureLeak=false and scoreMutation=false.
- Test runner v0.2.9.8 was removed; accepted v0.2.9.7 restored. Disposable source/persist saves were deleted and protected originals stayed byte-identical.
- This closes the lived-choice durability loop: event -> executed choice -> immutable memory -> native save -> full restart -> same choice identity -> later retrieval.
- Next gate: Observe-only causal reasoning. Compare a later incident baseline ranking against a bounded persisted-choice memory_resonance adjustment; emit provenance and wouldFlip receipts without auto-selection or native score writes.

## 2026-09-19 — Native incident cooldown discovered during v0.20W
- Live IncidentManager diagnostics proved Bannerlord incidents have a global cooldown measured in campaign-hours, not a short post-event delay.
- On the tested persisted Manan branch: nowHours=648650.9850730833, globalDeadlineHours=648917.3043761945, globalRemainingHours=266.3193, globalIsFuture=true.
- The live IncidentModel reported min global cooldown=192 hours and max global cooldown=360 hours; global trigger probability after eligibility=0.5.
- IncidentManager also held 26 incident-specific cooldown entries.
- Therefore the previous +2 campaign-hour later-incident gate was invalid by >100x and caused harness false negatives, not AI/memory failures.
- Rule: later-incident tests must read IncidentManager's live cooldown and advance to actual expiry before expecting another natural incident. Use model max as the safety ceiling; never hard-code a guessed short delay.

## 2026-09-19 — v0.20W persisted-choice later-decision shadow core proof
- Diagnostic-only run (manual time-mode nudge, so not final autonomous acceptance) reached a genuinely later incident after the native global incident cooldown expired.
- Earlier lived choice: Donative Demand option 0, choiceId=601f35b87f484b32a3a0bde96b3e3646.
- Later incident: Singing for Supper at campaignHours=648933.9811939722.
- The exact persisted choice was retrieved with same branch/choice identity, actor-valid, branch-valid, futureLeak=false, scoreMutation=false.
- Shadow scorer applied bounded positive continuity to the later ranking. For option 0: baselineScore=1.22, memoryDelta=+0.0145, adjustedScore=1.2345, similarity=0.1125, recencyDecay=0.7178, sourceChoiceId matched exactly.
- Baseline selected option 0 and memory-adjusted selected option 0, so wouldFlip=false. executionApplied=false, intentMutation=false, scoreMutation=false, nativeScoreWrites=0.
- Conclusion: a persisted lived choice can measurably influence a later decision ranking with exact provenance without changing gameplay. A clean autonomous rerun is still required because the diagnostic run needed one manual PAUSE->AUTO_FAST nudge.
- Runner bug found: TryMaintainAutomation only repaired desired time mode when actual mode==Stop. Bannerlord can downgrade UnstoppableFastForwardForPartyWaitTime to StoppablePlay, causing campaign-hours to stall while desired fast mode remains armed. Staged fix reasserts any desired/actual mode mismatch and logs previous mode.

## 2026-09-19 — Patrol reactive-defense gap observed
- PATROL_SETTLEMENT for the main/player party currently calls SetMovePatrolAroundSettlement; it is movement/orbit intent, not a complete local-defense policy.
- Existing ClanAI bandit initiative logic explicitly excludes party.IsMainParty, so Manan's operator-controlled patrol has no reactive bandit-defense interrupt.
- User observed a nearby Mountain Bandit with a potential villager fight while Manan continued patrol; treat this as a behavioral gap, not acceptable patrol semantics.
- Current test-world census had 643 bandit parties and 310 villager parties, so this is systemically relevant.
- Planned smallest safe fix after v0.20W acceptance: patrol-local threat lens only while patrol intent is active; detect bandit TargetParty/ShortTermTargetParty -> IsVillager, bound by local radius + main-party strength/readiness, emit shadow wouldInterrupt PATROL->DEFEND_VILLAGER first. Apply later may SetMoveEngageParty(bandit) then resume the prior patrol after threat resolves. Do not globally scan every party each frame and do not make patrol suicidal.

## 2026-09-19 — v0.20W clean autonomous persisted-choice later-decision reasoning accepted
- Clean r4 run passed without external/manual commands.
- Native global incident cooldown was respected: 266.3193h remaining initially; engine min/max 192h/360h; cooldown expired at campaignHours=648917.91497225 before later-incident attempts.
- v0.2.10.0 auto-mode-drift repair autonomously corrected two Bannerlord downgrades from StoppablePlay back to UnstoppableFastForwardForPartyWaitTime. Log evidence: AUTO_RESUME count=2 and count=3 from=StoppablePlay.
- A later natural Soldier in Debt incident opened at campaignHours=648929.1917281667 and was left unselected.
- Same persisted Donative Demand choice was retrieved with exact branch/choice identity, actor/branch validity, provenance, and futureLeak=false.
- Shadow ranking remained Observe-only. Baseline selected option 1; memory-adjusted selected option 1; wouldFlip=false.
- Persisted lived choice produced bounded positive deltas: option1 +0.0167, option2 +0.0179, option0 +0.0354; exact sourceChoiceId/provenance carried on every row.
- executionApplied=false, intentMutation=false, scoreMutation=false, nativeScoreWrites=0.
- Protected roots/source stayed byte-identical, disposable saves were cleaned, and accepted v0.2.9.7 runner was restored after the test.
- This is the first fully autonomous proof of: lived choice -> persistence -> full restart -> real time passes -> later unrelated incident -> provenance-bearing memory contribution to later reasoning, with zero behavior mutation.

## 2026-09-19 — Low-latency autonomous worker pipeline
- Existing fast_resume measured about 3x faster recovery than deep recovery; keep fast context as the default hot path.
- Added deterministic continuation supervisor: scheduled every minute, executes only explicit registry actions, never arbitrary prose.
- Added one-action claim/state so duplicate schedulers/chats cannot launch overlapping tests.
- Registered actions carry candidate-version and next-action guards plus bounded attempt budgets.
- Failed/nonzero registered work becomes an agent reasoning wall instead of an automatic blind retry.
- Agent continuation wakes periodically for reasoning/design gaps; local supervisor handles known execution between agent turns.
- Preserve near-equivalent outcome quality by carrying proven gates/checkers forward rather than re-reasoning them: single-owner lease, hashes, save integrity, no-repeat, causal mode, exact identity/time, evidence freeze, cleanup.

## 2026-09-19 — Mature automation office / chat-memory architecture

Finding:
Long-running ChatGPT engineering work behaves better when treated as a small high-reliability organization rather than one monolithic agent/chat.

Evidence-backed adaptations:
- FEMA ICS/NIMS -> one accountable owner per decision surface, explicit transfer/handoff, management by objectives, manageable span of control.
- Team Topologies -> narrow responsibility/context boundaries to reduce cognitive load; default service-like handoffs, temporary collaboration only for novel/ambiguous problems.
- Kanban -> WIP=1 on live Coder work and reasoning-heavy Analyzer work; pull the next task only when capacity is free.
- DORA -> keep engineering changes/tests small, independent, testable, and focused on the current constraint.
- Google SRE -> repetitive toil becomes automation; repeat/major failures produce structured postmortem-lite plus action-item closeout.
- High Reliability Organization research -> near misses matter; resist simplistic root causes; runtime evidence outranks hierarchy; defer to the desk closest to the actual evidence.
- Microsoft developer-productivity research -> reduce interruptions/context switching and externalize recoverable state.

Accepted office model:
Front Office -> Coder / Operator -> Analyzer / Archivist, with Supervisor / Platform enforcing clock/WIP/ownership/integrity.
Front coordinates; Coder generates proof; Analyzer turns proof into durable knowledge; Supervisor executes no novel reasoning.

Accepted information model:
FRONT -> HOT -> WARM Architecture/Evidence/Research -> INDEX -> RAW.
Active context shrinks as the project grows; deeper evidence remains searchable and recoverable.

Accepted chat retirement model:
CLOCKED_IN -> HANDOFF_WRITTEN -> DOWNSTREAM_REVIEW_PENDING -> SAFE_TO_ARCHIVE -> SAFE_TO_DELETE -> RETIRED.
No permanent retirement before durable local capture, semantic downstream review, Mem mirror, and Front approval.
Actual ChatGPT UI archive/delete remains a user action; office provides a verified retirement queue.

Accepted office-hours model:
- OPEN: full work classes permitted by policy.
- AFTER_HOURS: read-only Analyzer + Supervisor continuity; Coder offline code/build/static only.
- CLOSED: no Coder/Analyzer project execution; Front design/status + Supervisor maintenance only.
Explicit user approval is required to remove redesign freeze and launch automation.

Validation:
- CLOSED policy refused registered LIVE_GAME action with OFFICE_CLOSED.
- All project ChatGPT automations disabled during redesign.
- PC Continuation Supervisor and Continuity Watchdog disabled during redesign.
- DeepSeek archive brain indexed 726 sources.
- Mem layered mirror verified: FRONT=1, HOT=1, WARM Architecture=6, WARM Evidence=4, Research=3, Legacy=3.
- Chat lifecycle demo proved Analyzer review moves a chat to SAFE_TO_ARCHIVE; Mem gate then moves it to SAFE_TO_DELETE.
- Launch checker result: READY_FOR_USER_APPROVAL, launch_permitted=false until explicit approval.

## 2026-09-19 — Infrastructure improvement must stay incremental

Lesson:
The automation-office redesign became a process bottleneck. Infrastructure is valuable only when it improves the main BannerlordAI value stream.

Standing rule:
- Infrastructure evolves continuously through small patches.
- A change should remove a measured bottleneck, repeated failure, recovery cost, or manual toil.
- After each infrastructure patch, validate and return to BannerlordAI behavior work.
- Large multi-system redesigns require an explicitly approved maintenance window from the user.
- Without that approval, defer the redesign or reduce it to the smallest independently testable change.
- If support/process work starts taking materially longer than the behavior task it is supposed to unblock, stop and return to the roadmap.

This is consistent with DORA small-batch and constraint-focused continuous improvement guidance.

## 2026-09-19 — v0.2.10.1 patrol-local reactive defense shadow accepted
- Exact Manan/Syronea load proved the patrol-local observer can use Bannerlord's native threat radius plus nearby-party iteration to identify a real bandit targeting a local same-faction villager.
- Winning receipt: Corsairs `southern_pirates_47910` -> Fishers of Vargornis through `ShortTermTargetParty`; actor strength 273.499969 vs bandit 81.7 (ratio 3.3476), `safeToIntervene=true`, `wouldInterrupt=true`.
- Shadow proposal was `ENGAGE_BANDIT` with resume `PATROL_SETTLEMENT:town_ES7`; behaviorMutation=false, intentMutation=false, scoreMutation=false.
- Protected saves stayed byte-identical; accepted runner/config were restored and disposable save cleaned.
- The 18:24 failure is a harness false negative, not a feature failure: validator required `auto_fast_armed` even though AUTO_FAST had been consumed into town-wait/incident flow. The unchanged observer passed after the incident-aware harness fix.
- Architecture conclusion: local reactive defense can be decided at a bounded spatial/target-link boundary without global score writes. This proves eligibility/proposal only; native engage execution and patrol resumption remain unproven.
- Next gate is one bounded native `SetMoveEngageParty` interrupt with causal receipt, then patrol restoration on resolution/disappearance/timeout, followed by EXIT_NOSAVE and integrity restoration checks.

## 2026-09-19 — Structured handoffs replace transcript replay

Accepted rule:
- Orchestrator -> Worker delegation.
- Large logs/raw payloads stay in external durable storage; agents exchange stable pointers/paths instead of context dumps.
- Replacement chats recover from structured handoff packets containing intent, current state, constraints, evidence pointers, and next action.
- Chat title is cosmetic; operational identity comes from durable role/packet.
- Departing chats logically retire themselves by writing the handoff and printing the next activation command. The final ChatGPT Archive click remains a user UI action.

Canonical schema: D:\BannerlordAIResearch\Automation\Office\structured_handoff_schema.json

## 2026-09-19 — v0.2.10.2 bounded patrol-defense Apply accepted
- Exact-load Manan/Syronea proof issued one native `SetMoveEngageParty` to the observed Corsairs party `southern_pirates_47910`; TargetParty and ShortTermTargetParty both confirmed the exact bandit.
- After the 15-second safety bound, one native `SetMovePatrolAroundSettlement` restored `PATROL_SETTLEMENT:town_ES7`; action receipts recorded zero native score writes.
- Protected saves stayed byte-identical, disposable save cleanup passed, Observe remained the default causal config, the accepted runner was restored, and the deployment owner was released.
- This proves a recoverable native movement interrupt behind accepted local eligibility. It does not prove autonomous production triggering or combat outcome.
- Causal instrumentation lesson: when execution semantics depend on proposal fields, the action receipt should persist the proposal fingerprint/id and relevant identity fields. The current Apply receipt proves the exact bandit target but does not preserve victim/proposal provenance.
- The next gate should bind a fresh proposal to execution provenance and prove exactly-once automatic triggering while keeping the same bounded recovery and integrity envelope.
- Do not rerun v0.2.10.2 unchanged solely to re-prove explicit one-shot engage plus timeout restoration.
## 2026-09-20 — v0.2.10.3 automatic patrol-defense Apply accepted; terminal validator was a harness false negative
- Exact-load Manan/Syronea produced one fresh safe proposal with immutable proposalId/fingerprint plus settlement, bandit, victim, and campaignHours provenance.
- The automatic gate issued exactly one native `SetMoveEngageParty` to `southern_pirates_47910`; target receipts matched the exact bandit and proposal provenance.
- One bounded `SetMovePatrolAroundSettlement` restored `PATROL_SETTLEMENT:town_ES7`; engage and restore both recorded zero native score writes.
- All 14 semantic acceptance checks were true, protected saves matched, disposable save cleanup completed, Observe remained default, the accepted runner was restored, and the deployment lease was released.
- The validator then hit an obsolete `threat_receipts` name only in final summary printing, after `completed_local`; its exception handler overwrote result to FAIL. This is `HARNESS_FALSE_NEGATIVE`, not a behavior failure.
- Current validator source removes that obsolete name and compiles offline. Do not rerun the live v0.2.10.3 proof solely for this reporting bug.
- Architecture conclusion: proposal provenance plus a campaign-scoped consumed latch is sufficient for a bounded exactly-once automatic patrol-defense interrupt proof. Combat outcome, resolution-driven restore, and recurring rearm remain outside this acceptance.
- Next smallest gate is a post-restore latch soak: keep observing after restore and prove recurring same-threat shadow proposals cannot cause a second engage before any recurring-rearm design is introduced.
## 2026-09-20 — v0.2.10.8 persistent known/new enemy recognition primitive accepted
- Fresh-process reload restored the same branch and two identity-addressable patrol-defense memories.
- southern_pirates_47910 classified KNOWN_ENEMY and resolved to the exact persisted episode id with source/detail provenance; real distinct Syronea threat identity southern_pirates_48616 classified NEW_ENEMY with no prior episode.
- Recognition receipts are actor/branch/identity-valid, futureLeak=false, behaviorMutation=false, intentMutation=false, scoreMutation=false.
- The recognition-only runner statically disconnects automatic Apply/rearm/tick/enable paths; live validation produced no patrol-defense Apply receipt.
- Integrity closeout passed: protected saves matched, disposable saves were cleaned, accepted binaries restored, lease released.
- Scope limit: the deterministic recognition probe occurred after a full process restart but at the same campaign hour (memoryAgeHours=0) and used the explicit recognition command. This accepts the persistent recognition primitive plus observer wiring, not yet a naturally later observer-generated recognition event.
- Next gate: fresh reload, advance campaign time, let the real patrol-defense observer itself encounter a remembered bandit, and require an observer-generated KNOWN_ENEMY receipt with positive memory age and no manual recognition command or behavior authority.

## 2026-09-20 — v0.2.10.9 naturally later observer recognition accepted
- After full fresh-process reload, Manan's persisted branch naturally advanced from 648650.9850730833 to 648655.42420875 campaign-hours before the patrol observer encountered remembered Corsairs identity `southern_pirates_47910`.
- Observer-generated recognition classified it `KNOWN_ENEMY` with memoryAgeHours=4.439135666703805 and resolved the exact prior episode `fac5fdaf1e9e45d39c7545e5d0a17598` plus original provenance.
- The recognition receipt and patrol-defense shadow receipt share the exact proposalId/campaignHours. No explicit `PATROL_DEFENSE_RECOGNIZE` command occurred in Stage 2.
- Recognition remained read-only: actor/branch/identity valid, futureLeak=false, behaviorMutation=false, intentMutation=false, scoreMutation=false; automatic Apply/rearm stayed disconnected and no Apply side effects appeared.
- Protected saves stayed byte-identical, disposable saves were cleaned, accepted binaries restored, lease released.
- Harness lesson: when correlated evidence is emitted in a known same-tick order, validators must wait briefly for the second correlated write. Do not classify the feature from a transient file-order race when the post-run pair matches exact provenance.
- Scope limit: this proves natural later KNOWN recognition only. Next gate should prove natural observer discrimination between one remembered real group and one distinct unseen real group (KNOWN vs NEW) without behavior authority.


## 2026-09-20 — v0.2.10.10 natural mixed-identity recognition accepted
- One fresh-reload Manan/Syronea patrol naturally produced exact recognition for two different real enemy groups without manual recognition or behavior authority.
- Remembered Corsairs identity `southern_pirates_47910` resolved `KNOWN_ENEMY` at campaignHours 648655.9863583611 with memoryAgeHours=5.001285277772695 and exact persisted episode `e9b7889daf814b0f844f17f8c1695989` plus source/detail provenance.
- A distinct real Mountain Bandits identity `mountain_bandits_48971` later resolved `NEW_ENEMY` at campaignHours 648734.14065875 with visibleCount=0, priorEpisode=null, and memoryAgeHours=null.
- Each recognition row correlated to a natural patrol-defense shadow with exact proposal identity/campaignHours. The new group was threatening Villagers of Vargornis (`villager_empire_template_38770`).
- Recognition remained read-only: actor/branch/identity valid, futureLeak=false, behaviorMutation=false, intentMutation=false, scoreMutation=false. Automatic Apply/rearm remained disconnected and no patrol-defense Apply receipt existed.
- Shared runner.log still contains older v0.2.10.8 manual probe commands, but the v0.2.10.10 Stage-2 interval contained no `PATROL_DEFENSE_RECOGNIZE`.
- Protected saves remained byte-identical, disposable saves were cleaned, accepted binaries restored, Bannerlord exited, and the deployment lease was released.
- Architecture conclusion: exact real-party identity is now proven as a natural actor-memory discrimination boundary. Keep exact party identity separate from future lineage/familiarity semantics.
- Scope limit: this proves recognition only. Natural NEW encounter -> automatic episode creation is not yet proven.
- Next gate: v0.2.10.11 should record exactly one immutable PatrolDefense episode from a valid observer-generated NEW_ENEMY + matching natural shadow, with provenance and zero behavior/intent/score authority. Persistence of that autonomously learned episode belongs to the following gate.


## 2026-09-20 — v0.2.10.11 natural patrol-defense encounter acquisition accepted
- A fresh zero-episode Manan branch naturally observed Corsairs identity `southern_pirates_47910` at campaignHours 648655.6839331945 and classified it `NEW_ENEMY` with visibleCount=0, priorEpisode=null, memoryAgeHours=null.
- The matching natural patrol-defense shadow carried exact proposalId/campaignHours and real Corsairs -> Fishers of Vargornis threat provenance.
- The observer synchronously recorded exactly one immutable PatrolDefense episode `b36cc37c74b24292a1604611eb3f7582` with settlement/bandit/victim/proposal/fingerprint/time provenance and source `PatrolDefenseObserver.v02111.natural_new_enemy`.
- Immediate exact-identity retrieval returned visibleCount=1 and the same episode ID/context/source/detail, actor/branch/identity valid and futureLeak=false.
- No manual memory seed or manual recognition command occurred. Automatic Apply/rearm stayed disconnected; acquisition reported behaviorMutation=false, intentMutation=false, scoreMutation=false, nativeMovementCalls=0; no Apply receipt existed.
- Protected saves stayed byte-identical, disposable clone cleaned, accepted binaries restored, Bannerlord exited, and lease released.
- Architecture conclusion: autonomous first-encounter memory acquisition is now proven. The loop is natural observation -> NEW recognition -> immutable exact-identity episode -> exact retrieval.
- Scope limit: persistence of this autonomously acquired episode across native save/full restart is not yet proven.
- Next gate: SaveAs the naturally learned episode, fully restart Bannerlord, exact reload the same branch, and require the exact episode ID/provenance to restore by exact bandit identity. A later gate should then prove natural KNOWN recognition from that autonomously learned persisted memory.


## 2026-09-20 — v0.2.10.12 autonomously learned patrol-defense memory persistence accepted
- A PatrolDefense episode learned from a natural NEW_ENEMY observation now survives the complete native durability path without any validation seed.
- Stage 1 naturally learned Corsairs `southern_pirates_47910` as episode `f0602ed4320445c88af1574177edfcdf` on branch `d766c933cda44d5aafac0c7c329c2962`, with exact bandit/victim/proposal/fingerprint/time provenance and zero gameplay authority.
- Guarded native SaveAs produced persisted save SHA256 `9B0684CD66F825BF73B27D23D5241517EE8066BAFC38DFFC463208BC50AFB04C`, then Bannerlord fully exited.
- A fresh Bannerlord process exact-loaded the persisted branch and reported `restored=True`. Exact bandit retrieval returned the same episode ID, context, source, detail, and knownByHours; actor/branch/identity validity remained true and futureLeak=false.
- The branch episode store reported records=3 because incidents share this store. Architecture/test rule: total episode count is not a PatrolDefense identity invariant; key acceptance to exact episode identity/provenance.
- The first attempt's `operator_save_test_prefix_denied` was a harness false negative caused by an unapproved save name. The save-prefix guard worked correctly; repaired harness reused the accepted `ClanAI V020V PERSIST ...` namespace with unchanged feature binaries.
- No manual seed/recognize, no Apply side effects, persisted save hash remained stable through Stage2 EXIT_NOSAVE, protected saves were unchanged, disposables cleaned, binaries restored, owner released.
- Architecture conclusion: autonomous lived PatrolDefense experience is now a native-save-backed persistent memory primitive.
- Scope limit: naturally later observer-generated KNOWN recognition from that autonomously learned persisted episode remains unproven.
- Next gate: fresh restart/time advance/patrol and require natural KNOWN_ENEMY for the exact Stage1 learned bandit with priorEpisode.id equal to the autonomous episode and positive memoryAgeHours. Do not use an explicit retrieval as the decisive recognition proof.


## 2026-09-20 — v0.2.10.13 natural KNOWN from autonomous persisted memory accepted
- A zero-store Manan/Syronea patrol naturally observed real Corsairs identity `southern_pirates_47910` as NEW_ENEMY and autonomously created exactly one immutable PatrolDefense episode `469fd9018a2340a79e1d8515a92c066f` on branch `12c440ac942f4aeabed8c3861377af37`.
- Guarded native SaveAs persisted the episode; Bannerlord fully exited; a fresh process exact-loaded the persisted save with restored=True on the same branch.
- After positive post-reload campaign-time advance from 648654.5475612222 to 648731.3622146667, the real patrol-defense observer naturally encountered the exact same Corsairs party and emitted KNOWN_ENEMY with memoryAgeHours=77.05883355555125.
- The KNOWN receipt resolved the exact autonomous episode id and exact context/source/detail/knownByHours; its natural shadow matched the exact proposal/campaign time.
- The known proposal caused no duplicate memory acquisition. No manual recognize/seed or explicit memory retrieval supplied the decisive Stage2 proof.
- Recognition remained read-only: behaviorMutation=false, intentMutation=false, scoreMutation=false; Apply/rearm stayed disconnected and no Apply side effects occurred.
- Protected saves remained byte-identical, persisted hash stayed stable, disposables were cleaned, accepted binaries restored, Bannerlord exited, and the deployment lease was released.
- Attempt 1 was correctly ENVIRONMENTAL_INCONCLUSIVE because the exact learned bandit did not reappear after positive post-reload time; one unchanged fresh retry exercised the missing condition and passed.
- Architecture conclusion: the natural exact-identity memory loop is closed end to end: NEW encounter -> autonomous episode -> persistence -> restart -> later natural exact KNOWN recognition.
- Scope limit: memory is still context only. No patrol-defense eligibility, target, intent, score, movement or combat policy is yet allowed to depend on recognition.
- Next architecture step should be Observe-only decision-context integration: make exact memory provenance available inside patrol-defense reasoning without changing the decision, then validate before designing any causal policy influence.


## 2026-09-20 — v0.2.10.14 patrol-defense memory context shadow accepted
- The patrol-defense shadow decision now carries the exact observer recognition receipt as nested read-only memoryContext without performing a second memory lookup or recognition call.
- PatrolDefenseShadow.Evaluate still runs first and remains the baseline decision owner.
- Stage1 natural NEW for southern_pirates_47910 produced a memory-empty context (visibleCount=0, priorEpisode=null, memoryAgeHours=null) exactly matching the standalone recognition receipt, then exactly-one autonomous episode acquisition proceeded as before.
- After SaveAs/full restart and positive post-reload time, the same real Corsairs identity naturally reappeared as KNOWN_ENEMY with exact prior episode ae9de9cc43dd41b787ee02522b30dc89 and memoryAgeHours=0.9638314999174327.
- The matching natural shadow nested a memoryContext exactly equal to that KNOWN receipt, including exact branch/context/source/detail/knownByHours provenance.
- Baseline decision fields were unchanged; no Apply/rearm, behavior, intent, score or native movement authority was granted.
- Architecture conclusion: exact identity memory has crossed into the patrol-defense reasoning surface as context, while action ownership remains isolated.
- Policy caution: KNOWN alone has no justified aggression/avoidance valence. Before causal memory influence, add factual prior/current state or outcome evidence that can support a principled policy.


## 2026-09-20 — v0.2.10.15 prior/current patrol-defense threat-state memory shadow accepted
- Natural NEW acquisition now persists five already-computed patrol-defense facts in the existing episode Detail: actorStrength, actorHealthy, banditStrength, banditHealthy, actorToBanditStrengthRatio.
- The acquisition receipt carries the same five-value threatSnapshot; Stage1 shadow, acquisition snapshot, and persisted episode snapshot matched exactly.
- No new world scan was added. The values are copied from PatrolDefenseShadow.Evaluate locals.
- After guarded SaveAs, full Bannerlord restart, same-branch restore, and positive campaign-time advance, exact learned Corsairs identity southern_pirates_47910 naturally returned as KNOWN_ENEMY with the exact autonomous episode 10bef7ced1204b4a8a1fa6a13b0faea4 and memoryAgeHours=4.224479805561714.
- The later shadow embedded memoryContext exactly and exposed current threat facts. The validator parsed the prior snapshot and emitted BannerlordAI.PatrolDefenseThreatStateComparison.v1 with interpretationApplied=false.
- This run's prior and current five-value snapshots were identical, so all deltas were 0 and all current/prior ratios were 1.0. That proves comparison plumbing and exact fact preservation, not changed-state sensitivity.
- No reacquisition/manual retrieval/seed/recognize/Apply occurred; behavior/intent/score/movement authority remained unchanged.
- Protected saves stayed byte-identical, disposables were cleaned, accepted binaries restored, and the lease was released.
- Architecture rule: factual memory comparison remains a descriptive evidence layer. Do not infer “stronger,” “weaker,” “safer,” “retaliate,” “avoid,” or any other policy meaning until a separate bounded semantic gate is designed and proven.
- Next gate should reuse the same binaries and require a natural exact KNOWN re-encounter where at least one stored/current scalar differs, proving correct nonzero delta computation without policy interpretation.


## 2026-09-20 — v0.2.10.16 natural nonzero threat-state comparison accepted
- Unchanged v0.2.10.15 feature binaries were reused; this was a harness-only evidence gate.
- After natural NEW acquisition, persistence, full restart and exact branch restore, learned Corsairs identity `southern_pirates_47910` produced six exact KNOWN re-encounters whose five-value factual snapshots remained unchanged. The harness correctly treated those as evidence but not acceptance.
- At campaignHours 648740.2771461111 (memoryAgeHours=84.30123436113354), the same exact learned party produced the first naturally changed snapshot.
- Prior: actorStrength=273.499969, actorHealthy=213, banditStrength=81.7, banditHealthy=68, actorToBanditStrengthRatio=3.34761286.
- Current: actorStrength=273.499969, actorHealthy=213, banditStrength=82.86, banditHealthy=67, actorToBanditStrengthRatio=3.30074787.
- Exact validator-only deltas: actorStrength 0, actorHealthy 0, banditStrength +1.16, banditHealthy -1, actorToBanditStrengthRatio -0.04686499. comparison_math_correct=true and interpretationApplied=false.
- Exact episode identity/provenance remained intact; no reacquisition/manual retrieval/seed/recognize/Apply or gameplay authority occurred.
- Protected saves stayed byte-identical, disposables cleaned, accepted binaries restored, lease released.
- Architecture conclusion: exact-identity memory can now support factual then-vs-now comparison under naturally changed world state.
- Do not map these facts directly to aggregate policy semantics yet. Reference-first review supports keeping factual blackboard/memory context separate from later utility/hysteresis layers but does not justify a specific threat label from this vector.
- Next gate: emit the already-proven factual comparison as structured Observe-only runtime shadow context and independently validate its math on a natural changed-state re-encounter.


## 2026-09-20 — v0.2.10.17 runtime factual memory-state comparison shadow accepted
- NEW patrol-defense shadows remain comparison-free; exact KNOWN shadows with compatible prior snapshot now emit `memoryStateComparison` directly in normal runtime Observe evidence.
- Runtime comparison schema: `BannerlordAI.PatrolDefenseThreatStateComparison.v1`, containing exact prior/current snapshots, deltas, current/prior ratios, and `interpretationApplied=false`.
- Natural changed Corsairs re-encounter at memoryAgeHours=19.005398166715167 changed banditStrength 81.7 -> 81.9399948, banditHealthy 68 -> 66, and actorToBanditStrengthRatio 3.34761286 -> 3.337808.
- Independent validator recomputed the comparison from raw prior/current evidence and matched runtime math within numeric precision.
- No semantic labels or gameplay authority were introduced. No reacquisition/manual retrieval/seed/recognize/Apply; behavior/intent/score/movement remain unchanged.
- Protected saves, accepted binaries, disposables and lease integrity passed.
- New architecture gap exposed: current exact enemy memory remains anchored to the first autonomous episode because KNOWN re-encounters do not append new observations.
- Next gate should be append-only memory-update eligibility shadow for changed exact KNOWN re-encounters. Prove eligibility/provenance/append-vs-overwrite semantics before allowing a new episode write.

## 2026-09-20 — Continuous-controller lessons from v0.2.10.19
- A correct durable next_action without dispatch is a latent task, not productive work.
- Protocol PASS, routing, indexing, and REASONING_READY are transition states; the unified Front Controller must immediately execute the phase they authorize.
- Terminal Coder evidence must durably transition to Analyzer VERIFY; Analyzer VERIFY must classify and route bounded repair without waiting for user prompting.
- Accepted partial evidence should accumulate across natural-world runs. Do not rerun a proven sub-gate merely because a later run's natural event ordering differs.
- Validators should test product invariants, not incidental temporal ordering unless ordering is itself a requirement.
- For .19, unchanged/no-write and immediate exactly-one append/visibleCount=2 are accepted partial evidence; remaining proof is persistence across SaveAs/full restart.

## 2026-09-21 — v0.2.10.26 all-history least-squares factual slope accepted
- Four persisted exact same-enemy observations now emit stateLinearSlopePerHour for actorStrength, actorHealthy, banditStrength, banditHealthy, and actorToBanditStrengthRatio.
- Slope uses actual CampaignHours and ordinary least squares with centered time/state means; validator independently recomputed all five values.
- The exact slope object survived native SaveAs/full restart unchanged while all accepted .20-.25 history fields remained exact.
- No sign/magnitude semantics were assigned: interpretationApplied=false, behavior/intent/score mutation=false, nativeMovementCalls=0.
- Protected saves matched, disposables cleaned, accepted binaries restored, and lease released.
- Prior-art basis: NIST Engineering Statistics Handbook treats straight-line slope as a standard quantitative summary for sequential change. BannerlordAI uses only the descriptive slope, not significance testing or prediction.
- Next smallest factual gate: residual deviation around the accepted linear fit, because slope alone does not express how tightly observations follow that line. Keep residuals numeric/read-only; do not label stability, volatility, predictability, safety, or threat.

## 2026-09-21 — v0.2.10.27 factual residual RMSE accepted
- Persisted PatrolDefense history now exposes component-wise residual RMSE around the accepted all-history least-squares fit.
- Independent validator math matched product output for all five fields and the exact object survived native SaveAs/full restart unchanged.
- Residual magnitude remains descriptive only; no stable/noisy/volatile/predictable label, forecast, confidence score, or gameplay authority is permitted.
- This completes the current compact descriptive-statistics ladder (trajectory, time rates, aggregates, dispersion, slope, residual deviation).
- Do not extend statistics by inertia. The next architecture gap is the OUTCOME -> MEMORY side of the target loop.
- Reuse the previously live-proven Inspector v0.33 exact MapEvent outcome identity mechanism instead of inventing another battle-outcome observer.

## 2026-09-21 — v0.2.10.28 read-only native outcome-to-memory link bridge accepted
- Current ClanAI now observes native lord-bandit MapEvent start/end callbacks event-driven, tracking the exact native MapEvent object reference with bounded active/recent identity storage adapted from accepted Inspector v0.33.
- Current helper fixtures passed 29/29 across exact unique/NONE/AMBIGUOUS history resolution, role reversal, native winner mapping/nulls, duplicate callbacks, capacity and retention.
- A live current-binary run produced a real lord_2_12 vs mountain_bandits_48583 start/end pair with the same exact event key and factual native AttackerVictory outcome; exact actor+branch+bandit history correctly resolved NONE with no name/time/distance guess.
- Outcome observation remains read-only: no memory write, interpretation, behavior, intent, score, or native movement authority.
- Architecture rule: outcome provenance may join memory only by exact native event identity plus exact actor+branch+bandit history identity. NONE and AMBIGUOUS are first-class safe results; never infer settlement/context from names, proximity or time.
- Scope limit: live MainHero + EXACT_UNIQUE prior-history outcome remains unobserved and environmental. Do not append outcomes into memory until that exact live link is observed.

## 2026-09-21 — v0.2.10.29 MainHero exact outcome field gate environmental result
- One unchanged v0.2.10.28 field run advanced ~300.409 campaign-hours and observed 111 valid lord-bandit exact native outcome pairs plus continued natural PatrolDefense acquisition, but zero MainHero lord-bandit MapEvent outcomes.
- Classification is ENVIRONMENTAL_INCONCLUSIVE, not a feature/harness failure.
- Do not repeat the same 300-hour gate unchanged. The missing condition is specifically Manan entering a native lord-bandit MapEvent after exact prior memory exists.
- Outcome-memory append remains blocked on a real MainHero + EXACT_UNIQUE link, but independent read-only outcome/capture observation may continue using accepted native identity infrastructure.

## 2026-09-21 — Analyzer validation provenance guard
- A v0.2.10.30 pre-validation Python crash exposed that Analyzer intake selected the globally newest validation directory and could therefore attach a prior action's validation to the current failed action.
- Repair: validation discovery now accepts the current Autopilot dispatched_at as a not-before boundary and ignores validation directories older than that action window.
- Offline proof on the exact failure: filtered lookup returned null for the .30 dispatch while the unfiltered lookup returned the older .29 environmental validation.
- Rule: terminal action review may only attach validation evidence created within the current action's provenance window. If no current validation exists, classify from execution/log evidence only; never inherit a previous run's result.

## 2026-09-21 — v0.2.10.30 exact capture companion accepted
- Current ClanAI linked a real HeroPrisonerTaken callback to the exact same native MapEvent object identity used by the accepted lord-bandit start/end bridge.
- The live capture, start, and end shared one eventKey; linkGrade was exact_native_object_reference.
- The captured hero matched the tracked lord and the capturer matched the start/end bandit.
- Capture was observed before MapEventEnded in the accepted live case; callback ordering is factual and must not be assumed otherwise.
- Capture observation remains read-only: no memory write, interpretation, behavior, intent, score, or movement authority.
- Protected saves, disposables, binaries, and lease all closed cleanly.
- Outcome/capture memory append remains blocked on a real MainHero + EXACT_UNIQUE prior-history outcome; v0.2.10.29 remains parked environmental-inconclusive.
- Next safe gate: unify exact start/end/capture facts into one session-local structured outcome packet keyed by the native event identity. Keep persistence separate until the MainHero exact-link condition exists.

## 2026-09-21 — v0.2.10.31 unified session-local outcome packet accepted
- Current ClanAI normalizes exact battle start, exact history-link resolution, native battle end, and optional exact capture facts into one PatrolDefenseOutcomePacket.v1.
- Capture state is transient and first-write-only per exact event entry; duplicate capture callbacks cannot overwrite the first exact capture facts.
- Live no-capture proof preserved exact NONE history resolution and native AttackerVictory fields while representing every capture field explicitly absent/null.
- Packet creation is session-local normalization, not persistent memory.
- Architecture rule: future outcome-memory eligibility/writes should consume this normalized packet rather than separately rejoining raw callbacks.
- v0.2.10.29 remains the hard writer barrier: no persistent outcome append until a real MainHero + EXACT_UNIQUE prior-history outcome is observed.

## 2026-09-21 — v0.2.10.32 outcome-memory eligibility shadow accepted
- Unified PatrolDefense outcome packets now pass through a pure future-append eligibility gate before any memory writer exists.
- Positive MainHero+EXACT_UNIQUE eligibility and all required negative cases passed 57/57 deterministic fixtures.
- A natural non-MainHero/NONE outcome emitted eligible=false with the exact factual gate failures and no writer/persistent memory mutation.
- The v0.2.10.29 MainHero+EXACT_UNIQUE live barrier remains mandatory; eligibility proof does not waive it.
- Next safe gate: construct a read-only immutable future-append proposal payload only for eligible candidates. Keep episode ID creation and memory mutation owned by a later writer milestone after the live barrier is satisfied.

## 2026-09-21 — v0.2.10.33 immutable outcome append-proposal shadow accepted
- The accepted outcome packet + eligibility gate can now construct the exact immutable future append payload without writing memory.
- Positive proposal fixtures verify exact actor/branch/bandit/context/event/prior-history/native-outcome/capture provenance and knownByHours=endCampaignHours.
- The proposal type deliberately contains no episode ID; future writer ownership retains ID creation.
- Ineligible candidate fixtures return no proposal, and a natural non-MainHero/NONE event emitted proposalEligible=false with proposal=null and episodeIdGenerated=false.
- The terminal validator failed only on a helper-call arity bug after decisive product receipts existed. Independent raw-evidence + integrity verification accepted the milestone without rerunning the natural event.
- Architecture rule: once raw product invariants and cleanup are independently complete, a post-evidence validator false negative should not force duplicate live execution.
- Persistent outcome writer remains blocked on real MainHero + EXACT_UNIQUE field evidence from the parked v0.2.10.29 condition.

## 2026-09-21 — v0.2.10.34 future outcome-writer idempotency identity accepted
- Eligible future outcome proposals now have a deterministic SHA-256 append fingerprint over length-delimited branch+actor+bandit+targetContext+sourceEventKey provenance.
- Equivalent proposals produce the same 64-character uppercase hex key; every identity/provenance component mutation changes it; delimiter-like values cannot alias.
- Null/incomplete proposals produce no fingerprint.
- The fingerprint is not an episode ID and enables no writer.
- This closes duplicate-write identity design before write ownership exists, matching BannerlordAI event-sourcing/idempotent-replay architecture.
- Persistent outcome append remains blocked on v0.2.10.29 field evidence.
- Next safe live capability: bounded exact-identity session-local recent outcome context, separate from persistent memory.

## 2026-09-21 — v0.2.10.35 bounded session-local recent outcome context accepted
- Natural unified outcome packets can now be retained in a separate bounded 128-key session-local context cache keyed by exact actor+branch+bandit identity.
- Exact same-session retrieval returned FOUND with the identical natural eventKey and factual packet fields; wrong-bandit lookup returned explicit NONE.
- Cache supports latest replacement and deterministic bounded eviction; it has no SyncData/save key and never mutates DynastyBranchEpisodeMemory.
- This establishes a transient actor-known outcome layer distinct from persistent episodic memory.
- Next safe gate follows the v0.2.10.14 pattern: attach exact recentOutcomeContext to PatrolDefense shadow reasoning after baseline evaluation, without allowing it to affect decisions.

## 2026-09-21 — v0.2.10.36 recent outcome context crossed into PatrolDefense reasoning shadow
- PatrolDefense baseline evaluation still executes first; exact recent outcome lookup occurs afterward and is appended only to the evidence receipt.
- Natural Manan shadow carried recentOutcomeContext with exact MainHero/branch/bandit identity and explicit NONE for the observed Corsairs.
- No decision, Apply, behavior, intent, score, movement, or persistent-memory authority was granted.
- Terminal validator false negative came from checking top-level banditId; PatrolDefenseShadow stores enemy identity under threat.banditId.
- Independent raw receipt + integrity audit accepted the milestone without rerunning the natural shadow.
- Next boundary proof: full restart must erase recent outcome context because it is working/session context, not episodic memory.

## 2026-09-21 — v0.2.10.37 recent outcome working-context non-persistence accepted
- One natural exact lord-bandit outcome was retrieved FOUND from the session-local recent-outcome cache before SaveAs.
- The populated campaign was saved, Bannerlord fully exited, and a fresh process exact-loaded the same persisted branch.
- The exact same actor+branch+bandit retrieval then returned NONE while DynastyBranchEpisodeMemory reported restored=True on the same branch.
- This is causal proof that recent outcome context is working/session state, not persistent episodic memory.
- Architecture boundary is now explicit: recent native outcome context may inform same-session reasoning, but only an accepted persistent-memory writer may carry outcome facts across restart.
- The parked v0.2.10.29 MainHero+EXACT_UNIQUE field condition remains the hard barrier for that writer.

## 2026-09-21 — v0.2.10.38 recent outcome factual age accepted
- Session-local recent outcome context now reports live currentCampaignHours, deterministic nonnegative outcomeAgeHours, and futureLeak.
- Natural live proof advanced more than two campaign-hours after an exact outcome and independently matched age=current-end exactly; futureLeak=false.
- Wrong identity returned NONE with age/future fields null.
- No freshness threshold or stale/fresh semantic was introduced.
- This makes working-context recency an authoritative fact while preserving policy separation.
- Next architecture gap is the explicit ACTOR-KNOWN state composition layer: combine current threat, persistent memory facts, and transient recent outcome facts into one stable read-only reasoning surface.

## 2026-09-21 — v0.2.10.38 factual recent-outcome age context accepted
- Session-local recent outcome context now exposes currentCampaignHours, exact nonnegative outcomeAgeHours, and futureLeak from live CampaignTime at retrieval.
- Live validator independently recomputed outcomeAgeHours=currentCampaignHours-endCampaignHours and matched exactly after >2 campaign-hours of same-session aging.
- Wrong-bandit NONE preserves current time while outcomeAgeHours/futureLeak remain null.
- No stale/fresh label, TTL, expiry threshold, persistence, interpretation, or gameplay authority was introduced.
- Since v0.2.10.36 nests the recent outcome receipt unchanged after baseline PatrolDefense evaluation, these factual age fields automatically cross into the reasoning evidence surface.
- Do not invent a freshness threshold from one run. The next reasoning step should be prior-art-driven semantic/appraisal design, not more descriptive statistics by inertia.

## 2026-09-21 — v0.2.10.39 structured PatrolDefense actor-known factual context accepted
- PatrolDefense shadow now exposes one PatrolDefenseActorKnownContext.v1 after baseline evaluation, composing exact current threat facts, persistent memory context/comparison when present, and recent session-local outcome context when present.
- A natural no-local-threat Manan shadow proved the all-null composition case: actor/branch/settlement identity matched the exact session seed, bandit identity was explicitly empty, neutral bandit threat values were zero, and all availability flags were false.
- Baseline decision remained CONTINUE_PATROL / wouldInterrupt=false with zero Apply or gameplay authority.
- The terminal validator false negative came from assuming a nested threat object in all shadows and checking obsolete availability field names. Corrected offline verification passed every live invariant and integrity check, so the natural run was not repeated.
- Validation rule: PatrolDefense shadow has two valid baseline shapes. threatFound=false omits nested threat; validators must derive neutral enemy facts from the baseline result rather than treating absence as invalid.
- Actor-known state is still factual, not belief/appraisal/policy.
- Next safe reasoning layer: event-driven factual reconsideration evidence based on changes in actor-known context, following BDI/blackboard prior art, before any semantic utility or commitment influence.

## 2026-09-21 — v0.2.10.40 factual PatrolDefense reconsideration evidence accepted
- PatrolDefense now hashes four stable actor-known components: identity, currentThreat, persistentMemory provenance, and recentOutcome identity.
- Volatile currentCampaignHours, memoryAgeHours, and outcomeAgeHours are excluded by construction, so time passage alone does not cause reconsideration churn.
- First observation produces reconsiderationCandidate=true without falsely claiming a factual change; exact repeats are false in fixtures.
- Fixtures prove threat, memory, recent-outcome, identity, and null/presence changes are discriminated by component.
- Live Manan proof emitted firstObservation=true, factualContextChanged=false, changedComponents=[], with valid stable SHA-256 fingerprints and zero gameplay authority.
- This establishes an evidence-backed event-driven cognition boundary before any LLM/planner or semantic utility is introduced.
- Next safe gate: construct a deterministic read-only deliberation request packet only when reconsiderationCandidate=true; still do not invoke a model or change action ownership.

## 2026-09-21 - v0.2.10.41 deterministic PatrolDefense deliberation request accepted
- A reconsiderationCandidate can now produce one deterministic PatrolDefenseDeliberationRequest.v1 containing exact actor-known context, exact reconsideration evidence, and the unchanged baseline decision snapshot.
- Request fingerprint is independently reproducible from stable current component fingerprints plus baseline decision fields.
- Live runtime fingerprint exactly matched the validator independent SHA-256 recomputation.
- llmInvoked=false and plannerInvoked=false; no Apply, score, intent, behavior, movement, or memory-write ownership was introduced.
- This closes the cognition input-envelope boundary before any expensive reasoning is allowed.
- Next safe layer is request scheduling/idempotency: mark a first unseen request fingerprint eligible for future reasoning and suppress exact duplicates, still without invoking a model.

## 2026-09-21 - v0.2.10.42 deliberation route eligibility / duplicate suppression accepted
- First unseen deliberation request fingerprints now produce routeEligible=true / FIRST_UNSEEN_REQUEST in a bounded 128-entry session-local tracker.
- Exact duplicate fingerprints are suppressed in fixtures; changed request fingerprints become eligible; invalid requests are rejected; oldest entries are evicted deterministically at capacity.
- Route tracker resets on campaign/patrol intent boundaries so idempotency scope is explicit.
- Live first request showed seenCount=1, duplicate=false, and all model/LLM/planner invocation flags remained false.
- This closes cognition scheduling/idempotency before any model call.
- Next safe boundary is a strict non-executing structured advisory response admission contract, so future cognition cannot smuggle direct actions/scores into execution.

## 2026-09-21 - v0.2.10.43 strict advisory admission contract accepted
- Future cognition output is limited to a strict three-field flat JSON contract: schema, exact requestFingerprint, bounded disposition.
- Only KEEP_BASELINE, REVIEW_ALTERNATIVE, and ABSTAIN are admitted.
- Malformed/duplicate JSON, mismatched fingerprints, missing fields, unknown dispositions, and any extra fields are rejected.
- Explicit action-bearing fields (action/target/score/scoreDelta/movement/apply/command) were rejected in fixtures.
- Admission is advisory-only and explicitly executionAuthorized=false with zero gameplay authority.
- No model/network call exists yet.
- Next safe boundary is live request-response correlation: admit a synthetic advisory only against the exact currently route-eligible request fingerprint, then consume that pending request to prevent replay.

## 2026-09-21 - v0.2.10.45 dependency-inverted deliberation provider boundary accepted
- A cognition provider now sits behind exact route eligibility and the v0.2.10.43-.44 admission firewall.
- The deterministic local mock provider was invoked only for the exact pending request fingerprint and its raw advisory was admitted through the same strict runtime gate.
- Provider replay after successful admission was not invoked because the pending request had been consumed.
- externalNetworkUsed=false, modelInvoked=false, executionAuthorized=false; no gameplay authority was introduced.
- Architecture rule: future real providers must implement the same interface and may never bypass exact pending-request admission.
- Next safe layer is serialized non-blocking dispatch/outbox handoff so eventual provider work happens outside the gameplay reasoning path instead of blocking the game thread.

## 2026-09-21 - v0.2.10.46 non-blocking provider dispatch/outbox accepted
- First unseen deliberation requests can now be serialized into a bounded 32-job session-local provider queue and exact local outbox row.
- Live dispatch preserved the exact deliberation request JSON and requestFingerprint with queueCount=1, sequence=1 and state=PENDING.
- No provider implementation was invoked during dispatch; provider/network/model/execution flags remained false.
- Pending queues fail closed at capacity and duplicate exact fingerprints do not create second jobs.
- Dispatch sequence remains monotonic across queue resets within the runner session.
- This separates gameplay reasoning from future expensive cognition work.
- Next safe proof is an external/off-thread worker roundtrip using the unchanged dispatch + admission primitives before any real model integration.

## 2026-09-21 - v0.2.10.47 external provider-worker roundtrip accepted
- The unchanged v0.2.10.46 runner successfully handed one natural request to the outbox, then an external Python worker consumed the exact job and returned a strict advisory through the existing runtime admission command.
- In-game provider invocation rows remained zero, proving the worker path is external to gameplay reasoning.
- Exact fingerprint correlation and consume-on-success replay protection remained intact.
- This proves controller -> worker -> admission composition before any real provider/model integration.
- Next safe boundary is a strict external provider-result envelope with provider/attempt provenance and explicit success/failure semantics, still with no network/model dependency.

## 2026-09-21 - v0.2.10.48 strict provider result envelope accepted
- External provider results now carry required providerId, attemptId, exact requestFingerprint, explicit SUCCESS/TRANSIENT_FAILURE/PERMANENT_FAILURE status, and advisory payload.
- SUCCESS can only become providerResultAccepted=true after the nested advisory passes the existing exact pending-request admission firewall.
- Provider failure and malformed/advisory-rejected cases remain fail-closed and preserve pending request state for retry as specified.
- Replay after successful consumption returns NO_PENDING_ROUTE_REQUEST and now serializes advisoryAdmission=null correctly.
- The first live attempt exposed a bounded null-serialization defect; repair was regression-tested with 123/123 fixtures before rerun acceptance.
- Remaining queue lifecycle gap: terminal provider results currently consume admission state but do not remove the corresponding PENDING dispatch job, which can leak bounded queue capacity in long sessions.
- Next safe gate is exact dispatch completion/retry lifecycle accounting before any real model/provider connection.

## 2026-09-21 - v0.2.10.49 provider work completion lifecycle accepted
- Terminal provider work now releases exact bounded dispatch capacity: SUCCESS and PERMANENT_FAILURE remove the matching job; TRANSIENT_FAILURE and invalid/not-admitted results retain it.
- Live SUCCESS completion changed queueCount 1 -> 0; identical replay produced DISPATCH_JOB_NOT_FOUND 0 -> 0.
- Wrong fingerprints cannot remove unrelated jobs, double completion is a no-op, and freed capacity can be reused with monotonic sequence numbering.
- Remaining expensive-cognition gap: external workers can still observe the same outbox row without exclusive claim ownership, risking duplicate compute even though result admission is one-time.
- Next safe gate is exact provider/attempt claim identity with no invented timeout/steal policy.

## 2026-09-21 - v0.2.10.50 exclusive provider job claim identity accepted
- Exact PENDING dispatch jobs can now transition once to CLAIMED with providerId, attemptId and claimedUtcTicks.
- Same provider+attempt replay is idempotent and preserves original claim time; competing provider/attempt claims are rejected.
- No timeout, TTL, expiry or claim stealing policy exists yet.
- Claimed jobs still follow accepted completion lifecycle and can be removed on terminal completion.
- Remaining provenance gap: provider result admission is not yet required to prove that envelope providerId+attemptId matches the active claim before consuming the pending advisory request.
- Next safe gate is claim-bound provider result admission.

## 2026-09-21 - v0.2.10.51 claim-bound provider result admission accepted
- Provider result envelopes must now match the active dispatch job's exact requestFingerprint + providerId + attemptId before they can reach strict advisory admission.
- Live exact claim produced CLAIM_MATCHED, admitted KEEP_BASELINE, and terminal SUCCESS removed the queue job.
- Replay after removal failed at claim lookup with DISPATCH_JOB_NOT_FOUND before another admission could occur.
- Fixtures prove unclaimed and claim-mismatched results cannot consume pending advisory state.
- Remaining retry lifecycle gap: TRANSIENT_FAILURE intentionally retains the job CLAIMED to the same attempt, but no explicit release/reclaim transition exists yet.
- Next safe gate is explicit claim release back to PENDING, with no automatic timeout/steal policy.

## 2026-09-21 - v0.2.10.52 explicit provider claim release / retry transition accepted
- TRANSIENT_FAILURE keeps the exact job CLAIMED and queue capacity retained.
- Exact release by the active provider/attempt transitions CLAIMED -> PENDING and clears claim ownership without changing job identity or sequence.
- A new attempt can then claim the same immutable job.
- Wrong provider/attempt releases and replay releases are fail-closed.
- No automatic timeout/TTL/expiry/steal/backoff exists.
- Next safe proof is integration-only retry rotation: after attempt-2 reclaims the job, stale attempt-1 results must be rejected while exact attempt-2 results are accepted.

## 2026-09-21 - v0.2.10.53 retry-attempt rotation / stale-result rejection accepted
- End-to-end retry rotation is now proven on unchanged v0.2.10.52 binaries.
- After attempt-2 reclaimed the job, stale attempt-1 SUCCESS was rejected with PROVIDER_CLAIM_MISMATCH and queue work remained retained.
- Exact attempt-2 SUCCESS then matched the active claim, admitted KEEP_BASELINE, and completed the job.
- Replay after completion failed DISPATCH_JOB_NOT_FOUND.
- This closes stale retry-result concurrency before real expensive cognition.
- Next safe boundary is outbound provider-request provenance: exact claimed input hash, provider/model identity, prompt contract version and expected response schema before any network/model call exists.

## 2026-09-21 - v0.2.10.54 external provider request envelope provenance accepted
- The external worker now has a strict outbound provider-request contract containing provider/model/attempt/request provenance, immutable prompt-contract version, expected advisory response schema, exact base64 deliberation bytes, and independent SHA-256 input hash.
- Live proof used unchanged v0.2.10.52 binaries and no provider result/network/model call.
- Exact decoded bytes matched the raw outbox deliberation JSON, and independently recomputed SHA-256 matched the envelope.
- Contract rejects altered bytes/hash, wrong claim identity, malformed encodings, missing fields and all unknown/action-bearing fields.
- Remaining provenance gap: runtime has not yet registered which exact outbound provider request is authorized for a claimed job, so results cannot yet be bound to a specific provider request instance.
- Next safe gate is runtime provider-request registration with immutable providerRequestId.

## 2026-09-21 - v0.2.10.55 runtime provider-request registration accepted
- The runtime now independently validates exact outbound provider request bytes/hash/provenance against the active CLAIMED dispatch job and registers one immutable providerRequestId.
- Exact envelope replay is idempotent; a different second envelope for the same claim is rejected.
- Explicit claim release clears registered request metadata so a retry attempt must register its own outbound request.
- providerRequestId is SHA-256 of the exact UTF-8 provider-request envelope JSON bytes, while inputSha256 separately protects exact deliberation input bytes.
- Remaining provenance gap: accepted provider result v1 binds provider/attempt/fingerprint but does not echo or prove the exact registered providerRequestId.
- Next safe gate introduces a separate v2 result contract bound to providerRequestId without mutating accepted v1 behavior.

## 2026-09-21 - v0.2.10.56 registered provider-request ID bound result admission accepted
- ProviderResult.v2 now requires the exact registered providerRequestId in addition to provider/attempt/fingerprint claim identity.
- Live admission proved CLAIM_MATCHED -> PROVIDER_REQUEST_MATCHED -> strict KEEP_BASELINE advisory admission -> SUCCESS_ACKED completion.
- Replay after job removal fails at claim lookup before another request-ID match/admission can occur.
- Accepted ProviderResult.v1 behavior remains unchanged and fixture-covered.
- The request/result provenance chain is now complete enough to isolate transport as its own boundary.
- Next safe gate is a deterministic external transport adapter/loopback contract using unchanged product binaries and explicit no-network/no-model evidence.

## 2026-09-21 - v0.2.10.57 deterministic external transport loopback accepted
- The accepted v0.2.10.56 live-tested binaries were reused unchanged.
- deterministic_local_loopback consumed the exact registered ProviderRequest.v1 and emitted byte-auditable ProviderResult.v2 plus ProviderTransportReceipt.v1.
- Request and result SHA-256 values were independently verified; no socket/HTTP/provider SDK/model call existed.
- Existing runtime v2 admission matched providerRequestId, admitted KEEP_BASELINE, completed SUCCESS_ACKED 1 -> 0, and replay failed job lookup.
- Provenance metadata correction: accepted v0.2.10.56 validation is authoritative for tested staging hashes; stale summary hash fields must not override raw live validation.
- Remaining provenance gap: runtime has not registered which transport instance/transportId was authorized before external send.
- Next safe gate is exact runtime transport-request registration and transportRequestId assignment before real network integration.

## 2026-09-21 - v0.2.10.58 runtime transport-request registration accepted
- Runtime now registers one exact transport request against an active claimed job and already registered providerRequestId.
- transportRequestId is the SHA-256 of the exact UTF-8 transport-envelope bytes received by runtime.
- Exact replay is idempotent; a different second transport envelope is rejected.
- Registration requires exact provider/model/attempt/request/input-hash provenance and is cleared on explicit claim release.
- No transport send/result/model call occurred in the acceptance run.
- Remaining gap: ProviderResult.v2 is bound to providerRequestId but does not echo/match transportRequestId.
- Next safe gate is ProviderResult.v3 transport-bound admission.

## 2026-09-21 - v0.2.10.59 transport-bound ProviderResult.v3 accepted
- ProviderResult.v3 now requires exact active claim, registered providerRequestId and registered transportRequestId before strict advisory admission.
- Live result reported CLAIM_MATCHED -> PROVIDER_REQUEST_MATCHED -> TRANSPORT_REQUEST_MATCHED, admitted KEEP_BASELINE and completed SUCCESS_ACKED 1 -> 0.
- Replay after completion failed DISPATCH_JOB_NOT_FOUND.
- Remaining integrity gap: runtime does not yet bind the exact result bytes it receives to the transport receipt's resultSha256.
- Next safe gate is strict transport-receipt/result-byte hash binding before real network/model integration.

## 2026-09-21 - v0.2.10.60 transport receipt / exact result-byte hash binding accepted
- TransportReceipt.v2 is now submitted together with exact ProviderResult.v3 bytes and runtime independently recomputes result SHA-256 before v3 admission.
- Live receipt resultSha256 exactly matched runtime-computed SHA-256; registered transport/provider/request provenance matched before KEEP_BASELINE admission.
- Replay after terminal completion failed job lookup before another nested v3 admission.
- This closes the major transport-integrity gap before real external provider execution.
- Next safe boundary is explicit external transport execution-policy registration: timeout/attempt/result-size bounds and credential-reference identity with no hidden defaults or secret values.

## 2026-09-21 - v0.2.10.61 explicit external transport execution policy accepted
- Every external transport request can now carry explicit caller-supplied timeout, attempt count, result-size limit and env credential reference before execution.
- Runtime computes an immutable executionPolicyId over exact policy bytes and allows only one policy per active transport request; exact replay is idempotent.
- No product default limits or secret-value reads exist.
- Live policy registration used test-only values and performed no external send/model call.
- Remaining provenance gap: TransportReceipt.v2 and ProviderResult.v3 do not echo executionPolicyId, so runtime cannot yet prove which registered policy governed the result.
- Next safe gate is execution-policy-bound TransportReceipt.v3 + ProviderResult.v4.

## 2026-09-21 - v0.2.10.62 execution-policy-bound TransportReceipt.v3 / ProviderResult.v4 accepted
- Registered executionPolicyId is now carried through both deterministic transport receipt and ProviderResult.v4 and re-matched by runtime before advisory admission.
- Live binding proved TRANSPORT_RECEIPT_MATCHED + EXECUTION_POLICY_MATCHED + exact result SHA, followed by KEEP_BASELINE and SUCCESS_ACKED 1 -> 0.
- Replay failed job lookup.
- First live attempt exposed only a prelaunch validator static-assertion false negative; feature binary was unchanged and all offline gates were already green.
- Remaining policy gap: registered maxResultBytes/maxAttempts/timeoutMs are provenance-only, not yet enforced.
- Next smallest deterministic enforcement is maxResultBytes against exact result UTF-8 bytes, with dual external-loopback and runtime checks.

## 2026-09-21 - v0.2.10.63 maxResultBytes execution-policy enforcement accepted
- Registered maxResultBytes is now enforced against exact ProviderResult.v4 UTF-8 bytes on both deterministic external loopback and independently in runtime.
- Live result was 754 bytes under registered 65536 and reported RESULT_SIZE_WITHIN_LIMIT before KEEP_BASELINE admission and SUCCESS_ACKED completion.
- Oversize and invalid policy limits are fail-closed in fixtures.
- maxAttempts and timeoutMs remain registered-but-unenforced; credentialRef remains opaque.
- Architecture distinction: provider claim attemptId is worker ownership identity, while maxAttempts should govern transport executions under a registered execution policy.
- Next safe gate is explicit runtime transport-execution authorization with bounded maxAttempts accounting before any external send.

## 2026-09-21 - v0.2.10.64 maxAttempts transport-execution authorization accepted
- Registered maxAttempts now gates runtime transport-execution authorization under the exact executionPolicyId before deterministic result production.
- Live authorization was ordinal/count/max 1/1/1; exactly one authorization row existed, then KEEP_BASELINE -> SUCCESS_ACKED 1 -> 0 and replay job-missing.
- Claim attemptId remains worker ownership identity; transport execution ordinal/count is a separate policy-budget dimension.
- Authorization is zero-authority and deterministic; no provider/network/model invocation exists.
- timeoutMs is the remaining registered policy bound that is not actionable at the pre-send transport boundary.
- Next safe gate binds exact timeoutMs into the authorization/transport contract without claiming real wall-clock cancellation.

## 2026-09-21 - v0.2.10.65 exact timeout-budget binding accepted
- Runtime now binds registered timeoutMs into each transport-execution authorization before count increment/result production.
- Live policy timeoutMs="1000" matched authorization timeoutMs=1000; timeout_budget.bound=true, then existing v4 provenance -> KEEP_BASELINE -> SUCCESS_ACKED 1 -> 0.
- Deterministic transport rejects missing/zero/mismatched authorization timeout budgets without network/model work.
- This is pre-send budget binding only, not a claim of real elapsed-time cancellation.
- Remaining provenance gap: TransportReceipt.v3/ProviderResult.v4 do not carry transportExecutionAuthorizationId/ordinal, so multiple executions under one policy are not independently distinguishable at result admission.
- Next safe gate binds exact authorization ID/ordinal through transport receipt/result and rejects stale execution results.

## 2026-09-22 - v0.2.10.66 authorization-bound result provenance accepted
- TransportReceipt.v4 and ProviderResult.v5 carry exact transportExecutionAuthorizationId + ordinal.
- Runtime independently re-matches the latest authorization before size/advisory admission.
- Live maxAttempts=2 rejected stale auth1 with job retained, then accepted exact auth2 -> KEEP_BASELINE -> SUCCESS_ACKED 1 -> 0; replay was job-missing.
- Two validator-only issues were classified without changing feature code: authorization-receipt visibility race, then a post-completion print NameError after all semantic/cleanup checks passed.
- This closes within-policy stale transport-execution result concurrency.
- Next bridge toward a real model is a strict external ProviderExecutionPackage / adapter boundary on unchanged accepted runtime binaries.

## 2026-09-22 - v0.2.10.67 external ProviderExecutionPackage / adapter boundary accepted
- Accepted v0.2.10.66 runtime binary was reused unchanged.
- ProviderExecutionPackage.v1 binds exact provider request bytes, transport bytes, execution-policy bounds/credentialRef, and latest transport-execution authorization into one auditable package ID.
- Stale auth1 package is rejected before adapter invocation when auth2 is latest.
- Exact auth2 package invokes deterministic adapter exactly once and produces accepted TransportReceipt.v4 + ProviderResult.v5 provenance.
- Live runtime admitted KEEP_BASELINE, completed SUCCESS_ACKED 1 -> 0, and replay failed job lookup.
- No secret value, provider network, model invocation or gameplay authority occurred.
- Generic provider-neutral plumbing is now closed. Provider-specific configuration, credential resolution and real network/model use require explicit user authorization.

