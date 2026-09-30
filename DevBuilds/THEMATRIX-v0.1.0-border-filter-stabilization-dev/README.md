# THEMATRIX v0.1.0 - Border Filter Stabilization Developer Build

This package contains the tested stabilization build for the narrow, default-open, lord-party settlement destination filter. It is not full kingdom-border behavior and has not been deployed or runtime-tested.

- Package build identity: `v0.1.0-THEMATRIX-stabilization-dev` (assembly informational version).
- Public module display/version: `THEMATRIX v0.1.0`.
- Compatibility identifiers remain unchanged: module ID `ClanAI`, assembly/DLL `ClanAI`/`ClanAI.dll`, assembly/file version `0.23.0.0`.
- The package's SHA-256 manifest is `SHA256SUMS.txt`; the archive hash and test/build receipt are in the repository checkpoint evidence.
- Empty default-open configuration: `ClanAI/Data/KingdomBorderClosures.cfg`.
- Install procedure, exact scope, Evidence-profile diagnostics and rollback: `DEMO_RUNBOOK.md`.

This stabilization package supersedes the previous v0.1 destination-filter developer package for future controlled testing. The earlier directory and ZIP remain preserved unchanged. Neither package replaces frozen RC1.

Copy the **contents** of the archive's top-level `ClanAI` directory into the game's `Modules/ClanAI` directory. Do not create a nested `Modules/ClanAI/ClanAI` directory. This candidate remains uninstalled. Coordinate a controlled test first; never overwrite a running game or an original save.
