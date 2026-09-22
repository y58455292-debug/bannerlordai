from pathlib import Path
import json, subprocess, sys

R = Path(r"D:\BannerlordAIResearch")
curp = R / r"Longitudinal\EvidenceIndex\HandoffV3\current.json"
cur = json.loads(curp.read_text(encoding="utf-8-sig"))
val = r"D:\BannerlordAIResearch\Longitudinal\LiveValidation\PatrolDefense_v02165_TimeoutBudget_20260921_232607_198539\validation.json"

report = R / r"Longitudinal\DerivedReports\2026-09-21_v02165_TimeoutBudgetBinding_FinalAnalyzerReview.txt"
report.write_text("""BannerlordAI v0.2.10.65 FINAL VERIFY - ACCEPT

Decisive evidence
- Runner compiled with 0 errors; ClanAI unchanged.
- Offline gates: main 257/257 PASS; v3/hash 63/63 PASS; v4/timeout-aware 135/135 PASS; deterministic loopback-v65 57/57 PASS; provider-request contract 49/49 PASS.
- Runtime authorization parsed the exact registered timeoutMs before execution-count increment and emitted timeoutMs=1000.
- Zero/non-numeric direct timeout policies fail closed with EXECUTION_TIMEOUT_POLICY_INVALID and no count increment.
- Deterministic loopback requires exact positive authorization timeoutMs and rejects missing/zero/mismatched budgets.
- Live natural chain registered timeoutMs="1000" and authorization emitted integer timeoutMs=1000; timeout_budget.bound=true.
- Live authorization remained ordinal/count/max 1/1/1 with exactly one authorization row.
- Existing TransportReceipt.v3/ProviderResult.v4 exact SHA, execution-policy and maxResultBytes checks remained green.
- KEEP_BASELINE admitted; SUCCESS_ACKED removed queue 1 -> 0; replay failed DISPATCH_JOB_NOT_FOUND.
- Every live check true; provider invocation stream empty.
- No sleep-based timeout simulation, credential value read, network/model call, Apply/score/memory authority.
- Protected saves matched, disposable saves cleaned, accepted binaries restored, lease released.

Classification: ACCEPT.

Architecture meaning
The registered timeout budget is now actionable at the pre-send boundary: runtime binds it into each transport-execution authorization and deterministic transport refuses result production unless it receives the exact registered budget. This does not claim real elapsed-time cancellation.
""", encoding="utf-8")

plan = R / r"Longitudinal\DerivedReports\2026-09-21_v02166_TransportExecutionAuthorizationResultBinding_Plan.txt"
plan.write_text("""BannerlordAI v0.2.10.66 - Exact transport-execution-authorization-bound result provenance shadow

Question
Can the exact transportExecutionAuthorizationId and attempt ordinal be carried through transport receipt/result and independently re-matched by runtime before advisory admission, so stale transport executions under one executionPolicyId cannot consume the job?

Why this gate
- v0.2.10.64 created bounded transport-execution authorizations.
- v0.2.10.65 binds timeoutMs into that authorization.
- Current TransportReceipt.v3 and ProviderResult.v4 bind executionPolicyId but do not echo transportExecutionAuthorizationId.
- With maxAttempts > 1, an older authorized execution can therefore be indistinguishable from the latest authorized execution at result admission.
- Exact authorization provenance is required before real concurrent/retrying external transport is safe.

Smallest implementation
- Runner + deterministic loopback; ClanAI unchanged.
- Reuse active job LastTransportExecutionAuthorizationId and LastTransportExecutionAttemptOrdinal.
- Add exact runtime matcher for requestFingerprint + executionPolicyId + transportExecutionAuthorizationId + attemptOrdinal.
- Introduce new strict transport/result path without changing accepted v4 behavior:
  TransportReceipt.v4 includes transportExecutionAuthorizationId + transportExecutionAttemptOrdinal.
  ProviderResult.v5 includes the same fields.
  Runtime binding/admission independently matches the exact latest registered authorization before result hash/size/advisory admission.
- Deterministic loopback-v66 carries the exact consumed authorization ID/ordinal into both receipt and result.
- Fail closed on missing/wrong authorization ID, ordinal mismatch, policy/fingerprint mismatch, receipt/result disagreement.
- maxAttempts and timeoutMs authorization behavior remain unchanged.

Acceptance
A. Runner compiles; ClanAI unchanged; v1-v4 accepted paths stay fixture-green.
B. New fixtures with maxAttempts=2:
   auth1 ordinal1 then auth2 ordinal2;
   stale auth1-bound result rejected while job retained;
   exact auth2-bound result accepted;
   wrong/missing auth ID/ordinal rejected;
   receipt/result auth provenance disagreement rejected;
   exact hash/size/policy ordering remains intact;
   replay after completion job-missing.
C. Deterministic loopback-v66 emits exact authorization ID/ordinal in receipt/result and rejects tampered authorization provenance; no network/model.
D. Live bounded proof:
   use registered maxAttempts=2 and timeoutMs=1000;
   authorize ordinal1, then ordinal2;
   submit stale ordinal1 result -> reject authorization mismatch with job retained;
   submit exact ordinal2 result -> KEEP_BASELINE -> SUCCESS_ACKED 1->0;
   replay job-missing.
E. EXIT_NOSAVE cleanup and zero gameplay authority.

Scope
This closes within-policy stale transport-execution result concurrency. It does not add real provider/network execution or real elapsed-time cancellation. credentialRef remains opaque.
""", encoding="utf-8")

arch = R / r"Longitudinal\EvidenceIndex\HandoffV3\ARCHITECTURE_LEARNING.md"
entry = """
## 2026-09-21 - v0.2.10.65 exact timeout-budget binding accepted
- Runtime now binds registered timeoutMs into each transport-execution authorization before count increment/result production.
- Live policy timeoutMs="1000" matched authorization timeoutMs=1000; timeout_budget.bound=true, then existing v4 provenance -> KEEP_BASELINE -> SUCCESS_ACKED 1 -> 0.
- Deterministic transport rejects missing/zero/mismatched authorization timeout budgets without network/model work.
- This is pre-send budget binding only, not a claim of real elapsed-time cancellation.
- Remaining provenance gap: TransportReceipt.v3/ProviderResult.v4 do not carry transportExecutionAuthorizationId/ordinal, so multiple executions under one policy are not independently distinguishable at result admission.
- Next safe gate binds exact authorization ID/ordinal through transport receipt/result and rejects stale execution results.
"""
txt = arch.read_text(encoding="utf-8-sig")
if "v0.2.10.65 exact timeout-budget binding accepted" not in txt:
    arch.write_text(txt.rstrip() + "\n\n" + entry.strip() + "\n", encoding="utf-8")

patch = {
 "last_accepted_milestone": {
  "version": "v0.2.10.65-execution-policy-timeout-budget-binding-shadow",
  "name": "Registered timeoutMs transport-execution budget binding shadow",
  "status": "ACCEPTED",
  "result": "257/257 main + 63/63 v3/hash + 135/135 v4/timeout + 57/57 loopback-v65 + 49/49 request-contract fixtures; live registered timeoutMs=1000 -> authorization timeoutMs=1000 bound=true, ordinal/count/max 1/1/1 -> existing v4 SHA/size/policy chain -> KEEP_BASELINE -> SUCCESS_ACKED 1->0 -> replay job-missing; exactly one authorization row, zero network/model/gameplay authority, full cleanup.",
  "validation": val,
  "analyzer_report": str(report),
  "runner_test_sha256": "5E92F65137C6DBDF114BD77613CE5F384E7B240B496ACC6D7C7E39BDA07ADEB5",
  "clanai_test_sha256": "C437EE12AF5D223EEB1C3CF17D4191522F26EBAC350E4A0BA652A591A82399FD"
 },
 "active_candidate": {
  "version": "v0.2.10.66-transport-execution-authorization-bound-result-shadow",
  "name": "Exact transport-execution-authorization-bound result provenance shadow",
  "status": "PLAN_READY",
  "plan": str(plan),
  "base_clanai_staging": r"D:\BannerlordAIResearch\workspace\_PatchStaging\PatrolDefenseRecentOutcomeAge_v02138_20260921_1234\candidate",
  "base_runner_staging": r"D:\BannerlordAIResearch\workspace\_PatchStaging\AutonomousOperator_v002165_TimeoutBudget_20260921_2317\candidate"
 },
 "active_question": "Can exact transportExecutionAuthorizationId/ordinal be bound through transport receipt/result and re-matched by runtime so stale executions under one execution policy cannot consume the job?",
 "next_action": "Coder EXECUTE v0.2.10.66 authorization-bound result gate: add exact latest transportExecutionAuthorizationId/ordinal matcher, introduce strict TransportReceipt.v4 + ProviderResult.v5 path carrying authorization provenance, require exact receipt/result/runtime match before hash/size/advisory admission, pass maxAttempts=2 stale-auth1/exact-auth2/tamper/backcompat fixtures and deterministic loopback-v66, then bounded live auth1 -> auth2 -> stale auth1 reject retained -> exact auth2 KEEP_BASELINE -> SUCCESS_ACKED 1->0 -> replay job-missing.",
 "blockers": [],
 "do_not_repeat": list(dict.fromkeys((cur.get("do_not_repeat") or []) + [
  "Do not rerun v0.2.10.65 unchanged; exact timeoutMs pre-send authorization binding and live timeout budget proof are accepted.",
  "Do not treat executionPolicyId alone as sufficient provenance when maxAttempts permits multiple transport executions; bind the exact authorization ID/ordinal before real provider concurrency.",
  "Do not claim real elapsed-time cancellation from timeout-budget binding and do not connect a real provider/network without explicit authorization/configuration."
 ]))
}
pp = R / r"workspace\accept_v02165_open_v02166.json"
pp.write_text(json.dumps(patch, indent=2) + "\n", encoding="utf-8")
cp = subprocess.run([
 sys.executable, "-X", "utf8", str(R / r"Tools\Handoff\handoff_checkpoint.py"),
 "checkpoint", "--patch-file", str(pp), "--event-type", "MILESTONE_ACCEPTED",
 "--message", "v0.2.10.65 accepted: exact registered timeoutMs is bound into transport-execution authorization and deterministic transport. Open v0.2.10.66 exact transport-execution authorization ID/ordinal binding through result provenance.",
 "--action-id", "accept-v02165-open-v02166", "--expected-seq", str(cur["checkpoint_seq"])
], capture_output=True, text=True)
print(cp.stdout)
if cp.returncode:
 print(cp.stderr)
 raise SystemExit(cp.returncode)
subprocess.run([sys.executable, "-X", "utf8", str(R / r"Tools\Autopilot\sync_thinking_loop.py")], check=True)
