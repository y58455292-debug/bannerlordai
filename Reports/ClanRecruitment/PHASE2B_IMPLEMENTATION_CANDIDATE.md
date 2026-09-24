# Phase 2B — ruler courtship implementation candidate

Date: 2026-09-24

Candidate: `v0.22A-ruler-courtship-native-v1`

## Result

The first ruler-initiated clan-courtship candidate is implemented and build-proven against the installed Bannerlord assemblies.

It is not yet runtime-observed or gameplay-proven.

## Behavior

Once per NPC ruling clan's daily tick, `RulerClanCourtshipBehavior`:

1. identifies eligible independent, non-player, non-minor clans;
2. mirrors the relevant native join safety gates, including faction-change cooldown, active map-event exclusion, war/constant-war exclusion, and the stranded-war check;
3. selects the candidate with the highest native `DiplomacyModel.GetScoreOfKingdomToGetClan` score;
4. constructs the ordinary non-defecting `JoinKingdomAsClanBarterable`;
5. evaluates the native clan-side and kingdom-side barter values;
6. invokes only `BarterManager.ExecuteAiBarter` when their unmodified sum is positive;
7. records the immediate native barter result and post-state.

The implementation adds no custom score, relation, memory, wealth, or faction mutation. It does not call `ChangeKingdomAction`.

## Evidence emitted

`RULER_COURTSHIP_CONSIDER` records:

- recruiter kingdom and ruler;
- eligible candidate count;
- selected clan and leader;
- pre/post kingdom;
- strength, war-party limit, and holdings count;
- culture match;
- native selection score;
- both native barter values and their sum;
- positive-surplus, attempt, acceptance, and commit state;
- cumulative consideration / positive-surplus / attempt / commit counters;
- explicit `customScore=False` and `directFactionTransfer=False` markers.

No-candidate ticks are retained as null observations.

## Build

Command:

`dotnet build src/ClanAI/src/ClanAI/ClanAI.csproj -c Release`

Result: succeeded with 0 errors and the inherited `System.ValueTuple` version-conflict warning.

## Next evidence step

After this implementation and protocol are committed, deploy the candidate only with Bannerlord stopped and a verified rollback preserved. Run `PHASE2B_RECRUITMENT_PROTOCOL.md` against a separately named test fixture. Preserve null outcomes as well as any native join commit.
