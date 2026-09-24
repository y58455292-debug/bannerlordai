# Phase 2C native capability audit

Date: 2026-09-24

## Question

What can an ordinary non-player clan do after it leaves a kingdom, without ClanAI replacing Bannerlord's political or military authority?

## Method

This is a read-only audit of the locally installed Bannerlord `TaleWorlds.CampaignSystem.dll`, decompiled with ILSpy. It does not alter game state and is not gameplay proof. The relevant native classes were:

- `ChangeKingdomAction`
- `Clan`
- `ClanVariablesCampaignBehavior`
- `HeroSpawnCampaignBehavior`
- `AiMilitaryBehavior`
- `EmptyClanPartiesCampaignBehavior`
- `DefaultKingdomCreationModel`

## Findings

### Independence is a real native faction state

When `Clan.Kingdom` is null, the clan is its own map faction. The voluntary-leave action clears the kingdom through the native faction-change path, resets the stay-until time, makes peace with the former kingdom's enemies where permitted, and puts its war parties on hold when safe to do so.

Voluntary departure also transfers every clan settlement away through Bannerlord's `ApplyByLeaveFaction` path. A leaving clan therefore does not carry its fiefs into independence. This is a native consequence and ClanAI must not bypass it merely to manufacture an independent landed clan.

### Independent NPC clans retain native finance and party rebuilding

`ClanVariablesCampaignBehavior` continues daily finance and financial evaluation for non-player, non-bandit clans without requiring kingdom membership.

`HeroSpawnCampaignBehavior` runs `TrySpawnHeroesAndParties` on every non-eliminated non-player clan. Its party-creation score uses the native party limit, fief count, existing war-party count, clan gold, minor-faction status, and available commanders. If a party is created, Bannerlord uses `MobilePartyHelper.SpawnLordParty` and native initial-item logic. This is the ordinary independent-clan rebuild path; the player-only `EmptyClanPartiesCampaignBehavior` cache is not required for NPC recovery.

### Ordinary independent clans are excluded from native military target scoring

`AiMilitaryBehavior.AiHourlyTick` returns unless a lord party's map faction is either the player's map faction or a kingdom faction. An ordinary independent NPC clan therefore cannot use this behavior to score defending, raiding, or besieging settlements.

The deeper target code is otherwise faction-generic, but that does not make it reachable for an independent NPC clan. ClanAI should not claim that such clans can seek territory through native military AI on this supported build.

### Native political options remain available

Existing runtime evidence already proves that an independent clan can remain independent long enough to be evaluated by several kingdoms and later accept a positive native ruler barter. Phase 2B proved Banu Ruwaid was considered by Sturgia, Aserai, and Vlandia before Vlandia's native combined valuation crossed zero and the native barter committed the join.

`DefaultKingdomCreationModel` exposes the player kingdom-creation eligibility checks. This audit found no equivalent ordinary NPC-independent-clan loop that creates a kingdom. A future successor-state system must therefore be treated as a distinct design problem, not assumed to exist natively.

## Phase 2C boundary

Already established and not to be rerun:

- natural voluntary departure into independence;
- multiple ruler evaluations while independent;
- native rejection/null outcomes;
- later native recruitment for believable positive value.

Native capability established by code audit, still requiring focused runtime observation:

- continued clan finance;
- native lord-party respawn/rebuilding while independent.

Native gap:

- ordinary independent NPC clans do not enter native raid/siege/defense target scoring;
- no ordinary NPC kingdom-creation loop was identified.

## Next bounded proof

Use the existing Banu Ruwaid independent boundary save or its preserved evidence window. Observe, without forcing outcomes, whether the clan retains or respawns a lord party and whether party size/food/settlement visits change during a bounded independent interval. Preserve a null result. Do not change party limits, spawn scores, recruitment supplies, courtship valuations, or faction membership to create a positive result.

If the native party survives or rebuilds, Phase 2C can close at the supported native boundary: independent survival/recovery plus already-proven multi-kingdom courtship and later joining. Territory-seeking and NPC successor-state creation should be carried forward explicitly as native gaps for later political architecture, not simulated by direct ownership or faction-transfer actions.
