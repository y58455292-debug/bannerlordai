import sys
sys.path.insert(0, r"D:\BannerlordAIResearch\Tools\Operator")
import launch_supervisor
sys.argv = [
    "launch_supervisor.py",
    "--save", "ClanAI OPERATOR SMOKE TEST.sav",
    "--expected-save-sha", "11E3F8C04A1DF3ACA4C0DA4D51F9BB912C4D2970114C66B403729F2E5577FA6B",
    "--retries", "1",
    "--exit-nosave",
]
launch_supervisor.main()
