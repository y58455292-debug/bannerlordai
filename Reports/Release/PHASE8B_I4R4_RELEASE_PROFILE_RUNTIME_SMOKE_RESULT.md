# Phase 8B-I4R4 — Release-profile runtime smoke

## Classification: PASS

**Phase 8B-I4R4 release-profile runtime smoke: PASS.** This is a reporting-only closure of the later, already-completed runtime on **2026-09-27 UTC** (2026-09-26 Pacific Daylight Time), not a new gameplay run. Its native process was **40224**, launched from GitHub checkpoint `2344ea02c229e9dfa80d649c02902fa794bddc19`. The retained native save headers, controller chronology, pause/menu screenshots, native logs, and cleanup evidence support the bounded result below.

An **earlier I4R4 preflight was blocked before launch**. Commit `2344ea02c229e9dfa80d649c02902fa794bddc19` recorded that earlier `PRELIGHT BLOCKED / NOT EXECUTED` request. After machine access returned, a later fresh run was separately executed; only that later run is classified here. The historical blocked evidence is preserved byte-for-byte at `Reports/Release/evidence/phase8b_i4r4_preflight_blocked_20260927.txt`. Earlier I4/I4R1/I4R2/I4R3 test-control failures remain closed and are not reclassified.

| Required window | Retained measurement | Hard cutoff | Result |
|---|---:|---:|---|
| Pre-guarded-save: 20–28 campaign h | **20.016 h** | 30 h | Within bounds |
| Post-guarded-reload: 4–8 campaign h | **4.056 h** | 10 h | Within bounds |
| Total from protected fixture | **24.072 h** | 40 h | Within bounds |

Final native probe: **`DayLong=1040.764`**. Final retained helper decision: **`STOP_TARGET_REACHED`**. Arithmetic was independently recomputed from the retained metadata rather than copied from `cleanup.json`'s PASS field. These are exact Decimal results for native DayLong strings, which are rounded metadata values, not unrounded internal campaign ticks. Screenshots, calendar dates, and wall-clock intervals are not used to derive elapsed campaign hours.

## Retained sources and closure scope

The completed run's actual local root is:

`C:\Users\csala\AppData\Local\Temp\ClanAI_Phase8BI4R4_2344_20260927_0433`

Core sources are `preflight.json`, `actions.json` (196 records), `measurements.json` (25 reads), every named `*_metadata.json`, `guarded_metadata.json`, `cleanup.json`, `research_before.json`, `saves_before.json`, `release_scan_paused.json`, `final_log_scan.json`, and the retained PNG observations. Native log sources are `rgl_log_40224.txt`, `rgl_log_errors_40224.txt`, and `watchdog_log_40224.txt` under `C:/ProgramData/Mount and Blade II Bannerlord/logs`. Focused, reviewable receipts are published beside this report; raw screenshots and runtime artifacts remain local.

This closure launched no Bannerlord process, issued no game/UI input, advanced no campaign time, created no save, and reloaded nothing. It did not use the old controller's unfinished multiline prompt. Existing save files were read only for identity/header cross-checks. No build, deployment, DLL substitution, policy tuning, gameplay-source change, or save-schema change occurred. Review-generated contact sheets are derivatives of existing screenshots, not new live observations.

## 1. Preflight and exact candidate

The retained preflight at `2026-09-27T04:34:57.215281Z` records restored machine access, zero pre-existing Bannerlord processes, exact installed/package equality, 53 existing native saves, and 84 tracked research telemetry files. The launch action records another protected-fixture/package recheck before the single fresh process began at `04:36:03.609493Z`.

| Committed and installed file | Bytes | SHA-256 |
|---|---:|---|
| `bin/Win64_Shipping_Client/ClanAI.dll` | 335872 | `A18341FB2CD6B8623B45B52155C1A7D987DBEE7AAE14AE7DBF704514EC7F37B5` |
| `README.md` | 999 | `2B8D181068E8CFC38BB518B1EB144BCBA979E7B57EDE49420213811ABB8C6E7E` |
| `SubModule.xml` | 1177 | `287609EEBE39EE00D693CD1C354362C64878F40950BCD787D505F20F065B333D` |

The Git DLL blob is `a5fc3923b31e482960026a0044dd1b21d6e6d00a`. External `Bannerlord.Harmony` was `v2.4.2.248`, satisfying the declared dependency; its `0Harmony.dll` SHA-256 was `2EDDA13A18954B79795BAC0D7E0E8FDF0BFA05B96552D6056D90F446CD0A2AB2`. Native logs record successful loading of both Harmony assemblies and ClanAI.

The live module contained only the three committed package files before and after the run, with no Data or Logs directory and no configuration override. The unchanged package therefore used its documented defaults: **Release; Evidence OFF; Visual War OFF; Strategic Commitment Observe**. No RuntimeProfile.cfg, Evidence opt-in, Visual War marker, or Strategic Commitment override was present. This is a package/default-configuration finding, not an internal instrumented profile query.

Protected fixture: `ClanAI V020V PERSIST DEMO GATE V021M 20260924.sav`.

- SHA-256: `A91F15BF1403F1D29F942C56A1162F431113942CDACCAFE52F4D80303B4CB427`.
- Last-write UTC: `2026-09-24T17:20:12.2384633Z` (retained `mtime_ns=1790270412238463300`).
- Native DayLong: `1039.761`; size: 9040242 bytes.

All three required identity fields matched before launch and after cleanup. The reporting review also re-read the retained protected, guarded, and final probe files without writing to them and matched their recorded identities.

## 2. Initial load and time control

The selected runtime modules were external Harmony, native modules, NavalDLC for this existing nautical fixture, and ClanAI. Inspector, TestRunner, and an in-game research command bus were not selected. NavalDLC was fixture compatibility, not a new package dependency.

The normal main menu is retained in `05_startup_observation.png`; `07_fixture_search.png` and the selection receipt identify the protected fixture by name, not a previous test save. The normal compatibility dialog reported the historical Inspector removal and ClanAI version difference; the load proceeded and was not a dependency rejection. Native initialization completed and the Syronea campaign was usable.

Initial native zero-speed and explicit PAUSED views were acknowledged before the first Save As. The initial `BI4R4 PROBE` was saved, time pause was reconfirmed, and its retained header matched `1039.761`. The protocol used was `Reports/Release/PHASE8B_I4R4P_TIME_PAUSE_PROTOCOL.md`: native time pause is the safety latch; the clickable native menu is Save/Load navigation. Menu presence alone is not substituted for native time pause.

## 3. All retained pre-save probes

Each delta below is from the previous saved probe. Authorized seconds are external input intervals, not campaign-time measurements. Detailed actual monotonic intervals and all 21 burst authorizations are retained in the focused evidence.

| Retained probe | Native DayLong | Authorized seconds | Measured delta h | From protected h |
|---|---:|---:|---:|---:|
| `p00_initial` | `1039.761` | — | — | 0.000 |
| `p01_calibration` | `1039.772` | 1 | 0.264 | 0.264 |
| `p02_after_b01` | `1039.772` | 6.8 | 0.000 | 0.264 |
| `p03_after_b02` | `1039.857` | 6.8 | 2.040 | 2.304 |
| `p04_after_b03` | `1039.857` | 6.0 | 0.000 | 2.304 |
| `p05_after_b04` | `1039.932` | 6.0 | 1.800 | 4.104 |
| `p06_after_b05` | `1040.007` | 6.0 | 1.800 | 5.904 |
| `p07_after_b06` | `1040.082` | 6.0 | 1.800 | 7.704 |
| `p08_after_b07` | `1040.157` | 6.0 | 1.800 | 9.504 |
| `p09_after_b08` | `1040.232` | 6.0 | 1.800 | 11.304 |
| `p10_after_b09` | `1040.307` | 6.0 | 1.800 | 13.104 |
| `p11_after_b10` | `1040.382` | 6.0 | 1.800 | 14.904 |
| `p12_after_b11` | `1040.458` | 6.0 | 1.824 | 16.728 |
| `p13_after_b12` | `1040.533` | 6.0 | 1.800 | 18.528 |
| `p14_after_b13` | `1040.57` | 3.0 | 0.888 | 19.416 |
| `p15_after_b14` | `1040.595` | 2.0 | 0.600 | 20.016 |

Fresh normal-speed calibration measured `0.264 h` over a `1.000895 s` input interval. Later authorization used the maximum positive measured rate from this same process, rising to `0.304 h/s`; no historical run's rate was substituted. The `b02` delta was `2.040 h` against an expected `1.7952 h`: the rule bounded **expected** normal bursts to 2 h, not every realized delta, and this was below 3 h. The measured rate was updated before subsequent bursts. Near the target, expected bursts were at most 1 h.

Four input intervals in the complete run produced zero saved progression (`b01`, `b03`, `r01`, `r03`). They are retained, not discarded or counted as time advancement. Menu-visible observations and native navigation retries accompanied those intervals; the record does not prove the precise cause of every ineffective input.

The target was first reached at `p15_after_b14`, **20.016 h**. No further pre-save advancement was authorized. The guarded save preserved the same DayLong.

## 4. Guarded save and exact reload

**Actual guarded filename: `BI4R4 20260927 05240524.sav`.** Native materialization succeeded; size 6485236 bytes; SHA-256 `4F57AF375EF3C5B37F9097CB5C60402CD2A7E85C1174098427F0A8E76A5E0422`; retained last-write `mtime_ns=1790486792083627800`; native DayLong **`1040.595`**. It is separate from both the protected fixture and disposable probe.

The intended name ended `0524`, but a text-entry retry duplicated that suffix. The actual saved name was verified on disk, in the native log, and in the subsequent load selection. The report does not silently rename it, claim the shorter name existed, or describe a second guarded save. Exactly one guarded save was created.

Native receipts record `SaveAsCurrentGame: BI4R4 20260927 05240524` at local `22:26:29.076` and successful save completion at `22:26:32.097`. `guarded_postsave_time.png` shows PAUSED afterward. The exact longer filename is selected in `reload_saved_games.png` and named by `GUARDED_LOAD_SELECTED` at `05:28:42.171395Z`.

Native logs then record the second saved-game initialization (`22:28:46.787` through `22:28:48.701`) and `Loading Save Game` at `22:28:57.448`. `guarded_load_retry.png` is the campaign-ready/menu view at `05:29:06.478520Z` with native pause selected; it was acknowledged at `05:29:43.148042Z`. Its filename is not the proof of reload: the selected save, native initialization receipts, campaign return, and matching new post-reload probe together support it.

`postreload_baseline` is only a **re-read of the guarded file**, not an additional live save or independent live-time sample. The subsequently saved `postreload_probe_baseline` is the actual new probe after reload, also `1040.595`. Save As explicitly reselected/overwrote `BI4R4 PROBE` so later normal Save operations did not overwrite the guarded file.

## 5. All retained post-reload reads/probes

| Retained read/probe | Native DayLong | Authorized seconds | Measured delta h | Post-reload h | Total h |
|---|---:|---:|---:|---:|---:|
| `postreload_baseline` | `1040.595` | — | — | 0.000 | 20.016 |
| `postreload_probe_baseline` | `1040.595` | — | — | 0.000 | 20.016 |
| `r01_probe` | `1040.595` | 6.0 | 0.000 | 0.000 | 20.016 |
| `r02_probe` | `1040.67` | 6.0 | 1.800 | 1.800 | 21.816 |
| `r03_probe` | `1040.67` | 3.0 | 0.000 | 1.800 | 21.816 |
| `r04_probe` | `1040.708` | 3.0 | 0.912 | 2.712 | 22.728 |
| `r05_probe` | `1040.745` | 3.0 | 0.888 | 3.600 | 23.616 |
| `r06_probe` | `1040.764` | 1.5 | 0.456 | 4.056 | 24.072 |

The final `r06_probe` was measured at `05:45:00.686405Z`. Its helper returned `STOP_TARGET_REACHED`, with **5.944 h** to the post-reload cutoff and **15.928 h** to the total cutoff. The retained target-stop action is `05:45:19.173455Z`; there was no subsequent advancement or save.

Across the chronology, **49 native-time-pause acknowledgements** and **42 menu-navigation acknowledgements** are recorded. All referenced retained pause/control views were reviewed, including the final pause crops; the menu views were also reviewed. Every one of the 24 save authorizations is followed by a recorded post-save time-pause acknowledgement before its metadata read. The snapshots and metadata support time control sufficiently for this bounded smoke, not a claim of continuous pixel monitoring or a flawless controller implementation.

The retained record includes a longer save wait (`p05`), a foreground-check rejection followed by refocus before `p13` metadata, repeated observations after some post-reload bursts, and delayed acknowledgements. Reviewed views show PAUSED or the selected native zero-speed control. No native-time-control loss is established by those navigation/observation issues. No diagnostic save after a declared time-control failure is present. The later multiline reporting stall occurred after gameplay and cleanup; it is not a campaign hang.

## 6. Release-profile and reload checks

| Check | Retained result |
|---|---|
| Research telemetry created | **NO observed in checked locations** |
| Module `Logs/clanai.log` | **NO**; module Logs directory absent |
| Module `Logs/Sessions` | **NO** |
| Module `Logs/episodes.log` | **NO** |
| Phase 4/5/6 proof telemetry | **NO observed in checked locations** |
| Phase 7B/7C experiment receipts | **NO observed in checked locations** |
| Forbidden development references in current-PID scan | **NO matches** |
| ClanAI/Harmony exception trace | **NO observed** |
| Obvious duplicate initialization/lifecycle/message spam after reload | **NO observed in retained views/logs** |
| Campaign usable and time advances after reload | **YES**, supported by campaign views and four positive post-reload probe deltas |

The release finding is supported by the unchanged three-file module, absent Data/Logs directories, retained current-PID scans, and unchanged sizes/last-write timestamps for all 84 tracked research files. Two native initialization cycles correspond to the protected load and guarded reload; they are not treated as duplicate initialization. Release telemetry being OFF limits internal observability. No claim is made that every initializer, internal exception path, or lifecycle event was inspected. No rare Phase 6/7 event was pursued.

**Native logs are not error-free.** Independent review of the retained native log found two `TaleWorlds.PSAI.XmlSerializers.dll: Invalid Image` messages, three Granite GPU-cache errors at initial load, missing audio-event messages, and shutdown `Error: Non-Zero Device Reference Count! (ERC3039)`. The retained process exit code is `4294967295`, not zero. These are preserved without attributing them to ClanAI. The dedicated error log contains its startup header only. The native watchdog's `Waiting for an exception event...` line is startup diagnostics, not an observed exception or external development-watchdog dependency.

The earlier `final_log_scan.json` word-match list omitted the XML/Granite/shutdown errors. The focused evidence supplements that list from the unchanged retained log rather than repeating an incomplete “clean logs” claim. This closure does not modify that original scan file. No exhaustive network or file-access trace occurred; the research directory remained present.

## 7. Exit, save inventory, and safety

The retained no-save exit dialog explicitly states that it will not save. It was accepted at `05:46:56.677795Z`; the main menu is retained at `05:46:59.759865Z`; native Exit Game was selected at `05:47:22.350678Z`. Native final-cleanup/managed-interface deletion and watchdog process-exit receipts follow. At `05:48:09.281169Z`, cleanup retained an empty Bannerlord/TaleWorlds/Watchdog process enumeration: **zero matching processes**.

There were **24 successful native save operations: 23 disposable-probe writes and one guarded save**, with three Save As receipts and 21 normal-save/QuickSave receipts. The 25 measurement rows include the non-writing guarded-file re-read described above. After guarded reload there were seven authorized probe writes and no further guarded/gameplay save. The only new save filenames were `BI4R4 20260927 05240524.sav` and `BI4R4 PROBE.sav`. All 53 pre-existing native saves were unchanged.

Protected fixture hash, exact timestamp, size, and native DayLong matched preflight. The installed complete package and exact DLL SHA-256 were unchanged. All 84 tracked research telemetry files retained their sizes and timestamps. Final probe size: 6462091 bytes; SHA-256 `02C07AB03CAE3AB6ABE45BB6783C2C79CDA150A6396172D64E020DF260DC8295`; native DayLong `1040.764`.

## Closed checkpoint

**Final I4R4 classification: PASS.**

**ready for next Phase 8 release-hardening checkpoint: YES**

**Ready for RC freeze: YES.** Recommended next milestone: **Phase 8B-I5 — RC freeze / player-playable release-candidate checkpoint**. I5 is not started or completed by this closure; this bounded smoke is not a general balance, performance, long-run-stability, or rare-event certification.

Commit scope: corrected runtime report, focused evidence, byte-identical historical blocked-preflight evidence copy, and CODEX_STATUS. No gameplay/package/source/schema change is included. The broad roadmap file is unchanged; the current status names the next checkpoint. Stop after publishing and verifying GitHub main.
