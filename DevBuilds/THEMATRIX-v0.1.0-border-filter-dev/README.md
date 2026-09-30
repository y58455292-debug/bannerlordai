# THEMATRIX v0.1.0 — Developer Border Destination Filter

This exact Release module is a limited, default-open experiment that removes explicitly excluded foreign-owned settlement destinations from ordinary NPC lord-party visit candidates. It does not enforce physical borders or routes.

- Module compatibility ID and DLL remain `ClanAI` / `ClanAI.dll`.
- Candidate hash and package file hashes: `SHA256SUMS.txt`.
- Empty default configuration: `ClanAI/Data/KingdomBorderClosures.cfg`.
- The archive's top-level `ClanAI` folder contains module contents. Copy its contents into `Modules/ClanAI`; do not create a nested `Modules/ClanAI/ClanAI` directory.
- Runtime profile defaults to Release. Evidence mode is an optional manual opt-in and enables broader telemetry; see `DEMO_RUNBOOK.md`.
- Scope, setup, observation and rollback: `DEMO_RUNBOOK.md`.
- Runtime status: not yet tested; not deployed.

Do not treat this as complete kingdom-wide border behavior.
