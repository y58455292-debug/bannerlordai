# Codex Status

## Current checkpoint

Phase 4C has begun with the required **offline Troop Tiers / veteran-quality native-capability design audit**.

**Audit result: PASS — a clean shared native quality seam exists. No Phase 4C gameplay source is implemented in this checkpoint.**

Phase 4A remains closed. Phase 4B remains implemented/build-proven/runtime-observed at its accepted contract and is not retuned.

## Native quality seam

Bannerlord's daily notable volunteer update uses the same selected `VolunteerModel.GetDailyVolunteerProductionProbability` as the first gate for both empty-slot refill and occupied-slot quality progression.

For an occupied volunteer, native quality then independently requires:

- `UpgradeTargets`;
- current tier below native `MaxVolunteerTier=4`;
- a second native `log2(notable.Power/currentTier)*0.01` roll;
- native RNG selection of one direct `UpgradeTargets` target.

At most one direct native upgrade edge can occur for a slot in one daily update.

Changed volunteer slots are reordered by level/mounted weight, which tends to place higher-quality troops in later slots whose native first-gate probability is already lower.

Native quality is therefore already materially slower than raw refill.

## Selected Phase 4C-v1 architecture

Do **not** add another `VolunteerModel` wrapper.

Later extend the existing selected `LocalManpowerVolunteerModel` with:

- empty slot -> existing Phase 4B behavior unchanged;
- occupied but not native-upgrade-eligible -> exact native passthrough;
- occupied + upgrade-eligible -> separate pure Phase 4C quality policy adjusts only the first native probability gate.

Native notable power, current tier, `MaxVolunteerTier`, `UpgradeTargets`, RNG, culture/tree and actual slot mutation remain untouched.

## Candidate quality policy

Use the already-audited local context components:

- population: High 1.00, Mid 0.95, Low 0.80;
- security: existing 0.80..1.00 curve, full at 50;
- active raid/siege: 0.50.

Use a **separate quality safety floor**:

`qualityMultiplier = clamp(population * security * acute, 0.50, 1.00)`

`p_quality = clamp(p_native * qualityMultiplier, 0, 1)`

The 0.50 floor is a safety bound, not a balance claim. It is intentionally higher than Phase 4B's 0.35 floor because volunteer quality already has the slower slot-index gate plus the native notable-power/current-tier second gate.

Healthy secure territory preserves vanilla quality progression exactly.

## Other native veteran-recovery paths

Volunteer quality is not the only path.

Native quality can also recover through party XP/upgrading, daily training, battle/perk XP, garrison XP/upgrades and transfers, prisoner recruitment, mercenary pools, and post-defeat recreation.

Phase 4A proved one recreated native party naturally contained T1-T5 troops before settlement interaction.

Phase 4C-v1 therefore targets only the shared notable-volunteer quality source.

## Native binary

`TaleWorlds.CampaignSystem.dll`

SHA-256:

`5B23C3E36D7A5D6D47C47EAB075E9E2B1CC8D1AA53C79E135FA2B5EF434EBC5F`

See:

- `Reports/Manpower/PHASE4C_TROOP_QUALITY_NATIVE_CAPABILITY_AUDIT.md`
- `Reports/Manpower/evidence/phase4c_troop_quality_native_capability_audit_20260925.txt`

## Next bounded milestone

Separately implement **offline only**:

- pure Phase 4C troop-quality probability policy;
- occupied upgrade-eligible branch in the existing wrapper;
- Phase 4B empty behavior locked unchanged;
- deterministic/delegation/no-mutation/standalone tests;
- Release build.

Do not launch Bannerlord or deploy until that offline checkpoint passes.

Do not begin Phase 5.

Final product direction remains a standalone, installable, offline Bannerlord mod with no runtime dependency on ChatGPT, Codex, Desktop Commander, TestRunner, watchdogs, external IO, or development-machine absolute paths.
