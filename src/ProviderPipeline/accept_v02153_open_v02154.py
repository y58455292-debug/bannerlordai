from pathlib import Path
import json, subprocess, sys
R=Path(r"D:\\BannerlordAIResearch")
curp=R/r"Longitudinal\\EvidenceIndex\\HandoffV3\\current.json"
cur=json.loads(curp.read_text(encoding="utf-8-sig"))
val=r"D:\\BannerlordAIResearch\\Longitudinal\\LiveValidation\\PatrolDefense_v02153_RetryAttemptRotation_20260921_151659_876002\\validation.json"

report=R/r"Longitudinal\\DerivedReports\\2026-09-21_v02153_RetryAttemptRotation_FinalAnalyzerReview.txt"
report.write_text("""BannerlordAI v0.2.10.53 FINAL VERIFY - ACCEPT

Decisive evidence
- v0.2.10.52 feature binaries reused unchanged; fixture suite remained 173/173 PASS.
- Natural request claimed by attempt-1.
- TRANSIENT_FAILURE attempt-1 matched active claim and retained queue work.
- Explicit release returned CLAIMED -> PENDING.
- attempt-2 reclaimed the same job PENDING -> CLAIMED.
- Stale SUCCESS result from attempt-1 was rejected with claimMatchReason=PROVIDER_CLAIM_MISMATCH.
- Stale completion was SUCCESS_NOT_ADMITTED_RETAINED with queue 1 -> 1.
- Exact attempt-2 SUCCESS reported CLAIM_MATCHED, providerResultAccepted=true, nested KEEP_BASELINE admitted.
- Exact completion SUCCESS_ACKED removed queue 1 -> 0.
- Replay attempt-2 failed DISPATCH_JOB_NOT_FOUND and completion remained 0 -> 0.
- Provider invocation stream stayed empty; no network/model/Apply authority.
- Every live check passed; full no-save integrity cleanup passed.

Classification: ACCEPT.

Architecture meaning
Retry ownership rotation is now end-to-end safe: after a new attempt claims a released job, stale results from the previous attempt cannot consume advisory state or complete the job. Only the current active claim can complete work.
""",encoding="utf-8")

plan=R/r"Longitudinal\\DerivedReports\\2026-09-21_v02154_ExternalProviderRequestEnvelope_Plan.txt"
plan.write_text("""BannerlordAI v0.2.10.54 - External provider request envelope / prompt provenance shadow

Question
Can the external worker construct an immutable, fingerprinted provider-request envelope from the exact claimed deliberation job so eventual real model calls have auditable provider/model/prompt/input provenance before any network integration exists?

Why this gate
- v0.2.10.46-.53 now secure dispatch, ownership, retries, result provenance and stale-result rejection.
- The worker still lacks a strict outbound provider-request contract. A future model call must not be able to silently alter the exact deliberation input, prompt contract or expected response schema.

Smallest implementation
- External-worker contract first; no real network/model call.
- Define BannerlordAI.PatrolDefenseProviderRequest.v1 with:
  schema
  providerId
  modelId
  attemptId
  requestFingerprint
  promptContractVersion
  responseSchema
  deliberationRequestBase64
  inputSha256
- inputSha256 = SHA-256 of exact UTF-8 deliberationRequest JSON bytes from the outbox job.
- providerId/modelId/promptContractVersion are explicit nonempty provenance strings.
- responseSchema must equal BannerlordAI.PatrolDefenseDeliberationAdvisory.v1.
- requestFingerprint must equal exact claimed job fingerprint.
- attemptId must equal the active claim attempt.
- No free-form system prompt text is stored in product runtime yet; promptContractVersion identifies the immutable contract family.
- Build/validate helper in external deterministic worker tooling first; optionally mirror a pure C# validator only if needed for runtime audit.
- Result envelope must echo providerId/attemptId/fingerprint already accepted by v0.2.51.
- No provider SDK/network/model call.

Acceptance
A. Pure deterministic provider-request builder/validator:
   exact job+claim builds valid envelope;
   SHA-256 independently recomputes;
   altered deliberation JSON fails hash;
   wrong fingerprint fails;
   wrong attempt fails;
   missing provider/model/prompt version fails;
   wrong responseSchema fails;
   malformed base64 fails;
   unknown/extra action-bearing fields rejected.
B. Integration on unchanged v0.2.52 binaries:
   natural outbox job -> exact claim attempt-1 -> external worker builds provider request envelope;
   validator independently decodes exact deliberationRequest and recomputes inputSha256;
   no network/model invocation.
C. Existing claim/result/admission state remains untouched; this gate does not submit a result.
D. EXIT_NOSAVE integrity cleanup.

Scope
This is outbound request provenance only. No real provider/model call, token limits, timeout/backoff, prompt semantics, or gameplay influence yet.
""",encoding="utf-8")

arch=R/r"Longitudinal\\EvidenceIndex\\HandoffV3\\ARCHITECTURE_LEARNING.md"
entry="""

## 2026-09-21 - v0.2.10.53 retry-attempt rotation / stale-result rejection accepted
- End-to-end retry rotation is now proven on unchanged v0.2.10.52 binaries.
- After attempt-2 reclaimed the job, stale attempt-1 SUCCESS was rejected with PROVIDER_CLAIM_MISMATCH and queue work remained retained.
- Exact attempt-2 SUCCESS then matched the active claim, admitted KEEP_BASELINE, and completed the job.
- Replay after completion failed DISPATCH_JOB_NOT_FOUND.
- This closes stale retry-result concurrency before real expensive cognition.
- Next safe boundary is outbound provider-request provenance: exact claimed input hash, provider/model identity, prompt contract version and expected response schema before any network/model call exists.
"""
txt=arch.read_text(encoding="utf-8-sig")
if "v0.2.10.53 retry-attempt rotation / stale-result rejection accepted" not in txt:
    arch.write_text(txt.rstrip()+entry+"\n",encoding="utf-8")

patch={
 "last_accepted_milestone":{
  "version":"v0.2.10.53-retry-attempt-rotation-integration-shadow",
  "name":"Retry-attempt rotation and stale-result rejection integration",
  "status":"ACCEPTED",
  "result":"Unchanged v0.2.10.52 binaries/173 fixtures plus live attempt-1 transient -> release -> attempt-2 claim -> stale attempt-1 SUCCESS rejected/queue retained -> exact attempt-2 SUCCESS accepted/SUCCESS_ACKED 1->0 -> replay job-missing; zero provider/model/gameplay authority and full cleanup.",
  "validation":val,
  "analyzer_report":str(report)
 },
 "active_candidate":{
  "version":"v0.2.10.54-external-provider-request-envelope-shadow",
  "name":"External provider request envelope and prompt provenance shadow",
  "status":"PLAN_READY",
  "plan":str(plan),
  "base_runner_staging":r"D:\\BannerlordAIResearch\\workspace\\_PatchStaging\\AutonomousOperator_v002152_ProviderClaimRelease_20260921_1510\\candidate"
 },
 "active_question":"Can an external worker produce a strict provider-request envelope with exact claimed input hash, provider/model/attempt provenance and response schema before any real network/model call is permitted?",
 "next_action":"Coder EXECUTE v0.2.10.54 external-worker request provenance gate: build strict provider-request envelope helper/validator over exact outbox job + active claim, prove independent inputSha256 and malformed/mismatch/extra-field rejection offline, then one unchanged-binary natural outbox+claim live integration where worker builds and independently verifies envelope with zero network/model invocation.",
 "blockers":[],
 "do_not_repeat":list(dict.fromkeys((cur.get("do_not_repeat") or [])+[
  "Do not rerun v0.2.10.53 unchanged; stale-attempt rejection and exact retry-attempt completion are accepted.",
  "Do not connect a real provider/model until outbound provider-request provenance and exact input hashing are proven."
 ]))
}
pp=R/r"workspace\\accept_v02153_open_v02154.json"
pp.write_text(json.dumps(patch,indent=2)+"\n",encoding="utf-8")
cp=subprocess.run([
 sys.executable,"-X","utf8",str(R/r"Tools\\Handoff\\handoff_checkpoint.py"),
 "checkpoint","--patch-file",str(pp),
 "--event-type","MILESTONE_ACCEPTED",
 "--message","v0.2.10.53 accepted: retry ownership rotation rejects stale attempt results and only current claim can complete work. Open v0.2.10.54 external provider request provenance/input-hash shadow.",
 "--action-id","accept-v02153-open-v02154",
 "--expected-seq",str(cur["checkpoint_seq"])
],capture_output=True,text=True)
print(cp.stdout)
if cp.returncode:
 print(cp.stderr)
 raise SystemExit(cp.returncode)
subprocess.run([sys.executable,"-X","utf8",str(R/r"Tools\\Autopilot\\sync_thinking_loop.py")],check=True)
