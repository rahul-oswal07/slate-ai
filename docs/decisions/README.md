# Decisions

Product and technical decisions, each with an ID (`D-xx`) that code comments, tasks and specs cite.

| Where | What |
|---|---|
| [`D-01-to-D-99.md`](D-01-to-D-99.md) | Every decision up to 14 Nov 2026, as one frozen table. No new rows |
| `D-<number>-<slug>.md` | One file per decision from D-100 onwards |

## Adding a decision

1. Take the next free number: the highest `D-` number in this folder plus one (the frozen log ends at D-99).
2. Copy the template below into `D-<number>-<short-slug>.md`.
3. Cite the ID from the task file and from the spec of each screen it changes.

`./scripts/check-docs.sh` fails when two files here share a number, so a number taken twice on two branches is caught before the second one merges.

## Open gates

A decision that still needs a call has `Status: Proposed` and names who decides in **Owner**. Don't build past one (see `CLAUDE.md`, Collaboration).

```bash
grep -l "^| Status | Proposed" docs/decisions/D-*.md      # open decisions from D-100 on
```

In the frozen log only **D-98** is still Proposed.

## Template

```markdown
# D-100 · <the decision in a few words>

| | |
|---|---|
| Status | Proposed |
| Owner | <who decides>, <date> |
| Gate | <task it blocks, e.g. T-112, or none> |
| Screens | <screen or area folders it changes, e.g. T1-today, delivery> |

## Decision

<What is being decided, and why it came up.>

## Recommendation

<What we do. Keep numbers, names of settings and file names exact.>

## Notes

- <date>: <accepted by whom, what changed since>
```

Status is one of **Proposed**, **Accepted**, **Changed** (say what replaced it).
