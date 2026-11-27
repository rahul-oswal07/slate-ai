# T-170 · Past weeks on Missed, Back today and Week for every class

| | |
|---|---|
| Status | Proposed |
| Issue | #348 |
| Owner | R |
| Screens | T4-missed-lessons, T5-back-today, T6-week |
| Source | Rahul, 27 Nov 2026 (teacher screens list); T-134, D-120 (past days, demo class only behind an admin switch); S–M |

## What

Only the demo class can step back a week, and only with the admin switch on. A real teacher can't look at last week's Missed, Back today or Week. Give every class the `◀ Week of … ▶` arrows on those three screens, **read-only**: looking back never writes.

Today's step back a day stays demo-only, because it writes that day.

## Acceptance criteria

- Any class can step back to the first week with a register, and not past the current week.
- In a past week nothing can be written (buddy ticks, pacing answer); the API refuses too.
- The demo's own past-day switch behaves as today.
- All new strings in en, kn, te, ta, hi.

## How it's tested

- API tests for a past week, read-only; Vitest for the arrows on each screen.

## Notes

- 27 Nov 2026: proposed.
