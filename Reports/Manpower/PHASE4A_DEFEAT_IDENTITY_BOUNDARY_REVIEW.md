# Phase 4A — Defeat identity boundary offline review

Date: 2026-09-25 UTC  
Base: `67a848e3366782090fbcb6ea3dcabd78d9297cc7`

## Scope and result

**OFFLINE REVIEW PASSED — a minimal observation-only identity seam exists and was implemented.**

The closed 164.281-hour same-hero defeat -> recreation characterization remains a bounded null. It was not rerun or extended. No generic creation is reclassified, no identity rule is weakened, and Phase 4B is not started.

The failure mode is explained by native battle-result ordering: for non-player battles, defeated members are processed before `MapEventEnded`. When a defeated hero is the party leader, native result application can call `MobileParty.RemovePartyLeader()` before the end-event observer runs. Therefore `MobileParty.LeaderHero` is not a reliable identity source at `MapEventEnded` for a defeated lord party.

A reliable earlier native surface is available. `CampaignEvents.MapEventStarted` exposes the exact `MapEvent` plus initial attacker/defender `PartyBase` objects after map-event side assignment, and `CampaignEvents.OnPartyAddedToMapEventEvent` exposes later parties after they have joined the event. Those surfaces occur before defeat-result cleanup.

## Native evidence

The reviewed native binary was:

- `TaleWorlds.CampaignSystem.dll`
- SHA-256 `5B23C3E36D7A5D6D47C47EAB075E9E2B1CC8D1AA53C79E135FA2B5EF434EBC5F`

Offline reflection confirmed these public campaign events:

- `MapEventStarted: IMbEvent<MapEvent, PartyBase, PartyBase>`
- `OnPartyAddedToMapEventEvent: IMbEvent<PartyBase>`
- `MapEventEnded: IMbEvent<MapEvent>`

Offline decompilation established the relevant ordering:

1. `MapEvent.Initialize` assigns the attacker/defender map-event sides, adds event parties, and then dispatches `OnMapEventStarted`.
2. `MapEvent.AddInvolvedPartyInternal` dispatches `OnPartyAddedToMapEvent` after the party is part of the map event.
3. A non-player battle win runs `CalculateAndCommitMapEventResults`.
4. If defeated members can be captured, `CaptureDefeatedPartyMembers` removes a party leader before capture/escape processing.
5. `FinalizeEventAux` dispatches `OnMapEventEnded` later.

This sequencing directly explains the bounded run's repeated `reason=no-native-leader-at-defeat` without treating cached names, clans, or later destruction as defeat proof.

## Minimal observer seam

The recreation observer now caches an exact native battle participant only while leader identity is available. Each captured participant records:

- exact `MapEvent` object reference;
- exact `PartyBase` object reference;
- exact `MobileParty` object reference;
- captured native battle side;
- stable leader hero `StringId`;
- capture hour and whether capture came from `MapEventStarted` or `OnPartyAddedToMapEventEvent`.

At `MapEventEnded`, a defeat identity is admissible only when all of these are true:

- the cached `MapEvent` is the exact same native object as the ending battle;
- the cached `PartyBase` is the exact same native object as the defeated-side participant;
- the cached `MobileParty` is the exact same native object still attached to that `PartyBase`;
- the cached side equals the battle's native `DefeatedSide`;
- a nonblank hero `StringId` was captured from the native leader before result cleanup;
- if a live end-of-battle leader identity is still available, it must match the captured `StringId` ordinally.

A cached identity from another battle cannot satisfy the gate. Clan and hero-name matching are not inputs. `MobilePartyDestroyed` evidence cannot create a defeat identity and remains relevant only to the existing prior-party-gone requirement after a defeat has already been established.

Participant cache entries are removed by exact battle reference at map-event end.

## Answers to the review questions

1. **Is `MapEventEnded` too late?** Yes for this identity purpose. Native defeated-member processing can remove the leader before `MapEventEnded`.
2. **Is there an earlier callback?** Yes. `MapEventStarted` captures the initialized event and initial participants; `OnPartyAddedToMapEventEvent` covers later joiners.
3. **Can exact participant identity be recorded?** Yes. The seam records exact native `MapEvent`, `PartyBase`, and `MobileParty` references plus side and stable hero `StringId`.
4. **Can the defeated side verify the exact participant?** Yes. End-of-event acceptance requires same battle reference, same party references, and captured side == native `DefeatedSide`.
5. **Does this weaken recreation identity/distinct-party rules?** No. Same-hero ordinal `StringId`, distinct recreated native party, prior destruction/absence, time bounds, and first-settlement requirements are unchanged.
6. **Can it remain observation-only and standalone-safe?** Yes. It adds only native event listeners and in-memory observer state. No gameplay mutation, external IO, serialization, development-machine path, or development-tool dependency was added.

## Validation

Final offline validation:

```
PASS Phase 4A same-hero recreation policy checks=43
PASS Phase 4A same-hero recreation wiring and original-observer preservation
PASS Phase 4A same-hero recreation no-mutation and standalone invariant
PASS Phase 4A recovery policy tests checks=20
PASS Phase 4A recovery wiring
PASS Phase 4A recovery no-mutation/standalone invariant
1 Warning(s)
0 Error(s)
```

The added deterministic cases include:

- exact same-battle/same-party capture accepted when end-time leader identity is absent;
- different battle rejected;
- different native party rejected;
- different side rejected;
- clan/name-only state without stable hero `StringId` rejected;
- later destruction identity alone rejected;
- live end-time hero mismatch rejected.

The wiring invariant requires exact same-battle, exact `PartyBase`, exact `MobileParty`, and defeated-side matching. It also verifies that the destruction callback cannot construct a `DefeatLink`.

The single build warning remains the inherited MSB3277 `System.ValueTuple` conflict. Release DLL SHA-256 after this observer-only change:

`0D23F4A66E0A1E4413E879C97E963C7DB923D8D6B4421291105D8565120CD5A8`

## Limits and closure

This is an offline seam validation only. No Bannerlord runtime campaign was launched, no DLL was deployed, and no new same-hero recreation proof is claimed.

A battle already in progress before this observer session's start/add callbacks could still lack a participant capture and remain unresolved. Likewise, a party whose stable leader identity is unavailable even at the earlier native participant surface remains unresolved. Those cases are conservative nulls, not invitations to fall back to clan/name or destruction inference.

The prior 164.281-hour bounded null remains authoritative evidence for that completed run. A future runtime observation is a separate milestone and was not started here.

**Stop at this Phase 4A offline checkpoint. Do not start Phase 4B.**
