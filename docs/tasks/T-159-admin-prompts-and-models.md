# T-159 · Prompts and models, read-only

| | |
|---|---|
| Status | Proposed |
| Issue | #337 |
| Owner | R |
| Screens | admin, ai-pipeline |
| Source | Rahul, 27 Nov 2026 (admin pages list); guardrails 5 and 6; needs T-148; S |

## What

Which prompt version and which model a server is running is only in the repo and the App Service settings. Add `/admin/prompts`:

- each prompt in `prompts/` with its ID, version header and what it's for;
- the model each one uses (from `Ai:Models:*`), and the speech settings;
- when it was last called on this server and with what verdict (from `AiCalls`, links to T-157 when built).

## Acceptance criteria

- Read-only; shows no keys.
- The prompt text itself isn't shown (version and purpose only), unless decided otherwise.
- All new strings in en, kn, te, ta, hi.

## How it's tested

- API test that every file in `prompts/` is listed with its version; Vitest for the page.

## Notes

- 27 Nov 2026: proposed.
