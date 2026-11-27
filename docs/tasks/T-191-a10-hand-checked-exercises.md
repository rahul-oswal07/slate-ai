# T-191 · A hand-checked exercise list for the A10 eval

| | |
|---|---|
| Status | Proposed |
| Issue | #369 |
| Owner | R / M |
| Screens | ai-pipeline, curriculum |
| Source | Rahul, 27 Nov 2026 (AI list); `docs/status.md` Next 7; `quality.md` §2 (A10: exercises aren't measured yet); S–M |

## What

The textbook importer (A10) finds every lesson, but its exercises can't be measured. The 18 hand-typed pairs came from the book's worked examples, which A10 doesn't list as exercises. Type the exercises of 8–10 pages by hand (both parts, different kinds: sums, comparisons, word problems), and add an exercise score to the A10 eval: found, numbers right, answer right.

## Acceptance criteria

- A gold file in `evals/cases/` with page, exercise and expected answer for each.
- The A10 eval reports exercise recall and accuracy beside lesson recall.
- `quality.md` updated with the first run.

## How it's tested

- Pure tests for the exercise matching; one Eval run (costs about USD 10 for both parts unless T-198 lowers it).

## Notes

- 27 Nov 2026: proposed.
