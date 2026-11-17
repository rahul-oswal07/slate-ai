# Evals

Live evaluation of each Claude call against real-world cases. Run: `dotnet test --filter Category=Eval` (uses credits).

- `cases/a1-lesson-mapping.json` — `{ id, transcript, expected?, alsoAcceptable?, expectLow? }`: first lesson must be `expected` (or `alsoAcceptable`); `expectLow` cases must come back low/unclear. Drafts until real teacher transcripts replace them
- `cases/a2-compression.json` — absence pattern → acceptable targets (`{ id, missed[], lastTaught[], best, alsoAcceptable? }`; draft, 10 cases)
- `cases/a4-check.json` — script → Haiku content verdict (2 good, 4 flawed; `expectPass`)
- `cases/a3-script.json` — (targets, language) → rubric
- A10 textbook import (T-146) has no case file: it runs the real importer on the KTBS Grade 3 maths PDF and compares the draft with our hand-built `data/curriculum/grade3-maths.json` (lesson recall by printed page, hand-typed exercises found). The book isn't in the repo ("not to be republished"): set `Evals:TextbookPdf` and `Evals:TextbookPart` in the API's user-secrets. About USD 5 a part (an estimate)

Results: `results/<date>-<call>.json` with model, prompt version and score. Targets are in `docs/intent.md`.
