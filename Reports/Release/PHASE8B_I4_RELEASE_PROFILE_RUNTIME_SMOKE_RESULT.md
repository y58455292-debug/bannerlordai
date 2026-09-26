# Phase 8B-I4 — release-profile runtime smoke result

Date: 2026-09-26 UTC (2026-09-25 America/Los_Angeles).
Source/package checkpoint: `fa91c05f9523dd2000883bd84e573d0d7dddf467`.

## Classification: FAIL

**This smoke attempt is invalid/incomplete because the operator failed to enforce the 48-campaign-hour hard maximum. It is not a demonstrated ClanAI package defect.** Native metadata from the one successful guarded save gives approximately **52.320 pre-save campaign hours**. Further unsaved progression occurred before the final pause. The exact final campaign hour was not captured. The guarded save was not reloaded, and the post-reload progression/initialization checks were not performed.

The operator relied on normal UI time controls and screenshots instead of a reliable elapsed-hour stop measurement. Waiting/focus/menu transitions were not controlled reliably. The overrun was discovered only when the newly created save's native metadata was inspected. No continuation, reload, replacement run, rebuild, or product fix was attempted after that discovery. The campaign was exited without another save and Bannerlord was closed.

The requested PASS criteria were therefore not met. **No reproducible ClanAI/Harmony release blocker was established**, and none is invented to explain this failed test. The first demonstrated blocker to accepting this checkpoint is the test-control/protocol violation itself. Readiness for the next release-hardening checkpoint is **NO**.

## Exact candidate and clean deployment

The three committed package files were downloaded from the pinned source checkpoint, not built or substituted:

| Package file | Bytes | SHA-256 |
|---|---:|---|
| `bin/Win64_Shipping_Client/ClanAI.dll` | 335872 | `A18341FB2CD6B8623B45B52155C1A7D987DBEE7AAE14AE7DBF704514EC7F37B5` |
| `SubModule.xml` | 1177 | `287609EEBE39EE00D693CD1C354362C64878F40950BCD787D505F20F065B333D` |
| `README.md` | 999 | `2B8D181068E8CFC38BB518B1EB144BCBA979E7B57EDE49420213811ABB8C6E7E` |

Bannerlord was closed before deployment. The complete previous module, including its development harness files and old configuration, was moved into a separately verified rollback directory. Only the three package files were installed in the live `Modules/ClanAI` directory. They still match at cleanup. No gameplay source, package file, policy, or save schema was edited.

External `Bannerlord.Harmony` was present at the declared version `v2.4.2.248`. Its `0Harmony.dll` SHA-256 was `2EDDA13A18954B79795BAC0D7E0E8FDF0BFA05B96552D6056D90F446CD0A2AB2`. Installed base modules were v1.5.3; the actual native runtime reports v1.5.3.122374.

## Release profile and launch

At preflight and cleanup the live module had no `Data` directory, and therefore no module-local `RuntimeProfile.cfg`, Evidence opt-in, Visual War enable marker, or Strategic Commitment override. The committed package documents the default Release profile, Evidence OFF, Visual War OFF, and Strategic Commitment Observe. No override was created; no Evidence profile or observer was enabled to obtain this test's measurements.

A single Bannerlord process, PID `21940`, was launched at `2026-09-26T03:49:24.4250240Z`, using:

```text
/singleplayer _MODULES_*Bannerlord.Harmony*Native*SandBoxCore*BirthAndDeath*CustomBattle*Sandbox*StoryMode*NavalDLC*ClanAI*_MODULES_
```

NavalDLC was retained for this protected nautical fixture, not added as a ClanAI dependency. Inspector and TestRunner were excluded from the selected launch modules. No in-game research command channel, runtime API, or external-agent decision workflow was used. External desktop automation operated normal UI controls and recorded screenshots; it did not become part of the packaged module.

The native log records successful assembly loads for Harmony and ClanAI. The ordinary main menu was reached. The protected campaign loaded into Syronea with Manan and a functioning campaign UI. The saved fixture's normal compatibility dialog identified the removed historical Inspector module and the older saved ClanAI version; it allowed loading. This was not a dependency rejection. The newly created save lists ClanAI `v0.22.0.0` and does not list Inspector or TestRunner.

Individual gameplay-system initialization was not separately instrumented. Assembly activation, campaign loading, visible normal campaign updates, and successful native save serialization were observed; these do not prove every gameplay system or replace the missing reload checks.

## Campaign progression and single guarded save

The protected fixture was `ClanAI V020V PERSIST DEMO GATE V021M 20260924`.

| Measure | Observed result |
|---|---|
| Protected save native `DayLong` | `1039.761` |
| Guarded save native `DayLong` | `1041.941` |
| Metadata-derived pre-save progression | `(1041.941 - 1039.761) * 24 = 52.320` campaign hours |
| Requested pre-save target | Approximately 24 hours — not achieved as a bounded window |
| Hard maximum | 48 hours — exceeded before saving |
| Approximate excess already at save | 4.320 hours |
| Actual guarded filename | `BI4 20260926 0407.sav` |
| Native save result | Successful; one `SaveAsCurrentGame` and one successful-save receipt |
| Guarded save size | 9045534 bytes |
| Guarded save SHA-256 | `2DC7EFBD2E5697BB90020407DE088623C5D1D9BB4098A0CB6E06A0AD46481F1B` |
| Guarded reload | NOT PERFORMED |
| Post-reload elapsed hours | NOT EXERCISED |
| Exact final unsaved elapsed hours | NOT CAPTURED; greater than the saved interval |

`DayLong` is rounded native save metadata, not an exact internal tick measurement. Its difference nevertheless establishes the overrun. A later screenshot showed Autumn 6, 1127, after the initial Autumn 3 load; no precise final-hour claim is made.

Earlier name-entry attempts were rejected by the native 30-character rule or canceled before a save materialized. The final short name was accepted and underscores were normalized to spaces. The native log records one actual save only. No second save or reload was performed.

The native save receipts include:

```text
[21:07:10.659] SaveAsCurrentGame: BI4 20260926 0407
[21:07:12.267] SaveContext::Save completed in 1.6 seconds.
[21:07:12.750] ------Successfully saved------
```

## Current-run errors and release instrumentation

No visible crash or emitted exception trace attributable to ClanAI/Harmony was observed. This is not a claim that every managed initializer or swallowed exception was examined. The PID-specific error log contains its startup header only.

Other native log findings are preserved rather than reported as an error-free run:

- Three `Graphine::Granite::Internal::ContextImplementation::DestroyCache` GPU-cache errors at local time 20:55:21.655, during the load period.
- A missing tutorial-audio event message containing the word `warning`.
- `Error: Non-Zero Device Reference Count! (ERC3346)` at the end of native shutdown.

No evidence here attributes those engine/graphics messages to ClanAI; they were not investigated, fixed, or retried in this checkpoint.

A scan of the three PID-21940 native logs found no `D:\BannerlordAIResearch`, Codex, ChatGPT, Desktop Commander, TestRunner, or command-bus reference. Bannerlord's own `watchdog_log_21940.txt` is native engine diagnostics, not a development watchdog dependency. Discovery of the installed Inspector manifest and inherited unofficial-module tags are distinguished from selected/active modules; the launch arguments and new save metadata exclude Inspector.

The deployed module still contains exactly the three package files and has no `Logs` directory. No `Logs/clanai.log`, `Logs/Sessions`, `Logs/episodes.log`, Phase 4/5/6 proof output, or Phase 7B/7C receipts were created there. Size/last-write comparisons also found no changes among all 84 previously tracked files under the existing research telemetry root. Research telemetry created in the checked Release-profile locations: **NO**.

No external service/development-resource request was observed. No exhaustive file-access or network trace was collected, so absence of log strings/output is not presented as proof that every possible dependency path was exercised. The research directory was not removed or access-blocked for this attempt.

## Exit and protected-fixture safety

After discovering the bound violation, the operator selected the native **Exit to Main Menu** action and accepted the dialog explicitly stating that it would not save the game. Acceptance was recorded at `2026-09-26T04:08:59.833751+00:00`. A graceful main-window close followed; Bannerlord was verified absent. No force termination or extra campaign save was used. The engine's ordinary options save during shutdown is not a campaign save.

Final readback at `2026-09-26T04:13:23.1132327Z` confirmed:

| Item | Result |
|---|---|
| Protected fixture SHA-256 | `A91F15BF1403F1D29F942C56A1162F431113942CDACCAFE52F4D80303B4CB427` — unchanged |
| Protected fixture last-write UTC | `2026-09-24T17:20:12.2384633Z` — unchanged |
| Rollback | Full old-module file manifest unchanged |
| Rollback DLL SHA-256 | `6B3DC310C7491C2D976F34C9DBF64A3CF9A756B1077BCAD29C4E6577767C4364` |
| Final installed DLL | Exact packaged `A18341...F37B5`, unchanged |
| RuntimeProfile / Evidence / Visual War / Strategic Commitment override | Absent; package defaults retained |
| Bannerlord closed | YES |

The guarded save and exact failing-test package state remain available locally. The protected source fixture was not overwritten.

## Required checkpoint summary

| Field | Result |
|---|---|
| Final classification | **FAIL** |
| Packaged DLL verified | YES — exact required SHA-256 |
| Dependency/load result | Harmony and ClanAI assembly loads succeeded; main menu reached |
| Release-profile/default-config result | Default package with no configuration overrides |
| Protected campaign load | Successful |
| Pre-save elapsed campaign hours | Approximately 52.320; hard bound violated |
| Guarded save | `BI4 20260926 0407.sav` — successfully materialized once |
| Reload result | NOT PERFORMED |
| Post-reload elapsed hours | NOT EXERCISED |
| Runtime exceptions/errors | No observed ClanAI/Harmony exception trace; native graphics/shutdown messages retained |
| Forbidden development-path/tool dependency references observed | NO in the current-run scan; native engine watchdog/discovery strings distinguished above |
| Research telemetry created in Release profile | NO in checked locations |
| Protected fixture unchanged | YES |
| Gameplay modified by this checkpoint | NO — no policy/source changes; campaign state advanced naturally |
| Save schema modified | NO |
| Ready for next Phase 8 release-hardening checkpoint | **NO** |

Focused exact evidence: `Reports/Release/evidence/phase8b_i4_release_profile_smoke_20260926.txt`.

Only runtime result/evidence/status documentation is committed. This attempt is closed. Do not infer a product fix from the failed test-control procedure, claim a reload PASS, repeat the test, or begin another checkpoint as part of this task.
