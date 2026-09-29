# LW1-A — Persistent Home Assignment / Peacetime Responsibility Native Audit

Date: 2026-09-29. Source: GitHub main `cc706af55d771816aa22651742a20bcd681ee2c1`.
Scope: Codex-only offline source research and documentation. No implementation.

## Result and evidence boundary

**LW1-A audit: INCOMPLETE.** The current ClanAI architecture, persistence conventions and preservation boundary are audited. The decisive supported-native peacetime candidate generator cannot be fully inspected with the available inputs. Do not interpret this checkpoint as permission to implement LW1-B.

The player target remains: “I assigned my brother to Castle A and my son to Castle B. They now behave like those places are their homes instead of randomly wandering the kingdom.”

Verified finding: the current composer can prefer only existing candidate indices. It cannot return a party home when the exact candidate is absent. Unresolved finding: whether the supported native selector supplies that candidate near home or from far away during peace, including for player-clan lord parties. Neither general native movement capability nor prior war runtime evidence answers that question.

The required source documents were read at source main. Latest history is LW0 `cc706af`, RC1 freeze `9ec113e`, and accepted I4R4 closure `44f7bc6`. No checkout or interrupted local implementation existed; authoritative files were fetched into a scratch audit cache. No agent delegation or external coder was used.

Source references below are paths at source main. Focused excerpts, Git blob identities and inspection results are in [focused evidence](evidence/lw1a_home_assignment_native_audit_20260929.txt).

## 1. Existing Home Responsibility

| Surface | Exact current behavior |
|---|---|
| Registration | `SubModule.cs` registers `ClanAIStrategicBehavior`. Its `RegisterEvents` installs the Harmony postfix on `CampaignEventDispatcher.AiHourlyTick(MobileParty, PartyThinkParams)`. |
| Dispatcher eligibility | `ClanAIPostVanilla.Postfix`: enabled switch, non-null party/thinkParams, lord party, leader, map faction, no army. |
| Executed branch | `HourlyObserveOnly = true`. Despite that historical name, the scaffold calls layers and `StrategicDecisionComposer.Complete`, which writes adjusted candidate scores. Do not describe it as globally mutation-free. |
| Ordering | Visual War → negative-outcome observation → Holdback → Home Responsibility → duty memory → experience memory → Kingdom Objectives → outcome/final safety → composer completion. |
| Layer eligibility | `HomeResponsibilityLayer.Apply` verifies pending state first; then requires leader, thinkParams, composer, clan and non-main party. |
| War gate | `WorldScopeContext.EligibleIndependentLordAtWar(actor)`: active lord party, leader, non-main, no army, non-null map faction and actual clan; then `FactionAtWar(actor.MapFaction)`. “Independent” here means outside an army, not politically independent. |
| Candidate input | Existing `thinkParams.AIBehaviorScores`; settlement is `AIBehaviorData.Party as Settlement`. Score is `composer.CurrentScore(i, rawScore)`. |
| Pure policy | `HomeResponsibilityPolicy.Evaluate(actorEligibleAtWar, ownedByActorClan, baseScore, underSiege, underRaid, weak, behavior)`. No TaleWorlds types. |
| Weak context | Blackboard readiness/food if present, else `PartySizeRatio` and `GetNumDaysForFoodToLast()`. Weak: readiness < 0.72 or food days < 3. |
| Ownership | Matching actual clan and settlement owner by reference or ordinal StringId. There is no persistent explicit home. Every actor-clan-owned settlement is eligible. |
| Factors | Siege first 1.60; raid 1.60; weak GoToSettlement 1.35; DefendSettlement 1.30; PatrolAroundPoint 1.25; GoToSettlement 1.15; otherwise passthrough. Eligibility rejects score ≤ 0. |
| Threat detail | Siege/raid branches precede behavior classification and can support a positive owned settlement candidate classified Other. Preserve that existing behavior; do not silently narrow it. |
| Composer | Applies factors by existing index; captures fixed candidate count and rejects count changes on completion. Does not create targets or candidates. |
| Commit verifier | Only a layer winner change creates `PendingByParty`. On the next layer call, pending is removed and actual default/short behavior plus target/short-term target are compared. Match records successful duty memory. No pending expiry in this Home Responsibility verifier. |
| Counters | Evaluations, applications (frames with changes), winner changes, commit checks and matches; reset on session. |

`WorldScopeContext` caches war per faction StringId for less than one campaign hour. Cache misses scan Settlement.All. Its threat snapshot scans Settlement.All once per campaign-hour snapshot and groups siege/raid settlements by faction. `HasOtherUrgentClanThreat` checks the cached list for another actor-clan holding. `Reset` clears both. These existing scans are not an O(1) peacetime assignment lookup; do not copy them into a per-party new path.

Deterministic entry points: `Tests/TerritorialResponsibility/TerritorialResponsibilityPolicyTests.csproj`, `Program.cs` (ten cases), and `test_runtime_wiring.py`. Accepted result: `Reports/TerritorialResponsibility/PHASE3_DETERMINISTIC_TEST_RESULT.md`. The Python wiring check was rerun against current fetched source and passed; no build or compiled test was rerun.

Runtime evidence boundary: `PHASE3_SOURCE_AUDIT.md` explicitly preserves earlier Phase 0 Home Responsibility evidence and claims no new runtime result. The current tree has no dedicated Home Responsibility runtime report under TerritorialResponsibility. Do not invent one or substitute Kingdom Objective/Visual War proof for it. Their separate native commit proofs remain accepted for their own features. Existing source verification and historical evidence are preserved unchanged.

Reuse unchanged: candidate iteration, composer factor application, owner checks, dispatcher opportunity, session reset patterns and observation-only verification concepts. Preserve all war eligibility, priorities, factors, candidate identities, ordering and duty-memory behavior. Do not reuse “any owned settlement is home,” learned 72-hour duty memory as explicit assignment, name fallbacks as durable identity, war recovery factors as peace balance, or the war gate by deleting it.

## 2. Narrow actor scope and identity

Proposed v1 is a land mobile lord party of the player clan, led by an adult player-clan family member or companion. Main/player party, caravans, army members and non-lord temporary parties are excluded. Assignment can remain stored while a hero has no eligible party, but it exerts no influence.

Adapter predicates to validate against supported native types before implementation:

- party != null; IsActive; IsLordParty; !IsMainParty; not reference-equal to MobileParty.MainParty; !IsCaravan; Army == null; MapFaction != null;
- ActualClan matches Clan.PlayerClan;
- LeaderHero != null; hero != Hero.MainHero; !hero.IsHumanPlayerCharacter; IsAlive; !IsPrisoner; not disabled/template;
- hero.Clan matches Clan.PlayerClan; adult according to the selected AgeModel.HeroComesOfAge;
- hero.PartyBelongedTo is this party and hero is its current leader;
- allow clan nobles/family and player companions; reject notable/special/temporary actor identities;
- suspend while native AI is disabled, party is disbanding or another native command owns movement; exclude naval navigation from first scope until its control path is audited.

The first group is grounded in current dispatcher/WorldScope source; native age/player-clan exclusions are supported by retained Phase 6 E33 excerpts. Exact disabled/disbanding/temporary/navigation member names and player-party command semantics remain source-audit obligations, not compile-proven predicates. No custom age or family-name inference.

**Selected durable identity: leader Hero.StringId → Settlement.StringId**, ordinal and nonempty; never hero display name or MobileParty.StringId alone.

| Lifecycle | Proposed contract / evidence |
|---|---|
| Normal progression | Assignment follows the same hero; read current leader on every decision. |
| Recreation | Retained Phase 4A Megenhelda evidence proves the same hero StringId linked a distinct native PartyBase; party StringId was reused. Persist hero identity, not party object/ID. This sample is NPC-clan recreation, not proof of automatic player-clan recreation. |
| Save/reload | Existing duty/noble memories serialize hero keys and settlement IDs. Resolve IDs to fresh native objects after load; never persist object references. Ordinary ledger reload is accepted evidence, but this new contract has no runtime proof yet. |
| Leader change | New leader uses only their own assignment. Never transfer old leader's home to successor automatically. Clear old party pending verification/reference caches. |
| Destruction | Clear transient party references/pending state; retain living eligible hero's assignment for later native recreation. No spawning or command creation. |
| Hero death/clan departure | Remove assignment when authoritative state confirms permanent invalidity. No inheritance in v1. |
| Missing hero on import | Defer native resolution until campaign objects are ready; then reject unresolved identity. Do not confuse load ordering with deletion. |

The hypothesis is supported by project persistence conventions and a real recreation chain. Universal native identity uniqueness, player party recreation and load-ready resolution API still require LW1-A2 inspection.

## 3. Native peacetime candidate availability — unresolved gate

The supported binary identity in retained Phase 6 evidence is:

`TaleWorlds.CampaignSystem.dll` SHA-256 `5B23C3E36D7A5D6D47C47EAB075E9E2B1CC8D1AA53C79E135FA2B5EF434EBC5F`.

The manifest records `AiVisitSettlementBehavior` decompiler output: 810 lines, SHA-256 `6798de039c820487c0c557cba244d410c7844f26b4a3dc281692d6427ffa7281`. It does **not** include its full selector in the repository. The retained excerpt file has no AiVisitSettlementBehavior scoring body. A manifest entry is not inspected source.

Workspace searches found no supported native DLL/full selector. Library searches did not resolve the native archive; unrelated historical patches/images were not treated as authoritative. Device inventory reports DESKTOP-JO4B7VH offline, last seen 2026-09-27. No remote process, file command, game command, build or launch was issued. This is a source-access blocker.

| Native activity | What can be concluded now |
|---|---|
| GoToSettlement at assigned town/castle | Current layer can adjust it if present and positive. Native exact-home generation rules, distance cutoff and player-clan gates are uninspected. |
| PatrolAroundPoint at holding | Current layer accepts a settlement-valued candidate. Native peace generation/radius and whether it uses a settlement versus point target remain uninspected. Do not treat any nearby point as exact home. |
| DefendSettlement | Current war layer supports it. No evidence here establishes normal peace availability; keep peace policy passthrough. |
| Recruitment/recovery/resupply | Native systems exist and own effects. Presence of recruitment at settlements does not prove which destination candidate is available or viable. |
| Other local life | Retained Phase 6 E01 supports native issue handling on settlement entry; E28–E33 support prisoners, commerce and clan mobility. These are downstream activity, not return-home candidate proof. |

Explicit answers:

**A. Near home:** NOT ESTABLISHED. Suitable local candidates may be available, but neither “normally” nor exact assigned-holding targeting is proven.

**B. Far from home during peace:** NOT ESTABLISHED. No source-backed guarantee that native generation includes that exact arbitrary player-selected holding. Do not claim it is always absent either.

**C. Missing candidate:** NO. Current composer cannot legally express the missing destination under the stated restrictions. Multiplication of other indices cannot create an exact-home target. Zero times any finite factor stays zero.

**D. Smallest seam:** Not yet selected at a concrete native method/signature. The only acceptable research target is the native settlement candidate-enumeration/scoring stage **before** CampaignEventDispatcher.AiHourlyTick dispatch/composer capture. Investigate whether one existing native home/settlement reference can cause the native generator to evaluate/retain the assigned legal holding through its unchanged eligibility, reachability, scoring and candidate-add path. Do not patch the post-dispatch composer to append AIBehaviorData, overwrite destinations, call SetMoveGoToSettlement or alter position. Do not write Hero.HomeSettlement blindly: it may affect birth/spawn, economy, politics or other systems.

If an exact candidate already exists for the whole v1 scope, no additional seam is needed. If it is omitted by distance/enumeration, document one precise bounded native-generation seam and prove its legality before implementation. If no such seam exists, this v1 target must be narrowed or remain blocked. An unspecified seam is not a CONDITIONAL GO.

## 4. Score scale and factor safety

No supported-native peacetime score formula/range has been recovered here. Phase 3 tests use synthetic score 10 solely for arithmetic; it is not a native range/example. War/Kingdom Objective runtime values are not peace calibration.

A finite positive multiplicative factor is structurally appropriate only for an already-positive native candidate. Require finite raw and working scores, native raw > 0 and working > 0; preserve zero, negatives, NaN/infinity as passthrough. Do not resurrect native non-viability or invert another layer's safety decision.

| Candidate | Proposed treatment pending score audit |
|---|---|
| Exact-home GoToSettlement | Bounded support only if native supplied, legal and positive. No extra “far-away” bonus before distance/score analysis. |
| Exact settlement-valued home patrol | Bounded local support; no proximity reinterpretation or coordinate-created target. |
| Home recovery/resupply | Let native need scoring produce urgency. No copied weak-war multiplier or additional stacked recovery bonus in v1. |
| Unrelated settlement | Factor 1; no global suppression. |
| Defend/attack/raid/army/prisoner/caravan behavior | Peace policy passthrough. |

Tentative **test envelope**, not selected gameplay tuning: factors 1.00–1.25, one home contribution per candidate/frame. This is capped at the established war patrol factor and below existing recovery/threat factors, but does not prove adequacy for peace. Default remains 1.00 until score-source audit selects values. For factor f, home overtakes competing score s only when f × homeScore > s; a 1.25 ceiling can overcome at most a 25% score deficit. Example 1.00 versus 1.20 is illustrative arithmetic, not observed native telemetry. Stronger needs can still win. Never increase factors merely to manufacture return.

LW1-A2 must extract formulas, conditional zeroes, distance attenuation, target ownership/need multipliers and native chooser thresholds/tie behavior for GoToSettlement and patrol before choosing constants. If this envelope cannot produce useful home gravity, revisit the purpose/native seam rather than inflate it.

## 5. War integration and preservation boundary

Two mutually exclusive behavior domains:

- Existing War Home Responsibility: unchanged world-scope gate, all-owned-settlement scope, readiness/food rules, siege/raid precedence, factors, composer source and duty-memory match.
- New HomeAssignmentPolicy: player-clan assignment only, confirmed peace only, exact-home positive supplied candidates only. Factor 1 throughout war/unknown war state and on urgent clan siege/raid context.

Explicit assignment remains persistent during war and visible to the player, but v1 adds no war score. Thus existing urgent threats to another player-clan holding can supersede ordinary peace gravity without war regression. Assignment names the primary peacetime responsibility; it does not narrow the proven war layer to that holding. A later assignment-aware war extension is separate evidence-producing work.

Do not multiply a peace factor onto the 1.60 threat factor or redesign Kingdom Objectives. Return to peacetime responsibility when native war/control state ends, not by issuing a forced return. Immediate war state for the new layer must not rely solely on the old less-than-one-hour cached result; inspect an authoritative faction war surface/events or fail closed.

## 6. Persistence contract selected as design only

All active keys found across current ClanAI C# SyncData calls:

1. ClanAI_NobleMemory_v1
2. ClanAI_SocialLedger_v1
3. ClanAI_SocialEpisodes_v1
4. ClanAI_DynastyCanonCutoffHours_v1
5. ClanAI_DynastyBranchId_v1
6. ClanAI_DynastyBranchEpisodes_v1
7. ClanAI_CompanionDutyMemory_v1
8. ClanAI_CompanionExperienceMemory_v1
9. ClanAI_CompanionNegativeOutcomeMemory_v1
10. ClanAI_SocialLoyaltyClanLoss_v2
11. ClanAI_WarState_v1
12. ClanAI_KingdomContinuity_v1

No existing key, row interpretation or native save definition changes.

Proposed new key: **ClanAI_HomeAssignment_v1**, List<string>, absent key = empty assignment set. Proposed row: **D1|<base64-UTF8 Hero.StringId>|<base64-UTF8 Settlement.StringId>**. D1 is this new row's own version, not DynastyBranchEpisodeMemory's D1. Three fields only; no names, party IDs, timestamps, scores, counter or native references. Export sorted by ordinal hero ID for deterministic saves.

Import clears session assignment/transient caches once, parses independently per row, accepts only exact D1/three fields, strict valid UTF-8/base64, nonempty IDs and bounded row/ID sizes. Proposed safety ceilings: 256 rows, 512 decoded bytes per ID; over-limit input rejects the assignment payload rather than truncating silently. These are data-safety limits, not simulation tuning.

Resolve at a native objects-ready session boundary. Hero must be living adult player-clan family/companion, non-player; settlement must resolve to a town/castle owned by Clan.PlayerClan. Villages rejected. Unknown version, malformed/invalid rows, missing/deleted objects and foreign/lost settlement are discarded with aggregate diagnostic reasons, no exception escape. Duplicate identical rows coalesce; conflicting homes for one hero invalidate that hero's assignment, independent of row order. Do not select “last wins.”

One hero has at most one home. Change replaces the row/cache; clear removes it. At runtime always check current leader identity and current settlement.OwnerClan directly before any factor: loss stops influence immediately even if an event/cache update is late. Ownership-loss event removes assignment so recapture does not silently reactivate it. Hero temporarily without a party remains assigned; permanently invalid hero is removed. Session load must not clear restored records again; follow existing loaded-from-save lifecycle patterns.

Cache: Dictionary<string, AssignmentEntry> with ordinal hero key, settlement ID, resolved direct Settlement reference and session generation. ID resolution occurs on import/assignment/session initialization, not per-party scan. Never serialize caches or reuse prior-session objects.

## 7. Bounded performance design

Decision path: current leader StringId → dictionary TryGetValue → direct settlement reference and OwnerClan validation → authoritative peace/control checks → iterate the existing candidate list once → tiny pure policy → composer.

No assignment: return before world context lookup, candidate scan or resolution. No per-frame work, all-hero scan, all-settlement scan per party, ownership enumeration or external IO. O(1) assignment/context lookup; O(C) existing candidate evaluation where C is native candidate count, not an O(1) claim for the whole AI tick.

Cache one player-faction war state using native event invalidation/direct native war relations once its API is audited. Tiny assignment reverse index by settlement ID allows ownership events to invalidate only affected heroes. No copying WorldScopeContext's global scan into the new path. UI may enumerate only current player clan party/holding lists on explicit opening.

Invalidate on change/clear; load/new campaign; settlement owner change; party creation/destruction; leader-reference change; hero death/clan change; army entry/exit or native command ownership change. Direct per-tick ownership/current-leader/control guards are correctness backstops; unverified event names are not required to guarantee immediate no-influence.

Future counters: opportunities, noAssignmentFastPath, invalidAssignment, eligiblePeaceEvaluations, candidateEvaluations, homeCandidateMissing, factorApplications, winnerChanges, commitChecks, commitMatches, commitExpired, restored/rejectedRows; optional elapsed microseconds sampled every 64 eligible evaluations. Aggregate only; no per-tick UI messages or persistent counter save keys.

## 8. One small player UI surface

Selected surface as a design: **“ClanAI: Home assignments” option in the native town/castle campaign menu, only at a player-clan-owned holding**. Assignment target is the current settlement, so no all-map destination picker.

Open a small native selector/submenu listing eligible current clan party leaders, party name and current home (or Unassigned). Select a leader, show current versus this holding, then Assign/Change here or Clear. At Castle A select brother; at Castle B select son. Clearing and seeing the existing home are available in the same flow. Revalidate ownership and actor scope at consequence execution; cancelled selections change nothing.

Grounded seam: CampaignEvents.OnSessionLaunchedEvent/CampaignGameStarter.AddGameMenuOption; retained Phase 6 E34 shows native town and castle options. Current ClanAI source has no AddGameMenu/AddPlayerLine/AddDialogLine management hook to reuse. Exact native selector widget signature and menu lifecycle must be checked in LW1-A2; no compile-ready claim for an uninspected inquiry API.

This reuses native menus, needs no large management screen, lists only clan parties on opening and uses the same single assignment store. Clear saved/restored records through that store only. No settlement stats or native governor identity changes.

One-shot notifications: “<leader> assigned home: <holding>”; “<leader> home changed: <old> → <new>”; “<leader> home assignment cleared.” No-op repeat assignment should be silent. No per-tick spam. Optional invalidation notice once per removed assignment, not required for initial v1.

## 9. Pure policy contract for later LW1-B

Proposed pipeline: persistent assignment → minimal native adapter context → pure HomeAssignmentPolicy → factors on existing indices → StrategicDecisionComposer → native selection/action → separate observation-only verifier.

Pure inputs: valid assignment bool; actor eligible bool; peaceConfirmed bool; urgentExistingResponsibility bool; supported behavior enum; candidate target ID; assigned-home ID; native raw score; current composed score. Equality is ordinal. Local/away state is omitted from v1 until native scoring evidence proves it necessary. TaleWorlds object resolution, ownership, adulthood, control state and candidate extraction stay outside policy.

Output: Apply bool, Factor float, Reason enum/string. Passthrough: false, 1, no reason. Supported reasons: assigned-home-visit and assigned-home-patrol. Missing assignment/target, invalid/unknown/war/urgent state, unrelated behavior, different target or nonfinite/non-positive native/working score all pass through.

No change to war policy. No pure-policy output contains a destination, candidate, native action or position. Observation-only pending verification should include hero ID, party object/session identity, assignment revision, expected behavior/settlement ID and bounded expiry; invalidate on change/clear/lifecycle. Compare default behavior with its corresponding target and short-term behavior with its corresponding target as pairs, avoiding cross-pair false matches. Check the final composed winner, not merely an intermediate layer winner. Do not automatically write duty memory until separately justified.

## 10. Exact future deterministic plan

| Test | Required result |
|---|---|
| 1. No assignment | Apply=false, Factor=1; adapter exits without resolver/context/global scan. |
| 2. Invalid/missing home | Passthrough and safe invalidation; late-load resolution deferred correctly. |
| 3. Foreign/lost holding | No factor even before owner-change handler runs; removal prevents recapture reactivation. |
| 4. Exact positive home candidate in peace | Only allowed configured factor/reason. |
| 5. Unrelated settlement | Factor 1; candidate identity/score unchanged. |
| 6. Zero/negative/nonfinite raw or working score | No application; no revival or NaN propagation. |
| 7. Exact settlement-valued home patrol | Bounded support; point/non-settlement target passthrough. |
| 8. Exact home GoToSettlement | Bounded visit/return support only when supplied. Missing target creates nothing. |
| 9. War/urgent siege/raid elsewhere | New policy no-op; existing 1.60/war priorities and factors unchanged. |
| 10. Bounds | Reject invalid configuration; no factor outside audited range or double contribution. |
| 11. Candidate/action preservation | Same count, order, object/target identities; no AIBehaviorData construction, SetMove or position write. |
| 12. Main/player/ineligible actors | Untouched; army, caravan, child, dead, prisoner, temporary/disabled/foreign-clan controls. |
| 13. D1 round-trip | Two distinct homes restore; sorted export; absent key empty; malformed/unknown versions rejected. |
| 14. Change/clear | One row per hero; revision changes; old pending expectation never matches. |
| 15. Multiple parties | Hero A→A, Hero B→B; no shared mutable home/target. |
| 16. Fast path | Fake resolver/scan counters remain zero without assignment; only existing C candidates touched when assigned. |
| 17. Phase 3 preservation | All ten existing HomeResponsibility policy cases plus current wiring check pass. |
| 18. Duplicate/import safety | Identical coalesce; conflicts invalidate regardless of order; invalid UTF-8/base64 and bounds fail safely. |
| 19. Lifecycle | Party destruction/recreation with reused party ID; leader switch; reload fresh references; no automatic inheritance. |
| 20. Commit verification | Final-winner pairing, expiry, assignment revision, non-match and override by later safety layer. |

Do not implement tests in this audit. Rerun appropriate pure/integration tests only in a later implementation checkpoint; no RC rebuild.

## 11. Future runtime proof — not executed

Use a separate post-RC1 candidate with rollback, existing safe native-time-pause protocol and an explicitly authorized disposable campaign fixture. Do not substitute it into frozen RC1. Preregister a 72-campaign-hour observation cap for peace/centering cases; stop on minimum evidence or bound. Save/reload case uses native pause verification before/after Save, menu-confirmed navigation and at most six guarded post-reload hours. Do not manufacture wars, defeats, deficits or rare departures.

- **A — Peace:** eligible Party A assigned Holding A. Record actual supplied home candidate presence near/far, raw native winner, factor/reason/current score, adjusted/final winner and later paired native behavior/target. A causal home commit plus movement/arrival/local activity supports centering; already-at-home stationary state alone does not prove return.
- **B — Separate homes:** Party B assigned Holding B; verify isolated assignments and native targeting/centering. Preserve non-qualifying/null records if only one party produces an opportunity.
- **C — Temporary departure:** if a natural need leads away, record native purpose and end of that condition, then resumed responsibility/return. No natural departure within bound = bounded null; do not force one or extend indefinitely.
- **D — Save/reload:** uniquely named disposable save such as LW1_HOME_A_B_<UTC>; both D1 mappings exported/restored, references resolved, no duplicates/stale influence. Follow guarded reload and record distinct responsibilities afterward.

Evidence includes party/hero/holding IDs, ownership and peace/control state, native raw winner/candidates, ClanAI contribution, adjusted and final winner, later native committed behavior/target, match/expiry, evaluation/application/missing-candidate counts, campaign hours and save/reload receipt. Score changes alone are not centering, arrival or duty success. Preserve observed overrides and nulls.

## 12. Status, preservation and next checkpoint

| Field | Status |
|---|---|
| LW1-A audit | INCOMPLETE — decisive supported-native selector unavailable |
| Bannerlord launched | NO |
| Gameplay changed | NO |
| Save schema changed | NO |
| RC1 changed | NO |
| Native peacetime home candidate availability sufficient | PARTIAL — present-candidate influence verified; near/far generation unresolved |
| Persistence contract selected | YES — design only |
| UI seam selected | YES — native town/castle surface; selector API verification pending |
| Performance design bounded | YES — design only |
| War Home Responsibility preservation plan defined | YES |
| Implementation authorized by audit | NO-GO |
| Exact next checkpoint | LW1-A2 — supported-native peacetime candidate-generation and control-seam source closure |

RC1 source checkpoint: `9ec113e738af35254e113fbde7c05babbf3c405d`.
RC archive: `Releases/ClanAI-v0.22.0-RC1.zip`.
Archive SHA-256: `A03D183934BCF1C974EA6BCEBBF35CFF3A5413080D6F6634942811AE54C971D6`.
Tested DLL SHA-256: `A18341FB2CD6B8623B45B52155C1A7D987DBEE7AAE14AE7DBF704514EC7F37B5`.
Preservation is established by unchanged Git tree/blob identities, not a new live install verification.

Only this audit, focused evidence, CODEX_STATUS and the necessary roadmap sequence correction change. No src/, package/, Releases/, save key, DLL, gameplay/policy, build or deployment changes. No README edit is needed.

**LW1-A2 scope:** obtain read-only supported binary/full decompiler source; verify hashes; inspect complete AiVisitSettlementBehavior, patrol/defend candidate producers, PartyThinkParams and native selection/commit path, player-clan control exclusions, Hero/Settlement resolution/save identity, menu selector signature. Preserve targeted excerpts and formulas, answer A/B exactly, and select at most one exact native-compatible candidate exposure/retention method if required. Do not implement home assignments or score policy in A2. Current source alone proves candidate-preserving composition is unable to express an absent target; it does not prove that the native generator always omits it or that a future lawful seam is impossible.

The following conclusion is an implementation clearance decision under that missing evidence, not a claim that Bannerlord can never support home assignment. No speculative native seam is authorized. Stop after this documentation checkpoint's commit/main verification.

NO-GO — current architecture cannot satisfy the mission without violating native-authority rules.
