# T-147 · Review and publish an imported curriculum on /admin/curriculum

| | |
|---|---|
| Status | Open |
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

- 17 Nov 2026: planned with D-125.
