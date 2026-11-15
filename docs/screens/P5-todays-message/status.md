# P5 Today's message at home · status

What works today and what doesn't. No dated notes here: those go in the task's file ([`docs/tasks/`](../../tasks/README.md)).

Spec: [`spec.md`](spec.md)

## Built

- With today's message the link opens step by step: her voice and text → "Is everything OK?" (the reason, once) → concept → activity → `>,<,=` in sticks, scored by code → Done (T-90, T-91).
- Code scores each tap, with one more try after a first that isn't right; the answer never reaches the phone (T-90).
- A stop link is on every step; removed sections are skipped (T-91).
- When the lesson has a link on the admin page, the steps include "Watch how it's taught" with that video (T-94, D-106); the teacher can remove it.

## Known gaps and drafts

- No design card.

## Open tasks

Task files that name this screen:

```bash
grep -l "^| Screens | .*P5-todays-message" docs/tasks/[TM]-*.md
```
