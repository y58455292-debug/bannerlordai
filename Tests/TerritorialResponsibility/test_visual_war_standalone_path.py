from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[2]
BASE = ROOT / "src" / "ClanAI" / "src" / "ClanAI"
LAYER_PATH = BASE / "VisualWarDecisionLayer.cs"
ACTIVATION_PATH = BASE / "VisualWarActivation.cs"

layer = LAYER_PATH.read_text(encoding="utf-8")
activation = ACTIVATION_PATH.read_text(encoding="utf-8")
combined = layer + "\n" + activation

failed = []

if "D:\\BannerlordAIResearch" in combined:
    failed.append("development_root_reference_present")

if re.search(r'@"[A-Za-z]:\\', activation):
    failed.append("absolute_windows_path_present")

checks = {
    "assembly_location_source":
        r"typeof\(VisualWarDecisionLayer\)\.Assembly\.Location",
    "activation_resolver_used":
        r"VisualWarActivation\.ResolveEnablePath\(",
    "safe_off_empty_path":
        r"!string\.IsNullOrEmpty\(EnablePath\)",
    "marker_must_exist":
        r"File\.Exists\(EnablePath\)",
    "module_data_directory":
        r'Path\.Combine\([\s\S]*?"Data"[\s\S]*?MarkerFileName',
    "marker_filename":
        r'ENABLE_VISUAL_WAR_LAB\.txt',
    "expected_bin_layout_required":
        r'"bin"[\s\S]*?StringComparison\.OrdinalIgnoreCase',
}

for name, pattern in checks.items():
    source = layer if name in {
        "assembly_location_source",
        "activation_resolver_used",
        "safe_off_empty_path",
        "marker_must_exist",
    } else activation
    if not re.search(pattern, source, re.S):
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

if failed:
    print("FAIL VisualWar standalone activation path")
    for name in failed:
        print(" -", name)
    sys.exit(1)

print("PASS VisualWar standalone activation path")
print("activation is module-local Data/ENABLE_VISUAL_WAR_LAB.txt and OFF when absent")