# Phase 2B — ruler recruitment / clan courtship protocol

Date: 2026-09-24

## Purpose

Prove that an NPC ruler can actively identify a useful independent clan and initiate recruitment while Bannerlord retains clan acceptance, barter resolution, and final kingdom-join authority.

The first Phase 2B implementation must reuse Bannerlord's normal non-defecting `JoinKingdomAsClanBarterable` path. It must not directly call a faction-transfer action.

## Pre-registered strong win condition

A strong Phase 2B proof requires one natural campaign chain where:

1. an NPC kingdom/ruler reaches ClanAI's ruler-side recruitment consideration;
2. the selected clan is eligible, non-player, non-minor, and independent before the attempt;
3. candidate selection is based on observable native kingdom desirability, not a synthetic forced target;
4. Bannerlord's native clan-side join value is evaluated;
5. Bannerlord's native kingdom-side get-clan value is evaluated;
6. the combined native join surplus is positive;
7. ClanAI initiates only the native AI barter;
8. Bannerlord's native barter/action path commits;
9. post-state confirms the clan is actually in the recruiting kingdom;
10. no direct `ChangeKingdomAction`, relation edit, memory injection, faction edit, or score edit is used.
## Required evidence

For each ruler-side consideration preserve:

- recruiter kingdom and ruler;
- candidate clan and leader;
- candidate pre-state kingdom;
- candidate strength;
- candidate war-party limit;
- candidate holdings count/value where available;
- culture match;
- native kingdom-side get-clan value;
- native clan-side join value;
- native combined value;
- whether the candidate was selected as the ruler's best eligible candidate;
- whether native AI barter was attempted;
- whether the clan joined;
- actual kingdom after;
- candidate version and DLL SHA-256;
- save/fixture and save SHA-256;
- whether prohibited manipulation was used.

A positive kingdom-side score alone is not recruitment proof. An initiated barter without a join commit is a useful null, not a completed recruitment behavior.
## First implementation boundary

Before attempting a join action, measure the natural independent-clan candidate landscape for NPC kingdoms.

The initial selector should prefer the highest **native** `GetScoreOfKingdomToGetClan` among eligible independent clans and should not invent a new desirability scale.

The first behavior-changing courtship candidate should use the same native eligibility conditions where practical and should execute `JoinKingdomAsClanBarterable` through `BarterManager.ExecuteAiBarter` only when the native combined value is positive.

Do not add war-strain, border-need, memory, or custom relation bonuses until the base ruler-initiated native loop has been characterized.

## Null outcomes

Preserve as evidence:

- no eligible independent clans;
- kingdom wants a clan but the clan-side value makes combined surplus non-positive;
- native barter is attempted but no join commits;
- selected candidate changes because native conditions change;
- join commits natively without any custom score modification.

The last case would still prove ruler-initiated courtship even though no extra ClanAI score was needed.
