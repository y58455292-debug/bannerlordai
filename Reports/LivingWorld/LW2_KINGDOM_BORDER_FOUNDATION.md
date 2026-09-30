# THEMATRIX - Kingdom Border Foundation

**Checkpoint state: FOUNDATION IMPLEMENTED; 10 deterministic policy cases and installed-game Release compile pass. No runtime border behavior is implemented or claimed.**

## Direction and bounded scope

The user selected kingdom-wide borders as the first new behavior direction. Bannerlord's current settlement owner and a clan's current kingdom affiliation are distinct live facts. An isolated fief captured inside another kingdom remains owned by its actual current kingdom; proximity, majority ownership, centroids, and contiguous-hull guesses must never rewrite it.

Foreign kingdom destinations are open by default. A border has policy effect only when one kingdom explicitly closes its border to another; this is directional. The requested effects eventually include lord parties, villagers, and caravans. Convoys are tentative and remain out of scope until a supported native actor/route seam is verified. Resource/trade friction must follow observed native flows; this checkpoint does not delete resources, impose economic penalties, declare wars, or change diplomacy.

Passage permission is separate from trade/market authorization. The user affirmed domestic trade remains unrestricted and foreign trade requires a kingdom-wide ruler-controlled agreement policy. Whether rulers control only treaties or also all income remains unresolved. Open movement does not grant foreign trade access. Longer-term direction includes traceable food/money scarcity, aid or neglect affecting loyalty, and memories of war, hunger, fiefs, family/property loss, and relationships influencing loyalty or defection. Existing memory supports raids, capture, intentional mercy, and siege-fief loss followed by natural departure; target switching is implemented but runtime-unproven. Hunger, poverty, aid, and family death are not wired to loyalty. All economy/memory work is future scope only.

The pure `KingdomBorderPolicy` classifies one already-known settlement destination from supplied live kingdom/owner facts. It does not discover ownership, infer territory from nearby settlements, calculate land control zones, inspect route faces, or apply movement/action changes. Sea and open water are explicitly unclassified. It has no cache, scan, persistence, hot-path caller, or save-schema change.

## Native boundary and limits

The installed Bannerlord build is v1.5.3; the existing native source receipt identifies `TaleWorlds.CampaignSystem.dll` SHA-256 `5B23C3E36D7A5D6D47C47EAB075E9E2B1CC8D1AA53C79E135FA2B5EF434EBC5F`. Existing project code reads native `Settlement.OwnerClan` and `Settlement.MapFaction`; the policy consumes current identifiers and does not cache them, so settlement capture or clan kingdom change is reflected on the next call.

Focused external research found native `MapFaction`, `OwnerClan`, navigation-face and local-locatable APIs, but no documented political-border polygon or closed-border access API. The research references are version-limited: [Settlement API v1.2.12](https://apidoc.bannerlord.com/v/1.2.12/class_tale_worlds_1_1_campaign_system_1_1_settlements_1_1_settlement.html), [MapScene API v1.3.4](https://apidoc.bannerlord.com/v/1.3.4/class_sand_box_1_1_map_scene.html), and [PartyThinkParams API v1.3.4](https://apidoc.bannerlord.com/v/1.3.4/class_tale_worlds_1_1_campaign_system_1_1_party_think_params.html). Their signatures and semantics have not been assumed for installed v1.5.3.

The existing native visit-candidate seam can influence a settlement destination score; it cannot establish the territory a path crosses. A destination classifier or penalty therefore is not frontier enforcement. Independent clans, allied/war parties, actors already inside a closed kingdom, and legal exit/transit cases require a precise native rule before any runtime enforcement. Villagers/caravans need their actual owner/home affiliation resolved by actor type; a party leader lookup is not sufficient. Distant open sea must not inherit the nearest kingdom's sovereignty. No stuck loops or teleport behavior is authorized.

## Deterministic policy coverage

`Tests/KingdomBorders` covers same kingdom, foreign-open default, explicit directional closure, isolated enclave ownership, live capture/owner change, independent owner, missing owner, independent actor, sea/open-water unknown, and null kingdom identity. Result: **10 PASS**. The test project was restored with the task-local NuGet config after the initial attempt was denied while reading the user-level NuGet config.

Preservation suites were rerun with the installed .NET 11 preview as a compatible execution target: Home Assignment persistence/policy **43 PASS**, roster/native-adapter runtime **68 PASS**, and Phase 3 Home Responsibility policy **10 PASS**. These confirm their existing focused contracts; they do not prove any border behavior. The new file does not modify Home Assignment eligibility, its 1.25 factor, or the native visit patch.

The source also compiles against the installed Bannerlord references as a `netstandard2.0` Release build: **0 errors, 1 inherited `System.ValueTuple` MSB3277 warning**. This is a build check, not a deployment candidate or runtime result. The scratch build output `ClanAI.dll` SHA-256 is `DFCD74F2F96228CD963C0F43F37666992AA2DEFD9295217E5CA94FA0CB2F9135`; it was kept in the task-local temporary directory and was not installed. The existing deployed C2 DLL remains `B3760F027476C89555414C38D94CECB47C4AC0D08EA2B7E16E594BA516DA9563`.

This verifies only the pure classification rule. There is no runtime caller, build candidate, route observation, resource-flow evidence, closure storage, or save/reload behavior in this checkpoint.

## Branding and compatibility

Public module display text is `THEMATRIX`. `SubModule.xml` internal `<Id>ClanAI</Id>`, DLL/assembly/namespace/class names, save keys/schema, and installed module directory remain unchanged. The frozen RC1 package/archive was not edited.

## Next bounded work

1. Verify the installed v1.5.3 APIs for route/navigation faces, sea/open-water state, and actor ownership for lord parties, villagers, and caravans.
2. Select one native movement/scoring seam that can observe actual route territory rather than only destination ownership.
3. Define exceptions and safe behavior for war/allied/independent actors, already-inside actors, and exit/transit before enforcement.
4. Keep open borders as passable by default and make closures directional. Add bounded movement evidence and confirm no party stalls or teleports.
5. Treat convoy handling and downstream trade/resource pressure as later work until native paths and causal flows are proven.

**Border crossing behavior: NOT IMPLEMENTED. Kingdom-wide political-border awareness: FOUNDATION ONLY.**

Validation receipt: [lw2 kingdom border foundation validation](evidence/lw2_kingdom_border_foundation_20260930.txt).

