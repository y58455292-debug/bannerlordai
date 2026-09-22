from pathlib import Path
import json, subprocess, sys

R=Path(r"D:\BannerlordAIResearch")
curp=R/r"Longitudinal\EvidenceIndex\HandoffV3\current.json"
cur=json.loads(curp.read_text(encoding="utf-8-sig"))

arch=R/r"Longitudinal\EvidenceIndex\HandoffV3\ARCHITECTURE_LEARNING.md"
entry="""

## 2026-09-22 - v0.2.10.67 external ProviderExecutionPackage / adapter boundary accepted
- Accepted v0.2.10.66 runtime binary was reused unchanged.
- ProviderExecutionPackage.v1 binds exact provider request bytes, transport bytes, execution-policy bounds/credentialRef, and latest transport-execution authorization into one auditable package ID.
- Stale auth1 package is rejected before adapter invocation when auth2 is latest.
- Exact auth2 package invokes deterministic adapter exactly once and produces accepted TransportReceipt.v4 + ProviderResult.v5 provenance.
- Live runtime admitted KEEP_BASELINE, completed SUCCESS_ACKED 1 -> 0, and replay failed job lookup.
- No secret value, provider network, model invocation or gameplay authority occurred.
- Generic provider-neutral plumbing is now closed. Provider-specific configuration, credential resolution and real network/model use require explicit user authorization.
"""
text=arch.read_text(encoding="utf-8-sig")
if "v0.2.10.67 external ProviderExecutionPackage / adapter boundary accepted" not in text:
    arch.write_text(text.rstrip()+entry+"\n",encoding="utf-8")

val=r"D:\BannerlordAIResearch\Longitudinal\LiveValidation\PatrolDefense_v02167_ExecutionPackage_20260922_002907_963426\validation.json"
report=R/r"Longitudinal\DerivedReports\2026-09-22_v02167_ExecutionPackage_FinalAnalyzerReview.txt"
plan=R/r"Longitudinal\DerivedReports\2026-09-22_v02168_ProviderSpecificConfiguration_Plan.txt"

patch={
 "last_accepted_milestone":{
  "version":"v0.2.10.67-external-provider-execution-package-shadow",
  "name":"External provider execution-package and adapter-boundary shadow",
  "status":"ACCEPTED",
  "result":"Accepted v0.2.10.66 runtime reused unchanged; offline gates 257/257 + 63/63 + 135/135 + 40/40 + 63/63 + 49/49 + ProviderExecutionPackage 44/44; live exact package valid, stale auth1 package blocked before adapter, adapter invoked exactly once for auth2, EXECUTION_AUTHORIZATION_MATCHED -> KEEP_BASELINE -> SUCCESS_ACKED 1->0 -> replay job-missing; zero network/model/gameplay authority and full cleanup.",
  "validation":val,
  "analyzer_report":str(report),
  "runner_test_sha256":"D91136C0378834206ABB30C2E1A5FF728F51673836BA632F49F7019A82241401",
  "clanai_test_sha256":"C437EE12AF5D223EEB1C3CF17D4191522F26EBAC350E4A0BA652A591A82399FD",
  "execution_package_id":"74EF66EF4E5633DF2A83EC0222EA731D92B6351EB9C79CBA67531DDDC6C24FCB"
 },
 "active_candidate":{
  "version":"v0.2.10.68-provider-specific-controlled-execution",
  "name":"Provider-specific controlled execution configuration",
  "status":"WAITING_ON_USER",
  "plan":str(plan),
  "base_runner_staging":r"D:\BannerlordAIResearch\workspace\_PatchStaging\AutonomousOperator_v002166_AuthBoundResult_20260921_2331\candidate",
  "feature_binary_reuse":"UNCHANGED_ACCEPTED_V02166_UNTIL_PROVIDER_ADAPTER_REQUIRES_OTHERWISE"
 },
 "active_question":"Which real provider/model should BannerlordAI use for the first bounded live-model advisory, and is outbound provider API use plus local credential resolution explicitly authorized?",
 "next_action":"WAITING_ON_USER: select/authorize the first real provider/model and bounded outbound API use. Credential must be supplied via an opaque local environment-variable reference; do not place the secret value in chat/project logs. After authorization, Front EXECUTE builds the provider-specific adapter and runs one advisory-only real-model proof under the accepted package/policy/authorization bounds.",
 "blockers":[
  "USER_PROVIDER_SELECTION_REQUIRED",
  "USER_EXTERNAL_PROVIDER_NETWORK_AUTHORIZATION_REQUIRED",
  "LOCAL_CREDENTIAL_REFERENCE_REQUIRED_BEFORE_REAL_PROVIDER_CALL"
 ],
 "do_not_repeat":list(dict.fromkeys((cur.get("do_not_repeat") or [])+[
  "Do not rerun v0.2.10.67 unchanged; exact execution-package validation, stale-package rejection-before-adapter, deterministic adapter invocation, runtime admission/completion/replay and cleanup are accepted.",
  "Do not infer provider selection or network/credential authorization from prior research. DeepSeek V4.1 Flash research is a recommendation for controlled analysis use, not permission for live provider execution.",
  "Do not log, persist, print, or request secret credential values in BannerlordAI evidence; use only an opaque local credentialRef/environment-variable name."
 ]))
}
pp=R/r"workspace\accept_v02167_open_v02168.json"
pp.write_text(json.dumps(patch,indent=2)+"\n",encoding="utf-8")
cp=subprocess.run([
 sys.executable,"-X","utf8",str(R/r"Tools\Handoff\handoff_checkpoint.py"),
 "checkpoint","--patch-file",str(pp),
 "--event-type","MILESTONE_ACCEPTED",
 "--message","v0.2.10.67 accepted: strict provider-neutral execution package and deterministic adapter boundary proven on unchanged runtime. Next boundary is provider-specific and requires explicit user provider selection/network authorization plus opaque local credential reference.",
 "--action-id","accept-v02167-open-v02168",
 "--expected-seq",str(cur["checkpoint_seq"])
],capture_output=True,text=True)
print(cp.stdout)
if cp.returncode:
 print(cp.stderr)
 raise SystemExit(cp.returncode)
subprocess.run([sys.executable,"-X","utf8",str(R/r"Tools\Autopilot\sync_thinking_loop.py")],check=True)
