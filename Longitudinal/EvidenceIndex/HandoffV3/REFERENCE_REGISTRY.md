# BannerlordAI Reference Registry

Purpose: preserve reusable external structures, user-supplied references, and validated prior art so future agents search/adapt before rebuilding.

## Mandatory reuse rule
- For any complex architecture, workflow, memory, agent-control, observability, or AI-decision problem, inspect this registry and the local evidence archive first.
- Existing working structures should be adapted/composed when they fit.
- Replace a referenced structure only when Bannerlord-specific constraints or evidence justify a better design.
- Never copy code blindly; check license, version, fit, and validation requirements.
- Record important new sources and what they taught us in this registry and the architecture ledger.

## User-supplied / explicitly retained references
1. DeepSeek Harness
   URL: https://github.com/deepseek-ai/deepseek-harness.git
   Use: agent runtime/orchestration, tools/files/sessions/subagents, persistent workflows, bounded delegation, compaction.
   BannerlordAI relevance: evidence analysis workers, deterministic verification, task delegation, heavy-research compression.
   Status: reference architecture; do not adopt wholesale without comparison.

2. DeepAstra
   URL: https://github.com/ItsssssJack/DeepAstra.git
   Use: main-assistant -> bounded task brief -> worker -> main-assistant review; isolated project directories; run status/timeouts/provider tracking.
   BannerlordAI relevance: future research-worker/delegation architecture.
   Status: future reference; local note exists under Research/FutureReferences.
3. User architecture video
   URL: https://youtu.be/_MrumlZnGic?si=DGYf5sSl7WkN8EV4
   Use: conceptual explanation of the architecture/workflow approach the user identified as especially clear.
   Status: reference; preserve alongside implementation sources.

4. SkyrimNet GamePlugin
   URL: https://github.com/MinLL/SkyrimNet-GamePlugin
   Use: specialized reasoning, eligibility caching, actor-perspective semantic memory, event-driven/off-screen cognition, model routing, observability.
   BannerlordAI relevance: layered cognition and actor-local knowledge/memory.

5. IntelEngine GamePlugin
   URL: https://github.com/galanx/IntelEngine-GamePlugin
   Use: persistent tasks/commitments, save/load recovery, world-director layer, off-screen agents, conflict awareness.
   BannerlordAI relevance: persistent goals/tasks and background cognition.

6. SeverActions
   URL: https://github.com/Severause/SeverActions
   Use: LLM/high-level intent separated from deterministic legal execution; cheaper background-model use.
   BannerlordAI relevance: cognition chooses intent; TaleWorlds/native deterministic code validates/executes.

7. Inworld ecosystem
   Exact user URL: not recovered in current archive; source retained by name.
   Use: separated STT/LLM/TTS/memory/tools and structured emotion/urgency/cognition metadata.
   BannerlordAI relevance: modular cognition and model routing.
## Researched implementation prior art already used
8. SPTQuestingBots
   URL: https://github.com/dashed/SPTQuestingBots
   Use: utility/action scoring with hysteresis to reduce flip-flopping; selection separated from execution.
   Applied lesson: bounded commitment around the composer, never permanent hidden score bias.

9. GroveGames BehaviourTree
   URL: https://github.com/grovegs/BehaviourTree
   Use: allocation-conscious C# behavior framework; typed blackboards.
   Applied lesson: compact typed state/records and bounded dictionaries; avoid prose/reflection in hot paths.

10. GameAI.Net
    URL: https://github.com/likeslines-maker/GameAI.Net
    Use: centralized/tight world-agent update loop.
    Applied lesson: update cognition at Bannerlord decision/event boundaries, not per-agent per-frame polling.

11. Portal Agent
    URL family: https://github.com/cozyblaze/portal-agent
    Local source notes: C:\Users\csala\Documents\Codex\2026-09-10\bannerlord-astra-prototype\work\prototype\REFERENCES.md
    Use: persistent controller, serialized tool queue, bounded plans, engine observation/control patterns, independent stop-path considerations.
    BannerlordAI relevance: autonomous campaign operator and control-plane safety.


## Debugging / iteration / game-development references
12. Game Dev Happy Hour HK
    URL: https://www.youtube.com/@gamedevhappyhourhk/videos
    User-supplied standing source for production game-development, debugging, playtesting, systemic-design and AI engineering patterns.

13. User Game Dev Happy Hour video hTleipiAEJ8
    URL: https://youtu.be/hTleipiAEJ8?si=yAM-KPWJ7GAQaula
    Use: practical game-development/debugging/iteration reference; extract reusable methods before inventing new workflow machinery.

14. Game AI Pro
    URL: https://www.gameaipro.com/
    Use: production game-AI techniques, architecture and implementation lessons.

15. GDC Vault
    URL: https://www.gdcvault.com/
    Use: industry talks/postmortems for debugging, performance, iteration and systemic game design.

16. AAAI AIIDE
    URL: https://ojs.aaai.org/index.php/AIIDE/
    Use: research evidence for game-AI planning, behavior and evaluation.

17. Bannerlord API Documentation
    URL: https://apidoc.bannerlord.com/
    Use: public API source before reflection/new bridge machinery.

18. Harmony execution / priorities
    URLs:
    - https://harmony.pardeike.net/v3/articles/execution.html
    - https://harmony.pardeike.net/v3/articles/priorities.html
    Use: runtime patch ordering, conflict diagnosis and safe hook behavior.

19. Microsoft .NET performance guidance
    URL family: https://learn.microsoft.com/
    Use: profiling/performance guidance for C# hot paths and telemetry.

20. FsCheck
    URL: https://github.com/fscheck/FsCheck
    Use: property-based testing for controller invariants and state-transition regressions.

Rule: debugging research searches must include GitHub, user-supplied YouTube/talk sources, public API/docs, industry postmortems, and accepted internal live evidence.

## Internal source families that must be checked with the registry
- Accepted LiveValidation evidence and DerivedReports.
- Recovered Calastides chronicle/source index/native history.
- Economy/supply/traffic/market/world censuses and analyzer reports.
- Verified milestone source/build archives.
- ExternalPriorArt directory and FutureReferences.
- ARCHITECTURE_LEARNING.md for accepted mappings and do-not-repeat decisions.

## Source lifecycle
New useful source -> record exact URL/name -> classify purpose -> capture reusable mechanism -> compare with existing architecture -> validate Bannerlord adaptation -> promote lesson to architecture ledger/handoff.
