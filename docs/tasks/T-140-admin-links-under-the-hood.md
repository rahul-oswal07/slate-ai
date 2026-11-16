# T-140 · Under the hood listed on the admin pages

| | |
|---|---|
| Status | Done (#297) |
| Issue | #296 |
| Owner | R |
| Screens | admin, demo |
| Source | Rahul, 16 Nov 2026; T-126; XS |

## What

The demo's Under the hood on its own (`/demo/ai`, T-126) was linked only from `/demo`. It is now listed on `/admin` with its address and what it is for, and in the nav on every admin page. The page itself is unchanged: it stays the demo's page and signs in as the demo teacher.

## Acceptance criteria

- `/admin` lists "Under the hood" last, linking to `/demo/ai`, with the line "Every AI call in the demo since the last reset, on its own page."
- The nav on every admin page has the same link.
- Strings in en, kn, te, ta and hi.

## How it's tested

`AdminIndexScreen.test.tsx`: the list and its links include `/demo/ai`; a test for the new entry's link and description.

## Notes

- 16 Nov 2026: added `/demo/ai` to `ADMIN_PAGES`, so the `/admin` list and every admin page's nav show it. The kn, te, ta and hi strings are drafts for Monika.
