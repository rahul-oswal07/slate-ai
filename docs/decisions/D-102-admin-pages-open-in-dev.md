# D-102 · Admin pages open without a key on local dev

| | |
|---|---|
| Status | Accepted |
| Owner | Rahul, 15 Nov |
| Gate | T-112 |
| Screens | admin |

## Decision

Opening `/admin/greetings` or `/admin/lesson-audio` locally meant setting `Admin:Key` in user-secrets and pasting `#key=…` into the link (D-83). Rahul wants them to open directly. The pages show teacher names and voices and children's first names and roll numbers, and staging is public, so the key can go only where nobody else can reach the API.

## Recommendation

- **When the API runs in Development** (`ASPNETCORE_ENVIRONMENT=Development`, i.e. `dotnet run` on a laptop) the admin read routes (`GET /api/admin/greetings`, `…/greetings/{clipId}/audio`, `/api/admin/lesson-audio`, `…/lesson-audio/{clipId}/audio`) answer without `X-Admin-Key`, even with no `Admin:Key` set. `/admin/greetings` and `/admin/lesson-audio` open with no `#key`.
- **Staging and prod are unchanged** (both run as Production): D-83 holds, so a missing or wrong key is a 404 and no `Admin:Key` means no routes. The switch (`AdminOptions.OpenWithoutKey`) is set from the environment only, never from configuration, so no app setting can open the pages on a server.
- **The WhatsApp test send** (`POST /api/admin/whatsapp/test`, T-40) still needs the key in Development, because it sends a real message.
- A `#key=…` link still works everywhere. Without one, a server that needs the key shows "This page needs its full link."

## Notes

- 15 Nov: chosen by Rahul over removing the key everywhere and over teacher sign-in.
