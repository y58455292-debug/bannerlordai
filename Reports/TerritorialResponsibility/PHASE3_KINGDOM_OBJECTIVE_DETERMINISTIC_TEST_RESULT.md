# Phase 3 Kingdom Objective — Deterministic Test Result

Date: 2026-09-25

## Scope

This checkpoint implements only the bounded deterministic seam for the existing `KingdomObjectiveLayer`. It does not deploy a DLL and does not claim new campaign/runtime behavior.

## Implementation

Added `KingdomObjectivePolicy`, a pure helper with no Bannerlord types. `KingdomObjectiveLayer` still obtains the actor, ruler objective, native `PartyThinkParams` candidates, settlement targets, current composer scores, final winner, and post-vanilla behavior/target state from Bannerlord-facing runtime code.

The pure policy now owns only the already-existing decision rules:

- low-readiness refusal below `0.72`;
- low-food refusal below 3 days when food duration is known;
- urgent clan-home-threat refusal after readiness/supply acceptance;
- direct versus staging candidate factor selection;
- direct factors: besiege 1.75, assault 1.65, go-to 1.50, patrol 1.35;
- staging factors: go-to 1.55, patrol 1.40, defend 1.30, minus 0.10 per staging rank with a 1.10 floor;
- competitive ratio threshold 0.45;
- required winner factor `(beforeWinnerScore * 1.025) / baseScore`;
- direct factor bound 2.50 and staging factor bound 2.25;
- pending commit tracking only when the final composer winner changed, the new winner is an objective candidate, and it has a settlement target.

No threshold or factor was changed.

## Competitive boundary

At the exact 0.45 competitive-ratio threshold, the required winner factor is about 2.278. That remains inside the 2.50 direct bound, so a direct objective candidate can be escalated to the 1.025 winner margin. The same required factor exceeds the 2.25 staging bound, so a staging candidate at exactly 0.45 keeps its base staging factor and cannot clear the prior winner margin from that edge case.

This is deterministic policy behavior only, not a runtime claim.

## Validation

`dotnet run --project Tests/TerritorialResponsibility/KingdomObjectivePolicyTests.csproj -c Release`

Result:
`PASS KingdomObjective policy tests cases=34`

The cases cover readiness/food refusal and threshold equality, refusal precedence, urgent home threat, direct and staging behavior tables, staging rank penalty/floor, non-objective and non-positive candidates, competitive-ratio boundary behavior, required winner-factor escalation, direct/staging bounds, winner-margin behavior, and all pending-commit conditions.

`python Tests/TerritorialResponsibility/test_kingdom_objective_runtime_wiring.py`

Result:
`PASS KingdomObjective runtime wiring`
`native candidates, composer ownership, and post-vanilla commit verification preserved`

`dotnet build src/ClanAI/src/ClanAI/ClanAI.csproj -c Release`

Result: build succeeded with 0 errors. The inherited `System.ValueTuple` version-conflict warning remains the only warning.

## Native-authority boundary

The runtime layer still iterates only Bannerlord-provided AI behavior candidates. Direct candidates must already target the ruler objective settlement; staging settlements only classify already-existing native candidates. Scores are read and modified through `StrategicDecisionComposer`. The layer does not add native candidates, issue direct party orders, alter action legality, mutate settlement/faction/war state, or bypass Bannerlord's final behavior selection.

When the composer reports a new objective winner, the layer only stores a pending expectation. Existing post-vanilla verification still requires Bannerlord's actual default/short-term behavior and target (or arrival at the expected settlement) to match.

## Boundary and next milestone

No runtime deployment was performed. The next milestone is exactly one bounded runtime proof that a natural ruler objective changes a native candidate winner and Bannerlord commits the selected behavior/target. This checkpoint stops before that runtime proof.


## Runtime follow-up — proven 2026-09-25

The next bounded runtime milestone passed without changing the deterministic candidate. During a read-only run capped at 72 campaign hours, Culharn naturally received Battania ruler Rath's `BorderSecurity / CaptureSpecificSettlement / Uthelaim Castle` objective. Bannerlord's existing candidate winner was `RaidSettlement:Stathymos`. The existing staging candidate `GoToSettlement:Pendraic Castle` had competitive ratio 0.981; the unchanged 1.55 staging factor changed the composer winner to that candidate. The post-vanilla verifier then observed `actualDefault=GoToSettlement`, `actualShort=GoToSettlement`, `actualTarget=Pendraic Castle`, and `matched=True`.

The run stopped after 28.224 campaign hours, used no synthetic target/action or direct party order, exited without saving, and left the protected fixture hash unchanged. See `Reports/TerritorialResponsibility/PHASE3_KINGDOM_OBJECTIVE_RUNTIME_RESULT.md`.
