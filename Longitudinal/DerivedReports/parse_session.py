import re, json, pathlib, datetime
p=pathlib.Path(r"D:\BannerlordAIResearch\Telemetry\BannerlordInspector\logs\inspector.log")
lines=p.read_text(encoding="utf-8", errors="ignore").splitlines()
start_i=0
for i,l in enumerate(lines):
    if "[ClanAI v0.19C] SESSION_START" in l:
        start_i=i
session=lines[start_i:]
visual=[l for l in session if "VISUAL_WAR_WINNER_CHANGE" in l]
context=[l for l in session if "VISUAL_WAR_CONTEXT_REFRESH" in l]
memory=[l for l in session if "WORLD_MEMORY event=" in l]
reasons={}
targets={}
actors={}
for l in visual:
    mr=re.search(r"reason=([^ ]+)",l)
    mt=re.search(r"after=([^ ]+?)(?: reason=)",l)
    ma=re.search(r"actor=(.*?) party=",l)
    if mr: reasons[mr.group(1)]=reasons.get(mr.group(1),0)+1
    if mt: targets[mt.group(1)]=targets.get(mt.group(1),0)+1
    if ma: actors[ma.group(1)]=actors.get(ma.group(1),0)+1
mtypes={}
for l in memory:
    m=re.search(r"WORLD_MEMORY event=([^ ]+)",l)
    if m: mtypes[m.group(1)]=mtypes.get(m.group(1),0)+1
out={
"session_start_line":lines[start_i] if start_i < len(lines) else None,
"last_clanai":next((l for l in reversed(session) if "[ClanAI v0.19C]" in l),None),
"session_line_count":len(session),
"visual_changes":len(visual),
"context_refreshes":len(context),
"memory_events":len(memory),
"reasons":reasons,
"memory_types":mtypes,
"top_targets":sorted(targets.items(), key=lambda x:-x[1])[:20],
"top_actors":sorted(actors.items(), key=lambda x:-x[1])[:20],
"last_visual":visual[-10:],
"last_memory":memory[-10:],
}
print(json.dumps(out, indent=2))
