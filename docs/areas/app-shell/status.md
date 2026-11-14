# App shell and look · status

What works today and what doesn't. No dated notes here: those go in the task's file ([`docs/tasks/`](../../tasks/README.md)).

Spec: [`spec.md`](spec.md)

## Built

- **Teacher app** (React PWA, Kannada / English / Hindi / Telugu / Tamil).
- Slate & chalk look, AAA tokens, light and dark (D-77).
- Language is one dropdown in the header on every screen and on `/demo` (T-107).
- Bottom nav: Today · Approve · Missed · Back today · Week.

## Known gaps and drafts

- Tamil layout at 360 px (T-84) is fixed (T-101, #211); the Tamil tab label "திரும்பி வந்தோர்" is a draft for native review (T-26).
- Offline covers attendance and voice notes only (roadmap, D-70).
- The header takes two rows and Log out sits in it (T-109, T-95 propose changes).

## Open tasks

Task files that name this area:

```bash
grep -l "^| Screens | .*app-shell" docs/tasks/[TM]-*.md
```
