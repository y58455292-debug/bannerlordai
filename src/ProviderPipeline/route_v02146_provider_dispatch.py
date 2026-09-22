from pathlib import Path
import json,os,sys,subprocess,hashlib,datetime
R=Path(r"D:\BannerlordAIResearch")
root=R/r"workspace\_PatchStaging\AutonomousOperator_v002146_ProviderDispatchOutbox_20260921_1425"
runner=root/r"candidate\BannerlordAITestRunner.dll"
validator=R/r"workspace\validate_v02146_provider_dispatch_outbox.py"
fixtures=root/r"fixtures\Fixtures.csproj"
clan=R/r"workspace\_PatchStaging\PatrolDefenseRecentOutcomeAge_v02138_20260921_1234\candidate\ClanAI.dll"

fixture=subprocess.run(
    ["dotnet","run","--project",str(fixtures),"-c","Release"],
    capture_output=True,text=True)
fixture_result=(fixture.stdout or "").strip()
if fixture.returncode!=0 or "PASS_FIXTURES checks=108" not in fixture_result:
    raise SystemExit("fixture rerun failed: "+fixture_result+" "+(fixture.stderr or ""))

runner_sha=hashlib.sha256(runner.read_bytes()).hexdigest().upper()
validator_sha=hashlib.sha256(validator.read_bytes()).hexdigest().upper()
clan_sha=hashlib.sha256(clan.read_bytes()).hexdigest().upper()

regp=R/r"Automation\Autopilot\registry.json"
reg=json.loads(regp.read_text(encoding="utf-8-sig"))
reg.setdefault("actions",{})["v02146_provider_dispatch_outbox"]={
 "enabled":True,
 "description":"v0.2.10.46 bounded non-blocking provider dispatch queue/outbox shadow.",
 "command":["python","-X","utf8",str(validator)],
 "required_candidate_version":"v0.2.10.46-provider-dispatch-outbox-shadow",
 "required_next_action_contains":"v0.2.10.46",
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
  "dispatch_mode":"SESSION_LOCAL_QUEUE_32_NO_CONSUMER"
 },
 "next_action":"Coder EXECUTE v0.2.10.46 bounded live dispatch/outbox gate. Require one natural route-eligible Manan request to emit providerDispatch ENQUEUED with same request fingerprint, queueCount=1, capacity=32, sequence=1; dedicated outbox must contain exactly one PENDING job with exact deliberationRequest JSON; provider invocation stream must remain empty, all network/model/execution flags false, no Apply side effects; EXIT_NOSAVE and full cleanup.",
 "blockers":[]
}
pp=R/r"workspace\v02146_provider_dispatch_execute_route.json"
pp.write_text(json.dumps(patch,indent=2)+"\n",encoding="utf-8")
cp=subprocess.run([
 sys.executable,"-X","utf8",str(R/r"Tools\Handoff\handoff_checkpoint.py"),
 "checkpoint","--patch-file",str(pp),
 "--event-type","PLAN_COMPLETE",
 "--message","v0.2.10.46 runner compiles 0 errors; enqueue/duplicate/full/reset/exact-payload/sequence suite 108/108 PASS; validator py_compile PASS; route bounded natural ENQUEUED outbox/no-provider live gate.",
 "--action-id","v02146-provider-dispatch-live-route",
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
 "pending_action_id":"v02146_provider_dispatch_outbox",
 "last_result":None,
 "return_code":None,
 "last_supervisor_result":None,
 "rearmed_reason":"v0.2.10.46 build+108 fixtures+validator pycompile pass; bounded dispatch/outbox live gate ready.",
 "rearmed_at":datetime.datetime.now().astimezone().isoformat()
})
tmp=sp.with_suffix(".json.tmp")
tmp.write_text(json.dumps(s,indent=2)+"\n",encoding="utf-8")
os.replace(tmp,sp)
