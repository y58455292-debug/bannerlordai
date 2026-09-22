from pathlib import Path
import json, subprocess, sys
R=Path(r"D:\\BannerlordAIResearch")
curp=R/r"Longitudinal\\EvidenceIndex\\HandoffV3\\current.json"
cur=json.loads(curp.read_text(encoding="utf-8-sig"))
val=r"D:\\BannerlordAIResearch\\Longitudinal\\LiveValidation\\PatrolDefense_v02155_ProviderRequestRegistration_20260921_154454_293052\\validation.json"

report=R/r"Longitudinal\\DerivedReports\\2026-09-21_v02155_ProviderRequestRegistration_FinalAnalyzerReview.txt"
report.write_text("""BannerlordAI v0.2.10.55 FINAL VERIFY - ACCEPT

Decisive evidence
- Runner compiled with 0 errors; ClanAI reused unchanged.
- Runner fixture suite passed 190/190.
- External provider-request contract fixture suite remained 49/49 PASS.
- Natural request enqueued one job and exact claim attempt-1 succeeded.
- Runtime decoded and revalidated the exact v0.2.10.54 provider request envelope against the CLAIMED job.
- First registration succeeded:
  registered=true
  reason=REGISTERED
  providerRequestId=FBCB2459AF8AE4AB252354E386EDF636FFCD08D7E7B4B9A12FB8040D938B6E77.
- providerRequestId independently recomputed from exact UTF-8 envelope JSON bytes matched.
- Exact envelope replay was idempotent:
  registered=false
  idempotent=true
  reason=SAME_PROVIDER_REQUEST_ALREADY_REGISTERED
  same providerRequestId.
- Fixtures prove different second envelope rejection, unclaimed/wrong-provider/wrong-attempt/wrong-fingerprint rejection, altered bytes/hash rejection, wrong prompt/response schema rejection, release clears registration, and retry attempt can register a new request.
- Provider invocation/result streams remained empty; no network/model/Apply authority.
- Protected saves matched, disposable cleaned, binaries restored, lease released.
- Continuity refreshed clean.

Classification: ACCEPT.

Architecture meaning
The runtime now knows exactly which outbound provider request instance is authorized for an active claim. One immutable providerRequestId binds the exact envelope bytes, provider/model/attempt/input hash, prompt contract and expected response schema before any provider call exists.
""",encoding="utf-8")

plan=R/r"Longitudinal\\DerivedReports\\2026-09-21_v02156_ProviderResultRequestBinding_Plan.txt"
plan.write_text("""BannerlordAI v0.2.10.56 - Registered provider-request ID bound result admission shadow

Question
Can provider results be accepted only when they echo the exact providerRequestId registered for the active claimed job, in addition to matching providerId + attemptId + requestFingerprint?

Why this gate
- v0.2.10.55 registers one exact outbound provider request per active claim.
- v0.2.10.51 binds results to the claim, but not yet to the exact outbound provider request/model/prompt/input instance.
- Before real provider transport, a result from a different request instance must be unable to satisfy the claim.

Compatibility
- Keep accepted BannerlordAI.PatrolDefenseProviderResult.v1 behavior unchanged.
- Introduce new BannerlordAI.PatrolDefenseProviderResult.v2 for request-bound results.
- New v2 required fields:
  schema
  providerId
  attemptId
  requestFingerprint
  providerRequestId
  status
  advisoryBase64.

Smallest implementation
- Runner-only; ClanAI unchanged.
- Add non-mutating MatchRegisteredProviderRequest on dispatch queue:
  missing job -> DISPATCH_JOB_NOT_FOUND;
  not CLAIMED -> PROVIDER_JOB_NOT_CLAIMED;
  claim provider/attempt mismatch -> PROVIDER_CLAIM_MISMATCH;
  no providerRequestId registered -> PROVIDER_REQUEST_NOT_REGISTERED;
  wrong providerRequestId -> PROVIDER_REQUEST_ID_MISMATCH;
  exact -> PROVIDER_REQUEST_MATCHED.
- Add strict v2 result parser/admission:
  exact v2 field set only;
  validate provider/attempt/fingerprint/requestId;
  require MatchRegisteredProviderRequest before pending-request advisory admission;
  SUCCESS then decodes advisory and passes through existing strict advisory gate;
  TRANSIENT/PERMANENT failure semantics remain identical after exact request match.
- Command:
  PATROL_DEFENSE_PROVIDER_RESULT_V2_ADMIT <base64Utf8EnvelopeJson>
- Dedicated receipt stream:
  patrol_defense_provider_result_v2_admissions.jsonl
  schema BannerlordAI.PatrolDefenseProviderResultV2Admission.v1
  includes providerRequestId and providerRequestMatchReason.
- Dispatch completion lifecycle remains unchanged after admission result.

Acceptance
A. Runner compiles; ClanAI unchanged.
B. Fixtures:
   exact registered request + SUCCESS admitted;
   no request registration rejected PROVIDER_REQUEST_NOT_REGISTERED;
   wrong providerRequestId rejected and pending retained;
   wrong provider/attempt/fingerprint rejected;
   exact TRANSIENT failure retains job;
   exact PERMANENT failure removes through completion;
   malformed/extra fields rejected;
   exact SUCCESS consumes pending and completion removes job;
   replay rejected/no second removal;
   accepted v1 fixtures remain unchanged and PASS.
C. Static ordering: v2 envelope parse -> claim match -> providerRequestId match -> strict advisory gate -> completion.
D. Live:
   natural request -> claim attempt-1 -> register exact provider request -> synthetic v2 SUCCESS with same providerRequestId;
   providerRequestMatchReason=PROVIDER_REQUEST_MATCHED;
   nested KEEP_BASELINE admitted;
   SUCCESS_ACKED queue 1->0;
   replay rejects DISPATCH_JOB_NOT_FOUND.
E. Provider/model/network invocation streams remain empty; no Apply authority; full cleanup.

Scope
This binds results to the exact registered outbound request instance. Still no real provider/model call.
""",encoding="utf-8")

arch=R/r"Longitudinal\\EvidenceIndex\\HandoffV3\\ARCHITECTURE_LEARNING.md"
entry="""

## 2026-09-21 - v0.2.10.55 runtime provider-request registration accepted
- The runtime now independently validates exact outbound provider request bytes/hash/provenance against the active CLAIMED dispatch job and registers one immutable providerRequestId.
- Exact envelope replay is idempotent; a different second envelope for the same claim is rejected.
- Explicit claim release clears registered request metadata so a retry attempt must register its own outbound request.
- providerRequestId is SHA-256 of the exact UTF-8 provider-request envelope JSON bytes, while inputSha256 separately protects exact deliberation input bytes.
- Remaining provenance gap: accepted provider result v1 binds provider/attempt/fingerprint but does not echo or prove the exact registered providerRequestId.
- Next safe gate introduces a separate v2 result contract bound to providerRequestId without mutating accepted v1 behavior.
"""
txt=arch.read_text(encoding="utf-8-sig")
if "v0.2.10.55 runtime provider-request registration accepted" not in txt:
    arch.write_text(txt.rstrip()+entry+"\n",encoding="utf-8")

patch={
 "last_accepted_milestone":{
  "version":"v0.2.10.55-provider-request-registration-shadow",
  "name":"Runtime provider-request registration and outbound contract admission shadow",
  "status":"ACCEPTED",
  "result":"190/190 runner fixtures + 49/49 request-contract fixtures plus live exact registration/idempotent replay: runtime recomputed providerRequestId from exact envelope bytes, provider/model/attempt/input/prompt/schema provenance fixed, zero result/network/model/gameplay authority and full cleanup.",
  "validation":val,
  "analyzer_report":str(report)
 },
 "active_candidate":{
  "version":"v0.2.10.56-provider-result-request-binding-shadow",
  "name":"Registered provider-request ID bound result admission shadow",
  "status":"PLAN_READY",
  "plan":str(plan),
  "base_clanai_staging":r"D:\\BannerlordAIResearch\\workspace\\_PatchStaging\\PatrolDefenseRecentOutcomeAge_v02138_20260921_1234\\candidate",
  "base_runner_staging":r"D:\\BannerlordAIResearch\\workspace\\_PatchStaging\\AutonomousOperator_v002155_ProviderRequestRegistration_20260921_1541\\candidate"
 },
 "active_question":"Can a provider result be admitted only when it matches the active claim and echoes the exact registered providerRequestId for the outbound request instance?",
 "next_action":"Coder EXECUTE v0.2.10.56 runner-only v2 provider-result binding gate: add exact registered-request match + strict ProviderResult.v2 parser/command/receipt, keep v1 unchanged, compile and pass registered/no-registration/wrong-requestId/mismatch/transient/permanent/malformed/success/replay/backcompat/no-authority fixtures, then natural claim -> request registration -> exact v2 SUCCESS -> PROVIDER_REQUEST_MATCHED -> KEEP_BASELINE -> SUCCESS_ACKED live proof.",
 "blockers":[],
 "do_not_repeat":list(dict.fromkeys((cur.get("do_not_repeat") or [])+[
  "Do not rerun v0.2.10.55 unchanged; exact runtime provider request registration/providerRequestId and idempotent replay are accepted.",
  "Do not accept real provider results until the result echoes and matches the exact registered providerRequestId."
 ]))
}
pp=R/r"workspace\\accept_v02155_open_v02156.json"
pp.write_text(json.dumps(patch,indent=2)+"\n",encoding="utf-8")
cp=subprocess.run([
 sys.executable,"-X","utf8",str(R/r"Tools\\Handoff\\handoff_checkpoint.py"),
 "checkpoint","--patch-file",str(pp),
 "--event-type","MILESTONE_ACCEPTED",
 "--message","v0.2.10.55 accepted: runtime registers one exact outbound provider request and immutable providerRequestId per active claim. Open v0.2.10.56 separate v2 result contract bound to registered providerRequestId.",
 "--action-id","accept-v02155-open-v02156",
 "--expected-seq",str(cur["checkpoint_seq"])
],capture_output=True,text=True)
print(cp.stdout)
if cp.returncode:
 print(cp.stderr)
 raise SystemExit(cp.returncode)
subprocess.run([sys.executable,"-X","utf8",str(R/r"Tools\\Autopilot\\sync_thinking_loop.py")],check=True)
