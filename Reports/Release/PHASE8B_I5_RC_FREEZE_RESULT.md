# Phase 8B-I5 — RC Freeze / Player-Playable Release Candidate

Date: 2026-09-27 UTC

Parent checkpoint: `44f7bc6cf5b8d449f03232c072d73e27132fb01a`

External artifact label: **ClanAI v0.22.0 RC1**

Internal module/DLL version: **v0.22.0**, unchanged

Scope: offline verification, deterministic archive assembly, player documentation, and RC freeze only. Bannerlord was not launched, the module was not deployed or rebuilt, and gameplay/save behavior was not changed.

## Classification

**RC1 READY FOR PLAYER CAMPAIGN**

Phase 8B-I4R4 release-profile runtime smoke is accepted as **PASS**. The exact bytes exercised by that smoke are frozen without changing the module identity or DLL.

## Frozen package

Source Git checkpoint: `44f7bc6cf5b8d449f03232c072d73e27132fb01a`

Authoritative tested package identities:

| Relative path | Bytes | SHA-256 |
|---|---:|---|
| `ClanAI/README.md` | 999 | `2B8D181068E8CFC38BB518B1EB144BCBA979E7B57EDE49420213811ABB8C6E7E` |
| `ClanAI/SubModule.xml` | 1177 | `287609EEBE39EE00D693CD1C354362C64878F40950BCD787D505F20F065B333D` |
| `ClanAI/bin/Win64_Shipping_Client/ClanAI.dll` | 335872 | `A18341FB2CD6B8623B45B52155C1A7D987DBEE7AAE14AE7DBF704514EC7F37B5` |

These match the exact committed/installed file identities recorded by I4R4. The two text files include the tested downloaded trailing CRLF. No file inside the module was renamed, edited, or supplemented for the RC label.

Package contract remains:

- module ID `ClanAI`, version `v0.22.0`;
- entry point `ClanAI.ClanAISubModule` from `ClanAI.dll`;
- Native, SandBoxCore, Sandbox, StoryMode, and external `Bannerlord.Harmony v2.4.2.248` declared;
- NavalDLC absent as a ClanAI dependency;
- no PDB, `0Harmony.dll`, research/test content, logs, saves, build intermediates, or Data overrides;
- Release defaults: Evidence OFF, Visual War OFF, Strategic Commitment Observe.

## Deterministic RC archive

Artifact: `Releases/ClanAI-v0.22.0-RC1.zip`

- ZIP bytes: **129579**
- ZIP SHA-256: **`A03D183934BCF1C974EA6BCEBBF35CFF3A5413080D6F6634942811AE54C971D6`**
- Entry order: lexicographically sorted
- Entry timestamp: fixed to `2026-09-27 00:00:00` for all files
- Compression: deterministic DEFLATE level 9 in the recorded build environment
- Repeated assembly in the same environment reproduced the identical ZIP hash

The contained file hashes above are the authoritative cross-tool package identities. The archive hash identifies this exact RC1 artifact.

## I4R4 evidence accepted

The release-profile smoke proved, within its bounded scope:

- packaged module and external Harmony loaded;
- protected campaign loaded;
- bounded pre-save progression of `20.016` campaign hours;
- guarded save materialized;
- the exact guarded save reloaded;
- bounded post-reload progression of `4.056` campaign hours;
- total bounded progression of `24.072` campaign hours;
- Release telemetry/default configuration remained off;
- protected fixture identity remained unchanged.

This is not exhaustive performance, network, file-access, or every-feature proof.

## Honest RC notes

The following are accepted non-blocking gaps, not claims that the systems are broken:

- target-kingdom defection/join remains timeboxed and unproven;
- Phase 6 low-loyalty Festival substitution remained a bounded natural null;
- natural succession and ruling-clan structural-history runtime paths remain bounded rare-event nulls;
- long-run manpower, troop-tier, bandit, and WarScar equilibrium remains incompletely measured;
- perfect balance and player-facing polish are not claimed.

The RC is therefore for a fresh organic campaign and long-run field characterization, not a final public v1.0 declaration.

## Final offline gate

The focused gate passed:

- exact I4R4-tested DLL and text-file hashes;
- exact three-entry archive allowlist and deterministic rebuild;
- module/version/entry-point and external Harmony declaration;
- no NavalDLC dependency or bundled Harmony runtime;
- no PDB, Data override, Evidence config, Visual War marker, Strategic Commitment override, research/test artifact, or unexpected file;
- no `D:\BannerlordAIResearch`, local-user build path, ChatGPT, Codex, Desktop Commander, TestRunner, watchdog, or HTTP(S) runtime token in the DLL;
- all twelve save keys unchanged;
- Phase 8B-I1/I2/I3 preservation checks remain valid;
- I4R4 PASS report remained unchanged;
- no gameplay source or save schema changed.

Exact record: `Reports/Release/evidence/phase8b_i5_rc_freeze_validation_20260927.txt`.

## Player handoff

- Installation: `Reports/Release/CLANAI_V0220_RC1_INSTALL.md`
- Organic beta: `Reports/Release/CLANAI_V0220_RC1_PLAYER_BETA.md`

## RC status

- I4R4 runtime smoke: **PASS**
- RC package frozen from exact tested bytes: **YES**
- DLL unchanged: **YES**
- RC archive created: **YES**
- RC archive SHA-256: **`A03D183934BCF1C974EA6BCEBBF35CFF3A5413080D6F6634942811AE54C971D6`**
- Install instructions created: **YES**
- Player-beta runbook created: **YES**
- Gameplay changed: **NO**
- Save schema changed: **NO**
- Bannerlord launched: **NO**
- Ready for user organic campaign: **YES**

Recommended next milestone: **Phase 8C — organic player campaign / long-run field validation**.

Phase 8C was not started in this task.

