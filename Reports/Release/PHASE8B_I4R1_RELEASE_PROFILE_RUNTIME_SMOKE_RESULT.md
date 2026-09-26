# Phase 8B-I4R1 — controlled release-profile smoke retry

Runtime date: 2026-09-26 UTC.
Authoritative source checkpoint: `cf259e62f3854bae5eb044659d4d58999d186427`.
Exact packaged DLL SHA-256: `A18341FB2CD6B8623B45B52155C1A7D987DBEE7AAE14AE7DBF704514EC7F37B5`.

## Classification: FAIL

**The fresh retry failed campaign-hour control. This is an operator/procedure failure, not a demonstrated ClanAI defect.** The controlling `BI4R1 PROBE` after the third deliberate burst contained `DayLong=1041.662`, or **45.624 metadata-derived campaign hours** from protected `DayLong=1039.761`. Both the 30-hour pre-save cutoff and 40-hour total maximum were exceeded. No guarded save was created and no reload was attempted.

The first two measured bursts were bounded at 2.160 and 7.368 cumulative hours. The third burst requested four wall-clock seconds followed by the native pause button and Escape. Its retained screenshot did not show a paused game: fast-forward was active and the pause menu was absent. A subsequent Escape request restored the pause menu, but the next native probe established the overrun. I failed to verify and maintain pause before returning control to the tool/review loop. A sent pause request and a screenshot filename containing `paused` were not reliable acknowledgements.

The probe measurements—not screenshots, calendar dates, or wall-clock estimates—determine this FAIL. Once the failed measurement was read, the run was stopped without another advancement burst, guarded save, reload, rebuild, product fix, or restart. The previous I4 attempt and its guarded save remained untouched.

## Candidate and preflight

The installed module already matched the committed package exactly; no rebuild, DLL substitution, or redeployment was performed. A separate verified copy of its complete three-file module was retained as this retry's rollback.

| Package file | Bytes | SHA-256 |
|---|---:|---|
| `bin/Win64_Shipping_Client/ClanAI.dll` | 335872 | `A18341FB2CD6B8623B45B52155C1A7D987DBEE7AAE14AE7DBF704514EC7F37B5` |
| `SubModule.xml` | 1177 | `287609EEBE39EE00D693CD1C354362C64878F40950BCD787D505F20F065B333D` |
| `README.md` | 999 | `2B8D181068E8CFC38BB518B1EB144BCBA979E7B57EDE49420213811ABB8C6E7E` |

External Bannerlord.Harmony was present at the declared `v2.4.2.248`. Its `0Harmony.dll` SHA-256 was `2EDDA13A18954B79795BAC0D7E0E8FDF0BFA05B96552D6056D90F446CD0A2AB2`. Native runtime version was v1.5.3.122374.

There was no module-local Data directory, RuntimeProfile.cfg, Evidence opt-in, Visual War marker, or Strategic Commitment override. The package's Release / Evidence-off / Visual-War-off / Strategic-Commitment-Observe defaults were left unchanged.

The protected save was re-read before launch: SHA-256 and timestamp matched the required values, and native `DayLong` was exactly `1039.761`. An initial native probe before deliberate progression also returned `1039.761`, confirming this was a fresh protected-fixture load rather than the previous I4 guarded save.

## Launch and campaign load

One fresh process, PID `39676`, launched at `2026-09-26T04:30:46.091330+00:00` with the package, external Harmony, native modules, and NavalDLC retained for this existing nautical fixture. Inspector, TestRunner, and other research modules were not selected.

The ordinary main menu was reached. The source save's normal compatibility dialog identified its historical Inspector module and older ClanAI version; it permitted loading and was not a dependency rejection. The protected campaign loaded into Syronea with a usable campaign UI. Current-PID native logs record successful Harmony/ClanAI assembly loading and saved-game initialization.

No rare event was required or pursued. The on-screen campaign changed during deliberate progression. No claim is made that every individual gameplay initializer or every internal exception path was examined.

## Controlling native metadata

All four successful native saves in this retry targeted only the disposable `BI4R1 PROBE` slot: one initial baseline probe and three overwrites. Each probe's native metadata was retained before the next overwrite.

| Probe | Native DayLong | Exact decimal calculation from protected baseline | Cumulative hours |
|---|---|---|---:|
| Initial baseline probe | `1039.761` | `(1039.761 - 1039.761) * 24` | `0.000` |
| After burst 1 | `1039.851` | `(1039.851 - 1039.761) * 24` | `2.160` |
| After burst 2 | `1040.068` | `(1040.068 - 1039.761) * 24` | `7.368` |
| After burst 3 and pause recovery | `1041.662` | `(1041.662 - 1039.761) * 24` | `45.624` — FAIL |

These are exact arithmetic results for the native metadata strings, which themselves are rounded to three decimal days; they are not unrounded native tick measurements. The excess is nevertheless unambiguous: 15.624 hours beyond the pre-save cutoff and 5.624 beyond the total maximum.

The 20–28-hour pre-save window was not successfully captured. A further burst was forbidden after the failed controlling probe and none was issued. The final probe and subsequent exit screenshots show the native pause menu. There is no later metadata measurement; final reported total means the last controlling measurement, not a newly reconstructed internal campaign-hour value.

| Required guarded/reload field | Result |
|---|---|
| Guarded save name / DayLong | NOT CREATED / NOT APPLICABLE |
| Guarded native save success | NOT PERFORMED |
| Exact guarded-save reload | NOT PERFORMED |
| Post-reload probe DayLong / elapsed hours | NOT EXERCISED |
| Total last measured advancement | `45.624` campaign hours |
| Duplicate initialization/lifecycle issue observed | NO before exit; reload-specific check NOT EXERCISED |

## Release instrumentation and available errors

The module still contained exactly the three package files after the run, with no Data or Logs directory. No module `Logs/clanai.log`, `Logs/Sessions`, `Logs/episodes.log`, Phase 4/5/6 proof output, or Phase 7 experiment receipts were created there. All 84 previously tracked research telemetry files retained their sizes and last-write timestamps. Research telemetry created in the checked locations: **NO**.

The three current-PID native logs had no matching development-root, ChatGPT, Codex, Desktop Commander, TestRunner, or development command-bus reference. Bannerlord's own native watchdog diagnostics were not treated as a development watchdog dependency. The selected module arguments and probe metadata excluded research modules; discovery of an installed manifest or historical fixture tags is not evidence of an active dependency.

No visible/reproducible ClanAI/Harmony exception trace was observed. The native logs are not described as error-free: they include missing audio-event messages, three Graphine/Granite GPU-cache errors during loading, and `Error: Non-Zero Device Reference Count! (ERC3323)` at shutdown. The process return code was `4294967295`; native shutdown receipts and process absence are retained. No attribution to ClanAI or diagnosis of these native messages is claimed.

This was not an exhaustive network or file-access trace. The research directory remained present, so the absence of emitted references/output is not a proof that every possible resource-dependency path was exercised.

## Exit and safety

After the failed probe, the native no-save exit dialog was accepted at `2026-09-26T04:44:30.051483+00:00`, returning to the main menu. Native Exit Game was selected at `2026-09-26T04:45:04.883017+00:00`; no force termination was used. Bannerlord was verified closed.

| Final check | Result |
|---|---|
| Protected fixture SHA-256 | `A91F15BF1403F1D29F942C56A1162F431113942CDACCAFE52F4D80303B4CB427` — unchanged |
| Protected fixture timestamp | `2026-09-24T17:20:12.2384633Z` — unchanged |
| Protected fixture DayLong | `1039.761` — unchanged |
| Previous failed I4 guarded save | Hash and last-write time unchanged; not loaded |
| This retry's rollback | Three-file manifest unchanged; DLL hash is the exact packaged `A18341...F37B5` |
| Final installed DLL | `A18341FB2CD6B8623B45B52155C1A7D987DBEE7AAE14AE7DBF704514EC7F37B5` — unchanged |
| Evidence / Visual War / Strategic Commitment override | Absent |
| Native saves | 4 probe writes only; 0 guarded saves; 0 other campaign saves |
| Reloads | 0 |
| Bannerlord closed | YES |
| Gameplay changed by this checkpoint | NO — no policy/source changes; native campaign state advanced |
| Save schema changed | NO |

## Checkpoint

**Final classification: FAIL. Ready for next Phase 8 release-hardening checkpoint: NO.**

Focused retained evidence: `Reports/Release/evidence/phase8b_i4r1_release_profile_smoke_20260926.txt`. It records every controlling probe, native save/load/shutdown receipts, the failed pause sequence, package/fixture/log identities, and cleanup.

Only runtime result/evidence/status documentation is committed. This retry is closed. Do not continue it, restart it, load its probe as a substitute guarded test, change gameplay, or promote the missing guarded-save/reload proof to PASS.
