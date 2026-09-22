from pathlib import Path
import hashlib
import json
import sys

if len(sys.argv) != 3:
    raise SystemExit("usage: find_hash.py ROOT SHA256")

root = Path(sys.argv[1])
target = sys.argv[2].upper()
hits = []

for p in root.rglob("*"):
    if not p.is_file():
        continue
    try:
        if p.stat().st_size > 20_000_000:
            continue
        h = hashlib.sha256(p.read_bytes()).hexdigest().upper()
    except Exception:
        continue
    if h == target:
        hits.append(str(p))

print(json.dumps(hits, indent=2))
