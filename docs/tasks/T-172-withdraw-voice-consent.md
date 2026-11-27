# T-172 · Withdraw her voice consent from Settings

| | |
|---|---|
| Status | Proposed |
| Issue | #350 |
| Owner | R |
| Screens | T10-settings, T0-setup |
| Source | Rahul, 27 Nov 2026 (teacher screens list); D-44 (withdrawing deletes all greeting recordings, Settings → My greetings), D-70, T-133 (admin can already clear it); S |

## What

Only an admin can clear a teacher's voice consent (T-133). Add **My greeting** to Settings: play it, record it again, or **Withdraw consent**. Withdrawing asks first, then clears `ConsentAt` and deletes every greeting recording (D-44), using the same code as the admin's Clear. From then on families get messages without her voice, until she consents again.

## Acceptance criteria

- After withdrawing, no message, today's message or catch-up uses her recording, including ones approved before.
- She can consent and record again from the same place.
- Write endpoint takes `clientRequestId` and is idempotent.
- All new strings in en, kn, te, ta, hi.

## How it's tested

- API tests: withdraw, then what's sent has no greeting; Vitest for the section.

## Notes

- 27 Nov 2026: proposed.
