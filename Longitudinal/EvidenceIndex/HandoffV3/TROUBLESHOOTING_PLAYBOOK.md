# BannerlordAI Troubleshooting Playbook
Updated 2026-09-19 from live Manan autonomous tests.

## Core rule
Never equate a reported state, command acknowledgement, or UI appearance with successful effect.
Require the lowest practical authoritative outcome signal.

## Evidence ladder
1. Native world/runtime outcome
2. Low-level engine counters / clocks
3. Controller state
4. Command acknowledgement
5. UI appearance / assumptions

## Lessons promoted from live failures

### 1. Command accepted != effect executed
PATROL_SETTLEMENT was accepted while Manan remained inside Syronea because town_wait_menus blocked physical execution.
Proof requires time/position/settlement-state change.

### 2. TimeControlMode != time progression
Bannerlord can report UnstoppablePlay while MapTimeTracker.NumTicks is frozen and DeltaTimeInTicks=0.
Use MapTimeTracker.NumTicks / CampaignTime.Now change as authoritative progress proof.

### 3. Fresh status != UI readiness
v0.2.5 wrote status immediately on module load. The launcher interpreted this as main-menu readiness and fired LOAD_SAVE too early.
Main-menu launch readiness must verify actual GauntletInitialScreen before exact save load.

### 4. UI systems are distinct
Do not collapse GameMenu, Inquiry, Incident, Conversation, Gauntlet overlay, Mission into one "menu" abstraction.
Normalize them behind a UI State Router but preserve source type and legal executor.

### 5. Incident popup != Inquiry
Vanilla 1.3.x Incidents use MapIncidentView/GauntletMapIncidentView and are invisible to InformationManager.IsAnyInquiryActive().
Capture Incident directly from MapIncidentView.

### 6. Decision popup may not stop world time
An Incident can visually demand input while campaign time continues.
Autonomous controller must pause on detected decision-required UI before reasoning.

### 7. Snapshot before resume
After INCIDENT_SELECT, AUTO_RESUME occurred immediately and normal campaign ticks contaminated outcome measurement.
Correct sequence:
pause -> before snapshot -> decide -> execute -> immediate after snapshot -> memory/outcome update -> resume.

### 8. Hydration is asynchronous
Immediately after exact load, player party briefly reported 0 members, then stabilized at 213.
Never validate campaign state from the first readable frame; require hydration/stability checks.

### 9. Compile pass != runtime proof
Harmony introduced a System.ValueTuple warning but build succeeded.
Any new runtime dependency requires live validation before acceptance.

### 10. Focus hypothesis must be tested, not assumed
Window focus was suspected when time froze. AppActivate + 3-second sample disproved it.
Record ruled-out hypotheses to prevent repetition.

### 11. Reasserting intent may not clear engine residue
Reissuing PATROL_SETTLEMENT and PLAY did not clear IsMainPartyWaiting=true.
Do not repeatedly spam commands after a failed effect; inspect deeper native state.

### 12. Low-level clock is a stall detector
If expected-progress mode is active and MapTimeTracker.NumTicks is unchanged across watchdog window:
classify NO_PROGRESS even if blocker list is empty.

### 13. Unknown remains unknown
A stale IsMainPartyWaiting=true outside Syronea is currently observed, not yet explained.
Do not promote a root cause until the native setter/transition path is identified.

### 14. Live patch boundary
Prefer fixing:
- weights
- scoring
- loop thresholds
- controller sequencing
- telemetry cadence
- menu/incident rules
outside the DLL while game stays open.
Restart only for missing engine capability / bridge surface.

### 15. Video is evidence
Record major live tests and align video timestamps with runner UTC and monotonic telemetry.

## Standard troubleshooting loop
1. Observe symptom.
2. Freeze destructive/world-changing progression if necessary.
3. Capture authoritative baseline.
4. State one hypothesis.
5. Make one minimal intervention.
6. Re-measure authoritative effect.
7. Mark hypothesis confirmed/rejected.
8. Promote reusable lesson or fix.
9. Resume only after state is coherent.

## Current unresolved live issue
Observed:
- Manan outside Syronea
- CurrentSettlement=<none>
- TimeControlMode=UnstoppablePlay
- TimeControlModeLock=false
- SpeedUpMultiplier=4.0
- IsMainPartyWaiting=true
- MapTimeTracker.NumTicks frozen
- DeltaTimeInTicks=0
- PLAY does not clear
- reasserted PATROL_SETTLEMENT does not clear
- focus change does not clear

Next: identify the native state transition/setter that clears main-party waiting after incident close; prefer invoking the engine's normal path over direct field mutation.


## HOT promoted architecture — 2026-09-19

### A. Deterministic Decision Replay Lab
Capture normalized decision/state packets from live play and replay the external controller offline.
One live incident/failure becomes a permanent regression fixture.
Target:
- world/actor/UI/input snapshot
- controller policy version
- action sequence
- expected invariants
- observed result
- timing trace

### B. Model-based state-machine fuzzing
Use property/state-machine testing patterns to generate controller transition sequences and shrink failures to minimal reproductions.
First target sequence family:
LOAD_SAVE -> WAIT/STOP_WAITING -> LEAVE -> INCIDENT_OPEN -> INCIDENT_SELECT -> RESUME.
Invariant example:
AUTO_PLAY + no blockers + no active decision => MapTimeTracker.NumTicks must advance within watchdog window.

### C. Preloaded runtime patch control plane
Prefer a stable generic Harmony bridge with runtime-enable/disable controls over repeated DLL rebuilds.
Use explicit Harmony patch IDs/order/priority to diagnose and avoid mod conflicts.
New engine capability may still require restart; tuning/diagnostic hooks should not.

### D. Failure packet
Every detected invariant failure should automatically emit one compact packet:
- monotonic timestamp
- game time
- controller state
- engine low-level state
- active UI type
- current intent
- last N transitions
- error groups
- relevant hashes/policy version
Then freeze or run bounded recovery according to severity.


## RED ALERT — iteration and recovery latency

Treat these as first-class engineering metrics, not convenience work.

### 1. Command provenance / stale-command rejection
Every control command must carry:
- run_id
- issuer/controller id
- monotonic sequence
- wall timestamp
- expected campaign id
- expected runner version

Reject stale, duplicate, wrong-run, or wrong-campaign commands.
Unexpected exits must be attributable to an exact issuer and sequence.

### 2. One-operation exact recovery
Target workflow:
preflight -> deployment lease -> deploy only if hash differs -> launch -> real main-menu readiness -> exact named save -> hydration/stability check -> restore watcher -> restore intended autonomy -> ready.

Measured current exact Manan launch baseline: ~43 s.
First target: 25–35 s without weakening save/hash/identity checks.

### 3. Bug turnaround targets
- external policy/controller defect: seconds to 2 min
- bridge/DLL defect: 3–5 min target
- behavioral rescore/incident response: sub-second target already demonstrated

### 4. Restart avoidance
Prefer external/live-edit repair whenever capability already exists.
Restart only when the stable in-game bridge lacks a required engine capability.

### 5. Automatic failure capsule
On invariant failure or unexpected process exit, capture:
run_id, issuer, last commands, runner/hash versions, low-level engine state, UI state, campaign clock, video timestamp, and cause classification before recovery.

## RED ALERT — live-edit-first runtime policy

Default: keep Bannerlord open.

Hot-edit externally without restart whenever the existing bridge already exposes the needed capability, including:
- reasoning weights and scoring
- food/logistics pressure
- border/threat evaluation
- clan-party coverage reasoning
- sticky/temporary intents
- memory and lessons
- cooldowns and loop prevention
- decision timing
- watchdog/recovery policy

Restart only for a bridge/DLL capability change, module deployment, or engine hook that cannot be provided by the existing live interface.

EXIT_NOSAVE is a privileged operation. Test helpers must not use it as routine closeout. A live field run should continue after a bounded test unless a restart is technically required or the user explicitly wants the game closed.

Behavior pressures are inputs to reasoning, not hard restraints. Example: low food raises logistics urgency but does not force a fixed destination; the controller should weigh food days, nearby supply, border threat, clan-party coverage, morale, distance, enemy pressure, and current commitments before choosing an action.
