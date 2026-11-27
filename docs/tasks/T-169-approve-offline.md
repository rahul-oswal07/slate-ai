# T-169 · Approve offline

| | |
|---|---|
| Status | Proposed |
| Issue | #347 |
| Owner | R |
| Screens | T3-approve, app-shell |
| Source | Rahul, 27 Nov 2026 (teacher screens list); D-70 (offline beyond attendance and voice notes); M |

## What

Only the register and voice notes work offline. In a school with poor signal she can read today's message but can't approve it. Keep today's message and catch-ups she has opened in the browser, and let Approve go into the Dexie outbox (`src/offline/`) with the version she saw. When it reaches the server it counts only if that version is still current; otherwise she is told it changed and asked again.

## Acceptance criteria

- An approval replayed after the version changed is refused and shown to her, never applied (guardrail 3).
- The screen shows "Waiting to send" until the approval reaches the server, and the 6 pm time it has to beat.
- Replays are idempotent (`clientRequestId`).
- All new strings in en, kn, te, ta, hi.

## How it's tested

- Outbox tests in Vitest; API test for a stale version.
- Db suite: offline replay idempotency is already on its list; extend it there only if needed.

## Notes

- 27 Nov 2026: proposed.
