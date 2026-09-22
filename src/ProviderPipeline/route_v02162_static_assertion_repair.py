from pathlib import Path
import json, subprocess, sys, hashlib, datetime, os

R=Path(r"D:\BannerlordAIResearch")
curp=R/r"Longitudinal\EvidenceIndex\HandoffV3\current.json"
cur=json.loads(curp.read_text(encoding="utf-8-sig"))
validator=R/r"workspace\validate_v02162_policy_bound_transport_result_v4.py"
runner=R/r"workspace\_PatchStaging\AutonomousOperator_v002162_PolicyBoundResultV4_20260921_2051\candidate\BannerlordAITestRunner.dll"

report=R/r"Longitudinal\DerivedReports\2026-09-21_v02162_PrelaunchStaticHarness_AnalyzerReview.txt"
report.write_text("""BannerlordAI v0.2.10.62 PRELAUNCH VERIFY

Classification: HARNESS_FALSE_NEGATIVE_STATIC_ASSERTION

Observed failure
- Autopilot action v02162_policy_bound_transport_result_v4 returned before any validation directory, deployment, lease, or Bannerlord launch.
- The only failed static assertion was execution_policy_matched.
- That assertion incorrectly required the literal string EXECUTION_POLICY_MATCHED to appear inside PatrolDefenseProviderResultV4Admission.cs.

Feature evidence
- ProviderResult.v4 source calls dispatchQueue.MatchRegisteredExecutionPolicy(...).
- ProviderResult.v4 stores executionPolicyMatch.Reason in ExecutionPolicyMatchReason and serializes executionPolicyMatchReason.
- TransportReceipt.v3/ProviderResult.v4 binding source calls MatchRegisteredExecutionPolicy and explicitly normalizes the accepted case to EXECUTION_POLICY_MATCHED.
- Queue matcher returns EXECUTION_POLICY_MATCHED for exact active registered policy.
- Offline feature gates on the same binary are clean:
  257/257 main-policy fixtures
  63/63 v3/hash-binding fixtures
  41/41 v4 fixtures
  31/31 deterministic loopback-v4 fixtures
  49/49 provider-request contract fixtures
- Runner compiled with 0 errors.

Harness repair
- Static check now verifies the actual call chain:
  ProviderResult.v4 MatchRegisteredExecutionPolicy + serialized executionPolicyMatchReason
  Transport binding MatchRegisteredExecutionPolicy + accepted EXECUTION_POLICY_MATCHED literal
  Queue matcher accepted EXECUTION_POLICY_MATCHED literal.
- Feature binary unchanged.
- Validator py_compile PASS after repair.

Integrity
- Bannerlord never launched.
- No deployment owner/lease remained.
- No product evidence was consumed.

Disposition
Rerun the unchanged v0.2.10.62 feature binary with the repaired validator.
""",encoding="utf-8")

patch={
 "active_candidate":{
  "status":"EXECUTE_RETRY_STATIC_ASSERTION_REPAIRED",
  "latest_failure_classification":"HARNESS_FALSE_NEGATIVE_STATIC_ASSERTION",
  "analyzer_report":str(report),
  "validator":str(validator),
  "validator_sha256":hashlib.sha256(validator.read_bytes()).hexdigest().upper(),
  "runner_test_sha256":hashlib.sha256(runner.read_bytes()).hexdigest().upper(),
  "repair":"Validator now asserts the actual execution-policy call/serialization chain instead of requiring an accepted-reason literal inside ProviderResult.v4 source. Feature binary unchanged."
 },
 "next_action":"Coder EXECUTE bounded v0.2.10.62 live rerun after prelaunch static-assertion harness repair. Feature binary unchanged. Require natural full registration chain -> deterministic loopback v4 -> TransportReceipt.v3/ProviderResult.v4 exact executionPolicyId -> TRANSPORT_RECEIPT_MATCHED + EXECUTION_POLICY_MATCHED + exact result SHA -> KEEP_BASELINE -> SUCCESS_ACKED 1->0 -> replay DISPATCH_JOB_NOT_FOUND; zero network/model/Apply authority and full cleanup.",
 "blockers":[]
}
pp=R/r"workspace\v02162_static_assertion_repair.json"
pp.write_text(json.dumps(patch,indent=2)+"\n",encoding="utf-8")

cp=subprocess.run([
 sys.executable,"-X","utf8",str(R/r"Tools\Handoff\handoff_checkpoint.py"),
 "checkpoint","--patch-file",str(pp),
 "--event-type","VERIFY_BOUNDED_REPAIR",
 "--message","v0.2.10.62 first attempt classified HARNESS_FALSE_NEGATIVE_STATIC_ASSERTION: validator required accepted-reason literal in result source instead of verifying actual MatchRegisteredExecutionPolicy call chain. No launch/deployment; feature unchanged; repaired validator reroute.",
 "--action-id","v02162-static-assertion-repair",
 "--expected-seq",str(cur["checkpoint_seq"])
],capture_output=True,text=True)
print(cp.stdout)
if cp.returncode:
    print(cp.stderr)
    raise SystemExit(cp.returncode)

subprocess.run([
 sys.executable,"-X","utf8",str(R/r"Tools\Autopilot\sync_thinking_loop.py")
],check=True)

sp=R/r"Automation\Autopilot\state.json"
s=json.loads(sp.read_text(encoding="utf-8-sig"))
s.update({
 "enabled":True,
 "status":"READY",
 "attempts":0,
 "runner_pid":None,
 "pending_action_id":"v02162_policy_bound_transport_result_v4",
 "last_result":None,
 "return_code":None,
 "last_supervisor_result":None,
 "rearmed_reason":"v0.2.10.62 prelaunch static assertion repaired; feature unchanged; all offline gates already green.",
 "rearmed_at":datetime.datetime.now().astimezone().isoformat()
})
tmp=sp.with_suffix(".json.tmp")
tmp.write_text(json.dumps(s,indent=2)+"\n",encoding="utf-8")
os.replace(tmp,sp)
