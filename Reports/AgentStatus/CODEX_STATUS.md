# Codex Status

## Current checkpoint

Phase 4A corrected same-hero defeat-to-native-recreation runtime characterization is **PASSED** and closed at the first complete qualifying chain. Do not rerun it immediately and do not start Phase 4B.

Candidate commit: `55cb7ded907510ebab2a5e0210f8cccb28b8605f`  
Release DLL SHA-256: `0D23F4A66E0A1E4413E879C97E963C7DB923D8D6B4421291105D8565120CD5A8`

The earlier 164.281-hour old-observer run remains a bounded null and is not reinterpreted.

## Strong positive chain

Megenhelda (`heroId=lord_4_3_1`) provides the first complete same-hero causal chain.

At campaign hour `649520.37804783334`, the corrected observer accepted her defeated-side identity from `MapEventStarted` participant capture even though the live end-time leader was absent (`actor=<none>`). The accepted line records `participantSide=Defender` and native `defeatedSide=Defender`; the logger is downstream of exact `MapEvent`, `PartyBase`, and `MobileParty` reference checks.

The old party emitted `MobilePartyDestroyed` at the same campaign hour.

At `649603.91058702779`, 83.532539194449782 campaign hours later, the same hero StringId received a distinct native party creation. The observer recorded `linked=True`, `priorPartyGone=True`, and classification `PostDefeatNativeRecreationInitialTroopsSupported`.

Creation roster outside settlement:

- total 22;
- heroes 1;
- regular troops 21;
- healthy 22;
- wounded 0;
- T1 6, T2 4, T3 6, T4 1, T5 4;
- party limit 138;
- ratio 0.1594203;
- food 26;
- current settlement none;
- target settlement none.

At `649613.50488125`, the first pre-entry boundary was Pravend. The roster was still 22, pre-settlement net change was 0, observed positive growth was 0, and `completeChain=True`.

## Bound and session evidence

Observer session start: `649491.27044636116`.

The qualifying chain completed after **122.23443488884 campaign hours**, inside the 168-hour maximum. Pause acknowledgement occurred at 122.49796913884 elapsed hours.

Through the qualifying cutoff:

- accepted defeats: 31;
- unresolved defeats: 168;
- party-destruction events: 21;
- native creations: 25;
- generic creations: 24;
- linked recreations: 1;
- complete chains: 1;
- observer errors: 0.

One additional unresolved record arrived during pause latency after the qualifying cutoff and is preserved but excluded from the proof window.

## Safety

Bannerlord was closed before deployment. A rollback was created and verified.

The protected fixture was loaded without saving and exited via `EXIT_NOSAVE`. Its SHA-256 and timestamp remained unchanged:

`A91F15BF1403F1D29F942C56A1162F431113942CDACCAFE52F4D80303B4CB427`  
`2026-09-24T17:20:12.2384633Z`

Strategic Commitment remained `Mode=Observe`; Visual War remained OFF. Fresh TestRunner chronology contains zero save commands. Bannerlord is closed.

No source, troop, recruitment, volunteer, garrison, economy, target, order, score, faction, settlement, battle, destruction, recreation, or war-state mutation was introduced for this characterization.

## Evidence

- `Reports/Manpower/PHASE4A_CORRECTED_LINKED_RECREATION_RUNTIME_RESULT.md`
- `Reports/Manpower/evidence/phase4a_corrected_linked_recreation_runtime_20260925.txt`
- `Reports/Manpower/PHASE4A_DEFEAT_IDENTITY_BOUNDARY_REVIEW.md`
- preserved old-observer null: `Reports/Manpower/PHASE4A_LINKED_RECREATION_RUNTIME_RESULT.md`

## Scope closure

This proves one natural same-hero native post-defeat recreation with regular troops already present before first settlement interaction. It does not establish a universal respawn amount or the exact native troop-selection formula.

Stop at this Phase 4A checkpoint. Phase 4B is not started.

Final product direction remains a standalone, installable, offline mod with no runtime dependency on ChatGPT, Codex, Desktop Commander, TestRunner, watchdogs, external IO, or development-machine absolute paths.
