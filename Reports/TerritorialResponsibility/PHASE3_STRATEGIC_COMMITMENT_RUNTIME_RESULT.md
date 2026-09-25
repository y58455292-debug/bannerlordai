# Phase 3 Strategic Commitment — Bounded Runtime Result

Date: 2026-09-25
Repository candidate: `681f73e7f96fdd813534e05e3297f178d1eb0aac`

## Result

**BOUNDED NULL.** The tested Strategic Commitment candidate was deployed safely and explicit module-local `Mode=Apply` was confirmed at fresh campaign reset, but the qualifying 72-campaign-hour proof window produced no `STRATEGIC_COMMITMENT_APPLY` event.

Because no Apply retention occurred, no pending commit expectation was created and no `STRATEGIC_COMMITMENT_COMMIT_CHECK` occurred. The required full chain therefore remains unproven in runtime.

No factor, age threshold, coarse-state classification, candidate score, target, action, or strategic category was changed to manufacture a result.

## Deployment safety

Bannerlord was confirmed closed before deployment.

- tested candidate DLL SHA-256: `0267089ABE829248F145A468355041C17A91B8945816A8210BA0C6AD23CB4B5E`;
- prior installed DLL SHA-256: `F8C5E2AE389ADB29034CF18DE6DAA293C6F13571A4AA3F15CABA13E744A0B338`;
- verified rollback path: `D:\BannerlordAIResearch\Builds\Rollback_Phase3_StrategicCommitment_20260925_ClanAI`;
- rollback SHA-256: `F8C5E2AE389ADB29034CF18DE6DAA293C6F13571A4AA3F15CABA13E744A0B338`;
- deployed DLL SHA-256 exactly matched the tested candidate.

Visual War remained disabled; the module-local `ENABLE_VISUAL_WAR_LAB.txt` marker was absent.

## Temporary Apply configuration

Before launch, the installed module-local config was set to:

`ClanAI/Data/StrategicCommitment.cfg`

with:

`Mode=Apply`

SHA-256 during the run:

`2BB156FAD4751D151D0BB232CDED413C720BE1E34DD5C782EEA3ADE823FE4F92`

Fresh campaign telemetry confirmed the installed standalone path and explicit mode were active:

`2026-09-25T06:12:04.3835927Z ... STRATEGIC_COMMITMENT_RESET mode=Apply status=configured_apply factor=1.1 maxAgeHours=12`

Thus the null was not caused by commitment remaining in Observe mode.

The tracked repository config remained unchanged:

`Data/StrategicCommitment.cfg -> Mode=Observe`

## Protected fixture

The protected fixture was loaded read-only:

`ClanAI V020V PERSIST DEMO GATE V021M 20260924`

After the run:
- SHA-256 remained `A91F15BF1403F1D29F942C56A1162F431113942CDACCAFE52F4D80303B4CB427`;
- modification time remained `2026-09-24T17:20:12.2384633Z`;
- no save command was issued.

## Bounded observation

The preregistered proof window was 72 campaign hours.

- start campaign hour: `649491.27044636116`;
- strict 72-hour boundary: `649563.27044636116`;
- first status poll after crossing the boundary: `649563.46863080561`;
- polling overshoot at that first boundary observation: `0.19818444445` campaign hours, about 11.9 campaign minutes.

At that first post-boundary poll there were still:
- `STRATEGIC_COMMITMENT_APPLY = 0`;
- `STRATEGIC_COMMITMENT_COMMIT_CHECK = 0`.

The pause command was issued immediately after that poll. Because TestRunner command consumption is asynchronous, campaign time continued before `PAUSE` was processed; the eventual paused status was `649573.63734283333`. This extra interval is explicitly **outside the qualifying 72-hour proof window** and is not used to extend the experiment.

Importantly, even through that stop-control overshoot and until exit, the current session still had zero Apply and zero commit-check records. Therefore no event occurred after the boundary that could be mistaken for an in-window success.

## Current-session commitment telemetry

For the campaign session beginning with the fresh Apply reset:

- `STRATEGIC_COMMITMENT_RESET = 1`;
- `STRATEGIC_COMMITMENT_APPLY = 0`;
- `STRATEGIC_COMMITMENT_COMMIT_CHECK = 0`;
- `STRATEGIC_COMMITMENT_FAILURE = 0`;
- `STRATEGIC_COMMITMENT_WOULD_RETAIN = 0`.

In explicit Apply mode, a successful retention would have emitted `STRATEGIC_COMMITMENT_APPLY`. None did.

Therefore the proof chain never reached:
- actual 1.10 factor application on an eligible previous native candidate;
- retained previous-objective composer winner;
- pending commit expectation creation;
- later native commit verification.

The null does not identify a narrower gate such as age, same-state classification, candidate availability, or score boundary because the existing production telemetry intentionally logs the positive Apply/commit path rather than every rejection branch.

## Runner chronology

Relevant control records:

- `2026-09-25T06:11:54.6483053Z ... LOAD_SAVE ClanAI V020V PERSIST DEMO GATE V021M 20260924`;
- `2026-09-25T06:12:00.6008370Z ... CAMPAIGN_READY timeControl=Stop`;
- `2026-09-25T06:12:39.3501417Z ... FAST`;
- `2026-09-25T06:14:59.6836218Z ... CLOSE_ESCAPE_MENU`;
- `2026-09-25T06:15:42.1126019Z ... CLOSE_ESCAPE_MENU`;
- `2026-09-25T06:15:47.8760144Z ... FAST`;
- `2026-09-25T06:16:09.5786065Z ... PAUSE`;
- `2026-09-25T06:17:11.8752901Z ... EXIT_NOSAVE`.

The escape menu temporarily stalled time progression. The already-proven close control was used only to resume the same natural observation; no world state, candidate, target, score, or commitment rule was changed.

## Native-authority boundary

The candidate source was unchanged from `681f73e7f96fdd813534e05e3297f178d1eb0aac`.

The accepted deterministic and invariant evidence still applies:
- previous objective must already exist in Bannerlord's candidate list;
- no candidate insertion;
- no synthetic target/action;
- no direct movement/order;
- no behavior assignment;
- no target assignment;
- no faction/settlement/war mutation;
- `StrategicDecisionComposer` remains the score owner;
- `RetentionFactor=1.10`;
- `MaxAgeHours=12`;
- same-state classification remains unchanged;
- Apply remains explicit only.

No source or config semantics were changed during this runtime attempt.

## Cleanup / restoration

After the null:
- `EXIT_NOSAVE` completed;
- Bannerlord was confirmed closed;
- installed module-local config was restored to `Mode=Observe`;
- installed Observe config SHA-256: `B29F5139D1B13C701E6F91D634D5D6D57B709E6BD0933CCD6C7276FB57864398`;
- tracked repository config remained `Mode=Observe`;
- Visual War marker remained absent;
- protected fixture hash/timestamp remained unchanged;
- rollback remained present and verified;
- live DLL remained the tested candidate with SHA-256 `0267089ABE829248F145A468355041C17A91B8945816A8210BA0C6AD23CB4B5E`.

## Conclusion

The bounded Phase 3 Strategic Commitment runtime proof is **not proven** by this run.

Within the strict 72-campaign-hour qualifying window, explicit Apply mode produced no qualifying retention event. Per the protocol, no factor, age bound, coarse-state rule, score, candidate, or scenario was changed to force success.

This null is preserved as the checkpoint. Exact supporting evidence is in `Reports/TerritorialResponsibility/evidence/phase3_strategic_commitment_runtime_null_20260925.txt`.
