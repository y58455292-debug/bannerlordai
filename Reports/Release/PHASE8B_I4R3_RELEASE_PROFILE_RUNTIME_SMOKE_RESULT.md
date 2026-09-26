# Phase 8B-I4R3 — dual-pause release-profile runtime smoke

Runtime date: 2026-09-26 UTC.
Authoritative starting checkpoint: `77eb3ae676d792619eaa404de6b5ab5b5152fdfb`.
Protocol: `Reports/Release/PHASE8B_I4R3P_DUAL_PAUSE_PROTOCOL.md`.
Packaged and installed DLL SHA-256: `A18341FB2CD6B8623B45B52155C1A7D987DBEE7AAE14AE7DBF704514EC7F37B5`.

## Classification: FAIL

**The fresh attempt stopped after calibration because the native Escape-menu latch was not re-established by the requested keyboard inputs. This is an operator/UI-control failure, not a demonstrated ClanAI defect.** Native campaign time was visibly paused after the calibration and in every retained recovery observation. The failure was the missing menu acknowledgement, not a measured campaign-hour overrun.

Exactly one native save succeeded: the initial `BI4R3 PROBE`, with `DayLong=1039.761`, matching the protected baseline. Exactly one normal-speed calibration burst was authorized for 1.0 second. No post-calibration probe was saved. When the menu requests failed, advancement ended permanently. No diagnostic save, guarded save, reload, replacement run, product fix, rebuild, or package substitution followed.

The initial probe measures `0.000` elapsed hours **before calibration**. It is not the final live campaign total. Calibration campaign-hours, campaign-hours-per-second, and final elapsed campaign time were not measured; they remain **UNKNOWN**. No screenshot date, wall-clock duration, historical rate, or native pause icon is used to invent a DayLong result. The required progression windows and complete-run numerical cutoff proof were not established.

## Candidate and preflight

GitHub main matched the requested starting checkpoint. The committed three-file package was read at that immutable commit and compared to the installed module. It already matched exactly; nothing was rebuilt or deployed.

| Package file | Bytes | SHA-256 |
|---|---:|---|
| `bin/Win64_Shipping_Client/ClanAI.dll` | 335872 | `A18341FB2CD6B8623B45B52155C1A7D987DBEE7AAE14AE7DBF704514EC7F37B5` |
| `SubModule.xml` | 1177 | `287609EEBE39EE00D693CD1C354362C64878F40950BCD787D505F20F065B333D` |
| `README.md` | 999 | `2B8D181068E8CFC38BB518B1EB144BCBA979E7B57EDE49420213811ABB8C6E7E` |

External Bannerlord.Harmony satisfied the declared `v2.4.2.248` dependency. Its `0Harmony.dll` SHA-256 was `2EDDA13A18954B79795BAC0D7E0E8FDF0BFA05B96552D6056D90F446CD0A2AB2`. The live ClanAI directory contained only the three package files, with no Data directory, RuntimeProfile.cfg, Evidence opt-in, Visual War marker, Strategic Commitment override, or Logs directory. Release defaults were unchanged.

The protected fixture was re-read during preflight and again immediately before launch:

- `ClanAI V020V PERSIST DEMO GATE V021M 20260924.sav`;
- SHA-256 `A91F15BF1403F1D29F942C56A1162F431113942CDACCAFE52F4D80303B4CB427`;
- last-write UTC `2026-09-24T17:20:12.2384633Z`;
- native `DayLong=1039.761`.

All matched. Bannerlord was initially absent. `BI4R3 PROBE` did not exist. All 52 pre-existing native saves were inventoried for final preservation, including the closed I4, I4R1, and I4R2 artifacts.

## Fresh launch and protected load

One fresh native process, PID `41608`, launched at `05:49:32.094904Z`. The selected module arguments contained external Harmony, native modules, NavalDLC for the nautical fixture, and ClanAI. Inspector, TestRunner, and an in-game research command bus were not selected. Native logs record successful loads of `0Harmony.dll`, `Bannerlord.Harmony.dll`, and `ClanAI.dll`.

The normal main menu was visibly reached. The protected fixture was selected by its full name through Saved Games, not Continue Campaign. The compatibility dialog identified historical Inspector removal and a different historical ClanAI version; it was not a dependency rejection. The protected campaign loaded into the usable Syronea interface. No prior attempt's save was loaded.

A preliminary main-menu click sequence did not open Saved Games; the next observed retry did. This occurred before campaign load and caused no campaign advancement or save. The later campaign-control failure is documented separately below.

## Initial Save state machine and measurement

Initial native time pause was observed at `05:52:37.182546Z` and acknowledged at `05:52:52.224575Z`. The Escape overlay was then observed at `05:52:53.352803Z`; menu and dual pause were acknowledged at `05:53:11.539618Z` before Save As.

The initial save used the native UI and the exact name `BI4R3 PROBE`. Native save success was recorded at local log time `[22:54:05.372]`. On return, the fresh `05:54:06.393685Z` observation showed PAUSED and the native zero-speed control. Time pause was acknowledged at `05:54:21.888086Z`, then Escape was requested. The `05:54:22.958076Z` observation showed the menu and paused time control; menu and dual pause were acknowledged at `05:54:46.648581Z`. Only afterward was metadata read.

| Measurement | Native DayLong | Elapsed from protected baseline | Scope |
|---|---|---:|---|
| Protected fixture | `1039.761` | `0.000 h` | Read-only baseline. |
| Initial `p00_initial` probe | `1039.761` | `0.000 h` | The only probe write and only live-run saved measurement. |
| Calibration result | NOT SAVED / NOT MEASURED | UNKNOWN | No post-burst save after menu-control failure. |
| Final live campaign total | NOT MEASURED | UNKNOWN | Initial probe must not be presented as final total. |

The unmodified repository-only `Automation/ReleaseSmoke/measure_daylong.py` produced `(1039.761 - 1039.761) * 24 = 0.000`, target margin `20.000 h`, pre-save cutoff margin `30.000 h`, total cutoff margin `40.000 h`, and `decision=ADVANCE`. Its checklist remained reminders, not automatic visual acknowledgements. Helper SHA-256: `1EB8F6768FF5893F5D975B169AF7B7DC9F888949ABDBC71D9D0ACFE87B67A339`. It issued no game, Save, or Load command and was not installed in ClanAI.

## Calibration and menu-control failure

The only authorization was `b00_calibration`: 1.0 second at native normal speed, starting from dual pause, with the initial DayLong and all three margins recorded. No historical rate was reused and no high-speed burst was selected.

The native normal-speed input interval began at `05:54:47.520281Z`; native time pause was requested at `05:54:48.520891Z`. The external monotonic interval between start and pause request was `1.0004836000152864` seconds. This is an input-timing receipt, not a campaign-hours measurement.

At `05:54:49.802509Z`, the fresh view showed PAUSED and the highlighted native pause control. `TIME_PAUSED_CONFIRMED` was recorded at `05:55:09.281497Z`, before the Escape request. The image also showed the normal player-facing ClanAI war-strain message; that is not research telemetry or proof that every gameplay initializer ran.

The subsequent sequence remained in menu-pause request/recovery, without metadata inspection, helper use, Save, Load, or another advancement:

| Fresh observation UTC | UI action preceding observation | Observed content |
|---|---|---|
| `05:55:10.398343Z` | First Escape key request after time-pause acknowledgement. | PAUSED and native pause control visible; Escape overlay absent. |
| `05:55:37.193129Z` | One retry after observing absence, with game-focus reacquisition. | PAUSED and native pause control visible; Escape overlay still absent. |
| `05:56:08.223525Z` | After permanently stopping the experiment, one window-targeted Escape request for safe exit only. | PAUSED and native pause control visible; Escape overlay absent. |
| `05:56:38.216319Z` | Native lower-left menu-button click for safe exit only. | Escape overlay open and native pause control still selected. |

The first unsuccessful menu observation was not treated as acknowledgement. Following the unsuccessful observed retry, the operator latched failure before the next safe-exit recovery attempt. The later native menu-button success did not resume or repair the experiment. Both latches were acknowledged at `05:57:09.206295Z` for exit only. No additional save was made to measure calibration or reconstruct elapsed time.

The observations support a failed keyboard-driven menu acknowledgement and a successful native menu-button recovery for exit. They do not establish whether the synthetic Escape events were dropped, ignored, or handled differently; no specific native or ClanAI defect is inferred. No unintended post-calibration advancement is visible in the retained recovery observations, but no final native DayLong was captured to quantify the complete run.

## Release-profile and available error checks

After exit, ClanAI still contained exactly the three package files and no Data or Logs directory. No module-local `Logs/clanai.log`, `Logs/Sessions`, `Logs/episodes.log`, Phase 4/5/6 proof telemetry, or Phase 7B/7C experiment receipts appeared. All 84 tracked files under the existing research telemetry directory retained their sizes and last-write timestamps. Research telemetry created in checked locations: **NO**.

The three current-PID native logs had no matching development-root, ChatGPT, Codex, Desktop Commander, TestRunner, development command-bus, or external-development-watchdog reference. Probe module metadata excluded Inspector and TestRunner. Bannerlord's own native watchdog diagnostic log is not classified as an external development dependency. Forbidden development references observed in the current-PID scan: **NO**.

No emitted ClanAI/Harmony exception trace, obvious duplicate initialization, or attributable duplicate lifecycle/message spam was observed during this brief run. Native logs contain one saved-game initialization. Reload-specific checks were **NOT EXERCISED**. This was not an exhaustive initializer, network, or file-access trace; the research directory remained present and internal observability was limited by Release telemetry being off.

Native logs were not error-free: two `TaleWorlds.PSAI.XmlSerializers.dll: Invalid Image` messages, missing audio-event messages, three Granite GPU-cache errors during loading, and `Error: Non-Zero Device Reference Count! (ERC2957)` at shutdown were retained. Process return code was `4294967295`; the dedicated native error log contained only startup information. No diagnosis or attribution of these native messages to ClanAI is made.

## Exit and safety

The native no-save confirmation was accepted at `05:57:36.132135Z`. The main menu was visibly reached at `05:57:39.396649Z`. Native Exit Game was selected at `05:58:05.411273Z`. The process exited; no Bannerlord/TaleWorlds process remained. No force termination was used.

| Required final field | Result |
|---|---|
| Pre-save progression 20–28 h | NOT ESTABLISHED |
| Last saved elapsed measurement | `0.000 h` before calibration; not final live total |
| Calibration campaign-hours / rate | UNKNOWN / NOT ESTABLISHED |
| Final elapsed hours / complete-run numeric cutoff proof | UNKNOWN / NOT ESTABLISHED |
| Guarded save name / DayLong / result | NOT CREATED / NOT APPLICABLE / NOT PERFORMED |
| Guarded reload | NOT PERFORMED |
| Post-reload probes / 4–8 h progression / duplicate checks | NOT EXERCISED |
| Native saves | 1 initial probe write only; 0 overwrites |
| Diagnostic saves after control loss | 0 |
| Deliberate advancement bursts | 1 normal-speed calibration only |
| Protected fixture unchanged | YES — exact required SHA-256, timestamp, and `DayLong=1039.761` rechecked |
| Prior attempts and other existing saves unchanged | YES — all 52 pre-existing save identities unchanged |
| Final installed package unchanged | YES — complete three-file manifest and exact DLL SHA-256 |
| Research telemetry created | NO in checked locations |
| Forbidden development references observed | NO in current-PID scan |
| Duplicate initialization/lifecycle issue observed | NO before exit; reload-specific checks NOT EXERCISED |
| Bannerlord closed | YES |
| Gameplay changed | NO source/policy change; native campaign state advanced during calibration and was discarded |
| Save schema changed | NO |

## Closed checkpoint

**Final classification: FAIL.**

**Ready for next Phase 8 release-hardening checkpoint: NO.**

Focused evidence: `Reports/Release/evidence/phase8b_i4r3_release_profile_smoke_20260926.txt`.

This task changes only the runtime report, focused evidence, and CODEX_STATUS. The package, gameplay, helper, protocol, and previous reports/evidence are unchanged. I4R3 is closed. Do not resume it, load its probe as a substitute starting fixture, create a diagnostic save, infer a product fix, or promote the unperformed guarded-save/reload stages to PASS. No further run or milestone is authorized by this result.
