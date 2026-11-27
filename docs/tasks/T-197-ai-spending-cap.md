# T-197 · A daily AI spending cap per server

| | |
|---|---|
| Status | Proposed |
| Issue | #375 |
| Owner | R |
| Screens | ai-pipeline, admin |
| Source | Rahul, 27 Nov 2026 (AI list); works with T-157 (costs) and T-162 (jobs); needs a cap from Rahul; M |

## What

Nothing stops a bug, or a big textbook import, from spending without limit. Add a daily budget per server from config, counted from `AiCalls` (tokens × config prices, T-157). Near the cap, admin pages warn. Over it:

- the teacher's steps fall back to rules or wait, and she's told plainly;
- the textbook import pauses and can carry on the next day;
- nothing unapproved is sent.

## Acceptance criteria

- Cap and prices from config; none in code.
- The cap check never stops the plain evening message.
- The jobs and AI call pages show "over budget" and since when.

## How it's tested

- Pure tests for the budget count; unit tests for each step over the cap.

## Notes

- 27 Nov 2026: proposed.
