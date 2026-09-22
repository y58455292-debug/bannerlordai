from pathlib import Path
import json,subprocess,sys
R=Path(r"D:\BannerlordAIResearch")
cur=json.loads((R/r"Longitudinal\EvidenceIndex\HandoffV3\current.json").read_text(encoding="utf-8-sig"))

plan=R/r"Longitudinal\DerivedReports\2026-09-21_v02145_DeliberationProviderBoundary_Plan.txt"
plan.write_text("""BannerlordAI v0.2.10.45 - Dependency-inverted deliberation provider boundary with deterministic local mock

Question
Can BannerlordAI invoke a cognition-provider abstraction only for an exact pending route-eligible request, generate a structured advisory through a deterministic local mock, and still force that output through the accepted v0.2.10.43-.44 admission firewall before anything downstream can see it?

Prior art
- SkyrimNet: model/provider routing belongs behind eligibility and event-driven cognition boundaries.
- Inworld ecosystem: cognition providers are modular and should be separated from memory/tools/execution.
- SeverActions: high-level generated intent/advice must remain separate from registered deterministic execution.
- v0.2.10.41-.44 already provide request fingerprinting, route eligibility, strict advisory schema, exact pending-request admission, consume-on-success and replay rejection.

Smallest implementation
- Runner-only; ClanAI unchanged.
- Define IPatrolDefenseDeliberationProvider:
  providerId
  Generate(requestFingerprint, deliberationRequestJson) -> raw advisory JSON.
- Add deterministic local PatrolDefenseMockDeliberationProvider.
  No network/model access.
  Given exact request fingerprint and configured allowed disposition, returns the strict v0.2.10.43 advisory JSON.
- Add PatrolDefenseDeliberationProviderRuntime:
  requires exact pending request fingerprint already armed by v0.2.10.42/.44;
  invokes the provider;
  sends provider output through PatrolDefenseAdvisoryRuntimeGate.Evaluate;
  provider can never directly authorize execution.
- Command for validation:
  PATROL_DEFENSE_PROVIDER_MOCK_RUN <requestFingerprint> <disposition>
  allowed disposition still constrained by the strict admission layer.
- Dedicated receipt schema:
  BannerlordAI.PatrolDefenseDeliberationProviderInvocation.v1
  providerId,
  providerInvoked,
  requestFingerprint,
  rawAdvisory,
  admission,
  externalNetworkUsed=false,
  modelInvoked=false,
  executionAuthorized=false,
  behavior/intent/score/movement authority zero.
- Successful admission consumes the pending request through the same v0.2.10.44 gate.
- Provider invocation with no pending request must fail safely.
- Invalid mock disposition must fail at or before strict admission, never create execution authority.

Acceptance
A. Runner compiles; ClanAI unchanged.
B. Fixtures:
   mock KEEP_BASELINE/REVIEW_ALTERNATIVE/ABSTAIN produce strict advisory JSON;
   provider output exact fingerprint;
   provider invocation with exact pending request is admitted through runtime gate;
   successful invocation consumes pending request;
   second provider invocation is rejected NO_PENDING_ROUTE_REQUEST;
   wrong fingerprint rejected;
   invalid disposition rejected;
   fault/malformed provider output rejected while pending remains for retry;
   providerId exact;
   externalNetworkUsed/modelInvoked/executionAuthorized false;
   no Apply/score/memory write.
C. Static: no HttpClient/OpenAI/network/model API in provider/runtime implementation.
D. Live:
   natural first unseen Manan request becomes pending;
   invoke deterministic local mock KEEP_BASELINE for exact fingerprint;
   provider receipt shows providerInvoked=true, admission admitted=true, executionAuthorized=false, network/model false;
   immediate second mock invocation rejects because pending request was consumed;
   baseline PatrolDefense decision remains unchanged; no Apply side effects.
E. EXIT_NOSAVE integrity cleanup.

Scope
This milestone proves provider abstraction and firewall composition only. It does not connect a real LLM, choose model/vendor, create semantic utility, or influence gameplay.
""",encoding="utf-8")

patch={
 "active_candidate":{
  "version":"v0.2.10.45-deliberation-provider-boundary-shadow",
  "name":"Dependency-inverted deliberation provider boundary with deterministic local mock",
  "status":"PLAN_READY",
  "plan":str(plan),
  "base_clanai_staging":r"D:\BannerlordAIResearch\workspace\_PatchStaging\PatrolDefenseRecentOutcomeAge_v02138_20260921_1234\candidate",
  "base_runner_staging":r"D:\BannerlordAIResearch\workspace\_PatchStaging\AutonomousOperator_v002144_RuntimeAdvisoryAdmission_20260921_1336\candidate"
 },
 "active_question":"Can a cognition-provider abstraction generate advisory output for an exact pending route-eligible request while remaining fully behind strict admission and disconnected from network/model/gameplay execution?",
 "next_action":"Coder EXECUTE v0.2.10.45 runner-only provider-boundary gate: add provider interface + deterministic local mock + provider runtime through existing exact pending-request admission gate, compile and pass exact/mismatch/malformed/retry/consume/replay/disposition/no-network fixtures, then one natural request -> mock KEEP_BASELINE -> admitted -> replay rejection live proof with zero gameplay authority.",
 "blockers":[]
}
pp=R/r"workspace\v02145_provider_boundary_plan_route.json"
pp.write_text(json.dumps(patch,indent=2)+"\n",encoding="utf-8")
cp=subprocess.run([
 sys.executable,"-X","utf8",str(R/r"Tools\Handoff\handoff_checkpoint.py"),
 "checkpoint","--patch-file",str(pp),
 "--event-type","PLAN_COMPLETE",
 "--message","v0.2.10.45 plan resolved: dependency-inverted cognition provider interface with deterministic local mock, all output forced through accepted exact admission firewall, no network/model/gameplay authority.",
 "--action-id","v02145-provider-boundary-plan",
 "--expected-seq",str(cur["checkpoint_seq"])
],capture_output=True,text=True)
print(cp.stdout)
if cp.returncode:
 print(cp.stderr); raise SystemExit(cp.returncode)
subprocess.run([sys.executable,"-X","utf8",str(R/r"Tools\Autopilot\sync_thinking_loop.py")],check=True)
