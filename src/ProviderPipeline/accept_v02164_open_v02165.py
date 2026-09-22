from pathlib import Path
import json, subprocess, sys

R = Path(r"D:\BannerlordAIResearch")
curp = R / r"Longitudinal\EvidenceIndex\HandoffV3\current.json"
cur = json.loads(curp.read_text(encoding="utf-8-sig"))
val = r"D:\BannerlordAIResearch\Longitudinal\LiveValidation\PatrolDefense_v02164_MaxAttemptsPolicy_20260921_231036_527767\validation.json"

report = R / r"Longitudinal\DerivedReports\2026-09-21_v02164_MaxAttemptsPolicy_FinalAnalyzerReview.txt"
report.write_text("""BannerlordAI v0.2.10.64 FINAL VERIFY - ACCEPT

Decisive evidence
- Runner compiled with 0 errors; ClanAI unchanged.
- Offline gates: main 257/257 PASS; v3/hash 63/63 PASS; v4/maxAttempts 121/121 PASS; deterministic loopback-v64 50/50 PASS; provider-request contract 49/49 PASS.
- Runtime exact execution authorization receipt schema matched and carried the active requestFingerprint/executionPolicyId.
- Live authorization reason=EXECUTION_ATTEMPT_AUTHORIZED with attemptOrdinal=1, attemptCount=1, maxAttempts=1 and deterministic 64-hex authorization ID.
- Exactly one authorization row was emitted; no second execution authorization occurred after completion.
- Deterministic loopback required and consumed the exact authorization receipt before producing ProviderResult.v4.
- Existing transport receipt, exact result SHA, execution-policy match and maxResultBytes checks remained green.
- ProviderResult.v4 admitted KEEP_BASELINE and SUCCESS_ACKED removed queue 1 -> 0.
- Replay failed DISPATCH_JOB_NOT_FOUND.
- Every live check true; provider invocation stream remained empty.
- No credential value read, network/model call, Apply/score/memory authority.
- Protected saves matched, disposable saves cleaned, accepted binaries restored, lease released.

Classification: ACCEPT.

Architecture meaning
maxAttempts is now an enforced transport-execution budget under one registered executionPolicyId, separate from provider claim attemptId. Runtime authorizes each transport execution before send, increments only authorized executions, fails closed at the limit, clears authorization state on claim release, and supplies a bounded authorization receipt to the deterministic transport.
""", encoding="utf-8")

plan = R / r"Longitudinal\DerivedReports\2026-09-21_v02165_TimeoutBudgetBinding_Plan.txt"
plan.write_text("""BannerlordAI v0.2.10.65 - Registered timeoutMs transport-execution budget binding shadow

Question
Can runtime bind the exact registered timeoutMs into each transport-execution authorization and require deterministic transport to consume that exact timeout budget before result production, without introducing sleep, network, model, or secret access?

Why this gate
- v0.2.10.61 registers timeoutMs.
- v0.2.10.64 makes maxAttempts actionable at the pre-send authorization boundary.
- timeoutMs is still provenance-only at that boundary.
- The smallest safe next capability is exact timeout-budget propagation/binding, not real wall-clock cancellation before a real transport exists.

Smallest implementation
- Runner + deterministic loopback only; ClanAI unchanged.
- Extend PatrolDefenseProviderTransportExecutionAuthorization result/receipt with timeoutMs from the exact active execution policy.
- AuthorizeTransportExecution validates timeoutMs as a positive invariant integer before incrementing the execution count.
- Invalid/nonpositive timeout -> EXECUTION_TIMEOUT_POLICY_INVALID with no count increment.
- Existing maxAttempts ordering remains fail-closed and unchanged.
- authorizationId remains derived from requestFingerprint + executionPolicyId + ordinal; executionPolicyId already commits exact timeoutMs policy bytes.
- Deterministic loopback-v65 requires authorization receipt timeoutMs to equal the registered execution policy timeoutMs before any result is produced.
- Missing/wrong/invalid authorization timeout budget rejects deterministically.
- No sleeping, deadline polling, timeout thread, provider SDK, socket/HTTP, model call or credential value read.

Acceptance
A. Runner compiles; ClanAI unchanged.
B. Existing fixture layers remain green.
C. Runtime fixtures:
   timeoutMs=1000 -> authorization succeeds and receipt timeoutMs=1000;
   direct invalid/zero/non-numeric stored timeout -> EXECUTION_TIMEOUT_POLICY_INVALID with count unchanged;
   maxAttempts behavior remains max=1/max=2/limit-safe;
   release/new claim resets execution state;
   authorization ID determinism preserved;
   zero-authority receipt.
D. Loopback fixtures:
   exact timeout budget permits result;
   missing/wrong/zero timeout budget rejects;
   policy/authorization fingerprint and executionPolicyId mismatches still reject;
   no network/model.
E. Live:
   natural full chain with test timeoutMs=1000,maxAttempts=1;
   runtime authorization emits timeoutMs=1000 at ordinal 1;
   deterministic loopback consumes exact budget;
   existing v4 hash/size/policy chain -> KEEP_BASELINE -> SUCCESS_ACKED 1->0;
   exactly one authorization row; replay job-missing.
F. EXIT_NOSAVE cleanup.

Scope
This binds the registered timeout budget to the pre-send authorization/transport contract. It does not claim real elapsed-time cancellation; that belongs with a real or deterministic clocked transport gate later. credentialRef remains opaque and no real provider/network is enabled.
""", encoding="utf-8")

arch = R / r"Longitudinal\EvidenceIndex\HandoffV3\ARCHITECTURE_LEARNING.md"
entry = """
## 2026-09-21 - v0.2.10.64 maxAttempts transport-execution authorization accepted
- Registered maxAttempts now gates runtime transport-execution authorization under the exact executionPolicyId before deterministic result production.
- Live authorization was ordinal/count/max 1/1/1; exactly one authorization row existed, then KEEP_BASELINE -> SUCCESS_ACKED 1 -> 0 and replay job-missing.
- Claim attemptId remains worker ownership identity; transport execution ordinal/count is a separate policy-budget dimension.
- Authorization is zero-authority and deterministic; no provider/network/model invocation exists.
- timeoutMs is the remaining registered policy bound that is not actionable at the pre-send transport boundary.
- Next safe gate binds exact timeoutMs into the authorization/transport contract without claiming real wall-clock cancellation.
"""
txt = arch.read_text(encoding="utf-8-sig")
if "v0.2.10.64 maxAttempts transport-execution authorization accepted" not in txt:
    arch.write_text(txt.rstrip() + "\n\n" + entry.strip() + "\n", encoding="utf-8")

patch = {
 "last_accepted_milestone": {
  "version": "v0.2.10.64-execution-policy-max-attempts-enforcement-shadow",
  "name": "Registered maxAttempts transport-execution authorization shadow",
  "status": "ACCEPTED",
  "result": "257/257 main + 63/63 v3/hash + 121/121 v4/maxAttempts + 50/50 loopback-v64 + 49/49 request-contract fixtures; live exact authorization 1/1/1 -> deterministic authorized loopback -> RESULT_SIZE_WITHIN_LIMIT + EXECUTION_POLICY_MATCHED -> KEEP_BASELINE -> SUCCESS_ACKED 1->0 -> replay job-missing; exactly one authorization row, zero network/model/gameplay authority, full cleanup.",
  "validation": val,
  "analyzer_report": str(report),
  "runner_test_sha256": "12C265F313341C7BC37067110403CB7F650E16EC41A446FF646496C10A2D08F4",
  "clanai_test_sha256": "C437EE12AF5D223EEB1C3CF17D4191522F26EBAC350E4A0BA652A591A82399FD"
 },
 "active_candidate": {
  "version": "v0.2.10.65-execution-policy-timeout-budget-binding-shadow",
  "name": "Registered timeoutMs transport-execution budget binding shadow",
  "status": "PLAN_READY",
  "plan": str(plan),
  "base_clanai_staging": r"D:\BannerlordAIResearch\workspace\_PatchStaging\PatrolDefenseRecentOutcomeAge_v02138_20260921_1234\candidate",
  "base_runner_staging": r"D:\BannerlordAIResearch\workspace\_PatchStaging\AutonomousOperator_v002164_MaxAttemptsPolicy_20260921_2218\candidate"
 },
 "active_question": "Can runtime bind exact registered timeoutMs into each transport-execution authorization and require deterministic transport to consume that exact budget before result production?",
 "next_action": "Coder EXECUTE v0.2.10.65 timeout-budget binding gate: carry exact positive timeoutMs from active execution policy into transport-execution authorization receipt without increment on invalid policy, require exact timeoutMs match in deterministic loopback-v65, keep all maxAttempts/hash/size/policy/backcompat fixtures green, then natural full chain timeoutMs=1000,maxAttempts=1 -> authorization ordinal 1 timeoutMs=1000 -> loopback consumes exact budget -> existing v4 chain -> KEEP_BASELINE -> SUCCESS_ACKED 1->0 -> replay job-missing.",
 "blockers": [],
 "do_not_repeat": list(dict.fromkeys((cur.get("do_not_repeat") or []) + [
  "Do not rerun v0.2.10.64 unchanged; maxAttempts authorization/count/limit semantics, live exact authorization, completion and replay protection are accepted.",
  "Do not conflate timeout-budget binding with real elapsed-time cancellation; v0.2.10.65 proves exact pre-send budget propagation only.",
  "Do not read credentialRef secret values or connect a real provider/network without explicit authorization/configuration."
 ]))
}
pp = R / r"workspace\accept_v02164_open_v02165.json"
pp.write_text(json.dumps(patch, indent=2) + "\n", encoding="utf-8")
cp = subprocess.run([
 sys.executable, "-X", "utf8", str(R / r"Tools\Handoff\handoff_checkpoint.py"),
 "checkpoint", "--patch-file", str(pp), "--event-type", "MILESTONE_ACCEPTED",
 "--message", "v0.2.10.64 accepted: registered maxAttempts now bounds exact transport-execution authorization before deterministic result production. Open v0.2.10.65 exact timeoutMs budget binding at the same pre-send boundary.",
 "--action-id", "accept-v02164-open-v02165", "--expected-seq", str(cur["checkpoint_seq"])
], capture_output=True, text=True)
print(cp.stdout)
if cp.returncode:
 print(cp.stderr)
 raise SystemExit(cp.returncode)
subprocess.run([sys.executable, "-X", "utf8", str(R / r"Tools\Autopilot\sync_thinking_loop.py")], check=True)
