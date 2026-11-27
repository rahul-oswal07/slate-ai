# T-199 · Fix the prompt versions in the AI status table

| | |
|---|---|
| Status | Proposed |
| Issue | #377 |
| Owner | R |
| Screens | ai-pipeline |
| Source | Rahul, 27 Nov 2026 (AI list); S |

## What

`docs/areas/ai-pipeline/status.md` lists `a9-todays-message` as v4 "not run yet", and `a9-todays-message-sums` and `a9-todays-message-lesson` as v1. `prompts/` has v5, v2 and v2, and `quality.md` has their 16 Nov eval (T-141). Bring the table in line with `prompts/` and the latest results.

## Acceptance criteria

- Every prompt in `prompts/` is in the table at its file's version, with its latest result.

## How it's tested

- `./scripts/check-docs.sh`.

## Notes

- 27 Nov 2026: proposed.
