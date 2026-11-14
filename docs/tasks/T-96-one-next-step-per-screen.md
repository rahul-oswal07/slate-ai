# T-96 · One next step per screen

| | |
|---|---|
| Status | Built in part |
| Issue | none · Record card merged in #207 |
| Owner | R |
| Screens | T1-today |
| Source | G3 · Finale Should · size S |

## What

**One next step per screen:** Today shows the next action as one button (Record today's lesson → Check today's message → See answers); after Approve the app returns to Today; placeholders in every input

## Acceptance criteria

Each screen has one primary button and it leads to the next step

## How it's tested

Vitest

## Notes

- 15 Nov: moved here from `tasks.md` (T-110).
- 14 Nov: "after Approve the app returns to Today" was built by T-108 (#209).
- 14 Nov: Record card on Today follows Rahul's design (part of T-96, not the whole row): the mic hint is "Up to 30 seconds · in Kannada" (no "holding also works", all locales; holding still records); "Add a photo of the page" and "Pick from the list" sit side by side as outlined tiles (`.btn-tile`, icon above label) with the photo hint under both; "Pick from the list" stays under the photo once one is added. axe clean light and dark at 360 px; screenshots `T1-today-record-360-*.png`.
