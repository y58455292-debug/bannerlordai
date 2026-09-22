from pathlib import Path
import datetime
import json
import sys

root = Path(sys.argv[1])
limit = int(sys.argv[2]) if len(sys.argv) > 2 else 40

rows = []
for p in root.rglob("*"):
    if not p.is_file():
        continue
    try:
        st = p.stat()
    except Exception:
        continue
    rows.append({
        "path": str(p),
        "mtime": datetime.datetime.fromtimestamp(
            st.st_mtime
        ).astimezone().isoformat(),
        "size": st.st_size,
    })

rows.sort(key=lambda x: x["mtime"], reverse=True)
print(json.dumps(rows[:limit], indent=2))
