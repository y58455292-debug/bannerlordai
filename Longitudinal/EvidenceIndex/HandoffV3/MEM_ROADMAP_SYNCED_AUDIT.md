# BannerlordAI Cross-Chat Roadmap & Operating Contract v3

Purpose: durable cross-chat rules and roadmap. Exact live hashes/processes/test state are NOT sourced from Mem; they are recovered from the user's PC through HandoffV3.

## START-OF-CHAT RULE — NON-NEGOTIABLE
1. Run: python -X utf8 D:\BannerlordAIResearch\Tools\Handoff\resume_state.py
2. Read: D:\BannerlordAIResearch\Longitudinal\EvidenceIndex\HandoffV3\current.json
3. Inspect inherited Bannerlord/TestRunner processes before launching anything.
4. Reconcile newer reports/validation/staging when recovery warns of drift.
5. Continue the recorded next action only after recovery.
Legacy HOT/context-pyramid files and Mem summaries may supplement but never override newer local disk/runtime evidence.

## RESPONSIBILITY SPLIT
Mem = durable roadmap, architecture doctrine, work routines, stop rules, accepted milestone summaries, and do-not-repeat rules.
HandoffV3 current.json = volatile resume capsule: installed build/hash, active candidate/hash, run state, blockers, decisive evidence, exact next action.
events.jsonl + DerivedReports + LiveValidation + raw telemetry = proof/history.
Computer/runtime inspection = final authority for what is actually running/installed.

## CONTINUOUS HANDOFF RULE
Never wait until chat close. Checkpoint BEFORE every live deployment/test; AFTER every test/exit; after accept/reject/rollback; after blocker changes; whenever exact next action changes materially.
The project must be resumable after any tool call.

## ACTIVE-WORK AUTONOMY RULE
During an active turn, continue safe, authorized, reversible project steps without repeatedly asking the user for routine continuation.
Ask only when user input/permission is genuinely required or a choice materially changes intent.
Never claim work continues after the response ends unless a real automation/process was launched.

## USER UPDATE ROUTINE
For long work, give concise progress updates without waiting for a reply before continuing safe work.
Surface milestone reached, blocker, decisive finding, or plan change—not low-level tool spam.

## ANTI-REPETITION RULES
Never rerun a passed milestone because chat/Mem is stale.
Never launch overlapping live tests.
Never infer installed build from filenames; verify DLL hash.
Never mix NATURAL, INDUCED, RESPONSE.
Never treat candidate score change as committed behavior or reassertion as a new decision.
Never reintroduce multiple strategic score writers.
Never trade game/graphics quality for diagnostics.
Performance and save integrity are correctness criteria.

## ENGINEERING LOOP
Prior art -> exact hypothesis -> smallest intervention -> instrumentation -> offline gate -> controlled runtime test -> freeze evidence -> accept/reject -> rolling checkpoint -> roadmap update.
One new architectural capability at a time. Preserve rollback. Retire answered probes.
If a blocker recurs twice, build a guard/preflight/adapter.

## CURRENT ROADMAP — 2026-09-18
Accepted:
- v0.20M StrategicDecisionComposer: single strategic SetBehaviorScore owner.
- v0.20N Actor Blackboard shadow: ACCEPTED. 100 captures, 100 validations, 48 reuses, 0 blackboard mismatches/failures; composer clean; no-save integrity preserved.
Blackboard boundary: authoritative runtime facts only. Memory/personality/emotion/appraisal/commitment/role/planner-predicted state stay separate. Initiative/pursuit gate uses direct native reads at its distinct timing boundary.

Active candidate:
- v0.20O-review Actor Blackboard Consumption.
- Built offline; live validation pending.
- Goal: VisualWar + weak-recovery consume validated same-boundary blackboard facts while preserving exact formulas/winners, composer ownership, performance, and no-save integrity.
- Gate: >=100 captures/validations; nonzero VisualWar + weak-recovery blackboard reads; zero blackboard/composer mismatches/failures; clean writer; no material performance regression; byte-identical save after native no-save exit.

After v0.20O if accepted:
- compact actor strategic state / commitment / hysteresis architecture, prior-art-first;
- preserve separation between authoritative facts and memory/personality/emotion/commitment;
- use clean vanilla sandbox lab for causality and mature stress world for robustness;
- introduce capabilities incrementally, not state+commitment+planning simultaneously.

## ROADMAP UPDATE RULE
After each accepted/rejected milestone, update this Mem contract AND local HandoffV3 in the same work session.
If Mem update fails, HandoffV3 remains authoritative and the pending Mem payload stays on disk until any later agent syncs it.

## PATHS
Protocol: D:\BannerlordAIResearch\Longitudinal\EvidenceIndex\HandoffV3\PROTOCOL.md
Current: D:\BannerlordAIResearch\Longitudinal\EvidenceIndex\HandoffV3\current.json
Previous: D:\BannerlordAIResearch\Longitudinal\EvidenceIndex\HandoffV3\previous.json
Events: D:\BannerlordAIResearch\Longitudinal\EvidenceIndex\HandoffV3\events.jsonl
Recovery: D:\BannerlordAIResearch\Tools\Handoff\resume_state.py
Checkpoint: D:\BannerlordAIResearch\Tools\Handoff\handoff_checkpoint.py
