# Codex Status

## Current checkpoint

Phase 4B has begun with the required **offline Local Manpower native-capability/design audit**. The audit passes: Bannerlord exposes a clean shared volunteer-supply seam through the selected `VolunteerModel`. No Phase 4B gameplay code is implemented or wired in this checkpoint, no campaign was launched, and no DLL was deployed.

Phase 4A remains closed and unchanged. Its accepted evidence is now design input, not a reason to rerun recovery characterization.

## Selected Phase 4B-v1 seam

Native `RecruitmentCampaignBehavior` performs notable volunteer production once per settlement daily and calls `VolunteerModel.GetDailyVolunteerProductionProbability` for each of six notable volunteer slots.

The proposed implementation is a **delegating VolunteerModel wrapper** around the currently selected native/model instance. It will delegate every native method unchanged except the daily production probability.

Only **empty volunteer slots** are eligible for Phase 4B scaling. Occupied slots return the exact native probability so native volunteer upgrading/tier progression remains unchanged for Phase 4C.

The first candidate local multiplier uses only:

- native town prosperity level or village hearth/prosperity level;
- native local/bound-town security;
- active raid/siege as an acute disruption.

Candidate bounds:

- population: High 1.00, Mid 0.95, Low 0.80;
- security: 0.80 at security 0, rising to 1.00 by native midpoint 50;
- active raid/siege: 0.50;
- final local multiplier clamped to 0.35..1.00;
- final probability never exceeds the selected native result.

Healthy secure territory therefore stays at vanilla refill rate; damaged territory recovers more slowly but never stops.

## Native pipeline / parity

The same six notable `VolunteerTypes` slots are consumed by:

- the native player recruitment UI;
- ordinary AI lord settlement recruitment;
- garrison auto recruitment.

Therefore the proposed local supply ecology naturally affects player and AI supply through shared native state without direct pool mutation.

Separate native paths remain separate: minor-faction map recruitment, Phase 4A post-defeat initial troops, base garrison growth, mercenary stocks, prisoners and other recovery systems.

## Explicit v1 exclusions

Do not directly use loyalty, militia, garrison strength, recent-battle history, explicit raid history, war duration, recruitment-pressure history, peace timers, culture/tier modifiers, political memory or Home Responsibility state in the first policy.

Kingdom War Strain is also deferred from v1. The repository already has an AI-only `WarStrainRecruitmentPatch` with `rate=max(0.60,1-0.40*strain)`; adding shared strain production suppression now would double-suppress AI recruitment. A future bounded migration can retire that AI-only throttle and move a modest strain factor into shared production if desired.

## Native binaries

Offline audit relied on:

- `TaleWorlds.CampaignSystem.dll` SHA-256 `5B23C3E36D7A5D6D47C47EAB075E9E2B1CC8D1AA53C79E135FA2B5EF434EBC5F`
- `TaleWorlds.CampaignSystem.ViewModelCollection.dll` SHA-256 `76279E43C1E27B86BD0E1AA35AA407345725C7B208AC170E0EE0174C1A7ADF2A`

See `Reports/Manpower/PHASE4B_LOCAL_MANPOWER_NATIVE_CAPABILITY_AUDIT.md` and its evidence file.

## Next bounded milestone

Implement **offline only**:

- pure local-manpower probability policy;
- delegating `VolunteerModel` wrapper;
- deterministic tests;
- no-mutation and standalone invariants.

Do not launch a campaign or deploy the Phase 4B model until that offline implementation is separately validated. Phase 4C is not started.

Final product direction remains a standalone, installable, offline Bannerlord mod with no runtime dependency on ChatGPT, Codex, Desktop Commander, TestRunner, watchdogs, external IO or development-machine absolute paths.
