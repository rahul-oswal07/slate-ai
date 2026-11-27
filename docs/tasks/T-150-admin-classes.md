# T-150 · Classes page: create a class and choose its teachers

| | |
|---|---|
| Status | Proposed |
| Issue | #328 |
| Owner | R |
| Screens | admin |
| Source | Rahul, 27 Nov 2026 (admin pages list); D-79 (`ClassTeachers`), T-115 (class name); needs T-148; M |

## What

Classes and who can sign in to them come only from the seed. Add `/admin/classes`:

- every class with its name (e.g. `3A`), grade, medium, curriculum (link to the picker on `/admin/curriculum`), number of children and its teachers;
- **New class**: name, grade, medium;
- **Edit**: rename, change medium;
- **Teachers**: add or remove a teacher from the class (`ClassTeachers`). A class must keep at least one teacher.

## Acceptance criteria

- Two classes in one grade and medium need different names (the class dropdown on Today tells them apart by name, T-115).
- A class with children or past registers can't be deleted (only an empty one can).
- A teacher added to a class sees it in the class dropdown at her next sign-in; one removed no longer can open it (API refuses, not only the UI).
- Write endpoints take `clientRequestId` and are idempotent.
- `docs/database.md` updated if a migration is needed.
- All new strings in en, kn, te, ta, hi.

## How it's tested

- API tests over `FakeApi` with a fake store; Vitest for the page.

## Notes

- 27 Nov 2026: proposed.
