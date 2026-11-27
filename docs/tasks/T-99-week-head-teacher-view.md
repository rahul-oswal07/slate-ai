# T-99 · Week as the head teacher's view

| | |
|---|---|
| Status | Proposed |
| Issue | #318 |
| Owner | R |
| Screens | T6-week |
| Source | G2, G5 · Finale Should · size S |

## What

**Week as the head teacher's view:** one headline ("6 away · 4 got it · 2 need help") + the chart; zero rows hidden; stars collapsed to "N earned a star · See all"

## Acceptance criteria

Week fits in about two screens at 360 px

## How it's tested

Vitest; 360 px screenshots

## Notes

- 27 Nov: Week opens with one headline, "2 away · 0 got it · 1 needs help", with the level icons and colours (T-97), counted in the web app from the ledger (D-130); then the return-speed chart, whose own "10 of 12" is now title size, not display. Lengths of absence nobody had this week or last, and reasons nobody gave, are hidden. Stars are one line, "N earned a star" (current week: "N on track so far") with See all opening the names, the hint and the rule; D-130 point 6 records the class count against D-118's "no numbers". Not measured at 360 px here: by the card sizes Week is about two screens with a nudge. Vitest: `WeekScreen.test.tsx`, `StarsCard.test.tsx`. Left: 360 px screenshots and axe; Monika to update the T6 card and FR-24's wording; kn, te, ta and hi of the headline and star strings are drafts for her.
- 15 Nov: moved here from `tasks.md` (T-110).
