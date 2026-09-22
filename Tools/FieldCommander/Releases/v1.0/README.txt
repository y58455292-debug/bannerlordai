FIELD COMMANDER v1.0
External voice observer for live AI/mod testing.

Core rule:
WHAT CHANGED + WHERE SHOULD I LOOK + ALIVE REACTION = VOICE

Automatic reactions:
- active frontier defense
- frontier defense movement
- frontier offense movement
- rear-area security/bandit response
- new raid memory
- hero capture memory
- intentional mercy/release memory
- proven memory-driven decision change

Design:
- separate from Bannerlord
- starts at current telemetry EOF, never narrates old history
- 500ms file-tail check
- prioritizes the most important pending report
- 7s speech cooldown
- 120s duplicate suppression
- no browser, overlay, render hook, or game-loop model
- custom Commander Aldric v1 local voice profile

Manual speech:
Write a sentence to D:\BannerlordAIResearch\Tools\FieldCommander\command.txt

Stop:
Write !stop to command.txt