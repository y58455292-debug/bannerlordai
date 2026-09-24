# Player visibility v1 — 2026-09-24

## Scope

Candidate: `v0.21M-player-visibility-v1`

Build SHA-256: `74574BA34A9A9072E9FB256D1A7C25E54780CF5B4B7A1DD54B211A4AEC66CBB3`

The purpose of this slice is to make otherwise invisible ClanAI world state visible to the player through Bannerlord's native message feed. No external UI framework, TestRunner dependency, or controller process is required by the feature itself.

## Messages implemented

- real clan-owned fief loss -> loyalty-shock message;
- major near-boundary loyalty shift -> wavering/closer-loyalty message with a 72-campaign-hour per-clan cooldown;
- committed voluntary kingdom departure -> leave notification;
- committed target-kingdom defection -> defection notification;
- player-kingdom war strain -> percentage plus active wars, besieged holdings, and scar load;
- war-strain messages are suppressed unless the kingdom changes, the strain moves by at least 0.05, or it crosses a severity bucket.

The feature does not change any decision values. It only reads already-computed state and calls Bannerlord's `InformationManager.DisplayMessage`.
## Runtime validation

The first implementation emitted the initial war-strain message during the loading screen and then reset its visibility state. That candidate was not committed.

The corrected build moved the first war-strain display to the first campaign hourly tick.

Safe baseline used:

`ClanAI V020V PERSIST SAFE SYRONEA MERCY 20260923`

Load validation:

- no `PLAYER_VISIBILITY message=` line was emitted during the loading screen;
- campaign loaded with Manan parked in Syronea;
- after advancing approximately 1.45 campaign hours, the runtime emitted:

`PLAYER_VISIBILITY message=ClanAI: Khuzait war strain: 2% (0 active wars, 0 besieged holdings, 3% scar load).`

This proves the corrected war-strain message path runs after gameplay resumes, not during load.

## Loyalty / defection visibility validation status

The loyalty-loss, loyalty-shift, leave-commit, and defection-commit call sites compile and are wired directly to the proven SocialLoyalty/SocialDefection surfaces.
A subsequent attempt to re-run the Urikskala natural-loss fixture under v0.21M hit a dev-harness save-load stall before the new campaign initialized. That run is not counted as loyalty-UI proof.

Therefore:

- war-strain visibility: **runtime proven**;
- loyalty/leave/defection visibility: **implemented and build-proven, natural runtime message re-observation still pending**.

## Safety / rollback

The proven L6 DLL was preserved locally before deployment:

`Builds/Rollback/ClanAI_v021L6_pre_player_visibility_20260924.dll`

Rollback SHA-256:

`5A8FA3E07471D31FDC046E4301ACD09B2D59C4785850E9643B03169F898D2D00`

The visibility candidate introduces no new save schema and no behavior mutation.

## Files

- `src/ClanAI/src/ClanAI/PlayerVisibilityLayer.cs`
- `src/ClanAI/src/ClanAI/SocialLoyaltyPatch.cs`
- `src/ClanAI/src/ClanAI/SocialLoyaltyClanLossMemory.cs`
- `src/ClanAI/src/ClanAI/SocialDefectionPatch.cs`
- `src/ClanAI/src/ClanAI/WarStateBehavior.cs`
- `src/ClanAI/src/ClanAI/ClanAIStrategicBehavior.cs`
