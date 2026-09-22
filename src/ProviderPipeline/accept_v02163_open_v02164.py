from pathlib import Path
import json, subprocess, sys

R=Path(r"D:\BannerlordAIResearch")
curp=R/r"Longitudinal\EvidenceIndex\HandoffV3\current.json"
cur=json.loads(curp.read_text(encoding="utf-8-sig"))
val=r"D:\BannerlordAIResearch\Longitudinal\LiveValidation\PatrolDefense_v02163_ResultSizePolicy_20260921_221408_199993\validation.json"

report=R/r"Longitudinal\DerivedReports\2026-09-21_v02163_ResultSizePolicy_FinalAnalyzerReview.txt"
report.write_text("""BannerlordAI v0.2.10.63 FINAL VERIFY - ACCEPT

Decisive evidence
- Runner compiled with 0 errors.
- Offline gates:
  main fixtures 257/257 PASS
  v3/hash fixtures 63/63 PASS
  v4/result-size fixtures 72/72 PASS
  deterministic loopback-v63 fixtures 40/40 PASS
  provider-request contract fixtures 49/49 PASS.
- Queue-side registered result-size evaluator enforces the active executionPolicyId and invariant positive maxResultBytes.
- Runtime TransportReceipt.v3/ProviderResult.v4 binding checks exact result UTF-8 byte count after execution-policy identity match and before advisory admission.
- Deterministic loopback independently enforces the same registered maxResultBytes before returning a successful result.
- Live natural full chain used test maxResultBytes=65536.
- Exact ProviderResult.v4 byte count=754.
- Runtime resultSizePolicyReason=RESULT_SIZE_WITHIN_LIMIT.
- maxResultBytes=65536.
- transport receipt and execution policy matched.
- providerResultAccepted=true; KEEP_BASELINE admitted.
- SUCCESS_ACKED removed queue 1 -> 0.
- Replay failed DISPATCH_JOB_NOT_FOUND.
- Every live check true.
- Provider invocation stream empty; no credential value read, network/model call, Apply/score/memory authority.
- Protected saves matched, disposable saves cleaned, accepted binaries restored, lease released.

Classification: ACCEPT.

Architecture meaning
The first registered execution-policy resource bound is now enforced, not merely carried as provenance. Exact ProviderResult.v4 bytes must fit the registered maxResultBytes on both the deterministic external transport side and independently in runtime before cognition output can be admitted.
""",encoding="utf-8")

plan=R/r"Longitudinal\DerivedReports\2026-09-21_v02164_TransportExecutionAttemptBudget_Plan.txt"
plan.write_text("""BannerlordAI v0.2.10.64 - Registered maxAttempts transport-execution authorization shadow

Question
Can runtime enforce the registered execution policy maxAttempts as a bounded count of external transport executions for the exact active job/policy before any provider send is allowed?

Architecture interpretation
- maxAttempts applies to transport executions under one registered executionPolicyId.
- It does NOT redefine provider job claim attemptId or claim-release/retry ownership.
- Claim attempt identity and transport execution attempt count are separate state dimensions.

Why this gate
- v0.2.10.61 registers maxAttempts.
- v0.2.10.63 now enforces maxResultBytes.
- maxAttempts remains provenance-only.
- Bounded worker prior art (DeepAstra / DeepSeek Harness / Portal Agent) supports explicit bounded execution budgets and run status before expensive work.
- This can be enforced deterministically before any network/model call.

Smallest implementation
- Runner-only authorization state plus deterministic external-loopback consumption; ClanAI unchanged.
- Extend active dispatch job with:
  TransportExecutionAttemptCount
  LastTransportExecutionAuthorizationId
  LastTransportExecutionAttemptOrdinal.
- Add queue method AuthorizeTransportExecution(requestFingerprint, executionPolicyId):
  job missing -> DISPATCH_JOB_NOT_FOUND;
  execution policy missing/mismatch -> existing policy failures;
  registered maxAttempts parse invalid/nonpositive -> EXECUTION_ATTEMPT_POLICY_INVALID;
  count >= maxAttempts -> EXECUTION_ATTEMPT_LIMIT_EXCEEDED;
  else increment count and return EXECUTION_ATTEMPT_AUTHORIZED.
- Compute transportExecutionAuthorizationId deterministically from exact:
  requestFingerprint
  executionPolicyId
  attempt ordinal
  using length-delimited SHA-256.
- Command:
  PATROL_DEFENSE_PROVIDER_TRANSPORT_EXECUTION_AUTHORIZE <requestFingerprint> <executionPolicyId>
- Receipt:
  BannerlordAI.PatrolDefenseProviderTransportExecutionAuthorization.v1
  transportExecutionAuthorizationId
  requestFingerprint
  executionPolicyId
  attemptOrdinal
  attemptCount
  maxAttempts
  authorized
  reason
  externalNetworkUsed=false
  modelInvoked=false
  executionAuthorized=false
  zero gameplay authority.
- Explicit ReleaseClaim clears execution-policy registration and transport execution-attempt authorization state for the new claim/policy.
- Completion removes job.
- No timeout, sleeping, retry backoff, provider SDK or secret read.

Deterministic loopback integration
- Add optional required authorization receipt to loopback-v4.
- Loopback refuses to produce ProviderResult.v4 unless authorization receipt:
  schema exact,
  authorized=true,
  executionPolicyId matches registered policy,
  requestFingerprint matches provider request,
  maxAttempts/attempt ordinal are valid.
- No network/model call.

Acceptance
A. Runner compiles; ClanAI unchanged.
B. Existing fixture layers remain green.
C. Runtime fixtures:
   policy maxAttempts=1 -> first authorization succeeds ordinal=1/count=1/max=1;
   second authorization rejects EXECUTION_ATTEMPT_LIMIT_EXCEEDED without increment;
   policy maxAttempts=2 -> first and second authorized, third rejected;
   invalid/zero/non-numeric maxAttempts -> EXECUTION_ATTEMPT_POLICY_INVALID;
   wrong/missing executionPolicyId/job rejected;
   release clears execution-attempt state;
   new claim/new policy starts count at 0;
   authorization ID stable/deterministic for same exact state;
   zero-authority receipt.
D. Loopback fixtures:
   exact authorization receipt permits result;
   missing/unauthorized/wrong policy/wrong fingerprint receipt rejects;
   no network/model.
E. Live:
   natural full registration chain with test maxAttempts=1;
   runtime authorization succeeds ordinal=1;
   deterministic loopback consumes exact authorization receipt and returns result;
   normal v4 receipt/result/hash/size/policy chain succeeds -> KEEP_BASELINE -> SUCCESS_ACKED 1->0;
   no second execution is attempted after completion.
F. EXIT_NOSAVE cleanup.

Scope
This enforces bounded transport execution count only. timeoutMs remains registered-but-unenforced. credentialRef remains opaque; no credential value is read. Real provider/network integration remains out of scope until explicit authorization/configuration.
""",encoding="utf-8")

arch=R/r"Longitudinal\EvidenceIndex\HandoffV3\ARCHITECTURE_LEARNING.md"
entry="""

## 2026-09-21 - v0.2.10.63 maxResultBytes execution-policy enforcement accepted
- Registered maxResultBytes is now enforced against exact ProviderResult.v4 UTF-8 bytes on both deterministic external loopback and independently in runtime.
- Live result was 754 bytes under registered 65536 and reported RESULT_SIZE_WITHIN_LIMIT before KEEP_BASELINE admission and SUCCESS_ACKED completion.
- Oversize and invalid policy limits are fail-closed in fixtures.
- maxAttempts and timeoutMs remain registered-but-unenforced; credentialRef remains opaque.
- Architecture distinction: provider claim attemptId is worker ownership identity, while maxAttempts should govern transport executions under a registered execution policy.
- Next safe gate is explicit runtime transport-execution authorization with bounded maxAttempts accounting before any external send.
"""
txt=arch.read_text(encoding="utf-8-sig")
if "v0.2.10.63 maxResultBytes execution-policy enforcement accepted" not in txt:
    arch.write_text(txt.rstrip()+entry+"\n",encoding="utf-8")

patch={
 "last_accepted_milestone":{
  "version":"v0.2.10.63-execution-policy-result-size-enforcement-shadow",
  "name":"Execution-policy maxResultBytes enforcement shadow",
  "status":"ACCEPTED",
  "result":"257/257 main + 63/63 v3/hash + 72/72 v4/size + 40/40 loopback-v63 + 49/49 request-contract fixtures plus live exact ProviderResult.v4 size 754 <= registered 65536 -> RESULT_SIZE_WITHIN_LIMIT -> KEEP_BASELINE -> SUCCESS_ACKED 1->0 -> replay job-missing; zero network/model/gameplay authority and full cleanup.",
  "validation":val,
  "analyzer_report":str(report),
  "runner_test_sha256":"AC561AB57585EAE55ED5949F246DC1D60B22ECF98B3B1B1A0FF9210339807423",
  "clanai_test_sha256":"C437EE12AF5D223EEB1C3CF17D4191522F26EBAC350E4A0BA652A591A82399FD"
 },
 "active_candidate":{
  "version":"v0.2.10.64-execution-policy-max-attempts-enforcement-shadow",
  "name":"Registered maxAttempts transport-execution authorization shadow",
  "status":"PLAN_READY",
  "plan":str(plan),
  "base_clanai_staging":r"D:\BannerlordAIResearch\workspace\_PatchStaging\PatrolDefenseRecentOutcomeAge_v02138_20260921_1234\candidate",
  "base_runner_staging":r"D:\BannerlordAIResearch\workspace\_PatchStaging\AutonomousOperator_v002163_ResultSizePolicy_20260921_2208\candidate"
 },
 "active_question":"Can runtime enforce registered maxAttempts as a bounded transport-execution authorization count for the exact active execution policy before any external provider send?",
 "next_action":"Coder EXECUTE v0.2.10.64 maxAttempts gate: add queue-side transport execution authorization/count/ID + command/receipt, clear state on release, require authorization receipt in deterministic loopback-v4, compile and pass max=1/max=2/limit/invalid/mismatch/release/determinism/backcompat fixtures, then natural full chain maxAttempts=1 -> execution authorization ordinal 1 -> loopback consumes authorization -> existing v4 hash/size/policy chain -> KEEP_BASELINE -> SUCCESS_ACKED 1->0.",
 "blockers":[],
 "do_not_repeat":list(dict.fromkeys((cur.get("do_not_repeat") or [])+[
  "Do not rerun v0.2.10.63 unchanged; dual maxResultBytes enforcement and live RESULT_SIZE_WITHIN_LIMIT success are accepted.",
  "Do not conflate provider claim attemptId with execution-policy maxAttempts; maxAttempts governs transport executions under one registered policy.",
  "Do not read credentialRef secret values or connect a real provider/network without explicit authorization/configuration."
 ]))
}
pp=R/r"workspace\accept_v02163_open_v02164.json"
pp.write_text(json.dumps(patch,indent=2)+"\n",encoding="utf-8")

cp=subprocess.run([
 sys.executable,"-X","utf8",str(R/r"Tools\Handoff\handoff_checkpoint.py"),
 "checkpoint","--patch-file",str(pp),
 "--event-type","MILESTONE_ACCEPTED",
 "--message","v0.2.10.63 accepted: registered maxResultBytes is enforced on exact ProviderResult.v4 bytes externally and in runtime. Open v0.2.10.64 bounded transport-execution authorization/maxAttempts enforcement.",
 "--action-id","accept-v02163-open-v02164",
 "--expected-seq",str(cur["checkpoint_seq"])
],capture_output=True,text=True)
print(cp.stdout)
if cp.returncode:
 print(cp.stderr)
 raise SystemExit(cp.returncode)
subprocess.run([sys.executable,"-X","utf8",str(R/r"Tools\Autopilot\sync_thinking_loop.py")],check=True)
