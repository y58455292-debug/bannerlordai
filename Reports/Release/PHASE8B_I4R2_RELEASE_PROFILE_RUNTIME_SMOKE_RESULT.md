# Phase 8B-I4R2 — release-profile runtime smoke

Runtime date: 2026-09-26 UTC.
Authoritative starting checkpoint: `d730f672e5409943c926fb959f4d9ba7eb74594e`.
Protocol: `Reports/Release/PHASE8B_I4R2P_SMOKE_CONTROL_PROTOCOL.md`.
Exact packaged and installed DLL SHA-256: `A18341FB2CD6B8623B45B52155C1A7D987DBEE7AAE14AE7DBF704514EC7F37B5`.

## Classification: FAIL

**This fresh attempt failed pause control after a native probe save. It is an operator/test-control failure, not a demonstrated ClanAI defect.** Only one deliberate, one-second, normal-speed calibration burst was authorized. Its immediate Escape-menu acknowledgement succeeded, but the subsequent native Save was followed by unintended campaign advancement before the menu was recovered.

The three retained probe headers measure **0.000, 0.216, and 10.152 campaign hours** from the protected baseline. The last probe also predates a further uncontrolled, unsaved interval. **The exact elapsed time at exit is UNKNOWN. Neither the 30-hour pre-save cutoff nor the 40-hour total cutoff is established for the complete run.** The last saved 10.152-hour measurement must not be represented as the final campaign total or proof that the cutoffs were preserved.

The 20–28-hour pre-save target was not established. No guarded save was created, no guarded save was reloaded, and no post-reload test was performed. No second deliberate advancement burst, replacement run, build, gameplay fix, package substitution, policy change, or save-schema change occurred.

## Candidate and preflight

GitHub main was verified at the starting checkpoint. The package was fetched at that immutable commit and compared against the live module. The installed module already matched exactly; no rebuild or redeployment was needed or performed.

| File | Bytes | SHA-256 |
|---|---:|---|
| `bin/Win64_Shipping_Client/ClanAI.dll` | 335872 | `A18341FB2CD6B8623B45B52155C1A7D987DBEE7AAE14AE7DBF704514EC7F37B5` |
| `SubModule.xml` | 1177 | `287609EEBE39EE00D693CD1C354362C64878F40950BCD787D505F20F065B333D` |
| `README.md` | 999 | `2B8D181068E8CFC38BB518B1EB144BCBA979E7B57EDE49420213811ABB8C6E7E` |

External Bannerlord.Harmony matched the declared `v2.4.2.248`; its `0Harmony.dll` SHA-256 was `2EDDA13A18954B79795BAC0D7E0E8FDF0BFA05B96552D6056D90F446CD0A2AB2`. The live ClanAI directory contained only the three package files. There was no Data directory, RuntimeProfile.cfg, Evidence opt-in, Visual War marker, Strategic Commitment override, or module Logs directory. Release defaults were left unchanged.

Before launch, and again immediately before starting the process, the protected fixture was re-read:

- Name: `ClanAI V020V PERSIST DEMO GATE V021M 20260924.sav`.
- SHA-256: `A91F15BF1403F1D29F942C56A1162F431113942CDACCAFE52F4D80303B4CB427`.
- Last-write UTC: `2026-09-24T17:20:12.2384633Z`.
- Native DayLong: `1039.761`.

All matched. Neither the I4 guarded save nor I4R1 probe was loaded. The I4R2 probe slot did not exist at preflight.

## Fresh launch and initial probe

One fresh native process, PID `38216`, launched at `2026-09-26T05:10:23.147482+00:00`. Its module arguments selected external Harmony, native modules, NavalDLC for this nautical fixture, and ClanAI; Inspector and TestRunner were excluded. Native logs show successful Harmony and ClanAI assembly loads. The ordinary main menu was visibly reached.

The protected fixture was selected by its exact name through Saved Games, not Continue Campaign. Bannerlord's compatibility dialog identified the fixture's historical Inspector module as removed and the historical ClanAI version as different. This was not a dependency rejection. The fixture loaded into the usable Syronea campaign interface.

The first Escape request did not display the menu. A single retry after observing its absence produced the visible native Escape overlay. No metadata inspection preceded that acknowledgement. The initial `BI4R2 PROBE` was created through Save As, followed by a completed-save observation, an Escape request, and positive menu acknowledgement. Its native DayLong was exactly `1039.761`.

## Native measurements

The unmodified repository-only helper `Automation/ReleaseSmoke/measure_daylong.py` calculated all elapsed values from read-only native header extracts. Helper SHA-256: `0820C82ADE07DC944486752CF80FBFE496FC20A0AFE623D061FC2196D2BEEF4C`. It did not issue game, save, or load commands and was not installed in the module.

| Probe | Native DayLong | Delta from previous saved probe | Elapsed from protected baseline | Meaning |
|---|---|---:|---:|---|
| Initial `p00` | `1039.761` | — | `0.000 h` | Required fresh baseline matched. |
| Calibration-save `p01` | `1039.77` | `0.216 h` | `0.216 h` | Saved after the only authorized calibration; stale by the time post-save pause was recovered. |
| Failure-diagnostic `p02` | `1040.184` | `9.936 h` | `10.152 h` | Captures the first unintended interval; predates a second uncontrolled post-save interval. |
| Campaign at no-save exit | NOT MEASURED | UNKNOWN | UNKNOWN | No further save was made to reconstruct this value. |

Arithmetic: `(current DayLong - 1039.761) * 24`. The `9.936 h` difference is `(1040.184 - 1039.770) * 24`; it is **not an authorized burst**. These are exact decimal calculations on the native metadata strings, not unrounded internal tick measurements.

The only authorized interval was 1.0 second at normal speed. Authorization recorded last DayLong `1039.761`, target margin `20.000 h`, pre-save cutoff margin `30.000 h`, total margin `40.000 h`, and helper decision `ADVANCE`. The calibration probe produced `0.216 campaign hours per authorized second`. No subsequent interval was authorized from that rate.

All three helper calls printed `ADVANCE` for their saved inputs. That arithmetic result did not establish that the later live campaign still matched the saved input. The stale post-save readings were not used to authorize another burst.

## Pause-control failure and unsuccessful cleanup measurement

The calibration advanced at `05:15:24.269915Z`, requested Escape at `05:15:25.279450Z`, and produced a visible Escape-menu observation at `05:15:26.209282Z`. The menu was acknowledged before the next save action.

The subsequent native Save overwrote only `BI4R2 PROBE`. The observation at `05:15:50.024746Z` still showed the transient Saving overlay. I then issued Escape without first obtaining a fresh completed-save view. At `05:16:07.980673Z`, the campaign was visibly running at normal speed and the Escape menu was absent. A single retry restored the visible menu at `05:16:27.889707Z`.

The retained observations do not establish whether an Escape closed a menu that had appeared between observations or whether an input failed to take effect. No specific native input/save defect is inferred. The established failure is that I did not keep the post-save interval bounded and paused. The subsequent probe quantified 9.936 additional hours beyond the calibration-save reading.

After recognizing failure, I nevertheless made one additional diagnostic probe overwrite from the confirmed menu instead of proceeding directly to exit. This did not restart the test, but it introduced another uncontrolled post-save interval and did not repair the failed checkpoint. Its post-save observation at `05:17:56.673276Z` showed the campaign with no Escape overlay. The next Escape-response observation was not until `05:20:11.950361Z`; that gap and subsequent unsuccessful requests were not authorized progression. Two observations still showed the overlay absent. A later single Escape action produced a visible menu at `05:20:49.343586Z`.

Only pause actions and live observations were performed during those recovery sequences; metadata was read after visible menu acknowledgement. Nevertheless, the delayed and ineffective recovery allowed additional unsaved advancement. The diagnostic probe was therefore stale again. **This second cleanup-control failure is included in the FAIL, not omitted or presented as a successful hard stop.**

No additional save was made after that final recovery. The campaign was exited without saving. Screenshot labels such as `save_completion` or `escape` are not acknowledgements; the report uses their observed image content. Calendar dates, speed icons, and wall-clock timing are not used to calculate campaign hours.

## Release-profile and error checks

The live module still contained exactly the three package files after exit, with no Data or Logs directory. No module-local `Logs/clanai.log`, `Logs/Sessions`, `Logs/episodes.log`, Phase 4/5/6 proof telemetry, or Phase 7B/7C receipts were created there. All 84 pre-existing files tracked under the research telemetry directory retained their sizes and last-write timestamps. Research telemetry created in checked locations: **NO**.

The three current-PID native logs had no matching development-root, ChatGPT, Codex, Desktop Commander, TestRunner, or development command-bus reference. No external development-watchdog dependency was observed. Bannerlord's own native watchdog log was not classified as such a dependency. Probe metadata excluded Inspector and TestRunner. Forbidden development references observed in the current-PID scan: **NO**.

No immediate/reproducible ClanAI or Harmony exception trace, obvious duplicate initialization, or attributable duplicate lifecycle/message spam was observed before exit. Reload-specific checks were **NOT EXERCISED**. Release telemetry being off limits internal observability; this is not an exhaustive initializer, file-access, or network trace. The research directory remained present.

The native logs were not error-free. They contain two `TaleWorlds.PSAI.XmlSerializers.dll: Invalid Image` messages, missing audio-event messages, three Granite GPU-cache errors during loading, and shutdown `Error: Non-Zero Device Reference Count! (ERC3072)`. Native process return code was `4294967295`. These observations are retained without diagnosing or attributing them to ClanAI. The dedicated native error log contained only its startup header.

## Exit and final safety checks

The native confirmation explicitly stating that exit would not save was accepted at `05:21:24.042488Z`. The main menu was visibly reached at `05:21:27.321691Z`. Native Exit Game was selected at `05:21:44.406397Z`. Process exit and absence of Bannerlord/TaleWorlds processes were verified; no force termination was used.

| Required field | Result |
|---|---|
| Pre-save target 20–28 h | NOT ESTABLISHED |
| Guarded save name / DayLong / result | NOT CREATED / NOT APPLICABLE / NOT PERFORMED |
| Exact guarded reload | NOT PERFORMED |
| Post-reload probe / elapsed / usability and duplicate checks | NOT EXERCISED |
| Last saved measured total | `10.152 h`; not the final live total |
| Exact total at exit / hard-cutoff compliance | UNKNOWN / NOT ESTABLISHED |
| Native saves | 3 successful writes to `BI4R2 PROBE` only |
| Protected fixture unchanged | YES — exact hash, timestamp, and DayLong rechecked |
| I4 guarded save and I4R1 probe unchanged | YES — identities rechecked; neither loaded |
| Final installed DLL | Exact required `A18341...F37B5`; complete package manifest unchanged |
| Research telemetry created | NO in checked locations |
| Forbidden development references observed | NO in current-PID scan |
| Duplicate initialization/lifecycle issue observed | NO before exit; reload NOT EXERCISED |
| Bannerlord process gone | YES |
| Gameplay changed | NO — no source/policy change; unsaved native campaign state did advance |
| Save schema changed | NO |

## Closed checkpoint

**Final classification: FAIL.**

**Ready for next Phase 8 release-hardening checkpoint: NO.**

Focused evidence: `Reports/Release/evidence/phase8b_i4r2_release_profile_smoke_20260926.txt`.

Only this report, focused evidence, and CODEX_STATUS are committed. The run is closed. Do not continue it, reload its probe as a replacement test, infer a ClanAI gameplay fix, or promote the missing guarded-save/reload proof to PASS. No further run or milestone is authorized by this result.
