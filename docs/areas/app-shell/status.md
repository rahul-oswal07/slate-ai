# App shell and look · status

What works today and what doesn't. No dated notes here: those go in the task's file ([`docs/tasks/`](../../tasks/README.md)).

Spec: [`spec.md`](spec.md)

## Built

- **Teacher app** (React PWA, Kannada / English / Hindi / Telugu / Tamil).
- Slate & chalk look, AAA tokens, light and dark (D-77).
- Language is one dropdown in the header on every screen and on `/demo` (T-107). Beside the profile button it shows a short label ("ಕ", "En"); sign-in and `/demo` show the full name (T-109, D-101).
- The header is one row: the name with the tagline under it, then language and a profile button whose menu is Profile · Settings · Log out. Sign-in has no profile button; before set-up the menu has Log out only (T-109, D-101).
- Bottom nav: Today · Approve · Missed · Back today · Week.

## Known gaps and drafts

- Tamil layout at 360 px (T-84) is fixed (T-101, #211); the Tamil tab label "திரும்பி வந்தோர்" is a draft for native review (T-26).
- Offline covers attendance and voice notes only (roadmap, D-70).
- The short language labels and the kn, hi, te and ta strings of the profile menu are drafts for Monika (T-109).

## Open tasks

Task files that name this area:

```bash
grep -l "^| Screens | .*app-shell" docs/tasks/[TM]-*.md
```
