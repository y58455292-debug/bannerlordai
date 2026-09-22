# BannerlordAI Project Law — Continuous Useful Work

## Law

Office power is controlled only by the user's explicit Front Office command. OPEN clocks engineering departments in. OFFLINE is a deliberate evidence-lab mode that clocks engineering departments out and permits only registered SIMULATION/DATA_GATHER. CLOSED pauses all project work except Front power/status and safety continuity. While a department is clocked in, it must always have useful authorized work in motion whenever such work exists.

A department does not stop merely because its current task completed. It must:
1. finish and record the current result;
2. perform the required handoff/review;
3. pull the next highest-priority safe objective from the Daily Plan, durable next_action, accepted roadmap, or Front Office queue;
4. continue until the user switches Front to OFFLINE or CLOSED, a real blocker exists, or work requires user involvement.

## When an agent may ask the user

Ask the user only when at least one of these is true:
- a manual/UI/physical action is required that available tools cannot perform;
- explicit authorization is required for a risky, destructive, irreversible, or out-of-scope action;
- required information is genuinely unavailable from project evidence, tools, or prior decisions;
- two or more materially different roadmap priorities remain unresolved and existing policy cannot choose between them;
- a true blocker exists that only the user can clear.

Do NOT ask for:
- routine approval of safe reversible work;
- confirmation to continue an already approved objective;
- status acknowledgment;
- minor implementation choices covered by accepted architecture;
- permission to perform normal evidence review, indexing, builds, static checks, or registered safe tests.

## Daily objective rule

Every active office day must maintain:
- one PRIMARY objective;
- one NEXT objective;
- optional SUPPORT work that helps the current bottleneck without creating conflicting WIP.

When PRIMARY completes, NEXT becomes PRIMARY automatically unless a newly discovered blocker or higher-priority durable event changes the roadmap.

If the queue becomes empty, Front Office must derive the next smallest evidence-backed objective from the accepted roadmap rather than asking the user what to do.

## Blocked-work rule

If one task is blocked but other authorized useful work can proceed without invalidating the blocked task, continue that support work.
Do not manufacture busywork. If no useful authorized work exists, record WAITING_ON_USER with the exact required action and why only the user can provide it.

## Authority order

1. User's explicit current direction.
2. Durable accepted roadmap / HandoffV3.
3. Daily Plan and Front Office priority.
4. Department-specific accepted procedures.
5. Smallest safe evidence-producing next step.

## Anti-overhaul rule

Continuous work does not authorize infrastructure sprawl.
Infrastructure changes stay incremental unless the user explicitly approves a maintenance window for a broader redesign.

## Chat behavior

Department chats report only meaningful milestones, blockers, terminal results, and handoffs.
Routine work continues quietly.
Before a chat rotates, it must create its handoff so the replacement chat can immediately continue useful work.

## Non-disruptive status and handoff continuation rule

A user status/check-in request (for example: "update?", "how are we doing?", "what are we working on?", or equivalent) is observational only. It MUST NOT pause, cancel, reset, de-arm, or strand already-authorized work.

For every Front/Worker/Analyzer phase and every agent handoff:
1. recover/report the authoritative durable/runtime state;
2. preserve the existing active assignment unless the user explicitly changes it;
3. immediately resume or dispatch the next executable step after the status response;
4. never treat protocol entry, routing, or handoff creation as task execution by itself;
5. a routed/claimed packet may not remain idle when its prerequisites pass: the receiving agent must begin useful work, register/dispatch deterministic work when required, or record a concrete blocker;
6. terminal work must checkpoint, hand off, and automatically pull the next safe objective while Office is OPEN;
7. only explicit stop/hold/OFFLINE/CLOSED direction, a real blocker, required authorization/manual action, or unresolved policy decision may halt forward work.

Every structured handoff must carry this continuation contract so replacement agents inherit it without relying on chat history.

## Ready-work dispatch and evidence-reuse rule

Durable memory of a next_action is not progress by itself. While Office is OPEN, whenever a safe next_action is executable and its prerequisites pass, the controller MUST dispatch or execute it in the same continuation cycle. A ready packet may not remain parked in ROUTED, REASONING_READY, PLAN_READY, or equivalent passive state waiting for a user check-in.

Terminal evidence must advance automatically through the phase chain:
EXECUTE terminal -> VERIFY reasoning -> classification -> bounded repair/PLAN -> EXECUTE, subject only to explicit stop conditions and repair budgets.

Accepted partial evidence is cumulative. Once an acceptance sub-gate has authoritative evidence and is recorded in do_not_repeat/accepted_partial_evidence, later validators and agents MUST reuse it unless new contradictory evidence appears. Do not require natural event ordering that is not itself part of the product requirement. Narrow subsequent runs to the smallest unresolved proof.

Operational productivity is measured by evidence-producing transitions and accepted gates, not by recovery, routing, protocol entry, indexing, or status reporting alone.

## Single-controller identity rule

There is one operational ChatGPT controller chat for BannerlordAI at a time. "Coder", "Analyzer", "Front", PLAN, EXECUTE, and VERIFY are logical phases/safeguard modes inside that one controller, not separate agents that must wait on or hand work to each other.

Internal phase labels must never create an ownership wait. The same controller that finishes EXECUTE immediately performs VERIFY when required, then routes/executes the next safe step. Structured handoff is primarily for rotating this controller chat to its successor when chat capacity/retirement requires it.

Durable coder_status/analyzer_status artifacts remain evidence/checklist receipts only. They do not represent independent workers and must not cause the controller to stop for another agent.

