from pathlib import Path
import hashlib,json,time

ROOT=Path(r"D:\BannerlordAIResearch")
runner=ROOT/"workspace/_PatchStaging/AutonomousOperator_v0028_PauseIntentGuard_20260919_100000/candidate/bin/Release/netstandard2.0"

files={
 "runner_dll":runner/"BannerlordAITestRunner.dll",
 "runner_pdb":runner/"BannerlordAITestRunner.pdb",
 "command_bus":ROOT/"Automation/Control/command_bus.py",
 "desired_intent":ROOT/"Automation/Control/desired_intent.json",
 "low_overhead_loop":ROOT/"Automation/Control/low_overhead_live_loop.py",
 "fast_decider":ROOT/"Automation/DecisionPolicy/fast_live_decider.py",
 "policy":ROOT/"Automation/DecisionPolicy/manan_live_policy.json",
 "reasoning_levels":ROOT/"Automation/DecisionPolicy/reasoning_levels.json",
 "rapid_launcher":ROOT/"Tools/Operator/rapid_manan_lab.py"
}
def sha(p):
 h=hashlib.sha256()
 with p.open("rb") as f:
  for c in iter(lambda:f.read(1024*1024),b""):h.update(c)
 return h.hexdigest().upper()

m={
 "schema":"BannerlordAI.NextRestartBundle.v1",
 "created_unix":time.time(),
 "bundle_version":"v0.2.8-pause-intent-guard",
 "expected_campaign_id":"ZJtX6IZXozIG",
 "target_save":"ClanAI MANAN BRANCH ROOT",
 "expected_party_members":213,
 "accepted_clanai_sha256":"0C9EC39ACBE7ABCA60D97C9C43B5138D39D5E0DE9B0EA1D10F44D7DA8258EC59",
 "runner_source":str(runner),
 "files":{k:{"path":str(p),"sha256":sha(p)} for k,p in files.items()},
 "requirements":[
  "state-disable trace retained",
  "pause/escape menu explicitly detected",
  "escape menu auto-closes only with movement intent",
  "sticky movement intent restored after decisions",
  "low-overhead watchdog only",
  "command provenance active",
  "exact campaign identity and hydration required",
  "no save writes"
 ]
}
out=ROOT/"Automation/Control/NEXT_RESTART_MANIFEST.json"
out.write_text(json.dumps(m,indent=2),encoding="utf-8")
print(json.dumps(m,indent=2))
