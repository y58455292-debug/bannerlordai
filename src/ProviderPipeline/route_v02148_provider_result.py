from pathlib import Path
import json,os,sys,subprocess,hashlib,datetime
R=Path(r"D:\BannerlordAIResearch")
root=R/r"workspace\_PatchStaging\AutonomousOperator_v002148_ProviderResultEnvelope_20260921_1436"
runner=root/r"candidate\BannerlordAITestRunner.dll"
validator=R/r"workspace\validate_v02148_provider_result_envelope.py"
fixtures=root/r"fixtures\Fixtures.csproj"
clan=R/r"workspace\_PatchStaging\PatrolDefenseRecentOutcomeAge_v02138_20260921_1234\candidate\ClanAI.dll"

fixture=subprocess.run(
    ["dotnet","run","--project",str(fixtures),"-c","Release"],
    capture_output=True,text=True)
fixture_result=(fixture.stdout or "").strip()
if fixture.returncode!=0 or "PASS_FIXTURES checks=121" not in fixture_result:
    raise SystemExit("fixture rerun failed: "+fixture_result+" "+(fixture.stderr or ""))

runner_sha=hashlib.sha256(runner.read_bytes()).hexdigest().upper()
clan_sha=hashlib.sha256(clan.read_bytes()).hexdigest().upper()
validator_sha=hashlib.sha256(validator.read_bytes()).hexdigest().upper()

regp=R/r"Automation\Autopilot\registry.json"
reg=json.loads(regp.read_text(encoding="utf-8-sig"))
reg.setdefault("actions",{})["v02148_provider_result_envelope"]={
 "enabled":True,
 "description":"v0.2.10.48 strict external provider result envelope/provenance admission shadow.",
 "command":["python","-X","utf8",str(validator)],
 "required_candidate_version":"v0.2.10.48-provider-result-envelope-shadow",
 "required_next_action_contains":"v0.2.10.48",
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
  "static_zero_authority_checks":"PASS",
  "provider_result_contract":"STRICT_PROVIDER_ATTEMPT_FINGERPRINT_STATUS_ADVISORY"
 },
 "next_action":"Coder EXECUTE v0.2.10.48 bounded live provider-result-envelope gate. Natural outbox request -> synthetic external SUCCESS result envelope with providerId/attemptId/exact fingerprint/base64 KEEP_BASELINE advisory -> providerResultAccepted=true with nested strict advisory admission -> immediate identical replay rejects NO_PENDING_ROUTE_REQUEST; provider invocation stream empty, model/network/execution flags false, no Apply side effects, full cleanup.",
 "blockers":[]
}
pp=R/r"workspace\v02148_provider_result_execute_route.json"
pp.write_text(json.dumps(patch,indent=2)+"\n",encoding="utf-8")
cp=subprocess.run([
 sys.executable,"-X","utf8",str(R/r"Tools\Handoff\handoff_checkpoint.py"),
 "checkpoint","--patch-file",str(pp),
 "--event-type","PLAN_COMPLETE",
 "--message","v0.2.10.48 runner compiles 0 errors; provider-result provenance/failure fixture suite 121/121 PASS; validator pycompile PASS; route bounded SUCCESS envelope -> nested strict admission -> replay rejection live gate.",
 "--action-id","v02148-provider-result-live-route",
 "--expected-seq",str(cur["checkpoint_seq"])
],capture_output=True,text=True)
print(cp.stdout)
if cp.returncode:
 print(cp.stderr); raise SystemExit(cp.returncode)

subprocess.run([sys.executable,"-X","utf8",str(R/r"Tools\Autopilot\sync_thinking_loop.py")],check=True)

sp=R/r"Automation\Autopilot\state.json"
s=json.loads(sp.read_text(encoding="utf-8-sig"))
s.update({
 "enabled":True,
 "status":"READY",
 "attempts":0,
 "runner_pid":None,
 "pending_action_id":"v02148_provider_result_envelope",
 "last_result":None,
 "return_code":None,
 "last_supervisor_result":None,
 "rearmed_reason":"v0.2.10.48 build+121 fixtures+validator pass; bounded provider result live gate ready.",
 "rearmed_at":datetime.datetime.now().astimezone().isoformat()
})
tmp=sp.with_suffix(".json.tmp")
tmp.write_text(json.dumps(s,indent=2)+"\n",encoding="utf-8")
os.replace(tmp,sp)
