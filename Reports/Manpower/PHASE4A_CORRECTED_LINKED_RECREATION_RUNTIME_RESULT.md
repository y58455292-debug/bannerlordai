# Phase 4A — Corrected same-hero native recreation runtime characterization

Date: 2026-09-25 UTC  
Source candidate: `55cb7ded907510ebab2a5e0210f8cccb28b8605f`  
DLL SHA-256: `0D23F4A66E0A1E4413E879C97E963C7DB923D8D6B4421291105D8565120CD5A8`

## Result

**PASSED — strongest positive Phase 4A linked-recreation result.**

The corrected early-participant observer produced one complete natural same-hero defeat -> old-party loss -> distinct native recreation -> creation roster -> first-settlement boundary chain.

The qualifying subject was **Megenhelda** (`heroId=lord_4_3_1`). The recreated native party appeared outside settlement with **21 regular troops already present**, then reached its first observed settlement with the same 22-member roster and `completeChain=True`.

No battle, defeat, destruction, recreation, settlement entry, troop count, recruitment, volunteer pool, garrison, economy, target, order, score, faction, settlement, or war state was forced by ClanAI.

The run stopped at the first complete qualifying chain. Phase 4B was not started.

## Bound

- observer-session start: `649491.27044636116`;
- qualifying first-settlement boundary: `649613.50488125`;
- elapsed to qualifying chain: **122.23443488884 campaign hours**;
- acknowledged paused state: `649613.7684155`;
- elapsed to acknowledged pause: **122.49796913884 campaign hours**;
- maximum: 168 campaign hours;
- stop reason: `complete-linked-chain`.

One additional unresolved link record was emitted during pause latency after the qualifying line and is preserved separately in the raw evidence but excluded from the proof window.

## Exact causal chain

### 1. Accepted defeat identity

At campaign hour `649520.37804783334`, Megenhelda's defeated-side identity was accepted from the earlier exact participant capture even though the live end-time leader was already gone:

```
2026-09-25T08:14:01.1493801Z [ClanAI v0.22A-ruler-courtship-native-v1] PHASE4A_LINK_DEFEAT heroId=lord_4_3_1 hero=Megenhelda nativePartyId=lord_4_3_1_party_1 partyId=lord_4_3_1_party_1 party=Megenhelda's_Party actor=<none> clan=dey_Tihr kingdom=Vlandia campaignHour=649520.37804783334 defeatedSide=Defender heroIdentitySource=native-leader-at-same-battle-participant-capture participantSource=MapEventStarted participantCaptureHour=649516.338232 participantSide=Defender destructionObserved=False destructionHour=<unavailable> roster=Total=0,Healthy=0,Wounded=0,Heroes=0,T0=0,T1=0,T2=0,T3=0,T4=0,T5=0,T6=0,T7Plus=0
```

Observed fact: the accepted line uses the corrected early capture, not a live end-time leader; `participantSide=Defender` equals the native `defeatedSide=Defender`.

The accepted logger is downstream of the validated exact-reference gates in this candidate: the cached `MapEvent`, `PartyBase`, and `MobileParty` references must all match the ending defeated-side participant. Clan/name matching and later destruction cannot satisfy this gate.

### 2. Old native party loss

At the same campaign hour, the exact defeated party produced a native destruction callback:

```
2026-09-25T08:14:01.1562665Z [ClanAI v0.22A-ruler-courtship-native-v1] PHASE4A_LINK_PARTY_DESTROYED heroId=lord_4_3_1 hero=Megenhelda nativePartyId=lord_4_3_1_party_1 campaignHour=649520.37804783334 source=MobilePartyDestroyed
```

This destruction is evidence only after the defeat identity had already been accepted; it was not used to manufacture the defeat.

### 3. Same-hero distinct native recreation

At campaign hour `649603.91058702779`, **83.532539194449782 campaign hours after the accepted defeat**, Bannerlord emitted a native lord-party creation for the same hero StringId:

```
2026-09-25T08:15:41.9621818Z [ClanAI v0.22A-ruler-courtship-native-v1] PHASE4A_LINK_CREATION heroId=lord_4_3_1 hero=Megenhelda nativePartyId=lord_4_3_1_party_1 partyId=lord_4_3_1_party_1 party=Megenhelda's_Party actor=Megenhelda clan=dey_Tihr kingdom=Vlandia campaignHour=649603.91058702779 linked=True classification=PostDefeatNativeRecreationInitialTroopsSupported priorPartyGone=True initialBeforeSettlement=True rosterAvailable=True regularTroops=21 defeatHour=649520.37804783334 defeatedPartyId=lord_4_3_1_party_1 defeatToRecreationHours=83.532539194449782 destructionObserved=True destructionHour=649520.37804783334 roster=Total=22,Healthy=22,Wounded=0,Heroes=1,T0=0,T1=6,T2=4,T3=6,T4=1,T5=4,T6=0,T7Plus=0 partyLimit=138 partyRatio=0.1594203 foodDays=23 food=26 currentSettlement=<none> targetSettlement=<none>
```

The displayed party StringId was reused, but StringId reuse is not the distinct-party proof. In this candidate, `linked=True` is possible only when the recreated native `PartyBase` is not reference-equal to the defeated `PartyBase`, and the prior party is destroyed or absent. The runtime line also records `priorPartyGone=True` and `destructionObserved=True`.

Creation-time state:

- total: **22**;
- heroes: **1**;
- regular troops: **21**;
- healthy: **22**;
- wounded: **0**;
- tiers: T0 0, T1 6, T2 4, T3 6, T4 1, T5 4, T6 0, T7+ 0;
- party limit: **138**;
- ratio: **0.1594203**;
- food: **26**;
- food days: **23**;
- current settlement: **none**;
- target settlement: **none**.

This is direct evidence that the linked recreated native party already contained regular troops outside settlement.

### 4. First settlement boundary

Megenhelda's first observed settlement interaction was Pravend at campaign hour `649613.50488125`, **9.5942942221881822 hours after creation**:

```
2026-09-25T08:15:49.9516924Z [ClanAI v0.22A-ruler-courtship-native-v1] PHASE4A_LINK_FIRST_SETTLEMENT_PRE heroId=lord_4_3_1 hero=Megenhelda nativePartyId=lord_4_3_1_party_1 partyId=lord_4_3_1_party_1 party=Megenhelda's_Party actor=Megenhelda clan=dey_Tihr kingdom=Vlandia campaignHour=649613.50488125 createdHour=649603.91058702779 defeatHour=649520.37804783334 defeatedPartyId=lord_4_3_1_party_1 defeatToRecreationHours=83.532539194449782 recreationToVisitHours=9.5942942221881822 destructionObserved=True destructionHour=649520.37804783334 classification=PostDefeatNativeRecreationInitialTroopsSupported completeChain=True reason=BeforeSettlementEnteredEvent firstSettlement=Pravend[town_V3] creationRoster=Total=22,Healthy=22,Wounded=0,Heroes=1,T0=0,T1=6,T2=4,T3=6,T4=1,T5=4,T6=0,T7Plus=0 observedRoster=Total=22,Healthy=22,Wounded=0,Heroes=1,T0=0,T1=6,T2=4,T3=6,T4=1,T5=4,T6=0,T7Plus=0 preSettlementNetChange=0 observedPositiveGrowth=0 initialRegularTroops=21 observationFault=False partyLimit=138 partyRatio=0.1594203 foodDays=25 food=28 currentSettlement=Pravend[town_V3] targetSettlement=Pravend[town_V3]
```

Immediately before first settlement interaction:

- roster remained **22 total / 1 hero / 21 regular**;
- pre-settlement net headcount change: **0**;
- observed positive pre-settlement growth: **0**;
- first settlement: **Pravend**;
- `completeChain=True`;
- observation fault: false.

Thus the 21 regular troops were present at native creation and remained present to the first settlement boundary; they were not acquired during an observed pre-first-settlement visit.

## Session-wide corrected-observer evidence

Through the qualifying cutoff:

- `PHASE4A_LINK_RESET`: 1;
- accepted `PHASE4A_LINK_DEFEAT`: **31**;
- unresolved defeat records: 168;
- `PHASE4A_LINK_PARTY_DESTROYED`: 21;
- native creations: 25;
- generic creations: 24;
- linked initial-troop recreation: **1**;
- complete first-settlement chains: **1**;
- observer errors: **0**.

The unresolved records remain unresolved; they are not retroactively promoted. The 24 generic creations remain `GenericNativePartyCreation`.

## Classification

Final classification for the qualifying chain:

`PostDefeatNativeRecreationInitialTroopsSupported`

Observed fact: the same hero StringId was tied to an accepted exact defeated participant, the old native party was destroyed, a distinct native party creation linked to that defeat, the new party had 21 regular troops outside settlement, and the first pre-entry boundary was complete.

Interpretation: on the supported Bannerlord build, native post-defeat lord-party recreation can initialize a defeated lord's newly created party with regular troops before that party's first observed settlement interaction.

This result does **not** establish a global fixed respawn troop count, a universal rule for every lord, or the exact internal source/model that selected 21 regular troops. It proves one natural causal chain.

## Deployment and cleanup safety

Bannerlord was closed before deployment.

- deployed candidate SHA-256: `0D23F4A66E0A1E4413E879C97E963C7DB923D8D6B4421291105D8565120CD5A8`;
- rollback SHA-256: `0FE236EDA21FFED93F14003A588C5A97712A5CED66341413612244C40BF1F755`;
- Strategic Commitment: `Mode=Observe`;
- Visual War marker: absent.

The protected fixture `ClanAI V020V PERSIST DEMO GATE V021M 20260924` remained unchanged after `EXIT_NOSAVE`:

- SHA-256 `A91F15BF1403F1D29F942C56A1162F431113942CDACCAFE52F4D80303B4CB427`;
- modification time `2026-09-24T17:20:12.2384633Z`.

Fresh TestRunner chronology contains no save command. Bannerlord was closed after exit.

## Evidence

Exact raw link evidence, chain lines, deployment/result/cleanup records, and TestRunner chronology are preserved in:

`Reports/Manpower/evidence/phase4a_corrected_linked_recreation_runtime_20260925.txt`

The in-scope normalized-LF link trace SHA-256 is:

`c1a2fdfa4f3cd1546f84944011f9561acdccc01e145b1eb803b1dcfb58b5d920`

## Scope closure

The earlier 164.281-hour run remains an honest bounded null for the old end-time identity observer and is not reinterpreted.

This fresh run validates the corrected observation seam at runtime and supplies the missing same-hero causal link for one native recreation with initial troops.

No source change was made during this runtime checkpoint.

**Stop at this Phase 4A checkpoint. Do not start Phase 4B.**
