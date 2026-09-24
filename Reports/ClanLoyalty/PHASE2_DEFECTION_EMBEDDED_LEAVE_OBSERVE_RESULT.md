# Phase 2 — embedded leave-memory observation result

Date: 2026-09-24

Candidate: `v0.21M2-defection-leave-observe`

DLL SHA-256: `5ED68249A75E4A6FCE2F10246DEBB26A9E83FA76B1BCC807FB2B141B4EA7FE51`

Fixture: `ClanAI V020V PERSIST L6 REAL URIKSKALA LOSS 20260924`

The candidate was observation-only. It measured Bannerlord's exact native leave-current-kingdom subscore during target defection and calculated the counterfactual result of carrying the already-proven leave-memory modifier into that embedded native component. It did not modify the live defection barter value.

## Bounded result

Campaign hours: 649112.305 → 649531.369

Natural `SOCIAL_DEFECTION_CONSIDER` samples: **13**

- native leave subscore observable: **13 / 13**
- active direct-loss memory on sampled clan: **0**
- non-zero proven leave-memory carry: **0**
- counterfactual carry YES: **0**
- actual adjusted YES: **0**
- actual commits: **0**
The hypothesis was therefore **not exercised** in this bounded window. The correct result is not "carry failed"; it is:

`NULL_NO_ACTIVE_LEAVE_MEMORY_SAMPLED_FOR_DEFECTION`

The closest observed switch candidate in this particular M2 run was Banu Arbas, Aserai → Vlandia:

- adjusted sum: -657,932
- embedded native leave score: -664,894
- carry modifier: 0
- affordability: satisfied

Target-specific social memory remained independent: for example Vatatzes → Battania received the existing -1,638 target/social modifier while its current-kingdom leave-memory carry remained zero.

## Interpretation

This run confirms that Bannerlord's embedded leave component can be measured safely at the native defection surface without changing behavior. It does not yet establish whether propagating proven leave memory into that component will naturally cross a target-switch boundary.

The Urikskala fixture contains real persisted loss memory, but native target-defection sampling did not select the loss-bearing clan during this 419-campaign-hour window.

The next safe step is to seek a natural fixture/run where an active loss-bearing clan is actually sampled for `ConsiderDefection`; do not implement the carry merely because the formula is available.
## Evidence

- `Reports/ClanLoyalty/evidence/phase2_defection_embedded_leave_observe_20260924/validation.json`
- `Reports/ClanLoyalty/evidence/phase2_defection_embedded_leave_observe_20260924/considerations.log.txt`

Target-kingdom autonomous defection remains unproven.
