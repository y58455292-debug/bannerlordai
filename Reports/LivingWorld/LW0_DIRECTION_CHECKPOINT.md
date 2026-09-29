# LW0 — Living World Direction Checkpoint

Date: 2026-09-29
Scope: one offline documentation checkpoint, Codex only.

## Source and outcome

Authoritative GitHub main/source checkpoint: `9ec113e738af35254e113fbde7c05babbf3c405d`.
Source root tree: `4c8b888cae43f85b08fdab375d38ae41d9936b9d`.

Living World direction is durable in [LIVING_WORLD_DIRECTION.md](../../LIVING_WORLD_DIRECTION.md). Its 22 themes preserve the user's intent: meaningful lives inside native systems, homes and responsibilities in peace and war, bounded autonomy, purposeful movement, social memory, player authority and real native actions.

Only these files change:

- `LIVING_WORLD_DIRECTION.md` — new post-RC1 design charter.
- `README.md` — charter link, frozen baseline and next planned research.
- `ROADMAP.md` — Phase 8C ongoing field validation plus ordered LW1–LW9 development line.
- `Reports/AgentStatus/CODEX_STATUS.md` — current LW0 status; earlier evidence preserved.
- `Reports/LivingWorld/LW0_DIRECTION_CHECKPOINT.md` — this receipt.

## RC1 preservation

RC1 remains the frozen playable baseline. The accepted I5 hashes below are preserved by unchanged Git object identities; no new build or runtime validation is claimed. Source evidence: [PHASE8B_I5_RC_FREEZE_RESULT.md](../Release/PHASE8B_I5_RC_FREEZE_RESULT.md).

| Artifact | SHA-256 |
|---|---|
| `Releases/ClanAI-v0.22.0-RC1.zip` (129579 bytes) | `A03D183934BCF1C974EA6BCEBBF35CFF3A5413080D6F6634942811AE54C971D6` |
| Tested `ClanAI.dll` (335872 bytes) | `A18341FB2CD6B8623B45B52155C1A7D987DBEE7AAE14AE7DBF704514EC7F37B5` |
| Tested package `README.md` (999 bytes) | `2B8D181068E8CFC38BB518B1EB144BCBA979E7B57EDE49420213811ABB8C6E7E` |
| Tested package `SubModule.xml` (1177 bytes) | `287609EEBE39EE00D693CD1C354362C64878F40950BCD787D505F20F065B333D` |

Preservation identities from source main:

- RC archive Git blob: `a5124da1390f6bd9d381a4b1fb020f91439c6102`.
- Tested package Git tree: `6af297032fd55c03326d50ac3acbe2c9ba64b246`.
- Tested DLL Git blob: `a5fc3923b31e482960026a0044dd1b21d6e6d00a`.

## Offline validation

GitHub connector reads supplied authoritative main, the four required documents, latest commit history, repository tree and accepted RC freeze evidence. The scratch workspace had no checkout or interrupted local changes to reconcile. Git clone was unavailable because the configured network proxy could not connect; no tooling repair was needed. Changes are applied as one tree based on source main, followed by a single-parent commit and non-forced main update.

The candidate tree is checked against the source tree: exactly the five Markdown paths above differ; every other entry retains its Git object identity. This proves:

- no `src/` gameplay file changed;
- no package or RC artifact changed;
- no save key/schema changed (all source/configuration files unchanged);
- no DLL changed;
- `LIVING_WORLD_DIRECTION.md` created with all 22 themes;
- README references the charter;
- ROADMAP contains ordered LW1–LW9, all planned/not implemented;
- RC1 remains the frozen baseline.

This is documentation/preservation validation, not a repeated gameplay test. No tests requiring builds or Bannerlord are run. After committing, verify GitHub main resolves to the new commit and its diff contains only these five paths.

## Task boundaries and next milestone

- Gameplay changed: **NO**
- Policy tuned: **NO**
- Save schema changed: **NO**
- DLL/package/RC archive changed: **NO**
- Bannerlord launched: **NO**
- ClanAI rebuilt: **NO**
- ClanAI deployed: **NO**
- Home Assignment implementation started: **NO**

**Next milestone: LW1-A — Persistent Home Assignment native-seam/design audit.**

LW1 is not implemented. STOP after this documentation commit and main verification. Do not begin LW1 audit or implementation in this task.
