# Phase 2 — initial target-kingdom defection characterization

Date: 2026-09-24

Candidate: `v0.21M-player-visibility-v1`

DLL SHA-256: `74574BA34A9A9072E9FB256D1A7C25E54780CF5B4B7A1DD54B211A4AEC66CBB3`

Fixture: `ClanAI V020V PERSIST L6 REAL URIKSKALA LOSS 20260924`

Fixture SHA-256: `7236C3BA49F4E752F9E68BBBD57C813E45E15CDFD6150D50F23FF7E1070AC58F`

The run began only after `PHASE2_DEFECTION_BOUNDARY_PROTOCOL.md` was committed. No defection code, cap, relation, memory, score, faction membership, or target-selection state was changed.

## Sample

Campaign hours: 649256.009 → 649603.996

Natural `SOCIAL_DEFECTION_CONSIDER` samples: **15**

- memory-modified: **1**
- positive modifiers: **0**
- negative modifiers: **1**
- nativeWouldDefect=true: **0**
- adjustedWouldDefect=true: **0**
- committed: **0**
Affordability:

- adjusted demand affordable: **6 / 15**
- adjusted demand unaffordable: **9 / 15**
- adjusted score > 0 but unaffordable: **0**

Affordability therefore never became the decisive blocker in this run: every adjusted sum was already below zero.

## Closest natural candidates

1. **Vezhoving / Bovan: Sturgia → Nord**
   - native clan value: -173,024
   - target value: +3,601
   - native / adjusted sum: **-169,423**
   - modifier: 0
   - adjusted demand: 173,024
   - affordable amount: 1,063,954
   - affordability: satisfied

2. **Banu Atij / Jalfar: Aserai → Nord**
   - native / adjusted sum: **-239,411**
   - modifier: 0
   - affordability: satisfied

3. **Maneolis / Vipon: Calradian Empire → Battania**
   - native sum: -810,801
   - social modifier: **-1,638**
   - adjusted sum: -812,439
   - affordability: satisfied
Maneolis was the only memory-modified consideration. Its two target-kingdom social records were negative:

`targetClanState=trust:-3,grievance:8,bloodDebt:0,obligation:0,tension:6 targetClanRecords=2`

The resulting modifier correctly made Battania slightly less attractive; it did not approach the decision boundary.

## Current interpretation

**Primary observed blocker: score distance.**

The closest natural candidate was 169,423 below the required `> 0` sum and already passed affordability. The current social-defection absolute cap is 25,000. Even a theoretical maximum positive +25,000 modifier on that exact candidate would leave the sum at approximately **-144,423**.

Memory/target alignment is also sparse: only 1 of 15 sampled target choices had a non-zero social-memory modifier.

This run does **not** prove that naturally closer candidates cannot occur. It does show that, in the observed sample, the current bounded social modifier cannot bridge the closest available score gap.

Target-kingdom autonomous defection remains **unproven**.
## Evidence

- `Reports/ClanLoyalty/evidence/phase2_defection_initial_20260924/validation.json`
- `Reports/ClanLoyalty/evidence/phase2_defection_initial_20260924/considerations.log.txt`
- `Reports/ClanLoyalty/evidence/phase2_defection_initial_20260924/memory.log.txt`

The historical L2 result remains useful context: 18 considerations, four modified, zero flips, zero commits, with the closest historical boundary roughly 448k below zero.
