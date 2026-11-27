# T-184 · One more practice after Done

| | |
|---|---|
| Status | Proposed |
| Issue | #362 |
| Owner | R |
| Screens | P1-P4-catch-up, P5-todays-message |
| Source | Rahul, 27 Nov 2026 (parent screens list); D-108 (two submits, then locked); needs a decision; M |

## What

A child who enjoyed it has nothing more to do. After Done, offer **One more**: one or two extra questions from the same lesson, made and scored by code (sticks, `>,<,=` or a sum), with the right answer shown after the try.

These are practice: they don't count on the ledger and the teacher doesn't see them. Write a decision for this, since every other answer counts and never shows right or wrong.

## Acceptance criteria

- Questions and scoring are in code, never AI; numbers are inside the lesson's range.
- Nothing from practice reaches the ledger or Today.
- Content is built from the approved lesson only; nothing new needs her approval (confirm in the decision).
- All new strings in en, kn, te, ta, hi.

## How it's tested

- Pure tests for the question maker; Vitest for the step.

## Notes

- 27 Nov 2026: proposed.
