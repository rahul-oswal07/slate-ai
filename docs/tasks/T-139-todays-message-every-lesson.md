# T-139 · Today's message for every lesson; no picture question without the book's exercises

| | |
|---|---|
| Status | Done (#295) |
| Issue | # |
| Owner | R |
| Screens | P5-todays-message, T3-approve, T1-today, delivery, ai-pipeline |
| Source | Rahul, 16 Nov 2026; D-121; M |

## What

Every confirmed lesson now gets today's message (D-121), not only the 7 with the book's exercises.

- Without exercises, it has the concept (voiced), a home activity (voiced) and the DIKSHA video, and no picture question.
- It is approved as always and sent on WhatsApp with `todays_lesson_link_v1`. Rahul accepted the template text's "picture question" mismatch.

**Server**
- `TodaysSections.Picture` is nullable, and every reader handles null (`WithRemoved`, `HasAnySection`, the family view, the Approve view, edits, answers, the evening token lookup). Stored JSON with `"picture": null` reads back fine.
- `TodaysMessageService.StartAsync` starts a draft for any lesson the curriculum knows when a family is due. The exercises only decide the picture.
- The generator no longer fails with `fallback:no-exercises`; an unknown lesson fails with `fallback:unknown-lesson`.
- `TodaysMessageWriter.PromptFor(null)` is `a9-todays-message-lesson`. Its input has `pictureQuestion: null`, and the compare and sum inputs are unchanged.
- `AllowedNumbers(lessons, null)` is the lesson's example numbers, page and part.
- `TodaysSectionKind.PromisedFor` promises the picture only when there is one.
- The answers endpoint has nothing to return (204) without a picture.

**Prompt and eval**
- `prompts/a9-todays-message-lesson.md` v1.
- `evals/cases/a9-todays-message-lesson.json`: 10 cases over the curriculum and the five languages, one with what she said in class.
- The eval harness runs either case file. **10 of 10 passed first time** (`evals/results/2026-11-16-a9-todays-message-lesson.json`).

**Web**
- Approve hides the picture section and the pair picker when there is none.
- The family page goes activity → (video) → Done, with "Thank you for doing it together" (`parent.todays.doneBodyNoQuestion`, all five languages; kn, te, ta, hi are drafts for Monika).
- The answers hook already treats 204 as no answers and doesn't poll.

## Acceptance criteria

- Confirming `M3.GEO.2D` with a family due drafts today's message: concept and activity in every family language, voiced, the video, and no picture question.
- Approving and sending it reaches the family page and WhatsApp.
- Exercise lessons (compare, add, sub) are unchanged.
- No answers card or level for a message without a picture.

## How it's tested

- API:
  - `TodaysMessageTests`: no-picture promises, family view and round trip; numbers without a picture; prompt choice and inputs.
  - `TodaysMessageGeneratorTests`: a no-exercise lesson and a sum lesson with the flag off both get the lesson prompt and no picture.
  - `TodaysMessageEndpointTests`: any lesson starts a draft; a changed lesson replaces the old approval; no answers without a picture.
  - These replace the tests that pinned "no message without exercises".
- Lesson and voice tests now fake an empty register, because confirming any lesson checks who is due.
- Db: `DemoResetDbTests` now waits for the background draft and checks the AI log comes through Reset whole, instead of assuming no draft starts.
- Web:
  - `TodaysMessageTab.test.tsx`: no picture section or pair picker; concept and activity still promised.
  - `TodaysPages.test.tsx`: activity goes straight to Done, with the new Done text.
- Live A9 eval for the new prompt: 10/10.

## Notes

- 16 Nov 2026: built. The 2 failing generator tests were written first and failed until the prompt and the code were in. Written but not done: a Db round trip of a null picture (the JSON round trip is covered in unit tests). Left: Monika's check of the new Done text in kn, te, ta, hi; a WhatsApp template without the picture line (D-121).
