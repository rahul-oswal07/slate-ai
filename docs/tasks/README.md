# Tasks

One file per task that is not finished: `T-<number>-<slug>.md`. The file holds what the task is, its acceptance criteria, how it's tested, and the dated notes written while building it.

Tasks finished before 15 Nov 2026 are in [`../archive/tasks-until-2026-11-15.md`](../archive/tasks-until-2026-11-15.md), as they were written.

## Finding tasks

```bash
ls docs/tasks/                                   # every live task, by ID
grep -l "^| Status | Open" docs/tasks/[TM]-*.md       # by status
grep -l "^| Screens | .*T1-today" docs/tasks/[TM]-*.md  # by screen or area
```

## Adding a task

1. Take the next free number: the highest `T-` number in this folder plus one. Check that no GitHub issue has it in its title.
2. Open one GitHub issue titled `T-<number>: <task>`.
3. Copy the template below into `T-<number>-<short-slug>.md`. In **Screens**, use the folder names from [`../README.md`](../README.md).

`./scripts/check-docs.sh` fails when two files here share a number, so a number taken twice on two branches is caught before the second one merges.

## Working on a task

- Dated notes go under **Notes**, newest first, one line each: what was built, what was seen live, what was left out. This is where the old "Recent changes" in `status.md` went.
- When what a screen does changes, update that screen's `status.md` in the same PR.
- In the PR that finishes the task, set **Status** to `Done` and add the PR number, so the file is right the moment it merges and no follow-up PR is needed. After the merge, close the GitHub issue with the PR in a comment. The file stays here so links keep working; done files are moved to the archive in one go after a milestone.

## Status values

`Proposed` (not approved yet) · `Open` · `In progress` · `Built in part` · `Built, not merged` · `Done` · `Dropped`

## Template

```markdown
# T-111 · <the task in a few words>

| | |
|---|---|
| Status | Proposed |
| Issue | #<number> |
| Owner | R / M |
| Screens | <folders from docs/README.md, e.g. T1-today, T3-approve> |
| Source | <who asked, when; decision or requirement IDs; size> |

## What

<What changes for the teacher or the family, and why.>

## Acceptance criteria

- <something that can be checked>

## How it's tested

<Tests by name or kind; screenshots; a local run.>

## Notes

- <date>: <what happened>
```
