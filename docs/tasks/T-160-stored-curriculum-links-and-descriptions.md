# T-160 · Video links and lesson descriptions for a stored curriculum

| | |
|---|---|
| Status | Proposed |
| Issue | #338 |
| Owner | R |
| Screens | admin, curriculum |
| Source | Rahul, 27 Nov 2026 (admin pages list); `docs/status.md` Next 7; D-106, D-123, D-125, T-147; S–M |

## What

`LessonLinks` and `LessonContents` are keyed per curriculum since T-147, but `/admin/lesson-links` edits only the built-in curriculum's. A class following a published textbook curriculum has no way to get video links. Add a curriculum picker to `/admin/lesson-links` (built-in and every published one) and edit that curriculum's links and About this lesson.

`scripts/set-lesson-links.mjs` gets a `--curriculum <id>` option.

## Acceptance criteria

- Links and descriptions saved for one curriculum don't show in another.
- A family's today's message uses the link of their class's curriculum.
- The picker remembers the last curriculum chosen on the page.
- All new strings in en, kn, te, ta, hi.

## How it's tested

- API tests for links per curriculum; Vitest for the picker.
- Db suite only if a store query changes (ask first).

## Notes

- 27 Nov 2026: proposed.
