# Phase 4C — Troop tiers matter native-capability/design audit

Date: 2026-09-25 UTC  
Base: `1c937ec6ec5c5caac9098a356fb0cafee926681a`

## Scope and result

**OFFLINE AUDIT PASSED — a clean shared native quality seam exists. No Phase 4C gameplay change is implemented in this checkpoint.**

Phase 4B remains unchanged. Its empty-slot Local Manpower policy is already implemented and runtime-proven for the minimum contract. This audit does not retune it and does not reopen Phase 4A.

The smallest Phase 4C-v1 intervention is to extend the already-selected `VolunteerModel` wrapper on **occupied, upgrade-eligible notable volunteer slots only**. The wrapper should modify only the first native daily production/upgrade gate. Bannerlord should continue to own the second notable-power upgrade roll, `UpgradeTargets`, troop culture/tree legality, slot mutation, RNG, player/AI access, and all post-recruitment troop upgrading.

Do not add a second nested `VolunteerModel` wrapper. Keep the existing selected-model registration and add a separate pure troop-quality policy branch inside the current wrapper.

No source, volunteer pool, troop tree, roster, garrison, militia, settlement state, probability factor, or campaign state was changed. Bannerlord was not launched and no DLL was deployed.

## Native binary evidence

Installed supported-version binary inspected offline:

- `TaleWorlds.CampaignSystem.dll`
- SHA-256: `5B23C3E36D7A5D6D47C47EAB075E9E2B1CC8D1AA53C79E135FA2B5EF434EBC5F`

Reflection/decompilation was limited to the relevant recruitment, volunteer, notable-power, training, upgrading, garrison, prisoner, and mercenary paths.

## 1. Native volunteer-quality pipeline

`DefaultVolunteerModel.GetBasicVolunteer` chooses the native starting troop:

- ordinary source: notable culture `BasicTroop`;
- rural notable in a village bound to a castle: notable culture `EliteBasicTroop`.

`DefaultVolunteerModel.MaxVolunteerTier` is **4**.

For every notable slot, native `RecruitmentCampaignBehavior.UpdateVolunteersOfNotablesInSettlement` first tests:

`RandomFloat < VolunteerModel.GetDailyVolunteerProductionProbability(notable, slotIndex, settlement)`

That one first gate controls both empty-slot refill and the opportunity for an occupied slot to attempt a quality upgrade.

If the slot is occupied, native quality only proceeds when the troop has at least one `UpgradeTarget` and current `Tier < MaxVolunteerTier`.

The second native quality gate is:

`log2(notable.Power / currentTroopTier) * 0.01`

A second native random roll must pass that value.

On success, Bannerlord selects one **direct** target from the current troop's native `UpgradeTargets` with native RNG.

A slot can traverse at most one direct `UpgradeTargets` edge during a single daily settlement update. Native code does not recursively re-run the newly assigned troop through the occupied upgrade block during that same slot iteration. The code does not guarantee that every direct target is exactly +1 tier; the hard guarantee is one direct upgrade edge.

After a volunteer change, native code reorders the six slots by troop level with a mounted offset. Higher-quality troops therefore tend toward later slot indices.

The default first-gate probability also falls sharply by slot index. In the ordinary base case the sequence is approximately:

- slot 0: 0.525;
- slot 1: 0.3675;
- slot 2: 0.25725;
- slot 3: 0.180075;
- slot 4: 0.1260525;
- slot 5: 0.08823675.

Native volunteer quality is therefore already slower than raw refill because higher-quality volunteers tend toward slower slots **and** occupied upgrades require the additional notable-power/current-tier roll.

Other native first-gate inputs remain intact, including fief-count compensation, Cantons, the mounted-volunteer Cavalry Tactics contribution, and the game recruitment-rate option.

Local security, local hearth and local prosperity are not direct volunteer-quality inputs in the native occupied upgrade block.

Notable power is already a native, dynamic quality signal. It changes through `NotablePowerModel`, occupation/property/issue effects, affiliation, siege aftermath and other campaign events. Native low-security logic can also change urban-notable power. Phase 4C should therefore not add another notable-power multiplier.

There is no separate generic town-versus-village occupied quality rate. Native differences instead come from starting troop source, culture tree, notable state and relevant perks.

## 2. Other major native quality-recovery paths

Volunteer-slot maturation is only one veteran-recovery path.

### Volunteer-source quality

Player recruitment, ordinary AI lord settlement recruitment and garrison auto recruitment can consume the same notable volunteer slots at whatever quality native slot progression has reached.

This is the clean shared Phase 4C seam.

### Party XP and post-recruitment upgrading

`PartyUpgraderCampaignBehavior` upgrades AI/non-main parties after map events and on daily party ticks.

Native requirements include XP, direct `UpgradeTargets`, legality, required items/perks, gold, wage constraints and party state.

The player upgrades troops through the native party screen using the same troop tree/cost/XP legality model.

Phase 4C-v1 should not replace or slow this separate party-XP upgrade pipeline.

### Training and battle XP

`MobilePartyTrainingBehavior` gives daily troop XP through the selected native `PartyTrainingModel`.

Battle/perk/recovery systems can also add troop XP.

Therefore veteran quality can continue to mature after recruitment independently of volunteer quality.

### Garrison quality

Garrisons can gain quality through:

- auto recruitment from notable slots at their current quality;
- base garrison growth adding the faction basic troop;
- daily garrison XP;
- normal native party upgrading;
- native troop transfer/withdrawal into field parties.

Phase 4C-v1 should not mutate garrison composition directly.

### Prisoners

Native prisoner recruitment can add an existing regular prisoner at its current troop type/tier after conformity and other native gates.

The audited AI path permits regular non-bandit prisoners at tier 2 or above when native conditions are satisfied.

This can bypass notable volunteer-quality maturation.

### Post-defeat recreation

Phase 4A proved a natural post-defeat native recreation can begin with a mixed-quality roster before settlement interaction.

The accepted Megenhelda recreation contained:

- T1: 6;
- T2: 4;
- T3: 6;
- T4: 1;
- T5: 4.

This is a material quality bypass around volunteer progression and remains outside Phase 4C-v1.

### Mercenaries

Town mercenary supply is separate from notable volunteers.

Native mercenary selection can recursively choose along mercenary `UpgradeTargets`, with deeper-tree choices receiving lower weight, and then provides a tier-dependent quantity.

Phase 4C-v1 should not rewrite mercenary quality.

### Minor-faction map recruitment

The separate minor-faction map-recruit path adds the clan basic troop and is mainly a headcount bypass rather than a veteran-quality source.

## 3. Interaction with Phase 4B

Phase 4B intentionally returns the native probability unchanged for occupied slots.

Phase 4C should preserve Phase 4B's empty-slot branch exactly.

Recommended future branch structure:

- **empty slot** -> current Phase 4B behavior unchanged;
- **occupied but not native-upgrade-eligible** -> exact native passthrough;
- **occupied and native-upgrade-eligible** -> a new pure Phase 4C quality policy may modify only the first native gate.

Do not add a second selected `VolunteerModel` wrapper. Extending the existing `LocalManpowerVolunteerModel` avoids nested-wrapper ordering ambiguity and preserves one call to the wrapped inner production method.

## 4. Candidate Phase 4C-v1 policy

Native quality scarcity should remain primarily controlled by:

- slot index;
- notable power;
- current troop tier;
- native `MaxVolunteerTier`;
- native `UpgradeTargets`;
- native second RNG roll.

Phase 4C should add only a bounded local quality-context multiplier to the **first** occupied-slot gate.

### Included inputs

Use only:

1. occupied and native-upgrade-eligible slot;
2. native population band already audited for Phase 4B;
3. local/bound security already audited for Phase 4B;
4. active raid/siege acute state.

Current tier and notable power should be observable for diagnostics, but not new multiplier inputs.

Settlement type should only resolve context, not receive its own scalar.

Culture and castle-bound elite source remain native.

### Local components

The audit supports reusing the already-audited **local condition components**, but not blindly reusing Phase 4B's complete result.

Candidate components:

- High population: 1.00;
- Mid: 0.95;
- Low: 0.80;
- security factor: the existing 0.80..1.00 curve reaching 1.00 at security 50;
- active raid/siege: 0.50;
- otherwise acute factor: 1.00.

Candidate quality multiplier:

`qualityMultiplier = clamp(populationFactor * securityFactor * acuteFactor, 0.50, 1.00)`

For an occupied upgrade-eligible slot:

`p_quality = clamp(p_native * qualityMultiplier, 0, 1)`

### Why the quality floor is 0.50

The 0.50 floor is a **safety bound, not a balance claim**.

Volunteer quality already has later-slot probability decay, a separate notable-power/current-tier roll, native max tier and one-direct-edge-per-day behavior.

Using Phase 4B's 0.35 floor would compound those existing quality gates without evidence and creates a larger risk of excessive quality stall.

A 0.50 floor ensures Phase 4C-v1 cannot reduce the selected native first quality gate below half its native value.

Healthy High-population, security >=50, non-disrupted territory remains exact vanilla quality progression.

The native second gate still applies afterward, so Phase 4C never directly upgrades a slot.

## 5. Explicit v1 exclusions

Defer:

- explicit current-tier multiplier;
- explicit notable-power multiplier;
- direct town/castle/village quality bonus;
- loyalty;
- militia strength;
- garrison strength;
- war duration;
- recruitment-pressure history;
- peace timer;
- War Strain;
- social/political memory;
- Home Responsibility;
- direct troop downgrades;
- deleting high-tier volunteers;
- faction-wide tier caps;
- troop-tree rewriting;
- direct party composition enforcement;
- AI-only quality penalties;
- player-only quality penalties;
- direct garrison-quality mutation;
- post-defeat recreation rewriting;
- prisoner-quality rewriting;
- mercenary-quality rewriting.

Tier and notable power are deliberately excluded as new factors because native code already uses them directly.

## 6. Expected observable consequences

If later implemented as designed:

- Phase 4B still governs empty-slot headcount refill;
- low-tier/native starting volunteers return before repeated native quality gates can mature them;
- degraded localities have fewer first-gate opportunities for occupied volunteer upgrades;
- healthy secure territory returns to vanilla quality progression;
- active raid/siege can slow volunteer maturation but cannot create permanent collapse under the 0.50 floor;
- castle-linked villages retain their native elite-basic role;
- existing high-tier volunteers are never downgraded or deleted;
- troop culture and trees remain native;
- player, AI lords and auto-recruiting garrisons share the resulting volunteer quality pool where they share notable slots.

This is a quality-rate ecology, not a scripted composition system.

## 7. Player/AI parity

The quality seam is shared because the volunteer slot matures before a consumer recruits it.

Player, AI lord and garrison access can still differ through native relation limits, budgets, demand and garrison rules.

Phase 4C should not equalize those rules.

## 8. Native authority retained

Bannerlord remains authoritative over:

- culture;
- `BasicTroop` / `EliteBasicTroop`;
- troop trees;
- `UpgradeTargets`;
- `MaxVolunteerTier`;
- notable power;
- current tier;
- native second quality probability;
- RNG;
- slot ordering;
- actual slot mutation;
- relation/access rules;
- player recruitment;
- AI recruitment;
- garrison recruitment;
- party XP/upgrades;
- upgrade gold/items/perks;
- garrison XP/upgrades;
- prisoner recruitment;
- mercenaries;
- post-defeat recreation.

ClanAI would only bound the first occupied-slot probability result.

## 9. Deterministic test plan for a later implementation

At minimum:

1. empty slot returns the exact Phase 4B result;
2. occupied terminal/max-tier slot returns exact native probability;
3. occupied troop with no `UpgradeTargets` returns exact native probability;
4. occupied upgrade-eligible healthy context returns exact native probability;
5. Mid population component 0.95;
6. Low population component 0.80;
7. security 0 / 25 / 50 / >50;
8. active raid/siege component 0.50;
9. combined quality multiplier never below 0.50;
10. final first-gate quality probability never exceeds native;
11. positive finite native probability remains positive;
12. invalid/unsupported context passes through;
13. non-finite input cannot emit NaN/Infinity;
14. current tier is not a pure-policy multiplier;
15. notable power is not a pure-policy multiplier;
16. culture/troop tree/`UpgradeTargets` are not modified;
17. wrapped inner production method is called exactly once;
18. every other `VolunteerModel` member still delegates unchanged;
19. no volunteer-slot assignment exists in Phase 4C code;
20. all Phase 4B tests/invariants remain green.

Deterministic tests are not balance proof.

## 10. Runtime proof plan

After a separately authorized offline implementation:

### Model continuity
Prove the same selected wrapper and exact inner model.

### Phase 4B non-regression
Capture at least one empty-slot evaluation that matches the accepted Phase 4B contract.

### Healthy occupied quality passthrough
Observe a natural occupied upgrade-eligible slot in healthy context with quality multiplier 1 and final first-gate probability equal to native.

### Degraded occupied quality slowdown
Observe a natural occupied upgrade-eligible slot with quality multiplier <1 and final first-gate probability < native, without manufacturing degradation.

### Native quality mutation
Preferred strongest proof:

- record exact occupied troop before native daily update;
- record current tier, notable power and direct `UpgradeTargets`;
- record wrapper first-gate result;
- later observe Bannerlord change that exact slot to one of the previous troop's direct native `UpgradeTargets`;
- no ClanAI slot mutation.

Do not force upgrades, raise notable power, change settlement conditions or extend indefinitely if a qualifying natural case does not occur.

A bounded null remains valid evidence.

## 11. Limitation of volunteer-only Phase 4C-v1

Even a successful volunteer-quality policy will not make all veteran recovery obey one scarcity system.

Separate native quality paths remain through party XP/upgrades, garrison training/transfers, prisoners, mercenaries and post-defeat recreation.

Any later balance conclusion must account for those bypasses.

## 12. Release safety

Future implementation/runtime validation must preserve:

- no live DLL replacement while Bannerlord runs;
- verified rollback;
- protected fixture read-only;
- no-save exit;
- Phase 4B empty-slot behavior unchanged;
- no direct volunteer/troop mutation;
- no absolute development-machine runtime path;
- no external IO;
- no runtime dependency on ChatGPT, Codex, Desktop Commander, TestRunner, watchdogs or development harnesses.

## Checkpoint / next step

A clean seam exists.

**No Phase 4C source or tests are added in this audit checkpoint.**

The smallest next milestone is a separately bounded **offline-only Phase 4C-v1 implementation**:

- pure troop-quality probability policy;
- occupied upgrade-eligible branch in the existing selected `LocalManpowerVolunteerModel`;
- Phase 4B empty-slot behavior locked unchanged;
- deterministic/delegation/no-mutation/standalone tests;
- Release build;
- no runtime launch or deployment until that offline implementation passes.

Do not begin Phase 5.
