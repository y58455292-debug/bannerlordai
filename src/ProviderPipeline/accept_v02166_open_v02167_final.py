from pathlib import Path
import json, subprocess, sys

R=Path(r"D:\BannerlordAIResearch")
curp=R/r"Longitudinal\EvidenceIndex\HandoffV3\current.json"
cur=json.loads(curp.read_text(encoding="utf-8-sig"))
report=R/r"Longitudinal\DerivedReports\2026-09-22_v02166_AuthorizationBoundResult_FinalAnalyzerReview.txt"
plan=R/r"Longitudinal\DerivedReports\2026-09-22_v02167_ExternalProviderExecutionPackage_Plan.txt"
validation=r"D:\BannerlordAIResearch\Longitudinal\LiveValidation\PatrolDefense_v02166_AuthorizationBoundResult_20260922_000528_548237\validation.json"
dnr=list(dict.fromkeys((cur.get("do_not_repeat") or [])+[
 "Do not rerun v0.2.10.66 unchanged; raw live evidence proves stale auth1 rejection/retention, exact auth2 admission/completion, replay protection and full cleanup. Final FAIL was a post-completion summary NameError only.",
 "Do not modify accepted v0.2.10.66 runtime for v0.2.10.67 unless package-boundary evidence contradicts it; v67 is an external adapter-boundary proof.",
 "Do not choose/connect a real provider, read credentialRef secret values, or make network/model calls before explicit provider configuration/authorization."
]))
patch={
 "last_accepted_milestone":{
  "version":"v0.2.10.66-transport-execution-authorization-bound-result-shadow",
  "name":"Exact transport-execution-authorization-bound result provenance shadow",
  "status":"ACCEPTED",
  "result":"257/257 main + 63/63 v3 + 135/135 v4 + 40/40 v5 + 63/63 loopback-v66 + 49/49 request contract; live stale auth1 rejected/retained 1->1, exact auth2 matched and KEEP_BASELINE -> SUCCESS_ACKED 1->0, replay job-missing; every live check and cleanup PASS. Terminal FAIL was post-completion summary NameError only.",
  "validation":validation,
  "analyzer_report":str(report),
  "runner_test_sha256":"D91136C0378834206ABB30C2E1A5FF728F51673836BA632F49F7019A82241401",
  "clanai_test_sha256":"C437EE12AF5D223EEB1C3CF17D4191522F26EBAC350E4A0BA652A591A82399FD",
  "terminal_validator_classification":"HARNESS_FALSE_NEGATIVE_POST_COMPLETION_SUMMARY_NAMEERROR"
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
 "do_not_repeat":dnr
}
pp=R/r"workspace\accept_v02166_open_v02167_final.json"
pp.write_text(json.dumps(patch,indent=2)+"\n",encoding="utf-8")
cp=subprocess.run([
 sys.executable,"-X","utf8",str(R/r"Tools\Handoff\handoff_checkpoint.py"),
 "checkpoint","--patch-file",str(pp),"--event-type","MILESTONE_ACCEPTED",
 "--message","v0.2.10.66 accepted from complete raw semantic evidence. Stale auth1 cannot consume newer work; exact auth2 completes; replay and cleanup pass. Open v0.2.10.67 external provider execution-package/adapter boundary on unchanged accepted runtime.",
 "--action-id","accept-v02166-open-v02167-final","--expected-seq",str(cur["checkpoint_seq"])
],capture_output=True,text=True)
print(cp.stdout)
if cp.returncode:
 print(cp.stderr)
 raise SystemExit(cp.returncode)
subprocess.run([sys.executable,"-X","utf8",str(R/r"Tools\Autopilot\sync_thinking_loop.py")],check=True)
