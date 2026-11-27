# T-183 · The child's recent lessons on one page

| | |
|---|---|
| Status | Proposed |
| Issue | #361 |
| Owner | R |
| Screens | P0-evening-message, P1-P4-catch-up, P5-todays-message |
| Source | Rahul, 27 Nov 2026 (parent screens list); M |

## What

Each message has its own link, so a family who lost one can't find it. From any of their links, add **Recent lessons**: the last 14 days of evening messages, today's messages, catch-ups and stars for this child, newest first, each with done or not done. Tapping one opens it.

## Acceptance criteria

- Shows only this child's messages that are still valid (not expired, family not stopped).
- Only approved content, as on the pages themselves (guardrail 3).
- A link can't be used to reach another child (the token decides the child).
- All new strings in en, kn, te, ta, hi.

## How it's tested

- API tests: only this child, only valid links; Vitest for the list.

## Notes

- 27 Nov 2026: proposed.
