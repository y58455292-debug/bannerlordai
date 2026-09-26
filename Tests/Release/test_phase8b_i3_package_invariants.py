#!/usr/bin/env python3
from __future__ import annotations

import hashlib
import re
import sys
import xml.etree.ElementTree as ET
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
PACKAGE = ROOT / "src" / "ClanAI" / "package" / "ClanAI"
DLL = PACKAGE / "bin" / "Win64_Shipping_Client" / "ClanAI.dll"
MANIFEST = ROOT / "Reports" / "Release" / "evidence" / "phase8b_i3_package_manifest_20260925.txt"
EXPECTED_FILES = {
    "README.md",
    "SubModule.xml",
    "bin/Win64_Shipping_Client/ClanAI.dll",
}
EXPECTED_SAVE_KEYS = {
    "ClanAI_NobleMemory_v1",
    "ClanAI_SocialLedger_v1",
    "ClanAI_SocialEpisodes_v1",
    "ClanAI_DynastyCanonCutoffHours_v1",
    "ClanAI_DynastyBranchId_v1",
    "ClanAI_DynastyBranchEpisodes_v1",
    "ClanAI_CompanionDutyMemory_v1",
    "ClanAI_CompanionExperienceMemory_v1",
    "ClanAI_CompanionNegativeOutcomeMemory_v1",
    "ClanAI_SocialLoyaltyClanLoss_v2",
    "ClanAI_WarState_v1",
    "ClanAI_KingdomContinuity_v1",
}


def fail(message: str) -> None:
    raise AssertionError(message)


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()


def package_files() -> dict[str, Path]:
    return {
        path.relative_to(PACKAGE).as_posix(): path
        for path in PACKAGE.rglob("*")
        if path.is_file()
    }


def validate_package_shape() -> None:
    files = package_files()
    if set(files) != EXPECTED_FILES:
        fail(f"unexpected package content: {sorted(files)}")
    if not DLL.is_file() or DLL.stat().st_size == 0:
        fail("ClanAI.dll is missing or empty")
    forbidden_suffixes = {".pdb", ".deps.json", ".log", ".sav", ".mp4", ".zip"}
    if any(path.suffix.lower() in forbidden_suffixes for path in files.values()):
        fail("package contains a forbidden artifact type")
    if any("harmony" in name.lower() for name in files if name != "SubModule.xml"):
        fail("Harmony runtime was bundled")


def validate_module_contract() -> None:
    root = ET.parse(PACKAGE / "SubModule.xml").getroot()
    value = lambda tag: root.find(tag).attrib["value"]
    if value("Id") != "ClanAI" or value("Version") != "v0.22.0":
        fail("module identity/version mismatch")
    dependencies = {
        node.attrib["Id"]: (node.attrib["DependentVersion"], node.attrib["Optional"])
        for node in root.findall("./DependedModules/DependedModule")
    }
    expected = {
        "Native": ("v1.5.3", "false"),
        "SandBoxCore": ("v1.5.3", "false"),
        "Sandbox": ("v1.5.3", "false"),
        "StoryMode": ("v1.5.3", "false"),
        "Bannerlord.Harmony": ("v2.4.2.248", "false"),
    }
    if dependencies != expected or "NavalDLC" in dependencies:
        fail(f"dependency contract mismatch: {dependencies}")
    submodule = root.find("./SubModules/SubModule")
    if submodule is None:
        fail("missing SubModule entry")
    if submodule.find("DLLName").attrib["value"] != "ClanAI.dll":
        fail("DLL entry mismatch")
    if submodule.find("SubModuleClassType").attrib["value"] != "ClanAI.ClanAISubModule":
        fail("entry-point mismatch")


def validate_forbidden_content() -> None:
    payload = DLL.read_bytes().lower()
    forbidden_binary_tokens = (
        b"d:\\bannerlorda iresearch".replace(b" ", b""),
        b"c:\\users\\",
        b"chatgpt",
        b"codex",
        b"desktop commander",
        b"testrunner",
        b"watchdog",
        b"http://",
        b"https://",
    )
    hits = [token.decode("ascii") for token in forbidden_binary_tokens if token in payload]
    if hits:
        fail(f"forbidden runtime token(s) in DLL: {hits}")
    package_text = "\n".join(
        path.read_text(encoding="utf-8", errors="ignore")
        for path in package_files().values()
        if path.suffix.lower() in {".md", ".xml", ".cfg", ".txt"}
    )
    if "D:\\BannerlordAIResearch" in package_text:
        fail("development-machine path in package")
    forbidden_names = re.compile(r"(^|/)(Reports|Tests|Automation|Logs)(/|$)", re.IGNORECASE)
    if any(forbidden_names.search(name) for name in package_files()):
        fail("repository/research artifact directory in package")


def validate_release_defaults_and_save_keys() -> None:
    if (PACKAGE / "Data" / "RuntimeProfile.cfg").exists():
        fail("runtime profile must default by absence; Evidence config must not ship")
    if (PACKAGE / "Data" / "ENABLE_VISUAL_WAR_LAB.txt").exists():
        fail("Visual War enable marker must not ship")
    if (PACKAGE / "Data" / "StrategicCommitment.cfg").exists():
        fail("Strategic Commitment override must not ship")
    source = "\n".join(
        path.read_text(encoding="utf-8", errors="ignore")
        for path in (ROOT / "src" / "ClanAI" / "src" / "ClanAI").glob("*.cs")
    )
    found = set(re.findall(r'ClanAI_[A-Za-z0-9_]+_v\d+', source))
    if found != EXPECTED_SAVE_KEYS:
        fail(f"save-key contract changed: {sorted(found)}")


def validate_manifest() -> None:
    rows = {}
    for line in MANIFEST.read_text(encoding="utf-8").splitlines():
        if not line or line.startswith("#") or line.startswith("relative_path|"):
            continue
        relative, size, digest = line.split("|")
        rows[relative] = (int(size), digest)
    files = package_files()
    expected = {name: (path.stat().st_size, sha256(path)) for name, path in files.items()}
    if rows != expected:
        fail(f"package manifest mismatch: rows={rows} expected={expected}")


def main() -> int:
    validate_package_shape()
    print("PASS Phase 8B-I3 exact package-content invariant")
    validate_module_contract()
    print("PASS Phase 8B-I3 module/dependency contract invariant")
    validate_forbidden_content()
    print("PASS Phase 8B-I3 forbidden path/runtime dependency invariant")
    validate_release_defaults_and_save_keys()
    print("PASS Phase 8B-I3 release-default/save-key invariant")
    validate_manifest()
    print("PASS Phase 8B-I3 deterministic package-manifest invariant")
    return 0


if __name__ == "__main__":
    try:
        sys.exit(main())
    except AssertionError as error:
        print(f"FAIL {error}")
        sys.exit(1)

