# App shell and look · status

What works today and what doesn't. No dated notes here: those go in the task's file ([`docs/tasks/`](../../tasks/README.md)).

Spec: [`spec.md`](spec.md)

## Built

- **Teacher app** (React PWA, Kannada / English / Hindi / Telugu / Tamil).
- Slate & chalk look, AAA tokens, light and dark (D-77).
- Learner levels have their own tokens, `level-got-it`, `level-needs-help` and `level-not-answered`, each a colour and a fill, AAA on the fill and on canvas, surface and panel in both themes (T-97, D-95, D-108); `tokens.test.ts` checks them.
- Language is one dropdown in the header on every screen and on `/demo` (T-107). Beside the profile button it shows a short label ("ಕ", "En"); sign-in and `/demo` show the full name (T-109, D-101).
- The header stays at the top while she scrolls, on teacher screens only (family pages don't use it); focused headings and the skip link land below it (T-114).
- The tagline is "The missed lesson" (no "at home"); Kannada shows none, since the name already says it (T-114).
- The header is one row: the name centred with language and a profile button, the tagline under the name. The profile menu is Profile · Settings · Log out. Sign-in has no profile button; before set-up the menu has Log out only (T-109, D-101).
- Bottom nav: five tabs, Today · Approve · Missed · Back today · Week (D-117, T-129). Today stays current on Lesson confirm, Settings and Profile.
- Approve and Back today show a neutral count badge (waiting catch-ups; today's buddy cards, from the saved cards when offline); screen readers hear it in the tab name (T-129).
- `TeacherLayout` follows the demo's school-day switch and resets cached data when the day changes. The day controls (`SchoolDateNav`, `SchoolWeekNav`, `BackToToday`) sit at the date on Today, Missed, Back today and Week. `api()` sends the chosen day as `X-School-Date`, and the outbox keeps it per write (T-134, D-120).
- The header says which class the app is for, under the name on every teacher screen: "3A · Grade 3 · Kannada medium · Shobha's class". It names the class's main teacher, so co-teachers see it too. It isn't shown on sign-in (T-135).
- The class is the one she picked on Today (T-115): every screen reads it through `useClassId()` / `useCurrentClass()` (`features/auth/useCurrentClass.ts`), never `me.classId`. The demo flags and the past-day switch go with the demo class, so `X-School-Date` is sent only while it is open.

## Known gaps and drafts

- Tamil layout at 360 px (T-84) is fixed (T-101, #211); the Tamil tab label is "திரும்பி வந்தோர்" (native review, T-26, found nothing to change).
- Offline covers attendance and voice notes only (roadmap, D-70).
- The short language labels and the kn, hi, te and ta strings of the profile menu are drafts for Monika (T-109).
- `docs/design/tokens.md` does not list the level tokens yet (Monika's file, T-97).

## Open tasks

Task files that name this area:

```bash
grep -l "^| Screens | .*app-shell" docs/tasks/[TM]-*.md
```
