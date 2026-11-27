# T-187 · Fix: the catch-up's question shows the sum twice

| | |
|---|---|
| Status | Proposed |
| Issue | #365 |
| Owner | R |
| Screens | P1-P4-catch-up |
| Source | Rahul, 27 Nov 2026 (parent screens list); known gap in P1-P4 status; S |

## What

The catch-up's question shows the sum twice: once in A3's sentence and once as "103 + 463 = ?" under it. Show it once. Keep the line made by code and take the sum out of the sentence, or show the sentence without its sum.

## Acceptance criteria

- The sum shows once, and it's the one made by code (guardrail 4).
- If the prompt changes: bump its version and include an eval run (guardrail 6).

## How it's tested

- Failing test first (bug fix rule), then the fix; Vitest for the question step.

## Notes

- 27 Nov 2026: proposed.
