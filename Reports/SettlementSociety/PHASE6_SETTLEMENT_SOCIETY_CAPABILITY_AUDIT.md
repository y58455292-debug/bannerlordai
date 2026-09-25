# Phase 6 — Settlement Society / Vanilla Capability Audit

Date: 2026-09-25 UTC  
Authoritative base: `bb46ee809b6d33bfc75d80a805e16ca4b734c88c`  
Scope: **offline audit and prioritization only**

## 1. Executive summary

**Audit: COMPLETE for this bounded twelve-domain milestone. Phase 6 gameplay implementation: NOT STARTED. First Phase 6-v1 target: IDENTIFIED. Phase 7: NOT STARTED.**

The best first target is **needs-aware native daily civic-project selection for low-loyalty, idle NPC towns**. At Bannerlord's existing selection opportunity, prefer the town's existing **Festival and Games** project when loyalty is below the selected native model's rebellious-state threshold. Preserve the native call cadence, construction queue, project objects, effect calculation, player choices, and actual project-state mutation. This is a proposed decision rule, not implemented source or a balance claim.

The decisive finding is small and concrete: the installed `DefaultBuildingScoreCalculationModel.GetNextDailyBuilding(Town)` chooses randomly from daily projects. `BuildingsCampaignBehavior` already asks this model, then commits the returned existing project through `BuildingHelper.ChangeDefaultBuilding`. Festival and Games already supplies a native loyalty contribution. A narrowly gated selected-model wrapper can improve an actual NPC governance decision without inventing a new settlement simulation. **Construction must be idle:** `Town.CurrentDefaultBuilding` is null while the queue is nonempty, and a daily project's effect is gated on being the current default. [E09–E13]

The deeper issues/notables investigation also disproved several tempting feature premises:

* NPC settlement visitors already resolve eligible issues: the inspected entry handler uses a 5% attempt chance in the visitor's own clan settlement and 1% elsewhere, then selects an issue and checks native eligibility. This is an abstract visit-triggered resolution, not a demonstrated mission or logistics simulation. [E01]
* Unresolved issues already feed settlement and notable models. Example issue implementations contribute prosperity, loyalty, security, or notable-power penalties; their removal can stop those contributions. Effects are issue-specific. [E02–E04]
* Native clans already assign governors; governors have native economic/perk and relationship effects. The gap is not that NPCs cannot govern. [E07–E08]
* NPC workshops consume inputs, produce outputs, face capital constraints and bankruptcy, and can acquire a new owner and production type. Gangs also have native alley ownership turnover. Neither subsystem is simply a static asset list. [E16–E18, E29]

Accordingly, this audit does **not** recommend a generic NPC issue solver, a second protection/patrol system, new workshop simulation, or a replacement economy. Those would duplicate native or ClanAI functionality and introduce much greater risk.

### Evidence boundary

The evidence is targeted **source inspection of installed binaries**, not a new campaign observation. The main evidence ledger is [phase6_native_audit_evidence_20260925.md](evidence/phase6_native_audit_evidence_20260925.md); its E00–E38 identifiers are used throughout this report. The companion manifest records resolved type names, binary identity, targeted output digests, and current ClanAI source digests. Broad decompiled class trees remain local and are not part of this checkpoint.

Supported binary identity:

| DLL | SHA-256 |
|---|---|
| `TaleWorlds.CampaignSystem.dll` | `5B23C3E36D7A5D6D47C47EAB075E9E2B1CC8D1AA53C79E135FA2B5EF434EBC5F` |
| `SandBox.dll` | `16AF436C569675EB30E22514BB755E6FB3612AFCE38F6CC55D1748D079C19C1A` |

Both DLLs expose generic file/product versions; this report does not infer a marketing game version from them. Names were resolved from these installed assemblies, not guessed from another version's documentation. The default model's implementation is established offline; which model wins selection under a particular loaded module stack still requires explicit verification before future deployment. An unfamiliar replacement model must not be silently overridden.

## 2. Player-versus-NPC capability matrix

### Reading the matrix

**P/N** means player capability / NPC capability. **Yes** means a relevant native route exists in the audited source; it does not mean every actor can use it or that it was runtime-proven here. **Partial** distinguishes indirect ownership/control, abstract resolution, narrower NPC eligibility, and interfaces that are deliberately player-only. Systemic settlement simulation counts as NPC-world functionality, not proof that a named NPC deliberately planned the outcome.

Each row records capability, P/N, native actor and trigger, relevant state, persistent consequence, player-visible consequence, current ClanAI involvement, parity gap, importance, candidate seam, proof status, and implementation decision. Cross-references avoid treating the same recruitment, trade, or prisoner pipeline as three separate features.

**Proof codes:** `N` = native source route/effect inspected; no new runtime proof. `R4A`, `R4B`, `R4C`, and `R5` refer only to the particular already-accepted branches documented in the existing reports, not every capability in that row. `R3` refers to the named existing native-candidate/commit evidence. The full completion ladder remains separate: implemented, build-proven, runtime-observed, causal, boundary-crossing where relevant, committed in-world, player-legible, long-run stable. No row is declared complete across that ladder merely because it has code.

### A. Towns

| Capability | P/N | Native actor; trigger/cadence | State and persistent consequence; player-visible consequence | ClanAI coverage, gap, importance, seam, proof, decision |
|---|---|---|---|---|
| A1. Enter, wait, trade, recruit, and use town services | Yes / Partial | `PlayerTownVisitCampaignBehavior` menu routes; NPC visit, recruitment, caravan and prisoner behaviors on their own ticks/entry events. [E19, E24–E25, E31, E34] | Native access and ownership gates; inventories, gold, rosters and prisoners change through their own systems. Player sees service menus, stocks and parties. Walking into a UI scene is not itself a simulation gap. | Existing recruitment/strategic layers already participate. Gap: not every player service is an NPC activity. Importance: high for actual services, low for menu mimicry. Seam: existing service-specific models only. Proof: N plus relevant prior branches. Decision: **no** generic town-service AI. |
| A2. Notables, issues, gangs and local consequences | Yes / Partial | Notable/issue/alley behaviors; issue generation, NPC entries, daily settlement/hero ticks. [E01–E07, E14–E15, E29] | Issue state, power, relations and alley ownership have consequences; player sees issue offers, recruitment access, gangs and model breakdowns. NPC issue completion need not represent the same physical quest activities. | Social/mercy/Phase 4 systems cover separate parts, not a general issue solver. Gap: shallow issue response and limited NPC attribution. Importance: high. Seam: see H2–H4 and candidate 2. Proof: N. Decision: **defer** extra issue behavior. |
| A3. Choose development and daily civic projects | Yes / Partial | `BuildingsCampaignBehavior` asks selected `BuildingScoreCalculationModel`; NPC daily queue consideration 10%, daily-project consideration 1%; player management is separate. [E09–E12, E34] | Queue, existing buildings and saved default flag; actual building effects influence town conditions. Player can select projects; a native project/effect can be inspected or explained after commitment. | No current ClanAI building-selector integration found. Gap: audited default selectors choose randomly, not by current civic need. Importance: high. Seam: **GetNextDailyBuilding(Town)** only. Proof: N. Decision: **yes — selected v1**, limited to idle, low-loyalty NPC towns. |
| A4. Ownership, security, prosperity and governance effects | Partial / Yes | Native governor assignment, loyalty/security/prosperity models, owner-change and daily callbacks. [E07–E09, E13, E15] | Owner culture, governor, local state and issues enter native calculations; ownership also affects workshops and construction queues. Visible consequences include taxes, loyalty, prosperity and recruitment context. | WarState/War Strain, Phase 4 and Phase 5 already interact at separate boundaries. Gap: decision quality, not absence of settlement statistics. Importance: high. Seam: choose an existing action, do not add another stat multiplier. Proof: N; R4B/R4C/R5 are limited existing proofs. Decision: **no** replacement governance/economy model. |

### B. Villages

| Capability | P/N | Native actor; trigger/cadence | State and persistent consequence; player-visible consequence | ClanAI coverage, gap, importance, seam, proof, decision |
|---|---|---|---|---|
| B1. Production and trade | Partial / Yes | `VillageGoodProductionCampaignBehavior` daily production; `VillagerCampaignBehavior` hourly travel and settlement-entry sale. [E19–E20] | Village type, stock capacity and production model determine goods; villagers sell at towns and handle income/tax. Stocks, supply and moving villager parties are visible. | Bandit/security and war layers influence the surrounding world, not a new trade engine. Gap: no need for a second supply network. Importance: high. Seam: existing production/trade models only after a concrete defect. Proof: N. Decision: **no** new production/trade behavior. |
| B2. Raiding, protection and relations | Yes / Yes | Native raid/map-event and villager response paths; `CharacterRelationCampaignBehavior` raid/rescue callbacks. [E15, E19; accepted WarState/Home Responsibility source review] | Raids disrupt villagers and local state; defense/rescue can alter relations. Burning villages, losses and relation changes are player-visible. | Home Responsibility, Kingdom Objectives, Visual War, WarState/WarScar already cover strategic response/history. Gap: effectiveness and balance remain separate, not missing generic protection. Importance: high. Seam: existing native candidates; do not duplicate them. Proof: N and named prior strategic evidence. Decision: **no** new patrol/protection system. |
| B3. Recover after damage | Partial / Yes | `VillageHealCampaignBehavior` daily settlement health recovery outside battle/siege; native hearth/prosperity model and volunteer production. [E20–E24] | Settlement health, hearth and volunteer availability are different recovery dimensions. Recovery can restore visible village activity and recruiting supply; it is not an instantaneous reset. | Phase 4B/4C already alter shared volunteer recovery/quality; Phase 5 reads bound security. Gap: no evidence justifies another recovery boost. Importance: high. Seam: none selected. Proof: N plus R4B/R4C for their specific branches. Decision: **no** retuning. |
| B4. Notables, issues, militia and bound security | Partial / Partial | Issues/notables, militia model, and bound fortification reads; issue entries/daily model updates. [E01–E07, E21–E22] | Village notables and militia are native; bound-fortification security used by ClanAI is not a separately simulated village Security statistic. Effects can propagate to the fortification. Offers, defenders and shared recruitment are visible. | Existing Phase 4/5 bound-security paths apply without new ownership rules. Gap: task-specific civic response remains abstract. Importance: medium/high. Seam: existing issues or town civic choice, not invented village security. Proof: N and R5 village path. Decision: **defer** additional village agency. |

### C. Castles

| Capability | P/N | Native actor; trigger/cadence | State and persistent consequence; player-visible consequence | ClanAI coverage, gap, importance, seam, proof, decision |
|---|---|---|---|---|
| C1. Garrison recruiting, bound villages and troop transfers | Yes / Yes, with restrictions | `GarrisonRecruitmentCampaignBehavior` daily; `GarrisonTroopsCampaignBehavior` lord/army entry. [E22–E23] | Shared notable slots, separate basic-troop growth, wage/food limits and native transfer calculations. Player-clan holdings have important transfer exclusions. Visible garrison and party rosters change. | Phase 4 affects only its audited shared volunteer gates. Gap: castles are not missing all recruitment because they lack a town-style recruit menu. Importance: high. Seam: preserve native transfer/pool paths. Proof: N, R4A supporting transfers, R4B/R4C limited gates. Decision: **no** castle recruit-pool invention. |
| C2. Militia and defense | Partial / Yes | Native militia model, fortification defenses and native strategic candidates. [E21, E23; Phase 3 accepted reports] | Militia is distinct from garrison/volunteers; native population, retirement, loyalty and issue effects apply. Defenders and siege resistance are visible. | Home Responsibility/Kingdom Objectives/Visual War already bias appropriate native candidates. Gap: coverage quality is not missing capability. Importance: high. Seam: no new one selected. Proof: N/R3 where specifically reported. Decision: **no** new defense actor. |
| C3. Dungeons and noble/governor presence | Yes / Yes | Player dungeon menus; native entry prisoner handling; clan governor assignment and hero presence routines. [E08, E25, E27, E33–E34] | Native prisoners, governors and hero locations persist. Player can inspect/manage authorized dungeons and encounter residents. Presence alone does not prove a deliberate civic mission. | Mercy and companion duty/memory are already present. Gap: no generic absence of castle administration. Importance: medium/high. Seam: existing native actions only. Proof: N. Decision: **no** duplicate handling. |
| C4. Military recovery | Partial / Yes | Native lord-party recreation, garrison transfer, prisoner recruitment and shared volunteers. [E22–E27, E33; Phase 4A report] | Several independent manpower sources rebuild parties. Their rosters and movements are visible; they do not all pass through Phase 4. | Phase 4A closed characterization; Phase 4B/C bounded policy gates retained. Gap: bypasses are different systems, not automatically defects. Importance: high. Seam: none in this milestone. Proof: N plus R4A/R4B/R4C within accepted limits. Decision: **no** Phase 4 reopening. |

### D. Notable households

| Capability | P/N | Native actor; trigger/cadence | State and persistent consequence; player-visible consequence | ClanAI coverage, gap, importance, seam, proof, decision |
|---|---|---|---|---|
| D1. Roles, power and turnover | Partial / Yes | `NotablesCampaignBehavior`, `NotablePowerManagementBehavior`, `DefaultNotablePowerModel`; hero/settlement ticks. [E05, E14] | Merchant/rural/headman/gang/artisan roles, power, assets and support matter. Low-power assetless notables can disappear. Visible notable availability and power ranks change. | No custom notable-household simulation in current ClanAI. Gap: native household depth is limited, but not static or consequence-free. Importance: medium. Seam: native power/lifecycle models; none selected. Proof: N. Decision: **defer** household expansion. |
| D2. Volunteer generation and relationship gates | Yes / Yes, different access rules | Native recruitment/VolunteerModel and notable state. [E24; Phase 4 audits] | Power and troop eligibility affect quality; relations and ownership/perks gate access. Shared slots are consumed, not independent copies for each actor. Visible recruit lists differ by access. | Local Manpower and Troop Quality already cover their selected gates. Gap: parity does not mean identical access or eliminating native rules. Importance: high. Seam: unchanged existing wrapper. Proof: N, R4B/R4C. Decision: **no** duplicate recruitment feature. |
| D3. Interaction beyond recruitment | Yes / Partial | Native visitor issue resolution, governor relation perks, rescue relations and support changes. [E01, E07, E15, E36] | Issue removal, relation changes and affiliation affect the local world. Some actions are abstract and NPC attribution is less legible than player quests. | Social/companion systems cover different memories; no NPC issue-specific feature found. Gap: depth/visibility rather than universal absence. Importance: high. Seam: verified completion observation, candidate 2. Proof: N. Decision: **defer**, do not add a generic solver. |
| D4. Political support and affiliation | Yes / Yes, asymmetric | `NotableSupportersCampaignBehavior` player bargain; `NotablesCampaignBehavior.UpdateNotableSupport` native daily update. [E36] | SupporterOf and relations persist. Player support uses payment/relation gates; native NPC affiliation can change through its own relation-based update. Visible supporter status and loyalty/power implications exist. | Clan social/loyalty memory is not this entire patronage system. Gap: asymmetrical transactions, not missing NPC affiliation. Importance: medium. Seam: support model only with a demonstrated problem. Proof: N. Decision: **defer**, do not equate parity with copying the player bargain. |

### E. Gangs / crime

| Capability | P/N | Native actor; trigger/cadence | State and persistent consequence; player-visible consequence | ClanAI coverage, gap, importance, seam, proof, decision |
|---|---|---|---|---|
| E1. Alley ownership, growth and decline | Yes / Yes | Installed `SandBox.CampaignBehaviors.AlleyCampaignBehavior`; daily town ownership tick. [E29] | NPC gang leaders can take empty alleys or abandon owned alleys; player alleys have separate attack/defense state. Ownership and local gang presence are visible. | No new ClanAI gang controller. Gap: stochastic local turnover is shallow, not nonexistent. Importance: medium. Seam: existing alley model/ownership logic, not selected. Proof: N. Decision: **no** first-feature gang ecosystem. |
| E2. Clear or occupy alleys; assign a leader | Yes / Partial | Player alley mission/events; `DefaultAlleyModel` clan-member eligibility. [E29] | Clearing/occupation changes native owner and player relations; assigned leaders have Roguery/Mercy and availability gates. Player sees the alley and relevant relationship consequences. | Existing companion memory is not an alley-management replacement. Gap: NPC turnover is not the same mission/assignment process. Importance: medium. Seam: no clean first non-scripted civic suppression action established. Proof: N. Decision: **defer**; no AI mission mimicry. |
| E3. Criminal rating and suppression | Yes / Partial | `CrimeCampaignBehavior` crime-rating drift and player-facing state; native issue/security/combat systems are separate. [E04, E30; Phase 5 audit] | MainHeroCrimeRating is not a general per-NPC criminal ledger. Gang issues can affect security, but that does not prove a full NPC policing economy. Ratings, access and local conditions are visible in their own interfaces. | Local Bandit Control addresses ambient looter placement, not gangs or all crime. Gap: broader criminal careers/policing are unproven and high scope. Importance: medium. Seam: none justified here. Proof: N. Decision: **defer**. |
| E4. Bandits versus town gangs | Partial / Yes | Native bandit ecology, patrols/combat, distinct issue-specific callbacks. [E04; accepted Phase 5 audit/runtime report] | Ambient looters, hideout-linked parties and alleys are not one population. A native issue callback can redirect hideout parties without proving a hideout assault or destruction. | Phase 5 remains Security-only relative looter-site weighting; rear-security response already exists separately. Gap: do not double-count existing control mechanisms. Importance: high. Seam: no Phase 5 change. Proof: N and R5 only for its exact seam. Decision: **no** retuning or new patrols. |

### F. Workshops / economy

| Capability | P/N | Native actor; trigger/cadence | State and persistent consequence; player-visible consequence | ClanAI coverage, gap, importance, seam, proof, decision |
|---|---|---|---|---|
| F1. Buy, sell, change production and manage stocks | Yes / Partial | `WorkshopsCharactersCampaignBehavior` player transactions; `WorkshopsCampaignBehavior` production/storage options. [E18] | Ownership, type, capital and stock settings have native effects. Player financial/management interfaces expose them. A workshop is not a proven companion-governor assignment slot. | No current general workshop AI. Gap: player controls are richer than autonomous investment choices. Importance: medium. Seam: existing native workshop model; not selected. Proof: N. Decision: **defer** expanded ownership strategy. |
| F2. NPC production and input/output trade | Partial / Yes | `WorkshopsCampaignBehavior.DailyTickTown` and notable workshop production cycles. [E16] | Real town inputs, capital, output profitability and town liquidity gate production. Native item output and expenses affect stock/capital. Goods and prices provide visible consequences. | War/Phase 5 may affect surrounding supply indirectly, not workshop formulas. Gap: not a missing production loop. Importance: high. Seam: none needed without defect evidence. Proof: N. Decision: **no** economy rewrite. |
| F3. Bankruptcy and replacement | Partial / Yes | Expense failure invokes native `ChangeWorkshopOwnerByBankruptcy`; replacement type uses native input-density weighting. [E16–E17] | Ownership and type change, so assets are not permanently static. The function named DecideBestWorkshopType uses weighted selection, not a globally optimal plan. Visible workshop availability/type can change. | No duplicate ClanAI bankruptcy system. Gap: anticipatory investment could be deeper, but existing recovery must first be respected. Importance: medium. Seam: model/weighting only after separate evidence. Proof: N. Decision: **no** first-feature replacement simulation. |
| F4. War, ownership and disrupted supply | Partial / Yes | Native war/owner-change transfer handlers; rebel-state production gate; village trade/input supply. [E16, E18–E20] | Relevant player workshop transfers and input shortages follow existing rules. This does not establish identical treatment of every NPC asset. Lost ownership, output and stock changes are visible. | WarState/War Strain already record or affect other war/economy boundaries. Gap: quantify asymmetries before adding another penalty. Importance: high. Seam: none selected. Proof: N. Decision: **defer**, no extra raid/war multiplier. |

### G. Trade

| Capability | P/N | Native actor; trigger/cadence | State and persistent consequence; player-visible consequence | ClanAI coverage, gap, importance, seam, proof, decision |
|---|---|---|---|---|
| G1. Fund caravans and conduct autonomous trade | Yes / Yes, role-specific | `CaravansCampaignBehavior`, selected CaravanModel; native daily hero eligibility, hourly travel and settlement-entry transactions. [E31–E32] | NPC merchants have explicit creation eligibility; inventory, gold, capacity and destination choice drive trade. Player-funded caravans use the native trading machinery. Parties, transactions and income are visible. | Companion/strategic systems must not become a second caravan controller. Gap: not absent NPC commerce. Importance: high. Seam: native trade model only after defect evidence. Proof: N. Decision: **no** new merchant-movement system. |
| G2. Settlement supply and shortages/surpluses | Yes / Yes | Town trade menus, caravan buying/selling, villager sales and workshop/village production. [E16, E19–E20, E31, E34] | Shared stocks, costs and production shortages can affect trade and output. Availability/prices are observable; equilibrium and magnitude are not proven by static inspection. | Existing local conditions and war/bandit effects are indirect. Gap: do not infer that every shortage lacks a native response. Importance: high. Seam: no wholesale supply replacement. Proof: N. Decision: **no** new economy simulation. |
| G3. Bandit and war disruption of commerce | Partial / Yes | Native movement, encounter/combat and raid-response systems. [E19; accepted Phase 5 native audit] | Goods/parties can be lost or delayed; villagers react to raided homes. A direct Security-to-profit formula is not established here. Lost caravans, disrupted villages and stock changes are player-visible. | Phase 5/Visual War/Home Responsibility already have distinct upstream or defense roles. Gap: effectiveness/balance, not a mandate for custom escorts. Importance: high. Seam: none selected. Proof: N; R5 is not trade-balance proof. Decision: **defer** quantitative trade-risk work. |

### H. Local issues

| Capability | P/N | Native actor; trigger/cadence | State and persistent consequence; player-visible consequence | ClanAI coverage, gap, importance, seam, proof, decision |
|---|---|---|---|---|
| H1. Accept quests, delegate and use lord solutions | Yes / Partial | IssueBase and issue-specific behaviors; player conversation/quest/alternative timers and eligible lord-solution paths. [E02–E04, E35] | Quests, assigned troops/companions, costs and issue effects persist. Player journals and outcomes are explicit. LordSolution methods can debit Clan.PlayerClan and reward MainHero; they are not generic NPC dispatch APIs. | No new solver required. Gap: NPC abstract completion differs from a player quest. Importance: high. Seam: never repurpose player-only resource APIs blindly. Proof: N. Decision: **no** generic delegation copier. |
| H2. NPC completion | Partial / Yes, abstract | `IssuesCampaignBehavior.OnSettlementEntered`; 5% own-clan / 1% other attempt, then random issue, CanBeCompletedByAI and IsOngoingWithoutQuest gates. [E01] | Native completion deactivates an issue and can invoke issue-specific consequences. The visitor is not shown physically performing every quest action. Issue disappearance/effect changes may be noticed, but attribution is limited. | No CompleteIssueWithAiLord integration found in ClanAI. Gap: opportunistic response, not zero agency. Importance: high. Seam: passive observation or later existing-candidate prioritization. Proof: N. Decision: **defer** additional behavior. |
| H3. Unresolved issues, expiration and failure | Partial / Yes | IssueManager daily/hourly lifecycle and selected IssueModel effect aggregation. [E02–E04, E13–E14, E21] | Active effects influence modeled statistics; stay-alive cancellation and due-time checks remove issues. The audited overdue check is probabilistic, not guaranteed removal at the exact due time. Player sees offers, journal states and explained effects. | Phase 4/5 consume some resulting native conditions indirectly; extra correlated penalties risk double counting. Importance: high. Seam: none selected. Proof: N. Decision: **no** duplicate unresolved-issue penalty system. |
| H4. Consequence attribution and truthful civic visibility | Yes / Partial | Native OnIssueUpdated and JournalLogsCampaignBehavior; journal issue path checks solver==MainHero. [E05–E06] | The NPC event exists, but notable-removal cleanup also calls CompleteIssueWithAiLord. Event name alone is not proof of helpful civic action. The audited journal path does not record ordinary NPC solvers equivalently. | Existing player visibility concerns other events. Gap: verified NPC attribution/legibility. Importance: medium/high. Seam: entry-context observation plus post-finalization checks, not raw event announcements. Proof: N; full UI absence across all mods is not asserted. Decision: **defer — candidate 2**. |

### I. Recruitment

| Capability | P/N | Native actor; trigger/cadence | State and persistent consequence; player-visible consequence | ClanAI coverage, gap, importance, seam, proof, decision |
|---|---|---|---|---|
| I1. Shared notable slots and access | Yes / Yes | Player menus, RecruitmentCampaignBehavior and VolunteerModel; daily production and native recruitment decisions. [E24, E34] | Six shared slots, relations, costs and eligibility control supply/access. Actual native consumption removes a slot and changes a roster. Recruit lists and armies are visible. | Phase 4 wrapper already intercepts production only. Gap: none justifying a second pool. Importance: high. Seam: preserve native access and consumption. Proof: N, R4B/R4C specific observed branches. Decision: **no** new pool. |
| I2. Refill and occupied-slot quality | Partial / Yes | Native production and second upgrade gate, selected LocalManpowerVolunteerModel wrapper. [E24; Phase 4B/4C reports] | Empty refill and occupied eligible quality are separate bounded first-gate modifiers; power/tier/upgrade target choice and mutations stay native. Supply and troop quality are visible. | Local Manpower/Troop Quality are already implemented/build-proven/runtime-observed; balance remains unproven. Gap: no new defect identified here. Importance: high. Seam: unchanged. Proof: R4B/R4C, not new stability proof. Decision: **no** retuning. |
| I3. Garrison recruitment sources | Partial / Yes | GarrisonRecruitmentCampaignBehavior daily; town and normal bound-village notable cache plus independent base garrison growth. [E22] | Native pool auto-recruitment consumes slots; positive base growth can add a basic troop without those slots. Expenses, limits and garrison XP are separate. Visible garrison rosters reflect multiple sources. | Only the shared production component belongs to Phase 4's selected seam. Gap: do not mislabel native basic growth as a regression. Importance: high. Seam: none. Proof: N; previously observed gates remain limited. Decision: **no** bypass removal. |
| I4. Other military supply/quality paths | Yes / Yes, not identical | Native party recreation, garrison withdrawal, prisoner recruitment and troop XP/template paths. [E23, E26, E33; Phase 4A] | Roster growth/quality can happen without a notable refill/upgrade. Player can see party changes, but source attribution needs direct evidence. | Phase 4A classified these conservatively; it remains closed. Gap: broader balance accounting is deferred. Importance: high. Seam: none selected. Proof: N plus R4A only for its natural chains. Decision: **defer** long-run accounting, not a Phase 6 rewrite. |

### J. Prisoners

| Capability | P/N | Native actor; trigger/cadence | State and persistent consequence; player-visible consequence | ClanAI coverage, gap, importance, seam, proof, decision |
|---|---|---|---|---|
| J1. Sell/ransom/release | Yes / Yes | Player tavern/dungeon controls; PartiesSellPrisonerCampaignBehavior entry/daily processing; RansomOfferCampaignBehavior daily hero checks and barter. [E25, E27, E34] | Captivity, roster and gold changes use native routes. NPC ransom can require positive combined barter value. Offers, captives and payments are visible. | ClanAI Prisoner/Mercy adds its existing bounded release decisions, threshold 20 locked. Gap: no missing generic ransom system. Importance: high. Seam: preserve existing. Proof: N plus retained mercy evidence, no new runtime test. Decision: **no** redesign. |
| J2. Recruit ordinary prisoners | Yes / Yes, cadence differs | RecruitPrisonersCampaignBehavior; hourly main-party conformity, daily eligible AI lord processing. [E26] | Native conformity/model, capacity and morale determine transfers. Visible prisoners become members. This is a legitimate separate recruitment source. | Do not fold into Local Manpower. Gap: quantitative parity is not established merely by shared models. Importance: medium. Seam: none selected. Proof: N. Decision: **no** duplicate conversion. |
| J3. Settlement dungeons, escape and peace release | Yes / Yes | Native dungeon menus, entry sale/deposit handling, PrisonerReleaseCampaignBehavior on ticks/peace/faction events. [E25, E27, E34] | Prisoner custody and captivity end under native conditions; party versus settlement custody matters. Dungeons and released heroes are visible. | Mercy/social captivity observation already present. Gap: no broad custom dungeon manager justified. Importance: medium/high. Seam: none selected. Proof: N. Decision: **no** additional manager. |
| J4. Execution and feud consequences | Yes / Partial | ExecutionCampaignBehavior prisoner/death/feud callbacks and daily/quarter-hour processing. [E28] | Native execution, player-clan blood-feud and pending execution state exist. Audited routes are substantially player/player-clan-oriented; universal NPC-versus-NPC symmetry is not established. Death/feud notices are explicit. | Preserve Prisoner/Mercy and political memory; do not broaden killing rules. Gap: risky asymmetry, not an early civic feature. Importance: high consequence, high risk. Seam: none selected. Proof: N. Decision: **defer**, no redesign. |

### K. Companions / clan members

| Capability | P/N | Native actor; trigger/cadence | State and persistent consequence; player-visible consequence | ClanAI coverage, gap, importance, seam, proof, decision |
|---|---|---|---|---|
| K1. Governors | Yes / Yes | Player management; ClanVariablesCampaignBehavior daily NPC governor assignment; GovernorCampaignBehavior effects. [E07–E08] | Existing eligible clan lords are ranked for fiefs/governing strength; governor identity and native effects persist. Player sees assignment and settlement effects. | No need for a second governor dispatcher. Gap: civic choice quality differs from assignment capability. Importance: high. Seam: project decision, not reassigning governors. Proof: N. Decision: **no** assignment replacement. |
| K2. Parties and residence | Yes / Yes, role-specific | HeroSpawnCampaignBehavior daily clan/hero routines; native party command ranking and settlement movement. [E33] | Hero availability, governor/party roles and native creation rules govern usage. Parties and residents are visible. NPC use of family lords is not proof of identical wanderer hiring. | Companion duty/experience/negative memory already influences existing native choices. Gap: do not confuse a different actor pool with no clan-member usage. Importance: high. Seam: none selected. Proof: N and existing named evidence. Decision: **defer** wider staffing parity. |
| K3. Caravan/alley assignments and workshops | Yes / Partial | Native caravan and alley eligibility/control paths; workshop ownership is separate. [E18, E29, E31–E33] | Players assign eligible clan members to supported roles; NPC merchants/gang leaders use their own native roles. A generic companion workshop-manager assignment was not established in audited paths. Player sees leaders, owned assets and income. | Existing companion memory is not permission to invent unsupported roles. Gap: actor-role asymmetry, evidence limits. Importance: medium. Seam: existing specific role models only. Proof: N; unsupported workshop-role claim remains unproven. Decision: **defer**. |
| K4. Duty, experience and negative-outcome memory | Partial / Partial | Current ClanAI CompanionDutyMemory, CompanionExperienceMemory, CompanionNegativeOutcomeMemory and strategic integration. [E37] | Persisted bounded memories already bias eligible existing native candidates; this is not a new native society layer. Some results are visible through actual behavior, not automatically through UI explanations. | Direct existing coverage; do not implement it twice. Gap: individual proof/legibility coverage remains separate. Importance: high. Seam: preserve current contributions. Proof: source plus existing reports only, no blanket eight-level completion claim. Decision: **no** duplicate memory. |

### L. Local relationships

| Capability | P/N | Native actor; trigger/cadence | State and persistent consequence; player-visible consequence | ClanAI coverage, gap, importance, seam, proof, decision |
|---|---|---|---|---|
| L1. Recruitment access relationships | Yes / Yes, asymmetric | Native VolunteerModel access and recruitment behaviors. [E24] | Hero/notable relation and native perks/ownership affect access to persistent shared slots. Recruiting interfaces and roster outcomes expose this. | Phase 4 preserves access rules. Gap: no need to award synthetic relation to unlock slots. Importance: high. Seam: none selected. Proof: N/R4B/R4C limited. Decision: **no** relationship mutation. |
| L2. Protection, raids and local gratitude/grievance | Yes / Yes, event-specific | CharacterRelationCampaignBehavior native rescue/raid events. [E15] | Genuine actions can alter relations with notables/owners; explicit gates and beneficiaries vary. Player may see relation changes and later access differences. | Home Responsibility and existing social memories already use real events or decisions. Gap: do not add the same gratitude twice. Importance: high. Seam: existing event boundary only after a missing consequence is proven. Proof: N. Decision: **no** generic reward layer. |
| L3. Owner/governor/order consequences | Partial / Yes | Daily native high/low security and loyalty relationship routines, governor perks, notable support and loyalty model. [E07, E13–E15, E36] | Ownership, support, native conditions and relationships interact persistently. Native messages/model breakdowns expose some effects, not every causal chain. | War/economy/Phase 4/5 relationships are indirect; no extra correlated input is proposed. Gap: civic action choice is weak in the audited default model. Importance: high. Seam: selected A3 project choice. Proof: N. Decision: **yes only through A3**, no direct stat/relation writes. |
| L4. Issue-solver credit and social memory | Yes / Partial | Player-oriented issue reward/journal handlers; NPC IssueFinishedByAILord event has different handling. [E05–E06] | Player issue relations are explicit; universal persistent NPC solver-credit symmetry is not established. Cleanup can emit the same NPC completion event. Visible attribution can therefore be misleading without provenance. | Clan social/loyalty memory already exists but no matching NPC issue-credit integration was found. Gap: possible missing credit, not conclusively a globally absent native reward. Importance: medium. Seam: investigate subscribers before awarding anything. Proof: N with scoped absence only. Decision: **defer**, no new relation or save ledger. |

## 3. Existing BannerlordAI coverage

The current source was inspected from the authoritative archive, not the stale development checkout. E37 and the manifest preserve the reviewed files and hashes. The following systems are explicitly excluded from duplication:

| Existing system | Current source responsibility / retained evidence | Phase 6 boundary |
|---|---|---|
| Home Responsibility | `HomeResponsibilityLayer`/`HomeResponsibilityPolicy`: eligible existing clan-home native candidates, bounded factors; accepted deterministic/wiring evidence. | Do not add another generic owner-protection layer or retune factors. |
| Visual War | `VisualWarDecisionLayer`: native defense/offense/rear-security contributions; accepted matched native-commit evidence. OFF by default remains unchanged. | No bandit-engagement or patrol duplication. |
| Kingdom Objectives | `KingdomObjectiveLayer`: ruler objective candidates/staging through the composer; accepted native commit proof. | No new ruler/settlement orders. |
| Strategic Commitment | `StrategicCommitmentLayer`: existing-candidate retention; Observe remains default. Its bounded Apply runtime attempt was a null, not a proven commit. | No factor/config change or new commitment system. |
| WarState / WarScar | `WarStateBehavior`: native war/raid/siege/loss history and persistence. | Do not add redundant devastation history or infer all scar types are proven. |
| War Strain | `WarStrainRecruitmentPatch` and `WarStrainEconomicPatches`: existing recruitment/economic coupling. | No additional war penalty, no economy rewrite. |
| Local Manpower | Selected `LocalManpowerVolunteerModel` and pure policy: empty-slot first gate; implemented/build-proven/runtime-observed, balance unproven. | Phase 4B unchanged. |
| Troop Quality | Same selected wrapper, quality policy/native eligibility: occupied eligible first gate; implemented/build-proven/runtime-observed, balance unproven. | Phase 4C unchanged; native second gate/trees stay native. |
| Local Bandit Control | `LocalBanditControlPolicy`/patch: Security-only relative looter-site weight; Phase 5 runtime characterization already accepted. | No cap/refill/hideout/security tuning. |
| Prisoner/Mercy | `PrisonerMercyDecisionBehavior`: existing prisoner decision/release path; threshold 20 lock. | No redesign. |
| Companion duty | `CompanionDutyMemory`: bounded duties, persistence and existing-candidate factors. | Not a missing Phase 6 memory system. |
| Companion experience | `CompanionExperienceMemory`: bounded experience/recall and native-candidate influence. | No duplicate companion learning ledger. |
| Companion negative outcomes | `CompanionNegativeOutcomeMemory`: bounded consequence memory. | Preserve existing scope and save state. |
| Clan social/loyalty memory | `SocialLedger`, `SocialLoyaltyPatch`, `SocialDefectionPatch`, `SocialLoyaltyClanLossMemory`: actual event memory/native values. Voluntary leave proof does not prove target-kingdom switching. | Do not invent issue-credit merely to reuse the ledger. |
| Ruler courtship | `RulerClanCourtshipBehavior`: native eligibility/values and AI barter; accepted native joining proof. | No duplicated political recruitment. |
| Kingdom continuity | `KingdomContinuityBehavior`: native lifecycle observation/persisted continuity, with accepted save/reload evidence and separate natural-event limits. | No new political authority or Phase 7 work. |

No current ClanAI C# source reference was found to `BuildingScoreCalculationModel`, `GetNextDailyBuilding`, `ChangeDefaultBuilding`, or `FestivalAndGames`. The selected civic-project decision is therefore not another implementation of an existing ClanAI feature. This source finding does not certify compatibility with arbitrary third-party mods.

## 4. Important native parity gaps

**Governance choice is shallower than governance infrastructure.** NPCs have governors, construction, daily projects and persistent consequences, but the inspected default building selector is random. A player can deliberately choose the existing loyalty project; the default NPC selector does not read loyalty. This is the strongest concrete first gap. [E08–E13]

**Issue resolution exists but is abstract and opportunistic.** Entry probability and random issue selection are not targeted preparation, travel, supply delivery, or a governor recognizing a named issue. The absence of those steps in the inspected entry routine should not be inflated into a claim that no other native issue-specific response exists. [E01–E04]

**NPC civic attribution is weak and can be falsely inferred.** The audited journal path is main-hero-gated. However, a raw IssueFinishedByAILord event also occurs during notable removal. A future notice/credit system must distinguish genuine visitor resolution from cleanup and confirm deactivation after the native method returns. [E05–E06]

**Player economic/role controls and NPC simulation differ.** NPC workshops and caravans are active, but they do not expose every player warehouse, assignment or investment decision. NPC affiliation, crime and execution likewise have narrower/different routes. These are candidates for later questions, not automatic implementation mandates.

## 5. Gaps that should NOT be implemented in this milestone

Reject a generic NPC issue solver: it already exists and can invoke issue-specific side effects. Reject automatic quest success, manufactured supplies, direct relation rewards, and direct calls to player LordSolution methods. Reject interpreting an issue-completion label as a battle, hideout destruction, or grain delivery.

Reject new generic patrol/escort parties, scripted policing, automatic gang cleanup, forced noble orders, duplicated notable pools, universal castle recruitment menus, universal execution symmetry, and companion roles not supported by audited native infrastructure.

Reject replacing workshop production, villager trade, caravan routing, bankruptcy, militia growth, governor assignment, or the economy wholesale. Reject new Security/War Strain/raid-history multipliers, retrospective Phase 4/5 tuning, and a large society-memory save schema.

## 6. Gaps already handled sufficiently by native systems

For first-milestone prioritization, native source already supplies useful actor/state/action infrastructure for shared recruitment, prisoner sale/ransom/conversion/release, garrison transfer, militia, village production/recovery, caravan commerce, workshop failure/replacement, alley turnover, notable support, governor assignment and opportunistic issue resolution.

“Sufficiently handled” here means **do not build a duplicate before proving a specific deficiency**. It is not a statement that these systems are balanced, player-legible in every case, stable across all saves, or runtime-proven by this audit.

## 7. Ranked candidate Phase 6-v1 seams

The shortlist contains three candidates, not twelve parallel implementation projects.

| Rank / candidate | Evidence quality | Living-world / player / parity value | Implementation and save risk | Native authority / overlap | Runtime proof difficulty |
|---|---|---|---|---|---|
| **1. Low-loyalty idle-town daily civic-project choice** | High static evidence: two-method selected-model interface, random default, exact native caller/commit, existing loyalty project and queue gate. | High decision value; NPC uses the same available civic action a player can choose. Actual project/effects provide a legible outcome. | Low/moderate implementation; no new persistent schema. Long-run policy balance unproven. | Preserve native cadence, choices, effects and state mutation. No current ClanAI selector overlap found. | Moderate/high: native 1% daily consideration and idle/low-loyalty gates can yield a bounded null. |
| **2. Verified NPC issue-resolution visibility** | High for journal/entry/finalization paths; scoped, not universal UI-absence proof. | Medium/high legibility, but it adds no new NPC decision or material response. | Low state risk; ephemeral deduplication may suffice. Cleanup false positives require careful provenance. | Observation-only; no solving, reward, relation or party mutation. Separate from existing loyalty/war messages. | Moderate: capture natural visitor resolution, exclude cleanup, verify native finalization before announcing. |
| **3. Issue-aware prioritization of existing settlement-visit candidates** | Medium: issue state and native visitor completion exist; a clean minimal policy still needs narrower behavioral design. | Potentially high agency, but visits do not guarantee resolution or a physical fix. | Moderate/high behavioral risk; opportunity cost and oscillation; avoid new memory schema. | Must use existing native candidates/composer. Significant overlap risk with Home Responsibility and strategic objectives. | High: need a changed native winner, actual arrival and later native issue outcome, without synthetic pressure. |

Workshop redesign, new gang suppression and expanded issue-solver credit are deferred backlog questions, not additional first-feature candidates.

## 8. Selected first Phase 6-v1 target

**Native civic-project prioritization: an existing Festival and Games choice for an idle NPC town in native low-loyalty conditions.**

Proposed seam: a delegating wrapper around the already-selected `BuildingScoreCalculationModel`. Delegate `GetNextBuilding(Town)` unchanged. In `GetNextDailyBuilding(Town)`, execute the selected inner method exactly once and then, only in the supported audited context, return the existing eligible Festival and Games `Building` reference instead of another existing daily choice.

Compatibility boundary: v1 should only adjust the audited default-selector provenance. Unknown or foreign replacement selectors pass through rather than having their intent silently replaced. The selected model and supported signature must be verified before implementation is called build-proven or before a runtime candidate is deployed.

This is **not** “governors solve all issues.” It is a bounded improvement to a real native owner-side civic decision. Issues may contribute to low loyalty, but they remain owned and resolved by their existing native systems.

## 9. Why this target outranks the alternatives

It has a stronger causal gameplay endpoint than a notice alone: local need can change an existing choice, Bannerlord can commit that choice, and the native project can contribute to loyalty. It is smaller than changing noble travel or adding a solver. It uses no new parties, inventory transfers, relationship rewards, issue completions, crime simulation or save records.

Its scope also exposes its limitations clearly. It acts only at native consideration opportunities. It does not clear a construction queue, cure every rebellion, fix a queued-construction stall, ensure the net loyalty delta becomes positive, or repair every underlying issue. Those limits are preferable to hiding a broad rescue script behind a “governance” label.

The base project contributes native loyalty (3 before applicable native effect modifiers in the inspected defaults); the final town loyalty change still includes every other native cause. Choosing a project is not proof of net recovery or long-run balance. [E11–E13]

## 10. Native authority retained

Bannerlord retains NPC eligibility and invocation cadence, the town's building inventory and legal project identities, player project decisions, construction queue and normal development choice, project activation flags, building effect calculation, governor perks, loyalty updates, rebellion rules, all issue generation/completion, and save serialization.

The proposed wrapper returns a reference already belonging to the town. It does not call `BuildingHelper.ChangeDefaultBuilding`, set `IsCurrentlyDefault`, enqueue/dequeue buildings, create a Building, write Loyalty/Security/Prosperity, award relations, or manipulate parties. The existing native caller remains the commit authority. [E09, E12]

## 11. Proposed bounded intervention shape

At a native `GetNextDailyBuilding(town)` call:

1. Obtain the native result once, preserving native RNG use and null/unsupported semantics.
2. Require an initialized normal town, non-player ownership, audited inner model, finite loyalty and a valid selected native threshold.
3. Require an empty construction queue and a usable current daily default. A queued project must not be interrupted; null `CurrentDefaultBuilding` is a passthrough condition.
4. Compare Loyalty strictly below the selected `SettlementLoyaltyModel.RebelliousStateStartLoyaltyThreshold`. Do not hardcode one threshold: the inspected native threshold varies with the high-rebellion option.
5. Require the town already contains the native `DefaultBuildingTypes.SettlementDailyFestivalAndGames` project, that it is an eligible daily project with a usable level, and that the native result is itself a supported existing daily choice.
6. Return that existing festival reference. If native already chose it, preserve the exact result and classify as no intervention. If the current default is already festival, do not manufacture a new commit or duplicate announcement.
7. Otherwise return native unchanged.

The policy takes no independent security, prosperity, garrison, militia, patrol, raid-history, war-strain, Home Responsibility, Visual War, social-memory or external-state input. Loyalty is the native aggregate need signal. This avoids building a second parallel distress model.

No new probability, refill rate, effect magnitude or tick rate is proposed. The native daily-project consideration remains 1%; ordinary building consideration remains 10%. That sparse cadence is a real feasibility constraint, not a reason to increase it for a demonstration. No prototype or runtime source was added in this audit.

## 12. Deterministic test plan — proposed, NOT executed here

A later offline implementation should use a game-assembly-free choice policy plus compiled wrapper/delegation tests.

Required policy cases: supported low-loyalty idle town selects the available festival; equality and above-threshold pass through; negative/missing/non-finite/invalid context fails conservatively; player ownership, castles, villages and unsupported kinds pass through; queued construction and null current default pass through; missing, foreign, non-daily or unusable festival candidates pass through; null/foreign/non-daily native results pass through; native-already-festival preserves identity; current-festival retention creates no synthetic commit; native threshold values follow the selected model/options rather than a hardcoded number.

Required integration cases: inner daily selector called exactly once; ordinary `GetNextBuilding` delegates unchanged; current selected model is wrapped once; foreign inner models remain untouched; returned objects are members of the existing town building set; no extra RNG; no enumeration/candidate insertion side effects; native caller alone changes default state.

Static invariants must reject project-flag/queue/stat/relationship/party writes, direct helper/action calls, save registration, external IO/network/process calls, absolute development paths and runtime harness dependencies. Preserve Phase 4/5 policy blobs and relevant territorial tests unchanged. Release must build with 0 errors before a separately authorized runtime characterization; inherited warning handling stays explicit.

## 13. Runtime proof plan — separate future milestone

Do not launch from this audit. After an offline implementation and exact candidate hash/commit exist, use one preregistered bounded natural run, proposed maximum **168 campaign hours**. Verify a reliable early-stop/time-cap control before starting; do not rely on slow conversational polling to enforce the bound. Stop as soon as the required evidence is available. Native rarity or no qualifying town must produce a bounded null, not more tuning or an indefinite extension.

Required evidence chain:

* Actual selected inner/wrapper types and exact candidate DLL hash; normal native `DecideDailyProject` invocation.
* A naturally low-loyalty idle NPC town, its current queue/default, selected native threshold and existing festival candidate.
* Original native choice versus returned choice. Native-already-festival is a control, not a boundary crossing.
* A genuine changed native choice followed by native `BuildingHelper.ChangeDefaultBuilding` commitment; confirm saved native default flag/current project after the caller returns. A proposed result alone is not a commit.
* Native building-effect contribution and subsequent native loyalty calculation. Record the complete delta; do not claim guaranteed positive loyalty merely from the festival component.
* Player-owned, queued, healthy or otherwise ineligible passthrough controls where naturally available; offline tests cover branches not encountered.
* No telemetry errors, no direct ClanAI world mutations, protected fixture unchanged, rollback intact, no unauthorized save, Strategic Commitment Observe and Visual War OFF.

Never lower loyalty, clear queues, manufacture issues, trigger the selector manually, force project commitment, or accelerate native reconsideration to obtain proof. If the active model is not the audited one, stop/record the compatibility boundary rather than replacing broader native authority. Native project frequency and effect duration are not long-run balance evidence.

## 14. Player-legibility plan

Prefer the actual native project identity and named contribution in native explained settlement statistics. Verify that the relevant NPC-town information is accessible to the player; source-level `ExplainedNumber` labels alone do not prove a usable player-facing screen.

If a small native message is needed, emit it only after observing the actual native change, only for a directly relevant/visible town, with bounded deduplication. Example proposed wording: “{Town} has switched to Festival and Games while loyalty is low.” Do not name a governor as the decision-maker unless the evidence supports that attribution. Do not announce issue resolution, successful rebellion prevention or net recovery from a project choice.

A log file is development evidence, not player legibility. No message or UI behavior is implemented here.

## 15. Save/load considerations

The native `Building.IsCurrentlyDefault` field is already saveable. The first implementation should need no new persistent game state: decisions are derived from current native town state, and Bannerlord owns project flags and serialization. [E12]

Any transient observation/deduplication state must reset safely on load/session change and must not replay historical selections as new commits. Removal of the mod should leave only valid native building references and flags, not custom serialized types. Player acquisition of a town must immediately restore full player-choice passthrough.

A later authorized save/load check must use a separately named fixture, verify the native project survives reload, and protect the original baseline. Source attributes are not runtime save/load proof. No save was created or altered by this audit.

## 16. Standalone / release safety

This checkpoint adds documentation and evidence only. No Phase 6 gameplay source, DLL, package dependency, save schema, development activation file or world state was introduced. The base source/config/test trees were compared byte-for-byte with the authoritative archive. No build rerun or runtime success is claimed for Phase 6. [E38]

A future implementation must operate solely on native in-process state. No ChatGPT, Codex, Desktop Commander, TestRunner, watchdog, provider, external service, network, external file or development-machine path may be needed for the gameplay decision. Development tools may collect proof but cannot supply eligibility, outcomes or production configuration.

This does not certify that every inherited research logger/configuration path in the existing repository is already release-hardened. Existing research infrastructure remains separate from the selected feature and must not be turned into a product dependency. Final standalone packaging/hardening remains an explicit release obligation, not an inferred result of this audit.

## 17. Deferred Phase 6 backlog and closure

Prioritize later only with new evidence: verified NPC civic notices; issue-aware visits without strategic duplication; issue-specific completion/resource/credit semantics; queued-construction distress and ordinary building-choice quality; meaningful governor explanations; NPC investment strategy beyond existing bankruptcy; gang/crime depth beyond native turnover; actor-role asymmetries; and local consequence legibility.

Do not treat open items as permission for new behavior in this checkpoint. In particular, full NPC physical quest completion, a universal policing economy, generic economic optimization, workshop companion managers, universal execution parity, and a persistent custom notable society require evidence and scope decisions not supplied here.

### Completion ladder after this audit

| Item | Status |
|---|---|
| Phase 6 twelve-domain audit and prioritization | **COMPLETE**, bounded source audit with explicit uncertainty |
| Selected first Phase 6-v1 target | **IDENTIFIED** — low-loyalty idle NPC-town daily civic-project selection |
| Phase 6 gameplay implemented / build-proven | **NO / NO** |
| Phase 6 runtime-observed / causal / boundary-crossing / committed in-world | **NO / NO / NO / NO** |
| Phase 6 player-legible / long-run stable | **NO / NO** |
| Phase 4 / Phase 5 changes | **NONE**; accepted proofs retained, balance unproven |
| Phase 7 | **NOT STARTED** |

The next bounded milestone is a separate **offline-only implementation and validation of the selected project-choice seam**, with exact native/foreign-model passthrough and no direct mutation. Stop after committing this audit; do not implement that milestone, launch Bannerlord, deploy a DLL, or begin Phase 7 in this task.
