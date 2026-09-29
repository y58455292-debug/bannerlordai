# Living World Direction

Post-RC1 design charter, documented at LW0 on 2026-09-29. This is direction, not an implementation claim. [PROJECT_LAW.md](PROJECT_LAW.md) remains binding.

## 1. Why BannerlordAI exists

Bannerlord already has strong native systems. The problem is that NPCs often do not appear to live meaningful lives inside them. BannerlordAI should connect people, homes, responsibilities, experiences and real actions; it must not drift back toward “war-only smarter map AI.”

## 2. North star

NPCs should usually have understandable reasons for:

- where they are;
- why they left home;
- what they are trying to accomplish;
- who they care about;
- what place they are responsible for;
- why they return;
- who requested their help;
- what recent experiences affect their judgment.

## 3. Core architectural loop

Home → responsibility → need/event → purpose → native action → result → return → memory

## 4. Central design rule

The player manages responsibilities and major commitments. Adult family members, companions, governors and commanders execute routine responsibilities autonomously.

## 5. Home is the default

Home is a center of gravity, not a prison. Clan territory matters during both peace and war. Leaving should increasingly have a legitimate reason. Returning becomes relevant when the reason ends.

## 6. Peace is not downtime

Nobles should have meaningful peacetime lives: governing, local security, patrol, recruitment/recovery, resupply, prisoner handling, family/social activity, trade, estate recovery and preparing for future threats.

## 7. Household structure

Long-term clan roles may include clan leader, governor, local warden, expeditionary commander, logistics/supply, caravan leader, political representative and unassigned family member.

## 8. Player authority

The player decides major commitments: where someone is based, who governs, who commands, who is responsible for a holding, whether kingdom service is accepted, and major marriages/commitments where Bannerlord gives player authority. NPCs handle ordinary execution.

## 9. Kingdom authority

Rulers should request player-clan service rather than silently commandeer family members. External service should be temporary and return to prior household responsibility when complete.

## 10. Governors should eventually govern

Use real actors and real Bannerlord systems. Do not directly grant settlement resources or stats. Preferred pattern:

settlement condition → recognized need → responsibility → actor attempts real action → Bannerlord world determines success/failure

## 11. Settlements should flourish emergently

No generic flourish bonus. Healthy outcomes should emerge through security, recovery, trade, recruitment, garrison health, sensible civic choices and related real activity.

## 12. Social attitude

world event → personal interpretation → memory → current attitude/pressure → advice/action → consequence → new memory

Do not copy RimWorld literally.

## 13. Relationships should create activity

Relationships may create reasons to visit, support, avoid, request help, negotiate, exchange prisoners, pursue marriage, advise, cooperate and return favors. Banter is not a substitute for social simulation.

## 14. Advisement

Advice should come from responsibility, expertise, relationship, personal history and direct consequences. Advice informs leaders; it does not automatically control them.

## 15. Purposeful movement

Before adding destinations, ask whether existing state can generate legitimate purpose. Preferred movement loop:

home → task → destination → result → return

## 16. Native authority

Preserve PROJECT_LAW.md:

- Bannerlord decides legality.
- Bannerlord provides candidates/targets.
- Bannerlord performs final mutation.
- ClanAI supplies bounded context, priorities, memories and preferences.

Do not create parallel economy, movement, recruitment, trade or diplomacy systems unless evidence proves native systems cannot support the desired behavior.

## 17. Prove actions, not scores

Where practical record native state, ClanAI contribution, changed decision, native committed action and resulting world state. A score change is not an action commit. Nulls remain valid evidence.

## 18. Optimization

Maximum believable behavior for bounded computational cost.

Prefer event-driven state, staggered slow evaluation, existing AI ticks, small candidate sets, cached clan/territory context, bounded memories/social graphs, dormant healthy settlements, direct home references and no unnecessary global scans.

## 19. Reuse the existing project

Current foundations to reuse and preserve, with their existing evidence limits:

- SocialLedger / noble memory;
- Home Responsibility;
- Kingdom Objectives;
- Strategic Decision Composer;
- ruler courtship;
- companion memories;
- WarState/WarScar;
- manpower/troop quality;
- local security;
- settlement audits;
- kingdom/generational continuity;
- save/load infrastructure;
- deterministic policies;
- commit verification;
- release packaging.

Existing Home Responsibility does not mean Persistent Home Assignment is implemented. Reuse accepted evidence and preserve unresolved boundaries and nulls.

## 20. Avoid scope explosion

Every milestone should create one visible, testable improvement. Preferred current order:

1. Persistent Home Assignment / peacetime responsibility.
2. One governor/local-need response.
3. Household autonomy inside assignments.
4. Local purposeful economic/logistics activity.
5. Kingdom requests for clan service.
6. Ruler approaches/recruitment involving the player.
7. Household advisement.
8. Purposeful social visits/relationships.
9. Broader settlement flourishing and long-term social integration.

Next development checkpoint: **LW1-A — Persistent Home Assignment native-seam/design audit**. LW1 is not implemented. LW0 ends after the documentation commit; no LW1 audit or implementation begins in this task.

## 21. Feature evaluation checklist

Before implementing a feature answer:

1. What player-observed problem does this solve?
2. Does Bannerlord already have most of the mechanic?
3. Can an existing native candidate/action be reused?
4. Who is responsible?
5. Where is that actor's home?
6. Why would they travel?
7. What causes them to stop/return?
8. What memory/relationship should matter?
9. How will the player notice?
10. How will actual native action be verified?
11. What is the bounded performance cost?
12. Can this be demonstrated before adjacent systems?

If unclear: research first.

## 22. Target player experience

> I am the leader of one clan inside a living political world.
> My family has homes and responsibilities.
> My governors attempt to govern.
> My commanders protect places they care about.
> My relatives live lives of their own.
> Kings ask for our service.
> Rulers may recruit us.
> My family advises me and sometimes disagrees.
> Other clans behave similarly.
> People travel because something matters, and when that purpose ends they usually return.

## Frozen playable baseline

RC1 remains frozen and playable while post-RC1 development follows this charter.

- RC1 commit: `9ec113e738af35254e113fbde7c05babbf3c405d`
- RC archive: `Releases/ClanAI-v0.22.0-RC1.zip`
- RC archive SHA-256: `A03D183934BCF1C974EA6BCEBBF35CFF3A5413080D6F6634942811AE54C971D6`
- RC DLL SHA-256: `A18341FB2CD6B8623B45B52155C1A7D987DBEE7AAE14AE7DBF704514EC7F37B5`

This charter does not change the frozen archive, tested package, gameplay, policy or save schema.
