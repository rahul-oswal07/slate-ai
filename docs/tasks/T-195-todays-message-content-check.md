# T-195 · A content check for today's message

| | |
|---|---|
| Status | Proposed |
| Issue | #373 |
| Owner | R |
| Screens | ai-pipeline, P5-todays-message, T3-approve |
| Source | Rahul, 27 Nov 2026 (AI list); A4 runs only on catch-ups; needs a decision and an eval; M |

## What

Catch-ups get A4's content check (wrong topic, shaming, too hard, summary that doesn't match) as well as the code checks. Today's message (A9) gets code checks only, though it goes to every absent family every day. Run a content check on A9's output too: A4 itself or a variant prompt for today's message. Retry once, and if it still fails, show Approve's needs-review reasons, as for catch-ups.

## Acceptance criteria

- A decision says which checks run, on which languages, and what happens on a fail.
- A failed check never blocks the plain evening message; it only marks today's message for review.
- The prompt is logged to `AiCalls`, with model from config.
- An eval with good outputs and crafted flaws, at the A4 bar.

## How it's tested

- Unit tests with a fake `ILlmClient` for pass, fail, retry; one Eval run.

## Notes

- 27 Nov 2026: proposed.
