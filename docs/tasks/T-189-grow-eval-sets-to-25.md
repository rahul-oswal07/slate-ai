# T-189 · Grow each eval set to 25 cases with real inputs

| | |
|---|---|
| Status | Proposed |
| Issue | #367 |
| Owner | R / M |
| Screens | ai-pipeline |
| Source | Rahul, 27 Nov 2026 (AI list); `quality.md` §2 (25 cases per call); ai-pipeline status gap; M |

## What

`quality.md` asks for 25 cases per AI call. Today A2 has 10, A4 11, A3 14, A8 15 and A1 16, and most were drafted by Claude, so they're cleaner than real speech. Bring each to 25:

- **A1:** real teacher notes (from T-190's saved corrections) and noisy, code-mixed ones;
- **A3, A8, A9:** every family language, checked by a native speaker on 5 each;
- **A2:** more absence patterns;
- **A4:** more crafted flaws.

Then run each set and record the results in `evals/results/`.

## Acceptance criteria

- 25 cases or more in each of `evals/cases/a1`, `a2`, `a3`, `a4`, `a8`, `a9*`.
- No child, parent or teacher name in any case (guardrail 1).
- One results file per set, and `quality.md` and the ai-pipeline status table updated with the scores.

## How it's tested

- `dotnet test --filter Category=Eval` per prompt (costs credits; run once per set).

## Notes

- 27 Nov 2026: proposed. Monika and teachers write or check the real cases.
