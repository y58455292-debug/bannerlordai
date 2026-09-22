# Architecture Primer

## Native simulation is an asset

BannerlordAI is not trying to replace TaleWorlds' entire AI stack. Native movement/pathfinding/combat/campaign systems already solve large, performance-sensitive problems.

Our bias:
- preserve native execution;
- inject better perception, memory, context, intent, eligibility, and bounded strategic judgment;
- make ownership explicit;
- prove each new influence before granting mutation authority.

## One owner per write surface

A recurring source of AI instability is multiple layers independently writing the same decision score or movement intent.

BannerlordAI therefore aims for:
- **one canonical score/action owner** at each decision surface;
- named upstream contributions with source/reason attribution;
- observers and memories that propose or annotate before they mutate.

StrategicDecisionComposer has already been proven as the sole strategic `SetBehaviorScore` owner in the refined architecture.

## State layers must not collapse together

Keep these distinct:
- authoritative world/runtime facts;
- actor-known / actor-believed state;
- episodic memory;
- relationships / trust / grievance;
- personality;
- emotion/appraisal;
- goals;
- commitment/hysteresis;
- planner predictions;
- permissions/role;
- execution state.

They may interact, but they are not interchangeable.

## Blackboard principle

A compact per-actor blackboard is useful for authoritative runtime facts at a matching timing boundary. Do not force every subsystem through one cache when timing semantics differ.

## Memory principle

Memory must carry:
- actor identity;
- branch/save lineage;
- episode identity;
- context identity;
- campaign time;
- source;
- provenance/detail;
- future-leak protection;
- read/write authority flags.

Memory influence should be:
- bounded;
- decayed or context-sensitive where justified;
- observable;
- provenance-bearing;
- shadow-tested before Apply.

## Intent and commitment

Future architecture will need a distinction between:
- what an actor currently prefers;
- what it has committed to;
- when it may reconsider;
- when new evidence justifies breaking commitment.

Research on hysteresis, commitment windows, option value, interruptibility, and plan repair is highly relevant.

## Planning

Use planners only for rare, high-value multi-step decisions. Do not run heavy planning every tick or for trivial movement.

Promising pattern:
- event triggers cognition;
- generate small candidate set;
- apply hard eligibility;
- utility/heuristics rank candidates;
- bounded planner explores only when multi-step consequences matter;
- native systems execute.

## Outcomes feed learning

The architecture is a loop:
observation -> belief/context -> memory/goals -> intent -> action -> outcome -> memory/relationship/goal update.

A later patch should not “learn” from a preview or proposal that was never executed. We already use the rule: **remember actual executed choices, not previews or attempted commands.**
