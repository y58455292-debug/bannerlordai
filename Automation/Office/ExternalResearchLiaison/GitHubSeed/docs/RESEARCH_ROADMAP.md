# Long-Horizon Research Roadmap

This backlog is intentionally ahead of implementation. The objective is to have evidence and test designs ready before Front Office reaches each future patch family.

## Priority A — Current / near-term identity-memory work

### A1. Party/group identity persistence
Research stable identity semantics for dynamic game entities.

Questions:
- Which Bannerlord party identifiers are stable across save/reload, despawn/respawn, party-template reuse, clan ownership changes, capture, and reformation?
- How should memory distinguish “same party instance,” “same organization,” “same leader,” and “same cultural/group archetype”?
- What are failure cases for StringId-based identity?
- What identity migration rules are needed if a party disappears and a successor forms?

Deliverable:
- identity taxonomy;
- stability matrix;
- proposed canonical identity key;
- collision/alias handling;
- smallest runtime validation plan.

### A2. Familiarity vs episodic memory
Research how repeated encounters should become a compact familiarity representation without retaining unbounded event history.

Compare:
- count/frequency;
- exponentially decayed exposure;
- recency-weighted salience;
- Bayesian confidence;
- sketch/summary structures.

Deliverable:
- bounded update equations;
- storage/performance model;
- provenance strategy;
- prototype on synthetic encounter streams.

### A3. Known/new recognition confidence
Research how an agent should represent certainty when identity evidence is imperfect.

Questions:
- binary known/new versus confidence score;
- aliases and partial observations;
- stale identity;
- contradictory identity evidence;
- future safe fallback.

Deliverable:
- confidence model and thresholds;
- false-positive/false-negative risk analysis;
- test matrix.

## Priority B — Memory architecture

### B1. Salience, decay, consolidation
Study cognitive/game-agent memory systems for:
- salience scoring;
- time decay;
- repetition/rehearsal;
- consolidation from episodes to summaries;
- forgetting/pruning;
- protected high-impact memories.

Deliverable:
- model comparison;
- recommended bounded mechanism;
- complexity/storage estimates;
- prototype;
- falsification tests.

### B2. Retrieval
Research retrieval strategies:
- exact identity lookup;
- context-indexed lookup;
- recency + salience;
- semantic similarity;
- relationship-targeted retrieval;
- capped top-k;
- temporal firewall.

Deliverable:
- staged retrieval architecture that begins deterministic and only adds semantic search where justified.

### B3. Memory interference / contradictions
How should conflicting memories coexist?

Examples:
- party once hostile, later allied;
- trusted lord betrays actor;
- repeated rumors versus direct observation;
- stale belief corrected by authoritative fact.

Deliverable:
- source reliability / recency / confidence model;
- overwrite vs coexistence rules;
- reconciliation test cases.

## Priority C — Relationships, social memory, and reputation

### C1. Trust / grievance / obligation
Research computational models for:
- trust;
- fear;
- grievance;
- gratitude;
- obligation;
- rivalry;
- kinship/political loyalty.

Avoid one giant relationship score.

Deliverable:
- factorized relationship state;
- update events;
- decay/forgiveness options;
- performance/storage model;
- UI/debug representation.

### C2. Reputation propagation
Research how knowledge about another actor can spread without omniscience.

Questions:
- direct experience vs rumor;
- social network propagation;
- distance/culture/faction effects;
- source reliability;
- information delay;
- anti-future-leak constraints.

Deliverable:
- bounded propagation mechanism;
- actor-known versus world-truth separation;
- test plan.

## Priority D — Intent, commitment, and reconsideration

### D1. Intent state machine
Research mature agent architectures for:
- candidate preference;
- commitment;
- execution;
- suspension;
- interruption;
- abandonment;
- completion;
- cooldown.

Deliverable:
- minimal persistent intent schema;
- allowed transitions;
- provenance;
- integration boundary with native movement.

### D2. Hysteresis / anti-thrashing
Study methods that prevent oscillation:
- utility hysteresis;
- switching cost;
- minimum commitment window;
- interrupt thresholds;
- action inertia;
- tabu/cooldown;
- receding-horizon replanning.

Deliverable:
- comparison;
- recommended bounded mechanism;
- synthetic thrash benchmark.

### D3. Plan repair
Research when an intent should be repaired versus replaced.

Deliverable:
- repair taxonomy;
- observable triggers;
- minimal test scenarios.

## Priority E — Strategic planning and utility

### E1. Utility architecture
Research factorized utility scoring with:
- named contributions;
- normalization;
- hard eligibility;
- bounded memory influence;
- personality/goals;
- strategic pressures.

Deliverable:
- reference implementations;
- numerical stability guidance;
- explainability/provenance design.

### E2. Bounded planning
Compare:
- GOAP;
- HTN;
- MCTS;
- beam search;
- heuristic forward simulation;
- rule/utility hybrids.

Focus on rare high-value campaign decisions, not movement ticks.

Deliverable:
- applicability matrix for Bannerlord;
- compute budget;
- minimal planner interface;
- abort/fallback rules.

### E3. Counterfactual reasoning
Research how to estimate likely consequences without cloning the full world simulation.

Deliverable:
- coarse predictive state;
- uncertainty representation;
- rollout depth limits;
- validation design against actual outcomes.

## Priority F — World, logistics, economy, and military reasoning

### F1. Supply/logistics pressure model
Research available Bannerlord signals and modding APIs for:
- food;
- wages;
- troop recovery;
- reinforcement;
- travel time;
- settlement markets;
- army cohesion;
- naval logistics where applicable.

Deliverable:
- authoritative signal map;
- cheap derived features;
- likely strategic uses.

### F2. Geography / travel-time reasoning
Research native path/travel-time APIs and safe caching.

Deliverable:
- distance/time adapter design;
- cache invalidation;
- performance benchmark.

### F3. Settlement strategic value
Research how to value:
- defensibility;
- economy;
- position;
- supply;
- political importance;
- frontier pressure;
- recovery cost.

Deliverable:
- factor schema and source map, not a production weight set.

### F4. Army coordination / roles
Research multi-agent role assignment:
- commander;
- defender;
- raider;
- escort;
- reserve;
- recruiter/recovery.

Deliverable:
- auction/utility/contract-net/blackboard approaches;
- native Bannerlord constraints;
- anti-duplication mechanisms.

## Priority G — Perception and actor knowledge

### G1. Authoritative facts vs beliefs
Research a clean separation between:
- world truth;
- observed facts;
- inferred beliefs;
- rumors;
- stale beliefs.

Deliverable:
- data schema;
- update precedence;
- confidence/source fields.

### G2. Observation radius / sensing
Research how information availability should depend on:
- distance;
- scouting;
- faction network;
- settlements;
- recent contact.

Deliverable:
- cheap sensing model;
- anti-omniscience test cases.

## Priority H — Personality, goals, and emotion/appraisal

### H1. Personality representation
Compare compact trait systems versus free-form profiles.

Deliverable:
- stable numeric/enum representation;
- mapping to decision contributions;
- no-per-tick LLM requirement.

### H2. Goals / needs
Research hierarchical goals, goal persistence, satisfaction, conflict, and priority.

Deliverable:
- minimal goal schema;
- update events;
- interaction with intent and planning.

### H3. Emotion/appraisal
Research event-driven appraisal models that can influence but not replace rational eligibility.

Deliverable:
- compact transient state;
- decay;
- event triggers;
- bounded effect interface.

## Priority I — Performance / AI LOD

### I1. Cognition scheduling
Research event-driven scheduling and AI LOD across hundreds/thousands of actors.

Deliverable:
- tier model;
- trigger types;
- budgets;
- fairness/starvation considerations;
- synthetic benchmark.

### I2. Cache/index design
Research:
- identity indexes;
- spatial indexes;
- event queues;
- dirty flags;
- versioned caches;
- eviction.

Deliverable:
- ownership/invalidation table and benchmark methodology.

### I3. Serialization scale
Estimate save growth for:
- memories;
- relationships;
- goals;
- beliefs;
- intent.

Deliverable:
- byte/actor budgets;
- compression/pruning strategies;
- migration/versioning plan.

## Priority J — Persistence and schema evolution

### J1. Save-schema migration
Research versioned serialization patterns for mods.

Deliverable:
- backward-compatible schema strategy;
- missing-field defaults;
- migrations;
- corruption-safe fallback;
- test matrix across versions.

### J2. Branch / timeline identity
Research how memory should behave across:
- Save As;
- alternate timeline branches;
- load older save;
- copied saves;
- experimental disposable branches.

Deliverable:
- branch lineage semantics;
- merge prohibition/possibilities;
- future-leak defenses.

## Priority K — Testing, observability, and scientific validation

### K1. Causal provenance standard
Research event/provenance schemas that connect:
observation -> proposal -> action -> outcome -> memory update.

Deliverable:
- compact receipt standard;
- IDs/fingerprints;
- correlation rules;
- write ordering / eventual consistency rules.

### K2. Harness race robustness
Research validation patterns for correlated asynchronous writes.

Deliverable:
- event correlation with bounded waits;
- monotonic sequence IDs;
- atomic/frozen evidence options;
- false-negative avoidance.

### K3. Benchmark statistics
Research paired simulation experiment design for our historical dynasty save suite.

Deliverable:
- repeat counts;
- paired metrics;
- confidence/variance handling;
- RNG/desynchronization caveats;
- stopping rules.

### K4. Performance regression suite
Deliverable:
- frame/campaign-time metrics;
- CPU/memory counters;
- telemetry overhead controls;
- acceptance thresholds.

## Priority L — Human-readable behavior and explainability

### L1. Decision explanations
Research compact explanations generated from structured contribution/provenance data.

Deliverable:
- template/schema approach;
- avoid free-form hallucinated rationales;
- player/debug examples.

### L2. Dialogue/voice research
Research Bannerlord dialogue/voice trigger architecture, mod-safe audio replacement/extension, naming, licensing, caching, and optional generative voice workflow.

Deliverable:
- legal/mod-safe architecture;
- trigger map;
- asset pipeline proposal;
- performance/storage costs.

## Priority M — Agent/engineering workflow research

### M1. Durable agent handoff
Continue researching:
- event sourcing;
- workflow checkpoints;
- leases;
- optimistic concurrency;
- idempotency;
- compact hot context.

Only propose changes when they solve a measured project bottleneck.

### M2. Research retrieval architecture
Design the Liaison's own long-term cache so growing research reduces future search cost.

Deliverable:
- source registry;
- tags;
- claims-to-sources map;
- contradiction tracking;
- compact state/handoff.

### M3. Automated literature/code watch
Research safe ways to monitor:
- Bannerlord patches/API changes;
- relevant game-AI papers;
- modding framework changes;
- key dependency releases.

Deliverable:
- low-noise watch design; no automatic production promotion.

---

## Research packet quality gate

Every completed topic should include:
- strongest conclusion;
- strongest contrary evidence;
- source quality ranking;
- implementation-relevant mechanism;
- performance implications;
- license constraints when code is involved;
- open uncertainties;
- smallest BannerlordAI validation;
- explicit “do not conclude” notes.

The Liaison's job is not to generate volume. It is to make future Front Office decisions **cheaper, faster, and better grounded**.
