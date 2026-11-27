# T-190 · Teachers' corrections into the A1 eval by script

| | |
|---|---|
| Status | Proposed |
| Issue | #368 |
| Owner | R |
| Screens | ai-pipeline, T2-lesson-confirm |
| Source | Rahul, 27 Nov 2026 (AI list); T-102; ai-pipeline status gaps; pairs with T-158; S |

## What

Teachers' corrections of A1 are saved (`LessonFeedback`, T-102), but they reach `evals/cases/a1-lesson-mapping.json` only by hand. Add a script that reads `GET /api/admin/lesson-feedback` from a server (or T-158's download) and merges the new cases into the file, without duplicates. It's a dry run unless `--apply`.

Also close the known gap: a **Change** after confirming saves no case, because A1's match is gone by then. Keep A1's match with the lesson so a Change can still be saved as a case.

## Acceptance criteria

- Running it twice adds nothing the second time.
- A merged case has no name, transcript or audio, only what the endpoint gives (guardrail 1).
- A Change after confirming saves a case with outcome `replaced`.
- `evals/README.md` says how to run it.

## How it's tested

- Pure tests for the merge (duplicates, new cases); API test for the Change case.

## Notes

- 27 Nov 2026: proposed.
