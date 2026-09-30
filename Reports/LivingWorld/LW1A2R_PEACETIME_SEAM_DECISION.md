# LW1-A2R — Persistent Home Assignment Peacetime Seam Decision

Date: 2026-09-30 UTC.
Source GitHub checkpoint: `3f7928b165892180ba484adcd0374682efa92529`.

Supported native source identity:

- Bannerlord `v1.5.3`
- engine build `122374`
- Steam buildid `25302170`
- `TaleWorlds.CampaignSystem.dll` SHA-256 `5B23C3E36D7A5D6D47C47EAB075E9E2B1CC8D1AA53C79E135FA2B5EF434EBC5F`
- `SandBox.dll` SHA-256 `16AF436C569675EB30E22514BB755E6FB3612AFCE38F6CC55D1748D079C19C1A`

## Outcome

**LW1-A2R: COMPLETE.**

The existing `StrategicDecisionComposer` is **not sufficient by itself** for Persistent Home Assignment. Native visit generation can supply an exact friendly town/castle near the party, and can even retain a distant-but-still-in-range visit at the native `0.025` floor, but the exact assigned fortification can be absent before `PartyThinkParams.AIBehaviorScores` because `AiVisitSettlementBehavior.FillSettlementsToVisitWithDistancesAsDays` applies a hard native distance filter and its scoring loop may stop on an earlier “good enough” settlement. Native patrol is not a universal fallback because its defensive land generator iterates towns/villages and excludes castles.

One earlier native-compatible seam is therefore selected:

`TaleWorlds.CampaignSystem.CampaignBehaviors.AiBehaviors.AiVisitSettlementBehavior.FillSettlementsToVisitWithDistancesAsDays(MobileParty, List<SettlementNavigationData>)`

LW1-B may make the exact assigned native `Settlement` survive the visit producer’s **candidate exposure/retention** stage only after the existing native suitability and navigation checks succeed. It must not create `AIBehaviorData`, set a movement target, call `SetMoveGoToSettlement`, teleport, or bypass Bannerlord’s later scoring/winner/action commit.

## 1. Player-clan secondary party native control

**Confirmed: YES.** A normal non-main player-clan lord party uses the ordinary party-think selector and native commit path when AI is enabled.

Supported flow:

1. `AiPartyThinkBehavior.PartyHourlyAiTick(MobileParty)` first rejects `mobileParty.Ai.IsDisabled` or `mobileParty.Ai.DoNotMakeNewDecisions`.
2. On an eligible hourly tick it explicitly requires `mobileParty != MobileParty.MainParty`.
3. It resets `mobileParty.ThinkParamsCache`, calls `CampaignEventDispatcher.Instance.AiHourlyTick(mobileParty, thinkParamsCache)`, then directly iterates `thinkParamsCache.AIBehaviorScores`.
4. The selected native `AIBehaviorData` is committed through native `SetPartyAiAction` methods for `PatrolAroundPoint`, `GoToSettlement`, `DefendSettlement` and other native behaviors.
5. `LordPartyComponent.OnMobilePartySetOnCreation` sets `MobileParty.ActualClan = Owner.Clan`; `MobileParty.UpdatePartyComponentFlags` marks a `LordPartyComponent` as `IsLordParty`. The selector entry does not special-case player-clan secondary lord parties out.
6. The source does not distinguish family-led versus companion-led lord parties in `PartyHourlyAiTick`; LW1’s own adapter remains responsible for limiting assignment eligibility to adult player-clan family/companion leaders.

Relevant exclusions and control boundaries only:

- **Main party:** excluded by `PartyHourlyAiTick`.
- **Disabled/stopped AI:** `IsDisabled` and `DoNotMakeNewDecisions` stop the hourly selector. `MobilePartyAi.DisableForHours`, `DisableAi`, `EnableAi`, and `SetDoNotMakeNewDecisions` are the supported controls.
- **Army membership:** not a universal hourly-selector stop, but it materially changes native authority. `AiPatrollingBehavior` returns when `Army != null`; visit rejects attached nonleader army members; `AiMilitaryBehavior` returns for nonleader army members; `AiArmyMemberBehavior` contributes `EscortParty`. LW1-B therefore keeps the already-selected first-scope rule `Army == null`.
- **Disbanding/temporary states:** patrol and military reject `IsDisbanding`; visit has a separate disband/waiting-for-disband scoring path. The exact-assembly caller search also shows `SetDoNotMakeNewDecisions` used by disband/quest temporary-party code. LW1-B must fail closed while disbanding or decision-stopped.
- **Manual/player command state:** the retained exact CampaignSystem/SandBox search proves no separate ordinary player-clan secondary-lord “manual order” selector gate. Do not invent one. `DoNotMakeNewDecisions` remains the proven generic decision-stop gate.
- **Naval:** the same native selector supports land/naval navigation, ports and target-port flags, but navigation scoring differs materially. LW1-B keeps the prior land-only first scope.

## 2. GoToSettlement — exact assigned home

### A. Owned friendly town/castle during peace

**YES.** For a kingdom-faction lord party, `FillSettlementsToVisitWithDistancesAsDays` iterates `mobileParty.MapFaction.Settlements`. `IsSettlementSuitableForVisitingCondition` accepts villages or fortifications when they are not in a blocking map/siege state and are not hostile to the party faction. Therefore a player-clan-owned town or castle inside the friendly map faction is a legal visit settlement during ordinary peace.

### B. Exact assigned holding near the party

**YES, it can survive.** After suitability succeeds, native code computes navigation and distance. When navigation is valid and the settlement passes the native maximum-distance filter, it enters the sorted native settlement list. For sufficiently near legal fortifications the native base/need/owner scoring remains positive; `AddBehaviorTupleWithScore` constructs an exact `AIBehaviorData(settlement, AiBehavior.GoToSettlement, ...)` and adds or updates it in `PartyThinkParams`.

This is **availability**, not a guarantee on every tick. The settlement list is sorted, and a prior settlement whose visit score reaches the native “good enough” path can break the loop before a later exact home is processed.

### C. Exact assigned holding far from the party

**NO as a reliable arbitrary-distance candidate.**

Two distinct distance regions exist in the retained source:

- if the settlement is already in the native list but its distance is at or beyond `MaximumMeaningfulDistanceAsDays`, native visit logic appends `GoToSettlement` with score `0.025` and continues;
- before that scoring loop, `FillSettlementsToVisitWithDistancesAsDays` requires `distanceAsDays < GetMaximumDistanceAsDays(bestNavigationType)`. Beyond that hard maximum the exact friendly fortification never reaches the native list at all.

For non-kingdom map-faction parties, the native alternative path uses a bounded locatable search around the current party rather than the full faction-settlement enumeration, so an arbitrary far assigned holding is likewise not guaranteed to be evaluated.

### D. Filters/gates/pruning

Native exact-home availability is constrained by:

- settlement suitability and hostility/siege/map-event conditions;
- valid land/naval navigation;
- the hard `GetMaximumDistanceAsDays` filter in the candidate-population method;
- a meaningful-distance branch that floors retained distant visits at `0.025`;
- sorted processing and early `break` when an earlier settlement reaches the native good-enough score;
- final positive-score insertion in ordinary detailed scoring.

There is **no generic “need must exist” gate** before a legal near fortification can become a visit candidate; needs such as food, recovery/recruiting, prisoners, trade/owner/home context change the score. Native `leaderHero.HomeSettlement` is already a scoring input, but it runs after the upstream distance exposure gate and therefore cannot by itself solve arbitrary far assigned-home availability.

### E. Exact target into AIBehaviorScores

**YES when native visit processing reaches it.** `AddBehaviorTupleWithScore` uses the exact native `Settlement` object as `AIBehaviorData.Party` with behavior `GoToSettlement`, then calls `PartyThinkParams.SetBehaviorScore` or `AddBehaviorScore`. After the dispatcher returns, `AiPartyThinkBehavior` iterates `AIBehaviorScores` directly; the retained source shows no later native top-N pruning between list insertion and that winner scan.

## 3. PatrolAroundPoint — exact assigned home

The defensive patrol producer is useful local activity but **not a universal Persistent Home Assignment carrier**.

- During its eligible peace path, `CalculateDefensivePatrollingScores` iterates all map-faction settlements.
- It immediately skips any settlement that is neither `IsTown` nor `IsVillage`. **Castles are excluded from this defensive patrol producer.**
- For land patrol, `GetDistanceScoreForLandPatrolling` scores the settlement’s position relative to the faction mid-settlement and party-size threshold; it does **not** impose a current-party-to-target distance maximum.
- `CalculateDefensivePatrollingScoreForSettlement` still requires valid native navigation and `TargetScoreCalculatingModel.CalculateDefensivePatrollingScoreForSettlement(...) * scoreAdjustment > 0` before adding the exact `AIBehaviorData(settlement, PatrolAroundPoint, ...)`.

Therefore:

- exact owned **town** patrol target in peace: **YES, conditionally**;
- exact owned **castle** patrol target in this producer: **NO**;
- near/far from the current party does not itself gate the land-town patrol target, but the party-state/food/size/native target-score gates still do;
- once appended, the exact settlement-valued patrol target is in `AIBehaviorScores`.

For the LW1 assignment domain of **towns plus castles**, patrol is not sufficiently available near or far to replace the visit path.

## 4. DefendSettlement boundary

`AiMilitaryBehavior` maps `ArmyTypes.Defender` to `AiBehavior.DefendSettlement` and evaluates the party’s faction settlements. A defender settlement proceeds only when it has an active `LastAttackerParty`, and the attacker must still be active and hostile to the defending party’s map faction. Defense also has native travel-time/navigation checks before insertion.

**Ordinary peace does not generate a normal home `DefendSettlement` candidate.**

This cleanly preserves the existing Phase 3 War Home Responsibility design: siege/raid/active-hostile defense remains the stronger war/urgent path; LW1-B’s peacetime assignment seam and factor must be disabled outside confirmed peace and on urgent existing responsibility.

## 5. Direct composer sufficiency

**Direct composer sufficient: NO.**

The current composer only rescales candidates already in `PartyThinkParams.AIBehaviorScores` and preserves a fixed candidate count. That is correct for native authority, but it cannot express an assigned castle/town when the native visit producer drops that exact settlement at its hard distance exposure gate, and patrol cannot fill the gap for castles.

The actual player goal requires an assigned holding to remain a weak native return option after ordinary departures, including when it is outside the normal visit search radius. Therefore a source-supported pre-composer seam is required.

## 6. Selected one native-compatible seam

**Selected seam:**

Class:
`TaleWorlds.CampaignSystem.CampaignBehaviors.AiBehaviors.AiVisitSettlementBehavior`

Method:
`FillSettlementsToVisitWithDistancesAsDays`

Exact signature:
`private static void FillSettlementsToVisitWithDistancesAsDays(MobileParty mobileParty, List<SettlementNavigationData> listToFill)`

**Point in native flow:** native visit candidate exposure, after the current settlement has passed `IsSettlementSuitableForVisitingCondition` and `GetBestNavigationDataForVisitingSettlement`, but before the generic maximum-distance/local-search boundary causes the exact assigned holding to disappear.

**Architecture:** one narrowly scoped Harmony method patch in LW1-B may expose/retain **only the exact assigned native Settlement** for an otherwise eligible player-clan secondary lord party. It must reuse the same native suitability and navigation checks. For the kingdom-faction branch it only prevents the generic maximum-distance test from discarding that exact home. If the non-kingdom bounded-search branch did not encounter the assigned settlement, the same method-level seam may evaluate that one already-resolved assignment reference through those same native suitability/navigation checks; it must not start another settlement enumeration.

The seam changes **candidate exposure only**. It does not:

- construct or append `AIBehaviorData` in ClanAI;
- change native behavior type;
- substitute a different settlement object;
- bypass hostile/siege/map-event legality;
- bypass navigation validity;
- bypass the native good-enough early stop;
- set `TargetSettlement`;
- issue a move command;
- teleport;
- force periodic return.

Downstream remains native:

1. native visit method owns `SettlementNavigationData`;
2. native visit scoring owns the raw visit score;
3. native `AddBehaviorTupleWithScore` constructs exact `GoToSettlement` `AIBehaviorData`;
4. native `PartyThinkParams` owns the candidate list;
5. existing `StrategicDecisionComposer` may apply the bounded peacetime exact-home factor;
6. native `AiPartyThinkBehavior` chooses the winner;
7. native `SetPartyAiAction.GetActionForVisitingSettlement` commits movement.

The retained native distant-visit floor is `0.025`; for a non-army `GoToSettlement`/`PatrolAroundPoint` winner, the native selector threshold is `0.03`. The already-audited **1.25 maximum peacetime home factor** can raise an otherwise untouched `0.025` retained home visit to `0.03125`: enough to remain a weak actionable center of gravity when no stronger native purpose wins, while scores from real recovery/recruiting/trade/security needs can still dominate. LW1-B should use **1.25** as the v1 exact-home visit/patrol factor, one contribution per candidate/frame; war/unknown/urgent state remains factor `1.00`.

## 7. Performance

The selected path preserves the LW1-A performance contract:

`LeaderHero.StringId`
→ O(1) assignment dictionary lookup
→ cached assigned native `Settlement` reference
→ compare to the settlement currently being evaluated by native visit generation
→ native suitability/navigation
→ pure `HomeAssignmentPolicy`
→ one bounded retention decision and later one bounded composer factor.

No new global settlement scan is required. The native method already performs its own enumeration/search; ClanAI inspects only the current native settlement or, in the bounded non-kingdom case, the one already-resolved assigned settlement reference. There is no second all-settlement enumeration.

No-assignment fast path must return before assignment resolution/context work. The existing candidate-list scan in the composer remains O(C) over native candidates.

## 8. LW1-A design reconfirmation

Supported source does not invalidate the previously selected product design.

- **Persistent identity remains valid: YES.** `Hero.StringId → Settlement.StringId`.
- **Scope remains:** player clan; adult family/companion-led lord parties; main party excluded; one home per leader; player-owned towns/castles; land-only first slice; no army influence.
- **Multiple parties:** distinct leader IDs map independently to distinct settlements.
- **UI design remains valid: YES.** Keep the small player-owned town/castle “ClanAI: Home assignments” menu, with Assign/Change/Clear and current-home display.
- **Persistence design remains valid: YES.** Keep the new `ClanAI_HomeAssignment_v1` key and D1 `Hero.StringId`/ `Settlement.StringId` rows selected in LW1-A; this A2R checkpoint itself changes no save schema.
- **War preservation remains feasible: YES.** Peacetime seam/factor fail closed outside confirmed peace; existing Home Responsibility siege/raid/weak recovery priorities and factors remain unchanged.

## 9. Exact LW1-B vertical slice

The next checkpoint is implementation/build, not another source-research checkpoint.

LW1-B should implement:

1. **HomeAssignmentStore**
   - O(1) ordinal `Hero.StringId` dictionary;
   - cached resolved `Settlement` reference/session generation;
   - one home per hero, change/clear, ownership/lifecycle invalidation;
   - `ClanAI_HomeAssignment_v1` D1 save/import contract from LW1-A.

2. **Native menu**
   - player-owned town/castle only;
   - list eligible current player-clan secondary lord-party leaders;
   - show current home;
   - Assign/Change here/Clear;
   - restrained one-shot messages.

3. **One native visit seam**
   - Harmony patch only on `AiVisitSettlementBehavior.FillSettlementsToVisitWithDistancesAsDays`;
   - exact-assigned-settlement exposure/retention only;
   - native suitability/navigation mandatory;
   - no `AIBehaviorData` construction or move calls.

4. **HomeAssignmentPolicy + composer layer**
   - confirmed peace only;
   - exact assigned settlement only;
   - positive finite native/working score only;
   - `GoToSettlement` and settlement-valued `PatrolAroundPoint`;
   - factor **1.25**, otherwise `1.00`;
   - no suppression of unrelated native candidates;
   - existing War Home Responsibility unchanged.

5. **Player visibility and multiple homes**
   - brother at Castle A and son at Castle B remain distinct;
   - current assignments visible in menu;
   - no main-party modification.

6. **Observation-only commit verifier**
   - assignment revision + leader/party/session identity;
   - expected final behavior/settlement pair;
   - bounded expiry;
   - compare native default/short-term behavior with its corresponding target pair;
   - no automatic duty-memory mutation.

7. **Counters**
   - opportunities/no-assignment fast path;
   - eligible evaluations;
   - native candidate retained/exposed;
   - candidate missing;
   - factor applications;
   - winner changes;
   - commit checks/matches/expiry;
   - save restore/reject counts.

8. **Deterministic validation**
   - multiple distinct homes;
   - assign/change/clear;
   - D1 round-trip/malformed/conflict cases;
   - ownership loss;
   - main/army/disabled/disbanding/naval/war passthrough;
   - far assigned visit retention only after native legality/navigation;
   - no synthetic `AIBehaviorData`, movement or global scan;
   - existing Phase 3 Home Responsibility preservation;
   - final winner/commit-verifier pair correctness.

9. **Release build**
   - deterministic tests and wiring invariants;
   - Release build;
   - no deployment or live runtime unless separately authorized after LW1-B.

LW1-B must not add governor logistics, food convoys, kingdom service requests, advisement, social visits, extra party limits, a new economy, or another LW feature.

## Required status

| Field | Result |
|---|---|
| LW1-A2R | **COMPLETE** |
| Supported native version | **v1.5.3** |
| Player-clan secondary party native selector confirmed | **YES** |
| Exact owned GoToSettlement candidate near home | **YES — can survive when legal/processed** |
| Exact owned GoToSettlement candidate far from home | **NO — not beyond native hard maximum without the selected seam** |
| Exact owned PatrolAroundPoint candidate near home | **NO as a town/castle guarantee — town can; castle is excluded** |
| Exact owned PatrolAroundPoint candidate far from home | **NO as a town/castle guarantee — town can; castle is excluded** |
| Direct composer sufficient | **NO** |
| Selected native seam | **AiVisitSettlementBehavior.FillSettlementsToVisitWithDistancesAsDays** |
| Persistence design remains valid | **YES** |
| UI design remains valid | **YES** |
| O(1) assignment path preserved | **YES** |
| Existing war Home Responsibility preservation feasible | **YES** |
| Gameplay changed | **NO** |
| Save schema changed | **NO** |
| RC1 changed | **NO** |
| Bannerlord launched | **NO** |
| Implementation clearance | **GO — ONE NATIVE SEAM** |
| Exact next checkpoint | **LW1-B — implement and build the playable Persistent Home Assignment / Peacetime Responsibility vertical slice.** |

GO — ONE NATIVE SEAM: TaleWorlds.CampaignSystem.CampaignBehaviors.AiBehaviors.AiVisitSettlementBehavior.FillSettlementsToVisitWithDistancesAsDays
