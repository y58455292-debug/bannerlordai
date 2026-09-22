from pathlib import Path
import json, subprocess, sys
R=Path(r"D:\BannerlordAIResearch")
curp=R/r"Longitudinal\EvidenceIndex\HandoffV3\current.json"
cur=json.loads(curp.read_text(encoding="utf-8-sig"))
val=r"D:\BannerlordAIResearch\Longitudinal\LiveValidation\PatrolDefense_v02160_TransportReceiptResultHash_20260921_203621_450700\validation.json"

report=R/r"Longitudinal\DerivedReports\2026-09-21_v02160_TransportReceiptResultHashBinding_FinalAnalyzerReview.txt"
report.write_text("""BannerlordAI v0.2.10.60 FINAL VERIFY - ACCEPT

Decisive evidence
- Runner compiled with 0 errors.
- Backcompat fixture suite passed 225/225.
- ProviderResult.v3 + transport receipt/hash-binding fixture suite passed 63/63.
- Deterministic transport fixture suite remained 73/73.
- Live TransportReceipt.v2 and exact ProviderResult.v3 bytes were submitted together.
- Runtime independently recomputed exact ProviderResult.v3 SHA-256:
  FFDF4893505385327D32F0E65E2A5844CFC294414573D5385E4659B39FD0B220
- Runtime-computed hash exactly matched transport receipt resultSha256.
- Registered transport/provider/request provenance matched TRANSPORT_RECEIPT_MATCHED.
- Existing ProviderResult.v3 admission then accepted KEEP_BASELINE.
- Completion SUCCESS_ACKED removed queue 1 -> 0.
- Replay failed at transport receipt job lookup DISPATCH_JOB_NOT_FOUND; no second nested v3 admission was produced; completion stayed 0 -> 0.
- Every live check true.
- Protected saves matched, disposable cleaned, accepted binaries restored, lease released.

Classification: ACCEPT.

Architecture meaning
Exact ProviderResult.v3 bytes are now cryptographically bound to the transport receipt before advisory admission. A transport cannot claim one result hash while runtime admits different bytes.
""",encoding="utf-8")

plan=R/r"Longitudinal\DerivedReports\2026-09-21_v02161_ExternalTransportExecutionPolicy_Plan.txt"
plan.write_text("""BannerlordAI v0.2.10.61 - External transport execution policy registration shadow

Question
Can every registered external transport request require an explicit, auditable execution policy before any real network/provider call is authorized, without embedding hidden timeout/retry/credential defaults in product code?

Why this gate
- v0.2.10.60 closes request/result transport integrity.
- Real external execution still needs operational bounds: timeout, attempt count, response-size ceiling and credential lookup source.
- DeepAstra prior art supports bounded worker execution/status/timeouts.
- SeverActions supports separating high-level intent from controlled deterministic execution.
- Product should not invent provider-specific retry/cost constants before evidence exists.

Smallest implementation
- Runner-only policy registration; ClanAI unchanged.
- Strict envelope:
  BannerlordAI.PatrolDefenseProviderExecutionPolicy.v1
  fields:
    schema
    transportRequestId
    providerRequestId
    providerId
    modelId
    attemptId
    requestFingerprint
    timeoutMs
    maxAttempts
    maxResultBytes
    credentialRef
- timeoutMs/maxAttempts/maxResultBytes are decimal strings and must parse as positive integers.
- Product provides no defaults; missing/zero/negative/non-numeric values reject.
- credentialRef must be an identifier only and must begin with env:; runtime stores only the environment-variable name, never a secret value.
- Envelope provenance must exactly match the active CLAIMED job + registered provider request + registered transport request.
- Compute executionPolicyId = SHA-256 of exact UTF-8 policy envelope bytes.
- Register one executionPolicyId on the active transport request:
  first exact policy -> REGISTERED;
  byte-identical replay -> idempotent;
  second different policy -> EXECUTION_POLICY_ALREADY_REGISTERED reject.
- Explicit ReleaseClaim clears execution policy registration.
- Command:
  PATROL_DEFENSE_PROVIDER_EXECUTION_POLICY_REGISTER <base64Utf8EnvelopeJson>
- Receipt:
  BannerlordAI.PatrolDefenseProviderExecutionPolicyRegistration.v1
  executionPolicyId,
  transportRequestId,
  providerRequestId,
  providerId/modelId/attempt/fingerprint,
  timeoutMs/maxAttempts/maxResultBytes,
  credentialRef,
  registered/idempotent/reason,
  externalNetworkUsed=false,
  modelInvoked=false,
  executionAuthorized=false.
- Dedicated jsonl registration stream.

Acceptance
A. Runner compiles; ClanAI unchanged.
B. Fixtures:
   exact active transport policy registers;
   exact replay idempotent;
   different policy same transport rejected;
   missing/unclaimed/providerRequest/transport mismatch rejected;
   zero/negative/non-numeric timeout/attempt/result-size rejected;
   credentialRef missing/non-env rejected;
   extra fields rejected;
   release clears policy;
   retry claim+new transport can register new policy;
   zero-authority receipt.
C. Static:
   no HttpClient/socket/OpenAI/provider SDK/model call;
   no credential value logging or environment-variable read in runner;
   no hidden numeric default policy constants.
D. Live deterministic shadow:
   natural request -> claim -> provider-request registration -> transport registration;
   register explicit test policy against deterministic_local_loopback;
   exact replay idempotent;
   no external send/result submission.
E. EXIT_NOSAVE cleanup.

Scope
This gate registers explicit transport operational bounds only. It does not perform a network/model call, choose production limits, read credentials, or authorize gameplay.
""",encoding="utf-8")

arch=R/r"Longitudinal\EvidenceIndex\HandoffV3\ARCHITECTURE_LEARNING.md"
entry="""

## 2026-09-21 - v0.2.10.60 transport receipt / exact result-byte hash binding accepted
- TransportReceipt.v2 is now submitted together with exact ProviderResult.v3 bytes and runtime independently recomputes result SHA-256 before v3 admission.
- Live receipt resultSha256 exactly matched runtime-computed SHA-256; registered transport/provider/request provenance matched before KEEP_BASELINE admission.
- Replay after terminal completion failed job lookup before another nested v3 admission.
- This closes the major transport-integrity gap before real external provider execution.
- Next safe boundary is explicit external transport execution-policy registration: timeout/attempt/result-size bounds and credential-reference identity with no hidden defaults or secret values.
"""
txt=arch.read_text(encoding="utf-8-sig")
if "v0.2.10.60 transport receipt / exact result-byte hash binding accepted" not in txt:
    arch.write_text(txt.rstrip()+entry+"\n",encoding="utf-8")

patch={
 "last_accepted_milestone":{
  "version":"v0.2.10.60-transport-receipt-result-hash-binding-shadow",
  "name":"Transport receipt and exact ProviderResult byte-hash binding shadow",
  "status":"ACCEPTED",
  "result":"225/225 backcompat + 63/63 v3/hash-binding + 73/73 transport fixtures and live TransportReceipt.v2 -> independent exact ProviderResult.v3 SHA-256 match -> TRANSPORT_RECEIPT_MATCHED -> KEEP_BASELINE -> SUCCESS_ACKED 1->0; replay job-missing with no second v3 admission; full cleanup.",
  "validation":val,
  "analyzer_report":str(report),
  "runner_test_sha256":"629C5A24D09CF25A5EB9D0625546B580E0221595D67B158B2464FE16642177FE",
  "clanai_test_sha256":"C437EE12AF5D223EEB1C3CF17D4191522F26EBAC350E4A0BA652A591A82399FD"
 },
 "active_candidate":{
  "version":"v0.2.10.61-external-transport-execution-policy-shadow",
  "name":"Explicit external transport execution policy registration shadow",
  "status":"PLAN_READY",
  "plan":str(plan),
  "base_clanai_staging":r"D:\BannerlordAIResearch\workspace\_PatchStaging\PatrolDefenseRecentOutcomeAge_v02138_20260921_1234\candidate",
  "base_runner_staging":r"D:\BannerlordAIResearch\workspace\_PatchStaging\AutonomousOperator_v002160_TransportReceiptResultHash_20260921_1948\candidate"
 },
 "active_question":"Can every registered transport request carry explicit caller-supplied timeout/attempt/result-size/credential-reference policy before any real external provider execution, with no hidden defaults or secret values?",
 "next_action":"Coder EXECUTE v0.2.10.61 runner-only execution-policy registration gate: add strict policy parser + active transport provenance match + exact executionPolicyId registration/receipt/command, clear policy on release, compile and pass exact/idempotent/different/mismatch/numeric/credential/extra/release-retry/no-authority fixtures, then natural request -> claim -> provider/transport registration -> explicit deterministic-loopback policy registration + idempotent replay live proof with zero external send/model/result submission.",
 "blockers":[],
 "do_not_repeat":list(dict.fromkeys((cur.get("do_not_repeat") or [])+[
  "Do not rerun v0.2.10.60 unchanged; exact TransportReceipt.v2 to ProviderResult.v3 byte-hash binding and live success/replay behavior are accepted.",
  "Do not authorize real network/model execution before an explicit transport execution policy is registered for the exact transport request."
 ]))
}
pp=R/r"workspace\accept_v02160_open_v02161.json"
pp.write_text(json.dumps(patch,indent=2)+"\n",encoding="utf-8")
cp=subprocess.run([
 sys.executable,"-X","utf8",str(R/r"Tools\Handoff\handoff_checkpoint.py"),
 "checkpoint","--patch-file",str(pp),
 "--event-type","MILESTONE_ACCEPTED",
 "--message","v0.2.10.60 accepted: exact TransportReceipt.v2/result-byte SHA-256 binding before ProviderResult.v3 admission. Open v0.2.10.61 explicit external transport execution-policy registration with no hidden defaults or secrets.",
 "--action-id","accept-v02160-open-v02161",
 "--expected-seq",str(cur["checkpoint_seq"])
],capture_output=True,text=True)
print(cp.stdout)
if cp.returncode:
 print(cp.stderr)
 raise SystemExit(cp.returncode)
subprocess.run([sys.executable,"-X","utf8",str(R/r"Tools\Autopilot\sync_thinking_loop.py")],check=True)
