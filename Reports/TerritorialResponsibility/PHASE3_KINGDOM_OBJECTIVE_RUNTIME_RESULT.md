# Phase 3 Kingdom Objective — Runtime Result

Date: 2026-09-25
Repository candidate: `96813b4e9fe4951118f2dc5b4427bffc4cb58fc1`

## Result

**PASSED.** A natural ruler objective changed Bannerlord's existing native AI candidate winner through the existing `StrategicDecisionComposer` path, and Bannerlord then committed the selected behavior/target. The existing post-vanilla verification reported `matched=True`.

No factor, threshold, margin, cap, target, candidate, action, or objective was synthetically changed for this run.

## Deployment safety

Bannerlord was confirmed closed before deployment.

- candidate Release build: 0 errors; inherited `System.ValueTuple` warning only;
- candidate DLL SHA-256: `4124AA4F79D452E28B0C08892B1642CD77ED2DFA9F698129405C603A4E69AA2D`;
- prior installed DLL SHA-256: `5A88AC72C6154680A6B009F7C9A5E262CA4BE8F71AFC54B7A0C1F3057AA64B9C`;
- verified rollback: `D:\BannerlordAIResearch\Builds\Rollback_Phase3_KingdomObjective_20260925_ClanAI`;
- rollback DLL SHA-256 exactly matched the prior installed DLL;
- deployed DLL hash exactly matched the candidate build.

The protected fixture `ClanAI V020V PERSIST DEMO GATE V021M 20260924` was loaded read-only. Its SHA-256 after the run remained `A91F15BF1403F1D29F942C56A1162F431113942CDACCAFE52F4D80303B4CB427`, with its original timestamp unchanged.

## Bounded observation

The observation was preregistered in-session as a maximum of 72 campaign hours and stopped on the first polling cycle that exposed a qualifying commit.

- start campaign hour: `649491.27044636116`;
- paused campaign hour: `649519.49430022226`;
- elapsed: `28.2238538611` campaign hours;
- time commands: `CLOSE_ESCAPE_MENU`, `FAST`, then `PAUSE`;
- exit: `EXIT_NOSAVE`.

No save command, synthetic target/action command, direct party order, or mutation command was issued.

## Selected proof — Culharn

A naturally active Battania ruler objective existed:

- actor: Culharn;
- ruler: Rath;
- enemy: Calradian Empire;
- reason: `BorderSecurity`;
- objective: `CaptureSpecificSettlement`;
- objective target: Uthelaim Castle.

Bannerlord supplied existing AI candidates through `PartyThinkParams`. The layer found three existing objective-related candidates, all staging candidates:

- `objectiveCandidates=3`;
- `directCandidates=0`;
- `stagingCandidates=3`.

### Native before -> adjusted after

Exact runtime record:

`KINGDOM_OBJECTIVE_ACCEPTED actor=Culharn ... objectiveTarget=Uthelaim Castle staging=Pendraic Castle,Seonon,Nevyansk Castle readiness=1 foodDays=23 objectiveCandidates=3 directCandidates=0 stagingCandidates=3 competitiveCandidates=1 bestCompetitiveCandidate=GoToSettlement:Pendraic Castle bestCompetitiveRatio=0.981 minimumRequiredFactor=1.044 beforeWinnerScore=3.756 before=RaidSettlement:Stathymos after=GoToSettlement:Pendraic Castle winnerChanged=True objectiveWon=True winningMode=staging-1 winningActionTarget=Pendraic Castle`

The existing staging rule then recorded:

`KINGDOM_OBJECTIVE_WINNER_CHANGE actor=Culharn ... actionTarget=Pendraic Castle mode=staging-1 before=RaidSettlement:Stathymos after=GoToSettlement:Pendraic Castle factor=1.55`

This is the required boundary crossing: the pre-existing native winner was `RaidSettlement:Stathymos`; the existing native staging candidate `GoToSettlement:Pendraic Castle` became the composer winner after the already-tested 1.55 staging factor. The competitive ratio was 0.981, so no cap or threshold was raised or bypassed.

### Native commit

The existing post-vanilla verifier then recorded:

`KINGDOM_OBJECTIVE_COMMIT_CHECK actor=Culharn ... actionTarget=Pendraic Castle mode=staging-1 expected=GoToSettlement:Pendraic Castle actualDefault=GoToSettlement actualShort=GoToSettlement actualTarget=Pendraic Castle arrivedMatch=True matched=True expired=False ageHours=0.454 attempts=1 source=hourly-world`

Thus Bannerlord actually committed the expected behavior/target, and the existing verifier matched it.

## Native-authority boundary

The source was unchanged from the deterministic checkpoint. The already-passing wiring invariant remains the applicable boundary:

- candidates come only from Bannerlord's `PartyThinkParams.AIBehaviorScores`;
- direct/staging logic only classifies those existing candidates;
- score changes go through `StrategicDecisionComposer.ApplyFactor`;
- the composer/native candidate set determines the final winner;
- `KingdomObjectiveLayer` does not add candidates or issue a direct movement order;
- it does not mutate faction membership, settlement ownership, or war state;
- it stores only a pending expectation after a winner change and later observes Bannerlord's actual behavior/target.

The runtime command chronology likewise contains only load/time-control/no-save-exit commands. No synthetic target or action was used.

## Conclusion

This completes the requested Phase 3 runtime proof for the existing `KingdomObjectiveLayer`: natural ruler objective -> Bannerlord native candidates -> composer-based winner change -> existing staging candidate wins -> Bannerlord commits behavior/target -> post-vanilla verifier reports `matched=True`.

Exact excerpts are preserved in `Reports/TerritorialResponsibility/evidence/phase3_kingdom_objective_runtime_20260925.txt`.
