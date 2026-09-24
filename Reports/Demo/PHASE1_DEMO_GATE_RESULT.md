# Phase 1 — demo-first integration gate result

Date: 2026-09-24

Result: **PASS — no known blocker prevents a first integrated demo candidate.**

This result validates the current integrated v0.21M gameplay candidate. It does not claim final release hardening, complete research coverage, or long-run generational stability.

## Candidate

Runtime-tested DLL:

- version: `v0.21M-player-visibility-v1+c583672261862a764233e2305290d323f9acd134`
- SHA-256: `74574BA34A9A9072E9FB256D1A7C25E54780CF5B4B7A1DD54B211A4AEC66CBB3`

Current-main source also builds successfully. Its rebuilt DLL has different embedded Git revision metadata and SHA `E8753AC3...`; it was not deployed merely to replace an already runtime-proven behavior-equivalent v0.21M candidate.

Rollback remains:

`Builds/Rollback/ClanAI_v021L6_pre_player_visibility_20260924.dll`

SHA-256: `5A8FA3E07471D31FDC046E4301ACD09B2D59C4785850E9643B03169F898D2D00`.
## Build / test gate

- ClanAI Release build: **PASS, 0 errors**.
- Existing System.ValueTuple reference warning remains.
- Prisoner/Mercy threshold-20 invariant: **PASS**.
- ProviderPipeline execution-package fixtures: **PASS, 44 checks**.

No build/test failure blocks deployment or play.

## Campaign load reliability

Protected baseline:

`ClanAI V020V PERSIST SAFE SYRONEA MERCY 20260923`

SHA-256 remained exactly:

`A6EA4D14077DBC41E814086B435B5FEF79E385EA096E07A97A81493D62133D3C`

An initial validation attempt produced `command_failed_NullReferenceException` because the development runner received `LOAD_SAVE` immediately after `module_loaded`, before Bannerlord's actual main-menu screen existed.

The archived runner guidance explicitly warns against that sequence. The validation was repeated correctly by waiting until BannerlordInspector reported:

`TaleWorlds.MountAndBlade.GauntletUI.GauntletInitialScreen`

The protected baseline then loaded successfully with Manan in Syronea. The early error is classified as a **development-harness sequencing false failure**, not a ClanAI campaign-load failure.
A Safe Mode dialog also appeared after the deliberately forced cleanup of the bad launch. The dialog stated that the prior session shut down unexpectedly. Validation selected **No**, preserving the normal configured module stack. The following controlled EXIT_NOSAVE restart did not reproduce the prompt.

## Proven-system initialization

On baseline load the integrated candidate reported healthy restored/ready state for:

- WarState / WarScar;
- Prisoner/Mercy, release threshold 20;
- SocialLedger;
- target-defection patch;
- voluntary-loyalty/leave patch;
- player visibility;
- companion duty memory;
- companion experience memory;
- companion negative-outcome memory.

Representative restored state included WarState scars/objectives, 442 SocialLedger records, 53 duty-memory records, 208 heroes in experience memory, and 116 negative-outcome records.

## Continuous integration window

Campaign time advanced from:

`649390.2833 -> 649491.2704`

for **100.987 campaign hours** without a crash or unresponsive game state.

During that same window the logs contained:

- 2 player-visibility messages;
- 2 siege-tracking events;
- 53 loyalty considerations;
- 6 target-defection considerations;
- 49 Mercy decisions;
- 657 recruitment-related events.
Observed player messages included:

`ClanAI: Khuzait war strain: 2% (0 active wars, 0 besieged holdings, 3% scar load).`

and, after a natural siege ownership loss:

`ClanAI: fen Morcar's loyalty is shaken by the loss of Rhemtoil Castle.`

The corresponding real source event was:

`SOCIAL_LOYALTY_CLAN_LOSS_RECORDED clan=fen Morcar settlement=Rhemtoil Castle detail=BySiege ... source=OnSettlementOwnerChangedEvent`

The same run also exercised normal siege progression, diplomacy/loyalty evaluation, Mercy evaluation, war-strain recruitment cost calculation, and settlement waiting while Manan remained parked in Syronea.

A separate hands-on player battle was not forced for this gate; no evidence currently identifies ordinary player combat as a reproducible demo blocker.

## Save/load round trip

A new non-protected validation save was created:

`ClanAI V020V PERSIST DEMO GATE V021M 20260924`

SHA-256:

`A91F15BF1403F1D29F942C56A1162F431113942CDACCAFE52F4D80303B4CB427`

The runner's prefix/overwrite guard was respected, and the protected baseline was not modified.
After a controlled EXIT_NOSAVE, Bannerlord returned through a fresh main menu and loaded the new demo-gate save successfully at campaign hour `649491.2704`.

Restored post-save state included:

- WarState: scars=18, activeSieges=1, raids=20, losses=2, objectives=2;
- SocialLedger: 453 records;
- direct clan-loss memory: 1 record;
- companion duty memory: 69 records;
- experience memory: 216 heroes / 571 entries;
- negative-outcome memory: 138 records;
- Mercy threshold: 20.

The reloaded campaign then advanced another **2.406 campaign hours** while responsive. BannerlordInspector reported the active top screen as `NavalDLC.View.Map.NavalMapScreen`.

## Demo status

The current integrated candidate satisfies the roadmap's first-demo gate:

1. build succeeds — pass;
2. repository invariant tests pass — pass;
3. current proven systems initialize — pass;
4. protected baseline loads — pass;
5. campaign time advances — pass;
6. no immediate repeatable ordinary-campaign crash found — pass for the bounded integration window;
7. proven player-visible ClanAI state appears — pass;
8. rollback exists — pass;
9. incomplete systems remain documented — pass.

Known non-blockers remain target-kingdom defection research, incomplete WarScar subtype proof, broader balance, long-horizon/generational validation, and final release hardening.

The packaged mod still requires NavalDLC v1.3.3. That dependency was present for this validation and was not relaxed.

Machine-readable evidence and compact runtime excerpts are under `Reports/Demo/evidence/phase1_demo_gate_20260924/`.
