# Phase 5 — Security, Patrols, Banditry, and Local Control native-capability/design audit

Date: 2026-09-25 UTC  
Base: `e802e8d893c6bb310d165e13e23a3a162529eac0`

## Scope and result

**OFFLINE AUDIT PASSED — a clean, bounded native local-control seam exists. No Phase 5 gameplay behavior is implemented in this checkpoint.**

The supported Bannerlord version does **not** have one universal bandit-spawn loop. Ambient looters, culture-bandit parties tied to hideouts, hideout infestation/replacement, quest/incident bandits, and native settlement patrols are separate systems.

The smallest Phase 5-v1 recommendation is therefore deliberately narrow:

> **Modify only the native town/village spawn-site weight used for ambient looter placement, using existing settlement Security as the sole local-control input.**

Do **not** change global looter quantity, culture-bandit/hideout spawning, hideout counts, native patrol generation, party templates, bandit movement, combat removal, or lord target scoring in v1.

The candidate seam is an observation/reviewable postfix/result modifier on native:

`BanditSpawnCampaignBehavior.GetSpawnChanceInSettlement(Settlement)`

applied only when the candidate settlement is a **town or village**. Hideout calls remain exact native passthrough.

No source, DLL, campaign, settlement state, patrol party, bandit party, Phase 4 policy, Home Responsibility factor, or Visual War factor was changed. Bannerlord was not launched and no DLL was deployed.

## Native binaries

Primary campaign binary:

- `TaleWorlds.CampaignSystem.dll`
- SHA-256 `5B23C3E36D7A5D6D47C47EAB075E9E2B1CC8D1AA53C79E135FA2B5EF434EBC5F`

Supporting SandBox binary inspected to distinguish campaign-system ownership from mission/UI surfaces:

- `SandBox.dll`
- SHA-256 `16AF436C569675EB30E22514BB755E6FB3612AFCE38F6CC55D1748D079C19C1A`

The durable result below is distilled from targeted reflection/decompilation. Broad decompiled working trees remain local audit material and are not committed.

# 1. Native bandit spawning pipeline

## 1.1 Ambient spawn owner

Ambient land-bandit population is primarily owned by:

`TaleWorlds.CampaignSystem.CampaignBehaviors.BanditSpawnCampaignBehavior`

It registers:

- `MobilePartyCreated`;
- `MobilePartyDestroyed`;
- `SettlementEntered`;
- `DailyTickEvent`;
- `HourlyTickClanEvent`;
- `OnGameLoadedEvent`;
- `OnHomeHideoutChangedEvent`;
- new-game partial follow-up events.

It caches current bandit counts keyed by each native bandit party's `HomeSettlement` and adjusts those counts when parties are created, destroyed, or change home hideout.

## 1.2 Native density/cap model

`DefaultBanditDensityModel` supplies native population bounds:

- minimum parties inside a hideout to count as infested: **2**;
- maximum parties inside each hideout: **3**;
- maximum parties roaming around each hideout: **3**;
- maximum infested hideouts per normal bandit faction: **9**;
- initial infested hideouts per normal bandit faction: **7**;
- minimum bandit troops for a hideout mission: **10**.

Therefore the culture-bandit deficit target used by ambient spawning is based on up to:

`3 inside + 3 around = 6 parties per infested hideout`

before subtracting current clan war-party count.

For looters, the normal native support limit is:

`min(totalInfestedHideouts * 7, GetMaxSupportedNumberOfLootersForClan)`

The default maximum support is **270** looter parties, with native deserter interaction able to reduce that limit; deserter-specific support is **50**.

These are global/faction-level population constraints, not local-security constraints.

## 1.3 Hourly ambient spawning

Native hourly spawning runs only at night for bandit clans.

Looters:

`SpawnLooters(clan, 0.07f, false)`

The number to create is randomized from approximately:

`(nativeLooterLimit - currentLooterParties) * 0.07`

Culture bandit factions:

`SpawnBanditsAroundHideout(clan, 0.1f)`

The number to create is randomized from approximately:

`(infestedHideouts * 6 - currentClanBanditParties) * 0.10`

Phase 5-v1 should **not** change either 0.07/0.10 refill ratio or those native population limits.

## 1.4 Looter spawn path

For each ambient looter party:

1. native selects a town/village with `SelectARandomSettlementForLooterParty`;
2. every town/village candidate receives `GetSpawnChanceInSettlement(settlement)`;
3. native performs weighted selection;
4. native finds a reachable point around the selected settlement;
5. native creates the party through `BanditPartyComponent.CreateLooterParty`;
6. native uses the looter faction's `DefaultPartyTemplate`;
7. native initializes aggressiveness, trade gold and food;
8. native gives the party its normal patrol movement.

The spawn radius is based on roughly half a bandit travel-day; looters receive a **1.5x** radius multiplier.

Native spawn-position logic also tries to avoid spawning directly inside the main player's current sight/nearby radius.

## 1.5 Culture-bandit / hideout spawn path

Culture-bandit parties use a separate path:

1. select an **infested same-culture hideout**;
2. candidate hideouts are weighted with the same `GetSpawnChanceInSettlement` function;
3. spawn around the hideout;
4. create through `BanditPartyComponent.CreateBanditParty`;
5. use that bandit clan's native `DefaultPartyTemplate`;
6. preserve native bandit culture;
7. initialize native party state and patrol movement.

This path is deliberately **not** modified by the proposed Phase 5-v1 because a hideout does not expose the same direct settlement-security context as a town/village. Inventing a "nearest controller" mapping in the first implementation would be less native and less reviewable.

## 1.6 Native local spawn-site weighting

The key native function is:

`BanditSpawnCampaignBehavior.GetSpawnChanceInSettlement(Settlement)`

Native behavior:

- if the settlement/home already has bandit parties in the behavior's cache, return approximately `1 / count²`;
- otherwise return `1`.

Thus native already has a strong **anti-clumping** location weight.

That score is used for:

- looter town/village spawn-site selection;
- culture-bandit infested-hideout spawn-site selection.

The proposed v1 should multiply this native result rather than replace it.

## 1.7 No current local-control input in ambient spawn placement

The audited ambient spawn path does **not** directly use:

- town Security;
- prosperity/hearth;
- looted/raided state;
- active war;
- local garrison;
- native patrol presence;
- nearby lord presence;
- recent raid history.

Native control state therefore currently affects bandit pressure mainly through **combat/suppression and Security consequences**, not ambient looter spawn-site weighting.

## 1.8 Other non-ambient bandit creation paths

There is no universal bandit spawn path.

Other native code can create bandit/deserter parties for:

- deserter behavior;
- campaign incidents;
- caravan ambush quests;
- escort-caravan quests;
- extortion/deserter issues;
- other quest-specific encounters.

Phase 5-v1 must leave these special-purpose paths untouched.

# 2. Hideout lifecycle

## 2.1 Infestation is derived from party count

`Hideout.IsInfested` is not a permanent Boolean state.

A hideout is considered infested when its settlement contains at least the native minimum number of bandit parties: **2**.

This makes hideout pressure directly dependent on resident native bandit parties.

## 2.2 Initial and replacement hideouts

Normal bandit factions begin with a native target of **7** infested hideouts.

Daily `BanditSpawnCampaignBehavior.AddNewHideouts` can establish replacement/new infestation while the faction is below the native maximum of **9**.

The replacement candidate is a non-infested same-culture hideout site. Native weighting favors suitable spacing from existing infestations and respects recent `LastThreatTime` cooldown behavior.

When selected, native fills the hideout with the minimum **2** bandit parties, making it infested.

Security does not currently determine this hideout-site replacement weighting.

## 2.3 Hideouts retain bandit populations

`AiLandBanditPatrollingBehavior` prevents ordinary bandit parties from all leaving a hideout when the resident count is near the native infestation minimum.

When population is above that protected level, bandit parties can patrol around their `HomeSettlement`.

Thus hideouts are not only spawn markers; they retain enough parties to sustain infestation while releasing excess local pressure.

## 2.4 Roaming bandits return to hideouts

`AiVisitSettlementBehavior` gives bandit parties native hideout-return scores.

Those scores consider:

- same-culture hideouts;
- navigation distance;
- loot/value carried;
- prisoner load;
- number of infested same-culture hideouts;
- resident bandit count relative to native minimum/maximum.

Hideouts therefore function as native home/rest/deposit hubs.

## 2.5 Boss parties

A visible infested hideout containing bandits can receive a native boss party if one is absent, using the culture's native `BanditBossPartyTemplate`.

Phase 5-v1 should not alter boss behavior.

## 2.6 Clearing a hideout

The inspected player hideout-clear path:

- resolves the native hideout battle;
- tells surviving parties in the site to leave;
- temporarily prevents those survivors from attacking the main party;
- hides the site again.

Battle resolution supplies the actual casualties/removal. The clear handler itself does not mass-delete every bandit party.

The dormant hideout site remains part of the world and can later be infested again through native hideout replacement logic.

Failed/aborted hideout encounters can apply native attack cooldown via the `HideoutModel` hidden-duration setting (default **10 days**).

No normal AI-lord behavior was identified that replaces the player hideout-assault mission path.

# 3. Native suppression and removal

## 3.1 Actual party removal

The principal ambient removal mechanism is normal combat/map-event destruction.

When a bandit party is destroyed, `BanditSpawnCampaignBehavior` decrements its cached home-settlement count.

This affects later native spawn deficits and anti-clumping weights.

## 3.2 Native patrol parties

Bannerlord already has native settlement patrol parties.

`DefaultSettlementPatrolModel` allows land patrols for owned, non-rebel towns that have a `SettlementGuardHouse`.

Native patrol regeneration delay is approximately:

- Guard House level 1: **10 days**;
- level 2: **8 days**;
- level 3: **6 days**.

Native patrol strength templates are culture-specific weak/moderate/strong templates driven by the building effect.

`PatrolPartiesCampaignBehavior` sends the patrol around its home town and bound villages and can replenish it at home.

No custom Phase 5 patrol party type is needed.

## 3.3 Patrols are naturally anti-bandit

The general native initiative model gives patrol attackers a relative advantage when evaluating bandit targets.

When an enemy is a bandit and the attacker is already on `PatrolAroundPoint`, native initiative attack distance is expanded substantially (factor **3.5** in the inspected path).

Patrol parties also receive an approximately **1.2** relative-strength/initiative factor against bandit targets.

Native patrol conversation/status logic counts nearby bandit parties and already distinguishes:

- no significant reports;
- several nearby brigands;
- roughly five or more as "thick with bandits".

This is direct evidence that native patrols already model local criminal pressure through actual nearby parties.

## 3.4 Normal lord parties

Independent normal lord parties can attack bandits through the generic native initiative system.

There is no simple hard "maximum bandit party size" constant in the inspected decision surface. Attack feasibility depends on:

- relative estimated strength;
- aggressiveness;
- distance;
- speed/catchability;
- current behavior;
- army state;
- map-event state;
- navigation constraints.

Parties already patrolling are willing to pursue bandit/lord enemies over a larger initiative distance.

Army behavior is different: independent army leaders/members in the inspected initiative path explicitly skip opportunistic bandit targets in relevant army conditions. Therefore suppression is strongest from local independent parties/patrols rather than armies marching strategic missions.

## 3.5 Security changes from bandit outcomes

Native Security is not merely a static input.

`TownSecurityCampaignBehavior` updates nearby town Security after field battles:

- bandits defeating civilian/villager/caravan parties reduce nearby Security according to defeated strength;
- defeating bandit parties increases nearby Security according to defeated bandit strength.

The default magnitude is strength times approximately **0.005**.

Clearing a hideout gives nearby towns a native **+6 Security** effect inside the native clear radius.

This creates an existing world feedback between actual bandit success/suppression and local control.

## 3.6 No ambient mass-cleanup loop found

The audited ambient spawn behavior does not periodically destroy "excess" bandits.

Native global/faction caps prevent unlimited replenishment, but existing parties are primarily reduced through:

- combat;
- hideout battles;
- specific interaction/quest logic.

BanditSpawn behavior also periodically maintains bandit trade gold/food, so starvation should not be treated as the intended main suppression mechanism.

# 4. Native lord/patrol response

## Defensive patrolling

`DefaultTargetScoreCalculatingModel.CalculateDefensivePatrollingScoreForSettlement` uses native local threat and ally intensities.

For friendly settlements it considers approximately:

- `NearbyLandThreatIntensity`;
- `NearbyLandAllyIntensity`;
- number of other war parties already patrolling the same settlement;
- whether the actor's clan owns the settlement;
- party/army state;
- faction type;
- native distance/availability rules.

The score reduces duplicate patrolling when other friendly parties are already covering the target.

Thus Bannerlord already contains a real local-control response loop: threat can attract patrol behavior, and existing coverage lowers redundant response.

## No Phase 5 order forcing needed

Because patrol/lord engagement is already legal and native, Phase 5-v1 should not issue orders or force targets.

The first world-ecology seam should work *upstream* by changing where ordinary looter pressure tends to appear, then let native patrol/lord AI suppress it through existing combat.

# 5. Existing BannerlordAI interactions

## Home Responsibility

Home Responsibility already biases existing native `GoToSettlement`, `DefendSettlement`, and `PatrolAroundPoint` candidates for an eligible AI lord's own clan holdings during war.

It reacts to native siege/raid state and weak-party recovery state.

It does **not** directly target bandits or create patrol parties.

Phase 5 should not reuse its factors as crime-control multipliers. Its contribution is indirect: it can increase sustained noble presence near clan territory, which then participates in native suppression.

## Visual War rear security

Visual War already has an optional bandit-response modifier for native `EngageParty` candidates:

- bandit target required;
- weak-for-recovery parties skip the bonus;
- parties up to **90 men** receive a **1.25** factor;
- parties up to **160 men** receive a **1.15** factor.

That is an AI decision-score layer, not a world spawn ecology.

Visual War remains an independently proven Phase 3 system and should not be retuned or made a required Phase 5 dependency.

## WarState / WarScar

WarState records raids, losses, sieges and sustained-raiding scars.

Those histories are valuable strategic state, but direct Phase 5 reuse would double-count damage already represented natively through:

- looted villages;
- siege state;
- Security;
- local threat.

WarState local raid memory is therefore deferred from Phase 5-v1.

## Phase 4 Security reads

Phase 4 already established safe access to:

- town `Security`;
- village bound-fortification `Town.Security`.

That exact read-only context can be reused without coupling Phase 5 to Phase 4 policy constants.

# 6. Why Security should be the only v1 local-control input

Native `DefaultSettlementSecurityModel.CalculateSecurityChange` already incorporates:

- nearby infested hideout penalty;
- looted bound villages;
- active siege penalty;
- prosperity/corruption effect;
- garrison strength;
- policies;
- governor/perks/issues/projects;
- drift toward native midpoint 50;
- native settlement patrol-party bonus.

Examples from the supported version:

- nearby infested hideout: **-2** daily Security contribution;
- at least one looted bound village: **-2**;
- under siege: **-3**;
- native patrol-party presence adds a Guard House-level-dependent positive contribution;
- garrison strength contributes positively;
- Security naturally drifts toward **50**.

Therefore independently multiplying spawn weight by garrison, raid state, hideouts, patrol presence and Security would count much of the same condition multiple times.

Security is the smallest evidence-backed aggregate control signal.

### Deliberately deferred direct inputs

Phase 5-v1 should not separately use:

- prosperity/hearth;
- active raid/siege;
- recent raid history;
- hideout count;
- nearby bandit count;
- garrison strength;
- militia;
- patrol presence;
- lord count/strength;
- Home Responsibility state;
- Visual War state;
- War Strain/WarScar.

Specific reasons:

- many already feed Security;
- current bandit/home count already feeds native `1/count²` anti-clumping;
- hideout count already drives global looter support and culture-bandit population;
- nearby friendly parties already suppress through real combat;
- direct lord/patrol presence is highly volatile and could cause oscillatory spawn weighting.

# 7. Exact candidate Phase 5-v1 intervention seam

## Selected seam

**Harmony postfix/result modifier on:**

`BanditSpawnCampaignBehavior.GetSpawnChanceInSettlement(Settlement)`

### Scope

Apply only when:

- candidate is a normal `Town` or `Village`;
- safe Security context can be resolved.

For hideout settlements:

- return native result exactly.

This isolates v1 to **ambient looter spawn-site selection**.

### Why this seam is preferred

It preserves native:

- nighttime hourly cadence;
- global looter population limit;
- 0.07 looter deficit refill rate;
- hideout count/caps;
- culture-bandit spawning;
- culture/templates;
- party size/template generation;
- spawn radius;
- player-visibility avoidance;
- actual `CreateLooterParty`;
- movement AI;
- suppression/removal;
- native `1/count²` anti-clumping.

Phase 5 only changes the **relative local weight** among native town/village candidates.

This is materially smaller and safer than direct party creation/destruction or altering global density caps.

# 8. Candidate local-control policy

## Security source

Town:

`settlement.Town.Security`

Village:

`settlement.Village.Bound?.Town?.Security`

If no safe Security context exists:

- exact native weight passthrough.

## Candidate v1 multiplier

First candidate:

`controlMultiplier = clamp(1.25 - 0.005 * clamp(Security, 0, 100), 0.75, 1.25)`

Examples:

- Security 0 -> **1.25**;
- Security 25 -> **1.125**;
- Security 50 -> **1.00**;
- Security 75 -> **0.875**;
- Security 100 -> **0.75**.

Then:

`finalSpawnWeight = nativeSpawnWeight * controlMultiplier`

for supported town/village candidates only.

### Safety properties

This is a **relative weighted-selection score**, not a spawn probability and not a spawn-count multiplier.

The candidate:

- never makes a supported positive native spawn-site weight zero;
- never raises local weight above 125% of native;
- never lowers local weight below 75% of native;
- leaves native global looter count/cap unchanged;
- leaves native anti-clumping unchanged inside the base score;
- naturally returns to neutral at Security 50;
- automatically changes as native Security changes.

The 0.75–1.25 range is a first-candidate safety bound, **not a balance claim**.

# 9. Patrol/control evidence without new patrol units

Phase 5-v1 does not need artificial patrols.

Existing native control already supplies:

- Guard House patrol parties;
- lord defensive patrolling;
- owner-clan patrol preference;
- local threat/ally intensity;
- anti-duplication of multiple patrol assignments;
- general party-vs-bandit initiative;
- actual combat destruction.

Home Responsibility can independently increase native clan-home patrol/defend presence during war.

Visual War can independently boost selected bandit `EngageParty` candidates when enabled.

For v1, those systems should affect outcomes **through real native presence/combat and Security**, not by adding another direct spawn factor.

# 10. Player / AI / independent parity

The proposed spawn-site factor reads world settlement Security, not actor identity.

Therefore the same rule can apply to:

- player-owned towns/villages;
- AI-owned towns/villages;
- independent/rebel-owned supported settlements where native Security exists.

There is no hidden AI-only penalty.

For an unsupported village with no bound-town Security context, v1 passes through native weight.

# 11. What remains Bannerlord-authoritative

Bannerlord should continue to own:

- whether the looter clan is below its global support cap;
- how many looters attempt to spawn;
- nighttime cadence;
- random weighted choice;
- native anti-clumping base score;
- party culture/faction;
- party template;
- roster composition;
- spawn point legality;
- player-visibility avoidance;
- party creation;
- aggressiveness;
- food/trade state;
- movement AI;
- lord/patrol target legality;
- attack feasibility;
- combat;
- party destruction;
- hideout infestation/replacement;
- hideout clearing;
- Security mutation itself.

ClanAI would supply only a bounded multiplier to an existing local native selection score.

# 12. Expected observable consequences

If later implemented as designed:

- total ambient looter population still follows native global limits;
- weak-control settlements are relatively more likely looter spawn anchors;
- high-Security settlements remain possible spawn anchors, but less likely;
- native anti-clumping still discourages repeatedly selecting the same already-loaded location;
- successful bandit activity can reduce Security and thereby modestly increase future local looter weight;
- defeating bandits/clearing hideouts/patrol control can raise Security and modestly lower future local looter weight;
- security recovery automatically returns weight toward or below neutral;
- culture-bandit/hideout pressure remains native and separate in v1.

Thus v1 makes **ordinary looter distribution** reflect local control without creating a new total-bandit economy.

# 13. Explicit v1 exclusions

Do not add in Phase 5-v1:

- direct `CreateParty` or `DestroyParty`;
- direct hideout creation/removal;
- culture-bandit spawn changes;
- hideout-site weighting changes;
- global looter cap changes;
- native 0.07 / 0.10 refill-rate changes;
- custom patrol parties;
- scripted police forces;
- bandit teleportation;
- mass bandit deletion;
- faction-wide crime quotas;
- direct lord target/order forcing;
- Home Responsibility changes;
- Visual War changes;
- WarState/WarScar coupling;
- manpower/troop-quality retuning;
- prosperity/hearth/security mutation;
- arbitrary global crime slider;
- external/provider reasoning.

# 14. Deterministic test plan for a later implementation

A separate offline implementation checkpoint should prove:

1. unsupported/null context -> exact native weight;
2. hideout candidate -> exact native weight;
3. town Security 0 -> factor 1.25;
4. Security 25 -> 1.125;
5. Security 50 -> 1.00;
6. Security 75 -> 0.875;
7. Security 100 -> 0.75;
8. Security below/above native bounds clamps safely;
9. village uses bound-town Security;
10. missing village bound-town Security -> passthrough;
11. positive native weight remains positive;
12. final weight remains within native * [0.75, 1.25];
13. Security 50 preserves native bits where practical;
14. non-finite context cannot create NaN/Infinity;
15. pure policy has no faction/player/AI input;
16. pure policy has no garrison/hideout/patrol/lord/raid-history input;
17. patch targets the exact native `GetSpawnChanceInSettlement(Settlement)` method;
18. postfix changes only `__result`;
19. no `CreateParty` / `DestroyParty` / movement/order API exists in Phase 5 source;
20. Phase 3 and Phase 4 source/tests remain unchanged/green.

Deterministic tests are not balance proof.

# 15. Runtime proof plan

After a separately authorized offline implementation passes, use one bounded natural runtime characterization.

Minimum proof:

### A. Native target/seam
- patch resolves exact `BanditSpawnCampaignBehavior.GetSpawnChanceInSettlement`;
- no direct party mutation.

### B. Neutral control
Observe a town/village near Security 50:
- multiplier approximately 1;
- final weight approximately native weight.

### C. Secure control
Observe a naturally high-Security town/village:
- multiplier <1;
- final spawn-site weight < native.

### D. Weak control
Observe a naturally low-Security town/village:
- multiplier >1;
- final spawn-site weight > native.

Do not manufacture low Security.

### E. Native looter spawn authority
Preferred strongest proof:
- native selector evaluates candidate weights;
- one native looter creation later records its native related/home settlement;
- no ClanAI party creation occurs.

### F. Hideout passthrough
If naturally sampled:
- hideout candidate keeps exact native score.

Do not extend indefinitely only to exercise an optional branch.

If no naturally weak Security case appears within the bound, record a bounded null rather than changing thresholds or settlement state.

# 16. Rollback / release safety

Future Phase 5 implementation/runtime validation must preserve:

- Bannerlord closed before DLL replacement;
- verified rollback;
- protected fixture read-only;
- `EXIT_NOSAVE`;
- Phase 3 and Phase 4 behavior unchanged;
- no direct party spawn/destruction;
- no external IO;
- no absolute development-machine runtime path;
- no runtime dependency on ChatGPT, Codex, Desktop Commander, TestRunner, watchdogs, or other development harnesses.

The final mod remains standalone, installable, and offline.

## Checkpoint / next bounded step

A clean Phase 5-v1 seam exists.

**No Phase 5 gameplay source or tests are added in this audit checkpoint.**

The smallest next milestone is a separate **offline-only implementation** of:

- a pure Security-to-looter-spawn-weight policy;
- a narrow postfix/result modifier on native `GetSpawnChanceInSettlement`;
- town/village-only application;
- hideout passthrough;
- deterministic/no-mutation/standalone tests;
- Release build.

Do not launch Bannerlord or deploy until that offline implementation passes.

Do not begin Phase 6.
