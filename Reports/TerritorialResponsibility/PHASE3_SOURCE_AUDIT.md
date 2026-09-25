# Phase 3 Territorial Responsibility — Offline Source Audit

Date: 2026-09-25  
Repository checkpoint: GitHub `main` at `45c84325b911d91767d7188e06e0b491e0c165d0`

## Scope

This is a source-only audit of the existing home-responsibility decision path as the first offline Phase 3 task. It does not claim new campaign behavior or runtime proof. Phase 2B and Phase 2C were not repeated, and Phase 2D-L1 gameplay/ledger design was not changed.

## Existing native-authority boundary

`src/ClanAI/src/ClanAI/ClanAIStrategicBehavior.cs` installs a postfix on Bannerlord's native `CampaignEventDispatcher.AiHourlyTick(MobileParty, PartyThinkParams)`. The existing `HomeResponsibilityLayer.Apply` in `src/ClanAI/src/ClanAI/HomeResponsibilityLayer.cs`:

- returns unless the actor is a non-player party with a clan and `WorldScopeContext.EligibleIndependentLordAtWar(actor)` is true;
- considers only candidates Bannerlord already supplied in `PartyThinkParams`;
- considers only positive-scoring settlement candidates whose owner clan matches the actor's clan;
- applies bounded factors to existing native scores: 1.60 for an owned home settlement under siege/raid, 1.35 for going home while weak, 1.30 for defending home during war, 1.25 for patrolling around home during war, and 1.15 for going home during war;
- does not create candidates, transfer settlement ownership, issue a direct party order, or replace the native behavior selection;
- when its contribution changes the winning candidate, stores a pending expectation and later checks the party's native default/short-term behavior and target; successful home-duty memory is recorded only when that observed state matches.

These are bounded score preferences, not proof that the territory is safe or that the desired action always commits. The existing Phase 0 Home Responsibility evidence remains the accepted proof for that prior feature; this audit adds no runtime claim.

## Phase 3 gaps visible from source

The current path is intentionally narrower than the Phase 3 roadmap:

- it is gated to eligible independent NPC lords while their clan is at war;
- it can only prefer already-present native candidates targeting the actor clan's own settlements;
- it does not cover ruler-level assignment of campaign, reserve, border-defense, escort, or patrol roles;
- it does not add independent target generation for threatened territory and does not claim to alter settlement ownership or native action legality.

No code defect is established by this audit. These are scope gaps for the next roadmap work, not a reason to broaden the current layer blindly.

## Next offline implementation slice

Add focused deterministic tests around the pure decision portion of `HomeResponsibilityLayer`, extracting only the minimum helper needed to test eligibility and score factors without Bannerlord runtime objects. Preserve the native candidate list, existing owner/war/weak-state gates, the current factor values, and the post-vanilla commit check. Cover owned versus foreign settlement candidates, threatened owned settlements, weak recovery, and ordinary home/patrol/defense candidates. Run the focused tests and Release build offline. Do not deploy or claim campaign behavior from those tests. A later runtime check must use the established Inspector/TestRunner interface after it is available; it must not run against or disturb the current live session.
