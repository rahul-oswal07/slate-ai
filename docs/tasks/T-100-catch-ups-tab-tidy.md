# T-100 · Catch-ups tab tidy

| | |
|---|---|
| Status | Built in part |
| Issue | none · first part merged in #212 |
| Owner | R |
| Screens | T3-approve, ai-pipeline |
| Source | G1, G3 · Finale Should · size S |

## What

**Catch-ups tab tidy:** sort by action needed (ready, failed, sent); Try again on a failed card; a lesson without a practice rule never shows as "Generation failed" (staging showed "Multiplication in daily life" failed); "In Hindi · 1 child" plural bug

## Acceptance criteria

No failed card from a lesson that can't have a catch-up

## How it's tested

Unit test for candidates; Vitest

## Notes

- 15 Nov: moved here from `tasks.md` (T-110).
- 15 Nov: the first part is merged (#212). Still to do: sort by action needed, Try again on a failed card, the "In Hindi · 1 child" plural.
- 15 Nov: no failed catch-up from a lesson without a practice rule (part of T-100; branch `fix/T-100-no-failed-catchup-without-practice`, not merged). From Reset, confirming p 4 and then making the week's catch-ups gave "Generation failed" (`fallback:no-practice`): with no candidate, `A2Validator.Rule` took the latest missed lesson, `M3.BRIDGE.COMPARE`, which has no practice rule (D-89, kept so on purpose). The rule now takes the latest missed lesson that has one (`CompressionInput.Practicable`, from `CurriculumGraph.WithPractice`; not sent to Claude, so no prompt change), and a child with none gets no catch-up instead of a failed one. Live on a local server: the same steps now give Manjunath a ready, verified catch-up on `M3.ADD.NOCARRY` and no failed card. Catch-ups already made before the fix (staging's included) stay failed until the next week. The rest of T-100 (sorting, Try again, the Hindi plural) is not done.
- 14 Nov, how it was found: **the week's catch-up failed once today's lesson was confirmed** (`fallback:no-practice`: with no candidate, `A2Validator.Rule` took `M3.BRIDGE.COMPARE`, which has no practice rule).
