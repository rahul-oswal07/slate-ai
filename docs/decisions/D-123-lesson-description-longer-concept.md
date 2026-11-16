# D-123 · A description and worked examples per lesson; today's message's concept about a minute long

| | |
|---|---|
| Status | Accepted |
| Owner | Rahul, 16 Nov 2026 |
| Gate | T-141 |
| Screens | P5-todays-message, T3-approve, admin |

## Decision

Today's message's spoken concept (D-91, the lesson audio since D-105) was 3 sentences and at most 50–60 words: about 12–15 s heard once. A9 only knew the lesson's name, one or two example lines and the key words, so it could say little about how the class was taught. Rahul wants a longer voice note that is grounded in the textbook.

## Recommendation

- **Every curriculum lesson has a description and 1–4 worked examples**, in English, in the new table `LessonContents` (one row per competency ID, for every class, like `LessonLinks`).
  - They come from `data/lesson-content/grade3-maths.json`: read from the KTBS textbook in our own words (the book is not to be republished), with the book's methods and numbers.
  - No characters' names, and no pair of the book's exercises in an example.
- **Loaded at startup on every server.** New lessons are added and unedited rows follow the file. **An edit on `/admin/lesson-links` is never overwritten** (`EditedAt`).
- **Editable on `/admin/lesson-links`** ("About this lesson"), open like the other admin pages (D-112).
- **A9 builds the concept on it**: 5–6 sentences, 80–110 words, about a minute with the AI notice. It says what the lesson is and the book's way, walks through one worked example step by step, and ends with the idea to remember. Prompts: `a9-todays-message` v5, `a9-todays-message-sums` v2, `a9-todays-message-lesson` v2.
- **Code checks** (guardrails 2 and 4):
  - The concept may be at most 6 sentences and 140 words.
  - A worked example's numbers are allowed in the text, never the picture question's answer or a sum's other choices.
  - The 95 s audio cap is unchanged.
- The activity, the picture question, the teacher's approval (guardrail 3) and her editing are unchanged. Her own Kannada edit has the same longer limits.

## Notes

- 16 Nov 2026: accepted by Rahul (length about 1 minute; a curriculum-level table, not the per-day `Lessons` table; all three prompts, the demo lesson included). Evals in T-141.
