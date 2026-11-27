# T-192 · Run the evals in CI when a prompt changes

| | |
|---|---|
| Status | Proposed |
| Issue | #370 |
| Owner | R |
| Screens | ai-pipeline |
| Source | Rahul, 27 Nov 2026 (AI list); CLAUDE.md guardrail 6 (changing a prompt → bump its version and include an eval run); needs a spend limit; M |

## What

Guardrail 6 is kept by hand. Add a GitHub Action that runs on a PR that changes a file in `prompts/`:

- it checks the prompt's version was bumped;
- it runs that prompt's `Category=Eval` tests only;
- it posts the score, cost and time on the PR;
- it fails under the target (85% first pass, or the prompt's own rule in `quality.md`).

## Acceptance criteria

- No eval runs on a PR that doesn't touch `prompts/`.
- A spend limit per run; the run stops and fails over it.
- The API key is a repository secret, available only to PRs from this repository (not forks).
- The results file is attached to the run.

## How it's tested

- A test PR that changes one prompt without a version bump (fails) and with one (runs).

## Notes

- 27 Nov 2026: proposed. Spend limit per run to be set by Rahul.
