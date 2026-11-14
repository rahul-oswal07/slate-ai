# T11 Profile · spec

Her name, the last 4 digits of her phone and her class, read only.

| | |
|---|---|
| Route | `/profile` |
| Web | `features/profile/`, `app/ProfileMenu.tsx` |
| API | `Features/Auth/` (`GET /api/me`) |
| Design card (Monika) | none yet |
| Status | [`status.md`](status.md) |

## Purpose and states

Opened from the profile menu in the header (Profile · Settings · Log out), which is on every teacher screen except sign-in. `T11` is a new screen ID (15 Nov, T-109); there is no design card yet.

- **Default:** name, phone as "•••• 3210", class as "Grade 3 · Kannada medium", a link back to Today. Today stays the active tab.
- **A row the server didn't send** (a teacher without a class, a `me` cached before this screen existed) is left out.
- **Offline:** shows the last known details, since they come with `me`.

Nothing here can be changed in the app.

## Requirements

No FR of its own. From T-109 and D-101:

- The Profile screen never shows the full phone number, and the full number never leaves the server: `GET /api/me` carries `phoneLast4` only.
- Design tokens only, visible focus, light and dark; strings in all five locale files.

## Decisions cited

D-101.

D-01 to D-99 are in [`../../decisions/D-01-to-D-99.md`](../../decisions/D-01-to-D-99.md); later ones are one file each in [`../../decisions/`](../../decisions/README.md).
