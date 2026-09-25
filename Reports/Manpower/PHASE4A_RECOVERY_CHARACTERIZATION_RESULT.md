# Phase 4A — Vanilla AI Recovery Characterization

Date: 2026-09-25  
Repository candidate: `dfa18f3e56a90fbe90c6467a4680e2d545fadcf4`

## Result

**PASSED — first bounded characterization.**

The observation-only Phase 4A seam captured multiple natural AI lord recovery patterns in a 39.623-campaign-hour run, well inside the 168-hour cap.

No defeat, respawn, recruitment, settlement visit, garrison transfer, target, movement, roster change, economy change, or AI decision was forced by ClanAI.

The primary characterization is **Kulyat of the Forest People**, a naturally severe Battania lord party observed at session start. His party grew from 37 to 69 troops during early recovery. The telemetry separates settlement-supported recruitment from outside-settlement growth whose source remains unresolved.

Supporting observations also captured:
- native `MobilePartyCreated` lord parties already carrying troops before any observed settlement interaction;
- naturally defeated-side lord parties immediately after battle with wounded troops;
- a separate castle visit where party growth matched a garrison decrease while the volunteer pool remained unchanged, supporting a native garrison withdrawal.

## Runtime safety

Bannerlord was closed before deployment.

- observer candidate DLL SHA-256: `5CA45B7AE095E9B3F6F77E06FCCA287414FBAC7D345648AAAD58F39EA1919BF6`;
- prior installed DLL / rollback SHA-256: `0267089ABE829248F145A468355041C17A91B8945816A8210BA0C6AD23CB4B5E`;
- verified rollback: `D:\BannerlordAIResearch\Builds\Rollback_Phase4A_Recovery_20260925_ClanAI`;
- live DLL after run remained the observer candidate.

Unrelated experimental state stayed safe:
- `StrategicCommitment.cfg = Mode=Observe`;
- Visual War marker absent.

The protected fixture `ClanAI V020V PERSIST DEMO GATE V021M 20260924` was loaded read-only.

After `EXIT_NOSAVE`:
- SHA-256 remained `A91F15BF1403F1D29F942C56A1162F431113942CDACCAFE52F4D80303B4CB427`;
- modification time remained `2026-09-24T17:20:12.2384633Z`;
- Bannerlord was closed;
- rollback remained present and verified.

## Bound

- planned maximum: 168 campaign hours;
- start: `649491.27044636116`;
- paused: `649530.89386202779`;
- elapsed: `39.6234156666` campaign hours.

The run stopped early because qualifying recovery evidence was already present.

## Initial severe-depletion population

At session launch the observer found 40 NPC lord parties with `PartySizeRatio <= 0.35`.

This threshold is observation-only and does not affect Bannerlord behavior.

These start snapshots provide natural severely depleted subjects without manufacturing a battle or respawn.

## Primary subject — Kulyat of the Forest People

Identity:

- party id: `CharacterObject_19192_party_1`;
- clan: Forest People;
- kingdom: Battania;
- party-size limit: 133.

### First observed severe state

At campaign hour `649491.27044636116`:

- total: 37;
- healthy: 37;
- wounded: 0;
- hero: 1;
- regular tiers:
  - T1: 5
  - T2: 12
  - T3: 11
  - T4: 8
- ratio: `0.2781955`;
- food days: 22;
- current settlement: none;
- target settlement: Sibir.

This is a natural **severe-depletion-at-session-start** observation. The prior cause is not inferred.

### Before first settlement interaction

At campaign hour `649492.13191872218`, elapsed `0.8614723610` hours, Kulyat reached Sibir.

Immediately before the first observed settlement interaction:

- party total remained 37;
- roster/tier composition remained unchanged;
- Sibir volunteer pool: 20;
- Sibir garrison: 423.

Thus **37 troops were already present before the first observed settlement visit**, and no pre-visit headcount growth occurred for this subject.

### Sibir visit 1 — recruitment supported

Exit at campaign hour `649493.50573841669`, elapsed `2.2352920555` hours:

- party: 37 -> 44 (**+7**);
- volunteer pool: 20 -> 13 (**-7**);
- garrison: 423 -> 423 (**0**);
- regular T1: 5 -> 12;
- other recorded tier buckets unchanged.

Classification:

`SettlementRecruitmentSupported`.

Observed fact: party headcount rose by seven while the volunteer pool fell by seven and the garrison did not change.

Interpretation: settlement recruitment is directly supported; garrison withdrawal is not supported for this visit.

### Kvol visit — recruitment supported

At campaign hour `649494.68143505557`, elapsed `3.4109886944` hours:

- party: 44 -> 53 (**+9**);
- volunteer pool: 18 -> 9 (**-9**);
- village garrison: unavailable / not applicable;
- T3: 11 -> 15;
- T4: 8 -> 13.

Classification:

`SettlementRecruitmentSupported`.

### Growth outside an observed settlement visit — unknown source

At elapsed `4.7782096389` hours:

- party: 53 -> 54 (**+1**);
- party was outside a settlement.

Classification:

`OtherOrUnknownNativeSource`.

No recruitment/garrison cause is inferred from the count increase alone.

### Sibir visit 2 — recruitment supported, garrison unchanged

Across the next Sibir visit:

- party: 54 -> 58 (**+4**);
- volunteer pool: 20 -> 15 (**-5**);
- garrison: 424 -> 424 (**0**).

Classification:

`SettlementRecruitmentSupported`.

The volunteer decrease exceeds party growth by one, so this visit is evidence-compatible with recruitment but the telemetry does not claim every volunteer-pool change belonged to Kulyat.

### Radakmed visit — recruitment supported

At elapsed `17.0622438333` hours:

- party: 58 -> 66 (**+8**);
- volunteer pool: 18 -> 10 (**-8**);
- village garrison: unavailable / not applicable.

Classification:

`SettlementRecruitmentSupported`.

### Later outside-settlement growth remains unresolved

Subsequent hourly snapshots recorded:

- 66 -> 67;
- 67 -> 68;
- 68 -> 69;

while outside observed settlement visits.

Each was classified:

`OtherOrUnknownNativeSource`.

The observer deliberately does not relabel these as recruitment.

### Kulyat accounting through early recovery

From 37 to 69, net growth was **+32**.

Observed settlement-supported growth:
- Sibir 1: +7;
- Kvol: +9;
- Sibir 2: +4;
- Radakmed: +8;
- total settlement-supported increase: **+28**.

Observed outside-settlement increase:
- four separate +1 steps;
- total unresolved increase: **+4**.

Thus this characterization can account for the entire 37 -> 69 headcount increase without pretending the unresolved +4 has a known source.

At the later 69-troop snapshot, Kulyat's regular tier mix had shifted to approximately:
- T1: 17;
- T2: 17;
- T3: 16;
- T4: 18;
- hero: 1.

Tier redistribution can occur through native upgrades; it is recorded separately from headcount-source classification.

## Supporting evidence — created lord parties already contain troops

Two native `MobilePartyCreated` events were observed with troops already present while the party was outside a settlement:

### Pelicos of the Legion

At campaign hour `649517.467716`:

- new native lord party;
- total: 29;
- healthy: 29;
- current settlement: none;
- source label: `InitialPresenceBeforeSettlement`.

### Zachanis

At campaign hour `649527.4639460278`:

- new native lord party;
- total: 17;
- healthy: 17;
- current settlement: none;
- source label: `InitialPresenceBeforeSettlement`.

Observed fact: newly created native lord parties can appear with non-zero troops before observed settlement interaction.

**Important limitation:** this telemetry proves native party creation with initial troops. It does **not** independently prove that either creation was specifically a post-defeat respawn rather than another native lord-party creation path. The report therefore does not label these “free respawn troops.”

## Supporting evidence — natural defeated-side post-battle snapshots

A natural battle ending at campaign hour `649507.23171138891` produced defeated-side severe lord observations including:

- Stohrith: 32 total, 16 healthy, 16 wounded, ratio 0.2963;
- Elta: 46 total, 14 healthy, 32 wounded, ratio 0.3286;
- Megenhelda: 47 total, 24 healthy, 23 wounded, ratio 0.3406.

These are direct post-battle native snapshots. They were not followed to a settlement before this run stopped, so no recovery source is attributed to them.

## Supporting evidence — garrison withdrawal supported

Iara of fen Caernacht provides a different native recovery source.

At Ab Comer Castle, campaign hour `649529.20324575`:

- party: 67 -> 74 (**+7**);
- volunteer pool: 0 -> 0;
- garrison: 219 -> 212 (**-7**);
- garrison tier losses included T1 -1, T2 -2, T3 -2, T5 -2;
- party tier gains included T1 +1, T2 +2, T3 +1, T4 +1, T5 +2.

Classification:

`GarrisonWithdrawalSupported`.

Observed fact: the party gained seven while the castle garrison lost seven and the volunteer pool remained zero.

Interpretation: a native garrison withdrawal/transfer is strongly supported for this visit. The label remains “supported” rather than claiming the observer intercepted the underlying transfer call.

## Session-wide observation counts

Within the bounded run:

- recovery starts: 46;
- first-settlement pre-entry records: 29;
- settlement exits: 88;
- post-battle records: 4;
- tracked recovery ends: 3.

Source-label occurrences:

- `SettlementRecruitmentSupported`: 26;
- `GarrisonWithdrawalSupported`: 1;
- `MixedSettlementSources`: 0;
- `UnknownSettlementSource`: 0;
- `OtherOrUnknownNativeSource`: 21;
- `InitialPresenceBeforeSettlement`: 2.

Counts are telemetry occurrences, not unique parties and not global campaign rates.

## What this slice proves

On the supported Bannerlord version, this bounded natural characterization demonstrates that AI lord recovery can include at least:

1. **troops already present on native party creation before settlement interaction** — observed, but creation reason remains unresolved;
2. **settlement recruitment** — supported by matched party growth and volunteer-pool depletion, including repeated Kulyat examples with unchanged garrison where observable;
3. **garrison withdrawal/transfer** — supported by Iara's +7 party / -7 garrison / unchanged volunteer pool;
4. **other or unresolved native growth** — observed outside settlement visits and intentionally left unknown.

It also shows severe post-battle parties with substantial wounded counts, establishing that healthy/wounded state is observable for future recovery characterization.

## What this slice does not prove

It does not establish:

- the exact native code path that spawned Pelicos or Zachanis;
- that every volunteer decrease belongs exclusively to the tracked party when other native actors may interact with the same settlement;
- the cause of Kulyat's four outside-settlement +1 increases;
- the full long-run distribution of recovery sources;
- any balance conclusion about how many troops AI lords should receive;
- any need to change recruitment, garrisons, respawn troops, or troop tiers.

No Phase 4B behavior should be inferred from this single characterization alone.

## Conclusion

The first Phase 4A slice is complete.

A naturally severe AI lord party was followed from pre-settlement state through repeated early recovery. The observer distinguished settlement-recruitment-supported growth, unresolved outside-settlement growth, initial troops on native party creation, post-battle wounded states, and a separate garrison-withdrawal-supported visit without mutating the game.

Exact telemetry excerpts are preserved in `Reports/Manpower/evidence/phase4a_recovery_characterization_20260925.txt`.
