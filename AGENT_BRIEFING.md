# BannerlordAI — Briefing for External Review
Generated: 2026-09-24T17:08:42-07:00

## The goal, in one sentence

Create a long-lived Bannerlord campaign where clans, rulers, companions, and kingdoms act from persistent world state and memory while Bannerlord's native eligibility, decisions, ownership, and action systems remain authoritative.

## Current status, in one sentence

The integrated mod has passed its playable demo gate; ruler recruitment and independent-clan activity are runtime-proven, while the new kingdom-continuity ledger is implemented/build-proven and initializes correctly but still lacks a successful save/reload proof and a naturally observed succession or kingdom-destruction notice.

## What is proven to work (with evidence, not claims)

### Integrated campaign load, time progression, save, and reload
- The protected baseline loaded successfully after the runner waited for the real main-menu screen.
- Campaign time then advanced 100.987 hours without a crash or unresponsive state.
- A separate guarded demo save was created, exited, reloaded, and advanced another 2.406 campaign hours while responsive.
- Restored state after reload included WarState, SocialLedger, direct clan-loss memory, companion duty memory, experience memory, negative-outcome memory, and the Mercy threshold.
- Exact runtime messages in the same window included:
  - `PLAYER_VISIBILITY message=ClanAI: Khuzait war strain: 2% (0 active wars, 0 besieged holdings, 3% scar load).`
  - `PLAYER_VISIBILITY message=ClanAI: fen Morcar's loyalty is shaken by the loss of Rhemtoil Castle.`
- Evidence: `Reports/Demo/PHASE1_DEMO_GATE_RESULT.md`.

### Prisoner Mercy makes opposite decisions on opposite sides of the threshold
- Natural capture event above the threshold: score 21.7 -> RELEASE.
- Commit proof: `wasPrisoner=True ... isPrisonerAfter=False released=True`.
- Natural capture event below the threshold: score 19.8 -> KEEP, with zero matching release commits.
- Evidence: `Reports/PrisonerMercy/evidence/MercyThresholdBoundary_20260924/validation.json`.

### A real holding loss can change a clan's native leave outcome and commit the departure
- Banu Ruwaid naturally lost Vladiv Castle by siege.
- At the later native leave check: native value -250,332; bounded memory modifier +312,915; adjusted value +62,583.
- Exact decision proof: `nativeWouldLeave=False`, `adjustedWouldLeave=True`, `committed=True`, `kingdomAfter=independent`.
- Read-only Inspector verification also showed the clan with no kingdom.
- Evidence: `Reports/ClanLoyalty/PHASE1_BOUNDARY_RESULT.md`.

### Player-visible political feedback corresponds to real campaign events
- Natural loss message: `PLAYER_VISIBILITY message=ClanAI: fen Morcar's loyalty is shaken by the loss of Rhemtoil Castle.`
- Natural loyalty-shift message: `PLAYER_VISIBILITY message=ClanAI: Gundaroving is wavering in Sturgia after recent events.`
- Native leave-commit message: `PLAYER_VISIBILITY message=ClanAI: Gauting has left Nord after mounting losses and grievances.`
- The Gauting commit behind that message moved from native -401,733 to adjusted +100,433 and ended with `committed=true`.
- Evidence: `Reports/Visibility/2026-09-24-loyalty-runtime-proof.md`.

### NPC rulers can recruit an independent clan through Bannerlord's native barter
- Vlandia selected naturally independent Banu Ruwaid.
- Native clan value +649 plus native kingdom value +7,401 produced +8,050 combined.
- Exact runtime proof recorded `attempted=True`, `committed=True`, and `kingdomAfter=Vlandia`.
- Negative native offers were not forced: Sturgia's combined value was -30,774 and Aserai's was -4,281; neither attempted barter.
- No custom score, relation edit, wealth edit, direct faction mutation, or direct kingdom-change action was used.
- Evidence: `Reports/ClanRecruitment/PHASE2B_RUNTIME_RESULT.md` and its consideration log.

### Independent clans remain active and can recover without custom spawning or recruitment
- Banu Ruwaid stayed independent for 81.003 campaign hours with two active lord parties.
- Initial combined healthy troops: 195.
- Final combined healthy troops: 205.
- During the observation the parties used settlements, replenished food, and one party initiated and won a native field battle against Looters.
- Final Inspector result: 0 total errors, 0 distinct errors, 0 fatal errors.
- Evidence: `Reports/IndependentClans/PHASE2C_RUNTIME_RESULT.md`.

### The kingdom-continuity behavior is registered and reconciles live native state without political mutation
- Focused invariant checks pass and the Release build succeeds with zero errors.
- On campaign load the exact runtime line was `KINGDOM_CONTINUITY_RECONCILED active=7 records=7 mutation=False`.
- The campaign then advanced 76.401 hours with zero Inspector errors.
- No ruler, kingdom, settlement, faction, war, or timer state was changed by the behavior.
- This proves registration and initial reconciliation only; it does not yet prove persistence or event notices.
- Evidence: `Reports/KingdomContinuity/PHASE2D_L1_RUNTIME_RESULT.md`.

## What is currently broken

### TestRunner save materialization for the kingdom-continuity validation
- Symptom: TestRunner logged `SAVE_TEST_BEGIN` and `SAVE_TEST_RETURNED`, but no new save file appeared.
- Final verifier result: `SAVE_TEST_FAILED ... reason=verify_timeout`.
- Last seen: the current kingdom-continuity runtime validation, after the seven-record reconciliation.
- Already ruled out: compile failure, campaign-load failure, ledger-registration failure, and an overwritten protected baseline; the build passed, the campaign loaded, seven records reconciled, and the protected baseline remained untouched.
- Not yet ruled out: whether the save request is queued/asynchronous, blocked by game/menu state, or verified against the wrong completion signal/path.

### Native menu state can block unattended fast-forward
- Symptom: the runner repeatedly encountered the town-wait / escape-menu boundary and could not continue unattended time acceleration.
- Last seen: the same 76.401-hour kingdom-continuity observation.
- Already ruled out: an ordinary crash or Inspector-detected runtime exception; the run remained healthy with zero Inspector errors and was paused at the native menu boundary.

## What was tried and abandoned, and why

### Open-ended target-kingdom defection tuning
- A bounded natural attempt ran 486.070 campaign hours and produced 27 native defection considerations.
- Result: four non-zero target-memory modifiers, zero non-zero leave-memory carries, zero adjusted YES decisions, and zero committed target switches.
- Closest observed candidate remained -180,954 even though affordability was satisfied.
- This path was timeboxed rather than repeatedly increasing caps or manufacturing overlap; target-kingdom autonomous defection remains unproven.
- Evidence: `Reports/ClanLoyalty/PHASE2_DEFECTION_M3_ATTEMPT_RESULT.md`.

### Issuing LOAD_SAVE immediately after module load
- Symptom was `command_failed_NullReferenceException`.
- Waiting until BannerlordInspector reported the actual main-menu screen made the same protected baseline load successfully.
- The premature sequence was abandoned as a development-runner sequencing error, not treated as a ClanAI campaign-load defect.
- Evidence: `Reports/Demo/PHASE1_DEMO_GATE_RESULT.md`.

## The single most useful next step

Trace TestRunner's `SAVE_TEST` path from request dispatch to actual save-file materialization, fix only that completion/verification fault, then create and reload one uniquely named continuity test save to prove all seven records restore with no duplicate notices and without touching the protected baseline.

## Open questions for the reviewer

Does `SAVE_TEST_RETURNED` mean Bannerlord has actually completed the save, or only that the request was accepted/queued; and what native completion signal or file-state should TestRunner wait for before declaring the save successful?
