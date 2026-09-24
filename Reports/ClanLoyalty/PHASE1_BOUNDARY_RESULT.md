# Phase 1 boundary result — 2026-09-24

## Result

**PROVEN for the voluntary leave-current-kingdom path under L6.**

The preregistered boundary condition was satisfied in a natural campaign run with no loyalty-code changes and no direct manipulation of clan relations, holdings, kingdom membership, or leave values.

The proving clan was **Banu Ruwaid**, led by **Taq**.

## Natural trigger

Banu Ruwaid lost **Vladiv Castle** through a real siege ownership change:

- event source: `OnSettlementOwnerChangedEvent`
- detail: `BySiege`
- old owner: Taq
- new owner: Sidunric
- Bannerlord native settlement value: 1,801,322
- initial recorded pressure: 450,330.6

The direct loss record was created at 2026-09-24T08:34:35.5073910Z.

No synthetic loss, relation edit, faction transfer, or score edit was used.
## Primary boundary proof

At 2026-09-24T08:37:02Z, Bannerlord naturally evaluated Banu Ruwaid while it was in the **Aserai** kingdom.

- native leave value: -250,332
- social-memory component: 0
- decayed direct-loss pressure: 345,507.6
- direct-loss cap: 312,915
- applied memory modifier: +312,915
- adjusted leave value: +62,583
- nativeWouldLeave: false
- adjustedWouldLeave: true
- committed: true
- kingdomAfter: independent

This is the requested boundary shape: the native value was negative, memory pushed it above zero, and Bannerlord's original leave path committed the departure.

The -250,332 native value is effectively the preregistered ~150k-250k boundary band (332 points outside the nominal upper edge).

## Independent earlier crossing in the same natural run

The same real Vladiv Castle loss had already caused an earlier native departure from Khuzait:

- native leave value: -336,264
- applied memory modifier: +420,330
- adjusted leave value: +84,066
- committed: true
- kingdomAfter: independent

This earlier crossing was outside the preferred search band, so the later Aserai crossing is used as the primary preregistered proof.
## Pre-registered checklist

1. Natural `ConsiderClanLeaveKingdom`: **pass**
2. Triggering memory from natural world events: **pass**
3. Native <= 0 and adjusted > 0: **pass**
4. Native leave action committed and clan kingdom changed: **pass**
5. Evidence records native value, modifier, adjusted value, old kingdom, post-state, and commit: **pass**

Read-only Inspector verification after the run showed Banu Ruwaid with `Kingdom = null`, consistent with the logged native commit.

## Useful null retained

Vezhoving / Bovan naturally appeared at -183,529 and later -178,557, directly inside the preferred boundary band, but the clan had 0 settlements and 0 fiefs. It was therefore not suitable for the requested natural siege-loss setup and was not manipulated to create one.

## Preservation

Post-win save:

`ClanAI V020V PERSIST PHASE1 BOUNDARY WIN BANU RUWAID 20260924.sav`

SHA-256:

`3D49AEF69B1F9CA453A22748A45C7129472883BB9690EF0FA6D2A54B9FB0834B`

Machine-readable proof and exact log excerpts are under `Reports/ClanLoyalty/evidence/phase1_banu_ruwaid_boundary_win.*`.

## Scope

This proves the **voluntary leave-current-kingdom** boundary for the tested L6 memory path. It does not by itself prove that the separate target-kingdom defection/join bias can cross its own boundary, nor does it prove every clan will leave after a fief loss.
