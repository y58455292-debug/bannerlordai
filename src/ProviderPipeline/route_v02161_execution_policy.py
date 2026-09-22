from pathlib import Path
import json, os, sys, subprocess, hashlib, datetime
R=Path(r"D:\BannerlordAIResearch")
root=R/r"workspace\_PatchStaging\AutonomousOperator_v002161_ExecutionPolicy_20260921_2041"
runner=root/r"candidate\BannerlordAITestRunner.dll"
clan=R/r"workspace\_PatchStaging\PatrolDefenseRecentOutcomeAge_v02138_20260921_1234\candidate\ClanAI.dll"
validator=R/r"workspace\validate_v02161_execution_policy.py"
mainfx=root/r"fixtures\Fixtures.csproj"
v3fx=root/r"fixtures_v3\FixturesV3.csproj"
contractfx=R/r"workspace\test_provider_request_contract_v02154.py"

m=subprocess.run(["dotnet","run","--project",str(mainfx),"-c","Release"],capture_output=True,text=True)
mr=(m.stdout or "").strip()
if m.returncode!=0 or "PASS_FIXTURES checks=257" not in mr:
    raise SystemExit("main fixtures failed: "+mr+" "+(m.stderr or ""))

v=subprocess.run(["dotnet","run","--project",str(v3fx),"-c","Release"],capture_output=True,text=True)
vr=(v.stdout or "").strip()
if v.returncode!=0 or "PASS_V3_FIXTURES checks=63" not in vr:
    raise SystemExit("v3 fixtures failed: "+vr+" "+(v.stderr or ""))

c=subprocess.run([sys.executable,"-X","utf8",str(contractfx)],capture_output=True,text=True)
cr=(c.stdout or "").strip()
if c.returncode!=0 or "PASS_FIXTURES checks=49" not in cr:
    raise SystemExit("contract fixtures failed: "+cr+" "+(c.stderr or ""))

runner_sha=hashlib.sha256(runner.read_bytes()).hexdigest().upper()
clan_sha=hashlib.sha256(clan.read_bytes()).hexdigest().upper()
validator_sha=hashlib.sha256(validator.read_bytes()).hexdigest().upper()

regp=R/r"Automation\Autopilot\registry.json"
reg=json.loads(regp.read_text(encoding="utf-8-sig"))
reg.setdefault("actions",{})["v02161_execution_policy"]={
 "enabled":True,
 "description":"v0.2.10.61 explicit external transport execution-policy registration with caller-supplied bounds and env credential reference only.",
 "command":["python","-X","utf8",str(validator)],
 "required_candidate_version":"v0.2.10.61-external-transport-execution-policy-shadow",
 "required_next_action_contains":"v0.2.10.61",
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
  "compile_result":"PASS_RUNNER_0_ERRORS_CLANAI_REUSED_V02138",
  "execution_policy_fixture_result":mr,
  "v3_hash_binding_fixture_result":vr,
  "provider_request_contract_fixture_result":cr,
  "static_zero_authority_checks":"PASS",
  "network_model_policy":"NO_NETWORK_NO_MODEL_NO_ENV_VALUE_READ",
  "test_policy_values":"timeoutMs=1000,maxAttempts=1,maxResultBytes=65536,credentialRef=env:BANNERLORDAI_TEST_PROVIDER_KEY; TEST_ONLY_NOT_PRODUCT_DEFAULTS"
 },
 "next_action":"Coder EXECUTE v0.2.10.61 bounded live execution-policy registration gate. Natural request -> claim -> provider-request registration -> transport registration -> explicit test policy registration with caller-supplied timeout/attempt/result-size/env credential reference -> exact replay idempotent; provider/model/result/binding streams remain empty, no environment credential read, no Apply authority, full cleanup.",
 "blockers":[]
}
pp=R/r"workspace\v02161_execution_policy_execute_route.json"
pp.write_text(json.dumps(patch,indent=2)+"\n",encoding="utf-8")
cp=subprocess.run([
 sys.executable,"-X","utf8",str(R/r"Tools\Handoff\handoff_checkpoint.py"),
 "checkpoint","--patch-file",str(pp),
 "--event-type","PLAN_COMPLETE",
 "--message","v0.2.10.61 runner compiles 0 errors; execution-policy/backcompat 257/257, v3/hash-binding 63/63, request-contract 49/49 PASS; validator pycompile PASS; route bounded no-network policy registration + idempotent replay live proof.",
 "--action-id","v02161-execution-policy-live-route",
 "--expected-seq",str(cur["checkpoint_seq"])
],capture_output=True,text=True)
print(cp.stdout)
if cp.returncode:
    print(cp.stderr)
    raise SystemExit(cp.returncode)

subprocess.run([sys.executable,"-X","utf8",str(R/r"Tools\Autopilot\sync_thinking_loop.py")],check=True)

sp=R/r"Automation\Autopilot\state.json"
s=json.loads(sp.read_text(encoding="utf-8-sig"))
s.update({
 "enabled":True,
 "status":"READY",
 "attempts":0,
 "runner_pid":None,
 "pending_action_id":"v02161_execution_policy",
 "last_result":None,
 "return_code":None,
 "last_supervisor_result":None,
 "rearmed_reason":"v0.2.10.61 build + 257/257 + 63/63 + 49/49 fixtures + validator pass; bounded policy-registration live gate ready.",
 "rearmed_at":datetime.datetime.now().astimezone().isoformat()
})
tmp=sp.with_suffix(".json.tmp")
tmp.write_text(json.dumps(s,indent=2)+"\n",encoding="utf-8")
os.replace(tmp,sp)
