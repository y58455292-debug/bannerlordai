# ClanAI v0.22.0

ClanAI is an offline single-player Bannerlord module.

## Install

1. Install the `Bannerlord.Harmony` module version `v2.4.2.248` or a compatible supported version.
2. Copy this entire `ClanAI` folder into the game's `Modules` directory.
3. Enable `Bannerlord.Harmony` and `ClanAI` in the Bannerlord launcher.

ClanAI also uses the Bannerlord modules declared in `SubModule.xml`. NavalDLC is not required.

## Release defaults

No configuration file is required. Missing configuration selects conservative player-release defaults:

- runtime profile: Release;
- evidence/proof telemetry: disabled;
- Strategic Commitment: Observe;
- Visual War: disabled;
- weak-bandit recovery gate: Suppress.

Research tooling, TestRunner, watchdogs, network services, reports, tests, saves, and development logs are not required and are not included.

The optional Evidence profile is development-only and is never enabled by this package. Visual War is likewise not enabled by this package.

