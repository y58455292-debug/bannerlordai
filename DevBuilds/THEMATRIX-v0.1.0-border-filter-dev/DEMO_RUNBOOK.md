# THEMATRIX v0.1 Developer Demo Runbook

## Build identity and installation boundary

This is a developer-only experiment for closed-settlement visit-candidate filtering. It has not been deployed or verified in a campaign. Do not overwrite a running game's files. Before any future installation, confirm Bannerlord is closed, back up the exact installed `ClanAI.dll` and `SubModule.xml`, and record their hashes. Install only by copying this package's contents into a separately backed-up game `Modules/ClanAI` directory. The folder/module compatibility ID remains `ClanAI`; public display/version are `THEMATRIX v0.1.0`.

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

Use the existing on-demand diagnostic snapshot to inspect `KINGDOM_BORDER_DESTINATION_FILTER candidateChecks=... candidatesFiltered=...`. The two counters are in memory and included in the normal diagnostic snapshot; the filter adds no per-candidate file IO. A nonzero `candidatesFiltered` confirms a matching native visit candidate was removed. It does not prove the party avoided route crossing or that the campaign found another action.

If a party loses its last visit candidate, the native visit scorer produces no `GoToSettlement` score for that list. Other native behavior scores may exist, but an alternate action is not guaranteed. The retained candidate-list source loop tolerates an empty list; no end-to-end no-stall/liveness proof exists. Observe the party and stop the test if it repeatedly retries/stalls. Do not infer safe frontier enforcement from this narrow destination filter.

## Explicitly outside this demo

No route/frontier enforcement, arrival veto, direct movement, villager/caravan/convoy handling, trade agreement/vote or economic effects, custom territory map, automatic closure, save schema, or full border behavior is included. Do not claim the full kingdom-border feature is complete.
