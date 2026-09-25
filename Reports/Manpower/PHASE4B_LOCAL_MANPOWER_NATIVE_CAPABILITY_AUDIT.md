# Phase 4B — Local Manpower native-capability and design audit

Date: 2026-09-25 UTC  
Base: `5b1ee40453c8d4f040fcf66ff100cc5596583901`

## Scope and result

**OFFLINE AUDIT PASSED — a clean native-compatible intervention seam exists. No gameplay change is implemented in this checkpoint.**

Phase 4A remains closed and unchanged. Its accepted evidence is the design input: native post-defeat recreation can start with regular troops; ordinary settlement recruitment contributes materially to recovery; garrison withdrawal can contribute; some outside-settlement growth remains unknown; and the one proven recreation roster is not a universal formula.

The smallest Phase 4B-v1 direction is to modify **daily refill probability for empty notable volunteer slots** through Bannerlord's selected `VolunteerModel`, while leaving occupied-slot upgrades, troop type, culture, recruitment legality, relation gates, costs, AI choice, player UI, garrison logic, and all actual roster/pool mutation to native code.

This is a local recovery-capacity signal, not a troop grant or faction manpower pool.

No source, DLL, volunteer pool, recruitment rate, troop tier, garrison, militia, prosperity, hearth, security, economy, spawning, AI recruitment behavior, or campaign state was changed. Bannerlord was not launched and no DLL was deployed.

## Native binaries relied upon

Installed supported-version binaries inspected offline:

- `TaleWorlds.CampaignSystem.dll`  
  SHA-256: `5B23C3E36D7A5D6D47C47EAB075E9E2B1CC8D1AA53C79E135FA2B5EF434EBC5F`
- `TaleWorlds.CampaignSystem.ViewModelCollection.dll`  
  SHA-256: `76279E43C1E27B86BD0E1AA35AA407345725C7B208AC170E0EE0174C1A7ADF2A`

The audit used reflection/decompilation only. The decompiled working tree is local audit material and is not committed.

## 1. Native recruitment pipeline on this version

### Volunteer production

`RecruitmentCampaignBehavior` listens to `DailyTickSettlementEvent` and calls its notable-volunteer update once per settlement per campaign day. It also seeds volunteer updates during new-game follow-up.

Volunteer production runs only for supported town/village settlement contexts; rebellious town state blocks the relevant update. Each living notable that can have recruits owns exactly six `VolunteerTypes` slots.

For each of the six slots, native code calls:

`Campaign.Current.Models.VolunteerModel.GetDailyVolunteerProductionProbability(hero, index, settlement)`

If the random production check succeeds:

- an **empty** slot is filled with `VolunteerModel.GetBasicVolunteer(notable)`;
- an **occupied** slot may instead upgrade through the troop's native `UpgradeTargets`, subject to notable power and `VolunteerModel.MaxVolunteerTier`;
- native code then reorders the slot array by troop level/mounted weight.

Thus one probability method feeds both numeric refill and occupied-slot quality progression.

### Current default probability

`DefaultVolunteerModel` starts from a native probability curve by slot index. Lower-index slots regenerate faster; higher-index slots regenerate more slowly.

The native result also includes:

- a kingdom-wide compensation based on the faction's effective fief/village count, with town prosperity bands contributing to that kingdom count;
- the Cantons policy;
- a cavalry-related perk when an already occupied slot contains a mounted recruit;
- the game's recruitment-rate option.

**Local town prosperity, village hearths, and local security do not directly scale an individual settlement's daily volunteer production probability.** Town prosperity participates only indirectly in the kingdom-wide fief-count compensation. Village hearth and local security are absent from the default production formula.

### Native troop type and quality

`DefaultVolunteerModel.GetBasicVolunteer` keeps culture authoritative:

- ordinary recruit source: the notable's culture `BasicTroop`;
- rural notable in a village bound to a castle: culture `EliteBasicTroop`.

Occupied volunteer slots upgrade only through native troop `UpgradeTargets`, with the native max volunteer tier of 4 and notable-power-based upgrade probability.

Phase 4B-v1 therefore does not need to choose a troop type or tier.

### Town / village / castle distinction

Direct notable volunteer production is a **town/village** system. Castle settlements themselves are not a normal notable volunteer-production source in the audited behavior.

Castles still matter natively because villages bound to a castle can produce the culture's elite-basic troop, and castle garrisons can auto-recruit from eligible notables in their normal bound villages.

### Player consumption

The native `RecruitmentVM` reads the same `Hero.VolunteerTypes` arrays shown to the player. On purchase it clears the exact selected notable slot and adds the native troop to the player's party.

The player's relation/slot-access restrictions are evaluated through the native volunteer model/helper; Phase 4B need not reproduce them.

### AI lord consumption

Ordinary AI lord recruitment in a settlement also reads the same notable `VolunteerTypes`. When an AI lord recruits a volunteer, native `RecruitmentCampaignBehavior` clears that exact notable slot and adds one troop.

AI settlement recruiting is already gated by native party condition, money, wage, capacity, settlement availability, raid/siege state, relation/index access and random demand. Entry into a settlement may run several recruitment passes for an ordinary AI party, so native consumption can be bursty.

### Garrison consumption

`GarrisonRecruitmentCampaignBehavior` has two distinct paths:

1. **auto recruitment** can consume the same notable volunteer slots from a fortification and its normal bound villages, clearing those slots;
2. a separate native **base garrison change** can directly add the fortification faction's basic troop without consuming visible volunteer slots.

The proposed Phase 4B volunteer ecology therefore affects the auto-recruitment path through shared supply, but does not pretend every native garrison increase comes from notable volunteers.

### Native/AI recovery paths that bypass visible notable pools

Known separate paths include:

- minor-faction AI lord `VolunteerFromMap` recruitment outside settlement: a native 5% hourly opportunity under specific distance, money and depletion conditions, adding the clan basic troop without a notable slot;
- native post-defeat/party creation initial troops proven in Phase 4A;
- base garrison growth described above;
- tavern mercenary stock, which is a separate town pool;
- prisoner recruitment and other distinct native recovery systems.

Phase 4B-v1 should not rewrite these paths merely to force all recovery through one manpower mechanism.

## 2. Native/world inputs available for local manpower

| Input | Native surface | Audit assessment |
| --- | --- | --- |
| Settlement type | `Settlement.IsTown`, `IsVillage`, fortification/town state | Reliable. Use only to select native local population signal and acute-state rules. |
| Town prosperity | `Town.Prosperity`, native `GetProsperityLevel()` | Reliable. Native levels: Low below 2000, Mid from 2000, High from 5000. |
| Village hearths | `Village.Hearth`, native `GetHearthLevel()` / prosperity level | Reliable population proxy. Native hearth levels: <200 low, 200-599 mid, >=600 high. |
| Security | `Town.Security` | Reliable 0-100 local-control signal. Native security model uses midpoint 50 and includes garrison, looted villages, siege, hideouts, prosperity, policies and patrol effects. For villages use the bound fortification's town security when available. |
| Loyalty | `Town.Loyalty` | Reliable, but **defer as direct v1 input**: loyalty already feeds native prosperity change and would add correlated political/culture effects. |
| Militia | `Settlement.Militia` / `Town.Militia` | Reliable read, but defer. It is military defense capacity, not a clean population/recruit-flow measure. |
| Garrison strength | `Town.GarrisonParty` roster / healthy strength / limits | Reliable read, but defer as direct input. Garrison already contributes to native security; adding it separately would double-count local control and could reward depletion. |
| Current raid/siege | `Settlement.IsUnderRaid`, `IsUnderSiege`, `IsRaided`, `Village.VillageState` | Reliable. Use only acute active raid/siege in v1; raid aftermath already damages hearth/security. |
| Recent raid history | BannerlordAI `WarStateTracker` privately records raid events in a 30-day window | Evidence exists but no narrow reusable local query is currently exposed. Defer instead of widening state during the first implementation. |
| Recent battles / local war pressure | Native town security reacts to certain nearby bandit/civilian battle outcomes | Partial proxy only. There is no general local lord-battle pressure value suitable for v1. Defer explicit battle history. |
| Kingdom war strain | `WarStateTracker.GetWarStrain(Kingdom)` | Reliable 0-1 BannerlordAI signal, but kingdom-wide rather than local. Existing AI-only recruitment throttle already consumes it; defer shared use in v1 to avoid double suppression. |
| War duration | Not exposed as a clean reusable local value in current BannerlordAI API | Defer. Current strain tracks active wars/besieged/scar load, not a direct stable duration input. |
| Recruitment pressure | Current notable slot occupancy is readable | Current depletion is observable but cause is ambiguous. Exact historical consumption is not parity-clean across native player and AI recruitment events. Defer tracked pressure. |
| Peace/recovery time | No dedicated local timer needed for v1 | Native hearth, prosperity and security already recover over time. Their recovery becomes the first-model recovery signal. |
| Culture | notable/settlement culture; native basic/elite troop and upgrade tree | Reliable, but remains **native authority**, not a scalar input. |
| Volunteer pool | each notable's six `VolunteerTypes` slots | Reliable shared supply surface. It is an output/state to replenish and consume natively, not something Phase 4B-v1 mutates directly. |

## 3. Existing BannerlordAI state and coupling

### War Strain recruitment throttle

The existing `WarStrainRecruitmentPatch` Harmony-patches native AI `GetRecruitVolunteerFromIndividual`. For AI lord parties it computes:

`rate = max(0.60, 1 - 0.40 * warStrain)`

and fractionally skips some AI volunteer recruitment attempts.

This is **AI-only acquisition throttling**, not local volunteer production. It does not directly change player pool refill.

Phase 4B-v1 must not quietly multiply a second shared war-strain factor on top of this existing AI-only throttle. The first local model therefore **excludes war strain**. A later bounded migration may retire the AI-only throttle and reintroduce a modest strain component at shared pool production if player/AI parity is desired.

### War Strain economics

`WarStrainEconomicPatches` separately raises native recruitment cost and military upkeep based on kingdom strain. Those are economic consequences and are not the Phase 4B local supply seam.

### WarState / WarScar

`WarStateTracker` records raids, losses, sieges/scars and kingdom strain. The raid history is currently private implementation state. v1 should not add new coupling just to access it when native hearth/security already encode local damage and recovery.

### Home Responsibility / territorial state

Home Responsibility is an AI decision/target layer. It supplies no cleaner manpower-capacity value than native local settlement state and should remain decoupled from v1.

### Political holding-loss memory

Clan loyalty/holding-loss memory is political memory, not a local population signal. It is explicitly excluded from local manpower.

## 4. Exact candidate intervention seam

**Preferred seam: a delegating `VolunteerModel` wrapper registered through `CampaignGameStarter`.**

The implementation shape should mirror the repository's existing selected-model delegation pattern:

1. obtain the currently selected `VolunteerModel` from `CampaignGameStarter`;
2. construct a `DelegatingVolunteerModel` around that exact model;
3. delegate every method/property unchanged except `GetDailyVolunteerProductionProbability`;
4. add/select the wrapper as `VolunteerModel`;
5. verify the selected model is the wrapper.

This is preferable to patching `RecruitmentCampaignBehavior` or directly editing `VolunteerTypes` because native code remains responsible for:

- daily update timing;
- random roll;
- whether a slot fills or upgrades;
- basic/elite troop selection;
- troop culture;
- upgrade targets and max tier;
- notable eligibility;
- player relation gates;
- AI recruitment decisions;
- garrison consumption;
- actual slot clearing and roster changes.

It also delegates to the already-selected model rather than assuming `DefaultVolunteerModel`, preserving compatibility with another native/modded volunteer model where practical.

## 5. Proposed Phase 4B-v1 policy

### Scope: empty slots only

Let:

`p_native = inner.GetDailyVolunteerProductionProbability(hero, index, settlement)`

If the slot is already occupied, return `p_native` **exactly**.

That keeps native volunteer quality/upgrade progression unchanged and confines Phase 4B-v1 to replacement **numbers**.

If the hero/index/settlement context is invalid or cannot be resolved safely, return the native result unchanged.

### Local population factor

Use native prosperity/hearth levels instead of inventing a new population scale:

| Native population level | Factor |
| --- | ---: |
| High | 1.00 |
| Mid | 0.95 |
| Low | 0.80 |

For towns, use native town prosperity level.  
For villages, use native village prosperity/hearth level.

Healthy/high-population territory therefore keeps vanilla production; the model never creates a global bonus.

### Local security factor

For a town, use that town's security.  
For a village, use its bound fortification's `Town.Security` where available.

Use native security midpoint 50 as full recovery:

`securityFactor = clamp(0.80 + 0.20 * clamp(security, 0, 50) / 50, 0.80, 1.00)`

Examples:

- security 0 -> 0.80;
- security 25 -> 0.90;
- security >=50 -> 1.00.

Missing security context -> 1.00 passthrough.

### Acute disruption factor

If the volunteer settlement is **actively under raid or siege**, use:

`acuteFactor = 0.50`

Otherwise:

`acuteFactor = 1.00`

Do not add a second persistent `IsRaided` penalty in v1: raid aftermath already lowers native hearth and affects bound-town security, so a second long-lived raid scalar would double-count damage.

### Final v1 result

`localMultiplier = clamp(populationFactor * securityFactor * acuteFactor, 0.35, 1.00)`

`p_final = clamp(p_native * localMultiplier, 0, 1)`

Properties of this shape:

- never produces more volunteers than the selected native model;
- never shuts recovery off completely;
- healthy secure territory returns to the exact native refill rate;
- damaged/insecure territory recovers more slowly;
- active attack causes a stronger temporary slowdown;
- recovery automatically improves as native hearth/prosperity/security recover;
- occupied-slot upgrade probability is untouched.

These numeric constants are **first-candidate safety bounds**, not balance claims. They require deterministic boundary tests and one bounded runtime characterization before tuning.

## 6. Explicit exclusions / deferred inputs

Phase 4B-v1 should **not** directly use:

- loyalty;
- militia count;
- garrison count/strength;
- recent-battle history;
- explicit recent-raid history beyond native current/damage state;
- war duration;
- kingdom war strain;
- recruitment-pressure history;
- explicit peace timer;
- clan social memory;
- Home Responsibility state;
- troop tier/type/culture modifiers.

Reasons:

- several are already reflected in prosperity/hearth/security;
- some would double-count correlated damage;
- some lack a clean player/AI-parity observation source;
- war strain is already consumed by an active AI-only recruitment throttle;
- troop quality belongs to Phase 4C.

## 7. Player / AI parity behavior

The new **local supply ecology** would be shared because the same six notable slots feed:

- player recruitment UI;
- ordinary AI lord settlement recruitment;
- garrison auto recruitment.

Native parties still have different legal access, relation, budget, wage and AI-demand rules. Phase 4B should not equalize those; it should make the **underlying local supply** common.

Known native bypasses (minor-faction map recruits, post-defeat initial troops, base garrison growth, mercenaries, prisoners) remain native and separate. They prevent claiming perfect all-source parity.

## 8. What remains natively authoritative

Phase 4B-v1 should leave Bannerlord authoritative over:

- settlement/notable update cadence;
- RNG;
- notable eligibility;
- volunteer slot count;
- recruitment relationship/index access;
- culture;
- base vs elite basic troop;
- upgrade targets and tier;
- costs and wages;
- AI decision to recruit;
- player decision to recruit;
- garrison auto-recruit decision/limits;
- all actual pool and roster mutations.

ClanAI supplies only a bounded multiplier to an empty-slot probability result.

## 9. Expected observable consequences

With the proposed v1:

- high-population secure towns/villages refill volunteer numbers at vanilla speed;
- low-hearth villages and low-prosperity towns refill more slowly;
- insecure territory refills more slowly;
- active raid/siege temporarily suppresses refill more strongly but never to zero;
- as native local conditions recover, refill automatically returns toward vanilla;
- player, AI lords and auto-recruiting garrisons compete over the same locally recovering notable slots;
- existing volunteers are not removed;
- occupied volunteer tiers do not progress more slowly because v1 passes occupied slots through;
- Phase 4A native recreation troops remain unchanged;
- unresolved outside-settlement growth remains unresolved;
- separate native recovery sources remain separate.

## 10. Deterministic test plan

Before runtime wiring is accepted, add pure policy tests covering at least:

1. occupied slot returns exact native probability;
2. invalid/missing context returns exact native probability;
3. high town + security >=50 + no disruption => multiplier 1.00;
4. mid/low town population bands;
5. high/mid/low village hearth bands;
6. security 0, 25, 50, 75/100 boundaries;
7. active raid factor;
8. active siege factor;
9. combined minimum clamp 0.35;
10. result never exceeds native probability;
11. result remains within [0,1];
12. town/village source selection;
13. missing village bound-security passthrough;
14. culture/troop type/tier absent from policy inputs.

Wrapper/invariant tests should prove:

- all non-production `VolunteerModel` methods delegate unchanged;
- inner production method is called exactly once;
- only empty-slot results are adjusted;
- no `VolunteerTypes[index] = ...` assignment exists in ClanAI Phase 4B code;
- no member-roster/garrison/militia/prosperity/hearth/security mutation exists;
- no external IO or absolute development path exists;
- selected model registration is explicit and reviewable.

Existing repository tests must remain green and Release must build with 0 errors; the inherited `System.ValueTuple` warning may remain.

## 11. Runtime proof plan

After a separate offline implementation checkpoint, use one bounded natural campaign run.

Required telemetry should record only natural model evaluations and native pool changes:

- settlement/notable/slot identity;
- empty vs occupied;
- settlement kind;
- native population level and raw prosperity/hearth for evidence;
- security;
- active raid/siege state;
- native probability;
- local multiplier;
- final probability;
- notable pool occupancy before/after the native daily settlement tick.

Runtime success should demonstrate:

1. wrapper selected and native inner model identified;
2. healthy secure empty slot has exact vanilla probability;
3. at least one naturally degraded locality produces `final < native`;
4. native daily processing still performs the actual fill when its own roll succeeds;
5. occupied-slot probability remains exact native value;
6. culture/troop types remain native;
7. ordinary AI recruitment still consumes visible notable slots;
8. no direct Phase 4B mutation event occurs.

Do **not** manufacture a raid, siege, depleted settlement or recruitment event. If no naturally degraded refill event occurs within the bound, record a bounded null rather than changing thresholds.

A later parity/migration experiment can address moving kingdom War Strain from the existing AI-only attempt throttle into shared production. That is not part of the first v1 proof.

## 12. Rollback and release safety

For future implementation/runtime validation:

- no deployment while Bannerlord runs;
- verified DLL rollback;
- protected fixture read-only;
- no save command unless a separately named fixture is explicitly authorized;
- `EXIT_NOSAVE`;
- verify fixture hash/timestamp;
- keep Strategic Commitment Observe and Visual War OFF unless another milestone explicitly changes them;
- no development-machine absolute path;
- no runtime dependency on ChatGPT, Codex, Desktop Commander, TestRunner, watchdogs or external IO;
- local manpower policy/wrapper must require no development infrastructure at runtime.

The final mod remains standalone, installable and offline.

## Checkpoint / next bounded step

The clean seam is selected, but **no Phase 4B gameplay code is added here**.

The smallest next milestone is an **offline-only implementation checkpoint**:

- pure local-manpower probability policy;
- delegating `VolunteerModel` wrapper;
- deterministic tests/invariants;
- no campaign launch or DLL deployment until that offline implementation passes.

Phase 4C is not started.
