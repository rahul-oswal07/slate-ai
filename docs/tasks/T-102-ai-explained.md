# T-102 · AI explained (PAIR)

| | |
|---|---|
| Status | In progress |
| Issue | #315 |
| Owner | R |
| Screens | T0-setup, T2-lesson-confirm, ai-pipeline, admin |
| Source | G3, PAIR · Finale Should · size S |

## What

**AI explained (PAIR):** a short card on Welcome: what the AI does and doesn't, "you approve everything"; lesson confirm shows "What I heard: '…'" and the matched page; her lesson corrections and edits are saved as eval cases

## Acceptance criteria

She sees why the AI chose the lesson

## How it's tested

Vitest; unit test for the saved feedback

- `LessonFeedbackBuilderTests`: the saved case, no case without an A1 match, the outcome against what T2 offered (mapped and low), no field for a name, transcript or audio.
- `VoiceUploadEndpointTests`: upload → match → she picks another lesson → one case after an outbox replay, listed on `/api/admin/lesson-feedback` with no names; confirming as offered is `accepted`, and a later Change adds nothing. `LessonEndpointTests`: a pick with no note saves none.
- `LessonVoiceDbTests` (Db): the case is written in the confirmation's transaction, matched on the note's start time after a Postgres round trip, once per `clientRequestId`, and the `from` filter translates.
- Vitest: `WelcomeScreen.test.tsx` (the AI card), `ConfirmLesson.test.tsx` (What I heard, the matched pages for mapped and low, Kannada summary).

## Notes

- 27 Nov: built. Welcome has a "What the AI does" card (does, never does, you approve everything). T2's "What you said" card shows "What I heard: “…”" (A1's stored summary in her language; the transcript is never stored, so it isn't shown) and "Matched in the book:" with the pages of A1's matched lessons. Confirming over a mapped or low match saves an A1 eval case in a new `LessonFeedback` table (migration `LessonFeedback`): A1's ranked IDs, status, confidence and prompt version, her chosen IDs, how she confirmed, the outcome (accepted, edited, replaced) by code, and A1's own scrubbed summary and examples. No names, transcript or audio. `GET /api/admin/lesson-feedback` lists them in the A1 case shape. Left: copy (English included) and the kn/te/ta/hi strings are drafts for Monika; design cards T0 and T2 don't show the new parts; moving saved cases into `evals/cases/a1-lesson-mapping.json` is by hand; a Change after confirming saves no case. Why a table and not a column on `Lessons`: the lesson row is overwritten by Change, a new note and the demo reset, and a case must outlive them; the table holds nothing the lesson row doesn't already hold.
- 15 Nov: moved here from `tasks.md` (T-110).
