from pathlib import Path
import hashlib
import re

ROOT = Path(__file__).resolve().parents[2]
SRC = ROOT / "src" / "ClanAI" / "src" / "ClanAI"

source_files = list(SRC.glob("*.cs"))
production = "\n".join(p.read_text(encoding="utf-8") for p in source_files)
submodule = (SRC / "SubModule.cs").read_text(encoding="utf-8")
bandit_patch = (SRC / "LocalBanditControlPatch.cs").read_text(encoding="utf-8")

failed = []

if "D:\\BannerlordAIResearch" in production:
    failed.append("active production D:\\BannerlordAIResearch path")

for token in (
    "System.Net", "HttpClient", "WebRequest", "Socket", "DesktopCommander",
    "ChatGPTClient", "CodexClient", "TestRunner.", "Watchdog.",
):
    if token in production:
        failed.append("forbidden runtime dependency: " + token)

paths = (SRC / "ModuleRuntimePaths.cs").read_text(encoding="utf-8")
for token in (
    "ResolveModuleDirectory", '"bin"', 'Current("Data", fileName)',
    'Current("Logs", fileName)', "Path.IsPathRooted", 'part.IndexOf(".."',
):
    if token not in paths:
        failed.append("missing path boundary: " + token)

for name in (
    "ClanAISwitch.cs", "ClanAIStrategicBehavior.cs", "SocialEpisodeMemory.cs",
    "DynastyMindSeed.cs", "DynastyMindCausalConfig.cs",
    "SocialMemoryCausalConfig.cs", "RecoveryGateConfig.cs",
):
    if "ModuleRuntimePaths." not in (SRC / name).read_text(encoding="utf-8"):
        failed.append("path not migrated: " + name)

profile = (SRC / "RuntimeProfile.cs").read_text(encoding="utf-8")
for token in (
    "ClanAIRuntimeProfile.Release", "ClanAIRuntimeProfile.Evidence",
    "missing_default_release", "invalid_profile_default_release",
    'ModuleRuntimePaths.Data(ConfigFileName)',
):
    if token not in profile:
        failed.append("missing release-profile rule: " + token)

evidence_block = re.search(
    r"if \(RuntimeProfile\.EvidenceEnabled\)\s*\{(?P<body>.*?)\n\s*\}",
    submodule,
    re.S,
)
if evidence_block is None:
    failed.append("no evidence-only registration block")
else:
    body = evidence_block.group("body")
    # Find the registration block specifically, not the earlier lifecycle gates.
    if "GenerationalContinuityPreflightBehavior" not in body:
        registration = submodule[submodule.find("new KingdomContinuityBehavior") :]
        if not all(token in registration for token in (
            "if (RuntimeProfile.EvidenceEnabled)",
            "new GenerationalContinuityPreflightBehavior()",
            "new Phase4ARecoveryObserverBehavior()",
            "new DynastyStructuralRuntimeObserver()",
        )):
            failed.append("experiment observers not evidence-gated")

for token in (
    "CivicProjectRuntimeTelemetry.Install(",
    "LocalManpowerRuntimeTelemetry.Install();",
):
    position = submodule.find(token)
    gate = submodule.rfind("if (RuntimeProfile.EvidenceEnabled)", 0, position)
    if position < 0 or gate < 0 or position - gate > 180:
        failed.append("proof telemetry not evidence-gated: " + token)

position = bandit_patch.find("LocalBanditControlRuntimeTelemetry.Install();")
gate = bandit_patch.rfind("if (RuntimeProfile.EvidenceEnabled)", 0, position)
if position < 0 or gate < 0 or position - gate > 120:
    failed.append("bandit proof telemetry not evidence-gated")

for behavior in (
    "new ClanAIStrategicBehavior()", "new ClanAIObserverSafetyBehavior()",
    "new SocialCaptivityObserverBehavior()", "new PrisonerMercyDecisionBehavior()",
    "new WarStateBehavior()", "new RulerClanCourtshipBehavior()",
    "new KingdomContinuityBehavior()",
):
    if submodule.count(behavior) != 1:
        failed.append("gameplay behavior registration changed: " + behavior)

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

print("PASS Phase 8B standalone production dependency/path invariant")
print("PASS Phase 8B release-default/evidence-opt-in wiring invariant")
print("PASS Phase 8B gameplay behavior and save-key preservation invariant")
print("PASS Phase 3/4/5/6/7 policy hash preservation invariant")

