# T-94 · DIKSHA link per lesson

| | |
|---|---|
| Status | Done |
| Issue | #191 |
| Owner | M finds, R builds |
| Screens | admin, T3-approve, P5-todays-message, curriculum |
| Source | see What |

## What

**DIKSHA link per lesson:** a checked URL field per lesson, shown as "Watch how it's taught" on today's message and the family page; the teacher can remove it; chosen by code, never by the AI *(#191, Finale Must; J1, D-91; size S)*

## Acceptance criteria

Only links Monika checked appear; removed means not sent

## How it's tested

Unit test; Vitest

## Notes

- 15 Nov: **built** (Rahul's answers, D-106: links are set on a new admin page, `/admin/lesson-links`, by whoever checked them; any https link; one per lesson for every class; full scope). New table `LessonLinks` (migration `LessonLinks`); `GET`/`PUT /api/admin/lesson-links` with the D-102 key rule; a fresh today's message gets the first of its lessons' links as its video, by code; Approve → Today's message shows a Video section with Remove (no Edit), and the "remove" rule now counts the sections a message has; the family page already showed the video step. Also, at Rahul's request: `/admin` lists every admin page with its address (links keep `#key=…`; each page's nav has "All pages"). Left: Monika enters the checked Nalikali p 4 link on staging (M-1); kn, te, ta, hi strings of the admin page are drafts.
- 15 Nov: moved here from `tasks.md` (T-110).
