# BannerlordAI Project Law

## 1. Source of truth

The user's current direction has highest authority. After that, use the current repository source, accepted evidence in `Reports/`, and the smallest safe evidence-producing next step.

Chat history, old controller state, archived handoffs, and uncommitted staging folders are not authoritative project state.

## 2. Artifact rule

No progress claim is accepted unless the corresponding working code, evidence, or documentation exists in the repository.

Null results are evidence and must be retained beside positive results when they constrain the design.

## 3. Commit rule

Commit working code at least daily. Prefer small focused commits with descriptive messages. Do not use snapshot mega-commits as the normal workflow.

Behavior-changing work should be committed separately from repository cleanup or documentation-only changes whenever practical.
## 4. Native-authority rule

ClanAI should influence Bannerlord through bounded, auditable decision surfaces rather than replacing native world authority.

For autonomous decisions, preserve native eligibility, target selection, action legality, and final Bannerlord actions unless a specific experiment proves a different integration is required.

Do not raise thresholds, caps, or inject synthetic events merely to manufacture a positive result. A real boundary crossing must be observed as a real boundary crossing.

## 5. Evidence rule

For every behavioral claim, record enough evidence to reconstruct:

- the native value or state before intervention;
- the bounded ClanAI contribution;
- the resulting native/adjusted decision state;
- whether an actual native action committed;
- whether the triggering world event was natural or forced.

A score change is not an action commit. A causal modifier is not a completed autonomous behavior.
## 6. Deployment safety

Do not replace the live mod DLL while Bannerlord is running.

Preserve a verified rollback before deploying a behavior-changing candidate. Use a no-save exit for validation restarts unless a specifically named test fixture is being created.

Do not overwrite protected baseline saves. Test fixtures must be clearly named and separately verifiable.

## 7. Repository hygiene

The active repository contains only material needed to build, test, observe, review, or reproduce current evidence.

Research spikes must be distilled into a short durable note or report before their working trees are deleted. Raw telemetry, caches, videos, temporary staging trees, controller state, handoff machinery, and local runtime artifacts do not belong in the active repository.

`README.md` must describe the current state honestly enough for a fresh reviewer to understand what is proven and what is not.

## 8. Release direction

The intended final ClanAI module is standalone. Development harnesses may be used for observation and validation, but release behavior must not depend on TestRunner, command buses, office/controller machinery, absolute project paths, or external research state.
## 9. Reviewability

Keep current behavioral source under `src/`, reproducible evidence under `Reports/`, and test entry points under `Tests/`.

When an experiment supersedes an earlier implementation, preserve the meaningful result in Reports and remove redundant active scaffolding.

A reviewer should be able to start at README, find the relevant source, locate the matching evidence, and see the current unresolved boundary without consulting chat transcripts.

## 10. Mission-boundary repository synchronization

Do not interrupt an active experiment or implementation merely to check for repository updates.

A mission checkpoint occurs when the current requested milestone has been completed, bounded by a committed null result, or blocked with committed evidence. Small follow-up fixes that are clearly part of the same milestone may continue without a roadmap resync.

At every mission checkpoint, before beginning the next major feature or milestone:

1. commit the completed work and matching evidence;
2. fetch/pull the current `main` branch from GitHub;
3. re-read `PROJECT_LAW.md`, `README.md`, and any current roadmap/vision files;
4. inspect commits that landed while the mission was active;
5. reconcile newer user direction or repository roadmap changes with local state;
6. only then begin the next major milestone.

GitHub is authoritative at mission boundaries. Do not rely on stale chat, controller, handoff, or local state when newer repository guidance exists.

Mission-boundary synchronization is **not** an authorization gate. If the current roadmap or repository guidance already identifies the next milestone and there is no explicit user hold, unresolved blocker, or conflicting newer direction, proceed directly to that next milestone after the sync. Do not wait for fresh user permission merely because a checkpoint was reached.
