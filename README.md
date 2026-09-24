# BannerlordAI

BannerlordAI is an experimental Mount & Blade II: Bannerlord mod project focused on autonomous world-scope clan and lord behavior while keeping Bannerlord's native eligibility, action, and campaign systems authoritative.

The active mod source is `src/ClanAI`. `src/BannerlordInspector` is the observation harness. `src/ProviderPipeline` contains the retained provider-pipeline milestone work, but provider reasoning is not currently part of the proven clan-loyalty behavior described below.

## Current state

The current ClanAI source corresponds to `v0.21M-player-visibility-v1`. It builds successfully against the local Bannerlord assemblies. The proven Social 5B work concerns autonomous clan loyalty and voluntary kingdom leaving; the separate target-kingdom switch boundary remains unproven.

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

### Phase 2 target-kingdom defection characterization

A post-preregistration v0.21M run collected 15 natural `ConsiderDefection` samples: one memory-modified case, zero adjusted YES decisions, and zero commits. The closest natural candidate was Vezhoving → Nord at adjusted sum -169,423 with affordability already satisfied. The current social-defection absolute cap is 25,000, so even a theoretical maximum positive modifier would not have flipped that observed case. Memory/target alignment was also sparse: only one of the 15 sampled target choices had a non-zero memory modifier.

This is evidence of an observed scale mismatch, not proof that a future naturally closer candidate cannot occur. Target-kingdom autonomous defection remains **unproven**, and the cap has not been increased.

See `Reports/ClanLoyalty/PHASE2_DEFECTION_BOUNDARY_PROTOCOL.md`, `Reports/ClanLoyalty/PHASE2_DEFECTION_INITIAL_RESULT.md`, and `Reports/ClanLoyalty/PHASE2_DEFECTION_SCALE_NOTE.md`.

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
