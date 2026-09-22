from pathlib import Path
import json, subprocess, sys
R=Path(r"D:\BannerlordAIResearch")
curp=R/r"Longitudinal\EvidenceIndex\HandoffV3\current.json"
cur=json.loads(curp.read_text(encoding="utf-8-sig"))
val=r"D:\BannerlordAIResearch\Longitudinal\LiveValidation\PatrolDefense_v02161_ExecutionPolicy_20260921_204601_426839\validation.json"

report=R/r"Longitudinal\DerivedReports\2026-09-21_v02161_ExecutionPolicy_FinalAnalyzerReview.txt"
report.write_text("""BannerlordAI v0.2.10.61 FINAL VERIFY - ACCEPT

Decisive evidence
- Runner compiled with 0 errors.
- Main execution-policy/backcompat suite passed 257/257.
- Existing v3/hash-binding suite passed 63/63.
- Provider-request contract suite passed 49/49.
- Natural request -> claim -> provider request registration -> transport registration succeeded.
- Explicit test execution policy registered:
  timeoutMs=1000
  maxAttempts=1
  maxResultBytes=65536
  credentialRef=env:BANNERLORDAI_TEST_PROVIDER_KEY
  scope=TEST_ONLY_NOT_PRODUCT_DEFAULTS.
- Runtime computed executionPolicyId
  F69C436C1D830C33AC8B0D21E8898B559AE327D3960653B4C516F3ABBAC3C402
  over exact UTF-8 policy envelope bytes.
- Exact replay returned SAME_EXECUTION_POLICY_ALREADY_REGISTERED idempotently.
- Runtime stores only the credential reference; no environment variable value is read.
- Static checks confirm no HttpClient/socket/OpenAI/model API/credential-value read.
- Provider/result/binding streams remained empty; no external send occurred.
- Every live check passed; protected saves matched, disposable cleaned, accepted binaries restored, lease released.

Classification: ACCEPT.

Architecture meaning
External execution now requires explicit caller-supplied operational bounds and a non-secret credential reference registered against the exact transport request. Product code contains no hidden timeout/retry/result-size defaults and reads no secret values.
""",encoding="utf-8")

plan=R/r"Longitudinal\DerivedReports\2026-09-21_v02162_ExecutionPolicyBoundTransportResult_Plan.txt"
plan.write_text("""BannerlordAI v0.2.10.62 - Execution-policy-bound transport receipt + ProviderResult.v4 shadow

Question
Can the exact registered executionPolicyId be carried through the external transport receipt and ProviderResult, then verified by runtime before result-byte hash binding and advisory admission?

Why this gate
- v0.2.10.61 registers explicit timeout/attempt/result-size/credentialRef policy against the exact transport request.
- v0.2.10.60 binds exact result bytes to the transport receipt.
- The transport receipt/result currently do not prove which registered execution policy governed the external attempt.
- Before real network/model execution, policy identity should be part of the cryptographic provenance chain.

Smallest implementation
- Runner-only admission + deterministic external loopback helper; ClanAI unchanged.
- Add ProviderResult.v4 = v3 fields + executionPolicyId.
- Add TransportReceipt.v3 = v2 fields + executionPolicyId.
- Extend dispatch queue with exact MatchRegisteredExecutionPolicy:
  requestFingerprint/providerId/modelId/attemptId/providerRequestId/transportRequestId/executionPolicyId must all match active CLAIMED job and registered policy.
- Add strict ProviderResult.v4 admission ordering:
  claim match -> providerRequest match -> transportRequest match -> executionPolicy match -> advisory gate.
- Add strict TransportReceipt.v3/result-byte binding:
  receipt.executionPolicyId == ProviderResult.v4.executionPolicyId == registered job executionPolicyId;
  receipt/result provider/attempt/fingerprint/providerRequestId/transportRequestId/status still exact;
  runtime independently SHA-256 exact ProviderResult.v4 bytes and matches receipt.resultSha256;
  only then run v4 admission.
- Deterministic loopback v4 takes registered execution-policy receipt and refuses mismatched/unregistered policy.
- Combined command:
  PATROL_DEFENSE_PROVIDER_TRANSPORT_RESULT_V4_ADMIT <base64ReceiptV3> <base64ResultV4>
- Dedicated v4 admission/binding streams; preserve v1/v2/v3 commands for backward compatibility.
- No real network/model/credential read.

Acceptance
A. Runner compiles; ClanAI unchanged.
B. Backcompat fixtures remain green.
C. v4 fixtures:
   exact policy-bound receipt/result admitted;
   wrong/missing executionPolicyId rejected;
   stale policy after release/retry rejected;
   wrong claim/providerRequest/transportRequest families still reject;
   result hash mismatch/altered bytes/status mismatch rejected;
   exact SUCCESS completes 1->0;
   replay job-missing;
   zero authority.
D. Loopback v4 fixtures:
   exact executionPolicyId echoed in receipt + result;
   policy mismatch rejected before result success;
   resultSha256 independently recomputes;
   no network/model.
E. Live:
   natural request -> claim -> providerRequest registration -> transport registration -> executionPolicy registration;
   deterministic loopback v4 returns TransportReceipt.v3 + ProviderResult.v4 with exact executionPolicyId;
   runtime reports EXECUTION_POLICY_MATCHED, receipt hash match, KEEP_BASELINE, SUCCESS_ACKED 1->0;
   replay job-missing.
F. EXIT_NOSAVE cleanup.

Scope
This closes execution-policy provenance through transport and result. No real HTTP/provider credentials/model invocation yet; connecting a real external provider after this milestone requires explicit authorization/configuration.
""",encoding="utf-8")

arch=R/r"Longitudinal\EvidenceIndex\HandoffV3\ARCHITECTURE_LEARNING.md"
entry="""

## 2026-09-21 - v0.2.10.61 explicit external transport execution policy accepted
- Every external transport request can now carry explicit caller-supplied timeout, attempt count, result-size limit and env credential reference before execution.
- Runtime computes an immutable executionPolicyId over exact policy bytes and allows only one policy per active transport request; exact replay is idempotent.
- No product default limits or secret-value reads exist.
- Live policy registration used test-only values and performed no external send/model call.
- Remaining provenance gap: TransportReceipt.v2 and ProviderResult.v3 do not echo executionPolicyId, so runtime cannot yet prove which registered policy governed the result.
- Next safe gate is execution-policy-bound TransportReceipt.v3 + ProviderResult.v4.
"""
txt=arch.read_text(encoding="utf-8-sig")
if "v0.2.10.61 explicit external transport execution policy accepted" not in txt:
    arch.write_text(txt.rstrip()+entry+"\n",encoding="utf-8")

patch={
 "last_accepted_milestone":{
  "version":"v0.2.10.61-external-transport-execution-policy-shadow",
  "name":"Explicit external transport execution policy registration shadow",
  "status":"ACCEPTED",
  "result":"257/257 main-policy + 63/63 v3/hash-binding + 49/49 request-contract fixtures and live explicit policy registration/idempotent replay; no env secret read/network/model/result submission, zero gameplay authority and full cleanup.",
  "validation":val,
  "analyzer_report":str(report),
  "runner_test_sha256":"70F2733153CC21D910C4D424DD5BABA429EE54C93FEE02376D0C6FD41D81621B",
  "clanai_test_sha256":"C437EE12AF5D223EEB1C3CF17D4191522F26EBAC350E4A0BA652A591A82399FD"
 },
 "active_candidate":{
  "version":"v0.2.10.62-execution-policy-bound-transport-result-v4-shadow",
  "name":"Execution-policy-bound TransportReceipt.v3 and ProviderResult.v4 shadow",
  "status":"PLAN_READY",
  "plan":str(plan),
  "base_clanai_staging":r"D:\BannerlordAIResearch\workspace\_PatchStaging\PatrolDefenseRecentOutcomeAge_v02138_20260921_1234\candidate",
  "base_runner_staging":r"D:\BannerlordAIResearch\workspace\_PatchStaging\AutonomousOperator_v002161_ExecutionPolicy_20260921_2041\candidate"
 },
 "active_question":"Can the registered executionPolicyId be carried through both transport receipt and ProviderResult, then matched before result-byte hash binding and strict advisory admission?",
 "next_action":"Coder EXECUTE v0.2.10.62 runner/loopback policy-bound transport-result gate: add exact execution-policy match + ProviderResult.v4 + TransportReceipt.v3 + combined receipt/result v4 command, preserve backcompat, compile and pass policy/result/hash/provenance fixtures, then natural full registration chain -> deterministic loopback v4 -> EXECUTION_POLICY_MATCHED -> exact result SHA -> KEEP_BASELINE -> SUCCESS_ACKED 1->0 -> replay job-missing.",
 "blockers":[],
 "do_not_repeat":list(dict.fromkeys((cur.get("do_not_repeat") or [])+[
  "Do not rerun v0.2.10.61 unchanged; explicit execution-policy registration, idempotent replay, no-secret/no-network behavior are accepted.",
  "Do not authorize real provider/network/model execution before executionPolicyId is bound through both transport receipt and ProviderResult."
 ]))
}
pp=R/r"workspace\accept_v02161_open_v02162.json"
pp.write_text(json.dumps(patch,indent=2)+"\n",encoding="utf-8")
cp=subprocess.run([
 sys.executable,"-X","utf8",str(R/r"Tools\Handoff\handoff_checkpoint.py"),
 "checkpoint","--patch-file",str(pp),
 "--event-type","MILESTONE_ACCEPTED",
 "--message","v0.2.10.61 accepted: explicit caller-supplied execution policy registered/idempotent with no secret read/network/model call. Open v0.2.10.62 executionPolicyId-bound TransportReceipt.v3 + ProviderResult.v4 shadow.",
 "--action-id","accept-v02161-open-v02162",
 "--expected-seq",str(cur["checkpoint_seq"])
],capture_output=True,text=True)
print(cp.stdout)
if cp.returncode:
 print(cp.stderr)
 raise SystemExit(cp.returncode)
subprocess.run([sys.executable,"-X","utf8",str(R/r"Tools\Autopilot\sync_thinking_loop.py")],check=True)
