# T11 Profile · status

What works today and what doesn't. No dated notes here: those go in the task's file ([`docs/tasks/`](../../tasks/README.md)).

Spec: [`spec.md`](spec.md)

## Built

- Read-only Profile at `/profile`: name, phone with the last 4 digits only, class as grade and medium (T-109).
- Reached from the profile menu in the header on every teacher screen; links back to Today, and Today stays the active tab (T-109).
- The data comes with `GET /api/me` (`phoneLast4`, `grade`, `medium`), so the screen also shows offline (T-109).
- The class row reads like the header's class line (`classLine`): grade, medium and, when the server sends it, the main teacher's class (T-135).

## Known gaps and drafts

- No design card.
- Nothing can be edited here; changing a name, phone or PIN is not in the app.

## Open tasks

Task files that name this screen:

```bash
grep -l "^| Screens | .*T11-profile" docs/tasks/[TM]-*.md
```
