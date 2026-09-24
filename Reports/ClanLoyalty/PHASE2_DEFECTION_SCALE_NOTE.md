# Phase 2 — target-defection scale note

Date: 2026-09-24

This note is required by the preregistered protocol before any change to `SocialDefectionPatch`.

## Evidence trigger

The first post-preregistration natural run produced 15 target-kingdom defection considerations and no positive decision or commit.

The closest observed candidate was Vezhoving → Nord:

- adjusted sum: -169,423
- adjusted affordability: satisfied
- social modifier: 0
- current absolute social-defection cap: 25,000

Therefore the current cap could not have flipped this observed candidate even at its maximum positive value. A hypothetical +25,000 would still leave the sum near -144,423.

The only actually modified case was Maneolis → Battania:

- native sum: -810,801
- modifier: -1,638
- adjusted sum: -812,439
- affordability: satisfied

That modifier was directionally correct for the negative target memory, but far from a boundary crossing.
## What this does and does not establish

The observed sample shows a **real scale mismatch for the candidates that actually occurred**.

It does not establish that a future natural candidate can never fall within 25,000 of zero. Candidate availability and memory-target alignment are themselves sparse: only one of 15 sampled target choices had a non-zero memory modifier.

Affordability cannot be blamed as the primary blocker in this run. Nine cases were unaffordable, but every one of those also had a negative adjusted sum. The closest two candidates were affordable.

## Design constraint before any future code change

Do not raise `AbsoluteCap = 25000` to an arbitrary larger number.

If later evidence justifies stronger defection influence, the additional magnitude should come from major real-world events expressed in a Bannerlord-native value scale where possible, analogous to the settlement-value approach used for voluntary leave.

Any future contribution must remain:

- bounded;
- attributable to real world/social events;
- separately logged from the ordinary social modifier;
- explainable in native decision-value terms;
- unable to call `ChangeKingdomAction` directly;
- subordinate to Bannerlord's native eligibility, target selection, affordability, barter, and action commit.
## Candidate directions to investigate, not implement yet

Potential evidence-backed inputs include a clan's own lost-fief value, major ruler betrayal/expulsion history, repeated severe war outcomes, or other durable world events whose native economic/political value can be measured.

Before selecting one, measure whether such events are actually present for naturally near-boundary source/target pairs. Do not import the voluntary-leave loss modifier wholesale merely because it exists.

No defection behavior change is authorized by this note. The next safe work is additional natural characterization or a separately reviewed design/implementation decision.
