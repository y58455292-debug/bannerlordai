# Phase 7C-I3 — runtime characterization result

Runtime date: 2026-09-26 UTC (2026-09-25 America/Los_Angeles).

Structural-writer checkpoint: `12f8f430a8f192e100133a70787e7596e412099b`.
Observation-only runtime checkpoint: `3056a69f114b0acc85dfb4ed12a89fc6347b70d9`.
Runtime candidate DLL SHA-256: `6B3DC310C7491C2D976F34C9DBF64A3CF9A756B1077BCAD29C4E6577767C4364`.

## Result: BOUNDED NULL

This closes the already-completed run from retained local evidence. The experiment watched for the next natural `CampaignEvents.RulingClanChanged` in **any surviving kingdom**. It did not select Nemos or any particular ruler.

No qualifying ruling-clan transition was observed within the bounded run. No new `KingdomRulingClanChanged` structural episode was recorded. The run stopped conservatively before the 1,152-hour maximum and exited without saving. **Absence of the natural event is a bounded null, not a failure.**

The intended chain remains unexercised: native ruling-clan transition → exactly one structural episode → non-personal branch-history retrieval and personal-retrieval exclusion → guarded save/reload without duplication. None of those event-dependent steps is being promoted to a runtime PASS.

This closure is documentation/evidence only. Bannerlord was not launched, no experiment was rerun, no DLL was redeployed, no bound was extended, and no gameplay, writer/retrieval policy, ActorId/BranchId, or D1/D2 schema was changed.

## Exact retained chronology

| Measure | Retained value |
|---|---|
| Start campaign hour, from initial runtime receipt | `649491.27044636116` |
| Pause/final paused campaign hour, from retained status | `650632.31218391669` |
| Elapsed campaign hours, as recorded by the stop guard | `1141.0417375555262` |
| Authorized maximum | `1152` hours / two native campaign years |
| Absolute cap, as recorded | `650643.2704463612` |
| Remaining margin, computed from maximum minus recorded elapsed | `10.958262444473803` hours |
| Stop reason | `two-native-year-bound-conservative-margin` |
| Stop-record first event / monitor error | `null` / `null` |
| Original process / campaign generation | PID `34804` / generation `1` |

The retained JSON uses ordinary binary-floating-point serialization; the table preserves the more detailed hour strings from runtime receipts/status and the exact elapsed value stored in `runtime_stop.json`, rather than inventing a new chronology. Native calendar reads confirm 24 hours/day, 3 days/week, 2 weeks/season, and 4 seasons/year: 576 hours per native year and 1,152 for two years.

The protected source fixture was `ClanAI V020V PERSIST DEMO GATE V021M 20260924`. The session log was `ClanAI_20260926_021237_869_v020Q_review.log`.

## Event, episode, and retrieval evidence

The retained event-subscription response identifies three ClanAI listeners on `RulingClanChanged`: `DynastyStructuralRuntimeObserver.OnRulingClanChanged`, `GenerationalContinuityPreflightBehavior.OnRulingClanChanged`, and `KingdomContinuityLedger.OnRulingClanChanged`. This establishes registration, not execution of a qualifying event.

The complete retained session contains no `GENCONT_NATIVE_RULING_CLAN_CHANGED` or `KINGDOM_CONTINUITY_SUCCESSION` record. The stop guard recorded `firstEvent=null`. Initial I3 receipts reported `structuralRows=0`, `semanticIdentities=0`, and seven active kingdom ledger entries, each with `successionOrdinal=0`. Other native clan-leader events in the session are not substituted for the requested ruling-clan event.

Final read-only state:

| Field | Final retained result |
|---|---|
| Branch ID | `5399123936904b7d90beb7038ae998d1` |
| Observer/main-hero ActorId | `lord_3_12` |
| Total existing episode rows | `2` |
| Existing episode kinds | `IncidentOpened`, `IncidentChoice` |
| Final structural episode rows | `0` — neither existing row is structural |
| `_structuralRecorded` | `0` |
| `_duplicates` / `_rejected` | `0` / `0` |
| Kingdom-continuity record count | `7` |

The initial branch-history receipt was:

```text
2026-09-26T02:12:41.0289807Z [ClanAI v0.22A-ruler-courtship-native-v1] I3_RUNTIME_HISTORY_RECEIPT stage=post-load receipt=null mutationByClanAI=False
```

Initial personal-history retrieval returned the existing personal history, with `visibleCount=2`; the choice path returned one existing personal choice. No structural row existed for either retrieval path to include or exclude. These are baseline observations only: **they do not runtime-prove structural-event branch-history retrieval or personal-retrieval exclusion.** The `post-load` stage denotes the initial protected-fixture load, not a guarded post-event reload.

No event-specific kingdom/old-clan/new-clan/ruler IDs, succession ordinal, semantic identity, or provenance receipt can be supplied for a transition that did not occur. Those fields have not been reconstructed.

## Telemetry and command accounting

The closure read-only scan covered all `164924` lines / `72980811` bytes of the retained session. Session SHA-256: `692D7DBE92B3BDE7A012E405572B2892FF6C4DA6B5820B3FB8E9BB13A7269D53`.

| Check | Result |
|---|---|
| Emitted telemetry-error records in the retained session | `0` |
| Qualifying ruling-clan transitions observed | `0` |
| Newly recorded structural episodes | `0` |
| Save commands in this run's retained command interval | `0` |
| Source-fixture loads | `1` |
| Guarded post-event saves / reloads | `0` / `0` |

The error count is a count of emitted log records, not a claim that every exception in every module was absent. The focused evidence includes the exact command interval after the baseline's 397 existing runner-log lines and through recorded cleanup. It contains only `LOAD_SAVE`, `CLOSE_ESCAPE_MENU`, `FAST`, `PAUSE`, and `EXIT_NOSAVE`.

```text
2026-09-26T02:29:50.6615153Z [TestRunner v0.1] COMMAND command=PAUSE result=paused timeControl=Stop
2026-09-26T02:31:04.5226616Z [TestRunner v0.1] COMMAND command=EXIT_NOSAVE result=exit_scheduled timeControl=Stop
```

Final duplicate counters of zero are not a post-succession reload check. No guarded save/reload occurred, so preservation of a structural row/semantic identity/ordinal, duplicate-row suppression, exactly-once succession increment, and duplicate-notice suppression on reload are all **NOT EXERCISED**.

## Cleanup and identity checks

Recorded cleanup at `2026-09-26T02:31:37.1011659Z` verified Bannerlord closed and the protected fixture/rollback intact. A separate read-only closure check at `2026-09-26T02:44:30.3300396Z` agreed; it is labeled separately in evidence and is not a new runtime sample.

| Item | Verified retained result |
|---|---|
| Protected fixture SHA-256, before and after | `A91F15BF1403F1D29F942C56A1162F431113942CDACCAFE52F4D80303B4CB427` |
| Protected fixture last-write UTC, before and after | `2026-09-24T17:20:12.2384633Z` |
| Rollback DLL SHA-256 | `B15B7B4D3F77EF40D6080EECF07B0077BA4B0E5ABD176C3C4328166BFB4A3ED0` |
| Final installed runtime DLL SHA-256 | `6B3DC310C7491C2D976F34C9DBF64A3CF9A756B1077BCAD29C4E6577767C4364` |
| Strategic Commitment | `Mode=Observe` |
| Visual War activation marker | Absent / OFF |
| Exit / process state | `EXIT_NOSAVE` acknowledged; Bannerlord closed |

## Evidence provenance and limits

Focused evidence: `Reports/GenerationalContinuity/evidence/phase7c_i3_runtime_20260926.txt`.

It preserves retained chronology, deployment baseline, stop/cleanup records, final read-only values, native-calendar reads, event registrations, exact runtime receipts and command lines with original line numbers, and source-file SHA-256 values. Counts and the safety-margin subtraction are explicitly labeled as closure-time analysis of retained records. The broad session log and development controller are not added to the repository.

Historical final telemetry validation reports 31 structural-writer checks, 19 retrieval-scope checks, focused invariants, and a Release build with 0 errors / 1 warning. These were not rerun during closure. The earlier `base_validation.txt` also ends with a separate Phase 7B preservation-check failure for the `CivicProjectSelectionPolicy.cs` hash. That exact failure is retained, not silently cleared or relabeled; this closure does not diagnose it or claim all inherited checks passed. It is distinct from the runtime telemetry-error count and the natural-event null.

No source, test, configuration, or save file is changed by this checkpoint. No new runtime dependency is introduced. This documentation-only closure is not a whole-product release certification.

## Phase 7C-I3 status

| Required status | Result |
|---|---|
| Natural ruling-clan transition observed | **NO** |
| Structural episode recorded exactly once | **NO — no qualifying native event occurred** |
| Branch-history retrieval proven for structural event | **NO** |
| Personal-retrieval exclusion runtime-proven for structural event | **NO** |
| Guarded save/reload performed | **NO** |
| Post-reload duplicate detected | **NOT EXERCISED** |
| Gameplay mutation by ClanAI in this I3 lifecycle/political path | **NO** |
| Overall result | **BOUNDED NULL** |

This attempt is closed. Do not extend or restart it, force death/election/succession, change writer/retrieval policy or schema, or begin another Phase 7 experiment or Phase 8 as part of this checkpoint.
