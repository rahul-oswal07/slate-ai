# D-116 · Today's message for add and subtract lessons

| | |
|---|---|
| Status | Accepted |
| Owner | Rahul, 15 Nov 2026 (Monika will not make this one) |
| Gate | T-128 PR B |
| Screens | P5-todays-message, T3-approve, T1-today, T2-lesson-confirm, curriculum, ai-pipeline |

## Decision

Today's message (D-91) is only made for a lesson with the book's exercises (D-93). Its picture question is a comparison: the child taps >, < or = between two numbers drawn as bundles and loose sticks (T-90). The A9 prompt (v4) explains comparing and says "Do not do sums".

T-128 adds the book's own add and subtract pairs to six lessons (M3.ADD.NOCARRY, M3.ADD.CARRY, M3.ADD.WORD, M3.SUB.NOBORROW, M3.SUB.BORROW, M3.SUB.WORD), copied from their `examples`. Those lessons now carry a "Book exercises" tag. They still get no today's message, because nothing yet says what the picture question, the answer step and the home activity look like for a sum. Three things to decide:

1. **The picture question.** For example, "248 + 438 = ?" drawn as hundreds, bundles and sticks, with the child picking from 3 answers. Code makes the 2 wrong answers from the usual mistake (forgetting the carried ten; smaller-from-bigger in each column), as `ProblemGenerator` already does for the catch-up.
2. **The home activity.** The parent adds or takes away with sticks and bundles for one of the book's pairs. The child says the answer. The answer is computed by code; A9 doesn't do sums.
3. **The tag's words** in the five languages: "Book exercises" / "ಪುಸ್ತಕದ ಅಭ್ಯಾಸ" / "పుస్తక అభ్యాసం" / "புத்தகப் பயிற்சி" / "किताब के अभ्यास" (drafts).

## Recommendation

Accepted as written below, with one change: sums get their own prompt file, `prompts/a9-todays-message-sums.md` v1, instead of an A9 v5, so the Nalikali lesson keeps v4 exactly as evaluated.

- Option 1 for the picture question: choose from 3 code-made answers. The tap-to-choose step and the "Got it / Needs help" level (D-108) stay the same shape.
- PR B builds it behind `TodaysMessage:ArithmeticExercises`: on in `appsettings.Development.json`, off in `appsettings.json` (staging and prod) until the new prompt's eval run passes (guardrail 6).
- Every number and answer comes from code (guardrail 4):
  - the picture is the book's pair;
  - the three choices are the answer plus the usual mistake (forgot the carried ten; smaller-from-bigger in each column), then ten either side (`ExerciseAnswers.Choices`);
  - the answer's place is fixed by the numbers.
- The child's submit is the chosen number. `EveningMessages.PictureFirst`/`PictureSecond` widen to varchar 3 (migration `TodaysPictureSumAnswers`).
- The tag's words stay as drafted.

## Notes

- 15 Nov 2026: Proposed by Rahul with T-128 PR A (data, checks and the tag; no message changes).
- 15 Nov 2026: Accepted by Rahul, who takes this decision instead of Monika. Built in T-128 PR B behind the flag. The eval run for `a9-todays-message-sums` v1 is still to do; it needs an Anthropic key.
- 15 Nov 2026: Eval run passed (28/28 after at most one retry; the 8 sum cases 7 first try). Rahul asked for sums on by default: `TodaysMessage:ArithmeticExercises` now defaults to `true` in code and `appsettings.json`, and stays as an off switch.
