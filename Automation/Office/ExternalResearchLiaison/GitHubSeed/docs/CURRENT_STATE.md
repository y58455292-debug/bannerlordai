# Current BannerlordAI State

**Durable checkpoint:** 148  
**Accepted milestone:** v0.2.10.9 — Naturally later patrol-defense recognition soak  
**Active candidate:** v0.2.10.10 — Natural known-vs-new multi-group recognition soak

## What is already proven

### Persistent lived memory
BannerlordAI has already proved:
- executed player/operator choices can be captured as immutable episodic memory;
- memories survive native SaveAs + full Bannerlord process restart;
- exact branch IDs and episode IDs persist;
- later deliberation can retrieve those memories with provenance;
- bounded memory-derived scoring can be computed in shadow mode without changing native scores.

### Patrol-local reactive defense
We have proved:
- the patrol observer can detect a real local bandit threatening a villager;
- the proposal can remain read-only;
- one bounded native `SetMoveEngageParty` interrupt can target the exact observed bandit;
- patrol can be restored through native `SetMovePatrolAroundSettlement`;
- exactly-once automatic Apply and bounded restore were previously proven under dedicated gates.

### Identity-addressable enemy memory
We have proved:
- separate real patrol-defense episodes can be stored by settlement + bandit identity;
- after full restart, the same exact enemy identity can be retrieved;
- a distinct real enemy identity can return no prior memory;
- recognition can emit `KNOWN_ENEMY` vs `NEW_ENEMY` with no score/intent/behavior writes.

### Naturally later recognition — newest accepted result
In v0.2.10.9:
- remembered Corsairs identity `southern_pirates_47910` was persisted;
- Bannerlord fully restarted;
- campaign time advanced from 648650.9850730833 to 648655.42420875;
- the **real patrol-defense observer** naturally saw that same bandit again;
- it emitted `KNOWN_ENEMY`;
- memory age was **4.4391356667 campaign-hours**;
- it resolved the exact prior episode and source/detail provenance;
- no manual `PATROL_DEFENSE_RECOGNIZE` command occurred;
- behaviorMutation=false, intentMutation=false, scoreMutation=false;
- automatic Apply/rearm remained disconnected;
- protected saves and accepted binaries were restored cleanly.

## Current active question

Can one natural patrol run distinguish **multiple real enemy groups** correctly?

The active v0.2.10.10 gate requires:
- a remembered real bandit -> `KNOWN_ENEMY`;
- a distinct unseen real bandit -> `NEW_ENEMY`;
- both emitted by the normal observer after fresh reload/time advance;
- no manual recognition command;
- no Apply/rearm;
- no behavior/intent/score mutation;
- bounded nonappearance classified as environmental inconclusive, not feature failure.

## Why this matters

This is the foundation for identity-aware memory. We do not want “bandits” as one generic concept. We want the system to know:
- this is **that same group** I saw before;
- this other party is **different**;
- later, memory may inform trust, threat, pursuit, avoidance, retaliation, confidence, or strategic planning—but only after each influence is separately proven.
