from pathlib import Path
import json, os, sys, subprocess, hashlib, datetime

R=Path(r"D:\BannerlordAIResearch")
root=R/r"workspace\_PatchStaging\AutonomousOperator_v002163_ResultSizePolicy_20260921_2208"
runner=root/r"candidate\BannerlordAITestRunner.dll"
clan=R/r"workspace\_PatchStaging\PatrolDefenseRecentOutcomeAge_v02138_20260921_1234\candidate\ClanAI.dll"
validator=R/r"workspace\validate_v02163_result_size_policy.py"
loop_helper=R/r"workspace\provider_transport_loopback_v02163.py"
loop_fixture=R/r"workspace\test_provider_transport_loopback_v02163.py"

runner_sha=hashlib.sha256(runner.read_bytes()).hexdigest().upper()
clan_sha=hashlib.sha256(clan.read_bytes()).hexdigest().upper()
validator_sha=hashlib.sha256(validator.read_bytes()).hexdigest().upper()
loop_sha=hashlib.sha256(loop_helper.read_bytes()).hexdigest().upper()
loop_fixture_sha=hashlib.sha256(loop_fixture.read_bytes()).hexdigest().upper()

if runner_sha!="AC561AB57585EAE55ED5949F246DC1D60B22ECF98B3B1B1A0FF9210339807423":
    raise SystemExit("runner drift "+runner_sha)

regp=R/r"Automation\Autopilot\registry.json"
reg=json.loads(regp.read_text(encoding="utf-8-sig"))
reg.setdefault("actions",{})["v02163_result_size_policy"]={
    "enabled":True,
    "description":"v0.2.10.63 registered maxResultBytes enforcement on deterministic loopback and runtime ProviderResult.v4 admission.",
    "command":["python","-X","utf8",str(validator)],
    "required_candidate_version":"v0.2.10.63-execution-policy-result-size-enforcement-shadow",
    "required_next_action_contains":"v0.2.10.63",
    "max_attempts":1,
    "next_action_id":None,
    "owner":"coder",
    "work_class":"LIVE_GAME",
    "may_launch_game":True,
    "requires_analyzer_clear":True
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
        "validator":str(validator),
        "validator_sha256":validator_sha,
        "loopback_helper":str(loop_helper),
        "loopback_helper_sha256":loop_sha,
        "loopback_fixtures":str(loop_fixture),
        "loopback_fixtures_sha256":loop_fixture_sha,
        "compile_result":"PASS_RUNNER_0_ERRORS_CLANAI_REUSED_V02138",
        "main_fixture_result":"PASS_FIXTURES checks=257",
        "v3_fixture_result":"PASS_V3_FIXTURES checks=63",
        "v4_size_fixture_result":"PASS_V4_FIXTURES checks=72",
        "loopback_v63_fixture_result":"PASS_FIXTURES checks=40",
        "provider_request_contract_fixture_result":"PASS_FIXTURES checks=49",
        "result_size_policy":"REGISTERED_MAX_RESULT_BYTES_EXACT_UTF8_BYTE_ENFORCEMENT",
        "network_model_policy":"NO_NETWORK_NO_MODEL_NO_CREDENTIAL_VALUE_READ"
    },
    "next_action":"Coder EXECUTE v0.2.10.63 bounded live result-size policy gate. Natural full chain with test maxResultBytes=65536 -> deterministic loopback v4 must succeed within limit -> runtime binding independently reports RESULT_SIZE_WITHIN_LIMIT with exact resultByteCount and maxResultBytes=65536 before ProviderResult.v4 advisory admission -> KEEP_BASELINE -> SUCCESS_ACKED 1->0 -> replay DISPATCH_JOB_NOT_FOUND; provider stream empty, no env secret read/network/model/Apply authority, full cleanup.",
    "blockers":[]
}
pp=R/r"workspace\v02163_result_size_execute_route.json"
pp.write_text(json.dumps(patch,indent=2)+"\n",encoding="utf-8")

cp=subprocess.run([
    sys.executable,"-X","utf8",str(R/r"Tools\Handoff\handoff_checkpoint.py"),
    "checkpoint","--patch-file",str(pp),
    "--event-type","PLAN_COMPLETE",
    "--message","v0.2.10.63 final runner compiles 0 errors; fixtures 257/257 main + 63/63 v3/hash + 72/72 v4/size + 40/40 loopback-v63 + 49/49 request-contract; validator pycompile PASS; route bounded maxResultBytes live enforcement gate.",
    "--action-id","v02163-result-size-live-route",
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
    "pending_action_id":"v02163_result_size_policy",
    "last_result":None,
    "return_code":None,
    "last_supervisor_result":None,
    "rearmed_reason":"v0.2.10.63 result-size policy runner/validator fully preflighted; all fixture layers green.",
    "rearmed_at":datetime.datetime.now().astimezone().isoformat()
})
tmp=sp.with_suffix(".json.tmp")
tmp.write_text(json.dumps(s,indent=2)+"\n",encoding="utf-8")
os.replace(tmp,sp)
