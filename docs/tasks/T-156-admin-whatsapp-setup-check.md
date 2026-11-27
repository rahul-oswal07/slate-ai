# T-156 · WhatsApp setup check, read-only

| | |
|---|---|
| Status | Proposed |
| Issue | #334 |
| Owner | R |
| Screens | admin, delivery |
| Source | Rahul, 27 Nov 2026 (admin pages list); T-40, T-104, T-106, T-125; needs T-148; S |

## What

Whether WhatsApp works on a server depends on App Service settings nobody can see from the app. Add a read-only section (on `/admin/deliveries` from T-154, or its own page):

- `Delivery:Channel` (simulated or whatsapp);
- access token, phone number ID, app secret, verify token: set or not set (never the values);
- `WhatsApp:ChatLesson:Enabled`;
- the weekly star languages that are switched on, and per template the languages the app will send;
- the last webhook event received, and when.

## Acceptance criteria

- No secret or part of one is ever returned by the API.
- Each "not set" line says what it stops (e.g. "No app secret: taps in the chat are refused").
- All new strings in en, kn, te, ta, hi.

## How it's tested

- API test that the response holds no secret values, with each setting on and off.
- Vitest for the section.

## Notes

- 27 Nov 2026: proposed.
