# Codex Status

## Current task
Phase 4A vanilla AI recovery observer is offline-ready. Next: one bounded natural characterization, maximum 168 campaign hours.

## Current state
PHASE 3 CLOSED WITH STRATEGIC COMMITMENT RUNTIME BOUNDED NULL. PHASE 4A RECOVERY OBSERVER DETERMINISTIC / INVARIANT CHECKPOINT PASSED.

## Phase 4A observation seam
Added an observation-only campaign behavior that tracks NPC lord parties after native creation or natural severe depletion (`PartySizeRatio <= 0.35`), including immediate post-battle observation from `MapEventEnded`.

Telemetry records:
- identity / clan / kingdom;
- campaign hour / elapsed time;
- total / healthy / wounded roster;
- regular troop tiers;
- party-size limit / ratio;
- food;
- current/target settlement;
- first state before settlement;
- settlement entry/exit;
- party delta across visits;
- volunteer pool before/after;
- garrison roster before/after where available;
- conservative source labels with unknown preserved.

No recruitment, roster, garrison, economy, movement, target, faction, settlement, war, or AI-score mutation is introduced.

## Validation
- Phase 4A recovery policy: `PASS ... checks=20`;
- wiring invariant: PASS;
- no-mutation / standalone invariant: PASS;
- Release build: 0 errors, 1 inherited `System.ValueTuple` warning;
- built DLL SHA-256: `5CA45B7AE095E9B3F6F77E06FCCA287414FBAC7D345648AAAD58F39EA1919BF6`.

## Evidence
- `Reports/Manpower/PHASE4A_RECOVERY_OBSERVER_OFFLINE_RESULT.md`

## Runtime protocol
Run one natural characterization capped at 168 campaign hours. Do not force defeat, respawn, recruitment, garrison transfer, or settlement interaction. Prefer a naturally created or post-battle severely depleted NPC lord; a naturally observed severely depleted NPC lord also qualifies. Preserve unknown source classifications whenever pool/garrison evidence is insufficient.

## Resource discipline
Do not reopen Phase 3 or Phase 2 absent a new defect. Do not implement Phase 4B or tune manpower/recruitment until Phase 4A native recovery behavior is characterized.

The final product remains a standalone installable offline Bannerlord mod. Development harnesses remain validation-only.
