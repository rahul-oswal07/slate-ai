# Curriculum · status

What works today and what doesn't. No dated notes here: those go in the task's file ([`docs/tasks/`](../../tasks/README.md)).

Spec: [`spec.md`](spec.md)

## Built

- Each class follows one curriculum: the built-in one from `data/curriculum/*.json`, or a published one stored in the database (`Classes.CurriculumId`, T-145, D-125). Code reads it per class through `ICurriculumProvider`; the lesson picker loads `GET /api/classes/{id}/curriculum`.
- A stored curriculum gets every check a file gets (`CurriculumLoader`) and never changes once published.
- Every class so far follows the built-in curriculum: nothing can publish a stored one until the review page (T-147) is built.
- A textbook PDF can be imported into a **draft** curriculum through the admin API (T-146, prompts A10).
  - Chapters, IDs, order and pages are made by code.
  - Exercises are checked by `ExerciseAnswers`; other exercises are kept as summaries and pictures as descriptions.
  - Every lesson is marked needs-review, with flags for whatever was dropped.
  - The book's pages are deleted once the draft is written.
- The built-in curriculum is Grade 3 maths only: the whole book, 60 competencies (T-81, on prod since `v1.2`).
- The Nalikali demo lesson `M3.BRIDGE.COMPARE` (more, less, equal; Nalikali Part 1 p 4) is in chapter 0 with the book's exercises (T-87).
- Exercises can also be `add` and `sub` pairs, checked at startup: a sum ≤ 999, a difference not below 0 (T-128).
- M3.ADD.NOCARRY, M3.ADD.CARRY, M3.ADD.WORD, M3.SUB.NOBORROW, M3.SUB.BORROW and M3.SUB.WORD have `add`/`sub` exercises, and M3.NUM.COMPARE has `extremes`, all copied from their `examples` (T-128).
- `GET /api/curriculum` says which lessons have exercises (`hasExercises`) (T-128).

## Known gaps and drafts

- Lesson descriptions (`LessonContents`) and video links (`LessonLinks`) exist for the built-in curriculum only, keyed by lesson ID (T-147 makes them per curriculum).

- Only 13 have a practice rule, so the other 47 get no catch-up yet.
- Only the demo lesson has `compare` exercises. Add and subtract lessons start a today's message too, while `TodaysMessage:ArithmeticExercises` is on (the default; `false` turns it off) (D-116, T-128).
- The add/sub lessons have only the 2–3 pairs in their examples; the book pages' full lists are not extracted yet.
- No DIKSHA link per lesson yet (T-94).

## Open tasks

Task files that name this area:

```bash
grep -l "^| Screens | .*curriculum" docs/tasks/[TM]-*.md
```
