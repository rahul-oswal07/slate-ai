# D-125 · A class's curriculum from its own textbook, kept in the database

| | |
|---|---|
| Status | Accepted |
| Owner | Rahul, 17 Nov 2026 |
| Gate | T-145, T-146, T-147 |
| Screens | curriculum, ai-pipeline, admin |

## Decision

The curriculum is the hand-built `data/curriculum/grade3-maths.json` (T-81, T-87, T-128), loaded once at startup and shared by every class. Adding a book or a grade means days of typing lessons, pages and exercises by hand. Rahul asked for an admin to upload a whole textbook and have Claude turn it into a class's curriculum: chapters, lessons, key words, examples, exercises and pictures, stored in the database.

He chose four things:

1. **Where it lives:** in the database, per class. Options were per class, one for the whole server, or draft JSON files committed after review.
2. **Pictures:** described, not copied. Options were to describe them, to crop and store them, or to keep whole page images.
3. **Who does it:** an admin page. Options were an admin page, the teacher in the app, or an offline script.
4. **Exercises:** the known types plus "other". Options were the known types plus "other", new typed kinds as well, or the known types only.

## Recommendation

- **Per class.** Curricula are stored as draft, then published. A published curriculum never changes; a change is a new curriculum. Each class follows one published curriculum (`Classes.CurriculumId`), and code reads it through `ICurriculumProvider.ForClassAsync`, never an app-wide graph.
- **The built-in curriculum stays as it is.** `data/curriculum/*.json` is checked at startup and stays the curriculum of every class without a stored one (`CurriculumId` null), so nothing changes for the demo class or prod.
- **Every stored curriculum gets every check a file gets.** It goes through `CurriculumLoader`: ID format, names, prerequisites known, no cycles, exercise rules.
- **The importer** (T-146) has an admin upload a PDF. A background job runs Claude prompts A10 over page ranges, then code checks everything and writes a draft. Exercise answers stay in code (guardrail 4).
- **Pictures are described, not copied:** page, rough position, and a description in our own words. The PDF is deleted once the import is published or discarded. Same stance as T-141: our own words, with the book's numbers and methods as facts.
- **Exercises:** `compare`, `extremes`, `add` and `sub` are kept as typed numbers and checked by `ExerciseAnswers.Check`. Anything else is kept as text marked `other`, shown only on the review page; nothing new reaches families.
- **Review and publish** happen on `/admin/curriculum` (T-147). A class can be moved to another curriculum only when every lesson ID it has already used is in the new one.

## Notes

- 17 Nov 2026: accepted by Rahul (per class, describe pictures, admin page, known types + "other"). Build after the finale on 18 Nov, so staging doesn't change under the judges.
