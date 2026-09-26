from pathlib import Path
import hashlib
import re
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[2]
SRC = ROOT / "src" / "ClanAI" / "src" / "ClanAI"
PACKAGE = ROOT / "src" / "ClanAI" / "package" / "ClanAI"
MANIFEST = PACKAGE / "SubModule.xml"
PROJECT = SRC / "ClanAI.csproj"

failed = []

tree = ET.parse(MANIFEST)
module = tree.getroot()
version_node = module.find("Version")
module_version = None if version_node is None else version_node.attrib.get("value")

dependencies = {
    item.attrib.get("Id"): (
        item.attrib.get("DependentVersion"),
        item.attrib.get("Optional"),
    )
    for item in module.findall("./DependedModules/DependedModule")
}

if dependencies.get("Bannerlord.Harmony") != ("v2.4.2.248", "false"):
    failed.append("external Bannerlord.Harmony dependency contract missing")
if "NavalDLC" in dependencies:
    failed.append("NavalDLC remains required without production use")
if set(dependencies) != {
    "Native", "SandBoxCore", "Sandbox", "StoryMode", "Bannerlord.Harmony"
}:
    failed.append("unexpected manifest dependency set: " + repr(sorted(dependencies)))

bundled_harmony = [
    p for p in PACKAGE.rglob("*")
    if p.is_file() and p.name.lower() in {
        "0harmony.dll", "bannerlord.harmony.dll", "monomod.core.dll",
        "monomod.utils.dll", "monomod.backports.dll",
    }
]
if bundled_harmony:
    failed.append("Harmony runtime accidentally bundled: " + repr(bundled_harmony))

production = "\n".join(
    p.read_text(encoding="utf-8") for p in SRC.glob("*.cs")
)
for token in ("using NavalDLC", "NavalDLC.", "TaleWorlds.Naval", "NavalDLC.dll"):
    if token in production:
        failed.append("production NavalDLC dependency: " + token)

project = PROJECT.read_text(encoding="utf-8")
for token in (
    "<BannerlordInstallDir Condition=",
    "<BannerlordBinDir Condition=",
    "<BannerlordHarmonyBinDir Condition=",
    "$(BannerlordBinDir)\\TaleWorlds.CampaignSystem.dll",
    "$(BannerlordHarmonyBinDir)\\0Harmony.dll",
    '<Reference Include="0Harmony">',
    "<Private>false</Private>",
):
    if token not in project:
        failed.append("portable/external build contract missing: " + token)

identity = (SRC / "ReleaseIdentity.cs").read_text(encoding="utf-8")
expected_version = "v0.22.0"
if module_version != expected_version:
    failed.append("manifest version mismatch: " + repr(module_version))
for token in (
    "<AssemblyVersion>0.22.0.0</AssemblyVersion>",
    "<FileVersion>0.22.0.0</FileVersion>",
    "<InformationalVersion>v0.22.0</InformationalVersion>",
):
    if token not in project:
        failed.append("assembly version mismatch: " + token)
if 'Version = "v0.22.0"' not in identity:
    failed.append("runtime release identity mismatch")

runtime_identity_files = (
    "ClanAIStrategicBehavior.cs", "ClanAIDiagnostics.cs",
    "DynastyMindOperatorBridge.cs", "SocialEpisodeMemory.cs",
)
for name in runtime_identity_files:
    text = (SRC / name).read_text(encoding="utf-8")
    if "ReleaseIdentity.Version" not in text:
        failed.append("runtime identity not centralized: " + name)
    for stale in (
        "v0.1.0", "v0.22A-ruler-courtship-native-v1",
        "v0.20V-dynasty-lived-choice-shadow", "v0.20B", "v0.9",
        "_v020Q_review",
    ):
        if stale in text:
            failed.append("stale active runtime version in " + name + ": " + stale)

save_keys = set(re.findall(r'"(ClanAI_[A-Za-z0-9_]+_v[0-9]+)"', production))
expected_keys = {
    "ClanAI_NobleMemory_v1", "ClanAI_SocialLedger_v1",
    "ClanAI_SocialEpisodes_v1", "ClanAI_DynastyCanonCutoffHours_v1",
    "ClanAI_DynastyBranchId_v1", "ClanAI_DynastyBranchEpisodes_v1",
    "ClanAI_CompanionDutyMemory_v1", "ClanAI_CompanionExperienceMemory_v1",
    "ClanAI_CompanionNegativeOutcomeMemory_v1", "ClanAI_SocialLoyaltyClanLoss_v2",
    "ClanAI_WarState_v1", "ClanAI_KingdomContinuity_v1",
}
if save_keys != expected_keys:
    failed.append("save-key set changed: " + repr(sorted(save_keys)))

expected_policy_hashes = {
    "HomeResponsibilityPolicy.cs": "7B7A31EA8F7831F3AA5D48AC492D414B1B2619985E8205BE9F5289D00E3294AA",
    "KingdomObjectivePolicy.cs": "CD0E5EDDC68D58BF97C4EB06535C4A757304FD4F3B815A8DECF050F6B3CA6A68",
    "VisualWarPolicy.cs": "15B82EF59E53FD1A1F671B7FD3A7F6F101560EC99BD4C8A88407C71A11AD04B3",
    "StrategicCommitmentPolicy.cs": "48BCF4E734AA8E0C133B24884442BAA5E409DFAFB7FD858593506E4E91CC170E",
    "LocalManpowerProbabilityPolicy.cs": "CCA6ABF387D98F02E97E54FFD1D68B0E23AABE6450672CFAED30AC8AB67FFD49",
    "TroopQualityProbabilityPolicy.cs": "F1B683AD4169054F73816ED052C51FCD4F46412D74CB0AC6E6BFA274E72A6534",
    "LocalBanditControlPolicy.cs": "890DED9CAA10B1A4E84ACE80FBE5788E96E00FCE17654325E5EB94B5494A3FC2",
    "CivicProjectSelectionPolicy.cs": "D1EE6FEFF19D696ED66D99BEF2175A73F1F4198982EFBE1279F22A5B48A31D92",
    "DynastyEpisodeRetrievalPolicy.cs": "0DF3651DE73F06F75DAB11737BC693936D46FF22D7C3640DA320F33901583AEB",
    "DynastyStructuralEpisodePolicy.cs": "E92B09C74CC41BFD9A173A36B14793B6657A9C61544B2DBA3BE29303C442A0A2",
}
for name, expected in expected_policy_hashes.items():
    actual = hashlib.sha256((SRC / name).read_bytes()).hexdigest().upper()
    if actual != expected:
        failed.append(f"policy changed: {name} {actual}")

if failed:
    for item in failed:
        print("FAIL", item)
    raise SystemExit(1)

print("PASS Phase 8B-I2 external Harmony dependency/no-bundle invariant")
print("PASS Phase 8B-I2 NavalDLC non-dependency invariant")
print("PASS Phase 8B-I2 version identity invariant v0.22.0")
print("PASS Phase 8B-I2 configurable build-root invariant")
print("PASS Phase 8B-I1/save-key/gameplay-policy preservation invariant")

