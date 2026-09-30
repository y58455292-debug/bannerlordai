# THEMATRIX - Kingdom Border Foundation

**Checkpoint state: BOUNDED LORD-PARTY DESTINATION FILTER IMPLEMENTED; focused suites and installed-game Release compile pass. Runtime behavior is not tested.**

## Direction and bounded scope

The user selected kingdom-wide borders as the first new behavior direction. Bannerlord's current settlement owner and a clan's current kingdom affiliation are distinct live facts. An isolated fief captured inside another kingdom remains owned by its actual current kingdom; proximity, majority ownership, centroids, and contiguous-hull guesses must never rewrite it.

Foreign kingdom destinations are open by default. A border has policy effect only when one kingdom explicitly closes its border to another; this is directional. The requested effects eventually include lord parties, villagers, and caravans. Convoys are tentative and remain out of scope until a supported native actor/route seam is verified. Resource/trade friction must follow observed native flows; this checkpoint does not delete resources, impose economic penalties, declare wars, or change diplomacy.

Passage permission is separate from trade/market authorization. Domestic trade remains unrestricted; foreign trade agreements are kingdom-wide and require diplomacy votes. Vanilla income distribution to clans stays unchanged. None of trade access, diplomacy votes, or income flows is implemented here. Longer-term direction includes traceable food/money scarcity, aid or neglect affecting loyalty, and memories of war, hunger, fiefs, family/property loss, and relationships influencing loyalty or defection. Existing memory supports raids, capture, intentional mercy, and siege-fief loss followed by natural departure; target switching is implemented but runtime-unproven. Hunger, poverty, aid, and family death are not wired to loyalty. All economy/memory work is future scope only.

The pure `KingdomBorderPolicy` classifies one already-known settlement destination from supplied live kingdom/owner facts. A narrowly configured, directional, default-open rule now removes explicitly closed foreign settlements from Bannerlord's existing native visit-candidate list for ordinary AI lord parties. The module-local `Data/KingdomBorderClosures.cfg` is an optional developer/demo input; it is empty by default and is not a diplomacy UI, vote, or persisted campaign policy. Each line means `closed territory kingdom ID > excluded visitor kingdom ID`. The file is read once when the campaign behavior registers; edit requires restarting/reloading the campaign so registration runs again. Current party and settlement kingdom ownership is read live on candidate-list construction. Sea/open water remain unclassified. No global scan, per-tick IO, save schema, new AI action, or direct movement call was added. Candidate-check and filtered-candidate counters are included in the existing on-demand diagnostic snapshot.

## Native boundary and limits

The installed Bannerlord build is v1.5.3; the existing native source receipt identifies `TaleWorlds.CampaignSystem.dll` SHA-256 `5B23C3E36D7A5D6D47C47EAB075E9E2B1CC8D1AA53C79E135FA2B5EF434EBC5F`. Existing project code reads native `Settlement.OwnerClan` and `Settlement.MapFaction`; the policy consumes current identifiers and does not cache them, so settlement capture or clan kingdom change is reflected on the next call.

Focused external research found native `MapFaction`, `OwnerClan`, navigation-face and local-locatable APIs, but no documented political-border polygon or closed-border access API. The research references are version-limited: [Settlement API v1.2.12](https://apidoc.bannerlord.com/v/1.2.12/class_tale_worlds_1_1_campaign_system_1_1_settlements_1_1_settlement.html), [MapScene API v1.3.4](https://apidoc.bannerlord.com/v/1.3.4/class_sand_box_1_1_map_scene.html), and [PartyThinkParams API v1.3.4](https://apidoc.bannerlord.com/v/1.3.4/class_tale_worlds_1_1_campaign_system_1_1_party_think_params.html). Their signatures and semantics have not been assumed for installed v1.5.3.

The existing native visit-candidate seam can remove a settlement destination from lord-party consideration; it cannot establish the territory a path crosses. This is destination access filtering, not border-frontier or route-crossing enforcement. The filter excludes armies, caravans, player main party, non-lords, inactive/disabled/stopped/attached/battle/sea parties, leaderless or waiting-to-disband parties, independent clans, and unresolved affiliations. NPC-controlled secondary parties in the player clan kingdom are included. A party at war with the destination kingdom bypasses the closure; war with an unrelated kingdom does not. Villagers/caravans need their actual owner/home affiliation resolved by actor type; a party leader lookup is not sufficient. Already-inside/exit cases, alliance rules, convoy handling, and routes through third-party holdings are not enforced. Distant open sea does not inherit nearest-land sovereignty. No stuck loops or teleport behavior was added. Existing current routes are not forcibly cancelled, so config changes affect candidate construction only after reload.

The native visit scorer's retained v1.5.3 source sorts and iterates the candidate list; an empty list avoids a GoToSettlement score without crashing that loop, but no alternate action is guaranteed. Other native scores may still be selected. This implementation adds no fallback, hold, or retarget rule, and there is no end-to-end no-stall runtime proof. A kingdom with no available settlements may therefore have no visit candidate; campaign-level liveness remains unverified.

The separate exact-installed v1.5.3 safety inspection verified `EnterSettlementAction.ApplyForParty` can merge armies, change sea/current-settlement state, and fire lifecycle events; call sites include ordinary arrival plus fleeing, disabled-AI, attached-army, and party creation/spawn paths. A global arrival veto would be unsafe, and returning false without changing a target risks repeated retries or stranded parties. No entry patch was added. The chosen upstream candidate-list filter avoids these mutation and lifecycle paths, while still making no claim about physical transit.

## Deterministic policy coverage

`Tests/KingdomBorders` covers same kingdom, foreign-open default, explicit directional closure, isolated enclave ownership, live capture/owner change, independent owner, missing owner, independent actor, sea/open-water unknown, and null kingdom identity. Result: **14 PASS**. `Tests/LivingWorld/HomeAssignmentRuntime` compiles and exercises the production closure parser/policy (not a stub): **98 PASS**, including default-open, malformed reset, reverse direction, live ownership/capture, destination-specific war exemption, player-secondary parties, army/sea/leaderless/waiting-disband exclusions, unchanged current route target, native candidate removal and empty-list counter.

Preservation suites were rerun with the installed .NET 11 preview as a compatible execution target: Home Assignment persistence/policy **43 PASS** and Phase 3 Home Responsibility policy **10 PASS**. These deterministic tests do not prove in-game behavior. Home Assignment eligibility and its 1.25 factor are unchanged.

The source compiles against the installed Bannerlord references as a `netstandard2.0` Release build: **0 errors, 1 inherited `System.ValueTuple` MSB3277 warning**. Build command used `-p:InformationalVersion=v0.1.0-THEMATRIX-dev`. Release/package `ClanAI.dll` SHA-256: `F85382892B293FFABF78B57923A152735E812A033FE9593DEC9B7AEEBE88C8F9`. This is a development package, not deployed or runtime-tested. The installed C2 DLL remains `B3760F027476C89555414C38D94CECB47C4AC0D08EA2B7E16E594BA516DA9563`.

No in-game runtime border behavior, route observation, villager/caravan behavior, resource-flow effect, diplomacy-vote integration, or save/reload behavior has been tested. The corrected package ZIP SHA-256 is `E9A8CDDB4A8217CCA30E208A61E4172E45B1823752A0F8865DB2D42FD5F13612`; its Release DLL bytes remain unchanged. The current developer config can demonstrate destination filtering only after installed/runtime use, which has not happened.

## Branding and compatibility

Public module display text is `THEMATRIX`. `SubModule.xml` internal `<Id>ClanAI</Id>`, DLL/assembly/namespace/class names, save keys/schema, and installed module directory remain unchanged. The frozen RC1 package/archive was not edited.

## Next bounded work

1. Verify a native route/entry seam with callsite and loop-safety evidence; destination filtering must not be described as route enforcement.
2. Resolve native actor affiliation for villagers and caravans, plus safe exceptions for war/allied/independent parties, already-inside actors, exits, and transit.
3. Add an in-game bounded demonstration of the existing lord-party destination filter only after a coordinated deployment; observe no stuck loops and preserve save safety.
4. Then determine whether native APIs support genuine route/frontier enforcement without direct movement hacks; keep sea/open water unknown until supported ownership semantics are proven.
5. Treat convoys and downstream trade/resource effects as later work. Foreign trade agreements require diplomacy votes; vanilla clan income remains unchanged.

**Border crossing behavior: NOT IMPLEMENTED. THEMATRIX v0.1 limited developer demo: settlement visit-destination filter only; in-game behavior and no-stall fallback remain unverified.**

Validation receipt: [THEMATRIX v0.1 closed-border destination-filter validation](evidence/thematrix_v0_1_border_destination_filter_20260930.txt). Package diagnostic-access correction: [runbook correction receipt](evidence/thematrix_v0_1_diagnostics_runbook_20260930.txt).
