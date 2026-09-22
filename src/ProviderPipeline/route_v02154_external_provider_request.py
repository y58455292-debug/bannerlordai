from pathlib import Path
import datetime
import hashlib
import json
import os
import subprocess
import sys

R=Path(r"D:\BannerlordAIResearch")
helper=R/r"workspace\provider_request_contract_v02154.py"
fixtures=R/r"workspace\test_provider_request_contract_v02154.py"
validator=R/r"workspace\validate_v02154_external_provider_request_envelope.py"
runner=R/r"workspace\_PatchStaging\AutonomousOperator_v002152_ProviderClaimRelease_20260921_1510\candidate\BannerlordAITestRunner.dll"
clan=R/r"workspace\_PatchStaging\PatrolDefenseRecentOutcomeAge_v02138_20260921_1234\candidate\ClanAI.dll"

fixture=subprocess.run(
    [sys.executable,"-X","utf8",str(fixtures)],
    capture_output=True,
    text=True,
)
fixture_result=(fixture.stdout or "").strip()
if fixture.returncode != 0 or "PASS_FIXTURES checks=49" not in fixture_result:
    raise SystemExit(
        "provider request fixtures failed: "
        + fixture_result
        + " "
        + (fixture.stderr or "")
    )

subprocess.run([sys.executable,"-m","py_compile",str(helper)],check=True)
subprocess.run([sys.executable,"-m","py_compile",str(validator)],check=True)

runner_sha=hashlib.sha256(runner.read_bytes()).hexdigest().upper()
clan_sha=hashlib.sha256(clan.read_bytes()).hexdigest().upper()
helper_sha=hashlib.sha256(helper.read_bytes()).hexdigest().upper()
fixture_sha=hashlib.sha256(fixtures.read_bytes()).hexdigest().upper()
validator_sha=hashlib.sha256(validator.read_bytes()).hexdigest().upper()

expected_runner="CECAFBCD366E82A50D16A803621909E3C930A8BB62A5BC432FF3B4377B0424B8"
if runner_sha != expected_runner:
    raise SystemExit("unchanged v02152 runner drift: "+runner_sha)

regp=R/r"Automation\Autopilot\registry.json"
reg=json.loads(regp.read_text(encoding="utf-8-sig"))
reg.setdefault("actions",{})["v02154_external_provider_request_envelope"]={
    "enabled":True,
    "description":"v0.2.10.54 strict outbound external provider request envelope/input-hash provenance on unchanged v0.2.10.52 binaries.",
    "command":["python","-X","utf8",str(validator)],
    "required_candidate_version":"v0.2.10.54-external-provider-request-envelope-shadow",
    "required_next_action_contains":"v0.2.10.54",
    "max_attempts":1,
    "next_action_id":None,
    "owner":"coder",
    "work_class":"LIVE_GAME",
    "may_launch_game":True,
    "requires_analyzer_clear":True,
}
tmp=regp.with_suffix(".json.tmp")
tmp.write_text(json.dumps(reg,indent=2)+"\n",encoding="utf-8")
os.replace(tmp,regp)

curp=R/r"Longitudinal\EvidenceIndex\HandoffV3\current.json"
cur=json.loads(curp.read_text(encoding="utf-8-sig"))
patch={
    "active_candidate":{
        "status":"EXECUTE_READY",
        "candidate_clanai":str(clan),
        "candidate_runner":str(runner),
        "clanai_test_sha256":clan_sha,
        "runner_test_sha256":runner_sha,
        "provider_request_contract":str(helper),
        "provider_request_contract_sha256":helper_sha,
        "provider_request_fixtures":str(fixtures),
        "provider_request_fixtures_sha256":fixture_sha,
        "validator":str(validator),
        "validator_sha256":validator_sha,
        "feature_binary_reuse":"UNCHANGED_V02152",
        "isolated_fixture_result":fixture_result,
        "network_model_policy":"NO_NETWORK_NO_MODEL_CALL",
    },
    "next_action":"Coder EXECUTE v0.2.10.54 unchanged-binary external provider-request provenance gate. Natural request -> exact claim attempt-1 -> external worker builds strict provider/model/attempt/prompt/response-schema envelope from raw outbox deliberation JSON bytes; independently verify exact base64 bytes and inputSha256; no provider result submission, provider invocation/result streams remain empty, zero network/model/gameplay authority, full cleanup.",
    "blockers":[],
}
pp=R/r"workspace\v02154_external_provider_request_execute_route.json"
pp.write_text(json.dumps(patch,indent=2)+"\n",encoding="utf-8")
cp=subprocess.run([
    sys.executable,"-X","utf8",str(R/r"Tools\Handoff\handoff_checkpoint.py"),
    "checkpoint","--patch-file",str(pp),
    "--event-type","PLAN_COMPLETE",
    "--message","v0.2.10.54 external provider-request contract fixtures 49/49 PASS; helper/validator pycompile PASS; unchanged v0.2.10.52 binaries; route natural outbox+claim -> exact raw-input envelope/hash provenance live proof with no network/model invocation.",
    "--action-id","v02154-provider-request-live-route",
    "--expected-seq",str(cur["checkpoint_seq"]),
],capture_output=True,text=True)
print(cp.stdout)
if cp.returncode:
    print(cp.stderr)
    raise SystemExit(cp.returncode)

subprocess.run(
    [sys.executable,"-X","utf8",str(R/r"Tools\Autopilot\sync_thinking_loop.py")],
    check=True,
)

sp=R/r"Automation\Autopilot\state.json"
s=json.loads(sp.read_text(encoding="utf-8-sig"))
s.update({
    "enabled":True,
    "status":"READY",
    "attempts":0,
    "runner_pid":None,
    "pending_action_id":"v02154_external_provider_request_envelope",
    "last_result":None,
    "return_code":None,
    "last_supervisor_result":None,
    "rearmed_reason":"v0.2.10.54 provider request contract 49/49 fixtures + syntax-clean live validator; unchanged binaries ready.",
    "rearmed_at":datetime.datetime.now().astimezone().isoformat(),
})
tmp=sp.with_suffix(".json.tmp")
tmp.write_text(json.dumps(s,indent=2)+"\n",encoding="utf-8")
os.replace(tmp,sp)
