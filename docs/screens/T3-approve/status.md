# T3 Check and approve · status

What works today and what doesn't. No dated notes here: those go in the task's file ([`docs/tasks/`](../../tasks/README.md)).

Spec: [`spec.md`](spec.md)

## Built

- **Check and approve:** two tabs.
- **Today's message** has sections she can edit, remove or add a note to; her Kannada is kept word for word and the other languages are written from it, and the tick is bound to the version (T-89).
- **Draft of today's message** (finale, D-90–D-96): confirming a lesson with the book's exercises (today only Nalikali p 4) drafts a message for absent families: concept with voice, a parent-led activity and a `>,<,=` picture pair chosen by code (T-88).
- After a successful Approve she is back on Today (T-108).
- **Catch-ups** are the week's catch-ups, one per lesson group and language. Each has a segment player, a Kannada summary, **Why this**, the picture question, Simpler / Shorter, and a tick before Approve, bound to the version she read (T-14).
- A child whose missed lessons have no practice rule gets no catch-up instead of a failed one (part of T-100, #212).

## Known gaps and drafts

- Found, not fixed: after approving, changing the lesson to one without the book's exercises leaves the old approved message to go at 6 pm (not reachable in the demo, where Approve sends at once); needs its own issue.
- Catch-ups already made before the fix (staging's included) stay failed until the next week.
- The design card is not updated for T-89 and T-108 (Monika's file).

## Open tasks

Task files that name this screen:

```bash
grep -l "^| Screens | .*T3-approve" docs/tasks/[TM]-*.md
```
