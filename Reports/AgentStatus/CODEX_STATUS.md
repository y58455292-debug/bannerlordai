# Codex Status

## Current task
Stop at the completed first Phase 4A vanilla AI recovery characterization checkpoint. Do not implement Phase 4B yet.

## Current state
PHASE 3 CLOSED WITH STRATEGIC COMMITMENT RUNTIME BOUNDED NULL. PHASE 4A OBSERVATION SEAM PASSED OFFLINE AND FIRST NATURAL RECOVERY CHARACTERIZATION PASSED.

## Phase 4A observer
Observation-only behavior is committed at `dfa18f3e56a90fbe90c6467a4680e2d545fadcf4`.

Validation:
- recovery classification policy: 20 checks PASS;
- native wiring invariant: PASS;
- no-mutation / standalone invariant: PASS;
- Release build: 0 errors, 1 inherited `System.ValueTuple` warning;
- candidate DLL SHA-256: `5CA45B7AE095E9B3F6F77E06FCCA287414FBAC7D345648AAAD58F39EA1919BF6`.

## First natural characterization
Bound: maximum 168 campaign hours. Stopped after 39.623 hours when qualifying evidence was already sufficient.

Primary subject: Kulyat of the Forest People, Battania.

- start: 37/133 troops, ratio 0.278, healthy 37, wounded 0;
- before first settlement at Sibir after 0.861h: still 37 troops;
- Sibir 1: 37->44, volunteers 20->13, garrison 423->423 -> `SettlementRecruitmentSupported`;
- Kvol: 44->53, volunteers 18->9 -> `SettlementRecruitmentSupported`;
- outside visit: 53->54 -> `OtherOrUnknownNativeSource`;
- Sibir 2: 54->58, volunteers 20->15, garrison 424->424 -> `SettlementRecruitmentSupported`;
- Radakmed: 58->66, volunteers 18->10 -> `SettlementRecruitmentSupported`;
- later outside-visit +1 steps reached 69 and remain unknown.

Kulyat accounting:
- net gain: +32;
- settlement-supported gain: +28;
- outside-settlement unresolved gain: +4.

Supporting observations:
- two native `MobilePartyCreated` lord parties appeared outside settlements with 29 and 17 troops already present; this proves initial troops on native creation but does not prove those creations were specifically post-defeat respawns;
- natural defeated-side post-battle snapshots captured Stohrith, Elta, and Megenhelda with substantial wounded rosters;
- Iara at Ab Comer Castle: party 67->74, volunteers 0->0, garrison 219->212 -> `GarrisonWithdrawalSupported`.

Session labels:
- recruitment-supported: 26;
- garrison-withdrawal-supported: 1;
- other/unknown native source: 21;
- initial presence before settlement: 2.

Counts are telemetry occurrences, not global rates.

## Safety
- `EXIT_NOSAVE` completed;
- protected fixture hash/timestamp unchanged;
- rollback preserved;
- commitment remains `Mode=Observe`;
- Visual War remains OFF;
- no recruitment/manpower/economy/party/garrison/world mutation was introduced.

## Evidence
- `Reports/Manpower/PHASE4A_RECOVERY_OBSERVER_OFFLINE_RESULT.md`
- `Reports/Manpower/PHASE4A_RECOVERY_CHARACTERIZATION_RESULT.md`
- `Reports/Manpower/evidence/phase4a_recovery_characterization_20260925.txt`

## Resource discipline
This is a characterization checkpoint, not authorization to implement Phase 4B. Preserve unknown source labels; do not convert count growth into inferred recruitment without pool/garrison evidence. Do not reopen Phase 3 or Phase 2 absent a new defect.

The final product remains standalone and offline; development harnesses remain validation-only.
