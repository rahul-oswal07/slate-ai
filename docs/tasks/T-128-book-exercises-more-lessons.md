# T-128 · Book exercises for more lessons, so they get today's message

| | |
|---|---|
| Status | In progress |
| Issue | #260 |
| Owner | R / M |
| Screens | curriculum, ai-pipeline, P5-todays-message, T1-today, T2-lesson-confirm, T3-approve |
| Source | Rahul, 15 Nov 2026: why does only the Nalikali lesson open Approve after confirming? Extends D-93 (whose text says this is how lessons without a practice rule get messages later); M–L |

## What

After she confirms a lesson, the app opens Approve only when the server has started a today's message (T-108). The server starts one only when the lesson has the book's exercise list (D-93, `TodaysPicturePicker.Supported`) and a family is due tonight's message. Only `M3.BRIDGE.COMPARE` (Nalikali Part 1 p 4) has an `exercises` block, so for every other lesson she goes back to Today and no today's message goes out.

This task adds the book's exercises to more lessons, so they get the same flow: confirm → Approve → today's message tonight.

1. **Data.** Copy the exercises from the textbook pages into `exercises` on more competencies in `data/curriculum/grade3-maths.json`, as numbers only, following `docs/design/curriculum.md` (no page images in the repo). Monika picks which lessons come first.
2. **New exercise kinds.** `ExerciseList` only has `Compare` and `Extremes`, and the picture picker only uses `Compare`. Lessons like `M3.ADD.CARRY` or `M3.SUB.BORROW` need kinds such as `add` / `sub` pairs, checked at startup by `ExerciseAnswers.Check` (a pair, a sum ≤ 999, a difference not below 0; not the lesson's practice rule, since the book's own pairs can carry or borrow more than it allows) and answered by code (guardrail 4).
3. **Picture question and sections.** `TodaysPicturePicker` and the today's message sections (`Features/TodaysMessage/TodaysSections.cs`) build a question for each new kind. The A9 prompt (`prompts/a9-todays-message.md`) may need a slot for the new kind: if it changes, bump the version and run the evals (guardrail 6).

What the picture question and activity look like for add and subtract: [D-116](../decisions/D-116-arithmetic-exercises-todays-message.md), Accepted by Rahul on 15 Nov.

**Built in two PRs.**
- **PR A (no message changes):**
  - `ExerciseList` gets `add` and `sub` pairs, checked at startup: each a pair, a sum ≤ 999, a difference not below 0.
  - Seven lessons get the book's numbers from their own `examples`.
  - `GET /api/curriculum` says `hasExercises`, and a **Book exercises** tag shows in the lesson picker, on Lesson confirm and on Today's confirmed lesson.
  - Today's message and A9's input are unchanged.
- **PR B:** today's message for add and subtract, behind `TodaysMessage:ArithmeticExercises` (on in Development, off in staging and prod until the eval run).
  - add/sub picture kinds in `TodaysPicturePicker`, with three choices by code;
  - the new prompt `a9-todays-message-sums` v1 (the Nalikali lesson keeps A9 v4);
  - the parent's answer step for a sum;
  - the Approve picture-pair picker.

## Acceptance criteria

- Every new `exercises` block loads, and `ExerciseAnswers.Check` refuses bad items (out of range, not a pair, a sum over 999, a negative difference) at startup.
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

- 15 Nov 2026: PR B built, on the same branch as PR A ([#264](https://github.com/rahul-oswal07/slate-ai/pull/264)).
  - **Server:** with the flag on, confirming M3.ADD.* or M3.SUB.* with a family due starts a draft, so she goes to Approve as for Nalikali.
    - The picture is the book's first add/sub pair, with three choices made by code (`ExerciseAnswers.Choices`).
    - A9 writes with `prompts/a9-todays-message-sums.md` v1, and the allowed numbers add each number's digits and 100.
  - **Parent page:** "248 + 438 = ?" with three stick pictures; the numeral shows only on the one tapped; two submits, scored by code.
  - **Approve:** shows the sum, the choices and the answer, and the other pairs as sums.
  - **Database:** migration `TodaysPictureSumAnswers` widens the picture answer to 3 characters.
  - **Left:** run the A9 eval (8 new sum cases in `evals/cases/a9-todays-message.json`) with an Anthropic key, then turn the flag on for staging and prod. Native review of the new kn/te/ta/hi strings.

- 15 Nov 2026: PR A built.
  - **Exercises:** `add`/`sub` exercises, from the examples, on M3.ADD.NOCARRY, M3.ADD.CARRY, M3.ADD.WORD, M3.SUB.NOBORROW, M3.SUB.BORROW and M3.SUB.WORD; `extremes` on M3.NUM.COMPARE.
  - **Checks:** they aren't held to the practice rule, because the book's 872 − 586 borrows twice.
  - **Tag:** `hasExercises` on the curriculum and the **Book exercises** tag in 3 places; its kn/te/ta/hi words are drafts for Monika.
  - **Left:** PR B, held by D-116. The full exercise lists from the book pages are not extracted yet.
- 15 Nov 2026: Task opened. Only the demo lesson starts a today's message today (D-93); no code changed yet.
