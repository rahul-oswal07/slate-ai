# T-157 · AI call log with cost and failures

| | |
|---|---|
| Status | Proposed |
| Issue | #335 |
| Owner | R |
| Screens | admin, ai-pipeline |
| Source | Rahul, 27 Nov 2026 (admin pages list); CLAUDE.md (every call logged to `AiCall`); needs T-148; M |

## What

`/demo/ai` shows the demo story's calls. Add `/admin/ai-calls` over all of `AiCalls`:

- filters: day range, class, purpose (prompt ID), verdict, error;
- columns: time, purpose and prompt version, model, how long, tokens in and out, estimated cost, verdict (`valid`, `fallback:…`), error;
- totals per day and per purpose: calls, failures, fallbacks, cost.

Tokens and cost: record them in `AiCalls` if not there yet (migration); prices come from config, not code.

## Acceptance criteria

- Shows no prompt text, transcripts or names (the table holds none, and nothing is joined in).
- Cost is computed in code from config prices; a model without a price shows "no price".
- `docs/database.md` updated with any migration.
- All new strings in en, kn, te, ta, hi.

## How it's tested

- Pure tests for the totals and cost; API tests with a fake store; Vitest for the page.

## Notes

- 27 Nov 2026: proposed.
