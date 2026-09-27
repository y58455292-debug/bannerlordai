# ClanAI v0.22.0 RC1 — Installation

This is an internal player release candidate, not a final public v1.0 release. The RC1 label applies to the archive; the tested module remains internally versioned `v0.22.0`.

## Requirements

- Mount & Blade II: Bannerlord supported runtime (`v1.5.3` for this candidate).
- `Bannerlord.Harmony` `v2.4.2.248`, or a compatible version satisfying the declared dependency.

## Install

1. Close Bannerlord.
2. Extract `ClanAI-v0.22.0-RC1.zip`.
3. Copy the extracted `ClanAI` folder into Bannerlord's `Modules` directory.
4. Ensure `Bannerlord.Harmony` is installed.
5. In the Bannerlord launcher, enable `Bannerlord.Harmony` and `ClanAI`.
6. Start a **new campaign** for the organic long-run test.

The installed shape should be:

```text
Modules/
  ClanAI/
    SubModule.xml
    README.md
    bin/
      Win64_Shipping_Client/
        ClanAI.dll
```

Do not add a `RuntimeProfile.cfg`, Evidence profile, Visual War marker, or research configuration for ordinary play. Release defaults are Evidence OFF, Visual War OFF, and Strategic Commitment Observe.

BannerlordInspector and TestRunner are not required. ChatGPT, Codex, Desktop Commander, watchdogs, command buses, internet access, and API services are not required for installation or gameplay. NavalDLC is not a ClanAI dependency.

Tested DLL SHA-256:

`A18341FB2CD6B8623B45B52155C1A7D987DBEE7AAE14AE7DBF704514EC7F37B5`

