# BannerlordAI Handoff V3 — Crash-Safe Rolling Protocol

## Purpose
A chat may fill or terminate without warning. Therefore handoff state is NEVER created only at chat close.
The canonical active state lives on disk and is refreshed during work. Mem is a secondary mirror/search layer.

## Canonical files
- current.json — tiny atomic resume capsule; authoritative active state.
- previous.json — immediately previous valid capsule for rollback.
- events.jsonl — append-only decision/action ledger.
- last_resume_snapshot.json — reconstructed live state from disk/process/config evidence.
- Tools\Handoff\handoff_checkpoint.py — atomic checkpoint writer.
- Tools\Handoff\resume_state.py — recovery/preflight scanner.

## Mandatory boot sequence
1. Run resume_state.py before making project changes.
2. Read current.json and any drift warnings.
3. Read ARCHITECTURE_LEARNING.md before substantial engineering work.
4. If Bannerlord/TestRunner is active, inspect that run before starting another.
5. Open only evidence referenced by the capsule or newer disk artifacts.
6. Mem/HOT may add context but cannot override newer local evidence.

## Mandatory rolling checkpoints
Checkpoint BEFORE a live deployment/test and AFTER it exits.
Checkpoint after: accepted/rejected milestone, build/deploy, causal finding, rollback, blocker resolution, architecture change.
Checkpoint whenever the exact next action changes materially.
Do not wait for chat length warnings or for the user to ask for a handoff.
## Capsule size rule
current.json should remain one-screen/small: exact build hashes, accepted milestone, active candidate, active question, run state, decisive evidence, blockers, next action, and do-not-repeat items.
Deep history belongs in events.jsonl / reports / raw evidence, not the capsule.

## Crash recovery rule
If the capsule is stale or missing, resume_state.py reconstructs from:
- installed ClanAI hash
- running Bannerlord process
- TestRunner status
- newest DerivedReports
- newest LiveValidation directory
- newest PatchStaging candidate
- active config files
Then reconcile and write a new checkpoint BEFORE new work.

## Ownership
Local disk = canonical live state.
Derived reports/raw evidence = canonical proof.
Mem = searchable mirror and durable doctrine, not live-state authority.
Legacy hot_context.json = compatibility only; never trust it over HandoffV3 or newer evidence.

## Anti-duplication stop rules
Never rerun a passed milestone merely because Mem/HOT is stale.
Never launch a second live test while an inherited run may still be active.
Never infer installed build from candidate name; verify DLL hash.
Never infer completion from chat text; verify closeout/evidence files.

## Required next-agent behavior
The first action in a replacement chat is recovery, not research or coding.
A replacement agent must be able to continue correctly even if the previous chat disappeared one second after its last tool call.
Every agent must inherit and improve both the game architecture and the engineering/research method. Preserve reusable breakthroughs in ARCHITECTURE_LEARNING.md and mirror them to Mem when available.

## Evidence-before-architecture gate
When the user asks to tune workflow, rules, handoff, automation, memory, testing, or agent architecture:
1. Search reliable prior art first (official docs, primary papers, maintained open-source implementations).
2. Identify the exact mechanism and its failure model.
3. If no credible supporting architecture/mechanism exists, do NOT promote the idea into project doctrine; keep it as an unadopted proposal.
4. If credible proof exists, adapt the mechanism to BannerlordAI with explicit limitations and the smallest safe implementation.
5. Validate the adaptation before making it mandatory.
6. Preserve the source mapping and accepted/rejected decision in ARCHITECTURE_LEARNING.md and Mem.

Current durable-handoff prior art:
- Temporal durable execution: crash/interruption recovery.
- LangGraph checkpoints + cross-thread store: resumable agent state versus durable knowledge.
- Event Sourcing + snapshots: append-only history, replay, materialized current state.
- Kubernetes Leases: one active controller/owner at a time.
See: D:\BannerlordAIResearch\Longitudinal\DerivedReports\2026-09-18_DurableHandoff_PriorArt.txt

## Durable replay semantics
Handoff state now follows an event-sourced/write-ahead model:
- events.jsonl is the append-only transition journal for post-cutover handoff events.
- snapshots\*.json are immutable recovery checkpoints.
- current.json is a materialized fast-read view, not the sole source of truth.
- A checkpoint transition is journaled and fsync'd BEFORE current.json is replaced.
- If a crash happens after journal append but before materialization, resume_state.py reconstructs by replaying journal events from the newest snapshot.
- Each journal event carries sequence, previous-state hash, event-chain hash, patch/intent, and resulting-state hash.
- Stable action_id values provide idempotency for operations that must not be repeated accidentally.

## Operation continuity
For live deployment/test or other external side effects, record a stable operation_id and phase before execution. Preferred phases: PREPARED -> ACTIVE -> EVIDENCE_FROZEN -> COMPLETED (or ABORTED/ROLLED_BACK).
A replacement agent must inspect an incomplete operation and external runtime state before retrying it. Never start a new equivalent operation merely because the chat that began it disappeared.

## Single-owner lease rule
Live deployment/runtime control remains single-owner. Existing DEPLOYMENT_OWNER.json is the current guard. The next infrastructure iteration should evolve it toward lease semantics (holder identity, acquire/renew timestamps, duration, safe expiry) using Kubernetes leader-election principles; do not steal a live owner lock merely because another chat exists.

## Bottleneck escalation
If progress stalls or the same blocker appears twice, do not continue broad trial-and-error.
Use: blocked outcome -> smallest unresolved questions -> rank by information gain -> targeted reliable-source/prior-art search -> smallest discriminating test -> preserve ruled-out branches.
If reliable support cannot be found for a proposed mechanism, do not promote or force it. Mark it unsupported/unknown and select another evidence-backed path or stop.
This protocol is grounded in scientific troubleshooting and fault-identification/isolation practice.

## Reference-registry inheritance rule
REFERENCE_REGISTRY.md is required project context for complex engineering/research work.
A replacement agent must consult it when the task touches architecture, workflow, memory, cognition, autonomous control, observability, economy/world modeling, or a repeated blocker.

Reference handling:
1. Search the local BannerlordAI evidence/reference stack before designing a new subsystem.
2. Reuse or adapt proven implementations/mechanisms when they fit Bannerlord's constraints.
3. Search credible external prior art when local sources do not settle the mechanism.
4. Replace an existing/reference structure only with an explicit reason: better fit, stronger evidence, lower complexity, safer ownership, or measured performance/correctness gain.
5. Preserve source provenance, license constraints, limitations, and rejected branches.
6. New important GitHub repositories, videos, papers, mods, tools, or architecture sources must be added to REFERENCE_REGISTRY.md and summarized in ARCHITECTURE_LEARNING.md when they materially change project method/architecture.
7. Reference discovery is part of the handoff itself; do not depend on old chat history to remember source URLs or their purpose.

## Autonomous launch acceptance rule — 2026-09-19
Autonomous campaign tests MUST include launch/recovery as part of the pass criteria.
- Human relaunch/rescue invalidates the run as an autonomous test.
- Accepted lifecycle: supervisor launches Bannerlord -> fresh TestRunner status appears -> campaignReady=True -> operator acts/observes -> planned EXIT_NOSAVE or bounded recovery -> save hash verified unchanged.
- Stale status files must be cleared before launch and a fresh timestamp/schema must be required.
- Early process exit must be classified and recorded; do not silently substitute a human restart.
- One bounded automatic retry before campaign-ready is allowed when the failure is classified and no game process remains.
- Use D:\BannerlordAIResearch\Tools\Operator\launch_supervisor.py as the reference launcher.

### Machine-specific Bannerlord path rule
Do NOT normalize these names; both are correct in different places:
- Steam game install: C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord
- User saves/configs: C:\Users\csala\OneDrive\Documents\Mount and Blade II Bannerlord
The Steam path uses "&"; the Documents path uses the word "and".
This exact mismatch caused an autonomous-launch failure and was proven/fixed by comparing successful vs failed launch logs.

## Fast boot default — 2026-09-19
Replacement agents should start with:
`python -X utf8 D:\BannerlordAIResearch\Tools\Handoff\fast_resume.py`

Behavior:
- `FAST_OK`: trust the compact verified boot packet for volatile state; do not reread large handoff/index files unless the current task specifically needs them.
- `ESCALATE_DEEP`: run `resume_state.py`, reconcile evidence, then checkpoint before side effects.
- Keep boot/index output compact. State/hashes/ownership/warnings/next-action are hot-path data; explanations and historical context stay in reports and ledgers.
- The deep recovery path remains authoritative for detected drift, corruption, missing state, or ambiguous external side effects.

## User trigger phrase — "follow protocol"
If the user invokes Remote Desktop Commander and says **"follow protocol"**, treat that phrase as an instruction to immediately run:
`python -X utf8 D:\BannerlordAIResearch\Tools\Handoff\follow_protocol.py`

Semantics:
- FAST_OK -> inherit the compact verified state and continue from recorded next_action.
- Any mismatch -> automatically run deep recovery.
- Respect current deployment owner, active controller/watcher, no-save/hash gates, and do-not-repeat rules.
- Do not reread large project history unless the recovered state or current task specifically requires it.
- Keep the user-facing recovery reply concise unless a blocker/drift requires explanation.

## Chat-saturation / worker-update split — 2026-09-19
- Treat chat capacity as an expected failure mode, not an end-of-session event.
- The active worker chat owns engineering/tool execution only while it holds the live deployment lease.
- A lightweight update chat may read current.json / fast_boot.json and report progress, but must not start a competing controller, watcher, deployment, or equivalent test.
- The worker continuously checkpoints materially changed state to disk; it must never rely on the conversation transcript as the only record of unfinished work.
- If the worker chat fills/disappears, the replacement worker runs follow_protocol.py first, inherits the recorded next_action + operation phase, inspects any active external side effects, and continues without repeating accepted work.
- Keep current.json compact enough for instant recovery. Put implementation detail in evidence files, patch staging, events.jsonl, and ARCHITECTURE_LEARNING.md.
- Prefer one authoritative worker plus any number of read-only/update surfaces over multiple chats independently modifying the project.
- When context pressure becomes noticeable, checkpoint immediately before further risky/live work rather than waiting for a hard limit.

## Low-latency execution rule — 2026-09-19
- Default to FAST PATH: compact verified state -> registered deterministic next action -> evidence check -> checkpoint -> next registered action.
- Do not reread broad history, raw logs, references, or architecture notes when the hot packet already settles the next step.
- DEEP PATH is conditional only: integrity drift, failed registered action, repeated blocker, new architecture question, contradiction, or a result-changing improvement.
- Routine actions must use the explicit Autopilot action registry; never execute arbitrary next_action prose.
- Preserve quality gates even on the fast path: single-owner lease, installed hash, protected-save hashes, no-repeat ledger, causal mode, bounded attempts, exact identity/time gates, evidence freeze, cleanup, and accepted-build restoration.
- Optimize for near-equal outcome quality with less reasoning/context: carry forward proven invariants/checkers instead of re-deriving them every run.
- If a new diagnostic answers a question, remove it from the hot path and retain only the conclusion + evidence pointer.
- If the same failure repeats twice, stop retries and convert the failure into infrastructure or targeted deep investigation.
- Current local continuation: one-minute BannerlordAI Continuation Supervisor + registered action runner. Agent-level continuation handles only reasoning gaps/new actions.

## Mini-office role contract — 2026-09-19
BannerlordAI work is split into three primary lanes to reduce context switching and duplicated reasoning.

### 1. Coder / Operator
- Owns implementation, builds, test execution, runtime control, logs, failures, debugging traces, and raw technical explanations.
- Produces reproducible artifacts: patches, hashes, validator output, logs, failure packets, and exact commands.
- Does not spend hot-path time writing broad project summaries unless needed for debugging or handoff.
- Converts recurring failures into guards, adapters, diagnostics, caches, or automation.

### 2. Analyzer / Archivist
- Reviews coder/operator evidence after runs.
- Separates observation from inference and classifies PASS / FAIL / HARNESS_FALSE_NEGATIVE / UNKNOWN.
- Compresses raw evidence into durable findings without outranking the raw proof.
- Maintains architecture lessons, policies, workflow tips, efficiency cheatsheets, do-not-repeat rules, reference mappings, and next-test acceptance criteria.
- Looks for repeated friction and proposes workflow/process improvements backed by evidence/prior art.

### 3. Front / Coordinator
- User-facing command/update layer.
- Reads compact verified state plus analyzer conclusions, not all raw implementation detail by default.
- Communicates meaningful progress, tradeoffs, blockers, roadmap state, and accepts priority changes from the user.
- Routes new goals to the worker lane and preserves user intent.
- Does not duplicate coder debugging or analyzer archival work unless escalation requires it.

### Separation rule
- Coder generates evidence.
- Analyzer turns evidence into reusable knowledge/policy.
- Front coordinates and communicates.
- Supervisor handles deterministic scheduling/continuation.
- Raw proof remains authoritative; summaries/policies are derived layers.
- Any lane may escalate only when its own responsibility boundary is insufficient.

## User trigger phrase — "Front office"
If the user says **"Front office"**, **"front office update"**, or asks what the whole office/departments are doing:
1. Do not ask them to restate project history.
2. Run:
   `python -X utf8 D:\BannerlordAIResearch\Tools\Autopilot\front_office.py`
3. Treat its live runtime section as authoritative for what is happening right now.
4. Report the whole office compactly:
   - Coder / Operator: latest work/result.
   - Analyzer / Archivist: review/classification/knowledge work.
   - Supervisor: continuation state / pending registered action.
   - Front Office: accepted milestone, active goal, blocker, next roadmap step.
5. Read HOT/WARM/RAW only when the user's question requires deeper study.
6. If a department has stale or contradictory state, route that contradiction to the Analyzer rather than guessing.
7. Front Office never starts a competing live controller simply to answer an update question.

### Department state locations
- Directory/contract: D:\BannerlordAIResearch\Automation\Office\departments.json
- Coder desk: D:\BannerlordAIResearch\Automation\Office\coder_status.json
- Analyzer desk: D:\BannerlordAIResearch\Automation\Office\analyzer_status.json
- Front desk: D:\BannerlordAIResearch\Automation\Office\front_status.json
- Supervisor desk: D:\BannerlordAIResearch\Automation\Autopilot\state.json
- Fast information front page: D:\BannerlordAIResearch\Longitudinal\EvidenceIndex\ContextPyramid\front_page.json

## Mature Automation Office v1 — 2026-09-19

Research-backed operating model:
- FEMA ICS/NIMS: one accountable owner, unity/transfer of command, management by objectives, manageable span of control.
- Team Topologies: narrow cognitive-load boundaries; explicit service/collaboration/facilitation interactions.
- Kanban: WIP limits and pull by capacity.
- DORA: small independent testable batches; improve the active constraint/bottleneck.
- Google SRE: automate repetitive toil; structured incident response and blameless postmortem action items.
- High Reliability Organizations: sensitivity to operations, reluctance to simplify, preoccupation with failure, resilience, deference to expertise.
- Microsoft developer-productivity research: minimize interruptions/context switching and keep rationale/state externally recoverable.

Office roles:
- Front Office: user hub, daily plan, chat registry, roadmap priority, whole-office summaries.
- Coder/Operator: implementation/build/live test/debug/raw proof; one live WIP item.
- Analyzer/Archivist: read-only evidence review, semantic classification, policy/cheatsheet/index updates; one reasoning WIP item.
- Supervisor/Platform: deterministic clock/WIP/ownership/integrity/registered-action enforcement; no novel reasoning.

Chat retirement:
CLOCKED_IN -> HANDOFF_WRITTEN -> DOWNSTREAM_REVIEW_PENDING -> SAFE_TO_ARCHIVE -> SAFE_TO_DELETE -> RETIRED.
Permanent retirement requires local durable capture + downstream semantic review + Mem mirror + Front approval. Actual ChatGPT UI archive/delete is user-executed; the office produces the verified queue.

Information hierarchy:
FRONT -> HOT -> WARM Architecture/Evidence/Research -> INDEX -> RAW.
Raw proof always outranks summaries.

Office freeze:
The office may remain CLOSED through office_override.json. Dispatch policy must return OFFICE_CLOSED for live work while frozen. Reopening requires a clean launch checklist and explicit user approval.

Design report:
D:\BannerlordAIResearch\Longitudinal\DerivedReports\2026-09-19_BannerlordAI_Mature_Office_Operating_Model.txt
Launch checker:
D:\BannerlordAIResearch\Tools\Autopilot\office_launch_check.py
Archive brain:
D:\BannerlordAIResearch\Longitudinal\EvidenceIndex\ChatArchiveBrain

## Infrastructure evolution rule — 2026-09-19

Purpose: infrastructure must improve continuously without becoming a competing project.

Default:
- Improve infrastructure incrementally in small, independently testable changes.
- Prefer changes that remove a measured recurring bottleneck, repeated failure, recovery cost, or avoidable manual toil.
- Keep BannerlordAI behavior development as the primary value stream.
- One infrastructure change at a time; validate it, then return to feature/behavior work.
- Do not start a broad office/workflow/memory/handoff overhaul from a local inconvenience.

Mass-overhaul gate:
- A multi-system redesign, folder/chat reorganization, new department architecture, large automation rewrite, or other infrastructure program requires an explicitly approved maintenance window from the user.
- Approval must define why now is appropriate and what project work is intentionally paused.
- Without that approval, convert the problem into the smallest safe patch, defer it, or log it as infrastructure debt.

Escalation:
- If infrastructure work begins consuming materially more time than the BannerlordAI behavior task it was meant to unblock, stop and return to the main roadmap.
- Repeated blockers may justify infrastructure work, but scope must remain proportional to the measured bottleneck.

Evidence:
DORA recommends working in small batches for faster feedback and lower risk, and improving the most significant active constraint rather than expanding work indiscriminately.

## Project Law — Continuous Useful Work

While clocked in, every department must keep useful authorized work in motion whenever such work exists. Completion of one task triggers handoff plus automatic pull of the next highest-priority safe objective from the Daily Plan, durable next_action, accepted roadmap, or Front queue.

Ask the user only for a manual action unavailable to tools, explicit authorization for risky/destructive/out-of-scope work, genuinely unavailable required information, an unresolved roadmap choice not covered by policy, or a blocker only the user can clear.

Do not ask for routine continuation approval. If the active queue is empty, Front Office derives the next smallest evidence-backed objective from the accepted roadmap.

Canonical law file:
D:\BannerlordAIResearch\PROJECT_LAW.md
