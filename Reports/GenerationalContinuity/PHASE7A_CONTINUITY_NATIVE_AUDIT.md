# Phase 7A — continuity / generational native-capability audit

Date: 2026-09-25 UTC  
Authoritative source checkpoint: `05fe52003422d686eca86f2432c6f09a7e17eb0d`  
Scope: offline audit only; no Bannerlord launch, DLL deployment, gameplay implementation, or long-run campaign.

## Result

**CONTINUITY AUDIT: COMPLETE. NEW GAMEPLAY IMPLEMENTATION: NOT STARTED. FIRST RUNTIME TEST: IDENTIFIED.**

The smallest high-value next proof is one naturally occurring native leadership succession, followed by a guarded save/reload that verifies Bannerlord's family/clan/settlement/kingdom continuity and ClanAI's identity boundaries together. This does not repeat Phase 2D-L1: Phase 2D already proves seven kingdom-continuity records survive an ordinary save/load; Phase 7A's runtime target is the cross-generation transition itself and the correct survival, isolation, decay, or termination of other persisted state.

Phase 6-v1's bounded natural null is preserved unchanged. No Phase 4/5/6 balance or target-kingdom-defection work was reopened.

## Sources inspected

Only the requested handoff files and the minimum directly relevant current source/evidence were inspected:

- `PROJECT_LAW.md`
- `Reports/AgentStatus/CODEX_STATUS.md`
- `ROADMAP.md`
- `Reports/SettlementSociety/PHASE6_CIVIC_PROJECT_V1_RUNTIME_RESULT.md`
- current persistence entry points and memory implementations under `src/ClanAI/src/ClanAI/`
- accepted Phase 2D native-authority and reload evidence

No runtime or deployment action was taken.

## Native lifecycle findings

### Ruler succession

Bannerlord owns kingdom succession. A continuing kingdom retains its native kingdom identity while native kingdom-decision/action code selects and applies a ruling clan. ClanAI's `KingdomContinuityBehavior` only observes `RulingClanChanged`, records old/new ruling-clan IDs, and emits a notice. It never selects or installs a ruler.

Implication: Phase 7 should observe the native transition and its consequences, not create a ruler change.

### Clan-leader succession

Clan leadership is native clan state. When a leader dies, Bannerlord selects the successor and changes the clan leader without ClanAI assigning one. Hero-keyed ClanAI memories do not automatically become the successor's memories; clan-keyed state can continue with the clan.

Implication: the runtime proof must distinguish a new native clan leader from inheritance of the deceased hero's personal memory.

### Births, deaths, and aging

The enabled native lifecycle owns hero age progression, births, adulthood, and death. ClanAI has no birth, death, aging, fertility, or lifespan mutation in the audited continuity path.

Implication: lifecycle events must be natural. A bounded run that produces no qualifying transition is an honest null, not authorization to accelerate age or force death.

### Marriage and family continuity

Bannerlord owns spouses, children, family relationships, and the clan membership consequences of marriage. ClanAI's current personal and dynasty records refer to stable hero IDs; they do not create or rewrite family relationships.

Implication: family links should survive the guarded reload exactly as native state. Personal episodes should not silently transfer to a spouse, child, or successor merely because that person becomes clan leader.

### Settlement inheritance / ownership continuity

Settlements are owned through native settlement/clan ownership state. A clan leadership change does not require ClanAI to re-grant its settlements. Native ownership-change actions remain the only authority for an actual transfer.

Implication: after succession, the clan's pre-event settlement set and owners must remain native-consistent across reload. ClanAI must not write ownership to reproduce continuity.

### Kingdom destruction and survival

Native kingdom destruction is terminal for that kingdom object. Phase 2D records the terminal event and never resurrects it. Surviving kingdoms retain their object identity across ruler changes; culture is provenance and is not rewritten from the new ruler.

Implication: the first Phase 7 test should use a surviving kingdom succession. Kingdom destruction is a separate native outcome and is not required for the first test.

## Current BannerlordAI persisted-state inventory

| State | Save identity / scope | Existing lifetime behavior | Generational rule |
|---|---|---|---|
| Noble decision memory | Hero ID | Persisted; cumulative observation counters; no audited age decay | Same person only. Do not transfer to a successor. A dead hero's inert row may be pruned later for hygiene. |
| Social ledger | Actor hero plus actor/target clan identity | Persisted; no automatic leadership transfer was found | Personal relationship appraisal stays with the same person. Clan IDs are context, not permission to give the record to a new leader. |
| Social episodes | Rememberer hero ID, other hero/clan, event context | Persisted event history; no score mutation | Same rememberer only. Preserve as history; do not treat a successor as having lived it. |
| Social-loyalty holding-loss memory | Clan ID + lost settlement | 720-hour lifetime with pruning/decay at use | Follow the clan through leadership change, then decay normally. It represents a clan-owned holding loss, not one hero's recollection. |
| Companion duty memory | Hero ID + settlement | 72-hour lifetime; expires; forgotten when holding is no longer actor-clan owned | Same companion only; decay/expire; terminate applicability on death or loss of clan ownership. |
| Companion experience memory | Hero ID + settlement | Duty/threat windows of 168/72 hours (holdback threat 168); bounded holdings per hero | Same companion only; decay; never transfer skill/experience counters to an heir. |
| Companion negative-outcome memory | Hero ID | 336-hour lifetime; based on that hero's capture/loss observations | Same companion only; decay/expire; no inheritance. |
| Noble/social/dynasty branch observations | Hero or branch/actor IDs | Serialized observation/history state | Preserve historical records, but only expose them under an explicit lineage policy; do not silently convert personal experience into inherited memory. |
| Dynasty canon cutoff and branch ID | Campaign branch | Persisted without changing game state | Follow the campaign branch, not a particular ruler. Actor-specific canon entries still require the matching hero. |
| WarState / WarScar | Settlement, faction/kingdom pair, siege/raid/loss/objective IDs | Persisted; scars carry explicit decay; events/objectives remain structural records | Follow the affected settlement/faction/kingdom structure. Never attach them to a successor as personal memory. Terminate or close entries when their native structural subject is gone; decay scars by their existing rule. |
| Kingdom continuity ledger | Native kingdom ID | Persisted; succession count/history continues; destroyed records terminal | Follow the kingdom object across ruler generations. Never transfer to a different kingdom or revive a destroyed one. |
| Pending/session-only verification state | Party/session dictionaries and counters | Cleared on session start; not durable gameplay memory | Terminate on reload; it exists only to verify commits safely. |

## Inheritance and decay policy

The current code implies four categories:

1. **Person-bound:** NobleMemory, personal SocialLedger/SocialEpisode records, companion duty/experience/negative-outcome memory. These remain with the same hero ID and must not be inherited.
2. **Clan-bound:** holding-loss loyalty pressure. This survives a leader change because the clan suffered the loss, but it keeps its existing 720-hour decay and cap.
3. **World-structure-bound:** WarState/WarScar and kingdom continuity. These follow settlement, faction, war-pair, or kingdom identity, not a current leader.
4. **Campaign-branch history:** dynasty branch/canon metadata. The branch survives, while actor-specific access requires an explicit policy separating inherited structural history from impossible personal recollection.

Death or leadership change should not reset structural facts, and it should not make a successor remember events they did not experience.

## Concrete continuity defect

`DynastyBranchEpisodeMemory` serializes branch episodes with both `BranchId` and `ActorId`, but retrieval filters to the current `Hero.MainHero.StringId`. After a native player-heir transition, episodes recorded for the deceased/former main hero remain stored yet are no longer retrievable by the new main hero.

This is a concrete persistence/continuity defect in access semantics, not data loss:

- branch history survives serialization;
- actor-specific history remains correctly attributed;
- no explicit lineage-level retrieval or inheritance rule exists;
- therefore a new player heir cannot access even structural branch history unless it is duplicated or reclassified.

Per task scope, no fix is implemented here. Before gameplay work broadens, a focused design must decide which episode kinds remain strictly personal and which, if any, are dynasty/realm history visible to an heir. Personal choices must never be inherited as first-person memory.

A secondary hygiene gap exists across hero-keyed stores: dead-hero rows generally remain serialized until ordinary expiry or forever where no lifetime exists. Because lookup is by the same hero ID, this does not presently transfer behavior to successors, but long-run pruning should be specified before multi-year scale testing.

## Selected first Phase 7 runtime test

### Native ruler-and-clan succession guarded round trip

Use the existing protected campaign or a uniquely named guarded derivative. Select a living native ruler who is also clan leader and has:

- a surviving kingdom;
- a stable ruling clan;
- at least one clan-owned settlement;
- identifiable spouse/children/family links if present;
- at least one pre-event ClanAI structural record (kingdom continuity is sufficient);
- preferably one clan-bound holding-loss record and one hero-bound memory, if naturally present.

Do not alter age, health, death chance, family, ownership, elections, or succession decisions.

Run only within a predeclared bound until Bannerlord naturally records that ruler's death and applies both clan-leader and kingdom succession. If no qualifying event occurs, stop and record a bounded null. If it occurs:

1. pause after native post-state stabilizes;
2. capture pre-save identities and counts;
3. create one uniquely named guarded post-succession save;
4. exit without overwriting the source fixture;
5. reload that save once;
6. compare the same native and ClanAI state.

This is the highest-value first test because one natural event simultaneously checks native death/family continuity, clan leadership, settlement ownership, kingdom succession, structural ClanAI persistence, and non-inheritance of personal memory.

## Exact pass criteria

The test passes only if all are true:

1. The death/lifecycle event and successor selection are native; ClanAI performs no death, family, leader, ruler, settlement, membership, war, culture, or timer mutation.
2. The deceased hero remains dead after reload with the same stable hero identity.
3. Bannerlord's selected new clan leader is unchanged after reload.
4. Bannerlord's selected ruling clan/ruler for the surviving kingdom is unchanged after reload.
5. The kingdom keeps the same native kingdom ID and culture.
6. The clan's pre-event settlement ownership remains identical except for any separately logged native ownership event.
7. Native spouse/child/family relationships relevant to the transition remain identical after reload.
8. The kingdom continuity record restores with exactly one new succession increment for the observed native transition and no duplicate succession notice on reload reconciliation.
9. Clan-bound holding-loss pressure, if present before the event, remains attached to the same clan and continues its existing time decay.
10. Hero-bound companion/social/noble memory is not applied to the successor. Records for the deceased may remain historical/inert, but no successor lookup or score contribution may use the deceased hero's key.
11. WarState/WarScar records remain attached to their original structural IDs and preserve their normal decay/terminal rules.
12. The guarded save reloads successfully with no ClanAI persistence error and no development-tool runtime dependency.

## Exact fail criteria

The test fails if any of the following occurs:

- ClanAI causes or repairs the succession by mutating native lifecycle/political state;
- the new leader/ruler differs merely because of save/reload;
- a settlement, family link, kingdom ID, culture, membership, or war state changes without a native event;
- the continuity ledger increments twice or emits a duplicate load notice;
- a deceased hero's person-bound memory changes the successor's decision score;
- clan/world structural memory is lost solely because leadership changed;
- a persisted record resolves to the wrong hero, clan, settlement, faction, or kingdom after reload;
- the save cannot reload cleanly.

A run with no qualifying natural succession inside the declared bound is **not a failure**. It is a bounded null and does not justify forced death, aging changes, or retuning.

## Checkpoint

- Continuity audit: **COMPLETE**
- New gameplay implementation: **NOT STARTED**
- First runtime test: **IDENTIFIED**
- Concrete defect: **DOCUMENTED; NO FIX ATTEMPTED**
- Bannerlord launched: **NO**
- DLL deployed: **NO**
- Phase 6 rerun/retune: **NO**
