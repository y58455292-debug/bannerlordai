# Sol escalation request — successor-state continuity

Date: 2026-09-24

## Requested task

Design the smallest native-authority-preserving architecture for Phase 2D kingdom continuity and successor states, including the newly confirmed 28-day destruction boundary for ordinary landless independent clans.

## Why Sol is needed

The native audit now spans several coupled lifecycle systems whose interaction determines whether a proposed feature preserves or replaces Bannerlord authority:

- `FactionDiscontinuationCampaignBehavior` registers an ordinary non-player, non-minor, non-rebel, landless independent clan for destruction after 28 days;
- a landless kingdom is immediately discontinued, its clans leave by kingdom destruction, and the kingdom is destroyed;
- `DestroyKingdomAction` deactivates the kingdom and destroys remaining member clans;
- ruler death already has a native `KingSelectionKingdomDecision` / `ChangeRulingClanAction` succession path when eligible clans remain;
- rebellion creates a landed rebel clan through native settlement ownership and war actions, then matures it into an ordinary clan after 30 or 60 days if it still owns a settlement;
- ordinary independent NPC clans cannot enter native military raid/siege/defense target scoring;
- the exposed kingdom-creation eligibility model is player-oriented, and no ordinary NPC kingdom-creation loop has been identified.

These facts make a simple grace-period extension, direct settlement grant, direct kingdom creation, or direct faction transfer unsafe. A successor solution must decide where native authority should remain final, how a legitimate landed political entity can arise, what happens to existing clans and wars, how identity/name/culture persist, and how save/load compatibility is maintained. That is substantially deeper architecture reasoning and satisfies the project escalation rule.

## What Terra can still safely handle

Terra can continue bounded, read-only work while this architecture is reviewed:

- finish the native audit of kingdom naming/identity mutation and rebel-clan maturation;
- catalogue ruler succession and kingdom-destruction events already exposed to campaign behaviors;
- locate existing saves with natural rebellions, ruler changes, or kingdom collapse;
- run observation-only continuity checks;
- maintain honest documentation, build verification, and evidence packaging;
- implement a narrowly specified design after Sol resolves the ownership/lifecycle architecture.

## Constraints for the resolution

- preserve native king selection and native ownership transfer paths;
- do not raise the 28-day timer or other thresholds merely to manufacture success;
- do not directly assign settlements, kingdoms, rulers, or faction membership as evidence;
- keep culture persistent while allowing political identity to change;
- preserve null outcomes and eliminated factions;
- require normal-campaign, save/load-safe, player-legible proof.

After the architecture is resolved, return routine implementation and bounded runtime validation to Terra.
