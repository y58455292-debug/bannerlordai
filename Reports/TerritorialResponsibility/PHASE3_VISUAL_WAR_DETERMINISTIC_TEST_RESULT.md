# Phase 3 Visual War Decision Layer — Deterministic / Standalone Result

Date: 2026-09-25
Repository base: `f3512c7b2c8cc434df879b179773d91435c11a5b`

## Scope

This checkpoint completes only the bounded offline seam for the existing `VisualWarDecisionLayer`. It does not deploy a DLL, run a campaign proof, tune factors, add strategic roles, or change Bannerlord candidate/target legality.

## Implementation

Added `VisualWarPolicy`, a pure helper with no Bannerlord types. The runtime layer still owns:

- Bannerlord's existing `PartyThinkParams.AIBehaviorScores` candidate list;
- settlement and mobile-party targets supplied by Bannerlord;
- settlement frontier/attack classification;
- faction and war-state checks;
- bandit classification from the existing world snapshot;
- current scores and final winner through `StrategicDecisionComposer`;
- Visual War diagnostic counters and winner-change logging.

Only the already-existing weak/exclusion and factor-selection rules moved into the pure helper.

## Existing rules preserved

Weak/recovery remains:

`readiness < 0.72 || foodDays < 3`.

Friendly settlement defense:
- active attack: `min(1 + 0.22 * pressure + 0.10, 1.35)`; active attack pressure remains 1, producing 1.32;
- frontier defense: `min(1 + 0.22 * frontierScore, 1.35)`;
- weak actors receive neither active-defense nor frontier-defense contribution.

Enemy frontier offense remains:
- only existing Raid/Besiege/Assault settlement candidates;
- target settlement must be at war with the actor faction;
- target context must be frontier-positive and identify the actor faction as its nearest enemy;
- actor must not be weak;
- factor remains `1 + 0.16 * frontierScore`.

Rear-security remains:
- only existing `EngageParty` candidates whose native target is a mobile bandit party;
- actor must not be weak;
- men must be >0 and <=160;
- <=90 men: factor 1.25;
- 91-160 men: factor 1.15;
- non-bandit `EngageParty` receives no rear-security factor.

Non-positive native candidate scores and unrelated behaviors remain untouched.

## Standalone-safe activation

The previous absolute development switch:

`D:\BannerlordAIResearch\Data\ENABLE_VISUAL_WAR_LAB.txt`

was removed.

Activation now resolves from `VisualWarDecisionLayer`'s installed assembly location to:

`<Bannerlord Modules>/ClanAI/Data/ENABLE_VISUAL_WAR_LAB.txt`

for the standard `ClanAI/bin/<platform>/ClanAI.dll` module layout.

Safety behavior is preserved:
- unexpected/invalid module layout resolves no path;
- missing marker remains OFF;
- no marker file is added by this checkpoint;
- therefore cleaning the path does not silently enable Visual War;
- no ChatGPT, Codex, TestRunner, Desktop Commander, watchdog, or other development-tool dependency is introduced.

## Deterministic validation

`dotnet run --project Tests/TerritorialResponsibility/VisualWarPolicyTests.csproj -c Release`

Result:

`PASS VisualWar policy tests checks=32`

`defense/offense/rear-security factors and weak exclusions preserved`

`activation marker resolves module-locally and remains absent-by-default`

Coverage includes:
- weak active-defense skip;
- weak frontier-defense skip;
- exact 0.72 / 3-day healthy boundary;
- active friendly defense factor;
- friendly frontier-defense factor and 1.35 cap;
- enemy frontier offense;
- nearest-enemy and weak offense exclusions;
- rear-security bandit eligibility;
- 90/91/160/161 party-size edges;
- weak rear-security skip;
- non-bandit EngageParty exclusion;
- non-Engage unrelated behavior;
- zero/negative native score exclusions;
- module-local activation resolution and safe-off invalid layout.

`python Tests/TerritorialResponsibility/test_visual_war_runtime_wiring.py`

Result:

`PASS VisualWar runtime wiring`

`native candidates/world classification/composer ownership preserved`

`python Tests/TerritorialResponsibility/test_visual_war_standalone_path.py`

Result:

`PASS VisualWar standalone activation path`

`activation is module-local Data/ENABLE_VISUAL_WAR_LAB.txt and OFF when absent`

## Release build

`dotnet build src/ClanAI/src/ClanAI/ClanAI.csproj -c Release`

Result:
- Build succeeded;
- 0 errors;
- 1 inherited `System.ValueTuple` version-conflict warning;
- built DLL SHA-256: `F038F0160B3A39B4175DBE0BD7C6B81F6EB7AD052D904EA413DC71682076DEA1`.

No DLL was deployed.

## Native-authority boundary

The wiring invariant confirms:
- `VisualWarDecisionLayer` still iterates Bannerlord-provided candidates;
- native settlement/mobile-party targets remain authoritative;
- world classification remains runtime-owned rather than synthesized by the pure policy;
- the layer reads current candidate scores through the composer;
- all score contributions go through `StrategicDecisionComposer.ApplyFactor`;
- `StrategicDecisionComposer` remains the sole native score writer;
- `ActorStrategicBlackboard` still recognizes the existing `active-defense`, `frontier-defense`, and `rear-security` contribution reasons;
- no direct party order, synthetic candidate/target, settlement mutation, faction mutation, or war mutation was added.

## Boundary / next milestone

This is an offline deterministic and standalone-path checkpoint only. It makes no new runtime behavior claim.

The expected next milestone is one bounded runtime proof that an existing Visual War defensive/security contribution changes a Bannerlord-native candidate winner and Bannerlord commits the selected native behavior/target.
