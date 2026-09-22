from pathlib import Path
import shutil, datetime, hashlib, json, os

root = Path(r"D:\BannerlordAIResearch")
mod = Path(r"C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\Modules\ClanAI")
binp = mod / "bin/Win64_Shipping_Client"
src = root / r"workspace\_TestRunner_v001\bin\Release\netstandard2.0\BannerlordAITestRunner.dll"
xml = mod / "SubModule.xml"

stamp = datetime.datetime.now().strftime("%Y%m%d_%H%M%S")
backup = root / "workspace/_PatchBackups" / ("Before_TestRunner_v001_" + stamp)
backup.mkdir(parents=True)
shutil.copy2(xml, backup / "SubModule.xml")
shutil.copy2(src, binp / "BannerlordAITestRunner.dll")
txt = xml.read_text(encoding="utf-8-sig")
if "BannerlordAITestRunner.dll" not in txt:
    marker = "  </SubModules>"
    block = """    <SubModule>
      <Name value="BannerlordAI Test Runner" />
      <DLLName value="BannerlordAITestRunner.dll" />
      <SubModuleClassType value="BannerlordAITestRunner.TestRunnerSubModule" />
      <Tags>
        <Tag key="DedicatedServerType" value="none" />
        <Tag key="IsNoRenderModeElement" value="false" />
      </Tags>
    </SubModule>
"""
    if marker not in txt:
        raise RuntimeError("SubModules closing marker not found")
    txt = txt.replace(marker, block + marker)
    xml.write_text(txt, encoding="utf-8")
target_src = root / r"Longitudinal\SaveCheckpoints\20260918_161001_unexpected_exit_write\ClanAI v020D DIAGNOSTIC TEST.sav"
target = Path(r"C:\Users\csala\OneDrive\Documents\Mount and Blade II Bannerlord\Game Saves\ClanAI v020K MEMORY TARGETED OBSERVE TEST.sav")
shutil.copyfile(target_src, target)
now = datetime.datetime.now().timestamp()
os.utime(target, (now, now))

auto = root / "Automation/TestRunner"
auto.mkdir(parents=True, exist_ok=True)
for p in [auto/"command.txt", auto/"status.txt"]:
    if p.exists():
        p.unlink()

def sha(p):
    return hashlib.sha256(p.read_bytes()).hexdigest().upper()
out = {
    "backup": str(backup),
    "runner_sha256": sha(binp/"BannerlordAITestRunner.dll"),
    "xml_has_runner": "BannerlordAITestRunner.dll" in xml.read_text(encoding="utf-8"),
    "target_save": str(target),
    "target_sha256": sha(target),
    "source_sha256": sha(target_src),
    "target_mtime": datetime.datetime.fromtimestamp(target.stat().st_mtime).astimezone().isoformat(),
}
print(json.dumps(out, indent=2))

expected = "11E3F8C04A1DF3ACA4C0DA4D51F9BB912C4D2970114C66B403729F2E5577FA6B"
assert out["target_sha256"] == out["source_sha256"] == expected
assert out["xml_has_runner"]
