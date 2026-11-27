# T-193 · Compare two models on the same eval cases

| | |
|---|---|
| Status | Proposed |
| Issue | #371 |
| Owner | R |
| Screens | ai-pipeline |
| Source | Rahul, 27 Nov 2026 (AI list); guardrail 5 (models from `Ai:Models:*`); M |

## What

Whether a step could use a cheaper model is a guess. Add an eval mode that runs one prompt's cases on two models, set by config for the run, and writes one report side by side: score, first-pass rate, tokens, cost and time per case. For example, A9 on Opus vs Sonnet, or A1 on Sonnet vs Haiku.

## Acceptance criteria

- Models come from config for the run, never hard-coded.
- The report is in `evals/results/` with both models' outputs per case for a human to read.
- Production config doesn't change; a model change is its own PR with the report.

## How it's tested

- A unit test for the report; one run on a small set.

## Notes

- 27 Nov 2026: proposed.
