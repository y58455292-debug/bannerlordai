from pathlib import Path
import json, subprocess, sys
R=Path(r"D:\\BannerlordAIResearch")
curp=R/r"Longitudinal\\EvidenceIndex\\HandoffV3\\current.json"
cur=json.loads(curp.read_text(encoding="utf-8-sig"))
val=r"D:\\BannerlordAIResearch\\Longitudinal\\LiveValidation\\PatrolDefense_v02154_ExternalProviderRequestEnvelope_20260921_153519_821221\\validation.json"

report=R/r"Longitudinal\\DerivedReports\\2026-09-21_v02154_ExternalProviderRequestEnvelope_FinalAnalyzerReview.txt"
report.write_text("""BannerlordAI v0.2.10.54 FINAL VERIFY - ACCEPT

Decisive evidence
- External provider-request contract fixture suite passed 49/49.
- Helper and live validator both py_compile PASS.
- v0.2.10.52 ClanAI/runner binaries were reused unchanged.
- Natural route-eligible request produced one exact outbox job and exact claim attempt-1.
- External worker built BannerlordAI.PatrolDefenseProviderRequest.v1 with:
  providerId=deterministic_external_worker
  modelId=deterministic_mock_no_model
  attemptId=attempt-1
  exact requestFingerprint
  promptContractVersion=BannerlordAI.PatrolDefenseAdvisoryPrompt.v1
  responseSchema=BannerlordAI.PatrolDefenseDeliberationAdvisory.v1.
- deliberationRequestBase64 decoded to the exact raw deliberation JSON bytes from the outbox row.
- inputSha256 independently recomputed to the identical value:
  2EB87367B2DBF996D04C0345812D0FE7B967FC5C734579F525BAA1AAB6B337C2.
- Strict contract fixtures reject altered bytes/hash, wrong fingerprint/provider/attempt, malformed base64/JSON, wrong prompt/response schema, missing fields and all extra/action-bearing fields.
- No provider result was submitted.
- Provider invocation/result streams remained empty.
- No network/model/Apply authority.
- Protected saves matched, disposable cleaned, binaries restored, lease released.
- Continuity watchdog refreshed clean with no warnings.

Classification: ACCEPT.

Architecture meaning
The outbound cognition request is now deterministic and auditable before transport. Exact job bytes, claim ownership, provider/model identity, prompt-contract version, response schema and SHA-256 input provenance are fixed before any real provider/model call exists.
""",encoding="utf-8")

plan=R/r"Longitudinal\\DerivedReports\\2026-09-21_v02155_ProviderRequestRegistration_Plan.txt"
plan.write_text("""BannerlordAI v0.2.10.55 - Runtime provider-request registration / outbound contract admission shadow

Question
Can the runtime admit and register the exact outbound provider-request envelope against the active claimed job, independently verify its request bytes/hash/provenance, and assign an immutable providerRequestId before any network/model call is allowed?

Why this gate
- v0.2.10.54 proves the external worker can build a correct outbound request envelope.
- The runtime does not yet know which exact provider request was authorized for a claim.
- Before result binding or real transport, the runtime should register one exact outbound request per active provider attempt.

Smallest implementation
- Runner-only; ClanAI unchanged.
- Add strict ProviderRequestRegistration parser for BannerlordAI.PatrolDefenseProviderRequest.v1.
- Command:
  PATROL_DEFENSE_PROVIDER_REQUEST_REGISTER <base64Utf8EnvelopeJson>
- Validate:
  exact schema and field set;
  providerId/modelId/attemptId/fingerprint/prompt version/response schema nonempty and exact;
  active dispatch job exists and is CLAIMED;
  providerId+attemptId+fingerprint exactly match active claim;
  deliberationRequestBase64 decodes UTF-8 JSON exactly equal to the immutable job DeliberationRequestJson bytes;
  inputSha256 independently recomputes;
  promptContractVersion=BannerlordAI.PatrolDefenseAdvisoryPrompt.v1;
  responseSchema=BannerlordAI.PatrolDefenseDeliberationAdvisory.v1.
- Compute providerRequestId = SHA-256 of the exact UTF-8 provider-request envelope JSON bytes as received by the runtime.
- Register one providerRequestId on the active claimed job:
  first valid envelope -> REGISTERED;
  exact identical replay -> SAME_PROVIDER_REQUEST_ALREADY_REGISTERED idempotent;
  different envelope/request id for same claim -> PROVIDER_REQUEST_ALREADY_REGISTERED reject.
- Add job fields:
  providerRequestId, modelId, promptContractVersion, inputSha256.
- Receipt:
  BannerlordAI.PatrolDefenseProviderRequestRegistration.v1
  providerRequestId, providerId, modelId, attemptId, fingerprint,
  registered, idempotent, reason,
  inputSha256, promptContractVersion, responseSchema,
  externalNetworkUsed=false,
  modelInvoked=false,
  executionAuthorized=false,
  zero gameplay authority.
- Dedicated jsonl registration stream.
- ReleaseClaim clears providerRequestId/model/prompt/input registration because a retry attempt must register its own outbound request.
- Completion removal discards registration with the job.

Acceptance
A. Runner compiles; ClanAI unchanged.
B. Fixtures:
   exact claimed envelope registers;
   exact replay idempotent;
   second different envelope same claim rejected;
   unclaimed job rejected;
   wrong provider/attempt/fingerprint rejected;
   altered request bytes rejected;
   bad inputSha rejected;
   wrong response schema/prompt contract rejected;
   malformed/extra fields rejected;
   release clears registration;
   attempt-2 can register a new request after release/reclaim;
   zero-authority receipt.
C. Static: no network/OpenAI/provider SDK/model call/Apply/score/memory write.
D. Live:
   natural request -> claim attempt-1 -> external worker builds exact v0.2.10.54 envelope -> runtime registration succeeds;
   exact registration replay is idempotent;
   provider/model/network invocation streams remain empty;
   no provider result submitted.
E. EXIT_NOSAVE integrity cleanup.

Scope
This registers the exact outbound request only. Results are not yet required to echo providerRequestId; that result-binding step follows after registration is accepted.
""",encoding="utf-8")

arch=R/r"Longitudinal\\EvidenceIndex\\HandoffV3\\ARCHITECTURE_LEARNING.md"
entry="""

## 2026-09-21 - v0.2.10.54 external provider request envelope provenance accepted
- The external worker now has a strict outbound provider-request contract containing provider/model/attempt/request provenance, immutable prompt-contract version, expected advisory response schema, exact base64 deliberation bytes, and independent SHA-256 input hash.
- Live proof used unchanged v0.2.10.52 binaries and no provider result/network/model call.
- Exact decoded bytes matched the raw outbox deliberation JSON, and independently recomputed SHA-256 matched the envelope.
- Contract rejects altered bytes/hash, wrong claim identity, malformed encodings, missing fields and all unknown/action-bearing fields.
- Remaining provenance gap: runtime has not yet registered which exact outbound provider request is authorized for a claimed job, so results cannot yet be bound to a specific provider request instance.
- Next safe gate is runtime provider-request registration with immutable providerRequestId.
"""
txt=arch.read_text(encoding="utf-8-sig")
if "v0.2.10.54 external provider request envelope provenance accepted" not in txt:
    arch.write_text(txt.rstrip()+entry+"\n",encoding="utf-8")

patch={
 "last_accepted_milestone":{
  "version":"v0.2.10.54-external-provider-request-envelope-shadow",
  "name":"External provider request envelope and prompt provenance shadow",
  "status":"ACCEPTED",
  "result":"49/49 strict contract fixtures plus unchanged-binary natural outbox+claim live proof: exact raw deliberation bytes/base64/inputSha256, provider/model/attempt/prompt/response-schema provenance, zero provider result/network/model/gameplay authority and full cleanup.",
  "validation":val,
  "analyzer_report":str(report)
 },
 "active_candidate":{
  "version":"v0.2.10.55-provider-request-registration-shadow",
  "name":"Runtime provider-request registration and outbound contract admission shadow",
  "status":"PLAN_READY",
  "plan":str(plan),
  "base_clanai_staging":r"D:\\BannerlordAIResearch\\workspace\\_PatchStaging\\PatrolDefenseRecentOutcomeAge_v02138_20260921_1234\\candidate",
  "base_runner_staging":r"D:\\BannerlordAIResearch\\workspace\\_PatchStaging\\AutonomousOperator_v002152_ProviderClaimRelease_20260921_1510\\candidate"
 },
 "active_question":"Can the runtime register one exact outbound provider-request envelope against the active claimed job, independently verify input bytes/hash/provenance, and assign an immutable providerRequestId before any network/model call?",
 "next_action":"Coder EXECUTE v0.2.10.55 runner-only provider-request registration gate: add strict envelope parser + exact claimed-job byte/hash/provenance verification + providerRequestId registration/receipt/command, clear registration on release, compile and pass register/idempotent/different-envelope/unclaimed/mismatch/hash/schema/release-retry/no-authority fixtures, then natural request -> claim -> exact external envelope -> registration + idempotent replay live proof with zero network/model/result submission.",
 "blockers":[],
 "do_not_repeat":list(dict.fromkeys((cur.get("do_not_repeat") or [])+[
  "Do not rerun v0.2.10.54 unchanged; exact outbound provider request byte/hash/provenance contract and no-network live proof are accepted.",
  "Do not submit or accept a real provider/model result before the runtime registers the exact outbound provider request instance."
 ]))
}
pp=R/r"workspace\\accept_v02154_open_v02155.json"
pp.write_text(json.dumps(patch,indent=2)+"\n",encoding="utf-8")
cp=subprocess.run([
 sys.executable,"-X","utf8",str(R/r"Tools\\Handoff\\handoff_checkpoint.py"),
 "checkpoint","--patch-file",str(pp),
 "--event-type","MILESTONE_ACCEPTED",
 "--message","v0.2.10.54 accepted: strict outbound provider-request provenance with exact raw input SHA-256 and no model/network call. Open v0.2.10.55 runtime provider-request registration/providerRequestId shadow.",
 "--action-id","accept-v02154-open-v02155",
 "--expected-seq",str(cur["checkpoint_seq"])
],capture_output=True,text=True)
print(cp.stdout)
if cp.returncode:
 print(cp.stderr)
 raise SystemExit(cp.returncode)
subprocess.run([sys.executable,"-X","utf8",str(R/r"Tools\\Autopilot\\sync_thinking_loop.py")],check=True)
