from pathlib import Path
import datetime
import json
import subprocess
import sys

ROOT=Path(r"D:\BannerlordAIResearch")
OFFICE=ROOT/r"Automation\Office"
CHECKS=OFFICE/"launch_check.json"

FILES={
 "departments":OFFICE/"departments.json",
 "office_hours":OFFICE/"office_hours.json",
 "office_policy":OFFICE/"office_policy.json",
 "chat_registry":OFFICE/"chat_registry.json",
 "chat_shift_policy":OFFICE/"chat_shift_policy.json",
 "daily_plan":OFFICE/"daily_plan.json",
 "chat_retirement_audit":OFFICE/"chat_retirement_audit.json",
 "chatgpt_project_layout":OFFICE/"chatgpt_project_layout.json",
 "chatgpt_cleanup_plan":OFFICE/"chatgpt_cleanup_plan.json",
 "office_override":OFFICE/"office_override.json",
 "coder_status":OFFICE/"coder_status.json",
 "analyzer_status":OFFICE/"analyzer_status.json",
 "front_status":OFFICE/"front_status.json",
 "autopilot_state":ROOT/r"Automation\Autopilot\state.json",
 "autopilot_registry":ROOT/r"Automation\Autopilot\registry.json",
 "front_page":ROOT/r"Longitudinal\EvidenceIndex\ContextPyramid\front_page.json",
 "archive_brain":ROOT/r"Longitudinal\EvidenceIndex\ChatArchiveBrain\00_FRONT\archive_status.json",
}

SCRIPTS=[
 ROOT/r"Tools\Autopilot\office_clock.py",
 ROOT/r"Tools\Autopilot\office_dispatch_policy.py",
 ROOT/r"Tools\Autopilot\continuation_supervisor.py",
 ROOT/r"Tools\Autopilot\run_registered_action.py",
 ROOT/r"Tools\Autopilot\analyzer_intake.py",
 ROOT/r"Tools\Autopilot\front_office.py",
 ROOT/r"Tools\Autopilot\front_office_daily_plan.py",
 ROOT/r"Tools\Autopilot\chat_lifecycle.py",
 ROOT/r"Tools\Evidence\context_pyramid.py",
 ROOT/r"Tools\Evidence\archive_brain.py",
]

def read_json(p):
    try:
        return json.loads(p.read_text(encoding="utf-8-sig")) if p.exists() else {}
    except Exception as ex:
        return {"_error":type(ex).__name__+": "+str(ex)}

def now():
    return datetime.datetime.now().astimezone().isoformat()

def main():
    results={}
    results["required_files"]={
      str(k):v.exists() for k,v in FILES.items()
    }
    py_compile=subprocess.run(
      [sys.executable,"-m","py_compile"]+[str(x) for x in SCRIPTS],
      capture_output=True,text=True,errors="replace"
    )
    results["scripts_compile"]={
      "pass":py_compile.returncode==0,
      "stderr":py_compile.stderr[-2000:]
    }

    override=read_json(FILES["office_override"])
    auto=read_json(FILES["autopilot_state"])
    plan=read_json(FILES["daily_plan"])
    front=read_json(FILES["front_status"])
    brain=read_json(FILES["archive_brain"])
    ui_cleanup=read_json(OFFICE/"chatgpt_ui_cleanup_confirmed.json")
    approval=read_json(OFFICE/"user_launch_approval.json")

    results["user_approval"]={
      "pass":(
        approval.get("approved") is True
        and approval.get("scope")=="MATURE_OFFICE_V1"
      ),
      "approved":approval.get("approved"),
      "scope":approval.get("scope"),
      "condition":approval.get("condition")
    }

    results["chatgpt_ui_organization"]={
      "pass":(
        ui_cleanup.get("confirmed") is True
        and ui_cleanup.get("layout_version")=="ChatGPTProjectLayout.v1"
      ),
      "confirmed":ui_cleanup.get("confirmed"),
      "layout_version":ui_cleanup.get("layout_version"),
      "confirmed_at":ui_cleanup.get("confirmed_at")
    }

    results["freeze_gate"]={
      "pass":(
        override.get("enabled") is True
        and override.get("mode")=="CLOSED"
        and auto.get("enabled") is False
      ),
      "office_mode":override.get("mode"),
      "autopilot_enabled":auto.get("enabled")
    }

    policy_test=subprocess.run(
      [sys.executable,"-X","utf8",
       str(ROOT/r"Tools\Autopilot\office_dispatch_policy.py"),
       "v02101_patrol_defense_shadow_rerun"],
      cwd=str(ROOT),capture_output=True,text=True,errors="replace"
    )
    results["closed_dispatch_refusal"]={
      "pass":(
        policy_test.returncode!=0
        and "OFFICE_CLOSED" in (policy_test.stdout+policy_test.stderr)
      ),
      "output":(policy_test.stdout+policy_test.stderr)[-1200:]
    }

    results["daily_plan"]={
      "pass":bool(plan) and plan.get("launch_blocked") is True,
      "office_mode":plan.get("office_mode"),
      "bottleneck":plan.get("current_bottleneck")
    }

    results["front_office"]={
      "pass":bool(front),
      "office_state":front.get("office_state"),
      "checkpoint_seq":front.get("checkpoint_seq")
    }

    results["archive_brain"]={
      "pass":(
        isinstance(brain.get("source_count"),int)
        and brain.get("source_count",0)>0
      ),
      "source_count":brain.get("source_count"),
      "known_chat_count":brain.get("known_chat_count"),
      "archive_safe_count":brain.get("archive_safe_count"),
      "delete_safe_count":brain.get("delete_safe_count")
    }

    pc_tasks=subprocess.run(
      ["powershell","-NoProfile","-Command",
       "Get-ScheduledTask | Where-Object {$_.TaskName -like 'BannerlordAI*'} | Select-Object TaskName,State | ConvertTo-Json"],
      capture_output=True,text=True,errors="replace"
    )
    results["pc_scheduled_tasks"]={
      "pass":"\"State\":  1" in pc_tasks.stdout or '"State":1' in pc_tasks.stdout.replace(" ",""),
      "output":pc_tasks.stdout[-1500:]
    }

    process_test=subprocess.run(
      ["powershell","-NoProfile","-Command",
       "$g=Get-Process Bannerlord -ErrorAction SilentlyContinue; [bool]$g"],
      capture_output=True,text=True,errors="replace"
    )
    results["no_bannerlord_running"]={
      "pass":process_test.stdout.strip().lower()=="false",
      "output":process_test.stdout.strip()
    }

    mem_sync=read_json(OFFICE/"mem_archive_sync.json")
    mem_collections=mem_sync.get("collections") or {}
    mem_gate={
      "pass":(
        mem_sync.get("status")=="VERIFIED_LAYERED_MIRROR"
        and (mem_collections.get("00_FRONT") or {}).get("count",0)>=1
        and (mem_collections.get("10_HOT") or {}).get("count",0)>=1
        and (mem_collections.get("20_WARM_ARCHITECTURE") or {}).get("count",0)>=1
        and (mem_collections.get("30_WARM_EVIDENCE") or {}).get("count",0)>=1
      ),
      "status":mem_sync.get("status"),
      "front_count":(mem_collections.get("00_FRONT") or {}).get("count"),
      "hot_count":(mem_collections.get("10_HOT") or {}).get("count"),
      "architecture_count":(mem_collections.get("20_WARM_ARCHITECTURE") or {}).get("count"),
      "evidence_count":(mem_collections.get("30_WARM_EVIDENCE") or {}).get("count"),
      "research_count":(mem_collections.get("40_RESEARCH") or {}).get("count"),
      "legacy_count":(mem_collections.get("90_LEGACY") or {}).get("count")
    }
    results["mem_archive_gate"]=mem_gate

    required=[
      all(results["required_files"].values()),
      results["scripts_compile"]["pass"],
      results["user_approval"]["pass"],
      results["chatgpt_ui_organization"]["pass"],
      results["freeze_gate"]["pass"],
      results["closed_dispatch_refusal"]["pass"],
      results["daily_plan"]["pass"],
      results["front_office"]["pass"],
      results["archive_brain"]["pass"],
      results["no_bannerlord_running"]["pass"],
      results["mem_archive_gate"]["pass"],
    ]
    if all(required):
        overall="READY_TO_LAUNCH"
    elif (
        results["user_approval"]["pass"]
        and not results["chatgpt_ui_organization"]["pass"]
    ):
        overall="WAITING_FOR_CHATGPT_UI_ORGANIZATION"
    else:
        overall="NOT_READY"
    out={
      "schema":"BannerlordAI.OfficeLaunchCheck.v1",
      "generated_at":now(),
      "overall":overall,
      "launch_permitted":overall=="READY_TO_LAUNCH",
      "user_approval_required":not results["user_approval"]["pass"],
      "checks":results,
      "blocking_checks":[
        k for k,v in results.items()
        if isinstance(v,dict) and v.get("pass") is False
      ]
    }
    CHECKS.write_text(json.dumps(out,indent=2,ensure_ascii=False)+"\n",encoding="utf-8")
    print(json.dumps(out,indent=2,ensure_ascii=False))
    return 0 if overall=="READY_TO_LAUNCH" else 2

if __name__=="__main__":
    raise SystemExit(main())
