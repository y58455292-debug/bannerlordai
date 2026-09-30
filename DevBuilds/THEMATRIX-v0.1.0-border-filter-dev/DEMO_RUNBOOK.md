# THEMATRIX v0.1 Developer Demo Runbook

## Build identity and installation boundary

This is a developer-only experiment for closed-settlement visit-candidate filtering. It has not been deployed or verified in a campaign. Do not overwrite a running game's files. Before any future installation, confirm Bannerlord is closed, back up the exact installed `ClanAI.dll` and `SubModule.xml`, and record their hashes.

The archive contains a top-level `ClanAI` directory. After backing up the installed `Modules/ClanAI` directory, copy the **contents of the archive's `ClanAI` directory** into the game's `Modules/ClanAI` directory. The result must be `Modules/ClanAI/SubModule.xml`, `Modules/ClanAI/bin/.../ClanAI.dll`, and `Modules/ClanAI/Data/...`. Do not copy the package's `ClanAI` folder as a nested `Modules/ClanAI/ClanAI` folder. The module compatibility ID remains `ClanAI`; public display/version are `THEMATRIX v0.1.0`.

Keep the RC1 archive and the previously installed C2 candidate untouched. Rollback by restoring the recorded DLL and metadata backups, then verify the restored DLL hash.

## Configure a directed closure

The optional file is `ClanAI/Data/KingdomBorderClosures.cfg`. Each active line is:

```text
ClosedTerritoryKingdomStringId>ExcludedVisitorKingdomStringId
```

This means the first kingdom excludes visiting parties of the second kingdom from choosing settlements owned by the first as ordinary visit candidates. It is directional. To close both ways, two lines would be required. The committed template is empty and default-open.

Do not guess IDs from kingdom display names. Use exact live `Kingdom.StringId` values and confirm the destination settlement's current `OwnerClan.Kingdom.StringId` from a trusted read-only inspector/debugger against the active campaign. This package does not include an ID lookup UI. If those live IDs are not available, leave the file empty.

The file loads once when the campaign behavior registers. A change requires a campaign/session reload that reruns registration. Missing, unreadable, empty, malformed, or self-closure input resets the complete policy to open.

## What to observe

The implementation removes matching destinations from Bannerlord's existing native settlement visit-candidate list. It does not deny entry, cancel an existing route, prevent travel through a third kingdom, or guarantee an alternate action. It applies to eligible NPC lord parties, including NPC-controlled secondary parties belonging to the player clan's kingdom. It excludes the player main party, armies, caravans, villagers, convoys, sea/battle/attached/disabled/stopped/disbanding and leaderless parties, unresolved or independent affiliations, and parties at war with the destination kingdom. Being at war with another kingdom does not exempt a party.

## Optional diagnostic evidence

There is no UI, console, or public command to request a diagnostic snapshot. `ClanAIDiagnostics.Snapshot` is private and the diagnostics class is internal. The package defaults to the `Release` runtime profile, so evidence-gated diagnostics and telemetry are inactive by default.

For a bounded test, use a backed-up disposable campaign. With Bannerlord fully exited, create `Modules/ClanAI/Data/RuntimeProfile.cfg` beside `KingdomBorderClosures.cfg` with exactly this line:

    Profile=Evidence

Then start Bannerlord and load the disposable campaign. `RuntimeProfile` reads the file on first use and caches it for the process. Fully exit and restart the game after creating, editing, or removing this file; a campaign reload alone does not change the cached profile.

Evidence mode automatically writes session diagnostics; it does not expose a command. After the session starts, inspect the newest `*.diagnostics.txt` file under `Modules/ClanAI/Logs/Sessions`. It includes `KINGDOM_BORDER_DESTINATION_FILTER candidateChecks=... candidatesFiltered=...`. Snapshots are written at session start, periodically (after a frame gate and no more than once per 30 seconds), after a save boundary, and at session end. A `.finalhealth.txt` sidecar may be written at module unload. Permissions or logging failures can prevent output.

Evidence mode is broader than this border counter: it enables the module's existing evidence-gated telemetry and observer hooks and writes additional log/telemetry files. This adds runtime work and file I/O. Use it only for a short observation in a backed-up disposable campaign; an Evidence-profile run is not a Release-profile performance or behavior proof. The border filter itself adds no per-candidate file I/O. A nonzero `candidatesFiltered` confirms only that a matching native visit candidate was removed.

To restore the default `Release` profile, fully exit Bannerlord, remove `RuntimeProfile.cfg`, and restart. A missing profile defaults to `Release`. Do not include `RuntimeProfile.cfg` in the distributed/default package.

If a party loses its last visit candidate, the native visit scorer produces no `GoToSettlement` score for that list. Other native behavior scores may exist, but an alternate action is not guaranteed. The retained candidate-list source loop tolerates an empty list; no end-to-end no-stall/liveness proof exists. Observe the party and stop the test if it repeatedly retries/stalls. Do not infer safe frontier enforcement from this narrow destination filter.

## Explicitly outside this demo

No route/frontier enforcement, arrival veto, direct movement, villager/caravan/convoy handling, trade agreement/vote or economic effects, custom territory map, automatic closure, save schema, or full border behavior is included. Do not claim the full kingdom-border feature is complete.
