# Phase 8B-I3 — Installable Module Package Assembly Result

Date: 2026-09-25

Parent checkpoint: `ab138baf2ec8f5842d104792a21433bc0ab85e2c`

Scope: offline Release build, installable module assembly, and deterministic package-content validation only. Bannerlord was not launched, the package was not deployed, no runtime stability test was performed, and gameplay/save behavior was not changed.

## Result

Phase 8B-I3 is **COMPLETE**. The installable module root is:

`src/ClanAI/package/ClanAI`

Its complete content is:

- `SubModule.xml`;
- `README.md`;
- `bin/Win64_Shipping_Client/ClanAI.dll`.

The README documents installation, the external `Bannerlord.Harmony` requirement, and conservative defaults. No `Data` overrides are shipped: configuration absence is the approved deterministic Release default. Therefore Evidence telemetry is OFF, Strategic Commitment remains Observe, Visual War remains OFF, and the recovery gate remains Suppress. Users may create supported module-local configuration later; this package enables none.

No PDB was included. An initial offline scan found the Release DLL's PE debug directory still embedded the local build path to `ClanAI.pdb`. The Release build contract was narrowed to `DebugSymbols=false` and `DebugType=none`; the rebuilt packaged DLL contains no local-user/Codex path. This changes build metadata only, not gameplay or save schema.

## Package identity

- Module ID: `ClanAI`
- Module version: `v0.22.0`
- Entry assembly: `ClanAI.dll`
- Entry point: `ClanAI.ClanAISubModule`
- DLL size: `335872` bytes
- DLL SHA-256: `A18341FB2CD6B8623B45B52155C1A7D987DBEE7AAE14AE7DBF704514EC7F37B5`
- Assembly version: `0.22.0.0`
- File version: `0.22.0.0`
- Product/informational version: `v0.22.0`
- Package zip: not created; the committed module tree is the internal release-candidate artifact

The deterministic file-size/SHA-256 manifest is `Reports/Release/evidence/phase8b_i3_package_manifest_20260925.txt`.

## Dependency validation

`SubModule.xml` requires Native, SandBoxCore, Sandbox, StoryMode, and external `Bannerlord.Harmony`; NavalDLC is absent. The DLL metadata references only `netstandard`, the declared TaleWorlds game assemblies, and `0Harmony`. `0Harmony.dll` and all other Harmony runtime assemblies remain external and are not bundled.

## Offline validation

The package gate passed all of the following:

- exact allowlisted package content; no unexpected files;
- required DLL presence and non-empty content;
- deterministic package manifest matches every packaged byte;
- correct module ID, version, dependency set, DLL name, and entry point;
- no bundled Harmony runtime, PDB, deps file, log, save, video, ZIP, build intermediate, or repository-only artifact;
- no active `D:\BannerlordAIResearch`, local-user build path, ChatGPT, Codex, Desktop Commander, TestRunner, watchdog, or HTTP(S) token in the packaged production DLL;
- no Evidence-profile config, Visual War marker, or Strategic Commitment override ships;
- all twelve save keys remain unchanged;
- Phase 8B-I1 path/profile invariants pass;
- Phase 8B-I2 dependency/version invariants pass;
- Phase 3–7 policy-hash preservation and Phase 7 retrieval/writer invariants pass.

Release build completed with **0 errors** and the inherited `System.ValueTuple` warning. Exact validation evidence is `Reports/Release/evidence/phase8b_i3_package_validation_20260925.txt`.

## Status

- Installable module assembled: **YES**
- Package-content validation: **PASS**
- Harmony bundled: **NO**
- Evidence profile enabled by default: **NO**
- Gameplay changed: **NO**
- Save schema changed: **NO**
- Bannerlord launched: **NO**
- Runtime tested: **NO**
- Ready for first release-profile runtime smoke test: **YES**

The next milestone is one separately authorized, bounded release-profile runtime smoke test of this exact packaged DLL. No runtime work was started here.

