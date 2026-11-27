# P0 Evening message and reason · status

What works today and what doesn't. No dated notes here: those go in the task's file ([`docs/tasks/`](../../tasks/README.md)).

Spec: [`spec.md`](spec.md)

## Built

- **Families** (`/p/<token>`, no sign-in): the evening message with a reason in 5 pictures (T-44, T-45).
- The evening wording is v4 (D-99): code names the book inside the page slot, and Kannada says "{child} ಅವರ ಜಾಗ" (T-101).
- An approved lesson clip plays with the message, behind the spoken AI disclosure (T-62).
- The plain message has the title row ("Today's message", no Back) and the teacher line; its reason page has Back to the message (T-116).

## Known gaps and drafts

- The Telugu and Tamil evening text attach a suffix to a name written in Latin letters (`{child}ని`, `{child}இன்`); native review (T-26) asked for no change.
- She has to approve the v4 wording once on each server before any evening or today's message goes (see [`../../status.md`](../../status.md), Next).

## Open tasks

Task files that name this screen:

```bash
grep -l "^| Screens | .*P0-evening-message" docs/tasks/[TM]-*.md
```
