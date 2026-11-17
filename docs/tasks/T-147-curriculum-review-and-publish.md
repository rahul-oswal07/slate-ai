# T-147 · Review and publish an imported curriculum on /admin/curriculum

| | |
|---|---|
| Status | Done (#310) |
| Issue | #309 |
| Owner | R |
| Screens | admin, curriculum |
| Source | Rahul, 17 Nov 2026; D-125; needs T-146; M |

## What

- **Imports:** the admin page lists them, with live progress, Try again and Discard.
- **Draft:** shows chapters → lessons with their flags.
- **Lesson editor:** names, key words, pages, description and examples, typed exercises (with answers worked out by the API), `other` exercises, pictures, prerequisites, practice kind, and Mark reviewed.
- **Publish:**
  - only when every lesson is reviewed and the whole curriculum passes `CurriculumLoader`;
  - it picks the classes to give it to, and refuses a class that has already used a lesson ID missing from the new curriculum (409 with the IDs);
  - it writes the descriptions to `LessonContents` for that curriculum.
- **Per curriculum:** `LessonContents` and `LessonLinks` get a key per curriculum.

## Acceptance criteria

- A published curriculum never changes.
- A class never loses lessons it has taught.
- All strings in en, kn, hi, ta, te.

## How it's tested

- Vitest component tests for the screens; no new Playwright (22 Oct rule).
- Endpoint tests for publish and the switch guard.
- Screenshots at 360 px.

## Notes

- 17 Nov 2026: built on `feat/T-147-curriculum-review`, stacked on T-146 (committed locally) and T-145 (#310); each PR goes to `main` after the one below merges.
  - API in `Features/Curricula/`: list, one curriculum, edit and remove a draft lesson, publish, choose a class's curriculum.
  - `DraftRules` keeps every edit inside the curriculum file's rules; prerequisites are earlier lessons only.
  - Migration `LessonContentPerCurriculum`: `LessonContents` and `LessonLinks` keyed by `(CurriculumId, CompetencyId)`; existing rows are the built-in curriculum (`Guid.Empty`); today's message reads the class's own.
  - Web: `/admin/curriculum` (upload, imports, curricula, classes) and `/admin/curriculum/:id` (review, edit, publish), strings in 5 languages (kn, te, ta, hi drafts).
  - Tests: Vitest for both screens; no new Playwright (22 Oct rule); axe not run on the new screens.
  - Left: setting video links for a stored curriculum; screenshots at 360 px for the PR.
- 17 Nov 2026: planned with D-125.
