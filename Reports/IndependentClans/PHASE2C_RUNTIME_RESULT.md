# Phase 2C runtime result

Date: 2026-09-24

## Result

**PASS at the supported native boundary.** Banu Ruwaid remained an independent map faction for a bounded 81.003 campaign-hour observation. Its two native lord parties survived, moved between settlements, fought bandits, replenished food, and increased their combined troop count from 195 to 205. No custom spawn, recruitment, ownership, faction-transfer, or military score was applied.

Together with the already-proven ruler-courtship result, this demonstrates the Phase 2C loop available on the supported Bannerlord build: a clan can leave into independence, remain active and recover, receive independent evaluations from several kingdoms, reject negative native offers, and later join on a positive native barter.

## Isolation

The source fixture was `ClanAI V020V PERSIST PHASE1 BOUNDARY WIN BANU RUWAID 20260924`, where Banu Ruwaid's independent post-state was already proven.

The preserved pre-Phase-2B runtime DLL was temporarily deployed so the v0.22A daily ruler-courtship behavior could not end the independent observation early. This changed no save or repository state. The run ended with TestRunner `EXIT_NOSAVE`, and the current v0.22A live DLL was restored with SHA-256:

`E6AF9EC037C61E8EB094A5AFB8CD9BB737B0862D089C6ABC5D7051A46431CA9F`

## Initial state

Campaign hour: `649333.80161141662`

- clan leader Taq: faction `Banu Ruwaid`, kingdom null, alive, not prisoner;
- Dhila Party: 106 healthy troops, 24 food days, moving, target Razih, last visited Akiser;
- Shatha Party: 89 healthy troops, 20 food days, moving, target Amprela, last visited Skarthness;
- combined troops: 195.

## Observed native activity

During the run:

- Shatha entered/left Troverag and Gauksdal through ordinary settlement behavior;
- Dhila entered/left Onira and replenished food;
- Dhila initiated and won a native field battle against Looters;
- both parties continued to receive ordinary engage/flee evaluations against bandits and deserters;
- neither party was in an army, disbanding, besieging, or granted a holding.

## Final state

Campaign hour: `649414.80480625` (elapsed `81.00319483363` hours)

- Taq remained alive, not prisoner, and in the independent `Banu Ruwaid` faction;
- Dhila Party: 109 healthy troops, 8 prisoners, 30 food days, moving, last visited Onira, target Iyakis;
- Shatha Party: 96 healthy troops, 16 food days, moving, last visited Gauksdal, target Danustica;
- combined troops: 205, a net increase of 10;
- Inspector error collector: 0 total, 0 distinct, 0 fatal.

## Honest boundary

This result proves survival, ordinary settlement use, anti-bandit activity, and troop recovery while independent. It does **not** prove that ordinary independent NPC clans can raid, siege, retain a voluntarily surrendered holding, or create a kingdom. The native audit found those paths unavailable or absent for this actor type. Phase 2C therefore closes at the native boundary rather than manufacturing territory or a successor state with direct actions.

Long-horizon successor political entities remain Phase 2D architecture work.
