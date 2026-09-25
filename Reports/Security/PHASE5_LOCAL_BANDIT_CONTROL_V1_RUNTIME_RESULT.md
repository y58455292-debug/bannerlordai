# Phase 5-v1 Local Bandit Control — bounded runtime characterization

Date: 2026-09-25 UTC  
Gameplay implementation checkpoint: `0093c8d3d27d2252b9e13572441ab73bbe121dea`  
Observation-only telemetry candidate: `08e82ac8cf0f3fc126129fbc77206fb8733ba636`

## Result

**STRONG PASS — Phase 5-v1 Local Bandit Control is runtime-observed on the exact validated gameplay policy. Balance remains unproven.**

The bounded run naturally exercised:

- the intended private postfix on normal town/village spawn-site candidates;
- a secure candidate with multiplier below 1;
- a naturally weak candidate with multiplier above 1;
- the village bound-fortification Security path;
- a naturally near-neutral candidate;
- a hideout invocation with exact native passthrough;
- a naturally created native looter party after Bannerlord's own spawn processing.

No Security, raid, hideout, looter count, spawn rate, or party state was manufactured or mutated to obtain the proof.

No save command was issued.

## Candidate and policy integrity

Runtime DLL SHA-256:

`E4DB15DAF5C437F45F559CADDD3F385CE062416D2896EE451826D961D9DDDF7A`

The observation-only telemetry candidate was committed before deployment as:

`08e82ac8cf0f3fc126129fbc77206fb8733ba636`

The accepted Phase 5 gameplay policy remained byte-for-byte unchanged:

- `LocalBanditControlPolicy.cs` Git blob: `7b3d119f274ddf884167f8fd337815d8ba6e6c47`;
- policy source SHA-256: `890DED9CAA10B1A4E84ACE80FBE5788E96E00FCE17654325E5EB94B5494A3FC2`;
- bounds: `0.75..1.25`;
- neutral Security: `50`;
- sole v1 input: Security.

Before deployment, the full Phase 5 offline suite, all retained Phase 4 tests/invariants, and relevant Home Responsibility / Kingdom Objective / Visual War / Strategic Commitment tests/invariants passed. Release built with 0 errors and the inherited `System.ValueTuple` warning.

## Runtime bound

Protected fixture:

`ClanAI V020V PERSIST DEMO GATE V021M 20260924`

Start campaign hour:

`649491.27044636116`

The complete minimum proof first existed at:

`649510.79714630556`

Elapsed to minimum proof:

`19.526699944399297` campaign hours.

The optional hideout passthrough appeared at:

`649510.81641863892`

Pause acknowledgement occurred at:

`649579.19519833336`

Elapsed to pause acknowledgement:

`87.92475197219756` campaign hours.

The preregistered maximum was 168 campaign hours. The proof itself completed much earlier; the later pause acknowledgement reflects command/output polling latency while the campaign remained in fast-forward, not an extension to hunt for extra proof.

## A. Patch execution / native result availability

The postfix executed during normal native bandit spawning.

Every captured evaluation records:

- `hook=postfix`;
- `originalExecuted=True`;
- `nativeWeight` before Phase 5 adjustment;
- `mutationByClanAI=False`.

Secure town sample:

```
LOCAL_BANDIT_CONTROL_EVAL sample=town+secure campaignHour=649510.79714630556 settlementId=town_ES7 settlement=Syronea candidateKind=Town policyKind=Town boundSettlementId=<none> boundSettlement=<none> securityAvailable=True security=100 nativeWeight=1 controlMultiplier=0.75 finalWeight=0.75 applied=True reason=security-relative-weight hook=postfix originalExecuted=True mutationByClanAI=False
```

This is direct runtime evidence that Bannerlord produced the native result first and Phase 5 modified only that returned value in the postfix.

## B. Secure local-control effect

Natural secure sample:

- settlement: Syronea;
- candidate kind: Town;
- Security: 100;
- native weight: 1;
- multiplier: 0.75;
- final weight: 0.75.

Therefore:

- multiplier < 1;
- final < native;
- final remained positive.

A village bound to the same secure town was also observed:

- village: Psotai;
- bound settlement: Syronea;
- resolved Security: 100;
- native weight: 1;
- multiplier: 0.75;
- final weight: 0.75.

## C. Weak local-control effect

A naturally weak village candidate was observed without manufacturing damage or Security:

- village: Tepes;
- bound fortification: Tepes Castle;
- Security: 43.85594;
- native weight: 1;
- multiplier: 1.03072023;
- final weight: 1.03072023.

Therefore:

- Security < 50;
- multiplier > 1;
- final > native;
- multiplier remained below the 1.25 maximum.

## D. Neutral-region behavior

A natural near-neutral village candidate also appeared:

- village: Hertogea;
- bound fortification: Hertogea Castle;
- Security: 50.7775078;
- native weight: 0.25;
- multiplier: 0.996112466;
- final weight: 0.249028116.

Exact Security 50 was not required. Deterministic tests continue to prove the exact neutral branch.

## E. Village bound-Security path

Runtime directly exercised village resolution from the village's bound fortification.

Two examples:

- Psotai -> Syronea, Security 100;
- Tepes -> Tepes Castle, Security 43.85594.

The Phase 5 policy received the bound fortification Security and applied the same mathematics used for towns.

## F. Hideout passthrough

A native hideout invocation occurred naturally:

```
LOCAL_BANDIT_CONTROL_EVAL sample=missing-security+hideout campaignHour=649510.81641863892 settlementId=hideout_seaside_6 settlement=Hideout candidateKind=Hideout policyKind=Unsupported boundSettlementId=<none> boundSettlement=<none> securityAvailable=False security=<unavailable> nativeWeight=0.111111112 controlMultiplier=1 finalWeight=0.111111112 applied=False reason=unsupported-candidate hook=postfix originalExecuted=True mutationByClanAI=False
```

The native weight and final weight are exactly equal and `applied=False`.

This runtime-exercises the required hideout passthrough branch without broadening Phase 5 into hideout spawning.

## G. Native looter creation authority

Bannerlord naturally created a looter party during the same native spawning window:

```
LOCAL_BANDIT_CONTROL_NATIVE_LOOTER_CREATED campaignHour=649510.79714630556 partyId=looters_50217 party=Looters clanId=looters culture=looters template=looters_template partyComponent=TaleWorlds.CampaignSystem.Party.PartyComponents.BanditPartyComponent homeSettlementId=castle_village_A6_2 homeSettlement=Lamesa source=native-mobile-party-created-event mutationByClanAI=False
```

The observer subscribed only to Bannerlord's `MobilePartyCreated` event and read the already-created party.

Phase 5 did not call `BanditPartyComponent.Create*`, `MobileParty.CreateParty`, or any equivalent party-creation API.

This proves native looter creation was observed while Phase 5 supplied only relative settlement weights.

The evidence does **not** claim that a particular captured weighted candidate caused this specific party to be selected; no unsupported causal linkage is inferred.

## H. Global population authority / no direct mutation

Phase 5 still does not patch:

- `SpawnLooters`;
- global looter cap/refill logic;
- `SpawnBanditsAroundHideout`;
- hideout infestation/replacement;
- party templates/culture;
- party movement/destruction;
- patrol generation;
- lord target scoring.

No direct party or settlement mutation API appears in Phase 5 runtime source.

The runtime proof therefore characterizes the selected relative-weight seam while preserving Bannerlord's global population and actual creation authority.

## Telemetry health and safety

Phase 5 telemetry error count:

`0`

TestRunner save-command count for the fresh run:

`0`

Exit:

`EXIT_NOSAVE`

Post-run verification:

- Bannerlord closed: yes;
- protected fixture SHA-256 unchanged: `A91F15BF1403F1D29F942C56A1162F431113942CDACCAFE52F4D80303B4CB427`;
- protected fixture timestamp unchanged: `2026-09-24T17:20:12.2384633Z`;
- Strategic Commitment: `Mode=Observe`;
- Visual War marker: absent;
- rollback SHA-256 intact: `F8EC94C8973C180F7BEA688016B272C690728A188D61EF05166E3ED51AE445DC`;
- live Phase 5 candidate SHA-256: `E4DB15DAF5C437F45F559CADDD3F385CE062416D2896EE451826D961D9DDDF7A`.

## Evidence

Exact runtime lines and fresh TestRunner command history:

`Reports/Security/evidence/phase5_local_bandit_control_v1_runtime_20260925.txt`

Offline telemetry validation:

`Reports/Security/evidence/phase5_local_bandit_control_v1_runtime_telemetry_validation_20260925.txt`

## Capability status

Phase 5-v1:

- **Implemented:** yes.
- **Build-proven:** yes.
- **Runtime-observed:** yes.
- **Native looter creation observed:** yes.
- **Hideout passthrough runtime-observed:** yes.
- **Balance-proven:** NO.

This runtime characterization proves the selected bounded seam executes as designed. It does not establish long-run balance, bandit-density equilibrium, or an ideal 0.75..1.25 tuning range.

## Checkpoint

Stop at this Phase 5-v1 runtime characterization checkpoint.

Do not retune Phase 5 from this result alone. Do not reopen Phase 4. Phase 6 has not started in this task.
