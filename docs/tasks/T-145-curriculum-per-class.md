# T-145 · Curriculum in the database, one per class

| | |
|---|---|
| Status | Done (#310) |
| Issue | #307 |
| Owner | R |
| Screens | curriculum, T1-today, T2-lesson-confirm, admin |
| Source | Rahul, 17 Nov 2026 ("create a class curriculum by parsing the entire textbook"); D-125; first of three (T-146 importer, T-147 review and publish); L |

## What

The foundation for curricula made from a textbook. Nothing changes for teachers or families yet: every class still follows the built-in curriculum.

- **New tables** `Curricula` and `CurriculumLessons` (migration `ClassCurricula`).
  - A curriculum is `draft` or `published`; a published one never changes.
  - Each lesson is stored as the same JSON as a lesson in `data/curriculum/*.json`.
- **`Classes.CurriculumId`**: the published curriculum the class follows. Null is the built-in one from `data/curriculum/*.json`, which is still checked at startup.
- **`ICurriculumProvider.ForClassAsync(classId)`** replaces the app-wide `CurriculumGraph` everywhere: lessons, the lesson picker, voice matching (A1), catch-ups (A2, A3, A4), the evening message, lesson audio, today's message, buddy cards, Week, delivery, the family page and the admin lesson-audio day.
  - A stored curriculum goes through `CurriculumLoader`, so it gets every check a file gets.
  - Its graph is cached by ID.
- **`GET /api/curriculum` is now `GET /api/classes/{id}/curriculum`.** The web caches it per class, offline snapshot included.
- **Built-in only for now:** the admin lesson-links and lesson-content pages, and `data/lesson-content/`, still describe the built-in curriculum. T-147 makes them per curriculum, next to the publish step that first writes one.

## Acceptance criteria

- Every screen and message behaves exactly as before for a class without a stored curriculum.
- A class with a published curriculum lists, saves and names lessons from it only.
- A class pointing at a draft, or at a curriculum that fails the loader's checks, fails loudly rather than teaching from the wrong book.
- Nothing in the API reads an app-wide `CurriculumGraph`. `FakeApi` refuses one, so a test can't register one that is silently ignored.

## How it's tested

- `CurriculumProviderTests` covers: built-in by default, a published curriculum, one read shared by two classes, a draft failing, and a cycle failing the loader.
- `LessonEndpointTests` covers: a class following a stored curriculum gets its lessons and can only pick from them, and the route is for the class's teachers only.
- `CurriculumDbTests` (Db) covers the jsonb round trip, teaching order, and a draft never being served.
- The existing API, Vitest and Db suites are updated for the new signatures and all pass.

## Notes

- 17 Nov 2026: built on `feat/T-145-curriculum-in-db`.
  - Design: the built-in curriculum stays in memory from the files rather than being synced into the tables, so startup and the demo class are untouched.
  - `LessonContents`/`LessonLinks` keyed per curriculum, and the guard on switching a class's curriculum, moved to T-147, where they're first needed.
  - Merge after the finale (18 Nov).
