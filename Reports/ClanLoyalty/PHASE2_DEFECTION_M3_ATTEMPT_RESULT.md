# Phase 2A — native leave-carry implementation attempt result

Date: 2026-09-24

Candidate: `v0.21M3-defection-leave-carry-v1`

Implementation commit: `e651596de4c4548e0503939edde141c1ff9955fb`

DLL SHA-256: `9F4E050211AB2B76148FF8D5EDB173AFD305DCEEA7338F610C6764AFCE5A1C51`

Fixture: `ClanAI V020V PERSIST L6 REAL URIKSKALA LOSS 20260924`

Fixture SHA-256: `7236C3BA49F4E752F9E68BBBD57C813E45E15CDFD6150D50F23FF7E1070AC58F`

## Implementation under test

M3 mirrors Bannerlord's native defection composition:

- target-specific social memory remains separately bounded by the existing 25,000 cap;
- current-kingdom memory is evaluated through the already-proven voluntary-leave modifier against Bannerlord's native embedded leave score;
- the two contributions are added to the clan-side defection value;
- Bannerlord still owns eligibility, target selection, affordability, barter resolution, and final defection action.

No raw loss-pressure bonus, cap increase, synthetic memory, relation edit, target edit, score edit, faction transfer, or direct `ChangeKingdomAction` was used.
## Timeboxed natural proof

Campaign hours: 649112.305 → 649598.375

Natural `SOCIAL_DEFECTION_CONSIDER` samples: **27**

- target-specific memory modifier non-zero: **4**
- leave-memory carry non-zero: **0**
- sampled clan with active direct-loss record: **0**
- nativeWouldDefect=true: **0**
- adjustedWouldDefect=true: **0**
- committed target switch: **0**
- adjusted demand affordable: **9 / 27**
- adjusted demand unaffordable: **18 / 27**

The target-specific side was therefore exercised naturally. The new leave-carry side was **not** naturally exercised in this bounded run because none of the 27 sampled source clans carried a non-zero proven leave-memory modifier at the moment Bannerlord selected them for `ConsiderDefection`.
## Closest candidate

**Vezhoving / Bovan: Sturgia → Nord**

- native clan value: -185,351
- target kingdom value: +4,397
- native sum: -180,954
- target-social modifier: 0
- leave-memory modifier: 0
- adjusted sum: -180,954
- native leave subscore: -183,131
- affordability: satisfied
- nativeWouldDefect: false
- adjustedWouldDefect: false
- committed: false
- kingdomAfter: Sturgia

This remains a real near-boundary candidate, but there was no relevant remembered event available to alter the decision.
## Result and disposition

**TIMEBOXED NULL — target-kingdom autonomous defection remains unproven.**

The single evidence-backed implementation attempt did not produce the preregistered:

`native NO → adjusted YES → affordability satisfied → native committed kingdom switch`

The run does not prove that the M3 carry is ineffective; the carry path simply did not overlap a naturally sampled defection candidate in the bounded window. Earlier committed evidence already showed that such overlap can occur, but the project will not continue looping on this surface.

Per the Phase 2A timebox, preserve this blocker/null and move to the next roadmap milestone: **Phase 2B — ruler recruitment / clan courtship**.

M3 remains implemented and build/runtime-observed, but **not boundary-proven and not committed-in-world**.

## Evidence

- `Reports/ClanLoyalty/evidence/phase2_defection_m3_attempt_20260924/validation.json`
- `Reports/ClanLoyalty/evidence/phase2_defection_m3_attempt_20260924/considerations.log.txt`
- `Reports/ClanLoyalty/evidence/phase2_defection_m3_attempt_20260924/target_memory.log.txt`
- `Reports/ClanLoyalty/evidence/phase2_defection_m3_attempt_20260924/leave_memory.log.txt`
