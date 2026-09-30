# THEMATRIX v0.1.0 Border Filter Stabilization Runbook

## Package identity and safety

This developer build has assembly informational version `v0.1.0-THEMATRIX-stabilization-dev`. The public module display/version remain `THEMATRIX v0.1.0`. Compatibility values remain module ID `ClanAI`, assembly and DLL name `ClanAI`, and assembly/file version `0.23.0.0`.

The candidate has passed deterministic checks and a game-target Release compile. It has **not** been deployed or runtime-tested. No no-stall, route-crossing, or full border behavior is claimed. Do not change an active game installation or original campaign save. Keep the frozen RC1 and original destination-filter package untouched.

The archive contains a top-level `ClanAI` directory. For a future authorized controlled installation, fully exit Bannerlord, back up the exact installed `Modules/ClanAI` directory, and copy the **contents of the archive's `ClanAI` directory** into `Modules/ClanAI`. The result must be `Modules/ClanAI/SubModule.xml`, `Modules/ClanAI/bin/Win64_Shipping_Client/ClanAI.dll`, and `Modules/ClanAI/Data/...`. Do not create a nested `Modules/ClanAI/ClanAI` directory. Record pre-install hashes and verify the candidate hash against `SHA256SUMS.txt`; restore the backup for rollback.

## Directed closure input

`ClanAI/Data/KingdomBorderClosures.cfg` is empty by default. Each non-comment line is:

```text
ClosedTerritoryKingdomStringId>ExcludedVisitorKingdomStringId
```

The first kingdom excludes the second kingdom's eligible lord parties from choosing settlements currently owned by the first as ordinary visit candidates. This is directional. Two lines are required to close both directions. Use exact live `Kingdom.StringId` values; do not guess from display names. This is a developer input, not diplomacy voting, a campaign setting, or a saved policy. It loads when the behavior registers; edits require restarting/reloading the campaign so registration runs again. Missing, empty, unreadable, malformed, or self-closure input resets policy to default-open.

## Exact implementation boundary

The filter acts only on Bannerlord's settlement visit-candidate list for eligible NPC lord parties. It removes destinations owned by the explicitly closed kingdom using current party and settlement ownership. It does not classify the land between destinations, block route crossings, cancel an already selected route, veto entry, or handle villagers, caravans, convoys, trade, or diplomacy.

Exemptions are specific: the player's **main party** is excluded, but eligible NPC-controlled secondary parties in the player's clan kingdom are subject to that kingdom's policy. A party at war with the candidate's destination kingdom bypasses that destination's closure; being at war with an unrelated kingdom does not. Armies, caravans, non-lords, independent/unresolved affiliations, inactive/disabled/stopped/attached/battle/sea/disbanding/leaderless/waiting-for-disband parties are excluded. Other alliance, enclave transit, and already-inside/exit policies are not implemented.

### All-closed candidate safeguard

If a non-empty native candidate list is entirely blocked, the patch reopens exactly one row that has a non-empty settlement `StringId` and finite native distance: the lowest-distance row, with ordinal `StringId` as tie-break. The existing native row and its native-computed navigation data are reused. `candidateFailOpenEvents` and `candidateReopened` count this bypass. Closure is advisory for the reopened settlement in that case. `candidatesFiltered` counts policy block results before the safeguard, including the reopened row.

If all blocked rows lack a stable settlement ID or have NaN/infinite distances, no row is reopened and the list can remain empty. Tests cover these inputs, but no occurrence of such rows in a live native list has been established. An originally empty native list remains empty; the patch invents no destination.

The native selector emits a GoToSettlement score only when an eligible score exists; otherwise it may retain the current action. Retaining one candidate prevents this filter from emptying a non-empty list with at least one valid finite-distance/ID row. It does **not** guarantee another score, an action change, or campaign liveness. No end-to-end no-stall runtime proof exists.

## Optional diagnostic evidence

There is no UI, console, or public command to request a diagnostic snapshot. `ClanAIDiagnostics.Snapshot` is private and the diagnostics class is internal. The package defaults to Release; Evidence-profile telemetry is inactive by default.

For a bounded test using a backed-up disposable campaign, fully exit Bannerlord and create `Modules/ClanAI/Data/RuntimeProfile.cfg` beside `KingdomBorderClosures.cfg` with exactly:

```text
Profile=Evidence
```

Start the game and load the disposable campaign. RuntimeProfile caches this setting for the process, so fully exit and restart after creating, editing, or removing the file. Evidence mode automatically writes session snapshots under `Modules/ClanAI/Logs/Sessions`; snapshots are taken at session start, periodically after a frame gate (no more than once per 30 seconds), after a save boundary, and at session end. A `.finalhealth.txt` sidecar may be written on unload. Inspect `KINGDOM_BORDER_DESTINATION_FILTER candidateChecks=... candidatesFiltered=... candidateFailOpenEvents=... candidateReopened=...` in the newest diagnostics text.

Evidence mode also enables the module's existing telemetry and observer hooks, adds runtime work and file I/O, and can produce more log files. It is not a Release-profile performance or behavior proof. The filter itself performs no per-candidate file I/O. Logging permissions/errors can prevent snapshots. Remove `RuntimeProfile.cfg` and restart to return to Release; do not ship that file in the package.

## Not included

No route/frontier enforcement, movement/action override, entry veto, land-control heuristic, villager/caravan/convoy handling, trade agreement/vote or economic effect, custom territory grid/overlay, automatic closure, save schema, or complete kingdom-border behavior is included. The approximate land-control-zone policy remains pending. Do not deploy or launch as part of this package update.
