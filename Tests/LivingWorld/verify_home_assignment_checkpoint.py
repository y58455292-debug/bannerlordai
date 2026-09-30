"""Offline LW1-B preservation check against an authoritative GitHub source archive."""
from pathlib import Path
import sys, zipfile, hashlib, re
root = Path(__file__).resolve().parents[2]
archive = Path(sys.argv[1])
sha = lambda data: hashlib.sha256(data).hexdigest().upper()
with zipfile.ZipFile(archive) as z:
    prefix = z.namelist()[0]
    protected = [n for n in z.namelist() if not n.endswith("/") and (
        n[len(prefix):].startswith("src/ClanAI/package/") or n[len(prefix):].startswith("Releases/"))]
    for name in protected:
        relative = name[len(prefix):]
        assert z.read(name) == (root/relative).read_bytes(), relative
    print("PASS frozen package/Releases byte equality files=" + str(len(protected)))
    source = "src/ClanAI/src/ClanAI/"
    for file in ("HomeResponsibilityPolicy.cs","HomeResponsibilityLayer.cs","WorldScopeContext.cs","StrategicDecisionComposer.cs"):
        assert z.read(prefix+source+file) == (root/source/file).read_bytes(), file
        print("PASS unchanged " + file + " SHA256=" + sha((root/source/file).read_bytes()))
    pattern = r'"(ClanAI_[A-Za-z0-9_]+_v[0-9]+)"'
    before = set()
    for name in z.namelist():
        if name.startswith(prefix+source) and name.endswith(".cs"):
            before.update(re.findall(pattern,z.read(name).decode("utf-8-sig")))
    after=set()
    for p in (root/source).glob("*.cs"):
        after.update(re.findall(pattern,p.read_text(encoding="utf-8-sig")))
    assert after == before | {"ClanAI_HomeAssignment_v1"}
    print("PASS existing save keys preserved count=" + str(len(before)) + "; isolated addition=ClanAI_HomeAssignment_v1")
for relative in ("Releases/ClanAI-v0.22.0-RC1.zip","src/ClanAI/package/ClanAI/bin/Win64_Shipping_Client/ClanAI.dll",
                 "src/ClanAI/src/ClanAI/bin/Release/netstandard2.0/ClanAI.dll"):
    print("SHA256 " + relative + " " + sha((root/relative).read_bytes()))
assert sha((root/"src/ClanAI/package/ClanAI/bin/Win64_Shipping_Client/ClanAI.dll").read_bytes()) == "A18341FB2CD6B8623B45B52155C1A7D987DBEE7AAE14AE7DBF704514EC7F37B5"
print("Bannerlord launched: NO; deployed: NO; runtime proof: NOT YET TESTED")
