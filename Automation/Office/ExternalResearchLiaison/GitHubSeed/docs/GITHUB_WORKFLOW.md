# GitHub Collaboration Workflow

GitHub is the durable collaboration surface between Front Office and External Research Liaison.

## Repository usage

Use:
- **Issues** — assignments, questions, acceptance criteria, status;
- **branches/PRs** — durable research notes, prototypes, schemas, benchmarks, scripts;
- **comments** — concise coordination;
- **labels** — research domain/status;
- **milestones/projects** — long-horizon grouping when useful.

## Branch convention

`research/<issue-number>-<short-topic>`

Examples:
- `research/12-memory-salience`
- `research/18-intent-hysteresis`
- `research/24-ai-lod`

## Commit convention

Use focused commits:
- `research: map memory decay prior art`
- `prototype: add salience simulation notebook`
- `docs: record conflicting group-identity models`

## Pull request acceptance checklist

A research PR should answer:
- What question did this investigate?
- What are the strongest sources?
- What mechanism appears reusable?
- What limitations/failure modes were found?
- What did we rule out?
- What BannerlordAI layer/interface would this affect?
- What is the expected runtime/performance cost?
- What is the smallest future test?
- Does it require new mutation authority? If yes, only propose it—do not grant it.

## Communication contract

Front Office may post:
- a research brief;
- a source request;
- a prototype task;
- a test-design request;
- a contradiction to investigate.

Agent 2 replies on the Issue with concise progress checkpoints and submits durable work through a PR or research packet.

## Security / integrity

Never commit:
- credentials/tokens;
- private user secrets;
- protected save files;
- large binary game assets;
- copyrighted game binaries;
- private raw chat exports unless explicitly authorized and scrubbed.

Use stable pointers or summaries instead.
