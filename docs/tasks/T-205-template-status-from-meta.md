# T-205 · Template status from Meta on the setup check

| | |
|---|---|
| Status | Proposed |
| Issue | #383 |
| Owner | R |
| Screens | delivery, admin |
| Source | Rahul, 27 Nov 2026 (delivery list); extends T-156; S–M |

## What

Whether a template is approved, in review, paused or rejected, per language, is only on Meta's site. On the WhatsApp setup check (T-156), list each template the app uses, per language: Meta's status (from the Graph API's message templates endpoint), and whether the app will send it (config). Flag a language the app sends that Meta hasn't approved, and one approved that the app doesn't use.

## Acceptance criteria

- Read-only; uses the existing token; no secret shown.
- Cached for a few minutes so the page doesn't call Meta on every open.
- Works (with "can't reach Meta") when the token is missing or refused.

## How it's tested

- Unit tests with recorded Graph API responses; Vitest for the list.

## Notes

- 27 Nov 2026: proposed.
