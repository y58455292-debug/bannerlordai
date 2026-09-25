# Phase 4A — Same-hero defeat/recreation observation seam

Date: 2026-09-25 UTC
Base: `6ea934ee00c7a6ccc011240d48a6e63fa6de2327`

## Scope and result

OFFLINE VALIDATION PASSED. No runtime characterization is claimed here. This extends the existing Phase 4A observer only; Phase 4B is not started.

`Phase4ARecoveryObserverBehavior` is now partial and registers additional observation callbacks. Its existing recovery code, classifications, roster capture, and context formatting are preserved. A hash-based invariant reconstructs the original observer after removing the partial keyword and registration call (ignoring a final newline). No strategic behavior or configuration changes are included.

## Identity and causal evidence

The new partial file observes the existing native session, MapEventEnded, MobilePartyDestroyed, MobilePartyCreated, hourly party, and before/after settlement-entry surfaces.

A defeat is recorded only for the native defeated side of a battle with a winner. The hero StringId must be available from the native lord-party leader at map-event end. Cached identity supports destruction logging, but does not by itself establish a defeat when the native leader is unavailable. Such cases emit `PHASE4A_LINK_DEFEAT_UNRESOLVED`, not a proven defeat link.

A creation links only when all of the following hold: the hero StringId matches ordinally; the defeat was already observed in this session; the times are finite, ordered, and within 168 campaign hours; the new native PartyBase is a distinct instance; and the old party has an observed destruction or is absent from MobileParty.All. A reused party StringId does not imply a reused native instance. Clan and name matching are never linking keys. Duplicate creation callbacks cannot establish another recreation, and a linked defeat is consumed once.

At native creation the observer records stable hero/party identity, clan/kingdom, campaign hour, time since defeat, destruction evidence if available, total/healthy/wounded roster, heroes, regular troop tiers, size limit/ratio, food, and current/target settlement. Existing capture helpers are reused.

## Classification and first settlement boundary

Allowed classifications are exactly:

- `PostDefeatNativeRecreationInitialTroopsSupported`
- `PostDefeatNativeRecreationZeroTroops`
- `PostDefeatRecreationObservedButPreSettlementStateIncomplete`
- `GenericNativePartyCreation`
- `UnknownOrUnlinkedCreation`

The strong initial-troops label requires a linked defeat and an available roster captured outside settlement interaction. Regular troops are counted separately from heroes: a leader alone is not evidence of supplied replacement troops. Both total and hero counts remain in the raw evidence. ZeroTroops means zero regular troops and may coexist with a hero-only roster.

Creation-time evidence and a complete first-visit chain are distinct. The observer follows linked creations until BeforeSettlementEnteredEvent, records the first pre-interaction roster, initial roster, net headcount change and observed positive hourly increments, then ends that follow-up. Generic creations retain their generic label irrespective of roster size.

A creation already in a settlement, a missing pre-entry boundary, changed hero identity, unavailable roster, time reversal/out-of-window observation, or observer exception prevents a complete-chain claim. An after-entry-only observation is incomplete. Exceptions are contained within the new callbacks and set a session fault flag that blocks complete claims.

Pre-settlement growth is observational: hourly snapshots plus the pre-entry boundary can establish observed increases/net change, not exclude perfectly offsetting changes between samples. Its source remains unknown. No troop count or source is fabricated.

## Validation

Commands from repository root:

```
dotnet run --project Tests/Manpower/Phase4ALinkage/Phase4ALinkageTests.csproj -c Release
python Tests/Manpower/test_phase4a_recreation_link_invariants.py
dotnet run --project Tests/Manpower/Phase4ARecoveryPolicy/Phase4ARecoveryPolicyTests.csproj -c Release
python Tests/Manpower/test_phase4a_recovery_wiring.py
python Tests/Manpower/test_phase4a_recovery_no_mutation.py
dotnet build src/ClanAI/src/ClanAI/ClanAI.csproj -c Release
```

Final results at 2026-09-25T07:08:32.7348118Z:

```
PASS Phase 4A same-hero recreation policy checks=35
PASS Phase 4A same-hero recreation wiring and original-observer preservation
PASS Phase 4A same-hero recreation no-mutation and standalone invariant
PASS Phase 4A recovery policy tests checks=20
PASS Phase 4A recovery wiring
PASS Phase 4A recovery no-mutation/standalone invariant
1 Warning(s)
0 Error(s)
BUILD_EXIT=0
```

The warning is the inherited MSB3277 System.ValueTuple reference conflict. Build SHA-256: `0FE236EDA21FFED93F14003A588C5A97712A5CED66341413612244C40BF1F755`.

The tests cover identity mismatches/missing ids, distinct-party and prior-removal requirements, time bounds/invalid times, generic versus linked creation, hero-only/zero/invalid rosters, missed settlement boundaries, observer faults, and first-visit identity continuity. Source invariants reject relevant troop, party, target, score, economy and world mutation APIs, external IO, and development-machine paths in the new seam. No runtime tooling dependency or serialized observer state is introduced.

One initial invariant incorrectly matched `Clan` inside the namespace `ClanAI`; it was corrected to match the standalone token. The final suite above is the accepted result. No game behavior was changed to address that test issue.

## Bounded runtime protocol

One natural run only. Maximum 168 campaign hours; stop on the first `PHASE4A_LINK_FIRST_SETTLEMENT_PRE ... completeChain=True`. Use a conservative external pause margin near the cap to account for time-control latency. No extension, forced battle, forced destruction, forced recreation, recruitment, movement, target or score changes.

Deploy only while Bannerlord is closed, after a verified rollback. Load the protected fixture read-only; record its before/after hash and timestamp. Keep Strategic Commitment Observe and Visual War OFF. Exit without saving. Preserve exact new event lines and control chronology, with a pass, incomplete observation, or bounded null stated honestly. Stop at this Phase 4A checkpoint; do not start Phase 4B.
