# Evals

Live evaluation of each Claude call against real-world cases. Run: `dotnet test --filter Category=Eval` (uses credits).

- `cases/a1-lesson-mapping.json` — `{ id, transcript, expected?, alsoAcceptable?, expectLow? }`: first lesson must be `expected` (or `alsoAcceptable`); `expectLow` cases must come back low/unclear. Drafts until real teacher transcripts replace them
- `cases/a2-compression.json` — absence pattern → acceptable targets
- `cases/a3-script.json` — (targets, language) → rubric

Results: `results/<date>-<call>.json` with model, prompt version and score. Targets are in `docs/intent.md`.
