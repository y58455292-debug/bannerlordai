FIELD COMMANDER v2.1 — SPOTTER MODE

Purpose:
Keep the exact approved Commander Aldric Neural v2 voice, but make live directions short enough to use while teleporting and watching the campaign map.

Core rule:
WHERE TO GO + WHAT TO WATCH.

Voice identity:
UNCHANGED from approved v2.0.
45% bm_george
25% bm_fable
20% bm_lewis
10% bm_daniel
Kokoro 82M v1.0 int8, British English.

Automatic spoken events:
- active defense
- frontier offense
- frontier defense
- rear-area security / bandit hunting
- raid worth watching
- proven memory-driven strategic decision

Silent but still logged:
- captures
- mercy/release by itself
These become spoken only when they later produce an observable strategic change.

Latency design:
- shorter 25–35 character orders where possible
- first-time synthesis typically ~3–5 seconds on this PC
- every rendered dispatch is cached
- identical future dispatches play immediately from cache
- 5 second auto-speech cooldown
- 120 second duplicate suppression
- no voice/model work inside Bannerlord

Examples:
To Saneopa. Watch Maritzios.
To Gorcorys. Follow Maritzios.
To Fenon Etir. Watch the raid.
Find Olek. Watch the hunt.