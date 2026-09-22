from pathlib import Path
import datetime
import json
import os
import subprocess
import sys

ROOT=Path(r"D:\BannerlordAIResearch")
OFFICE=ROOT/r"Automation\Office"
APPROVAL=OFFICE/"user_launch_approval.json"
UI_CLEANUP=OFFICE/"chatgpt_ui_cleanup_confirmed.json"
OVERRIDE=OFFICE/"office_override.json"
AUTOPILOT=ROOT/r"Automation\Autopilot\state.json"
CHECK_SCRIPT=ROOT/r"Tools\Autopilot\office_launch_check.py"
CHECK=OFFICE/"launch_check.json"
PLAN_SCRIPT=ROOT/r"Tools\Autopilot\front_office_daily_plan.py"
CLOCK_SCRIPT=ROOT/r"Tools\Autopilot\office_clock.py"

def now():
    return datetime.datetime.now().astimezone().isoformat()

def read_json(p):
    try:
        return json.loads(p.read_text(encoding="utf-8-sig")) if p.exists() else {}
    except Exception as ex:
        return {"_error":type(ex).__name__+": "+str(ex)}

def atomic_json(p,obj):
    p.parent.mkdir(parents=True,exist_ok=True)
    tmp=p.with_suffix(p.suffix+".tmp")
    tmp.write_text(json.dumps(obj,indent=2,ensure_ascii=False)+"\n",encoding="utf-8")
    os.replace(tmp,p)

def main():
    approval=read_json(APPROVAL)
    if not (
        approval.get("approved") is True
        and approval.get("scope")=="MATURE_OFFICE_V1"
    ):
        print(json.dumps({
          "launched":False,
          "reason":"USER_APPROVAL_REQUIRED",
          "approval_path":str(APPROVAL),
          "required":{"approved":True,"scope":"MATURE_OFFICE_V1"}
        },indent=2))
        return 3

    cleanup=read_json(UI_CLEANUP)
    if not (
        cleanup.get("confirmed") is True
        and cleanup.get("layout_version")=="ChatGPTProjectLayout.v1"
    ):
        print(json.dumps({
          "launched":False,
          "reason":"CHATGPT_UI_ORGANIZATION_REQUIRED",
          "cleanup_path":str(UI_CLEANUP),
          "required":{
            "confirmed":True,
            "layout_version":"ChatGPTProjectLayout.v1"
          }
        },indent=2))
        return 5

    check_cp=subprocess.run(
        [sys.executable,"-X","utf8",str(CHECK_SCRIPT)],
        cwd=str(ROOT),capture_output=True,text=True,errors="replace"
    )
    check=read_json(CHECK)
    if check.get("overall")!="READY_TO_LAUNCH":
        print(json.dumps({
          "launched":False,
          "reason":"LAUNCH_CHECK_NOT_READY",
          "check":check.get("overall"),
          "blocking_checks":check.get("blocking_checks")
        },indent=2))
        return 4

    override=read_json(OVERRIDE)
    override["enabled"]=False
    override["mode"]="SCHEDULED"
    override["released_at"]=now()
    override["released_by"]="explicit_user_approval"
    atomic_json(OVERRIDE,override)

    auto=read_json(AUTOPILOT)
    auto["enabled"]=True
    auto["status"]="READY_FOR_FRONT_PLANNING"
    auto["runner_pid"]=None
    auto["attempts"]=0
    auto["last_supervisor_result"]="OFFICE_LAUNCHED_WAITING_FOR_POLICY_DISPATCH"
    auto["office_launched_at"]=now()
    atomic_json(AUTOPILOT,auto)

    task_cp=subprocess.run(
      ["powershell","-NoProfile","-Command",
       "Get-ScheduledTask | Where-Object {$_.TaskName -like 'BannerlordAI*'} | Enable-ScheduledTask | Out-Null; Get-ScheduledTask | Where-Object {$_.TaskName -like 'BannerlordAI*'} | Select-Object TaskName,State | ConvertTo-Json"],
      capture_output=True,text=True,errors="replace"
    )

    subprocess.run(
        [sys.executable,"-X","utf8",str(CLOCK_SCRIPT)],
        cwd=str(ROOT),capture_output=True,text=True,errors="replace"
    )
    plan_cp=subprocess.run(
        [sys.executable,"-X","utf8",str(PLAN_SCRIPT)],
        cwd=str(ROOT),capture_output=True,text=True,errors="replace"
    )
    print(json.dumps({
      "launched":True,
      "launched_at":now(),
      "pc_tasks":task_cp.stdout,
      "daily_plan_return_code":plan_cp.returncode,
      "chatgpt_automations":"Front Office must explicitly enable/update Coder/Analyzer schedules after reading the Daily Plan.",
      "next":"Run Front Office, complete current Analyzer bottleneck, then register next Coder packet."
    },indent=2))
    return 0

if __name__=="__main__":
    raise SystemExit(main())
