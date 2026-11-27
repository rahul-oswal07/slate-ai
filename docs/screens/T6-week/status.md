# T6 Week · status

What works today and what doesn't. No dated notes here: those go in the task's file ([`docs/tasks/`](../../tasks/README.md)).

Spec: [`spec.md`](spec.md)

## Built

- **Week:** return speed this week and last, reasons for absence, the pacing nudge, the weekly star (T-46, T-63).
- Week opens with one headline, "6 away · 4 got it · 2 need help", with the level icons and colours: children away this week and how many of them got their catch-up or need help, counted in the web app from the ledger (T-99, D-130). No headline when the ledger can't load.
- The return-speed chart hides a length of absence nobody had this week or last; the reasons card hides a reason nobody gave (T-99).
- The stars card is one line, "N earned a star" (the current week: "N on track so far"), with See all opening, in roll order, the children present on every school day of the week shown, the hint and the rule. N is the class count; nothing per child and no totals over weeks (D-118, T-125, T-99, D-130).
- In the demo class, arrows around Week's date range step a week back or forward, and tapping the range opens the calendar (T-134, D-120).

## Known gaps and drafts

- FR-24's acceptance in `spec.md` still says "with no numbers"; the star count (D-130) needs Monika's wording.
- The design card is not updated for T-99's headline, hidden zero rows or collapsed stars (Monika's file).
- The kn, te, ta and hi strings of the headline and the star count are drafts for Monika (T-99).
- Not yet measured at 360 px against "about two screens" (T-99).

## Open tasks

Task files that name this screen:

```bash
grep -l "^| Screens | .*T6-week" docs/tasks/[TM]-*.md
```
