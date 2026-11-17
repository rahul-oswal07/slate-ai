# T-146 · Import a textbook PDF into a draft curriculum with Claude

| | |
|---|---|
| Status | Open |
| Issue | #308 |
| Owner | R |
| Screens | curriculum, ai-pipeline, admin |
| Source | Rahul, 17 Nov 2026; D-125; needs T-145; L |

## What

An admin uploads a textbook PDF on `/admin/curriculum`. A background job turns it into a **draft** curriculum.

1. **Outline:** prompt A10 reads the book 10 pages at a time. Code builds the chapters and their printed page ranges.
2. **Lessons:** per chapter, A10 gives each lesson:
   - names (kn, en, and drafts in te, ta, hi);
   - key words;
   - pages;
   - a description and examples in our own words;
   - the typed exercises (`compare`, `extremes`, `add`, `sub`), plus any other exercise as text marked `other`;
   - its pictures, described (page, position, what it shows), never copied.
3. **Prerequisites:** A10 picks them from the lesson IDs. Code keeps only earlier lessons, so there can be no cycle.
4. **Assemble:** code mints the IDs, checks the exercises with `ExerciseAnswers.Check` (dropping and flagging any that fail), and runs the whole draft through `CurriculumLoader`.

Each stage is saved, so Try again resumes where it stopped. The PDF is kept only until the import is published or discarded.

## Acceptance criteria

- No number, ID or prerequisite from Claude is used without a check in code (guardrail 2). Answers are never stored (guardrail 4).
- A failed or stalled import says so and can be retried without starting again.
- Model from `Ai:Models:Import`. Prompts versioned, with an eval run (guardrail 6). Every call logged to `AiCall`.

## How it's tested

- Unit tests: the outline assembler, ID minting, exercise checks, the prerequisite rule, resume and stale.
- Endpoint tests with a scripted fake `ILlmClient`.
- An A10 eval against the hand-built `grade3-maths.json`, using the KTBS PDF from user-secrets (never committed).

## Notes

- 17 Nov 2026: planned with D-125.
