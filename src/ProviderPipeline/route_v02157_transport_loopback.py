from pathlib import Path
import json, os, sys, subprocess, hashlib, datetime
R=Path(r"D:\\BannerlordAIResearch")
helper=R/r"workspace\\provider_transport_loopback_v02157.py"
fixtures=R/r"workspace\\test_provider_transport_loopback_v02157.py"
validator=R/r"workspace\\validate_v02157_external_transport_loopback.py"
runner=R/r"workspace\\_PatchStaging\\AutonomousOperator_v002156_ProviderResultV2Binding_20260921_1549\\candidate\\BannerlordAITestRunner.dll"
clan=R/r"workspace\\_PatchStaging\\PatrolDefenseRecentOutcomeAge_v02138_20260921_1234\\candidate\\ClanAI.dll"

transport_fixture=subprocess.run(
    [sys.executable,"-X","utf8",str(fixtures)],
    capture_output=True,text=True)
transport_result=(transport_fixture.stdout or "").strip()
if transport_fixture.returncode!=0 or "PASS_FIXTURES checks=61" not in transport_result:
    raise SystemExit("transport fixture failed: "+transport_result+" "+(transport_fixture.stderr or ""))

runner_fixture=R/r"workspace\\_PatchStaging\\AutonomousOperator_v002156_ProviderResultV2Binding_20260921_1549\\fixtures\\Fixtures.csproj"
runner_test=subprocess.run(
    ["dotnet","run","--project",str(runner_fixture),"-c","Release"],
    capture_output=True,text=True)
runner_fixture_result=(runner_test.stdout or "").strip()
if runner_test.returncode!=0 or "PASS_FIXTURES checks=206" not in runner_fixture_result:
    raise SystemExit("runner fixture failed: "+runner_fixture_result+" "+(runner_test.stderr or ""))

contract_fixture=R/r"workspace\\test_provider_request_contract_v02154.py"
contract_test=subprocess.run(
    [sys.executable,"-X","utf8",str(contract_fixture)],
    capture_output=True,text=True)
contract_fixture_result=(contract_test.stdout or "").strip()
if contract_test.returncode!=0 or "PASS_FIXTURES checks=49" not in contract_fixture_result:
    raise SystemExit("contract fixture failed: "+contract_fixture_result+" "+(contract_test.stderr or ""))

subprocess.run([sys.executable,"-m","py_compile",str(helper)],check=True)
subprocess.run([sys.executable,"-m","py_compile",str(validator)],check=True)

runner_sha=hashlib.sha256(runner.read_bytes()).hexdigest().upper()
clan_sha=hashlib.sha256(clan.read_bytes()).hexdigest().upper()
helper_sha=hashlib.sha256(helper.read_bytes()).hexdigest().upper()
fixture_sha=hashlib.sha256(fixtures.read_bytes()).hexdigest().upper()
validator_sha=hashlib.sha256(validator.read_bytes()).hexdigest().upper()

if runner_sha!="7732C135648EE1E81587EF72E742DEB29260E0CEBD3005009BF66774F413A39C":
    raise SystemExit("v56 runner provenance drift: "+runner_sha)
if clan_sha!="C437EE12AF5D223EEB1C3CF17D4191522F26EBAC350E4A0BA652A591A82399FD":
    raise SystemExit("v56 ClanAI provenance drift: "+clan_sha)

regp=R/r"Automation\\Autopilot\\registry.json"
reg=json.loads(regp.read_text(encoding="utf-8-sig"))
reg.setdefault("actions",{})["v02157_external_transport_loopback"]={
 "enabled":True,
 "description":"v0.2.10.57 deterministic external transport adapter loopback over exact registered ProviderRequest.v1 -> ProviderResult.v2.",
 "command":["python","-X","utf8",str(validator)],
 "required_candidate_version":"v0.2.10.57-external-transport-loopback-shadow",
 "required_next_action_contains":"v0.2.10.57",
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

curp=R/r"Longitudinal\\EvidenceIndex\\HandoffV3\\current.json"
cur=json.loads(curp.read_text(encoding="utf-8-sig"))
patch={
 "active_candidate":{
  "status":"EXECUTE_READY",
  "candidate_clanai":str(clan),
  "candidate_runner":str(runner),
  "clanai_test_sha256":clan_sha,
  "runner_test_sha256":runner_sha,
  "provider_transport_helper":str(helper),
  "provider_transport_helper_sha256":helper_sha,
  "provider_transport_fixtures":str(fixtures),
  "provider_transport_fixtures_sha256":fixture_sha,
  "validator":str(validator),
  "validator_sha256":validator_sha,
  "feature_binary_reuse":"UNCHANGED_LIVE_TESTED_V02156",
  "transport_fixture_result":transport_result,
  "runner_fixture_result":runner_fixture_result,
  "provider_request_contract_fixture_result":contract_fixture_result,
  "transport_mode":"DETERMINISTIC_LOCAL_LOOPBACK_NO_NETWORK_NO_MODEL",
  "provenance_note":"Accepted v0.2.10.56 validation records runner_test_sha256=7732C135... and clanai_test_sha256=C437EE12...; stale milestone summary hashes are not used for execution."
 },
 "next_action":"Coder EXECUTE v0.2.10.57 bounded external transport-loopback integration on unchanged live-tested v0.2.10.56 binaries. Natural request -> claim -> exact provider request registration -> deterministic_local_loopback validates request/registration and emits byte-auditable ProviderResult.v2 + transport receipt -> existing v2 runtime admission must PROVIDER_REQUEST_MATCHED / KEEP_BASELINE / SUCCESS_ACKED 1->0 -> replay DISPATCH_JOB_NOT_FOUND; provider invocation stream empty, externalNetworkUsed=false, modelInvoked=false, no Apply authority, full cleanup.",
 "blockers":[]
}
pp=R/r"workspace\\v02157_transport_loopback_execute_route.json"
pp.write_text(json.dumps(patch,indent=2)+"\n",encoding="utf-8")
cp=subprocess.run([
 sys.executable,"-X","utf8",str(R/r"Tools\\Handoff\\handoff_checkpoint.py"),
 "checkpoint","--patch-file",str(pp),
 "--event-type","PLAN_COMPLETE",
 "--message","v0.2.10.57 transport helper 61/61 PASS; reused v0.2.10.56 runner fixtures 206/206 and request-contract fixtures 49/49; helper/validator pycompile PASS; exact live-tested v56 hashes confirmed; route deterministic loopback live integration.",
 "--action-id","v02157-transport-loopback-live-route",
 "--expected-seq",str(cur["checkpoint_seq"])
],capture_output=True,text=True)
print(cp.stdout)
if cp.returncode:
    print(cp.stderr)
    raise SystemExit(cp.returncode)

subprocess.run([sys.executable,"-X","utf8",str(R/r"Tools\\Autopilot\\sync_thinking_loop.py")],check=True)

sp=R/r"Automation\\Autopilot\\state.json"
s=json.loads(sp.read_text(encoding="utf-8-sig"))
s.update({
 "enabled":True,
 "status":"READY",
 "attempts":0,
 "runner_pid":None,
 "pending_action_id":"v02157_external_transport_loopback",
 "last_result":None,
 "return_code":None,
 "last_supervisor_result":None,
 "rearmed_reason":"v0.2.10.57 exact v56 binary provenance confirmed; 61+206+49 fixture gates PASS; deterministic loopback live integration ready.",
 "rearmed_at":datetime.datetime.now().astimezone().isoformat()
})
tmp=sp.with_suffix(".json.tmp")
tmp.write_text(json.dumps(s,indent=2)+"\n",encoding="utf-8")
os.replace(tmp,sp)
