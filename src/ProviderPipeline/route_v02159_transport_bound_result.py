from pathlib import Path
import json, os, sys, hashlib, datetime, subprocess

R=Path(r"D:\\BannerlordAIResearch")
root=R/r"workspace\\_PatchStaging\\AutonomousOperator_v002159_TransportBoundResultV3_20260921_1658"
runner=root/r"candidate\\BannerlordAITestRunner.dll"
clan=R/r"workspace\\_PatchStaging\\PatrolDefenseRecentOutcomeAge_v02138_20260921_1234\\candidate\\ClanAI.dll"
validator=R/r"workspace\\validate_v02159_transport_bound_result_v3.py"
helper=R/r"workspace\\provider_transport_loopback_v02159.py"
v3source=root/r"candidate\\PatrolDefenseProviderResultV3Admission.cs"

runner_sha=hashlib.sha256(runner.read_bytes()).hexdigest().upper()
clan_sha=hashlib.sha256(clan.read_bytes()).hexdigest().upper()
validator_sha=hashlib.sha256(validator.read_bytes()).hexdigest().upper()
helper_sha=hashlib.sha256(helper.read_bytes()).hexdigest().upper()
v3_sha=hashlib.sha256(v3source.read_bytes()).hexdigest().upper()

regp=R/r"Automation\\Autopilot\\registry.json"
reg=json.loads(regp.read_text(encoding="utf-8-sig"))
reg.setdefault("actions",{})["v02159_transport_bound_result_v3"]={
 "enabled":True,
 "description":"v0.2.10.59 transport-bound ProviderResult.v3 admission over exact claim/providerRequestId/transportRequestId provenance.",
 "command":["python","-X","utf8",str(validator)],
 "required_candidate_version":"v0.2.10.59-transport-bound-provider-result-v3-shadow",
 "required_next_action_contains":"v0.2.10.59",
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
  "validator":str(validator),
  "validator_sha256":validator_sha,
  "transport_v3_helper":str(helper),
  "transport_v3_helper_sha256":helper_sha,
  "provider_result_v3_source_sha256":v3_sha,
  "compile_result":"PASS_RUNNER_0_ERRORS_CLANAI_REUSED_V02138",
  "backcompat_fixture_result":"PASS_FIXTURES checks=225",
  "v3_fixture_result":"PASS_V3_FIXTURES checks=40",
  "provider_request_contract_fixture_result":"PASS_FIXTURES checks=49",
  "transport_v3_fixture_result":"PASS_FIXTURES checks=73",
  "result_binding":"CLAIM_PLUS_PROVIDER_REQUEST_PLUS_TRANSPORT_REQUEST",
  "network_model_policy":"NO_NETWORK_NO_MODEL_CALL"
 },
 "next_action":"Coder EXECUTE v0.2.10.59 bounded live transport-bound ProviderResult.v3 gate. Natural request -> exact claim -> providerRequest registration -> transportRequest registration -> deterministic v3 loopback echoes providerRequestId+transportRequestId -> runtime must report CLAIM_MATCHED / PROVIDER_REQUEST_MATCHED / TRANSPORT_REQUEST_MATCHED -> KEEP_BASELINE admitted -> SUCCESS_ACKED queue 1->0 -> replay DISPATCH_JOB_NOT_FOUND; provider invocation stream empty, no network/model/Apply authority, full cleanup.",
 "blockers":[]
}
pp=R/r"workspace\\v02159_transport_bound_result_execute_route.json"
pp.write_text(json.dumps(patch,indent=2)+"\n",encoding="utf-8")
cp=subprocess.run([
 sys.executable,"-X","utf8",str(R/r"Tools\\Handoff\\handoff_checkpoint.py"),
 "checkpoint","--patch-file",str(pp),
 "--event-type","PLAN_COMPLETE",
 "--message","v0.2.10.59 runner compiles 0 errors; backcompat 225/225, focused v3 40/40, request contract 49/49, external v3 transport 73/73 PASS; validator pycompile PASS; route bounded claim+providerRequest+transportRequest bound ProviderResult.v3 live gate.",
 "--action-id","v02159-transport-bound-result-live-route",
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
 "pending_action_id":"v02159_transport_bound_result_v3",
 "last_result":None,
 "return_code":None,
 "last_supervisor_result":None,
 "rearmed_reason":"v0.2.10.59 225+40+49+73 fixture gates and validator pass; bounded transport-bound v3 live gate ready.",
 "rearmed_at":datetime.datetime.now().astimezone().isoformat()
})
tmp=sp.with_suffix(".json.tmp")
tmp.write_text(json.dumps(s,indent=2)+"\n",encoding="utf-8")
os.replace(tmp,sp)
