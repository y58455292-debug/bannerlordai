from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[2]
BASE = ROOT / "src" / "ClanAI" / "src" / "ClanAI"
LAYER = (BASE / "StrategicCommitmentLayer.cs").read_text(encoding="utf-8")
CONFIG = (BASE / "StrategicCommitmentConfig.cs").read_text(encoding="utf-8")
POLICY = (BASE / "StrategicCommitmentPolicy.cs").read_text(encoding="utf-8")
CFG = (ROOT / "Data" / "StrategicCommitment.cfg").read_text(encoding="utf-8")
combined = LAYER + "\n" + CONFIG + "\n" + POLICY

failed = []

if "D:\\BannerlordAIResearch" in combined:
    failed.append("development_root_reference_present")

if re.search(r'@"[A-Za-z]:\\', CONFIG):
    failed.append("absolute_windows_path_present")

checks = {
    "assembly_location_source":
        r"typeof\(StrategicCommitmentConfig\)[\s\S]*?\.Assembly[\s\S]*?\.Location",
    "module_data_config":
        r'Path\.Combine\([\s\S]*?"Data"[\s\S]*?ConfigFileName',
    "expected_bin_layout":
        r'"bin"[\s\S]*?StringComparison\.OrdinalIgnoreCase',
    "missing_path_observe":
        r"string\.IsNullOrEmpty\([\s\S]*?ConfigPath[\s\S]*?missing_default_observe",
    "invalid_mode_observe":
        r"invalid_mode_default_observe",
    "observe_value":
        r'"Observe"',
    "apply_value":
        r'"Apply"',
}

for name, pattern in checks.items():
    if not re.search(pattern, CONFIG, re.S):
        failed.append(name)

for forbidden in (
    "ChatGPT",
    "Codex",
    "Desktop Commander",
    "DesktopCommander",
    "TestRunner",
    "watchdog",
):
    if forbidden in combined:
        failed.append("development_tool_dependency:" + forbidden)

mode_lines = [
    line.strip()
    for line in CFG.splitlines()
    if line.strip() and not line.strip().startswith("#")
]
if mode_lines != ["Mode=Observe"]:
    failed.append("tracked_config_must_default_observe:" + repr(mode_lines))

if failed:
    print("FAIL Strategic Commitment standalone path")
    for name in failed:
        print(" -", name)
    sys.exit(1)

print("PASS Strategic Commitment standalone path")
print("config resolves module-locally and tracked/missing/invalid defaults remain Observe")