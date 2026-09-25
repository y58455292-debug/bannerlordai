# Phase 4A - Same-hero native recreation runtime characterization

Date: 2026-09-25 UTC
Source candidate: `0b58b55d7ffd42381340a61a510ba4eaa040754b`
DLL SHA-256: `0FE236EDA21FFED93F14003A588C5A97712A5CED66341413612244C40BF1F755`

## Result

**BOUNDED NULL. The complete-chain pass condition was not met.**

The single natural runtime observation stopped after **164.28120388884 campaign hours**, below the maximum of 168 campaign hours. There was no accepted same-hero defeat -> distinct native recreation -> first-settlement boundary chain. All 36 native creation records retain `GenericNativePartyCreation` with `linked=False`.

Do not extend or repeat the run. Do not start Phase 4B. No observer tuning, identity-rule change, source change, rebuild, forced event, or gameplay intervention is justified or included in this checkpoint.

## Resumption and provenance

Current GitHub main was checked at the source candidate above. The runtime had already completed after that source commit but was not checkpointed on main. Its local deployment, protocol, event capture, result, cleanup, and TestRunner chronology were recovered and checked against the original game log. A second run was deliberately not launched.

The scoped capture starts at the unique current `PHASE4A_LINK_RESET`. Filtering the original game log from that reset for `PHASE4A_LINK_` produced exactly the same 93 lines in the same order as the retained capture. Their UTF-8/LF-normalized SHA-256 is:

`70d5652bf308188eea7c45fab2f02f0027b5ac001004bfbaf946af275cdf3fdc`

Exact selected lines, all event counts, the deployment/cleanup records, runner command chronology, and fresh verification are retained in `evidence/phase4a_linked_recreation_runtime_20260925.txt`. The complete 93-line capture remains at the local evidence path recorded there. The repository evidence is an explicitly identified excerpt plus full-run verification, not a claim that omitted event text is reproduced in the excerpt.

## Bound and stop

| Item | Campaign hour / value |
| --- | --- |
| Observer session start | 649491.27044636116 |
| Maximum allowed end | 649659.27044636116 |
| External pause trigger | start + 164 hours |
| Acknowledged paused state | 649655.55165025 |
| Actual elapsed campaign hours | 164.28120388884 |
| Remaining margin below maximum | 3.71879611116 hours |
| Stop reason | bounded-window-stop-margin |
| Qualifying chain | none |

The protocol used a conservative four-hour external stop margin to accommodate control latency. The actual run did not cross 168 hours. This is a 164.281-hour observation inside a 168-hour maximum, not a claim to have observed all 168 hours.

Runtime control began at `2026-09-25T07:14:11.447395+00:00`; the result recorded the paused stop at `2026-09-25T07:17:20.917019+00:00`. TestRunner acknowledged `PAUSE` at `07:17:20.7676897Z` and `EXIT_NOSAVE` at `07:18:20.1430510Z`. Cleanup recorded Bannerlord closed at `07:19:01.3865075Z`.

The observed commands were protected-fixture load, FAST, five CLOSE_ESCAPE_MENU acknowledgements, PAUSE, and EXIT_NOSAVE. There was no save command. The existing runner also reported its pre-existing visual-only naval guard; that development harness was not added to the mod by this checkpoint.

## Full-capture observations

| Record / result | Count |
| --- | ---: |
| PHASE4A_LINK_RESET | 1 |
| PHASE4A_LINK_DEFEAT_UNRESOLVED | 28 |
| PHASE4A_LINK_PARTY_DESTROYED | 28 |
| PHASE4A_LINK_CREATION | 36 |
| Accepted PHASE4A_LINK_DEFEAT records | 0 |
| Linked native creations | 0 |
| Linked first-settlement boundaries | 0 |
| completeChain=True records | 0 |
| PHASE4A_LINK_OBSERVER_ERROR | 0 |

These are telemetry record counts, not 28 distinct battles or a campaign-wide rate. All unresolved defeat records report `reason=no-native-leader-at-defeat`. All 36 creations are classified `GenericNativePartyCreation`; none is assigned any post-defeat classification.

### Observed fact versus interpretation

Observed: natural defeated-side callbacks reached the observer, but the native lord-party leader was unavailable at that callback. The observer emitted unresolved records rather than registering admissible defeat links. Native destruction callbacks and native creations were also observed.

Interpretation: the run exercised an identity-availability limitation at the accepted defeat boundary. Cached StringIds, hero names, clan matches, and destruction evidence cannot substitute for the missing identity required by the already-validated rule. The absence of an observer exception does not establish adequate causal coverage. Conversely, an unresolved callback is not permission to invent a defeat link or tune the observer during this completed bounded run.

No claim is made that post-defeat recreation never occurred in the native campaign, only that this run did not prove it under the accepted observer requirements.

## Most tempting partial chronology - Mikri of the Forest People

This example is retained specifically to prevent an overclaim.

At campaign hour `649604.22173575`, the observer emitted an unresolved defeated-side record with cached `heroId=CharacterObject_19413`, party id `CharacterObject_19413_party_1`, and `reason=no-native-leader-at-defeat linked=False`. A `MobilePartyDestroyed` record at that same campaign hour named Mikri of the Forest People with the same StringId and native party id.

At campaign hour `649615.90802380559`, a native creation record for `heroId=CharacterObject_19413` and party id `CharacterObject_19413_party_1` contained:

| Creation snapshot field | Observed value |
| --- | --- |
| Observer classification | GenericNativePartyCreation |
| Linked / priorPartyGone | False / False |
| Initial before settlement / roster available | True / True |
| Total / heroes / regular troops | 29 / 1 / 28 |
| Healthy / wounded | 28 / 1 |
| Regular tiers | T0=0, T1=0, T2=16, T3=8, T4=4, T5=0, T6=0, T7Plus=0 |
| Party limit / ratio | 109 / 0.266055048 |
| Food / food days | 22 / 15 |
| Current settlement / target settlement | none / none |
| Accepted defeatHour / defeatedPartyId | none / none |

Observed fact: that native creation already had 28 regular troops outside a settlement. This is generic creation evidence, not a completed same-hero post-defeat chain. Reuse of the displayed party StringId proves neither reuse nor distinctness of the native PartyBase instance. The generic record's `priorPartyGone=False` is not promoted into a separately verified native-instance test. No accepted defeat-to-recreation elapsed time or linked first-settlement boundary exists for this example.

The same hero has another unresolved/destruction pair at `649641.62796008331`. This does not repair the earlier causal gap. Temeon also appears in both creation and destruction records, but his observed creation precedes his observed destruction; that overlap is not a defeat-to-recreation chain either.

## Required-evidence disposition

Natural battle defeat: defeated-side callbacks were observed, but none established an accepted native-leader-at-defeat identity. Hero names from destruction and cached identifiers remain explicitly separate from admissible defeat evidence.

Prior party loss: 28 native MobilePartyDestroyed records were captured. These alone do not establish the required defeat/recreation identity chain.

Same-hero distinct recreation and elapsed defeat-to-recreation time: not proven. No accepted linked creation exists.

Creation-time roster: present for generic creations; Mikri's exact snapshot above is an example, not a post-defeat label.

First settlement boundary, immediately pre-entry roster, pre-settlement net change, observed positive pre-settlement growth, and first settlement identity for a linked recreation: not available. No values are fabricated from unrelated recovery telemetry.

Final conservative classification: overall **bounded null**; creation records remain **GenericNativePartyCreation**. `PostDefeatNativeRecreationInitialTroopsSupported`, `PostDefeatNativeRecreationZeroTroops`, and `PostDefeatRecreationObservedButPreSettlementStateIncomplete` are not supported because a linked recreation was not established.

## Deployment, rollback, fixture and final state

The retained deployment record at `2026-09-25T07:10:33.0826504Z` documents Bannerlord closed before copy, the required candidate hash, and rollback SHA-256 `5CA45B7AE095E9B3F6F77E06FCCA287414FBAC7D345648AAAD58F39EA1919BF6` at `D:\BannerlordAIResearch\Builds\Rollback_Phase4A_LinkedRecreation_20260925_ClanAI`.

The protected fixture was `ClanAI V020V PERSIST DEMO GATE V021M 20260924`. Before deployment, after EXIT_NOSAVE, and on fresh resumption verification:

- SHA-256: `A91F15BF1403F1D29F942C56A1162F431113942CDACCAFE52F4D80303B4CB427`;
- modification time: `2026-09-24T17:20:12.2384633Z`.

Fresh verification confirmed Bannerlord closed, rollback hash intact, candidate and live DLL matching the required hash, installed Strategic Commitment `Mode=Observe`, and no module-local Visual War activation marker. The protected fixture was loaded without saving; no claim is made here about an operating-system read-only attribute.

No troop, recruitment, volunteer, garrison, economy, faction, settlement, war, target, order, score, or party state was forcibly changed for this observation. Natural native changes remained observable. This resumption only read runtime evidence and checked safety state; it did not redeploy or launch another campaign.

## Scope closure

The first Phase 4A recovery characterization and the Phase 3 bounded null remain unchanged. README and ROADMAP retain their existing historical characterization and explicit Phase 4A stop/Phase 4B hold; CODEX_STATUS is updated to close this newer single-run obligation.

This is a documentation/evidence-only checkpoint. No source, identity policy, runtime configuration, dependency, or release packaging changes are included. BannerlordAI's standalone, installable, offline release direction remains mandatory; development tools used to collect this evidence are not new mod runtime dependencies.

**Stop at this Phase 4A bounded-null checkpoint. Do not start Phase 4B.**
