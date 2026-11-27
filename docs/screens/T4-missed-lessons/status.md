# T4 Missed lessons · status

What works today and what doesn't. No dated notes here: those go in the task's file ([`docs/tasks/`](../../tasks/README.md)).

Spec: [`spec.md`](spec.md)

## Built

- **Missed lessons:** by child or by lesson, child history, and deleting a child's data (T-13, T-19).
- It is the **Missed** tab, titled "Missed lessons" (D-117, T-129). By child has two filters: Absent this week, Everyone.
- The old link `/missed?filter=back` opens Back today (T-129).
- By child is a table: Child · Days away · Lessons missed · Level. The name links to the child view; the family channel is a small icon + word under the name; the row lists no lesson names (T-98).
- The Level column is the child's catch-up level for this week's missed lessons (Needs help first, then Not answered yet, then Got it); a child with no missed lesson this week shows today's picture question level (T-92); no level shows "—" (D-130).
- Levels are the same tag everywhere: icon + word in the level's own colour token, AAA in light and dark (T-97).
- The child view shows the away days on one line ("Away 2 days: Mon, Tue"), one chip per missed lesson with its level ("Missed lesson" before its catch-up goes), then the other events by day (T-98).
- Delete is under a "More" fold at the bottom of the child view and still asks first (T-98).
- In the demo class, `◀ Week of … ▶` under the Missed title steps a week back or forward, and tapping it opens the calendar. The ledger is then as of that day (T-134, D-120).

## Known gaps and drafts

- Recording consent per child is the T8 Families screen, which is roadmap (D-70).
- The design card is not updated for T-98's table, lesson chips and More fold (Monika's file).
- The kn, te, ta and hi strings of the table, "No level yet", "Away N days" and "More" are drafts for Monika (T-98).

## Open tasks

Task files that name this screen:

```bash
grep -l "^| Screens | .*T4-missed-lessons" docs/tasks/[TM]-*.md
```
