# First Assignment — Stable Party / Group Identity

**Task ID:** A1  
**Priority:** PRIMARY  
**Reason:** v0.2.10.10 is actively proving natural KNOWN-vs-NEW group recognition. Future memory depends on knowing exactly what “same group” means over time.

## Research question

What identity model should BannerlordAI use to distinguish:
- the same live MobileParty instance;
- the same persistent party across save/reload;
- a party whose leader/owner changes;
- a party that is destroyed and later recreated;
- a successor group with continuity but a different runtime ID;
- a genuinely new group of the same culture/template?

## Required work

### Bannerlord-specific evidence
Map public/modding evidence for:
- MobileParty.StringId lifecycle;
- PartyBase / MobileParty creation;
- bandit/looter/hideout party spawning;
- destruction/removal;
- save serialization;
- clan/leader ownership;
- template IDs;
- party component types;
- target/short-term-target references;
- whether IDs are regenerated or persisted in common cases.

### General prior art
Compare identity strategies:
- object/runtime identity;
- persisted UUID;
- composite natural key;
- lineage/successor identity;
- entity alias table;
- event-sourced identity;
- probabilistic identity matching.

## Deliverable

Produce `research/packets/A1-party-group-identity.md` containing:
- identity taxonomy;
- evidence-backed Bannerlord lifecycle map;
- stability matrix across save/reload/despawn/reform;
- recommended minimal identity key for current patches;
- separate optional lineage key for later “same organization” reasoning;
- collision/false-recognition risks;
- storage/performance cost;
- migration strategy;
- smallest local runtime tests.

## Critical constraint

Do **not** solve future organization/lineage semantics by weakening exact current identity. Exact party recognition and broader “successor/faction/group familiarity” should remain separable layers unless evidence shows otherwise.

## Completion signal

Comment/relay:
- strongest conclusion;
- confidence;
- top 3 sources;
- recommended key;
- biggest unresolved risk;
- smallest runtime test.
