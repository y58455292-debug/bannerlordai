from pathlib import Path
import json, datetime, hashlib

root = Path(r"C:\Users\csala\OneDrive\Documents\Mount and Blade II Bannerlord\Game Saves")
rows = []
for p in root.glob("*.sav"):
    st = p.stat()
    rows.append({
        "name": p.name,
        "size": st.st_size,
        "mtime": datetime.datetime.fromtimestamp(st.st_mtime).astimezone().isoformat(),
    })

rows.sort(key=lambda x: x["mtime"])
print(json.dumps(rows, indent=2))
