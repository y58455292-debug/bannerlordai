# Phase 1 — clan-leave boundary protocol

Date: 2026-09-24

## Purpose

Prove or fail to prove the behavioral boundary for memory-driven voluntary clan departure without changing ClanAI code first.

The test must engineer world conditions rather than manipulate the leave decision directly. A useful campaign candidate is a non-player, non-minor, non-ruling border clan whose native `ConsiderClanLeaveKingdom` value is roughly 150,000–250,000 below zero and which owns one or more realistically losable castles or towns.

## Pre-registered win condition

A run is a success only when all of the following occur in the same natural campaign chain:

1. The clan becomes eligible and Bannerlord naturally executes `ConsiderClanLeaveKingdom`.
2. The triggering loyalty memory comes from natural world events; no relation edit, direct score edit, forced faction transfer, or synthetic holding-loss event is used.
3. `adjustedValue > 0` while the corresponding native pre-memory leave value is <= 0.
4. Bannerlord's native leave path commits and the clan's kingdom changes through the normal `LeaveKingdomAsClanBarterable.Apply` / `ChangeKingdomAction` path.
5. The evidence records the native value, memory contribution, adjusted value, old kingdom, kingdom after, and commit result.

Until all five conditions are met, the claim "clans leave from memory" remains unproven.
## Non-success / null outcomes

The following do **not** satisfy the win condition and must be retained as null evidence:

- memory changes a leave value but the adjusted value remains <= 0;
- adjustedValue crosses >0 but Bannerlord does not commit the native leave action;
- the clan leaves through player barter/persuasion or any direct scripted faction transfer;
- a holding loss is manufactured rather than produced by a real siege/ownership change;
- a clan is sampled only after the relevant memory has expired;
- a target clan is millions of points below the boundary and therefore unsuitable for a boundary proof.

## Campaign engineering rule

World engineering may select a save/campaign, choose a naturally disloyal clan, and prefer a border situation with exposed holdings. It may advance time and observe native wars/sieges. It may not edit the target clan's loyalty score, relations, kingdom membership, holdings, or the recorded loss event merely to force the threshold.

If no suitable ~150k–250k candidate exists in the available saves, record that null and find/create a better natural campaign fixture before changing loyalty code.

## Deferred work

No Phase 2 feature build or small code fix begins until this boundary attempt is either proven or recorded as an explicit Phase 1 null/blocker. The requested later code fixes remain queued, not applied.
