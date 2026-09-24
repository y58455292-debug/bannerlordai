# Phase 2 — defection loss-context observation

Date: 2026-09-24

Candidate: `v0.21M1-defection-loss-observe`

DLL SHA-256: `373949D907E1F6F1E6EF4F319EA704BBD2A39DDD46AEC901485F4926B0EBE502`

Fixture: `ClanAI V020V PERSIST L6 REAL URIKSKALA LOSS 20260924`

This candidate was observation-only. It appended persisted exact-clan fief-loss context to natural `SOCIAL_DEFECTION_CONSIDER` logs and did not add that pressure to the defection score.

## Result

Natural target-kingdom considerations: **26**

- direct-loss memory present: **4**
- native YES: **0**
- adjusted YES: **0**
- commits: **0**
- counterfactual flips if the entire already-recorded loss pressure were added: **0**

Loss memory therefore does naturally overlap with target-kingdom defection evaluation, but simply adding the existing loss pressure would not have crossed the boundary in this observed sample.

## Loss-bearing cases

**Dionicos: Calradian Empire → Sturgia**

- adjusted sum: -804,501
- loss pressure: +750,000
- counterfactual full-loss sum: **-54,501**
- counterfactual demand: 72,459
- affordable: 589,574

This was the closest loss-bearing case after the observation-only counterfactual. Affordability would have been satisfied, but the score would still be negative.

**Gundaroving: Sturgia → Nord**

- adjusted sum: -322,273
- loss pressure: +135,416
- counterfactual full-loss sum: **-186,857**
- affordability satisfied

**Banu Ruwaid: Khuzait → Vlandia**

- adjusted sum: -569,820
- loss pressure: +374,537
- counterfactual full-loss sum: **-195,283**
- affordability satisfied

Banu Ruwaid later received a different natural target (Calradian Empire); the older loss had decayed further and would leave the counterfactual sum at -639,853.

## Interpretation

This closes one uncertainty from the initial scale note:

- major real-world loss memory **can** coexist with a native target-kingdom consideration;
- its native-value scale is materially larger than the 25k social cap;
- but raw loss pressure alone did not solve any observed target-switch boundary.

The result argues against blindly copying the voluntary-leave pressure into defection.

The next design should respect Bannerlord's own score composition: the clan-side defection barter value already contains a native leave-current-kingdom component. The appropriate question is whether the **proven memory adjustment to that embedded leave component** should carry into defection, while target-specific desirability remains separately bounded.

## Evidence

- `Reports/ClanLoyalty/evidence/phase2_defection_loss_context_20260924/validation.json`
- `Reports/ClanLoyalty/evidence/phase2_defection_loss_context_20260924/considerations.log.txt`

No target-kingdom autonomous defection is proven by this result.
