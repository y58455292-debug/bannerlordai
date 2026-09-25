# Phase 4A — Vanilla AI Recovery Observation Seam

Date: 2026-09-25  
Repository base: `d0d75172bbc4a37d7f757f7a0fac8230d420f813`

## Scope

This checkpoint begins Phase 4A only. It adds the minimum observation-only telemetry needed to characterize how native Bannerlord AI lord parties recover after natural party creation, battle losses, or severe depletion.

It does **not** change recruitment, respawn troops, party size, troop tiers, garrisons, volunteer pools, economy, settlement state, faction state, war state, AI scoring, targets, or movement.

No runtime characterization is claimed by this offline checkpoint.

## Observer shape

Added `Phase4ARecoveryObserverBehavior`, registered as a normal campaign behavior with empty `SyncData`.

It listens only to native events:

- `MobilePartyCreated`;
- `MobilePartyDestroyed`;
- `MapEventEnded`;
- `HourlyTickPartyEvent`;
- `BeforeSettlementEnteredEvent`;
- `AfterSettlementEntered`;
- `OnSettlementLeftEvent`;
- session launch for reset / initial severe-depletion discovery.

Tracking scope is limited to NPC lord parties. The player/main party and player-clan parties are excluded.

A party becomes a recovery subject when:
- a native NPC lord party is created; or
- a surviving NPC lord party is naturally observed with `PartySizeRatio <= 0.35`, including immediately after a native `MapEventEnded`.

The 0.35 threshold is only a telemetry qualifier for “severely depleted”; it changes no game behavior.

Each tracked subject is observed for at most 168 campaign hours.

## Party telemetry

Snapshots preserve:

- party id / party name;
- leader;
- clan;
- kingdom;
- campaign hour and elapsed recovery hours;
- roster total;
- healthy vs wounded;
- hero count;
- regular troop tier composition T0-T6 and T7+;
- party-size limit;
- party-size ratio;
- food days and current food;
- current settlement;
- target / short-term / besieged settlement where available;
- tracking start reason;
- whether a settlement has been visited.

## Settlement-visit telemetry

For each tracked settlement visit:

- a pre-entry party snapshot;
- first-settlement pre-entry record;
- post-entry observation;
- exit snapshot;
- party roster delta across the visit;
- settlement notable volunteer-pool total and tier mix before/after;
- settlement garrison roster total / health / tiers before/after where a garrison exists;
- visit elapsed campaign hours.

The observer reads public native `Hero.VolunteerTypes` and `Town.GarrisonParty.MemberRoster` state only.

## Conservative source classification

Added pure `Phase4ARecoveryClassificationPolicy`.

A troop-count increase alone is never labeled recruitment.

Classification rules are intentionally conservative:

- `InitialPresenceBeforeSettlement`: native created lord party already has troops and is not inside a settlement at the first observer snapshot;
- `SettlementRecruitmentSupported`: party grows across a settlement visit and the observed volunteer pool decreases while the observed garrison does not decrease;
- `GarrisonWithdrawalSupported`: party grows across a settlement visit and the observed garrison decreases while the volunteer pool does not decrease;
- `MixedSettlementSources`: party grows while both observed volunteer pool and garrison decrease;
- `UnknownSettlementSource`: party grows during a settlement visit without a supporting pool/garrison delta;
- `OtherOrUnknownNativeSource`: party grows outside an observed settlement visit;
- `None`: no increase / insufficient initial condition.

“Supported” is evidence language, not a claim that concurrent native activity by other parties is impossible. Ambiguous cases remain unknown.

## Deterministic validation

`dotnet run --project Tests/Manpower/Phase4ARecoveryPolicy/Phase4ARecoveryPolicyTests.csproj -c Release`

Result:

`PASS Phase 4A recovery policy tests checks=20`

`source labels require supporting volunteer/garrison evidence; unsupported growth remains unknown`

Coverage includes:
- severe-depletion threshold and invalid values;
- created-party initial troops before settlement;
- no fake respawn source for generic severe depletion;
- no source for no growth / losses;
- outside-settlement growth stays unknown;
- volunteer decrease supports recruitment;
- garrison decrease supports withdrawal;
- both decreases remain mixed;
- no supporting deltas remain unknown;
- unobserved pools remain unknown;
- pool increases do not prove party source.

## Wiring invariant

`python Tests/Manpower/test_phase4a_recovery_wiring.py`

Result:

`PASS Phase 4A recovery wiring`

`native creation/battle/hourly/settlement observations and conservative source classification are wired`

The invariant confirms native event wiring plus read-only roster, health, tier, limit, ratio, food, settlement, volunteer, and garrison state access.

## No-mutation / standalone invariant

`python Tests/Manpower/test_phase4a_recovery_no_mutation.py`

Result:

`PASS Phase 4A recovery no-mutation/standalone invariant`

`observer introduces no troop/recruit/garrison/order/world mutation and no dev-machine runtime path`

The guard rejects troop roster writes, volunteer/garrison writes, direct movement/order calls, AI candidate/score writes, settlement/faction/war actions, economy writes, development-tool dependencies, and absolute runtime paths.

## Release build

`dotnet build src/ClanAI/src/ClanAI/ClanAI.csproj -c Release`

Result:
- Build succeeded;
- 0 errors;
- 1 inherited `System.ValueTuple` version-conflict warning;
- built DLL SHA-256: `5CA45B7AE095E9B3F6F77E06FCCA287414FBAC7D345648AAAD58F39EA1919BF6`.

No DLL was deployed as part of this offline checkpoint.

## Next bounded step

Run one natural Phase 4A characterization with a maximum 168 campaign-hour observation window. Qualifying evidence should follow at least one naturally created / post-battle severely depleted / otherwise severely depleted AI lord party through early rebuilding, preserving first observed roster, pre-first-settlement roster, settlement visits, party growth, volunteer/garrison deltas where available, and only evidence-supported source labels.

If no qualifying recovery case develops within the bound, record the null without manufacturing a defeat or recruitment event.


## Runtime follow-up — first characterization passed

The observer candidate from `dfa18f3e56a90fbe90c6467a4680e2d545fadcf4` completed a natural runtime characterization within 39.623 campaign hours. Kulyat of the Forest People began at 37/133 troops before any observed settlement visit, then gained +7 at Sibir while volunteers fell 20->13 and garrison remained 423, +9 at Kvol while volunteers fell 18->9, +4 on a later Sibir visit while garrison remained 424, and +8 at Radakmed while volunteers fell 18->10. Four additional +1 increases occurred outside observed settlement visits and remain `OtherOrUnknownNativeSource`.

The same run observed native lord-party creation with non-zero troops before settlement interaction, natural defeated-side post-battle parties with wounded troops, and an Iara castle visit where party +7 matched garrison -7 with volunteers unchanged, supporting a garrison withdrawal.

No gameplay mutation was used. See `Reports/Manpower/PHASE4A_RECOVERY_CHARACTERIZATION_RESULT.md`.
