# Phase 8B-I2 — Dependency and Version Contract Result

Date: 2026-09-25

Parent checkpoint: `c071c117a89a2085eeffd1b435573f7c0c2e8bcb`

Scope: offline dependency, metadata, and build-contract cleanup only. Bannerlord was not launched, no DLL was deployed, no runtime stability test or package assembly was performed, and gameplay/save behavior was not changed.

## Result

Phase 8B-I2 is **COMPLETE**.

ClanAI uses Harmony directly for production patches. The supported release model is therefore an external required dependency on the canonical Bannerlord module ID `Bannerlord.Harmony`. `SubModule.xml` now declares that module at the locally supported `v2.4.2.248` contract, and the project references its installed `0Harmony.dll` with `Private=false`. No Harmony runtime assembly is present in the ClanAI package tree and no custom loader was introduced.

The production source, project references, and package assets contain no NavalDLC assembly, namespace, type, API, or data dependency. The stale required `NavalDLC` declaration was removed. NavalDLC may still appear in historical research evidence or test-launch descriptions; those records are not runtime dependencies and were not rewritten.

The active Phase 8 release-line identity is now consistently `v0.22.0`, a pre-1.0 identity compatible with Bannerlord's module-version form. `SubModule.xml`, runtime-visible labels, and `InformationalVersion` use `v0.22.0`; `AssemblyVersion` and `FileVersion` use `0.22.0.0`. Save keys and protocols were not renamed.

`ClanAI.csproj` now accepts configurable `BannerlordInstallDir`, `BannerlordBinDir`, and `BannerlordHarmonyBinDir` MSBuild properties. The current Steam installation remains only a developer fallback, preserving existing local builds without making that exact location part of the release contract. All TaleWorlds and Harmony hint paths use these properties.

## Validation

Focused offline invariants proved:

- external Harmony dependency declaration and absence of bundled Harmony DLLs;
- no active NavalDLC production dependency;
- manifest, assembly, file, informational, and runtime identity consistency;
- configurable Bannerlord dependency roots with the current path retained only as fallback;
- Phase 8B-I1 module-local paths and release-default profile remain intact;
- all twelve save keys remain unchanged;
- Phase 3–7 gameplay policy/source hashes and retrieval/writer semantics remain unchanged;
- Phase 6's stale `LocalBanditControlPatch.cs` preservation fixture was aligned to the authoritative current-main blob; no gameplay source changed.

Compiled metadata reports `AssemblyVersion=0.22.0.0`, `FileVersion=0.22.0.0`, and `ProductVersion=v0.22.0`. Release build completed with **0 errors** and the inherited `System.ValueTuple` warning.

Exact focused evidence: `Reports/Release/evidence/phase8b_i2_dependency_version_validation_20260925.txt`.

## Status

- Harmony dependency contract resolved: **YES**
- Harmony model: **external declared dependency**
- NavalDLC required: **NO**
- Version identity reconciled: **YES**
- Portable build seam added: **YES**
- Gameplay changed: **NO**
- Save schema changed: **NO**
- Bannerlord launched: **NO**
- Runtime tested: **NO**

The next small Phase 8 checkpoint is package assembly and offline package-content validation. That work was not started here.

