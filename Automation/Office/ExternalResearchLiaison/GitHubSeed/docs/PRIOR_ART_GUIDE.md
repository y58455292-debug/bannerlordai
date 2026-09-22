# Prior-Art Guide

This file tells the Liaison what outside mechanisms already inform BannerlordAI's engineering method and what those sources do **not** prove about the game itself.

## Already adopted engineering/process prior art

### Microsoft — Event Sourcing
Source: https://learn.microsoft.com/en-us/azure/architecture/patterns/event-sourcing

Useful mechanism:
- append-only event history;
- materialized current/read views;
- replay/recovery;
- optimistic concurrency;
- explicit trade-offs and schema-evolution cost.

BannerlordAI adaptation:
- journal transitions before replacing materialized current state;
- immutable snapshots;
- stable action IDs;
- reject stale writers.

Do not conclude:
- that every BannerlordAI subsystem should be event-sourced. Microsoft explicitly notes the complexity/trade-offs; we use it where audit/recovery value justifies the cost.

### Kubernetes — Leases / leader election
Sources:
- https://kubernetes.io/docs/concepts/architecture/leases/
- https://kubernetes.io/docs/concepts/cluster-administration/coordinated-leader-election/

Useful mechanism:
- one active controller for a shared control surface;
- holder identity;
- renewal;
- bounded expiry/takeover.

BannerlordAI adaptation:
- single live deployment/runtime owner;
- do not duplicate controllers or steal an active lease.

### Temporal — durable execution
Source: https://docs.temporal.io/

Useful mechanism:
- externalize workflow state so execution can recover after interruption rather than relying on one process/chat remaining alive.

BannerlordAI adaptation:
- crash-safe rolling checkpoints and operation phases around live side effects.

### DORA — small batches and active constraints
Sources:
- https://dora.dev/capabilities/working-in-small-batches/
- https://dora.dev/guides/dora-metrics/

Useful mechanism:
- small independent changes shorten feedback loops;
- speed and stability need not be treated as opposites;
- improve the most significant active bottleneck.

BannerlordAI adaptation:
- one small acceptance gate at a time;
- infrastructure improvements stay incremental;
- avoid giant rewrites.

### Google SRE — scientific troubleshooting
Source: https://sre.google/sre-book/effective-troubleshooting/

Useful mechanism:
- observations -> hypotheses -> discriminating tests;
- troubleshooting is a teachable process.

BannerlordAI adaptation:
- after repeated blocker, stop broad trial-and-error;
- decompose fault domains;
- test the smallest branch-eliminating hypothesis.

### FEMA ICS/NIMS — role clarity and management by objectives
Examples:
- https://emilms.fema.gov/_is0700b/groups/203.html
- https://emilms.fema.gov/_is_0200c/groups/242.html

Useful mechanism:
- management by explicit objectives;
- unity of command;
- clear reporting/ownership;
- documented results and next operational period.

BannerlordAI adaptation:
- Front Office -> Worker -> Analyzer;
- one accountable owner per live decision/control surface;
- structured handoffs instead of transcript replay.

## Game-AI / cognition prior art: research targets, not automatically adopted

Agent 2 should develop source maps for:
- utility AI;
- GOAP;
- HTN;
- MCTS / bounded lookahead;
- blackboard architectures;
- BDI / intention reconsideration;
- appraisal/emotion models;
- episodic memory and consolidation;
- trust/reputation models;
- multi-agent role allocation / contract-net / auctions;
- AI LOD / event-driven cognition;
- belief/knowledge modeling under partial observability.

For each family, do not ask “is this smart?” Ask:
1. Which problem does it solve?
2. What state/ownership assumptions does it make?
3. What is its computational cost?
4. What failure modes occur at Bannerlord scale?
5. Can native TaleWorlds execution remain the lower layer?
6. What is the smallest shadow-mode proof?

## Research-quality rule

A source can support a mechanism without proving the BannerlordAI adaptation.

Always preserve:
**source claim -> adaptation hypothesis -> local test -> accepted/rejected finding**.
