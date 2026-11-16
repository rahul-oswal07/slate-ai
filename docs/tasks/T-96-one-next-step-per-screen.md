# T-96 · One next step per screen

| | |
|---|---|
| Status | Done (#262) |
| Issue | #261 · Record card merged in #207 |
| Owner | R |
| Screens | T1-today, T3-approve, T9-sign-in, admin |
| Source | G3 · Finale Should · size S |

## What

**One next step per screen:** Today shows the next action as one button (Record today's lesson → Check today's message → See answers); after Approve the app returns to Today; placeholders in every input

## Acceptance criteria

Each screen has one primary button and it leads to the next step

## How it's tested

Vitest

## Notes

- 16 Nov: done: one next step on Today and placeholders in every input merged in #262 (#261 closed). On prod in `v1.6`.
- 15 Nov: moved here from `tasks.md` (T-110).
- 14 Nov: "after Approve the app returns to Today" was built by T-108 (#209).
- 14 Nov: Record card on Today follows Rahul's design (part of T-96, not the whole row): the mic hint is "Up to 30 seconds · in Kannada" (no "holding also works", all locales; holding still records); "Add a photo of the page" and "Pick from the list" sit side by side as outlined tiles (`.btn-tile`, icon above label) with the photo hint under both; "Pick from the list" stays under the photo once one is added. axe clean light and dark at 360 px; screenshots `T1-today-record-360-*.png`.
- 15 Nov 2026: the rest built, on `feat/T-96-next-step-button`. Rahul chose a next-step button on top over fixing the cards in place.
  - Today has one primary button under the day strip (`NextStepButton`): Record today's lesson (focuses the mic) → Confirm today's lesson (`/lesson`) → Check today's message (`/approve`) → See answers (focuses the answers heading).
  - The step is worked out by code in `today/nextStep.ts`. No button when there is nothing to do now: approved and waiting for 6 pm, no today's message, or a note waiting to upload.
  - **Check and approve** and the mic-blocked "Pick from the list" are now secondary. The mic is outlined, not the amber hero, once the lesson is confirmed, except after Re-record.
  - Placeholders were added on the five text fields that had none: Login phone and PIN, the `/admin/lesson-links` search, and today's message concept and activity steps. Checkboxes, radios, the file picker and the date field take none.
  - The kn, te, ta and hi strings are drafts for native review (T-26).
  - Tests: `nextStep.test.ts`, `NextStepButton.test.tsx` (exactly one primary on Today in each state), and placeholder assertions in the Login, lesson-links and today's message tests.
  - Screenshots `T1-today-next-{record,confirm,check,answers}-360-{light,dark}.png` come from a throwaway Playwright script with the API mocked; the script was not kept (no new e2e). In all 8, there's exactly one primary and axe finds no serious or critical violations. The 12 existing e2e specs pass.
  - Left: the PR; Monika's check of the copy; the T1 design card (Monika's file).
