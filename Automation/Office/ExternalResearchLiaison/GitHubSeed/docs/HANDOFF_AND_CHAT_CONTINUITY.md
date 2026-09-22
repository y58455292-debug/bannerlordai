# Handoff & Chat Continuity

Chat history is not operational memory.

## Durable-state rule

A research chat must be replaceable at any time without replaying a huge transcript.

The Liaison should maintain:

`research/state/current.json` — compact active state  
`research/state/events.jsonl` — append-only meaningful transitions  
`research/packets/` — finished research packets  
`research/cache/` — optional large research notes / source indexes

## Required recovery sequence

A replacement Liaison chat should:
1. read this onboarding;
2. read `research/state/current.json`;
3. inspect the current PRIMARY task;
4. open only evidence/source pointers relevant to that task;
5. continue from `next_action`;
6. never restart completed research merely because the old chat disappeared.

## Compact state fields

Keep at least:
- current task ID/title;
- question;
- current hypothesis;
- evidence pointers;
- findings so far;
- unresolved questions;
- rejected branches;
- blockers;
- next action;
- last meaningful checkpoint;
- status.

## Structured handoff

When rotating chats, write a compact handoff with:
- intent;
- current state;
- constraints;
- evidence pointers;
- next action;
- completion/terminal status if applicable.

Do **not** paste the full research transcript into a successor chat.

## Evidence compression

Large source collections stay external. Finished packets should preserve:
- conclusion;
- uncertainty;
- source map;
- constraints;
- failure modes;
- smallest BannerlordAI test.

## Concurrency

Do not create duplicate Liaison workers on the same PRIMARY research task.

Multiple side investigations are allowed only when they do not duplicate ownership and their outputs are separately named.

## Promotion

A Liaison finding is a research candidate, not accepted BannerlordAI architecture.

Front Office routes it to Analyzer/Coder. Local runtime evidence remains authoritative.
