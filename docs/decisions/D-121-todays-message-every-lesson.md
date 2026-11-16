# D-121 · Today's message for every lesson; no picture question without the book's exercises

| | |
|---|---|
| Status | Accepted |
| Owner | Rahul, 16 Nov 2026 |
| Gate | T-139 |
| Screens | P5-todays-message, T3-approve, T1-today, delivery |

## Decision

D-93 drafted today's message only for a lesson with the book's exercise list, because the picture question is built from it. That is 7 of 61 lessons. For every other lesson the family got the plain evening message: no voice unless the teacher ticked lesson audio (T-62), and no WhatsApp at all, because that message has no approved template (D-107). Rahul: "even though there is no exercise it should still create an audio clip and it should send to the parent."

## Recommendation

- **Every confirmed lesson the curriculum knows gets today's message** when a family is due tonight.
- **Without the book's exercises there is no picture question.** The message has the concept in every family language (voiced), a home activity (2–3 steps, voiced) and the DIKSHA video if the lesson has one. The teacher checks and approves it as always (guardrail 3).
- **A new prompt, `a9-todays-message-lesson` v1, writes it:**
  - Numbers come only from the lesson's own `examples` (or none); never the answer to a question it asks.
  - It is picked by code when there is no picture.
  - v4 (compare) and `sums` v1 are untouched, and so are their inputs.
  - Eval: 10 of 10 cases passed the checks first time (16 Nov, `evals/results/2026-11-16-a9-todays-message-lesson.json`).
- **Promised sections (D-108):** a message without a picture promises the concept and the activity only. There are no answers to see: Today shows no answers card and Missed no level.
- **The family page** goes from the activity (and video) straight to Done, which says "Thank you for doing it together" instead of "the teacher will see the answer".
- **WhatsApp:** it goes with the existing `todays_lesson_link_v1`, so it reaches families now. That template's fixed text says "a home activity and a picture question are ready", which is not true for these messages. **Rahul accepts this mismatch for now.** A template without the picture line can replace it later; then this choice can be revisited.
- With `TodaysMessage:ArithmeticExercises` off, add and subtract lessons get a message without the sum picture rather than none.

Changes D-93's gate (exercises now decide only the picture question). Refines D-108 (the picture is promised only when there is one).

## Notes

- 16 Nov 2026: asked for by Rahul, who chose "today's message, no picture" over making the lesson audio clip automatically, and the existing WhatsApp template over waiting for a new one. Built in T-139.
