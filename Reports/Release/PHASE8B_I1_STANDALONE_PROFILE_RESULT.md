# Phase 8B-I1 — Standalone Path and Default Release-Profile Result

Date: 2026-09-25

Parent checkpoint: `32afeaf77ee8fad8bbc654edd2b95b345f7325b4`

Scope: offline implementation and deterministic validation only. Bannerlord was not launched, no DLL was deployed, no runtime stability test was performed, no gameplay policy/threshold was changed, and no save schema was changed.

## Result

Phase 8B-I1 is **COMPLETE**.

One shared `ModuleRuntimePaths` resolver now derives the installed ClanAI module root from the production assembly layout (`ClanAI/bin/<platform>/ClanAI.dll`). It accepts only non-rooted child segments, rejects parent traversal, and returns null for an invalid module layout. The seven active development-machine path families now resolve under the installed module:

- `Data/CLANAI_OFF.txt`;
- `Logs/clanai.log` and `Logs/Sessions/`;
- `Logs/episodes.log`;
- `Data/DynastyMindSeed.cfg`;
- `Data/DynastyMindCausal.cfg`;
- `Data/SocialMemoryCausal.cfg`;
- `Data/WeakBanditRecovery.cfg`.

Missing or unresolvable paths preserve the prior conservative behavior: ClanAI remains enabled without an off marker, Dynasty/Social causal modes remain Observe, the recovery gate remains Suppress, and missing seed input yields no external seed rows.

One `RuntimeProfile.cfg` gate now defaults to `Profile=Release` semantics whenever the file is absent, unreadable, missing a valid profile, or contains an invalid value. Only an explicit module-local `Data/RuntimeProfile.cfg` containing `Profile=Evidence` enables research instrumentation.

The Release default keeps the existing gameplay models, patches, and behaviors registered. It excludes:

- `GenerationalContinuityPreflightBehavior`;
- `Phase4ARecoveryObserverBehavior`;
- `DynastyStructuralRuntimeObserver` and its application-tick flush;
- `LocalManpowerRuntimeTelemetry`;
- `CivicProjectRuntimeTelemetry`;
- `LocalBanditControlRuntimeTelemetry`;
- general external evidence logs, session logs, episode audit logs, and diagnostic snapshots.

The Local Manpower, Civic Project, and Local Bandit Control gameplay wrappers/patches remain installed; only their proof hooks are gated. All twelve existing save keys are unchanged.

## Validation

Focused deterministic validation passed:

- module-root/data/log resolution, traversal rejection, invalid-layout rejection;
- missing/invalid profile -> Release, explicit Evidence opt-in;
- zero active production `D:\BannerlordAIResearch` references;
- no production network/API or development-tool runtime dependency;
- Release-default observer/telemetry exclusion;
- gameplay-behavior registration and save-key preservation;
- Phase 3 Home Responsibility, Kingdom Objective, Visual War, and Strategic Commitment wiring;
- Phase 4B/4C policy, delegation, and no-mutation invariants;
- Phase 5 policy/result-only/no-mutation invariants;
- Phase 6 policy/wiring/no-mutation invariants;
- Phase 7 actor/branch-history, D1/D2, structural-writer, and no-mutation invariants;
- Phase 7B observation-only/no-save-schema/no-lifecycle-mutation invariants.

Release build succeeded with **0 errors** and the inherited `System.ValueTuple` warning.

Exact validation record: `Reports/Release/evidence/phase8b_i1_standalone_profile_validation_20260925.txt`.

## Status

- Development-machine runtime paths removed: **YES**
- Default release profile added: **YES**
- Experiment/proof telemetry default-off: **YES**
- Gameplay policy changed: **NO**
- Save schema changed: **NO**
- Bannerlord launched: **NO**
- Runtime tested: **NO**

Harmony dependency cleanup, NavalDLC dependency cleanup, version reconciliation, and package assembly were deliberately not included.

Next milestone: one separate offline **Phase 8B-I2 dependency/version contract checkpoint** to resolve the declared Harmony/NavalDLC requirements and version identity before package assembly.

