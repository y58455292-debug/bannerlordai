# BannerlordAI Reference Quick Index

Lookup command:
python -X utf8 D:\BannerlordAIResearch\Tools\ReferenceLookup\lookup.py <term>

## Fast paths
- memory/personality: SkyrimNet, IntelEngine, Inworld, existing SocialEpisodeMemory/SocialLedger
- commitment/hysteresis: SPTQuestingBots + BDI commitment review + accepted v0.20Q evidence
- blackboard/performance: GroveGames BehaviourTree + ActorBlackboard
- autonomous operator/control: Portal Agent + SeverActions + TestRunner/Inspector
- research-worker/delegation: DeepSeek Harness + DeepAstra
- world/economy/trade: local census/analyzer archive first, then external sources

## Source list
- DeepSeek Harness [agents, delegation, workflow, compression, subagents, tools, memory] — Bounded worker delegation, persistent sessions, tools/files, heavy-research compression.
- DeepAstra [agents, delegation, codex, workflow, cost, isolation] — Main-agent -> bounded task -> worker -> review pattern; isolated project runs and status/timeouts.
- User architecture video [architecture, workflow, compression, explanation] — High-level architecture explanation the user found especially clear.
- SkyrimNet GamePlugin [memory, agents, eligibility, event-driven, offscreen, model-routing, observability] — Layered cognition, actor-local knowledge, eligibility caching, event-driven/off-screen reasoning.
- IntelEngine GamePlugin [goals, tasks, commitment, memory, save-load, offscreen, world-director] — Persistent tasks/commitments, save/load recovery, world-director patterns and off-screen agents.
- SeverActions [actions, deterministic-execution, llm, intent, validation] — High-level intent separated from registered/legal deterministic execution.
- Inworld ecosystem [memory, emotion, llm, tools, routing, modular-cognition] — Modular STT/LLM/TTS/memory/tools and structured cognition metadata.
- SPTQuestingBots [utility, hysteresis, commitment, actions, scoring] — Utility scoring plus hysteresis to reduce action flip-flopping.
- GroveGames BehaviourTree [blackboard, csharp, performance, behavior-tree, typed-state] — Allocation-conscious C# behavior framework and typed blackboard patterns.
- GameAI.Net [tick, agents, performance, world-loop] — Centralized world/agent update loop instead of expensive per-agent per-frame polling.
- Portal Agent [operator, controller, mcp, automation, stop-path, observability] — Persistent controller, serialized tool queue, bounded plans and game observation/control patterns.
