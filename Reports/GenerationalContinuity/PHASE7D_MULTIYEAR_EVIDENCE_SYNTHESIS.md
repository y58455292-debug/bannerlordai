# Phase 7D — Multi-Year Evidence Synthesis and Remaining-Risk Audit

Date: 2026-09-25

Authoritative checkpoint: `6c01caaa2fccd8921664fb1d9c54dbab0939523a`

Scope: synthesis of already-accepted runtime reports/evidence only. No Bannerlord launch, DLL deployment, succession experiment, gameplay implementation, balance change, or Phase 8 work occurred.

## Executive finding

The repository contains two genuine long-horizon observations of the protected campaign: the Phase 7B run covered `2869.1297` campaign hours, just under five native years, and the Phase 7C-I3 run covered `1141.0417` hours, just under two native years. Both ended safely at conservative margins. Neither produced the selected rare succession/ruling-clan event.

Those runs are strong evidence against obvious short-to-medium-term instability in the observed campaign and against unbounded dynasty-episode duplication. They are not comprehensive balance tests for every Phase 4–6 subsystem because those systems' accepted runtime proofs used narrower, purpose-built windows and did not maintain full longitudinal population metrics.

No accepted report shows runaway collapse, permanent manpower failure, uncontrolled bandit growth, succession/persistence corruption, duplicate records after load, or obvious save/load instability. Broken long-term decay and long-run equilibrium remain incompletely measured where the relevant telemetry was absent. Missing measurements are retained as unknowns, not converted into passes.

## Evidence windows

| System | Longest relevant accepted runtime window | Stable/proven behavior | Still unproven | Dedicated Phase 7 runtime test justified? |
|---|---:|---|---|---|
| Social/loyalty memory | Phase 7B: `2869.1297 h` (almost five native years); earlier natural loyalty boundary proof | Social episode total changed from `195` at hour `649491.2704` to `382` at hour `652066.4598`, a finite increase of 187 across roughly 4.47 native years. Actor-key resolution stayed on the requested hero in emitted checks; no cross-hero influence application or telemetry error was detected. Earlier natural fief-loss pressure visibly decayed (`450330.6` initial to `345507.6`) and still produced a bounded native leave crossing. | The five-year run did not expose per-record pressure trajectories or a complete final ledger census. It does not prove every decay path, memory cap, or inheritance boundary. No successor existed to exercise post-death personal-memory isolation. | **No.** The remaining successor boundary depends on the same rare event already bounded by Phase 7B, while growth/decay hardening belongs in release/balance work with purpose-built metrics. |
| WarState / WarScar | Phase 7B: `2869.1297 h`, but only the initial structural read was available | Initial read recovered 18 WarScars, one active siege, 20 raid records, two loss records, two objectives, and seven strain records. The campaign continued nearly five years without a reported WarState exception or an accepted report of world collapse attributable to these records. | Preflight snapshots reported WarState fields as unavailable, so no valid end-to-end count/decay comparison exists. WarScar decay, pruning, and objective retirement over years are therefore **not proven**. | **No additional Phase 7 test.** This is a telemetry/long-run balance gap, not a generational-continuity event gap; carry it into Phase 8 hardening rather than reopening Phase 7. |
| Kingdom continuity | Phase 7C-I3: `1141.0417 h` (almost two native years); Phase 7B: `2869.1297 h`; Phase 2D-L1 guarded reload | Phase 2D-L1 restored exactly seven records, reconciled seven active kingdoms, emitted no duplicate succession/destruction notice, and performed no political mutation. Phase 7C-I3 still reported seven ledger entries with succession ordinal zero and no event/telemetry error. | No natural kingdom creation, destruction, or ruling-clan transition was captured in the accepted bounded windows. Exactly-once post-event persistence remains unexercised. | **No.** Five-year and two-year natural-event bounds already constrain this rarity; another hunt would repeat the same null-seeking experiment. |
| Local Manpower | Phase 4A recovery: `39.6234 h`; Phase 4B mechanism proof: `0.4154 h`; supporting Phase 4C shared-pool observation: about `1.07 h` | Natural severe party recovery was observed (`37 -> 69` troops), settlement recruitment matched volunteer-pool reductions, and a garrison withdrawal pattern was observed. Phase 4B preserved healthy/occupied passthrough, applied a natural Mid-population slowdown, and Bannerlord filled the slot. Phase 4C observed native AI shared-pool consumption. These observations argue against an immediate or structurally permanent manpower failure. | Low-population, low-security, raid/siege factors and long-run volunteer equilibrium are not runtime-balanced. Permanent failure is not proven impossible over years. | **No Phase 7 test.** Long-run manpower equilibrium is a Phase 8 balance-hardening concern; Phase 4 tuning must not be reopened here. |
| Troop Quality | Phase 4C: `1.0718 h` | Healthy passthrough, natural degraded slowdown, non-upgradeable passthrough, native eligibility, native direct-target upgrade, and Phase 4B empty-slot preservation were observed. Bannerlord retained troop mutation authority. | Multi-year tier distribution and recovery equilibrium are unmeasured; balance remains explicitly unproven. | **No Phase 7 test.** Carry the acknowledged balance gap to Phase 8 rather than retuning Phase 4C. |
| Local Bandit Control | Phase 5: `87.9248 h`; minimum proof completed at `19.5267 h` | Secure, weak, near-neutral, village-bound-security, and hideout passthrough paths ran naturally. Bannerlord created the observed looter party; ClanAI only adjusted bounded relative weight. No telemetry error or direct party mutation occurred. | The run did not measure total bandit density or equilibrium, so uncontrolled multi-year growth is neither observed nor ruled out by a population series. | **No Phase 7 test.** Population equilibrium is a Phase 8 balance-hardening metric; the Phase 5 policy should not be reopened here. |
| Phase 6 civic project choice | `155.7880 h` | Selected wrapper and audited inner model executed; 12 evaluations and 12 native commits were correlated; two NPC-town cases preserved native choices; zero telemetry errors or direct mutation occurred. | The naturally rare low-loyalty, idle-town Festival substitution did not occur. Player legibility and substituted native commit remain unproven. | **No.** This is an accepted bounded natural null. Do not retune or repeat the Festival hunt. |
| Phase 7 succession/history | Phase 7B: `2869.1297 h`; Phase 7C-I3: `1141.0417 h` | Across Phase 7B, Nemos remained alive/ruler and no cross-hero personal-memory application was detected in emitted telemetry. Phase 7C-I3 retained exactly two personal episode rows, zero structural rows, and duplicate/rejected counters of zero. Offline I1/I3 tests prove scope isolation, structural provenance, semantic deduplication, D1/D2 compatibility, and no political mutation. | No qualifying player succession or ruling-clan change occurred, so structural writer execution, cross-heir history visibility, and post-event save/reload deduplication remain runtime-unproven. | **No further hunt.** Both rare-event searches reached their authorized bounds without an event; deterministic semantics and existing persistence evidence are sufficient for roadmap characterization. |

## Risk questions

### Runaway collapse

No accepted evidence reports a campaign-wide collapse attributable to ClanAI during the almost-five-year Phase 7B run or the almost-two-year Phase 7C-I3 run. However, those runs did not collect a complete economy, settlement, party-population, or faction-survival time series. Result: **no sign observed; not a comprehensive balance proof**.

### Runaway memory growth

The strongest comparable metric is social episodes: `195 -> 382` over roughly 2575 campaign hours between the initial and last detailed Phase 7B ruler snapshots. This is finite continued accumulation, not evidence of explosive growth. Dynasty episodes remained at two in the later Phase 7C-I3 baseline and structural rows remained zero. Result: **no runaway growth observed; complete per-store caps/pruning remain uncharacterized**.

### Broken decay

The natural loyalty proof showed direct-loss pressure decreasing before its boundary crossing, so the active decay path operated. Phase 7B did not emit comparable start/end values for all memories or WarScars. Result: **decay operation proven in the loyalty path; multi-year decay completeness unproven**.

### Permanent manpower failure

Natural recovery, volunteer filling, native upgrading, and AI shared-pool consumption were all observed. Result: **no immediate permanent failure signal**, but no multi-year volunteer/population equilibrium proof.

### Uncontrolled bandit growth

Phase 5 proved bounded local weighting and native creation authority, not density equilibrium. No accepted long-horizon report identifies uncontrolled bandit growth. Result: **no sign reported; long-run population balance unmeasured**.

### Succession or persistence corruption

Phase 2D-L1 restored seven continuity records exactly with no duplicate notices or political mutation. Phase 7B/I3 produced no qualifying succession event, so post-succession continuity could not be exercised. Result: **ordinary continuity reload proven clean; post-succession persistence remains an event-dependent null**.

### Duplicate records after load

Phase 2D-L1 found no duplicate lifecycle callback/notice after reload. Phase 7C-I3 ended with `_duplicates=0`, `_rejected=0`, but no structural event or post-event reload occurred. Result: **no duplicate evidence observed; structural post-event duplicate suppression remains deterministic-test-proven only**.

### Save/load instability

The early Phase 2D save-materialization blocker was resolved; the uniquely named save then materialized and directly reloaded to `campaignReady=True`, restoring seven records. Protected fixtures remained unchanged in all bounded no-save runs. Result: **no outstanding ordinary save/load instability in accepted evidence**.

## Remaining-risk disposition

The unresolved items divide into two categories:

1. rare native-event boundaries already subjected to long bounded searches (player succession, ruling-clan transition, low-loyalty Festival substitution); and
2. multi-year balance/equilibrium metrics not instrumented by their original mechanism proofs (WarScar retirement, manpower distribution, troop tiers, and bandit density).

Repeating a Phase 7 succession/ruling-clan hunt would spend another long run on the same low-incidence event without addressing the broader balance metrics. Forcing the event would violate native authority. The second category is better handled as explicit Phase 8 release/balance hardening with selected longitudinal metrics, not by reopening completed Phase 4–6 tuning inside Phase 7.

## Status

- Phase 7D synthesis: **COMPLETE**
- New gameplay implementation: **NO**
- New runtime experiment performed: **NO**
- Next step: **Phase 8**

## Recommendation

**B. Phase 7 is sufficiently characterized for roadmap purposes; preserve the remaining natural-event nulls, and make the next milestone Phase 8 release/balance hardening.**

