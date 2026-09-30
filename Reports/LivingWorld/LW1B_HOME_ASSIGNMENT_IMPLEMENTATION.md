# LW1-B — Persistent Home Assignment implementation

**COMPLETE — IMPLEMENTED + TESTED + RELEASE BUILD READY FOR DEPLOYMENT.**

Source: GitHub main `b026973c62141d860fe82000199cd7c5839b07aa`. Completed 2026-09-30 UTC. Research remained closed; implementation used the accepted A2R seam and retained native evidence. No internet search, Bannerlord launch, live deployment or LW2 work occurred.

## Implemented vertical slice

At an owned town or castle, **Manage Home Assignments** lists eligible clan parties with leader and current home. Select a leader, then **Assign current holding** or **Clear assigned home**. Assign replaces the prior home atomically. Cancel changes nothing. Empty rosters show a non-error message. Ownership and eligibility are rechecked at confirmation.

Assignments apply to active player-clan mobile lord parties led by an adult, alive clan lord/family member or player companion. Main/player parties, caravans, armies, sea navigation, disabled/decision-stopped AI, disbanding/waiting-for-disband, attached, siege/map-event, inactive, prisoner/child or inconsistent leader/party states fail closed. The native WarPartyComponents roster is enumerated only when the player opens the menu.

Messages occur only for actual set/change/clear:
- “Leader is now responsible for Settlement.”
- “Leader’s home responsibility has changed from Old to New.”
- “Leader no longer has an assigned home.”

No optional per-tick/resumption notification was added. This is Home Assignment only: no party limits, household jobs, governor logistics or other LW feature.

## Architecture

`HomeAssignmentRecords` stores durable IDs; `HomeAssignmentStore` caches native references; `HomeAssignmentCampaignBehavior` owns the new save key/session/menu; `HomeAssignmentVisitPatch` exposes the exact legal home to native visit scoring; `HomeAssignmentPolicy` supplies a pure bounded decision; `HomeAssignmentLayer` contributes through the existing composer and observes later native commits.

The layer runs immediately before each existing composer completion path, after existing contributions/safety layers. Only existing candidate indices are factored. Composer code, candidate count and target identities remain unchanged. Pending observations therefore describe the final composed winner, rather than a winner subsequently replaced by another layer.

## Persistence contract

New isolated key: **ClanAI_HomeAssignment_v1**. Native IDataStore serializes `List<string>`; no custom Saveable type/definer or existing key/schema was changed.

Row: `D1|base64(UTF8 Hero.StringId)|base64(UTF8 Settlement.StringId)`.

One hero has at most one home; villages are excluded. Export sorts hero IDs ordinally. Same assignment is a no-op; change/clear increments revision. Identical duplicate rows coalesce. Conflicting duplicate homes invalidate that hero regardless of import order. Malformed/version-invalid/non-base64/non-UTF8/empty/oversized rows are rejected. Import is bounded to 256 rows; an oversized payload fails closed.

On session restoration, MBObjectManager resolves IDs once, outside AI ticks. Missing/dead/foreign/ineligible hero identities and missing/foreign/non-town/castle settlements are discarded. Native references and pending observations reset across load/new session. A hero temporarily without an eligible party keeps a dormant assignment, but receives no influence. Party recreation follows the same hero identity; a new leader follows its own home.

Every hot lookup checks current ownership and native hero identity. Ownership loss immediately clears influence and the stale assignment; reacquisition does not silently restore it. Daily cleanup visits only cached assignments/pending observations, never all heroes/settlements. Direct runtime checks provide immediate invalidation between cleanup ticks.

Actual campaign save/reload restoration is **not yet runtime-tested**; deterministic D1/session/identity fixtures passed.

## Exact native visit seam

Only Harmony target:

`TaleWorlds.CampaignSystem.CampaignBehaviors.AiBehaviors.AiVisitSettlementBehavior.FillSettlementsToVisitWithDistancesAsDays`.

A narrow **postfix** leaves original native collection construction intact. If the exact assigned settlement is already present, it does nothing. Otherwise, an eligible peaceful party without current urgent home/defense responsibility calls the supported native private:
- `IsSettlementSuitableForVisitingCondition`;
- `GetBestNavigationDataForVisitingSettlement`.

Only a suitable exact owned home with finite nonnegative distance and native land navigation can append one native `SettlementNavigationData` row. The row uses the real settlement, native distance/port flags/navigation and native `GetHashCode()` identifier. Reflection metadata/constructor are cached at installation; unsupported signatures fail explicitly. Native helper invocation failure adds no row.

This is a native navigation-list entry, **not AIBehaviorData or a manufactured target/action**. It does not alter native visit score calculation, distance-sort/good-enough early exit, PartyThinkParams production, winner selection, SetPartyAiAction or movement. It contains no move command, TargetSettlement write, position write or teleport.

Supported assemblies used:
- Bannerlord v1.5.3, engine 122374, Steam buildid 25302170.
- TaleWorlds.CampaignSystem.dll: `5B23C3E36D7A5D6D47C47EAB075E9E2B1CC8D1AA53C79E135FA2B5EF434EBC5F`.
- SandBox.dll: `16AF436C569675EB30E22514BB755E6FB3612AFCE38F6CC55D1748D079C19C1A`.

Offline metadata checking confirmed the exact selector/helper/navigation-row signatures. One compile correction imported `CampaignBehaviors.IDisbandPartyCampaignBehavior`; no seam decision changed.

## Policy and war boundary

Pure TaleWorlds-free `HomeAssignmentPolicy` outputs apply/factor/reason. Exact assigned-home native **GoToSettlement** or settlement-valued **PatrolAroundPoint** gets **1.25** only during applicable peace. Nonpositive/nonfinite raw or working scores, overflow risk, unrelated targets, invalid assignment, ineligible actor, other behavior, war or urgency return exact factor 1.00.

Native candidates to other destinations are neither suppressed nor replaced. Stronger recruiting, recovery, food, prisoner handling, local security and other legal purposes can still win.

Peace uses the native faction war list, excluding bandit/outlaw factions rather than treating permanent bandit hostility as kingdom war. Urgent committed native clan-defense, home siege/raid, or an existing positive native DefendSettlement candidate for a clan holding disables the ordinary factor. The seam also abstains during current urgent home/committed-defense responsibility. No additional global threat search was added.

Existing `EligibleIndependentLordAtWar`, HomeResponsibilityPolicy/Layer, WorldScopeContext and StrategicDecisionComposer are byte-identical. War assignment gravity is deliberately dormant; established siege/raid/recovery/patrol/other-clan-holding responsibility remains authoritative. No war strategy was redesigned.

## Observation and performance

Pending verification exists only when the Home Assignment contribution changes the final composed winner to the exact home. Records include party object, leader object, assignment revision, home, native/pre-contribution/adjusted winner, expected behavior/target and campaign hour. Logs include raw/composed score, factor/reason and counters.

On a later normal AI tick, observation checks the **default behavior with default target** or **short-term behavior with short-term target**. Crossed pairs cannot falsely match. Revision/leader/eligibility/ownership changes or an 18-hour expiry invalidate the observation. Daily bounded pruning handles parties no longer ticking. Pending count is capped at the 256-assignment bound. Verification mutates no movement or social duty memory.

O(1) leader-ID record/cache lookup precedes eligibility/context work. No assignment means no object resolution/global scan. Each assigned visit opportunity performs at most one extra exact native suitability/navigation evaluation; composer work is bounded by its existing candidate list. No per-frame scan or external IO is required.

In-memory counters: lookups, no-assignment fast path, valid evaluations, exact-home candidate evaluations, visit-seam evaluations/applications, composer factor applications, winner changes, commit checks/matches/mismatches/expiry. Sparse evaluation/winner/commit/restore evidence uses the existing explicit Evidence profile only; release behavior works without telemetry.

## Validation

- Pure policy/persistence: **43 PASS**.
- Runtime adapter fixtures compiling the actual Store/VisitPatch/Layer against small native-boundary doubles: **29 PASS**.
- Existing Phase 3 Home Responsibility, isolated intermediate/output directories: **10 PASS**.
- Home Assignment native-authority/UI/save/cache/composer/verifier invariants: **PASS**.
- Supported native signature metadata: **PASS**.
- All six Release Python invariant scripts: **PASS**.
- Existing 12 save keys and protected package/archive bytes: **PASS**.
- Release build: **0 errors**, one MSB3277 System.ValueTuple conflict warning from existing Harmony/netstandard references. No suppression or runtime-dependency change was introduced.

Coverage includes no/invalid assignment, foreign/missing home, fixed visit/patrol factor, nonpositive/nonfinite score, unrelated target, urgency/war, factor bound, no synthetic candidate/target/movement, main/army/caravan/disabled/stopped/disbanding/naval/child exclusion, D1 round-trip/duplicate/malformed/change/clear/distinct homes, dormant/recreated leader identity, no-assignment no-resolution path, exact-home-only visit exposure, native suitability/navigation refusals, fixed count/identity, and correctly paired commit observations.

The boundary fixtures prove adapter logic; they do not claim real navigation/scoring, Harmony installation inside a campaign, menu rendering, natural departures or native movement commitment. Those belong to LW1-C.

Release tests retain their baseline dependency/policy/default checks while allowing exactly the additive new save key and separate dev source identity. The historical I3 text manifest predates RC1 (README/XML sizes 997/1175 versus frozen 999/1177). The package test now accepts only exact bytes from the known-hash frozen RC1 archive when that historical manifest differs; historical evidence and frozen payload are untouched.

A wider optional regression sweep also encountered **pre-existing, out-of-scope** limitations: malformed Phase6 project XML, a stale Phase7 observer bridge hash, and Phase5PatchResolution native dependency loading. These files were not changed. Two delegation suites initially needed BANNERLORD_GAME_DIR; rerun passed 13/23 checks. Phase3 shared project output collisions were avoided by isolated output directories. All LW1-B/Phase3/Release acceptance checks above pass; the wider suite is not claimed wholly green.

Focused output: [build/test evidence](evidence/lw1b_home_assignment_build_tests_20260930.txt).

## Development candidate and RC1 preservation

Dev identity: **v0.23.0-LW1B-dev**, assembly/file **0.23.0.0**.

Built module:
`D:\BannerlordAIResearch\Builds\LW1B_b026973\source\y58455292-debug-bannerlordai-b026973\src\ClanAI\src\ClanAI\bin\Release\netstandard2.0\ClanAI.dll`.

Durable separate candidate: [DevBuilds/ClanAI-v0.23.0-LW1B-dev/ClanAI.dll](../../DevBuilds/ClanAI-v0.23.0-LW1B-dev/ClanAI.dll).

DLL SHA-256: **EEE566615AA72BFEB01C95D05AE8FCF022271F90A2519319D95AF3C90D6A4622**.

Only the dev DLL is supplied; no new release packaging or bundled Harmony was added. It is separate from the frozen package and is ready for the next authorized deployment checkpoint.

Frozen baseline: `9ec113e738af35254e113fbde7c05babbf3c405d`.
- RC1 archive `Releases/ClanAI-v0.22.0-RC1.zip`: **A03D183934BCF1C974EA6BCEBBF35CFF3A5413080D6F6634942811AE54C971D6**.
- Frozen packaged/tested DLL: **A18341FB2CD6B8623B45B52155C1A7D987DBEE7AAE14AE7DBF704514EC7F37B5**.
- All three package files and RC archive compare byte-for-byte to authoritative source archive; no protected file is included in this commit.

## Files changed

- `src/ClanAI/src/ClanAI/HomeAssignmentPolicy.cs`
- `src/ClanAI/src/ClanAI/HomeAssignmentRecords.cs`
- `src/ClanAI/src/ClanAI/HomeAssignmentStore.cs`
- `src/ClanAI/src/ClanAI/HomeAssignmentCampaignBehavior.cs`
- `src/ClanAI/src/ClanAI/HomeAssignmentVisitPatch.cs`
- `src/ClanAI/src/ClanAI/HomeAssignmentLayer.cs`
- `src/ClanAI/src/ClanAI/ClanAIStrategicBehavior.cs`
- `src/ClanAI/src/ClanAI/SubModule.cs`
- `src/ClanAI/src/ClanAI/ClanAI.csproj`
- `src/ClanAI/src/ClanAI/ReleaseIdentity.cs`
- `Tests/LivingWorld/HomeAssignment/HomeAssignmentTests.csproj`
- `Tests/LivingWorld/HomeAssignment/Program.cs`
- `Tests/LivingWorld/HomeAssignmentRuntime/HomeAssignmentRuntimeTests.csproj`
- `Tests/LivingWorld/HomeAssignmentRuntime/NativeFixtures.cs`
- `Tests/LivingWorld/HomeAssignmentRuntime/Program.cs`
- `Tests/LivingWorld/test_home_assignment_invariants.py`
- `Tests/LivingWorld/test_supported_native_visit_signature.ps1`
- `Tests/Release/test_phase8b_i2_dependency_version_invariants.py`
- `Tests/LivingWorld/verify_home_assignment_checkpoint.py`
- `Tests/Release/test_phase8b_i3_package_invariants.py`
- `Tests/Release/test_phase8b_standalone_profile_invariants.py`
- `DevBuilds/ClanAI-v0.23.0-LW1B-dev/ClanAI.dll`.
- This implementation report and focused evidence.
- `Reports/AgentStatus/CODEX_STATUS.md`, `ROADMAP.md`, `README.md`.

## Checkpoint status

| Field | Result |
|---|---|
| LW1-B | COMPLETE |
| Gameplay implementation changed | YES — Home Assignment vertical slice only |
| Existing save keys/schema changed | NO |
| New isolated Home Assignment key/schema added | YES |
| RC1 changed | NO |
| Bannerlord launched | NO |
| Deployed | NO |
| Runtime status | NOT YET TESTED |
| Release build | 0 errors |
| Next | LW1-C — deploy the LW1-B dev candidate and run the bounded playable Home Assignment demo/runtime proof |

STOP after GitHub checkpoint. No runtime proof or LW2 work is part of LW1-B.
