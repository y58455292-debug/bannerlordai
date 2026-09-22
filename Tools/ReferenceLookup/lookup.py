from pathlib import Path
import json,sys
p=Path(r"D:\BannerlordAIResearch\Longitudinal\EvidenceIndex\HandoffV3\REFERENCE_REGISTRY.json")
d=json.loads(p.read_text(encoding="utf-8"))
q=" ".join(sys.argv[1:]).strip().lower()
if not q:
    print("usage: lookup.py <memory|operator|blackboard|trade|deepseek|hysteresis|...>")
    raise SystemExit(0)
terms=q.split()
scored=[]
for s in d["sources"]:
    hay=" ".join([s["id"],s["name"],s["type"],s["use"]," ".join(s["tags"]),s.get("status","")]).lower()
    score=sum(3 if t in [x.lower() for x in s["tags"]] else 1 for t in terms if t in hay)
    if score:
        scored.append((score,s))
for score,s in sorted(scored,key=lambda x:(-x[0],x[1]["name"])):
    print(f"{s['name']}  score={score}")
    print(f"  use: {s['use']}")
    print(f"  url: {s.get('url') or '<not recovered>'}")
    if s.get("local_note"): print(f"  local: {s['local_note']}")
    print(f"  status: {s['status']}")
