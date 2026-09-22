from pathlib import Path
import json, subprocess, sys

R=Path(r"D:\BannerlordAIResearch")
curp=R/r"Longitudinal\EvidenceIndex\HandoffV3\current.json"
cur=json.loads(curp.read_text(encoding="utf-8-sig"))
val=r"D:\BannerlordAIResearch\Longitudinal\LiveValidation\PatrolDefense_v02166_AuthorizationBoundResult_20260922_000528_548237\validation.json"

report=R/r"Longitudinal\DerivedReports\2026-09-22_v02166_AuthorizationBoundResult_FinalAnalyzerReview.txt"
report.write_text("""BannerlordAI v0.2.10.66 FINAL VERIFY - ACCEPT

Decisive evidence
- Candidate runner compiled with 0 errors; ClanAI unchanged.
- Offline/backcompat gates all passed:
  main 257/257
  v3/hash 63/63
  v4/timeout 135/135
  v5 stale-authorization 40/40
  deterministic loopback-v66 63/63
  provider-request contract 49/49.
- Attempt 1 was a validator observation race: raw runtime stream proved both exact authorization rows existed (ordinal/count/max 1/1/2 then 2/2/2). Feature binary was unchanged.
- Attempt 2 produced complete semantic live proof with every live check true.
- Stale authorization #1 carried ordinal 1 and was rejected with EXECUTION_AUTHORIZATION_ID_MISMATCH.
- Stale SUCCESS did not consume work: completion=SUCCESS_NOT_ADMITTED_RETAINED and queue remained 1 -> 1.
- Exact authorization #2 carried ordinal 2, matched EXECUTION_AUTHORIZATION_MATCHED, preserved exact receipt/result SHA and size/policy gates, admitted KEEP_BASELINE and completed SUCCESS_ACKED 1 -> 0.
- Terminal FAIL was a post-completion validator print NameError; raw static/live checks, cleanup and lease release all passed, so no live rerun was required.
- Real external execution still lacks a distinct explicit permission/configuration grant; existing transport authorization alone must not imply permission to read credentials or use the network.
- Next safe gate is an exact one-shot ExternalExecutionGrant bound to all existing provenance, with no secret read/network call yet.
"""
txt=arch.read_text(encoding="utf-8-sig")
if "v0.2.10.66 exact transport-execution authorization-bound results accepted" not in txt:
    arch.write_text(txt.rstrip()+"\n\n"+entry.strip()+"\n",encoding="utf-8")

patch={
 "last_accepted_milestone":{
  "version":"v0.2.10.66-transport-execution-authorization-bound-result-shadow",
  "name":"Exact transport-execution-authorization-bound result provenance shadow",
  "status":"ACCEPTED",
  "result":"257/257 main + 63/63 v3 + 135/135 v4 + 40/40 v5 + 63/63 loopback-v66 + 49/49 request contract; live auth1 stale -> EXECUTION_AUTHORIZATION_ID_MISMATCH retained 1->1, exact auth2 -> EXECUTION_AUTHORIZATION_MATCHED + KEEP_BASELINE -> SUCCESS_ACKED 1->0 -> replay job-missing; all live checks and cleanup PASS. Terminal validator FAIL was post-completion summary NameError only.",
  "validation":val,
  "analyzer_report":str(report),
  "runner_test_sha256":"D91136C0378834206ABB30C2E1A5FF728F51673836BA632F49F7019A82241401",
  "clanai_test_sha256":"C437EE12AF5D223EEB1C3CF17D4191522F26EBAC350E4A0BA652A591A82399FD",
  "terminal_validator_classification":"HARNESS_FALSE_NEGATIVE_POST_COMPLETION_SUMMARY_NAMEERROR"
 },
 "active_candidate":{
  "version":"v0.2.10.67-explicit-external-execution-grant-shadow",
  "name":"Explicit external-provider execution grant shadow",
  "status":"PLAN_READY",
  "plan":str(plan),
  "base_clanai_staging":r"D:\BannerlordAIResearch\workspace\_PatchStaging\PatrolDefenseRecentOutcomeAge_v02138_20260921_1234\candidate",
  "base_runner_staging":r"D:\BannerlordAIResearch\workspace\_PatchStaging\AutonomousOperator_v002166_AuthBoundResult_20260921_2331\candidate"
 },
 "active_question":"Can a real provider adapter be gated behind an explicit one-shot external execution grant bound to the exact latest request/transport/policy/authorization provenance, without yet reading credentials or using the network?",
 "next_action":"Coder EXECUTE v0.2.10.67 explicit external execution-grant gate: add strict grant envelope/state/receipt bound to exact claim + providerRequestId + transportRequestId + executionPolicyId + latest transportExecutionAuthorizationId/ordinal + provider/model/transport/credentialRef; require explicit externalNetworkAuthorized=true with no default, exact replay idempotent, stale/mismatch/false grant rejected, release clears, all v1-v5/backcompat fixtures green, then bounded live deterministic grant registration with zero credential read/network/model call and full cleanup.",
 "blockers":[],
 "do_not_repeat":list(dict.fromkeys((cur.get("do_not_repeat") or [])+[
  "Do not rerun v0.2.10.66 unchanged; raw live evidence proves stale auth1 rejection/retention, exact auth2 acceptance/completion, replay protection and full cleanup. Terminal FAIL was only a post-completion print-summary NameError.",
  "Do not treat transport execution authorization as permission to read a credential or use the network; require a separate explicit external execution grant.",
  "Do not read credentialRef secret values or connect a real provider/network until the explicit grant/configuration boundary is accepted and required user/provider configuration is available."
 ]))
}
pp=R/r"workspace\accept_v02166_open_v02167.json"
pp.write_text(json.dumps(patch,indent=2)+"\n",encoding="utf-8")
cp=subprocess.run([
 sys.executable,"-X","utf8",str(R/r"Tools\Handoff\handoff_checkpoint.py"),
 "checkpoint","--patch-file",str(pp),"--event-type","MILESTONE_ACCEPTED",
 "--message","v0.2.10.66 accepted from raw PASS evidence despite post-completion validator summary NameError. Stale transport execution results are now fail-closed. Open v0.2.10.67 explicit external execution-grant boundary before any credential read/network provider execution.",
 "--action-id","accept-v02166-open-v02167","--expected-seq",str(cur["checkpoint_seq"])
],capture_output=True,text=True)
print(cp.stdout)
if cp.returncode:
 print(cp.stderr)
 raise SystemExit(cp.returncode)
subprocess.run([sys.executable,"-X","utf8",str(R/r"Tools\Autopilot\sync_thinking_loop.py")],check=True)
- Exact replay after completion failed DISPATCH_JOB_NOT_FOUND.
- Provider invocation stream remained empty; no network/model/gameplay authority.
- Protected saves matched, disposable save cleaned, accepted ClanAI/runner restored, lease released.
- The validator wrote completed_local after the semantic PASS and cleanup, then a post-completion summary NameError ('authorization' variable) overwrote result to FAIL. This is a harness-only closeout false negative; raw acceptance evidence is complete and no rerun is required.

Classification: ACCEPT.

Architecture meaning
Transport execution authorization is now part of end-to-end result provenance. Multiple executions under one executionPolicyId are distinguishable: once a newer authorization exists, an older authorized execution cannot consume the job. This closes the major stale-result concurrency gap before a real external provider is connected.
""",encoding="utf-8")

plan=R/r"Longitudinal\DerivedReports\2026-09-22_v02167_ExternalProviderExecutionPackage_Plan.txt"
plan.write_text("""BannerlordAI v0.2.10.67 - External provider execution-package / adapter-boundary shadow

Question
Can the external worker receive one strict, byte-auditable execution package containing the exact approved request, transport, execution policy and latest transport-execution authorization, validate it before any provider call, and hand it to a pluggable provider adapter without changing the accepted runtime contract?

Why this gate
- v0.2.10.54-66 now prove the complete request -> claim -> provider request -> transport -> policy -> execution authorization -> authorization-bound result chain.
- Runtime stale-result, retry, hash, size and timeout provenance is closed.
- The remaining practical gap before real model execution is the external adapter input/configuration boundary: a real provider driver must receive exactly the already-authorized work and bounds, not reconstruct them from loose state.
- This can be proven without reading a credential value or making a network/model call.

Smallest implementation
- Reuse the accepted v0.2.10.66 game/runner binary unchanged.
- Add external Python ProviderExecutionPackage.v1 builder/validator.
- Package contains exact:
  providerRequest JSON bytes + providerRequestId/inputSha256,
  transport request JSON bytes + transportRequestId,
  executionPolicyId + timeoutMs/maxAttempts/maxResultBytes/credentialRef,
  transportExecutionAuthorizationId + attemptOrdinal/attemptCount,
  providerId/modelId/promptContractVersion/responseSchema.
- Compute executionPackageId as SHA-256 over canonical length-delimited exact component bytes/IDs.
- Strict validation independently re-hashes provider/transport request bytes and checks all IDs/identity fields/policy bounds/latest authorization receipt.
- credentialRef is treated as opaque metadata only; no environment value read.
- Define a provider-adapter interface whose deterministic adapter consumes the validated package and returns the already accepted TransportReceipt.v4 + ProviderResult.v5 shape.
- Adapter is never invoked if package validation fails.
- No HttpClient/socket/provider SDK/model call.

Acceptance
A. Accepted v0.2.10.66 runner reused unchanged.
B. Package fixtures:
   exact package validates and deterministic executionPackageId is stable;
   changed request bytes/hash/provider/model/transport/policy/auth ID/ordinal/bounds reject;
   stale auth1 package rejects when auth2 is designated latest;
   unknown fields reject;
   credentialRef value is never resolved/read.
C. Adapter fixtures:
   deterministic adapter receives only a validated package;
   emits exact TransportReceipt.v4 + ProviderResult.v5 carrying auth2 ID/ordinal;
   tampered/unvalidated package cannot invoke adapter;
   no network/model.
D. Bounded live integration on unchanged runner:
   natural route -> claim -> exact registrations -> maxAttempts=2 -> auth1 then auth2;
   external worker builds/validates exact auth2 execution package;
   deterministic adapter emits v4/v5;
   runtime admits exact auth2 -> KEEP_BASELINE -> SUCCESS_ACKED 1->0 -> replay job-missing.
E. Full EXIT_NOSAVE cleanup.

Scope
This is the final generic adapter/input boundary before provider-specific execution. It does not choose a provider, read credentials, or make a network/model call. Provider-specific configuration/authorization remains the next user-dependent boundary.
""",encoding="utf-8")

arch=R/r"Longitudinal\EvidenceIndex\HandoffV3\ARCHITECTURE_LEARNING.md"
entry="""
## 2026-09-22 - v0.2.10.66 authorization-bound result provenance accepted
- TransportReceipt.v4 and ProviderResult.v5 now carry exact transportExecutionAuthorizationId + ordinal.
- Runtime independently re-matches the latest authorization before result-size/advisory admission.
- Live maxAttempts=2 proof rejected stale auth1 with job retained, then accepted exact auth2 -> KEEP_BASELINE -> SUCCESS_ACKED 1 -> 0; replay was job-missing.
- Two validator-only issues were classified without changing feature code: authorization receipt visibility race, then post-completion summary NameError after all semantic/cleanup checks had passed.
- This closes within-policy stale transport-execution result concurrency.
- Next safe bridge toward a real model is a strict external provider execution-package / adapter input boundary on unchanged accepted runtime binaries.
"""
txt=arch.read_text(encoding="utf-8-sig")
if "v0.2.10.66 authorization-bound result provenance accepted" not in txt:
    arch.write_text(txt.rstrip()+"\n\n"+entry.strip()+"\n",encoding="utf-8")

ac_prev=cur.get("active_candidate") or {}
patch={
 "last_accepted_milestone":{
  "version":"v0.2.10.66-transport-execution-authorization-bound-result-shadow",
  "name":"Exact transport-execution-authorization-bound result provenance shadow",
  "status":"ACCEPTED",
  "result":"257/257 main + 63/63 v3/hash + 135/135 v4/timeout + 40/40 v5 + 63/63 loopback-v66 + 49/49 request-contract; live auth1 stale -> EXECUTION_AUTHORIZATION_ID_MISMATCH / SUCCESS_NOT_ADMITTED_RETAINED 1->1, exact auth2 -> EXECUTION_AUTHORIZATION_MATCHED -> KEEP_BASELINE -> SUCCESS_ACKED 1->0, replay job-missing; every live check true and full cleanup. Final validator FAIL was post-completion NameError only.",
  "validation":val,
  "analyzer_report":str(report),
  "runner_test_sha256":"D91136C0378834206ABB30C2E1A5FF728F51673836BA632F49F7019A82241401",
  "clanai_test_sha256":"C437EE12AF5D223EEB1C3CF17D4191522F26EBAC350E4A0BA652A591A82399FD",
  "terminal_validator_classification":"HARNESS_FALSE_NEGATIVE_POST_COMPLETION_NAMEERROR"
 },
 "active_candidate":{
  "version":"v0.2.10.67-external-provider-execution-package-shadow",
  "name":"External provider execution-package and adapter-boundary shadow",
  "status":"PLAN_READY",
  "plan":str(plan),
  "base_clanai_staging":r"D:\BannerlordAIResearch\workspace\_PatchStaging\PatrolDefenseRecentOutcomeAge_v02138_20260921_1234\candidate",
  "base_runner_staging":r"D:\BannerlordAIResearch\workspace\_PatchStaging\AutonomousOperator_v002166_AuthBoundResult_20260921_2331\candidate",
  "feature_binary_reuse":"UNCHANGED_ACCEPTED_V02166"
 },
 "active_question":"Can the external worker consume one strict execution package containing the exact approved request/transport/policy/latest authorization and pass only validated work into a provider adapter boundary?",
 "next_action":"Coder EXECUTE v0.2.10.67 on unchanged accepted v0.2.10.66 runner: build strict ProviderExecutionPackage.v1 + independent validator + deterministic adapter boundary, prove exact package/hash/identity/bounds/latest-auth validation and fail-closed tamper/stale behavior, then bounded live natural chain exports exact auth2 package to external deterministic adapter -> TransportReceipt.v4 + ProviderResult.v5 -> KEEP_BASELINE -> SUCCESS_ACKED 1->0 -> replay job-missing. No credential value read/network/model call.",
 "blockers":[],
 "do_not_repeat":list(dict.fromkeys((cur.get("do_not_repeat") or [])+[
  "Do not rerun v0.2.10.66 unchanged; raw live evidence proves stale auth1 rejection/retention, exact auth2 admission/completion, replay protection and full cleanup. The final FAIL was post-completion summary NameError only.",
  "Do not modify the accepted v0.2.10.66 runner for v0.2.10.67 unless the external package gate exposes contradictory runtime evidence; v67 is an external adapter-boundary proof.",
  "Do not choose/connect a real provider, read credentialRef secret values, or make network/model calls before explicit provider configuration/authorization."
 ]))
}
pp=R/r"workspace\accept_v02166_open_v02167.json"
pp.write_text(json.dumps(patch,indent=2)+"\n",encoding="utf-8")
cp=subprocess.run([
 sys.executable,"-X","utf8",str(R/r"Tools\Handoff\handoff_checkpoint.py"),
 "checkpoint","--patch-file",str(pp),
 "--event-type","MILESTONE_ACCEPTED",
 "--message","v0.2.10.66 accepted from complete raw semantic evidence: stale auth1 cannot consume newer work; exact auth2 completes; replay blocked; cleanup clean. Open v0.2.10.67 external provider execution-package/adapter boundary on unchanged accepted runtime.",
 "--action-id","accept-v02166-open-v02167",
 "--expected-seq",str(cur["checkpoint_seq"])
],capture_output=True,text=True)
print(cp.stdout)
if cp.returncode:
 print(cp.stderr)
 raise SystemExit(cp.returncode)
subprocess.run([sys.executable,"-X","utf8",str(R/r"Tools\Autopilot\sync_thinking_loop.py")],check=True)
