# BannerlordAI

BannerlordAI is an experimental Mount & Blade II: Bannerlord mod project focused on autonomous world-scope clan and lord behavior while keeping Bannerlord's native eligibility, action, and campaign systems authoritative.

The active mod source is `src/ClanAI`. `src/BannerlordInspector` is the observation harness. `src/ProviderPipeline` contains the retained provider-pipeline milestone work, but provider reasoning is not currently part of the proven clan-loyalty behavior described below.

## Current state

The current ClanAI source builds on the Phase 2B candidate `v0.22A-ruler-courtship-native-v1` and now also includes the Phase 2D-L1 kingdom-continuity ledger. Phase 2B ruler recruitment and Phase 2C independent-clan activity are runtime-proven. Phase 2D-L1 is implemented, build-proven, deployed with rollback, and runtime-proven for its guarded save/load continuity checkpoint. The uniquely named continuity save (`ClanAI V020V PERSIST PHASE2D L1 CONTINUITY 20260925 0130`) materialized successfully and, on a fresh direct reload, restored seven serialized records (`found=True`), reconciled seven active kingdoms into seven records, emitted no succession/destruction callback or duplicate notice, and reported `mutation=False`. Exact reload evidence is under `Reports/KingdomContinuity/evidence/phase2d_l1_reload_20260925.txt`. Natural kingdom lifecycle-event observation remains an honest null and a separate broader Phase 2D follow-up. Phase 3 has now completed its first offline Home Responsibility deterministic seam: factor/eligibility logic is isolated in a pure policy, ten deterministic cases pass, runtime wiring invariants pass, and the Release build remains at 0 errors. No DLL was deployed and no new campaign behavior is claimed by that checkpoint. The next bounded Phase 3 seam is also complete for the existing Kingdom Objective layer: readiness/supply and urgent-home-threat refusals, direct/staging factor selection, competitive-ratio/winner-margin logic, factor bounds, and pending-winner conditions are isolated in `KingdomObjectivePolicy`; 34 deterministic cases and a wiring invariant pass, with Release still at 0 errors. No DLL was deployed and the next milestone is a single bounded runtime proof of a natural ruler objective changing and then committing a native behavior/target. The separate target-kingdom defection boundary remains timeboxed and unproven.

The Phase 1 demo-first integration gate passed on 2026-09-24. The integrated candidate loaded the protected Syronea baseline, advanced 100.987 campaign hours without a crash, emitted proven player-visible state, created and reloaded a separate guarded demo-gate save, restored persisted ClanAI systems, and advanced again after reload. The playable validation fixture is `ClanAI V020V PERSIST DEMO GATE V021M 20260924` (SHA-256 `A91F15BF1403F1D29F942C56A1162F431113942CDACCAFE52F4D80303B4CB427`). See `Reports/Demo/PHASE1_DEMO_GATE_RESULT.md`.

A real causal chain has been demonstrated:

1. an AI clan experiences a real campaign event;
2. ClanAI records bounded memory from that event;
3. Bannerlord later reaches its own autonomous diplomacy consideration;
4. ClanAI biases only the clan-side native barter value;
5. Bannerlord keeps authority over the >0 decision threshold and the actual `ChangeKingdomAction`.

Phase 1 now proves the voluntary leave-current-kingdom boundary: a natural holding loss can push a native negative leave value above zero and Bannerlord's own leave path can commit the departure. The separate target-kingdom switch/join boundary remains a different surface.
### Rhemtoil / L5 result

fen Morcar lost Rhemtoil Castle in a real siege. 317.338 campaign hours later Bannerlord naturally evaluated whether the clan should leave Battania. The recorded exact-clan holding loss moved the native leave value from -3,955,367 to -3,955,259: a +108 adjustment.

The decision remained no. No faction transfer was forced.

This run demonstrated real-event -> memory -> native loyalty-score causality, but also exposed the scale mismatch in the first direct-loss implementation.

### Nevyansk / L6 result

L6 values a lost clan-owned fief using Bannerlord's native settlement-value model rather than a flat castle/town weight.

fen Morcar later lost Nevyansk Castle in a real siege. Bannerlord valued that holding at 532,054.6 native settlement-value units. L6 recorded 133,013.7 pressure at loss. The record persisted through save/load (`records=1` before and after reload).

At a natural loyalty check 50.017 campaign hours after the loss:

- native leave value: -2,298,743
- decayed direct-loss pressure: 123,773.5
- applied direct-loss modifier: +123,773
- adjusted leave value: -2,174,970
- nativeWouldLeave: false
- adjustedWouldLeave: false
- committed: false
An earlier natural sample of 60 leave considerations produced no native leave decisions, no adjusted leave decisions, and no commits. The least-loyal sampled clan was still about 191,000 points below the >0 leave boundary.

### Phase 1 boundary proof / Banu Ruwaid

On 2026-09-24, Banu Ruwaid naturally lost Vladiv Castle by siege. Bannerlord valued the lost holding at 1,801,322 native settlement-value units. At a later natural leave check while the clan was in Aserai, its native leave value was -250,332. The decayed direct-loss memory was capped to +312,915, producing an adjusted value of +62,583. The log recorded `nativeWouldLeave=False`, `adjustedWouldLeave=True`, and `committed=True`; read-only Inspector verification then showed the clan independent.

This satisfies the preregistered Phase 1 win condition for the **voluntary leave-current-kingdom** path. It does not prove the separate target-kingdom defection/join bias can cross its own boundary.

### Phase 2A target-kingdom defection

Phase 2A was timeboxed after the initial scale-mismatch evidence. Native-score inspection showed that Bannerlord's clan-side defection value already contains the current-kingdom leave score, so M3 made one evidence-backed implementation attempt: target-specific social memory remains separately capped at 25,000, while the already-proven voluntary-leave memory modifier is carried only into Bannerlord's embedded leave component.

The bounded M3 proof advanced 486.070 campaign hours and produced 27 natural `ConsiderDefection` samples:

- target-specific modifier non-zero: 4;
- leave-memory carry non-zero: 0;
- adjusted YES: 0;
- committed target switch: 0.

The closest candidate was Vezhoving → Nord at -180,954 with affordability satisfied. No sampled clan had an active non-zero leave-memory modifier during the M3 window, so the new carry path was not naturally exercised.

**Target-kingdom autonomous defection remains unproven.** The project will not keep instrumenting or tuning this surface in an open-ended loop. Phase 2A is preserved as a timeboxed blocker/null and the roadmap moves to **Phase 2B — ruler recruitment / clan courtship**.

See `Reports/ClanLoyalty/PHASE2_DEFECTION_BOUNDARY_PROTOCOL.md`, `PHASE2_DEFECTION_IMPLEMENTATION_DECISION.md`, and `PHASE2_DEFECTION_M3_ATTEMPT_RESULT.md`.

### Phase 2B ruler recruitment / clan courtship

The first Phase 2B candidate is now implemented and build-proven. On each NPC ruling clan's daily tick it mirrors the relevant native eligibility and safety gates, selects the eligible independent clan with the highest native `GetScoreOfKingdomToGetClan`, evaluates both sides of the ordinary non-defecting `JoinKingdomAsClanBarterable`, and calls only `BarterManager.ExecuteAiBarter` when the native combined surplus is positive. It adds no custom score or direct faction transfer.

Runtime validation passed the preregistered strong win condition. Vlandia selected naturally independent Banu Ruwaid; native values were clan +649 and kingdom +7,401 for a combined +8,050; ClanAI invoked only the native AI barter; and post-state confirmed Banu Ruwaid joined Vlandia. See `Reports/ClanRecruitment/PHASE2B_RUNTIME_RESULT.md`.

## In-game visibility

`v0.21M-player-visibility-v1` adds native Bannerlord message-feed visibility for major clan loyalty shocks, committed voluntary departures, committed target-kingdom defections, and the player kingdom's war-strain state.

Runtime proof now covers the player kingdom's war-strain message, natural clan-owned fief-loss messages, a fresh loyalty-shift message, and a native committed voluntary-leave message. In the strongest fresh example, Gauting naturally lost Skarthness Castle, memory moved its leave value from -401,733 to +100,433, Bannerlord committed the departure from Nord, and the game emitted `ClanAI: Gauting has left Nord after mounting losses and grievances.`

A fresh natural target-kingdom defection commit has **not** yet been observed under the visibility build, so that notification remains implemented/build-proven rather than runtime-proven.

See `Reports/Visibility/2026-09-24-player-visibility-v1.md` and `Reports/Visibility/2026-09-24-loyalty-runtime-proof.md`.

## Loyalty memory and clamps

Ordinary current-kingdom social memory uses:

`social_index = grievance*0.50 + bloodDebt*0.70 + tension*0.25 - trust*0.35 - obligation*0.45`

When both sources exist, leader-direct memory and clan aggregate kingdom memory are blended 65% / 35%. The index is clamped to [-100, 100].

`social_cap = min(25000, max(5000, abs(native_leave_value) * 0.25))`

`social_modifier = social_cap * social_index / 100`

Direct clan-owned fief losses use Bannerlord's native settlement valuation:

`raw_loss_pressure = native_settlement_value * 0.25`

Each loss decays linearly over 720 campaign hours (30 days). Multiple direct losses accumulate for the same clan, with aggregate pressure capped at 750,000.
At the leave-kingdom hook:

`direct_loss_cap = min(750000, max(75000, abs(native_leave_value) * 1.25))`

`direct_loss_modifier = min(decayed_direct_loss_pressure, direct_loss_cap)`

`adjusted_leave_value = native_leave_value + social_modifier + direct_loss_modifier`

The leave patch is context-gated to Bannerlord's own `DiplomaticBartersBehavior.ConsiderClanLeaveKingdom` path and patches the clan-side `LeaveKingdomAsClanBarterable.GetUnitValueForFaction` result. The kingdom-switch patch is similarly scoped to `ConsiderDefection` / `JoinKingdomAsClanBarterable`. These patches do not directly call a faction-transfer action.

## Other retained ClanAI systems

The current source also contains the previously developed Home Responsibility, companion duty/experience/negative-outcome memory, Holdback, world-scope lord behavior, WarState/WarScar tracking, Kingdom Orders, War Strain recruitment/economy coupling, and Prisoner/Mercy behavior. Prisoner/Mercy remains at release threshold 20.

Not all retained experimental layers are release-ready. In particular, external/provider reasoning is not integrated into the proven loyalty path, and a natural completed `SUSTAINED_RAIDING` WarScar proof remains separate outstanding evidence.

## Repository layout

- `src/ClanAI/` — current mod source and package metadata.
- `src/BannerlordInspector/` — current observation harness.
- `src/ProviderPipeline/` — retained provider-pipeline milestone source/tests.
- `Reports/` — consolidated experiment evidence, including null results.
- `Tests/` — retained test entry points.
- `Data/` — small runtime configuration files still referenced by current ClanAI source.
The old Front Office/controller, handoff, historical dump, longitudinal dump, video-update, and abandoned research trees are intentionally not part of the active repository. The pre-cleanup tracked state is preserved on branch `archive/pre-cleanup-2026-09-23`.

## Reviewer entry points

For the current loyalty slice, start with:

- `src/ClanAI/src/ClanAI/SocialLoyaltyPatch.cs`
- `src/ClanAI/src/ClanAI/SocialLoyaltyClanLossMemory.cs`
- `src/ClanAI/src/ClanAI/SocialDefectionPatch.cs`
- `src/ClanAI/src/ClanAI/SocialLedger.cs`
- `src/ClanAI/src/ClanAI/ClanAIStrategicBehavior.cs`
- `Reports/ClanLoyalty/PHASE1_BOUNDARY_RESULT.md`
- `Reports/ClanLoyalty/2026-09-23-clan-loyalty.md`

Machine-readable supporting evidence is under `Reports/ClanLoyalty/evidence/`.

## Build

From the repository root:

`dotnet build src/ClanAI/src/ClanAI/ClanAI.csproj -c Release`

The current local build has zero compilation errors. The Bannerlord/Harmony dependency set still emits the inherited `System.ValueTuple` version-conflict warning.

## Working rules

Working code must be committed at least daily in small focused commits with descriptive messages. Experimental claims, including null results, must have matching source/evidence artifacts in the repository before they are treated as project progress. Snapshot mega-commits and chat-only progress are not acceptable project state.

Do not interrupt an active experiment or implementation merely to check for repository updates. At a natural mission checkpoint—completed milestone, committed null result, or committed blocker—sync current `main`, re-read the project law/README/current roadmap guidance, reconcile newer commits or user direction, and only then begin the next major milestone. GitHub is authoritative at mission boundaries.
