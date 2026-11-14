# T-112 · Admin pages open without a key on local dev

| | |
|---|---|
| Status | Done |
| Issue | #219 |
| Owner | R |
| Screens | admin |
| Source | Rahul, 15 Nov; D-102; small |

## What

Opening the admin pages locally meant setting `Admin:Key` in user-secrets and adding `#key=…` to the link (D-83). Now, when the API runs in Development, `/admin/greetings` and `/admin/lesson-audio` open directly. Staging and prod are unchanged. The pages show teacher names and voices, plus children's first names and roll numbers, so the key stays wherever the API is reachable by others (D-102).

## Acceptance criteria

- In Development, with or without `Admin:Key`, the four admin read routes answer without `X-Admin-Key`, and both pages load and play clips with no `#key`.
- Outside Development a missing or wrong key is still a 404, and no `Admin:Key` still means no routes.
- No configuration setting can open the pages outside Development (`AdminOptions.OpenWithoutKey` is set from the host environment only).
- `POST /api/admin/whatsapp/test` still needs the key in Development.
- A `#key=…` link still works. Without one, a server that needs it shows "This page needs its full link."

## How it's tested

- `AdminEndpointTests`: Development opens all four routes without a key; Production and Staging still 404 without one; `Admin:OpenWithoutKey=true` can't open them.
- `WhatsAppTestEndpointTests`: in Development the test send without the key is still 404.
- Vitest, `GreetingsAdminScreen.test.tsx` and `LessonAudioAdminScreen.test.tsx`: with no key and an open API the page loads with no key header and a greeting plays; with no key and a 404 the page asks for the full link.
- `./scripts/verify.sh`. No Db suite: no store, query or migration changed.

## Notes

- 15 Nov: built on `feat/T-112-admin-open-in-dev`. `ApiFactory`/`FakeApi` take an optional `environment` (default `Testing`) so tests can run the host as Development or Production. No UI copy or string change.
- 15 Nov, local run: after restarting the API in Development, `GET /api/admin/greetings` returned 200 with no header, a greeting clip played on `/admin/greetings` at 360 px, `/admin/lesson-audio` loaded, and the WhatsApp test send without a key was 404. Axe not run (no new screen; the only layout change is that the nav now shows without a key).
