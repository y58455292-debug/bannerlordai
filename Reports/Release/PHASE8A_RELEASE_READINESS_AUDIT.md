# Phase 8A — Release-Readiness / Standalone Audit

Date: 2026-09-25

Authoritative checkpoint: `542d3529a12fde2a7a5920b8ee027e25fb8c5db0`

Scope: offline source, configuration, save-contract, package, and accepted-evidence audit only. Bannerlord was not launched, no DLL was deployed, no runtime stability test was performed, and no gameplay source or balance was changed.

## Executive finding

Current source has no production network/API client and no runtime integration with ChatGPT, Codex, Desktop Commander, watchdogs, command buses, or external services. `Automation/TestRunner`, reports, tests, and research evidence are repository-only and need not ship.

Current main is nevertheless **not yet a standalone release candidate**. Production code still reads and writes several development-machine absolute paths, three explicitly experiment-only campaign behaviors and multiple runtime telemetry patches are installed unconditionally, Harmony is a real runtime dependency but is absent from module metadata and the package, and the committed package contains only `SubModule.xml` rather than an installable module. These are release-engineering blockers, not reasons to reopen gameplay balance or the accepted Phase 6/7 natural-event nulls.

## 1. Runtime independence

### Clear findings

- No production source reference to ChatGPT, Codex, Desktop Commander, network/API access, watchdogs, or a command bus was found.
- The sole production `TestRunner` mention is a comment in `ClanAIObserverSafetyBehavior`; no TestRunner assembly, file, or command interface is called.
- Production behavior does not require the research repository's reports or evidence to deserialize a save or make a game decision.

### Blocking findings

- `ClanAI.dll` directly uses `HarmonyLib` in gameplay and telemetry patches. `ClanAI.csproj` resolves `0Harmony.dll` from a local `Bannerlord.Harmony` installation, but `SubModule.xml` does not declare `Bannerlord.Harmony` and the package does not include `0Harmony.dll`. The release must choose and document one supported dependency model: declare the normal Bannerlord.Harmony module dependency, or legally bundle and load a compatible Harmony assembly. The present package does neither.
- Absolute development paths are active runtime inputs/outputs, detailed below. Although most failures are caught, silently losing configuration/diagnostics or always taking a development fallback is not standalone behavior.

## 2. Paths and configuration

### Active absolute paths

| Production component | Current path | Release impact |
|---|---|---|
| `ClanAISwitch` | `D:\BannerlordAIResearch\Data\CLANAI_OFF.txt` | Player cannot use the intended off marker outside the development machine; code polls the stale path every two seconds. |
| `ClanAIPostVanilla` | `D:\BannerlordAIResearch\Telemetry\ClanAI\logs\clanai.log` and `...\sessions` | All high-volume diagnostics target a developer drive. Failures are swallowed, but every call still enters the writer/fallback path. |
| `SocialEpisodeMemory` | `D:\BannerlordAIResearch\Telemetry\ClanAI\episodes.log` | Episode audit output is tied to the developer drive. |
| `DynastyMindSeed` | `D:\BannerlordAIResearch\Data\DynastyMindSeed.cfg` | Optional seed input is unavailable to an installed copy and can make behavior differ from the research setup. |
| `DynastyMindCausalConfig` | `D:\BannerlordAIResearch\Data\DynastyMindCausal.cfg` | Missing path defaults to `Observe`; the configured mode cannot travel with the module. |
| `SocialMemoryCausalConfig` | `D:\BannerlordAIResearch\Data\SocialMemoryCausal.cfg` | Missing path defaults to `Observe`; the configured mode cannot travel with the module. |
| `RecoveryGateConfig` | `D:\BannerlordAIResearch\Data\WeakBanditRecovery.cfg` | Missing path defaults to `Suppress`; this is a gameplay-affecting fallback currently determined by absence of a developer file. |

`StrategicCommitmentConfig` and the Visual War marker already resolve under the installed module's `Data` directory. Their missing defaults are `Observe` and OFF respectively, and no enabling file is present in the package. This path pattern is suitable for a release profile, but the settings and defaults still require documentation and an explicit package manifest.

The absolute `C:\Program Files (x86)\...` references in `ClanAI.csproj` are build-machine paths, not runtime paths. They prevent portable/reproducible builds but do not by themselves prevent an already-built mod from running. They are release-pipeline cleanup, secondary to removing active `D:` runtime paths.

## 3. Observation and experiment scaffolding

The following experiment-only behaviors are registered unconditionally in `SubModule.InitializeGameStarter`:

- `GenerationalContinuityPreflightBehavior` — explicitly marked Phase 7B runtime-preflight only; subscribes to lifecycle, succession, save, and load events and emits snapshots.
- `Phase4ARecoveryObserverBehavior` — maintains session tracking dictionaries and subscribes to party creation/destruction, map events, hourly party ticks, and settlement entry/exit solely for recovery evidence.
- `DynastyStructuralRuntimeObserver` — explicitly a read-only evidence adapter for Phase 7C-I3; uses reflection and emits structural snapshots around ruling-clan, save, and load events.

`LocalManpowerRuntimeTelemetry` and `CivicProjectRuntimeTelemetry` are also installed unconditionally and add Harmony instrumentation used to prove Phase 4B/4C and Phase 6 runtime behavior. Local bandit-control telemetry and the broad `ClanAIPostVanilla.WriteExternalLog` call surface generate additional research logging throughout normal play.

None of these observers adds save schema or intentionally mutates politics, but their event traffic, reflection, allocation, Harmony interception, background file writer, and high-volume logs are unnecessary player-release overhead. They should be absent from the default release profile, while evidence-critical gameplay logic and player-facing messages remain intact. Removal/disablement must be guarded by deterministic wiring tests because some files named “telemetry” sit beside real policy wrappers.

## 4. Save compatibility

### Persisted keys on current main

| Save key | Current contract |
|---|---|
| `ClanAI_NobleMemory_v1` | Noble memory rows; missing key imports an empty set. |
| `ClanAI_SocialLedger_v1` | Social/loyalty ledger rows; missing key imports an empty set. |
| `ClanAI_SocialEpisodes_v1` | Social episode rows. |
| `ClanAI_DynastyCanonCutoffHours_v1` | Dynasty canon cutoff scalar. |
| `ClanAI_DynastyBranchId_v1` | Current dynasty branch identity. |
| `ClanAI_DynastyBranchEpisodes_v1` | Current export is D2; D1 and D2 imports remain supported. Accepted production kinds are closed, including `IncidentOpened`, `IncidentChoice`, and `KingdomRulingClanChanged`; malformed/unknown kinds are rejected. |
| `ClanAI_CompanionDutyMemory_v1` | Companion duty rows. |
| `ClanAI_CompanionExperienceMemory_v1` | Companion experience rows. |
| `ClanAI_CompanionNegativeOutcomeMemory_v1` | Companion negative-outcome rows. |
| `ClanAI_SocialLoyaltyClanLoss_v2` | Current clan-loss loyalty memory. No v1 fallback is visible under this key; compatibility with any historical v1 save is unproven and should be resolved before release labeling. |
| `ClanAI_WarState_v1` | WarState, WarScar, objective, raid/loss, and strain serialized lines. |
| `ClanAI_KingdomContinuity_v1` | Kingdom continuity records and succession ordinal. |

The Phase 7B preflight, Phase 4A recovery observer, Dynasty structural runtime observer, observer safety behavior, prisoner/mercy behavior, ruler courtship behavior, and social captivity observer have empty `SyncData` implementations. Their counters, caches, and observations are correctly session-only.

Ordinary save/load continuity has strong accepted evidence: seven kingdom-continuity records restored without duplicate lifecycle notices or political mutation, and D1/D2 dynasty rows have focused compatibility tests. Remaining risks are format fragmentation across several hand-rolled line serializers, unknown historical compatibility for `SocialLoyaltyClanLoss_v2`, and the lack of a release-level corrupted-row/mixed-version matrix. No schema change is justified by this audit.

## 5. Packaging

- `src/ClanAI/package/ClanAI` contains only `SubModule.xml`. It has no `bin/Win64_Shipping_Client/ClanAI.dll`, configs/data, license/readme, or install artifact. The repository therefore cannot currently provide an installable mod without a developer build/deploy step.
- `SubModule.xml` identifies module version `v0.1.0`, while the assembly is `FileVersion 0.22.0.0` / `InformationalVersion v0.22A-ruler-courtship-native-v1`. Release identity is inconsistent.
- The manifest requires Native, SandBoxCore, Sandbox, StoryMode, and NavalDLC, but omits the Harmony dependency used by the DLL. Requiring NavalDLC may unnecessarily exclude players who do not own/install it unless source use proves it is mandatory; this needs a dependency audit before packaging.
- Research content (`Reports/`, `Tests/`, `Automation/`, telemetry, save fixtures, videos, build outputs) is not under the package tree and should remain excluded. A clean packaging script/manifest should copy only `SubModule.xml`, the release DLL and explicitly approved module-local data/config/documentation.
- An installed release should not require the research repository. This is achievable after resolving paths, release profile, dependencies, and artifact assembly.

## 6. Known unresolved gameplay evidence

### Not release blockers for an honestly labeled initial candidate

- Phase 6 Festival substitution is an accepted bounded natural null.
- Phase 7B player succession and Phase 7C-I3 ruling-clan structural history are accepted bounded natural-event nulls; deterministic ownership, scope, deduplication, and serialization tests exist.
- Target-kingdom defection remains timeboxed/unproven and must not be forced by raising its cap.
- Long-run WarScar retirement, volunteer/manpower equilibrium, troop-tier distribution, and bandit-density equilibrium remain incompletely measured.
- Complete player legibility and perfect balance of all modifiers remain future hardening work.

These gaps must remain disclosed in release notes and should inform later stability/balance telemetry. They do not justify reopening Phase 4/5/6 tuning or another rare-event hunt before the product is installable and independent.

### Release-readiness status

- Phase 8A audit: **COMPLETE**
- Standalone release blockers found: **YES**
- Gameplay changed: **NO**
- Bannerlord launched: **NO**
- Runtime experiment performed: **NO**

## Release-blocking issues

1. Replace every active `D:\BannerlordAIResearch` production path with a safe module-local or explicitly user-local release path and preserve conservative documented defaults.
2. Define a default release profile that does not register/install experiment-only observers and runtime proof telemetry, without removing gameplay policy or save compatibility.
3. Resolve and declare the Harmony runtime dependency; verify whether NavalDLC is genuinely required.
4. Produce a version-consistent installable module containing the manifest, `ClanAI.dll`, and only approved release data/docs; no research/test artifacts.
5. Add an offline release gate covering forbidden runtime dependencies/absolute paths, package contents, dependency declarations, save keys, and release-profile wiring before runtime stability testing.

## Non-blocking cleanup

- Replace absolute Bannerlord assembly hint paths with configurable build properties for reproducible contributor builds.
- Document every supported configuration switch and keep lab/debug switches absent or OFF by default.
- Consolidate or gate verbose diagnostics and retain only bounded player-support logging.
- Add corrupted-row and historical `SocialLoyaltyClanLoss` compatibility fixtures; do not change schema without evidence.
- Reconcile module/assembly/version labels and prepare concise install, compatibility, known-null, and rollback notes.
- Carry the unmeasured long-run equilibrium items into later Phase 8 balance/stability work without reopening completed tuning now.

## First recommended Phase 8 implementation checkpoint

**Phase 8B-I1 — standalone path and release-profile seam.** Introduce one pure, deterministic module-path resolver and one default release diagnostics gate; migrate the seven active `D:\BannerlordAIResearch` runtime paths to module-local locations, prevent the three experiment-only campaign behaviors plus Phase 4/6 proof telemetry from registering in the default release profile, and add focused offline invariants proving no production absolute development path, no development-tool dependency, unchanged save keys, unchanged gameplay-policy wiring, and conservative defaults. Do not package or run Bannerlord in this checkpoint.

