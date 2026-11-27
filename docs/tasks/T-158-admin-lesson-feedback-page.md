# T-158 · Lesson feedback page for the A1 eval cases

| | |
|---|---|
| Status | Proposed |
| Issue | #336 |
| Owner | R |
| Screens | admin, ai-pipeline |
| Source | Rahul, 27 Nov 2026 (admin pages list); T-102 (`GET /api/admin/lesson-feedback`, API only); needs T-148; S |

## What

Teachers' corrections of A1's lesson match are saved as eval cases (`LessonFeedback`, T-102) but can only be read from the API. Add `/admin/lesson-feedback`:

- a From date, then the newest 500 as rows: day, class, what A1 matched and how sure, the lesson she confirmed, whether they differ;
- how many she changed out of all, for the range;
- **Download** as the file the A1 eval reads.

## Acceptance criteria

- Uses the existing endpoint; no names or note text beyond what it returns now.
- The download opens in the A1 eval without edits.
- All new strings in en, kn, te, ta, hi.

## How it's tested

- Vitest for the page and the download; API test for the file format.

## Notes

- 27 Nov 2026: proposed.
