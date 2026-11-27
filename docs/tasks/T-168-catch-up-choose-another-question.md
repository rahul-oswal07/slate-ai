# T-168 · "Choose another question" for a catch-up's picture question

| | |
|---|---|
| Status | Proposed |
| Issue | #346 |
| Owner | R |
| Screens | T3-approve, P1-P4-catch-up |
| Source | Rahul, 27 Nov 2026 (teacher screens list); D-129 (gap in T3 status); S |

## What

Today's message lets her pick another picture question; a catch-up doesn't (D-129). Add **Choose another question** to the catch-up's learning check. Code picks the next question from the same lesson's candidates (picture scoring stays in code), and the approval clears, as with any edit.

## Acceptance criteria

- The new question is made and scored by code, never by AI.
- Changing it bumps the version and clears the approval (guardrail 3).
- The ledger scores the family's answer against the question that was sent.
- All new strings in en, kn, te, ta, hi.

## How it's tested

- Pure tests for the next-question pick; API test that the approval clears; Vitest for the button.

## Notes

- 27 Nov 2026: proposed.
