# BannerlordAI — Tomorrow Handoff

## Boot state
- Installed ClanAI: v0.20R dynasty-mind observe candidate
- ClanAI SHA256: 0DBC936234E29CE50F93107EF885090A2DF1C6C2BAB8125C51C4FE8EB4FAB363
- Bannerlord: closed
- Deployment lease: released
- Protected Manan clone + BLOOD FUED original: unchanged SHA256 94DFE7F05D89C6E4CC328A416943608713ECFFDF96A572AAEA43A9BDD5CD2DAA
- Live testbed: one evolving dynasty branch. Do not resume save hunting for specific events.

## Tonight accepted
v0.20R canon bootstrap observe proof PASSED.
- Exact Manan root: Manan, Spring 4 1126, 213 troops.
- Canon cutoff: 648650.98507308331.
- Parsed 15; available 14; futureHidden 1; malformed 0.
- Manan available memories: 8.
- scoreMutation=False; futureCanonLeak=False.
- Sole hidden future item: Vadrios / succession / inheritance_responsibility / knownByHours 649331.3949947777.
- Live branch advanced 13.8741193055 campaign-hours with no bootstrap/composer/blackboard/commitment failures and clean EXIT_NOSAVE.
- Evidence: D:\BannerlordAIResearch\Longitudinal\LiveValidation\DynastyMind_v020R_LivePass_20260919.json

## Main thing that needs work
PLAYER/OPERATOR MEMORY RETRIEVAL GAP.
The memories are loaded, but dynastyMindRetrievals remained 0 during the player-controlled Manan run because the current retrieval hook is on AI-hourly decision context.

Do not enable memory score effects yet.

## First actions tomorrow
1. Add a read-only high-value retrieval hook to the player/operator deliberation path (strategic command, incident choice, kingdom decision when present).
2. Produce a deliberation receipt: actor, decision context, retrieved memory keys/provenance, proposed feature deltas, but NO score write.
3. After shadow evidence looks sane, enable only a bounded causal Apply path.

## Workday automation plan
When the user is awake, configure the automation together.
Goal: run the evolving dynasty as a safe field test while the user is at work and can still chat.
- Single deployment owner/controller.
- Native TaleWorlds execution.
- Monitor strategic movement, incidents, memory retrieval, diplomacy whenever it naturally appears, world-state adaptation, performance and stalls.
- Treat events as opportunities; never require a particular historical event.
- Stop/notify on unknown blockers, integrity drift, repeated freeze, or unsafe decision boundary.
- Use disposable/no-save or explicitly approved checkpoint saves; never overwrite benchmark originals.
- Keep concise live receipts and checkpoint meaningful changes.

## Resume
@Remote Desktop Commander follow protocol
Then read this file only if the compact state does not already contain the above.
