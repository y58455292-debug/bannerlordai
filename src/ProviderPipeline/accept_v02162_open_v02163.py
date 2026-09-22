from pathlib import Path
import json, subprocess, sys

R=Path(r"D:\BannerlordAIResearch")
curp=R/r"Longitudinal\EvidenceIndex\HandoffV3\current.json"
cur=json.loads(curp.read_text(encoding="utf-8-sig"))
val=r"D:\BannerlordAIResearch\Longitudinal\LiveValidation\PatrolDefense_v02162_PolicyBoundTransportResultV4_20260921_220121_334270\validation.json"

report=R/r"Longitudinal\DerivedReports\2026-09-21_v02162_PolicyBoundTransportResultV4_FinalAnalyzerReview.txt"
report.write_text("""BannerlordAI v0.2.10.62 FINAL VERIFY - ACCEPT

Decisive evidence
- Final runtime-wired runner compiled with 0 errors.
- Offline gates:
  main execution-policy fixtures 257/257 PASS
  v3/hash-binding fixtures 63/63 PASS
  v4 policy-bound result/binding fixtures 41/41 PASS
  deterministic loopback-v4 fixtures 31/31 PASS
  provider-request contract fixtures 49/49 PASS.
- First live route failed before deployment/game launch because the validator required an accepted-reason literal inside ProviderResult.v4 source; Analyzer classified HARNESS_FALSE_NEGATIVE_STATIC_ASSERTION and repaired only the validator.
- Repaired live rerun on unchanged feature binary PASS.
- Natural request completed the full provenance chain:
  exact claim
  providerRequest registration
  transportRequest registration
  executionPolicy registration
  deterministic loopback TransportReceipt.v3 + ProviderResult.v4.
- Runtime binding reported:
  transportReceiptMatchReason=TRANSPORT_RECEIPT_MATCHED
  executionPolicyMatchReason=EXECUTION_POLICY_MATCHED
  exact resultSha256 == computedResultSha256.
- ProviderResult.v4 admission reported executionPolicyMatchReason=EXECUTION_POLICY_MATCHED.
- providerResultAccepted=true; nested advisory admitted=true / KEEP_BASELINE.
- Dispatch completion SUCCESS_ACKED removed queue 1 -> 0.
- Replay failed DISPATCH_JOB_NOT_FOUND and completion remained 0 -> 0.
- Every live check true.
- Provider invocation stream remained empty.
- No environment secret value read, network/model call, Apply/score/memory authority.
- Protected saves matched, disposable saves cleaned, accepted binaries restored, lease released.

Classification: ACCEPT.

Architecture meaning
Registered execution policy identity is now part of the end-to-end cryptographic provenance chain. TransportReceipt.v3 and ProviderResult.v4 must carry the exact executionPolicyId, runtime re-matches that policy before result-byte hash binding and strict advisory admission, and replay cannot reuse completed work.
""",encoding="utf-8")

plan=R/r"Longitudinal\DerivedReports\2026-09-21_v02163_ResultSizePolicyEnforcement_Plan.txt"
plan.write_text("""BannerlordAI v0.2.10.63 - Execution-policy maxResultBytes enforcement shadow

Question
Can the registered maxResultBytes policy be enforced against the exact ProviderResult.v4 UTF-8 byte count before strict advisory admission, both in the deterministic external transport and independently in runtime?

Why this gate
- v0.2.10.61 registers explicit execution policy including maxResultBytes.
- v0.2.10.62 binds executionPolicyId through transport receipt/result provenance.
- maxResultBytes is currently provenance-only; an oversized result with the correct policy ID is not yet rejected.
- Bounded-worker prior art (DeepAstra, DeepSeek Harness, Portal Agent) supports enforcing explicit work/resource bounds before downstream review.
- This field is deterministic to enforce without network credentials, timing assumptions, or real model invocation.

Smallest implementation
- Runner + deterministic loopback helper; ClanAI unchanged.
- Add queue-side result-size policy evaluation using the active job's registered executionPolicyId and ExecutionPolicyMaxResultBytes.
- Parse registered maxResultBytes as a positive integer using invariant culture.
- Result-size decision:
  exact policy missing/mismatch -> existing policy match failure;
  invalid registered maxResultBytes -> RESULT_SIZE_POLICY_INVALID;
  exact result byte count <= max -> RESULT_SIZE_WITHIN_LIMIT;
  exact result byte count > max -> RESULT_SIZE_LIMIT_EXCEEDED.
- TransportReceipt.v3/ProviderResult.v4 binding ordering:
  receipt/result provenance + exact hash;
  registered executionPolicyId match;
  exact result byte-count policy check;
  only then ProviderResult.v4 advisory admission.
- Audit fields in binding receipt:
  resultByteCount,
  maxResultBytes,
  resultSizePolicyReason.
- Deterministic loopback-v4 also enforces policy.maxResultBytes before returning success:
  oversize -> success=false, result absent, errorCode=RESULT_SIZE_LIMIT_EXCEEDED;
  normal -> unchanged success.
- No token semantics or model-specific limits; byte bound is transport/runtime safety only.

Acceptance
A. Runner compiles; ClanAI unchanged.
B. Existing fixtures stay green.
C. New runtime fixtures:
   exact result under limit admitted;
   exact result equal to limit admitted;
   oversized exact-hash result rejected RESULT_SIZE_LIMIT_EXCEEDED and pending request/job retained;
   malformed/zero/negative maxResultBytes rejected policy invalid;
   wrong executionPolicyId still rejected before size admission;
   result hash mismatch still rejected;
   exact SUCCESS under limit completes 1->0;
   replay job-missing;
   zero-authority receipt.
D. Loopback fixtures:
   normal registered limit returns success;
   tiny registered maxResultBytes rejects before successful result receipt;
   receipt errorCode=RESULT_SIZE_LIMIT_EXCEEDED;
   no network/model.
E. Live:
   natural full registration chain using existing test policy maxResultBytes=65536;
   deterministic loopback result is within limit;
   runtime reports RESULT_SIZE_WITHIN_LIMIT plus exact result byte count/max;
   KEEP_BASELINE admitted; SUCCESS_ACKED 1->0; replay job-missing.
F. EXIT_NOSAVE cleanup.

Scope
This enforces exact result byte size only. maxAttempts and timeoutMs remain registered-but-unenforced and should be handled in later evidence-backed gates. credentialRef remains an opaque reference and no secret value is read.
""",encoding="utf-8")

arch=R/r"Longitudinal\EvidenceIndex\HandoffV3\ARCHITECTURE_LEARNING.md"
entry="""

## 2026-09-21 - v0.2.10.62 execution-policy-bound TransportReceipt.v3 / ProviderResult.v4 accepted
- Registered executionPolicyId is now carried through both deterministic transport receipt and ProviderResult.v4 and re-matched by runtime before advisory admission.
- Live binding proved TRANSPORT_RECEIPT_MATCHED + EXECUTION_POLICY_MATCHED + exact result SHA, followed by KEEP_BASELINE and SUCCESS_ACKED 1 -> 0.
- Replay failed job lookup.
- First live attempt exposed only a prelaunch validator static-assertion false negative; feature binary was unchanged and all offline gates were already green.
- Remaining policy gap: registered maxResultBytes/maxAttempts/timeoutMs are provenance-only, not yet enforced.
- Next smallest deterministic enforcement is maxResultBytes against exact result UTF-8 bytes, with dual external-loopback and runtime checks.
"""
txt=arch.read_text(encoding="utf-8-sig")
if "v0.2.10.62 execution-policy-bound TransportReceipt.v3 / ProviderResult.v4 accepted" not in txt:
    arch.write_text(txt.rstrip()+entry+"\n",encoding="utf-8")

patch={
 "last_accepted_milestone":{
  "version":"v0.2.10.62-execution-policy-bound-transport-result-v4-shadow",
  "name":"Execution-policy-bound TransportReceipt.v3 and ProviderResult.v4 shadow",
  "status":"ACCEPTED",
  "result":"257/257 main + 63/63 v3/hash + 41/41 v4 + 31/31 loopback-v4 + 49/49 request-contract fixtures plus live exact claim/provider/transport/execution-policy chain -> TRANSPORT_RECEIPT_MATCHED + EXECUTION_POLICY_MATCHED + exact SHA -> KEEP_BASELINE -> SUCCESS_ACKED 1->0 -> replay job-missing; zero network/model/gameplay authority and full cleanup.",
  "validation":val,
  "analyzer_report":str(report),
  "runner_test_sha256":"1A08ACA2F41F8F6C1675D510E829A85039CC8B17711FE2091E893429DEDFAB9E",
  "clanai_test_sha256":"C437EE12AF5D223EEB1C3CF17D4191522F26EBAC350E4A0BA652A591A82399FD"
 },
 "active_candidate":{
  "version":"v0.2.10.63-execution-policy-result-size-enforcement-shadow",
  "name":"Execution-policy maxResultBytes enforcement shadow",
  "status":"PLAN_READY",
  "plan":str(plan),
  "base_clanai_staging":r"D:\BannerlordAIResearch\workspace\_PatchStaging\PatrolDefenseRecentOutcomeAge_v02138_20260921_1234\candidate",
  "base_runner_staging":r"D:\BannerlordAIResearch\workspace\_PatchStaging\AutonomousOperator_v002162_PolicyBoundResultV4_20260921_2051\candidate"
 },
 "active_question":"Can registered maxResultBytes be enforced against exact ProviderResult.v4 UTF-8 bytes in both deterministic transport and runtime before strict advisory admission?",
 "next_action":"Coder EXECUTE v0.2.10.63 result-size policy gate: add queue-side registered maxResultBytes evaluation + v4 binding audit fields and pre-admission byte-limit check, enforce same limit in deterministic loopback-v4, compile and pass under/equal/over/invalid/hash/policy/backcompat fixtures, then natural full chain with test maxResultBytes=65536 -> RESULT_SIZE_WITHIN_LIMIT -> KEEP_BASELINE -> SUCCESS_ACKED 1->0 -> replay job-missing.",
 "blockers":[],
 "do_not_repeat":list(dict.fromkeys((cur.get("do_not_repeat") or [])+[
  "Do not rerun v0.2.10.62 unchanged; execution-policy-bound TransportReceipt.v3/ProviderResult.v4 provenance, exact result SHA, live success and replay protection are accepted.",
  "Do not connect a real provider/model or read credentialRef secret values before explicit authorization/configuration; continue deterministic policy enforcement only."
 ]))
}
pp=R/r"workspace\accept_v02162_open_v02163.json"
pp.write_text(json.dumps(patch,indent=2)+"\n",encoding="utf-8")
cp=subprocess.run([
 sys.executable,"-X","utf8",str(R/r"Tools\Handoff\handoff_checkpoint.py"),
 "checkpoint","--patch-file",str(pp),
 "--event-type","MILESTONE_ACCEPTED",
 "--message","v0.2.10.62 accepted: executionPolicyId bound through TransportReceipt.v3/ProviderResult.v4, exact result SHA, strict advisory admission and completion. Open v0.2.10.63 deterministic maxResultBytes enforcement before real provider authorization.",
 "--action-id","accept-v02162-open-v02163",
 "--expected-seq",str(cur["checkpoint_seq"])
],capture_output=True,text=True)
print(cp.stdout)
if cp.returncode:
 print(cp.stderr)
 raise SystemExit(cp.returncode)
subprocess.run([sys.executable,"-X","utf8",str(R/r"Tools\Autopilot\sync_thinking_loop.py")],check=True)
