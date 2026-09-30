# LW1-C3 — Persistent Roster Deployment Status

**Deployment: COMPLETE, 2026-09-30 UTC. Runtime roster and save/reload verification: NOT YET TESTED.**

## Candidate and target

The exact previously validated `v0.23.0-LW1C2-dev` candidate from GitHub commit `54506d232623ea63c04dbac1f9b200b3c9d197d9` was installed at:

`C:\\Program Files (x86)\\Steam\\steamapps\\common\\Mount & Blade II Bannerlord\\Modules\\ClanAI\\bin\\Win64_Shipping_Client\\ClanAI.dll`

Candidate and post-copy installed SHA-256: `B3760F027476C89555414C38D94CECB47C4AC0D08EA2B7E16E594BA516DA9563`.

Before deployment, the installed DLL SHA-256 was `B1911B17A04F42C11AB6E23DB3543AFE788FDFA07EFCF34DCF3638D4AB241271`. The module metadata identifies `ClanAI` with version `v0.22.0`; `SubModule.xml` was not changed and its SHA-256 remained `287609EEBE39EE00D693CD1C354362C64878F40950BCD787D505F20F065B333D`.

## Rollback and safety

A byte-verified pre-deployment DLL and `SubModule.xml` backup, plus rollback steps, are retained locally in `Reports/LivingWorld/evidence/lw1c3_predeploy_backup_20260930/` inside the authorized task workspace. The DLL backup hash matches the pre-deployment target; the metadata backup hash matches the unchanged installed metadata.

The Bannerlord/TaleWorlds process check was clear before deployment and after copy. Only `ClanAI.dll` was replaced. No RC1 archive or other module file was changed. During DLL deployment, no game was launched or campaign time advanced. The selected save was copied later as documented below; the original remains unchanged. Installation proves file placement and hash only; it does not prove launcher load, menu appearance, runtime status, or persistence behavior.

## Bounded live verification plan

Coordinate the live demo with the user before launch. Identify the relevant organic campaign save at that time and create a clearly named disposable copy through the normal game save workflow. Preserve the original and autosaves. With campaign time paused, inspect the existing Manage Home Assignments menu for stable names, home labels, and activity/status explanations. Then perform the bounded save/reload closure on the disposable copy, checking that valid homes and roster identity/status remain correct. Stop at the agreed bound; do not begin LW2, add AI behaviors, or tune policy factors.

The next action is user-coordinated runtime verification. The historical save name `LW1C LAND FIX 20260930 0436.sav` was not assumed to be current or modified.

## Selected save and disposable copy

The user selected `LW1C LAND FIX 20260930 0436.sav` from the normal Bannerlord `Documents/Mount and Blade II Bannerlord/Game Saves` directory (OneDrive-redirected). It was present as one `.sav` file with no same-name sidecars. The source was 7,855,918 bytes with SHA-256 `C792B001164A9AF911AF5571C092AC7AC7C1C586FE0FC09D7B1988675D4F8D38`.

A no-overwrite byte copy is ready beside it as `LW1C LAND FIX 20260930 0436 - LW1-C3 DEMO COPY.sav`. The copy is 7,855,918 bytes with the identical SHA-256. The original was re-hashed after copying and remains unchanged. The game was closed during the operation. The unique file name is intended to distinguish the copy; its visible label in Bannerlord's save picker remains unverified until the user launches the game. No save content or binary metadata was edited.

