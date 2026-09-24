# Clan loyalty / defection evidence — 2026-09-23

This report consolidates the Social 5B clan-loyalty work that previously lived across staging folders, logs, and Longitudinal validation output. It records both positive causal results and null results. No player persuasion, console faction transfer, forced relation edit, or synthetic clan defection is counted as proof.

## Native decision surfaces

Bannerlord's autonomous daily diplomacy behavior exposes two relevant paths:

- \`DiplomaticBartersBehavior.ConsiderDefection(Clan, Kingdom)\` creates a \`JoinKingdomAsClanBarterable\` and lets the native barter system decide whether a clan changes kingdoms.
- \`DiplomaticBartersBehavior.ConsiderClanLeaveKingdom(Clan)\` creates a \`LeaveKingdomAsClanBarterable\`; the native action is applied only when the adjusted clan-side value is greater than zero.

ClanAI patches only the clan-side barter value while those native AI consideration methods are active. Eligibility, daily sampling, target selection, affordability, and the final \`ChangeKingdomAction\` remain Bannerlord-owned.

## Switch-kingdom result (L2)

\`v0.21L2-social-defection-clan-memory\` proved that persisted social memory can change a real AI clan's native defection value. Across the extended natural run there were 18 native defection considerations, four non-zero memory modifications, zero threshold flips, and zero committed defections.

Example: dey Rothad -> Calradian Empire had native clan value -1,261,909 and target value 54,359. Two negative clan social records toward the target empire produced a -1,638 modifier, moving the combined sum from -1,207,550 to -1,209,188. The decision remained no.

The closest native switch sample observed in that run was still about 448,208 below the >0 boundary. The cap was not increased to manufacture a result.

## Leave-current-kingdom result (L5)

\`v0.21L5-social-loyalty-direct-loss\` added exact-clan holding-loss memory. Only a real \`OnSettlementOwnerChangedEvent\` with \`BySiege\`, where the old owning clan actually loses a town or castle to another faction, can create the record.

Natural causal chain:

1. fen Morcar lost **Rhemtoil Castle** in a real siege.
2. The loss was recorded against fen Morcar specifically.
3. 317.338 campaign hours later Bannerlord naturally called the clan's leave-kingdom consideration.
4. The direct-loss memory changed the native leave score by +108:
   - native: -3,955,367
   - adjusted: -3,955,259
5. \`nativeWouldLeave=False\`, \`adjustedWouldLeave=False\`, \`committed=False\`.

This proved real-event -> memory -> native loyalty-score causality, but also exposed a magnitude problem: a flat loss index was far too small relative to Bannerlord's native score scale.

## Value-scaled result (L6)

\`v0.21L6-social-loyalty-value-loss\` replaced the flat town/castle weight with Bannerlord's native settlement-value units and added persistence.

For a direct clan-owned fief loss:

\`raw_loss_pressure = native_settlement_value * 0.25\`

Each loss decays linearly over 720 campaign hours (30 days). Multiple direct losses for the same clan accumulate. Aggregate direct-loss pressure is capped at 750,000.

At the loyalty hook, the direct-loss contribution is additionally bounded by:

\`direct_loss_cap = min(750000, max(75000, abs(native_leave_value) * 1.25))\`

\`direct_loss_modifier = min(decayed_direct_loss_pressure, direct_loss_cap)\`

This direct-loss modifier is then added to the separate small social-memory modifier.

The ordinary social-memory component remains bounded independently:

\`social_index = grievance*0.50 + bloodDebt*0.70 + tension*0.25 - trust*0.35 - obligation*0.45\`

Leader-direct memory and clan aggregate memory are blended 65% / 35% when both exist. \`social_index\` is clamped to [-100, 100].

\`social_cap = min(25000, max(5000, abs(native_leave_value) * 0.25))\`

\`social_modifier = social_cap * social_index / 100\`

The final patched value is:

\`adjusted_leave_value = native_leave_value + social_modifier + direct_loss_modifier\`

Natural L6 causal chain:

1. fen Morcar lost **Nevyansk Castle** in a real siege.
2. Bannerlord valued the lost holding at 532,054.6 native settlement-value units.
3. L6 recorded pressure-at-loss of 133,013.7.
4. The memory was saved with \`records=1\` and reloaded with \`records=1\`.
5. 50.017 campaign hours after the loss, the decayed direct-loss pressure was 123,773.5.
6. Bannerlord naturally evaluated fen Morcar's loyalty:
   - native leave value: -2,298,743
   - direct-loss modifier: +123,773
   - adjusted leave value: -2,174,970
7. \`nativeWouldLeave=False\`, \`adjustedWouldLeave=False\`, \`committed=False\`.

The L6 DLL used for this proof was \`v0.21L6-social-loyalty-value-loss\`, SHA-256 \`5A8FA3E07471D31FDC046E4301ACD09B2D59C4785850E9643B03169F898D2D00\`.

## Null results and current limit

No clan has yet left a kingdom because of this system.

In an earlier natural loyalty sample, 60 native leave considerations produced zero native leave decisions, zero adjusted leave decisions, and zero commits. The least-loyal sampled clan was still approximately 191,000 points below the >0 leave threshold. fen Morcar has repeatedly been millions of points below zero even after losing holdings.

This means the authority path and causal memory path are demonstrated, but the behavioral boundary is not yet demonstrated. The system should not be described as "autonomous defections working" until a natural event causes an adjusted score to cross >0 and Bannerlord's own \`ChangeKingdomAction\` commits the leave.

## Evidence files

The machine-readable evidence retained beside this report is:

- \`evidence/l2_leader_only_baseline.json\`
- \`evidence/l2_clan_memory_first_modifier.json\`
- \`evidence/l2_switch_extended.json\`
- \`evidence/l5_rhemtoil_causal.json\`
- \`evidence/l5_rhemtoil_log.txt\`
- \`evidence/l6_nevyansk_value_scaled.json\`

These replace the need to keep the corresponding Longitudinal validation dump directories in the active repository.

## Superseding Phase 1 result — 2026-09-24

The "no clan has yet left" statement above was correct for the 2026-09-23 evidence state. It is superseded by `PHASE1_BOUNDARY_RESULT.md`.

Banu Ruwaid later lost Vladiv Castle naturally by siege. At a natural Aserai leave check, its native value was -250,332; L6 applied +312,915 of decayed direct-loss pressure, producing +62,583. The native leave path reported `adjustedWouldLeave=True`, `committed=True`, and `kingdomAfter=<independent>`. A post-run read-only Inspector check showed the clan with no kingdom.

This proves the voluntary leave-current-kingdom boundary for the tested L6 path. It does not prove the separate target-kingdom switch/join boundary.
