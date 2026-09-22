from pathlib import Path
import json, subprocess, sys
R=Path(r"D:\\BannerlordAIResearch")
curp=R/r"Longitudinal\\EvidenceIndex\\HandoffV3\\current.json"
cur=json.loads(curp.read_text(encoding="utf-8-sig"))
val=r"D:\\BannerlordAIResearch\\Longitudinal\\LiveValidation\\PatrolDefense_v02157_ExternalTransportLoopback_20260921_164455_964667\\validation.json"

report=R/r"Longitudinal\\DerivedReports\\2026-09-21_v02157_ExternalTransportLoopback_FinalAnalyzerReview.txt"
report.write_text("""BannerlordAI v0.2.10.57 FINAL VERIFY - ACCEPT

Decisive evidence
- Deterministic external transport helper fixtures passed 61/61.
- Reused v0.2.10.56 runner fixtures passed 206/206.
- Reused provider-request contract fixtures passed 49/49.
- Helper and validator py_compile PASS.
- Exact v0.2.10.56 live-tested binary provenance confirmed from accepted validation:
  runner SHA-256=7732C135648EE1E81587EF72E742DEB29260E0CEBD3005009BF66774F413A39C
  ClanAI SHA-256=C437EE12AF5D223EEB1C3CF17D4191522F26EBAC350E4A0BA652A591A82399FD.
- Natural route-eligible request was claimed and registered with providerRequestId
  A5C354AC122346AAC85808C97DECD3BEE9045F2C5FF18968BECBD2D13087543F.
- deterministic_local_loopback consumed the exact registered provider request.
- Transport receipt:
  transportId=deterministic_local_loopback
  resultStatus=SUCCESS
  externalNetworkUsed=false
  modelInvoked=false
  requestInputSha256=C88D29F6BA56B10BA1B46E0F01AA8FA6BE25E86AA9E35BA25CCFD84E384D4574
  resultSha256=1B9F34120DC21FA41CA4330255A8AF290F0C077814F48092F59A83E9AFE6AA85.
- ProviderResult.v2 bytes independently hashed to the same resultSha256.
- Existing runtime ProviderResult.v2 admission matched the registered providerRequestId, admitted strict KEEP_BASELINE, and completion emitted SUCCESS_ACKED queue 1 -> 0.
- Replay completion emitted DISPATCH_JOB_NOT_FOUND queue 0 -> 0.
- Every live check true; provider invocation stream remained empty.
- Protected saves matched, disposable cleaned, accepted binaries restored, lease released.

Classification: ACCEPT.

Architecture meaning
BannerlordAI now has a deterministic external transport adapter contract between registered provider requests and ProviderResult.v2, including byte-auditable request/result hashes. No real network/model provider is connected.
""",encoding="utf-8")

plan=R/r"Longitudinal\\DerivedReports\\2026-09-21_v02158_ProviderTransportRegistration_Plan.txt"
plan.write_text("""BannerlordAI v0.2.10.58 - Runtime provider transport-request registration / transport identity shadow

Question
Can the runtime register one exact transport instance for the active registered provider request before any external send occurs, so eventual provider results can later be bound to an authorized transport provenance chain?

Why this gate
- v0.2.10.55 registers the exact outbound provider request and assigns providerRequestId.
- v0.2.10.56 binds ProviderResult.v2 to providerRequestId.
- v0.2.10.57 proves a deterministic transport adapter and receipt externally.
- The runtime still does not know which transportId/request instance was authorized before send.
- A future real HTTP/provider result should not be accepted solely because it knows providerRequestId; it should eventually prove it came from the registered transport attempt.

Smallest implementation
- Runner-only; ClanAI unchanged.
- Add strict transport-request envelope:
  BannerlordAI.PatrolDefenseProviderTransportRequest.v1
  required fields:
    schema
    transportId
    providerRequestId
    providerId
    modelId
    attemptId
    requestFingerprint
    requestInputSha256
- All values must exactly match the active CLAIMED job and registered provider request metadata.
- Compute transportRequestId = SHA-256 of exact UTF-8 transport-request envelope JSON bytes as received by runtime.
- Store on active job:
  transportRequestId
  transportId.
- Registration rules:
  first exact envelope -> REGISTERED;
  byte-identical replay -> SAME_TRANSPORT_REQUEST_ALREADY_REGISTERED idempotent;
  second different transport envelope for same providerRequest -> TRANSPORT_REQUEST_ALREADY_REGISTERED reject;
  unclaimed/no job/providerRequest mismatch/claim mismatch/inputSha mismatch -> reject.
- Command:
  PATROL_DEFENSE_PROVIDER_TRANSPORT_REGISTER <base64Utf8EnvelopeJson>
- Receipt:
  BannerlordAI.PatrolDefenseProviderTransportRegistration.v1
  transportRequestId,
  transportId,
  providerRequestId,
  providerId,
  modelId,
  attemptId,
  requestFingerprint,
  requestInputSha256,
  registered,
  idempotent,
  reason,
  externalNetworkUsed=false,
  modelInvoked=false,
  executionAuthorized=false,
  zero gameplay authority.
- Dedicated jsonl registration stream.
- Explicit ReleaseClaim clears transport registration along with provider-request registration so retry attempt must register a new transport request.
- Completion removes registration with job.

Acceptance
A. Runner compiles; ClanAI unchanged.
B. Fixtures:
   exact registered provider request + active claim -> transport REGISTERED;
   exact replay idempotent;
   different transport envelope same providerRequest rejected;
   no provider-request registration rejected;
   wrong providerRequestId rejected;
   wrong transport/provider/model/attempt/fingerprint/inputSha rejected;
   unclaimed/missing job rejected;
   malformed/extra fields rejected;
   release clears transport registration;
   retry claim/register can create new transportRequestId;
   zero-authority receipt.
C. Static: no HTTP/socket/provider SDK/OpenAI/model call/Apply/score/memory write.
D. Live:
   natural request -> claim -> exact provider-request registration;
   external validator builds transport-request envelope transportId=deterministic_local_loopback from exact registered metadata;
   runtime registration succeeds and returns transportRequestId;
   exact replay idempotent;
   no transport send/result submitted in this milestone;
   provider/model/network invocation streams remain empty.
E. EXIT_NOSAVE integrity cleanup.

Scope
This registers transport provenance before send only. ProviderResult is not yet required to echo transportRequestId; that result-transport binding is the next gate.
""",encoding="utf-8")

arch=R/r"Longitudinal\\EvidenceIndex\\HandoffV3\\ARCHITECTURE_LEARNING.md"
entry="""

## 2026-09-21 - v0.2.10.57 deterministic external transport loopback accepted
- The accepted v0.2.10.56 live-tested binaries were reused unchanged.
- deterministic_local_loopback consumed the exact registered ProviderRequest.v1 and emitted byte-auditable ProviderResult.v2 plus ProviderTransportReceipt.v1.
- Request and result SHA-256 values were independently verified; no socket/HTTP/provider SDK/model call existed.
- Existing runtime v2 admission matched providerRequestId, admitted KEEP_BASELINE, completed SUCCESS_ACKED 1 -> 0, and replay failed job lookup.
- Provenance metadata correction: accepted v0.2.10.56 validation is authoritative for tested staging hashes; stale summary hash fields must not override raw live validation.
- Remaining provenance gap: runtime has not registered which transport instance/transportId was authorized before external send.
- Next safe gate is exact runtime transport-request registration and transportRequestId assignment before real network integration.
"""
txt=arch.read_text(encoding="utf-8-sig")
if "v0.2.10.57 deterministic external transport loopback accepted" not in txt:
    arch.write_text(txt.rstrip()+entry+"\n",encoding="utf-8")

patch={
 "last_accepted_milestone":{
  "version":"v0.2.10.57-external-transport-loopback-shadow",
  "name":"External provider transport adapter contract with deterministic loopback",
  "status":"ACCEPTED",
  "result":"61/61 transport + 206/206 runner + 49/49 request-contract fixtures plus live registered providerRequest -> deterministic_local_loopback -> byte-auditable ProviderResult.v2 -> existing request-ID admission -> SUCCESS_ACKED 1->0 -> replay job-missing; zero network/model/gameplay authority and full cleanup.",
  "validation":val,
  "analyzer_report":str(report),
  "runner_test_sha256":"7732C135648EE1E81587EF72E742DEB29260E0CEBD3005009BF66774F413A39C",
  "clanai_test_sha256":"C437EE12AF5D223EEB1C3CF17D4191522F26EBAC350E4A0BA652A591A82399FD"
 },
 "active_candidate":{
  "version":"v0.2.10.58-provider-transport-registration-shadow",
  "name":"Runtime provider transport-request registration and transport identity shadow",
  "status":"PLAN_READY",
  "plan":str(plan),
  "base_clanai_staging":r"D:\\BannerlordAIResearch\\workspace\\_PatchStaging\\PatrolDefenseRecentOutcomeAge_v02138_20260921_1234\\candidate",
  "base_runner_staging":r"D:\\BannerlordAIResearch\\workspace\\_PatchStaging\\AutonomousOperator_v002156_ProviderResultV2Binding_20260921_1549\\candidate"
 },
 "active_question":"Can the runtime register one exact transport request/transportId for the active registered provider request and assign transportRequestId before any external send occurs?",
 "next_action":"Coder EXECUTE v0.2.10.58 runner-only provider transport registration gate: add strict transport-request envelope parser + active claim/providerRequest metadata match + transportRequestId registration/receipt/command, clear registration on release, compile and pass register/idempotent/different/no-provider-request/mismatch/unclaimed/malformed/release-retry/no-authority fixtures, then natural claim+providerRequest registration -> exact deterministic_local_loopback transport registration + idempotent replay live proof with zero network/model/result submission.",
 "blockers":[],
 "do_not_repeat":list(dict.fromkeys((cur.get("do_not_repeat") or [])+[
  "Do not rerun v0.2.10.57 unchanged; deterministic external transport loopback and byte-auditable ProviderResult.v2 integration are accepted.",
  "Do not authorize a real external send before runtime registers the exact transport request instance/transportId."
 ]))
}
pp=R/r"workspace\\accept_v02157_open_v02158.json"
pp.write_text(json.dumps(patch,indent=2)+"\n",encoding="utf-8")
cp=subprocess.run([
 sys.executable,"-X","utf8",str(R/r"Tools\\Handoff\\handoff_checkpoint.py"),
 "checkpoint","--patch-file",str(pp),
 "--event-type","MILESTONE_ACCEPTED",
 "--message","v0.2.10.57 accepted: deterministic external transport loopback with exact request/result hashes and existing ProviderResult.v2 admission. Open v0.2.10.58 runtime transport-request registration/transportRequestId shadow.",
 "--action-id","accept-v02157-open-v02158",
 "--expected-seq",str(cur["checkpoint_seq"])
],capture_output=True,text=True)
print(cp.stdout)
if cp.returncode:
 print(cp.stderr)
 raise SystemExit(cp.returncode)
subprocess.run([sys.executable,"-X","utf8",str(R/r"Tools\\Autopilot\\sync_thinking_loop.py")],check=True)
