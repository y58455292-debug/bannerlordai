import sys
sys.path.insert(0, r"D:\BannerlordAIResearch\Tools\Operator")
import launch_supervisor
sys.argv = [
    "launch_supervisor.py",
    "--save", "ClanAI MANAN BRANCH ROOT.sav",
    "--expected-save-sha", "94DFE7F05D89C6E4CC328A416943608713ECFFDF96A572AAEA43A9BDD5CD2DAA",
    "--retries", "1",
]
launch_supervisor.main()
