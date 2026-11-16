# T-141 · A longer voice note, built on the textbook's description of each lesson

| | |
|---|---|
| Status | Done (not merged yet) |
| Issue | #299 |
| Owner | R |
| Screens | P5-todays-message, T3-approve, admin |
| Source | Rahul, 16 Nov 2026 ("make the voice note a little bigger … refer to the textbook"); D-123; M |

## What

Today's message's spoken concept (the "voice note", D-105) was capped at 3 sentences and 50–60 words: about 12–15 s. Rahul asked for about a minute, built on what the textbook actually teaches.

- **Every lesson gets a description and worked examples**: `data/lesson-content/grade3-maths.json`.
  - 60 lessons were read from the KTBS Grade 3 maths PDFs (Part 1 and 2, the books of D-89), page by page.
  - `M3.BRIDGE.COMPARE` (Nalikali p 4; that PDF isn't on Rahul's machine) is written from the notes in `docs/design/curriculum.md`, with numbers that aren't in its exercises.
  - All are in our own words: the book is marked "not to be republished" and the repo is public. Its methods and numbers are facts.
  - No characters' names (the book has some): the names check would refuse a class child with the same name.
  - No worked example uses a pair of the book's exercises, which the picture question comes from.
- **Stored in the database**: a new table `LessonContents`, one row per lesson.
  - The app loads the file at startup on every server.
  - It adds new lessons and updates rows nobody edited; an edit on the admin page is never overwritten.
- **Shown and edited on `/admin/lesson-links`**: "About this lesson" under each lesson.
  - It shows the description, the examples and where they came from (From the textbook / Written for this app / Edited here).
  - Edit and Save work per lesson; each chapter says "N described".
- **A9 builds the concept on it**: `a9-todays-message` v5 (compare, the demo lesson), `a9-todays-message-sums` v2 and `a9-todays-message-lesson` v2. The concept is 5–6 sentences and 80–110 words:
  - what the lesson is and how the book teaches it;
  - one worked example, step by step;
  - the idea to remember.

  The activity and the picture question are unchanged.
- **Code checks**:
  - The concept may be 6 sentences and 140 words; the prompts ask for 80–110, and Hindi and English run longer.
  - All the text may be 2000 characters.
  - A9's output limit is 16384 tokens: five languages went past 8192 in the eval.
  - A worked example's numbers are allowed, but never the picture question's answer or its other choices (guardrail 4).
  - The voice still has to fit 95 s with the AI notice; a longer one stays text only, as before.

## Acceptance criteria

- Every curriculum lesson has content that passes the admin page's rules. Every sum in it is right, and no example uses a pair of the book's exercises (`LessonContentFileTests`).
- A new today's message's request to A9 carries each lesson's `content`. A concept that uses a worked example's numbers passes; without the content the same numbers are refused.
- On `/admin/lesson-links` the content can be read, added, edited and refused with a reason. A restart doesn't undo an edit.
- The A9 eval passes every case with no picture question's numbers in a concept, and the concept is about a minute.

## How it's tested

- API tests:
  - `LessonContentFileTests`, `LessonContentRulesTests`, `LessonContentEndpointTests`, `LessonContentLoaderTests` (with the fake store);
  - `TodaysMessageContentTests` (A9's input, allowed numbers, the 6-sentence / 140-word limit);
  - two `TodaysMessageGeneratorTests`.
- Db (Rahul agreed, 17 Nov): `StartupDbTests.Lesson_content_loads_at_startup_follows_the_file_and_never_overwrites_an_admin_edit` (61 rows at startup, `text[]` examples, an edit and its replay, a reload that keeps the edit).
- Web: `LessonContentEditor.test.tsx` (show, count, add, refuse, cancel).
- Live evals (16 Nov, Opus 5.5, on the shipped content):
  - `a9-todays-message` v5 + `sums` v2: 28/28 first pass, 0 concepts with the picture's numbers;
  - `a9-todays-message-lesson` v2: 10/10 first pass.
  - Average Kannada concept 81–84 words, about 45 s at the 1.86 words a second measured for Bulbul (`quality.md`), plus the 5–7 s AI notice.
  - Files: `evals/results/2026-11-16-a9-todays-message.json` and `…-lesson-v2.json` (the lesson eval writes `…-lesson.json`; renamed so T-139's v1 run stays).

## Notes

- 16 Nov 2026: built.
  - The first eval run (prompts asking for "at most 110 words") gave about 30–35 s, so the prompts now ask for 80–110 words.
  - The second run had `p4-all-five` truncated at 8192 tokens (now 16384), and two Hindi concepts at 124 and 139 words that passed on the retry (the limit is now 140).
  - The third run is the one in the PR.
- 16 Nov 2026: the demo lesson's message changes two days before the final. Run the demo once on staging after this merges and listen to the concept.
- Left:
  - Monika's review of the kn/te/ta/hi admin strings (drafts).
  - The real voice length was estimated from word counts, not measured with Sarvam.
- 17 Nov 2026: added the Db test; renumbered the Under the hood task T-140 → T-142 so check-docs passes again.
