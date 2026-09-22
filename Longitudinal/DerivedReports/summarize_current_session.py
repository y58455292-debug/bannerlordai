import re, json, pathlib, collections, datetime as dt
logp=pathlib.Path(r"D:\BannerlordAIResearch\Telemetry\BannerlordInspector\logs\inspector.log")
lines=logp.read_text(encoding="utf-8",errors="ignore").splitlines()
starts=[i for i,l in enumerate(lines) if "[ClanAI v0.19C] SESSION_START" in l]
start_i=starts[-1] if starts else 0
sess=lines[start_i:]

def ts(line):
    m=re.match(r"(\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d+)?Z)",line)
    return dt.datetime.fromisoformat(m.group(1).replace("Z","+00:00")) if m else None

clan=[l for l in sess if "[ClanAI v0.19C]" in l]
visual=[l for l in clan if "VISUAL_WAR_WINNER_CHANGE" in l]
ctx=[l for l in clan if "VISUAL_WAR_CONTEXT_REFRESH" in l]
worldmem=[l for l in clan if "WORLD_MEMORY event=" in l]
cap=[l for l in clan if "CAPTIVITY_EVENT type=Taken" in l]
rel=[l for l in clan if "CAPTIVITY_RELEASE" in l]
mercy=[l for l in clan if "WORLD_MEMORY event=MercyRelease" in l]
probes=[l for l in clan if "MEMORY_SCORE_PROBE" in l]
probe_flips=[l for l in probes if "wouldFlip=True" in l]
memcausal=[l for l in clan if "MEMORY_CAUSAL_RESULT" in l]
summary=[l for l in clan if " SUMMARY " in l]

reasons=collections.Counter()
actors=collections.Counter()
raw_targets=collections.Counter()
actor_reason=collections.Counter()
events=[]
for l in visual:
    m=re.search(r"actor=(.*?) party=.*? before=(.*?) after=(.*?) reason=([^ ]+)",l)
    if not m: continue
    actor,before,after,reason=m.groups()
    t=ts(l)
    reasons[reason]+=1; actors[actor]+=1; raw_targets[after]+=1; actor_reason[(actor,reason)]+=1
    events.append((t,actor,before,after,reason))

# intervals / chatter
intervals=[]
for a,b in zip(events,events[1:]):
    if a[0] and b[0]: intervals.append((b[0]-a[0]).total_seconds())
per_min=collections.Counter()
for e in events:
    if e[0]: per_min[e[0].strftime("%Y-%m-%dT%H:%MZ")]+=1

# repeated same actor-after target
same_actor_target=collections.Counter((a,af) for _,a,_,af,_ in events)

memtypes=collections.Counter()
rememberers=collections.Counter()
for l in worldmem:
    m=re.search(r"WORLD_MEMORY event=([^ ]+)",l)
    if m: memtypes[m.group(1)]+=1
    m=re.search(r"rememberer=(.*?) remembererClan=",l)
    if m: rememberers[m.group(1)]+=1

bandits=[]
for l in ctx:
    m=re.search(r"bandits=(\d+)",l)
    if m: bandits.append(int(m.group(1)))

out={
"session_start":clan[0] if clan else None,
"session_end":clan[-1] if clan else None,
"clanai_lines":len(clan),
"visual_winner_changes":len(visual),
"visual_reason_counts":dict(reasons),
"unique_visual_actors":len(actors),
"top_visual_actors":actors.most_common(15),
"top_after_targets":raw_targets.most_common(20),
"same_actor_target_repeats":same_actor_target.most_common(15),
"peak_visual_changes_per_minute":max(per_min.values()) if per_min else 0,
"top_minutes":per_min.most_common(10),
"median_seconds_between_visual_changes":sorted(intervals)[len(intervals)//2] if intervals else None,
"context_refresh_count":len(ctx),
"bandit_first":bandits[0] if bandits else None,
"bandit_last":bandits[-1] if bandits else None,
"bandit_min":min(bandits) if bandits else None,
"bandit_max":max(bandits) if bandits else None,
"world_memory_events":len(worldmem),
"world_memory_types":dict(memtypes),
"unique_rememberers":len(rememberers),
"captivity_taken_events":len(cap),
"captivity_release_events":len(rel),
"mercy_memory_events":len(mercy),
"memory_score_probes":len(probes),
"memory_probe_would_flip":len(probe_flips),
"memory_causal_results":len(memcausal),
"last_summary":summary[-1] if summary else None
}
outp=pathlib.Path(r"D:\BannerlordAIResearch\Longitudinal\DerivedReports\2026-09-18_session_summary.json")
outp.write_text(json.dumps(out,indent=2),encoding="utf-8")
print(json.dumps(out,indent=2))
