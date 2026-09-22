# Performance & Quality Bar

BannerlordAI treats performance and integrity as correctness.

## Performance expectations

Every proposed subsystem should include a cost model.

At minimum, estimate:
- trigger frequency;
- affected actor count;
- lookup complexity;
- allocations;
- reflection cost;
- serialization growth;
- logging volume;
- cache invalidation burden;
- worst-case campaign density.

Prefer:
- event-driven updates;
- spatially bounded scans;
- native indices/relationships;
- compact IDs instead of duplicated payloads;
- incremental state;
- coarse cadence for expensive cognition;
- AI LOD by relevance/importance;
- precomputed or cached data when ownership/timing remain correct.

Avoid:
- global scans every frame;
- per-tick LLM/planner calls;
- duplicate caches of the same truth;
- unbounded history;
- verbose permanent telemetry in hot paths;
- reflection in tight loops when a stable adapter can be built.

## Correctness gates

A future implementation is not “done” because it compiles.

Evidence should cover as applicable:
- exact actor/world identity;
- exact campaign time;
- branch/save lineage;
- causal provenance;
- fresh process lifecycle;
- autonomous launch/recovery;
- no duplicate runtime owner;
- protected-save hashes unchanged;
- disposable-save cleanup;
- installed binary restoration;
- bounded timeout/failure behavior;
- negative controls;
- absence of unintended score/intent/movement writes.

## Test classifications

Use precise semantic classes:
- PASS;
- FAIL;
- HARNESS_FALSE_NEGATIVE;
- ENVIRONMENTAL_INCONCLUSIVE;
- UNKNOWN.

Do not convert “the required natural event did not occur in a bounded window” into a feature failure.

## Evidence hierarchy

1. runtime/raw receipts and hashes;
2. frozen validation artifacts;
3. derived analysis;
4. architecture doctrine;
5. chat summaries.

Never invert this hierarchy.

## Engineering style

Prefer the smallest patch that answers one question.

One hypothesis -> one intervention -> one measurable result.

If the same blocker happens twice:
- stop broad trial-and-error;
- isolate the fault domain;
- search prior art;
- add the smallest guard/adapter/preflight/cache/automation that prevents recurrence.
