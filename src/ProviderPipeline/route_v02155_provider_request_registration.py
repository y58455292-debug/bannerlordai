from pathlib import Path
import json, os, sys, subprocess, hashlib, datetime
R=Path(r"D:\BannerlordAIResearch")
root=R/r"workspace\_PatchStaging\AutonomousOperator_v002155_ProviderRequestRegistration_20260921_1541"
runner=root/r"candidate\BannerlordAITestRunner.dll"
validator=R/r"workspace\validate_v02155_provider_request_registration.py"
fixtures=root/r"fixtures\Fixtures.csproj"
contract_fixture=R/r"workspace\test_provider_request_contract_v02154.py"
clan=R/r"workspace\_PatchStaging\PatrolDefenseRecentOutcomeAge_v02138_20260921_1234\candidate\ClanAI.dll"

fixture=subprocess.run(
    ["dotnet","run","--project",str(fixtures),"-c","Release"],
    capture_output=True,text=True)
fixture_result=(fixture.stdout or "").strip()
if fixture.returncode!=0 or "PASS_FIXTURES checks=190" not in fixture_result:
    raise SystemExit("runner fixtures failed: "+fixture_result+" "+(fixture.stderr or ""))

contract=subprocess.run(
    [sys.executable,"-X","utf8",str(contract_fixture)],
    capture_output=True,text=True)
contract_result=(contract.stdout or "").strip()
if contract.returncode!=0 or "PASS_FIXTURES checks=49" not in contract_result:
    raise SystemExit("contract fixtures failed: "+contract_result+" "+(contract.stderr or ""))

subprocess.run([sys.executable,"-m","py_compile",str(validator)],check=True)

runner_sha=hashlib.sha256(runner.read_bytes()).hexdigest().upper()
validator_sha=hashlib.sha256(validator.read_bytes()).hexdigest().upper()
clan_sha=hashlib.sha256(clan.read_bytes()).hexdigest().upper()

regp=R/r"Automation\Autopilot\registry.json"
reg=json.loads(regp.read_text(encoding="utf-8-sig"))
reg.setdefault("actions",{})["v02155_provider_request_registration"]={
 "enabled":True,
 "description":"v0.2.10.55 runtime registration of exact outbound provider request envelope against active claimed job.",
 "command":["python","-X","utf8",str(validator)],
 "required_candidate_version":"v0.2.10.55-provider-request-registration-shadow",
 "required_next_action_contains":"v0.2.10.55",
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
  "isolated_fixture_result":fixture_result,
  "provider_request_contract_fixture_result":contract_result,
  "registration_policy":"ONE_EXACT_PROVIDER_REQUEST_PER_ACTIVE_CLAIM_IDEMPOTENT_REPLAY"
 },
 "next_action":"Coder EXECUTE v0.2.10.55 bounded live provider-request registration gate. Natural request -> exact claim attempt-1 -> exact v0.2.10.54 provider-request envelope -> runtime validates bytes/hash/provenance and registers providerRequestId; exact identical envelope replay must be idempotent with the same providerRequestId; provider invocation/result streams remain empty, zero network/model/gameplay authority, full cleanup.",
 "blockers":[]
}
pp=R/r"workspace\v02155_provider_request_registration_execute_route.json"
pp.write_text(json.dumps(patch,indent=2)+"\n",encoding="utf-8")
cp=subprocess.run([
 sys.executable,"-X","utf8",str(R/r"Tools\Handoff\handoff_checkpoint.py"),
 "checkpoint","--patch-file",str(pp),
 "--event-type","PLAN_COMPLETE",
 "--message","v0.2.10.55 runner compiles 0 errors; registration fixture suite 190/190 PASS plus provider-request contract 49/49 PASS; validator pycompile PASS; route natural claim -> exact registration -> idempotent replay live gate.",
 "--action-id","v02155-provider-request-registration-live-route",
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
 "pending_action_id":"v02155_provider_request_registration",
 "last_result":None,
 "return_code":None,
 "last_supervisor_result":None,
 "rearmed_reason":"v0.2.10.55 build+190 fixtures+49 contract fixtures+validator pass; bounded registration live gate ready.",
 "rearmed_at":datetime.datetime.now().astimezone().isoformat()
})
tmp=sp.with_suffix(".json.tmp")
tmp.write_text(json.dumps(s,indent=2)+"\n",encoding="utf-8")
os.replace(tmp,sp)
