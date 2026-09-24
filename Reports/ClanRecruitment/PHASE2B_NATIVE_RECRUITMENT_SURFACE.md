# Phase 2B — native ruler recruitment surface

Date: 2026-09-24

## Installed Bannerlord surface

The supported Bannerlord build already contains a non-defection clan join path in `DiplomaticBartersBehavior`.

On `DailyTickClan`, an eligible non-player clan can naturally sample a kingdom, with same-culture kingdoms weighted more heavily. The path rejects eliminated kingdoms, hostile/constant-war targets, unsafe active map-event state, and clans still inside their faction-change cooldown. A normal clan that is already a non-mercenary vassal is not eligible for this ordinary join branch.

For a non-minor eligible clan, Bannerlord calls:

`ConsiderClanJoin(clan, kingdom)`

That method constructs:

`new JoinKingdomAsClanBarterable(clan.Leader, kingdom)`

and only sends the barter to the AI barter manager when:

`GetValueForFaction(clan) + GetValueForFaction(kingdom) > 0`

Execution then remains:

`Campaign.Current.BarterManager.ExecuteAiBarter(...)`
## Native acceptance and desirability

The clan side of `JoinKingdomAsClanBarterable` uses:

`DiplomacyModel.GetScoreOfClanToJoinKingdom(clan, targetKingdom)`

For an independent clan, this is the clan's native acceptance/value surface. Relevant native ingredients include relation with the target ruling clan, average relation with target clans, culture, the clan's settlements, war-party capacity, and the target kingdom's settlement/party structure.

The kingdom side uses:

`DiplomacyModel.GetScoreOfKingdomToGetClan(targetKingdom, clan)`

Its installed formula already rewards useful recruits through native variables including:

- relation between ruling clan and candidate clan;
- same culture;
- candidate current strength;
- candidate war-party capacity;
- target kingdom power ratio versus enemies;
- candidate leader reliability;
- candidate settlement value for the target kingdom.

Thus Phase 2B does not need an invented replacement utility score to recognize military usefulness.
## Native final authority

For a normal non-defecting clan join, `JoinKingdomAsClanBarterable.Apply()` ultimately calls:

`ChangeKingdomAction.ApplyByJoinToKingdom(clan, TargetKingdom)`

ClanAI does not need to call that action directly.

The existing vanilla daily join trigger is **clan-driven**: the candidate clan gets the daily tick and samples a kingdom. The installed `DiplomaticBartersBehavior` does not expose an equivalent ruler-driven scan in this path.

That gap is the Phase 2B opportunity: let an NPC ruler actively identify a useful eligible independent clan, while reusing the exact native join barterable, both native faction values, AI barter resolution, and final native join action.

## Design constraint

The first ruler-courtship implementation should not override clan acceptance or directly transfer the clan.

A safe structure is:

1. ruler side identifies an eligible independent candidate using native kingdom desirability;
2. candidate's native clan-side join value is still evaluated;
3. no artificial relation/memory/score edit is injected merely to obtain acceptance;
4. native combined value determines whether a barter is worth attempting;
5. `ExecuteAiBarter` remains the only executor;
6. post-state determines whether recruitment actually committed.

This surface can later incorporate proven ClanAI context such as war strain or border need, but the first candidate should establish the native ruler-initiated loop before adding extra weighting.
