from pathlib import Path
import json, subprocess, sys

R=Path(r"D:\BannerlordAIResearch")
curp=R/r"Longitudinal\EvidenceIndex\HandoffV3\current.json"
cur=json.loads(curp.read_text(encoding="utf-8-sig"))
val=r"D:\BannerlordAIResearch\Longitudinal\LiveValidation\PatrolDefense_v02166_AuthorizationBoundResult_20260921_235935_610961\validation.json"
report=R/r"Longitudinal\DerivedReports\2026-09-21_v02166_Attempt1_HarnessRace_AnalyzerReview.txt"
report.write_text("""BannerlordAI v0.2.10.66 attempt 1 - HARNESS FALSE NEGATIVE

Observed
- Validator terminated with: two transport execution authorizations missing.
- Raw runtime authorization stream contains two successful receipts for the exact same job/policy:
  ordinal/count/max 1/1/2, then 2/2/2.
- Both receipts have timeoutMs=1000, authorized=true, EXECUTION_ATTEMPT_AUTHORIZED, distinct deterministic authorization IDs, and zero network/model/gameplay authority.
- The second receipt appeared after the validator's immediate file-length check.
- Candidate runner is unchanged at D91136C0378834206ABB30C2E1A5FF728F51673836BA632F49F7019A82241401.
- Failure cleanup succeeded: game closed, deployment owner released, accepted installed hashes restored, disposable v66 source save removed.

Classification
HARNESS_FALSE_NEGATIVE_AUTHORIZATION_RECEIPT_FLUSH_RACE.

Repair
Validator-only: after issuing the second authorize command, bounded-poll the authorization JSONL until two new rows are visible before classifying missing evidence. Do not change feature code.

Remaining proof
Stale auth1 result must reject/retain, exact auth2 must admit/complete, replay must be job-missing, followed by full cleanup.
""",encoding="utf-8")

ac=dict(cur.get("active_candidate") or {})
ac.update({
    "status":"RERUN_READY",
    "attempt1_validation":val,
    "attempt1_classification":"HARNESS_FALSE_NEGATIVE_AUTHORIZATION_RECEIPT_FLUSH_RACE",
    "attempt1_analyzer_report":str(report),
    "runner_sha256":"D91136C0378834206ABB30C2E1A5FF728F51673836BA632F49F7019A82241401",
    "validator_repair":"Validator-only bounded wait for two authorization JSONL rows after the second authorize command; feature binary unchanged.",
    "accepted_partial_evidence":list(dict.fromkeys((ac.get("accepted_partial_evidence") or [])+[
        "runtime_authorization_ordinal1_count1_max2",
        "runtime_authorization_ordinal2_count2_max2",
        "distinct_deterministic_authorization_ids",
        "timeoutMs_1000_bound_on_both_authorizations",
        "failure_cleanup_owner_release_and_baseline_restore"
    ]))
})
patch={
    "active_candidate":ac,
    "next_action":"Coder EXECUTE v0.2.10.66 validator-only rerun on unchanged runner SHA D91136C0378834206ABB30C2E1A5FF728F51673836BA632F49F7019A82241401. After two authorize commands, bounded-wait for both JSONL receipts, then prove stale auth1 -> EXECUTION_AUTHORIZATION_ID_MISMATCH with job retained, exact auth2 -> KEEP_BASELINE -> SUCCESS_ACKED 1->0, replay -> DISPATCH_JOB_NOT_FOUND, full cleanup.",
    "blockers":[],
    "do_not_repeat":list(dict.fromkeys((cur.get("do_not_repeat") or [])+[
        "Do not modify v0.2.10.66 feature logic because attempt 1 said two authorizations were missing; raw runtime evidence proves both ordinal 1 and ordinal 2 authorizations were emitted. Repair validator observation timing only."
    ]))
}
pp=R/r"workspace\classify_v02166_attempt1.json"
pp.write_text(json.dumps(patch,indent=2)+"\n",encoding="utf-8")
cp=subprocess.run([
    sys.executable,"-X","utf8",str(R/r"Tools\Handoff\handoff_checkpoint.py"),
    "checkpoint","--patch-file",str(pp),
    "--event-type","HARNESS_FALSE_NEGATIVE_CLASSIFIED",
    "--message","v0.2.10.66 attempt 1 classified harness false negative: both authorization rows exist; validator observed JSONL before second receipt became visible. Route validator-only bounded-wait rerun on unchanged feature binary.",
    "--action-id","v02166-attempt1-harness-race",
    "--expected-seq",str(cur["checkpoint_seq"])
],capture_output=True,text=True)
print(cp.stdout)
if cp.returncode:
    print(cp.stderr)
    raise SystemExit(cp.returncode)
subprocess.run([sys.executable,"-X","utf8",str(R/r"Tools\Autopilot\sync_thinking_loop.py")],check=True)
