from pathlib import Path
import json, os, sys, subprocess, hashlib, datetime
R=Path(r"D:\BannerlordAIResearch")
root=R/r"workspace\_PatchStaging\AutonomousOperator_v002160_TransportReceiptResultHash_20260921_1948"
runner=root/r"candidate\BannerlordAITestRunner.dll"
clan=R/r"workspace\_PatchStaging\PatrolDefenseRecentOutcomeAge_v02138_20260921_1234\candidate\ClanAI.dll"
validator=R/r"workspace\validate_v02160_transport_receipt_result_hash.py"
backcompat=root/r"fixtures\Fixtures.csproj"
v3fx=root/r"fixtures_v3\FixturesV3.csproj"

b=subprocess.run(["dotnet","run","--project",str(backcompat),"-c","Release"],capture_output=True,text=True)
br=(b.stdout or "").strip()
if b.returncode!=0 or "PASS_FIXTURES checks=225" not in br:
    raise SystemExit("backcompat failed: "+br+" "+(b.stderr or ""))

v=subprocess.run(["dotnet","run","--project",str(v3fx),"-c","Release"],capture_output=True,text=True)
vr=(v.stdout or "").strip()
if v.returncode!=0 or "PASS_V3_FIXTURES checks=63" not in vr:
    raise SystemExit("v3/v60 failed: "+vr+" "+(v.stderr or ""))

runner_sha=hashlib.sha256(runner.read_bytes()).hexdigest().upper()
clan_sha=hashlib.sha256(clan.read_bytes()).hexdigest().upper()
validator_sha=hashlib.sha256(validator.read_bytes()).hexdigest().upper()
if runner_sha!="629C5A24D09CF25A5EB9D0625546B580E0221595D67B158B2464FE16642177FE":
    raise SystemExit("runner drift "+runner_sha)

regp=R/r"Automation\Autopilot\registry.json"
reg=json.loads(regp.read_text(encoding="utf-8-sig"))
reg.setdefault("actions",{})["v02160_transport_receipt_result_hash"]={
 "enabled":True,
 "description":"v0.2.10.60 strict transport receipt to exact ProviderResult.v3 byte-hash binding before existing v3 admission.",
 "command":["python","-X","utf8",str(validator)],
 "required_candidate_version":"v0.2.10.60-transport-receipt-result-hash-binding-shadow",
 "required_next_action_contains":"v0.2.10.60",
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
  "backcompat_fixture_result":br,
  "v3_hash_binding_fixture_result":vr,
  "static_zero_authority_checks":"PASS",
  "binding_contract":"TRANSPORT_RECEIPT_V2_PLUS_EXACT_PROVIDER_RESULT_V3_BYTES_SHA256"
 },
 "next_action":"Coder EXECUTE v0.2.10.60 bounded live receipt/result-byte binding gate. Natural request -> claim -> provider-request registration -> transport registration -> deterministic loopback TransportReceipt.v2 + exact ProviderResult.v3 bytes submitted together -> runtime independently computes result SHA-256, requires TRANSPORT_RECEIPT_MATCHED, then existing v3 admission must KEEP_BASELINE and SUCCESS_ACKED 1->0; replay must fail DISPATCH_JOB_NOT_FOUND with no second nested v3 admission; full cleanup.",
 "blockers":[]
}
pp=R/r"workspace\v02160_receipt_hash_execute_route.json"
pp.write_text(json.dumps(patch,indent=2)+"\n",encoding="utf-8")
cp=subprocess.run([
 sys.executable,"-X","utf8",str(R/r"Tools\Handoff\handoff_checkpoint.py"),
 "checkpoint","--patch-file",str(pp),
 "--event-type","PLAN_COMPLETE",
 "--message","v0.2.10.60 runner compiles 0 errors; backcompat 225/225 and v3/receipt-hash suite 63/63 PASS; validator pycompile PASS; route bounded TransportReceipt.v2 + exact ProviderResult.v3 byte-hash binding live proof.",
 "--action-id","v02160-receipt-hash-live-route",
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
 "pending_action_id":"v02160_transport_receipt_result_hash",
 "last_result":None,
 "return_code":None,
 "last_supervisor_result":None,
 "rearmed_reason":"v0.2.10.60 build + 225/225 backcompat + 63/63 v3/hash-binding fixtures + validator pass; bounded live proof ready.",
 "rearmed_at":datetime.datetime.now().astimezone().isoformat()
})
tmp=sp.with_suffix(".json.tmp")
tmp.write_text(json.dumps(s,indent=2)+"\n",encoding="utf-8")
os.replace(tmp,sp)
