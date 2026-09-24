# Phase 2 — Bannerlord native defection score composition

Date: 2026-09-24

Source inspected: installed `TaleWorlds.CampaignSystem.dll`, type `DefaultDiplomacyModel` and barterables `JoinKingdomAsClanBarterable` / `LeaveKingdomAsClanBarterable`.

This inspection is read-only and exists to constrain the next ClanAI design.

## Clan-side defection value

For the source clan, `JoinKingdomAsClanBarterable.GetUnitValueForFaction` begins with:

`DiplomacyModel.GetScoreOfClanToJoinKingdom(clan, targetKingdom)`

If the clan already belongs to a kingdom, Bannerlord then creates a native `LeaveKingdomAsClanBarterable` and adds that clan-side leave value to the join score.

For a normal non-minor clan, `LeaveKingdomAsClanBarterable.GetUnitValueForFaction(clan)` directly returns the integer value of:

`DiplomacyModel.GetScoreOfClanToLeaveKingdom(clan, currentKingdom)`

Thus native target defection already contains an explicit **leave-current-kingdom subcomponent**.

When the target and current kingdoms are not at war, Bannerlord also subtracts the clan's current settlement value for its present kingdom from the clan-side join value (halved only for the player-led target case). This makes existing holdings a strong native reason not to defect peacefully.

The target-kingdom side is evaluated separately through:

`DiplomacyModel.GetScoreOfKingdomToGetClan(targetKingdom, clan)`

Native barter then combines the two faction values and affordability before any action can commit.

## Native join-score ingredients

`DefaultDiplomacyModel.GetScoreOfClanToJoinKingdom` includes, among other terms:

- relation with the target ruling clan;
- average relation with target kingdom clans;
- same-culture / different-culture factor;
- clan total settlement base value;
- clan settlement value evaluated for the target kingdom;
- clan war-party limit;
- target kingdom settlement value and existing non-mercenary party capacity;
- whether current and target factions are at war.

`GetScoreOfKingdomToGetClan` separately includes relation, culture, clan strength / party capacity, target power ratio, leader reliability, and a target-faction settlement-value term.

## Native action authority

For `IsDefecting=true`, `JoinKingdomAsClanBarterable.Apply` ultimately calls:

`ChangeKingdomAction.ApplyByJoinToKingdomByDefection(clan, oldKingdom, targetKingdom, ...)`

ClanAI does not need to call this action directly.

## Design implication

A principled ClanAI change should mirror this native structure rather than add one large opaque defection bonus:

1. current-kingdom memory should adjust the embedded **leave** component;
2. target-specific social memory should adjust target desirability separately and remain tightly bounded;
3. Bannerlord should continue to own target selection, native join economics, affordability, barter resolution, and the final defection action.

This native composition is the reason to evaluate carrying the already-proven voluntary-leave memory adjustment into target defection. It is not authorization to copy raw loss pressure or raise the defection cap arbitrarily.
