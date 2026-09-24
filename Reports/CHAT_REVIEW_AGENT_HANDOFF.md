# Chat Review Agent Handoff

## Purpose

This is a lightweight operating handoff for future ChatGPT review/conversation agents supporting the BannerlordAI project while a separate coder agent is doing implementation and runtime work.

It is **not** coder instructions, controller state, or an authoritative replacement for the repository.

Authority remains:

1. the user's current direction;
2. current `main`;
3. `README.md`;
4. `PROJECT_LAW.md`;
5. current source and committed evidence under `Reports/`.

Always re-read live GitHub state before making project claims. If this note conflicts with current repository evidence, the current repository wins.

## Default role boundary

While the coder is working, operate **GitHub-first and read-only by default**.

Do:

- inspect commits, diffs, branches, source, tests, reports, and evidence;
- compare implementation and claims against `PROJECT_LAW.md`;
- distinguish proven behavior from implemented-only or unproven behavior;
- identify missing evidence and the smallest safe next milestone;
- prepare coder prompts when the user asks;
- summarize project state and roadmap for the user;
- review new coder commits after they land.

Do **not** unless the user explicitly asks:

- take control of the user's computer;
- launch or interact with Bannerlord;
- operate the runtime/dev harness;
- deploy or replace DLLs;
- edit saves;
- interfere with the coder's active runtime session;
- create behavior-changing code;
- make repository changes.

The intended division is:

- **Coder agent:** implementation, builds, runtime testing, deployment, evidence generation, commits.
- **Chat review agent:** GitHub inspection, evidence review, roadmap, architecture discussion, and coder-task preparation.

## Current known project frontier

At the time this note was created, the reviewed repository state was:

- active version: `v0.21M-player-visibility-v1`;
- Prisoner/Mercy release threshold 20: treated as closed unless contradictory repo evidence appears;
- natural Mercy boundary evidence exists for 21.7 RELEASE and 19.8 KEEP;
- threshold-20 source/test drift protection exists;
- Clan Loyalty Phase 1 is proven for the voluntary **leave-current-kingdom** path;
- the separate **target-kingdom switch/defection** boundary remains unproven;
- war-strain player visibility is runtime-proven;
- loyalty-loss / loyalty-shift / leave-commit / defection-commit visibility call sites are implemented, with fresh natural runtime observation still incomplete at this snapshot.

Do not rely on these bullets if newer commits have superseded them.

## User design direction to preserve

The user wants kingdom politics to eventually support more than passive clan switching.

Desired high-level political behaviors include:

- clans staying loyal;
- clans leaving a kingdom and becoming independent;
- clans joining another kingdom;
- rulers/kings actively recruiting desirable clans;
- independent clans being recruitable;
- rulers potentially attempting to recruit clans from rival kingdoms.

This is a **design direction**, not a claim that these systems are already implemented or proven.

A future implementation should continue respecting the project's native-authority rule: ClanAI may influence bounded decision surfaces, but Bannerlord should retain native eligibility, legality, target/action authority, and final faction-change execution unless evidence justifies a different integration.

## Review discipline

For every new coder result:

1. inspect the actual commit/diff;
2. inspect matching evidence;
3. check whether the action merely changed a score or actually committed in-world;
4. verify whether the triggering event was natural or synthetic;
5. check whether README claims match the evidence;
6. preserve null results as meaningful evidence;
7. do not recommend arbitrary cap/threshold increases merely to force a positive result.

When asked "what next?", answer from the current repository, not old chat history.

## Repository hygiene

Do not revive the old controller/front-office/handoff machinery that was intentionally removed.

This single file is only a compact continuity note for future chat review agents. Keep it small and update or replace it only when the user explicitly asks.
