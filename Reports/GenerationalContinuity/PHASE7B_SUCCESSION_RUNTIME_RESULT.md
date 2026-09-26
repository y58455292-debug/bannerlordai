# Phase 7B — natural succession runtime result

Date: 2026-09-26 UTC (2026-09-25 America/Los_Angeles)

Source/preflight checkpoint: `e6293f191c84b8db51a343ec82c3676a962d179b`

Candidate DLL SHA-256: `B15B7B4D3F77EF40D6080EECF07B0077BA4B0E5ABD176C3C4328166BFB4A3ED0`

## Result: BOUNDED NULL

The original Bannerlord process and campaign were resumed, not restarted. Nemos remained alive and remained the ruling clan's leader through the final paused observation. No qualifying Nemos death, clan-leader succession, or Calradian Empire ruling-clan succession was observed. The run stopped at its conservative five-native-year boundary margin, exited without saving, and is complete as a bounded null. This is not a successful succession-continuity proof and is not an interrupted attempt.

No gameplay source, age, health, death chance, family, leadership, election, ownership, memory-inheritance rule, or campaign-calendar constant was changed. `DynastyBranchEpisodeMemory` was not fixed. No additional Phase 7 feature was begun.

## Original session and chronology

The original process was PID `20740`, started at `2026-09-26T00:24:26.8187938Z`. It was still the same responding process at the final resumption check. Campaign generation remained `1`, using `ClanAI V020V PERSIST DEMO GATE V021M 20260924`. The original log remained `ClanAI_20260926_002525_616_v020Q_review.log`.

| Measure | Observed value |
|---|---|
| Original campaign-hour baseline | 649491.27044636116 |
| Five-native-year maximum | 2880 campaign hours |
| Absolute campaign-hour cap | 652371.2704463612 |
| Pause acknowledgement | 652360.40016455553 |
| Elapsed original-run hours | 2869.1297181943664 |
| Remaining safety margin | 10.870281805633567 hours |
| Stop reason | `five-native-year-bound-conservative-margin` |

The earlier 10,080-hour estimate was incorrect for this loaded campaign. Read-only native calendar inspection returned 24 hours/day, 3 days/week, 2 weeks/season, and 4 seasons/year. Therefore one native year is 576 hours and five years is 2,880 hours. Native tick constants independently agreed: `TimeTicksPerYear=20736000000` and `TimeTicksPerHour=36000000`. The fixed five-year authorization was retained; its hour conversion was corrected, not extended. No calendar or aging value was written.

The original baseline and target were retained throughout. No redeployment, target reselection, telemetry reset, or preflight rerun was performed when resuming.

## Selected target

Nemos (`lord_1_44`) was selected as the oldest suitable living ruler, age `81.51159`, leading `clan_empire_west_1` in the surviving `calradian_empire` kingdom, culture `empire`. The identically associated eliminated `empire_w` kingdom was not used as the target kingdom.

His recorded clan settlements were `town_EW2`, `castle_EW3`, `castle_village_EW3_1`, `castle_village_EW3_2`, `village_EW2_2`, `village_EW2_3`, `village_EW2_4`, `town_EW3`, `village_EW3_2`, and `village_EW3_3`. Spouse/family ID was `lord_1_48_3`; no children were reported.

At selection, kingdom-continuity succession count was 0; clan holding-loss count 0; NobleMemory count 0; SocialLedger actor count 1; companion duty count 0; companion experience count 4; negative-outcome count 1; dynasty actor episode count 0; total dynasty episodes 2; and total social episodes 195. The recovered read-only structural snapshot recorded 18 WarScars and two objectives (`battania|calradian_empire`, `calradian_empire|vlandia`), plus siege, raid, loss, and strain IDs preserved in the evidence.

Final paused live reads again returned kingdom `calradian_empire`, `IsEliminated=false`, ruling clan `clan_empire_west_1`, leader `lord_1_44`, `IsDead=false`, and Nemos age `86.4927139`.

## What the evidence does and does not prove

The original session log contains 35 ruler snapshots, 615 hero-memory-resolution lines, 548 native hero-death event lines (before/after records, not 548 distinct deaths), and 29 native clan-leader-change lines. None of the death/change lines matched the selected Nemos succession. No native ruling-clan-change line for the selected kingdom was found. Other clans' events were not substituted for the selected test.

The last Nemos snapshot, at hour `652066.45982325`, still showed him alive and leading the same clan/kingdom, the same recorded settlement/family set, and succession count 0. Its `stage=clan-leader-change` label resulted from an event elsewhere in that kingdom; the label alone is not evidence that Nemos was replaced.

No cross-hero or deceased-key application was reported among the emitted identity-resolution checks; no `GENCONT_PREFLIGHT_ERROR` line was found. This means **none detected in the available telemetry**, not proof of all inheritance paths. There was no successor on whom to exercise the required post-death boundary.

The preflight ruler snapshots continued to report unavailable WarState fields (`-1`/`<none>`). The separately recovered selection snapshot supplies the initial structural IDs/counts without changing the candidate. This limitation is retained, not silently converted into a structural persistence PASS. No telemetry re-audit or fix was performed during resumption.

No post-succession save or reload occurred. The initial fixture load's `stage=post-reload` label is not a guarded post-succession reload. Deceased-state persistence, successor continuity, exactly-once succession increment, post-reload duplicate suppression, and structural/person-memory continuity across succession remain unproven.

## Safety and cleanup

The fresh command chronology contains one source-fixture load, time-control/escape-menu commands, pause, and `EXIT_NOSAVE`; no save command or second load occurred. Pause was acknowledged at `2026-09-26T01:15:19.8549387Z`. `EXIT_NOSAVE` was acknowledged at `2026-09-26T01:15:42.6623801Z`. Bannerlord was verified closed afterward.

Protected fixture SHA-256 remained `A91F15BF1403F1D29F942C56A1162F431113942CDACCAFE52F4D80303B4CB427`; its timestamp remained `2026-09-24T17:20:12.2384633Z`. Rollback SHA-256 remained `2909ADE31B427E122FDDE539229BD0A4A2B4FA91C84CFD3AB63D0C8EB6F729D7`. The installed DLL still matched the exact preflight candidate. Strategic Commitment remained `Mode=Observe`; the Visual War activation marker remained absent.

This checkpoint changes only report/evidence/status documentation. Development controls were used only to observe the run and control time/exit; no new runtime dependency was introduced. This is not a whole-product release-dependency certification.

## Required status

| Status | Result |
|---|---|
| Original session resumed | YES |
| Selected natural succession observed | NO |
| Guarded save/reload performed | NO |
| Succession continuity result | BOUNDED NULL |
| Cross-hero personal-memory application detected | NO — none in emitted evidence; successor boundary unexercised |
| Duplicate succession increment on reload | NO observed — guarded reload not performed |
| Gameplay changed | NO |
| Protected fixture unchanged | YES |
| Bannerlord closed at completion | YES |

Exact evidence: `Reports/GenerationalContinuity/evidence/phase7b_succession_runtime_20260926.txt`.

Stop at this checkpoint. Do not extend this attempt, force a death, alter aging/succession, or treat the null as permission for a new feature or another run.
