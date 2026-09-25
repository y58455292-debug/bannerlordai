# Phase 3 Visual War — Bounded Runtime Result

Date: 2026-09-25
Repository candidate: `b79e64201914c97a354d5bc411a54a80d6fa7745`

## Result

**PASSED.** An existing Visual War defensive contribution changed the current Bannerlord-native candidate winner, and Bannerlord later committed the expected native behavior/target. The new observation-only verifier recorded `VISUAL_WAR_COMMIT_CHECK ... matched=True`.

No factor, threshold, role, target, action, candidate, or world-context classification was changed for this proof.

## Deployment safety

Bannerlord was confirmed closed before deployment.

- tested candidate DLL SHA-256: `F8C5E2AE389ADB29034CF18DE6DAA293C6F13571A4AA3F15CABA13E744A0B338`;
- prior installed DLL SHA-256: `4124AA4F79D452E28B0C08892B1642CD77ED2DFA9F698129405C603A4E69AA2D`;
- verified rollback path: `D:\BannerlordAIResearch\Builds\Rollback_Phase3_VisualWar_20260925_ClanAI`;
- rollback SHA-256 exactly matched the prior installed DLL;
- deployed DLL SHA-256 exactly matched the tested candidate.

Visual War was enabled only through the standalone module-local marker:

`<Bannerlord module>/ClanAI/Data/ENABLE_VISUAL_WAR_LAB.txt`

Marker SHA-256 during the run:
`D1F84444C6B76EDE1AE8D148C825F28CD83FA2C21376775302CE94E1680AB9BF`.

The marker was removed after `EXIT_NOSAVE`; Visual War is therefore OFF by default again.

## Protected fixture

The existing protected fixture was loaded read-only:

`ClanAI V020V PERSIST DEMO GATE V021M 20260924`

After the run:
- SHA-256 remained `A91F15BF1403F1D29F942C56A1162F431113942CDACCAFE52F4D80303B4CB427`;
- modification time remained `2026-09-24T17:20:12.2384633Z`;
- no save command was issued.

## Bounded observation

The observation was capped at 72 campaign hours and stopped after the first qualifying defensive/security winner-change + matched commit was exposed.

- start campaign hour: `649491.27044636116`;
- paused campaign hour: `649521.18544391671`;
- elapsed before stop: `29.91499755555` campaign hours;
- commands after campaign load: `FAST`, `PAUSE`, `EXIT_NOSAVE`;
- no synthetic target/action command or direct party-order command was issued.

The qualifying event itself occurred before the stop boundary.

## Activation proof

Fresh campaign initialization logged:

`2026-09-25T05:33:24.5740526Z ... VISUAL_WAR_RESET enabled=True`

This confirms the deployed layer resolved and observed the module-local marker. No development-machine absolute Visual War activation path was used.

## Selected proof — Arthamund frontier defense

Visual War observed a healthy native lord-party state:

- actor: Arthamund;
- party: Arthamund's Party;
- men: 112;
- readiness: 0.868;
- food days: 26;
- reason: `frontier-defense`.

### Bannerlord-native winner -> Visual War winner

Exact winner-change record:

`2026-09-25T05:33:39.6975958Z ... VISUAL_WAR_WINNER_CHANGE actor=Arthamund party=Arthamund's Party men=112 readiness=0.868 foodDays=26 before=PatrolAroundPoint:Sibir after=PatrolAroundPoint:Goleryn reason=frontier-defense previousVisual=<none> applications=10 winnerChanges=1 selectionChanges=1 repeatsSuppressed=0 weakFrontierSkips=15`

This is the required boundary crossing:
- before Visual War's bounded contribution, the current native/composer winner was `PatrolAroundPoint:Sibir`;
- an already-existing Bannerlord candidate `PatrolAroundPoint:Goleryn` became the current composer winner;
- the reason was the existing `frontier-defense` rule;
- no candidate or target was synthesized.

The final strategic blackboard observation in the same decision cycle recorded:

`ACTOR_BLACKBOARD_BORN actor=Arthamund state=Defend objective=PatrolAroundPoint:Goleryn winnerScore=3.61009574 runnerScore=3.49321747 gapPct=0.0323753953`

This confirms the final composed native decision remained the Goleryn defensive patrol after all current composer contributions.

### Bannerlord native commit

The new verifier later recorded:

`2026-09-25T05:33:44.7457195Z ... VISUAL_WAR_COMMIT_CHECK actor=Arthamund partyId=lord_4_181_party_1 reason=frontier-defense expectedBehavior=PatrolAroundPoint expectedTarget=Goleryn expectedTargetKey=S:castle_village_EW5_2 actualDefault=PatrolAroundPoint actualShort=GoToPoint actualTarget=Goleryn actualTargetKey=S:castle_village_EW5_2 arrivedTarget=<none> arrivedTargetKey=<none> behaviorMatch=True targetMatch=True arrivedMatch=False matched=True expired=False ageHours=6.06 checks=1 matches=1 expiries=0`

Therefore:
- expected behavior: `PatrolAroundPoint`;
- expected existing Bannerlord target: Goleryn;
- actual native default behavior: `PatrolAroundPoint`;
- actual native target: Goleryn with the same stable settlement id;
- `behaviorMatch=True`;
- `targetMatch=True`;
- `matched=True`;
- `expired=False`.

This is the required post-vanilla native commit proof.

## Native-authority boundary

The runtime candidate source was unchanged from `b79e64201914c97a354d5bc411a54a80d6fa7745`.

The accepted offline invariants still apply:
- Visual War iterates only Bannerlord-provided candidates;
- settlement/mobile targets remain native candidate targets;
- settlement frontier/attack classification and bandit classification remain the existing world-context logic;
- scoring remains through `StrategicDecisionComposer`;
- no direct party order or target assignment is present;
- no candidate insertion is present;
- no faction, settlement, or war mutation is present;
- the verifier only records an expectation and later reads native state.

The runtime command chronology also contains only campaign load/time-control/no-save-exit commands. No synthetic target or action was used.

## Conclusion

This completes the requested bounded Phase 3 Visual War runtime proof:

natural world context -> Bannerlord native candidates -> existing `frontier-defense` contribution changes the current winner -> existing native candidate `PatrolAroundPoint:Goleryn` remains the final composed winner -> Bannerlord commits `PatrolAroundPoint` to Goleryn -> observation-only verifier records `matched=True`.

Exact evidence is preserved in `Reports/TerritorialResponsibility/evidence/phase3_visual_war_runtime_20260925.txt`.
