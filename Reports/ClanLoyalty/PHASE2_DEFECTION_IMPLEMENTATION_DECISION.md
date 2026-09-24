# Phase 2A — target-defection implementation decision

Date: 2026-09-24

## Decision

Make one behavior-changing implementation attempt that mirrors Bannerlord's native defection score composition.

Do **not** add raw fief-loss pressure directly to target defection. Do **not** increase the existing 25,000 target-social cap.

Instead:

1. measure Bannerlord's native `GetScoreOfClanToLeaveKingdom(clan, currentKingdom)` during the natural `ConsiderDefection` path;
2. compute the already-proven ClanAI voluntary-leave memory modifier against that native leave score;
3. carry that modifier into the clan-side defection value because Bannerlord itself already embeds the native leave score in `JoinKingdomAsClanBarterable`;
4. compute target-kingdom social desirability separately from **target-specific** memory only, under the existing 25,000 absolute cap;
5. let Bannerlord retain target selection, eligibility, affordability, barter resolution, and the final `ApplyByJoinToKingdomByDefection` action.

This avoids double-counting current-kingdom social memory.
## Evidence basis

Committed evidence already establishes:

- initial Phase 2 sample: closest natural adjusted sum -169,423, current 25k social cap too small for the observed boundary;
- loss-context audit: real fief-loss memory overlaps natural defection considerations in 4/26 cases, but adding raw loss pressure produced no counterfactual flip; closest remained -54,501;
- native score map: Bannerlord's clan-side defection value explicitly contains a native leave-current-kingdom component;
- embedded-leave observation: the exact native leave subscore can be measured safely for natural defection considerations;
- bounded M2 run: 13/13 native leave subscores observed, but no active leave-memory modifier happened to be sampled in that window.

The M2 null does not invalidate the design because the earlier committed loss-context run already proves that loss-bearing clans can naturally reach `ConsiderDefection`. It only shows that the overlap is sparse.
## Formula for the single implementation attempt

Conceptually:

`targetSocialModifier = bounded_target_specific_memory_only`

`leaveMemoryModifier = proven_leave_memory(nativeLeaveValue, currentKingdom)`

`adjustedClanValue = nativeClanValue + targetSocialModifier + leaveMemoryModifier`

The target-social cap remains:

- relative cap: 0.25;
- absolute cap: 25,000;
- minimum meaningful cap: 5,000.

The leave-memory calculation keeps its already-proven bounds; no new larger constant is introduced for defection.

Logs must expose at least:

- native clan value;
- native leave value;
- target-social modifier;
- leave-memory modifier;
- total modifier;
- target value;
- native/adjusted sums;
- native/adjusted demand;
- affordability;
- nativeWouldDefect;
- adjustedWouldDefect;
- committed;
- actual kingdom after.
## Timeboxed acceptance

Run one natural proof attempt after deployment.

Success remains the preregistered boundary:

- natural native NO;
- adjusted YES;
- affordability satisfied;
- Bannerlord commits the native target-kingdom switch;
- post-state confirms the target kingdom.

If that exact shape is not observed in the bounded validation attempt, preserve the null/blocker and end Phase 2A for now. Do not continue adding instrumentation or tuning this surface in an open-ended loop. Resume the next roadmap milestone after the mission-boundary GitHub sync.
