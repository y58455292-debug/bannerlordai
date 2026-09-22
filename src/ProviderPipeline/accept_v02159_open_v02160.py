from pathlib import Path
import json, subprocess, sys
R=Path(r"D:\BannerlordAIResearch")
curp=R/r"Longitudinal\EvidenceIndex\HandoffV3\current.json"
cur=json.loads(curp.read_text(encoding="utf-8-sig"))
val=r"D:\BannerlordAIResearch\Longitudinal\LiveValidation\PatrolDefense_v02159_TransportBoundProviderResultV3_20260921_170958_052032\validation.json"

report=R/r"Longitudinal\DerivedReports\2026-09-21_v02159_TransportBoundProviderResultV3_FinalAnalyzerReview.txt"
report.write_text("""BannerlordAI v0.2.10.59 FINAL VERIFY - ACCEPT

Decisive evidence
- Transport-bound ProviderResult.v3 live validation PASS.
- Exact active claim matched.
- Exact registered providerRequestId matched.
- Exact registered transportRequestId matched.
- Runtime admission sequence reported:
  CLAIM_MATCHED
  PROVIDER_REQUEST_MATCHED
  TRANSPORT_REQUEST_MATCHED.
- providerResultAccepted=true.
- Nested advisory admitted=true / disposition=KEEP_BASELINE.
- Dispatch completion SUCCESS_ACKED removed queue 1 -> 0.
- Replay after completion rejected DISPATCH_JOB_NOT_FOUND and completion remained 0 -> 0.
- Every live check true.
- Protected saves matched, disposable saves cleaned, accepted binaries restored, lease released.
- No provider/network/model/gameplay authority was introduced.

Classification: ACCEPT.

Architecture meaning
Provider results are now bound to the exact authorized transport request in addition to claim and provider-request identity. A result cannot be accepted merely by knowing providerRequestId; it must prove the registered transportRequestId too.
""",encoding="utf-8")

plan=R/r"Longitudinal\DerivedReports\2026-09-21_v02160_TransportReceiptResultHashBinding_Plan.txt"
plan.write_text("""BannerlordAI v0.2.10.60 - Transport receipt / exact result-byte hash binding shadow

Question
Can the runtime verify that the exact ProviderResult.v3 bytes submitted for admission are the same bytes the registered transport receipt claims it produced, before any real network provider is connected?

Why this gate
- v0.2.10.58 registers the authorized transportRequestId.
- v0.2.10.59 requires ProviderResult.v3 to echo the exact transportRequestId.
- v0.2.10.57 transport receipt already records resultSha256 over exact ProviderResult bytes.
- Runtime result admission does not yet verify a submitted transport receipt or independently bind result bytes to resultSha256.

Smallest implementation
- Runner-only receipt/result binding; ClanAI unchanged.
- Add strict transport receipt envelope:
  BannerlordAI.PatrolDefenseProviderTransportReceipt.v1
  transportId
  providerRequestId
  providerId
  modelId
  attemptId
  requestFingerprint
  requestInputSha256
  resultStatus
  resultSha256
  externalNetworkUsed
  modelInvoked
  success
  errorCode.
- Add command that submits one base64 transport receipt plus one base64 ProviderResult.v3 JSON payload together.
- Admission ordering:
  parse strict transport receipt;
  parse strict ProviderResult.v3;
  active claim/providerRequest/transportRequest match as in v0.2.10.59;
  transport receipt transportId must match registered transport request;
  receipt providerRequestId/providerId/modelId/attemptId/fingerprint/requestInputSha256 must match registered job/request metadata;
  independently SHA-256 exact ProviderResult.v3 UTF-8 bytes;
  computed hash must equal receipt.resultSha256;
  receipt.resultStatus must equal result.status;
  only then run existing v3 advisory admission and dispatch completion.
- Add receipt-binding audit fields:
  transportReceiptMatchReason,
  resultSha256,
  computedResultSha256.
- Exact same bytes + receipt replay after SUCCESS must fail job lookup as before.
- Wrong hash/transport/provider/model/attempt/fingerprint/request hash/status must reject without consuming pending request or removing queue work.

Acceptance
A. Runner compiles; ClanAI unchanged.
B. Fixtures:
   exact receipt + exact v3 result admitted;
   resultSha mismatch rejected;
   altered result bytes rejected;
   wrong transportId/providerRequestId/provider/model/attempt/fingerprint/requestInputSha/status rejected;
   malformed/extra receipt fields rejected;
   active claim/provider-request/transport-request mismatch families still reject;
   exact SUCCESS completes 1->0;
   replay fails job lookup;
   v1/v2/v3 backward compatibility fixtures remain PASS;
   zero-authority receipt.
C. External deterministic loopback fixtures:
   same ProviderResult.v3 bytes produce identical resultSha256;
   altered byte changes hash;
   receipt/result hash independently recomputes;
   no network/model.
D. Live:
   natural job -> claim -> provider-request registration -> transport registration;
   deterministic loopback returns transport receipt + ProviderResult.v3;
   runtime independently computes result hash and reports exact transport receipt match;
   KEEP_BASELINE admitted; SUCCESS_ACKED 1->0;
   replay job-missing.
E. EXIT_NOSAVE cleanup.

Scope
This closes transport receipt to result-byte integrity. No real HTTP/provider credentials/model invocation, timeout/backoff/cost policy, or gameplay authority yet.
""",encoding="utf-8")

arch=R/r"Longitudinal\EvidenceIndex\HandoffV3\ARCHITECTURE_LEARNING.md"
entry="""

## 2026-09-21 - v0.2.10.59 transport-bound ProviderResult.v3 accepted
- ProviderResult.v3 now requires exact active claim, registered providerRequestId and registered transportRequestId before strict advisory admission.
- Live result reported CLAIM_MATCHED -> PROVIDER_REQUEST_MATCHED -> TRANSPORT_REQUEST_MATCHED, admitted KEEP_BASELINE and completed SUCCESS_ACKED 1 -> 0.
- Replay after completion failed DISPATCH_JOB_NOT_FOUND.
- Remaining integrity gap: runtime does not yet bind the exact result bytes it receives to the transport receipt's resultSha256.
- Next safe gate is strict transport-receipt/result-byte hash binding before real network/model integration.
"""
txt=arch.read_text(encoding="utf-8-sig")
if "v0.2.10.59 transport-bound ProviderResult.v3 accepted" not in txt:
    arch.write_text(txt.rstrip()+entry+"\n",encoding="utf-8")

patch={
 "last_accepted_milestone":{
  "version":"v0.2.10.59-transport-bound-provider-result-v3-shadow",
  "name":"Transport-bound ProviderResult.v3 admission shadow",
  "status":"ACCEPTED",
  "result":"Live exact claim + providerRequestId + transportRequestId matched; ProviderResult.v3 KEEP_BASELINE admitted; SUCCESS_ACKED 1->0; replay DISPATCH_JOB_NOT_FOUND; all live checks true, zero network/model/gameplay authority and full cleanup.",
  "validation":val,
  "analyzer_report":str(report)
 },
 "active_candidate":{
  "version":"v0.2.10.60-transport-receipt-result-hash-binding-shadow",
  "name":"Transport receipt and exact ProviderResult byte-hash binding shadow",
  "status":"PLAN_READY",
  "plan":str(plan),
  "base_clanai_staging":cur.get("active_candidate",{}).get("base_clanai_staging"),
  "base_runner_staging":cur.get("active_candidate",{}).get("candidate_runner")
 },
 "active_question":"Can runtime admission independently bind exact ProviderResult.v3 bytes to the deterministic transport receipt resultSha256 and all registered transport/provider/request provenance before strict advisory admission?",
 "next_action":"Coder EXECUTE v0.2.10.60 runner-only transport-receipt/result-byte binding gate: add strict transport receipt parser + combined receipt/result command + exact SHA-256 comparison and metadata match before existing v3 admission, compile and pass exact/hash/altered-byte/provenance/status/malformed/backcompat/no-authority fixtures, then deterministic loopback live receipt+result proof -> KEEP_BASELINE -> SUCCESS_ACKED -> replay job-missing.",
 "blockers":[],
 "do_not_repeat":list(dict.fromkeys((cur.get("do_not_repeat") or [])+[
  "Do not rerun v0.2.10.59 unchanged; transportRequestId-bound ProviderResult.v3 admission and live success/replay behavior are accepted.",
  "Do not authorize real network/model transport before exact transport-receipt/result-byte hash binding is proven."
 ]))
}
pp=R/r"workspace\accept_v02159_open_v02160.json"
pp.write_text(json.dumps(patch,indent=2)+"\n",encoding="utf-8")
cp=subprocess.run([
 sys.executable,"-X","utf8",str(R/r"Tools\Handoff\handoff_checkpoint.py"),
 "checkpoint","--patch-file",str(pp),
 "--event-type","MILESTONE_ACCEPTED",
 "--message","v0.2.10.59 accepted: ProviderResult.v3 bound to exact active claim + providerRequestId + transportRequestId before advisory admission. Open v0.2.10.60 transport receipt/result byte-hash binding.",
 "--action-id","accept-v02159-open-v02160",
 "--expected-seq",str(cur["checkpoint_seq"])
],capture_output=True,text=True)
print(cp.stdout)
if cp.returncode:
 print(cp.stderr)
 raise SystemExit(cp.returncode)
subprocess.run([sys.executable,"-X","utf8",str(R/r"Tools\Autopilot\sync_thinking_loop.py")],check=True)
