# T9 Sign in · status

What works today and what doesn't. No dated notes here: those go in the task's file ([`docs/tasks/`](../../tasks/README.md)).

Spec: [`spec.md`](spec.md)

## Built

- **Sign-in:** phone + PIN, lockout after 5 wrong PINs, 30-day session (T-47).
- Several teachers can share one class (D-79).
- The header on sign-in has the name, tagline and language only; Log out is in the profile menu on the other screens (T-109).

## Known gaps and drafts

- All logins on a server share one PIN until it is changed after judging (see [`../../status.md`](../../status.md)).

## Open tasks

Task files that name this screen:

```bash
grep -l "^| Screens | .*T9-sign-in" docs/tasks/[TM]-*.md
```
