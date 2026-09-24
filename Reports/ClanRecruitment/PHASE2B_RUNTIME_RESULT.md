# Phase 2B — ruler recruitment runtime result

Date: 2026-09-24

Candidate: `v0.22A-ruler-courtship-native-v1`

Commit: `fef4d943b04a221939a53f252b383dc4bca50f64`

DLL SHA-256: `E6AF9EC037C61E8EB094A5AFB8CD9BB737B0862D089C6ABC5D7051A46431CA9F`

## Result

**Phase 2B strong win condition passed.**

Vlandia's ruler-side ClanAI consideration selected naturally independent Banu Ruwaid as the best eligible clan by Bannerlord's native kingdom desirability score. Both native join values produced a positive combined surplus, ClanAI invoked only the native AI barter, and immediate post-state confirmed Banu Ruwaid had joined Vlandia.

No custom score, relation edit, memory injection, wealth edit, direct faction mutation, or direct `ChangeKingdomAction` was used.

## Strong proof

- recruiter: Vlandia;
- ruler: Morcon;
- selected clan: Banu Ruwaid;
- leader: Taq;
- pre-state: independent;
- strength: 225.59;
- war-party limit: 2;
- holdings: 0;
- culture match: false;
- native kingdom selection / barter value: 7,401.263 / 7,401;
- native clan join value: +649;
- native combined surplus: +8,050;
- native AI barter attempted: true;
- post-state: Banu Ruwaid in Vlandia;
- committed: true.

The runner's `LastBarterIsAccepted` snapshot was false even though the synchronous post-state was committed. The authoritative completion check is actual clan membership after `ExecuteAiBarter`, as preregistered.

## Natural setup

The source fixture was the already-proven Phase 1 Banu Ruwaid voluntary-leave save, created by a natural holding-loss-driven departure:

`ClanAI V020V PERSIST PHASE1 BOUNDARY WIN BANU RUWAID 20260924`

Save SHA-256: `3D49AEF69B1F9CA453A22748A45C7129472883BB9690EF0FA6D2A54B9FB0834B`

The test exited without saving, so the source fixture was not overwritten.

## Supporting nulls

An initial Urikskala fixture advanced 29.955 campaign hours and produced nine ruler considerations. All seven NPC kingdoms were sampled, but no eligible independent clan existed. No barter was attempted. This preregistered null established that the behavior does not invent a candidate.

On the Banu Ruwaid fixture, Sturgia and Aserai first selected Banu Ruwaid but correctly did not attempt barter because their native combined values were negative:

- Sturgia: `-38,045 + 7,271 = -30,774`;
- Aserai: `-6,719 + 2,438 = -4,281`.

Vlandia later reached the positive native boundary and committed the join.

## Runtime safety

- the prior live DLL was preserved at `Builds/Rollback_Phase2B_v021M3_20260924_ClanAI` with SHA-256 `9F4E050211AB2B76148FF8D5EDB173AFD305DCEEA7338F610C6764AFCE5A1C51`;
- deployment occurred only while Bannerlord was stopped;
- the candidate initialized with restored ClanAI systems;
- both runs ended through TestRunner `EXIT_NOSAVE`;
- no `RULER_COURTSHIP_FAILED` entry occurred.

## Evidence excerpt

See `evidence/phase2b_ruler_courtship_20260924/considerations.log.txt`.
