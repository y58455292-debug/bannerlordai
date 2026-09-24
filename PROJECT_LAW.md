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
