# Evals

Live evaluation of each Claude call against real-world cases. Run: `dotnet test --filter Category=Eval` (uses credits).

- `cases/a1-lesson-mapping.json` — `{ id, transcript, expected?, alsoAcceptable?, expectLow? }`: first lesson must be `expected` (or `alsoAcceptable`); `expectLow` cases must come back low/unclear. Drafts until real teacher transcripts replace them
- `cases/a2-compression.json` — absence pattern → acceptable targets (`{ id, missed[], lastTaught[], best, alsoAcceptable? }`; draft, 10 cases)
- `cases/a4-check.json` — script → Haiku content verdict (2 good, 4 flawed; `expectPass`)
- `cases/a3-script.json` — (targets, language) → rubric
- A10 textbook import (T-146) has no case file: it runs the real importer on the KTBS Grade 3 maths PDF and compares the draft with our hand-built `data/curriculum/grade3-maths.json` (lesson recall by printed page, hand-typed exercises found). The book isn't in the repo ("not to be republished"): set `Evals:TextbookPdf` and `Evals:TextbookPart` in the API's user-secrets. Part 1 (160 pages) cost about USD 5 and took 17 minutes; the results record the tokens

Results: `results/<date>-<call>.json` with model, prompt version and score. Targets are in `docs/intent.md`.

## Teachers' corrections (A1, T-102)

Each time a teacher confirms a lesson over A1's match (T2), the app saves what A1 matched and what she chose (`LessonFeedback` table). `GET /api/admin/lesson-feedback` (optional `?from=YYYY-MM-DD`, newest 500) lists them as `{ cases: [...] }` in the shape of `cases/a1-lesson-mapping.json`, plus `outcome` (`accepted`, `edited`, `replaced`), `matched`, `confidence`, `matchStatus`, `promptVersion`, `classId` and `date`. `expected` is the first lesson she confirmed and `alsoAcceptable` the others.

`transcript` there is **A1's own Kannada summary of her note, not the transcript**: neither the transcript nor the audio is ever stored, and no names are kept. So a case is a weaker input than a real note. To grow the A1 set, copy the `edited` and `replaced` cases (A1 was wrong) into `cases/a1-lesson-mapping.json`, keep `id`, `transcript`, `expected` and `alsoAcceptable`, and check the class followed the built-in curriculum (`Classes.CurriculumId` null) or that the IDs exist in it.
