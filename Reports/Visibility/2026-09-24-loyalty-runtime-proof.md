# v0.21M loyalty player-visibility runtime proof — 2026-09-24

Candidate: `v0.21M-player-visibility-v1`

DLL SHA-256: `74574BA34A9A9072E9FB256D1A7C25E54780CF5B4B7A1DD54B211A4AEC66CBB3`

No loyalty, relation, fief, barter-score, memory, or kingdom-membership value was edited for these runs. Campaign time was advanced naturally with the development runner while Manan remained parked in Syronea.

## Natural fief-loss message

Protected baseline:

`ClanAI V020V PERSIST SAFE SYRONEA MERCY 20260923`

SHA-256: `A6EA4D14077DBC41E814086B435B5FEF79E385EA096E07A97A81493D62133D3C`

Natural causal line:

`SOCIAL_LOYALTY_CLAN_LOSS_RECORDED clan=fen Morcar settlement=Rhemtoil Castle detail=BySiege nativeSettlementValue=221250 pressureAtLoss=55312.49 ... source=OnSettlementOwnerChangedEvent`

Immediate player-visible line:

`PLAYER_VISIBILITY message=ClanAI: fen Morcar's loyalty is shaken by the loss of Rhemtoil Castle.`

This closes the required runtime proof for a real clan-owned fief-loss visibility message.

## Fresh loyalty-shift message

The previously troublesome Urikskala fixture was loaded successfully by exiting to the main menu first, then issuing the save load from a fresh campaign process.

Fixture:

`ClanAI V020V PERSIST L6 REAL URIKSKALA LOSS 20260924`

SHA-256: `7236C3BA49F4E752F9E68BBBD57C813E45E15CDFD6150D50F23FF7E1070AC58F`

A natural Gundaroving loyalty evaluation produced:

- native leave value: -391,519
- direct-loss modifier: +258,930
- adjusted value: -132,589
- committed: false

Player-visible line:

`PLAYER_VISIBILITY message=ClanAI: Gundaroving is wavering in Sturgia after recent events.`

This is a visible loyalty shift, not a leave commit.

## Native leave-commit message

In the same natural Urikskala run, Gauting lost Skarthness Castle by siege:

`SOCIAL_LOYALTY_CLAN_LOSS_RECORDED clan=Gauting settlement=Skarthness Castle detail=BySiege nativeSettlementValue=3856745 pressureAtLoss=964186.1 ...`

The matching loss message was:

`PLAYER_VISIBILITY message=ClanAI: Gauting's loyalty is shaken by the loss of Skarthness Castle.`

Later Bannerlord naturally evaluated Gauting:

- old kingdom: Nord
- native leave value: -401,733
- bounded modifier: +502,166
- adjusted value: +100,433
- nativeWouldLeave: false
- adjustedWouldLeave: true
- committed: true
- kingdomAfter: independent

The matching player-visible commit line was:

`PLAYER_VISIBILITY message=ClanAI: Gauting has left Nord after mounting losses and grievances.`

This is the strongest Mission 1 proof: the visibility line corresponds to a native committed voluntary leave, not merely a score change.

## Stability and nulls

The baseline advanced from campaign hour 649390.283 to 649775.102 without a crash. The Urikskala fixture advanced from 649112.305 to 649256.009 without a crash. Manan remained parked in Syronea. Both protected save hashes were unchanged because no validation save was written.

No fresh target-kingdom defection commit occurred in these Mission 1 runs. Therefore target-defection player-message runtime proof remains open and belongs to the separate Phase 2 defection boundary work.

Machine-readable evidence and exact log excerpts are in:

- `Reports/Visibility/evidence/v021M_loyalty_runtime_20260924/validation.json`
- `Reports/Visibility/evidence/v021M_loyalty_runtime_20260924/proof.log.txt`
