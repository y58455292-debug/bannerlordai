# Phase 2 — target-kingdom defection boundary protocol

Date: 2026-09-24

## Purpose

Determine whether real ClanAI social memory can naturally change Bannerlord's target-kingdom defection decision from NO to YES and have Bannerlord commit the kingdom switch.

This protocol applies to the separate `DiplomaticBartersBehavior.ConsiderDefection(Clan, Kingdom)` / `JoinKingdomAsClanBarterable` surface. The voluntary leave-current-kingdom path is already proven and is not the subject of this experiment.

No defection behavior or cap is changed before the natural boundary is characterized.

## Pre-registered win condition

A run is a success only when all of the following occur in one natural campaign chain:

1. Bannerlord naturally invokes `ConsiderDefection`.
2. The source clan has a real current kingdom and Bannerlord naturally supplies a different target kingdom.
3. Relevant ClanAI memory comes from real world/social events.
4. The native decision is NO.
5. The adjusted decision is YES.
6. The affordability requirement remains satisfied after adjustment.
7. Bannerlord's original barter/action path commits.
8. Post-state proves the clan is actually in the target kingdom.
9. ClanAI does not directly call a faction-transfer action.
10. No synthetic relation, memory, barter-score, kingdom-membership, or target-selection edit is used.
11. The 25,000 absolute social-defection cap is not increased merely to cross the boundary.

Until every condition is met, target-kingdom autonomous defection remains unproven.

## Required evidence fields

Each candidate receipt must preserve, where available:

- clan;
- leader;
- old kingdom;
- target kingdom;
- native clan value;
- target kingdom value;
- native sum;
- ClanAI modifier;
- adjusted clan value;
- adjusted sum;
- native demand;
- adjusted demand;
- affordable amount;
- nativeWouldDefect;
- adjustedWouldDefect;
- committed;
- actual kingdom after;
- natural event provenance for relevant memory;
- candidate version and DLL SHA-256;
- save/fixture and save SHA-256;
- whether any prohibited manipulation was used.

For a winning case, post-state membership must be independently readable after the native action.
## Natural-boundary search

Before any algorithm change, collect natural `SOCIAL_DEFECTION_CONSIDER` samples from the current candidate.

Rank useful candidates primarily by distance of `adjustedSum` from the required `> 0` score boundary, while separately checking affordability.

Preserve:

- total considerations;
- total non-zero memory modifications;
- positive and negative modifiers;
- closest native sum;
- closest adjusted sum;
- native/adjusted affordability failures;
- nativeWouldDefect count;
- adjustedWouldDefect count;
- committed count;
- representative closest candidates, including nulls.

The search should answer whether the limiting factor is score distance, affordability, target selection frequency, memory sparsity, or another measured condition.
## Scale-mismatch rule

The current social defection modifier remains bounded by its existing relative cap and 25,000 absolute cap during characterization.

If natural evidence shows that suitable candidates remain systematically far outside the reachable range, stop and write an evidence-backed design note before changing code.

Do not replace the 25,000 cap with a larger arbitrary constant.

Any later scale change must be attributable to real world events, bounded, explainable in native Bannerlord value units where possible, independently logged, and incapable of directly forcing `ChangeKingdomAction`.

## Non-success outcomes

The following are explicit nulls, not proof:

- a non-zero modifier with both native and adjusted decisions still NO;
- an adjusted sum above zero when affordability fails;
- a score flip without an actual native kingdom switch;
- a native commit that would have happened without ClanAI memory;
- a clan joining through player barter/persuasion;
- any result produced after direct relation/memory/score/faction manipulation.

Nulls must remain in the repository evidence.
## Initial candidate

The experiment begins on `v0.21M-player-visibility-v1` with no defection behavior change.

The existing L2 evidence remains the historical baseline:

- 18 natural defection considerations;
- 4 modified;
- 0 positive flips;
- 0 commits;
- closest observed boundary approximately 448,000 below zero.

The first Phase 2 run must measure the current natural distribution again rather than assuming that historical sample still represents the present campaign.
