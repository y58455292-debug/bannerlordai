from pathlib import Path
import json,subprocess,sys
R=Path(r"D:\BannerlordAIResearch")
curp=R/r"Longitudinal\EvidenceIndex\HandoffV3\current.json"
cur=json.loads(curp.read_text(encoding="utf-8-sig"))
val=r"D:\BannerlordAIResearch\Longitudinal\LiveValidation\PatrolDefense_v02156_ProviderResultV2Binding_20260921_155502_017712\validation.json"

report=R/r"Longitudinal\DerivedReports\2026-09-21_v02156_ProviderResultV2Binding_FinalAnalyzerReview.txt"
report.write_text("""BannerlordAI v0.2.10.56 FINAL VERIFY - ACCEPT

Decisive evidence
- Runner compiled with 0 errors; ClanAI reused unchanged.
- Combined v2 + accepted v1 back-compat fixture suite passed 206/206.
- External provider-request contract suite remained 49/49 PASS.
- Natural request enqueued and exact attempt-1 claim succeeded.
- Exact outbound provider request was registered with providerRequestId:
  59F4B6CEC45552740060069383759BC2D61FFEB2570F79B233F4AF201EDC4660.
- Synthetic ProviderResult.v2 echoed exact providerId, attemptId, requestFingerprint and providerRequestId.
- Admission order proved:
  claimMatchReason=CLAIM_MATCHED
  providerRequestMatchReason=PROVIDER_REQUEST_MATCHED
  then strict advisory admission.
- providerResultAccepted=true; nested KEEP_BASELINE admitted.
- Dispatch completion SUCCESS_ACKED removed queue 1 -> 0.
- Identical replay failed at claim lookup DISPATCH_JOB_NOT_FOUND before request matching/advisory admission and completion stayed 0 -> 0.
- Accepted ProviderResult.v1 schema/command remained present and fixture-covered.
- Provider invocation stream remained empty; no network/model/Apply authority.
- Every live check passed; protected saves matched, disposable cleaned, binaries restored, lease released.
- Continuity refreshed clean.

Classification: ACCEPT.

Architecture meaning
Provider results can now be bound to the exact registered outbound request instance, not merely the job claim. providerRequestId closes provider/model/prompt/input-instance provenance across request -> result while preserving accepted v1 compatibility.
""",encoding="utf-8")

plan=R/r"Longitudinal\DerivedReports\2026-09-21_v02157_ExternalTransportLoopback_Plan.txt"
plan.write_text("""BannerlordAI v0.2.10.57 - External provider transport adapter contract with deterministic loopback

Question
Can an external transport adapter consume the exact registered provider request and produce a ProviderResult.v2 bound to the same providerRequestId through a reproducible transport contract, before any real network/model provider is connected?

Prior art
- DeepAstra reference: bounded worker task, isolated runs, status/timeouts, provider/cost tracking; explicitly future-reference only.
- BannerlordAI should prefer a lightweight auditable bridge before selecting an external harness/provider.
- v0.2.10.54-.56 now secure outbound request bytes/provenance, runtime registration and result request-ID binding.

Smallest implementation
- External Python transport contract/helper only; feature binaries can reuse v0.2.10.56 unchanged.
- Define deterministic adapter interface:
  transportId
  send(providerRequestEnvelopeJson) -> ProviderResult.v2 JSON.
- Implement deterministic loopback adapter:
  transportId=deterministic_local_loopback
  no socket/HTTP/provider SDK/model call,
  validates strict ProviderRequest.v1 using v0.2.10.54 helper,
  requires registered providerRequestId supplied by runtime registration receipt,
  emits ProviderResult.v2:
    providerId/attemptId/requestFingerprint echoed from exact request,
    providerRequestId echoed from registration,
    status=SUCCESS,
    strict KEEP_BASELINE advisoryBase64.
- Transport receipt schema:
  BannerlordAI.PatrolDefenseProviderTransportReceipt.v1
  transportId,
  providerRequestId,
  providerId,
  modelId,
  attemptId,
  requestFingerprint,
  requestInputSha256,
  resultStatus,
  resultSha256,
  externalNetworkUsed=false,
  modelInvoked=false,
  success=true/false,
  errorCode nullable.
- resultSha256 = SHA-256 exact UTF-8 ProviderResult.v2 JSON bytes.
- Strict transport helper rejects altered request bytes/hash/provenance, missing registration, mismatched providerRequestId and malformed request.
- No timeout/retry/cost constants yet because no real transport exists.

Acceptance
A. External transport helper fixtures:
   exact request + registration -> deterministic result v2;
   same input -> byte-identical result and receipt hashes;
   wrong/missing providerRequestId registration rejected;
   invalid provider request rejected;
   altered input hash rejected;
   malformed request rejected;
   receipt network/model flags false.
B. Integration on unchanged v0.2.10.56 binaries:
   natural job -> claim -> exact provider request registration;
   external loopback consumes exact provider request + registered providerRequestId;
   validator independently hashes ProviderResult.v2 bytes;
   submit returned v2 through existing runtime command;
   PROVIDER_REQUEST_MATCHED -> KEEP_BASELINE -> SUCCESS_ACKED 1->0;
   replay DISPATCH_JOB_NOT_FOUND.
C. Provider invocation stream remains empty; no real network/model call; no Apply authority.
D. EXIT_NOSAVE integrity cleanup.

Scope
This proves transport-adapter composition using deterministic loopback. It does not authorize HTTP, external credentials, model/provider selection, timeouts, backoff, cost limits or gameplay influence.
""",encoding="utf-8")

arch=R/r"Longitudinal\EvidenceIndex\HandoffV3\ARCHITECTURE_LEARNING.md"
entry="""

## 2026-09-21 - v0.2.10.56 registered provider-request ID bound result admission accepted
- ProviderResult.v2 now requires the exact registered providerRequestId in addition to provider/attempt/fingerprint claim identity.
- Live admission proved CLAIM_MATCHED -> PROVIDER_REQUEST_MATCHED -> strict KEEP_BASELINE advisory admission -> SUCCESS_ACKED completion.
- Replay after job removal fails at claim lookup before another request-ID match/admission can occur.
- Accepted ProviderResult.v1 behavior remains unchanged and fixture-covered.
- The request/result provenance chain is now complete enough to isolate transport as its own boundary.
- Next safe gate is a deterministic external transport adapter/loopback contract using unchanged product binaries and explicit no-network/no-model evidence.
"""
txt=arch.read_text(encoding="utf-8-sig")
if "v0.2.10.56 registered provider-request ID bound result admission accepted" not in txt:
    arch.write_text(txt.rstrip()+entry+"\n",encoding="utf-8")

patch={
 "last_accepted_milestone":{
  "version":"v0.2.10.56-provider-result-request-binding-shadow",
  "name":"Registered provider-request ID bound result admission shadow",
  "status":"ACCEPTED",
  "result":"206/206 v2+v1-backcompat fixtures + 49/49 request-contract fixtures plus live exact providerRequestId-bound ProviderResult.v2 -> CLAIM_MATCHED -> PROVIDER_REQUEST_MATCHED -> KEEP_BASELINE -> SUCCESS_ACKED 1->0; replay job-missing, zero network/model/gameplay authority and full cleanup.",
  "validation":val,
  "analyzer_report":str(report)
 },
 "active_candidate":{
  "version":"v0.2.10.57-external-transport-loopback-shadow",
  "name":"External provider transport adapter contract with deterministic loopback",
  "status":"PLAN_READY",
  "plan":str(plan),
  "base_runner_staging":r"D:\BannerlordAIResearch\workspace\_PatchStaging\AutonomousOperator_v002156_ProviderResultV2Binding_20260921_1549\candidate"
 },
 "active_question":"Can an external deterministic transport adapter consume the exact registered provider request and return a byte-auditable ProviderResult.v2 through the existing request-ID admission chain, with no real network/model call?",
 "next_action":"Coder EXECUTE v0.2.10.57 external transport-loopback integration on unchanged v0.2.10.56 binaries: build deterministic transport helper/fixtures over exact ProviderRequest.v1 + runtime providerRequestId, prove byte/hash reproducibility and no-network/model receipt, then natural claim+registration -> loopback ProviderResult.v2 -> existing v2 admission -> SUCCESS_ACKED -> replay rejection live proof.",
 "blockers":[],
 "do_not_repeat":list(dict.fromkeys((cur.get("do_not_repeat") or [])+[
  "Do not rerun v0.2.10.56 unchanged; exact registered providerRequestId result binding, v1 backcompat, completion and replay rejection are accepted.",
  "Do not enable real HTTP/provider credentials/model invocation before deterministic external transport adapter composition is proven."
 ]))
}
pp=R/r"workspace\accept_v02156_open_v02157.json"
pp.write_text(json.dumps(patch,indent=2)+"\n",encoding="utf-8")
cp=subprocess.run([
 sys.executable,"-X","utf8",str(R/r"Tools\Handoff\handoff_checkpoint.py"),
 "checkpoint","--patch-file",str(pp),
 "--event-type","MILESTONE_ACCEPTED",
 "--message","v0.2.10.56 accepted: ProviderResult.v2 is bound to exact registered providerRequestId with v1 backcompat. Open v0.2.10.57 deterministic external transport loopback on unchanged product binaries.",
 "--action-id","accept-v02156-open-v02157",
 "--expected-seq",str(cur["checkpoint_seq"])
],capture_output=True,text=True)
print(cp.stdout)
if cp.returncode:
 print(cp.stderr)
 raise SystemExit(cp.returncode)
subprocess.run([sys.executable,"-X","utf8",str(R/r"Tools\Autopilot\sync_thinking_loop.py")],check=True)
