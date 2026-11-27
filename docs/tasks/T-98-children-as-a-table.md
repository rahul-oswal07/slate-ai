# T-98 · Children as a table; child view tidy

| | |
|---|---|
| Status | Proposed |
| Issue | #317 |
| Owner | R |
| Screens | T4-missed-lessons |
| Source | G1, G2 · Finale Should · size S |

## What

**Children as a table; child view tidy:** rows name · days away · lessons missed · level; the child view groups absences, one chip per lesson, Delete under "More"

## Acceptance criteria

No lesson list in the row; delete still needs confirm

## How it's tested

Vitest; 360 px screenshots

## Notes

- 27 Nov: By child is a table, Child · Days away · Lessons missed · Level; the name links to the child, the family channel is a small icon + word under it, no lesson names in the row, "—" for no level (D-130). The child view shows the away days as one line ("Away 2 days: Mon, Tue") with one chip per lesson (its level, or "Missed lesson" before the catch-up goes), then the other events by day; Delete is under a "More" fold and still asks first. Vitest: `MissedScreen.test.tsx`, `ChildScreen.test.tsx`. Left: 360 px screenshots and axe (not run here); Monika to update the T4 card; kn, te, ta and hi of the new strings (table caption and header, "No level yet", "Away N days", "More") are drafts for her.
- 15 Nov: moved here from `tasks.md` (T-110).
