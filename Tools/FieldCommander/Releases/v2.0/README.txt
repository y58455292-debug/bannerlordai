FIELD COMMANDER v2.0 — NEURAL RELEASE

Purpose
-------
A separate external utility for live game/mod observation.
It does not inject voice generation into Bannerlord.

Core rule
---------
WHAT CHANGED + WHERE SHOULD I LOOK + ALIVE REACTION = VOICE

Voice
-----
Commander Aldric Neural v2 is a CUSTOM BLENDED neural voice:
  45% bm_george
  25% bm_fable
  20% bm_lewis
  10% bm_daniel

Renderer: Kokoro 82M v1.0 int8, fully local after installation.
Language delivery: British English.
Urgency changes delivery speed.
A very light room reflection is added after synthesis.

Automatic reactions
-------------------
- active frontier defense
- frontier defense movement
- frontier offense movement
- rear-area security / bandit response
- raid memory
- capture memory
- intentional mercy memory
- memory-caused strategic decision changes

Performance design
------------------
- separate process
- BELOW_NORMAL Windows process priority
- model loads once
- no browser or overlay
- no code in the game's render loop
- no model in the game AI loop
- 450ms lightweight file-tail poll
- 7 second speech cooldown
- 120 second duplicate suppression
- voice inference runs only when a meaningful report is spoken

Manual command channel
----------------------
Write any sentence to:
D:\BannerlordAIResearch\Tools\FieldCommander\command.txt

Stop
----
Write:
!stop

Runtime status
--------------
D:\BannerlordAIResearch\Tools\FieldCommander\v2_ready.txt
D:\BannerlordAIResearch\Tools\FieldCommander\field_commander_v2.log
