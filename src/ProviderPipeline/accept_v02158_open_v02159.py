from pathlib import Path
import json, subprocess, sys
R=Path(r"D:\\BannerlordAIResearch")
curp=R/r"Longitudinal\\EvidenceIndex\\HandoffV3\\current.json"
cur=json.loads(curp.read_text(encoding="utf-8-sig"))
val=r"D:\\BannerlordAIResearch\\Longitudinal\\LiveValidation\\PatrolDefense_v02158_ProviderTransportRegistration_20260921_165415_866077\\validation.json"

report=R/r"Longitudinal\\DerivedReports\\2026-09-21_v02158_ProviderTransportRegistration_FinalAnalyzerReview.txt"
report.write_text("""BannerlordAI v0.2.10.58 FINAL VERIFY - ACCEPT

Decisive evidence
- Runner compiled with 0 errors; ClanAI reused unchanged.
- Transport registration fixture suite passed 225/225.
- Provider-request contract fixture suite passed 49/49.
- Natural request was claimed attempt-1 and exact provider request registered first.
- Runtime accepted strict transport envelope:
  transportId=deterministic_local_loopback
  transportRequestId=2C105C64263F1BFF5CEF68E95CFAF658DE7A7F831DAFAFE2919B8B23A2F1B50D.
- Runtime-computed transportRequestId exactly matched the independent SHA-256 of the exact transport-envelope UTF-8 bytes.
- Exact registration replay was idempotent with reason SAME_TRANSPORT_REQUEST_ALREADY_REGISTERED.
- Fixtures prove second different transport envelope rejection, provider-request-not-registered, providerRequestId/provider/model/attempt/fingerprint/input-hash mismatches, unclaimed/missing/malformed/extra-field rejection, release cleanup and retry re-registration.
- No transport send occurred.
- Provider invocation and provider-result streams remained empty.
- No network/model/Apply authority.
- Every live check passed; protected saves matched, disposable cleaned, binaries restored, lease released.

Classification: ACCEPT.

Architecture meaning
Runtime now records the exact authorized transport instance for a registered provider request before any send occurs. Transport provenance has an immutable transportRequestId and idempotent replay semantics.
""",encoding="utf-8")

plan=R/r"Longitudinal\\DerivedReports\\2026-09-21_v02159_TransportBoundProviderResult_Plan.txt"
plan.write_text("""BannerlordAI v0.2.10.59 - Transport-bound ProviderResult.v3 admission shadow

Question
Can a provider result be admitted only when it proves the exact registered transportRequestId in addition to the active claim and providerRequestId?

Why this gate
- v0.2.10.55 registers providerRequestId.
- v0.2.10.56 binds ProviderResult.v2 to providerRequestId.
- v0.2.10.58 registers transportRequestId before send.
- ProviderResult.v2 does not yet prove which authorized transport instance produced the result.

Smallest implementation
- Runner-only result-contract extension plus deterministic external helper; ClanAI unchanged.
- Preserve ProviderResult.v1 and v2 behavior for backward-compat fixtures.
- Add BannerlordAI.PatrolDefenseProviderResult.v3 required fields:
  schema
  providerId
  attemptId
  requestFingerprint
  providerRequestId
  transportRequestId
  status
  advisoryBase64.
- Admission ordering:
  strict envelope parse;
  active claim match providerId+attemptId+fingerprint;
  registered providerRequestId match;
  registered transportRequestId match;
  then existing strict advisory admission;
  then existing dispatch completion lifecycle.
- Add non-mutating queue transport match:
  no job -> DISPATCH_JOB_NOT_FOUND;
  not claimed -> PROVIDER_JOB_NOT_CLAIMED;
  claim mismatch -> PROVIDER_CLAIM_MISMATCH;
  provider request missing/mismatch -> existing reasons;
  transport request missing -> TRANSPORT_REQUEST_NOT_REGISTERED;
  transportRequestId mismatch -> TRANSPORT_REQUEST_ID_MISMATCH;
  exact -> TRANSPORT_REQUEST_MATCHED.
- Command:
  PATROL_DEFENSE_PROVIDER_RESULT_V3_ADMIT <base64Utf8Json>.
- Receipt:
  BannerlordAI.PatrolDefenseProviderResultV3Admission.v1
  claimMatchReason,
  providerRequestMatchReason,
  transportRequestMatchReason,
  providerRequestId,
  transportRequestId,
  advisoryAdmission,
  existing zero-authority flags.
- Deterministic external loopback v3 helper echoes exact providerRequestId + transportRequestId and produces strict KEEP_BASELINE advisory.
- No real network/model call.

Acceptance
A. Runner compiles; ClanAI unchanged.
B. Fixtures:
   exact claim + provider request + transport request + v3 SUCCESS admitted;
   transport not registered rejected;
   wrong transportRequestId rejected without consuming pending request;
   wrong providerRequestId rejected;
   wrong provider/attempt rejected;
   exact TRANSIENT/PERMANENT failures preserve accepted lifecycle semantics;
   malformed/extra fields rejected;
   replay after SUCCESS fails job lookup;
   v2 and v1 backward-compat fixtures remain PASS;
   zero-authority receipt.
C. External v3 loopback fixtures:
   exact registered request+transport -> byte-deterministic ProviderResult.v3;
   echoes both request IDs;
   independent result hash match;
   no network/model.
D. Live:
   natural job -> claim -> provider request registration -> transport registration;
   deterministic loopback v3 returns exact providerRequestId+transportRequestId;
   runtime admission reports CLAIM_MATCHED -> PROVIDER_REQUEST_MATCHED -> TRANSPORT_REQUEST_MATCHED -> KEEP_BASELINE;
   SUCCESS_ACKED queue 1->0;
   replay DISPATCH_JOB_NOT_FOUND.
E. Full EXIT_NOSAVE cleanup.

Scope
This closes transport-to-result provenance. No real network/model provider, timeout/backoff/cost policy, or gameplay influence yet.
""",encoding="utf-8")

arch=R/r"Longitudinal\\EvidenceIndex\\HandoffV3\\ARCHITECTURE_LEARNING.md"
entry="""

## 2026-09-21 - v0.2.10.58 runtime transport-request registration accepted
- Runtime now registers one exact transport request against an active claimed job and already registered providerRequestId.
- transportRequestId is the SHA-256 of the exact UTF-8 transport-envelope bytes received by runtime.
- Exact replay is idempotent; a different second transport envelope is rejected.
- Registration requires exact provider/model/attempt/request/input-hash provenance and is cleared on explicit claim release.
- No transport send/result/model call occurred in the acceptance run.
- Remaining gap: ProviderResult.v2 is bound to providerRequestId but does not echo/match transportRequestId.
- Next safe gate is ProviderResult.v3 transport-bound admission.
"""
txt=arch.read_text(encoding="utf-8-sig")
if "v0.2.10.58 runtime transport-request registration accepted" not in txt:
    arch.write_text(txt.rstrip()+entry+"\n",encoding="utf-8")

patch={
 "last_accepted_milestone":{
  "version":"v0.2.10.58-provider-transport-registration-shadow",
  "name":"Runtime provider transport-request registration and transport identity shadow",
  "status":"ACCEPTED",
  "result":"225/225 runner + 49/49 request-contract fixtures plus live exact deterministic_local_loopback transport registration with independently matched transportRequestId and idempotent replay; zero transport send/provider result/network/model/gameplay authority and full cleanup.",
  "validation":val,
  "analyzer_report":str(report),
  "runner_test_sha256":"B7D93EBADF946A3781EF89BF71BA567B74F89C23A30680F4794D0A0FEA98A657",
  "clanai_test_sha256":"C437EE12AF5D223EEB1C3CF17D4191522F26EBAC350E4A0BA652A591A82399FD"
 },
 "active_candidate":{
  "version":"v0.2.10.59-transport-bound-provider-result-v3-shadow",
  "name":"Transport-bound ProviderResult.v3 admission shadow",
  "status":"PLAN_READY",
  "plan":str(plan),
  "base_clanai_staging":r"D:\\BannerlordAIResearch\\workspace\\_PatchStaging\\PatrolDefenseRecentOutcomeAge_v02138_20260921_1234\\candidate",
  "base_runner_staging":r"D:\\BannerlordAIResearch\\workspace\\_PatchStaging\\AutonomousOperator_v002158_TransportRegistration_20260921_1650\\candidate"
 },
 "active_question":"Can provider results be admitted only when providerId+attemptId+fingerprint, providerRequestId and transportRequestId all match the active registered provenance chain?",
 "next_action":"Coder EXECUTE v0.2.10.59 transport-bound ProviderResult.v3 gate: add exact registered-transport match and v3 result parser/command/receipt while preserving v1/v2 backcompat, build deterministic v3 loopback helper, compile and pass exact/no-transport/wrong-transport/wrong-request/claim/failure/malformed/replay/backcompat/no-authority fixtures, then natural claim+providerRequest+transport registration -> v3 loopback -> TRANSPORT_REQUEST_MATCHED -> KEEP_BASELINE -> SUCCESS_ACKED 1->0 -> replay job-missing live proof.",
 "blockers":[],
 "do_not_repeat":list(dict.fromkeys((cur.get("do_not_repeat") or [])+[
  "Do not rerun v0.2.10.58 unchanged; exact transportRequestId registration/idempotency and zero-send live proof are accepted.",
  "Do not accept a real transport/provider result before result admission is bound to the registered transportRequestId."
 ]))
}
pp=R/r"workspace\\accept_v02158_open_v02159.json"
pp.write_text(json.dumps(patch,indent=2)+"\n",encoding="utf-8")
cp=subprocess.run([
 sys.executable,"-X","utf8",str(R/r"Tools\\Handoff\\handoff_checkpoint.py"),
 "checkpoint","--patch-file",str(pp),
 "--event-type","MILESTONE_ACCEPTED",
 "--message","v0.2.10.58 accepted: runtime exact transport-request registration/transportRequestId with idempotent replay and no send. Open v0.2.10.59 transport-bound ProviderResult.v3 admission.",
 "--action-id","accept-v02158-open-v02159",
 "--expected-seq",str(cur["checkpoint_seq"])
],capture_output=True,text=True)
print(cp.stdout)
if cp.returncode:
 print(cp.stderr)
 raise SystemExit(cp.returncode)
subprocess.run([sys.executable,"-X","utf8",str(R/r"Tools\\Autopilot\\sync_thinking_loop.py")],check=True)
