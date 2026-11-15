# T-128 · Book exercises for more lessons, so they get today's message

| | |
|---|---|
| Status | Proposed |
| Issue | #260 |
| Owner | R / M |
| Screens | curriculum, ai-pipeline, P5-todays-message, T2-lesson-confirm, T3-approve |
| Source | Rahul, 15 Nov 2026: why does only the Nalikali lesson open Approve after confirming? Extends D-93 (whose text says this is how lessons without a practice rule get messages later); M–L |

## What

After she confirms a lesson, the app opens Approve only when the server has started a today's message (T-108). The server starts one only when the lesson has the book's exercise list (D-93, `TodaysPicturePicker.Supported`) and a family is due tonight's message. Only `M3.BRIDGE.COMPARE` (Nalikali Part 1 p 4) has an `exercises` block, so for every other lesson she goes back to Today and no today's message goes out.

This task adds the book's exercises to more lessons, so they get the same flow: confirm → Approve → today's message tonight.

1. **Data.** Copy the exercises from the textbook pages into `exercises` on more competencies in `data/curriculum/grade3-maths.json`, as numbers only, following `docs/design/curriculum.md` (no page images in the repo). Monika picks which lessons come first.
2. **New exercise kinds.** `ExerciseList` only has `Compare` and `Extremes`, and the picture picker only uses `Compare`. Lessons like `M3.ADD.CARRY` or `M3.SUB.BORROW` need kinds such as `add` / `sub` pairs, checked at startup by `ExerciseAnswers.Check` (≤ 999, the lesson's carry or borrow rule) and answered by code (guardrail 4).
3. **Picture question and sections.** `TodaysPicturePicker` and the today's message sections (`Features/TodaysMessage/TodaysSections.cs`) build a question for each new kind. The A9 prompt (`prompts/a9-todays-message.md`) may need a slot for the new kind: if it changes, bump the version and run the evals (guardrail 6).

[DECIDE: Monika] Which lessons come first, and what the picture question and activity look like for kinds other than comparing (e.g. "48 + 35 = ?" with the answer computed). This needs a new decision file (next free D- number) before step 3 is built.

## Acceptance criteria

- Every new `exercises` block loads, and `ExerciseAnswers.Check` refuses bad items (out of range, wrong arity, a carry where the lesson has none) at startup.
- Confirming one of the new lessons, with a family due tonight's message, starts a today's message and opens Approve. Without such a family she goes to Today, as now.
- Every number in the picture question and in the activity comes from the book's list. Every answer is computed in code. The AI writes the wording only.
- `M3.BRIDGE.COMPARE` behaves exactly as before.
- `docs/areas/curriculum/status.md` and `docs/design/curriculum.md` list which lessons have exercises.

## How it's tested

- `ExerciseAnswersTests`: answers and `Check` for each new kind.
- `CurriculumLoaderTests` / `CurriculumStartupTests`: the real file loads, and a bad item fails startup.
- `TodaysPicturePicker` unit tests per kind. `TodaysMessageTests`: confirming a new lesson over HTTP (`FakeApi` + fake `ILlmClient`) starts a draft.
- An eval run in the PR if the A9 prompt changes.
- `./scripts/verify.sh`.

## Notes

- 15 Nov 2026: Task opened. Only the demo lesson starts a today's message today (D-93); no code changed yet.
