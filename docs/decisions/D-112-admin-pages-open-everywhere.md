# D-112 · Admin pages open without a key, everywhere

| | |
|---|---|
| Status | Accepted |
| Owner | Rahul, 15 Nov 2026 |
| Gate | T-121 |
| Screens | admin, delivery |

## Decision

The admin pages needed `Admin:Key` (at least 16 characters) on staging and prod, sent from the `#key=…` part of the link as `X-Admin-Key` (D-83). Locally they opened without one (D-102). A short key switched the pages off without any warning, and Rahul wants to open them directly, without a key, wherever they run.

## Recommendation

- Every admin route answers without a key in every environment:
  - `/api/admin/greetings`, `/api/admin/lesson-audio` and their clip routes.
  - `/api/admin/lesson-links` (GET, PUT).
  - `/api/admin/children` (GET, PUT phone).
  - `POST /api/admin/whatsapp/test`.
- The web pages under `/admin*` take no `#key`. There is no "needs its full link" state any more.
- `Admin:Key` / `Admin__Key` and `AdminOptions` are removed. The setting is deleted from staging, and prod never had it.
- Admin responses still carry `Cache-Control: no-store` and `X-Robots-Tag: noindex`, and the pages are still linked from nowhere in the app.
- **Accepted risk:** the repo is public, so the paths are known. Anyone with the address can:
  - read teacher names, greetings, children's first names and roll numbers;
  - read and change the WhatsApp number each family's messages go to (`/admin/children`, D-110);
  - change lesson links;
  - send Meta's `hello_world` to a phone listed in `WhatsApp:Recipients`.
- The approval rules (guardrail 3) are unchanged: no admin route sends a lesson message to a parent.

Supersedes the key rule of D-83, and D-102.

## Notes

- 15 Nov 2026: Rahul set staging's key to `7755`, which was too short and switched the pages off. He then asked to remove the key everywhere. He was told about `/admin/children` and the WhatsApp test send and chose to open everything, on every environment. Built in T-121.
