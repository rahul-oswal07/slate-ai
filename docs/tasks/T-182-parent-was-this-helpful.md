# T-182 · "Was this helpful?" after Done

| | |
|---|---|
| Status | Proposed |
| Issue | #360 |
| Owner | R |
| Screens | P1-P4-catch-up, P5-todays-message, T6-week |
| Source | Rahul, 27 Nov 2026 (parent screens list); S |

## What

Nobody knows which catch-ups and today's messages families find useful. On Done, ask **Was this helpful?** with a yes or no. Counts per lesson show on Week for the teacher and on the admin side for the team. Counts only, never per family.

## Acceptance criteria

- One answer per link; a later tap replaces it; idempotent.
- Counts are computed in code; no family is named next to an answer.
- Optional: skipping it changes nothing.
- All new strings in en, kn, te, ta, hi.

## How it's tested

- API tests; pure test for the counts; Vitest for the buttons.

## Notes

- 27 Nov 2026: proposed.
